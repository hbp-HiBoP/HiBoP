# Plan d'implémentation de l'inflation corticale

## Statut du document

- **Date :** 27 août 2026
- **Statut :** plan d'implémentation proposé ; décisions produit de phase 0 partiellement figées
- **Périmètre :** génération à la demande d'une représentation `inflated` à partir du mesh actuellement sélectionné dans une visualisation HiBoP
- **Document d'étude associé :** [Inflation dynamique des surfaces corticales](README.md)

## Résumé

L'implémentation est techniquement faisable et doit être répartie entre :

- `hbp_core`, pour la validation géométrique, l'algorithme d'inflation et les métriques de qualité ;
- le wrapper C# `HBP.Core.DLL.Surface`, pour l'ABI, l'ownership et l'exécution asynchrone ;
- les objets `Mesh3D` et `MeshManager`, pour gérer la représentation anatomique et sa variante inflated ;
- l'interface Unity, pour déclencher le calcul, afficher sa progression et sélectionner la représentation.

Deux contrats structurants doivent guider toute l'implémentation :

1. Une surface inflated est une **représentation dérivée** du mesh anatomique sélectionné, pas une nouvelle anatomie indépendante dans la liste des meshes.
2. Les activités, atlas et autres données spatiales sont calculés sur la **surface anatomique de référence**, puis transportés vers l'inflated grâce à la correspondance des indices de sommets. Ils ne doivent pas être rééchantillonnés aux coordonnées inflated.

## Architecture cible

```text
GIFTI natif
   ├── surface anatomique ── transformation HiBoP ── affichage/référence scientifique
   └── inflation native
         └── mêmes triangles et mêmes indices
               └── transformation HiBoP ── représentation inflated
```

Chaque `Mesh3D` concerné conserve :

- sa surface anatomique de référence ;
- une variante inflated générée paresseusement ;
- pour un mesh gauche/droite, une variante par hémisphère et une variante fusionnée ;
- un cache en mémoire pour la durée de vie du mesh.

`MeshManager` distingue explicitement :

- `BrainSurface` : géométrie effectivement affichée ;
- `ReferenceSurface` : géométrie anatomique utilisée pour les projections, les atlas et les associations spatiales.

Cette architecture s'applique aussi bien à un mesh persistant chargé depuis un GIFTI qu'à une surface déjà disponible uniquement en mémoire. La différence de repère de calcul entre ces deux cas doit être documentée et testée.

La V1 doit d'abord rendre la représentation inflated statique correcte, puis ajouter une transition animée purement visuelle entre les deux géométries déjà calculées. Les sites restent affichés à leurs coordonnées anatomiques. Leur éventuelle projection sur la surface inflated et le cache disque sont des extensions ultérieures.

## Décisions produit figées pour la V1

| Sujet | Décision proposée |
|---|---|
| Surface source | N'importe quel mesh actuellement sélectionné et géométriquement admissible |
| Représentations | `Anatomical` et `Inflated` uniquement |
| Objectif algorithmique | Inflation visuelle avec régularisation métrique, sans reproduction exacte de Workbench |
| Remise à l'échelle | Isotrope, par conservation du rayon RMS ou de l'aire |
| Frontières | Sommets de bord fixes |
| Composantes connexes | Traitement indépendant autour de leur propre centre |
| Maillage non-manifold | Refus avec diagnostic précis |
| Entrée en mode inflated avec des coupes | Autorisée ; message informatif indiquant que les coupes restent anatomiques et ne découpent pas visuellement l'inflated |
| Création et modification de coupes en mode inflated | Entièrement autorisées, y compris depuis le panneau, les raccourcis et « couper autour du site » |
| Fréquence du message | Une seule fois par instance de visualisation, quel que soit le premier des deux scénarios déclencheurs |
| Sites et électrodes | Conservés à leurs coordonnées anatomiques ; influence et projection calculées sur la surface anatomique de référence |
| Transition animée | Incluse dans la V1, après validation du mode statique |
| Cache | Mémoire par session ; cache disque seulement si les mesures le justifient |

La priorité scientifique exacte — fidélité métrique ou proximité visuelle avec une implémentation de référence — doit être confirmée à l'issue du prototype, sur les surfaces réellement utilisées par HiBoP.

### Portée sémantique de l'inflation

