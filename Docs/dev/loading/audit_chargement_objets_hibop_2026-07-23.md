# Audit du chargement des objets HiBoP

Date : 23 juillet 2026  
Périmètre : base locale persistée et métadonnées des projets `.hibop`  
Version Unity attendue : `6000.5.2f1`  
Sérialiseur : package Unity `com.unity.nuget.newtonsoft-json` `3.2.2`

## 1. Synthèse

L'impression de lenteur est fondée, mais le travail exécuté dépasse largement
une simple désérialisation JSON.

Le coût dominant identifié est la reconstruction des références de tags. Sur
un workspace réel de 240 patients :

- 109,27 Mio de fichiers patients ;
- 34 829 sites ;
- 370 918 valeurs de tags ;
- 122 définitions de tags ;
- 24,79 Mio environ de métadonnées `$type` dans les seuls fichiers patients ;
- environ 71,4 millions de comparaisons de tags pendant la désérialisation et
  `CheckTagsAsync` ;
- environ 742 076 reconstructions de `TagCollection.AllTags`.

Chaque lecture de `AllTags` crée une nouvelle liste, trois wrappers de
sous-collections et un wrapper final. À l'échelle de ce workspace, cela
représente environ 3,7 millions de petits objets, auxquels s'ajoutent les
tableaux internes successifs des listes. L'ordre de grandeur des tableaux
transitoires associés approche le gigaoctet alloué sur toute l'opération, même
si cette mémoire n'est pas retenue simultanément.

Le second coût structurel est la construction des objets. Le constructeur sans
paramètre de `BaseData` génère systématiquement un GUID, alors que Json.NET
remplace ensuite `ID` par la valeur du fichier. Le graphe mesuré contient au
moins 476 738 objets dérivés de `BaseData` : autant de GUID et de chaînes
temporaires inutiles.

Enfin, certains callbacks `OnDeserialized` vérifient immédiatement l'existence
des fichiers de meshes et d'IRM. Le chargement mesuré inclut donc des accès au
système de fichiers, éventuellement à des partages réseau indisponibles. Ce
travail doit être présenté et instrumenté comme une phase de validation, pas
comme du parsing JSON.

La priorité n'est donc ni de remplacer Json.NET, ni d'augmenter immédiatement
le nombre de threads. Les deux premières corrections doivent être :

1. un index de tags stable et sans allocation par accès ;
2. une construction sans GUID temporaire pendant la désérialisation.

## 2. Périmètre exact

### Inclus

Le démarrage et l'ouverture de projet chargent les objets suivants :

| Source | Objets |
| --- | --- |
| Données persistantes globales | `UserPreferences`, `TagCollection`, `AliasCollection`, `FilterConditionsPresetCollection` |
| Base | `GlobalDatabaseSettings`, `Workspace`, `Protocol`, `DatabaseReference` |
| Patients | `Patient`, `BaseMesh`, `MRI`, `Site`, `Coordinate`, `BaseTagValue` et sous-types |
| Données fonctionnelles | `DataInfo` et sous-types, `DataContainer` et sous-types, erreurs et avertissements |
| Projet | `ProjectPreferences`, patients, `Group`, `Dataset`, `Visualization`, colonnes et configurations |

### Non inclus

Cet audit ne traite pas du chargement des séries temporelles, des volumes NIfTI,
des surfaces ou de la construction des scènes 3D. Ces données sont chargées
plus tard à l'ouverture d'une visualisation. Elles font l'objet des documents
existants sur le chargement des séries temporelles.

## 3. Flux actuel

### 3.1 Démarrage de la base

`DatabaseWorkflow.InitializeAsync` exécute les phases suivantes
(`Assets/Scripts/HBP/UI/Main/Database/DatabaseWorkflow.cs:13`) :

