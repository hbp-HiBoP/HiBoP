# Étape 5 — Lecture et écriture JSON streamées

Date : 24 juillet 2026

## Résultat

`ClassLoaderSaver` ne matérialise plus le contenu complet d'un fichier JSON
dans une `string` avant de le désérialiser. Json.NET consomme directement un
`JsonTextReader` placé sur un `StreamReader`.

L'écriture est également streamée par un `JsonTextWriter`. Elle ne construit
plus une grande chaîne avec `JsonConvert.SerializeObject` avant de la copier
dans le fichier.

À la demande du produit, tous les JSON écrits restent en
`Formatting.Indented`. Cette étape ne revendique donc aucune réduction de
taille des fichiers. Elle porte sur les copies intermédiaires, le pic mémoire
et la pression GC tout en conservant la lisibilité manuelle des projets.

## Pipeline

Avant :

```text
fichier -> StreamReader.ReadToEnd -> string JSON complète
        -> JsonConvert.DeserializeObject -> graphe
```

Après :

```text
FileStream -> StreamReader -> JsonTextReader
           -> JsonSerializer.Deserialize -> graphe
```

L'écriture suit le chemin symétrique :

```text
graphe -> JsonSerializer.Serialize -> JsonTextWriter
       -> StreamWriter -> FileStream
```

Les streams utilisent un buffer cohérent de 64 Kio et
`FileOptions.SequentialScan`.

## Settings séparés

Deux instances distinctes de `JsonSerializerSettings` sont maintenant
utilisées :

- `m_ReadSettings` conserve `TypeNameHandling.Auto`, les noms d'assemblies
  simples et le binder de compatibilité ;
- `m_WriteSettings` conserve les mêmes contrats de types et fixe explicitement
  `Formatting.Indented`.

Le binder reste partagé et continue de résoudre les anciennes assemblies
`Assembly-CSharp` ainsi que les migrations de namespaces existantes. La
séparation empêche désormais qu'une future option propre à la lecture modifie
accidentellement le format produit, ou inversement.

## Allocations supprimées

Les méthodes JSON n'ont plus de contrainte `where T : new()`.
`LoadFromJson<T>` ne construit donc plus une instance temporaire de `T` avant
de la remplacer par le résultat Json.NET.

Cette suppression concerne :

- `LoadFromJson<T>` ;
- `LoadFromJsonAsync<T>` ;
- `LoadFromJsonString<T>` ;
- `SaveToJSon<T>` ;
- `SaveToJsonAsync<T>`.

Les méthodes XML conservent leur contrainte et leur fonctionnement historique.

## Instrumentation

Avec un parseur streamé, la lecture physique a lieu pendant
`JsonSerializer.Deserialize`. Il n'est plus possible de mesurer une lecture
complète puis une désérialisation séparée sans recréer précisément la chaîne
que cette étape supprime.

Le marqueur `Read` est donc l'enveloppe extérieure :

- il conserve le nombre de fichiers et le nombre d'octets ;
- il inclut la lecture et la désérialisation streamées ;
- lorsque `Deserialize` est une phase distincte, celle-ci est imbriquée dans
  `Read`.

Les deux durées se chevauchent et ne doivent pas être additionnées. Si les
deux paramètres désignent la même phase, un seul scope est enregistré.
`totalWallMilliseconds` reste la mesure de référence.

## Compatibilité

- tous les noms de champs et toutes les valeurs `$type` sont inchangés ;
- les JSON indentés historiques restent lisibles ;
- les JSON compacts restent également lisibles ;
- les fichiers sauvegardés restent indentés et modifiables à la main ;
- le format et la structure des archives `.hibop` ne changent pas ;
- l'encodage d'écriture reste UTF-8 sans BOM ;
- aucune nouvelle réflexion ni génération dynamique n'est ajoutée.

La lecture directe dans une archive ZIP et la suppression de l'extraction
temporaire restent du ressort de l'étape 6.

## IL2CPP

Le nouveau chemin utilise uniquement `FileStream`, `StreamReader`,
`JsonTextReader`, `JsonSerializer` et leurs équivalents d'écriture. Il
n'ajoute ni réflexion, ni génération dynamique, ni type construit à
l'exécution.

La validation de cette étape a été exécutée dans l'Editor Mono. Comme pour les
étapes précédentes, le round-trip dans les players Windows et Linux IL2CPP
reste le verrou global prévu à l'étape 7.

## Validation automatisée

`ClassLoaderSaverTests` couvre désormais :

- la conservation des noms de types concrets ;
- la présence de l'indentation dans les fichiers produits ;
- les anciens noms d'assemblies et de namespaces ;
- la lecture équivalente de JSON indenté et compact ;
- un type sans constructeur par défaut, en synchrone et asynchrone ;
- un fichier vide ;
- des fichiers tronqué et malformé ;
- un fichier de plus de 85 Kio.

Résultats :

- tests ciblés `ClassLoaderSaverTests` et `LoadingDiagnosticsTests` :
  **13 / 13** ;
- `HBP.Serialization.Tests` et `HBP.ProjectWorkflow.Tests` :
  **415 / 415** ;
- erreurs et avertissements dans la console Unity : **0**.

## Benchmark

La médiane chaude de l'étape 4 constitue la référence avant cette
implémentation :

- base `Default` : **9 614,3 ms** ;
- projet `full_test` : **3 628,9 ms**.

Après l'étape 5, la médiane de trois passes chaudes atteint :

- base `Default` : **8 625,6 ms**, soit **-10,3 %** ;
- projet `full_test` : **2 929,8 ms**, soit **-19,3 %**.

Les collections GC baissent de 41,8 % sur la base et de 25,0 % sur le projet.
La première passe après compilation est toutefois plus lente que la référence
chaude, ce qui explique le ressenti initial.

Le protocole, les résultats détaillés et les limites de comparaison des phases
streamées sont consignés dans
[`resultats_etape_5_2026-07-24.md`](resultats_etape_5_2026-07-24.md).
