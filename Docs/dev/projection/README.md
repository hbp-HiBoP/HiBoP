# Plan d'implémentation de la projection d'activité volumique

## 1. Statut et rôle du document

**Statut :** phases 0 à 6 implémentées et validées

**Date de la décision :** 13 août 2026

**Prochaine étape :** plan achevé ; les évolutions fMRI et CCEP relèvent de
chantiers distincts.

Ce document est la référence canonique pour faire évoluer progressivement la
projection d'activité de HiBoP. Il consolide les décisions prises à la suite de
l'audit de `GeneratorSurface` et doit être relu avant de commencer chaque phase.

Après chaque phase :

1. mettre à jour son statut et consigner les résultats de validation ici ;
2. documenter tout écart entre l'architecture prévue et l'implémentation ;
3. ne commencer la phase suivante que lorsque sa gate de sortie est satisfaite.

L'objectif est de réaliser des changements vérifiables et réversibles, sans
regrouper le refactoring natif, les invalidations Unity et l'export NIfTI dans
une seule modification difficile à diagnostiquer.

## 2. Décisions acquises

Les décisions suivantes sont considérées comme validées :

1. La projection d'activité est un champ volumique. La surface affichée est une
   cible d'échantillonnage de ce champ, pas une composante de sa géométrie.
2. `GeneratorSurface` doit être remplacé par le concept
   `ActivityProjectionGrid`, qui ne contient aucun sommet de surface.
3. Changer la surface affichée ne doit pas reconstruire la grille ni recalculer
   l'activité lorsque le volume, la configuration de grille et les données
   d'activité n'ont pas changé.
4. La grille de visualisation interactive et la grille d'export Localizer ont
   des réglages indépendants.
5. La grille est définie dans le repère du volume de référence. Une surface et
   un volume incompatibles ne doivent jamais provoquer une union implicite de
   leurs bounding boxes.
6. La compatibilité surface/volume est évaluée à partir des échantillons
   réellement projetables, et pas uniquement à partir de l'intersection de
   deux AABB.
7. Aucun message utilisateur de compatibilité ne doit être affiché lors de la
   seule sélection d'un volume ou d'une surface. Une demande manuelle présente
   le diagnostic avant calcul avec « Continue / Cancel » ; une demande
   automatique incompatible attend silencieusement une géométrie compatible.
8. Le calcul du diagnostic doit être intégré à la construction des stencils et
   mis en cache. Une passe complète supplémentaire sur la surface est exclue.
9. Le rendu CCEP n'est pas refondu dans ce chantier. Son comportement courant
   est conservé avec l'invalidation minimale nécessaire en cas de changement
   automatique de mode.
10. Les overlays fMRI gérés directement par `FMRIManager` restent hors du
    pipeline unifié dans un premier temps. Ils pourront être migrés dans un
    chantier ultérieur.
11. L'ancienne ABI `GeneratorSurface` est supprimée de HiBoP et de `hbp_core`.
    `hbp_suite` et HiBoP HoloLens ne sont plus des consommateurs maintenus depuis
    la migration vers `hbp_core` et n'imposent donc aucune compatibilité.

## 3. État actuel à corriger

### 3.1 Responsabilités mélangées

Le `GeneratorSurface` natif contient actuellement :

- la surface cible ;
- le volume de référence ;
- les sommets de la surface concaténés aux points de grille ;
- les dimensions de grille et l'interpolation ;
- les caches de recherche spatiale utilisés par les générateurs d'activité.

Cette structure oblige les générateurs à calculer et stocker des valeurs sur
des sommets de surface qui ne sont plus utilisés comme source du rendu
d'activité. Elle lie aussi artificiellement la durée de vie du champ volumique
à celle du mesh affiché.

### 3.2 Invalidation trop large

`Base3DScene.UpdateGeneratorsAndUnityMeshes()` recrée actuellement le
`GeneratorSurface`, réinitialise toutes les colonnes et met à jour les meshes en
une seule opération. `MeshManager` et `TriangleEraser` appellent
`ResetGenerators()`, ce qui invalide l'activité pour des modifications qui ne
concernent que la surface ou sa visibilité.

### 3.3 Grille d'export couplée à l'affichage

