# Phase 5 — Validation scientifique et performances

## Statut

**État :** campagne URP opérationnelle ; Gate 5 en attente d'un nouveau passage
Built-in avec le même collecteur.

Ce document ne valide pas encore la Gate 5. Le baseline de phase 0 reste utile,
mais il a été produit avec une autre disposition de l'éditeur et une version
moins complète du collecteur. Une conclusion sur le seuil P95 de 10 % exige un
dernier passage Built-in strictement symétrique.

## Collecteur de phase 5

`RenderingBaselineCapture` recharge désormais la visualisation depuis le
projet avant chaque campagne. Il ne réutilise plus une scène éventuellement
redimensionnée ou modifiée à la main.

La campagne enregistre :

- opaque, transparent, Edges actif et inactif ;
- rotation continue de la caméra ;
- mise à jour temporelle de l'activité ;
- survol d'atlas ;
- déplacement continu d'une coupe ;
- 30 000 sites source dans une vue ;
- 9 colonnes × 3 vues ;
- allocations GC par frame ;
- mémoire Unity, mémoire réservée, mémoire graphique et RenderTextures vivantes.

Chaque scénario de performance utilise 120 images de chauffe puis 900 images
mesurées. Les instantanés mémoire distinguent les RenderTextures globales de
Unity, les cibles réellement attachées aux caméras de la scène et les textures
nommées et possédées par les vues HiBoP.

Le cas 9×3 est produit à partir des trois vraies colonnes de `Small`, dupliquées
dans un projet de mesure conservé avec les artefacts. Il rend donc les vrais
meshes, textures, sites, shaders et caméras du produit. La duplication évite de
dépendre du projet historique `Mini / VISU`, dont certains chemins EEG ne sont
plus présents sur cette machine.

Pour la comparaison avec le baseline de phase 0, les trois RenderTextures de
`Small` sont forcées à `348×516`, soit exactement `538 704` pixels comme le
passage Built-in. Cet override n'est actif que pendant la mesure. Hors campagne,
la taille reste strictement celle du rectangle physique affiché à l'écran.

## Résultats URP provisoires

Machine : Windows 11, NVIDIA GeForce RTX 2070 SUPER, Direct3D 11, Unity
`6000.5.2f1`, qualité `Fantastic`, espace Linear.

Campagne URP de référence (schéma 3) :
`.test-results/rendering/urp-phase5/20260807-175312/manifest.json`.

| Scénario | Vues | Pixels rendus | Sites | Frame médiane | Frame P95 | CPU main médiane | CPU main P95 | GPU médiane |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Small opaque, Edges off | 3 | 538 704 | 1 299 | 4,759 ms | 11,184 ms | 3,275 ms | 4,035 ms | 0,334 ms |
| Small opaque, Edges on | 3 | 538 704 | 1 299 | 5,009 ms | 16,285 ms | 3,472 ms | 4,264 ms | 0,430 ms |
| Small transparent, Edges off | 3 | 538 704 | 1 299 | 9,646 ms | 22,935 ms | 3,242 ms | 4,034 ms | 0,298 ms |
| Small transparent, Edges on | 3 | 538 704 | 1 299 | 5,111 ms | 11,382 ms | 3,526 ms | 4,254 ms | 0,387 ms |
| 30 000 sites, 1 vue | 1 | 179 568 | 30 000 | 19,770 ms | 32,781 ms | 18,296 ms | 20,027 ms | 13,390 ms |
| 9×3 | 27 | 603 216 | 3 897 | 11,835 ms | 29,728 ms | 10,216 ms | 11,607 ms | 3,790 ms |

Le temps mural entre deux images reste sensible à la planification de l'éditeur :
le témoin avec toutes les caméras 3D désactivées mesure lui-même `13,241 ms` au
P95, alors que son CPU main P95 n'est que de `2,924 ms`. La comparaison finale
ne devra donc pas attribuer au pipeline les pauses également présentes dans ce
témoin. Les valeurs CPU main, le différentiel par rapport au témoin et la
campagne Built-in symétrique seront examinés ensemble.

Le CPU main du cas 9×3 reste sous 12 ms au P95 sur cette machine. Le cas extrême
9×3 × 30 000 sites par colonne demeure un test de robustesse sans budget FPS et
n'est pas synthétisé par cette campagne : le cas 30 000 mesure isolément le
renderer de sites, et le 9×3 mesure isolément la multiplication des vues.

## Allocations et mémoire

