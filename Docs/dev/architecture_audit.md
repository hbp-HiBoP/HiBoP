# Architecture Audit

## 1. Résumé exécutif

La codebase présente une architecture Unity/C# déjà partiellement structurée autour d'assemblies `HBP.Core.Runtime`, `HBP.Data.Runtime`, `HBP.UI.Runtime`, `HBP.Theme.Runtime` et `HBP.Dev.Runtime`. Cette séparation donne une intention claire : un coeur applicatif, une couche de données/3D, une couche UI, une couche de thème et des outils de développement.

L'état réel est plus mélangé que cette intention. Les principaux risques observés sont :

* `HBP.Data` contient une large part de l'intégration Unity 3D, des `MonoBehaviour`, des prefabs, des caméras, du post-processing et du thème. Le nom "Data" ne décrit donc pas le rôle réel du module.
* `HBP.Core` n'est pas une couche métier indépendante : il contient des `MonoBehaviour`, des `ScriptableObject`, des `UnityEvent`, des `UnityEngine` types, des références UI (`TMPro`, `UnityEngine.UIElements`) et des accès fichiers/base de données.
* Plusieurs classes centrales sont très grosses et portent plusieurs responsabilités, notamment `Base3DScene`, `DataManager`, `Project`, `Patient`, `BIDSUtility` et certaines fenêtres d'export.
* Les dépendances sont majoritairement orientées `UI -> Data -> Core`, mais des dépendances de thème, post-processing, UIExtensions et état global statique s'infiltrent dans des couches qui devraient être plus testables.
* Les namespaces sont trop plats dans les zones les plus volumineuses : 263 fichiers sont dans `HBP.UI.Main` et 135 dans `HBP.Core.Data`, ce qui masque les sous-domaines réels.
* Aucune structure de tests claire n'a été trouvée, malgré la présence du package Unity Test Framework.

Les zones les plus problématiques sont `Assets/Scripts/HBP/Data/Module3D`, `Assets/Scripts/HBP/Core/Data`, `Assets/Scripts/HBP/UI/Main`, les managers statiques (`Module3DMain`, `DataManager`, `DatabaseManager`, `PersistentDataManager`) et les workflows import/export BIDS/localizer.

Priorités recommandées :

1. Clarifier la frontière entre domaine, persistance, rendu 3D Unity et UI.
2. Découper progressivement `Base3DScene` et `DataManager`.
3. Extraire les workflows import/export hors des fenêtres UI et hors des entités domaine.
4. Créer des asmdef de tests et réduire les dépendances Unity dans le coeur testable.
5. Nettoyer les namespaces et les fichiers suivis par Git qui sont spécifiques aux postes.

## 2. Cartographie de la codebase

### Racine du projet

Rôle apparent :

* Projet Unity principal avec solution générée, paramètres Unity, packages, assets, plugins natifs et documentation.

Dépendances principales :

* Unity, packages déclarés dans `packages/manifest.json`, plugins natifs sous `Assets/Plugins`, packages tiers sous `Assets/ThirdParty`.

Ambiguïtés :

* Plusieurs fichiers générés par Unity/IDE apparaissent à la racine, mais `.gitignore` les exclut (`*.csproj`, `*.sln`). `HiBoP.slnx` est suivi, et des fichiers `UserSettings` sont aussi suivis.

Fichiers notables :

* `README.md`
* `packages/manifest.json`
* `Assets/_Scenes/HiBoP.unity`
* `Assets/Plugins/Managed`, `Assets/Plugins/Native` et `Assets/Plugins/Editor/LegacyNative`
* `Assets/ThirdParty`
* `ProjectSettings/ProjectSettings.asset`

### `Assets/Scripts/HBP/Core`

Rôle apparent :

* Coeur applicatif : modèles métier (`Patient`, `Project`, `Dataset`, `Visualization`, tags, protocoles), base de données, préférences, outils, wrappers DLL, objets 3D partagés.

Dépendances principales :

* `UniTask`, `Unity.TextMeshPro` selon `Assets/Scripts/HBP/Core/HBP.Core.Runtime.asmdef`.
* Utilise aussi largement `UnityEngine`, `UnityEngine.Events`, `UnityEngine.Scripting`, `Newtonsoft.Json`, `Ionic.Zip`, DLL natives et filesystem.

Ambiguïtés :

* Le module combine modèle domaine, sérialisation JSON/XML, accès disque, import depuis bases de données, cache global, événements Unity et objets de scène Unity.
* Le nom `Core` suggère une couche stable et testable, mais l'asmdef a `noEngineReferences: false` et contient des types Unity concrets.

Fichiers notables :

