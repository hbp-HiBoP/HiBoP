# Phase 2 — Cerveau, coupes et bascule globale URP

**Statut :** implémentation terminée le 6 août 2026. Les validations
automatiques et la campagne `visu_full_test / Small` passent. La clôture
formelle de la Gate 2 attend uniquement la validation visuelle du responsable
sur le modèle final palette/relief et sa continuité surface/coupe.

## Pipeline actif

`Assets/Settings/Rendering/HBP-Desktop-URP.asset` est maintenant le pipeline
global de `GraphicsSettings` et des six niveaux de qualité. Il n'existe plus de
mode Built-in/URP sélectionnable à l'exécution.

Les 30 matériaux actifs de
`Assets/Settings/Rendering/HBP-Material-Migration-Inventory.json` utilisent leur
shader cible. L'inventaire est exécutable : le test échoue si un matériau actif
est absent, si son shader diffère de la cible, s'il n'est pas supporté ou s'il
contient une erreur de compilation.

## Cerveau

Les shaders `HBP/Brain` et `HBP/Brain/Transparent` conservent l'API historique
des matériaux afin de ne migrer aucune donnée :

- `_MainTex`, `_AoTex`, `_ColorTex` ;
- `_Atlas`, `_Activity`, `_FMRI` ;
- `_Amount`, `_MaxRadius`, `_Center` ;
- `_StrongCuts`, `_CutCount`, `_CutPoints[20]`, `_CutNormals[20]` ;
- `_Color` et les propriétés anatomiques existantes.

Le vertex shader applique la même extrusion aux passes Forward, DepthOnly et
DepthNormals. Les trois passes partagent aussi la même fonction de clipping,
avec un compte borné à 20 plans et les modes fort/faible historiques. Les 20
points et normales restent des uniforms de matériau hors du bloc SRP Batcher :
cela évite de recopier un bloc de 40 vecteurs dans chaque constante batchée tout
en conservant des valeurs distinctes par scène. `BrainMaterials` transmet
toujours des tableaux de longueur 20, y compris lorsqu'un seul plan est actif,
ce qui respecte la taille immuable des vector arrays Unity.

L'anatomie utilise un éclairage relatif à la caméra, indépendant des lumières
de scène et sans ombre. L'anatomie et l'overlay scientifique sont désormais
éclairés séparément, puis composés :

- atlas et fMRI lisent les vertex colors sRGB puis les convertissent une fois
  vers Linear ;
- l'activité lit la LUT sRGB et l'alpha linéaire transporté par les UV
  existants ;
- le multiplicateur historique d'alpha `×2,5` est supprimé ;
- l'alpha scientifique est remappé par `1 - (1 - alpha)²` sur mesh et coupe :
  l'alpha usuel `0,8` donne un poids palette de `0,96`, tout en conservant les
  extrémités `0` et `1` ;
- l'anatomie garde son éclairage et son reflet neutre ;
- la composante diffuse de l'activité reçoit une modulation multiplicative de
  sa propre couleur. Les ombres suivent l'ambient historique — `0,35` par défaut — sans
  borne basse artificielle ; les hautes lumières restent bornées à `1,08`, avec
  limitation supplémentaire selon le canal le plus lumineux pour éviter
  l'écrêtage ;
- le reflet scientifique principal reste teinté par la palette ;
- un reflet neutre secondaire, limité à 50 % de l'intensité spéculaire
  anatomique, rapproche néanmoins la réponse matérielle des zones actives et
  inactives. Il est très localisé et n'affecte pas la couleur diffuse ;

`Assets/Resources/Textures/alpha.png` est donc importé avec `sRGBTexture =
false`. Les UV restent le transport vers le GPU, mais leurs valeurs d'activité
ne sont plus calculées directement aux sommets indépendamment des coupes : la
grille volumique du générateur est désormais leur source commune.

## Continuité activité surface/coupe

Le diagnostic a distingué trois écarts :

- la surface normalisait l'activité calculée directement à chaque sommet du
  mesh ;
- la coupe interpolait au même instant les valeurs et poids bruts de la grille
  volumique, puis floutait une seconde fois l'image déjà composée avec la LUT ;
- surtout, le shader du mesh composait anatomie et palette en espace Linear,
  tandis que le générateur natif de coupe mélangeait directement leurs octets
  sRGB. Cette différence de domaine colorimétrique déplaçait visiblement la
  teinte même lorsque l'activité et l'alpha étaient voisins.

La correction conserve l'architecture performante par UV, sans texture 3D ni
upload volumique par colonne et par frame :

