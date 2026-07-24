# Étape 4 — Contexte explicite de liaison

Date : 24 juillet 2026

## Résultat

La désérialisation ne consulte plus les singletons pour relier les tags,
protocoles, patients, datasets ou blocs. Les objets conservent leurs
identifiants sérialisés jusqu'à une passe explicite de `LoadingContext`.

Le pipeline de la base et du projet est maintenant :

```text
lecture -> désérialisation brute -> construction des index
        -> liaison des références -> validation des fichiers
        -> publication atomique
```

Le format JSON n'est pas modifié.

## `LoadingContext`

Le contexte construit une seule fois les index canoniques :

```text
TagById
ProtocolById
PatientById
DatasetById
BlocByIdByProtocolId
```

Les index utilisent une comparaison ordinale des IDs. Deux objets distincts
portant le même ID provoquent une erreur explicite dès la construction. La
même instance de tag peut néanmoins rester présente dans plusieurs catégories
de `TagCollection`, conformément au contrat existant.

Chaque résolution est ensuite un accès dictionnaire. La complexité de la passe
est donc `O(nombre d'objets + nombre de références)`, au lieu des parcours
répétés des listes globales.

## Objets migrés

La passe couvre :

- les `BaseTagValue` des patients et de leurs sites ;
- `DataInfo` et `Dataset` vers leur protocole ;
- `PatientDataInfo` vers le patient du scope courant ;
- `Group` et `Visualization` vers les patients du projet ;
- les colonnes IEEG, CCEP, FMRI, MEG et statiques vers leur dataset ;
- les colonnes IEEG et CCEP vers leur bloc, indexé dans le protocole ;
- `PatientConfiguration` ;
- les conditions de filtre patient/site liées aux tags, y compris les
  conditions imbriquées `All`, `Any` et `MultipleSiteTags`.

Les colonnes conservent maintenant les instances résolues de leur dataset et
de leur bloc. Leurs getters ne parcourent plus
`ApplicationState.LoadedProject`.

Les constructeurs sans paramètre de `Dataset` et des variantes de `DataInfo`
ne sélectionnent plus implicitement le premier protocole global. Cette
sélection aurait conservé une dépendance à l'ordre d'initialisation pendant la
création Json.NET.

## Scopes et erreurs

Le contexte de la base est construit avec les patients de la base en cours de
chargement. Celui du projet est construit avec les patients et datasets du
projet en cours de chargement. Il n'existe plus de fallback implicite entre
les deux scopes.

Une référence structurante absente — protocole, patient, dataset ou bloc —
est collectée avec le type de l'objet propriétaire. Toutes les références
absentes de la passe sont ensuite levées dans une seule
`ReferenceResolutionException`.

Les tags inconnus conservent leur politique historique : ils ne rendent pas le
chargement invalide et sont retirés par `Patient.CheckTagsAsync`. Les filtres
pointant vers un ancien tag restent chargés avec `Tag == null` et s'affichent
comme non supportés.

Lors d'une modification interactive de `Project.SetPatients`, les références
qui ne font plus partie de la nouvelle liste sont supprimées comme auparavant,
mais la remise en correspondance utilise maintenant un index local.

## Publication atomique

`GlobalDatabase.LoadDatabaseAsync` garde patients et `DataInfo` dans des listes
locales jusqu'à la fin de la liaison et de la validation des fichiers.

`Project.LoadAsync` garde préférences, patients, groupes, datasets et
visualisations dans des variables locales jusqu'à la réussite des mêmes
phases.

`ProjectWorkflowService` ne remplace
`ApplicationState.LoadedProject` et son emplacement qu'après la réussite du
chargement. Le projet précédent reste donc publié pendant toute l'opération et
est toujours présent en cas d'échec ou d'annulation.

Les méthodes historiques `LoadFromFile` restent utilisables : elles créent un
contexte explicite à leur frontière à partir du scope actuellement publié, au
lieu de dépendre des callbacks Json.NET.

## Compatibilité

- aucun champ JSON n'a été ajouté, retiré ou renommé ;
- les IDs continuent d'être écrits par les callbacks de sérialisation ;
- les fichiers historiques restent lus par le binder existant ;
- les archives projet historiques et les variantes de conteneurs de données
  sont couvertes par les suites existantes ;
- l'implémentation utilise uniquement des types génériques et des branches de
  types explicites, sans réflexion ajoutée ni génération dynamique.

## Validation automatisée

`LoadingContextTests` vérifie :

- la liaison exacte vers chaque instance canonique ;
- la priorité déterministe du patient de projet ;
- les index dataset/protocole/bloc des colonnes ;
- le rejet des IDs dupliqués ;
- le regroupement des références absentes ;
- l'absence de liaison pendant la désérialisation brute ;
- les conditions de filtre liées aux tags ;
- `PatientConfiguration`.

Les tests de workflow vérifient également que le nouveau projet n'est pas
publié pendant son chargement.

Résultats du 24 juillet 2026 :

- `HBP.Serialization.Tests` : **394 / 394** ;
- `HBP.ProjectWorkflow.Tests` : **15 / 15** ;
- sous-ensemble archives et compatibilité historique : **61 / 61**.

La console Unity ne contient aucune erreur de compilation.

## Benchmark

Les mesures avant et après implémentation ont été capturées sur la nouvelle
machine, le workspace `Default` et le projet `full_test`.

Sur la médiane de trois passes chaudes, le coût instrumenté cumulé de la
liaison et de l'ancien `BindTags` baisse de **80,2 %** sur la base et de
**87,2 %** sur le projet. Le temps mural total baisse de **5,1 %** sur la base
et de **17,1 %** sur le projet par rapport à la capture de référence effectuée
avant l'implémentation.

Le protocole, les tableaux détaillés, les limites d'interprétation et les noms
des fichiers bruts sont consignés dans
[`resultats_etape_4_2026-07-24.md`](resultats_etape_4_2026-07-24.md).
