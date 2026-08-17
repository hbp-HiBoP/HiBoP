# Étape 6 — Manifeste projet et lecture ZIP directe

Date : 24 juillet 2026

## Résultat

Le chargement d'un projet `.hibop` ne passe plus par une extraction complète
dans `ApplicationState.ExtractProjectFolder`. Les settings et les objets du
projet sont lus directement depuis les streams des entrées ZIP.

Le format écrit par `Project.SaveAsync` est inchangé :

```text
<nom>.settings
Patients/
Groups/
Datasets/
Visualizations/
```

Aucun fichier de manifeste n'est ajouté dans l'archive. Le nouveau
`ProjectManifest` est uniquement un objet créé en mémoire à partir de la table
des entrées existantes.

## Manifeste

Une inspection produit maintenant :

- la version de schéma logique `legacy-v0` (`SchemaVersion == 0`) ;
- la version produit lue dans les préférences ;
- le nom et le chemin de l'archive ;
- les nombres de patients, groupes, datasets et visualisations ;
- un index des entrées et de leurs tailles décompressées ;
- les préférences du projet ou l'exception rencontrée pendant leur lecture.

`ProjectInfo` conserve ce manifeste. `Project.LoadAsync` le réutilise tant que
la longueur et la date de modification de l'archive n'ont pas changé. Si le
fichier a été remplacé entre l'affichage et le chargement, il est inspecté de
nouveau.

La liste de projets utilise `Project.GetProjectInfos` afin que la validation,
le résumé et les settings soient produits pendant la même inspection. L'API
historique `Project.GetProject`, qui retourne uniquement les chemins, reste
disponible.

## Lecture directe et concurrence

`ClassLoaderSaver` accepte maintenant un `Stream`. Le pipeline d'une entrée
devient :

```text
entrée ZIP -> stream décompressé -> StreamReader -> JsonTextReader
           -> JsonSerializer.Deserialize
```

DotNetZip partage le même `FileStream` entre les entrées d'une instance
`ZipFile`. Plusieurs `OpenReader` concurrents sur cette instance ne sont donc
pas sûrs. Pour conserver le parallélisme existant sans recopier les JSON en
mémoire, le chargement utilise un pool borné de lecteurs ZIP indépendants :

- un seul manifeste et un seul index logique ;
- au plus 20 lecteurs lorsque le multithreading est activé ;
- un lecteur par tâche active ;
- fermeture de tous les lecteurs sur succès, erreur et annulation.

Chaque JSON est toujours désérialisé directement depuis son stream ZIP. Il
n'est ni extrait sur le disque, ni matérialisé dans une chaîne complète ou un
`MemoryStream`.

## Compatibilité

- la structure et les noms des entrées sauvegardées sont inchangés ;
- les quatre entrées de dossiers explicites restent écrites, y compris quand
  elles sont vides, afin de rester lisibles par les versions précédentes ;
- les JSON restent indentés et modifiables manuellement ;
- les archives sans version de schéma sont classées `legacy-v0` ;
- les entrées supplémentaires sûres n'invalident pas le projet ;
- un dossier historique `Protocols/` est accepté mais entièrement ignoré :
  ses entrées ne figurent pas dans l'index métier et ne sont jamais
  désérialisées ;
- les protocoles de la base restent l'unique source canonique.

La sauvegarde reste volontairement inchangée. Son dossier de staging et
l'appel DotNetZip `AddDirectory` continuent à produire exactement le contrat
physique attendu par les anciens lecteurs.

## Sécurité

Le manifeste valide tous les noms avant le chargement et rejette :

- les chemins contenant un segment `.` ou `..` ;
- les chemins absolus Unix, UNC ou avec lettre de lecteur ;
- les entrées dupliquées après normalisation des séparateurs et de la casse.

L'absence d'extraction supprime en plus le chemin d'exploitation classique
« Zip Slip » lors du chargement.

## Validation automatisée

Les tests couvrent notamment :

- les projets minimaux et complets ;
- le résumé et les settings du manifeste ;
- le round-trip sauvegarde, chargement, resauvegarde et rechargement ;
- un ancien dossier `Protocols/` contenant volontairement un JSON invalide ;
- les archives incomplètes, malformées et les settings multiples ;
- les JSON corrompus pour chaque famille d'objet ;
- les chemins `../`, absolus Unix et absolus Windows ;
- l'annulation pendant chaque phase ;
- l'absence de modification du dossier d'extraction ;
- la fermeture des archives après succès, erreur et annulation ;
- la conservation du format d'archive actuel.

Résultats :

- tests ciblés archive, compatibilité et lecteur JSON : **77 / 77** ;
- `HBP.Serialization.Tests` et `HBP.ProjectWorkflow.Tests` : **421 / 421** ;
- `HBP.Workflow.PlayModeTests` : **17 / 17** ;
- erreurs de compilation ou d'exécution : **0**. La suite complète conserve
  ses avertissements attendus liés aux tests négatifs et aux budgets mémoire.

## Benchmark

Les trois chargements complets de `full_test` ont réussi. Par rapport à la
médiane chaude de l'étape 5, la médiane du temps mural passe de
**2 929,8 ms à 1 792,5 ms** (**-38,8 %**) et la lecture de l'archive de
**1 082,9 ms à 105,2 ms** (**-90,3 %**).

Le détail des passes, les marqueurs cumulés et l'analyse de l'incident Unity
sont consignés dans
[`resultats_etape_6_2026-07-24.md`](resultats_etape_6_2026-07-24.md).

Cette étape ne modifie pas le chargement de la base de données.
