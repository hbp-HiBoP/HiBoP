# Architecture cible URP

## 1. Choix du pipeline

La cible est URP avec :

- Universal Renderer ;
- Rendering Path `Forward` ;
- espace colorimétrique Linear ;
- HDR désactivé initialement ;
- post-processing URP désactivé initialement, hors effet de contours custom ;
- une lumière principale ;
- lumières additionnelles désactivées si aucune fonctionnalité ne les exige ;
- ombres de la main light conservées pour la parité, puis benchmarkées ;
- opaque texture désactivée sauf besoin démontré ;
- depth texture et depth normals activées uniquement là où les contours les
  requièrent.

Forward est préféré parce que HiBoP a peu de lumières, beaucoup de caméras et
vise des GPU intégrés, la VR et potentiellement WebGL. Ce choix réduit également
la différence conceptuelle avec le rendu historique.

### Alternatives évaluées

| Option | Avantages | Inconvénients pour HiBoP | Décision |
| --- | --- | --- | --- |
| URP Forward | multi-plateforme, VR, GPU intégrés, SRP extensible, coût de maintenance raisonnable | réécriture des shaders custom et de PPv2 | **retenu** |
| URP Deferred | beaucoup de lumières et découplage partiel géométrie/éclairage | G-buffer, bande passante, MSAA et multi-caméras moins adaptés au besoin | rejeté au premier portage |
| URP Forward+ | nombreuses lumières et culling clusterisé | complexité sans bénéfice avec une main light | rejeté au premier portage |
| HDRP | éclairage haut de gamme et outils avancés | GPU minimal, VR/WebGL, coût et absence de besoin photoréaliste | rejeté |
| SRP custom | contrôle total et potentiel très spécialisé | maintenance d'un pipeline complet, plateformes, outillage et risque projet | rejeté |
| Rester Built-in | aucun portage immédiat | pipeline déprécié, dette croissante et fenêtre de support limitée | solution transitoire seulement |

Le choix URP n'impose pas d'utiliser les shaders génériques URP pour tous les
objets. Les composants critiques de HiBoP restent des shaders spécialisés
fonctionnant au sein d'URP.

## 2. Assets et niveaux de qualité

Créer au minimum :

- un `UniversalRenderPipelineAsset` desktop ;
- un `UniversalRendererData` desktop ;
- un profil VR si les réglages XR exigent une séparation ;
- éventuellement un profil low-end seulement après mesure.

Chaque niveau de qualité livré doit référencer un asset existant et documenté.
Les anciennes références de pipeline absentes doivent être supprimées.

Les assets doivent être créés et reliés dans les prefabs/scènes/configurations
appropriés. Il est interdit de créer au runtime des objets de remplacement pour
compenser une référence prefab manquante.

## 3. Architecture du shader cerveau

Le shader cerveau doit être un shader HLSL URP dédié, pas une conversion
automatique et pas un Shader Graph monolithique.

### Raisons

- tableaux de 20 plans de coupe ;
- boucle de clipping partagée par plusieurs passes ;
- extrusion de sommets ;
- trois flux UV ;
- variantes opaque/transparente ;
- atlas et activités ;
- besoin de contrôler précisément l'ordre éclairage/composition ;
- besoin de garantir le même comportement dans depth, normals et shadows.

### Organisation recommandée

```text
Assets/Shaders/HBP/Brain/
  HBPBrain.shader
  HBPBrainTransparent.shader       # uniquement si un shader commun est impraticable
  Includes/
    HBPBrainInput.hlsl
    HBPBrainClipping.hlsl
    HBPBrainScientificOverlay.hlsl
    HBPBrainLighting.hlsl
```

L'emplacement exact peut suivre les conventions finales du projet, mais le code
partagé ne doit pas être dupliqué entre opaque et transparent.

### Passes requises

- `UniversalForward` ;
- `DepthOnly` ;
- `DepthNormals` pour les contours ;
- `ShadowCaster` si les ombres restent actives ;
- `Meta` seulement si un besoin de baking apparaît.

Le clipping et l'extrusion doivent être communs à toutes les passes concernées.

### Données matériau

Les propriétés par matériau doivent être placées dans
`CBUFFER_START(UnityPerMaterial)` pour la compatibilité SRP Batcher. Les tableaux
et données changeant fréquemment doivent être conçus avec prudence :

- phase de parité : conserver l'API actuelle si nécessaire ;
- phase d'optimisation : préférer buffer structuré, texture ou état partagé si
  le profilage justifie le changement.

Il ne faut pas supposer que `MaterialPropertyBlock` améliore automatiquement les
performances : son interaction avec SRP Batcher et le nombre de renderers doit
être mesurée dans la version Unity utilisée.

## 4. Composition scientifique

Le fragment du cerveau suit conceptuellement :

1. échantillonnage et préparation de l'anatomie ;
2. calcul de l'éclairage URP de l'anatomie ;
3. calcul valeur/identifiant -> couleur scientifique Linear ;
4. composition unlit de l'overlay ;
5. application de l'alpha de sortie selon le mode opaque/transparent.

Le code de mapping scientifique doit être partagé autant que possible avec :

- les coupes ;
- les légendes ;
- les exports ;
- les tests de référence.

### Atlas cible

À terme, préférer :

- un identifiant de région stable par vertex ou texture ;
- une palette 1D GPU ;
- l'identifiant sélectionné/survolé comme petit état uniforme ;
- aucune réécriture complète des vertex colors lors d'un simple survol.

La migration initiale peut conserver les vertex colors afin de réduire le
risque, à condition de corriger explicitement leur interprétation sRGB/Linear.

### Activité cible

