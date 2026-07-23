# Résultats de l'étape 1 — Index des tags

Date : 23 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Concurrence du chargement : 20

## Conclusion

L'étape 1 est validée sur les tests fonctionnels et sur les deux graphes réels
de référence.

Sur la base de 240 patients et 370 918 valeurs de tags, la médiane des trois
passes chaudes passe de la référence disponible de **9,992 s** à **5,101 s**,
soit une baisse de **49,0 %** du temps mural. Le travail cumulé de
`CheckTagsAsync` baisse de **86,0 %** et le CPU de session de **58,1 %**.

Sur le projet de 218 patients et 340 783 valeurs de tags, la médiane passe de
**8,134 s** à **4,926 s**, soit une baisse de **39,4 %**. La lecture de
l'archive reste stable ; la baisse se situe bien dans le travail patient.

Le nombre de requêtes logiques de tags et le nombre d'objets chargés sont
strictement identiques avant et après. L'optimisation accélère donc les mêmes
opérations métier sans en masquer ni en supprimer.

## Protocole

Les entrées sont celles de la baseline du même jour :

- base :
  `C:\Users\Zigaroula\AppData\LocalLow\CRNL\HiBoP`,
  240 patients et 370 918 valeurs de tags ;
- projet :
  `visu_full_test.hibop`,
  218 patients et 340 783 valeurs de tags.

L'identité du projet a été vérifiée à partir de l'archive : ses 340 783 valeurs
produisent exactement les 1 022 349 requêtes mesurées dans les anciens
rapports.

Pour chaque cible, une première passe a été conservée séparément puis trois
passes supplémentaires ont servi à calculer la médiane chaude. Tous les
rapports post-optimisation ont le statut `Succeeded`.

Les rapports bruts de cette session sont dans :

```text
.test-results/loading/step1/Editor-Mono-Unknown
```

Le suffixe `Unknown` est une limite du hook de métadonnées lorsqu'il est
exécuté par le Test Runner EditMode en ligne de commande. Les journaux et les
assemblies confirment Unity `6000.5.2f1` sous Mono sur Windows.

## Base de données

### Exécutions post-optimisation

| Passe | Temps mural | CPU session | Désérialisation patients cumulée | `BindTags` cumulé |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 6 269 ms | 48 891 ms | 102 838 ms | 817 ms |
| Chaude 1 | 4 689 ms | 43 484 ms | 68 429 ms | 6 489 ms |
| Chaude 2 | 5 325 ms | 58 266 ms | 90 957 ms | 967 ms |
| Chaude 3 | 5 101 ms | 49 609 ms | 79 069 ms | 4 546 ms |
| **Médiane chaude** | **5 101 ms** | **49 609 ms** | **79 069 ms** | **4 546 ms** |

La variabilité de `BindTags` vient notamment du temps d'attente des tâches
concurrentes et des collections globales observées par chaque scope. Même sa
médiane haute reste très inférieure à la baseline.

### Comparaison

| Mesure | Baseline | Étape 1, médiane chaude | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 9 992 ms | 5 101 ms | **-49,0 %** |
| CPU processus | 118 359 ms | 49 609 ms | **-58,1 %** |
| Lecture patients cumulée | 2 367 ms | 2 218 ms | -6,3 % |
| Désérialisation patients cumulée | 156 586 ms | 79 069 ms | **-49,5 %** |
| `CheckTagsAsync` / `BindTags` cumulé | 32 533 ms | 4 546 ms | **-86,0 %** |
| Collections GC de session | 96 | 74 | **-22,9 %** |
| Croissance mémoire managée | +185,66 Mio | +171,79 Mio | -7,5 % |
| Requêtes de tags | 1 112 754 | 1 112 754 | 0 % |
| Patients | 240 | 240 | 0 % |
| Valeurs de tags | 370 918 | 370 918 | 0 % |

La lecture cumulée varie peu, tandis que désérialisation et liaison chutent.
C'est la signature attendue de la suppression des recherches linéaires et des
reconstructions de `AllTags`.

