# Plan complet d'implémentation de la migration URP

## 1. Statut du document

Ce document est la spécification opérationnelle canonique de la migration du
rendu de HiBoP depuis le Built-in Render Pipeline vers URP. Il consolide
l'audit, le contrat visuel, l'architecture cible, la validation et les décisions
prises avec le responsable du projet les 5 et 6 août 2026.

Il ne reste aucun arbitrage produit bloquant avant l'implémentation. Lorsqu'une
mesure est encore nécessaire, ce document donne le protocole et la règle de
décision ; la mesure ne constitue donc pas une question d'architecture ouverte.

La migration se fait sur la branche courante. Codex ne crée pas de branche, ne
change pas de HEAD, ne stage pas, ne commit pas et ne pousse pas. Le responsable
du projet gère Git et peut changer de HEAD pour les comparaisons Built-in/URP.

## 2. Objectif et ordre des priorités

La migration est réussie si HiBoP fonctionne sous URP sur les desktops ciblés,
sans perte fonctionnelle ni régression scientifique ou de performance
significative.

En cas de conflit, appliquer cet ordre :

1. exactitude des couleurs et des données scientifiques ;
2. conservation des fonctionnalités et des exports ;
3. performances dans les usages réels ;
4. lisibilité anatomique, en particulier des sillons ;
5. proximité esthétique avec le rendu Built-in ;
6. améliorations visuelles non nécessaires à la migration.

Une reproduction pixel par pixel globale du Built-in n'est pas demandée. Elle
est même indésirable si elle reproduit une conversion colorimétrique incorrecte,
un artefact de transparence ou une limitation du pipeline historique.

## 3. Périmètre

### Inclus dans la migration

- ajout et configuration d'URP pour Unity `6000.5.2f1` ;
- migration de tous les matériaux et shaders actifs ;
- cerveau opaque et transparent ;
- anatomie, atlas, fMRI, iEEG, MEG et autres overlays scientifiques existants ;
- extrusion et jusqu'à 20 plans de clipping ;
- coupes opaques et transparentes ;
- sites, états, filtres, survol, sélection et picking existants ;
- sphères de ROI et leur wireframe ;
- caméras multi-vues et RenderTextures ;
- Edges sur le cerveau et les coupes ;
- exports PNG individuels transparents, composites et vidéo ;
- tests colorimétriques, fonctionnels, mémoire et performance ;
- validation Windows, macOS Apple Silicon et Linux.

### Explicitement reporté

- remplacement de la projection d'activité actuelle par un échantillonnage GPU
  direct d'un volume 3D ;
- refonte générale des générateurs natifs ;
- optimisation atlas par identifiant de région et palette GPU, sauf si une
  régression de migration l'impose ;
- amélioration parfaite de la continuité spatiale surface/voxel ;
- certification VR ;
- support WebGL ;
- HDR, tone mapping, color grading, Deferred ou Forward+ ;
- ombres temps réel ;
- modernisation esthétique sans rapport avec URP.

VR reste un chantier séparé. La migration ne doit pas volontairement empêcher
un futur portage XR, mais aucune gate de cette migration ne dépend d'un casque ou
d'un runtime non spécifié. WebGL ne doit influencer aucun choix de cette
livraison.

## 4. Décisions définitives

