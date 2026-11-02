# Tests et preuves de non-régression

Cette référence remplace la chronologie des campagnes de juillet 2026 par les frontières et contrats encore utiles. La présence d'un test dans le dépôt ne signifie pas qu'il a été exécuté dans la session courante. Cette consolidation du 2 novembre 2026 a inspecté les sources et passé le contrôle statique des assemblies ; elle n'a lancé ni Unity, ni campagne native, ni validation de casque.

## Choisir la preuve

| Comportement | Preuve et emplacement |
|---|---|
| Modèles, parsing, sérialisation, calcul déterministe, cache | EditMode ciblé, principalement `Assets/Tests/EditMode/HBP.Serialization.Tests`. |
| Archive et orchestration de projet | `ProjectArchiveTests` et `HBP.ProjectWorkflow.Tests`, avec dépendances contrôlées. |
| Références Unity, composants, prefabs, matériaux | Tests d'intégrité des assets, sans démarrer toute l'application. |
| Cycle de vie, publication à la frame, scène, interaction | Harness minimal dans les assemblies PlayMode Module3D, Toolbar ou UI. |
| Bootstrap et parcours multi-fenêtres | Quelques smokes Workflow avec le bootstrap réel lorsqu'il fait partie du contrat. |
| Bibliothèque native, pixels, Player, transport réel ou Quest | Suite/qualification explicite dans l'environnement approprié ; un pass Editor ne prouve pas ces frontières. |

Réutiliser les assemblies et helpers existants, nommer le test d'après le comportement et affirmer un résultat observable. Le seul héritage de `MonoBehaviour` ne justifie pas PlayMode ; inversement, EditMode ne prouve pas `Start`, `Update` ou la destruction différée. Un screenshot vérifie une apparence, pas l'identité des données ni la correction des références.

## Carte des contrats existants

Les chemins ci-dessous sont relatifs à `Assets/Tests`. Les listes regroupent les familles principales, sans figer les nombres de tests ou les anciens numéros de phase.

| Famille | Entrées actuelles et contrat à préserver |
|---|---|
| Sérialisation | `EditMode/HBP.Serialization.Tests/ClassLoaderSaverTests.cs`, `LegacyProjectCompatibilityTests.cs`, `SerializationContractAuditTests.cs` : lecture historique, aller-retour courant, noms/types des membres, constructibilité, callbacks et contrats `Clone`, `Copy`, `GenerateID`, `GetAllIdentifiable`. |
| Archives | `ProjectArchiveTests.cs` : structure, identités et références, protocole externe à l'archive, aliases, chemins portables, noms invalides/doublons, sauvegarde répétée sans entrées obsolètes, archives malformées et entrées dangereuses, annulation/nettoyage, récupération lecture seule et progression Ready. |
| Workflow projet | `EditMode/HBP.ProjectWorkflow.Tests/ProjectWorkflowServiceTests.cs` : succès/échec/annulation, restauration du projet et de son emplacement, Save As, confirmations, Quick Start, sauvegarde des configurations, conservation des scènes en cas d'ouverture annulée. |
| Patients/groupes/tags/sites | `PatientsGroupsTagsSitesTests.cs` : identités, copie, références, filtres et import/export tabulaire. Préserver les clés scientifiques, pas seulement les libellés affichés. |
| Protocoles/datasets/conteneurs | `ProtocolDatasetDataContainerTests.cs` : familles de données, traitements et liens aux protocoles ; PlayMode UI/Workflow complète les interactions et la persistance. |
| BIDS/localizer/base | `BidsLocalizerDatabaseWorkflowTests.cs` : découverte, chemins et export reproductibles ; vérifier formats et culture numérique, sans donnée privée. |
| Lecture/traitement/caches | `DataLoadingProcessingCacheTests.cs` et les suites ciblées du domaine : valeurs, normalisation, clé/cache et réutilisation, invalidation, annulation et propriété des ressources. Un test de stockage seul ne prouve pas la publication du résultat. |
| Données d'information | `InformationDataTests.cs`, `EditMode/HBP.ProjectWorkflow.Tests/InformationPresentationTests.cs`, `TrialMatrixValidationScopeTests.cs` : données et portée de validation ; les fenêtres sont couvertes séparément. |
| Module3D | `PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs`, `CutsTriangleErasingPlayModeTests.cs` : configuration/scènes, coupes et effacement avec petits volumes/maillages déterministes. |
| Toolbar | `PlayMode/HBP.Toolbar.PlayModeTests/ToolbarCoveragePlayModeTests.cs`, `CompareSiteToolbarPlayModeTests.cs` : commandes et état observable ; les actions externes passent par des adaptateurs contrôlés. |
| Graphiques/UI | `PlayMode/HBP.UI.PlayModeTests/InformationGraphPlayModeTests.cs`, `ProtocolDatasetUiPlayModeTests.cs` : courbes, matrices, filtres, champs et événements ; jeux synthétiques pour les valeurs et fixture native explicite pour le chemin NIfTI. |
| Parcours utilisateur | `PlayMode/HBP.Workflow.PlayModeTests/MainWorkflowPlayModeTests.cs`, `ProtocolDatasetPlayModeTests.cs` : création/ouverture/enregistrement, fenêtres et intégration des modèles. |
| Architecture et assets | `UnityAssetIntegrityEditModeTests.cs`, `*PlayModeArchitectureTests.cs`, assemblies `HBP.PlatformConfiguration.Tests` et `HBP.Rendering.Tests` : frontières des fixtures, références et configuration. Les tests de structure ne remplacent pas les assertions de comportement. |
| Transfert/synchronisation/Quest | Assemblies dédiées EditMode/PlayMode et utilitaires sous `Assets/Tests` ; sélectionner la famille touchée et le niveau de preuve au lieu de relancer systématiquement le casque. |

