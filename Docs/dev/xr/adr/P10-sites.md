# ADR P10 — sites bufferisés et picking

- **Statut :** ACCEPTED — P10-A–E RESOLVED, QUEST GATE PASS
- **Date :** 2026-09-03
- **Accepté par :** propriétaire du dépôt via l'ordre d'exécution explicite de P10
- **Décisions héritées :** D13, D17, D20, ADR P03 et ADR P09
- **Périmètre :** 37 500 sites sans plafond fonctionnel, sans objet par site

## P10-B — éligibilité et classement déterministe

Un site est sélectionnable si et seulement si sa frame courante appartient au
`SiteAsset` actif, si son entrée `visibility` vaut `1` et si son rayon est
strictement positif. Les flags décrivent l'état affiché mais ne changent pas le
classement : un site blacklisté encore visible reste sélectionnable ; un site
masqué, filtré ou hors ROI n'est sélectionnable que si la frame le déclare
explicitement visible.

Le picking reproduit la sphère effectivement rendue, avec les règles suivantes :

- ray : plus petite distance positive jusqu'à la surface de la sphère de picking ;
- proximité : plus petite distance positive jusqu'à la surface de la sphère ;
- égalité à `1e-5` mm : distance du centre à l'axe du ray, puis `siteId`
  lexicographique ; en proximité, distance au centre puis `siteId` ;
- aucune priorité implicite au site déjà sélectionné, highlighted ou pending ;
- le hover est local et remplaçable ; une validation crée une seule intention
  P07 `SelectSite(siteId)` corrélée par `commandId`, avec le vrai scope colonne,
  sa révision et la session/epoch ; le feedback pending conserve cet ID jusqu'à
  l'outcome correspondant ;
- un outcome accepté affiche l'ID canonique renvoyé par Desktop ; un rejet ne
  retire que le pending du même `commandId` ; un outcome tardif ou d'un ancien
  contexte est ignoré sans toucher une intention plus récente.

Ces règles rendent le résultat indépendant de l'ordre des buffers, de l'ordre
des cellules et du framerate. Un changement de règle UX rouvre P10-B.

## P10-C — unités, échelle et volumes de sélection

`SiteAsset.Positions` et `SiteRenderFrame.Sizes` sont exprimés dans le repère
P03 `DesktopUnityMillimetersV1`. La taille est le **rayon** de la sphère unitaire
Desktop, pas son diamètre. Le renderer effectue une seule conversion
millimètres → mètres (`0,001`) avant la transformation du `BrainInstance`.

Le transform du `BrainInstance` doit rester uniforme (invariant P09). Son scale
agrandit donc de façon identique le cerveau, le rayon rendu et les seuils de
picking. Les calculs de picking transforment ray/point dans le repère local du
renderer de sites, puis travaillent en millimètres ; aucune compensation inverse
ne maintient une taille monde artificiellement constante.

Pour les contrôleurs, la V1 retient :

- ray : rayon de picking `max(rayonRendu, 2 mm)` ;
- proximité : surface de la sphère rendue à au plus `12 mm` ;
- distance maximale du ray : segment fourni par l'appelant, strictement positif ;
- seuils appliqués uniquement après transformation en local brain space.

Ainsi un site très petit garde une cible ray minimale, tandis que la proximité
reste une distance de surface et ne favorise pas artificiellement les grosses
sphères. Les mains ne sont pas qualifiées par P10 ; leur éventuel seuil distinct
relève de P13 et doit être mesuré.

Les positions sont immuables pour un `SiteAsset`. La première frame complète est
comparée à l'asset ; ensuite le hash de l'asset déjà validé fait autorité et les
positions répétées des frames de streaming ne sont ni consommées, ni retenues,
ni rescannées. Un déplacement canonique exige un nouvel asset et un nouveau
hash. Couleurs, rayons, visibilité et flags sont dynamiques et ne provoquent
jamais de rebuild de l'index spatial.

## P10-E — métadonnées transitoires autorisées

La réponse de sélection Quest est liée à `session/epoch`, `siteId`, `columnId` et
`sourceStateRevision`. L'allowlist V1 contient uniquement :

- un libellé court du site, fourni à la demande pour l'affichage actif ;
- jusqu'à deux mesures numériques typées (`value`, unité contrôlée, rôle
  activité/amplitude/latence) ;
- les états booléens déjà visibles dans la frame : selected, highlighted et
  blacklisted.

