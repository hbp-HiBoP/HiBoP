# Phase 2 — Migration HiBoP et invalidations ciblées

## 1. Statut et portée

**Statut :** implémentée et validée le 13 août 2026

Cette phase migre le rendu interactif de HiBoP vers la grille volumique créée
en phase 1. Elle ne modifie ni la résolution de l'export Localizer, ni la
politique de diagnostic surface/volume prévue en phase 3.

## 2. Cycle de vie livré

HiBoP possède désormais un wrapper C# `ActivityProjectionGrid`. Une instance
est détenue par `Base3DScene` et reste vivante tant que le volume de référence,
la dimension maximale et l'interpolation ne changent pas.

Chaque `ActivityGenerator` est initialisé avec cette grille. Le champ calculé
reste volontairement détenu par le générateur natif : un wrapper
`ActivityField` séparé n'aurait ajouté aucun propriétaire ou contrat utile à
ce stade.

`SurfaceGenerator` reçoit maintenant explicitement le couple générateur
d'activité/surface. Le nouveau symbole natif
`hbp_surface_generator_initialize_projection_grid` construit les stencils de
surface depuis la géométrie volumique du générateur, sans dépendre de
`GeneratorSurface`. L'initialisation legacy reste disponible pour les autres
consommateurs de l'ABI.

Les références C# assurent les durées de vie natives suivantes :

- la grille conserve son volume ;
- le générateur d'activité conserve sa grille ;
- le générateur de surface conserve le générateur d'activité et la surface ;
- lors d'une reconstruction, les générateurs sont rattachés à la nouvelle
  grille avant la libération de l'ancienne.

## 3. Invalidations

`UpdateGeneratorsAndUnityMeshes()` a été remplacée par
`UpdateProjectionResources()`. Les responsabilités sont séparées comme suit :

| Événement | Grille | Champ | Binding surface | Mesh/UV |
| --- | --- | --- | --- | --- |
| changement de surface ou d'hémisphère | conservée | conservé | reconstruit | actualisés |
| effacement/restauration de triangles | conservée | conservé | conservé | actualisés |
| changement de volume | reconstruite | invalidé | reconstruit | actualisés |
| dimension/interpolation de grille | reconstruite | invalidé | reconstruit | actualisés |
| sites, ROI, implantation, masque ou rayon | conservée | invalidé | conservé | actualisés après calcul |
| temps, alpha ou calibration fonctionnelle | conservée | conservé | conservé | actualisés |

Les drapeaux `ProjectionGridNeedsUpdate` et
`SurfaceProjectionNeedsUpdate` complètent `GeneratorNeedsUpdate`, qui conserve
le rôle d'invalidation du champ. `ActivityProjectionSettings.OnChanged`
propage les changements de géométrie à toutes les scènes actives.

`ResetGenerators()` reste une façade de compatibilité publique, mais les
appels internes modifiés utilisent maintenant `InvalidateActivityField()`,
`InvalidateProjectionGrid()`, `InvalidateSurfaceProjection()` ou
`InvalidateSurfaceMesh()` selon leur responsabilité.

`MeshManager` ne réinitialise plus l'activité. `TriangleEraser` réapplique les
UV déjà calculées au nouveau mesh Unity sans reconstruire les stencils. Le
changement automatique réel d'un mode CCEP continue à invalider le champ via
`OnSelectSource`; une simple sélection de mesh compatible ne le fait pas.

Trois compteurs de diagnostic exposés par la scène rendent le comportement
testable : `ProjectionGridVersion`, `ActivityFieldVersion` et
`SurfaceProjectionVersion`.

## 4. Validation

La validation finale couvre :

- grille C# sans surface, dimensions et points exclusivement volumiques ;
- calcul de densité puis projection sur deux surfaces explicites sans nouveau
  calcul du champ ;
- parité native des UV activité/alpha entre la voie legacy et la nouvelle voie
  volumique trilinéaire ;
- changement de surface avec conservation de l'instance et de la version de
  grille, de la version du champ et de `IsGeneratorUpToDate` ;
- recalcul du champ sur la même grille ;
- reconstruction de grille sans recalcul implicite du champ ;
- modification des masques de triangles sans invalidation de grille, de champ
  ou de binding de surface ;
- libération déterministe des nouveaux wrappers par les cycles `using` et le
  nettoyage de scène existant.

Résultats :

- build Release hbp_core réussi ;
- suite native hbp_core : **13/13 tests réussis** ;
- suite Unity EditMode `HBP.Serialization.Tests` : **472/472 tests réussis** ;
- tests PlayMode ciblés de cycle de vie et de triangle eraser : **2/2 réussis** ;
- formatage C# et `git diff --check` réussis ;
- DLL Release copiée dans HiBoP et hash identique à l'artefact compilé.

## 5. Gate de sortie

La gate de phase 2 est satisfaite :

- un changement de surface ne reconstruit ni la grille ni le champ et ne rend
  pas la timeline obsolète ;
- un changement de volume ou de configuration de grille reconstruit la grille
  et invalide explicitement le champ ;
- une invalidation d'activité recalcule le champ sur l'instance de grille
  existante ;
- les surfaces et les coupes restent alimentées par la même géométrie
  volumique ;
- les nouveaux propriétaires natifs sont libérés par le cycle de vie de scène.

La phase 3 peut maintenant introduire le cache de bindings, le rapport de
couverture et les messages différés sans remettre en cause la durée de vie du
champ volumique.