HiBoP ne sait actuellement pas identifier de manière fiable la nature anatomique du mesh sélectionné. Le nom est libre et ne doit pas être utilisé pour autoriser ou refuser l'inflation. L'admissibilité V1 est donc exclusivement géométrique.

L'interface doit néanmoins prévenir l'utilisateur avec un tooltip proche de :

> Génère une représentation gonflée du mesh sélectionné. Cette transformation est conçue pour une surface corticale. Sur un autre type de mesh, le résultat reste une déformation géométrique sans signification anatomique garantie.

Il n'est pas nécessaire de limiter ce message à la matière blanche : l'étude montre que le résultat dépend de la surface corticale source, qui peut notamment être `white`, `pial` ou `midthickness`.

### Terminologie

- **Connectome Workbench** est un outil de neuro-imagerie utilisé notamment dans les pipelines Human Connectome Project. Il sert ici uniquement de référence externe pour comparer les résultats ; il n'est ni embarqué ni requis par HiBoP.
- Un mesh **non-manifold** contient une connectivité ambiguë, par exemple une arête partagée par plus de deux triangles. Les notions d'intérieur, d'extérieur et de voisinage n'y sont plus suffisamment fiables pour garantir l'inflation ; la V1 refuse donc ce cas avec une explication lisible.

## Phase 0 — Contrat produit et baseline

### Objectifs

- figer le périmètre V1, ses interactions avec les coupes et la signification des sites en mode inflated ;
- constituer un corpus de surfaces représentatives ;
- définir les métriques et les références de comparaison ;
- caractériser précisément les transformations appliquées avant affichage.

### Actions

1. Constituer un corpus comprenant au minimum :
   - les deux hémisphères MNI actuels ;
   - plusieurs surfaces patients de densités différentes ;
   - une surface ouverte ;
   - des fixtures synthétiques dégénérées, non-manifold et multicomposantes.
2. Générer les références Workbench et, lorsque possible, FreeSurfer.
3. Mesurer pour chaque surface :
   - nombre de sommets et de triangles ;
   - topologie et composantes connexes ;
   - frontières et arêtes non-manifold ;
   - bounding box, centre, aire et rayon RMS ;
   - distributions des longueurs d'arêtes et des aires de triangles.
4. Classer les transformations `.trm` en transformations rigides, uniformes ou anisotropes.
5. Archiver les paramètres et résultats de référence afin que les comparaisons soient reproductibles.
6. Formaliser les états produit suivants :
   - anatomique sans coupe ;
   - anatomique avec une ou plusieurs coupes ;
   - inflated sans coupe ;
   - inflated avec coupes anatomiques actives mais sans clipping de l'enveloppe inflated ;
   - transition anatomique vers inflated ;
   - transition inflated vers anatomique avec réactivation du clipping seulement à l'état final.
7. Définir une propriété centrale distinguant la présence de coupes de leur effet de clipping sur la géométrie affichée.
8. Définir les textes d'aide expliquant la portée corticale de l'inflation, le maintien des sites dans le repère anatomique et la portée anatomique des coupes.

### Constat MNI initial

Un premier diagnostic des fichiers embarqués donne :

| Hémisphère | Sommets | Triangles | Topologie anatomique/inflated |
|---|---:|---:|---|
| Gauche | 33 036 | 66 068 | Strictement identique |
| Droit | 33 263 | 66 522 | Strictement identique |

L'aire totale des surfaces inflated actuelles vaut environ 51 % de l'aire anatomique et leurs bounding boxes sont environ 4 à 10 % plus petites selon l'axe. Pour le MNI actuel, la correspondance sommet à sommet est donc déjà assurée ; l'écart observé paraît principalement lié à la contraction et à l'échelle.

### Critère de sortie

Le corpus, les références et les métriques sont reproductibles. Les règles de source, de coupes, de sites et de transition sont représentées par une machine d'état produit non ambiguë et par des textes d'interface validés. La création ou la modification d'une coupe ne doit jamais être empêchée par le mode inflated.

## Phase 1 — Prototype algorithmique dans `hbp_core`

### Objectif

Sélectionner un algorithme indépendant, robuste et suffisamment rapide avant de stabiliser une ABI publique.

### Fichiers proposés