```mermaid
flowchart LR
    A["GlobalDatabase.InitializeAsync"] --> B["Settings.json"]
    B --> C["LoadProtocolsAsync"]
    C --> D["LoadDatabaseReferencesAsync"]
    D --> E["LoadPatientsAsync"]
    E --> F["Patient.CheckTagsAsync"]
    F --> G["LoadDataInfosAsync"]
    G --> H["IsLoaded = true"]
```

Les patients sont terminés avant les `DataInfo`, car les seconds doivent
retrouver leurs patients. Les fichiers d'un même groupe sont exécutés avec une
concurrence maximale fixée à 20
(`GlobalDatabase.cs:341-376`, `CSharpExtensions.cs:402-453`).

### 3.2 Ouverture d'un projet

La liste des projets et le chargement complet effectuent plusieurs lectures de
l'archive :

1. `Project.GetProject` appelle `Project.IsProject` pour chaque fichier
   (`Project.cs:342-350`) ;
2. `new ProjectInfo(path)` rappelle `Project.IsProject`, rouvre l'archive,
   compte les entrées, extrait les settings et les désérialise
   (`ProjectInfo.cs:33-74`) ;
3. `Project.LoadAsync` rouvre et extrait toute l'archive
   (`Project.cs:434-461`) ;
4. après l'extraction, `Project.LoadAsync` rappelle encore `IsProject`
   (`Project.cs:460`) ;
5. les settings sont désérialisés une seconde fois, puis les patients, groupes,
   datasets et visualisations sont chargés séquentiellement par catégorie
   (`Project.cs:465-493`, `593-716`).

```mermaid
flowchart LR
    A["Archive .hibop"] --> B["Validation / comptage"]
    B --> C["Extraction complète vers persistentDataPath"]
    C --> D["Settings"]
    D --> E["Patients + tags"]
    E --> F["Groups"]
    F --> G["Datasets + DataInfo"]
    G --> H["Visualizations + configurations"]
    H --> I["Suppression du dossier extrait"]
```

La séquence entre catégories est justifiée par les références, mais la
validation et la lecture des settings sont redondantes.

### 3.3 Désérialisation commune

`ClassLoaderSaver` :

- lit tout le fichier dans une `string` ;
- appelle `JsonConvert.DeserializeObject` ;
- utilise `TypeNameHandling.Auto` ;
- écrit au format indenté ;
- emploie un binder de rétrocompatibilité
  (`ClassLoaderSaver.cs:13-42`).

La variante `LoadFromJsonAsync` est un travail synchrone déporté sur le thread
pool. Il ne s'agit pas d'I/O asynchrone progressive.

## 4. Constats détaillés

### P0 — `TagCollection.AllTags` reconstruit la collection à chaque accès

**Preuve.** `TagCollection.AllTags` construit une `List<BaseTag>`, récupère
trois nouvelles `ReadOnlyCollection`, puis retourne encore une nouvelle
`ReadOnlyCollection` (`TagCollection.cs:21-30`).

`BaseTagValue.OnDeserialized` appelle cette propriété pour chaque valeur et
effectue une recherche linéaire (`BaseTagValue.cs:125-128`).

`Patient.CheckTagsAsync` recommence deux parcours :

- validation via `PersistentDataManager.Tags.AllTags.Contains` ;
- sélection via `tags.Contains`
  (`Patient.cs:177-183`).

**Mesure.** Pour les 370 918 valeurs observées, leur définition se trouve en
moyenne à la position 64,2 parmi 122 tags. Une passe représente 23 814 096
comparaisons. Les trois passes représentent environ 71 442 288 comparaisons.

Le nombre de reconstructions de `AllTags` est :

```text
370 918  pendant OnDeserialized
370 918  pendant la validation CheckTagsAsync
    240  pour l'argument passé une fois par patient
-------
742 076
```

**Impact.** CPU, pression GC et contention accrue lorsque 20 patients sont
désérialisés en parallèle. C'est le premier candidat à la lenteur extrême.

**Correction recommandée.**

- conserver une vue `IReadOnlyList<BaseTag>` reconstruite uniquement lors
  d'une mutation de la collection ;
