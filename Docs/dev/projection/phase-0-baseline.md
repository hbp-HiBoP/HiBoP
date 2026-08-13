# Phase 0 — Baseline, contrats et mesures

## 1. Statut

**Statut :** implémentée et validée le 13 août 2026

**Portée :** tests, inventaire et mesures uniquement ; aucun comportement produit modifié

Cette phase fige le comportement historique qui doit être préservé ou modifié
explicitement pendant la migration vers `ActivityProjectionGrid`.

## 2. Inventaire des consommateurs

### 2.1 ABI `hbp_core`

L'ABI publique déclare actuellement :

- `hbp_generator_surface_create` ;
- `hbp_generator_surface_destroy` ;
- `hbp_generator_surface_initialize` ;
- `hbp_generator_surface_set_volume_interpolation` ;
- `hbp_activity_generator_initialize`, qui reçoit un `hbp_GeneratorSurface`.

Les handles et implémentations se trouvent dans :

- `C:\HBP\Software\hbp_core\include\hbp_core.h` ;
- `C:\HBP\Software\hbp_core\src\api\native_objects.h` ;
- `C:\HBP\Software\hbp_core\src\api\generator_api.cpp` ;
- `C:\HBP\Software\hbp_core\src\generators\generator_surface.*`.

Les consommateurs natifs internes directs sont :

- `ActivityGenerator`, donc `DensityGenerator`, `IEEGGenerator`,
  `FMRIGenerator` et `MEGGenerator` ;
- `SurfaceGenerator` ;
- `CutGeometryGenerator` et `CutGenerator` ;
- l'export activité/masque NIfTI.

### 2.2 HiBoP

Les consommateurs produit directs sont :

- `Assets/Scripts/HBP/Core/DLL/Generators/GeneratorSurface.cs` ;
- `Assets/Scripts/HBP/Core/DLL/Generators/ActivityGenerator.cs` ;
- `Assets/Scripts/HBP/Core/DLL/Generators/SurfaceGenerator.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs` ;
- `Assets/Scripts/HBP/UI/Main/Database/Localizer/ExportLocalizerAtlasWindow.cs`.

Les principaux consommateurs de test sont les tests fonctionnels, de parité,
de migration et de performance de `HBP.Serialization.Tests`.

### 2.3 Autres dépôts locaux

La recherche ciblée dans `C:\HBP\Software` donne :

| Dépôt | Résultat | Conséquence |
| --- | --- | --- |
| `hbp_suite` | possède sa propre classe C++ historique `hbp::GeneratorSurface` et les exports `create_GeneratorSurface` | ce code n'appelle pas l'ABI `hbp_core`, mais constitue un oracle legacy et un consommateur conceptuel à suivre |
| `HiBoP_HoloLens` | possède les wrappers C# historiques et utilise `hbp_export`, avec une précision 300 | migration séparée ; ne pas supprimer l'ancien concept sans décision explicite |
| `API_HiBoP` | aucun usage trouvé | aucun travail identifié |
| `Localizer` | aucun usage trouvé | aucun travail identifié |
| `MovieBrainer` | aucun usage trouvé | aucun travail identifié |
| `HiBoP_Utilities` | aucun usage trouvé | aucun travail identifié |

Cette recherche ne prouve pas l'absence de consommateurs binaires ou de dépôts
non présents localement. La phase 1 doit donc ajouter une ABI parallèle ; elle
ne doit pas retirer les symboles `hbp_generator_surface_*`.

## 3. Couverture automatisée

### 3.1 Tests existants conservés comme oracles

| Domaine | Oracle principal |
| --- | --- |
| iEEG dynamique et statique | `ActivityGeneratorFunctionalTests.DensityAndIeegGenerators_ApplyEveryDistanceModeThroughVolumeProjection` et tests iEEG de parité |
| CCEP Site | même chemin natif `IEEGGenerator` que l'iEEG |
| CCEP MarsAtlas | `ActivityGeneratorFunctionalTests.IeegGenerator_ComputeActivityAtlasCoversTimelinesMasksAndRepeatedCalls` |
| anatomie/densité | tests `DensityGenerator` fonctionnels et de parité |
| fMRI de colonne | `VolumeActivityGenerator_CoversMultiVolumeEveryHideModeAndRepeatedCalls(Fmri)` |
| MEG | `VolumeActivityGenerator_CoversMultiVolumeEveryHideModeAndRepeatedCalls(Meg)` |
| surface | tests `SurfaceGenerator`, UV principales et activité |
| coupes | `VolumeInterpolation_DrivesBothSurfaceAndCutSampling`, composition et tests visuels de parité |
| export | tests de lisibilité et parité activité/masque NIfTI |
| cycle de scène CCEP | tests PlayMode de création de colonnes et de repli MarsAtlas vers Site |

