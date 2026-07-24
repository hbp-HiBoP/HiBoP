# Résultats de l'étape 5 — JSON streamé et indenté

Date : 24 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Workspace : `Default`  
Projet : `full_test`

## Conclusion

La médiane de trois passes chaudes s'améliore sur les deux périmètres :

- base : **9 614,3 ms -> 8 625,6 ms**, soit **-10,3 %** ;
- projet : **3 628,9 ms -> 2 929,8 ms**, soit **-19,3 %**.

La première passe après compilation était en revanche plus lente que la
médiane chaude de l'étape 4 :

- base : **10 242,9 ms**, soit **+6,5 %** ;
- projet : **3 936,4 ms**, soit **+8,5 %**.

Cette première passe peut expliquer l'impression visuelle de ralentissement.
Le gain médian apparaît après échauffement du cache et du runtime.

Le signal le plus stable en faveur du streaming est la baisse des collections
GC :

- base : **55 -> 32**, soit **-41,8 %** ;
- projet : **8 -> 6**, soit **-25,0 %**.

Le format reste indenté et les volumes d'octets lus sont inchangés. La baisse
du GC est cohérente avec la suppression des grandes chaînes intermédiaires ;
aucun gain ne peut provenir d'une réduction de taille JSON.

## Protocole

La comparaison utilise deux cohortes de trois passes chaudes sur le même
ordinateur et les mêmes données :

- avant : médianes chaudes de l'étape 4 ;
- après : trois chargements manuels effectués à 11:30–11:31.

Une paire de chargements exécutée à 10:40 après compilation est conservée
séparément comme passe initiale.

Les six nouvelles passes ont le statut `Succeeded`. La console Unity ne
contient ni erreur ni avertissement.

## Base de données

### Série après l'étape 5

| Passe | Temps mural | CPU processus | Collections GC |
| --- | ---: | ---: | ---: |
| Initiale | 10 242,9 ms | 66 250,0 ms | 54 / 54 / 54 |
| Chaude 1 | 9 629,8 ms | 70 250,0 ms | 32 / 32 / 32 |
| Chaude 2 | 8 485,8 ms | 71 515,6 ms | 32 / 32 / 32 |
| Chaude 3 | 8 625,6 ms | 70 875,0 ms | 32 / 32 / 32 |
| **Médiane chaude** | **8 625,6 ms** | **70 875,0 ms** | **32 / 32 / 32** |

### Comparaison des médianes chaudes

| Mesure | Étape 4 | Étape 5 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 9 614,3 ms | 8 625,6 ms | **-10,3 %** |
| CPU processus | 72 218,8 ms | 70 875,0 ms | -1,9 % |
| Collections GC | 55 / 55 / 55 | 32 / 32 / 32 | **-41,8 %** |
| Liaison | 264,6 ms | 267,8 ms | +1,2 % |
| Validation des fichiers | 2 124,7 ms | 2 009,8 ms | -5,4 % |
| Mémoire managée nette | +284,59 Mio | +266,76 Mio | -6,3 % |

La liaison reste stable, comme attendu. La mémoire nette diminue, mais elle
n'est pas une mesure du pic mémoire réel.

## Projet

### Série après l'étape 5

| Passe | Temps mural | CPU processus | Collections GC |
| --- | ---: | ---: | ---: |
| Initiale | 3 936,4 ms | 22 421,9 ms | 9 / 9 / 9 |
| Chaude 1 | 2 929,8 ms | 19 843,8 ms | 7 / 7 / 7 |
| Chaude 2 | 3 778,1 ms | 21 250,0 ms | 6 / 6 / 6 |
| Chaude 3 | 2 911,1 ms | 19 406,3 ms | 6 / 6 / 6 |
| **Médiane chaude** | **2 929,8 ms** | **19 843,8 ms** | **6 / 6 / 6** |

### Comparaison des médianes chaudes

| Mesure | Étape 4 | Étape 5 | Écart |
| --- | ---: | ---: | ---: |
| Temps mural total | 3 628,9 ms | 2 929,8 ms | **-19,3 %** |
| CPU processus | 21 578,1 ms | 19 843,8 ms | **-8,0 %** |
| Collections GC | 8 / 8 / 8 | 6 / 6 / 6 | **-25,0 %** |
| Liaison | 25,2 ms | 26,0 ms | +3,4 % |
| Validation des fichiers | 922,2 ms | 257,4 ms | -72,1 % |

La validation des chemins varie fortement entre les passes et dépend du cache
du système de fichiers. Elle contribue à la dispersion du temps projet et ne
doit pas être attribuée au streaming JSON.

## Limites de comparaison des phases JSON

Avant l'étape 5, `Read` mesurait la création de la chaîne complète puis
`Deserialize` mesurait le parsing de cette chaîne.

Après l'étape 5, `Read` enveloppe le stream complet et `Deserialize` est
imbriqué : la lecture physique a lieu pendant le parsing. Les durées de ces
deux phases ont donc changé de définition et ne sont pas comparées entre les
étapes.

Les indicateurs comparables restent :

- `totalWallMilliseconds` ;
- le CPU total ;
- les collections GC de session ;
- les phases indépendantes comme la liaison et la validation ;
- les nombres de fichiers et d'octets, qui sont inchangés.

L'instrumentation actuelle ne capture pas un véritable pic mémoire continu.
Une mesure Profiler dédiée serait nécessaire pour démontrer précisément ce
point.

## Fichiers bruts

Répertoire :

```text
C:\Users\Benjamin BONTEMPS\AppData\LocalLow\CRNL\HiBoP\LoadingBenchmarks\Editor-Mono-WindowsEditor
```

Passe initiale :

```text
loading-database-20260724-084039-354-4.json
loading-project-20260724-084051-623-23.json
```

Passes chaudes :

```text
loading-database-20260724-093039-267-4.json
loading-database-20260724-093100-754-4.json
loading-database-20260724-093120-663-4.json
loading-project-20260724-093128-713-23.json
loading-project-20260724-093137-106-42.json
loading-project-20260724-093143-866-61.json
```