| Sujet | Décision |
| --- | --- |
| Pipeline | URP `17.5.0`, Universal Renderer, Forward, Render Graph actif |
| Bascule | globale, sans mode runtime Built-in/URP et sans double jeu de scènes |
| Couleur | projet Linear ; palettes et couleurs éditées interprétées en sRGB |
| Sortie | SDR/LDR RGBA8 ; HDR, tone mapping et color grading désactivés |
| Éclairage | anatomie éclairée par un modèle léger caméra-relatif, sans shadow map |
| Données scientifiques | compositées après l'éclairage anatomique et non éclairées |
| Activité surface | transport actuel par UV/valeurs de sommets conservé |
| Activité coupe | buffers RGBA actuels des générateurs conservés |
| Transparence | alpha blending classique ; sites, coupes et ROI restent lisibles à travers le cerveau |
| Faces transparentes | `Cull Back`, donc faces avant seulement au premier portage |
| Edges opaques | profondeur + normales, cerveau et coupes uniquement |
| Edges transparents | silhouette extérieure uniquement, cerveau et coupes uniquement |
| Edges exportés | identiques à l'état de la vue ; aucun fond noir ajouté au PNG |
| ROI | wireframe barycentrique URP, sans geometry shader |
| Sites | shader unlit minimal ; performance très prioritaire sur la qualité géométrique |
| RenderTextures | RGBA8 sRGB, profondeur/stencil 24/8, MSAA 1× initial |
| Export individuel | 2048×2048 par défaut, fond `(0,0,0,0)`, PNG straight alpha |
| Composite/vidéo | fonctionnalités conservées, fond historique `#282828` lorsque applicable |
| macOS | Apple Silicon, Metal, version minimale actuelle du projet macOS 12.0 |
| Linux | Vulkan en premier choix ; OpenGL Core seulement comme fallback après test |
| Windows | cible de développement initiale ; API existante conservée au premier portage |
| Performance | objectif <= Built-in ; investigation >5 %, refus si régression soutenue >10 % |
| Référence réelle | projet `visu_full_test`, visualisation `Small`, lorsque disponible localement |

## 5. Architecture cible

### 5.1 Assets URP

Créer des assets sérialisés, référencés explicitement par les niveaux de qualité :

```text
Assets/Settings/Rendering/
  HBP-Desktop-URP.asset
  HBP-Desktop-Renderer.asset
```

Les noms peuvent être adaptés aux conventions existantes, mais il doit exister
un propriétaire évident pour chaque asset. Les références SRP historiques
cassées sont supprimées. Aucun asset ou `GameObject` de remplacement n'est créé
au runtime pour compenser une référence manquante.

Configuration initiale :

- Forward ;
- Render Graph activé ;
- HDR désactivé ;
- MSAA pipeline désactivé ou 1× ;
- main light sans ombres ;
- additional lights désactivées ;
- opaque texture désactivée ;
- depth/depth-normals demandées uniquement par la Renderer Feature Edges ;
- post-processing URP désactivé hors Renderer Feature HBP ;
- SRP Batcher activé.

### 5.2 Shader cerveau

Créer un shader HLSL URP dédié, avec des includes partagés. Shader Graph n'est
pas utilisé pour ce shader central.

Organisation recommandée :

```text
Assets/Shaders/HBP/Brain/
  HBPBrain.shader
  HBPBrainTransparent.shader        # seulement si les états de rendu l'exigent
  Includes/
    HBPBrainInput.hlsl
    HBPBrainClipping.hlsl
    HBPBrainColor.hlsl
    HBPBrainLighting.hlsl
```

Fonctions obligatoires :

- tous les flux UV et vertex colors actuels ;
- anatomie et AO existantes ;
- atlas et activités ;
- extrusion ;
- 0, 1 et 20 plans de clipping ;
- opaque et transparent ;
- même calcul de clipping/extrusion dans les passes qui en dépendent.

Passes opaques :

- `UniversalForward` ;
- `DepthOnly` ;
- `DepthNormals`.

Passes transparentes :

- `UniversalForward`, `ZWrite Off`, `Cull Back` ;
- une passe de masque dédiée à la silhouette des Edges transparents.

Il n'y a pas de `ShadowCaster` dans la cible initiale, puisque les shadow maps
sont exclues. Une telle passe ne pourra être ajoutée plus tard qu'avec un besoin
visuel et un benchmark multi-vues.

Les propriétés stables par matériau utilisent `UnityPerMaterial`. Les tableaux
de clipping et états fréquemment modifiés conservent d'abord leur API actuelle ;
leur représentation n'est changée que sur preuve de coût.

