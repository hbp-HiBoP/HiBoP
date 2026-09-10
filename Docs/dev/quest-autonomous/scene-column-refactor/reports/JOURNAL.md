# Journal — lot A

## SCENE-001 — 10 septembre 2026

Départ : `feature/xr-autonomous@6c1b481c3`, arbre propre. Documents du cadrage,
workflow et fiches 001–003 lus. Inspection HoloLens en lecture seule :
`HoloLens/Module3D/{Base3DScene,Column3D}` et `HoloLens/UI/CutsController` ;
manipulations MRTK et caméra casque mêlées aux objets, sans View3D équivalente.
Historique `2f0abc4d2:Shared/Packages/com.crnl.hibop.contracts/Runtime/Commands.cs`
consulté comme référence seulement.

| Fonction | Opérations / propriétaire | Données à conserver en 005 | Rendu | Interaction actuelle |
| --- | --- | --- | --- | --- |
| Anatomie/densité | Column3DAnatomy, AnatomyDataParameters, DensityGenerator | Implantations, états des sites, distance/règle d’influence | SurfaceGenerator, CutTexturesUtility | Toolbar paramètres |
| iEEG | Column3DIEEG/Column3DDynamic, calibration et sampling communs | ProcessedValuesByChannel, DataByChannelID (essais), StatisticsByChannelID, unités, timelines navigation/projection et blocs | UV surface, coupes, SiteActivityAppearance | Timeline, seuils, outils de sites |
| CCEP | Column3DCCEP, source site ou label MarsAtlas, amplitudes/latences | Dictionnaires par source/cible, essais/statistiques, source courante, mode, seuils | Même projection dynamique et rendu sites CCEP | Sélection source, timeline |
| fMRI | Column3DFMRI/FMRIGenerator | Toutes les FMRIs/volumes, index ressource et volume courant, seuils/masques | Surface/coupes fonctionnelles | Sélecteur, timeline, seuils |
| MEG | Column3DMEG/MEGGenerator | MEGItems, FMRIs calculées, métadonnées patients, index et timeline | Surface/coupes fonctionnelles | Sélecteur, paramètres |
| Statique | Column3DStatic/IEEGGenerator | Valeurs par label/site, labels, label sélectionné, états, bornes | Surface/coupes/sites | Sélecteur label, seuils |
| Meshes/volumes | MeshManager/MRIManager, représentation et invalidation scène | Toutes ressources et représentations, préchargements patients, références standard MNI | DisplayedObjects et copies de meshes par colonne | Sélecteurs Desktop |
| Coupes | Base3DScene.Add/Update/RemoveCutPlane | Plans courants, orientation/flip/position, mode fort, coupe automatique | CutGeometryGenerators, textures, matériaux | Contrôleurs de coupes |
| ROI | ROIManager/ROI/Sphere | Sphères, sélection et masques, rayon scientifique | Sphères et masques de projection | Toolbar, déplacement caméra |
| Atlas | AtlasManager/FMRIManager | Atlas actif, opacité, labels et ressources standard, réglages fonctionnels | Couleurs surfaces/coupes | Sélecteurs et survol |
| Sites | Column3D/Site/SiteState | États par FullID, configurations, sélection, positions affichées, données d’essais | Meshes sites et matériaux partagés | Listes, filtres, déplacement hémisphère |
| Effacement | TriangleEraser | Mode, seuils, masques nécessaires à reconstruction | Géométrie scène et copies colonnes | Toolbar |

Choix : conserver les classes et GUID existants ; une présentation Desktop
optionnelle porte placement, création et synchronisation des vues ainsi que leur
configuration. Les colonnes restent les propriétaires de leurs buffers mutables,
générateurs et meshes de rendu. Les ressources anatomiques peuvent être partagées ;
leur fermeture doit attendre les utilisateurs natifs et tenir compte des scènes
encore vivantes, indépendamment des fenêtres Desktop.

Extractions ciblées : groupes homogènes de colonnes (Tool), opérations timeline,
symétrie des sites (MoveSites), filtres/sélection et interactions ROI. Les opérations
déjà dans les managers restent à cet emplacement. Aucun modèle scientifique bis.
QuestAnatomyView possède encore NativeProjectionInputs, résultats et calculs du
prototype mono-instant : son remplacement et le branchement réception restent
dans 005–007 ; les opérations communes utilisent le chemin Column3D existant.

