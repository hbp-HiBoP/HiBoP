# Étape 1 — Index stable des tags

Date : 23 juillet 2026

## Résultat

L'étape 1 du plan d'optimisation est implémentée et validée. Le chargement ne reconstruit
plus `TagCollection.AllTags` et ne parcourt plus linéairement les 122 tags pour
chaque valeur désérialisée ou validée.

Les résultats fonctionnels et les mesures avant/après sont détaillés dans
[`resultats_etape_1_2026-07-23.md`](resultats_etape_1_2026-07-23.md).

`TagCollection` maintient maintenant :

- une vue stable en lecture seule pour chaque catégorie ;
- une vue stable de tous les tags, dans l'ordre historique patients, sites,
  généraux ;
- un `Dictionary<string, BaseTag>` utilisant `StringComparer.Ordinal`.

Les vues et l'index sont reconstruits uniquement à la construction, après
désérialisation et après une mutation explicite de la collection.

## Changements fonctionnels

| Zone | Avant | Après |
| --- | --- | --- |
| `AllTags` | nouvelle liste et nouvelle vue à chaque accès | même vue entre deux mutations |
| `BaseTagValue.OnDeserialized` | parcours LINQ par ID | `TryGetTag`, recherche O(1) moyenne |
| Conditions de filtre | parcours LINQ par ID | `TryGetTag` |
| Validation des tags patients | `AllTags.Contains` par valeur | `ContainsTagId` |
| Sélection des tags à convertir | recherche dans un enumerable de tags | `HashSet<string>` partagé pour tout le chargement |
| Mutation de collection | modification directe des listes | construction transactionnelle d'un nouvel index |

Les trois chemins qui chargent ou revérifient plusieurs patients construisent
le jeu d'IDs une seule fois :

- chargement de la base ;
- chargement d'un projet ;
- modification de la collection de tags depuis l'interface.

Le même jeu est ensuite lu simultanément par les tâches patients. Il n'est
jamais modifié pendant leur exécution.

## Complexité attendue sur le cas W1

La baseline comportait 370 918 valeurs de tags, 122 définitions et environ
71,4 millions de comparaisons linéaires dans les trois passes principales.
Elle provoquait également environ 742 000 reconstructions de `AllTags`.

Après cette étape :

- l'index est construit en O(122) ;
- chaque résolution ou validation est une recherche de dictionnaire en O(1)
  moyen ;
- un accès à `AllTags` n'alloue rien entre deux mutations ;
- la complexité de ces passes devient linéaire dans le nombre de valeurs.

La mesure Editor Mono sur le même workspace confirme ce changement :
`BindTags` baisse de 86,0 % en durée cumulée médiane et le chargement total de
49,0 %.

## Contrats et cas particuliers

### Format JSON

Le format ne change pas. Les vues et le dictionnaire ne portent pas
`[JsonProperty]` dans une classe en `MemberSerialization.OptIn`; ils ne sont
donc ni écrits ni attendus dans les anciens fichiers. Le callback
`OnDeserialized` les reconstruit à partir des trois listes existantes.

Les `$type`, IDs, valeurs et noms de propriétés historiques restent inchangés.

Le `Tags.json` réel utilisé pour la baseline a aussi été contrôlé après
implémentation : 122 tags, aucun ID absent, aucun groupe d'IDs dupliqués et
trois types concrets (`FloatTag`, `IntTag`, `StringTag`). La nouvelle
validation d'unicité accepte donc le fichier existant.

### IDs dupliqués

Deux objets tags distincts portant le même ID sont désormais rejetés par une
`InvalidOperationException`. La construction du nouvel index précède la
publication des listes : une mutation invalide ne corrompt donc pas l'état
précédent.

Le même objet tag peut rester présent dans plusieurs catégories. Ce cas existe
dans certains scénarios de test et conserve la représentation historique de
`AllTags`; une seule entrée d'index pointe vers cet objet.

### IDs régénérés

`GenerateID` reconstruit l'index après avoir régénéré les IDs des tags.
`Copy`, les constructeurs, `Set*Tags`, `Add*Tag` et `Remove*Tag` passent tous
par la même méthode de reconstruction.

Un changement direct de `BaseTag.ID` effectué en dehors de ces opérations
laisserait en revanche l'index obsolète. Aucun chemin de chargement ou
d'édition recensé ne procède ainsi : l'interface republie les listes par les
méthodes `Set*Tags`.

### IL2CPP

L'implémentation utilise uniquement des génériques AOT usuels
(`Dictionary<string, BaseTag>`, `HashSet<string>` et collections en lecture
seule). Elle n'ajoute ni réflexion, ni génération dynamique, ni nouveau type
polymorphe sérialisé. Elle ne modifie donc pas le registre de types Json.NET
ou les contraintes de stripping IL2CPP.

## Validation ajoutée

`TagCollectionIndexTests` couvre :

- la stabilité des quatre vues entre deux mutations ;
- la mise à jour après `Add`, `Remove` et `Set` des trois catégories ;
- le rejet transactionnel des IDs dupliqués ;
- le même objet présent dans plusieurs catégories ;
- la reconstruction après `Copy` et `GenerateID` ;
- un aller-retour JSON et l'absence des caches dans le fichier ;
- la reconstruction de l'index avec les anciens `$type` de
  `Assembly-CSharp` ;
- la suppression d'un tag inconnu ;
- la conversion d'une ancienne `BaseTagValue` vers le sous-type courant ;
- une validation avec 100 000 valeurs.

Les assemblies runtime, UI et tests compilent sans erreur. Les 27 tests Unity
ciblés passent, ainsi que quatre chargements de base et quatre ouvertures du
projet de référence. Les nombres d'objets et de requêtes de tags sont
identiques à la baseline.
