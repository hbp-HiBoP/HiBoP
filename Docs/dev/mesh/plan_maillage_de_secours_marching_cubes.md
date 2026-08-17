# Plan d'implémentation d'un maillage de secours généré depuis une IRM

Statut : plan d'architecture ; incréments 0 à 5 implémentés le 31 juillet 2026.

Date de l'étude : 28 juillet 2026.

## 1. Décision proposée

Lorsqu'une visualisation mono-patient ne dispose d'aucun maillage patient
utilisable, HiBoP doit pouvoir générer automatiquement une isosurface
approximative pour chaque IRM anatomique associée au patient.

Ces surfaces :

- sont générées en mémoire par `hbp_core` ;
- sont exposées à HiBoP comme des `Mesh3D` mono-surface utilisables par le pipeline
  existant ;
- restent associées à leur IRM source ; celle correspondant à l'IRM configurée est
  sélectionnée par défaut pour la scène ;
- ne sont pas ajoutées à `Patient.Meshes` ;
- ne sont ni sérialisées comme des `SingleMesh`, ni enregistrées comme GIfTI ;
- ne sont jamais présentées comme des reconstructions corticales validées ;
- sont libérées avec la scène, sauf si un cache mémoire explicite en devient
  propriétaire ;
- ne supportent ni MarsAtlas, ni les fonctions dépendant d'hémisphères ou de
  labels de surface pré-calculés.

Le moteur d'extraction recommandé est Marching Cubes 33, via MC33++ 5.4, intégré
aux sources de `hbp_core` et compilé statiquement dans la bibliothèque. Il
retournera directement un `hbp_Surface`. Aucun tableau complet de voxels,
vertices ou triangles ne transitera de `hbp_core` vers C# avant la construction
normale du `UnityEngine.Mesh`.

Cette solution est un mécanisme de continuité fonctionnelle. Elle ne remplace
pas FreeSurfer, FastSurfer, BrainVISA ou un autre pipeline de segmentation et de
reconstruction anatomique.

## 2. Objectifs et non-objectifs

### 2.1 Objectifs fonctionnels

Le maillage de secours doit permettre, dans la mesure où les données partagent
le même repère spatial :

- l'affichage d'une enveloppe 3D patient ;
- le contrôle de caméra, la transparence et le rendu filaire ;
- les coupes et la suppression interactive de triangles ;
- l'affichage des électrodes et des sites ;
- la projection d'activité iEEG, CCEP et anatomique fondée sur la distance ;
- l'utilisation du générateur de surface et du générateur volumique ;
- l'échantillonnage d'une IRM fonctionnelle sur les sommets, quand cette IRM
  est alignée sur l'IRM source ;
- la création et l'affichage des ROI qui ne supposent pas d'atlas ;
- le remplacement immédiat du maillage de secours par un véritable maillage
  si celui-ci devient disponible dans la scène.

### 2.2 Non-objectifs

La première version ne doit pas :

- produire un fichier `.gii`, `.obj` ou tout autre fichier de maillage ;
- modifier les données persistantes du patient ;
- effectuer une segmentation cérébrale anatomique ;
- reconstruire la matière blanche, la matière grise, les sillons ou les
  surfaces piales ;
- séparer les hémisphères ;
- générer des labels MarsAtlas ;
- promettre une correspondance anatomique avec un mesh FreeSurfer ;
- générer un maillage pour chaque patient d'une visualisation multi-patients,
  puisque ces scènes utilisent par conception le référentiel MNI commun ;
- masquer silencieusement l'échec de chargement d'un GIfTI déclaré. En version
  initiale, la génération automatique est déclenchée quand aucun mesh n'est
  défini/utilisable, pas lorsqu'un mesh attendu est corrompu.

## 3. État actuel du code

### 3.1 Côté `hbp_core`

Les briques nécessaires existent déjà :

- `C:\HBP\Software\hbp_core\src\volume\volume.h` contient les dimensions, les
  extrema, les transformations voxel-vers-monde et l'accès aux voxels ;
- `C:\HBP\Software\hbp_core\src\volume\nifti_reader.cpp` charge le NIfTI,
  applique le scaling NIfTI et construit un `Volume` en `float` ;
- `C:\HBP\Software\hbp_core\src\surface\surface.h` représente une surface
  native et sait recevoir vertices et triangles, calculer les normales,
  simplifier et effectuer les découpes ;
- `C:\HBP\Software\hbp_core\src\api\native_objects.h` associe les handles
  opaques `hbp_Volume` et `hbp_Surface` aux objets C++ ;
- `C:\HBP\Software\hbp_core\include\hbp_core.h` expose l'ABI C consommée par
  Unity ;
- `C:\HBP\Software\hbp_core\src\generators\generator_surface.cpp` initialise
  les générateurs à partir d'une `Surface` et d'un `Volume`, sans supposer que
  la surface provient d'un GIfTI ;
- `C:\HBP\Software\hbp_core\src\generators\ieeg_generator.cpp` projette
  l'activité en fonction de distances euclidiennes. Il ne dépend ni de la
  topologie FreeSurfer ni de distances géodésiques.

La simplification native repose déjà sur la dépendance MIT
`Fast Quadric Mesh Simplification`. Elle pourra être réutilisée après
l'extraction.

### 3.2 Côté HiBoP

Les points d'intégration actuels sont :

- `Assets/Scripts/HBP/Core/DLL/Volume.cs` pour le wrapper de `hbp_Volume` ;
- `Assets/Scripts/HBP/Core/DLL/Surface.cs` pour le wrapper de `hbp_Surface` ;
- `Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs` pour `Mesh3D`,
  `SingleMesh3D` et `LeftRightMesh3D` ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs` pour la liste, la
  sélection et la surface active ;
- `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs` pour l'ordre de chargement,
  l'initialisation des générateurs et le nettoyage ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/AtlasManager.cs` pour les calculs
  de labels de surface ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/FMRIManager.cs` pour
  l'échantillonnage fonctionnel sur les sommets.

`Base3DScene.InitializeAsync` charge actuellement :

1. les objets MNI ;
2. les meshes patient ;
3. les IRM patient ;
4. les sites ;
5. les colonnes.

Les meshes MNI sont donc toujours présents avant les données patient. Un
simple test `MeshManager.Meshes.Count == 0` serait incorrect. Il faut tester
explicitement l'absence de mesh de type `MeshType.Patient`, ou mieux l'absence
de mesh patient persistant utilisable.

`SingleMesh3D` ne possède actuellement qu'un constructeur basé sur
`Data.SingleMesh` et sa méthode `Load()` suppose qu'un fichier GIfTI existe.
À l'inverse, `LeftRightMesh3D` possède déjà un précédent de construction depuis
des `DLL.Surface` en mémoire.

Enfin, `Surface(IntPtr)` ne marque pas actuellement l'objet comme chargé et
`Surface.SetBuffers(...)` ne met pas `IsLoaded` à `true`. Ce contrat doit être
corrigé ou contourné par une factory explicite avant qu'une surface native
retournée par l'extracteur puisse être considérée comme chargée.

## 4. Architecture cible

```text
Patient sans mesh persistant
          |
          v
Pour chaque IRM anatomique du patient
          |
          v
MRI3D.Volume (hbp_Volume déjà chargé)
          |
          v
hbp_volume_extract_preview_surface(...)
          |
          +-- réduction de résolution
          +-- seuil automatique ou explicite
          +-- masque principal et nettoyage
          +-- MC33
          +-- affine NIfTI
          +-- nettoyage topologique
          +-- normales
          |
          v
hbp_Surface possédée par le caller
          |
          v
RuntimeSingleMesh3D
  - Both = surface complète
  - SimplifiedBoth = surface simplifiée
  - Type = Patient
  - Origin = GeneratedFromMRI
  - Lifetime = SceneOwned
          |
          v
MeshManager.AddRuntime(...)
          |
          v
Pipeline HiBoP existant
  affichage / coupes / activité / ROI / fMRI alignée
