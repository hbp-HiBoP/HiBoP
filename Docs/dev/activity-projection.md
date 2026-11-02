# Projection d’activité et export NIfTI

Le champ d’activité est volumique. La surface est une cible d’échantillonnage,
indépendante de la géométrie du champ. Le code actuel emploie
`ActivityProjectionGrid`; l’ancien `GeneratorSurface` et son ABI de production
ont été supprimés. Les adaptateurs `LegacyNative` des tests restent des oracles.

## Propriétaires et géométrie

`Base3DScene` possède la grille; celle-ci conserve le volume de référence.
`ActivityGenerator` conserve la grille et possède le champ natif.
`SurfaceGenerator` conserve le générateur et une surface explicite. Lors du
remplacement d’une grille, les consommateurs sont rattachés avant de libérer
l’ancienne. Un objet `ActivityField` managé distinct n’est pas nécessaire.

Pour chaque axe d’un volume dont les dimensions sont toutes au moins deux :

```text
gridAxis = max(2, truncate(maximumDimension * sourceAxis / largestSourceAxis))
stepAxis = (sourceAxis - 1) / (gridAxis - 1)
gridToWorld = sourceVoxelToWorld × diag(stepX, stepY, stepZ, 1)
```

Les centres extrêmes sont conservés, y compris pour une affine oblique ou
anisotrope. Les échelles multiplient les **colonnes** de l’affine. La grille
ne contient aucun sommet de surface et n’effectue aucune union de bounding boxes.
Le stockage natif est `i * dimY * dimZ + j * dimZ + k`.
La [convention de coordonnées](hbp_core_coordinate_system.md) s’applique à
la frontière Unity; l’affine d’export reste dans le repère natif.

## Invalidation

| Changement | Ressources à actualiser |
|---|---|
| Volume de référence, dimension ou interpolation de grille | Grille, champ et bindings |
| Activité, sites/implantation, source ou mode, ROI/masque, rayon | Champ sur la grille existante |
| Surface ou hémisphère | Binding et UV; champ et timeline conservés |
| Visibilité/effacement des triangles | Mesh et application des UV existantes |
| Géométrie de coupe | Échantillonnage/textures de coupe |
| Temps, alpha, palette, calibration d’affichage | Échantillonnage/couleurs, sans recalcul du champ brut |

Les entrées sont `InvalidateProjectionGrid`, `InvalidateActivityField`,
`InvalidateSurfaceProjection`, `InvalidateSurfaceMesh` et
`UpdateProjectionResources`. Les compteurs `ProjectionGridVersion`,
`ActivityFieldVersion`, `SurfaceProjectionVersion` rendent les responsabilités
observables. Un changement réel de source/mode CCEP invalide le champ; un
changement de mesh compatible ne le fait pas. Les overlays directs de
`FMRIManager` restent un chemin distinct.

## Couverture d’une surface

Le binding natif est indexé par identité/version de la grille et de la surface,
plus l’interpolation. `Surface.GeometryVersion` suit les positions (remplacement,
transformation, fusion), pas UV, couleurs, normales ou visibilité des triangles.
La première validation ou projection construit les indices nearest/stencils
trilinéaires et compte les sommets valides **dans la même passe**. Le résultat
est réutilisé, sans scan par frame.

Le rapport expose nombres total/valide/invalide, ratio, classification
`Unavailable`, `None`, `Partial`, `Complete`, version et durée de construction.
Une couverture partielle devient significative lorsque :

```text
invalidVertexCount > max(32, floor(totalVertexCount * 0.01))
```

Les sommets hors grille reçoivent des UV neutres, jamais un échantillon de bord
rabattu. Sélectionner volume ou surface reste silencieux. Une demande manuelle
incompatible propose Continue/Cancel avant calcul; l’autorisation porte seulement
sur le couple de versions courant. Un changement pendant le dialogue impose un
nouveau contrôle. Le calcul automatique attend silencieusement une géométrie
compatible ou une autorisation explicite et conserve son état obsolète. Les
coupes et l’export restent utilisables même sans couverture de la surface.