```text
hbp_core/src/surface/surface_inflation.h
hbp_core/src/surface/surface_inflation.cpp
hbp_core/tools/native/surface_inflation_export.cpp
hbp_core/tests/native/hbp_core_surface_inflation_test.cpp
```

### Prototypes à comparer

1. Un lissage non rétrécissant simple servant de baseline visuelle.
2. Un lissage avec compensation de contraction et régularisation métrique vers les longueurs d'arêtes originales.

Le calcul doit être écrit indépendamment à partir des principes mathématiques publiés. Le code GPL de Connectome Workbench ne doit pas être repris.

### Pipeline proposé

1. Valider les buffers de positions et de triangles.
2. Construire les arêtes, l'adjacence, les frontières et les composantes connexes.
3. Mémoriser les longueurs d'arêtes, aires, centres et rayons RMS de référence.
4. Effectuer des mises à jour Jacobi dans deux buffers de positions.
5. Appliquer un déplacement Laplacien borné.
6. Compenser la contraction séparément pour chaque composante.
7. Ajouter une force de rappel vers les longueurs d'arêtes originales.
8. Réduire ou rejeter un pas produisant une inversion locale.
9. Recentrer et remettre à l'échelle selon la politique testée.
10. Recalculer les normales et produire le rapport de qualité.

Les positions finales peuvent rester en `float`, mais les réductions, calculs d'aires et métriques de qualité doivent utiliser `double`.

### Critère de sortie

Un algorithme et un preset `Inflated` sont retenus sur la base des résultats visuels, des distorsions mesurées et des performances. Aucune ABI définitive n'est introduite avant cette décision.

## Phase 2 — Robustesse et contrat natif

### Types natifs

Ajouter des types versionnés par `struct_size`, sur le modèle des options et rapports existants :

```text
hbp_SurfaceInflationOptions
hbp_SurfaceInflationReport
hbp_SurfaceInflationJob
```

Le rapport doit inclure au minimum :

- nombre de sommets, triangles et composantes ;
- nombre d'arêtes de frontière et non-manifold ;
- nombre d'itérations réalisées ;
- état de convergence ;
- triangles inversés détectés ;
- ratios d'arêtes et d'aires aux percentiles 50, 90, 95 et 99, plus les maxima ;
- variation d'aire, de bounding box et de rayon RMS ;
- temps de validation, préparation, inflation et finalisation.

### API d'opération

Une fonction bloquante unique ne permet pas de fournir proprement progression et annulation. Utiliser un handle d'opération :

```text
hbp_surface_inflation_create
hbp_surface_inflation_execute
hbp_surface_inflation_get_progress
hbp_surface_inflation_request_cancel
hbp_surface_inflation_take_result
hbp_surface_inflation_get_report
hbp_surface_inflation_destroy
```

Contrat :

- `execute` reste synchrone pour le thread appelant ; HiBoP l'appelle depuis un worker ;
- `get_progress` et `request_cancel` sont thread-safe ;
- la source n'est jamais modifiée ;
- le résultat n'est accessible qu'après succès ;
- `take_result` transfère explicitement l'ownership ;
- une annulation ne publie aucun résultat partiel ;
- les buffers par sommet compatibles, les triangles et le masque de visibilité sont conservés ;
- seules les positions et les normales changent.

L'ABI publique, le smoke test et `hbp_core/baseline/hbp_core_abi_exports.txt` doivent être mis à jour.

### Validation géométrique

Refuser avec un diagnostic précis :

- positions non finies ;
- indices invalides ;
- triangles dégénérés ;
- arêtes de longueur nulle ;
- surface ou bounding box nulle ;
- arête partagée par plus de deux triangles.

Les composantes connexes valides sont traitées indépendamment. Les petits composants parasites doivent être signalés dans le rapport, sans politique de suppression silencieuse.

### Critère de sortie

Les tests natifs prouvent les invariants, les erreurs, l'annulation, le déterminisme et l'absence de résultat partiel.

## Phase 3 — Wrapper C# et gestion du repère

### Wrapper `Surface`

Ajouter dans `Assets/Scripts/HBP/Core/DLL/Surface.cs` :

