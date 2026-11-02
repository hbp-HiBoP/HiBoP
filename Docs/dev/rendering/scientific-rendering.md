# Rendu scientifique

Le projet utilise URP 17.5, Universal Forward, Render Graph et l’espace Linear.
La migration Built-in est implémentée; ses anciens audits décrivaient un état
remplacé. Les packages PPv2/AGM ne sont plus des dépendances actives.

## Pipeline et responsabilités

Les assets de `Assets/Settings/Rendering/` séparent les profils : Desktop utilise
MSAA 1, HDR/ombres/lumières additionnelles désactivés; Quest utilise notamment
MSAA 4 et active ombres principales/lumières additionnelles. Les réglages desktop
ne sont donc pas une description universelle du profil XR. Le cerveau scientifique
utilise son éclairage propre relatif à la caméra, indépendamment des lumières
de scène.

| Propriétaire | Responsabilité |
|---|---|
| `BrainMaterial.cs`, shaders `Assets/Shaders/HBP/Brain/` | Anatomie, overlays, extrusion, clipping, inflation |
| `Includes/HBPColor.hlsl`, `HBPLighting.hlsl` | Conversion, composition et relief scientifique |
| `HBPEdgeRendererFeature` | Composite cerveau transparent et contours Render Graph |
| `HBPRenderTextureDescriptorFactory`, `HBPRenderTextureOwner` | Descripteurs et durée de vie des cibles de vue |
| `View3D`, `View3DUI`, `Scene3DWindow` | Taille des vues, captures et exports |
| Générateurs natifs activité/coupes | Valeurs, support et composition scientifique |

Le faible nombre de lumières et le coût multiplié par les vues expliquent le
choix Forward; Deferred/Forward+, HDRP et un SRP complet n’apportaient pas de
bénéfice démontré. Les shaders HLSL dédiés conservent l’API des matériaux et les
20 plans de clipping. Extrusion, clipping et déformation d’inflation doivent
rester cohérents dans toutes les passes concernées.

## Couleurs et activité

Les palettes/UI sont sRGB; conversion vers Linear **une fois** avant composition.
Textures de scalaires, masque, indices et alpha ne sont pas sRGB. Une couleur
portée par vertex, texture ou palette doit avoir le même mapping. Atlas et
sélection/hover restent sémantiquement distincts. La composition courante est :

```text
paletteAlpha = 1 - (1 - clamp(scientificAlpha, 0, 1))²
mesh = lerp(litAnatomy, scientificLinear * controlledRelief, paletteAlpha)
cut  = lerp(anatomy, scientificLinear, paletteAlpha)
```

À alpha 0,8, le poids palette vaut 0,96. Les coupes et légendes sont des
références non éclairées. Le relief scientifique diffuse multiplie la couleur,
est borné à 1,08 et par la marge des canaux; un reflet neutre très localisé
est une exception visuelle volontaire. Il ne modifie ni normalisation, ni LUT,
ni seuils. Tone mapping, grading ou modification des palettes ne servent pas
à compenser une erreur de couleur.

En interpolation continue, le champ volumique commun subit deux passes d’un
noyau binomial séparable à cinq taps par axe pour valeurs/poids, puis une
interpolation trilinéaire pondérée. Le cache est partagé par instant entre
mesh et coupes. Le support reçoit séparément une seule passe de rayon deux :
atténuation sur deux couches intérieures et halo au plus deux cellules extérieures.
Le mode nearest reste la référence discrète. On ne floute pas séparément les RGB
après LUT. Le mesh n’ajoute pas un masque basé sur le voxel IRM nearest.

Les préférences `Mask activity on MRI background` (coupes seulement) et
`Smooth activity boundaries` (alpha du support) sont indépendantes, activées par
défaut; désactiver le lissage ne change pas les valeurs ni le mapping de palette.

### Support anatomique actuel des coupes

Le code natif courant a remplacé l’ancienne règle « seuil 5 % puis flood-fill
relié au bord ». `anatomy_value_is_supported` utilise une calibration intrinsèque,
indépendante de la fenêtre d’affichage et de la palette : valeur finie non nulle,
plage robuste `calMax - calMin` finie positive et
`(value - calMin) / range * 254 >= 1`.

