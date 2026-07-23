# Baseline de chargement

Date : 23 juillet 2026

## 1. Objectif

Cette baseline décrit les jeux de données utilisés pendant l'audit, les mesures
obtenues sans modifier les données et le protocole à conserver pour évaluer les
optimisations.

Les résultats de cette page ne sont pas des timings du player HiBoP. Ils
isolent les volumes, l'I/O et le parsing JSON générique sur la même machine.
La capture runtime Editor Mono effectuée ensuite est documentée dans
[`baseline_runtime_editor_mono_2026-07-23.md`](baseline_runtime_editor_mono_2026-07-23.md).

## 2. Jeux de données observés

Les identifiants, noms de patients, valeurs de tags et chemins métiers n'ont pas
été recopiés dans les résultats.

### 2.1 Workspaces de base locale

| Workspace anonymisé | Patients | Taille patients | Fichiers `DataInfo` | Taille `DataInfo` | Références |
| --- | ---: | ---: | ---: | ---: | ---: |
| W1, cas de charge | 240 | 109,27 Mio | 240 | 1,36 Mio | 4 |
| W2, vide | 0 | 0 | 0 | 0 | 0 |
| W3, actif pendant l'audit | 2 | 6,68 Mio | 3 | < 0,01 Mio | 1 |

La base contient aussi neuf protocoles pour environ 0,05 Mio.

### 2.2 Graphe patient de W1

| Objet | Nombre |
| --- | ---: |
| `Patient` | 240 |
| `BaseMesh` | 482 |
| `MRI` | 241 |
| `Site` | 34 829 |
| `Coordinate` | 70 028 |
| Tags de patient | 480 |
| Tags de site | 370 438 |
| **Valeurs de tags** | **370 918** |

La collection globale contient 122 définitions de tags :

- 45 tags de patient ;
- 77 tags de site ;
- aucun tag général dans cette capture.

### 2.3 Polymorphisme de W1

Types les plus fréquents dans les fichiers de W1 :

| Type | Occurrences `$type` |
| --- | ---: |
| `StringTagValue` | 370 678 |
| `Elan` | 1 517 |
| `IEEGDataInfo` | 1 517 |
| `LeftRightMesh` | 482 |
| `IntTagValue` | 240 |
| `BlocsCantBeEpochedWarning` | 190 |
| `NoMatchingSiteWarning` | 104 |

### 2.4 Projets réels

| Projet anonymisé | Archive | Non compressé | Patients | Autres entrées notables | `$type` |
| --- | ---: | ---: | ---: | --- | ---: |
| P1 | 16,04 Mio | 118,42 Mio | 239 | 2 datasets, 2 visualisations | 410 444 environ |
| P2, ancien format | 10,22 Mio | 81,92 Mio | 214 | 510 groupes, 8 datasets, 3 visualisations, 16 protocoles historiques | 159 147 environ |

P2 confirme qu'une rétrolecture doit tolérer des entrées de structure qui ne
sont plus produites par la version actuelle.

## 3. Mesures structurelles

### 3.1 Métadonnées de type

| Scope | Taille brute | `$type` | Taille des lignes `$type` | Part |
| --- | ---: | ---: | ---: | ---: |
| W1 patients | 109,27 Mio | 371 400 | 24,79 Mio | 22,7 % |
| W1 `DataInfo` | 1,36 Mio | 3 328 | 0,21 Mio | 15,3 % |
| W3 patients | 6,68 Mio | 26 664 | 1,78 Mio | 26,6 % |

La taille a été calculée en UTF-8 sur les lignes complètes qui portent
`"$type"`.

### 3.2 Indentation

Les 240 fichiers patients de W1 ont été parsés comme `JToken`, puis réémis
avec `Formatting.None`.

| Format | Taille |
| --- | ---: |
| JSON actuel indenté | 109,27 Mio |
| JSON compact équivalent | 73,88 Mio |
| Réduction | 32,4 % |

Cette mesure inclut la suppression des espaces et retours à la ligne, mais
conserve tous les champs et toutes les valeurs `$type`.

## 4. Mesures hors Unity

Machine de la session :

- Windows 11 x64 ;
- 32 Gio de mémoire physique ;
- cache OS chaud au moment des répétitions ;
- DLL Json.NET fournie par le package Unity.

### 4.1 Fichiers patients W1

Trois répétitions :

| Opération | Mesures | Médiane |
| --- | --- | ---: |
| `File.ReadAllText` des 240 fichiers, 109,27 Mio | 268 / 243 / 234 ms | 243 ms |
| Parse en `JToken`, sans modèles HiBoP | 1 066 / 1 111 / 967 ms | 1 066 ms |

Une passe parse + réémission compacte a pris 2 220 ms.

**Interprétation.** Sur ce cache chaud, les octets et la grammaire JSON brute
sont traités en environ 1,3 seconde. Tout écart important avec le chargement
HiBoP vient de la création des modèles, des callbacks, des références, des
validations de chemins, des allocations et du GC.

