# Plan d'optimisation du chargement HiBoP

Ce plan vise des gains progressifs sans abandonner :

- les types custom et polymorphes ;
- la lecture des anciens fichiers ;
- la compatibilité complète IL2CPP ;
- le découpage actuel en fichiers par patient lorsqu'il est utile aux mises à
  jour incrémentales.

## Principes

1. Mesurer chaque phase avant de changer le format.
2. Corriger d'abord les complexités et allocations prouvées.
3. Garder un lecteur de l'ancien format tant que des fichiers historiques
   existent.
4. Utiliser des tables et des `switch` explicites plutôt que de la découverte
   runtime par réflexion.
5. Séparer parsing, liaison des références, validation des chemins et
   publication.
6. Ne régler le parallélisme qu'après réduction de la pression GC.

## Étape 0 — Verrouiller la baseline

Statut : implémentée et première capture analysée le 23 juillet 2026. Voir
[`instrumentation_et_benchmark_etape_0.md`](instrumentation_et_benchmark_etape_0.md)
et
[`baseline_runtime_editor_mono_2026-07-23.md`](baseline_runtime_editor_mono_2026-07-23.md).

La capture confirme l'étape 1 comme prochaine priorité : 1 112 754 recherches
de tags ont été observées sur 370 918 valeurs, avec 156,6 s de
désérialisation patient et 32,5 s supplémentaires dans `CheckTagsAsync`,
cumulées sur les workers.

### Changements

- ajouter des `ProfilerMarker` ou un équivalent aux phases listées dans
  `baseline_chargement_2026-07-23.md` ;
- créer un test de performance opt-in qui n'est pas lancé dans la suite courte ;
- produire un résumé JSON ou CSV par exécution ;
- enregistrer séparément Editor et player IL2CPP.

### Fichiers probables

- `Assets/Scripts/HBP/Core/Tools/ClassLoaderSaver.cs`
- `Assets/Scripts/HBP/Core/Database/GlobalDatabase.cs`
- `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
- `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
- nouvelle assembly de tests performance

### Validation

- aucune modification du graphe chargé ;
- mesures présentes même en cas d'annulation ou d'erreur ;
- aucune donnée patient dans les logs.

## Étape 1 — Corriger l'index des tags

**Statut au 23 juillet 2026 : terminée et validée sur la base et le projet de
référence.** Voir
[`etape_1_index_tags_2026-07-23.md`](etape_1_index_tags_2026-07-23.md) et
[`resultats_etape_1_2026-07-23.md`](resultats_etape_1_2026-07-23.md).

### Conception minimale

Dans `TagCollection`, maintenir :

```csharp
private IReadOnlyList<BaseTag> m_AllTagsView;
private Dictionary<string, BaseTag> m_TagById;
```

Recalculer ces deux objets uniquement après :

- construction/désérialisation de la collection ;
- `Add*Tag` ;
- `Remove*Tag` ;
- `Set*Tags` ;
- régénération des IDs.

Exposer :

```csharp
public IReadOnlyList<BaseTag> AllTags => m_AllTagsView;
public bool TryGetTag(string id, out BaseTag tag);
public bool ContainsTagId(string id);
```

`BaseTagValue.OnDeserialized` utilise `TryGetTag`. `Patient.CheckTagsAsync`
capture une fois l'index et effectue les tests par ID.

### Compatibilité

Le JSON ne change pas. Les anciens `$type`, IDs et valeurs restent identiques.

### Tests

- mutation de chaque sous-collection met à jour vue et index ;
- ID dupliqué détecté explicitement ;
- tag inconnu supprimé comme aujourd'hui ;
- conversion entre anciens et nouveaux sous-types de valeurs inchangée ;
- test de charge avec au moins 100 000 valeurs ;
- allocations de `AllTags` nulles entre deux mutations.

### Critère de fin

Le cas W1 passe d'environ 71,4 millions de comparaisons linéaires à un nombre
linéaire de recherches de dictionnaire, sans différence fonctionnelle.

## Étape 2 — Supprimer les GUID temporaires

### Option retenue et implémentée

Introduire un champ d'ID avec génération paresseuse :

```csharp
[JsonProperty]
public string ID
{
    get => m_ID ??= NewId();
    set => m_ID = value;
}
```

Le constructeur sans paramètre n'appelle plus `NewId`. Un objet créé par le
produit obtient toujours un ID dès le premier accès, la sérialisation ou une
opération qui en dépend. Un objet désérialisé reçoit directement l'ID du
fichier.

L'implémentation réelle utilise `Volatile.Read` et
`Interlocked.CompareExchange` pour rendre le premier accès concurrent sûr. Le
getter ne traite que `null` comme « jamais assigné » : une chaîne vide
explicitement affectée reste observable par les contrôles d'intégrité.
`OnDeserialized` continue en revanche de réparer les anciens JSON sans ID ou
avec un ID vide.

### Points à vérifier

