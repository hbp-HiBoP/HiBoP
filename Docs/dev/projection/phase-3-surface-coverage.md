# Phase 3 — Bindings, couverture et messages différés

## 1. Statut et portée

**Statut :** implémentée et validée le 13 août 2026

Cette phase sécurise la projection d'activité sur une surface qui ne recouvre
pas, ou ne recouvre que partiellement, le volume de référence. Elle ne modifie
ni la grille d'export Localizer, ni l'affine NIfTI, qui restent les objectifs
des phases 4 et 5.

Le comportement décrit ici s'applique au chemin volumique introduit par les
phases 1 et 2. L'ABI historique mentionnée dans la livraison initiale de cette
phase a ensuite été supprimée par la phase 6.

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
appel à `ValidateProjectionCoverage` ou `ComputeActivityUV` appelle
`SurfaceProjectionBinding::ensure`. Le préflight et le rendu réutilisent donc
le même binding tant que sa clé ne change pas.

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

Le rapport est exposé par `hbp_surface_generator_get_projection_coverage` et
par la propriété C# `SurfaceGenerator.ProjectionCoverage`. L'appel explicite
`hbp_surface_generator_validate_projection_coverage` permet de construire le
rapport avant tout calcul d'activité.

## 4. Projection et politique utilisateur

Le comportement livré est le suivant :

| Couverture | Projection automatique | Projection forcée par le bouton |
| --- | --- | --- |
| nulle | suspendue silencieusement | warning « Continue / Cancel » |
| partielle significative | suspendue silencieusement | warning avec pourcentage et « Continue / Cancel » |
| complète ou quasi complète | normale | normale, sans message |

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

`Base3DScene` valide la couverture avant de calculer ou d'appliquer l'activité.
La sélection d'un volume ou d'une surface reste silencieuse : elle peut
construire le binding en cache, mais n'ouvre aucune boîte de dialogue.

Le résultat est mémorisé avec la clé portée par la scène :

```text
ProjectionGridVersion + SurfaceProjectionVersion
```

Cette clé commune à toutes les colonnes évite toute reconstruction par frame.
En mode automatique, une couverture problématique conserve l'activité en état
« à recalculer » et attend une nouvelle version de grille ou de surface. En
mode manuel, `ComputeActivity` demande confirmation avant d'invalider ou de
calculer le champ. « Cancel » ne lance rien ; « Continue » autorise uniquement
le couple grille/surface courant. Si ce couple change pendant que la boîte est
ouverte, le préflight est recommencé.

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
- le rapport disponible avant `ComputeActivityUV` et réutilisé par celui-ci ;
- le blocage silencieux de la projection automatique sur un couple incompatible ;
- la reprise automatique après une nouvelle version de surface compatible ;
- la production du warning manuel avec les noms de surface et de volume.

Résultats :

- build Release hbp_core réussi ;
- suite native hbp_core : **13/13 tests réussis** ;
- suite Unity EditMode `HBP.Serialization.Tests` : **472/472 tests réussis** ;
- test PlayMode ciblé de cycle grille/champ/surface et diagnostic :
  **1/1 réussi** ;
- benchmark MNI réel réussi ;
- formatage C# réussi ;
- DLL Release de HiBoP identique à l'artefact hbp_core compilé.

## 8. Gate de sortie

La gate de phase 3 est satisfaite :

- les sélections seules ne produisent aucun message ;
- une projection manuelle incompatible produit un diagnostic avant calcul ;
- une projection automatique incompatible attend silencieusement ;
- les couvertures nulle et partielle ne provoquent ni crash ni lecture de
  voxel de bord trompeuse sur le nouveau chemin ;
- le rapport ne nécessite aucune passe géométrique supplémentaire ;
- le cache dépend uniquement de la géométrie de grille, de la géométrie de
  surface et de l'interpolation documentées.

La phase suivante est la phase 4 : rendre l'affine et les dimensions NIfTI
exportées strictement identiques à la géométrie portée par la grille.