## Export Localizer

`LocalizerExportGridSettings` appartient à l’opération d’export, indépendamment
d’`ActivityProjectionSettings` et des scènes ouvertes : dimension maximale par
défaut **80**, intervalle **2–512**, interpolation **trilinéaire**. La fenêtre ne
mémorise pas la saisie entre ouvertures.

L’aperçu calcule sans grille native les dimensions, le nombre de voxels et les
volumes non compressés : activité float par instant et masque. Il ne prédit pas
la taille gzip. Au-delà de **8 000 000 voxels spatiaux**, une confirmation précède
la fermeture des visualisations. La saisie est revalidée avant tout effet de
bord; la mémoire augmente aussi avec la timeline.

L’export natif copie l’affine exacte de la grille dans `sform`, calcule `sto_ijk`
par inversion et dérive quaternion, `qform` et `pixdim` avec niftilib. Si un
cisaillement ne peut pas être représenté fidèlement, `qform_code` est désactivé;
`sform` reste la référence. Activité et masque partagent dimensions et affine.
Les wrappers dédiés sont libérés en fin d’export.

Exemple : MNI `208 × 256 × 219`, dimension maximale 80 → `65 × 80 × 68`,
353 600 voxels, environ 1,35 MiB d’activité par instant et 0,34 MiB de masque.

## Implémentations et vérification

- [Grille et réglages C#](../../Assets/Scripts/HBP/Core/DLL/Generators/ActivityProjectionGrid.cs),
  [réglages d’export](../../Assets/Scripts/HBP/Core/DLL/Generators/LocalizerExportGridSettings.cs),
  [cycle de scène](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs).
- Dans le checkout `hbp_core` : `src/generators/activity_projection_grid.cpp`,
  `surface_projection_binding.cpp`, `activity_generator_nifti_io.cpp`.
- [Tests de couverture](../../Assets/Tests/EditMode/HBP.Serialization.Tests/ActivityProjectionPhase3Tests.cs),
  [réglages](../../Assets/Tests/EditMode/HBP.Serialization.Tests/ActivityProjectionSettingsTests.cs),
  [cycle PlayMode](../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs)
  et `hbp_core/tests/native/hbp_core_generator_functional_test.cpp`.

Contrôler notamment centres extrêmes et axes obliques, activité/masque relus,
couverture nulle/partielle, cache conservé après triangle eraser et changement
de représentation, recalcul après mutation scientifique. Ces tests sont des
points d’entrée; leur présence ne prouve pas une exécution sur le build courant.

## Mesure historique utile

Le 13 août 2026, l’A/B natif dans **un même processus et une même DLL** utilisait
MNI `208 × 256 × 219` (0,72 mm), 69 104 sommets, dimension 80, 30 000 sites
Halton, 100 instants, rayon linéaire 15 et trois répétitions. Environnement de
référence : Windows 11, i7-12700K, environ 32 Go.

| Mesure | Ancien hybride | Grille seule |
|---|---:|---:|
| Points calculés | 422 704 | 353 600 |
| Valeurs + poids | 170 772 416 octets | 142 854 400 octets |
| Médiane du calcul A/B | 4 703,06 ms | 3 516,82 ms |

La suppression des sommets du champ explique −16,35 % de stockage et le gain
observé. Les temps Unity isolés ne sont pas comparables à cet A/B. Le binding MNI
mesuré ensuite couvrait 100 % des sommets en 1,89 ms et conservait sa version au
second échantillonnage. Ce sont des résultats historiques, non des performances
actuelles garanties.

Reproduction : `NativeProjectionLoadBenchmarkCli.Run` dans
[le benchmark permanent](../../Assets/Tests/EditMode/HBP.Serialization.Tests/NativeProjectionLoadBenchmarkCli.cs)
(profil Product, dimension 80, timeline 100, répétitions 3, interpolation
Trilinear), ou cible native `hbp_core_activity_projection_grid_benchmark`.
Suivre le [workflow de validation](../../.agents/skills/hibop-unity-validation/SKILL.md)
pour toute exécution Unity.