Références pour 008 : Module3DConfigurationTests, CutsTriangleErasingTests,
ActivityProjectionPhase3Tests, NativeFixtureProjectGenerator/NativeFixtureIntegrationTests,
LegacyProjectCompatibilityTests, suites HBP.Module3D.PlayModeTests et
HBP.Toolbar.PlayModeTests. Les fixtures de transfert/projection Quest existantes
restent des références scientifiques limitées au prototype. À compléter en C :
scène sans présentation, multicolonnes/modalités complètes et fermeture pendant
chargement/calcul. Aucun test/build ni recette intermédiaire lancé.

## SCENE-002 — implémentation

`DesktopScenePresentation`, référencé dans `Scene 3D.prefab`, prend en charge
placement, création/synchronisation/sélection des vues, caméras et sauvegarde des
vues. `Scene 3D Content.prefab` fournit les mêmes managers et six colonnes sans
ce composant : `Initialize`/`InitializeAsync`/`FinalizeInitialization` ne créent
alors aucune View3D. Les points d'entrée de chargement Desktop utilisent toujours
les classes communes. Les champs/GUID historiques restent conservés ; les
parents des vues et les matériaux des colonnes/sphères sont sérialisés.

Le layer de rendu est fixé avant l'initialisation de colonne ; le choix
`ColumnN` appartient à la présentation Desktop. Le contenu emploie son layer de
prefab, sans utiliser le numéro de colonne comme identité spatiale. Les plans
restent en coordonnées scientifiques locales ; les entrées raycast/ROI font la
conversion depuis les coordonnées du monde.

Fermeture idempotente : suivi de l'initialisation, de l'anatomie d'arrière-plan,
des colliders, des générateurs et des corrélations ; attente du verrou de
préparation des représentations avant libération. Les colliders travaillent sur
leurs propres copies de surface/plans. Le registre des scènes communes comprend
les scènes en fermeture jusqu'à libération, indépendamment de Module3DMain.
Le reset des ressources standard attend leurs utilisateurs et la préparation MNI
(Load devient attendable). L'échec de chargement Desktop ferme la scène partielle.
Les anciens RawSiteList restent vivants jusqu'au retour de leur calcul ; les
abonnements de sites, timelines et préférences sont retirés à leur fin de vie.

Revue indépendante unique du découpage réalisée en lecture seule. Ses points
prioritaires (layers, matériaux, singleton de nettoyage, tâches natives non
suivies, filtre utilisant SelectedScene, timeline MEG) ont guidé les changements.

## SCENE-003 — implémentation

- `GetColumnGroup` centralise les groupes par modalité. Les vrais outils Desktop
  utilisent cette résolution et les opérations d'index/lecture/boucle/pas ; MEG
  rejoint l'API et les contrôles temporels. Les timelines iEEG/CCEP ont une copie
  de navigation par colonne, sans recopier les essais/signaux.
- Les opérations hémisphère/reset des positions, sélection explicite scène/site,
  reset et application des filtres, ainsi que l'orchestration des corrélations
  résident dans la scène. La validité d'une source CCEP est contrôlée dans sa
  colonne, même sans bouton Desktop. Les résultats de corrélation sont publiés
  complets sur le thread Unity après vérification de leur cible.
- `SiteConditions` contient les prédicats scientifiques et le parseur avancé
  extraits des composants UI. Les contrôles lisent leurs widgets une fois avant
  filtrage. Le filtrage cède régulièrement la main et publie un masque complet ;
  suppression de la queue non synchronisée et de l'accès aux widgets depuis un
  worker. Correction adjacente : les comparaisons Y/Z lisaient le toggle X.
- Les opérations déjà communes de mesh/MRI, représentation, coupe, ROI, atlas,
  effacement, seuils, source CCEP, sélection fMRI/MEG/label et projection restent
  dans leurs managers/colonnes. Leurs callbacks d'invalidation restent dans
  `Base3DScene.AddColumn`, utilisés sans toolbar. Les informations de survol
  peuvent être demandées aux managers sans lire la souris ; la présentation
  Desktop choisit leur position à l'écran.

## Handoff A et suite

Contrôles A : lecture des changements et callers, revue bornée de conception,
contrôle des références de prefabs/GUID et formatage via `Tools/format-code.cmd`.
La première restauration NuGet a échoué dans le sandbox ; l'exécution autorisée
hors sandbox a permis le formatage. Aucune compilation, aucun test Unity, build
ou essai matériel exécuté. IMPLEMENTEE ne vaut pas qualification runtime.
Contrôle statique final : les références locales et GUID des neuf prefabs
concernés se résolvent ; les nouveaux scripts ont leurs `.meta` ;
`git diff --check` ne signale pas d'erreur.