- `Equals` et `GetHashCode` ;
- collections utilisant un objet comme clé avant accès explicite à l'ID ;
- génération récursive ;
- création et duplication dans les écrans ;
- objets historiques sans champ `ID` ;
- threads concurrents accédant pour la première fois à l'ID.

Si la génération paresseuse crée une ambiguïté métier, limiter d'abord
l'optimisation aux familles très fréquentes via des `JsonConstructor` ou des
DTO. Ne pas utiliser `FormatterServices.GetUninitializedObject`, qui contourne
les invariants et augmente le risque IL2CPP.

### Critère de fin

Aucun GUID n'est créé pour un objet dont le JSON contient déjà un ID.

**État : validé le 23 juillet 2026.** Voir
[`etape_2_ids_paresseux_2026-07-23.md`](etape_2_ids_paresseux_2026-07-23.md)
et
[`resultats_etape_2_2026-07-23.md`](resultats_etape_2_2026-07-23.md).

## Étape 3 — Séparer validation de chemins et désérialisation

**Statut au 23 juillet 2026 : implémentée et validée fonctionnellement.** Voir
[`etape_3_validation_references_2026-07-23.md`](etape_3_validation_references_2026-07-23.md)
et
[`resultats_etape_3_2026-07-23.md`](resultats_etape_3_2026-07-23.md).

La séparation est effective, mais cette étape isolée ne démontre pas de gain
mural sur la campagne Editor Mono. La médiane augmente de 15,3 % sur la base
et de 7,5 % sur le projet par rapport à l'étape 2 ; le coût explicite de
validation reste pourtant limité à 32 ms et 19 ms.

### Conception

Les callbacks se limitent à :

- restaurer les champs ;
- normaliser les séparateurs.

Le validateur collecte ensuite les chemins depuis le graphe patient complet.
Ce choix évite un registre transitoire global pendant les callbacks.

Un `AssetReferenceValidator` :

- développe les aliases une fois ;
- déduplique les chemins ;
- valide existence et extension ;
- utilise une concurrence bornée distincte du parsing ;
- accepte un `CancellationToken` ;
- remplit `WasUsable` ou un résultat équivalent.

### Compatibilité

Le chargement peut continuer à attendre la validation avant de rendre
l'opération terminée. La sémantique visible reste alors identique.

### Tests

- chemins locaux, absents et réseau simulé ;
- aliases Windows/Linux ;
- annulation ;
- aucun `File.Exists` pendant le marqueur `Deserialize` ;
- même valeur finale de `IsUsable`.

## Étape 4 — Introduire un contexte de liaison

### Objectif

Retirer les recherches dans les singletons et les parcours répétés des
callbacks.

### API proposée

```text
LoadingContext
  TagById
  ProtocolById
  PatientById
  DatasetById
  BlocByIdByProtocolId
```

Le pipeline :

```mermaid
flowchart LR
    A["Read / parse"] --> B["Raw objects ou DTO"]
    B --> C["Build indexes"]
    C --> D["Resolve references"]
    D --> E["Validate files"]
    E --> F["Publish atomically"]
```

### Objets à migrer

- `BaseTagValue`
- `DataInfo`
- `PatientDataInfo`
- `Dataset`
- `Group`
- `Visualization`
- `PatientConfiguration`
- colonnes de visualisation
- conditions de filtre liées aux tags

### Bénéfices

- complexité O(n) ;
- résolution déterministe patient projet/base ;
- erreurs de références regroupées et contextualisées ;
- désérialisation indépendante de l'ordre global d'initialisation ;
- tests hors scène plus simples.

### Tests

- une référence résout exactement l'instance canonique attendue ;
- un `PatientDataInfo` de projet pointe vers le patient du projet ;
- références absentes : politique explicite, sans `First()` imprévisible ;
- IDs dupliqués : erreur explicite ;
- ancien projet et base actuelle.

## Étape 5 — Réduire les copies et la taille JSON sans nouveau schéma

### Changements

- settings de lecture séparés des settings d'écriture ;
- `Formatting.None` pour la base ;
- `JsonSerializer.Deserialize` sur `JsonTextReader`/`StreamReader` ;
- suppression de l'instance `new T()` inutile dans `LoadFromJson<T>` ;
- suppression de la contrainte `where T : new()` si elle n'est plus requise ;
- buffer de stream cohérent et métriques d'octets.

### Notes

La lecture streamée réduit surtout le pic mémoire et le passage par de grandes
chaînes. Elle ne corrigera pas une résolution de graphe quadratique.

Le format compact reste lisible par les versions actuelles de Json.NET. Il
faudra seulement accepter que les diffs manuels de fichiers deviennent moins
pratiques.

### Tests

- égalité fonctionnelle compact/indenté ;
- fichiers vides, tronqués et malformés ;
- gros fichier dépassant le LOH ;
- Mono et IL2CPP.

## Étape 6 — Simplifier le projet `.hibop`

### Manifeste unique

Créer une lecture unique qui retourne :

```text
ProjectManifest
  SchemaVersion
  ProductVersion
  Name
  Counts
  Entry index
  Preferences summary
```