* `Assets/Scripts/HBP/Core/Data/DataManager.cs`
* `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
* `Assets/Scripts/HBP/Core/Database/GlobalDatabase.cs`
* `Assets/Scripts/HBP/Core/Preferences/PersistentDataManager.cs`
* `Assets/Scripts/HBP/Core/Object3D/Site.cs`
* `Assets/Scripts/HBP/Core/Tools/ClassLoaderSaver.cs`

### `Assets/Scripts/HBP/Data`

Rôle apparent :

* Données applicatives d'affichage, module 3D runtime, BIDS export, informations graph/trial matrix, outils runtime.

Dépendances principales :

* `HBP.Core.Runtime`, `HBP.Theme.Runtime`, `UniTask`, `ThirdParty.UIExtensions.Runtime`, `Unity.Postprocessing.Runtime` selon `Assets/Scripts/HBP/Data/HBP.Data.Runtime.asmdef`.

Ambiguïtés :

* Le dossier s'appelle `Data`, mais `Assets/Scripts/HBP/Data/Module3D` est une couche d'intégration Unity 3D : `MonoBehaviour`, `GameObject`, `Transform`, `Camera`, prefabs, post-processing, `ThemeElement`.
* Le module contient aussi des workflows BIDS qui manipulent fichiers, JSON/TSV et données métier.

Fichiers notables :

* `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs`
* `Assets/Scripts/HBP/Data/Module3D/Module3DMain.cs`
* `Assets/Scripts/HBP/Data/Module3D/Camera3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/Column3D.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`
* `Assets/Scripts/HBP/Data/Informations/Graph/CurveData.cs`

### `Assets/Scripts/HBP/UI`

Rôle apparent :

* Présentation Unity UI : fenêtres, outils UI, toolbar, module 3D UI, informations, graphes, menus, préférences, base de données.

Dépendances principales :

* `HBP.Core.Runtime`, `HBP.Data.Runtime`, `HBP.Theme.Runtime`, `UniTask`, `ThirdParty.UIExtensions.Runtime`, `ThirdParty.SFB.Runtime`, `Unity.TextMeshPro`.

Ambiguïtés :

* Certaines fenêtres contiennent de la logique métier/export lourde, pas seulement de la coordination UI.
* `HBP.UI.Main` est un namespace très large qui couvre protocoles, patients, datasets, tags, visualisations, menus, filtres et plusieurs sous-dossiers.

Fichiers notables :

* `Assets/Scripts/HBP/UI/Main/Database/BIDS/ExportBIDSWindow.cs`
* `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`
* `Assets/Scripts/HBP/UI/Module3D/Scene3DWindow.cs`
* `Assets/Scripts/HBP/UI/Informations/Graph/Graph.cs`
* `Assets/Scripts/HBP/UI/Tools/List/List.cs`

### `Assets/Scripts/HBP/Theme`

Rôle apparent :

* Système de thème, états visuels, composants d'application de style.

Dépendances principales :

* `HBP.Core.Runtime`, `Unity.TextMeshPro`.

Ambiguïtés :

* La référence de `HBP.Data.Runtime` vers `HBP.Theme.Runtime` indique que le rendu 3D/data connaît directement le système de thème.

Fichiers notables :

* `Assets/Scripts/HBP/Theme/HBP.Theme.Runtime.asmdef`
* `Assets/Scripts/HBP/Theme/Components/ThemeElement.cs`
* `Assets/Scripts/HBP/Theme/Settings/*.cs`

### `Assets/Scripts/HBP/Dev`

Rôle apparent :

* Outils de développement, debug, build, éditeurs.

Dépendances principales :

* `HBP.Core.Runtime`, `HBP.Data.Runtime`, `HBP.Theme.Runtime`, `HBP.UI.Runtime`, `HBP.Dev.Runtime`.

Ambiguïtés :

* `HBP.Dev.Runtime` référence toute l'application, ce qui est acceptable pour des outils debug, mais augmente la surface runtime si ces scripts sont inclus dans les builds.

Fichiers notables :

* `Assets/Scripts/HBP/Dev/DevDebug.cs`
* `Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs`

### `Assets/ThirdParty` et `Assets/Plugins`

Rôle apparent :

* Dépendances externes C# et natives.

Dépendances principales :

* UIExtensions, StandaloneFileBrowser, DLL natives runtime `hbp_core`, `hbp_math` et `EEGFormat`, ainsi que `hbp_export`, OpenCV et ses dépendances réservés aux tests Editor.

Ambiguïtés :

* Les dépendances tierces C# sont isolées par asmdef, ce qui est positif.
* Les DLL natives sont consommées depuis `Core.DLL`, mais les couches métier/rendu appellent encore directement ces wrappers.

Fichiers notables :

* `Assets/ThirdParty/UIExtensions/ThirdParty.UIExtensions.Runtime.asmdef`
* `Assets/ThirdParty/StandaloneFileBrowser/ThirdParty.SFB.Runtime.asmdef`
* `Assets/Plugins/Native/Windows/x86_64/hbp_math.dll`
* `Assets/Plugins/Native/Linux/x86_64/libhbp_math.so`
* `Assets/Plugins/Editor/LegacyNative/macOS/arm64/hbp_export.bundle`

## 3. Problèmes identifiés

### Problème 1 — `HBP.Data` mélange données, scène Unity 3D et rendu

**Gravité :** élevée
**Type :** organisation / responsabilité / Unity / testabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Data/HBP.Data.Runtime.asmdef`
* `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs`
* `Assets/Scripts/HBP/Data/Module3D/Column3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/Camera3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/View3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/Modules/DisplayedObjects.cs`

**Constat :**
Le module `Data` ne contient pas seulement des structures de données. Il contient le coeur du module 3D Unity : scènes, colonnes, caméras, prefabs, `MonoBehaviour`, `GameObject`, `Transform`, post-processing et interactions de rendu.

**Preuves dans la codebase :**

* `HBP.Data.Runtime.asmdef` référence `HBP.Theme.Runtime`, `ThirdParty.UIExtensions.Runtime` et `Unity.Postprocessing.Runtime`.
* `Base3DScene` hérite de `MonoBehaviour`, référence `MeshManager`, `MRIManager`, `ImplantationManager`, `TriangleEraser`, `AtlasManager`, `FMRIManager`, `ROIManager`, `DisplayedObjects`, et déclare des prefabs `GameObject` pour les colonnes.
* `Column3D` hérite de `MonoBehaviour` et manipule `GameObject`, `Transform`, `LayerMask`, `Instantiate`.
* `Camera3D` utilise `UnityEngine.Rendering.PostProcessing`, `Theme.State` et `Theme.ThemeElement`.
* `DisplayedObjects` instancie des objets Unity (`new GameObject`, `Instantiate`, `MeshFilter`, layers).

**Pourquoi c’est un problème :**
Le nom et l'assembly `Data` donnent une impression de couche métier ou données réutilisable, alors qu'ils portent une couche Unity 3D complète. Cela complique les dépendances, rend les tests hors Unity difficiles et encourage les autres modules à dépendre d'un "Data" qui transporte en réalité la scène.

**Recommandation :**
Renommer/redécouper progressivement le module :

* garder les modèles purs dans `HBP.Core` ou un futur `HBP.Domain`;
* déplacer `Assets/Scripts/HBP/Data/Module3D` vers un namespace et dossier explicites, par exemple `Assets/Scripts/HBP/Runtime3D` ou `Assets/Scripts/HBP/Presentation/Module3D`;
* créer un asmdef dédié `HBP.Module3D.Runtime` qui référence `Core`, `Theme`, `Unity.Postprocessing` et les dépendances Unity nécessaires;
* réserver `HBP.Data` aux structures non-UI/non-scène, ou le renommer si son rôle réel est "runtime data + visualization".

**Risque de la correction :**
Moyen à élevé. Le déplacement physique et namespace peut impacter beaucoup de prefabs/scènes et références Unity. A faire par étapes : créer l'asmdef, déplacer uniquement les nouveaux fichiers ou un sous-ensemble stable, puis migrer les namespaces avec tests de scène.

### Problème 2 — `Base3DScene` est une classe centrale trop large

**Gravité :** élevée
**Type :** responsabilité / maintenabilité / Unity / testabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs`

**Constat :**
`Base3DScene.cs` fait 2337 lignes et orchestre presque tout le module 3D : état de scène, sélection, gestion de colonnes, cuts, textures, générateurs DLL, chargement async, préférences, événements UI, raycasts, nettoyage, chargement MNI, mesh, MRI, implantations, colonnes et activité.

**Preuves dans la codebase :**

* Le commentaire de classe dit explicitement : "This class manages everything concerning the scene."
* Le fichier déclare de nombreux managers sérialisés (`MeshManager`, `MRIManager`, `ImplantationManager`, `TriangleEraser`, `AtlasManager`, `FMRIManager`, `ROIManager`, `DisplayedObjects`).
* Le même fichier contient des régions `Cuts`, `Save/Load`, `Private Methods`, `Public Methods`.
* Il possède de nombreux événements `UnityEvent` et `GenericEvent`, dont `OnSelect`, `OnUpdateCuts`, `OnRequestSiteInformation`, `OnSelectSite`, `OnSceneCompletelyLoaded`, `OnUpdatingGenerators`.
* La méthode `Update()` déclenche la plupart des calculs différés : géométrie, cuts, textures MRI, textures fonctionnelles, surface fonctionnelle, rendu sites, générateur.
* Le fichier contient aussi des opérations de haut niveau comme `InitializeAsync`, `LoadBrainVolumeAsync`, `LoadBrainSurfaceAsync`, `LoadSitesAsync`, `LoadColumnsAsync`, `LoadActivityAsync`, `Clean`.

**Pourquoi c’est un problème :**
Une modification d'un détail de rendu, de chargement, de sélection ou de calcul d'activité risque d'avoir des effets collatéraux dans la même classe. La classe est difficile à tester car elle dépend de Unity, des prefabs, du singleton `Module3DMain`, des préférences globales, des wrappers DLL et de l'état de scène.

**Recommandation :**
Découper sans big bang :

* extraire un `SceneColumnController` pour création/sélection de colonnes;
* extraire un `SceneCutController` pour `AddCutPlane`, `RemoveCutPlane`, `UpdateCutPlane`, `ComputeMeshesCut`;
* extraire un `SceneLoadingService` ou `SceneInitializationWorkflow` pour `InitializeAsync`, chargements mesh/MRI/sites/colonnes;
* extraire un `SceneActivityComputationService` pour `LoadActivityAsync` et interactions avec `GeneratorSurface`;
* conserver `Base3DScene` comme façade `MonoBehaviour` qui câble les composants Unity et délègue.

**Risque de la correction :**
Élevé si fait en une fois. Moyen si on commence par extraire des classes internes ou services sans changer les prefabs ni les noms publics.

### Problème 3 — `HBP.Core` n'est pas une couche coeur indépendante de Unity

**Gravité :** élevée
**Type :** dépendance / Unity / testabilité / maintenabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/HBP.Core.Runtime.asmdef`
* `Assets/Scripts/HBP/Core/Object3D/Site.cs`
* `Assets/Scripts/HBP/Core/Object3D/SharedMaterials.cs`
* `Assets/Scripts/HBP/Core/Tools/Singleton.cs`
* `Assets/Scripts/HBP/Core/Tools/Manager.cs`
* `Assets/Scripts/HBP/Core/Tools/UnityExtensions.cs`
* `Assets/Scripts/HBP/Core/Data/FilterConditions/ProtocolFilterCondition.cs`
* `Assets/Scripts/HBP/Core/Data/Dataset/DataInfo/DataInfo.cs`

**Constat :**
`Core` contient des types Unity concrets et des dépendances UI. Il ne peut pas être utilisé comme domaine pur ni facilement testé hors Unity.

**Preuves dans la codebase :**

* `HBP.Core.Runtime.asmdef` a `noEngineReferences: false` et référence `Unity.TextMeshPro`.
* `Core/Object3D/Site.cs` déclare `public class Site : MonoBehaviour, IConfigurable`.
* `Core/Object3D/SharedMaterials.cs` déclare `public class SharedMaterials : ScriptableObject`.
* `Core/Tools/Singleton.cs` et `Core/Tools/Manager.cs` sont des bases `MonoBehaviour`.
* `Core/Tools/UnityExtensions.cs` utilise `UnityEngine`, `UnityEngine.Events`, `UnityEngine.UI`, `GameObject`, `Transform`, `RectTransform`, `Canvas`.
* `Core/Data/FilterConditions/ProtocolFilterCondition.cs` importe `TMPro`.
* `Core/Data/Dataset/DataInfo/DataInfo.cs` importe `UnityEngine.UIElements`.

**Pourquoi c’est un problème :**
Les modèles métier, la persistance et les règles de filtre deviennent dépendants de Unity et de composants UI. Cela bloque les tests unitaires classiques, augmente la surface de build IL2CPP/Unity et rend plus difficile l'utilisation du domaine dans des outils de conversion, CLI ou tests rapides.

**Recommandation :**
Créer une cible progressive :

* `HBP.Domain.Runtime` ou `HBP.Model.Runtime` avec `noEngineReferences: true`, sans `UnityEngine`;
* déplacer ou dupliquer d'abord les types simples qui peuvent vivre sans Unity;
* garder `HBP.Core.Unity.Runtime` pour `MonoBehaviour`, `UnityEvent`, `ScriptableObject`, matériaux, wrappers Unity;
* remplacer `TMPro`/`UIElements` dans les conditions métier par des types simples ou par des adaptateurs UI.

**Risque de la correction :**
Moyen à élevé. Il faut éviter de déplacer d'emblée les types sérialisés en JSON avec `TypeNameHandling`, car cela peut casser la compatibilité des projets sauvegardés. Commencer par les dépendances UI les plus évidentes.

### Problème 4 — Les asmdef expriment des dépendances UI/thème dans des couches basses

**Gravité :** moyenne
**Type :** dépendance / organisation / Unity
**Fichiers concernés :**

* `Assets/Scripts/HBP/Data/HBP.Data.Runtime.asmdef`
* `Assets/Scripts/HBP/Core/HBP.Core.Runtime.asmdef`
* `Assets/Scripts/HBP/Theme/HBP.Theme.Runtime.asmdef`
* `Assets/Scripts/HBP/Data/Module3D/Camera3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/View3D.cs`
* `Assets/Scripts/HBP/Data/Informations/Graph/CurveData.cs`
* `Assets/Scripts/HBP/Data/Informations/TrialMatrix/ChannelStruct.cs`

**Constat :**
Les assembly definitions officialisent des dépendances de présentation dans `Core` et `Data`.

**Preuves dans la codebase :**

* `HBP.Core.Runtime.asmdef` référence `Unity.TextMeshPro`.
* `HBP.Data.Runtime.asmdef` référence `HBP.Theme.Runtime`, `ThirdParty.UIExtensions.Runtime`, `Unity.Postprocessing.Runtime`.
* `Camera3D.cs` utilise `Theme.State`, `Theme.ThemeElement` et `PostProcessLayer`.
* `View3D.cs` utilise `PostProcessLayer`.
* `CurveData.cs`, `ShapedCurveData.cs` et `ChannelStruct.cs` utilisent `UnityEngine.UI.Extensions`.
* `HBP.Theme.Runtime.asmdef` référence `HBP.Core.Runtime`, ce qui donne une direction `Theme -> Core`, mais `Data -> Theme` réintroduit le thème dans le runtime 3D/data.

**Pourquoi c’est un problème :**
Les assemblies ne permettent pas d'isoler une couche domaine/data testable. Toute dépendance à `HBP.Data.Runtime` embarque aussi thème, post-processing et UIExtensions. Les builds et tests sont plus lourds, et les cycles conceptuels deviennent plus probables même si les asmdef n'ont pas de cycle direct.

**Recommandation :**
Créer des assemblies plus explicites :

* `HBP.Data.Model.Runtime` pour les structures de données sans UI;
* `HBP.Module3D.Runtime` pour Unity 3D, post-processing et thème;
* `HBP.Informations.Runtime` ou `HBP.Graph.Data.Runtime` si les données de graphes doivent rester séparées;
* limiter `Unity.TextMeshPro` à `HBP.UI.Runtime` ou `HBP.Theme.Runtime` si possible.

**Risque de la correction :**
Moyen. Les asmdef peuvent être introduits sans renommer tous les namespaces, mais Unity peut nécessiter des mises à jour de références de scripts si des fichiers changent d'assembly.

### Problème 5 — `DataManager` est un cache global statique trop central

**Gravité :** élevée
**Type :** responsabilité / testabilité / maintenabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/Data/DataManager.cs`

**Constat :**
`DataManager` est une classe statique de 1344 lignes qui concentre chargement, déchargement, cache, statistiques, normalisation, verrouillage thread-safe et politique de défauts.

**Preuves dans la codebase :**

* Le fichier déclare de nombreux dictionnaires statiques : `m_DataByRequest`, `m_BlocDataByRequest`, `m_ChannelDataByRequest`, `m_ChannelStatisticsByRequest`, `m_BlocEventsStatisticsByRequest`, `m_NormalizeByRequest`.
* Il utilise un `ReaderWriterLockSlim` global.
* Il expose `Load`, `UnLoad`, `Reload`, `Clear`, `GetData`, `GetStatistics`, `GetEventsStatistics`, `NormalizeiEEGData`, `Dispose`.
* Il contient les algorithmes `NormalizeByNone`, `NormalizeBySubTrial`, `NormalizeByTrial`, `NormalizeBySubBloc`, `NormalizeByBloc`, `NormalizeByProtocol`.

**Pourquoi c’est un problème :**
La classe combine stockage, politique de cache, calcul de statistiques et transformation de données. L'état global rend les tests interdépendants, complique la concurrence et rend difficile de raisonner sur la durée de vie des données en mémoire, surtout avec de gros volumes iEEG/MEG/fMRI.

**Recommandation :**
Introduire une façade instance-based sans supprimer immédiatement l'API statique :

* `IDataRepository` ou `IDataCache` pour le stockage par requête;
* `DataStatisticsService` pour les statistiques;
* `DataNormalizationService` pour la normalisation;
* conserver `DataManager` comme adaptateur statique temporaire qui délègue à une instance injectable;
* ajouter des tests ciblés sur les services extraits.

**Risque de la correction :**
Moyen. Les appels sont probablement nombreux; la compatibilité peut être préservée si l'API publique statique reste en façade pendant la migration.

### Problème 6 — Les entités domaine gèrent aussi persistance, import et base de données

**Gravité :** élevée
**Type :** responsabilité / testabilité / dépendance
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/BaseMesh.cs`
* `Assets/Scripts/HBP/Core/Data/Dataset/Container/DataContainer.cs`

**Constat :**
Des classes qui ressemblent à des modèles métier contiennent directement des opérations de filesystem, sérialisation, zip, import BIDS/Intranat, copie de fichiers et accès aux préférences/base de données.

**Preuves dans la codebase :**

* `Project.cs` utilise `Ionic.Zip`, `UnityEngine`, `PersistentDataManager`, `ApplicationState`, `Directory`, `File`, `ClassLoaderSaver`, et contient `LoadAsync`, `SaveAsync`, `LoadPatientsAsync`, `SavePatientsAsync`.
* `Patient.cs` implémente `ILoadableFromDatabase<Patient>` et `ILoadableFromDirectory<Patient>`, lit des dossiers, charge des patients depuis Intranat/BIDS, manipule `DatabaseReference`, `PersistentDataManager.Tags`, `BIDSParser`.
* `BaseMesh.cs` recherche directement des fichiers anatomiques et transformations dans l'arborescence disque.
* `DataContainer.cs` impose `CopyDataToDirectory` et chemins de projet directement dans l'abstraction de donnée.

**Pourquoi c’est un problème :**
Les modèles deviennent difficiles à valider indépendamment. Chaque changement de format de projet, de base de données ou de convention BIDS risque de modifier les classes centrales. La compatibilité JSON avec `TypeNameHandling` rend aussi les déplacements futurs plus délicats.

**Recommandation :**
Extraire les workflows hors des modèles :

* `ProjectSerializer` / `ProjectArchiveService` pour `.hibop` et zip;
* `PatientImportService` avec implémentations `IntranatPatientImporter` et `BIDSPatientImporter`;
* `DataContainerFileService` pour les copies et chemins;
* garder `Project`, `Patient`, `BaseMesh`, `DataContainer` centrés sur état et invariants métier.

**Risque de la correction :**
Moyen à élevé. Les formats sauvegardés et le binder legacy de `ClassLoaderSaver` doivent être pris en compte. L'extraction peut commencer par déplacer seulement les méthodes statiques d'import.

### Problème 7 — Des fenêtres UI portent de la logique d'export métier lourde

**Gravité :** moyenne
**Type :** responsabilité / UI / testabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/UI/Main/Database/BIDS/ExportBIDSWindow.cs`
* `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`

**Constat :**
Certaines fenêtres UI ne se limitent pas à collecter les choix utilisateur. Elles construisent les listes depuis `DatabaseManager`, valident, ferment des scènes 3D, instancient des items, lancent des exports, manipulent chemins et orchestrent des workflows métier.

**Preuves dans la codebase :**

* `ExportBIDSWindow.OK()` valide dataset, patients, protocoles, données, dossier de sortie et overwrite.
* `ExportBIDSWindow` lit directement `DatabaseManager.Database.DataInfos`, `PersistentDataManager.Tags`, charge une configuration BIDS, crée des listes de protocoles/data names et appelle `LoadingManager.LoadAsync(ExportBIDSAsync)`.
* `ExportLocalizerAtlasWindow.OK()` inspecte `ApplicationState.LoadedProject`, `Module3DMain.Visualizations`, peut appeler `Module3DMain.RemoveAllScenes()`, puis lance `ExportAtlasAsync`.
* `ExportLocalizerAtlasWindow.ExportAtlasAsync` initialise directement `GeneratorSurface`, manipule données iEEG et export atlas.

**Pourquoi c’est un problème :**
Ces workflows sont difficiles à tester sans UI Unity, et toute évolution du format d'export modifie des fenêtres. Les règles métier d'export ne sont pas réutilisables depuis une CLI, un batch, un test ou un outil futur.

**Recommandation :**
Créer des services d'application :

* `BIDSExportService` consommant une commande `BIDSExportRequest`;
* `LocalizerAtlasExportService` consommant une commande `LocalizerAtlasExportRequest`;
* garder les fenêtres responsables de l'affichage, de la sélection utilisateur et de la présentation des erreurs;
* déplacer la validation métier dans les services et retourner un résultat typé.

**Risque de la correction :**
Moyen. La logique peut être extraite sans changer l'UI visible si les fenêtres appellent les nouveaux services.

### Problème 8 — L'état global statique crée un couplage transversal fort

**Gravité :** élevée
**Type :** dépendance / testabilité / maintenabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Data/Module3D/Module3DMain.cs`
* `Assets/Scripts/HBP/Core/Tools/ApplicationState.cs`
* `Assets/Scripts/HBP/Core/Preferences/PersistentDataManager.cs`
* `Assets/Scripts/HBP/Core/Database/DatabaseManager.cs`
* `Assets/Scripts/HBP/Core/Data/DataManager.cs`
* `Assets/Scripts/HBP/Core/Data/FilterConditions/SpecificSiteLocationFilterCondition.cs`

**Constat :**
L'application repose sur de nombreux singletons/statics et événements statiques pour l'état courant, les préférences, la base, le projet, les scènes 3D et les données chargées.

**Preuves dans la codebase :**

* `Module3DMain` expose `SelectedScene`, `SelectedColumn`, `Scenes`, `Visualizations`, `SharedMaterials`, `SharedDirectionalLight`, et de nombreux événements statiques.
* `ApplicationState` expose `LoadedProject`, `LoadedProjectLocation`, `TMPFolder`, `ExtractProjectFolder`, `DataPath`, `DatabasePath`.
* `PersistentDataManager` expose statiquement `UserPreferences`, `Tags`, `Aliases`, `FilterConditionsPresets`.
* `DatabaseManager.Database` donne un accès global à `GlobalDatabase`.
* `DataManager` stocke globalement les données chargées.
* `SpecificSiteLocationFilterCondition.SceneLocationEvaluator` est un délégué statique défini depuis `Module3DMain.Initialization()`, ce qui crée une dépendance inversée implicite entre un filtre de `Core` et la scène 3D.

**Pourquoi c’est un problème :**
Les tests doivent initialiser une grande partie de l'application pour exercer une règle métier. Les dépendances implicites rendent les bugs d'ordre d'initialisation plus probables. Les événements statiques peuvent garder des abonnements et créer des effets entre scènes ou entre tests.

**Recommandation :**
Réduire progressivement l'état global :

* introduire des interfaces `IApplicationState`, `IPreferencesProvider`, `IDatabaseProvider`, `IModule3DContext`;
* injecter ces dépendances dans les services extraits;
* garder les singletons Unity comme composition root temporaire;
* remplacer `SpecificSiteLocationFilterCondition.SceneLocationEvaluator` par un service de filtre ou un contexte passé explicitement lors de l'évaluation.

**Risque de la correction :**
Moyen à élevé. Les singletons sont largement utilisés. Commencer par les nouveaux services et les tests, sans supprimer les APIs statiques existantes.

### Problème 9 — Namespaces trop plats et incohérents avec les dossiers

**Gravité :** moyenne
**Type :** namespace / organisation / maintenabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/UI/Main/**/*.cs`
* `Assets/Scripts/HBP/UI/Main/Database/BIDS/ExportBIDSWindow.cs`
* `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`
* `Assets/Scripts/HBP/UI/Main/Database/TrialMatrixExplorer/TrialMatrixExplorerWindow.cs`
* `Assets/Scripts/HBP/Data/Informations/TrialMatrix/ChannelStruct.cs`
* `Assets/Scripts/HBP/Core/Data/Loaded/Processed/Timeline.cs`

**Constat :**
Les namespaces ne reflètent pas toujours la structure de dossiers. Les deux principaux namespaces sont très larges : 263 fichiers dans `HBP.UI.Main` et 135 dans `HBP.Core.Data`.

**Preuves dans la codebase :**

* Beaucoup de fichiers sous `Assets/Scripts/HBP/UI/Main/Experience`, `Patients`, `Tags`, `Visualization`, `FilterConditions`, `Menu` utilisent tous `namespace HBP.UI.Main`.
* Sous `Assets/Scripts/HBP/UI/Main/Database`, certains fichiers utilisent `HBP.UI.Database` (`TrialMatrixExplorerWindow.cs`, `DatabaseBrowserWindow.cs`, `DatabaseReferenceModifier.cs`), tandis que les exports BIDS/localizer utilisent `HBP.UI.Main`.
* `Assets/Scripts/HBP/Data/Informations/TrialMatrix/ChannelStruct.cs` est dans `namespace HBP.Data.Informations`, alors que les autres fichiers de ce dossier utilisent plutôt `HBP.Data.Informations.TrialMatrix`.
* `Assets/Scripts/HBP/Core/Data/Loaded/Processed/Timeline.cs` contient un commentaire `FIXME` indiquant que ces classes n'ont peut-être rien à faire dans ce namespace.

**Pourquoi c’est un problème :**
Les imports ne renseignent pas sur le sous-domaine réel. Le risque de collisions de noms augmente (`Data`, `ChannelBloc`, `TrialMatrixGrid`, etc.). Il devient plus difficile de déplacer un module, d'identifier les frontières et de chercher les dépendances.

**Recommandation :**
Adopter une convention simple :

* `HBP.UI.Main.Patients`, `HBP.UI.Main.Protocols`, `HBP.UI.Main.Visualization`, `HBP.UI.Database.BIDS`, `HBP.UI.Database.Localizer`, etc.;
* `HBP.Data.Informations.TrialMatrix` pour tout le dossier trial matrix;
* créer une règle de revue : nouveau dossier public = namespace correspondant;
* traiter les fichiers sérialisés avec prudence si leurs types sont persistés.

**Risque de la correction :**
Moyen. Les renommages namespace sont mécaniques mais peuvent impacter la sérialisation et les références Unity. A faire par familles cohérentes.

### Problème 10 — Workflows BIDS dispersés entre `Core`, `Data` et `UI`

**Gravité :** moyenne
**Type :** organisation / responsabilité / dépendance
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
* `Assets/Scripts/HBP/Core/Tools/BIDSParser.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSConfigurationManager.cs`
* `Assets/Scripts/HBP/UI/Main/Database/BIDS/ExportBIDSWindow.cs`
* `Assets/Data/BIDS/example_bids_config.json`

**Constat :**
Le domaine BIDS est réparti dans plusieurs couches : parsing en `Core.Tools`, import patient dans `Patient`, export/configuration dans `Data.BIDS`, orchestration dans `UI.Main.Database.BIDS`, configuration exemple dans `Assets/Data/BIDS`.

**Preuves dans la codebase :**

* `Patient.LoadFromBIDSDatabase` lit `participants.tsv`, utilise `BIDSParser.FindFiles`, construit mesh/MRI/electrodes/tags.
* `BIDSUtility` crée patients BIDS, écrit `participants.tsv`, `dataset_description.json`, copie données anatomiques/fonctionnelles.
* `ExportBIDSWindow` sélectionne patients/protocoles/tags et lance l'export.
* `Assets/Data/BIDS/example_bids_config.json` vit avec les données Unity, pas avec le code de configuration BIDS.

**Pourquoi c’est un problème :**
Chaque évolution BIDS demande de comprendre plusieurs couches. L'import et l'export peuvent diverger. La logique est difficile à tester sans Unity UI et sans état global.

**Recommandation :**
Créer un module explicite `HBP.BIDS.Runtime` ou `HBP.IO.BIDS.Runtime` :

* `BIDSParser`, `BIDSPatient`, `BIDSUtility`, `BIDSConfigurationManager`, import/export requests;
* interfaces vers filesystem et tags;
* UI réduite à l'adaptateur;
* config exemple documentée et versionnée avec le module ou dans `Assets/Data/BIDS` mais chargée par un service clair.

**Risque de la correction :**
Moyen. Le code BIDS est assez regroupable, mais il touche modèles patient, tags, protocoles et fichiers.

### Problème 11 — Absence apparente de structure de tests applicatifs

**Gravité :** moyenne
**Type :** testabilité / build / maintenabilité
**Fichiers concernés :**

* `packages/manifest.json`
* `Assets/Scripts/HBP/Core/Data/DataManager.cs`
* `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`

**Constat :**
Le package `com.unity.test-framework` est déclaré, mais aucune structure claire de tests applicatifs ni asmdef de tests n'a été trouvée. Les seuls fichiers détectés avec `Test` dans le nom sont des utilitaires UI ou tiers (`MinimumSizeTester`, `ColorPickerTester`), pas des tests.

**Preuves dans la codebase :**

* `packages/manifest.json` contient `"com.unity.test-framework": "1.6.0"`.
* La recherche `*Test*.cs` / `*Tests*.cs` dans `Assets` ne remonte pas de tests métier ou edit mode/play mode.
* Aucun asmdef de type test (`.Tests.asmdef`) n'a été trouvé dans la liste des asmdef.
* Les classes à risque (`DataManager`, `Project`, `Patient`, `BIDSUtility`) n'ont pas de tests observés dans le dépôt.

**Pourquoi c’est un problème :**
Les refactors recommandés touchent des formats de données, des imports/exports et des calculs de données. Sans tests, chaque découpage architectural sera risqué et dépendra de vérifications manuelles Unity.

**Recommandation :**
Ajouter d'abord des tests de caractérisation :

* `Assets/Tests/EditMode/HBP.Core.Tests.asmdef` pour modèles purs, sérialisation et utilitaires;
* tests sur `ClassLoaderSaver`, tags, `BIDSParser`, `BIDSUtility` avec petits fixtures;
* tests sur `DataManager` via scénarios minimaux;
* play mode tests seulement pour les composants Unity 3D qui nécessitent scène/prefab.

**Risque de la correction :**
Faible à moyen. Ajouter des tests n'affecte pas le runtime si les asmdef sont correctement isolés. Le risque principal est de devoir rendre certaines dépendances injectables.

### Problème 12 — Fichiers générés ou spécifiques utilisateur suivis par Git

**Gravité :** faible
**Type :** organisation / maintenabilité / build
**Fichiers concernés :**

* `.gitignore`
* `HiBoP.slnx`
* `UserSettings/EditorUserSettings.asset`
* `UserSettings/Layouts/CurrentMaximizeLayout.dwlt`
* `UserSettings/Layouts/default-2021.dwlt`
* `UserSettings/Layouts/default-6000.dwlt`
* `UserSettings/Search.index`
* `UserSettings/Search.settings`

**Constat :**
`.gitignore` ignore bien `UserSettings` indirectement ? Il ignore surtout `Library`, `Temp`, `Obj`, `Logs`, `*.csproj`, `*.sln`, etc. Pourtant des fichiers `UserSettings` sont suivis par Git et `HiBoP.slnx` est suivi.

**Preuves dans la codebase :**

* `git ls-files '*.csproj' '*.sln' '*.slnx' Library Temp obj UserSettings` retourne `HiBoP.slnx` et 7 fichiers sous `UserSettings`.
* `git status --short` montre actuellement `M HiBoP.slnx` et `M UserSettings/Layouts/default-6000.dwlt`, ce qui illustre le bruit de changements locaux.

**Pourquoi c’est un problème :**
Ces fichiers changent selon l'IDE, la version Unity ou la disposition locale. Ils ajoutent du bruit dans les revues et peuvent créer des conflits sans lien avec l'architecture applicative.

**Recommandation :**
Vérifier si `HiBoP.slnx` est volontairement versionné. Si non, le retirer du suivi Git. Ajouter explicitement `/[Uu]ser[Ss]ettings/` à `.gitignore` et retirer les fichiers `UserSettings` du suivi, après validation de l'équipe.

**Risque de la correction :**
Faible. Attention toutefois si l'équipe veut partager certains layouts ou settings Unity; dans ce cas, documenter l'exception.

### Problème 13 — Dépendances natives et wrappers DLL très proches du coeur

**Gravité :** moyenne
**Type :** dépendance / build / testabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/DLL/*.cs`
* `Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs`
* `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs`
* `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`
* `Assets/Plugins/Native/Windows/x86_64/hbp_math.dll`
* `Assets/Plugins/Native/Linux/x86_64/libhbp_math.so`
* `Assets/Plugins/Editor/LegacyNative/macOS/arm64/hbp_export.bundle`