- maintenir un `Dictionary<string, BaseTag>` indexé par ID ;
- exposer `TryGetById` au lieu de faire rechercher les consommateurs ;
- utiliser un `HashSet<string>` ou le dictionnaire pour la validation ;
- faire recevoir à `CheckTagsAsync` un index stable, pas une collection
  reconstruite ;
- à terme, déplacer la résolution des tags dans une phase de liaison en lot
  après le parsing.

Cette correction ne modifie pas le format de fichier et reste totalement
compatible avec IL2CPP.

### P0 — Un GUID temporaire est généré pour presque chaque objet

**Preuve.** `BaseData()` appelle `Guid.NewGuid().ToString()`
(`BaseData.cs:39-47`). Json.NET appelle les constructeurs sans paramètre des
types HiBoP, puis affecte l'`ID` lu.

Le workspace mesuré contient au minimum :

| Type | Nombre |
| --- | ---: |
| Patients | 240 |
| Meshes | 482 |
| IRM | 241 |
| Sites | 34 829 |
| Coordonnées | 70 028 |
| Valeurs de tags | 370 918 |
| **Minimum `BaseData`** | **476 738** |

Les protocoles, `DataInfo`, containers et autres objets augmentent encore ce
total.

**Impact.** Génération cryptographique de GUID, création de chaînes de
36 caractères et collecte de ces chaînes après remplacement.

**Correction recommandée.** Rendre l'ID paresseux :

- le constructeur sans paramètre laisse le champ interne nul ;
- le getter `ID` génère un GUID uniquement lors du premier accès ;
- le setter Json.NET affecte directement l'ID du fichier ;
- `OnDeserialized` conserve la génération de secours pour les anciens objets
  réellement sans ID.

Une autre option est un DTO ou un `JsonConstructor` par type, mais elle demande
beaucoup plus de changements. La variante paresseuse doit être caractérisée
par des tests sur `Equals`, `GetHashCode`, `Clone`, création UI et
désérialisation d'un ancien objet sans ID.

### P1 — Des accès aux fichiers sont exécutés dans les callbacks JSON

Les callbacks des meshes et IRM recalculent leur utilisabilité :

- `BaseMesh.OnDeserialized` appelle `RecalculateUsable`
  (`BaseMesh.cs:258-262`) ;
- `MRI.OnDeserialized` appelle `RecalculateIsUsable`
  (`MRI.cs:248-252`) ;
- `LeftRightMesh.HasMesh` appelle plusieurs fois `ConvertToFullPath`,
  `File.Exists` et `new FileInfo` (`LeftRightMesh.cs:124-138`) ;
- `MRI.HasMRI` fait de même (`MRI.cs:71-88`).

Le workspace mesuré contient 482 meshes et 241 IRM. Les chemins peuvent viser
des disques externes ou des partages réseau.

**Impact.** Une latence réseau ou un chemin indisponible est imputé au
« chargement JSON ». Le coût varie fortement selon la machine et rend le
parallélisme imprévisible.

**Correction recommandée.**

1. désérialiser et normaliser les chemins sans I/O ;
2. publier les métadonnées ;
3. valider l'existence des fichiers dans une phase explicite, instrumentée,
   annulable et à concurrence bornée ;
4. dédupliquer les vérifications par chemin normalisé ;
5. ne calculer qu'une fois chaque chemin développé avec les aliases.

Le comportement visible peut rester identique si la fin du chargement attend
toujours la phase de validation. Une évolution ultérieure peut permettre
l'affichage anticipé des métadonnées.

### P1 — Le format JSON transporte beaucoup de métadonnées

`TypeNameHandling.Auto` écrit le nom complet du type et de l'assembly pour les
objets polymorphes (`ClassLoaderSaver.cs:19`). Sur le workspace mesuré :

