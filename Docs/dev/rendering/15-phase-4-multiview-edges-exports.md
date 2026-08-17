# Phase 4 — Multi-vues, Edges et exports

## Statut

**Implémentée et Gate 4 validée sous Windows / Direct3D 11 le 7 août 2026.**

Cette phase ferme le cycle de rendu d'une vue URP, depuis sa `RenderTexture`
jusqu'aux PNG, au composite et à la vidéo. Elle ne modifie pas le modèle
scientifique de projection de l'activité et ne constitue pas encore la
validation de performance multi-plateforme de la phase 5.

## RenderTextures des vues

Les vues utilisent désormais `HBPRenderTextureDescriptorFactory` et
`HBPRenderTextureOwner` :

- format couleur `R8G8B8A8_SRGB` et profondeur/stencil explicites ;
- une texture est réutilisée tant que son descripteur reste compatible ;
- un redimensionnement libère l'ancienne allocation avant d'en créer une ;
- une vue minimisée, masquée, désactivée ou détruite détache et libère sa
  texture ;
- la caméra d'une vue désactivée est arrêtée puis restaurée à la réactivation ;
- aucune allocation n'est effectuée pour une dimension nulle.

La taille d'une texture de vue est calculée depuis le rectangle physique à
l'écran, après application de l'échelle du Canvas. Un pixel de la texture
correspond donc à un pixel affiché, sans modifier le ratio de la vue.

Les meshes partagés des sites et des cages ROI sont également recréés à la
demande si Unity détruit leur instance mise en cache pendant un changement de
domaine ou de mode de lecture.

Les tests couvrent 27 propriétaires de textures, soit le cas réel 9 colonnes ×
3 vues, pendant 100 cycles de redimensionnement. Le nombre de textures vivantes
reste borné à une texture par vue et revient à sa valeur initiale après
libération.

## Edges URP

L'ancien effet PPv2/AGM est remplacé par `HBPEdgeRendererFeature`, une Renderer
Feature URP Render Graph exécutée avant le post-traitement :

- buffer profondeur/normales dédié aux cerveaux et coupes opaques ;
- masque `HBPEdgeMask` dédié à la silhouette des cerveaux et coupes
  transparents ;
- composition alpha-over prémultipliée afin de préserver les exports
  transparents ;
- réglage `HBPEdgeCameraSettings` sérialisé sur chaque caméra de vue ;
- paramètres et état copiés dans les données de passe, sans état global entre
  caméras ;
- `MaterialPropertyBlock` réutilisé, sans allocation managée par frame.

Les ruptures de profondeur sont détectées par la variation de pente autour du
pixel central. Une pente régulière, telle qu'une coupe plane vue presque de
profil, n'est ainsi plus interprétée comme une succession d'arêtes.

Les shaders des sites et de la cage ROI n'exposent pas la passe
`HBPEdgeMask`. Ils restent donc exclus. Un test de rendu compare les pixels avec
Edges on/off : une coupe reçoit bien ses contours, tandis qu'un site reste
strictement identique.

Les repères interactifs de caméra et de coupe conservent leur écriture de
profondeur et leur occultation habituelle. Ils sont néanmoins absents du buffer
dédié aux Edges, qui ne dessine que les passes `HBPEdgeData` des cerveaux et
coupes opaques. Le masque transparent utilise de même uniquement les passes
`HBPEdgeMask`, sans être découpé par la profondeur des repères.

Le prefab `View 3D` ne contient plus de composant Post Processing Stack v2. Les
packages PPv2 et Amplify/AGM, le profil d'edges, les références d'assembly et
les données de scène associées ont été retirés.

## Exports et alpha

`View3D.GetTexture` crée une cible d'export avec le même descripteur explicite,
restaure systématiquement la texture cible, l'aspect et le fond de la caméra,
puis détruit la cible temporaire dans un `finally`.

Pour un PNG individuel transparent, le framebuffer URP contient des RGB
prémultipliés. La conversion en straight alpha est effectuée dans le bon espace
colorimétrique :

1. décodage sRGB vers linéaire ;
2. division des RGB linéaires par l'alpha ;
3. clamp puis réencodage linéaire vers sRGB ;
4. mise à zéro des RGB lorsque l'alpha est nul.

Une simple division des octets sRGB aurait surexposé les pixels semi-
transparents et produit des halos. Le test de rendu d'une sphère rouge à 50 %
vérifie désormais que le PNG conserve un rouge proche de 255 pour un alpha
proche de 128. La recomposition est également testée sur blanc et `#282828`.

Les chemins de capture produit libèrent maintenant :

- chaque PNG 3D temporaire ;
- les captures de graphe et de matrice d'essais ;
- les fragments de matrice d'essais ;
- la capture complète de l'interface ;
- chaque sous-image utilisée pour une frame vidéo ;
- la texture vidéo finale et le `VideoStream`, y compris en cas d'exception.

Le buffer JPEG de la vidéo accepte des frames encodées de tailles successives
différentes ; seule la portion contenant la frame courante est copiée et écrite.

La position de scroll de la matrice et les index temporels de toutes les
colonnes dynamiques sont restaurés après l'export.

## Preuves automatiques

Suites exécutées dans l'éditeur Unity `6000.5.2f1` :

- `HBP.Rendering.Tests` EditMode : **77/77** ;
- `HBP.Module3D.PlayModeTests` : **43/43**.

Les tests de phase 4 couvrent notamment :

- réutilisation, redimensionnement et libération des RenderTextures ;
- 9 × 3 vues pendant 100 cycles ;
- 100 exports après préchauffage des tailles du pool Render Graph ;
- arrêt et libération d'une vue masquée ;
- restauration complète de la caméra après export ;
- straight alpha sRGB sur données synthétiques et rendu réel ;
- feature Render Graph active et shader compilable Metal ;
- isolation des réglages Edges entre caméras ;
- présence des contours sur les coupes et exclusion des sites/ROI ;
- absence de PPv2/AGM dans le prefab, la scène, les assemblies et les packages.

## Validation réelle `visu_full_test / Small`

La campagne rapide a été exécutée avec le mode idle suspendu sur une RTX 2070
SUPER / Direct3D 11. Les artefacts sont dans :

`.test-results/rendering/urp-phase4/20260807-150554`

Résultats :

- 46 captures et 15 échantillons surface/coupe ;
- 0 avertissement et 0 erreur Console ;
- 10 exports individuels 2048 × 2048, tous avec quatre coins RGBA zéro ;
- contours opaques présents sur les sillons ;
- contour transparent limité à la silhouette extérieure ;
- recompositions contrôlées visuellement sur blanc et `#282828`, sans halo ;
- composite 1920 × 1080 et capture UI non vides ;
- vidéo MJPEG 1920 × 1080, 10 frames confirmées par `ffprobe`.

La mesure produite pendant cette campagne est conservée pour la phase 5, mais
elle n'est pas utilisée ici comme preuve comparative Built-in/URP.

## Sortie de phase

Le Gate 4 est fermé sous Windows. La suite relève de la phase 5 : validation
humaine complète de `Small`, mesures comparatives du cas courant, puis audit des
configurations 8–9 colonnes × 3 vues avec la charge de sites réelle. Les
validations Metal Apple Silicon et Vulkan Linux restent dans le Gate 6.