**Constat :**
Les wrappers et générateurs natifs sont au coeur des modèles/3D et sont appelés depuis plusieurs couches sans abstraction applicative claire.

**Preuves dans la codebase :**

* `Base3DScene` manipule `Core.DLL.GeneratorSurface`, `Core.DLL.CutGeometryGenerator`, `Core.DLL.BBox`.
* `ExportLocalizerAtlasWindow` instancie directement `GeneratorSurface`.
* `Mesh3D` charge des fichiers GIFTI via des objets `Surface`.
* Les plugins natifs runtime sont séparés par OS et architecture sous `Assets/Plugins/Native`; le legacy de comparaison est isolé sous `Assets/Plugins/Editor/LegacyNative`.

**Pourquoi c’est un problème :**
Les tests et outils hors Unity dépendent implicitement de DLL natives présentes et correctement configurées. Les builds multi-plateformes et IL2CPP peuvent être fragiles si les appels natifs ne sont pas isolés derrière des services remplaçables.

**Recommandation :**
Créer des interfaces fines autour des opérations natives critiques :

* `ISurfaceLoader`, `IVolumeLoader`, `IActivitySurfaceGenerator`, `ICutGeometryGeneratorFactory`;
* garder les implémentations DLL dans `HBP.Native` ou `HBP.Core.DLL`;
* injecter ces interfaces dans les services 3D/export plutôt que d'instancier directement les wrappers.

