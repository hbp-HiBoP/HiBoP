# Aperçus de maillage issus d’IRM

Une scène **mono-patient sans mesh patient persistant** génère automatiquement
une enveloppe approximative pour chaque IRM patient utilisable. Les meshes MNI
ne comptent pas comme reconstruction patient. Un GIfTI déclaré mais illisible
reste une erreur de chargement; le fallback ne masque pas sa corruption.
Les scènes multi-patients MNI ne génèrent pas de previews individuels.

## Chargement, identité et durée de vie

Après les IRM et avant les sites, `Base3DScene.EnsureRuntimePatientMeshAsync`
extrait la surface sur un worker puis enregistre le résultat sur le thread Unity.
L’annulation est vérifiée avant et après l’appel natif synchrone : elle n’interrompt
pas MC33 en cours, mais libère toute sortie non publiée. Un échec est journalisé
et n’empêche pas les autres IRM de produire leur aperçu.

`RuntimeSingleMesh3D` conserve sa `SourceMRI`, le rapport et deux surfaces
complète/simplifiée aux handles distincts. L’identité de l’IRM, pas son nom,
empêche les doublons. Le mesh est détenu par la scène, libéré par `Clean`, sans
clone implicite de handles. Il n’entre ni dans `Patient.Meshes`, ni dans les
préchargements persistants, ni dans BIDS. Aucun GIfTI, chemin fictif ou cache
disque n’est produit; sa sélection transitoire n’est pas sauvegardée comme mesh
persistant.

La sélection initiale privilégie un mesh chargé explicitement configuré, puis
la préférence patient; faute de mesh patient, le preview de l’IRM configurée,
le premier preview réussi, puis MNI. Aucune heuristique sur le nom des IRM.

## Extraction native

`Volume.ExtractPreviewSurface` appelle `hbp_volume_extract_preview_surface`.
Les structs ABI portent `struct_size`; options nulles signifie défauts. La
sortie est initialisée à null, transférée au caller seulement sur succès et
libérée par `hbp_surface_destroy`. Un volume vide/null est `HBP_INVALID_HANDLE`;
options/layout invalides sont `HBP_INVALID_ARGUMENT`; échec géométrique avec
message natif ne publie pas de surface.

Le pipeline courant :

1. Intensités finies, bornes robustes 0,5/99,5 percentiles; seuil Otsu, absolu
   ou normalisé dans ces bornes.
2. Réduction trilinéaire préservant les centres extrêmes; dimension maximale
   par défaut 160, cible 20 000 triangles.
3. Masque, composante principale 26-connexe, fermeture binaire, remplissage
   6-connexe des cavités, bordure de fond et lissage scalaire.
4. MC33 sur grille mémoire; retrait de la bordure, retour vers IJK source puis
   application de l’affine NIfTI **une fois**. Un déterminant négatif corrige le
   winding natif avant la conversion Unity séparée.
5. Contrôle des indices, nettoyage des triangles dégénérés, simplification et
   normales; conservation du rapport de dimensions, seuil, comptes et durées.

Bornes actives : dimension maximale 32–256, cible 1 000–200 000 triangles,
occupation 0,5–90 %, masque au plus 256³, grille bordée 258³, au plus deux
millions de triangles MC33 intermédiaires et comptes compatibles int32.
Ces limites appartiennent au **preview**, pas à toutes les données scientifiques.

MC33++ 5.4 est une dépendance privée statique du dépôt natif, figée au commit
`252dabd7ea6dea3d0529e66b047adbe878caf2fa`. Les lecteurs et fonctions OpenGL ne
font pas partie du chemin d’extraction. `USE_MM_RSQRT_SS=0` évite une dépendance
x86; `GRD_ORTHOGONAL` n’est pas défini à cause d’une incompatibilité MSVC du
commit upstream. Les raisons, licence et notices sont dans
`hbp_core/third_party/mc33/UPSTREAM.md` et `third_party/components.json`.

## Portée scientifique et limites

L’enveloppe peut représenter la tête plutôt que le cortex : ce n’est ni un skull
stripping, ni une segmentation, ni une reconstruction corticale qualifiée.
Elle propose seulement `Both`, sans hémisphères, MarsAtlas ou ressources MNI.
La sélection applique ces capacités et le CCEP MarsAtlas revient au mode Site.
Activité, coupes, ROI, eraser et fMRI patient restent soumis à l’alignement réel
des données et à la [couverture de projection](activity-projection.md).

Après chargement de configuration, le diagnostic mesure les distances des sites
au **sommet** le plus proche (pas à la surface triangulaire exacte), P50/P90/P95.
Si au moins 25 % dépassent le plus petit rayon d’influence actif, un log suggère
`ceil(P90 + 2 mm)`. Aucun rayon n’est modifié automatiquement.

La régénération manuelle avec seuil/profils et le réglage global d’activation
étaient des propositions; ce chemin automatique ne les implémente pas. Leur
absence ne constitue pas à elle seule un backlog accepté.

## Sources et validation

- [Modèle runtime et diagnostic](../../Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs),
  [gestionnaire](../../Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs),
  [chargement](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs),
  [wrapper](../../Assets/Scripts/HBP/Core/DLL/Volume.cs).
- Natifs : `hbp_core/src/meshing/preview_surface_extractor.cpp`,
  `src/api/preview_surface_api.cpp`,
  `tests/native/hbp_core_preview_surface_functional_test.cpp`.
- [Tests runtime](../../Assets/Tests/EditMode/HBP.Serialization.Tests/PreviewSurfaceRuntimeMeshTests.cs)
  et [scènes PlayMode](../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs).

Les oracles couvrent affines anisotropes/négatives, NaN/Inf, topologie, erreurs,
ownership, sélection multi-IRM et séparation persistante/runtime. Pour une
inspection anatomique indépendante, l’outil natif `hbp_core_preview_surface_export`
produit un OBJ `SPACE=RAS` et un rapport à superposer au NIfTI dans 3D Slicer;
voir `hbp_core/docs/preview_surface_visual_validation.md`.

Mesure historique de juillet 2026 : fantôme `160 × 192 × 144`, réduit à
`107 × 128 × 96`, environ 0,30 s sur la machine Windows de développement,
62 600 → 20 000 triangles. CPU non consigné ici : ni budget multiplateforme,
ni qualification clinique. Les anciens rapports annonçaient des validations
Windows; la qualification matérielle actuelle n’a pas été rejouée pour cette
consolidation documentaire.