```

La frontière native/managée doit rester étroite : une seule fonction ABI
transforme un `hbp_Volume` en `hbp_Surface`. La surface suit ensuite exactement
le cycle de vie des autres surfaces natives.

## 5. Choix de la bibliothèque Marching Cubes

### 5.1 Comparaison

| Solution | Licence | Dépendances | Intégration statique | Décision |
|---|---|---|---|---|
| MC33++ 5.4 | MIT | Bibliothèque standard C++ | Oui | Recommandée |
| MC33 C 5.5 | MIT | Bibliothèque standard C | Oui | Alternative si l'adaptation C++ pose problème |
| MarchingCubeCpp | Public domain/MIT | Header-only | Oui | Alternative minimale, mais Marching Cubes classique |
| libigl | MPL-2.0, plus Eigen | Eigen, modèle header-only | Possible | Non retenue : dépendance et obligations supplémentaires |
| VTK/Flying Edges | BSD-3-Clause | Ensemble VTK important | Techniquement possible | Non retenue : disproportionnée et difficile à distribuer |
| scikit-image/Lewiner | BSD-3-Clause | Python, NumPy, Cython | Non adaptée | Non retenue pour un runtime Unity natif |

### 5.2 Pourquoi MC33++

MC33 traite les cas ambigus du Marching Cubes classique et vise une topologie
cohérente, notamment par un test intérieur correct. La version C++ 5.4 :

- est distribuée sous licence MIT ;
- expose des vertices partagés et des triangles indexés ;
- accepte une grille `float` externe ;
- est compilable avec MSVC, GCC et Clang ;
- peut être construite comme bibliothèque statique ;
- n'a besoin ni d'OpenGL, ni de FLTK, ni de GLUT pour son noyau ;
- permet de réduire la résolution d'une grille ;
- reste suffisamment petite pour être auditée et vendored.

Sources :

- dépôt officiel : <https://github.com/dvega68/MC33_cpp_library> ;
- publication : <https://jcgt.org/published/0008/03/01/> ;
- page officielle des versions : <https://facyt-quimicomp.neocities.org/MC33_libraries>.

### 5.3 Contraintes de licence et de distribution

Avant intégration, il faudra :

1. choisir un commit précis de MC33++ 5.4 et enregistrer son SHA dans
   `third_party/components.json` ou dans un fichier `UPSTREAM.md` ;
2. copier uniquement les sources requises, le README utile et
   `LICENSE.txt` dans `hbp_core/third_party/mc33` ;
3. conserver le copyright et le texte MIT complets ;
4. déclarer le composant dans
   `C:\HBP\Software\hbp_core\third_party\components.json` avec
   `"license": "MIT"` et `"linkage": "static"` ;
5. régénérer `docs/third_party/THIRD_PARTY_NOTICES.md` et
   `docs/third_party/hbp_core.spdx.json` avec
   `tools/New-HbpCoreThirdPartyDocumentation.ps1` ;
6. faire vérifier le résultat par le processus juridique habituel. Le présent
   document est une analyse technique, pas un avis juridique.

Les exemples FLTK/GLUT, les projets Visual Studio upstream, les fonctions de
dessin OpenGL et les binaires précompilés ne doivent pas être importés.

### 5.4 Liaison statique

`hbp_core` reste le seul artefact natif chargé dynamiquement par Unity :

- Windows : `hbp_core.dll` ;
- Linux : `libhbp_core.so` ;
- macOS : bundle contenant `libhbp_core`.

MC33 doit être une cible interne CMake `STATIC`, liée en `PRIVATE` à
`hbp_core`. Aucun `MC33.dll`, `.so` ou `.dylib` ne doit être distribué.

Configuration CMake cible :

```cmake
add_library(hbp_core_mc33 STATIC
    third_party/mc33/source/libMC33++.cpp)

target_include_directories(hbp_core_mc33
    SYSTEM PUBLIC
        "${CMAKE_CURRENT_SOURCE_DIR}/third_party/mc33/include")

target_compile_features(hbp_core_mc33 PRIVATE cxx_std_17)
target_compile_definitions(hbp_core_mc33
    PRIVATE
        MC33_DOUBLE_PRECISION=0
        USE_MM_RSQRT_SS=0)

set_target_properties(hbp_core_mc33 PROPERTIES
    POSITION_INDEPENDENT_CODE ON)

target_link_libraries(hbp_core PRIVATE hbp_core_mc33)
```

Les définitions exactes devront être confirmées sur le commit vendored.
`USE_MM_RSQRT_SS=0` est obligatoire par défaut : l'implémentation SSE x86 ne
convient pas au job macOS arm64 de `hbp_core`. Une optimisation SIMD pourra être
réintroduite ultérieurement derrière des branches d'architecture testées, sans
modifier les résultats de référence.

Le commit `252dabd7ea6dea3d0529e66b047adbe878caf2fa` ne compile pas sous MSVC
avec `GRD_ORTHOGONAL` : certains lecteurs upstream référencent encore le membre
`nonortho` que cette macro retire. Les sources vendored restent intactes et la
macro n'est donc pas définie. L'extracteur fournit néanmoins une grille mémoire
orthogonale par `grid3d::set_data_pointer`. L'affine NIfTI complète est appliquée
aux vertices après l'extraction ; MC33 ne représente directement ni rotation,
ni cisaillement, ni réflexion du volume.

## 6. Développement natif dans `hbp_core`

### 6.1 Nouveaux fichiers proposés

Créer :

```text
src/meshing/preview_surface_extractor.h
src/meshing/preview_surface_extractor.cpp
src/meshing/mesh_component_filter.h
src/meshing/mesh_component_filter.cpp
src/api/preview_surface_api.cpp
src/core/error_state.h
src/core/error_state.cpp
tests/native/hbp_core_preview_surface_functional_test.cpp
tests/native/hbp_core_preview_surface_performance_test.cpp
```

Responsabilités :

- `preview_surface_extractor.*` : validation, histogramme, réduction de
  résolution, construction du masque, appel MC33, affine et rapport ;
- `mesh_component_filter.*` : composantes connexes, faces dégénérées,
  compaction des indices et contrôle de fermeture ;
- `preview_surface_api.cpp` : validation de l'ABI, conversion des options,
  gestion des erreurs et transfert de propriété du `hbp_Surface` ;
- `error_state.*` : si un message natif détaillé est retenu, déplacer dans ce
  helper interne l'état thread-local actuellement privé de
  `src/core/hbp_core.cpp`, afin que la nouvelle API puisse alimenter
  `hbp_core_last_error()` sans ajouter de symbole public ;
- tests fonctionnels : exactitude géométrique et contrats d'erreur ;
- test de performance : temps, mémoire et taille du résultat.

Éviter de placer la logique dans `surface.cpp` ou `volume.cpp`. L'extraction est
une opération de meshing qui dépend simultanément des deux concepts et mérite
un module isolé.

### 6.2 Modèle C++ interne

Types internes proposés :

```cpp
namespace hbp::core {

enum class PreviewThresholdMode {
    AutoOtsu = 0,
    Absolute = 1,
    Normalized = 2
};

struct PreviewSurfaceOptions {
    PreviewThresholdMode threshold_mode = PreviewThresholdMode::AutoOtsu;
    float threshold = 0.0f;
    int maximum_grid_dimension = 160;
    int target_triangle_count = 20000;
    int binary_closing_iterations = 1;
    int scalar_smoothing_iterations = 1;
    bool keep_largest_component = true;
    bool fill_internal_cavities = true;
    bool pad_with_background = true;
};

struct PreviewSurfaceReport {
    float applied_threshold = 0.0f;
    int input_x = 0;
    int input_y = 0;
    int input_z = 0;
    int sampled_x = 0;
    int sampled_y = 0;
    int sampled_z = 0;
    int foreground_voxel_count = 0;
    int component_count = 0;
    int vertex_count_before_simplification = 0;
    int triangle_count_before_simplification = 0;
    int vertex_count_after_simplification = 0;
    int triangle_count_after_simplification = 0;
    double preprocessing_milliseconds = 0.0;
    double extraction_milliseconds = 0.0;
    double postprocessing_milliseconds = 0.0;
};

class PreviewSurfaceExtractor {
public:
    static bool extract(
        const Volume& volume,
        const PreviewSurfaceOptions& options,
        Surface& output,
        PreviewSurfaceReport* report);
};

}
```

Le rapport sert aux logs, tests et futurs diagnostics UI. Il ne doit contenir
aucune donnée patient ni aucun chemin de fichier.

### 6.3 API C publique

Ajouter des types à `include/hbp_core_status.h` :

```c
typedef enum hbp_PreviewThresholdMode {
    HBP_PREVIEW_THRESHOLD_AUTO_OTSU = 0,
    HBP_PREVIEW_THRESHOLD_ABSOLUTE = 1,
    HBP_PREVIEW_THRESHOLD_NORMALIZED = 2
} hbp_PreviewThresholdMode;