**Risque de la correction :**
Moyen. Les wrappers existent déjà; l'extraction d'interfaces peut se faire sans changer les DLL, mais il faudra adapter les consommateurs.

### Problème 14 — Module `Core.Data.Loaded.Processed` signale lui-même un mauvais emplacement

**Gravité :** faible
**Type :** organisation / namespace / maintenabilité
**Fichiers concernés :**

* `Assets/Scripts/HBP/Core/Data/Loaded/Processed/Timeline.cs`
* `Assets/Scripts/HBP/Core/Data/Loaded/Processed/*.cs`
* `Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs`
* `Assets/Scripts/HBP/Data/Module3D/Column3DFMRI.cs`
* `Assets/Scripts/HBP/Data/Module3D/Column3DMEG.cs`

**Constat :**
Le fichier `Timeline.cs` contient un commentaire `FIXME` indiquant que ces classes n'ont peut-être rien à faire dans ce namespace et sont surtout utilisées pour l'affichage 3D.

**Preuves dans la codebase :**

* `Timeline.cs` déclare `namespace HBP.Core.Data` avec le commentaire : `FIXME : maybe these classes have nothing to do in this namespace. They are mostly used in 3D display...`
* Les colonnes 3D dynamiques/fMRI/MEG exposent des événements liés à la timeline et à l'index courant.
* Le module 3D appelle `column.Timeline.OnUpdateCurrentIndex.Invoke()` depuis `Base3DScene`.

