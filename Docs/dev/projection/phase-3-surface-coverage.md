# Phase 3 — Bindings, couverture et messages différés

## 1. Statut et portée

**Statut :** implémentée et validée le 13 août 2026

Cette phase sécurise la projection d'activité sur une surface qui ne recouvre
pas, ou ne recouvre que partiellement, le volume de référence. Elle ne modifie
ni la grille d'export Localizer, ni l'affine NIfTI, qui restent les objectifs
des phases 4 et 5.

L'ABI historique fondée sur `GeneratorSurface` reste disponible. Le nouveau
comportement strict s'applique au chemin volumique introduit par les phases 1
et 2.

## 2. Binding natif et clé de cache

`SurfaceProjectionBinding` possède les indices nearest ou les stencils
trilinéaires nécessaires pour échantillonner un champ volumique sur une
surface. Sa clé effective contient :

- l'identité et la version géométrique de l'`ActivityProjectionGeometry` ;
- l'identité et la version géométrique de la `Surface` ;
- le mode d'interpolation.

`Surface` expose désormais une version géométrique incrémentée uniquement par
les opérations qui modifient ses sommets : effacement, remplacement des
sommets, fusion et transformation. Les UV, couleurs, normales, triangles et
masques de visibilité n'invalident pas le binding.

L'initialisation de `SurfaceGenerator` ne construit aucun stencil. Le premier
appel à `ComputeActivityUV` appelle `SurfaceProjectionBinding::ensure`. Les
appels suivants, y compris les autres instants d'une timeline, réutilisent le
même binding tant que sa clé ne change pas.

## 3. Rapport de couverture

La boucle unique qui construit les indices ou stencils compte simultanément
les sommets valides. Elle produit `SurfaceProjectionCoverage` :

```text
totalVertexCount
validVertexCount
invalidVertexCount
validRatio
classification
bindingVersion
buildMilliseconds
```

Les classifications sont `Unavailable` avant la première demande, `None`,
`Partial` et `Complete`. La version et le temps de construction rendent la
réutilisation du cache observable sans ajouter de chronométrage C# autour de
chaque frame.

Le rapport est exposé par la nouvelle fonction ABI
`hbp_surface_generator_get_projection_coverage` et par la propriété C#
`SurfaceGenerator.ProjectionCoverage`.

## 4. Projection et politique utilisateur

Le comportement livré est le suivant :

| Couverture | Projection | Message |
| --- | --- | --- |
| nulle | UV activité et alpha neutres sur toute la surface | erreur |
| partielle significative | sommets valides projetés, sommets extérieurs neutres | warning avec pourcentage |
| complète ou quasi complète | projection normale | aucun |

Le seuil initial d'un warning partiel est strictement plus de
`max(32 sommets, 1 % des sommets)` invalides. La comparaison entière utilise la
partie entière de 1 %, de sorte que 101 sommets invalides sur 10 001 dépassent
bien le seuil.

Sur le nouveau chemin volumique, l'anatomie de surface ne rabat plus les
sommets extérieurs sur le voxel de bord le plus proche. Ces sommets conservent
des UV anatomiques neutres. Le rabattement historique reste volontairement
préservé dans la façade legacy afin de ne pas modifier ses consommateurs sans
migration explicite.

## 5. Moment du message et déduplication

`Base3DScene` consulte la couverture uniquement après
`ComputeSurfaceBrainUVWithActivity`, donc lors d'une demande réelle de
projection. `UpdateProjectionResources`, la sélection d'un volume et la
sélection d'une surface restent silencieux.

Un diagnostic est dédupliqué avec la clé de binding portée par la scène :

```text
ProjectionGridVersion + SurfaceProjectionVersion
```

Cette clé commune à toutes les colonnes empêche une répétition entre colonnes
ou rafraîchissements tout en autorisant un nouveau diagnostic après changement
réel de grille ou de surface. La `bindingVersion` native reste un compteur de
cache propre à chaque `SurfaceGenerator` et ne sert donc pas à dédupliquer des
colonnes distinctes.

La couche `HBP.Data.Runtime` publie
`OnSurfaceProjectionDiagnostic(type, title, message)`. `Scene3DWindow`, dans
la couche UI, transforme cet événement en boîte de dialogue. Cette séparation
évite une dépendance inverse de l'assembly de données vers l'assembly UI. Les
messages nomment la surface et le volume, suggèrent de vérifier leurs repères
et leur recalage, et indiquent que les coupes et l'export restent disponibles
en cas de couverture nulle.

## 6. Performance

Le benchmark natif `hbp_core_activity_projection_grid_benchmark` mesure
maintenant le binding sur la surface MNI réelle et vérifie immédiatement une
seconde projection sur la même clé.

Environnement et dataset identiques à la phase 0 : grille trilinéaire de
dimension maximale 80, `MNI.nii` et `MNI_single_hight_Bhemi.obj`.

| Mesure | Résultat |
| --- | ---: |
| Sommets de surface | 69 104 |
| Sommets valides | 69 104 |
| Couverture | 100 % |
| Construction stencils + couverture | 1,89 ms |
| Version après construction | 1 |
| Version au second échantillonnage | 1 |
| Temps mémorisé au second échantillonnage | 1,89 ms |

Le comptage de couverture est effectué dans la boucle de construction des
stencils : il n'existe aucune passe supplémentaire sur les 69 104 sommets. Le
second échantillonnage conserve exactement la version et le temps mémorisés,
ce qui confirme l'absence de reconstruction par timeline ou par frame.

## 7. Validation

Les tests ajoutés couvrent :

- le rapport `Unavailable` après initialisation et calcul anatomique seul ;
- une surface partielle avec deux sommets valides et deux invalides ;
- les UV anatomiques neutres pour les sommets extérieurs ;
- la conservation de la version et du temps de binding au second instant ;
- l'invalidation après modification des sommets de la même surface ;
- une surface totalement disjointe, sans crash et avec UV fonctionnelles
  neutres ;
- les limites exactes du seuil `max(32, 1 %)` ;
- l'absence de diagnostic au moment de la sélection dans une scène réelle ;
- une seule erreur différée lors de la projection d'une surface disjointe ;
- l'absence de répétition au rafraîchissement suivant.

Résultats :

- build Release hbp_core réussi ;
- suite native hbp_core : **13/13 tests réussis** ;
- suite Unity EditMode `HBP.Serialization.Tests` : **474/474 tests réussis** ;
- test PlayMode ciblé de cycle grille/champ/surface et diagnostic :
  **1/1 réussi** ;
- benchmark MNI réel réussi ;
- formatage C# réussi ;
- DLL Release de HiBoP identique à l'artefact hbp_core compilé.

## 8. Gate de sortie

La gate de phase 3 est satisfaite :

- les sélections seules ne produisent aucun message ;
- une projection incompatible produit un seul diagnostic pertinent ;
- les couvertures nulle et partielle ne provoquent ni crash ni lecture de
  voxel de bord trompeuse sur le nouveau chemin ;
- le rapport ne nécessite aucune passe géométrique supplémentaire ;
- le cache dépend uniquement de la géométrie de grille, de la géométrie de
  surface et de l'interpolation documentées.

La phase suivante est la phase 4 : rendre l'affine et les dimensions NIfTI
exportées strictement identiques à la géométrie portée par la grille.
