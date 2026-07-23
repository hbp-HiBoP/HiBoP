# Résultats de l'étape 3 — Validation explicite des fichiers

Date : 23 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Concurrence parsing et validation : 20, sans chevauchement

## Conclusion

L'objectif fonctionnel est atteint :

- aucun accès fichier n'est exécuté pendant les callbacks JSON ;
- la validation dispose maintenant de sa propre phase mesurable ;
- les résultats sont publiés atomiquement après validation ;
- aliases, annulation et concurrence bornée sont couverts.

Le bénéfice de structure ne produit pas de gain mural sur cette série. Par
rapport à l'étape 2, la médiane chaude élargie augmente de **15,3 %** sur la
base et de **7,5 %** sur le projet.

Le validateur lui-même ne représente que **32 ms** sur la base et **19 ms** sur
le projet. La hausse mesurée se trouve principalement dans des phases
antérieures ou non modifiées (`Read`, `Deserialize`, archive), avec une forte
redistribution du temps entre `Deserialize` et `BindTags`. Elle ne peut donc
pas être attribuée aux seules sondes de fichiers.

L'étape est conservée comme prérequis d'architecture, mais aucun gain de
performance global n'est revendiqué.

## Protocole

Les mêmes données réelles sont utilisées :

- base de 240 patients, 482 meshes et 241 IRM ;
- projet de 218 patients ;
- mêmes fichiers, aliases et concurrence que les étapes 1 et 2.

Après la série initiale d'une passe plus trois passes chaudes, trois contrôles
chauds supplémentaires ont été exécutés pour chaque cible à cause de la hausse
observée. Les comparaisons utilisent donc :

- étape 2 : médiane de trois passes chaudes ;
- étape 3 : médiane de six passes chaudes.

Les 14 tests de benchmark ont le statut `Passed`. Les rapports bruts sont dans :

```text
.test-results/loading/step3/Editor-Mono-Unknown
.test-results/unity-cli/stage3
```

Les rapports de base produits comme dépendance des tests projet ne sont pas
mélangés à la cohorte principale de base.

## Base de données

### Série initiale

| Passe | Temps mural | CPU session | Désérialisation cumulée | Validation explicite |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 4 525 ms | 50 484 ms | 75 588 ms | 57 ms |
| Chaude 1 | 4 808 ms | 52 031 ms | 83 729 ms | 45 ms |
| Chaude 2 | 4 765 ms | 51 906 ms | 81 202 ms | 34 ms |
| Chaude 3 | 4 980 ms | 53 719 ms | 84 380 ms | 30 ms |

Les trois contrôles supplémentaires donnent 4 522 ms, 4 651 ms et 4 866 ms.

### Comparaison

| Mesure | Étape 2 | Étape 3 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 4 153 ms | 4 787 ms | **+15,3 %** |
| CPU processus | 49 031 ms | 52 664 ms | +7,4 % |
| Lecture patients cumulée | 1 825 ms | 2 308 ms | +26,5 % |
| Désérialisation patients cumulée | 60 458 ms | 82 465 ms | +36,4 % |
| CPU cumulé de désérialisation | 749 578 ms | 933 195 ms | +24,5 % |
| `BindTags` cumulé | 5 165 ms | 944 ms | -81,7 % |
| Validation explicite | incluse dans les callbacks | 32 ms | séparée |
| Appels `File.Exists` | 1 201 | 1 201 | 0 % |
| Temps cumulé `File.Exists` | 171 ms | 134 ms | -21,7 % |
| Collections GC de session | 71 | 69,5 | -2,1 % |

Les durées cumulées de workers se recouvrent. La baisse de `BindTags` sans
changement de son code et la hausse simultanée de la lecture montrent une
variabilité de planification importante. La régression murale est néanmoins
réelle dans cette campagne et reste documentée comme telle.

Malgré cette hausse par rapport à l'étape 2, la médiane reste environ 52 % sous
la baseline initiale de 9,992 s.

## Projet

### Série initiale

| Passe | Temps mural | CPU session | Désérialisation cumulée | Validation explicite |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 5 059 ms | 37 922 ms | 63 063 ms | 19 ms |
| Chaude 1 | 5 520 ms | 43 906 ms | 73 487 ms | 20 ms |
| Chaude 2 | 5 132 ms | 42 438 ms | 69 629 ms | 18 ms |
| Chaude 3 | 5 109 ms | 40 734 ms | 69 374 ms | 19 ms |

Les trois contrôles supplémentaires donnent 5 200 ms, 5 112 ms et 5 309 ms.

### Comparaison

| Mesure | Étape 2 | Étape 3 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 4 805 ms | 5 166 ms | **+7,5 %** |
| CPU processus | 42 813 ms | 41 961 ms | -2,0 % |
| Lecture patients cumulée | 1 837 ms | 1 840 ms | +0,2 % |
| Désérialisation patients cumulée | 65 592 ms | 70 149 ms | +7,0 % |
| CPU cumulé de désérialisation | 761 031 ms | 741 734 ms | -2,5 % |
| `BindTags` cumulé | 833 ms | 749 ms | -10,0 % |
| Validation explicite | incluse dans les callbacks | 19 ms | séparée |
| Appels `File.Exists` | 1 090 | 1 090 | 0 % |
| Temps cumulé `File.Exists` | 185 ms | 85 ms | -54,2 % |
| Lecture archive | 1 095 ms | 1 227 ms | +12,1 % |
| Collections GC de session | 27 | 27 | 0 % |

Le CPU total baisse légèrement alors que le temps mural augmente. Avec la
hausse indépendante de la lecture d'archive, ce profil pointe davantage vers
la planification et l'I/O de la campagne que vers les 19 ms de validation.

La médiane reste environ 36 % sous la baseline initiale de 8,134 s.

## Validation fonctionnelle

| Périmètre | Résultat |
| --- | ---: |
| `AssetReferenceValidatorTests` | 3 / 3 |
| autres suites ciblées | 101 / 101 |
| **Total** | **104 / 104** |
| benchmarks base et projet | 14 / 14 |

## Décision

L'étape 3 remplit ses exigences fonctionnelles et de conception. Elle est
conservée parce qu'elle :

- retire l'I/O du parseur ;
- isole la validation ;
- rend l'annulation correcte ;
- permet de mesurer et modifier sa concurrence indépendamment ;
- prépare la séparation complète des liaisons de l'étape 4.

La hausse murale interdit toutefois de présenter cette étape comme une
optimisation autonome. Le réglage de la concurrence CPU et la variabilité des
workers devront être mesurés avant toute conclusion supplémentaire.
