# Architecture cible URP

## 1. Choix du pipeline

La cible est URP avec :

- package `17.5.0` pour Unity `6000.5.2f1` ;
- Universal Renderer ;
- Rendering Path `Forward` ;
- Render Graph activé ;
- espace colorimétrique Linear ;
- HDR désactivé initialement ;
- post-processing URP désactivé initialement, hors effet de contours custom ;
- une lumière principale ;
- lumières additionnelles désactivées si aucune fonctionnalité ne les exige ;
- ombres temps réel désactivées ;
- opaque texture désactivée sauf besoin démontré ;
- depth texture et depth normals activées uniquement là où les contours les
  requièrent.

Forward est préféré parce que HiBoP a peu de lumières, beaucoup de caméras et
vise plusieurs plateformes desktop. Ce choix réduit également la différence
conceptuelle avec le rendu historique.

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
- `Meta` seulement si un besoin de baking apparaît.

Le clipping et l'extrusion doivent être communs à toutes les passes concernées.

### Données matériau

Les propriétés par matériau doivent être placées dans
`CBUFFER_START(UnityPerMaterial)` pour la compatibilité SRP Batcher. Les tableaux
et données changeant fréquemment doivent être conçus avec prudence :

- phase de migration : conserver l'API actuelle si nécessaire ;
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

La migration conserve les UV/valeurs par sommet actuels pour la surface et les
RGBA natifs pour les coupes. Elle unifie LUT, seuils, gamma et alpha. Un passage
ultérieur à l'échantillonnage direct d'un volume 3D est hors périmètre.

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

La migration reproduit la fonction de lecture anatomique avec :

- une main directional light blanche ;
- un modèle léger dont les constantes sont calibrées visuellement ;
- une orientation caméra-relative ;
- une contribution ambiante proche de l'existant ;
- aucune shadow map.

Cette logique doit être encapsulée dans le prefab/système existant, pas recréée
ad hoc dans chaque vue.

## 8. Contours

Remplacer PPv2 et AGM Edge Detection par une Renderer Feature URP compatible
Render Graph :

1. demander depth et normals ;
2. détecter les discontinuités de profondeur et de normales du cerveau et des
   coupes opaques ;
3. rendre les transparents cerveau/coupes dans un masque mono-canal et n'en
   extraire que la silhouette extérieure ;
4. exclure explicitement sites et ROI ;
5. appliquer couleur et épaisseur configurables ;
6. respecter l'activation par vue et les exports, canal alpha compris.

L'effet doit être compatible avec plusieurs caméras et ne conserver aucun état
global dépendant de la dernière caméra. Son coût doit être mesuré à 24 puis 27
vues.

## 9. Sites

### Phase de migration

Porter d'abord le shader custom minimal vers URP HLSL sans ajouter de lumière,
texture ou fonctionnalités. Conserver le petit mesh partagé et les états
visuels. Mesurer avant/après avec 30 000 sites. Un simple cercle coloré est une
qualité cible acceptable si une optimisation structurelle devient nécessaire.

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
survol, filtrage, transparence, taille, couleur, visibilité par colonne et
picking. Le cas extrême multi-colonnes doit être stable, mais n'a pas de budget
de fluidité.

## 10. ROI

Porter le rendu ROI vers des coordonnées barycentriques dans le mesh et un
shader wireframe URP sans geometry stage. Cette solution est obligatoire pour
Metal/Apple Silicon ; le geometry shader existant n'est pas conservé comme
chemin de production.

## 11. Export

Le même renderer, les mêmes shaders et les mêmes propriétés doivent être
utilisés à l'écran et à l'export.

L'export individuel :

- utilise une caméra/configuration équivalente ;
- remplace seulement la couleur de clear par `(0,0,0,0)` ;
- rend dans une cible avec alpha ;
- lit le résultat sans conversion colorimétrique supplémentaire ;
- détruit les ressources temporaires ;
- restaure l'état de caméra même en cas d'exception ;
- encode un PNG straight alpha et élimine tout halo noir de prémultiplication ;
- applique les Edges exactement selon l'état de la vue sans modifier le fond
  transparent.

Un chemin séparé de « rendu d'export » dupliquant les shaders est interdit.

## 12. Variantes et plateformes

Limiter les keywords aux dimensions réellement indépendantes. Éviter une
explosion combinatoire atlas × activité × transparence × clipping × contours.
Préférer des branches uniformes lorsque leur coût est inférieur à la maintenance
de variantes nombreuses, après mesure.

Matrice cible :

| Plateforme | Statut | Contraintes principales |
| --- | --- | --- |
| Windows | requise | API existante au premier portage |
| macOS | requise | Apple Silicon, Metal, macOS 12.0+ |
| Linux | requise | Vulkan ; OpenGL Core seulement comme fallback testé |
| VR | reportée | chantier séparé après spécification XR |
| WebGL | hors périmètre | aucun compromis imposé à cette migration |

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