Les overlays directs de `FMRIManager` sont couverts par leurs tests de
non-régression existants, mais ne sont pas intégrés à la migration volumique.

Les références visuelles automatisées reposent sur les pixels de coupe et les
UV surface quantifiés par `NativeParityCutVisualTests` et
`NativeParityWorkflowArtifactTests`. Ces oracles sont préférés à une capture
d'écran dépendante de la caméra pour la phase native ; les captures complètes de
scène restent régies par `Docs/dev/rendering/05-validation-and-reference-captures.md`.

### 3.2 Nouveaux oracles de phase 0

Le fichier
`Assets/Tests/EditMode/HBP.Serialization.Tests/ActivityProjectionPhase0BaselineTests.cs`
ajoute quatre tests `ActivityProjection.Phase0` :

1. `GeneratorSurface_LegacyStorageIncludesSurfaceAndGridPoints` fige le nombre
   de points, les tailles de buffers, la timeline, le cache spatial et la
   normalisation de densité.
2. `SurfaceProjection_LegacyDisjointAndPartialCoverageBehaviorIsCaptured`
   démontre qu'une surface disjointe reçoit une activité neutre mais une
   anatomie bornée aux voxels de bord, et qu'une surface partielle ne projette
   que ses sommets valides.
3. `NiftiExport_LegacyDimensionsAffineTimelineAndPayloadAreCaptured` fige les
   dimensions, l'affine, le temps, les valeurs et le masque d'un export simple.
4. `NiftiExport_LegacyObliqueAnisotropicRowScalingIsCaptured` fige le
   comportement actuel d'un volume oblique anisotrope et démontre le décalage
   des extrémités exportées.

Les deux incohérences ainsi capturées — anatomie bornée pour une surface
disjointe et affine NIfTI reconstruite — sont des baselines historiques, pas le
contrat cible. Les tests seront modifiés volontairement dans les phases 3 et 4.

## 4. Jeux de données spatiaux

Les fixtures sont générées en mémoire ou dans un répertoire temporaire par les
tests ; aucun NIfTI volumineux supplémentaire n'est versionné.

### 4.1 Identité

- dimensions source : `5 × 5 × 5` ;
- spacing : `1 × 1 × 1 mm` ;
- sform identité ;
- valeurs strictement positives croissantes de 1 à 125 ;
- grille demandée : dimension maximale 8, soit `8 × 8 × 8`.

L'affine historique exportée utilise un pas de `5 / 8 = 0,625 mm`. Le dernier
centre annoncé est donc `(4,375 ; 4,375 ; 4,375)` au lieu du dernier centre
source `(4 ; 4 ; 4)`.

### 4.2 Oblique anisotrope

- dimensions source : `3 × 5 × 9` ;
- spacing : `2 × 3 × 4 mm` ;
- translation : `(10 ; -20 ; 30)` ;
- axes X/Y tournés de 90 degrés ;
- sform :

```text
[ 0 -3  0  10 ]
[ 2  0  0 -20 ]
[ 0  0  4  30 ]
[ 0  0  0   1 ]
```

Avec une dimension maximale 8, la grille historique vaut `2 × 4 × 8`. Le
dernier centre source est `(-2 ; -16 ; 62)`, tandis que l'affine exportée
annonce `(-3,5 ; -17,5 ; 61,5)`.

### 4.3 Surfaces incompatibles

- surface disjointe : tétraèdre déplacé d'au moins dix diagonales de bounding
  box hors du volume ;
- surface partielle : deux sommets proches du centre du volume et deux sommets
  au même déplacement extérieur ;
- interpolation : trilinéaire.

Ces fixtures seront réutilisées en phase 3 pour valider le rapport de couverture
et le déclenchement différé des messages.

## 5. Benchmark reproductible

### 5.1 Environnement de référence