- les enums et structures correspondant aux options et au rapport ;
- un owner managé pour le handle d'opération ;
- les imports P/Invoke ;
- une méthode asynchrone de haut niveau retournant la surface et son rapport ;
- une méthode publique permettant d'appliquer un `Transformation3` à une surface déjà chargée.

### Inflation avant transformation

`Surface.LoadGIIFile` applique actuellement immédiatement la transformation `.trm`. Une transformation anisotrope modifierait les distances et le comportement de l'algorithme.

Pour les surfaces GIFTI persistantes, la génération paresseuse doit donc :

1. recharger temporairement le GIFTI sans transformation ;
2. l'inflater dans son repère natif ;
3. appliquer ensuite la même transformation que celle de l'anatomique ;
4. publier le résultat ;
5. libérer la surface source temporaire dans un `finally`.

Pour une surface générée en mémoire sans source GIFTI, l'inflation utilise le repère courant et cette différence doit être explicitement documentée dans le rapport ou le modèle métier.

### Critère de sortie

Les tests EditMode prouvent l'ownership, la propagation des erreurs, l'annulation et l'ordre inflation puis transformation.

## Phase 4 — Représentations dérivées dans `Mesh3D`

### Modèle

Ajouter un état de représentation :

```text
SurfaceRepresentation.Anatomical
SurfaceRepresentation.Inflated
```

La variante inflated reste rattachée au `Mesh3D` source. Elle ne doit pas être ajoutée comme un mesh indépendant dans `MeshManager.Meshes`, afin de préserver l'identité, la configuration et les associations de données du mesh anatomique.

Tout `Mesh3D` sélectionné peut demander une inflation. Aucune déduction ne doit être faite à partir de son nom ou de son `MeshType`. Le bouton n'est désactivé qu'en cas d'inadmissibilité géométrique connue, de calcul déjà en cours ou d'état transitoire incompatible.

Pour `LeftRightMesh3D` :

1. inflater gauche et droite indépendamment ;
2. publier les deux résultats seulement si les deux calculs réussissent ;
3. fusionner les résultats pour produire `Both` ;
4. générer ensuite les versions simplifiées ;
5. disposer toutes les surfaces dérivées avec leur owner de scène.

Le chargement et la génération ne doivent plus utiliser d'attente bloquante par `Thread.Sleep` sur le thread Unity. Le nouveau chemin async doit être attendu explicitement.

### Cache

La V1 conserve les variantes inflated en mémoire dans le `Mesh3D`. Une clé doit inclure :

- l'identité géométrique de la source ;
- la version de l'algorithme ;
- le preset et ses paramètres ;
- la politique de frontière et de remise à l'échelle.

Un cache disque ne sera ajouté qu'après mesure du coût réel. S'il devient nécessaire, l'écriture devra être atomique et le cache ne devra contenir que des données applicatives, jamais modifier les fichiers patients.

### Critère de sortie

Les représentations gauche, droite et complète sont générées, sélectionnées, nettoyées et régénérées sans fuite ni collision d'ownership.

## Phase 5 — Intégration dans `MeshManager` et `Base3DScene`

### Séparation affichage/référence

Ajouter à `MeshManager` :

```text
BrainSurface       // représentation affichée
ReferenceSurface   // surface anatomique correspondante
```

Lors du passage en inflated :

- `BrainSurface` devient la variante inflated correspondant à `MeshPartToDisplay` ;
- `ReferenceSurface` reste la surface anatomique gauche, droite ou complète correspondante ;
- `MeshCenter` et la cible caméra utilisent la géométrie affichée ;
- les projections et associations scientifiques utilisent la référence anatomique.

### Activité et atlas

Adapter les consommateurs :

- `SurfaceGenerator.Initialize` reçoit `ReferenceSurface` ;
- les UV obtenus sont appliqués au mesh affiché grâce à l'identité des indices ;
- MarsAtlas conserve ses labels et couleurs par sommet ;
- JuBrain, fMRI et localizers volumétriques sont échantillonnés sur `ReferenceSurface` ;
- changer uniquement de représentation ne reconstruit ni `ActivityProjectionGrid` ni le champ d'activité ;
- une projection calculée sur l'anatomique reste valide sur l'inflated si les invariants topologiques sont respectés.

### Triangle eraser et surfaces simplifiées

Pour la V1 :