Le temps de `File.Exists` baisse fortement sur les passes chaudes, mais il
dépend du cache du système de fichiers et n'est pas attribué à l'index.

La baseline de charge ne contient qu'une exécution complète pour cette base.
Le pourcentage avant/après est donc une comparaison utile mais pas un intervalle
statistique. Les trois nouvelles passes montrent néanmoins un résultat stable
entre 4,689 s et 5,325 s.

## Projet

### Exécutions post-optimisation

| Passe | Temps mural | CPU session | Travail patient cumulé | Lecture archive |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 5 022 ms | 43 797 ms | 72 458 ms | 1 120 ms |
| Chaude 1 | 4 805 ms | 46 484 ms | 69 923 ms | 1 047 ms |
| Chaude 2 | 5 010 ms | 46 734 ms | 73 725 ms | 1 064 ms |
| Chaude 3 | 4 926 ms | 46 641 ms | 70 116 ms | 1 177 ms |
| **Médiane chaude** | **4 926 ms** | **46 641 ms** | **70 116 ms** | **1 064 ms** |

Le travail patient post-optimisation additionne les nouvelles phases
`Read`, `Deserialize` et `BindTags`. Il est comparable à l'ancienne phase
agrégée `Loading.Project.Patients`.

### Comparaison

La baseline projet ne comporte que deux ouvertures. La colonne de référence
utilise leur médiane interpolée, comme le document de baseline.

| Mesure | Baseline interpolée | Étape 1, médiane chaude | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 8 134 ms | 4 926 ms | **-39,4 %** |
| CPU processus | 88 313 ms | 46 641 ms | **-47,2 %** |
| Travail patient cumulé | 125 322 ms | 70 116 ms | **-44,1 %** |
| Lecture archive | 1 019 ms | 1 064 ms | +4,4 % |
| Collections GC de session | 45,5 | 28 | **-38,5 %** |
| Requêtes de tags | 1 022 349 | 1 022 349 | 0 % |
| Patients | 218 | 218 | 0 % |
| Valeurs de tags | 340 783 | 340 783 | 0 % |

La phase `BindTags`, désormais isolée, a une médiane chaude de **674 ms**.
La lecture de l'archive ne baisse pas, ce qui confirme que le gain se trouve
dans la construction et la validation du graphe patient.

La mémoire projet n'est pas comparée : la seconde ouverture de la baseline
partait d'un graphe déjà retenu en mémoire, ce qui rend les deltas incompatibles
avec les exécutions CLI isolées.

## Validation fonctionnelle

Cinq suites ciblées disposent d'un XML Unity valide :

| Suite | Résultat |
| --- | ---: |
| `TagCollectionIndexTests` | 8 / 8 |
| `ClassLoaderSaverTests` | 4 / 4 |
| `LegacyProjectCompatibilityTests` | 7 / 7 |
| `PatientsGroupsTagsSitesTests` | 5 / 5 |
| `LoadingDiagnosticsTests` | 3 / 3 |
| **Total** | **27 / 27** |

Les huit exécutions de benchmark, quatre base et quatre projet, passent
également.

La suite complète `HBP.Serialization.Tests` retourne le code Unity `0`, mais
Unity Test Framework lève ensuite une `NullReferenceException` interne pendant
l'écriture de son XML global. Ce résultat global n'est donc pas utilisé comme
preuve. Les suites pertinentes ont été relancées séparément pour obtenir les
27 résultats vérifiables ci-dessus.

## Décision

L'étape 1 remplit son critère de fin :

- les trois passes logiques restent présentes ;
- elles utilisent maintenant un index au lieu d'un parcours linéaire ;
- les graphes chargés conservent leurs volumes ;
- les formats actuels et historiques passent ;
- le gain est important sur la base comme sur le projet.

L'étape suivante recommandée reste la suppression des GUID temporaires pendant
la désérialisation. Le gain devra principalement être recherché dans
`Patients.Deserialize`, le CPU et les collections GC.

