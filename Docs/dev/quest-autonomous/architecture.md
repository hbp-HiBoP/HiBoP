# HiBoP autonome sur Quest

Référence de développement relue contre le code le 2 novembre 2026. Les résultats matériels datés sont regroupés dans [validation.md](validation.md) ; ils ne constituent pas une nouvelle qualification du code courant.

## Répartition des responsabilités

Desktop prépare et ouvre les visualisations. Quest reçoit les données préparées et utilise localement le modèle de scène, les configurations, les générateurs et le rendu communs. Une coupure réseau ne transforme pas Quest en lecteur vidéo : les scènes déjà publiées restent locales. Le transport d'une scène, la réplication de ses mutations et les fichiers projet sont trois contrats distincts.

Le dépôt Unity contient les profils Desktop et Quest. `HBP.Core.Runtime` reste le socle sans dépendance vers les autres assemblies `HBP.*`. Data porte les scènes ; Transfer leur capture et restauration ; Sync les commandes ; Quest et UI les adaptateurs de plateforme. La composition Quest part de [QuestBootstrap](../../../Assets/Scripts/HBP/Quest/Runtime/QuestBootstrap.cs) et de [QuestAnatomySession](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs). Les règles d'assembly et de travail Unity sont dans [AGENTS.md](../../../AGENTS.md).

| État | Propriétaire et portée |
| --- | --- |
| Scènes, colonnes, patients, données préparées, coupes, sélection, paramètres scientifiques | Modèle commun ; mutations répliquées selon le [contrat Sync](sync/synchronization-architecture.md). |
| Placement physique, taille du cerveau dans la pièce, caméra, fenêtres, guides, feedback des manettes | Présentation locale Quest. |
| Installation MNI/atlas, caches et handles natifs | Ressources locales possédées ; une identité logique ne vaut pas empreinte universelle de tous les fichiers installés. |
| État de connexion et publications | Session authentifiée, incarnations et livraisons identifiées ; aucune déduction à partir du seul nom de visualisation. |

La [référence scène/colonne](shared-scientific-scenes.md) précise scènes multi-patients, vues mono-patient, colonnes et visibilité. Une scène multi-patients n'embarque pas les anatomies individuelles simplement parce qu'elles ont été préchargées sur Desktop. Les définitions globales de protocoles/tags sont transmises lors de l'association puis leurs sauvegardes sont synchronisées pendant la session.

## Données et calcul local

[SceneRestoration](../../../Assets/Scripts/HBP/Transfer/Scene/SceneRestoration.cs) reconstruit les données préparées et les ressources natives privées, initialise la scène commune, applique sa configuration et attend `PrepareRenderingAsync` avant publication. Les résultats dérivés sont reconstruits à partir des données et paramètres ; la livraison ne remplace pas cette préparation par une image figée. Elle n'impose pas non plus un calcul scientifique non demandé pour satisfaire une mesure de performance.

La capture courante prend en charge les modalités représentées par les colonnes communes, leurs essais et séries préparées. La présence du code et des tests de restauration ne prouve pas une recette matérielle exhaustive de chaque protocole. L'import EEG brut sur Quest, l'édition complète Desktop et l'ouverture d'un projet portable depuis le casque ne sont pas à déduire du transfert de scène. Un cache de ressources n'est pas une sauvegarde de session ; aucune garantie générale de conservation du travail non sauvegardé après arrêt du processus n'est apportée ici.

Les coordonnées scientifiques sont en millimètres. Les wrappers à la frontière `hbp_core` réalisent le changement de repère natif/Unity (X et winding) décrit dans [le contrat de coordonnées](../hbp_core_coordinate_system.md). La présentation Quest applique ensuite la conversion mm→m et sa pose locale, sans nouvelle réflexion native. Les matrices, normales, indices, couleurs linéaires et états de surface ne doivent pas subir une seconde conversion lors du transport. Points d'entrée : [ReferenceSystemConversion](../../../Assets/Scripts/HBP/Core/DLL/HbpCoreValueTypes.cs), [QuestColumnPresentation](../../../Assets/Scripts/HBP/Quest/Runtime/QuestColumnPresentation.cs).

La calibration `SpanMin/Middle/SpanMax` porte sur les données préparées, sans renormalisation à l'instant sélectionné. Pour une navigation temporelle non alignée, la surface et les sites suivent leurs règles communes : le site évalue `TemporalSample`, la surface utilise l'index de projection défini par sa politique. L'alpha temporel d'interpolation ne doit pas être confondu avec `ActivityAlpha`, qui règle l'opacité. Les masques scientifiques et la visibilité de présentation sont également distincts.

## Durée de vie et concurrence

Les objets Unity, les mutations du graphe vivant et la publication restent sur le thread Unity. Les workers reçoivent des données détachées ou des ressources retenues explicitement. Annuler une tâche n'interrompt pas un appel natif synchrone : rejoindre le travail précède toute destruction de fichier, buffer ou handle.

Les lectures GIFTI de géométrie et MarsAtlas partagent le verrou statique de [Surface](../../../Assets/Scripts/HBP/Core/DLL/Surface.cs), car le parseur possède un état global. Ce verrou ne remplace pas l'ordonnancement des jobs scientifiques. [MRI3D](../../../Assets/Scripts/HBP/Core/Object3D/MRI3D.cs) sérialise chargement, lecture paresseuse et nettoyage de son volume ; cette protection ne dispense pas les propriétaires de drainer leurs lecteurs avant fermeture.

Consulter [transfert et Display](transfer.md), [interactions](interactions.md), [déploiement](deployment.md) et [USB/réseau](usb-and-network-connectivity.md) pour leurs contrats. Les anciennes étapes du prototype et leurs listes de tâches ont été absorbées dans ces références ; elles ne constituent pas un backlog courant.
