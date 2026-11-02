# Tags : import, conversion et réparation

Les définitions sont globales; les valeurs patient/site et les filtres les
référencent par ID. Cette référence décrit le code vérifié le 2 novembre 2026.
Les conventions opérationnelles restent dans
[Tags and imports](../../agent-knowledge/tags-and-imports.md).

## Import

`GlobalDatabase.UpdateDatabaseAsync` utilise le scan de toutes les références de
métadonnées configurées, même non sélectionnées pour l'update, avant de construire
un draft. [TagImportScanner et les importeurs](../../../Assets/Scripts/HBP/Core/Database)
alimentent les services de [Core/Data/Tags](../../../Assets/Scripts/HBP/Core/Data/Tags).
La clé associe scope patient/site et nom normalisé; un `GeneralTag` existant est
résolu explicitement. L'ordre des sources ne doit pas changer le résultat.

`TagInferenceService` ignore les cellules vides et les tokens exclus, puis choisit
Bool → Int → Float → String. Sans valeur significative, il choisit String.
Zéro seul est un entier, et Enum reste un choix manuel. Un import ne retype
jamais un tag existant : il rapporte les valeurs incompatibles et laisse la
source externe intacte. Il peut étendre un enum par ajout ordonné en fin de liste.

La politique immuable trimme les tokens, ignore leur casse, les déduplique et
rejette les chevauchements. Valeurs par défaut : `true, yes`; `false, no`;
valeurs ignorées `n/a, na, nan, null, none, -, not found`. `0/1` ne sont pas
booléens par défaut. Le scan ne publie ni ne sauvegarde de tags; le draft et
la base sont validés avant publication, avec restauration sur erreur de persistance.

## Enums et identité

`EnumTagValue` et `EnumTagFilterValue` conservent `Value` (index historique) et
`StringValue` (label faisant autorité). Après résolution,
`Tag.Values[Value] == StringValue`. Une divergence se répare par comparaison
ordinale exacte du label, jamais par clamp vers l'option zéro. Les labels sont
uniques; l'ajout append-only préserve les indices existants.

Un enum ancien sans texte peut retrouver son label par un index valide dans la
définition courante, avec avertissement d'ambiguïté. Si la définition a changé
vers un type non-enum, le sens ancien est irrécupérable : aucun historique de
schéma ni identifiant stable d'option n'existe. Réordonner, renommer ou supprimer
des options doit donc informer des conséquences sur les anciens fichiers.

`TagCollection` maintient des vues stables et un index ordinal O(1) moyen,
reconstruits ensemble après mutation. L'ordre `AllTags` reste patient, site,
général. Deux objets différents ayant le même ID sont refusés sans altérer la
collection précédente; une même instance peut figurer dans plusieurs catégories.

## Changement manuel

[TagCollectionModifier](../../../Assets/Scripts/HBP/UI/Main/Tags/TagCollectionModifier.cs)
prépare un `TagSchemaMigrationPlan` sur clones de la base chargée, du projet ouvert
et des filtres globaux, puis demande confirmation du plan. Les changements de
contexte pendant la confirmation sont détectés et imposent une nouvelle préparation.

La conversion centrale préserve ID de définition, IDs des valeurs/filtres,
propriétaire exact et position. Les références sont rebondées vers la définition
canonique. Float → Int exige une valeur finie entière; un enum moderne se convertit
par son texte; contraintes Min/Max/Clamp s'appliquent après conversion sémantique
et les pertes sont rapportées.

Les valeurs impossibles sont retirées et rapportées; un filtre incompatible
(par exemple `contains` texte vers entier) est retiré dans le plan avec
avertissement. Il ne bloque pas à lui seul le plan et n'exige pas un second
dialogue de suppression. Le rollback restaure les collections et bindings.
La sauvegarde des définitions, filtres et base suit la confirmation; le projet
ouvert n'est pas sauvegardé silencieusement. Si la restauration des fichiers
échoue elle aussi, l'UI expose cette erreur : l'atomicité mémoire n'est pas une
garantie de transaction de fichiers résistante à tout incident système.

## Réparation à l'ouverture

`DeferredTagMigrationService` intervient sur le graphe non publié, avant la
résolution générique de `LoadingContext`. Il conserve les valeurs compatibles,
convertit les autres si possible et rapporte les valeurs supprimées par patient
et site. Les valeurs brutes supprimées ne sont pas sérialisées dans un refuge.
Les enums modernes peuvent étendre les options; les indices legacy invalides
sont supprimés avec rapport.

Les presets conservent leur nom et leur ID, même vides. La réparation descend
dans les conditions : seules les feuilles invalides sont retirées; All/Any à
un enfant devient cet enfant avec `child.IsNot ^= group.IsNot`; un groupe vide
est supprimé. L'UI informe après publication. Une sauvegarde normale persiste la
réparation, sans `.bak` dédié et sans répéter le rapport au prochain chargement.

Les données structurelles corrompues ont un autre traitement : récupération en
lecture seule, conservation de la source et interdiction de sauvegarder un
sous-graphe incomplet. Cela concerne aussi un fichier global Tags/presets illisible.

## Limite observée et vérification

L'import construit sa politique depuis `UserPreferences.Data.TagImport`.
En revanche, les appels actuels de migration dans `Project`, `GlobalDatabase`,
`PersistentDataManager` et `TagCollectionModifier` passent `TagParsingPolicy.Default`.
Les préférences personnalisées d'import ne sont donc pas garanties lors d'une
migration; l'ancien plan qui les promettait à l'ouverture dépassait le code.
Ce constat documentaire ne modifie pas le produit.

Tests existants : `TagInferenceTests`, `EnumTagReferenceTests`,
`TagCollectionIndexTests`, `TagSchemaMigrationTests`, `DeferredTagMigrationTests`
dans [HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests).
Ils vérifient notamment ordre des sources, propriétaires/IDs, rollback,
suppression ciblée des filtres et réparation idempotente. Leur présence n'est pas
une exécution récente ni une qualification Player.