- réinitialiser les états temporaires lors du changement de représentation ;
- conserver les masques du mesh complet lorsque leurs triangles sont identiques ;
- ne pas transférer directement un masque vers une version simplifiée de topologie différente ;
- recalculer ou réinitialiser explicitement le masque simplifié.

### Coupes anatomiques avec enveloppe inflated intacte

Les coupes restent entièrement disponibles en mode inflated. Elles conservent leur sens volumique et anatomique, mais ne modifient jamais visuellement l'enveloppe inflated.

Le pipeline doit séparer deux responsabilités actuellement liées à `BrainSurface` :

1. **Calcul anatomique des coupes**
   - `GenerateRawCutSurfaces` et `GenerateCutSurfaces` utilisent `ReferenceSurface` ;
   - les bounding boxes de `UpdateCutPlane` et `CutAroundSelectedSite` utilisent le volume de référence et `ReferenceSurface` ;
   - le mesh simplifié utilisé pour les colliders de coupe provient de la représentation anatomique ;
   - les textures, panneaux, meshes de coupe et modes automatiques continuent d'être recalculés normalement.
2. **Clipping de la surface affichée**
   - en mode anatomique stable, les plans sont transmis aux matériaux du cerveau comme aujourd'hui ;
   - en mode inflated ou pendant une transition, le matériau du cerveau reçoit un nombre de plans de clipping nul ;
   - les matériaux et objets propres aux coupes continuent néanmoins de recevoir les paramètres nécessaires à leur affichage.

Les opérations suivantes restent autorisées sans garde ni désactivation particulière :

- ajout, suppression et modification d'une coupe ;
- ouverture et utilisation du panneau de coupe ;
- raccourcis clavier ;
- modes `StrongCuts` et `RawCuts` ;
- `AutomaticCutAroundSelectedSite` et `CutAroundSelectedSite` ;
- appels programmatiques maintenus.

Lors du retour à l'anatomique, le clipping du cerveau est réactivé à partir des coupes courantes. Il ne s'agit pas d'une restauration d'un ancien snapshot : toutes les modifications effectuées pendant le mode inflated prennent effet immédiatement sur les vues de coupe et sur le cerveau anatomique dès son retour.

### Critère de sortie

La bascule anatomique/inflated ne modifie pas les valeurs fonctionnelles associées aux indices de sommets et ne relance pas les calculs volumétriques inutiles. Les coupes restent éditables et anatomiques, leurs vues continuent de fonctionner, et seule leur action de clipping sur l'enveloppe inflated est neutralisée.

## Phase 6 — Exécution asynchrone et interface

### Orchestration

Utiliser le mécanisme existant de chargement annulable :

1. déclencher la génération au premier passage en mode inflated ;
2. exécuter `hbp_surface_inflation_execute` sur un thread de travail ;
3. interroger la progression sans bloquer le thread principal ;
4. transmettre l'annulation au job natif ;
5. revenir sur le thread principal avant de modifier le mesh Unity et l'interface ;
6. vérifier que le mesh et la scène sources existent toujours avant publication.

La publication doit être transactionnelle : pendant le calcul, l'anatomique reste affiché. En cas d'échec ou d'annulation, aucune référence du `Mesh3D` n'est remplacée.

### Interface

Ajouter, selon le workflow prefab-first du projet :

- un sélecteur `Anatomical / Inflated` dans le prefab approprié ;
- un état désactivé avec explication lorsque la surface n'est pas admissible ;
- la progression et l'annulation via `LoadingManager` ;
- un message d'erreur exploitable à partir du rapport natif ;
- une `DialogBoxType.Informational` avec un unique bouton `OK` sur la portée anatomique des coupes ;
- la persistance de `SurfaceRepresentation` dans `VisualizationConfiguration`, avec `Anatomical` comme valeur par défaut pour les anciens projets.

Le message relatif aux coupes est déclenché dans exactement deux cas :

1. passage en mode inflated alors qu'au moins une coupe existe ;
2. création de la première coupe alors que le mode inflated est affiché.

Cette boîte n'est pas une confirmation : elle ne propose ni annulation ni choix de comportement. Un unique booléen transitoire par instance de visualisation, non sérialisé, déduplique les deux scénarios. Dès que l'information a été affichée, l'autre scénario ne doit plus la déclencher pendant la durée de vie de cette visualisation. Une nouvelle visualisation ou son rechargement peut présenter de nouveau l'information.

