# Inflation dynamique des surfaces corticales

## Statut du document

- **Date de l'enquête :** 26 août 2026
- **Statut :** note d'investigation et proposition d'architecture
- **Périmètre :** génération à la demande d'une représentation `inflated` à partir d'une surface corticale GIFTI dans HiBoP
- **Décision actuelle :** faisable techniquement ; prototype recommandé avant intégration produit

Ce document consigne l'enquête initiale sur la faisabilité, les méthodes existantes, les licences et l'intégration possible dans HiBoP. Il ne constitue pas un avis juridique ni une spécification finale.

## Résumé exécutif

La génération d'une surface corticale `inflated` à partir de la surface anatomique correspondante est faisable. FreeSurfer et Connectome Workbench proposent déjà des solutions reconnues, mais avec des algorithmes et des licences différents.

Pour HiBoP, l'approche recommandée est :

1. Implémenter dans `hbp_core` un algorithme indépendant, sous une licence compatible avec la BSD-3-Clause de HiBoP.
2. Gonfler chaque hémisphère ou composante connexe séparément, puis les fusionner si nécessaire.
3. Conserver strictement la topologie, l'ordre des sommets et les données attachées aux sommets.
4. Exécuter le calcul à la demande sur un thread de travail, puis mettre le résultat en cache.
5. Pour une transition interactive, interpoler au rendu entre les positions anatomiques et inflated déjà calculées, plutôt que de recalculer l'inflation à chaque frame.
6. Utiliser FreeSurfer et Connectome Workbench comme références de validation, sans reprendre directement le code GPL de Workbench.

Cette solution supprimerait le risque d'utiliser un modèle inflated MNI dont l'échelle, les proportions ou la correspondance sommet à sommet ne sont pas garanties par rapport au modèle anatomique affiché.

## Définition de l'inflation corticale

L'inflation corticale est une transformation géométrique destinée à rendre visibles les zones enfouies dans les sillons. Elle ne calcule pas une nouvelle anatomie et ne produit pas une forme anatomique de référence unique.

Une inflation cherche généralement à :

- réduire progressivement les plis de la surface ;
- conserver la topologie du maillage ;
- limiter la déformation des distances, angles ou aires ;
- préserver la correspondance entre un sommet anatomique et son sommet inflated ;
- maintenir une taille et une position globales adaptées à la visualisation.

Le résultat dépend nécessairement :

- de la surface source (`white`, `pial`, `midthickness`, etc.) ;
- de l'algorithme ;
- du nombre d'itérations ;
- des coefficients de lissage et de préservation métrique ;
- de la règle de remise à l'échelle finale.

Il faut donc définir ce que signifie « bonnes proportions » pour HiBoP. Deux objectifs différents peuvent entrer en concurrence :

- **préservation scientifique de la métrique :** minimiser la variation des longueurs d'arêtes et des aires locales ;
- **cohérence visuelle :** faire correspondre le centre et les dimensions XYZ de l'inflated à ceux de la surface anatomique.

FreeSurfer privilégie explicitement la préservation métrique. Workbench remet explicitement les bounding boxes en correspondance à la fin du calcul.

## Méthodes existantes

### FreeSurfer

La méthode décrite par Fischl, Sereno et Dale minimise une énergie combinant principalement :

- une force de type ressort qui lisse la surface ;
- une pénalité de distorsion métrique qui contraint la surface à conserver autant que possible les propriétés de la surface d'origine.

L'intégration numérique utilise une optimisation itérative et multirésolution. L'implémentation `mris_inflate` présente également le procédé comme un maillage de ressorts avec une correction d'aire.

Avantages :

- méthode scientifique de référence ;
- attention explicite à la préservation des distances et des aires ;
- bon candidat si la fidélité métrique est prioritaire.

Inconvénients :

- méthode plus complexe à réimplémenter correctement ;
- dépendances importantes si FreeSurfer est embarqué tel quel ;
- temps de calcul potentiellement supérieur à une méthode purement visuelle ;
- licence spécifique à respecter pour toute reprise de code.

Références :

