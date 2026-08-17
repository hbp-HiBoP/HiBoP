# Résultats de l'étape 2 — Identifiants paresseux

Date : 23 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Concurrence du chargement : 20

## Conclusion

L'étape 2 améliore surtout la désérialisation du grand graphe de la base.

Pour 240 patients et 370 918 valeurs de tags, la médiane des trois passes
chaudes passe de **5,101 s** après l'étape 1 à **4,153 s**, soit **-18,6 %**.
La durée cumulée de désérialisation des patients baisse de **23,5 %**.

Pour le projet de 218 patients et 340 783 valeurs de tags, la médiane passe de
**4,926 s** à **4,805 s**, soit **-2,5 %**. Le CPU de session baisse de
**8,2 %** et le CPU cumulé de la phase de désérialisation baisse de **9,2 %**.

Le gain projet est plus faible que le gain base : la lecture et l'extraction
de l'archive, les liaisons et les autres catégories du projet ne sont pas
modifiées par cette étape.

## Protocole

Les données, la machine et le protocole sont identiques à l'étape 1 :

- base persistante réelle de 240 patients ;
- projet `visu_full_test.hibop` de 218 patients ;
- une passe initiale conservée séparément ;
- trois passes chaudes pour la médiane ;
- chaque exécution dans un processus Unity CLI fermé puis relancé ;
- statut `Succeeded` vérifié sur les huit rapports.

Les rapports bruts sont dans :

```text
.test-results/loading/step2/Editor-Mono-Unknown
```

Les résultats Unity des tests et benchmarks sont dans :

```text
.test-results/unity-cli/stage2
```

## Base de données

### Exécutions

| Passe | Temps mural | CPU session | Désérialisation patients cumulée | `BindTags` cumulé |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 4 523 ms | 51 609 ms | 76 173 ms | 1 015 ms |
| Chaude 1 | 4 304 ms | 52 922 ms | 67 318 ms | 3 845 ms |
| Chaude 2 | 4 153 ms | 49 031 ms | 60 458 ms | 5 603 ms |
| Chaude 3 | 4 026 ms | 46 609 ms | 57 956 ms | 5 165 ms |
| **Médiane chaude** | **4 153 ms** | **49 031 ms** | **60 458 ms** | **5 165 ms** |

### Comparaison avec l'étape 1

| Mesure | Étape 1 | Étape 2 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 5 101 ms | 4 153 ms | **-18,6 %** |
| CPU processus | 49 609 ms | 49 031 ms | -1,2 % |
| Lecture patients cumulée | 2 218 ms | 1 825 ms | -17,7 % |
| Désérialisation patients cumulée | 79 069 ms | 60 458 ms | **-23,5 %** |
| CPU cumulé de désérialisation | 1 034 266 ms | 844 906 ms | **-18,3 %** |
| Collections GC de session | 74 | 71 | -4,1 % |
| Croissance mémoire managée | +171,79 Mio | +164,20 Mio | -4,4 % |

Les scopes patients s'exécutent en parallèle ; leurs durées et CPU cumulés se
recouvrent et ne doivent pas être additionnés au temps de session. Ils restent
comparables entre les deux étapes, avec les mêmes 240 échantillons.

La lecture bénéficie aussi du cache du système de fichiers et ne peut pas être
attribuée aux IDs paresseux. Le signal spécifique de cette étape est la baisse
de la phase `Patients.Deserialize`.

`BindTags` varie fortement entre les exécutions concurrentes. Son nombre de
requêtes est inchangé à 1 112 754 et cette étape ne modifie pas son
algorithme ; sa variation n'est donc pas interprétée comme une régression.

## Projet

### Exécutions

| Passe | Temps mural | CPU session | Désérialisation patients cumulée | Lecture archive |
| --- | ---: | ---: | ---: | ---: |
| Initiale | 4 950 ms | 42 078 ms | 69 365 ms | 1 046 ms |
| Chaude 1 | 4 805 ms | 42 813 ms | 65 592 ms | 1 145 ms |
| Chaude 2 | 4 826 ms | 43 063 ms | 65 072 ms | 1 066 ms |
| Chaude 3 | 4 769 ms | 42 641 ms | 65 694 ms | 1 095 ms |
| **Médiane chaude** | **4 805 ms** | **42 813 ms** | **65 592 ms** | **1 095 ms** |

### Comparaison avec l'étape 1

| Mesure | Étape 1 | Étape 2 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 4 926 ms | 4 805 ms | **-2,5 %** |
| CPU processus | 46 641 ms | 42 813 ms | **-8,2 %** |
| Lecture patients cumulée | 1 991 ms | 1 837 ms | -7,7 % |
| Désérialisation patients cumulée | 67 400 ms | 65 592 ms | **-2,7 %** |
| CPU cumulé de désérialisation | 838 750 ms | 761 984 ms | **-9,2 %** |
| Collections GC de session | 28 | 27 | -3,6 % |
| Lecture archive | 1 064 ms | 1 095 ms | +2,9 % |

La croissance mémoire managée de session varie trop entre les ouvertures de
projet pour être attribuée à cette étape : sa médiane monte de 147 à 173 Mio,
alors que le pic médian observé dans les scopes de désérialisation baisse
légèrement. Aucun bénéfice mémoire projet n'est donc revendiqué.

## Validation fonctionnelle

Les suites ciblées donnent un XML Unity valide :

| Périmètre | Résultat |
| --- | ---: |
| `BaseDataLazyIdTests` | 9 / 9 |
| sérialisation, types historiques, projet, contrats et tags | 85 / 85 |
| **Total** | **94 / 94** |

Les huit exécutions de benchmark, quatre base et quatre projet, passent
également.

## Décision

L'étape 2 remplit son critère de fin :

- aucun GUID préalable n'est généré quand Json.NET fournit un ID ;
- les anciens objets sans ID restent réparés ;
- les IDs vides explicitement affectés restent détectables ;
- les accès concurrents produisent une identité stable ;
- les contrats JSON et historiques sont inchangés ;
- le gain est mesurable, particulièrement sur la base réelle.

L'étape suivante recommandée est la séparation des validations de chemins et
de la désérialisation.