**Pourquoi c’est un problème :**
Le namespace `Core.Data` absorbe des concepts de présentation temporelle du module 3D. Cela renforce le mélange entre données chargées, données traitées et état d'affichage.

**Recommandation :**
Identifier quelles classes `Processed` sont des données métier calculées et lesquelles sont de l'état d'affichage. Déplacer l'état d'affichage dans un module `HBP.Module3D.Timeline` ou `HBP.Visualization.Runtime`, et garder seulement les résultats calculés réutilisables dans `Core.Data.Processed`.

**Risque de la correction :**
Moyen si les types sont sérialisés; faible si ce sont seulement des classes runtime non persistées.

## 4. Dépendances suspectes ou à surveiller

* Source : `HBP.Data.Runtime`
  Cible : `HBP.Theme.Runtime`
  Pourquoi elle est suspecte : `Data` devrait idéalement rester indépendant du thème; la dépendance vient notamment de `Camera3D.cs` qui manipule `Theme.State` et `ThemeElement`.
  Alternative recommandée : déplacer `Camera3D` dans un module 3D/presentation, ou injecter une interface d'application de thème côté UI/Module3D.

* Source : `HBP.Data.Runtime`
  Cible : `Unity.Postprocessing.Runtime`
  Pourquoi elle est suspecte : la dépendance post-processing concerne la caméra et le rendu, pas les données.
  Alternative recommandée : isoler `Camera3D` et `View3D` dans `HBP.Module3D.Runtime`.