- `SurfaceGenerator` pré-calcule une fois les indices ou stencils de la grille
  pour chaque sommet ;
- à chaque instant, il applique exactement l'interpolation configurée par
  `GeneratorSurface` — nearest ou trilinéaire pondérée — puis les mêmes
  fonctions de normalisation que les coupes ;
- en mode trilinéaire, les valeurs scientifiques normalisées et leurs poids
  sont lissés dans la grille volumique commune avant tout accès à la palette.
  Deux passes d'un noyau binomial séparable à cinq échantillons par axe
  atténuent la lecture visuelle des cellules sans mélanger les couleurs RGB de
  la LUT ;
- le support binaire de l'activité reçoit séparément une seule passe du noyau,
  soit une bande compacte de deux cellules de part et d'autre de la frontière.
  Les deux couches actives voisines du vide modulent le plancher d'opacité et le
  halo extérieur s'éteint au plus deux cellules plus loin, tandis que
  l'intérieur plus profond reste pleinement opaque. Les valeurs et les poids
  restent inchangés ;
- les sommets du mesh ne sont plus rejetés à cause d'un échantillon MRI nul :
  leur appartenance au mesh cérébral et le support d'activité suffisent ;
- les coupes conservent séparément un support anatomique indépendant de la
  palette. Les faibles intensités sous le seuil tissulaire sont des candidats
  de fond, mais seules celles reliées aux bords de la coupe par huit-connexité
  sont classées comme extérieures. Les poches sombres internes restent donc
  colorables. Le masque intérieur est flouté puis multiplié par sa version
  binaire brute : la transition reste douce sans restaurer de couleur dans
  l'extérieur ;
- ce champ lissé est mis en cache par générateur, géométrie et instant. Le mesh
  et toutes les coupes le réutilisent donc sans recalcul, tandis que le mode
  nearest historique reste strictement inchangé ;
- le calcul est parallélisé et ne change ni la taille des UV envoyés à Unity ni
  l'ABI publique de `hbp_core` ;
- le lissage anatomique des coupes est conservé, mais le second flou de
  l'overlay d'activité composé est supprimé afin de ne plus mélanger des teintes
  de palette après l'échantillonnage. L'adoucissement visible vient désormais
  du champ scalaire commun, en amont de la LUT ;
- le compositeur de coupe décode désormais les deux couleurs sRGB, les mélange
  en Linear, puis réencode le résultat en sRGB. Une LUT de décodage à 256
  entrées et une LUT d'encodage à 4 096 entrées évitent tout `pow` dans la
  boucle par pixel. Le même chemin corrige activité, atlas et fMRI ;
- ce compositeur applique le même remapping quadratique de l'alpha que le
  shader, afin que la saturation accrue ne recrée aucune jointure.

La suite native contient un contrat ciblé qui vérifie que la projection
trilinéaire lissée produit des valeurs et poids visibles bornés sur la surface.
Les 13 suites `hbp_core` passent. L'ABI complète compte désormais 212 symboles,
avec deux setters additifs pour les préférences de masque MRI et de lissage des
frontières d'activité. Un test
d'intégration Unity supplémentaire construit une coupe
synthétique à anatomie, palette et alpha connus, puis vérifie canal par canal le
composite sRGB → Linear → sRGB renvoyé par la DLL.

Le collecteur de validation a également été corrigé : il applique maintenant
la transformation UV réellement sérialisée sur le mesh de coupe
`(ratio.y, 1 - ratio.x - 0,005)`, échantillonne les textures en Linear et
compare la couleur de coupe à la composition anatomie/palette/alpha du mesh,
plutôt qu'à la palette pure. Après le modèle final, sur les sept échantillons
actifs proches de la jointure, la distance RGB Linear moyenne passe de `0,0445`
à `0,0205` (`-54 %`) et le maximum de `0,0882` à `0,0441` (`-50 %`). La distance
moyenne entre la coupe et la palette pure passe simultanément de `0,156` à
`0,0332` (`-79 %`). Le résidu inclut la discrétisation
du texel de coupe et le léger décalage entre le sommet et le centre du texel ;
il ne présente plus la dérive colorimétrique systématique initiale.

## Coupes, transparence et compatibilité

`HBP/Cut` est un shader unlit opaque avec Forward, DepthOnly et DepthNormals.
`HBP/Cut/Transparent` est sa variante transparente avec alpha séparé et
`ZWrite Off`. Les deux gardent `_MainTex` et `_Color`, utilisés par le générateur
de textures de coupe et par `BrainMaterials`.

Les shaders de compatibilité suivants évitent tout matériau magenta lors de la
bascule globale :

