# Frontière de coordonnées hbp_core / Unity

Les lecteurs natifs normalisent leurs formats dans le repère scientifique
natif droitier. La frontière HiBoP vers la scène Unity gauchère applique une
seule réflexion : `R = diag(-1, 1, 1)`, donc `p_unity = R * p_native` et
`p_native = R * p_unity`. Ce n’est pas une affirmation universelle sur les
coordonnées brutes de chaque format d’entrée.

| Donnée | Conversion |
|---|---|
| Points, directions, normales, sommets | Réflexion X une fois |
| Triangles | Inversion du winding une fois avec la réflexion |
| Bounding box | Conversion puis recalcul min/max |
| Affine | `A_unity = R * A_native * R` |
| Rotation | Matrice conjuguée; pas un changement arbitraire de composante quaternion |
| Distances, rayons, dimensions, spacing, indices, UV, couleurs, labels | Aucune réflexion |

`Vector3` est Unity par défaut; `Vec3`/`hbp_Vec3` sont natifs. Les conversions
`Vec3.FromVector3` et `ToVector3` reflètent X. Les exceptions explicites actuelles
à `convertReferenceSystem` sont `RawSiteList.AddSite(nativePosition)`,
`RawSiteList.GetNativePositions()` (coordonnées scientifiques natives) et
`Volume.Spacing` (magnitudes non signées). Un `Vector3` temporairement natif doit
être identifiable comme tel; les calculs ne mélangent jamais les deux repères.

La production charge `hbp_core` depuis `Assets/Plugins/Native/` par plateforme.
`Assets/Plugins/Editor/LegacyNative/` conserve `hbp_export` comme oracle de tests,
sans fallback runtime. Ne pas appliquer une adaptation legacy à un résultat
déjà converti par le wrapper actuel.

Références : [types de valeur](../../Assets/Scripts/HBP/Core/DLL/HbpCoreValueTypes.cs),
[transformations](../../Assets/Scripts/HBP/Core/DLL/Transformation3.cs),
[bounds](../../Assets/Scripts/HBP/Core/DLL/BBox.cs),
[sites natifs](../../Assets/Scripts/HBP/Core/DLL/Electrodes.cs),
[contrat du dépôt natif voisin](../../../hbp_core/docs/coordinate_system_contract.md).
Vérifier bounds asymétriques, winding/normales, affines obliques et distances
site/coupe; une sphère symétrique ne détecte pas une double réflexion.