typedef struct hbp_PreviewSurfaceOptions {
    uint32_t struct_size;
    int threshold_mode;
    float threshold;
    int maximum_grid_dimension;
    int target_triangle_count;
    int binary_closing_iterations;
    int scalar_smoothing_iterations;
    int keep_largest_component;
    int fill_internal_cavities;
    int pad_with_background;
} hbp_PreviewSurfaceOptions;

typedef struct hbp_PreviewSurfaceReport {
    uint32_t struct_size;
    float applied_threshold;
    int input_x;
    int input_y;
    int input_z;
    int sampled_x;
    int sampled_y;
    int sampled_z;
    int foreground_voxel_count;
    int component_count;
    int vertex_count_before_simplification;
    int triangle_count_before_simplification;
    int vertex_count_after_simplification;
    int triangle_count_after_simplification;
    double preprocessing_milliseconds;
    double extraction_milliseconds;
    double postprocessing_milliseconds;
} hbp_PreviewSurfaceReport;
```

Ajouter une seule fonction à `include/hbp_core.h` :

```c
HBP_CORE_EXTERN_C HBP_CORE_API HBP_Status HBP_CORE_CALL
hbp_volume_extract_preview_surface(
    const hbp_Volume* volume,
    const hbp_PreviewSurfaceOptions* options,
    hbp_Surface** out_surface,
    hbp_PreviewSurfaceReport* out_report);
```

Règles de l'ABI :

- `out_surface` est mis à `NULL` avant tout traitement ;
- le caller devient propriétaire du handle uniquement en cas de `HBP_OK` ;
- le handle est détruit par `hbp_surface_destroy` ;
- `options == NULL` signifie utiliser tous les paramètres par défaut ;
- `struct_size` permet d'ajouter des champs en fin de structure dans une
  version future ;
- si `out_report != NULL`, sa taille est vérifiée avant écriture ;
- les options invalides retournent `HBP_INVALID_ARGUMENT` ;
- un volume vide retourne `HBP_INVALID_HANDLE` ou `HBP_ERROR` selon la
  convention retenue, à figer par test ;
- une surface vide, un volume constant ou un seuil sans intersection
  retournent `HBP_ERROR` avec un message exploitable dans
  `hbp_core_last_error()` ;
- aucune exception C++ ne doit traverser l'ABI C. L'adaptateur doit capturer
  `std::bad_alloc`, `std::exception` et les erreurs inconnues, nettoyer les
  allocations et retourner `HBP_ERROR`.

Dans l'état actuel, la fonction interne qui renseigne la dernière erreur est
dans un namespace anonyme de `src/core/hbp_core.cpp`. Pour respecter le contrat
ci-dessus, la déplacer dans `src/core/error_state.*`, sans changer l'ABI, puis
l'utiliser aussi depuis `preview_surface_api.cpp`. À défaut de ce petit
refactor, le wrapper managed ne doit pas afficher un `LastError` potentiellement
obsolète et doit se limiter au statut natif.

Cette fonction porte la baseline ABI de 209 à 210 symboles. Après validation,
mettre à jour :

```text
C:\HBP\Software\hbp_core\baseline\hbp_core_abi_exports.txt
```

avec `tools/Test-HbpCoreAbi.ps1 -UpdateBaseline`, puis revalider le header et
les trois binaires de CI. Une montée de version mineure de `hbp_core` est
recommandée, car l'ABI est étendue sans casser les symboles existants.

### 6.4 Pipeline d'extraction exact

L'algorithme recommandé suit cet ordre.

#### Étape A — Validation

1. Vérifier que le volume contient au moins `2 × 2 × 2` voxels.
2. Vérifier que les dimensions ne débordent pas les calculs `size_t`.
3. Vérifier qu'il existe assez de valeurs finies et au moins deux intensités
   distinctes.
4. Valider `maximum_grid_dimension` dans `[32, 256]`.
5. Valider `target_triangle_count` dans `[1000, 200000]`.
6. Valider les nombres d'itérations dans une petite plage, par exemple `[0, 4]`.
7. Travailler sur les données 3D déjà contenues dans `Volume`. Le contrat
   HiBoP garantit que les entrées de `Patient.MRIs` sont anatomiques ; chaque
   entrée est donc une source valide pour son propre preview.

#### Étape B — Estimation robuste des intensités

1. Parcourir les voxels sans créer de copie complète.
2. Ignorer `NaN` et les infinis.
3. Construire un histogramme de 512 bins entre des bornes robustes. Les
   percentiles 0,5 et 99,5 peuvent être estimés par un premier histogramme ou
   par échantillonnage déterministe.
4. En mode `AutoOtsu`, calculer le seuil d'Otsu sur cet histogramme.
5. En mode `Absolute`, utiliser directement la valeur donnée.
6. En mode `Normalized`, convertir `[0, 1]` dans les bornes robustes.
7. Enregistrer le seuil finalement appliqué dans le rapport.

Otsu ne réalise pas un skull stripping. Il donne un seuil de premier niveau
pour séparer le fond d'une partie des tissus. Le résultat restera une enveloppe
approximative, souvent plus proche de la tête que d'une surface piale.

#### Étape C — Réduction de résolution

La grille doit être réduite avant les opérations morphologiques et MC33.

1. Calculer un facteur isotrope de façon que la plus grande dimension soit au
   plus `maximum_grid_dimension`.
2. Conserver les deux extrémités de chaque axe :

   ```text
   sampledSize = max(2, round((inputSize - 1) * scale) + 1)
   sourceStep  = (inputSize - 1) / (sampledSize - 1)
   ```

3. Rééchantillonner les intensités par interpolation trilinéaire dans l'espace
   IJK.
4. Conserver pour chaque axe `sourceStep`, nécessaire pour reconvertir les
   sommets MC33 en coordonnées voxel d'origine.

Ce calcul évite de tronquer la dernière tranche et reste valable avec un
espacement anisotrope. L'affine sera appliquée plus tard ; les dimensions
physiques ne sont donc pas perdues.

Profils initiaux proposés :

| Profil | Dimension maximale | Triangles cibles | Usage |
|---|---:|---:|---|
| Rapide | 128 | 15 000 | Chargement automatique |
| Équilibré | 160 | 20 000 | Valeur par défaut recommandée |
| Détaillé | 192 | 30 000 | Régénération manuelle |

Commencer avec `Équilibré`, puis choisir le profil automatique définitif après
benchmark sur les IRM réelles.

#### Étape D — Construction d'une enveloppe

1. Classer un voxel comme premier plan lorsque son intensité est supérieure ou
   égale au seuil.
2. Conserver la plus grande composante 26-connexe du masque. Utiliser le nombre
   de voxels ou le volume physique, pas seulement le nombre de futurs
   triangles.
3. Appliquer au maximum une fermeture binaire 3D de rayon un voxel pour
   supprimer les petites discontinuités.
4. Si `fill_internal_cavities` est actif :
   - ajouter virtuellement une bordure de fond ;
   - lancer un flood-fill 6-connexe depuis l'extérieur ;
   - convertir les cavités non atteintes en premier plan.
5. Rejeter un masque occupant moins de 0,5 % ou plus de 90 % de la grille,
   seuils initiaux à confirmer sur les fixtures réelles.
6. Ajouter une bordure d'un voxel de fond. Elle garantit que les objets
   touchant les bords du NIfTI peuvent produire une surface fermée.
7. Convertir le masque en champ scalaire `0/1`.
8. Appliquer zéro ou une passe de lissage scalaire séparable
   `[1, 2, 1] / 4` sur les trois axes. Ne pas lisser directement les vertices
   en première version, afin d'éviter un retrait géométrique non contrôlé.

Le mode explicite doit réutiliser le même pipeline ; seul le choix du seuil
change.

#### Étape E — MC33

1. Donner à MC33 la grille `float` réduite.
2. Utiliser l'isovaleur `0.5` sur le masque lissé.
3. Ne jamais activer les fonctions OpenGL ou les lecteurs de fichiers MC33.
4. Copier les vertices et triangles de la surface MC33 vers des
   `std::vector<Vec3>` et `std::vector<int>`.
5. Vérifier chaque indice avant de construire le `Surface`.
6. Rejeter un résultat vide ou dépassant les limites `int` de l'ABI et de
   Unity.

MC33 accepte un pointeur de données externes, mais son API upstream le déclare
non-const. Deux solutions sont acceptables :

- construire la grille réduite dans un `std::vector<float>` détenu par
  l'extracteur et lui transmettre ce buffer, puisqu'il est de toute façon
  mutable et local ;
- maintenir un patch vendored minimal rendant l'entrée const si l'audit du
  code confirme qu'elle n'est jamais modifiée.

La première solution est recommandée pour limiter les modifications upstream.

#### Étape F — Coordonnées NIfTI

Les sommets MC33 sont d'abord exprimés dans la grille réduite avec bordure.
Pour chaque sommet :

1. retirer l'offset de la bordure ;
2. multiplier chaque coordonnée par le `sourceStep` correspondant ;
3. obtenir une coordonnée fractionnaire dans l'espace IJK original ;
4. appliquer `Volume::to_xyz().apply_point(...)`.

Le résultat est stocké dans le repère natif droitier de `hbp_core`.

Ne pas convertir en coordonnées Unity dans l'extracteur. La conversion
`R = diag(-1, 1, 1)` et l'inversion du winding restent assurées une seule fois
par `hbp_surface_copy_unity_mesh`, conformément à
`C:\HBP\Software\hbp_core\docs\coordinate_system_contract.md`.

Si le déterminant de la partie linéaire de l'affine NIfTI est négatif,
l'application de l'affine inverse le winding. L'extracteur doit :

1. calculer le signe du déterminant ;
2. inverser les triangles après transformation si nécessaire ;
3. calculer ensuite les normales ;
4. confirmer l'orientation extérieure en comparant l'intensité ou le masque
   légèrement de part et d'autre de quelques faces ;
5. inverser toute la surface si la majorité des normales pointe vers le
   premier plan.

Ce comportement doit être couvert par une fixture affine avec déterminant
négatif.

#### Étape G — Nettoyage et simplification

1. Supprimer les faces avec indices répétés.
2. Supprimer les triangles d'aire quasi nulle. L'epsilon doit être relatif au
   plus petit espacement physique, et non une constante arbitraire en mm².
3. Supprimer une seconde fois les petites composantes éventuellement créées
   par le lissage ou MC33.
4. Compacter les vertices non référencés et remapper les indices.
5. Calculer les normales sur la surface complète.
6. Si le résultat dépasse la cible, appeler la simplification existante
   `Surface::simplify(target_triangle_count, 7)`.
7. Nettoyer de nouveau les triangles dégénérés après simplification.
8. Recalculer les normales de la surface simplifiée.

La surface complète peut rester plus détaillée que la cible si les performances
le permettent, tandis que `SimplifiedBoth` doit rester autour de 10 000 à
20 000 triangles. Une autre option est de faire de la sortie native la surface
déjà limitée à 20 000–30 000 triangles et de produire `SimplifiedBoth` à
10 000 triangles côté `RuntimeSingleMesh3D`. Ce choix doit être décidé après
mesure du coût des projections et du rendu.

## 7. Wrapper C# et propriété des handles

### 7.1 `Volume.cs`

Ajouter dans `Assets/Scripts/HBP/Core/DLL/Volume.cs` :

- les structures `[StructLayout(LayoutKind.Sequential)]` correspondant aux
  options et au rapport ;
- un enum managed strictement aligné sur `hbp_PreviewThresholdMode` ;
- le `DllImport` de `hbp_volume_extract_preview_surface` ;
- une méthode :

```csharp
public Surface ExtractPreviewSurface(
    PreviewSurfaceOptions options,
    out PreviewSurfaceReport report)