### 5.3 Modèle anatomique et scientifique

Le fragment suit l'ordre normatif suivant :

```text
anatomie_linear = texture_anatomique × AO
anatomie_éclairée = éclairage_caméra_relatif(anatomie_linear, normale)
scientifique_linear = sRGBToLinear(palette_sRGB)
sortie_linear = lerp(anatomie_éclairée, scientifique_linear, alpha_scientifique)
```

L'éclairage anatomique utilise une contribution ambiante et une contribution
directionnelle caméra-relative bon marché. Un spéculaire discret peut être
conservé uniquement s'il aide la lecture sans coûter plus cher ni modifier les
overlays. La validation humaine des sillons décide du calibrage, pas la
reproduction exacte des constantes du shader Standard.

La couleur scientifique ne varie jamais avec la normale, la lumière ou la
caméra. Les palettes sont sRGB et sont converties exactement une fois. Les
textures de couleur sont sRGB ; les scalaires, indices, masques, UV et alpha
sont Linear.

Le facteur d'alpha implicite `× 2,5` du shader historique ne doit pas être
recopié tel quel. La formule cible est :

```text
alpha_final = saturate(alpha_source_normalisé × alpha_utilisateur)
```

Si la phase de caractérisation prouve que `alpha_source` n'est volontairement
pas normalisé sur `[0,1]`, la correction est déplacée à la frontière des données
dans un paramètre nommé, documenté et testé. Aucun multiplicateur magique ne
reste dans le shader.

### 5.4 Continuité surface/coupe

La migration conserve :

- `SurfaceGenerator` et ses UV/valeurs par sommet ;
- `CutGenerator` et ses pixels RGBA ;
- la fréquence et le modèle temporel existants.

Elle unifie le mapping, les seuils, la LUT, la conversion colorimétrique et
l'alpha. À des points monde comparables, la discontinuité ne doit pas être plus
forte que dans le Built-in. Une différence résiduelle due à l'interpolation
vertex sur le mesh et voxel sur la coupe est acceptée et documentée.

### 5.5 Coupes

Créer un shader URP unlit dédié, opaque et transparent selon les états de rendu.
Il partage la convention colorimétrique avec le cerveau et les légendes. La
génération native et les copies existantes ne sont pas refondues pendant le
portage.

- images anatomiques/continues : filtrage bilinéaire si c'est le comportement
  fonctionnel actuel ;
- indices/classes d'atlas : filtrage point ;
- aucune lumière, exposition, tone mapping ou correction indépendante ;
- clipping/masque cohérent avec les Edges ;
- sortie alpha explicite.

### 5.6 Transparence et alpha

Le tri conserve l'intention fonctionnelle suivante :

1. cerveau et coupes ;
2. ROI ;
3. sites et aides de lecture nécessaires.

Le but est que sites, coupes et ROI restent lisibles à travers le cerveau, sans
reproduire les artefacts exacts de tri du Built-in. Les changements d'angle et
les intersections font partie de la validation visuelle.

Les RenderTextures de vue et d'export conservent le canal alpha. Les shaders
écrivent explicitement RGBA. Le mélange transparent doit produire un alpha de
couverture correct, par exemple avec des facteurs RGB et alpha séparés. Le
buffer intermédiaire peut être prémultiplié pour composer correctement plusieurs
couches, mais le PNG final est encodé en **straight alpha** après une unique
conversion contrôlée. Cette règle évite les halos noirs lors de la réutilisation
du PNG sur un autre fond.

Tests obligatoires :

- fond totalement vide : RGBA `(0,0,0,0)` ;
- objet opaque : alpha 1 à l'intérieur ;
- objet à alpha 0,25/0,5/0,75 : alpha conforme hors bords ;
- recomposition du PNG sur fond blanc et `#282828` sans frange sombre ;
- plusieurs transparents superposés ;
- Edges on/off ;
- état caméra restauré même après exception.