- 371 400 marqueurs `$type` dans les patients ;
- 24,79 Mio, soit 22,7 % des 109,27 Mio, sont uniquement les lignes `$type` ;
- un passage en JSON compact réduirait les patients de 109,27 à 73,88 Mio
  avant compression, soit 32,4 %.

Le format actuel est justifié par les nombreux types custom et par la
rétrocompatibilité. Il n'est pas nécessaire de le casser pour obtenir les gains
P0.

**Corrections sans changement de schéma.**

- écrire la base non compressée avec `Formatting.None` ;
- désérialiser depuis un `JsonTextReader` branché au stream pour éviter la
  `string` complète ;
- séparer les settings de lecture et d'écriture.

**Évolution de schéma ultérieure.**

- introduire un manifeste `schemaVersion` ;
- écrire des discriminants courts et stables (`"kind":"string"`) pour les
  familles polymorphes ;
- conserver un lecteur des anciens `$type` ;
- ne basculer l'écriture qu'après tests de double lecture.

Dans une archive ZIP, les longues chaînes `$type` se compressent bien. Le gain
de taille y sera inférieur à celui de la base non compressée.

### P1 — Résolution des références dispersée et linéaire

Les callbacks et propriétés reconstruisent le graphe en consultant l'état
global :

- `Dataset` et chaque `DataInfo` recherchent un protocole par parcours linéaire
  (`Dataset.cs:339-343`, `DataInfo.cs:488-492`) ;
- `Group` et `Visualization` recherchent chaque patient par parcours linéaire
  (`Group.cs:153-160`, `Visualization.cs:339-346`) ;
- les colonnes recherchent leur dataset à chaque accès, par exemple
  `IEEGColumn.cs:31-35` ;
- `PatientConfiguration.OnDeserialized` recherche directement le patient
  (`PatientConfiguration.cs:76-80`).

**Risque fonctionnel.** Pendant la désérialisation d'un dataset de projet,
celui-ci n'est pas encore ajouté à `ApplicationState.LoadedProject.Datasets`.
`PatientDataInfo.UpdatePatient` peut donc choisir le patient de la base plutôt
que l'instance du projet (`PatientDataInfo.cs:160-173`). Les tests unitaires de
dataset appellent d'ailleurs manuellement `UpdatePatient` après
`SetDatasets`.

**Correction recommandée.** Ajouter un `LoadingContext` construit une fois :

```text
TagById
ProtocolById
PatientById
DatasetById
BlocByIdByProtocolId
```

Le pipeline devient :

```text
parse brut -> indexation -> liaison des références -> validation -> publication
```

Les callbacks de modèle ne doivent plus dépendre de
`ApplicationState`, `DatabaseManager` ou `PersistentDataManager`. Cette
séparation améliore à la fois les performances, le déterminisme et la
testabilité.

### P1 — L'archive projet est relue et extraite inutilement

Le même projet est validé plusieurs fois et ses settings sont désérialisés
deux fois. Le chargement extrait ensuite tous les JSON sur disque avant de les
relire.

Sur un projet réel :

- archive : 16,04 Mio ;
- contenu non compressé : 118,42 Mio ;
- 248 entrées ;
- 239 patients ;
- 409 023 marqueurs `$type`.

Une mesure hors Unity, cache OS chaud, donne environ :

- 682 ms pour décompresser et lire directement toutes les entrées ;
- 443 ms pour extraire, puis 491 ms pour relire les fichiers ;
- soit 934 ms pour la stratégie extraction + relecture.

Le gain I/O potentiel observé est donc d'environ 250 ms sur cette machine. Il
est réel, mais inférieur au potentiel de la correction des tags.

**Correction recommandée.**

- remplacer `IsProject` + `ProjectInfo` par un unique
  `TryReadProjectManifest` ;
- réutiliser ce manifeste lors du chargement ;
- tester l'existence du fichier avant de l'ouvrir ;
- charger les entrées JSON directement depuis l'archive quand le pipeline de
  liaison est stabilisé ;
- si la lecture parallèle des entrées est conservée, ne pas partager sans
  validation un unique stream ZIP entre plusieurs workers.