`Project.GetProject`, `ProjectInfo` et `Project.LoadAsync` réutilisent ce
résultat au lieu de rouvrir l'archive.

### Lecture directe

Une fois le pipeline de liaison stable :

- désérialiser les entrées depuis leur stream ZIP ;
- ne plus extraire tout le projet vers
  `ApplicationState.ExtractProjectFolder` ;
- garder une extraction ciblée uniquement si une future entrée doit réellement
  exister comme fichier.

### Rétrocompatibilité

Le manifeste doit tolérer l'absence de `schemaVersion`. Dans ce cas, le projet
est classé `legacy-v0`. Les entrées historiques, notamment `Protocols/`, sont
indexées même si la politique courante choisit la base comme source canonique.

### Tests

- fixtures actuelles ;
- ancien projet avec dossier `Protocols/` ;
- archive incomplète ;
- noms d'entrées malveillants (`../`, chemins absolus) ;
- annulation et fermeture propre des streams ;
- resauvegarde puis relecture par la version courante.

## Étape 7 — Registre de types explicite et IL2CPP

### Génération Editor

Générer un fichier C# versionné contenant :

```csharp
["HBP.Core.Data.StringTagValue"] = typeof(StringTagValue)
```

La table contient :

- tous les types sérialisables actuels ;
- les noms d'assemblies actuels ;
- `Assembly-CSharp` et autres aliases historiques ;
- les migrations de namespace ;
- à terme, les discriminants courts du format v2.

Le générateur peut utiliser la réflexion dans l'Editor. Le player n'en a pas
besoin.

### Sécurité

Supprimer le fallback général `Type.GetType`. Un fichier ne peut demander que
les types explicitement autorisés.

### Validation IL2CPP

Créer un test player qui :

1. charge les fixtures anciennes ;
2. charge un graphe couvrant tous les types polymorphes ;
3. resauvegarde ;
4. recharge ;
5. compare types concrets, IDs et références ;
6. s'exécute en Windows IL2CPP et Linux IL2CPP.

Le registre `typeof` et les attributs `[Preserve]` fournissent ensemble des
racines claires au linker.

## Étape 8 — Ajuster le parallélisme

À mesurer seulement après les étapes 1 à 5.

### Scheduler attendu

- concurrence paramétrée par phase ;
- valeur par défaut issue des mesures, pas `20` en dur ;
- résultats stockés à leur index d'entrée ;
- `WaitAsync(token)` ;
- progression agrégée sans invoquer l'UI depuis un worker ;
- erreurs regroupées avec le nom logique du fichier ;
- aucune double bascule inutile vers le thread pool.

### Matrice

Tester 1, 2, 4, 8 et 20 workers sur W3, W1, P1 et P2. Capturer temps, CPU,
allocations, pic mémoire et GC.

Un plus grand nombre de workers n'est retenu que s'il améliore le temps sans
dépasser le budget mémoire ni dégrader l'annulation.

## Étape 9 — Format v2 optionnel

Cette étape n'est nécessaire que si, après les gains précédents, la taille et
le parsing des `$type` restent significatifs.

### Proposition

- manifeste `schemaVersion: 2` ;
- discriminants courts, stables et indépendants des assemblies ;
- sérialisation spécialisée pour `BaseTagValue`, `DataInfo`, `DataContainer`,
  `Column`, `Treatment` et conditions de filtre ;
- lecteur v1 conservé ;
- écriture v2 uniquement ;
- outil de migration séparé et réversible.

### À éviter

- remplacer tous les formats en une fois ;
- dépendre de noms de classes comme nouveau contrat ;
- retirer le lecteur v1 avant inventaire des projets ;
- utiliser de la génération dynamique indisponible en IL2CPP.

## Ordre de livraison proposé

| Lot | Contenu | Gain attendu | Risque |
| --- | --- | --- | --- |
| A | instrumentation + index tags | Très élevé sur gros graphes | Faible |
| B | ID paresseux + tests | Élevé sur allocations | Moyen |
| C | validation fichiers séparée | Très élevé si réseau lent | Moyen |
| D | contexte de liaison | Élevé et structurel | Moyen à élevé |
| E | JSON compact + streams | Modéré, mémoire améliorée | Faible |
| F | manifeste et lecture ZIP directe | Modéré | Moyen |
| G | registre généré + CI IL2CPP | Robustesse, sécurité, démarrage | Moyen |
| H | tuning concurrence | Variable, mesuré | Faible à moyen |
| I | format v2 | Taille et parsing | Élevé |

## Définition de terminé

Le chantier est terminé lorsque :

- les phases sont mesurables séparément ;
- les tags sont liés par index sans allocation par accès ;
- les IDs présents ne génèrent pas de GUID temporaire ;
- les callbacks JSON ne font plus d'I/O ;
- toutes les références sont liées par un contexte explicite ;
- les résultats sont déterministes quel que soit le nombre de workers ;
- les anciens projets et bases restent lisibles ;
- un player IL2CPP Windows et Linux couvre le round-trip ;
- le nouveau benchmark documente temps et mémoire avant/après sur W1 et P1.