Cette comparaison n'est pas un benchmark direct de Json.NET dans Unity :
runtime, GC et options peuvent différer.

### 4.2 Archive projet P1

Mesure avec `System.IO.Compression` :

| Opération | Temps |
| --- | ---: |
| Ouverture + lecture directe des 248 entrées | 682 ms |
| Extraction des entrées vers le disque | 443 ms |
| Relecture de tous les fichiers extraits | 491 ms |
| Extraction + relecture | 934 ms |

L'économie I/O potentielle d'une lecture directe est d'environ 250 ms dans ce
cas. Elle ne suffit pas à expliquer une lenteur de plusieurs secondes ou
minutes.

## 5. Complexité calculée du chemin tags

Pour chaque valeur de tag :

1. `BaseTagValue.OnDeserialized` cherche le tag par ID ;
2. `CheckTagsAsync` vérifie qu'il appartient encore à `AllTags` ;
3. `CheckTagsAsync` vérifie qu'il fait partie des tags à mettre à jour.

La position moyenne réelle de la définition est 64,2. Aucun ID n'est absent.

| Élément | Valeur |
| --- | ---: |
| Valeurs | 370 918 |
| Comparaisons par passe | 23 814 096 |
| Passes linéaires | 3 |
| **Comparaisons estimées** | **71 442 288** |
| Reconstructions de `AllTags` | 742 076 |
| Objets liste/wrappers associés, hors tableaux | environ 3 710 380 |

Avec un dictionnaire :

- résolution par ID : une recherche moyenne O(1) ;
- appartenance : une recherche moyenne O(1) ;
- la liste de 122 tags est construite uniquement à la mutation.

## 6. Limites de la session

### Unity MCP

Le serveur `unityMCP` à `http://127.0.0.1:8080/mcp` n'était pas joignable.
L'éditeur était fermé ou le serveur non démarré.

### Unity CLI

Un test EditMode temporaire, ensuite supprimé, devait mesurer le pipeline
`GlobalDatabase.LoadDatabaseAsync` avec et sans parallélisme. Unity a obtenu la
licence correctement mais a quitté avant compilation :

```text
UnityPackageManager.exe absent de l'installation 6000.5.2f1
```

Aucun timing Unity n'est donc revendiqué. Le dossier de test temporaire et les
logs ont été supprimés.

## 7. Instrumentation à ajouter

Ajouter des marqueurs autour de phases distinctes, et non un seul chronomètre
global :

```text
Loading.Database.Settings
Loading.Database.Protocols
Loading.Database.References
Loading.Database.Patients.Read
Loading.Database.Patients.Deserialize
Loading.Database.Patients.BindTags
Loading.Database.Patients.ValidateFiles
Loading.Database.DataInfos.Read
Loading.Database.DataInfos.Deserialize
Loading.Database.LinkReferences

Loading.Project.Manifest
Loading.Project.ArchiveRead
Loading.Project.Settings
Loading.Project.Patients.Read
Loading.Project.Patients.Deserialize
Loading.Project.Patients.BindTags
Loading.Project.Groups
Loading.Project.Datasets
Loading.Project.Visualizations
Loading.Project.LinkReferences
Loading.Project.ValidateFiles
```

Pour chaque phase, capturer :

- durée murale et CPU ;
- nombre de fichiers et octets ;
- nombre d'objets par famille ;
- octets alloués et collections GC ;
- mémoire managée avant/après et pic ;
- degré de concurrence ;
- nombre de recherches de tags et de références ;
- nombre et durée des appels `File.Exists` ;
- annulation et erreurs.

## 8. Matrice de benchmark

### Jeux

1. projet synthétique minimal ;
2. fixtures actuelles de rétrocompatibilité ;
3. workspace W3 : 2 patients, gros fichiers individuels ;
4. workspace W1 : 240 patients, beaucoup de tags ;
5. projet P1 : 239 patients ;
6. projet P2 : ancien format avec protocoles historiques.

### Runtimes

- Editor Mono ;
- player Windows Mono si encore supporté par le produit ;
- player Windows IL2CPP ;
- player Linux IL2CPP.

### Variantes

- cache disque froid et chaud ;
- 1, 2, 4, 8 et 20 workers ;
- validation des fichiers activée/différée ;
- JSON indenté/compact ;
- lecture par `string`/`JsonTextReader` ;
- ancien binder/répertoire de types généré.

### Critères

Avant de fixer un objectif de temps arbitraire :

- réduire les résolutions de tags de O(V × T) à O(V) ;
- supprimer au moins 95 % des allocations liées à `AllTags` ;
- ne plus générer de GUID temporaire pour un ID présent ;
- n'exécuter aucun `File.Exists` dans le marqueur de parsing ;
- préserver tous les tests de sérialisation et les fixtures anciennes ;
- obtenir le même graphe de références en Mono et IL2CPP.

Une baseline murale officielle pourra alors servir à fixer un objectif produit.
