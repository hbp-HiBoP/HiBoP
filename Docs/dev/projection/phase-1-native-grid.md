# Phase 1 — Grille volumique native

## 1. Statut

**Statut :** implémentée et validée le 13 août 2026

**Portée :** `hbp_core` et DLL Windows intégrée à HiBoP. Les wrappers et le
cycle de vie Unity restent volontairement sur `GeneratorSurface` jusqu'à la
phase 2.

## 2. Architecture livrée

`ActivityProjectionGrid` est une primitive native indépendante de toute
surface. Elle possède :

- le volume de référence ;
- les dimensions de grille ;
- les points volumiques, indexés à partir de zéro ;
- les transformations `gridToWorld` et `worldToGrid` ;
- le mode d'interpolation ;
- le cache LRU des index de recherche spatiale par rayon.

Les points sont construits dans les axes voxel du volume. Pour chaque axe, le
pas vaut `(dimensionSource - 1) / (dimensionGrille - 1)`. Les centres extrêmes
de la grille correspondent donc exactement aux centres extrêmes du volume,
y compris pour un volume oblique ou anisotrope. Ce contrat est distinct du
comportement historique AABB de `GeneratorSurface`.

Une interface interne minimale, `ActivityProjectionGeometry`, est partagée par
la nouvelle grille et la façade legacy. `ActivityGenerator`, les générateurs
iEEG, densité, fMRI et MEG, les coupes et l'export NIfTI consomment cette
interface. La nouvelle voie calcule et normalise exclusivement sur le champ
volumique ; la voie historique conserve ses index et ses résultats.

`SurfaceGenerator` continue temporairement à utiliser la surface portée par
`GeneratorSurface`. Son découplage et le binding d'une surface explicite sont
réservés aux phases 2 et 3.

## 3. ABI ajoutée

Les symboles historiques `hbp_generator_surface_*` et
`hbp_activity_generator_initialize` restent inchangés. L'ABI parallèle ajoute :

- `hbp_activity_projection_grid_create` et `destroy` ;
- `hbp_activity_projection_grid_initialize` ;
- `hbp_activity_projection_grid_set_volume_interpolation` ;
- `hbp_activity_projection_grid_get_dimensions` ;
- `hbp_activity_projection_grid_get_point_count` et `copy_points` ;
- `hbp_activity_projection_grid_get_grid_to_world` et `get_world_to_grid` ;
- `hbp_activity_generator_initialize_projection_grid`.

Les dix symboles sont présents dans la table d'exports de la DLL Release.

## 4. Validation scientifique et géométrique

Les tests natifs couvrent :

- dimensions et nombre de points ;
- capacité des buffers ABI ;
- correspondance exacte des points avec `gridToWorld` ;
- aller-retour `gridToWorld` / `worldToGrid` ;
- centres extrêmes du volume ;
- volume oblique et anisotrope ;
- interpolation nearest et trilinéaire dans une grille affine ;
- calcul iEEG et densité sur les seuls points de grille ;
- parité exacte des valeurs et poids iEEG avec la portion volumique legacy ;
- calcul fMRI et MEG sur les seuls points de grille ;
- échantillonnage des coupes depuis la nouvelle grille ;
- compteurs mémoire et métriques iEEG.

Résultats :

- build Release complet réussi ;
- suite native : **13/13 tests réussis** ;
- suite Unity EditMode `HBP.Serialization.Tests` avec la DLL mise à jour :
  **470/470 tests réussis** ;
- aucun `DllNotFoundException`, `EntryPointNotFoundException` ou échec de test.

Unity a été lancé avec l'installation complète
`6000.5.2f1-x86_64`. Le dossier local `6000.5.2f1` sans suffixe est incomplet
et ne contient pas `UnityPackageManager.exe`.

## 5. Mesures

Le benchmark natif A/B utilise la même DLL, le même processus, `MNI.nii`, la
surface `MNI_single_hight_Bhemi.obj`, une dimension maximale 80, 30 000 sites
Halton, 100 instants, un rayon linéaire de 15 et trois répétitions.

| Mesure | `ActivityProjectionGrid` | `GeneratorSurface` legacy |
| --- | ---: | ---: |
| Points calculés | 353 600 | 422 704 |
| Valeurs stockées | 35 360 000 | 42 270 400 |
| Valeurs + poids | 142 854 400 octets | 170 772 416 octets |
| Liens de voisinage, dernière répétition | 33 180 947 | 41 969 782 |
| Calcul total, médiane A/B | 3 516,82 ms | 4 703,06 ms |

La nouvelle voie retire exactement les 69 104 sommets de surface attendus :

- **−16,35 %** de points, de valeurs et de poids ;
- **−27 918 016 octets** pour les valeurs et poids du champ ;
- environ **−25,2 %** sur la médiane du calcul A/B observé.

Une mesure Unity legacy isolée avec la même DLL confirme le checksum historique
`DD77E36640A7FB8C` et une médiane de calcul de 2 681,33 ms. La différence de
durée absolue avec le benchmark natif A/B provient de leur environnement de
mesure ; seule la comparaison A/B au sein d'un même processus est utilisée
pour attribuer le gain à la nouvelle grille.

## 6. Gate de sortie

La gate de phase 1 est satisfaite :

- la nouvelle API ne contient aucun sommet de surface ;
- les points correspondent exactement à `gridToWorld` ;
- les volumes obliques et anisotropes sont représentés dans leurs axes réels ;
- les quatre familles de générateurs utilisent le domaine volumique commun ;
- l'ancienne ABI et ses résultats restent compatibles ;
- la suite native, la suite Unity et la parité scientifique passent ;
- la réduction mémoire et le gain A/B sont expliqués par le retrait des points
  de surface.

La phase suivante est la phase 2 : wrappers C#, cycle de vie persistant de la
grille et invalidations ciblées lors des changements de surface.
