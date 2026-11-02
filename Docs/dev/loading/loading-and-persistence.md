# Chargement, persistance et validation

Référence du code courant, vérifiée le 2 novembre 2026.

- [Séries temporelles et mémoire](timeseries.md) : époques, grilles, statistiques, buffers, Trial Matrix et caches.
- [Tags](tags.md) : inférence, changements de type et réparation.
- [Performance](performance.md) : résultats historiques utiles et reproduction.

Les règles de développement restent dans [Data and persistence](../../agent-knowledge/data-and-persistence.md).

## Propriétaires du pipeline

| Responsabilité | Implémentation |
|---|---|
| Base et workspace | [GlobalDatabase](../../../Assets/Scripts/HBP/Core/Database/GlobalDatabase.cs) |
| Archive et graphe projet | [Project](../../../Assets/Scripts/HBP/Core/Data/Project/Project.cs), [ProjectManifest](../../../Assets/Scripts/HBP/Core/Data/Project/ProjectManifest.cs), [ProjectArchiveReader](../../../Assets/Scripts/HBP/Core/Data/Project/ProjectArchiveReader.cs) |
| JSON et compatibilité | [ClassLoaderSaver](../../../Assets/Scripts/HBP/Core/Tools/ClassLoaderSaver.cs), [SerializationTypeRegistry](../../../Assets/Scripts/HBP/Core/Tools/SerializationTypeRegistry.cs) |
| Liaison et récupération | [LoadingContext](../../../Assets/Scripts/HBP/Core/Data/LoadingContext.cs), [LoadingRecovery](../../../Assets/Scripts/HBP/Core/Data/LoadingRecovery.cs) |
| Travail partagé | [SharedLoadingOperation](../../../Assets/Scripts/HBP/Core/Tools/SharedLoadingOperation.cs), [LoadingWorkScheduler](../../../Assets/Scripts/HBP/Core/Tools/LoadingWorkScheduler.cs) |
| Aspects et invalidation | [ValidationModel](../../../Assets/Scripts/HBP/Core/Tools/ValidationModel.cs), [ValidationImpactAnalyzer](../../../Assets/Scripts/HBP/Core/Tools/ValidationImpactAnalyzer.cs) |
| Ouverture/création/SaveAs | [ProjectWorkflowService](../../../Assets/Scripts/HBP/UI/Tools/ProjectWorkflowService.cs) |

Le pipeline est : lecture/désérialisation → réparation du graphe détaché → index
et liaison → publication atomique (`Ready`) → validation des aspects demandés.
Les callbacks JSON restaurent les invariants locaux, sans rechercher les relations
dans les singletons ni ouvrir les sources scientifiques.

`LoadingContext` construit les index ordinaux tags/protocoles/patients/datasets
et blocs par protocole. Les colonnes conservent les instances résolues. Base et
projet utilisent chacun leur scope capturé, sans fallback implicite entre eux.
Une collision structurelle exclut les objets ambigus et leurs dépendances en
récupération lecture seule; elle ne choisit pas arbitrairement le premier objet.
Le workflow conserve le projet et l'emplacement précédents en cas d'échec ou
d'annulation; Quick Start restaure son snapshot à l'annulation.

## Contrat JSON et archive

Les JSON sont streamés en UTF-8 sans BOM, indentés pour l'édition manuelle, avec
settings de lecture/écriture séparés et `TypeNameHandling.Auto`. Les anciennes
formes compactes sont lisibles. `BaseData.ID` est généré au premier accès si nul
(`Volatile`/`Interlocked`), ou réparé à la désérialisation si absent/vide. Une
chaîne vide affectée explicitement au runtime reste détectable par la validation.

Une archive `.hibop` conserve `<nom>.settings`, `Patients/`, `Groups/`,
`Datasets/`, `Visualizations/`, avec entrées de dossiers même vides. Le dossier
historique `Protocols/` est accepté mais ignoré : les protocoles globaux sont
canoniques. Les entrées supplémentaires sûres restent tolérées.

`ProjectManifest` est un index en mémoire (`SchemaVersion == 0`, legacy-v0),
pas un fichier supplémentaire. Sa réutilisation dépend de la longueur et de la
date du ZIP. Les entrées sont lues directement, sans extraction de chargement.
Des lecteurs ZIP indépendants sont loués à un pool borné, car les streams
DotNetZip d'une même instance partagent leur position. Tous sont fermés même
sur erreur/annulation. Les noms absolus, chemins lecteur/UNC, segments `.`/`..`
et doublons après normalisation séparateurs/casse sont rejetés.

Le format v2 à discriminants courts proposé dans l'ancien audit n'est pas le
format actif. Le streaming et l'indexation ont été retenus en conservant la
rétrolecture et l'édition manuelle, sans imposer cette migration de schéma.

## Types et propriétés sérialisés

Le registre généré autorise les noms de types, avec alias historiques compilés;
il ne fait ni scan runtime des assemblies ni fallback `Type.GetType`. Les noms
d'assembly historiques restent acceptés pour un type connu : l'assembly n'est
pas la clé d'autorisation. Un type inconnu échoue explicitement.