Le témoin `small_static_3d_cameras_disabled_control` mesure `10 513` octets de
GC par frame. Le cas opaque avec les trois caméras mesure exactement la même
médiane. Le rendu 3D statique n'ajoute donc aucune allocation récurrente ; les
allocations observées viennent de la boucle application/éditeur et du
collecteur, pas des caméras URP.

Après fermeture du fixture 9×3 et rechargement de `Small` :

- les RenderTextures globales créées reviennent de 51 à 21 ;
- les RenderTextures possédées par les vues HiBoP reviennent exactement de 27
  à 3, comme avant l'ouverture du fixture ;
- les trois caméras de `Small` ont chacune une unique cible ;
- la mémoire graphique revient de 354,6 Mo à 318,7 Mo, sous les 322,3 Mo
  observés sur `Small` avant le cas haut.

Le total de pixels des trois cibles restaurées vaut `603 216` au lieu des
`538 704` pixels de mesure : l'override de benchmark a alors été retiré et les
vues sont revenues à leur taille physique courante (`354×568` chacune). Cette
différence de dimensions n'est pas une ressource résiduelle.

La mémoire réservée Unity reste un high-water mark et n'est pas censée revenir
immédiatement à sa valeur initiale. Les ressources graphiques propriétaires,
elles, sont bien libérées.

## Contrôles visuels et fonctionnels

La campagne produit 47 captures couvrant cerveau, activité, sites, atlas,
coupes, transparence, Edges, ROI, export individuel, export composite, export
vidéo et interface complète. Les exports PNG individuels conservent un fond
alpha nul.

Les validations humaines réalisées pendant les phases 2 à 4 couvrent déjà :

- lecture du volume, des sillons et des couleurs scientifiques ;
- continuité activité mesh/coupes ;
- masque IRM et lissage de bord ;
- silhouette transparente ;
- Edges limités au cerveau et aux coupes ;
- cage analytique des ROI et états de sélection ;
- exports PNG et vidéo.

Le collecteur retire maintenant les coupes temporaires avant les captures ROI
et attend l'extinction des gizmos de coupe afin que chaque famille de captures
reste indépendante.

Validation automatique courante :

- `HBP.Rendering.Tests` : 79/79 ;
- `HBP.Module3D.PlayModeTests` : 43/43 ;
- aucune erreur de compilation ou d'exécution pendant la campagne complète.

## Optimisations évaluées et rejetées

Deux changements ont été essayés avec le même collecteur puis annulés :

1. passer le mode de texture intermédiaire URP de `Always` à `Auto` ;
2. désactiver l'occlusion culling des caméras 3D.

Aucun des deux ne produit un gain net et répétable sur `Small`. Ils dégradent
même plusieurs médianes CPU. Ils ne font donc pas partie de la migration.

## Condition restante pour la Gate 5

Le baseline historique indique `2,857 ms` au P95 sur `Small`, mais il ne contient
ni témoin sans caméras ni métriques GC, et sa session d'éditeur n'est pas la
même. Le passage URP montre par ailleurs que ses pointes P95 existent aussi dans
le témoin sans caméras 3D. Une comparaison brute de ces deux fichiers
attribuerait donc à tort tout le bruit de l'éditeur au pipeline.

La Gate 5 sera close après :

1. un passage Built-in au commit de phase 0 avec le collecteur de schéma 3 ;
2. comparaison des scénarios à `348×516` par vue ;
3. validation humaine des captures finales `Small` ;
4. consignation de la décision P95, puis mise à jour du statut dans le README.

Le port temporaire vers le commit Built-in doit rester volontairement minimal :

- reprendre `RenderingBaselineCapture` et `RenderingBaselineReport` du commit de
  phase 5 ;
- ajouter à l'ancien `View3DUI` uniquement l'override statique de taille et le
  calcul d'aspect associé ;
- ne reprendre ni le propriétaire de RenderTexture URP, ni les shaders, ni les
  matériaux ou assets de pipeline de la migration ;
- conserver le projet, la visualisation, la taille `348×516`, les 120 images de
  chauffe et les 900 images mesurées strictement identiques.

La compatibilité statique avec l'API de `25ebdc125` a été vérifiée. Les API de
scène, coupes, atlas, caméra, vues, sites et suspension du mode idle requises par
le collecteur existent déjà dans ce commit. La seule adaptation attendue est
donc bien l'override de taille dans l'ancien chemin d'allocation de
`View3DUI`.