```

La méthode :

1. vérifie `IsLoaded` ;
2. initialise `structSize` avec `Marshal.SizeOf<T>()` ;
3. appelle le natif ;
4. en cas d'échec, détruit tout handle éventuellement retourné et lève une
   exception contenant le statut et `HbpCoreRuntime.LastError` ;
5. en cas de succès, transfère le handle à un `Surface` propriétaire marqué
   comme chargé.

Placer le `DllImport` dans `Volume.cs` évite d'ajouter un nouveau fichier à
l'inventaire des wrappers natifs. Si un nouveau fichier est préféré, mettre à
jour l'assertion exacte de `NativeMigrationBaselineTests`.

### 7.2 `Surface.cs`

Introduire une factory interne explicite, par exemple :

```csharp
internal static Surface FromOwnedLoadedHandle(IntPtr handle)
```

Elle doit :

- rejeter `IntPtr.Zero` ;
- construire un `Surface` propriétaire ;
- définir `IsLoaded = true` ;
- garantir que `Dispose()` appelle exactement une fois
  `hbp_surface_destroy`.

Éviter de rendre public un constructeur ambigu `Surface(IntPtr, bool)`.

Vérifier également les autres surfaces créées depuis des handles natifs
(`Simplify`, coupes, clones). Le plan n'impose pas de refactor global, mais
toutes les nouvelles utilisations doivent avoir un état `IsLoaded` correct.

Si `SetBuffers(...)` reste utilisé ailleurs pour créer une surface complète en
mémoire, il devrait définir `IsLoaded` seulement après succès de tous les
buffers obligatoires. Cette correction est indépendante du chemin MC33, qui
doit préférer le handle direct et éviter les copies managed.

## 8. Modèle de mesh transitoire

### 8.1 Ne pas créer de `Data.SingleMesh`

Le maillage généré ne doit jamais instancier `HBP.Core.Data.SingleMesh`. Cela
évite :

- qu'il apparaisse dans l'éditeur du patient ;
- qu'un chemin GIfTI fictif soit requis ;
- qu'il soit exporté en BIDS ;
- qu'il soit sérialisé dans le projet ;
- que le code tente de le recharger depuis le disque.

### 8.2 Classe proposée

Ajouter une classe spécialisée dans
`Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs` ou dans un fichier voisin :

```csharp
public sealed class RuntimeSingleMesh3D : SingleMesh3D
{
    public MRI3D SourceMRI { get; }
    public string SourceMRIName => SourceMRI.Name;
    public RuntimeMeshOrigin Origin => RuntimeMeshOrigin.GeneratedFromMRI;
    public bool IsTransient => true;
    public bool SupportsMarsAtlas => false;
    public bool SupportsHemispheres => false;
    public PreviewSurfaceReport GenerationReport { get; }