- `HBP/Utility/UnlitColor` ;
- `HBP/UI/Texture` et `HBP/UI/Mask` ;
- `HBP/Site` et `HBP/Site/Selection` ;
- `HBP/ROI/AnalyticCage`.

La Phase 3 branchera les coordonnées barycentriques du wireframe ROI. En
attendant, les anciens meshes ROI sans barycentriques utilisent un remplissage
translucide normal/sélectionné : ils restent visibles et compatibles Metal sans
geometry shader. Le shader de site reste volontairement limité à une couleur
et un alpha.

## Validation automatisée

La suite `HBP.Rendering.Tests` couvre notamment :

- présence, support et absence d'erreur des dix shaders URP actifs ;
- assignation globale Graphics/Quality ;
- import linéaire de l'alpha scientifique ;
- contraste de luminance scientifique lorsque la normale change, y compris une
  normale vue de profil, et brillance mesurable lorsque `_Glossiness` augmente ;
- conservation d'au moins 90 % de la saturation de la palette hors reflet et
  75 % au sommet du reflet neutre localisé à 50 % ;
- courbe exacte d'opacité scientifique, dont `0,8 -> 0,96`, et proximité de la
  palette à l'alpha usuel ;
- égalité des modes activité, atlas et fMRI sous le même éclairage, et égalité
  exacte des pixels non éclairés coupe/UI avec la palette ;
- clipping Forward à 0, 1 et 20 plans, en modes fort et faible ;
- présence des passes Forward, DepthOnly et DepthNormals et partage des
  implémentations d'extrusion/clipping ;
- correspondance exhaustive entre inventaire, matériaux et shaders cibles.

Résultats :

- `55/55` tests de rendu ciblés ;
- suite EditMode complète : `506` réussites, `0` échec, `30` tests legacy
  explicitement exclus ;
- `HBP.Module3D.PlayModeTests` : `42/42`, `0` échec.

La dernière exécution PlayMode a été lancée et suivie intégralement par MCP :
les 42 tests ont terminé avec succès et aucun test n'a été ignoré.

## Campagne réelle URP

Le collecteur de Phase 0 est maintenant multi-pipeline. Sous URP il écrit dans :

```text
.test-results/rendering/urp-phase2/<UTC yyyyMMdd-HHmmss>/
```

Il ne modifie donc jamais la baseline Built-in. La campagne charge le projet
local `visu_full_test.hibop`, visualisation `Small`, et couvre activité,
anatomie, atlas, transparence, coupes, ROI, UI, PNG individuels, composite et
vidéo. Un cas supplémentaire confirme le ROI derrière le cerveau transparent.

La dernière exécution validée possède :

- dossier `.test-results/rendering/urp-phase2/20260806-135457` ;
- pipeline `HBP-Desktop-URP` ;
- 3 colonnes, 3 vues et 1 299 sites ;
- 47 captures et 49 fichiers, stress 30 000 sites inclus ;
- 11 captures à fond transparent, dont les exports individuels ont des coins
  d'alpha nul et un contenu d'alpha non nul ;
- 8 patchs colorimétriques et 15 échantillons surface/coupe, dont 7 actifs
  utilisés pour la mesure de continuité ;
- aucune entrée `Warnings` et aucune erreur de capture ;
- des mesures focalisées, normatives et non idle pour le cas réel et le stress
  30 000 sites.

Les exports PNG conservent donc un fond réellement transparent : les coins ont
un alpha nul et le contenu un alpha non nul.

La mesure numérique porte sur la couleur non éclairée au même point. Sur le
mesh, la modulation scientifique est appliquée avant composition et ne change
que la luminance ; les ombres sont libres et les hautes lumières protégées. La
capture et l'inspection à l'angle qui avait révélé le défaut restent la preuve
perceptuelle de Gate 2 à faire accepter.

Après la suppression de la borne basse artificielle du relief et l'ajout du
reflet neutre secondaire, la campagne visuelle rapide `20260806-141204` a
régénéré 46 captures sans warning. Son échantillon `Small` est focalisé,
normatif et non idle (`3,056 ms` frame, `2,233 ms` CPU main, `0,495 ms` GPU).
Le stress 30 000 sites n'a pas été rejoué pour ces variations limitées au
fragment shader.