### P2 — Le binder fait une découverte par réflexion

Le binder construit une seule fois un registre en parcourant les assemblies et
leurs types annotés `JsonObject` (`ClassLoaderSaver.cs:193-224`). Les
résolutions usuelles suivantes sont des recherches de dictionnaire. Cette
réflexion initiale n'explique donc pas à elle seule la lenteur proportionnelle
au nombre de patients.

Points positifs :

- le package Unity fournit une DLL AOT ;
- les types JSON HiBoP sont largement annotés `[Preserve]` ;
- le binder sait relire les anciennes assemblies `Assembly-CSharp` et deux
  anciens préfixes de namespace.

Risques :

- `Assembly.GetTypes` élargit la surface de réflexion IL2CPP ;
- le fallback `Type.GetType` n'est pas une allowlist stricte
  (`ClassLoaderSaver.cs:142-151`, `187-190`) ;
- les tests actuels sont EditMode/PlayMode, pas un player IL2CPP ;
- il n'existe pas de version de schéma permettant de choisir une migration
  avant de matérialiser le graphe.

**Correction recommandée.**

- générer en Editor une table source avec des références `typeof(...)` ;
- inclure les noms actuels et les aliases historiques ;
- refuser tout type absent de la table ;
- comparer la table au manifeste déjà contrôlé par
  `SerializationContractAuditTests` ;
- ajouter un test de round-trip dans un player IL2CPP Windows et Linux.

Cette table est plus sûre, plus rapide au démarrage et sert également de racine
explicite pour le linker.

### P2 — Le parallélisme fixe à 20 masque les allocations

`PerformMultipleTasksAsync` utilise un `SemaphoreSlim` avec une limite 20
codée dans les appels de projet et de base. Pour du parsing CPU et très
allocateur :

- 20 workers peuvent augmenter la contention GC ;
- chaque tâche rappelle `SwitchToThreadPool` ;
- le résultat générique est ajouté dans l'ordre de fin, donc non déterministe
  (`CSharpExtensions.cs:427-453`) ;
- l'optimum dépend du nombre de cœurs, des volumes de fichiers et des chemins
  réseau.

Il faut d'abord supprimer les allocations P0, puis mesurer 1, 2, 4, 8 et 20
workers. Le scheduler final doit conserver l'ordre des entrées et accepter
directement le token d'annulation dans `WaitAsync`.

## 5. Priorités recommandées

| Priorité | Changement | Compatibilité fichier | Risque |
| --- | --- | --- | --- |
| P0 | Index et vue cachée des tags | Inchangée | Faible |
| P0 | ID paresseux pendant la construction | Inchangée | Moyen |
| P1 | Phase de validation fichiers séparée | Inchangée | Moyen |
| P1 | `LoadingContext` et liaison en lot | Inchangée | Moyen à élevé |
| P1 | JSON compact + lecture streamée | Lecture inchangée | Faible |
| P1 | Manifeste projet unique, moins de scans | Inchangée | Moyen |
| P2 | Concurrence mesurée et ordonnée | Inchangée | Faible à moyen |
| P2 | Registre de types généré | Rétrolecture conservée | Moyen |
| P2 | Format v2 à discriminants courts | Double lecteur requis | Élevé |

## 6. Conclusion

Il est possible d'améliorer fortement ce chargement sans sacrifier les types
custom, la rétrocompatibilité ou IL2CPP.

La stratégie la plus sûre est progressive :

- garder Json.NET et le format actuel pendant les premiers gains ;
- rendre les recherches O(1) ;
- supprimer les constructions temporaires ;
- sortir les accès fichiers des callbacks ;
- centraliser la liaison des références ;
- mesurer ensuite le parallélisme et le streaming ;
- faire évoluer le format seulement avec un manifeste versionné et un double
  lecteur.

Cette approche cible les coûts prouvés avant d'engager une migration risquée du
sérialiseur.
