# Phase 3 — ROI, sites et sélection

**Statut :** implémentation et Gate 3 terminées sous Windows le 7 août 2026.
La validation runtime Apple Silicon/Metal reste dans la matrice de plateforme de
la phase 6 ; le shader ROI est déjà compilable pour le backend Metal.

## Cage analytique des ROI

`SharedMeshes.ROISphere` utilise un mesh indexé compact. Le shader
`HBP/ROI/AnalyticCage` ne dépend pas de sa triangulation : il calcule quatre
méridiens, l'équateur et deux parallèles à ±45° à partir de la position locale
normalisée, ainsi qu'une silhouette dépendant de la caméra. Les quatre cercles
intermédiaires sont légèrement plus fins que les trois cercles principaux.
L'épaisseur et l'anticrénelage sont stabilisés en pixels avec `fwidth`.

L'intérieur est toujours entièrement transparent. Chaque ligne possède un
sous-trait sombre plus large qui la distingue du cerveau comme des sites,
quelle que soit leur couleur. La largeur du sous-trait est proportionnelle à
`Cage Thickness`, et la couleur centrale est renforcée dans sa zone
d'anticrénelage afin d'éviter que le sous-trait apparaisse par points. Les
matériaux `ROI.mat` et `ROISelected.mat` portent la couleur sur la cage : bleu à
l'état normal et rouge à l'état sélectionné. Le prefab conserve son
`MeshRenderer`, son `SphereCollider` et le composant `Sphere`; ses motion vectors
sont explicitement désactivés.

Le calcul du nombre de triangles du générateur de sphère a aussi été corrigé :
il produit exactement `2 × longitude × latitude` triangles. Les entrées
dégénérées anciennement laissées en fin d'index buffer ont disparu. Le mesh de
site et la sphère ROI restent tous deux indexés et compacts. La densité du mesh
ROI n'affecte plus le nombre de lignes affichées.

## Sites

Le chemin existant est conservé volontairement :

- petit mesh de sphère partagé `SharedMeshes.Site` ;
- shader `HBP/Site` unlit, limité à une couleur RGBA ;
- aucune texture, normale, lumière, ombre ou motion vector ;
- matériaux partagés pour les états positif, négatif, source, non-source,
  blacklisté, normal et leurs variantes highlight ;
- un GameObject et un `SphereCollider` par site dans l'architecture réelle,
  afin de préserver le picking.

Les états masqué, hors ROI, filtré et blacklisté continuent de piloter la
visibilité. Le gain pilote l'échelle, le highlight l'alpha opaque et l'état
normal conserve son alpha semi-transparent. La sélection continue d'être
affichée par `UI/Toolbar/Site/SelectedSite.cs` ; aucun nouvel indicateur 3D
permanent n'a été ajouté.

Le shader contient une variante d'instancing compatible avec une optimisation
future, mais les matériaux et l'architecture n'ont pas été basculés vers un
renderer instancié : la gate de performance passe nettement, donc cette refonte
serait un coût et un risque sans bénéfice démontré.

## Validation automatique

### EditMode

La suite `HBP.Rendering.Tests` passe : **60/60**.

Les nouveaux contrats vérifient :

- le nombre exact de triangles et l'absence de triangles dégénérés ;
- le caractère indexé et compact des meshes ROI et site ;
- la génération analytique des sept cercles et de la silhouette ;
- l'absence de `#pragma geometry` et l'utilisation de `fwidth` ;
- une couverture rendue suffisamment faible pour exclure une sphère opaque ;
- le support Metal déclaré par `ShaderUtil.IsGraphicsAPISupported` et
  l'absence de message d'erreur du compilateur Metal ;
- les shaders et propriétés des deux matériaux ROI et des treize matériaux de
  sites ;
- les colliders, ombres et motion vectors des prefabs.

### PlayMode

Quatre scénarios ciblés passent : **4/4**.

