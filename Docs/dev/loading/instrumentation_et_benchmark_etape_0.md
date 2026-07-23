# Instrumentation et benchmark de chargement — étape 0

Date : 23 juillet 2026

## Objectif

Cette instrumentation temporaire verrouille la baseline avant les
optimisations. Elle est active systématiquement et n'altère ni le JSON lu ni
les objets chargés. Elle sera retirée à la fin des optimisations.

Toute l'implémentation centrale se trouve dans :

```text
Assets/Scripts/HBP/Core/Tools/LoadingDiagnostics.cs
```

Les appels dans le code métier portent le commentaire :

```text
TEMP-LOADING-PROFILING
```

Cette convention rend l'instrumentation facile à localiser et à retirer.

## Données enregistrées

Un fichier JSON est produit par session. Il contient :

- l'opération `Database` ou `Project` ;
- `Editor` ou `Player` ;
- `Mono` ou `IL2CPP` ;
- la plateforme et la version Unity ;
- le statut `Succeeded`, `Canceled`, `Failed` ou `Incomplete` ;
- le type d'exception, sans son message ;
- les durées murales et CPU ;
- la mémoire managée avant/après ;
- les collections GC ;
- les fichiers et octets traités ;
- les objets racines et les familles d'objets ;
- la concurrence maximale configurée ;
- les demandes de recherche de tags et de références ;
- le nombre et la durée cumulée des appels `File.Exists`.

Les durées des phases exécutées en parallèle sont cumulatives. Elles ne doivent
donc pas être additionnées pour retrouver la durée murale totale de la session.
Le champ `totalWallMilliseconds` reste la référence pour la durée perçue.

Aucun des éléments suivants n'est écrit :

- chemin de base ou de projet ;
- nom ou identifiant de patient ;
- valeur de tag ;
- message d'exception.

## Exécution dans un Editor ou un player

Aucune activation, variable d'environnement ou option de lancement n'est
nécessaire. Les résultats vont systématiquement dans :

```text
<Application.persistentDataPath>/LoadingBenchmarks
```

Les résultats sont séparés automatiquement dans des sous-répertoires tels que :

```text
Editor-Mono-WindowsEditor
Player-IL2CPP-WindowsPlayer
Player-IL2CPP-LinuxPlayer
```

Pour obtenir une baseline IL2CPP, lancer normalement le player construit avec
IL2CPP, puis effectuer un chargement normal de la base ou du projet. Aucun code
de benchmark spécifique au player n'est requis.

## Tests de baseline opt-in dans l'Editor

L'assembly Editor-only suivante est toujours compilée :

```text
Assets/Tests/Performance/HBP.Loading.PerformanceTests
```

Les deux tests sont également marqués `Explicit` et appartiennent à la
catégorie `LoadingPerformance`. Ils ne font donc pas partie de la suite courte.

Les variables ci-dessous servent uniquement à fournir les chemins des jeux de
données aux tests automatisés explicites. Elles n'activent ni ne désactivent
l'instrumentation :

```text
HIBOP_LOADING_BENCHMARK_ROOT=<Application.persistentDataPath à mesurer>
HIBOP_LOADING_BENCHMARK_OUTPUT=<répertoire des résultats>
HIBOP_LOADING_BENCHMARK_PROJECT=<archive .hibop à mesurer>
```

`HIBOP_LOADING_BENCHMARK_PROJECT` n'est nécessaire que pour le test projet.
Le benchmark utilise les fichiers en lecture seule et place l'extraction du
projet dans un répertoire temporaire distinct.

Exécuter individuellement :

```text
HBP.Tests.LoadingPerformance.LoadingBaselinePerformanceTests.Database_WritesBaselineSummary
HBP.Tests.LoadingPerformance.LoadingBaselinePerformanceTests.Project_WritesBaselineSummary
```

Pour chaque comparaison avant/après :

1. utiliser le même jeu de données ;
2. exécuter une passe froide si elle est recherchée ;
3. exécuter au moins trois passes chaudes ;
4. conserver séparément Editor Mono et chaque player IL2CPP ;
5. comparer la médiane de `totalWallMilliseconds` ;
6. vérifier que `status` vaut `Succeeded`.

## Couverture actuelle des phases

Les phases de la baseline sont présentes :

```text
Loading.Database.Settings
Loading.Database.Protocols
Loading.Database.References
Loading.Database.Patients.Read
Loading.Database.Patients.Deserialize
Loading.Database.Patients.BindTags
Loading.Database.Patients.ValidateFiles
Loading.Database.DataInfos.Read
Loading.Database.DataInfos.Deserialize
Loading.Database.LinkReferences

Loading.Project.Manifest
Loading.Project.ArchiveRead
Loading.Project.Settings
Loading.Project.Patients.Read
Loading.Project.Patients.Deserialize
Loading.Project.Patients.BindTags
Loading.Project.Groups
Loading.Project.Datasets
Loading.Project.Visualizations
Loading.Project.LinkReferences
Loading.Project.ValidateFiles
```

Depuis l'étape 3, les accès fichiers des meshes et IRM ne sont plus exécutés à
l'intérieur de `JsonConvert.DeserializeObject`. `ValidateFiles` est une phase
physiquement séparée, exécutée après le parsing et avant la publication des
patients.

Les liaisons de tags et de références restent partiellement imbriquées dans
les callbacks ou dans le travail patient. Leur séparation complète relève de
l'étape 4.

## Vérifications automatisées

`LoadingDiagnosticsTests` vérifie que :

- une session réussie produit un JSON agrégé ;
- les erreurs et annulations produisent aussi un résumé ;
- les messages d'exception et chemins sources ne sont jamais enregistrés ;
- le chemin de chargement instrumenté conserve les valeurs désérialisées.

Les tests de sérialisation et de rétrocompatibilité existants restent la
référence pour l'intégrité complète du graphe.

## État de la validation locale

Les assemblies `HBP.Core.Runtime`, `HBP.Serialization.Tests` et le source du
benchmark opt-in compilent sans erreur avec les références générées du projet.
Les avertissements restants sont des avertissements de sérialisation déjà
présents hors de cette étape.

L'installation `6000.5.2f1-x86_64` a permis d'exécuter les tests et les
benchmarks en batchmode. Les cinq suites ciblées passent, soit 27 tests sur 27.
Quatre chargements de base et quatre ouvertures de projet ont produit des
rapports réussis. Les résultats sont dans
[`resultats_etape_1_2026-07-23.md`](resultats_etape_1_2026-07-23.md).

## Retrait après optimisation

La suppression est volontairement mécanique :

1. supprimer `LoadingDiagnostics.cs` et son fichier `.meta` ;
2. supprimer `Assets/Tests/Performance/HBP.Loading.PerformanceTests` ;
3. supprimer `LoadingDiagnosticsTests.cs` et son fichier `.meta` ;
4. rechercher `TEMP-LOADING-PROFILING` et `LoadingDiagnostics` ;
5. remettre les appels `ClassLoaderSaver` sur leurs surcharges sans phase ;
6. remplacer `LoadingDiagnostics.FileExists(path)` par `File.Exists(path)` ;
7. supprimer ce document.

Commande de contrôle :

```powershell
rg -n "TEMP-LOADING-PROFILING|LoadingDiagnostics" Assets
```