Les deux fragments Core/Data préservent Data → Core. Le générateur Editor
`Tools/Serialization/Generate Type Registry` consomme
[SerializationTypeAliases.json](../../../Assets/SerializationTypeAliases.json).
Les alias de propriétés utilisent le nom JSON et le propriétaire exact, sont
lus mais jamais écrits, et refusent deux noms visant le même membre. Les
transformations de valeurs passent par `SerializationPropertyMigrations`;
l'alias CCEP de `DynamicConfiguration` vers `CCEPConfiguration` préserve l'identité
sans renommer globalement toutes les configurations dynamiques.

Play refuse un registre périmé; HBPBuilder régénère avant build et demande une
recompilation si nécessaire. Le garde prebuild des autres chemins valide sans
réécrire pendant le préprocessing. Les `typeof` et
[link.xml](../../../Assets/link.xml) sont complémentaires : les membres réfléchis
par Json.NET doivent aussi survivre au stripping. Les seuils de configuration
sont persistés en ratios; les anciennes valeurs absolues restent lisibles jusqu'à
résolution, puis seule la représentation courante est écrite.

## Disponibilité et validations ciblées

`Ready` garantit un graphe cohérent publié, sans garantir l'existence de ses
fichiers. `Validated` vaut pour un périmètre et des aspects précis.
`ValidatedWithIssues` décrit des données manquantes/invalides; `ValidationFailed`
conserve une exception technique, bloque l'opération dépendante et permet un retry.

L'opération partagée fournit progression, tâches et exception observables. Plusieurs
consommateurs rejoignent le même travail. Annuler une attente ne détruit pas
nécessairement le travail utile aux autres. Remplacer projet/workspace invalide
la génération; une ancienne génération ne peut plus publier. Les workers
préparent des résultats distincts avant application groupée.

Au démarrage/ouverture, les contrôles portent sur disponibilité et empreintes
légères, sans lecture native EEG ni scan CSV complet. Browse Database demande
`Ready`; Trial Matrix, visualisation et export demandent les prérequis de leurs
données sélectionnées. Save attend les validations pertinentes déjà déclenchées,
sans forcer toute sémantique périmée. Le rapport d'intégrité explicite est le
consommateur d'une validation complète.

Les aspects sont Structure, SourceAvailability, SourceReadability, StaticContent,
Epoching, ChannelMapping et PatientAssets, avec états, diagnostics et signatures
sérialisés. La fusion des requêtes conserve les cibles de chaque aspect; elle
ne crée pas un produit croisé. Changer un événement principal revalide l'epoching
des sous-blocs concernés; un code secondaire, des coordonnées ou tags de site
ne justifient pas une nouvelle lecture EEG. Changer les noms des sites revalide
leurs liaisons; changer le canal stimulé CCEP demande une liaison en mémoire.
Une opération partage son ouverture native pour triggers/channels et réutilise
les métadonnées d'un chargement réel quand disponibles.

Le loader s'attache à l'opération en cours sans la redémarrer. Son propriétaire
seul peut le fermer; une action composée conserve son affichage entre validation
et chargement. Les prérequis déjà satisfaits n'ouvrent pas un loader vide.

## Concurrence et diagnostic

Sans override, avec `P = max(1, Environment.ProcessorCount)` :

| Catégorie | Limite |
|---|---:|
| JSON/ZIP et budget global partagé | `min(8, P)` |
| Chemins | `min(8, P)` |
| Métadonnées | `min(4, max(1, P/2))` avec division entière |
| Natif | `min(2, max(1, P/4))` avec division entière |

Le multithreading désactivé impose 1. `HIBOP_LOADING_CONCURRENCY_OVERRIDE` positif
force la limite de diagnostic (sans plafond CPU); `HIBOP_BACKGROUND_VALIDATION=false`
ou `0` fait attendre la validation dans le même pipeline. Le scheduler partage
ses places entre base et projet et favorise les attentes visibles sans interrompre
les tâches déjà actives. Les résultats gardent l'ordre des entrées.

## Validation et limites

Les tests de [sérialisation](../../../Assets/Tests/EditMode/HBP.Serialization.Tests)
couvrent `ClassLoaderSaverTests`, `LoadingContextTests`, `ProjectArchiveTests`,
`SerializationTypeRegistryTests`, `SharedLoadingOperationTests`,
`LoadingWorkSchedulerTests`, `TargetedValidationTests` et les barrières projet.
Les [tests de workflow](../../../Assets/Tests/EditMode/HBP.ProjectWorkflow.Tests)
vérifient publication et restauration; le round-trip registre Player existe dans
[SerializationTypeRegistryPlayModeTests](../../../Assets/Tests/PlayMode/HBP.Workflow.PlayModeTests/SerializationTypeRegistryPlayModeTests.cs).

La campagne historique de juillet a rapporté un round-trip Windows IL2CPP,
sans exécution Linux IL2CPP équivalente établie dans ce corpus. Cela ne qualifie
pas le checkout actuel. Le diagnostic des partages réseau et la qualification
Player nécessitent leurs environnements propres; voir
[procédure Unity](../../../.agents/skills/hibop-unity-validation/SKILL.md).
