# Audit du rendu actuel

## 1. Résumé exécutif

HiBoP utilise encore le Built-in Render Pipeline, malgré quelques références
partielles ou obsolètes à des assets SRP. Le rendu 3D est distribué entre des
Surface Shaders custom, des shaders vertex/fragment très simples, des matériaux
créés ou clonés au runtime, des caméras rendant dans des RenderTextures et un
effet de contours basé sur Post-processing Stack v2.

La migration n'est donc pas une conversion automatique de matériaux. Les
éléments déterminants — cerveau, transparence, clipping, activité, atlas,
sites, contours et wireframe ROI — sont custom ou liés à PPv2.

L'écart de couleur entre surface du cerveau et coupes est expliqué par au moins
deux mécanismes indépendants :

1. la surface mélange les couleurs d'atlas ou d'activité avant un éclairage
   `Standard`, alors que les coupes utilisent un rendu unlit ;
2. le projet est en Linear, mais une couleur issue d'un vertex color et la même
   valeur issue d'une texture sRGB ne suivent pas nécessairement la même
   conversion. Cette hypothèse doit être confirmée par un test de patchs de
   couleur avant de modifier les palettes.

## 2. Configuration globale

### Faits vérifiés

- `ProjectSettings/GraphicsSettings.asset` contient
  `m_CustomRenderPipeline: {fileID: 0}` : le pipeline global est Built-in.
- `ProjectSettings/ProjectSettings.asset` contient `m_ActiveColorSpace: 1` :
  le projet est en Linear.
- `Packages/manifest.json` ne déclare pas URP.
- `Packages/manifest.json` déclare `com.unity.postprocessing` version `3.5.4`.
- `Packages/manifest.json` déclare l'effet
  `com.agm.edge-detection` depuis Git.
- Plusieurs niveaux de qualité et entrées de global settings référencent des
  GUID de pipeline absents de `Assets`. Ces références sont probablement
  anciennes ou incomplètes et doivent être nettoyées pendant la migration.
- Quelques matériaux pointent déjà vers le GUID connu du shader URP/Lit alors
  que le package URP n'est pas installé. Le projet est donc dans un état
  partiellement préparé, mais non cohérent.

### Conséquence

Il faut créer des assets URP propres et explicites. Réutiliser aveuglément les
références existantes risquerait de propager des réglages obsolètes.

## 3. Surface du cerveau

Le shader principal est `Assets/Resources/Shaders/MeshShader.shader`
(`Custom/Brain`). Il utilise :

- `#pragma surface surf Standard vertex:vert` ;
- trois jeux d'UV et plusieurs textures ;
- les vertex colors ;
- une extrusion des sommets via `_Amount` ;
- jusqu'à 20 plans de coupe via `_CutPoints` et `_CutNormals` ;
- `clip` pour le clipping géométrique ;
- des branches atlas et iEEG ;
- les paramètres métalliques et de smoothness du modèle Standard.

Le shader transparent actif est
`Assets/Resources/Shaders/TransparentMeshUncompiledShader.shader`. Il reprend la
même logique sous forme de Surface Shader alpha. Le fichier généré
`TransparentMeshShader.shader` ne doit pas être pris comme source d'architecture
tant que ses usages n'ont pas été vérifiés.

Les matériaux runtime sont centralisés par
`Assets/Scripts/HBP/Core/Object3D/BrainMaterial.cs`. Cette classe clone les
matériaux cerveau, cerveau transparent, coupe et coupe transparente, puis leur
fournit textures, drapeaux, alpha et tableaux de plans de coupe.

### Incompatibilités URP

- URP ne supporte pas les Surface Shaders Built-in.
- Le shader doit être réécrit avec des passes URP explicites.
- Le clipping doit être identique dans les passes Forward, DepthOnly,
  DepthNormals et ShadowCaster pour éviter contours, profondeur ou ombres
  incohérents.
- Le comportement transparent doit être caractérisé avant réécriture :
  profondeur, ordre, faces, clipping, contours et export.

## 4. Coupes et textures fonctionnelles

`Assets/Scripts/HBP/Data/Module3D/Modules/CutTexturesUtility.cs` coordonne les
textures de coupe. Des générateurs natifs remplissent les buffers RGBA pour :

- l'anatomie ;
- l'atlas ;
- le fMRI ;
- l'activité ;
- les textures GUI.

Le chemin actuel contient plusieurs copies CPU/GPU :

- remplissage natif ;
- `SetPixels32` ;
- `Apply` ;
- copie de la texture de base vers la texture fonctionnelle ;
- invalidation de plusieurs familles de textures.

Le matériau de coupe opaque utilise un shader unlit Built-in. Le matériau
transparent utilise `Assets/Resources/Shaders/UnlitTextureAlpha.shader`.

### Dette identifiée

Une modification de survol d'atlas appelle `UpdateAtlasColors()` dans
`Assets/Scripts/HBP/Data/Module3D/Modules/AtlasManager.cs`, réécrit les couleurs
du mesh principal et de chaque colonne, puis positionne
`BaseCutTexturesNeedUpdate = true`. Un simple survol peut donc déclencher bien
plus de travail que nécessaire. Ce comportement doit d'abord être mesuré, puis
remplacé dans la phase d'optimisation par une représentation à base d'identifiant
de région et palette GPU si le gain est confirmé.

## 5. Éclairage et caméra

`Assets/Prefabs/3D/3D.prefab` contient :