    // Constructeur recevant une surface complète possédée et, si elle a déjà
    // été produite, sa version simplifiée.
}
```

Contrats :

- `Type = MeshType.Patient` pour réutiliser le comportement patient actuel ;
- `Both` et `SimplifiedBoth` sont non null et chargés avant l'ajout au manager ;
- `Load()` est un no-op validé ou lève une exception claire si l'objet a perdu
  ses surfaces ; il ne consulte jamais `m_Mesh` ;
- `Clone()` doit être interdit tant qu'aucun besoin n'existe, ou cloner
  réellement les handles. Il ne doit jamais partager deux handles
  scene-owned sans comptage de références ;
- `Clean()` possède et libère les deux surfaces ;
- `HasBeenLoadedOutside` reste `false`. Ce champ signifie actuellement
  « ressource partagée chargée hors scène », comme MNI. Le mettre à `true`
  provoquerait une fuite lors de `Base3DScene.CleanAsync`.

À moyen terme, remplacer le booléen ambigu `HasBeenLoadedOutside` par un contrat
explicite :

```csharp
public enum NativeObjectLifetime
{
    SceneOwned,
    SharedExternal
}
```

Ce refactor n'est pas requis pour le premier incrément si
`RuntimeSingleMesh3D` est clairement scene-owned.

### 8.3 Identité runtime

Chaque mesh conserve la référence de son objet `MRI3D` source. Le manager
refuse uniquement un second preview pour cette même instance d'IRM. Le texte
affiché reste dérivé du nom de l'IRM :

```text
Display name : MRI preview – <MRI name>
```

La configuration actuelle enregistre uniquement `MeshName`. Deux choix sont
possibles :

1. enregistrer le nom réservé et le régénérer à chaque ouverture ;
2. ne jamais sauvegarder la sélection transitoire et la re-sélectionner selon
   la règle de fallback.

La seconde option est recommandée tant qu'il n'existe pas de `MeshId` séparé.
Elle évite qu'une configuration persistante référence un objet qui peut ne pas
être généré lors d'un prochain chargement.

## 9. Intégration dans `MeshManager`

Ajouter une méthode dédiée :

```csharp
public void AddRuntime(RuntimeSingleMesh3D mesh)
```

Elle doit :

- valider que le mesh est chargé ;
- refuser un doublon pour la même `MRI3D` source ;
- l'ajouter uniquement à `MeshManager.Meshes` ;
- ne jamais toucher à `Patient.Meshes` ni `PreloadedMeshes` ;
- conserver `MeshPartToDisplay = MeshPart.Both` ;
- permettre sa sélection immédiate ;
- notifier la toolbar pour que les capacités soient recalculées.

Ajouter des propriétés explicites :

```csharp
public bool HasPersistentPatientMesh { get; }
public List<RuntimeSingleMesh3D> RuntimePreviewMeshes { get; }
```

Ne pas déduire l'absence de mesh patient de l'index ou du nombre total de
meshes, car les trois surfaces MNI sont toujours chargées en premier.

`SelectMeshPart` doit forcer `Both` ou refuser `Left`/`Right` lorsque le mesh
sélectionné ne possède pas d'hémisphères.

## 10. Ensemble des IRM sources

La génération automatique est limitée aux visualisations mono-patient.

Toutes les entrées du champ `Patient.MRIs` sont considérées comme anatomiques.
Chaque `MRI3D` patient utilisable doit donc produire son propre preview. Les
objets partagés chargés hors patient, en particulier MNI, sont exclus. Aucune
heuristique basée sur `Preimplantation`, `CT`, `fMRI`, `BOLD` ou le nom du
volume n'est appliquée.

## 11. Modification de la séquence de chargement

Dans `Base3DScene.InitializeAsync`, ajouter après le chargement des IRM patient
et avant le chargement des sites :

```text
EnsureRuntimePatientMeshAsync pour chaque IRM patient
```

Pseudo-flux :

```csharp
if (Type == SceneType.SinglePatient
    && !MeshManager.HasPersistentPatientMesh)
{
    foreach (MRI3D source in MRIManager.PatientMRIs)
    {
        // Accéder à source.Volume sur le thread de travail afin que son
        // éventuel chargement paresseux ne bloque pas le thread Unity.
        RuntimeSingleMesh3D preview =
            await GenerateRuntimePatientMeshAsync(source, token);

        if (preview != null)
        {
            MeshManager.AddRuntime(preview);
        }
    }
}
```

La génération native est synchrone, mais elle doit être appelée après
`UniTask.SwitchToThreadPool()`. Seules les opérations Unity
(`MeshManager` si nécessaire, événements, `UnityEngine.Mesh`, UI) reviennent sur
le thread principal.

La première version n'a pas besoin d'une annulation native complexe :

- vérifier le `CancellationToken` avant l'appel ;
- exécuter MC33 sur un worker ;
- vérifier à nouveau le token au retour ;
- si l'opération a été annulée, disposer immédiatement la surface obtenue et
  ne pas l'ajouter à la scène.

Cette stratégie n'arrête pas MC33 en cours de calcul, mais n'immobilise pas le
thread Unity et reste sûre si le temps d'extraction respecte le budget. Un job
natif annulable ne sera justifié que si les benchmarks dépassent ce budget.

### 11.1 Sélection finale

`ResetConfiguration()` sélectionne actuellement le mesh préféré avec
`onlyIfAlreadyLoaded = true`, puis retombe sur l'index zéro, donc MNI. Il faut
modifier la politique de sélection :

1. si la configuration référence un véritable mesh chargé, le
   respecter ;
2. sinon, si un mesh patient persistant est disponible, appliquer la préférence
   actuelle ;
3. sinon, sélectionner le runtime preview associé à
   `Visualization.Configuration.MRIName` ou à la préférence d'IRM ;
4. si ce preview n'a pas pu être généré, sélectionner le premier runtime
   preview disponible ;
5. sinon, utiliser le mesh MNI actuel.

Cette décision doit être centralisée dans
`MeshManager.SelectInitialMeshForScene(...)`, plutôt que répétée dans
`Base3DScene`.

### 11.2 Progression de chargement

Ajouter un poids spécifique par IRM, par exemple
`LOADING_PREVIEW_MESH_WEIGHT`, utilisé uniquement lorsque le fallback est
nécessaire.

Messages suggérés :

```text
Preparing MRI preview
Generating MRI preview <MRI name> [<index>/<count>]
Optimizing MRI preview
```

Les messages ne doivent pas employer « segmentation cérébrale ».

## 12. Compatibilité des fonctionnalités

| Fonction | État cible | Traitement |
|---|---|---|
| Affichage du cerveau | Activé | Pipeline `BrainSurface` normal |
| Caméra et bounds | Activé | Bounds de la surface générée |
| Transparence et edges | Activé | Aucun changement |
| Coupes | Activé | Utilise les fonctions génériques de `Surface` |
| Triangle eraser | Activé | Masque de visibilité standard |
| Sites et électrodes | Activé | Exige le même repère spatial |
| Projection iEEG/CCEP | Activé | Distance euclidienne aux sommets |
| Projection anatomique/densité | Activé | Même réserve sur la distance |
| Export d'activité NIfTI | Activé | Réutilise le volume et le générateur |
| fMRI patient alignée | Activé avec avertissement | Échantillonnage aux sommets |
| MarsAtlas de surface | Désactivé | Aucun label associé |
| JuBrain/MNI spatial | Désactivé | Mesh patient non enregistré dans MNI |
| IBC, DiFuMo, localizers MNI | Désactivé | Politique existante pour `MeshType.Patient` |
| Parties Left/Right | Désactivé | Seulement `MeshPart.Both` |
| Couleurs de parcelles GIfTI | Désactivé | Aucun fichier de parcelles |

### 12.1 Atlas

`AtlasManager.UpdateAtlasIndices()` calcule actuellement JuBrain et MarsAtlas
pour toute `BrainSurface`, même lorsqu'aucun atlas n'est affiché. Modifier cette
méthode pour :

- ne calculer MarsAtlas que si le mesh sélectionné le supporte et si
  MarsAtlas est effectivement demandé ;
- ne calculer JuBrain que si le mesh est dans le référentiel compatible et si
  JuBrain est demandé ;
- remettre les caches d'indices à `null` lors d'un changement vers un mesh
  incompatible ;
- faire en sorte que la toolbar et les raccourcis refusent proprement
  l'activation.

Le comportement existant de `MeshManager.Select`, qui désactive les atlas et
les données MNI pour les meshes `Patient`, fournit déjà une partie de cette
protection. Il faut néanmoins supprimer les calculs inutiles et auditer les
modes CCEP basés sur MarsAtlas afin qu'une configuration précédemment
sauvegardée ne réactive pas indirectement un calcul incompatible.

### 12.2 Projection d'activité

La projection est mathématiquement compatible, mais sa pertinence dépend de la
distance entre la surface extraite et les électrodes.

La valeur par défaut de l'influence est actuellement de 15 mm. Une enveloppe
proche du scalp peut être située à plus de 15 mm de nombreux contacts
intracrâniens ; ces sommets n'afficheront alors aucune activité.

Ne pas modifier silencieusement la distance enregistrée par l'utilisateur.
Ajouter plutôt un diagnostic après chargement des sites :

1. calculer, pour chaque site, la distance au vertex ou triangle le plus
   proche du runtime preview ;
2. calculer les percentiles 50, 90 et 95 ;
3. si une proportion importante des sites dépasse la distance d'influence
   active, afficher un avertissement ;
4. proposer une action non destructive « adapter la distance pour cette
   scène », avec une valeur suggérée basée sur le percentile 90 plus une petite
   marge ;
5. ne persister cette valeur que si l'utilisateur l'accepte explicitement.

Pour une première version minimale, le diagnostic peut seulement être logué et
testé. Il ne doit pas bloquer la projection.

### 12.3 fMRI et autres volumes

`FMRIManager.UpdateSurfaceFMRIValues()` peut échantillonner une valeur à chaque
sommet d'une surface arbitraire. Cette fonction reste activable pour une
fMRI patient si :

- son affine la place dans le même espace que l'IRM ayant généré le mesh ;
- les sommets se trouvent dans son volume ;
- elle n'est pas une ressource MNI supposant un mesh MNI.

Si la surface représente la tête plutôt que le cortex, de nombreux sommets
peuvent recevoir zéro ou une valeur hors tissu. L'interface doit conserver la
mention « aperçu approximatif » et ne pas présenter ce rendu comme une mesure
de surface corticale.

## 13. UX et contrôles

### 13.1 Chargement automatique

La génération doit être automatique uniquement lorsque :

- la scène est mono-patient ;
- aucun mesh patient persistant utilisable n'est défini ;
- au moins une IRM patient utilisable existe ;
- la préférence globale « Générer un aperçu IRM en l'absence de mesh » est
  activée. Elle peut être activée par défaut après validation des performances.

En cas de succès, afficher un badge ou une information persistante :

```text
Aperçu IRM approximatif — aucune reconstruction corticale n'est chargée.
```

### 13.2 Régénération manuelle

Dans une deuxième étape, ajouter un outil de scène :

```text
Régénérer l'aperçu IRM
```

Paramètres :

- IRM source ;
- profil Rapide / Équilibré / Détaillé ;
- seuil Automatique ;
- seuil manuel avec slider et valeur numérique ;
- restauration des valeurs automatiques.

La régénération doit :

1. générer une nouvelle surface sans supprimer l'ancienne ;
2. valider le nouveau résultat ;
3. remplacer atomiquement le mesh dans `MeshManager` ;
4. reconstruire les générateurs et colliders ;
5. disposer l'ancien mesh seulement après succès ;
6. conserver l'ancien mesh si la génération échoue.

Aucun bouton « Sauvegarder en GIfTI » ne doit être ajouté dans ce périmètre.

## 14. Cache et cycle de vie

### 14.1 Première version

Générer une surface par scène et la libérer dans `Base3DScene.CleanAsync`.

Avantages :

- propriété simple ;
- aucun cache global à invalider ;
- aucun risque de partage d'un handle modifiable entre scènes ;
- pas de fichier temporaire.

### 14.2 Cache mémoire optionnel

Si plusieurs scènes du même patient sont ouvertes simultanément et que le coût
est significatif, ajouter ultérieurement un cache mémoire avec :

```text
clé =
  MRI stable ID
  + identité/version du fichier
  + taille
  + dernière modification
  + paramètres d'extraction
  + version de hbp_core/MC33
