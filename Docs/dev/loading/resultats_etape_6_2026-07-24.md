# Résultats de l'étape 6 — lecture ZIP directe

Date : 24 juillet 2026

## Conclusion

Les trois chargements complets du projet `full_test` ont réussi. Par rapport à
la médiane chaude de l'étape 5, la médiane de l'étape 6 passe de
**2 929,8 ms à 1 792,5 ms**, soit une baisse de **38,8 %**.

Le gain principal est bien localisé dans la lecture de l'archive :

- `Loading.Project.ArchiveRead` passe de **1 082,9 ms à 105,2 ms**
  (**-90,3 %**) ;
- le temps CPU passe de **19 843,8 ms à 16 265,6 ms** (**-18,0 %**) ;
- les collections GC de session passent de **6 à 5** (**-16,7 %**) ;
- le temps patient cumulé sur les workers passe de **28 204,1 ms à
  23 913,8 ms** (**-15,2 %**).

La suppression de l'extraction complète retire donc environ une seconde du
chemin critique sans modifier le format `.hibop`.

## Protocole

- runtime : Unity Editor, Mono, Windows Editor ;
- projet : `full_test` ;
- trois chargements complets après redémarrage de l'Editor ;
- comparaison avec les trois passes chaudes de l'étape 5 ;
- agrégation par médiane.

L'ouverture de la liste des projets crée aussi de petites sessions
d'instrumentation limitées à `ProjectInfo` et `ProjectManifest`. Elles ne sont
pas des chargements de `full_test` et ont été exclues. Les trois captures
retenues possèdent les sept phases du chargement complet.

## Mesures brutes

| Capture | Temps mural | CPU | GC session | Archive | Patients, cumulé | Liaison | Validation fichiers |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `loading-project-20260724-101917-529-23.json` | 1 792,5 ms | 16 265,6 ms | 6 | 105,2 ms | 25 905,8 ms | 23,4 ms | 207,2 ms |
| `loading-project-20260724-101923-049-42.json` | 2 535,7 ms | 18 500,0 ms | 5 | 103,9 ms | 23 371,8 ms | 23,8 ms | 1 179,9 ms |
| `loading-project-20260724-101927-672-61.json` | 1 618,7 ms | 16 187,5 ms | 4 | 114,1 ms | 23 913,8 ms | 25,1 ms | 228,9 ms |
| **Médiane** | **1 792,5 ms** | **16 265,6 ms** | **5** | **105,2 ms** | **23 913,8 ms** | **23,8 ms** | **228,9 ms** |

Le projet contient 245 patients. L'archive mesure 6 912 292 octets et les JSON
patients représentent environ 39 029 233 octets décompressés.

## Comparaison avec l'étape 5

| Mesure médiane | Étape 5 | Étape 6 | Évolution |
| --- | ---: | ---: | ---: |
| Temps mural | 2 929,8 ms | 1 792,5 ms | **-38,8 %** |
| CPU | 19 843,8 ms | 16 265,6 ms | **-18,0 %** |
| GC session | 6 | 5 | **-16,7 %** |
| Lecture archive | 1 082,9 ms | 105,2 ms | **-90,3 %** |
| Lecture patients, cumulée | 28 204,1 ms | 23 913,8 ms | **-15,2 %** |
| Liaison | 26,0 ms | 23,8 ms | **-8,5 %** |
| Validation fichiers | 257,4 ms | 228,9 ms | **-11,1 %** |

La deuxième passe est ralentie par la validation des chemins
(`1 179,9 ms`). Une variation comparable existait déjà à l'étape 5
(`1 319,7 ms`). La lecture ZIP reste, elle, stable entre **103,9 et
114,1 ms** : cette passe ne révèle donc pas de régression du nouveau lecteur.

## Interprétation des marqueurs

Le marqueur patient additionne le travail de 245 scopes parallèles. Il peut
donc être très supérieur au temps mural et ne doit pas être ajouté au total de
session.

La phase archive compte 21 ouvertures : une inspection de fraîcheur du
manifeste et jusqu'à 20 lecteurs du pool ZIP. Le nombre d'octets de l'archive
n'est compté qu'une fois.

Les deltas de mémoire managée observés en fin de session varient fortement
selon le moment où le GC passe. Ils ne constituent pas une mesure de pic
mémoire et ne sont pas utilisés pour conclure.

## Incident Unity observé

Unity s'est fermé avant cette campagne, pendant la fin de la suite EditMode.
Le rapport natif situe le crash dans le calcul de vivacité Mono
(`mono_add_process_object`), appelé pendant le nettoyage des assets et la
restauration de scène du Test Runner. La pile ne contient ni
`ProjectArchiveReader`, ni DotNetZip, ni le pipeline de chargement projet.

Un rapport antérieur au développement de l'étape 6 présente déjà la même
famille de pile Mono. À ce stade, rien ne relie donc ce crash à la lecture ZIP
directe. Les trois chargements effectués après le redémarrage ont tous réussi.
L'incident doit néanmoins être suivi séparément s'il se reproduit en usage
normal ou avec un scénario de test minimal.