- [Cortical Surface-Based Analysis II: Inflation, Flattening, and a Surface-Based Coordinate System](https://www.martinos.org/~fischl/reprints/recon2_neurimage_reprint.pdf)
- [`mris_inflate.cpp`](https://github.com/freesurfer/freesurfer/blob/dev/mris_inflate/mris_inflate.cpp)
- [Documentation `mris_inflate`](https://surfer.nmr.mgh.harvard.edu/fswiki/mris_inflate)
- [Documentation FreeSurfer sur l'inflation](https://surfer.nmr.mgh.harvard.edu/fswiki/inflate)

### Connectome Workbench

Workbench fournit la commande :

```text
wb_command -surface-generate-inflated \
  <anatomical-surface-in> \
  <inflated-surface-out> \
  <very-inflated-surface-out>
```

La méthode actuelle enchaîne des cycles de lissage et une expansion destinée à compenser la contraction causée par le lissage. Elle génère successivement une surface intermédiaire, une surface inflated et une surface very-inflated. Les surfaces finales sont remises à l'échelle afin d'avoir la même étendue XYZ que la surface anatomique.

La documentation recommande d'augmenter le facteur d'itérations sur les surfaces très denses, par exemple autour de 150 000 sommets. Les pipelines HCP calculent également ce facteur en fonction de la densité du maillage.

Avantages :

- entrée directe en GIFTI ;
- algorithme relativement simple ;
- résultat adapté à la visualisation ;
- production standard de variantes `inflated` et `very_inflated` ;
- référence particulièrement pertinente pour des données HCP/GIFTI.

Inconvénients :

- code source sous GPL ;
- préservation métrique moins explicite que dans FreeSurfer ;
- l'intégration directe du code compromettrait une distribution HiBoP restant purement BSD.

Références :

- [Documentation `-surface-generate-inflated`](https://humanconnectome.org/software/workbench-command/-surface-generate-inflated)
- [`AlgorithmSurfaceGenerateInflated.cxx`](https://github.com/Washington-University/workbench/blob/master/src/Algorithms/AlgorithmSurfaceGenerateInflated.cxx)
- [`AlgorithmSurfaceInflation.cxx`](https://github.com/Washington-University/workbench/blob/master/src/Algorithms/AlgorithmSurfaceInflation.cxx)
- [`AlgorithmSurfaceSmoothing.cxx`](https://github.com/Washington-University/workbench/blob/master/src/Algorithms/AlgorithmSurfaceSmoothing.cxx)

### VTK et lissage générique

`vtkWindowedSincPolyDataFilter` fournit un lissage non rétrécissant basé sur un filtre passe-bas approché par des polynômes de Chebyshev. Il conserve la connectivité et permet de contrôler les frontières, les arêtes caractéristiques et les maillages non-manifold.

Cette méthode peut servir de prototype ou de baseline permissive, mais elle ne cherche pas explicitement à préserver la métrique corticale. Un lissage excessif peut supprimer des détails importants sans produire le compromis attendu d'une véritable inflation corticale.

Références :

- [`vtkWindowedSincPolyDataFilter`](https://vtk.org/doc/nightly/html/classvtkWindowedSincPolyDataFilter.html)
- [Dépôt et licence VTK](https://github.com/Kitware/VTK)

## Entrées GIFTI admissibles

Il n'est pas possible de gonfler littéralement n'importe quel fichier `.gii`, car GIFTI peut contenir des labels, des métriques, des séries temporelles ou d'autres données sans géométrie.

Une entrée admissible doit être un fichier de surface comprenant au minimum :

- un tableau `NIFTI_INTENT_POINTSET` de forme `N x 3` ;
- un tableau `NIFTI_INTENT_TRIANGLE` de forme `M x 3` ;
- des indices de triangles compris entre `0` et `N - 1` ;
- des coordonnées finies ;
- une connectivité exploitable.

Référence : [GIFTI Surface Data Format, Version 1.0](https://www.nitrc.org/frs/download.php/2871/GIFTI_Surface_Format.pdf).

### Validations nécessaires

Avant inflation, l'implémentation devrait vérifier :

- présence des tableaux de points et de triangles ;
- absence d'indices invalides ;
- absence de coordonnées `NaN` ou infinies ;
- absence de triangles dégénérés ou d'arêtes de longueur nulle ;
- nombre et taille des composantes connexes ;
- arêtes de frontière ;
- arêtes partagées par plus de deux triangles ;
- orientation cohérente des triangles lorsque cela est nécessaire ;
- surface totale et bounding box non nulles.

### Cas particuliers

#### Hémisphères séparés

Chaque hémisphère doit être gonflé autour de son propre centre. Les deux surfaces peuvent être fusionnées après inflation.

#### Deux hémisphères dans une surface unique

Si le fichier contient deux composantes connexes, elles doivent être traitées séparément. Une inflation autour du centre global rapprocherait ou déformerait incorrectement les hémisphères.

#### Surface ouverte

Une ouverture au niveau de la paroi médiale exige une politique de frontière :

- sommets de bord fixes ;
- lissage tangentiel du bord ;
- lissage identique à l'intérieur, avec risque de déformation de l'ouverture ;
- fermeture temporaire, si une méthode le justifie scientifiquement.

Cette politique doit être documentée et testée.

#### Surface non-manifold

Le comportement le plus sûr est de refuser le calcul avec un diagnostic précis. Une option expérimentale pourrait traiter séparément certaines composantes réparables, mais elle ne devrait pas être silencieuse.

#### Structures non corticales

Un algorithme géométrique peut lisser un cervelet, une structure sous-corticale ou un maillage quelconque, mais le terme `inflated cortex` n'a alors pas nécessairement de sens. L'interface devrait parler de surface corticale admissible plutôt que de promettre l'inflation de tout GIFTI.

## Faisabilité temps réel

### Génération à la demande

Les méthodes considérées effectuent des passes locales sur les sommets et leurs voisins. Pour `K` itérations, la complexité attendue est approximativement :

```text
Temps   O(K × (V + F))
Mémoire O(V + F)
```

avec `V` le nombre de sommets et `F` le nombre de faces.

Cela convient à un calcul à la demande sur un thread de travail. Les performances exactes doivent être mesurées sur les GIFTI réellement utilisés par HiBoP, notamment les surfaces de 32k, 80k et 150k sommets ou davantage.

Le calcul ne doit pas bloquer le thread principal Unity. Il doit fournir :

- une progression ;
- une annulation ;
- une limite d'itérations ;
- un diagnostic en cas d'échec ;
- un résultat transactionnel, publié seulement lorsque le calcul est terminé.

### Interaction à 60 FPS

Recalculer une optimisation complète à chaque frame n'est pas nécessaire. L'approche recommandée est :

1. Calculer une ou plusieurs géométries cibles (`low`, `inflated`, `very inflated`).
2. Conserver les positions originales et cibles dans des buffers distincts.
3. Afficher une interpolation entre ces buffers dans un shader ou un compute shader.

Une interpolation linéaire sommet par sommet est suffisante pour une animation visuelle, mais les états intermédiaires ne sont pas eux-mêmes le résultat d'une optimisation métrique. Ils peuvent également présenter ponctuellement des auto-intersections. Les calculs scientifiques, projections ou mesures ne doivent pas considérer ces états interpolés comme des surfaces validées.

### Cache

Le cache devrait être indexé par une clé comprenant :

- un hash des positions et des triangles de la surface source ;
- l'identifiant et la version de l'algorithme ;
- les paramètres d'inflation ;
- la politique de frontière ;
- éventuellement la précision numérique et la plateforme si les résultats ne sont pas strictement déterministes.

Le cache applicatif est préférable à une écriture automatique à côté des données utilisateur. Une exportation explicite en `.surf.gii` peut être proposée séparément.

## Intégration dans l'architecture HiBoP

### État actuel pertinent

La couche C# `HBP.Core.DLL.Surface` sait déjà :

- charger une surface GIFTI dans `hbp_core` ;
- cloner une surface ;
- recalculer les normales ;
- fusionner des surfaces ;
- simplifier une surface ;
- copier les buffers natifs dans un `UnityEngine.Mesh` ;
- définir des buffers de sommets et triangles.

Fichier concerné : [`Assets/Scripts/HBP/Core/DLL/Surface.cs`](../../../Assets/Scripts/HBP/Core/DLL/Surface.cs).

`MNIObjects` charge actuellement des fichiers distincts :

- `MNI_Lwhite.gii` et `MNI_Rwhite.gii` ;
- `MNI_Lwhite_inflated.gii` et `MNI_Rwhite_inflated.gii`.

Fichier concerné : [`Assets/Scripts/HBP/Core/Object3D/MNIObjects.cs`](../../../Assets/Scripts/HBP/Core/Object3D/MNIObjects.cs).

`LeftRightMesh3D` charge déjà les hémisphères séparément, puis clone et fusionne les surfaces complètes. C'est le bon niveau pour garantir une inflation indépendante des deux hémisphères.

Fichier concerné : [`Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs`](../../../Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs).

`MeshManager` sélectionne ensuite la surface complète ou un hémisphère et met à jour le mesh Unity.

Fichier concerné : [`Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs`](../../../Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs).

### Frontière d'implémentation recommandée

Le calcul devrait être implémenté dans `hbp_core`, puis exposé par un wrapper fin dans `Surface.cs` :

```text
hbp_surface_inflate(
    source,
    options,
    out inflated_surface,
    out inflation_report)
```

Une forme C# correspondante pourrait être :

```text
Surface Inflate(InflationOptions options, out InflationReport report)
```

Le contrat proposé est :

- la surface source n'est jamais modifiée ;
- la surface de sortie possède le même nombre de sommets ;
- les triangles et leur ordre sont identiques ;
- les couleurs, UV, masques et métadonnées compatibles sont conservés ;
- seules les positions et les normales changent ;
- les erreurs ne laissent pas de surface partiellement publiée ;
- le résultat rapporte les métriques de qualité et avertissements.

### Ordre des opérations

Ordre recommandé :

1. Charger les coordonnées natives et la topologie.
2. Valider le maillage.
3. Séparer les composantes ou hémisphères.
4. Calculer l'inflation dans le repère natif.
5. Recalculer les normales.
6. Appliquer à l'anatomique et à l'inflated la même transformation vers le repère HiBoP.
7. Fusionner les hémisphères si nécessaire.
8. Générer séparément les versions simplifiées.

Il est préférable d'inflater avant une transformation affine anisotrope, qui modifierait les distances et donc le comportement de l'algorithme. Une transformation rigide ne pose pas ce problème.

### Chargement paresseux

L'inflated ne devrait pas être généré systématiquement pour tous les maillages préchargés. Une stratégie paresseuse évite des coûts inutiles :

- génération au premier passage en mode inflated ;
- réutilisation du cache lors des passages suivants ;
- possibilité de précharger explicitement si les préférences le demandent.

### MNI

Avant de remplacer les fichiers MNI actuels, il faut comparer entre les variantes anatomique et inflated :

- nombre de sommets ;
- tableau complet des triangles ;
- ordre des sommets ;
- centre ;
- bounding box ;
- aire totale ;
- longueur des arêtes ;
- transformation appliquée.

Si les topologies sont strictement identiques, le problème actuel peut être principalement un problème d'échelle ou de repère. Une correction de bounding box pourrait suffire pour MNI, même si la génération dynamique reste utile pour les surfaces patients.

Si les topologies diffèrent, il n'existe pas de correspondance directe fiable entre les sommets ; générer l'inflated depuis la surface anatomique devient la solution préférable.

## Données fonctionnelles, atlas et électrodes

### Données attachées aux sommets

Si l'ordre des sommets et les triangles restent identiques :

- les couleurs par sommet restent valides ;
- les labels et parcelles restent associés au bon sommet ;
- les activités surfaciques restent directement transportables ;
- les masques de visibilité fondés sur les triangles peuvent être réutilisés.

Il s'agit d'un avantage majeur par rapport à l'utilisation d'un inflated provenant d'une autre source ou d'un autre maillage.

### Électrodes et sites

Les coordonnées anatomiques des électrodes ne suivent pas automatiquement la surface inflated. Plusieurs politiques produit sont possibles :

1. Conserver les électrodes à leurs coordonnées anatomiques.
2. Masquer les électrodes en mode inflated.
3. Projeter les électrodes corticales sur la surface anatomique, mémoriser le triangle et les coordonnées barycentriques, puis reconstruire leur position sur le même triangle inflated.
4. Distinguer les contacts corticaux des contacts profonds et appliquer des politiques différentes.

La troisième option donne la meilleure continuité visuelle pour les contacts corticaux :

```text
position_anatomique projetée
    -> triangle source + coordonnées barycentriques
    -> même triangle de la surface inflated
    -> position inflated + éventuel décalage selon la normale
```

Les électrodes profondes n'ont pas de correspondance surfacique naturelle. Leur projection ferait perdre l'information de profondeur et ne doit pas être appliquée silencieusement.

### Coupes

L'idée initiale prévoit l'inflation en l'absence de coupe. C'est une première restriction raisonnable.

Lorsque la topologie reste identique, un masque de triangles existant peut techniquement être appliqué à l'inflated. En revanche :

- un plan de coupe défini en coordonnées anatomiques ne représente plus la même région spatiale ;
- une coupe recalculée géométriquement sur la surface déformée peut sélectionner d'autres triangles ;
- les états interpolés compliquent encore cette correspondance.

Pour une première version, le mode inflated devrait donc être désactivé ou clairement limité lorsque des coupes géométriques sont actives.

## Licences et propriété intellectuelle

### Principe général

Le droit d'auteur protège le code source et la forme d'expression d'une méthode. Il ne protège généralement pas l'idée, la procédure, la méthode d'opération ou le concept mathématique sous-jacent.

Référence : [OMPI — What Can I Protect with a Copyright?](https://www.wipo.int/en/web/copyright/protection).

Une implémentation indépendante d'une méthode décrite dans un article scientifique peut donc recevoir sa propre licence, à condition de ne pas copier une implémentation existante soumise à une licence incompatible.

Les brevets restent une question distincte. En Europe, un programme « en tant que tel » est exclu de la brevetabilité, mais une invention mise en œuvre par ordinateur peut être brevetable lorsqu'elle produit un effet technique supplémentaire.

Référence : [OEB — Guidelines for Examination, Computer programs](https://www.epo.org/en/legal/guidelines-epc/2026/g_ii_3_6.html).

La recherche initiale n'a pas identifié de brevet visant précisément l'inflation corticale publiée par Fischl, Sereno et Dale en 1999. Elle a trouvé des brevets plus récents utilisant le terme `surface inflation` pour la création de formes 3D depuis des contours 2D, avec contraintes de normales ou de courbure. Ces revendications paraissent éloignées du lissage d'une surface corticale déjà maillée, mais cette observation ne constitue pas une étude formelle de liberté d'exploitation.

Une validation juridique reste recommandée avant un usage commercial ou clinique sensible.

### FreeSurfer

La licence FreeSurfer actuelle accorde notamment une licence :

- sans redevance ;
- non exclusive ;
- avec droit d'utiliser, reproduire, modifier, afficher et distribuer ;
- avec droit d'incorporer le logiciel dans des programmes propriétaires.

Elle impose toutefois notamment :

- de reproduire les termes applicables dans les copies, sous-licences et documentations ;
- de conserver les attributions et mentions de copyright ;
- d'identifier clairement les versions modifiées ;
- de ne pas utiliser les noms et marques pour promouvoir le produit ;
- de vérifier les droits relatifs aux dépendances tierces.

La licence précise également que le logiciel a été conçu pour la recherche et n'est pas recommandé comme application clinique.

Référence : [FreeSurfer Software License Agreement](https://github.com/freesurfer/freesurfer/blob/dev/LICENSE.txt).

Conclusion : le code FreeSurfer est potentiellement intégrable, y compris dans un produit propriétaire, mais il n'est pas « sans contraintes ». La reprise d'une petite partie de `mris_inflate` nécessiterait aussi un audit de ses dépendances et des fichiers transitivement repris.

### Connectome Workbench

Le code source Workbench est sous GPL v2 ou ultérieure. Le dépôt indique que les exécutables actuels sont effectivement sous GPLv3 en raison d'une dépendance GPLv3, `libCZI`.

Références :

- [Licence officielle Workbench](https://dp.humanconnectome.org/software/connectome-workbench-license)
- [Dépôt Workbench](https://github.com/Washington-University/workbench)

Conséquences :

- copier ou lier son code dans HiBoP imposerait des obligations GPL sur l'ensemble combiné lors de la distribution ;
- cela est incompatible avec l'objectif de conserver une distribution HiBoP uniquement sous BSD-3-Clause ;
- exécuter un `wb_command` installé séparément peut rester une interaction entre programmes distincts ;
- distribuer l'exécutable Workbench avec HiBoP demanderait malgré tout une conformité GPL complète ;
- la sortie produite par un programme GPL n'est en général pas couverte par la GPL simplement parce qu'elle a été calculée par ce programme.

Référence complémentaire : [GNU GPL FAQ](https://www.gnu.org/licenses/gpl-faq.en.html).

Conclusion : Workbench est très utile comme oracle de développement ou outil externe optionnel, mais son code ne devrait pas être recopié dans `hbp_core`.

### VTK

VTK est distribué sous BSD-3-Clause, une licence compatible avec HiBoP. L'utilisation de `vtkWindowedSincPolyDataFilter` serait juridiquement simple sous réserve de conserver les notices nécessaires.

Son coût principal est technique : taille de la dépendance et adéquation scientifique limitée par rapport à une inflation corticale dédiée.

### Matrice de décision

| Option | Licence | Compatibilité avec HiBoP BSD | Adéquation scientifique | Recommandation |
|---|---|---:|---:|---|
| Embarquer FreeSurfer | Licence MGH spécifique | Possible avec obligations | Élevée | Non pour un premier prototype ; dépendance trop lourde |
| Reprendre du code FreeSurfer | Licence MGH spécifique | Possible après audit | Élevée | Possible mais moins simple qu'une implémentation indépendante |
| Reprendre du code Workbench | GPL v2+ | Non pour une distribution purement BSD | Bonne pour la visualisation | À éviter |
| Appeler `wb_command` externe | GPL, programme séparé | Possible sous conditions | Bonne | Utile comme outil optionnel ou oracle |
| Utiliser VTK Windowed Sinc | BSD-3 | Oui | Moyenne | Baseline ou prototype |
| Implémentation indépendante dans `hbp_core` | BSD-3 choisie par HiBoP | Oui | À valider | Option recommandée |

## Algorithme recommandé pour un prototype

### Objectif V1

Construire une inflation visuelle robuste, rapide et déterministe, inspirée des principes publiés mais écrite indépendamment.

Étapes possibles :

1. Cloner positions, triangles et données par sommet.
2. Construire l'adjacence et les composantes connexes.
3. Stocker les métriques de référence : longueurs d'arêtes, aires, centre et bounding box.
4. Appliquer plusieurs échelles de lissage local, par exemple un Laplacien pondéré par aire ou un Laplacien cotangent.
5. Après chaque cycle, compenser la contraction par une expansion contrôlée autour du centre de chaque composante.
6. Ajouter si nécessaire une force de ressort vers les longueurs d'arêtes originales.
7. Limiter le pas pour éviter les inversions de triangles.
8. Recentrer et appliquer une règle explicite de remise à l'échelle.
9. Recalculer les normales.
10. Produire un rapport de qualité.

Le prototype peut commencer par un schéma simple proche conceptuellement de Workbench, puis ajouter une pénalité métrique si les mesures de distorsion sont insuffisantes.

### Paramètres envisageables

```text
InflationOptions
  preset: Low | Inflated | VeryInflated | Custom
  cycles
  iterationsPerCycle
  smoothingStrength
  expansionStrength
  metricPreservationStrength
  boundaryPolicy
  finalScalePolicy
  convergenceTolerance
  maximumDisplacementPerIteration
```

Pour l'interface utilisateur, il est préférable de proposer des presets stables et reproductibles. Les paramètres avancés peuvent rester réservés au développement ou à une configuration experte.

### Politique de remise à l'échelle

Options à comparer :

- conserver l'aire totale originale ;
- conserver le rayon RMS ;
- conserver un facteur isotrope dérivé de la bounding box ;
- faire correspondre séparément les trois étendues XYZ comme Workbench ;
- ne pas remettre à l'échelle et mesurer la variation.

Faire correspondre indépendamment X, Y et Z garantit les proportions visuelles de la bounding box mais introduit une transformation anisotrope supplémentaire. Une conservation isotrope de l'aire ou du rayon préserve mieux la géométrie, mais peut modifier l'étendue selon certains axes.

La décision doit découler de l'usage scientifique attendu dans HiBoP.

## Critères de validation

### Invariants structurels

- même nombre de sommets ;
- même nombre de triangles ;
- tableau de triangles strictement identique ;
- aucun sommet non fini ;
- aucune donnée par sommet perdue ;
- déterminisme dans une tolérance documentée.

### Qualité géométrique

- distribution des ratios de longueur d'arête ;
- distribution des ratios d'aire des triangles ;
- aire totale ;
- centre et bounding box ;
- nombre de triangles inversés ;
- nombre d'auto-intersections nouvelles ;
- courbure moyenne ou mesure équivalente du niveau de pli ;
- convergence et nombre d'itérations effectives.

Le rapport devrait au minimum fournir médiane, 90e, 95e et 99e percentiles des distorsions, ainsi que leurs maxima.

### Validation fonctionnelle

- conservation des couleurs et labels par sommet ;
- maintien de MarsAtlas lorsque la topologie est identique ;
- projection correcte des activités surfaciques ;
- comportement documenté des électrodes ;
- masques de triangles cohérents ;
- sélection gauche, droite et deux hémisphères ;
- nettoyage et ownership corrects des surfaces natives ;
- annulation sans fuite ni état partiel.

### Validation de référence

Sur plusieurs surfaces représentatives :

1. Générer des références avec `wb_command -surface-generate-inflated`.
2. Générer des références avec FreeSurfer lorsque le format et la topologie le permettent.
3. Comparer qualitativement les sillons visibles.
4. Comparer les métriques de distorsion.
5. Ne pas exiger une égalité sommet par sommet entre algorithmes différents.

### Performance

Mesurer séparément :

- construction de l'adjacence ;
- temps par itération ;
- temps total par preset ;
- mémoire temporaire ;
- coût de copie natif vers C# et Unity ;
- coût du premier calcul et du cache ;
- comportement sur 32k, 80k, 150k sommets et le plus gros maillage réellement supporté.

Les budgets de performance ne doivent être fixés qu'après ces mesures.

## Plan de travail proposé

### Phase 0 — diagnostic MNI

- comparer les fichiers MNI anatomiques et inflated actuels ;
- vérifier la correspondance stricte des topologies ;
- mesurer les bounding boxes, aires et distorsions ;
- déterminer si le problème actuel est une simple incohérence d'échelle.

### Phase 1 — prototype hors interface

- ajouter une primitive d'inflation indépendante dans `hbp_core` ;
- traiter une composante connexe ;
- fournir un rapport de validation ;
- produire des sorties GIFTI ou OBJ pour comparaison visuelle ;
- comparer avec Workbench et FreeSurfer.

### Phase 2 — robustesse

- ajouter les composantes multiples et hémisphères ;
- définir la politique de frontière ;
- rejeter proprement les maillages non-manifold ;
- ajouter annulation, progression et déterminisme ;
- couvrir les invariants et métriques par des tests natifs.

### Phase 3 — intégration HiBoP

- ajouter le wrapper C# ;
- intégrer le calcul asynchrone sans blocage du thread Unity ;
- ajouter le chargement paresseux et le cache ;
- conserver les données attachées aux sommets ;
- intégrer un preset `Inflated` dans la sélection de maillage.

### Phase 4 — interaction et produit

- ajouter une transition anatomique/inflated ;
- définir le comportement des sites et électrodes ;
- définir le comportement en présence de coupes ;
- ajouter progression, annulation, erreurs et export ;
- documenter les limites scientifiques.

## Décisions à prendre ultérieurement

1. La priorité est-elle la fidélité métrique ou une forme visuellement proche de Workbench ?
2. La surface source par défaut doit-elle être `white`, `pial` ou `midthickness` ?
3. Faut-il un unique mode `Inflated` ou aussi `Low` et `Very inflated` ?
4. Quelle règle de remise à l'échelle définit les « bonnes proportions » ?
5. Les sites corticaux doivent-ils être projetés sur l'inflated ?
6. Que faire des électrodes profondes ?
7. Le mode inflated est-il interdit lorsque des coupes sont actives ?
8. Le résultat est-il seulement mis en cache ou peut-il être exporté en GIFTI ?
9. Quel niveau de non-manifold ou de frontière doit être accepté ?
10. Une validation juridique formelle est-elle requise pour le modèle de distribution prévu ?

## Conclusion

Il n'existe pas de blocage algorithmique ou de licence fondamental. Le principal risque serait de reprendre directement le code GPL de Connectome Workbench ou de promettre une inflation fiable pour tout fichier GIFTI sans valider sa géométrie.

Une implémentation indépendante dans `hbp_core`, conservant la topologie, calculée en arrière-plan et mise en cache, correspond bien à l'architecture actuelle de HiBoP. Elle permettrait de générer une surface inflated depuis le maillage réellement affiché, avec une correspondance exacte des sommets et des données associées.

La prochaine étape la plus rentable est un prototype natif limité au MNI et à quelques surfaces patients représentatives, accompagné de métriques de distorsion et d'une comparaison avec Workbench.