* Source : `HBP.Core.Runtime`
  Cible : `Unity.TextMeshPro`
  Pourquoi elle est suspecte : le coeur métier ne devrait pas dépendre d'un package texte UI. L'usage observé passe par `ProtocolFilterCondition.cs`.
  Alternative recommandée : déplacer les helpers UI/TMP vers `HBP.UI.Runtime` ou remplacer par une représentation domaine simple.

* Source : `HBP.Core.Runtime`
  Cible : `UnityEngine.UIElements`
  Pourquoi elle est suspecte : `DataInfo.cs` importe UIElements dans `Core.Data`.
  Alternative recommandée : vérifier l'usage réel; supprimer si inutile ou déplacer la logique UI vers une classe de présentation.

* Source : `HBP.Core.Data.SpecificSiteLocationFilterCondition`
  Cible : `HBP.Data.Module3D.Module3DMain` via `SceneLocationEvaluator`
  Pourquoi elle est suspecte : dépendance inversée implicite par static delegate; le filtre métier dépend de la scène courante sans dépendance asmdef visible.
  Alternative recommandée : passer un contexte d'évaluation explicite ou un service `ISiteLocationEvaluator`.

* Source : `HBP.UI.Main.Database.Localizer.ExportLocalizerAtlasWindow`
  Cible : `HBP.Data.Module3D.Module3DMain`
  Pourquoi elle est suspecte : une fenêtre d'export localizer ferme des scènes 3D (`RemoveAllScenes`) pour éviter des conflits.
  Alternative recommandée : un service d'export qui déclare ses préconditions et un contrôleur UI qui demande la fermeture des scènes.

* Source : `HBP.UI.Main.Database.BIDS.ExportBIDSWindow`
  Cible : `DatabaseManager`, `PersistentDataManager`, `BIDSUtility`
  Pourquoi elle est suspecte : la fenêtre combine sélection UI, lecture base, tags, validation et export.
  Alternative recommandée : `BIDSExportService` avec DTO d'entrée et résultat.

* Source : `HBP.Core.Data.Project`
  Cible : filesystem, `Ionic.Zip`, `ApplicationState`, `PersistentDataManager`
  Pourquoi elle est suspecte : le modèle de projet porte le format d'archive et les préférences globales.
  Alternative recommandée : `ProjectArchiveService` / `ProjectSerializer`.