- une lumière directionnelle blanche ;
- intensité `1.2` ;
- ombres soft ;
- un spotlight partagé actuellement inactif.

`Assets/Scripts/HBP/Data/Module3D/Camera3D.cs` applique avant rendu :

- le mode et l'intensité ambiants ;
- une lumière ambiante plate autour de `0.2` ;
- l'orientation de la lumière globale d'après la caméra.

Ce dernier point produit un éclairage de type « headlight » relativement stable
pour l'anatomie. Il doit être reproduit pendant la phase de parité, même si une
autre direction artistique est envisagée ensuite.

Le prefab `Assets/Prefabs/3D/Scenes/View 3D.prefab` utilise :

- un fond gris `#282828` ;
- HDR autorisé sur la caméra ;
- MSAA désactivé sur la caméra ;
- un `PostProcessLayer`.

L'HDR doit être désactivé au début de la migration, sauf preuve qu'il est
nécessaire au rendu historique. Le tone mapping et le color grading doivent
rester désactivés pour ne pas altérer les couleurs scientifiques.

## 6. Multi-vues et RenderTextures

Chaque vue 3D rend normalement dans une RenderTexture. Dans
`Assets/Scripts/HBP/UI/Module3D/View3DUI.cs`, une nouvelle texture est créée lors
d'un changement de taille avec :

- largeur et hauteur égales au rectangle UI ;
- profondeur 24 bits ;
- `antiAliasing = 1`.

`Assets/Scripts/HBP/Data/Module3D/View3D.cs` libère l'ancienne RenderTexture mais
ne détruit pas explicitement son objet. Cette gestion doit être profilée pour
détecter allocations et pression mémoire lors des redimensionnements.

Dimensionnement fonctionnel fourni :

- charge réaliste haute : 8 colonnes × 3 vues = 24 caméras/vues ;
- limite théorique : 12 colonnes × 5 vues = 60 caméras/vues.

Le coût par caméra est donc un multiplicateur majeur, même si le GPU n'a pas
historiquement été le goulot d'étranglement.

## 7. Contours

Le rendu de contours repose sur :

- Post-processing Stack v2 ;
- un profil global ;
- `com.agm.edge-detection` ;
- une reconstruction à partir de profondeur/normales.

URP n'est pas compatible avec PPv2. L'effet doit être réimplémenté comme
Renderer Feature / Full Screen Pass URP. L'utilisateur autorise une
modernisation de son apparence, mais les fonctions suivantes doivent être
conservées :

- activation/désactivation ;
- contours lisibles sur cerveau opaque et transparent selon le comportement
  validé ;
- cohérence avec les plans de coupe ;
- coût compatible avec le nombre maximal de vues.

## 8. Sites

Le shader `Assets/Resources/Shaders/SiteShader.shader` est un vertex/fragment
unlit minimal : couleur uniforme, blending alpha et `ZWrite Off`.

L'architecture actuelle comporte :

- un `Site : MonoBehaviour` par site ;
- un `MeshFilter`, un `MeshRenderer` et un `SphereCollider` dans
  `Assets/Prefabs/3D/Objects/Site.prefab` ;
- un petit mesh partagé créé par `SharedMeshes.Site` ;
- une hiérarchie de sites clonée par colonne dans `Column3D.UpdateSites` ;
- des matériaux partagés pour les états connus ;
- un dictionnaire `Color -> Material` qui crée un matériau pour chaque nouvelle
  combinaison de couleur et surbrillance.

Cette architecture peut être limitée par le main thread, le culling, le nombre
de renderers, les colliders, les changements d'état ou les draw calls avant de
l'être par la complexité du fragment shader. Une optimisation sérieuse devra
mesurer chaque composante.

## 9. ROI et wireframe

Le wireframe ROI tiers utilise un geometry shader. Cette solution peut être
acceptable sur les desktops ciblés, mais elle constitue un risque pour WebGL,
certaines APIs graphiques et la VR. Une variante à coordonnées barycentriques,
un mesh d'arêtes ou un rendu alternatif doit être prévu avant de promettre la
compatibilité WebGL.

## 10. Export

L'export individuel dans
`Assets/Scripts/HBP/UI/Module3D/Scene3DWindow.cs` appelle :

`GetTexture(2048, 2048, new Color(0, 0, 0, 0))`.

Le fond transparent est donc un comportement explicite. Les exports composites
utilisent actuellement un fond opaque `#282828`. La migration doit préserver
ces deux cas.

`View3D.GetTexture` crée une RenderTexture temporaire, force `antiAliasing = 1`,
appelle `Camera.Render`, effectue un `ReadPixels`, puis libère la texture. Le
nouveau chemin doit définir explicitement le format colorimétrique, l'alpha,
l'anti-aliasing et la destruction des ressources temporaires.

## 11. Hypothèses à confirmer avant la première bascule

- Les palettes sources sont-elles définies conceptuellement en sRGB ? C'est
  l'hypothèse recommandée pour les valeurs éditées comme couleurs UI.
- Quelles variantes de casques et APIs XR doivent être prises en charge ?
- Le rendu transparent actuel dépend-il d'un ordre précis entre cerveau, sites,
  coupes et ROI ?
- Quels modes de contours sont réellement utilisés en production ?
- Le nombre de 30 000 sites désigne-t-il les sites source, les instances
  visibles toutes colonnes confondues, ou les deux selon les projets ?
- WebGL est-il une cible de livraison ou seulement une piste exploratoire ?