Les empreintes et nombres attendus de `SerializationContractAuditTests` évoluent avec les migrations approuvées ; leur source courante est le test, pas un ancien compte de 515 membres ou 376 descripteurs. En cas de différence, inspecter le manifeste, expliquer les ajouts/suppressions/renommages et couvrir la compatibilité avant d'approuver la nouvelle empreinte. Les membres historiques peuvent être pris en charge par le registre d'alias plutôt que rester sur le modèle.

## Fixtures et isolation

Les fixtures doivent être petites, anonymisées/synthétiques, avec identifiants stables. Conserver les formes historiques `$type` nécessaires à la compatibilité. Générer une archive lorsque cela suffit ; ajouter un binaire `.hibop` seulement si la forme binaire protège un cas que le générateur ne représente pas. Les fixtures MNI versionnées sont réutilisables ; un chemin personnel ou un fichier `.artifacts` inexpliqué ne l'est pas.

`ApplicationStateTestScope`, `PersistentDataTestScope` et `TempDirectoryScope` isolent les tests EditMode. `HBP.PlayModeTestUtilities` fournit notamment `PlayModeApplicationStateScope`, `PlayModePersistentDataScope`, `PlayModeTempDirectoryScope`, `PlayModeSceneScope`, `PlayModeWindowHarness`, `PlayModeProjectHarness` et `PlayModeModule3DTestHarness`. Ces helpers organisent le cycle de vie ; les assertions produit restent dans la suite concernée. Attendre la fermeture des opérations, scènes et objets avant de restaurer les états globaux. La restauration doit aussi fonctionner après échec ou annulation, pour qu'une session Play ultérieure n'hérite pas de répertoires temporaires.

Ne jamais lire/modifier les préférences, aliases, historiques, bases ou workspaces personnels pour simplifier une fixture. Pour un cache, vérifier aussi le nettoyage puis la réutilisation : confondre `Clear` et la fermeture définitive du gestionnaire peut invalider un test suivant ou la session utilisateur.

