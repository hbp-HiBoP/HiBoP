# Phase 4 — Géométrie et robustesse de l'export NIfTI

## 1. Statut et portée

**Statut :** implémentée et validée le 13 août 2026

Cette phase rend la géométrie des NIfTI d'activité et de masque strictement
identique à celle de l'`ActivityProjectionGrid` qui a servi au calcul. Elle ne
change pas encore la résolution de l'export Localizer : celui-ci utilise
toujours `ActivityProjectionSettings.VolumeGridDimension`, dont la valeur par
défaut est 80. Le réglage indépendant et son interface appartiennent à la
phase 5.

## 2. Cause corrigée

L'export connaissait les dimensions générées, mais reconstruisait son affine à
partir du volume de référence :

```text
ancien facteur = dimensionSource / dimensionGrille
```

Il appliquait ensuite les facteurs aux lignes de la matrice du volume. Deux
erreurs en résultaient :

- les centres extrêmes n'étaient pas conservés, car une grille de `N` centres
  contient `N - 1` intervalles ;
- les volumes obliques ou anisotropes recevaient les facteurs sur les mauvais
  axes matriciels.

`ActivityProjectionGrid` possédait déjà le contrat correct :

```text
facteur = (dimensionSource - 1) / (dimensionGrille - 1)
gridToWorld = voxelToWorld × scaleParColonne
```

La correction consiste donc à consommer cette donnée existante, pas à refaire
le calcul dans l'export.

## 3. Contrat natif

`ActivityProjectionGeometry` expose désormais
`exact_grid_to_world()` :

- `ActivityProjectionGrid` retourne son affine persistante exacte ;
- `GeneratorSurface` retourne `nullptr`, ce qui sélectionne explicitement la
  voie d'export historique.

Cette distinction conserve les symboles et résultats legacy tout en empêchant
une approximation silencieuse sur le nouveau pipeline volumique.

Pour une géométrie exacte, l'export :

1. copie directement `gridToWorld` dans le sform ;
2. calcule `sto_ijk` comme son inverse ;
3. dérive les champs quaternion, le qform et son inverse avec niftilib ;
4. écrit les `pixdim` spatiaux produits par cette décomposition ;
5. applique exactement le même traitement à l'activité et au masque.

Le sform reste la représentation de référence exacte. Si une affine future
contient du cisaillement que le qform quaternion ne peut représenter sans
orthogonalisation, son `qform_code` est désactivé afin qu'aucun lecteur ne
préfère une approximation au sform exact.

## 4. Export Localizer

L'export Localizer autonome utilisait encore un `GeneratorSurface` contenant
la surface grise MNI. Il utilise maintenant une `ActivityProjectionGrid` et
initialise directement l'`IEEGGenerator` avec cette grille.

Conséquences :

- l'affine corrigée s'applique réellement aux fichiers Localizer ;
- aucun sommet de surface MNI n'entre dans le calcul ou les buffers exportés ;
- la géométrie scientifique est la même que dans le rendu interactif ;
- la dimension reste provisoirement couplée au réglage interactif, jusqu'à la
  phase 5.

Les wrappers natifs sont libérés à la fin de l'opération d'export par leurs
portées `using`.

## 5. Cas de validation

### 5.1 Volume identité

Source : `5 × 5 × 5`, affine identité, grille `8 × 8 × 8`.

| Propriété | Ancien export | Export exact |
| --- | ---: | ---: |
| Pas spatial | `5 / 8 = 0,625` | `4 / 7 ≈ 0,571429` |
| Premier centre | `(0, 0, 0)` | `(0, 0, 0)` |
| Dernier centre | `(4,375, 4,375, 4,375)` | `(4, 4, 4)` |

### 5.2 Volume oblique anisotrope

Source : `3 × 5 × 9`, spacing `2 × 3 × 4 mm`, rotation de 90 degrés des axes
X/Y et translation `(10, -20, 30)`. La grille vaut `2 × 4 × 8`.

Sform exporté :

```text
[ 0  -4       0       10 ]
[ 4   0       0      -20 ]
[ 0   0   32 / 7      30 ]
[ 0   0       0        1 ]
```

Les centres testés correspondent exactement aux centres source :

- `(0, 0, 0) → (0, 0, 0)` ;
- `(1, 0, 0) → (2, 0, 0)` ;
- `(0, 3, 0) → (0, 4, 0)` ;
- `(0, 0, 7) → (0, 0, 8)` ;
- `(1, 3, 7) → (2, 4, 8)`.

Le dernier centre monde redevient ainsi `(-2, -16, 62)`, au lieu de
`(-3,5, -17,5, 61,5)` dans la baseline legacy.

## 6. Validation

La validation vérifie :

- dimensions 3D/4D exactes ;
- sform, qform et `pixdim` ;
- premier et dernier centres de grille ;
- extrémité de chacun des trois axes obliques ;
- identité spatiale entre activité et masque ;
- contenu activité/masque attendu sur la fixture à couverture complète ;
- relecture des deux fichiers avec le wrapper `NIFTI` de HiBoP utilisé dans le
  workflow Localizer ;
- conservation des deux baselines d'export `GeneratorSurface` historiques.

Résultats :

- build Release hbp_core réussi ;
- suite native hbp_core : **13/13 tests réussis** ;
- tests ciblés export nouveau et legacy : **6/6 réussis** ;
- suite Unity EditMode `HBP.Serialization.Tests` : **476/476 réussis** ;
- test PlayMode de la fenêtre Localizer : **1/1 réussi** ;
- formatage C# et `git diff --check` réussis ;
- DLL Release de HiBoP identique à l'artefact hbp_core compilé.

## 7. Gate de sortie

La gate de phase 4 est satisfaite :

- chaque voxel exporté décrit le même point monde que le point calculé par la
  grille ;
- activité et masque ont des dimensions et une affine superposables ;
- les volumes anisotropes et obliques conservent leurs axes et leurs centres
  extrêmes ;
- les fichiers sont relus par le chemin NIfTI de HiBoP ;
- la façade `GeneratorSurface` permet toujours de reproduire l'export
  historique pour comparaison.

La phase suivante est la phase 5 : introduire des réglages de grille propres à
l'export Localizer et les exposer dans sa fenêtre sans modifier la résolution
interactive.