* Source : `HBP.Core.Data.Patient`
  Cible : `DatabaseReference`, `BIDSParser`, `PersistentDataManager.Tags`
  Pourquoi elle est suspecte : l'entité patient connaît les formats de bases externes et modifie les tags globaux.
  Alternative recommandée : services d'import `PatientImporter`.

* Source : `HBP.Core.Data.DataManager`
  Cible : état global statique + wrappers données/DLL
  Pourquoi elle est suspecte : cache, calcul et normalisation sont non injectables et partagés globalement.
  Alternative recommandée : instance `IDataCache` + services de calcul.

## 5. Fichiers potentiellement mal placés

| Fichier actuel | Emplacement actuel | Problème supposé | Emplacement recommandé | Justification |
| -------------- | ------------------ | ---------------- | ---------------------- | ------------- |
| `Base3DScene.cs` | `Assets/Scripts/HBP/Data/Module3D` | Scène Unity 3D complète dans un module nommé `Data` | `Assets/Scripts/HBP/Module3D/Runtime/Base3DScene.cs` | Hérite de `MonoBehaviour`, manipule prefabs, événements Unity, générateurs et scène |
| `Column3D.cs` | `Assets/Scripts/HBP/Data/Module3D` | Composant Unity/rendu dans `Data` | `Assets/Scripts/HBP/Module3D/Runtime/Columns/Column3D.cs` | Hérite de `MonoBehaviour`, instancie des `GameObject`, gère vues et sites |
| `Camera3D.cs` | `Assets/Scripts/HBP/Data/Module3D` | Caméra, post-processing et thème dans `Data` | `Assets/Scripts/HBP/Module3D/Runtime/Camera/Camera3D.cs` | Utilise `PostProcessLayer`, `Theme.State`, `ThemeElement` |
| `DisplayedObjects.cs` | `Assets/Scripts/HBP/Data/Module3D/Modules` | Factory/registry de GameObjects dans `Data` | `Assets/Scripts/HBP/Module3D/Runtime/SceneObjects/DisplayedObjects.cs` | Instancie brain/site/cut/ROI prefabs |
| `CurveData.cs` | `Assets/Scripts/HBP/Data/Informations/Graph` | `ScriptableObject` et UIExtensions dans `Data` | `Assets/Scripts/HBP/UI/Informations/Graph/Data/CurveData.cs` ou module graph dédié | Donnée directement liée au rendu de graphe Unity |
| `ShapedCurveData.cs` | `Assets/Scripts/HBP/Data/Informations/Graph` | Dépendance UIExtensions dans `Data` | `Assets/Scripts/HBP/UI/Informations/Graph/Data/ShapedCurveData.cs` | Extension de données de courbe UI |
| `ChannelStruct.cs` | `Assets/Scripts/HBP/Data/Informations/TrialMatrix` | Namespace `HBP.Data.Informations` moins précis que le dossier | `Assets/Scripts/HBP/Data/Informations/TrialMatrix/ChannelStruct.cs` avec namespace `HBP.Data.Informations.TrialMatrix` | Les autres fichiers du dossier utilisent `TrialMatrix` |
| `Timeline.cs` | `Assets/Scripts/HBP/Core/Data/Loaded/Processed` | Commentaire `FIXME` indique un lien fort avec l'affichage 3D | `Assets/Scripts/HBP/Module3D/Runtime/Timeline/Timeline.cs` ou `Assets/Scripts/HBP/Visualization/Runtime/Timeline.cs` | Etat temporel surtout utilisé par colonnes 3D |
| `ExportBIDSWindow.cs` | `Assets/Scripts/HBP/UI/Main/Database/BIDS` | Namespace `HBP.UI.Main` et logique export dans UI | Garder la fenêtre ici, mais déplacer logique vers `Assets/Scripts/HBP/BIDS/Runtime/BIDSExportService.cs`; namespace fenêtre `HBP.UI.Database.BIDS` | La fenêtre devrait coordonner l'UI, pas porter le workflow |
| `ExportLocalizerAtlasWindow.cs` | `Assets/Scripts/HBP/UI/Main/Database/Localizer` | Export localizer + générateur DLL dans UI | Garder la fenêtre ici, déplacer export vers `Assets/Scripts/HBP/Export/Runtime/LocalizerAtlasExportService.cs`; namespace fenêtre `HBP.UI.Database.Localizer` | La logique d'export doit être testable hors UI |
| `Project.cs` | `Assets/Scripts/HBP/Core/Data/Project` | Modèle + zip + filesystem + préférences globales | Garder modèle, extraire `ProjectArchiveService` dans `Assets/Scripts/HBP/Core/Persistence/ProjectArchiveService.cs` | Réduit la responsabilité du modèle |
| `Patient.cs` | `Assets/Scripts/HBP/Core/Data/Patient` | Modèle + import Intranat/BIDS/database | Garder modèle, extraire importeurs dans `Assets/Scripts/HBP/Core/Import/PatientImportService.cs` ou `Assets/Scripts/HBP/IO/Patients` | Sépare entité et intégrations externes |

## 6. Modules à clarifier ou redécouper

### `HBP.Core`

Rôle actuel apparent :

* Coeur applicatif, modèles, persistance, base de données, préférences, outils Unity, wrappers DLL.

Problème :

* Trop large. Mélange domaine, Unity, sérialisation, filesystem, base de données, UI events et objets 3D.

Découpage recommandé :

* `HBP.Domain.Runtime` : modèles et règles sans Unity.
* `HBP.Persistence.Runtime` : JSON/XML/zip/project archive.
* `HBP.Database.Runtime` : base locale et workspaces.
* `HBP.Native.Runtime` : wrappers DLL.
* `HBP.UnityCore.Runtime` : `MonoBehaviour`, `ScriptableObject`, `UnityEvent`, matériaux.

Bénéfice attendu :

* Tests plus rapides, dépendances lisibles, moindre risque lors des changements de format ou de rendu.

### `HBP.Data`

Rôle actuel apparent :

* Données runtime, module 3D Unity, BIDS export, graph/trial matrix.

Problème :

* Le nom ne correspond pas au contenu réel; il attire des dépendances de rendu, UI et thème.

Découpage recommandé :

* `HBP.Module3D.Runtime` pour scène 3D, colonnes, caméras, managers 3D.
* `HBP.BIDS.Runtime` pour import/export/config BIDS.
* `HBP.Informations.Data` pour données graph/trial matrix non-UI.
* Garder `HBP.Data` uniquement si un sous-ensemble de données runtime clair reste.

Bénéfice attendu :

* Les dépendances post-processing/thème/UIExtensions ne contaminent plus les données.

### `HBP.UI.Main`

Rôle actuel apparent :

* Toutes les fenêtres et composants de l'application principale.

Problème :

* Namespace de 263 fichiers, trop plat; workflows métier dans plusieurs fenêtres.

Découpage recommandé :

* Namespaces par domaine UI : `HBP.UI.Main.Patients`, `HBP.UI.Main.Protocols`, `HBP.UI.Main.Visualization`, `HBP.UI.Database.BIDS`, `HBP.UI.Database.Localizer`.
* Services d'application pour les workflows lourds.

Bénéfice attendu :

* Recherche, revue et dépendances plus lisibles; UI plus mince.

### Module 3D

Rôle actuel apparent :

* Scènes 3D, mesh/MRI/sites, colonnes, cuts, activité, caméra, timeline.

Problème :

* Concentré dans `Data/Module3D` et fortement couplé à `Base3DScene` et `Module3DMain`.

Découpage recommandé :