Les valeurs scalaires doivent rester scalaires aussi longtemps que possible,
puis être converties par une LUT/colormap partagée au moment du rendu. Une
texture déjà colorée reste acceptable pour les coupes si le coût ou le
générateur natif l'impose, mais son espace colorimétrique doit être explicite et
testé.

## 5. Coupes

Créer un shader URP unlit dédié aux coupes, avec variantes opaque/transparente
si nécessaire. Il doit :

- respecter la même convention de couleur que le cerveau ;
- prendre en charge alpha et ordre de rendu ;
- éviter tout tone mapping ;
- utiliser le bon filtrage selon le type de texture ;
- rester très simple.

La génération native peut être conservée pendant la migration. Les copies et
invalidations seront optimisées seulement après instrumentation.

## 6. Caméras et RenderTextures

Chaque vue doit obtenir une `RenderTextureDescriptor` explicite :

- largeur/hauteur ;
- format couleur LDR sRGB compatible avec le projet Linear ;
- depth/stencil ;
- MSAA ;
- usage XR si applicable ;
- mipmaps désactivées ;
- random write désactivé sauf besoin.

Le gestionnaire doit :

- réutiliser une texture tant que son descripteur ne change pas ;
- libérer et détruire correctement les textures remplacées ;
- éviter les reallocations pendant un simple frame stable ;
- ne pas rendre une vue réellement invisible ou minimisée si son image n'est
  pas nécessaire ;
- permettre un rendu à la demande pour les vues statiques lors d'une phase
  ultérieure.

La stratégie MSAA doit être explicite. Le rendu historique des RenderTextures
est actuellement à 1× ; le premier jalon peut rester à 1× pour la parité, puis
2×/4× peuvent être évalués. Le coût est multiplié par le nombre de vues.

## 7. Éclairage

La phase de parité reproduit :

- une main directional light blanche ;
- intensité proche de 1.2, à recalibrer car les modèles Built-in Standard et
  URP Lit ne sont pas identiques ;
- une orientation caméra-relative ;
- une contribution ambiante proche de l'existant ;
- les ombres uniquement si elles participent réellement à la lecture.

Cette logique doit être encapsulée dans le prefab/système existant, pas recréée
ad hoc dans chaque vue.

## 8. Contours

Remplacer PPv2 et AGM Edge Detection par une Renderer Feature URP :

1. demander depth et normals ;
2. exécuter un Full Screen Pass après les opaques, à un point déterminé pour les
   transparents ;
3. détecter les discontinuités de profondeur et de normales ;
4. appliquer couleur et épaisseur configurables ;
5. respecter l'activation par vue et les exports.

L'effet doit être compatible avec plusieurs caméras et ne conserver aucun état
global dépendant de la dernière caméra. Son coût doit être mesuré à 24 puis 60
vues.

## 9. Sites

### Phase de migration

Porter d'abord le shader custom minimal vers URP HLSL sans ajouter de lumière,
texture ou fonctionnalités. Conserver le petit mesh partagé et les états
visuels. Mesurer avant/après avec 30 000 sites.

### Phase d'optimisation

Évaluer séparément :

1. réduction du nombre de matériaux dynamiques ;
2. regroupement par état/couleur ;
3. suppression ou remplacement des renderers individuels ;
4. GPU instancing ;
5. `DrawMeshInstancedIndirect`, BatchRendererGroup ou autre architecture
   data-oriented si supportée sur les plateformes retenues ;
6. stratégie de picking indépendante des `SphereCollider` individuels ;
7. culling CPU/GPU adapté aux colonnes et vues.

Une architecture instanciée ne sera adoptée que si elle préserve sélection,
survol, filtrage, transparence, taille, couleur, visibilité par colonne et VR.
WebGL peut exiger un chemin de repli.

## 10. ROI

Porter d'abord le rendu ROI vers une solution URP desktop. Pour la compatibilité
large, préférer à terme :

- coordonnées barycentriques dans le mesh et shader wireframe sans geometry
  stage ; ou
- mesh d'arêtes explicite.

Le geometry shader existant ne doit pas être la seule option si WebGL devient
une cible confirmée.

## 11. Export

Le même renderer, les mêmes shaders et les mêmes propriétés doivent être
utilisés à l'écran et à l'export.

L'export individuel :

- utilise une caméra/configuration équivalente ;
- remplace seulement la couleur de clear par `(0,0,0,0)` ;
- rend dans une cible avec alpha ;
- lit le résultat sans conversion colorimétrique supplémentaire ;
- détruit les ressources temporaires ;
- restaure l'état de caméra même en cas d'exception.

Un chemin séparé de « rendu d'export » dupliquant les shaders est interdit.

## 12. Variantes et plateformes

Limiter les keywords aux dimensions réellement indépendantes. Éviter une
explosion combinatoire atlas × activité × transparence × clipping × contours.
Préférer des branches uniformes lorsque leur coût est inférieur à la maintenance
de variantes nombreuses, après mesure.

Matrice cible :

| Plateforme | Statut | Contraintes principales |
| --- | --- | --- |
| Windows | requise | Intel iGPU minimal, DX11/DX12 à décider |
| macOS | requise | Metal, Intel et Apple Silicon à préciser |
| Linux | requise | API et drivers de référence à préciser |
| VR | requise | casque, runtime, single-pass instanced à préciser |
| WebGL | provisoire | pas de geometry shader, limites instancing/buffers |

## 13. Ce qui n'est pas recommandé

- HDRP ;
- Deferred pour le premier portage ;
- Shader Graph comme unique implémentation du cerveau ;
- activation globale de HDR/tone mapping ;
- conversion automatique non revue des shaders custom ;
- remplacement du site shader par URP/Unlit sans mesure ;
- optimisation simultanée de tous les sous-systèmes pendant la bascule ;
- maintien de PPv2 dans une couche de compatibilité ;
- duplication d'un renderer spécialement pour les exports.