### 5.7 Edges

Supprimer le chemin PPv2/AGM des vues migrées et créer une Renderer Feature URP
compatible Render Graph.

Elle possède deux sources, fusionnées dans un seul composite final :

1. profondeur + normales pour les contours complets du cerveau et des coupes
   opaques ;
2. masque mono-canal du cerveau et des coupes transparents pour leur silhouette
   extérieure uniquement.

Le masque transparent rend les objets éligibles avec le même clipping, sans
sites ni ROI. La feature filtre explicitement par layer/rendering layer ou
ShaderTag HBP ; elle ne se base pas sur toutes les géométries de la caméra.

La feature :

- est activable par vue ;
- n'utilise aucun état global dépendant de la dernière caméra ;
- alloue ses ressources temporaires via Render Graph ;
- ne demande depth/normals que lorsqu'elle est active ;
- conserve une épaisseur visuelle stable selon la résolution ;
- s'exécute aussi lors des exports ;
- écrit de l'alpha seulement aux pixels réellement dessinés sur un fond
  transparent.

### 5.8 ROI

Remplacer les shaders `Wireframe*.shader` à geometry stage par :

- un mesh de sphère partagé dont les sommets sont dupliqués par triangle ;
- coordonnées barycentriques stockées dans un canal libre ;
- un shader fragment URP utilisant les dérivées écran pour une épaisseur stable ;
- deux états sérialisés : normal et sélectionné.

La génération de `SharedMeshes.ROISphere` est intrinsèquement dynamique et peut
rester dans le code. Le prefab de sphère continue de porter ses Renderer,
Collider et références de matériaux. Le résultat doit fonctionner sous Metal.

### 5.9 Sites

Le port minimal est volontairement élémentaire :

- unlit ;
- couleur et alpha uniquement ;
- aucune normale requise par le shader ;
- aucune texture ;
- aucune lumière, ombre ou Edges ;
- aucun motion vector inutile ;
- petit mesh partagé existant conservé au premier passage.

La qualité du modelé des sites n'est pas une exigence. Un simple cercle coloré
est acceptable. La priorité est d'éviter tout surcoût avec 30 000 sites source
par colonne et un nombre potentiellement élevé de colonnes et de vues.

Ordre de décision :

1. porter le shader minimal sans changer l'architecture ;
2. mesurer le cas courant et un cas isolé à 30 000 sites ;
3. si la gate performance passe, reporter toute refonte ;
4. sinon, réduire matériaux/états et mises à jour inutiles ;
5. si cela ne suffit pas, rendre des cercles/quads instanciés par lots et
   séparer rendu et picking ;
6. conserver les `SphereCollider` au début du prototype instancié, puis les
   remplacer par une structure de picking uniquement si leur coût domine.

La solution instanciée doit préserver couleur, alpha, activité, highlight,
blacklist, filtres, sélection, visibilité par colonne et picking. Le cas extrême
de 30 000 sites dans neuf colonnes et 27 vues est légal et ne doit ni planter ni
fuir, mais il n'a pas d'objectif de fluidité.

La sélection active est principalement reflétée dans l'UI par
`UI/Toolbar/Site/SelectedSite.cs`. Les assets legacy `Select ring.prefab` et
`SiteSelectionShader.shader` ne sont pas référencés par les scripts ou prefabs
audités. Ne pas créer un nouvel indicateur 3D toujours visible ; vérifier une
fois le projet réel, puis supprimer ces assets uniquement dans le nettoyage
final s'ils sont confirmés inutilisés.

### 5.10 Caméras et RenderTextures

Centraliser la création dans une fabrique ou un gestionnaire testable. Le
descripteur initial est explicite :