L'export Localizer utilise `ActivityProjectionSettings.VolumeGridDimension`,
dont la valeur par défaut est 80. Une résolution choisie pour maintenir un rendu
interactif fluide détermine donc aussi la résolution scientifique et le poids du
NIfTI exporté.

### 3.4 Géométrie NIfTI reconstruite

La grille actuelle est échantillonnée dans une bounding box en coordonnées
monde, tandis que l'affine d'export est reconstruite depuis celle du volume de
référence. Ce contrat est ambigu pour les volumes anisotropes ou obliques. La
géométrie exacte de la grille doit devenir une donnée de première classe et être
réutilisée sans approximation par la visualisation, les coupes et l'export.

## 4. Architecture cible

```text
Volume de référence
        |
        v
ActivityProjectionGrid
  - dimensions
  - gridToWorld / worldToGrid
  - points de grille uniquement
  - version de géométrie
  - caches de recherche spatiale nécessaires au calcul
        |
        +------------------------------+
        |                              |
        v                              v
ActivityField                    Export NIfTI
  - valeurs par voxel             - affine exacte de la grille
  - timeline                      - dimensions exactes
  - normalisation                 - réglages d'export indépendants
        |
        +------------------------------+
        |                              |
        v                              v
SurfaceProjectionBinding         CutProjectionBinding
  - surface cible                 - géométrie de coupe
  - stencils mis en cache         - stencils mis en cache
  - rapport de couverture         - textures de coupe
  - UV d'activité
```

### 4.1 Contrat de géométrie de grille

La grille porte explicitement :

- ses dimensions entières ;
- sa transformation `gridToWorld` ;
- son inverse `worldToGrid` ;
- la position de ses centres d'échantillonnage ;
- une version ou une clé stable permettant de valider les caches.

Pour une grille couvrant les centres du premier au dernier voxel du volume de
référence, le pas d'un axe doit être calculé à partir des intervalles :

```text
scale = (referenceDimension - 1) / (gridDimension - 1)
```

Le cas d'une dimension inférieure à 2 reste invalide. Les matrices, et non les
seules tailles d'AABB, définissent le repère d'échantillonnage.

### 4.2 Clés d'identité recommandées

Les caches ne doivent pas dépendre d'un booléen global. Les clés minimales sont :

- `ProjectionGridKey` : identité et géométrie du volume, dimensions de grille ;
- `ActivityFieldKey` : grille, données source, sites, états, ROI, rayon
  d'influence et paramètres qui modifient les valeurs calculées ;
- `SurfaceProjectionKey` : grille, version géométrique de la surface et mode
  d'interpolation ;
- `CutProjectionKey` : grille, géométrie de coupe et mode d'interpolation.

Une première implémentation peut utiliser des compteurs de version plutôt que
des hashes complexes, à condition que leur propriétaire et leurs déclencheurs
soient explicites.

## 5. Stratégie d'invalidation

| Invalidation | Déclencheurs principaux | Travail autorisé |
| --- | --- | --- |
| `ProjectionGridNeedsUpdate` | changement de volume de référence ou de dimensions de grille | reconstruire la grille, ses caches, puis tous les champs d'activité qui en dépendent |
| `ActivityFieldNeedsUpdate` | données d'activité, sites, implantation, états, ROI, masque ou rayon d'influence | recalculer l'activité sur la grille existante |
| `SurfaceProjectionNeedsUpdate` | changement de surface ou d'interpolation de surface | reconstruire les stencils, le rapport de couverture et les UV ; ne pas recalculer le champ |
| `SurfaceMeshNeedsUpdate` | partie de mesh, effacement/restauration de triangles, visibilité | mettre à jour le mesh ou ses masques uniquement |
| `CutProjectionNeedsUpdate` | géométrie ou interpolation d'une coupe | reconstruire les stencils ou textures de coupe uniquement |
| `FunctionalAppearanceNeedsUpdate` | temps courant, palette, alpha ou calibration n'affectant pas le champ brut | mettre à jour les couleurs/UV/textures uniquement |