| Propriété | Valeur |
| --- | --- |
| Date | 13 août 2026 |
| Machine | `DESKTOP-3BV1CNJ` |
| CPU | Intel Core i7-12700K, 20 processeurs logiques |
| Mémoire physique | 32 485 MB rapportés par Unity |
| OS | Windows 11 `10.0.26200` |
| Unity | `6000.5.2f1` |
| Interpolation | trilinéaire |
| Surface | `MNI_single_hight_Bhemi.obj` |
| Volume | `MNI.nii`, `208 × 256 × 219`, spacing `0,72 mm` |

Le benchmark utilise
`HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run`. Unity doit être
fermé, et la commande doit être exécutée hors sandbox conformément aux règles
de licence du projet.

### 5.2 Commande produit

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1-x86_64\Editor\Unity.exe"
$project = "C:\HBP\Software\HiBoP"
$results = Join-Path $project ".test-results\projection-phase0"

$arguments = @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", $project,
  "-executeMethod", "HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run",
  "-hbpProjectionOutput", (Join-Path $results "product-reference.json"),
  "-hbpProjectionProfile", "Product",
  "-hbpProjectionTimeline", "100",
  "-hbpProjectionRepetitions", "3",
  "-hbpProjectionWorkers", "0",
  "-hbpProjectionBatchSites", "0",
  "-hbpProjectionVolumeInterpolation", "Trilinear",
  "-hbpProjectionFilter", "projection.product-reference.d80",
  "-forgetProjectPath",
  "-logFile", (Join-Path $results "product-reference.log")
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
exit $process.ExitCode
```

### 5.3 Résultats produit

Charge : dimension 80, 30 000 sites, 100 instants, rayon 15, une colonne.

| Mesure | Baseline |
| --- | ---: |
| Sommets de surface | 69 104 |
| Points de grille | 353 600 (`65 × 80 × 68`) |
| Points générés totaux | 422 704 |
| Part des sommets de surface | 16,35 % |
| Valeurs stockées | 42 270 400 |
| Poids stockés | 422 704 |
| Valeurs + poids estimés | 170 772 416 octets |
| Pic de mémoire privée | 201 330 688 octets |
| Mémoire privée retenue après libération | 6 393 856 octets |
| Initialisation `GeneratorSurface`, médiane | 3,53 ms |
| Calcul total, médiane | 2 687,60 ms |
| Calcul total, P95 | 3 053,31 ms |
| Projection surface, médiane | 6,19 ms |
| Préparation coupe, médiane | 2,35 ms |
| Mise à jour coupe, médiane par instant | 1,38 ms |
| Checksum reproductible sur 3 répétitions | `DD77E36640A7FB8C` |

Décomposition native médiane : allocation 29,48 ms, index spatial 5,93 ms,
requêtes de voisinage 239,76 ms, accumulation 2 398,98 ms et normalisation
6,77 ms.

La phase 1 devra comparer ses résultats à cette référence. Le retrait des
69 104 sommets de surface doit notamment faire passer le nombre de points de
422 704 à 353 600 pour ce scénario, sans modifier involontairement le champ
volumique validé.

### 5.4 Baseline courte avec export

Le profil Smoke a été exécuté avec trois répétitions, dimension 24, 1 000 sites,
100 instants et export activé :

| Mesure | Baseline |
| --- | ---: |
| Points générés | 78 224 |
| Valeurs stockées | 7 822 400 |
| Initialisation grille, médiane | 0,29 ms |
| Calcul, médiane | 26,57 ms |
| Projection surface, médiane | 3,56 ms |
| Export NIfTI, médiane | 4,95 ms |
| Taille du fichier activité | 3 648 352 octets |

Les rapports bruts sont générés localement dans
`.test-results/projection-phase0/`. Ils contiennent les échantillons individuels,
les compteurs mémoire, les phases natives et les checksums.

## 6. Validation et gate de sortie

Validation effectuée :

- compilation Unity EditMode réussie ;
- 26 tests ciblés réussis sans skip : 4 nouveaux oracles, générateurs
  fonctionnels, parité activité/NIfTI et artefacts surface/coupes ;
- benchmark produit réussi sur 3 répétitions avec checksum stable ;
- benchmark Smoke avec export réussi sur 3 répétitions ;
- aucune modification de code produit ou de `hbp_core` ;
- inventaire ABI et consommateurs locaux consigné.

La gate de phase 0 est satisfaite. La phase suivante est la phase 1 : ajout de
`ActivityProjectionGrid` en parallèle de l'API existante dans `hbp_core`.