- `R8G8B8A8_SRGB` ou équivalent RGBA8 sRGB supporté ;
- `D24_UNorm_S8_UInt` ou fallback documenté équivalent ;
- MSAA 1 ;
- mipmaps off ;
- random write off ;
- taille exacte de la vue ou de l'export.

Une texture est réutilisée tant que le descripteur ne change pas. Lors d'un
remplacement, le propriétaire la libère et détruit l'objet Unity. Une vue
réellement masquée ou minimisée ne rend pas si son image n'est consommée nulle
part. Le rendu à la demande des vues statiques est une optimisation ultérieure,
pas une condition du portage.

### 5.11 Export

L'écran, le PNG, le composite et la vidéo utilisent les mêmes caméras, shaders,
matériaux et Renderer Features. Aucun shader d'export parallèle n'est autorisé.

L'export individuel :

- utilise la résolution demandée, 2048×2048 par défaut ;
- fixe uniquement cible, aspect et clear color transparent ;
- rend avec Edges selon l'état de la vue ;
- lit les octets sRGB sans seconde conversion ;
- convertit le buffer prémultiplié vers straight alpha si nécessaire ;
- encode le PNG ;
- restaure cible, aspect, clear flags et couleur dans un `try/finally` ;
- détruit toute ressource temporaire.

Le composite conserve le fond `#282828`. La vidéo conserve son comportement et
son format actuels, avec le rendu URP comme source.

## 6. Phases d'implémentation

Chaque phase se termine par une gate. Une phase suivante peut être préparée,
mais aucun comportement dépendant ne doit être considéré fini avant la gate.

### Phase 0 — Baseline Built-in et fixtures

**But :** obtenir une référence suffisante sans transformer la migration en
campagne de comparaison pixel perfect.

Travaux :

- charger `visu_full_test` / `Small` si disponible sur la machine ;
- enregistrer configuration, caméra, résolution, couleurs, alpha, Edges,
  coupes, colonnes, vues et nombre de sites ;
- capturer anatomie opaque/transparente, activité, atlas, coupes, ROI, sites,
  Edges et tous les exports ;
- enregistrer un profil de performance du cas réel après warm-up ;
- ajouter une fixture synthétique de patchs sRGB/Linear et alpha ;
- relever la discontinuité surface/coupe existante à quelques points monde ;
- vérifier un cas 1×1 à 30 000 sites pour isoler le coût du rendu de sites.

Gate 0 :

- une capture réelle couvre chaque famille fonctionnelle ;
- les PNG individuels de référence ont un fond alpha zéro ;
- les métriques Built-in médiane/P95/P99 sont enregistrées ;
- les patchs déterministes peuvent être rejoués ;
- aucune décision ne dépend d'une capture non reproductible.

### Phase 1 — Fondation URP et code partagé

**But :** préparer la bascule globale en un changement cohérent.

Travaux :

- ajouter URP `17.5.0` ;
- créer et sérialiser pipeline asset et renderer data ;
- implémenter les helpers HLSL de clipping, couleur et éclairage ;
- créer les shaders minimaux coupes, sites et ROI ;
- créer la fabrique de `RenderTextureDescriptor` ;
- inventorier tous les matériaux actifs et leurs futurs shaders ;
- ajouter les tests EditMode des conversions, mappings et descripteurs.

Gate 1 :

- le projet compile avec le package ajouté ;
- tous les assets URP sont valides et référencés ;
- les tests de couleur et descripteurs passent ;
- l'inventaire ne contient aucun matériau actif sans stratégie de migration.

### Phase 2 — Cerveau, coupes et bascule globale

**But :** rendre les fonctions scientifiques principales directement selon le
contrat cible, sans porter d'abord les erreurs colorimétriques historiques.

Ordre :

1. cerveau anatomique opaque + AO + éclairage caméra-relatif ;
2. extrusion ;
3. clipping 0/1/20 plans dans Forward/Depth/DepthNormals ;
4. atlas et activités avec composition scientifique après éclairage ;
5. coupes opaques ;
6. variantes cerveau/coupes transparentes ;
7. assignation des matériaux et bascule globale Graphics/Quality vers URP.