Changer de surface doit laisser `ActivityField` valide et ne doit pas arrêter
une timeline en cours. Changer de volume invalide initialement la grille et le
champ ; une éventuelle réutilisation entre deux volumes de géométrie identique
est une optimisation ultérieure, pas un prérequis.

Pour CCEP, la sélection du mesh ne déclenche pas elle-même un recalcul global.
Si les capacités du nouveau mesh provoquent réellement un changement de mode ou
de source CCEP, ce changement marque explicitement `ActivityFieldNeedsUpdate`.
Aucune autre refonte CCEP n'est incluse.

## 6. Validation surface/volume à la demande

### 6.1 Moment du diagnostic

La sélection d'un volume ou d'une surface ne produit ni popup, ni toast, ni
warning utilisateur. Elle marque seulement le binding de surface comme obsolète.

Le binding est construit paresseusement au premier besoin réel de projection,
par exemple lorsque :

- une colonne d'activité visible doit produire ses UV de surface ;
- l'utilisateur demande explicitement le calcul ou l'affichage de l'activité ;
- une activité déjà calculée doit être appliquée à une nouvelle surface.

Ainsi, une séquence « changer le volume, puis changer la surface » ne montre
aucun message intermédiaire.

### 6.2 Coût et cache

La boucle qui construit les indices nearest ou les stencils trilinéaires compte
simultanément les sommets valides. Le diagnostic ne lance pas de seconde boucle.

Le résultat est un `SurfaceProjectionCoverage` mis en cache avec le binding :

```text
totalVertexCount
validVertexCount
invalidVertexCount
validRatio
classification
```

Sa clé est `SurfaceProjectionKey`. Il n'est recalculé que si la géométrie de la
grille, la géométrie de la surface ou l'interpolation change. Il n'est jamais
recalculé à chaque frame ou à chaque pas temporel.

Une vérification AABB peut servir de rejet rapide avant la construction des
stencils, mais elle ne remplace pas le rapport de couverture.

### 6.3 Politique utilisateur

- **Couverture nulle :** ne pas projeter l'activité sur cette surface ; afficher
  une erreur claire uniquement au moment de la demande de projection. Les
  coupes, l'export et le reste de la scène restent utilisables.
- **Couverture partielle significative :** effectuer la projection sur les
  sommets valides et afficher un warning indiquant le pourcentage couvert.
- **Couverture complète ou quasi complète :** ne rien afficher.

Le seuil de warning doit tolérer les erreurs numériques de bord. Sa valeur
exacte sera fixée en phase 3 à partir des surfaces réelles ; une valeur initiale
raisonnable à évaluer est plus de `max(32 sommets, 1 %)` invalides.

Les messages sont dédupliqués par `SurfaceProjectionKey` et par demande de
projection. Ils mentionnent les noms de la surface et du volume et suggèrent de
vérifier le repère ou le recalage. Aucun message identique ne doit être répété à
chaque rafraîchissement ou frame.

## 7. Réglages de l'export Localizer

L'export possède un objet dédié, par exemple :

```text
LocalizerExportGridSettings
  - resolutionMode
  - targetVoxelSizeMm
  - optionalMaximumDimension
  - interpolation
```

Il n'utilise pas implicitement `ActivityProjectionSettings`. Les paramètres
sont présentés dans la fenêtre d'export Localizer et peuvent mémoriser la
dernière valeur utilisateur, mais ils restent des paramètres de l'opération
d'export.

Interface cible :

- taille de voxel cible en millimètres ;
- preset facultatif « grille de l'IRM de référence » ;
- dimensions calculées en lecture seule ;
- estimation de la mémoire et de la taille des fichiers ;
- avertissement ou confirmation pour les exports très volumineux.

Pour une livraison minimale, un champ « dimension maximale » initialisé à 80
est acceptable. Il doit néanmoins être encapsulé dès le départ dans
`LocalizerExportGridSettings`, afin de ne pas recréer un couplage global.

## 8. Plan par phases

### Phase 0 — Baseline, contrats et mesures

**Statut :** terminée le 13 août 2026

**Résultats :** voir [phase-0-baseline.md](phase-0-baseline.md)

Objectif : établir les références qui permettront de distinguer une évolution
volontaire d'une régression.

Travaux :