```

Le cache doit stocker une surface native immutable de référence. Chaque scène
reçoit soit un clone qu'elle possède, soit un lease avec comptage de références.
Ne jamais partager directement une surface dont le triangle eraser modifie le
masque de visibilité.

Il n'est pas prévu de cache disque : cela recréerait un format persistant
implicite, des problèmes d'invalidation et des données dérivées à gérer.

## 15. Concurrence, mémoire et performance

### 15.1 Threading

- lecture et extraction natives sur un worker ;
- aucune API Unity depuis ce worker ;
- construction et affectation du `UnityEngine.Mesh` sur le thread principal ;
- une génération au maximum par scène ;
- si MC33 ou le code vendored utilise un état global mutable, sérialiser les
  appels ou adapter le code avant d'autoriser plusieurs générations en
  parallèle ;
- exécuter un test natif de deux extractions simultanées sur des volumes
  distincts avant de déclarer le composant thread-safe.

### 15.2 Budget mémoire

Pour une grille `160³` :

- champ `float` : environ 16 MiB ;
- masque binaire stocké en `uint8_t` : environ 4 MiB ;
- buffers de composantes/flood-fill : dépend du type d'index, viser moins de
  32 MiB supplémentaires ;
- sortie MC33 : variable, limitée ensuite par la simplification.

Objectif provisoire :

- pic natif additionnel inférieur à 256 MiB pour une IRM clinique standard ;
- aucune allocation proportionnelle au volume complet en C# ;
- mémoire retenue après destruction de la scène inférieure au bruit mesuré de
  la baseline existante ;
- aucune augmentation du nombre de dépendances dynamiques du paquet.

### 15.3 Budget temps

Budgets provisoires à ratifier sur la machine de benchmark HiBoP :

- génération Équilibrée médiane inférieure à 2 secondes pour une IRM
  `256 × 256 × 256` ;
- percentile 95 inférieur à 5 secondes sur le corpus de validation ;
- blocage cumulé du thread Unity inférieur à 100 ms hors chargement normal du
  `UnityEngine.Mesh` ;
- recalcul des générateurs comparable à celui d'un GIfTI de 20 000 triangles ;
- le temps de génération doit apparaître séparément dans les métriques de
  chargement, afin de ne pas être attribué au chargement NIfTI.

Si ces budgets ne sont pas atteints :

1. passer le profil automatique à 128 ;
2. réduire les opérations morphologiques ;
3. profiler les copies MC33 ;
4. envisager une implémentation Flying Edges légère uniquement après preuve
   que MC33 est le goulot ;
5. ne pas introduire VTK uniquement pour gagner ce temps.

## 16. Gestion des erreurs

| Situation | Comportement |
|---|---|
| Aucun mesh, aucune IRM patient | Conserver MNI et afficher une information |
| IRM illisible | Erreur de chargement actuelle, pas de faux preview |
| Volume constant ou vide | Log d'erreur, ignorer ce preview ; conserver les autres ou MNI |
| Seuil ne produisant aucune surface | Réessayer une fois avec le seuil automatique si l'utilisateur avait donné un seuil manuel, sinon conserver MNI |
| Surface trop grande | Simplifier ; si limite de sécurité dépassée, abandonner |
| Surface dégénérée/non fermée | Accepter seulement si les outils ciblés restent sûrs ; sinon abandonner et conserver MNI |
| Annulation de la scène | Disposer le handle retourné, ne plus ajouter de preview |
| Échec de régénération | Conserver l'ancien preview |
| Véritable mesh ajouté | Sélectionner selon l'action utilisateur, puis disposer le preview lorsqu'il n'est plus référencé |

Les échecs sont indépendants pour chaque IRM. Le fallback MNI reste disponible
comme dernier recours lorsqu'aucun preview n'a pu être généré. Un échec de
Marching Cubes ne doit jamais empêcher l'ouverture de la visualisation.

## 17. Plan de réalisation par incréments

### Incrément 0 — Fixtures et mesures de référence

Statut : **réalisé**, avec un périmètre recentré sur la correction du nouvel
extracteur plutôt que sur les performances des fonctionnalités HiBoP déjà
implémentées.

Dans `hbp_core` :

- générateur déterministe d'une sphère binaire isotrope ;
- générateur déterministe d'un fantôme anatomique synthétique avec intensités
  multiples, cavités, composante parasite et affine anisotrope réaliste ;
- variante du fantôme avec déterminant affine négatif ;
- oracles exécutables pour dimensions, intensités, centre, bornes, occupation,
  composantes 26-connexes et remplissage des cavités ;
- contrat géométrique futur pour rayon, orientation, fermeture et winding ;
- limites de sécurité initiales sur la grille, les triangles, les entiers ABI
  et la mémoire ;
- protocole non versionné pour le corpus anatomique réel.

Les fichiers de référence sont dans
`C:\HBP\Software\hbp_core\tests\native\preview_surface_fixtures.*` et
`C:\HBP\Software\hbp_core\docs\preview_surface_fixture_baseline.md`. Le CTest
`hbp_core_preview_surface_fixture_test` matérialise temporairement les NIfTI et
les recharge par l'ABI publique. Aucun code produit, GIfTI ou donnée patient
n'a été ajouté.

Les mesures d'une scène GIfTI, du coût des générateurs et des distances
sites-surface sont retirées de cet incrément. Elles n'influencent pas la
correction de la création du mesh depuis l'IRM ; le diagnostic de distance
reste à l'incrément 5.

Critère de sortie : fixtures et oracles reproductibles, limites de sécurité
documentées et CTest vert, sans code produit.

### Incrément 1 — Dépendance MC33 et primitive native

Statut : **réalisé localement sur Windows x64**. Linux x64 et macOS arm64
seront validés par la matrice GitHub Actions lors du push.

Dans `hbp_core` :

- vendor MC33++ 5.4 au commit approuvé ;
- intégrer la cible statique CMake ;
- mettre à jour licences, notices et SBOM ;
- écrire l'adaptateur `PreviewSurfaceExtractor` sur un volume synthétique ;
- exposer `hbp_volume_extract_preview_surface` ;
- ajouter les tests de géométrie, affine, erreurs et ownership ;
- mettre à jour la baseline ABI.

La primitive de cet incrément extrait directement le champ scalaire dans sa
grille d'origine. Elle applique le seuil explicite, normalisé ou un Otsu
préliminaire sur l'étendue complète, puis l'affine NIfTI et la correction de
winding. Le rééchantillonnage, les bornes robustes, la morphologie, les
composantes, les cavités et la simplification restent à l'incrément 2. Les
champs ABI correspondants sont déjà validés mais ne sont pas encore appliqués.

MC33++ 5.4 est figé au commit
`252dabd7ea6dea3d0529e66b047adbe878caf2fa`. La version de `hbp_core` passe à
0.2.0 et la baseline ABI à 210 symboles.

Critère de sortie local : CTest et inspection des dépendances passent sur
Windows x64, sans nouvelle dépendance dynamique. Les deux autres plateformes
restent un gate CI avant merge.

### Incrément 2 — Prétraitement robuste

Statut : **réalisé localement sur Windows x64**. Linux x64 et macOS arm64
seront validés par la matrice GitHub Actions lors du push. La validation
visuelle a été effectuée dans 3D Slicer sur plusieurs IRM réelles non
versionnées ; les résultats ont été jugés corrects.

Dans `hbp_core` :

- Otsu sur bornes robustes et seuil explicite finalisé ;
- rééchantillonnage ;
- composante principale ;
- fermeture/cavités/bordure ;
- nettoyage des triangles ;
- simplification et rapport ;
- benchmarks.

Le pipeline utilise les percentiles robustes 0,5/99,5, un rééchantillonnage
trilinéaire qui conserve les extrémités, des composantes 26-connexes, une
fermeture binaire, un remplissage 6-connexe des cavités, une bordure de fond et
un lissage scalaire séparable. Il vérifie l'occupation, pré-estime la taille
MC33, remappe l'IJK réduit dans l'affine NIfTI, nettoie les triangles et
simplifie avec un second nettoyage.

Les oracles synthétiques couvrent aussi les affines de déterminant négatif,
les NaN/Inf, le déterminisme exact, les limites d'occupation et le cycle de
propriété. Le benchmark natif dédié au nouvel extracteur traite le fantôme
`160 x 192 x 144`, réduit à `107 x 128 x 96`, en environ 0,30 s sur la machine
Windows de développement et simplifie 62 600 triangles à 20 000. Cette mesure
n'inclut aucune fonctionnalité HiBoP existante et ne constitue pas un budget
multi-plateforme.

L'outil autonome `hbp_core_preview_surface_export` permet de convertir une IRM
locale en OBJ marqué `SPACE=RAS`, d'enregistrer le rapport JSON et de superposer
le résultat au NIfTI dans 3D Slicer sans HiBoP. Le protocole complet est décrit
dans `C:\HBP\Software\hbp_core\docs\preview_surface_visual_validation.md`.

Critère de sortie : résultats stables sur les fixtures synthétiques et le
corpus anatomique manuel ; aucun crash sur les cas limites.

### Incrément 3 — Wrapper et runtime mesh

Statut : **réalisé localement sur Windows x64**. Le chargement automatique
reste volontairement hors de cet incrément et commence à l'incrément 4.

Dans HiBoP :

- wrapper `Volume.ExtractPreviewSurface` ;
- factory de `Surface` chargée et propriétaire ;
- `RuntimeSingleMesh3D` ;
- `MeshManager.AddRuntime` ;
- tests EditMode de propriété, chargement et nettoyage.

Le wrapper managed initialise et vérifie les layouts ABI, conserve le message
d'erreur natif et transfère le handle uniquement à une `Surface` chargée et
propriétaire. `RuntimeSingleMesh3D` possède deux handles distincts pour la
surface complète et sa copie simplifiée, refuse le clone avec partage implicite
et est libéré par le nettoyage normal de la scène. `MeshManager.AddRuntime`
enregistre cette instance uniquement dans la liste runtime, force l'affichage
`Both`, refuse le doublon réservé et ne modifie ni les meshes persistants du
patient ni le cache de préchargement.

Validation locale : les six tests EditMode ciblés passent sous Unity
6000.5.2f1. Ils couvrent le layout managed/native, le refus d'un volume non
chargé, l'extraction depuis une fixture NIfTI, la création d'un
`UnityEngine.Mesh`, le chargement sans GIfTI, le double nettoyage sans double
libération et la séparation persistante/runtime en présence des trois meshes
MNI. Le test d'inventaire `DllImport` passe également avec 246 imports au total,
dont 192 vers `hbp_core`. La DLL Windows installée correspond au paquet natif à
210 symboles ABI.

Critère de sortie : une surface native synthétique peut devenir
`BrainSurface`, créer un `UnityEngine.Mesh` et être libérée sans fuite.

### Incrément 4 — Chargement automatique

Statut : **réalisé localement sur Windows x64**. Les autres plateformes seront
validées par la matrice GitHub Actions lors du push.

Dans HiBoP :

- génération d'un preview distinct pour chaque IRM patient ;
- association directe de chaque preview à sa `MRI3D` source ;
- génération après les IRM et avant les sites ;
- progression et annulation post-appel ;
- sélection initiale du preview ;
- fallback MNI ;
- aucune mutation de `Patient.Meshes` ou de la configuration persistante.

Toutes les IRM patient sont traitées, sans heuristique sur leur nom. Les objets
MNI chargés hors patient ne peuvent pas devenir une source d'extraction.

La génération est lancée sur un thread de travail après le chargement des IRM
et avant celui des sites. L'annulation est vérifiée avant et après l'appel
natif ; une surface produite après annulation est immédiatement libérée et
n'est jamais enregistrée. L'échec d'une IRM est journalisé sans interrompre les
autres extractions ; la scène utilise MNI seulement si aucune ne réussit.

La politique de sélection initiale conserve un mesh chargé explicitement nommé
par la configuration, puis la préférence de mesh patient si elle existe. En
l'absence de mesh patient persistant, elle sélectionne le
`RuntimeSingleMesh3D` lié à l'IRM configurée, puis le premier preview réussi, et
force l'affichage de la surface complète ; MNI reste le dernier recours. Un
preview runtime n'est jamais recopié dans
`Visualization.Configuration.MeshName` lors d'une sauvegarde.

Validation locale sous Unity 6000.5.2f1 :

- 7 tests EditMode ciblés passent pour l'ABI, l'ownership, plusieurs runtime
  meshes et l'inventaire des IRM patient ;
- 5 tests PlayMode ciblés passent pour un preview par IRM, la sélection selon
  l'IRM configurée, la présence d'un mesh persistant, les échecs partiels ou
  complets et la scène multi-patients ;
- 2 tests PlayMode de non-régression passent pour le graphe de scène historique
  et le cycle chargement/sauvegarde de la configuration.

Critère de sortie : une scène mono-patient sans mesh crée un preview par IRM et
s'ouvre sur celui de l'IRM configurée ; les scènes avec mesh et les scènes
multi-patients ne changent pas.

### Incrément 5 — Capacités et régressions fonctionnelles

Statut : **réalisé localement sur Windows x64**. Le runtime mesh expose
maintenant explicitement ses capacités : pas d'hémisphères indépendants, pas de
MarsAtlas et pas de ressources réservées au repère MNI. Le gestionnaire de mesh
applique ces capacités à chaque sélection : affichage forcé sur `Both`, atlas et
fMRI MNI désactivés, et toute colonne CCEP en mode MarsAtlas revient au mode
`Site`. Les mêmes conditions pilotent les contrôles correspondants dans la
toolbar. La génération CCEP MarsAtlas possède en plus une garde défensive avant
l'appel natif.

Après le chargement de la configuration, une scène utilisant un runtime mesh
calcule les distances de chaque site au sommet de surface le plus proche et
produit P50, P90 et P95. Si au moins 25 % des sites dépassent la plus petite
distance d'influence active, un avertissement propose une valeur indicative
arrondie à `P90 + 2 mm`. Ce diagnostic est strictement informatif : il ne
modifie ni les colonnes ni la configuration persistée.

Dans HiBoP :

- désactivation Atlas/hémisphères ;
- audit CCEP MarsAtlas ;
- projections iEEG, CCEP, densité et export NIfTI ;
- coupes, triangle eraser, ROI et fMRI alignée ;
- diagnostic des distances sites-surface.

Validation Windows x64 :

- 9 tests EditMode ciblés passent pour les distances, la densité, l'iEEG avec
  les trois lois de distance, le chemin CCEP MarsAtlas, les UV de
  surface, les activités fMRI/MEG volumiques et l'export/rechargement NIfTI ;
- 4 tests PlayMode ciblés passent pour les garde-fous Atlas/hémisphères/fMRI/
  CCEP, les coupes et projections de surface sur le runtime mesh, le triangle
  eraser et les ROI ;
- les 5 tests PlayMode de l'incrément 4 et les 2 tests de régression de
  configuration passent encore.

Critère de sortie : matrice de compatibilité testée, aucune commande
incompatible proposée silencieusement.

### Incrément 6 — UX de régénération

Optionnel après validation :

- contrôle de seuil ;
- choix du profil ;
- régénération atomique ;
- badge et avertissements ;
- cache mémoire seulement si justifié par les mesures.

## 18. Plan de tests

### 18.1 Tests natifs fonctionnels

Ajouter au minimum :

1. **Sphère binaire**
   - surface non vide ;
   - bbox proche du rayon attendu ;
   - erreur radiale maximale bornée par l'espacement ;
   - normales majoritairement vers l'extérieur.

2. **Ellipsoïde anisotrope**
   - dimensions physiques correctes après affine ;
   - absence d'utilisation erronée du spacing comme direction.

3. **Affine avec translation et rotation**
   - centroïde et bbox dans le repère monde attendu.

4. **Affine de déterminant négatif**
   - winding et normales cohérents après transformation.

5. **Cas ambigus MC33**
   - pas de trous introduits sur les configurations couvertes ;
   - résultat déterministe.

6. **Composantes multiples**
   - seule la plus grande est conservée.

7. **Objet touchant le bord**
   - la bordure virtuelle ferme la surface.

8. **NaN et infinis**
   - ignorés sans crash ;
   - échec propre si aucune valeur finie.

9. **Volume constant**
   - `HBP_ERROR`, handle nul, message explicite.

10. **Seuil hors plage**
    - erreur déterministe.

11. **Triangles**
    - indices dans les bornes ;
    - aucun indice répété par face ;
    - aire non nulle ;
    - chaque arête d'une fixture fermée appartient à deux triangles.

12. **Ownership**
    - création/destruction répétée ;
    - aucun double free en cas d'erreur partielle.

13. **Concurrence**
    - deux volumes extraits simultanément si le composant est déclaré
      thread-safe.

### 18.2 Tests ABI et packaging

- symbole présent dans le header public ;
- symbole présent dans les trois binaires ;
- baseline ABI à 210 symboles ;
- `THIRD_PARTY_NOTICES.md` et SPDX à jour ;
- `dumpbin /dependents`, `otool -L` et `ldd` sans nouvelle dépendance externe ;
- architecture arm64 du bundle macOS ;
- aucune référence à OpenGL, FLTK, GLUT, VTK, Eigen, Python ou OpenCV ;
- paquet final contenant la licence MC33.

### 18.3 Tests EditMode HiBoP

- mapping exact des structures managed/native ;
- wrapper : succès, erreur, handle nul, dispose ;
- `RuntimeSingleMesh3D.IsLoaded == true` ;
- `RuntimeSingleMesh3D.Load()` ne cherche jamais un GIfTI ;
- `Clean()` détruit chaque surface une fois ;
- `MeshManager.AddRuntime()` n'altère pas `Patient.Meshes` ;
- plusieurs previews peuvent coexister, sans doublon pour une même IRM ;
- détection correcte malgré les trois meshes MNI ;
- sélection `Both` forcée ;
- sauvegarde de configuration sans référence invalide ;
- atlas non calculés ;
- inventaire `DllImport` mis à jour si nécessaire ;
- IL2CPP/linker : wrapper et structs conservés.

### 18.4 Tests PlayMode

Scénarios :

1. patient sans mesh avec plusieurs IRM anatomiques ;
2. patient avec véritable mesh et IRM ;
3. patient sans mesh et sans IRM ;
4. extraction native en échec pour toutes les IRM ;
5. extraction en échec pour une IRM mais réussie pour une autre ;
6. fermeture de scène pendant la génération ;
7. deux scènes sans mesh ouvertes successivement ;
8. mesh de secours avec activité iEEG synthétique ;
9. coupe et triangle eraser ;
10. fMRI alignée synthétique ;
11. tentative d'activation MarsAtlas et Left/Right ;
12. sauvegarde/rechargement de visualisation ;
13. passage des previews à un véritable mesh.

### 18.5 Validation visuelle manuelle

Pour chaque IRM de référence :

- vérifier orientation gauche/droite, antérieur/postérieur et supérieur/inférieur ;
- comparer l'enveloppe aux coupes MRI ;
- vérifier que les électrodes sont dans un repère plausible ;
- tester plusieurs seuils ;
- afficher une activité proche et éloignée ;
- vérifier les faces avant/arrière et les ombres ;
- vérifier les découpes fortes et faibles ;
- vérifier qu'aucune fonction d'atlas n'est présentée comme disponible.

## 19. Critères d'acceptation produit

La fonctionnalité est prête à être activée par défaut lorsque :

- une visualisation mono-patient sans mesh mais avec IRM s'ouvre sans action
  manuelle ;
- un preview est généré pour chaque IRM patient et celui associé à l'IRM
  configurée devient la `BrainSurface` active ;
- aucune donnée patient persistante n'est modifiée ;
- aucun fichier dérivé n'est écrit ;
- les fonctions annoncées dans la matrice sont opérationnelles ;
- MarsAtlas et les hémisphères sont indisponibles explicitement ;
- les projections d'activité ne crashent pas et le risque de distance est
  signalé ;
- un échec conserve un MNI utilisable ;
- le thread Unity reste réactif ;
- les budgets de temps et mémoire sont atteints ;
- toutes les surfaces natives sont libérées ;
- Windows x64, Linux x64 et macOS arm64 passent le build, CTest, l'inspection
  des dépendances et les tests Unity applicables ;
- la licence MIT de MC33 figure dans les notices et le SBOM ;
- aucune nouvelle dépendance dynamique tierce n'est livrée.

## 20. Risques principaux et mitigations

| Risque | Impact | Mitigation |
|---|---|---|
| L'isosurface représente le scalp | Activité trop loin de la surface | Avertissement, seuil ajustable, diagnostic de distance |
| Otsu échoue sur une modalité atypique | Surface vide ou aberrante | Contrôles d'occupation, fallback MNI, seuil manuel |
| Affine mal appliquée | Mesh décalé ou miroir | Extraction en IJK, affine unique, tests déterminant négatif |
| Winding inversé | Rendu/coupes incorrects | Déterminant, test intérieur/extérieur, normales recalculées |
| Surface énorme | Mémoire et générateurs lents | Grille bornée, limite de triangles, simplification |
| Surface non fermée | Coupes et inside-test fragiles | Bordure de fond, MC33, contrôle des arêtes |
| Double libération native | Crash | Ownership explicite, factory de handle, tests répétés |
| Fuite par `HasBeenLoadedOutside` | Mémoire retenue | Runtime mesh scene-owned, ne pas détourner ce booléen |
| MarsAtlas activé depuis une ancienne config | Résultat trompeur | Capabilities et garde dans `AtlasManager`/CCEP |
| SSE upstream sur Apple Silicon | Build ou crash macOS | `USE_MM_RSQRT_SS=0`, CI arm64 obligatoire |
| Dérive de la dépendance vendored | Reproductibilité/licence | Commit figé, licence copiée, SBOM et hash |
| Génération trop longue | Mauvaise expérience | Worker thread, profil 128/160, métriques et fallback |

## 21. Décisions à figer avant codage

Les recommandations par défaut sont :

1. **Bibliothèque** : MC33++ 5.4 MIT, commit figé.
2. **Liaison** : cible CMake statique privée dans `hbp_core`.
3. **Plateformes** : Windows x64, Linux x64, macOS arm64 dès le premier
   incrément.
4. **Profil automatique** : grille 160, cible 20 000 triangles, à confirmer
   par benchmark.
5. **Seuil automatique** : Otsu sur plage robuste, puis enveloppe binaire.
6. **Déclenchement** : uniquement mono-patient sans mesh patient persistant.
7. **IRM** : un preview pour chaque entrée de `Patient.MRIs` ; l'IRM
   configurée détermine seulement la sélection initiale.
8. **Persistance** : aucune surface ni entrée patient persistée.
9. **Durée de vie** : propriété de la scène, pas de cache initial.
10. **Atlas** : MarsAtlas et atlas MNI désactivés.
11. **Projection** : activée, sans modifier automatiquement les 15 mm ; ajouter
    un diagnostic de distance.
12. **Échec** : fallback MNI, jamais d'échec d'ouverture de scène.

## 22. Références

- MC33++ officiel et licence MIT :
  <https://github.com/dvega68/MC33_cpp_library>
- Vega, Abache et Coll, *A Fast and Memory-Saving Marching Cubes 33
  implementation with the correct interior test* :
  <https://jcgt.org/published/0008/03/01/>
- Historique et versions MC33 :
  <https://facyt-quimicomp.neocities.org/MC33_libraries>
- Documentation officielle VTK Flying Edges, consultée pour comparaison :
  <https://vtk.org/doc/nightly/html/classvtkFlyingEdges3D.html>
- Licence officielle libigl, consultée pour comparaison :
  <https://libigl.github.io/license/>
- Convention de coordonnées locale :
  `C:\HBP\Software\hbp_core\docs\coordinate_system_contract.md`
- Inventaire des dépendances locales :
  `C:\HBP\Software\hbp_core\third_party\components.json`
- Génération des notices et du SBOM :
  `C:\HBP\Software\hbp_core\tools\New-HbpCoreThirdPartyDocumentation.ps1`