Gate 2 :

- aucun matériau actif n'est magenta ;
- tous les modes scientifiques se chargent sans migration de données ;
- les sillons sont jugés lisibles ;
- lumière et normale ne changent pas le RGB scientifique ;
- mesh, coupe et légende respectent le même mapping ;
- clipping cohérent dans couleur/profondeur/normales ;
- sites, coupes et ROI restent visibles à travers le cerveau transparent ;
- la discontinuité surface/coupe n'est pas pire que la baseline.

### Phase 3 — ROI, sites et sélection

**But :** fermer tous les objets spécialisés avec le chemin le moins coûteux.

Travaux :

- brancher le wireframe barycentrique ROI normal/sélectionné ;
- vérifier animation, rayon d'influence et picking ROI ;
- brancher le shader site minimal ;
- vérifier tous les états de site, filtres, activités, alpha et picking ;
- confirmer dans `Small` que la sélection active est bien reflétée par l'UI ;
- profiler 30 000 sites en 1×1 ;
- n'engager l'instancing/billboard qu'en cas d'échec de la gate performance.

Gate 3 :

- ROI correct sous Windows et shader compilable Metal ;
- aucune dépendance active au geometry shader ROI ;
- sites fonctionnellement identiques ;
- le port minimal des sites ne régresse pas de plus de 10 % ;
- toute régression supérieure à 5 % est comprise et consignée.

### Phase 4 — Multi-vues, Edges et exports

**But :** rétablir le rendu complet de la vue jusqu'au fichier exporté.

Travaux :

- remplacer la gestion implicite des RenderTextures par les descripteurs
  explicites ;
- réutiliser/détruire correctement les textures ;
- implémenter la Renderer Feature Edges Render Graph ;
- brancher profondeur/normales opaques et masque de silhouettes transparentes ;
- retirer PPv2/AGM des prefabs de vue ;
- porter PNG individuel, composite et vidéo ;
- implémenter et tester la conversion straight alpha ;
- vérifier redimensionnement, fermeture et vues masquées.

Gate 4 :

- Edges uniquement sur cerveau/coupes ;
- contours complets opaques et silhouettes transparentes ;
- Edges identiques à l'écran et dans les exports ;
- fond du PNG individuel RGBA zéro ;
- aucun halo noir à la recomposition ;
- composite et vidéo fonctionnent ;
- 100 cycles de redimensionnement/export reviennent à un plateau mémoire ;
- aucune fuite d'état entre caméras.

### Phase 5 — Validation scientifique et performance intégrée

**But :** transformer le port fonctionnel en candidat de production.

Travaux :

- exécuter toute la matrice de `05-validation-and-reference-captures.md` ;
- faire valider visuellement `visu_full_test` / `Small` par le responsable ;
- comparer le cas courant Built-in/URP sur la même machine et résolution ;
- mesurer séparément Edges, transparence, sites et multi-vues ;
- corriger seulement les régressions mesurées ;
- si nécessaire, appliquer les optimisations conservatrices des sites ;
- tester le cas légal extrême seulement comme robustesse, sans objectif de FPS.

Gate 5 :

- aucun défaut scientifique ou fonctionnel ouvert ;
- validation humaine des sillons, transparence, Edges et ROI ;
- objectif de performance égal ou meilleur atteint dans le cas courant ;
- aucune régression P95 soutenue >10 % ;
- les écarts entre 5 et 10 % ont une cause et une décision consignées ;
- zéro allocation GC récurrente due au rendu dans une frame statique ;
- mémoire stable après fermeture des vues.

### Phase 6 — Plateformes desktop

**But :** fermer les trois plateformes de livraison.

Ordre :

1. Windows sur l'API existante ;
2. macOS 12+ Apple Silicon sous Metal ;
3. Linux sous Vulkan ;
4. OpenGL Core Linux uniquement si Vulkan échoue sur une machine réellement
   supportée.