Les dialogues OS, ouverture de fichiers et actions externes sont substitués dans les tests ciblés. Cela prouve la demande et ses paramètres ; une vérification manuelle reste nécessaire lorsque l'intégration réelle au système est le comportement modifié. Le bootstrap complet reste réservé aux parcours qui en dépendent : il masque sinon les responsabilités et alourdit inutilement les suites.

## Asynchronisme et niveau de preuve

Les tests Unity/UniTask utilisent `async Task` et `await` directement. Ne pas bloquer le PlayerLoop avec `.Wait()`, `.Result`, `GetAwaiter().GetResult()`, sommeil ou boucle d'attente ; capturer une exception asynchrone attendue avec un `try/catch` réellement attendu. Les exceptions et annulations doivent être observées par le test. Des rapports historiques Quest T13/T15 ont été invalidés parce qu'un résultat affiché `Passed` masquait une Task annulée : ces résultats ne constituent pas une preuve de réussite. La procédure de validation actuelle protège explicitement ce cas.

Piloter les courses avec une horloge injectée, des barrières et des sources de complétion : démarrer A, le remplacer/annuler, compléter B, puis A, et prouver que seul B publie ou libère l'UI. Vérifier le partage d'opération, l'annulation d'un seul consommateur, les générations obsolètes, le nettoyage d'erreur et l'absence de loader vide lorsqu'un résultat est déjà prêt. Ne pas faire dépendre la course d'un délai réel arbitraire, ni répéter une suite jusqu'à obtenir du vert.

Les suites de synchronisation distinguent contrats/ordonnancement (`Sync.Fast`), transport local (`Sync.Loopback`), handlers de scène (`Sync.SceneFocused`) et qualification/appareil (`Sync.Qualification`, `Sync.Device`). Vérifier les deux origines, identités, application atomique, suppression d'écho, invalidations ciblées et présentation locale conservée. Un effet dérivé se prouve à sa cause ; chaque geste UI ne demande pas une opération réseau supplémentaire.

`NativeFixtureIntegrationTests` sépare les catégories `NativeFixtures` et `NativeDll`. Les chemins natifs des graphiques portent aussi `NativeDll`. Certaines dépendances natives indisponibles déclenchent explicitement `Assert.Ignore` : comptabiliser ces cas comme non exécutés, jamais comme une qualification scientifique réussie. Les tests synthétiques restent utiles, mais ne prouvent ni ABI, ni chargement du plugin, ni rendu final. De même, une comparaison numérique ou une médiane de frame historique ne vaut pas qualification du matériel actuel ; voir [validation du rendu](rendering/rendering-validation.md).

## Exécuter et rendre compte

Les instructions opérationnelles à jour sont dans le skill [hibop-unity-validation](../../.agents/skills/hibop-unity-validation/SKILL.md) : sélection de l'instance Editor, MCP/CLI, résultats, sécurité async et validation appareil. Les anciens lanceurs mentionnés dans la stratégie de juillet ne constituent plus l'entrée de référence. Cette page n'autorise pas une campagne supplémentaire lorsque la mission est documentaire.

Choisir les tests proportionnés à la frontière modifiée, examiner leur résultat réel et distinguer compilation, reload, démarrage, exécution et qualification Player/appareil. Rapporter tests réussis, échoués, ignorés, non exécutés et limitations d'environnement. Un crash utile doit être diagnostiqué ; le marquer ignoré pour obtenir un succès masquerait la couverture manquante. Les workflows GitHub actuels sont déclenchés manuellement : l'ancienne proposition « toutes les suites sur chaque changement » n'est pas une politique CI livrée.

Pour un changement d'assembly, exécuter également [check-assembly-dependencies.ps1](../../Tools/check-assembly-dependencies.ps1). Pour des changements de rendu ou scientifiques, conserver paramètres, entrée, version native, plateforme et métrique afin qu'une comparaison soit reproductible. Les validations supplémentaires découlent d'une modification, d'un échec ou d'une incertitude concrète, sans relancer des suites inchangées par défaut.