Pour 004–007 : préparation complète des ressources et des préchargements patients,
packaging MNI Android, contrat de restauration et branchement de QuestAnatomyView
sur ces colonnes. Le prototype NativeProjectionInputs reste actif tant que son
remplacement par la restauration commune n'est pas raccordé ; ses limites
mono-instant ne définissent pas celles des nouvelles opérations. Les façades de
vues de Base3DScene/Column3D restent des points d'entrée Desktop délégants, sans
seconde implémentation scientifique.

Pour 005, capturer l'état courant des timelines et ressources sélectionnées,
sources CCEP, label statique, positions/états/sites/essais, sélection ROI et atlas,
masques d'effacement, réglages de projection/préférences scientifiques ; ne pas
se limiter à SaveConfiguration. Le prefab de contenu conserve les ressources
de rendu existantes : adaptation et validation effective Quest restent en B/C.

Pour 008 : compiler puis vérifier les six modalités, les APIs sans présentation,
la sauvegarde Desktop et les GUID, l'indépendance des colonnes, les fermetures
pendant chargement/calcul et les allocations/libérations Unity/natives. Mesurer
aussi le coût du filtrage coopératif. Limitation préexistante conservée : les
prédicats statistiques des filtres ignorent leurs bornes start/end (TODO des
anciennes méthodes) ; ne pas annoncer une correction scientifique de ce point.

## Retours de revue utilisateur du Lot A

- Ajout des régions Properties/Public Methods/Private Methods à
  `DesktopScenePresentation`, et des régions de durée de vie à
  `Base3DScene.Lifetime`. Nouvelle exécution du formateur : correction des blocs
  try/catch, indentations et espacements qui avaient subsisté dans les nouveaux
  fichiers. Aucune modification des règles de formatage.
- `SubTimeline` implémente `ICloneable` avec `Clone()` à la place de `Copy()`.
  Le dictionnaire conserve ses entrées ; les statistiques sont des valeurs
  (`EventStatistics` est une struct). La stratégie `CopyForNavigation` demeure
  inchangée ; sa justification a été expliquée puis acceptée par l'utilisateur.
- Remplacement des conversions UniTask/Task du suivi de durée de vie par des
  UniTask réutilisables via `AsyncLazy` et `UniTaskCompletionSource` : préparation
  MNI, initialisation, anatomie, colliders, corrélations, générateurs et fermeture.
  `BeginClose` et `AnatomyCompletion` exposent désormais UniTask. La fermeture
  attend chaque travail déjà lancé même après erreur ; un simple
  `UniTask.WhenAll` ne garantirait pas cette attente en cas d'échec. L'attente du
  SemaphoreSlim existant conserve naturellement son API .NET.
- Adaptation des accès par réflexion aux tâches des générateurs dans les deux
  tests existants DesktopAnatomyCaptureTests et DensityProjectionTests. Aucun
  lancement de tests ni compilation Unity dans cette revue (qualification C).
- Lecture des parcours de filtres : toolbar et SitesInformations ouvrent
  `SiteFiltersWindow`. BasicSiteConditions/AdvancedSiteConditions appartiennent
  à l'ancien panneau, toujours sérialisé dans le prefab SitesInformations.
- Points discutés mais non modifiés : présentation Quest et raccordement des
  contours au rendu XR (006), accès statique aux matériaux communs indépendant
  de Module3DMain, et découpage supplémentaire de Base3DScene en fichiers partiels.
- À la demande de l'utilisateur, suppression de `Column3D.Scene` et de son
  affectation. La façade `Column3D.AddView`, sans appelant C# ni référence
  sérialisée trouvée, est supprimée ; DesktopScenePresentation possède déjà la
  création des vues. Les corrélations utilisent le token de durée de vie transmis
  par la scène et contrôlent toujours la validité de la colonne et des sites
  avant publication. Le callback des filtres spatiaux résout le propriétaire via
  les colonnes du registre des scènes communes et exclut les scènes en fermeture.
- Les références sérialisées au même asset SharedMaterials sont conservées.
  L'indépendance du contenu vis-à-vis de Module3DMain n'impose pas l'absence de
  singletons sur Quest : les services communs restent réutilisables ; l'intégration
  B doit distinguer leur initialisation des vues et du chargement projet Desktop.