Pour chaque plateforme : build propre, démarrage, `Small` ou fixture équivalente,
opaque/transparent, Edges, ROI, sites, clipping, palettes, PNG/composite/vidéo.

Règle Linux : Vulkan devient l'API supportée si la matrice passe. Si un échec
spécifique au driver rend Vulkan inutilisable, tester OpenGL Core ; ne conserver
le fallback que s'il passe toute la matrice et ne force pas de dégradation sur
les autres plateformes.

Gate 6 :

- builds Windows, macOS ARM64 et Linux réussis ;
- aucun shader manquant ou fallback rose ;
- patchs scientifiques dans la tolérance ;
- exports valides ;
- wireframe ROI fonctionnel sous Metal ;
- API Linux finale inscrite dans les Player Settings et le rapport.

### Phase 7 — Nettoyage

**But :** supprimer la dette Built-in seulement après la preuve de remplacement.

Travaux :

- supprimer les composants PPv2 actifs et les scripting defines associés ;
- retirer `com.unity.postprocessing` et `com.agm.edge-detection` si aucun autre
  consommateur n'existe ;
- supprimer les matériaux/shaders Built-in devenus inutiles ;
- confirmer puis supprimer les assets legacy de sélection 3D non référencés ;
- nettoyer les références SRP absentes ;
- mettre à jour l'audit et la documentation avec les chemins finaux ;
- exécuter builds et matrice une dernière fois après suppression.

Gate 7 :

- aucune dépendance runtime au Built-in ou à PPv2 ;
- aucun asset actif ne référence un shader supprimé ;
- builds propres sur les trois desktops ;
- documentation conforme à l'implémentation ;
- validation humaine finale obtenue.

## 7. Protocole de performance

### Cas qui porte la gate

Le cas principal est l'usage réel sauvegardé dans `visu_full_test` / `Small`, sur
la même machine, à la même résolution, avec le même état de caméra, d'activité,
de transparence et d'Edges. Si ce projet n'est pas disponible sur une machine,
utiliser une copie locale équivalente et consigner précisément ses paramètres.

Le cas 30 000 sites × 1 vue isole le renderer de sites. Le cas 8×3 ou 9×3 mesure
la montée en charge réelle. Le produit autorise 30 000 sites source par colonne,
donc jusqu'à 270 000 instances pour neuf colonnes ; ce cas extrême doit rester
correct et stable mais peut être lent.

### Mesures

- build Development pour diagnostiquer, build non Development pour confirmer ;
- VSync et target frame rate identiques ;
- warm-up identique ;
- médiane, P95 et P99 CPU main/render et GPU ;
- batches, SetPass, draw calls, vertices, temps de culling ;
- mémoire RenderTexture/matériaux/meshes ;
- allocations GC ;
- durée des changements d'activité, d'atlas et des exports.

### Verdict

- objectif : candidat inférieur ou égal à la baseline ;
- régression soutenue <=5 % : bruit acceptable si les runs se recouvrent ;
- régression >5 % : investigation obligatoire ;
- régression soutenue >10 % : gate refusée, sauf approbation explicite et
  documentée du responsable ;
- aucun FPS absolu n'est imposé au cas combiné extrême.

## 8. Validation

### Automatique stricte

- conversion sRGB/Linear une fois ;
- mapping valeur/palette/seuils ;
- RGB mesh/coupe/légende pour des patchs déterministes ;
- invariance à la lumière et à la normale ;
- alpha de fond et d'objets exportés ;
- clipping 0/1/20 ;
- allocation et destruction des RenderTextures ;
- état caméra restauré après export ;
- sérialisation des assets/prefabs ;
- compilation des shaders par plateforme.

Tolérance locale des patchs opaques : une unité par canal 8 bits. Pour les
mesures entre plateformes, investiguer au-delà de `ΔE00 = 2`.