Texte proposé :

> En mode inflated, les coupes restent définies dans le repère anatomique. Elles continuent d'alimenter les vues et l'exploration volumique, mais ne découpent pas visuellement la surface inflated.

Tooltips recommandés :

- inflation : préciser que la transformation vise les surfaces corticales mais reste accessible à tout mesh géométriquement admissible ;
- sites : préciser qu'ils restent dans le repère anatomique et que leur influence est calculée relativement au mesh anatomique de base ;
- coupes : préciser qu'elles restent anatomiques et n'ont pas d'effet de clipping sur la surface inflated.

### Sites et électrodes

Les sites et électrodes restent visibles et sélectionnables à leurs coordonnées anatomiques. Ils ne suivent pas la déformation du cerveau.

Leur influence, l'activité volumique et les données projetées continuent d'utiliser `ReferenceSurface` et les coordonnées anatomiques. Ce contrat doit être visible dans le tooltip du mode inflated afin que l'écart spatial apparent ne soit pas interprété comme une erreur de projection.

Le mode « couper autour du site » reste disponible. Les coupes produites utilisent la position anatomique du site, le volume et `ReferenceSurface`, comme dans le mode anatomique.

### Transition animée V1

La transition est ajoutée après validation de la bascule statique et ne déclenche aucun nouveau calcul d'inflation. Elle interpole uniquement les positions et normales anatomiques/inflated déjà disponibles.

La voie privilégiée est une interpolation GPU dans les shaders du cerveau :

1. stocker les positions et normales cibles dans des canaux de sommets supplémentaires ;
2. ajouter un paramètre matériau `_InflationBlend` compris entre `0` et `1` ;
3. interpoler positions et normales dans tous les passes concernés : opaque, transparent, profondeur, normales et edge data ;
4. conserver les UV scientifiques et les couleurs inchangés ;
5. ne mettre à jour le collider du cerveau qu'à la fin de la transition ;
6. suspendre pendant la transition les interactions dépendant précisément du collider ou de la géométrie du cerveau.

Les canaux `TEXCOORD0`, `TEXCOORD1` et `TEXCOORD2` étant déjà utilisés par les UV anatomiques, alpha et scientifiques, le prototype doit vérifier l'utilisation de canaux supplémentaires et mesurer leur coût mémoire sur les meshes les plus denses.

Les états intermédiaires sont exclusivement visuels. `ReferenceSurface`, les sites, les activités, les atlas et toutes les mesures restent anatomiques pendant toute l'animation.

La durée et la courbe d'animation doivent être configurées par une constante produit stable dans la V1, sans exposer de réglages avancés tant que l'expérience n'est pas validée.

### Critère de sortie

Le workflow complet est utilisable depuis une visualisation sans blocage du thread principal, avec progression, annulation, transition fluide, maintien des sites anatomiques et exploration volumique par les coupes sans clipping de l'inflated.

## Phase 7 — Validation et stabilisation

### Tests natifs

- source inchangée ;
- mêmes nombres de sommets et de triangles ;
- tableau de triangles strictement identique ;
- positions et normales finies ;
- conservation des UV, couleurs et masques compatibles ;
- composantes traitées indépendamment ;
- frontières conformes à la politique ;
- refus des maillages invalides et non-manifold ;
- aucun résultat après annulation ;
- déterminisme dans une tolérance documentée ;
- métriques de distorsion correctes sur des oracles synthétiques ;
- absence de fuite sur des cycles répétés de création, exécution, prise de résultat et destruction.

### Tests Unity EditMode

- mapping ABI des structures ;
- ownership du job et de la surface retournée ;
- ordre inflation puis transformation ;
- compatibilité de sérialisation ;
- valeur par défaut anatomique ;
- cache mémoire et invalidation par paramètres ;
- machine d'état anatomique/inflated/transition ;
- sélection correcte de `ReferenceSurface` pour toutes les opérations de coupe ;
- déduplication du message d'information par instance de visualisation.

### Tests Unity PlayMode