- inventorier les consommateurs HiBoP et externes de l'ABI `GeneratorSurface` ;
- figer des scénarios iEEG dynamique/statique, anatomie/densité, fMRI, MEG,
  CCEP, surface et coupe ;
- enregistrer dimensions, nombre de points, normalisation, valeurs et captures
  de référence sur les données de test disponibles ;
- mesurer temps et allocations pour la construction de grille, le calcul
  d'activité et la construction des stencils d'une surface MNI ;
- ajouter ou compléter les tests d'export couvrant dimensions, affine, coins du
  volume, nombre de timelines et cohérence activité/masque ;
- définir les jeux de données anisotropes, obliques, sans intersection et à
  intersection partielle.

Gate de sortie :

- baseline automatisée reproductible ;
- benchmark reproductible avec machine et dataset documentés ;
- liste des consommateurs ABI connue ;
- aucune modification fonctionnelle livrée dans cette phase.

### Phase 1 — Nouvelle grille volumique dans `hbp_core`

**Statut :** implémentée et validée le 13 août 2026

Objectif : introduire `ActivityProjectionGrid` comme primitive indépendante,
sans supprimer immédiatement l'ancienne API.

Travaux :

- créer la géométrie de grille avec dimensions, `gridToWorld`, `worldToGrid` et
  points exclusivement volumiques ;
- faire porter les caches de recherche spatiale par la grille ou par un cache
  explicitement associé au champ d'activité ;
- adapter les générateurs iEEG, densité, fMRI et MEG pour calculer uniquement
  sur les points de grille ;
- définir explicitement le domaine de normalisation sur le champ volumique ;
- ajouter une nouvelle ABI C sans casser les symboles historiques ;
- conserver temporairement `GeneratorSurface` comme façade de compatibilité ou
  implémentation legacy isolée ;
- ajouter les tests natifs de dimensions, matrices, bords, interpolation et
  volumes obliques/anisotropes.

Gate de sortie :

- aucun sommet de surface dans les valeurs produites par la nouvelle API ;
- correspondance exacte entre points générés et `gridToWorld` ;
- tests natifs et parité scientifique acceptés ;
- absence de régression mesurable inexpliquée par rapport à la phase 0.

Résultats détaillés : [`phase-1-native-grid.md`](phase-1-native-grid.md).

### Phase 2 — Migration HiBoP et invalidations ciblées

**Statut :** implémentée et validée le 13 août 2026

Objectif : faire de la grille et du champ d'activité des ressources persistantes
indépendantes du mesh sélectionné.

Travaux :

- introduire les wrappers C# `ActivityProjectionGrid` et, si utile,
  `ActivityField` ;
- remplacer la responsabilité combinée de
  `UpdateGeneratorsAndUnityMeshes()` par des opérations ciblées ;
- implémenter les invalidations définies à la section 5 ;
- modifier `MeshManager` pour qu'un changement de surface ne déclenche plus
  `ResetGenerators()` ;
- modifier `TriangleEraser` pour ne mettre à jour que mesh, visibilité et
  projections dérivées nécessaires ;
- conserver l'invalidation complète lors d'un changement de volume ;
- rendre explicites les invalidations liées aux sites, ROI, implantation,
  données et rayons d'influence ;
- traiter CCEP par l'invalidation minimale décrite plus haut ;
- vérifier qu'une timeline continue après un changement de surface.

Gate de sortie :

- changer de surface ne relance ni le calcul de grille ni celui du champ ;
- changer de volume relance les deux ;
- modifier les données d'activité recalcule le champ sans recréer la grille ;
- les surfaces et coupes donnent les mêmes résultats scientifiques attendus ;
- les tests de cycle de vie et de libération native ne détectent aucune fuite.

Résultats détaillés : [`phase-2-hibop-lifecycle.md`](phase-2-hibop-lifecycle.md).

### Phase 3 — Bindings, couverture et messages différés

**Statut :** implémentée et validée le 13 août 2026

Objectif : sécuriser les surfaces incompatibles sans coût récurrent ni spam.

Travaux :

