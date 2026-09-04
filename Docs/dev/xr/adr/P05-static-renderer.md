# ADR P05 — renderer statique de surface XR

- **Statut :** CANDIDATE NON GELÉE — P05-A–C validées, P05-D non conforme sur Quest 3
- **Date :** 2026-09-02
- **Baseline :** branche courante, commit parent `43afdbf186581bb4b3e341c7bbc014d3f9984b45`
- **Projet :** `XR/`, Unity `6000.5.2f1`, URP `17.5.0`
- **Gate restant à fermer :** composition alpha URP/OpenXR/Meta sur Quest 3

Le renderer candidat consomme uniquement des `SurfaceAsset` P03 déjà construits en mémoire. Son assembly runtime référence `CRNL.HiBoP.RenderModel` et `CRNL.HiBoP.Contracts`; il ne référence ni Core, ni Data, ni Protocol, ni Bootstrap, ni API réseau ou native.

## P05-A — shader et matériau Android

Réécrire dans l'assembly XR un shader URP minimal plutôt que copier l'ensemble du shader cerveau Desktop. La fonction statique reprend exactement `HBP_ApplySurfaceLighting` : facing vue/normale, ambiance `0,35`, diffuse `0,65`, puissance spéculaire `8→40` et intensité `0,36 × smoothness` avec smoothness `0,45`.

Les shaders opaques et transparents ont un seul pass `UniversalForward`, supportent l'instancing/stéréo URP, ciblent Shader Model 3.5 et déclarent `Fallback Off`. Le build Android Vulkan compile quatre programmes internes uniques pour chacun sans erreur. Aucun shader de substitution n'est autorisé.

Le renderer n'emploie pas les lumières, ombres, probes ou motion vectors Unity. La couleur par surface passe par `MaterialPropertyBlock`; les deux matériaux de référence sont partagés et ne sont jamais clonés par instance.

## P05-B — couleur, vertices et indices

- projet et RenderTexture de comparaison en espace `Linear`;
- couleurs auteur sRGB converties par Unity en linéaire avant la fonction d'éclairage, comme sur Desktop;
- positions, normales et UV uploadés en `float32` sans quantification;
- coordonnées P03 acceptées seulement en XYZ gauche, millimètres, mapping V1 identité; conversion unique millimètres → mètres à l'upload;
- indices `UInt16` jusqu'à 65 535 sommets inclus, `UInt32` au-delà;
- bounds P03 contrôlés contre les positions avant conversion, normales unitaires à `1e-3`;
- copie CPU du `Mesh` abandonnée après upload avec `UploadMeshData(true)`.

## P05-C — anatomical et inflated

`Anatomical` et `Inflated` sont deux `SurfaceAsset` immuables et distincts, chacun avec son hash, sa représentation, ses positions, normales, UV et bounds. P05 n'introduit ni attribut de morph, ni interpolation, ni clone implicite de topologie. Un changement vers un blend GPU rouvrirait P03/P05.

La scène de démonstration ne fabrique aucune géométrie. Elle désérialise deux `TextAsset` binaires locaux, générés au build depuis `MNI_Lhemi.gii` + `MNI_Rhemi.gii` et `MNI_Lwhite_inflated.gii` + `MNI_Rwhite_inflated.gii`, avec `MNI.trm`. L'outil Desktop est seul autorisé à charger les GIFTI et projette leur résultat via `DesktopSurfaceRenderModelAdapter`; l'assembly XR reçoit uniquement les buffers P03 et vérifie leur SHA-256. Les blobs générés sont ignorés par Git, reproductibles et embarqués comme données locales.

Après fusion des hémisphères, l'anatomique contient 69 104 sommets et 138 216 triangles (`ab8794d4…795d135`), l'inflated 66 299 sommets et 132 590 triangles (`fd029198…f4e80`). Les bounds sont recalculés exactement depuis les positions sérialisées afin de conserver l'invariant P03 sans élargir la tolérance runtime.

## P05-D — transparence et ordre

La baseline Desktop ne repose pas sur un simple matériau alpha : `HBPEdgeRendererFeature` rend d'abord la surface cérébrale la plus proche dans des textures couleur/profondeur, puis la compose avant les autres transparents. Pour le périmètre P05 limité aux surfaces statiques, l'adapter XR candidat retient deux passes sérialisées partageant le même `Mesh` :

- opaque par défaut : queue Geometry, `Cull Back`, `ZWrite On`;
- prépasse profondeur transparente : queue `Transparent-1`, `ColorMask 0`, `Cull Back`, `ZWrite On`, `ZTest LEqual`;
- passe couleur transparente : queue Transparent, couleur prémultipliée, `Blend One OneMinusSrcAlpha`, `Cull Back`, `ZWrite Off`, `ZTest Equal`;
- ordre inter-objets transparent sérialisé par `sortingOrder`;
- la prépasse stabilise la face visible sans accumulation des triangles internes.

Cette simplification élimine bien l'accumulation de triangles observée sur le casque. Le golden hôte sur fond coloré confirme numériquement un alpha de `0,25`. En revanche, trois contrôles visuels sur Quest 3 montrent encore l'inflated bleu comme opaque par rapport au passthrough. L'activation de `PlayerSettings.preserveFramebufferAlpha`, de la sortie alpha URP et du chemin prémultiplié attendu par Meta OpenXR n'a pas modifié ce résultat physique. P05-D reste donc **non résolue**; elle doit être reprise comme investigation ciblée de la composition de la projection OpenXR, et non par de nouveaux ajustements empiriques du matériau.

## Propriété et mémoire

Un cache par `AssetHash` détient exactement un `Mesh` Unity par surface. Chaque renderer acquiert un lease; le dernier `Dispose` détruit le mesh. Les tests vérifient le partage entre instances, l'absence de clone et 256 cycles création/libération sans mesh résident.

## Décision de gel

P05-A–D sont enregistrées et l'implémentation est une candidate reproductible. Les goldens D1 réels passent sur six vues fixes Desktop/XR à 512 × 512 : IoU de silhouette 1, erreur maximale 2/255 et erreur moyenne maximale `0,000141817` en Linear. Le comparateur refuse explicitement une image noire et archive les PNG/raw sous `.artifacts/xr/p05/d1-golden/`.

Elle **n'est pas gelée pour la production**. Le profil Quest 3 confirme Vulkan/Linear, la cadence 72 Hz, la mémoire, le GC, la charge GPU par compteur système, l'absence de throttling et la libération après fermeture. Le contrôle visuel confirme l'orientation, l'échelle, le placement côte à côte, l'anatomical opaque et la disparition de l'accumulation triangulaire. Il invalide toutefois la transparence perceptible de l'inflated sur passthrough.

Le gel reste interdit tant qu'un test physique discriminant, par exemple alpha `0`/`0,5` avec capture ou inspection de la swapchain, n'a pas identifié et corrigé la perte d'alpha entre la sortie caméra URP et la projection OpenXR.

## Réouverture

Réouvrir cet ADR si le format P03 change, si une conversion de repère autre que P03 V1 est nécessaire, si D1 exige une variante visuelle, si le tri transparent est insuffisant ou si le profil Quest dépasse le budget accepté.