- sélection gauche, droite et complète ;
- changement anatomique/inflated avec activité visible ;
- conservation des UV par indice ;
- absence de reconstruction du champ d'activité ;
- atlas et fMRI fondés sur `ReferenceSurface` ;
- annulation pendant le calcul ;
- destruction ou changement de scène pendant le calcul ;
- message informatif lors de l'entrée en inflated avec des coupes ;
- message informatif lors de la première coupe créée en mode inflated ;
- absence de second message lorsque l'autre scénario survient dans la même visualisation ;
- création, suppression et modification de coupes en mode inflated depuis le panneau et les raccourcis ;
- fonctionnement de « couper autour du site » en mode inflated ;
- géométries, textures, panneaux et colliders de coupe calculés depuis `ReferenceSurface` ;
- absence de clipping de l'enveloppe inflated malgré la présence de coupes ;
- réactivation sur le cerveau anatomique des coupes modifiées pendant le mode inflated ;
- maintien, affichage et sélection des sites à leurs coordonnées anatomiques ;
- influence des sites et projection fonctionnelle inchangées par le mode inflated ;
- transition dans les deux sens avec tous les passes de rendu ;
- collider mis à jour uniquement à l'état final ;
- interactions géométriques suspendues pendant la transition ;
- nettoyage et rechargement répétés.

Lorsque l'éditeur Unity est ouvert, exécuter ces tests via Unity MCP conformément aux instructions du projet. Tous les changements C# doivent être formatés avec :

```powershell
.\Tools\format-code.cmd
```

### Benchmarks

Mesurer séparément :

- validation et construction de l'adjacence ;
- temps par itération et par cycle ;
- coût total des presets ;
- mémoire temporaire et mémoire du cache ;
- copie native vers Unity ;
- mémoire des canaux de sommets supplémentaires ;
- coût GPU et CPU de l'animation ;
- premier calcul et réutilisation du cache ;
- comportement sur 32k, 80k, 150k sommets et le plus gros maillage supporté.

Les budgets définitifs doivent être fixés après ces mesures. Une fois les géométries calculées, le changement de représentation ne doit pas dégrader le rendu interactif à 60 FPS.

### Comparaison de référence

Pour chaque surface représentative :

1. produire la sortie HiBoP ;
2. produire les sorties Workbench et FreeSurfer disponibles ;
3. comparer visuellement la visibilité des sillons ;
4. comparer les distributions de distorsion ;
5. vérifier les invariants topologiques ;
6. ne pas exiger une égalité sommet par sommet entre algorithmes différents.

### Critère de sortie

La feature peut être activée par défaut lorsque les tests natifs et Unity passent, que les performances sont acceptables sur le corpus réel et que les limites scientifiques sont documentées dans l'interface et la documentation utilisateur.

## Extensions après la V1

### Projection des électrodes corticales

Projeter chaque contact cortical sur la surface anatomique et mémoriser :

- l'indice du triangle ;
- les coordonnées barycentriques ;
- un éventuel décalage selon la normale.

La position inflated est reconstruite sur le même triangle. Les contacts profonds restent anatomiques ou sont masqués ; ils ne doivent jamais être projetés silencieusement.

### Cache disque

Ajouter un cache applicatif versionné si les benchmarks montrent que le cache mémoire ne suffit pas. La clé devra couvrir la géométrie source, l'algorithme, les paramètres et les politiques géométriques. Les écritures seront atomiques et séparées des données utilisateur.

### Export GIFTI

Proposer un export explicite en `.surf.gii`. Le chargeur GIFTI actuel de `hbp_core` ne conserve que la géométrie et initialise les couleurs ; la conservation de métadonnées GIFTI arbitraires demanderait donc un chantier distinct.

## Ordre de livraison recommandé

1. Baseline et corpus de validation.
2. Prototype natif sans ABI publique définitive.
3. Algorithme robuste et tests natifs.
4. ABI, wrapper C# et exécution asynchrone.
5. Représentation inflated statique dans HiBoP.
6. Séparation complète entre surface affichée et surface de référence.
7. Découplage entre les coupes anatomiques et le clipping de la surface affichée, avec maintien des sites anatomiques.
8. UI, message informatif dédupliqué, tooltips et tests PlayMode de la bascule statique.
9. Transition GPU anatomique/inflated et tests de tous les passes de rendu.
10. Benchmarks et comparaison de référence.
11. Projection optionnelle des électrodes et cache disque seulement après stabilisation de la V1.
