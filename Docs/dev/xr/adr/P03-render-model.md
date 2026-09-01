# ADR P03 — modèle de rendu pur et parité Desktop

- **Statut :** ACCEPTED — GATE P03-A–E RESOLVED
- **Date :** 2026-09-01
- **Accepté par :** propriétaire du dépôt et responsable scientifique via validation explicite des décisions P03-A–E
- **Baseline :** branche `feature/xr`, commit parent `43e589db05614ca64f0d4c6c34623e438037a7b9`
- **Validation automatique :** D0, D5 et D6 synthétiques uniquement
- **Validation sur données réelles :** manuelle et visuelle, hors dépôt et hors tests automatiques

## P03-A — sémantique temporelle canonique

La V1 reproduit exactement le comportement Desktop observé :

- les sites évaluent linéairement `value[index]` et `value[index + 1]` avec `TemporalAlpha` ;
- les surfaces et overlays de coupe utilisent le seul échantillon `index` (`SampleAndHold`) ;
- `TemporalAlpha` reste attaché à toutes les frames comme provenance de l'instant demandé ;
- `ActivityOpacity` est un paramètre visuel distinct et ne doit jamais être nommé ou interprété comme alpha temporel.

Cette asymétrie est intentionnelle pour la parité V1. Elle ne doit pas être corrigée silencieusement côté XR. Une interpolation future des surfaces/coupes exige une nouvelle décision scientifique précisant si elle intervient avant projection, après projection ou après calibration.

## P03-B — repère canonique

Les adaptateurs remettent au RenderModel uniquement des coordonnées déjà exprimées dans l'espace Unity :

- axes `XYZ` ;
- handedness gauche ;
- unité millimètre et `metersPerUnit = 0.001` ;
- `assetToBrain = identity` pour les buffers Desktop déjà normalisés ;
- `mappingVersion = 1`.

La frontière native reste externe au package : les `Vec3` natifs sont right-handed, tandis que les `Vector3` HiBoP sont en espace Unity. Le wrapper Desktop inverse X et le winding avant capture. Chaque asset porte son repère, ses bounds et la version du mapping ; aucun repère implicite n'est accepté.

## P03-C — tolérances et profils de données

Les validations automatiques utilisent exclusivement des fixtures synthétiques D0/D5/D6 :

- IDs, counts, dimensions, indices, masques, flags et RGBA8 : égalité exacte ;
- buffers seulement capturés ou reconstruits : hash SHA-256 et octets identiques ;
- calcul D5 répété par le vrai oracle Desktop `HBP.Core.Data.TemporalSample.Evaluate` puis par `RenderTemporalSample.EvaluateLinear` : erreur absolue maximale `1e-6` ;
- manifeste P00 synthétique versionné et immuable pendant les tests P03 ; les sorties Desktop et RenderModel doivent chacune retrouver ses longueurs et hashes approuvés ;
- PNG synthétique produit sur la même version Unity à partir, d'une part, des pixels Desktop et, d'autre part, des pixels reconstruits depuis le DTO : octets identiques ;
- aucune tolérance scientifique n'est déduite de ces seuils pour les données réelles.

Les D1–D4 réels sont réservés aux validations visuelles manuelles. Ils ne sont ni copiés, ni nommés, ni hashés par les tests P03 automatiques.

## P03-D — propriété et durée de vie des buffers

`RenderBuffer<T>` possède son tableau et n'expose que `Count`, un indexeur par valeur et `ToArray`. Il ne publie ni tableau, ni `Memory`, ni `Span` adossé au stockage interne. Sa création rend la copie explicite :

- `CopyFrom` crée la photographie défensive d'un tableau encore possédé/réutilisé par HiBoP ;
- `TakeOwnership` adopte sans copie un tableau neuf que l'appelant s'engage à ne plus toucher ;
- `ToArray` est une copie explicite pour les consommateurs qui en ont besoin.

Les adaptateurs Desktop copient une fois les tableaux mutables/réutilisables de Core/Data dans de nouveaux tableaux, puis les transfèrent au DTO. Il n'existe pas de pooling ni de `Dispose` en V1 : les assets sont conservés par hash, les frames vivent tant que leur bundle est référencé, puis le GC les récupère. Toute optimisation par pool exige des mesures et des tests de lifetime supplémentaires.

## P03-E — représentations V1

| Résultat Desktop | Primitive RenderModel V1 | Remarque |
| --- | --- | --- |
| surface anatomique/inflated/autre topologie compatible | `SurfaceAsset` | positions, normales, indices, UV statiques, bounds, repère, hash |
| activité/opacité sur surface | `SurfaceFrame` | buffers par sommet et masque actif ; modalité source opaque |
| implantation/site | `SiteAsset` | IDs opaques et positions Unity |
| état dynamique des sites | `SiteRenderFrame` | position actuellement rendue, RGBA8, taille, visibilité et flags dérivés |
| géométrie de coupe | `CutGeometryAsset` | dédupliquée par hash |
| texture anatomique ou autre image immutable | `TextureAsset` | dimensions, espace colorimétrique, RGBA8 |
| overlay de coupe par colonne | `CutOverlayFrame` | IDs coupe/colonne, révision source, pixels complets et provenance temporelle |
| résultat de coupe atomique | `CutRenderResult` | plan, révisions, géométrie/base optionnelles et overlays |
| instant multi-colonnes | `DynamicFrameBundle` | exactement une `ColumnFrame` par colonne attendue |

iEEG, CCEP, fMRI, MEG, anatomie et atlas se projettent dans ces primitives génériques. Le RenderModel ne contient aucun DTO par modalité. Données sources, volumes complets, noms humains, calcul scientifique, transport, UI, matériaux Desktop et poses XR restent hors du package.

## Frontières d'assemblage

- `CRNL.HiBoP.RenderModel` dépend seulement de `CRNL.HiBoP.Contracts` et de la BCL ; il porte `noEngineReferences: true`.
- `HBP.RenderModelAdapters.Runtime` dépend de Core/Data, Unity, Contracts et RenderModel et ne modifie aucune classe HiBoP.
- `RenderModelReconstructor` constitue l'oracle CPU du renderer de test indépendant et ne référence ni Unity, ni Core, ni Data.

Les overlays sont atomiques avec leur résultat et leur bundle : identité de coupe, colonne, sample et révision source sont validés ; les doublons et dimensions incohérentes sont rejetés.

## Réouverture

Réouvrir P03 si la VR doit interpoler surfaces/coupes, si un asset n'est pas en millimètres Unity, si un pool devient nécessaire, si une modalité ne peut pas se projeter dans les primitives V1 ou si une validation réelle exige une nouvelle tolérance automatique.