Le libellé et les mesures vivent seulement dans le modèle de panneau ouvert. Ils
sont effacés à la désélection, à la fermeture du panneau, à la perte de scope, à
la fermeture de session et au changement d'epoch. Ils ne sont ni mis en cache,
ni sérialisés, ni inclus dans snapshots, métriques, exceptions ou logs.

Sont interdits sur Quest dans P10 : nom ou identifiant patient, `FullID` Desktop,
chemin, tag/label libre, note, démographie, donnée source, série temporelle et
stack trace. Si le produit exige l'un de ces champs, P10-E et P14 doivent être
rouverts avant transport.

## P10-A — backend GPU retenu

Le backend V1 utilise deux `GraphicsBuffer` structurés : positions statiques et
attributs dynamiques. Un unique `Graphics.RenderMeshPrimitives` dessine toutes
les instances d'un cerveau. Le shader lit `SV_InstanceID`; aucune matrice, aucun
renderer et aucun collider n'est créé par site. La primitive finale est un
imposteur sphérique caméra-facing de quatre sommets/deux triangles, découpé et
ombré comme une sphère dans le fragment shader. Le fragment écrit également la
profondeur de la surface sphérique, afin que silhouette, occlusion et picking
P10-C décrivent le même volume.

La comparaison D3 hôte (37 500 sites, Unity 6000.5.2f1, D3D12, RTX 2070 SUPER)
donne :

| Prototype | p50 CPU | p95 CPU | max CPU | draws | octets représentatifs |
| --- | ---: | ---: | ---: | ---: | ---: |
| matrices, lots de 1023 | 2,7729 ms | 3,4495 ms | 4,5523 ms | 37 | 2 400 000 CPU |
| buffers, update complet | 0,0151 ms | 0,5557 ms | 1,3269 ms | 1 | 1 200 000 GPU |
| buffers, dirty range 256 | 0,0012 ms | 0,0016 ms | 0,0044 ms | 1 | sous-plage |

Ce benchmark mesure préparation/upload/soumission CPU. La première mesure Quest
avec la sphère tessellée de 120 triangles par site a ensuite donné des frames
p95 de 41,66 / 111,10 / 1 374,90 ms pour 1/3/8 instances. Sans retirer aucun
site, le passage à l'imposteur deux triangles ramène les trois phases à 72 Hz,
avec CPU p95 2,74 / 2,93 / 3,47 ms et GPU p95 0,92 / 0,92 / 0,95 ms. Cette
mesure cible confirme le choix bufferisé tout en fixant la géométrie réellement
compatible avec D20.

## P10-D — index spatial et rebuild

La V1 retient un BVH CPU médian sur les centres statiques de `SiteAsset`. Il est
construit une fois à l'activation d'un hash et détruit avec cet asset. Couleur,
rayon, visibilité, flags, hover, pending et sélection canonique ne déclenchent
jamais de rebuild. Les bounds de nœud portent les centres ; chaque instance
maintient en parallèle le rayon dynamique maximum de chaque sous-arbre, mis à
jour depuis les dirty ranges. Une valeur extrême n'élargit donc que ses ancêtres
et non tout le BVH. Les feuilles font huit entrées et les tests exacts emploient
le rayon individuel courant.

La comparaison D3 hôte donne :

| Prototype | build p50/p95 | proximité p50/p95 | ray p50/p95 | exactitude |
| --- | ---: | ---: | ---: | ---: |
| grille 8 mm | 4,7512 / 7,1903 ms | 53,3 / 86,5 µs | 33,3 / 58,1 µs | 100 % |
| BVH médian | 116,2792 / 149,7928 ms | 4,2 / 6,6 µs | 3,7 / 6,9 µs | 100 % |

Le coût de build BVH est accepté car il est payé une fois par hash ; le gain de
proximité est supérieur à 10× et le ray reste très inférieur au gate 50 ms.

## Validation Quest

Le profil final Quest 3 mesure 721 frames par phase, avec 721/721 picks exacts à
1, 3 et 8 instances et un picking p95 maximal de 0,0518 ms. Les 30 minutes à
huit instances maintiennent le dirty update et le picking exact à chaque frame :
129 600/129 600 validations. Les 232 échantillons VrApi restent entre 72 et
73 fps, avec un temps application p95 de 0,98 ms et un temps CPU+GPU p95 de
2,97 ms. Le PSS passe de 384 734 KiB à 5 min à 386 591 KiB à 30 min, soit une
dérive de 1 857 KiB ; la mémoire graphique reste voisine de 95 MiB et le statut
thermique reste 0. Aucun crash, ANR, OOM ou processus résiduel n'est observé. Le
gate P10/D20 est fermé.