* `SceneLifecycle`, `SceneSelection`, `SceneCuts`, `SceneLoading`, `SceneActivity`, `Columns`, `Camera`, `DisplayedObjects`.
* Interfaces pour les générateurs natifs.

Bénéfice attendu :

* Changements localisés, meilleure testabilité des calculs hors scène Unity.

### Import/export BIDS et localizer

Rôle actuel apparent :

* Import patient BIDS dans `Patient`, export BIDS dans `Data.BIDS`, orchestration UI dans `ExportBIDSWindow`, export localizer dans `ExportLocalizerAtlasWindow`.

Problème :

* Workflows dispersés et difficilement testables.

Découpage recommandé :

* `HBP.IO.BIDS` ou `HBP.BIDS.Runtime`.
* `HBP.Export.Localizer`.
* DTO de requêtes/résultats, services sans UI.

Bénéfice attendu :

* Tests de format, réutilisation batch/CLI, évolution plus sûre des formats.

## 7. Recommandations prioritaires

### Priorité 1 — À corriger en premier

* Créer une cible de tests edit mode et commencer par des tests de caractérisation sur `BIDSParser`, `BIDSUtility`, `ClassLoaderSaver`, `Project` save/load minimal, `DataManager` scénarios simples.
* Extraire la logique d'export de `ExportBIDSWindow.cs` et `ExportLocalizerAtlasWindow.cs` vers des services d'application.
* Découper `Base3DScene` par extraction interne de services autour des cuts, colonnes, chargement et activité.
* Encapsuler l'accès aux générateurs DLL utilisés par `Base3DScene` et `ExportLocalizerAtlasWindow`.
* Stabiliser l'état global le plus critique en introduisant des interfaces autour de `DatabaseManager`, `PersistentDataManager`, `ApplicationState`.

### Priorité 2 — Important mais non bloquant

* Clarifier l'assembly `HBP.Data.Runtime` en créant `HBP.Module3D.Runtime`.
* Réduire les dépendances Unity/UI dans `HBP.Core.Runtime`, notamment `Unity.TextMeshPro`, `UnityEngine.UIElements`, `MonoBehaviour` et `ScriptableObject`.
* Regrouper import/export BIDS dans un module dédié.
* Harmoniser les namespaces de `UI/Main/Database` et `Data/Informations/TrialMatrix`.
* Remplacer `SpecificSiteLocationFilterCondition.SceneLocationEvaluator` par un contexte ou service explicite.

### Priorité 3 — Nettoyage et amélioration progressive

* Retirer du suivi Git les fichiers `UserSettings` si l'équipe confirme qu'ils ne sont pas volontairement partagés.
* Vérifier si `HiBoP.slnx` doit rester versionné.
* Renommer progressivement les namespaces trop plats (`HBP.UI.Main`, `HBP.Core.Data`) par domaine.
* Clarifier `Core/Data/Loaded/Processed/Timeline.cs` et les classes signalées par le `FIXME`.
* Documenter les frontières attendues des modules dans un court `Docs/architecture.md`.

## 8. Plan de refactor progressif

### Étape 1 — Ajouter un filet de sécurité de tests

Objectif :

* Créer des tests edit mode pour les parties non-Unity les plus critiques.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/Core/Tools/ClassLoaderSaver.cs`
* `Assets/Scripts/HBP/Core/Tools/BIDSParser.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`
* `Assets/Scripts/HBP/Core/Data/DataManager.cs`

Bénéfice :

* Permet de refactorer sans dépendre uniquement de tests manuels Unity.

Risque :

* Faible à moyen. Certaines dépendances globales devront être contournées ou injectées.

Ordre recommandé :

* En premier.

### Étape 2 — Extraire les workflows d'export hors UI

Objectif :

* Rendre BIDS/localizer testables et réduire la responsabilité des fenêtres.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/UI/Main/Database/BIDS/ExportBIDSWindow.cs`
* `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`
* `Assets/Scripts/HBP/Data/BIDS/BIDSUtility.cs`

Bénéfice :

* Les fenêtres deviennent des adaptateurs UI; les exports peuvent être testés et réutilisés.

Risque :

* Moyen. Les workflows utilisent `DatabaseManager`, `PersistentDataManager`, `Module3DMain` et des DLL.

Ordre recommandé :

* Après les premiers tests, avant les grands déplacements de namespaces.

### Étape 3 — Créer une assembly explicite pour le module 3D

Objectif :

* Faire correspondre les dépendances réelles à une frontière nommée.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/Data/Module3D/**/*.cs`
* `Assets/Scripts/HBP/Data/HBP.Data.Runtime.asmdef`
* futur `HBP.Module3D.Runtime.asmdef`

Bénéfice :

* Rend explicite que ces fichiers sont de l'intégration Unity 3D, pas de simples données.

Risque :

* Moyen à élevé selon les références Unity/prefabs. Commencer par créer l'assembly sans déplacer tous les fichiers si possible.

Ordre recommandé :

* Après extraction de services d'export ou en parallèle si l'équipe veut d'abord clarifier les asmdef.

### Étape 4 — Découper `Base3DScene`

Objectif :

* Réduire la classe god object tout en gardant les prefabs/scènes stables.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs`
* nouveaux contrôleurs/services dans le même module 3D.

Bénéfice :

* Modifications localisées, meilleure lecture, tests ciblés possibles.

Risque :

* Élevé si comportement changé. Limiter les premières extractions aux blocs sans changement fonctionnel.

Ordre recommandé :

* Après tests et clarification du module 3D.

### Étape 5 — Extraire persistance/import des modèles `Project` et `Patient`

Objectif :

* Séparer les entités métier des formats externes et du filesystem.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
* `Assets/Scripts/HBP/Core/Data/Patient/BaseMesh.cs`
* `Assets/Scripts/HBP/Core/Data/Dataset/Container/DataContainer.cs`

Bénéfice :

* Modèles plus simples, services testables, formats évolutifs.

Risque :

* Moyen à élevé à cause de la compatibilité des fichiers `.hibop`, `.patient` et JSON typés.

Ordre recommandé :

* Après avoir verrouillé les tests de sérialisation et import.

### Étape 6 — Réduire l'état global statique

Objectif :

* Rendre les services et règles métier évaluables avec un contexte explicite.

Fichiers/modules concernés :

* `ApplicationState.cs`
* `PersistentDataManager.cs`
* `DatabaseManager.cs`
* `DataManager.cs`
* `Module3DMain.cs`
* `SpecificSiteLocationFilterCondition.cs`

Bénéfice :

* Tests indépendants, moins de bugs d'ordre d'initialisation, dépendances visibles.

Risque :

* Élevé si suppression directe. Faible à moyen si ajout d'interfaces en façade.

Ordre recommandé :

* Progressif, au fil des extractions de services.

### Étape 7 — Harmoniser namespaces et dépôt

Objectif :

* Aligner dossiers, namespaces et fichiers suivis.

Fichiers/modules concernés :

* `Assets/Scripts/HBP/UI/Main/**/*.cs`
* `Assets/Scripts/HBP/UI/Main/Database/**/*.cs`
* `Assets/Scripts/HBP/Data/Informations/TrialMatrix/ChannelStruct.cs`
* `Assets/Scripts/HBP/Core/Data/Loaded/Processed/Timeline.cs`
* `.gitignore`
* `UserSettings/*`
* `HiBoP.slnx`

Bénéfice :

* Navigation plus simple, moins de bruit Git, frontières plus lisibles.

Risque :

* Moyen pour namespaces sérialisés; faible pour nettoyage Git si validé par l'équipe.

Ordre recommandé :

* Après les refactors fonctionnels prioritaires, sauf nettoyage Git qui peut être fait séparément.
