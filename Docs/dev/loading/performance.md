# Performance : protocoles et mesures historiques

Ces chiffres documentent des décisions, pas les performances du checkout actuel.
Aucune mesure Unity n'a été exécutée pendant la consolidation du 2 novembre 2026.
Les données produit et anciennes sorties locales ne sont pas des fixtures
redistribuables; les séries ci-dessous ne sont pas interchangeables.

## Métadonnées : indexer avant de changer de format

Campagne du **23 juillet 2026**, Unity 6000.5.2f1 Editor Mono Windows,
concurrence 20 : base de 240 patients / 370918 valeurs de tags, projet de
218 patients / 340783 valeurs. Avant indexation, environ 71,4 millions de
comparaisons et 742076 reconstructions de `AllTags` étaient estimées pour la
base. Une baseline base et deux ouvertures projet seulement précèdent les
trois passes chaudes après optimisation : les écarts ne sont pas des intervalles
statistiques.

| Temps mural | Base | Projet |
|---|---:|---:|
| Avant indexation | 9992 ms | 8134 ms (médiane interpolée de deux passes) |
| Index stable des tags | 5101 ms | 4926 ms |
| IDs paresseux | 4153 ms | 4805 ms |
| Validation fichiers séparée (six passes chaudes) | 4787 ms | 5166 ms |

La séparation de validation a augmenté le temps mural (+15,3 % / +7,5 %), malgré
seulement 32/19 ms de validation explicite. Elle est retenue pour l'architecture
et l'annulation, pas comme gain autonome. La variabilité de planification/I/O
interdit d'attribuer toute hausse aux sondes.

Campagne du **24 juillet**, autre ordinateur et autres données : base `Default`
de 419 patients / 577632 valeurs de tags / 3561 DataInfo, projet `full_test`
de 245 patients, archive 6912292 octets, JSON patients ~39029233 octets.
Runtime Editor Mono Windows 6000.5.2f1. Comparer uniquement au sein de cette série.

| Changement | Base, médiane chaude | Projet, médiane chaude |
|---|---:|---:|
| Contexte de liaison | 10125,6 → 9614,3 ms | 4379,2 → 3628,9 ms |
| JSON streamé indenté | 9614,3 → 8625,6 ms | 3628,9 → 2929,8 ms |
| ZIP direct | non concernée | 2929,8 → 1792,5 ms |

La référence avant liaison contient une passe; les résultats après et les
comparaisons suivantes utilisent trois passes chaudes. Le streaming réduit les
GC de session de 55 à 32 pour la base et 8 à 6 pour le projet, sans réduire le
JSON. Le ZIP direct fait passer la phase archive de 1082,9 à 105,2 ms.
Les phases streamées `Read`/`Deserialize` sont imbriquées; elles ne s'additionnent
pas. Les temps cumulés de workers, CPU/GC globaux observés par chaque scope et
deltas mémoire ne donnent ni le temps mural ni un pic mémoire réel.

## Scheduler

Campagne du **28 juillet**, Editor Mono 6000.5.2f1, 20 processeurs logiques,
`Default` et `visu_full_test` (pas `full_test`). Une chauffe écartée, trois
répétitions par combinaison, soit 30 mesures chaudes.

| Workers | Base Ready / Validated | Projet Ready / Validated |
|---:|---:|---:|
| 1 | 4,09 / 124,31 s | 4,18 / 113,15 s |
| 2 | 3,69 / 64,01 s | 3,49 / 58,17 s |
| 4 | 2,98 / 24,72 s | 2,29 / 16,04 s |
| 8 | 3,02 / 10,82 s | 2,74 / 9,69 s |
| 20 | 4,72 / 11,30 s | 4,01 / 9,80 s |

Vingt workers n'amélioraient pas Validated et dégradaient Ready/CPU. Cela motive
les plafonds 8/4/2 et le budget partagé du [pipeline actuel](loading-and-persistence.md).
Le plafond natif 2 est conservateur : cette campagne ne couvrait pas un corpus
natif représentatif. L'instrumentation temporaire de chargement a été retirée;
ses anciennes commandes ne doivent pas être annoncées comme disponibles.
Pour comparer : fixer données/build/machine, séparer froid/chaud, mesurer Ready,
validation ciblée et attente visible, CPU, allocations et pic réel; ne pas
réutiliser les temps de validation complète comme baseline du démarrage.

## Benchmark synthétique de projection encore disponible

Points d'entrée sous
[HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests) :
`NativeProjectionLoadBenchmarkCli`, `NativeProjectionLoadBenchmarkScenarios` et
`NativeProjectionLoadBenchmarkModel`. Valeurs déterministes calculées à partir de
patient/canal/essai/index, sans ajouter un gros enregistrement au dépôt.

Profils : `Smoke` court; `Product` (alias `Typical`) avec 30000 sites × 100
instants et variante trois colonnes; `Extreme` incluant 97/3073 positions sur
1500 ms à 64/2048 Hz. Le CLI accepte notamment :

```text
-executeMethod HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run
-hbpProjectionOutput <rapport.json>
-hbpProjectionProfile Smoke
-hbpProjectionTimeline 100
-hbpProjectionRepetitions 10
-hbpProjectionWorkers 0
-hbpProjectionBatchSites 0
```

`-hbpProjectionFilter` sélectionne un scénario. Le CLI fournit aussi surface,
volume, interpolation volumique et inclusion export : consulter ses arguments
avant comparaison. Il termine Unity par `EditorApplication.Exit`; ne pas ajouter
`-quit`. Utiliser la version de `ProjectVersion.txt` et la
[procédure Unity](../../../.agents/skills/hibop-unity-validation/SKILL.md), sans
lancer un second Editor lorsque HiBoP est déjà ouvert.

Référence **21 juillet 2026**, Unity 6000.5.2f1, Windows 11, 16 processeurs
logiques, dix répétitions, ancienne géométrie de projection :

| Scénario | Checksum | Total P50 / P95 | Calcul P50 / P95 | Coupe P50 / P95 |
|---|---|---:|---:|---:|
| Smoke 1000 sites × 100 | `F72D0F4CEE55D5F3` | 200,22 / 224,07 ms | 58,72 / 62,70 ms | 0,227 / 0,270 ms |
| Product 30000 sites × 100 | `2FDA5CD13773B845` | 4949,91 / 5806,37 ms | 4828,66 / 5568,66 ms | 0,193 / 0,370 ms |

Product : 250 patients × 120 canaux, 100 essais, fenêtres 2001 échantillons,
baselines 501; 422704 points et 42270400 valeurs projetées. Mémoire réellement
observée : tableau colonne 12000000 octets, projection native 170772416,
texture estimée 109068, pic privé 191606784, privé retenu maximal 6475776.
Smoke : 400000, 31602496, 109068, 40972288 et 4222976 octets respectivement.
Les tailles de brut/époques/dérivés Product (24,012 / 30,024 / 24,012 milliards
d'octets) étaient des **estimations logiques non allouées** par ce benchmark.
Un checksum par scénario et retour au delta privé zéro sur les neuf répétitions
suivantes ont été rapportés; cela ne prouve pas un plateau sur toute charge produit.

Les évolutions de grille native et de stockage rendent ces checksums impropres
à servir d'oracle universel actuel. Conserver pour une nouvelle mesure version
des sources et du plugin, OS, runtime, CPU, données, paramètres, fréquence de
mesure mémoire et rapport complet; vérifier séparément équivalence scientifique,
latence et mémoire. Les gains théoriques des anciens plans ne sont pas des résultats.