`CutGeometryGenerator` intègre ce prédicat sur une empreinte **4 × 4** par pixel,
avec raccourci pour les régions uniformes; le mode Raw garde une couverture
binaire. Cette couverture géométrique est mise en cache indépendamment de
l’activité. `CutGenerator` reconstruit les couleurs au bord pour enlever la
contribution de fond noir puis applique la couverture fractionnaire. Les
valeurs/poids d’activité et leur lissage restent distincts. Références natives :
`src/volume/anatomy_support.h`, `src/generators/cut_geometry_generator.cpp`,
`cut_generator.cpp`, test `test_calibrated_anatomy_support`.

## Transparence, contours, sites et ROI

Le cerveau transparent n’est plus seulement un matériau à alpha blending
classique. `HBPTransparentBrainSurface` écrit la surface visible et sa profondeur
dans des cibles dédiées (`Cull Back`, `ZWrite On`), puis un composite compare
profondeur cerveau/scène avant les transparents ordinaires. Cette passe de
`HBPEdgeRendererFeature` est exécutée pour les caméras Game même si Edges est off.
Elle évite de composer indistinctement toutes les couches du cerveau. Ne pas
supprimer la feature en supposant qu’elle ne porte que les contours.

La passe Edges optionnelle s’exécute avant post-processing : discontinuités
profondeur/normales sur cerveau/coupes opaques et silhouette externe des
transparents. Les tags `HBPEdgeData`/`HBPEdgeMask` excluent sites, ROI et repères
interactifs. Les repères conservent leur occultation normale. Une variation de
pente, plutôt qu’une pente régulière, déclenche les contours de profondeur.
Les données de passe sont isolées par caméra; le composite préserve l’alpha.

La ROI réelle est une **cage analytique**, malgré le nom du fichier
`ROI/HBPROIWireframe.shader` : sept cercles, silhouette, sous-trait contrasté et
épaisseur stabilisée par `fwidth`, sans geometry shader ni besoin de barycentriques.
Elle utilise un mesh indexé compact.

Les sites conservent un petit mesh partagé, shader RGBA spécialisé, matériaux
par état et objets/colliders individuels pour le picking. Le shader accepte une
variante d’instancing, ce qui ne signifie pas que le renderer produit est
instancié. Une refonte doit préserver sélection, hover, filtres, taille et
visibilité par colonne; les mesures historiques expliquent le maintien du
chemin simple.

## Vues, ressources et export

Les RenderTextures de vue ont un descripteur explicite RGBA8 sRGB, D24/S8,
MSAA 1, sans mipmaps ni random-write. La taille suit les pixels physiques après
scaling Canvas. Réutiliser un descripteur compatible; libérer/détruire au
remplacement, masquage, minimisation ou destruction. Une vue inactive arrête
sa caméra; aucune cible de dimension nulle n’est allouée.

L’export utilise les mêmes caméras, shaders et état qu’une vue équivalente.
PNG individuel : fond alpha zéro, résolution demandée (2048² par défaut),
straight alpha. Pour une cible sRGB prémultipliée, décoder vers Linear, diviser
RGB par alpha, clamp/réencoder sRGB; RGB zéro si alpha zéro. Diviser directement
les octets sRGB ou déprémultiplier deux fois crée des halos. Les composites
historiques gardent le fond opaque `#282828`.

Les blocs `finally` restaurent cible/aspect/fond caméra, scroll de matrice et
indices temporels; ils libèrent captures et vidéo. Une frame JPEG écrit sa
longueur courante, pas la capacité d’un ancien buffer.

## Sources et qualification

[Code de rendu](../../../Assets/Scripts/HBP/Rendering/),
[shaders](../../../Assets/Shaders/HBP/),
[tests de rendu](../../../Assets/Tests/EditMode/HBP.Rendering.Tests/),
[protocole et mesures historiques](rendering-validation.md).
Les [contrats de projection](../activity-projection.md) et
[d’inflation](../surface-inflation.md) expliquent la géométrie scientifique.

La qualification Windows de la migration date d’août 2026. Elle ne prouve pas
la qualification actuelle de tous les desktops ou du casque; la compilation
Metal ne vaut pas un essai matériel. Les anciens reports de VR concernent la
migration desktop passée, pas le périmètre actuel de Quest.
