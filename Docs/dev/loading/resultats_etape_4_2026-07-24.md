# Résultats de l'étape 4 — Contexte explicite de liaison

Date : 24 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Workspace : `Default`  
Projet : `full_test`

## Conclusion

La passe explicite de liaison remplit son objectif mesuré :

- sur la base, le coût instrumenté cumulé de `LinkReferences` et de l'ancien
  `BindTags` passe de **1 333,6 ms à 264,6 ms**, soit **-80,2 %** ;
- sur le projet, le même coût passe de **196,2 ms à 25,2 ms**, soit
  **-87,2 %** ;
- les six passes chaudes supplémentaires réussissent sans erreur ni
  avertissement Unity.

Le temps mural total évolue différemment selon le périmètre :

- base : **10 125,6 ms -> 9 614,3 ms**, soit **-5,1 %** ;
- projet : **4 379,2 ms -> 3 628,9 ms**, soit **-17,1 %**.

Ces valeurs après l'étape 4 sont les médianes de trois passes chaudes. Elles
remplacent la comparaison provisoire fondée sur la première passe après
compilation, qui donnait +5,8 % sur la base et -12,6 % sur le projet.

## Protocole

Le changement d'ordinateur interdit de comparer directement cette campagne aux
mesures des étapes 1 à 3. La comparaison utilise les captures faites sur le
même ordinateur, avec les mêmes données :

- référence immédiatement avant l'étape 4 : une passe à 09:34 ;
- passe initiale après l'étape 4 : 10:27 ;
- trois passes chaudes après l'étape 4 : 10:33 à 10:34.

La base contient 419 patients, 1 487 meshes, 805 IRM, 56 409 sites,
577 632 valeurs de tags et 3 561 `DataInfo`. Le projet contient 245 patients,
948 meshes, 487 IRM, 35 572 sites, un dataset et une visualisation.

La colonne « après » des comparaisons utilise la médiane des trois dernières
passes. La référence avant l'étape 4 ne comporte qu'une passe ; les écarts
avant/après restent donc indicatifs, même si la cohorte après modification est
désormais plus robuste.

## Base de données

### Série après l'étape 4

| Passe | Temps mural | CPU processus | Liaison | Validation |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 10 716,7 ms | 67 375,0 ms | 253,2 ms | 2 883,9 ms |
| Chaude 1 | 9 832,5 ms | 71 343,8 ms | 265,8 ms | 2 600,1 ms |
| Chaude 2 | 9 393,1 ms | 72 218,8 ms | 264,6 ms | 1 945,6 ms |
| Chaude 3 | 9 614,3 ms | 73 734,4 ms | 259,7 ms | 2 124,7 ms |
| **Médiane chaude** | **9 614,3 ms** | **72 218,8 ms** | **264,6 ms** | **2 124,7 ms** |

### Comparaison

| Mesure | Référence avant étape 4 | Médiane chaude après | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 10 125,6 ms | 9 614,3 ms | **-5,1 %** |
| CPU processus | 70 734,4 ms | 72 218,8 ms | +2,1 % |
| Liaison + ancien `BindTags` | 1 333,6 ms | 264,6 ms | **-80,2 %** |
| Lecture patients cumulée | 10 375,8 ms | 4 263,3 ms | -58,9 % |
| Désérialisation patients cumulée | 114 658,5 ms | 131 258,7 ms | +14,5 % |
| Validation des fichiers | 3 192,3 ms | 2 124,7 ms | -33,4 % |
| Lecture `DataInfo` cumulée | 3 805,8 ms | 565,4 ms | -85,1 % |
| Désérialisation `DataInfo` cumulée | 5 589,0 ms | 5 143,1 ms | -8,0 % |
| Collections GC de session | 66 / 66 / 66 | 55 / 55 / 55 | -16,7 % |

Les durées patients et `DataInfo` sont cumulées entre les workers et peuvent
donc dépasser le temps mural de la session.

## Projet

### Série après l'étape 4

| Passe | Temps mural | CPU processus | Liaison | Validation |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 3 825,8 ms | 21 734,4 ms | 25,6 ms | 1 222,4 ms |
| Chaude 1 | 3 719,5 ms | 21 578,1 ms | 30,8 ms | 922,2 ms |
| Chaude 2 | 3 140,3 ms | 20 531,3 ms | 25,2 ms | 385,4 ms |
| Chaude 3 | 3 628,9 ms | 22 515,6 ms | 23,0 ms | 1 059,3 ms |
| **Médiane chaude** | **3 628,9 ms** | **21 578,1 ms** | **25,2 ms** | **922,2 ms** |

### Comparaison

| Mesure | Référence avant étape 4 | Médiane chaude après | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 4 379,2 ms | 3 628,9 ms | **-17,1 %** |
| CPU processus | 23 062,5 ms | 21 578,1 ms | **-6,4 %** |
| Liaison + ancien `BindTags` | 196,2 ms | 25,2 ms | **-87,2 %** |
| Lecture archive | 1 059,5 ms | 1 091,6 ms | +3,0 % |
| Lecture patients cumulée | 861,9 ms | 820,6 ms | -4,8 % |
| Désérialisation patients cumulée | 30 109,6 ms | 28 680,4 ms | -4,7 % |
| Validation des fichiers | 1 576,9 ms | 922,2 ms | -41,5 % |
| Datasets | 47,5 ms | 43,8 ms | -7,7 % |
| Visualisations | 42,8 ms | 3,5 ms | -91,9 % |
| Collections GC de session | 11 / 11 / 11 | 8 / 8 / 8 | -27,3 % |

Le temps d'archive reste pratiquement stable. Le gain global est réparti entre
la liaison, la désérialisation et la validation. Les petites phases projet sont
plus sensibles au bruit relatif.

## Lecture correcte de la phase de liaison

Avant l'étape 4, `LinkReferences` et `BindTags` étaient instrumentés par de
nombreux scopes courts, parfois exécutés en parallèle. Après l'étape 4, toute
la liaison est mesurée par un scope de lot unique. Les valeurs cumulées
confirment la disparition des recherches répétées, mais leur différence ne
doit pas être soustraite directement du temps mural.

La nouvelle passe effectue davantage de recherches comptabilisées car chaque
valeur de tag est désormais résolue explicitement. Ces recherches sont des
accès dictionnaire en O(1), et non des parcours de listes globales.

## Fichiers bruts

Répertoire :

```text
C:\Users\Benjamin BONTEMPS\AppData\LocalLow\CRNL\HiBoP\LoadingBenchmarks\Editor-Mono-WindowsEditor
```

Référence avant étape 4 :

```text
loading-database-20260724-073415-990-4.json
loading-project-20260724-073432-613-23.json
```

Après étape 4 :

```text
loading-database-20260724-082722-826-4.json
loading-project-20260724-082732-981-23.json
loading-database-20260724-083349-468-4.json
loading-database-20260724-083409-454-4.json
loading-database-20260724-083431-266-4.json
loading-project-20260724-083440-977-23.json
loading-project-20260724-083448-312-42.json
loading-project-20260724-083455-211-61.json
```
