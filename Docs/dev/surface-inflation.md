# Inflation : représentation affichée et référence anatomique

L’inflation est implémentée comme une représentation dérivée du `Mesh3D`
sélectionné, générée à la demande et conservée en mémoire par ce mesh. Elle ne
crée pas une seconde anatomie dans la liste des ressources. L’admissibilité est
géométrique; le nom du mesh ne garantit pas sa nature corticale.

## Contrat scientifique

| Propriété | Anatomique | Inflated / transition |
|---|---|---|
| `BrainSurface`, centrage caméra | Surface anatomique | Géométrie affichée dérivée |
| `ReferenceSurface`, activité, atlas, fMRI, mesures | Anatomique | Anatomique |
| Sites/électrodes | Coordonnées anatomiques | Mêmes coordonnées, visibles et sélectionnables |
| Coupes et colliders de coupe | Anatomiques | Anatomiques et éditables |
| Clipping de l’enveloppe cérébrale | Coupes courantes | Aucun |

Les UV/labels/couleurs scientifiques sont transportés par les mêmes indices de
sommets; on ne rééchantillonne pas les données aux coordonnées gonflées. Une
bascule de représentation conserve grille, champ d’activité et timeline. Les
meshes de section anatomique sont masqués dans la vue principale inflated,
mais restent disponibles dans les vues de coupe dédiées. Au retour, ce sont
les **coupes courantes**, y compris leurs modifications récentes, qui découpent
l’anatomie.

Les masques complets peuvent être conservés seulement à topologie identique;
une surface simplifiée indépendamment n’hérite pas aveuglément de leurs indices.

## Entrée, algorithme et cache

Le calcul version **2** consomme les buffers anatomiques complets déjà chargés
et transformés (`CurrentSurfaceCoordinates`) : MNI, GIfTI patient ou preview IRM.
Il ne relit pas le fichier, ne gonfle pas une surface déjà gonflée ou tronquée,
ne réapplique pas le `.trm` et ne conserve pas une copie permanente dans un ancien
repère. Une transformation anisotrope modifie le résultat numérique; ce choix
est accepté pour partager le chemin Desktop/Quest.

Pour un mesh gauche/droite, les hémisphères sont calculés séparément; ils ne
sont publiés qu’ensemble, puis fusionnés et simplifiés. Le cache inclut identité
et `GeometryVersion` des sources, version d’algorithme et tous les paramètres.
Un résultat devenu obsolète pendant le travail n’est pas publié.

Le preset `Inflated` courant utilise `MetricRegularized`, conservation isotrope
du rayon RMS, 160 itérations, lissage 0,35, régularisation 0,12, pas maximal
0,20, tolérance `1e-5`, huit retours arrière au maximum et frontières fixes.
Le prototype `NonShrinkingSmoothing` reste disponible comme baseline technique.
Les composantes connexes sont traitées autour de leur propre centre; petits
composants et sommets isolés ne sont pas supprimés silencieusement. Les entrées
non finies, indices invalides, dégénérescences et topologies non-manifold sont
refusées avec diagnostic. Seules positions et normales changent : ordre des
sommets, triangles, UV, couleurs et visibilité compatible sont préservés.

L’implémentation géométrique est indépendante; Workbench est un oracle externe,
pas une dépendance runtime. Une correspondance topologique ne démontre ni
équivalence numérique avec Workbench/FreeSurfer, ni validité anatomique d’un
mesh quelconque. Les états interpolés sont purement visuels.

## Exécution, interaction et persistance

Le job natif prend un snapshot, s’exécute une seule fois sur un worker et expose
progression/annulation thread-safe. `take_result` transfère un résultat possédé
une seule fois. L’annulation ne publie pas de résultat partiel; le wrapper attend
la fin du worker avant de détruire le job. Les représentations restent attachées
à leur owner et sont libérées avec lui.

`SurfaceRepresentation` est sérialisée; les anciens projets prennent
`Anatomical` par défaut. La première coexistence inflated/coupes affiche un seul
message informatif OK par visualisation, qu’elle résulte d’une inflation avec
coupes ou de la première coupe créée en inflated. Ce marqueur appartient à
l’instance de scène et n’est pas sérialisé. Elle n’interdit pas les coupes.

La transition GPU interpole positions et normales préparées dans les passes
couleur, profondeur, normales et contours. Elle conserve les attributs
scientifiques, suspend les interactions dépendant précisément de la géométrie
et actualise le collider final. Les jobs partagés et leur publication coordonnée
relèvent de la [synchronisation](quest-autonomous/sync/synchronization-architecture.md).

Une transition annulée avant publication conserve la représentation acceptée
précédemment. Le clipping reste désactivé pendant la transition; sa restauration
relit la liste courante des coupes, y compris celles ajoutées ou retirées depuis
le début de l’animation. Les six états de l’ancienne spécification correspondaient
aux combinaisons représentation/coupes et aux deux directions de transition,
pas à six valeurs de représentation persistées.

Les anciennes valeurs d’enum `NativeGifti`/`NativeGiftiThenTransformed` restent
reconnues pour rejeter explicitement une représentation préparée incompatible
et demander sa préparation/livraison à nouveau. Le conteneur de scène reste en
version 4; Desktop et Quest doivent utiliser le contrat de calcul version 2.

La projection des électrodes vers l’inflated, le cache disque et l’export GIfTI
avec métadonnées arbitraires restent des extensions non livrées par ce chemin.

## Sources et vérification

- [Surface / job / options](../../Assets/Scripts/HBP/Core/DLL/Surface.cs),
  [Mesh3D / cache](../../Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs),
  [MeshManager](../../Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs),
  [Base3DScene](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs).
- Natifs : `hbp_core/src/surface/surface_inflation.cpp`,
  `src/api/surface_inflation_api.cpp` et tests natifs d’inflation.
- Tests EditMode `SurfaceInflationPhase3Tests` à `SurfaceInflationPhase7Tests`
  sous `Assets/Tests/EditMode/HBP.Serialization.Tests/` : layouts, ownership,
  annulation, source transformée sans fichiers, cache, publication atomique,
  surfaces de référence, sérialisation, transition et visibilité des coupes.

La couverture du comportement produit appartient à ces tests, indépendamment
du corpus géométrique : `SurfaceInflationPhase5Tests` vérifie référence anatomique
et clipping, `SurfaceInflationPhase6Tests` la valeur par défaut et le message
unique, `SurfaceInflationPhase7Tests` les UV/couleurs et vues de coupe. Le scénario
d’inflation de
[SceneRestorationPlayModeTests](../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/SceneRestorationPlayModeTests.cs)
vérifie notamment le retour à Anatomical et la conservation des masques après
annulation d’une animation synchronisée. Leur présence ne vaut pas exécution
récente de ces tests.

Le [corpus historique conservé](../../Tools/InflationReference/reference-corpus.md) documente MNI, fixtures et
comparaisons Workbench reproductibles. Il n’établit pas une qualification actuelle
sur un corpus patient réel. Les anciens plans de phases ne sont plus des
instructions d’implémentation ni la preuve que tous les matériels ont été testés.