### Visuelle humaine

Le responsable valide :

- lisibilité des sillons ;
- cohérence des palettes ;
- transparence et visibilité des objets internes ;
- discontinuité surface/coupe non aggravée ;
- Edges opaques et silhouettes transparentes ;
- wireframe ROI ;
- `visu_full_test` / `Small` sous tous les angles utiles ;
- correspondance écran/PNG/composite/vidéo.

Une comparaison pixel par pixel du cerveau Built-in/URP n'est jamais une gate.

## 9. Fichiers actuels particulièrement concernés

- `Packages/manifest.json` et `Packages/packages-lock.json` ;
- `ProjectSettings/GraphicsSettings.asset` ;
- `ProjectSettings/QualitySettings.asset` ;
- `ProjectSettings/ProjectSettings.asset` ;
- `Assets/Resources/Shaders/MeshShader.shader` ;
- `Assets/Resources/Shaders/TransparentMeshUncompiledShader.shader` ;
- `Assets/Resources/Shaders/TransparentMeshShader.shader` ;
- `Assets/Resources/Shaders/UnlitTextureAlpha.shader` ;
- `Assets/Resources/Shaders/SiteShader.shader` ;
- `Assets/ThirdParty/Shaders/WireframeTransparentCulled.shader` ;
- `Assets/Scripts/HBP/Core/Object3D/BrainMaterial.cs` ;
- `Assets/Scripts/HBP/Core/Object3D/Geometry.cs` ;
- `Assets/Scripts/HBP/Core/Object3D/SharedMeshes.cs` ;
- `Assets/Scripts/HBP/Core/Object3D/SharedMaterials.cs` ;
- `Assets/Scripts/HBP/Core/DLL/Generators/SurfaceGenerator.cs` ;
- `Assets/Scripts/HBP/Core/DLL/Generators/CutGenerator.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Camera3D.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/View3D.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/CutTexturesUtility.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/AtlasManager.cs` ;
- `Assets/Scripts/HBP/UI/Module3D/View3DUI.cs` ;
- `Assets/Scripts/HBP/UI/Module3D/Scene3DWindow.cs` ;
- `Assets/Prefabs/3D/3D.prefab` ;
- `Assets/Prefabs/3D/Scenes/View 3D.prefab` ;
- `Assets/Prefabs/3D/Objects/Site.prefab`.

Cette liste est un point d'entrée, pas une autorisation de modifier en masse les
assets. L'inventaire de Phase 1 doit ajouter tout consommateur découvert.

## 10. Règles d'exécution

- respecter le workflow prefab-first ;
- utiliser Unity MCP lorsque l'éditeur est ouvert ;
- lire la console avant et après chaque lot ;
- exécuter `Tools/format-code.cmd` avant tout handoff contenant du C# ;
- ne jamais bloquer le PlayerLoop dans les tests async ;
- ne pas modifier les palettes pour compenser visuellement une erreur de gamma ;
- ne pas lancer une optimisation structurelle sans mesure avant/après ;
- ne pas supprimer les anciens assets avant la Phase 7 ;
- ne pas effectuer d'opération Git pour le compte du responsable.

## 11. Définition globale de fini

La migration est terminée uniquement lorsque :

- les gates 0 à 7 sont passées ;
- tous les modes et contrôles actuels fonctionnent sous URP ;
- les couleurs scientifiques respectent le contrat ;
- les PNG individuels ont un straight alpha correct et aucun halo ;
- composite et vidéo sont fonctionnels ;
- le cas courant ne présente pas de régression soutenue supérieure à 10 % ;
- Windows, macOS Apple Silicon/Metal et Linux passent leur matrice ;
- aucune dépendance active à PPv2/AGM ou aux shaders Built-in ne subsiste ;
- le responsable a validé visuellement `Small` ;
- la documentation décrit les assets et APIs réellement livrés.