Après le passage au lissage scalaire volumique commun et l'adoucissement du
support d'opacité, la campagne finale `20260806-192004` a régénéré 46 captures
sans warning. Une capture supplémentaire à `Z=3,12` confirme la rampe
anatomique quadratique : les cavités de faible intensité ne reçoivent plus un
overlay plein et les transitions ne révèlent pas de cubes. Les blocs
cartésiens ne sont plus lisibles sur les exports axial et coronal, et la même
transition progressive est visible sur le mesh. Les 520 tests EditMode
`HBP.Serialization.Tests` et `HBP.Rendering.Tests` ainsi que les 13 tests natifs
`hbp_core` passent. L'échantillon `Small` reste focalisé,
normatif et non idle (`2,936 ms` frame, `2,182 ms` CPU main, `0,669 ms` GPU,
87 SetPass).

Une IRM individuelle non skull-strippée (`visu_full_test / Single`) a ensuite
montré que `recomputed_cal_min == raw_min == 0` réduisait la détection du fond
à une demi-unité 8 bits (`≈ 6,5` pour une plage `0..3322`). Le fallback à `5 %`
du maximum robuste fournit désormais le seuil des candidats de fond. Une
propagation depuis les bords remplace l'ancienne atténuation proportionnelle à
l'intensité : elle conserve les tissus sombres internes du MNI tout en excluant
le fond connecté de l'IRM individuelle. Le masque flouté reste multiplié par sa
version binaire brute pour empêcher tout halo extérieur. Les 13 tests natifs
passent, dont un contrat synthétique opposant une faible intensité extérieure à
une poche de même valeur enfermée. L'ABI reste à 212 symboles.

## Performance observée

La campagne complète `20260806-135457` mesure :

| Scénario | Frame médiane | CPU main médian | GPU médian | SetPass |
| --- | ---: | ---: | ---: | ---: |
| `visu_full_test / Small` | 2,952 ms | 2,146 ms | 0,448 ms | 95 |
| 30 000 sites, 1 vue | 16,126 ms | 14,985 ms | 10,239 ms | 109 |

Les deux échantillons sont focalisés, normatifs et non idle. Par rapport à la
campagne immédiatement antérieure, `Small` est légèrement plus rapide et le
stress 30 000 sites varie de `+0,315 ms` en frame médiane et `+0,544 ms` GPU,
sans changement des SetPass. Ces écarts restent dans la variabilité observée
de l'Editor et ne signalent pas de régression structurelle.
La conversion colorimétrique s'exécute lors de la régénération des textures de
coupe, pas dans le rendu de chaque frame, et ses conversions non linéaires sont
pré-calculées dans les LUT.

La Gate d'optimisation finale des sites et l'éventuel instancing restent en
Phase 3 ; ces résultats confirment néanmoins l'absence de régression de rendu
mesurable liée à la correction de continuité.

## Limites volontaires

- le wireframe barycentrique ROI final est en Phase 3 ;
- le profil d'optimisation final à 30 000 sites et l'éventuel instancing sont
  en Phase 3 ;
- les Edges URP sont en Phase 4 ;
- la fabrique et la durée de vie centralisée des RenderTextures, ainsi que la
  fermeture complète des exports, sont en Phase 4 ;
- la matrice Windows/macOS Apple Silicon/Linux est une validation ultérieure.

Ces limites ne réintroduisent aucun shader Built-in actif dans le rendu courant.

## Audit Gate 2

| Exigence | Preuve actuelle | État |
| --- | --- | --- |
| Aucun matériau actif magenta | inventaire exhaustif, shader supporté et sans erreur ; captures réelles | validé |
| Modes scientifiques sans migration | activité et atlas dans `Small`, activité/atlas/fMRI dans la fixture pixel | validé |
| Sillons lisibles | captures opaques multi-vues | validé par le responsable |
| Palette scientifique fidèle, relief et brillance | saturation >= 90 % hors reflet et >= 75 % au sommet du reflet à 50 %, alpha `0,8 -> 0,96`, ombres historiques | validation responsable requise sur le modèle final |
| Mapping mesh/coupe/légende | test pixel commun activité/coupe/UI | validé |
| Clipping couleur/profondeur/normales | test 0/1/20 Forward, passes compilées et implémentation partagée | validé |
| Sites, coupes et ROI visibles en transparence | scénarios `transparent`, `cuts_transparent`, `roi_through_transparent_brain` | validé |
| Continuité surface/coupe corrigée | source volumique commune, composition/alpha et support lissé communs, distance moyenne initiale `0,0445 -> 0,0205`, campagne `20260806-170118` sans blocs visibles ni warning | validation responsable requise |

La Gate 2 ne doit être marquée fermée qu'après l'acceptation des deux lignes de
validation visuelle restantes. Ce contrôle n'autorise pas à réintroduire le multiplicateur
historique `×2,5` pour masquer une différence.
