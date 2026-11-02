# Scène scientifique commune Desktop et Quest

Desktop et Quest exécutent le même modèle scientifique `Base3DScene` / `Column3D`. La plateforme change la présentation et les interactions, pas la définition d'une colonne ou de ses calculs. Les [comparaisons multimodales retenues](../../../Validation/Quest/retained-results.md) documentent la qualification historique et ses limites. Le protocole de livraison est décrit dans [transfert](transfer.md), les mutations dans [synchronisation](sync/synchronization-architecture.md).

## Propriétaires

| Élément | Responsabilité |
| --- | --- |
| [Base3DScene](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs) et ses partiels | Modèle scientifique, initialisation, ressources, configurations, calculs, fermeture et capture cohérente. |
| [Column3D](../../../Assets/Scripts/HBP/Data/Module3D/Column3D.cs) et six prefabs de colonnes | Anatomie/densité, iEEG, CCEP, MEG, fMRI et statique ; état scientifique mutable propre à chaque colonne. |
| [DesktopScenePresentation](../../../Assets/Scripts/HBP/Data/Module3D/DesktopScenePresentation.cs) | Vues, caméras et wrappers Desktop ; aucune obligation d'en créer pour une scène Quest. |
| [QuestAnatomyView](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) | Restauration du contenu commun et présentation Quest. Les poses des wrappers et leur échelle visuelle restent locales. |
| [SceneRestoration](../../../Assets/Scripts/HBP/Transfer/Scene/SceneRestoration.cs) | Construction de la scène depuis les ressources préparées, état complet et publication après préparation. |

Le prefab `Scene3DContent` ne simule pas un ensemble de fenêtres Desktop cachées. Les six modalités restent accessibles au code commun. Les coordonnées scientifiques, données natives, meshes/coupes, ROI, sources et état d'activité ne dépendent ni d'une caméra Desktop ni d'une pose Quest.

Les ressources préparées immuables peuvent être partagées entre colonnes. Les objets mutables — sélections, coupes, filtres, masques, timelines et sorties — ont un propriétaire explicite ; le changement d'une colonne ne doit pas modifier ses voisines par aliasing. Les clés de ressources et de colonnes servent aussi à la livraison et aux opérations, pas des noms d'objets Unity transitoires.

## Préparation, capture et publication

[Base3DScene.Transfer](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Transfer.cs) expose `CapturePreparedAsync`. La capture attend initialisation et anatomie, charge les ressources manquantes depuis les listes **courantes de la scène**, attend la barrière des surfaces et les générateurs/corrélations/colliders concernés, puis copie l'état sur le thread Unity sans mutation intercalée. Le verrou de préparation protège le choix exact ressource/représentation ; il ne suffit pas d'attendre uniquement le premier chargement de la scène.

La livraison conserve les enregistrements, essais, statistiques et ressources utiles de toutes les colonnes ; elle ne change pas le format de persistance des projets Desktop. La scène livrée conserve ses alternatives préparées : choix de mesh/hémisphère, IRM, sources de données et état des colonnes. Une restauration prépare avant publication ; une erreur ne publie pas un contenu partiel. Un nouvel envoi produit une nouvelle incarnation et termine l'ancienne avant de remplacer la vue. Les opérations live ne servent pas de mécanisme implicite pour expédier une ressource inconnue.

Le format initial expérimental décrit par les anciens lots a évolué : se référer au codec et à [transfer](transfer.md) pour les versions acceptées. De même, le live snapshot des premiers essais a été remplacé par les opérations typées ; ne pas réimplémenter les anciens adapters comme deuxième modèle scientifique.

## Travail asynchrone et durée de vie native

[Base3DScene.Lifetime](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Lifetime.cs) est la frontière de fermeture. Fermer, remplacer ou annuler interdit toute nouvelle publication puis draine **tous** les travaux déjà commencés avant libération des ressources. L'échec du premier travail attendu n'autorise pas à oublier les suivants. Les coroutines de présentation, générateurs et callbacks tardifs ne doivent plus publier dans une scène fermée.

Un token annulé interrompt une intention ou une attente ; il ne prouve pas la fin d'un appel natif en cours. Les buffers et handles empruntés restent retenus jusqu'au retour réel, y compris lorsqu'une génération plus récente existe. Les résultats tardifs sont écartés par identité/génération sans libérer les entrées encore utilisées.

Les sockets/.NET, continuations de transport et barrières partagées gardent des `Task` quand ce contrat est requis ; les chemins Unity peuvent employer `UniTask` avec transitions de thread explicites. Le seul nombre de consommateurs ne justifie pas à lui seul une migration de type. `Task` et `UniTask` ne sont pas interchangeables mécaniquement : un travail partagé multi-attente doit conserver une représentation réattendable ; une continuation qui accède à Unity doit revenir explicitement au thread principal. L'annulation d'un seul observateur ne doit pas détruire le travail partagé. Les attentes de PlayerLoop sont asynchrones, sans `.Wait()`, `.Result` ni boucles de blocage. Les problèmes observés et essais datés sont dans [validation](sync/synchronization-validation.md).

## Vérifier une évolution de scène

[SceneQualification](../../../Assets/Scripts/HBP/Transfer/Scene/SceneQualification.cs), [SceneQualificationDiagnostic](../../../Assets/Scripts/HBP/Dev/SceneQualificationDiagnostic.cs) et [QuestSceneDiagnostic](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSceneDiagnostic.cs) exportent des données comparables de la scène. Les tests `SceneRestorationPlayModeTests` génèrent leurs projets avec [SyntheticSceneProjectFixture](../../../Assets/Tests/PlayMode/HBP.PlayModeTestUtilities/SyntheticSceneProjectFixture.cs), puis exécutent directement le diagnostic Desktop et la scène restaurée. Aucun ancien build ou dossier `.artifacts` préparé manuellement n'est nécessaire. [Run-QuestSceneQualification.ps1](../../../Tools/Run-QuestSceneQualification.ps1) et [Compare-SceneQualification.py](../../../Tools/Compare-SceneQualification.py) restent réservés aux mesures matérielles et comparaisons de plateformes explicitement sélectionnées.

Une comparaison utile couvre les six modalités en MNI et patient, alternatives de ressources, buffers géométriques et scientifiques, restauration et recalcul. Mesurer tailles/identités, écarts maximaux et RMS ; ne pas inventer une tolérance globale à partir d'une capture visuellement correcte. La mémoire Unity échantillonnée par ces diagnostics n'est ni le pic RSS Windows ni le pic PSS Android. Les artefacts doivent nommer la configuration, les binaires et les données effectivement utilisés.

Les tests [SceneRestorationPlayModeTests](../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/SceneRestorationPlayModeTests.cs) et [Module3DScenePlayModeTests](../../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs) complètent les tests de capture/restauration et de durée de vie. Leur présence ne qualifie pas automatiquement le rendu stéréo, les interactions ou la parité Windows/ARM.