- créer `SurfaceProjectionBinding` et son cache de stencils ;
- produire `SurfaceProjectionCoverage` pendant la même boucle ;
- construire le binding uniquement lorsqu'une projection est demandée ;
- implémenter erreur de couverture nulle et warning de couverture partielle ;
- dédupliquer les messages par clé de binding ;
- empêcher l'anatomie ou l'activité d'utiliser silencieusement des voxels de
  bord lorsque ce comportement serait trompeur ;
- mesurer le coût sur la surface MNI et vérifier l'absence de travail par frame ;
- tester les séquences de sélection volume puis surface sans projection.

Gate de sortie :

- aucun message lors des seules sélections de volume/surface ;
- un seul message pertinent lors de la demande de projection incompatible ;
- aucun crash avec couverture nulle ou partielle ;
- coût du rapport négligeable par rapport à la construction des stencils, car
  aucune passe supplémentaire n'est effectuée ;
- cache invalidé uniquement par sa clé documentée.

Résultats détaillés : [`phase-3-surface-coverage.md`](phase-3-surface-coverage.md).

### Phase 4 — Géométrie et robustesse de l'export NIfTI

**Statut :** implémentée et validée le 13 août 2026

Objectif : faire de l'export un consommateur exact de la géométrie de grille.

Travaux :

- écrire l'affine portée par la grille sans la reconstruire à partir de l'IRM ;
- corriger le calcul des espacements entre centres et l'application des échelles
  aux axes de la matrice ;
- garantir que le masque et l'activité exportés partagent dimensions et affine ;
- tester les coordonnées monde du premier et du dernier centre ;
- tester les volumes anisotropes et obliques ;
- vérifier la lecture des fichiers produits par les outils cibles du workflow
  Localizer.

Gate de sortie :

- chaque voxel exporté décrit le même point monde que le voxel calculé ;
- masque et activité sont superposables sans recalage implicite ;
- tous les tests NIfTI de géométrie passent ;
- l'export à résolution historique reste disponible pour comparaison.

Résultats détaillés : [`phase-4-nifti-geometry.md`](phase-4-nifti-geometry.md).

### Phase 5 — Résolution d'export Localizer indépendante

**Statut :** implémentée et validée le 13 août 2026

Objectif : exposer une résolution d'export explicite et découplée de
l'affichage interactif.

Travaux :

- ajouter `LocalizerExportGridSettings` ;
- ajouter les contrôles dans le prefab de la fenêtre d'export, conformément au
  workflow prefab-first ;
- afficher dimensions et estimation de taille avant lancement ;
- appliquer des validations de valeur et un avertissement pour les tailles très
  élevées ;
- mémoriser éventuellement le dernier choix utilisateur sans modifier les
  réglages de visualisation ;
- tester que changer la résolution d'affichage ne change plus l'export et
  réciproquement.

Gate de sortie :

- les deux résolutions sont totalement indépendantes ;
- l'affine et les dimensions annoncées correspondent au fichier produit ;
- les erreurs de saisie sont gérées avant le lancement du calcul ;
- l'export par défaut reste maîtrisé en mémoire et en espace disque.

Résultats détaillés : [`phase-5-localizer-export-resolution.md`](phase-5-localizer-export-resolution.md).

### Phase 6 — Nettoyage et retrait progressif de `GeneratorSurface`

**Statut :** implémentée et validée le 13 août 2026

Objectif : supprimer la dette de compatibilité une fois tous les consommateurs
migrés.

Travaux :

- migrer les derniers usages C#, tests et outils vers les nouveaux noms ;
- consigner que `hbp_suite` et HiBoP HoloLens sont obsolètes depuis la migration
  vers `hbp_core` et ne nécessitent pas de fenêtre de compatibilité ;
- supprimer l'ancienne ABI sans façade transitoire ;
- supprimer les champs, appels et tests devenus legacy ;
- mettre à jour la documentation d'architecture et les diagrammes ;
- répéter la baseline scientifique et les benchmarks complets.

Gate de sortie :

- aucun consommateur maintenu n'utilise `GeneratorSurface` ;
- aucune concaténation surface + grille ne subsiste ;
- les responsabilités et propriétaires natifs/C# sont documentés ;
- performances, mémoire, rendu et export satisfont les références approuvées.

Résultats détaillés : [`phase-6-generator-surface-removal.md`](phase-6-generator-surface-removal.md).