- animation d'une ROI sans modification de son rayon d'influence ;
- sélection, changement de matériau et picking de la sphère la plus proche ;
- visibilité des sites masqués, hors ROI, filtrés et blacklistés ;
- alpha normal/highlight, gain, raycast site et texte UI du site sélectionné.

Le pont MCP a dépassé son délai pendant le rechargement de domaine, après
l'exécution réelle des tests. Le fichier Unity
`C:/Users/Zigaroula/AppData/LocalLow/CRNL/HiBoP/TestResults.xml` fait foi :
`result="Passed" total="4" passed="4" failed="0"`.

La vérification dans la scène réelle `Small` sélectionne le site `A2` de la
première colonne. L'instance de `SelectedSite` reliée à `Column 0` affiche bien
`A2 (LYONNEURO_2021_LEMl)` ; le lien sélection colonne → barre d'outils est donc
également confirmé sur le projet de référence, et pas uniquement sur la fixture.

## Campagne `visu_full_test / Small`

Artefacts :
`.test-results/rendering/urp-phase3/20260807-100334`.

Configuration : Unity `6000.5.2f1`, Windows, même machine et protocole que la
baseline, 120 frames de warm-up puis 300 échantillons, VSync désactivée et idle
suspendu pendant la mesure.

| Scénario | Pipeline | Frame médiane | P95 | P99 | CPU médiane | GPU médiane | SetPass |
|---|---:|---:|---:|---:|---:|---:|---:|
| `Small` | Built-in | 2,514 ms | 2,857 | 3,005 | 1,792 | 0,898 | 114 |
| `Small` | URP phase 2 | 2,952 ms | 3,344 | — | 2,146 | 0,448 | — |
| `Small` | URP phase 3 | 3,027 ms | 3,560 | 4,160 | 2,220 | 0,457 | 89 |
| 30 000 sites, 1×1 | Built-in | 20,404 ms | 22,641 | 26,192 | 19,243 | 11,313 | 103 |
| 30 000 sites, 1×1 | URP phase 2 | 16,126 ms | 17,932 | — | 14,985 | 10,239 | — |
| 30 000 sites, 1×1 | URP phase 3 | **14,667 ms** | **15,662** | **16,796** | **13,522** | **8,751** | 103 |

Le cas isolé des sites améliore la médiane de **28,1 %** par rapport au
Built-in et de **9,0 %** par rapport à la phase 2. La P95 s'améliore de
**30,8 %** par rapport au Built-in. Il n'existe donc aucune régression site
supérieure à 5 %, et l'instancing/billboard est reporté conformément au plan.

Le cas réel `Small` contient 1 299 sites, 1 299 renderers, 1 299 colliders et
trois matériaux de site uniques. La fixture 30 000 contient 30 000 objets et
renderers, trois matériaux uniques et 433 colliders réels : les objets
temporaires omettent volontairement leurs colliders afin d'isoler le coût du
renderer, comme le prescrit le protocole. Le compteur `Draw Calls Count` n'est
pas exposé par le `ProfilerRecorder` sur cette configuration ; `SetPass`,
triangles et vertices restent consignés.

La médiane intégrée `Small` est 2,5 % plus haute que la campagne phase 2, sous
le seuil d'investigation de 5 %. Son écart au Built-in concerne l'ensemble du
pipeline URP et non le port des sites ; il reste suivi par la phase 5.

## Gate 3

- ROI correct sous Windows : **oui**, cage analytique transparente avec
  silhouette et sept cercles visibles.
- Shader ROI compilable Metal : **oui**, backend Metal supporté et zéro erreur
  de compilation ; exécution matérielle prévue en phase 6.
- Dépendance active à un geometry shader ROI : **aucune**. Les matériaux et le
  prefab actifs référencent uniquement `HBP/ROI/AnalyticCage`.
- Sites fonctionnellement identiques : **oui**, états, alpha, gain, filtres,
  sélection UI et picking couverts.
- Régression du port minimal supérieure à 10 % : **non** ; le cas isolé est
  nettement plus rapide.
- Instancing ou billboard requis : **non**.