## 9. Matrice minimale de tests

| Scénario | Grille | Champ | Surface | Coupe | Export | Message |
| --- | --- | --- | --- | --- | --- | --- |
| Changement de surface compatible | réutilisée | réutilisé | reconstruite | selon besoin | sans objet | aucun |
| Changement de surface incompatible sans projection | réutilisée | réutilisé | différée | selon besoin | sans objet | aucun |
| Projection manuelle sur surface sans couverture | réutilisée | après confirmation | invalide | utilisable | utilisable | warning Continue/Cancel |
| Projection manuelle avec couverture partielle | réutilisée | après confirmation | partielle | utilisable | utilisable | warning Continue/Cancel |
| Projection automatique incompatible | réutilisée | différé | neutre | selon état précédent | utilisable | aucun |
| Changement de volume | reconstruite | recalculé | binding invalidé | binding invalidé | selon demande | aucun avant projection |
| Changement de rayon iEEG | réutilisée | recalculé | UV actualisées | textures actualisées | selon demande | aucun |
| Changement d'alpha ou de temps | réutilisée | réutilisé | apparence seule | apparence seule | sans objet | aucun |
| Changement de résolution d'export | affichage inchangé | affichage inchangé | inchangée | inchangée | dimensions modifiées | aucun |
| Volume oblique/anisotrope | affine exacte | valeurs cohérentes | stencils cohérents | stencils cohérents | affine exacte | selon couverture |

Les modalités minimales couvertes sont : iEEG dynamique, iEEG statique, CCEP,
anatomie/densité, fMRI de colonne et MEG. Les overlays directs de `FMRIManager`
font l'objet de tests de non-régression mais ne sont pas migrés dans ce plan.

## 10. Mesures de performance

Chaque benchmark doit préciser machine, configuration, dataset, nombre de
sommets, dimensions de grille, nombre de sites et longueur de timeline.

Mesures requises :

- temps de construction de `ActivityProjectionGrid` ;
- mémoire des points de grille et des champs d'activité ;
- temps de calcul par modalité ;
- temps de construction d'un `SurfaceProjectionBinding` ;
- coût additionnel du comptage de couverture ;
- temps d'un changement de surface avec activité déjà calculée ;
- temps et taille d'un export Localizer pour plusieurs résolutions.

Le diagnostic de couverture est accepté s'il ne réalise aucune allocation ou
passe majeure distincte de la construction normale des stencils. Toute
validation exécutée par frame constitue une régression architecturale.

## 11. Risques et parades

| Risque | Parade |
| --- | --- |
| Modification involontaire de la normalisation en retirant les sommets de surface | baseline numérique et validation scientifique avant migration Unity |
| Rupture ABI pour un autre produit | nouvelle API parallèle puis retrait différé |
| Affine incorrecte sur volume oblique | tests par coordonnées monde des centres et coins |
| Spam de messages pendant la configuration de scène | diagnostic paresseux, déclenché par projection et dédupliqué |
| Coût élevé sur une surface dense | comptage fusionné avec la construction des stencils et cache par versions |
| Invalidation oubliée après découpage des booléens | matrice événement → invalidation et tests de compteur d'appels |
| Export trop volumineux | aperçu dimensions/taille, validation et confirmation utilisateur |
| Régression CCEP due aux capacités d'un mesh | conserver le comportement courant et invalider explicitement lors du changement réel de mode |

## 12. Règles de reprise pour l'implémentation

Au début de chaque phase :

1. relire ce document et les résultats de la phase précédente ;
2. vérifier l'état des deux dépôts HiBoP et `hbp_core` ;
3. limiter les modifications à la portée de la phase ;
4. annoncer les éventuelles différences découvertes avant d'élargir le chantier ;
5. exécuter les tests ciblés puis la gate complète de la phase ;
6. mettre à jour le statut et les résultats dans ce document.

Les optimisations non nécessaires à la phase courante sont consignées plutôt
qu'implémentées immédiatement. En particulier, l'unification des overlays
directs de `FMRIManager`, la refonte du rendu CCEP et la réutilisation d'une
grille entre plusieurs volumes géométriquement identiques restent hors périmètre.
