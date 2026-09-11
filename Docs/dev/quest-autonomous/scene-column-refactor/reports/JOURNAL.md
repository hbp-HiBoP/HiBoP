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


## Lot B — SCENE-004 à SCENE-007, 10 septembre 2026

Implémentation successive depuis `feature/xr-autonomous@a3fdb36b5`, sans commit,
compilation, campagne Unity, build ou essai casque. Les statuts IMPLEMENTEE /
DIFFEREE décrivent l’avancement du code, conformément à la cadence A/B/C.
Le cadrage 01–04, le workflow, les fiches 001–008 et ce journal ont été relus.
Une revue indépendante bornée des références et propriétaires du transfert a
orienté les corrections des identités de protocole, timelines et durées de vie.

### SCENE-004 — ressources préparées et références locales

- `StandardData` définit l’installation Android dans `persistentDataPath/Data`.
  Le préprocesseur de build Quest copie MNI (MRI, transformation et quatre GIfTI)
  et les atlas de `Assets/Data` vers `StreamingAssets/ScientificData`, avec un
  index SHA-256. Les lecteurs natifs reçoivent de vrais chemins locaux après
  extraction par UnityWebRequest. Les chemins Data Desktop restent inchangés.
- La préparation MNI est partagée et attendable ; les échecs de chargement
  libèrent les surfaces/volumes préparés partiellement. Les empreintes identifient
  les références locales et sont comparées avant la restauration.
- L’export attend l’initialisation puis complète les préchargements anatomiques
  de tous les patients. L’ouverture Desktop ordinaire conserve sa préférence de
  préchargement ; cette expansion est demandée au moment de la capture.
- Les ressources MRI/fMRI conservent la provenance des fichiers chargés, y compris
  les couples `.img`/`.hdr` et les masques. Chaque couple a une identité propre :
  des voxels identiques avec des headers différents ne sont pas fusionnés. Les chargements fMRI/IBC/DiFuMo sont
  attendables ; les masques fMRI sont libérés avec leur propriétaire.

### SCENE-005 — paquet complet et restauration commune

- Nouveau chemin `HBP.Transfer.Scene` : `DesktopSceneCapture`, `SceneArchive`,
  `ScenePayload`, `SceneRestoration` et `SceneDelivery`. Le bouton Desktop exporte
  la visualisation sélectionnée, avec toutes ses colonnes dans leur ordre.
  Les modèles de projets et leurs réglages de sérialisation ne changent pas.
- Le contrat de transfert version 1 contient les six modalités, patients/sites,
  protocoles/tags, configurations, signaux préparés, essais valides et invalides,
  statistiques, unités, fréquences et métadonnées exactes des timelines. Les
  dictionnaires à clés SubBloc/Event préservent les références canoniques ; le
  resolver de transfert évite les callbacks de résolution des fichiers projet.
  Les champs scientifiques privés utilisés par ce contrat sont conservés pour
  IL2CPP dans le `link.xml` du transfert.
- Les ressources binaires sont adressées par SHA-256 et dédupliquées. Les MRI,
  masques et fichiers fonctionnels complets sont conservés. Les coordonnées,
  triangles, normales, UV/couleurs et masques des surfaces patient/transitoires
  sont capturés. Les surfaces simplifiées sont transférées sans nouvelle
  simplification : les masques indexent une topologie précise. Une copie native
  temporaire permet d’exporter aussi les triangles effacés sans modifier la source.
  Les previews générées gardent leur type transitoire, leur MRI source et leur
  rapport de génération, pour préserver les choix anatomiques du socle commun.
  MNI anatomique est référencé localement ; ses masques et représentations dérivées
  sont transportés séparément, avec les paramètres et le repère du cache de
  gonflement actif pour ne pas le réétiqueter avec un autre preset.
- L’état courant inclut sélection de ressource/site/source CCEP/label, temps,
  lecture/boucle/pas, positions et états des sites, coupes/ROI, effacement, atlas,
  réglages de projection, échantillonnage et corrélations. Les états conservés pour
  d’autres implantations sont inclus. Les illustrations de protocole sont des
  fichiers du paquet, et non des chemins vers le projet source.
- La capture attend les travaux en cours, prend le verrou de représentation et
  copie/sérialise sans céder le thread Unity. Elle ne sauvegarde pas le projet,
  ne change pas les sélections Desktop et ne rééchantillonne pas les données.
- La réception vérifie format, tailles, empreintes, chemins, références et
  dimensions avant publication ; les lecteurs natifs vérifient ensuite les
  ressources elles-mêmes. Elle instancie `Scene 3D Content`, injecte les données
  préparées et laisse les mêmes managers/générateurs préparer le rendu.
- Transport de fichiers HBT version 3, en blocs de 64 KiB, avec SHA-256 par bloc
  et par fichier. Plus de tableau contenant tout le transfert ni de plafond
  historique 64 MiB pour ce parcours. Les gardes-fous initiaux sont 4 GiB pour le
  fichier, le contenu développé et les buffers numériques cumulés, 128 MiB pour
  les métadonnées et 100 000 entrées. Un dépassement produit un refus explicite,
  jamais une réduction scientifique. Ces budgets restent à qualifier en 008.
- L’ACK suit la préparation et le remplacement complet. Les identités et
  tombstones conservent retry, AlreadyPublished, Superseded et Closed. Une erreur
  conserve l’ancienne visualisation. Fermer/annuler attend les travaux natifs ;
  fichiers reçus et fichiers d’envoi restent vivants jusqu’au dernier utilisateur.

### SCENE-006 — présentation Quest

- `QuestAnatomyView` possède la scène restaurée ; la logique scientifique du
  prototype est remplacée par `Base3DScene` et ses six classes de colonnes.
- `QuestColumn.prefab` fournit une pose indépendante, un repère millimètre→mètre,
  un label et un manipulateur. Chaque colonne commune y est rattachée, avec ses
  surfaces, sites et coupes. Les représentations visuelles des sphères ROI suivent
  les objets communs dans chaque pose, via le prefab `QuestROIProxy`.
- Gestes de déplacement/rotation/échelle, recentrage et visibilité sont des
  adaptations de présentation. Le recalcul appelle la scène commune. Aucun
  panneau scientifique Quest ni protocole de commandes synchronisées n’est ajouté.
- Les prefabs de bootstrap et services référencent les composants communs.
  Le renderer Quest utilise la feature de contours/transparence commune ; son
  shader est référencé explicitement pour le build et ses passes plein écran
  reçoivent les macros stéréo. La caméra exclut les couches techniques masquées.
  Le rendu effectif Vulkan/OpenXR/passthrough n’a pas été qualifié.

### SCENE-007 — intégration et références

- Les assemblies de production UI/Quest/Dev ne référencent plus les contrats et
  générateurs scientifiques du prototype Anatomy/Projection. Ses composants,
  diagnostics, shaders, matériaux et prefabs de référence résident dans
  `Assets/Tests/Support`, derrière des assemblies de tests. Les anciens tests
  scientifiques sont conservés et redirigés explicitement vers ces références.
  Ils ne constituent pas une preuve du nouveau parcours.
- Les menus de configuration Quest et la validation des références de build sont
  raccordés à la présentation commune. Les opérations scientifiques n’appellent
  pas les vues Desktop ; les événements de notification communs sont conservés.
- Fixtures ajoutées dans `HBP.Transfer.Scene.Tests` : round-trip des six modalités,
  identités patients/protocole/tags, deux fréquences de timeline, essais invalides,
  rejet de versions/ressources manquantes, contenu corrompu et chemins invalides ;
  transport multibloc, refus d’un bloc corrompu et ordre publication→ACK.
  Elles sont préparées pour 008 et n’ont pas été exécutées.

### Vérifications de B et campagne 008 à poursuivre

Contrôles par lecture : parcours complet, dépendances des assemblies modifiées,
références locales YAML des prefabs Quest et des fixtures, nouveaux GUID et `.meta`.
Formatage obligatoire via `Tools/format-code.cmd` : restauration NuGet impossible
initialement dans le sandbox, puis exécution autorisée hors sandbox. Les contrôles
statiques ne remplacent ni compilation ni qualification scientifique.
`git diff --check` ne signale pas d’erreur. La fermeture conserve aussi la référence
managée d’une scène déjà détruite par Unity pour attendre ses travaux avant
de supprimer les fichiers reçus.

La campagne C devra notamment :

1. Compiler les assemblies Desktop/Android et exécuter les nouvelles fixtures,
   les références numériques existantes et les suites communes du Lot A. Vérifier
   le resolver Newtonsoft et la conservation IL2CPP des champs privés sur appareil.
   L’inventaire des asmdefs a aussi signalé la référence préexistante `Unity.ugui`
   dans `HBP.PlayModeTestUtilities`, à vérifier lors de cette compilation.
2. Ouvrir une visualisation de chaque modalité puis une visualisation mixte avec
   plusieurs patients/implantations, fréquences distinctes, essais invalides,
   MEG canaux seul/volumique, plusieurs sources CCEP et plusieurs labels statiques.
   Comparer les données et les sorties communes Desktop/Quest après réception,
   navigation, changement de ressource/source et recalcul hors connexion.
3. Comparer MNI et anatomie patient, surfaces anatomiques/gonflées, hémisphères,
   effacement avant transfert, ROI, coupes, atlas et contours/transparence. Vérifier
   les deux yeux et le passthrough ; manipuler chaque colonne et contrôler que
   coupes, sites et ROI restent dans son repère scientifique.
4. Annuler/fermer pendant extraction, chargement MRI/fMRI, génération et envoi ;
   répéter/remplacer une livraison, perdre l’ACK, déconnecter puis continuer à
   utiliser la scène. Mesurer RAM native/managée, objets Unity et fichiers après
   répétitions. Vérifier spécialement la première installation Data et sa reprise.
5. Mesurer taille du paquet, temps de capture synchrone sur le thread Unity,
   extraction et première préparation sur Quest. La sérialisation des champs privés
   et les budgets initiaux sont des points de stabilisation explicites. L’adaptation
   des signaux ou de la géométrie pour tenir une limite n’est pas autorisée implicitement.
6. Vérifier la non-régression de l’ouverture/sauvegarde Desktop et l’absence de
   dépendance aux anciens composants dans un build de production. Vérifier le
   contenu généré de StreamingAssets lors des builds successifs Android/Desktop.

Aucune campagne C n’est lancée au titre de cette demande limitée au Lot B.

### Relecture du Lot B — première série de commentaires (2026-09-10)

À la demande explicite de l’utilisateur, la compilation est désormais vérifiée
dans l’éditeur HiBoP ouvert, via MCP, avec la cible Android active. Les ambiguïtés
de types `FMRI`, `DynamicData` et `CompressionLevel` ont été corrigées par des
alias explicites. L’assembly des diagnostics historiques déplacés dans
`Tests/Support/QuestPrototype` référence maintenant `Newtonsoft.Json.dll`.

Le paramètre inutilisé `marsAtlas` de `SceneArchive.ReadSurface` et le champ
redondant correspondant de `MeshResource` sont supprimés. La disponibilité
MarsAtlas reste enregistrée individuellement dans chaque surface binaire,
avec ses couleurs, puis restaurée par `SetPreparedAtlasAvailability`.

Le formateur obligatoire `Tools/format-code.cmd` a terminé avec succès sur les
89 fichiers C# modifiés/nouveaux ; les nouvelles assemblies font maintenant
partie de la solution générée par Unity. `git diff --check` est sans erreur.
Après rechargement, l’éditeur rapporte `isCompiling=false` et
`scriptCompilationFailed=false` ; l’assembly `HBP.Transfer.Scene` est chargée.
Des avertissements de sérialisation du code existant restent présents, ainsi
qu’un message de `XRManagerSettings.OnDisable` concernant `StopSubsystems`
pendant le rechargement. Aucun build IL2CPP ni test fonctionnel n’a été lancé
dans cette relecture ; la qualification scientifique du Lot C reste à faire.

Les questions sur les préférences et les caches Preloaded ont reçu une analyse,
sans changement de leur fonctionnement. L’utilisateur confirme que le transport
de l’état des atlas standards, sans leur contenu déjà inclus dans le build,
convient.

### Relecture du Lot B — vérification explicite des caches (2026-09-10)

Après accord de l’utilisateur sur le point 7, `MeshManager.LoadMissing` et
`MRIManager.LoadMissing` se limitent aux listes principales. La capture vérifie
explicitement `IsLoaded` pour les ressources des caches Preloaded avant tout
accès aux getters susceptibles de déclencher un chargement. Une ressource non
chargée interrompt l’export avec son nom et celui du patient, sans retenter son
chargement. Le chargement initial par `AddPreloaded` reste inchangé.

Formatage obligatoire terminé avec succès ; compilation MCP dans l’éditeur
avec cible Android : `isCompiling=false`, `scriptCompilationFailed=false`.
`git diff --check` sans erreur. Aucun test fonctionnel supplémentaire exécuté.
La proposition d’envoyer les données globales au pairing et les données propres
à la visualisation lors de son envoi reste en discussion, sans implémentation.

### Relecture du Lot B — globaux au pairing (2026-09-10)

Après accord explicite, le pairing transmet désormais un instantané complet des
préférences utilisateur, des définitions globales de tags, des protocoles de la
base Desktop (illustrations incluses), des alias et des préréglages de filtres.
Les réglages globaux de grille/interpolation suivent cet instantané. Le prefab
QuestServices inclut DatabaseManager ; le contexte reçu est installé uniquement
en mémoire, sans sauvegarde implicite des préférences ou de la base Desktop.

Les scènes passent au format 2 et référencent l’identité de leur pairing ainsi
que les définitions globales par identité/empreinte. Les valeurs de tags des
patients et sites restent spécifiques à la scène. Le resolver réutilise les
objets canoniques du pairing, y compris pour les filtres et les clés des
dictionnaires d’essais/timelines. Les illustrations sont identifiées par contenu,
indépendamment des chemins Desktop/Quest. Toute définition requise absente ou
modifiée, identité ambiguë ou ancien pairing est refusé explicitement.

Les overrides PreparedPreferences, PreparedTemporalSampling, paramètres de
corrélation et grille/interpolation sont retirés. Le chemin scientifique commun
utilise directement PersistentDataManager.UserPreferences et les réglages globaux
existants. Aucune synchronisation ultérieure n’est introduite : les préférences
reçues restent celles du pairing jusqu’à son remplacement.

Le handshake distingue la réservation du propriétaire de l’état prêt. L’ancien
pairing sans globaux est refusé par le récepteur de production. Le nouveau
contexte est validé avant fermeture attendue des scènes courantes et anciennes ;
l’ACK vient après installation. Les fichiers globaux vivent jusqu’à la fin de
leurs utilisateurs. La destruction pendant préparation attend celle-ci avant
libération. Une interruption après réservation impose un nouvel appairage.

La revue indépendante demandée par AGENTS a contrôlé les références canoniques,
les chemins d’illustrations, les annulations et les durées de vie. Les corrections
incluent l’attente des scènes déjà remplacées et l’effacement du credential sur
annulation tardive côté Desktop.

Vérification finale : Tools/format-code.cmd terminé avec succès sur 93 fichiers
C# ; git diff --check sans erreur ; compilation Unity via MCP avec cible Android
active réussie (`scriptCompilationFailed=false`). Le prefab importé contient bien
DatabaseManager. Les 14 tests EditMode de HBP.Transfer.Scene.Tests passent
(job `4cfe3bfec2534c698fd39d5f6bdc4fa8`, 14/14, environ 8 s). Ils couvrent les six
modalités, les références canoniques protocoles/tags/filtres, les illustrations,
les définitions absentes/modifiées, les anciens pairings, l’intégrité du transport,
l’attente/échec de l’installation globale et l’attente des préparations/libérations.
Les avertissements Unity de sérialisation existants et le message XR au rechargement
restent distincts de cette validation. Pas de build IL2CPP ni de test sur casque
au titre de cette relecture ; la qualification scientifique du Lot C reste à faire.

## SCENE-008 — 10 septembre 2026

Lot C implémenté sur la base `753d3ce7219d`, sans commit. La campagne locale a
stabilisé l'initialisation/restauration, les états de boucle et d'effacement, les
durées de vie partielles et la propriété des vues Desktop. Deux projets réels à
six modalités exercent chargement, sauvegarde, ressources alternatives, capture,
restauration et calculs communs. Les diagnostics et comparaisons sont réutilisables
sur le Quest après une vraie réception.

Les builds Windows/Android IL2CPP proviennent des mêmes sources d'exécution. Le build
Android a révélé une décompression automatique des atlas `.gz` : correction de
l'empaquetage sans réduction du Data, puis vérification des 49 SHA-256 embarqués.
Le rapport intégré et les preuves se trouvent dans [FINAL.md](FINAL.md) et
`../evidence/final/manifest.json`. Le Quest étant éteint, la réception sur appareil,
l'autonomie hors connexion, les mesures Android et le retour manuel restent en
attente. SCENE-008 : IMPLEMENTEE / PARTIELLE / EN_ATTENTE ; aucune clôture implicite
de QUEST-024.

Suite au retour du propriétaire, `QuestStandardDataBuild` utilise maintenant
`BuildPlayerContext.AddAdditionalPathToStreamingAssets` pour inclure directement
`Assets/Data`. Les copies sous `Assets/StreamingAssets` sont supprimées et ne
sont plus générées. Le manifeste temporaire seul reste sous `Library/HBP`.
Cette modification concerne exclusivement la préparation Editor du build Android ;
les sources exécutées par les Players et les masques d'effacement sont inchangés.

### 2026-09-10 — pause après le premier essai physique

Installation initiale réussie sur Quest 3 ; contrôleurs réveillés par le propriétaire.
L'essai révèle une création de répertoires Desktop invalide sur Android et deux
défauts de graphiques (capacité du pool avec MEG, réponses CCEP d'un autre patient).
Corrections implémentées, quatre tests GraphZone réussis. Les Players Windows et
Android corrigés compilent avec succès ; le diagnostic du Windows corrigé réussit
sans exception dans le journal vérifié. Le nouvel APK n'a pas encore été installé.
Aucun appairage ni transfert réel n'a été mené à terme ; rendu, poses, autonomie,
remplacement et mesures Android restent à qualifier.

Le propriétaire reprend sur un autre ordinateur. Les instructions complètes sont
dans [RESTART-PROMPT.md](../RESTART-PROMPT.md), avec les dépendances, la régénération
des fixtures et les commandes de build. Les petites preuves les plus récentes sont
dans `../evidence/final/pause-2026-09-10/`. Le manifeste précédent reste un instantané
historique, antérieur aux dernières corrections. Les sorties `.artifacts` et
`.test-results` sont locales et ignorées par Git. Les trois changements générés par
Unity (BuildInfo et réglages URP Desktop/Quest) ont été restaurés après compilation.
État maintenu : IMPLEMENTEE / PARTIELLE / EN_ATTENTE. Aucun commit ni push effectué.

### 2026-09-11 — reprise sur le poste du laboratoire

Checkout `feature/xr-autonomous@f9db4b56e14d0a7d1a3f9f94f7c3b0b7ac6236a1`,
initialement propre : le Lot C et les corrections de pause sont désormais dans
ce commit. Unity 6000.5.2f1, Windows IL2CPP, Android SDK/NDK r27c/OpenJDK et
Python 3.13.7 présents. Les quatre plugins Windows/Android contrôlés correspondent
exactement à `Tools/NativePlugins.lock.json`. Les deux fixtures sont régénérées
avec les chemins de ce checkout. L'utilisateur confirme l'éditeur fermé ; les
builds utilisent le lanceur CLI hors sandbox.

Quest 3 USB `2G0YC5ZHB20370` détecté, puis débogage autorisé dans le casque par
l'utilisateur. Le laboratoire ne dispose pas de Wi-Fi accessible. Le transport
de cette recette utilisera une redirection ADB USB
`tcp:45871 -> tcp:45871`, avec `127.0.0.1` dans le panneau Desktop. Le code
existant accepte cette adresse ; appairage TLS et livraison restent inchangés.
Les mesures seront identifiées comme USB : elles ne qualifient ni débit ni
reconnexion Wi-Fi. Les preuves de préparation sont conservées dans
`.test-results/scene-008/resume-20260911/preflight.json`.

La capture utilisateur du dialogue « ID issue » révèle un doublon dans la
fixture : `quest-020-blacklist` était réutilisé par les six configurations de
site. Le générateur attribue désormais des IDs par colonne. Les deux archives
ont 79 objets identifiés sans doublon ; les diagnostics Windows MNI et patient
réussissent ensuite (12 et 13 états, 12,01 s et 15,46 s), sans exception dans
les journaux. Les premières exécutions bloquées/expirées sont conservées.

La mise à jour du Quest est refusée pour signature incompatible. L'ancien APK,
les données privées et externes sont sauvegardés (159 fichiers, environ 3,14 Go,
empreintes vérifiées). Aucune désinstallation n'est encore effectuée. Le
propriétaire demande une clé commune aux deux PC : génération explicite d'une
identité dédiée hors Git, valide jusqu'au 18 août 2126. Le lanceur vérifie
l'empreinte publique épinglée avant Unity, puis signe et vérifie l'APK avec
`apksigner`, sans modifier les réglages Unity ni y stocker de secrets. La revue
indépendante de cette solution ne trouve aucun défaut bloquant ; contrôles
réels de signature, clé absente et refus de régénération réussis.

Les 18 localizers VISU fournis ensuite par le propriétaire sont présents, avec
des empreintes différentes de l'ancien poste. Il confirme qu'ils doivent
rester distribués séparément : la modification envisagée de `HBPBuilder` est
annulée ; le packaging Quest les exclut également. La copie préparée est placée
sous `.artifacts/scene-008/optional-localizers`, hors du Player Windows. Les
localizers restent disponibles dans `Assets/Data` pour une installation séparée.

### 11 septembre — migration Quest autorisée et restauration vérifiée

Le propriétaire autorise explicitement la désinstallation unique, l’installation
du nouvel APK et la restauration. Les deux commandes Android réussissent.
L’APK installé correspond exactement au SHA-256 du binaire final avec clé commune
(394 365 917 octets, 31 références standard, zéro localizer). Restauration vérifiée
par SHA-256 : 4 fichiers privés et 148 fichiers externes, hors caches régénérables.
La restriction `adb push` sur la création des dossiers a été résolue par création
explicite avec `adb shell mkdir -p`, puis copie des fichiers. Sauvegarde conservée.
Le lancement initial est intercepté par le dialogue Meta « contrôleurs requis » ;
validation et réveil des manettes demandés au propriétaire. Aucun résultat physique
de démarrage/rendu n’est encore attribué au nouvel APK.

### 11 septembre — premier démarrage du nouvel APK

Le propriétaire confirme avoir validé le dialogue contrôleurs. PID 8444 actif,
OpenXR `COMPOSITION READY`, aucune exception de préférences dans le journal
capturé. Erreurs Meta `xrRequestBoundaryVisibilityMETA / XR_ERROR_RUNTIME_FAILURE`
observées lors des transitions de session ; effet visuel en attente de retour.
PSS ponctuel avant appairage : 1 652 691 Ko, sans prétendre à un pic.
Parcours manuel d’appairage USB à 127.0.0.1 puis livraison demandé au propriétaire ;
le contrôle UI HiBoP demeure refusé et l’authentification ne doit pas être automatisée.

### 11 septembre — appairage bloqué par les illustrations facultatives

Le propriétaire signale plusieurs échecs de Pair malgré Y et confirme garder
le casque porté. Le diagnostic Windows isolé reproduit `DirectoryNotFoundException`
pour `[DATABASE_FOLDER]/Images/LEC1_SEM.jpg` dans `PairingContext.Fingerprint`,
avant toute connexion. Le catalogue local référence aussi 24 illustrations
absentes de sa base locale ; aucun fichier local n’a été restauré.

Le propriétaire impose que la validité de ces fichiers ne conditionne pas le
transfert des définitions. Les chemins/octets d’illustrations sont exclus des
empreintes ; les images lisibles sont capturées séparément, les sources
indisponibles omises. Les erreurs d’écriture d’archive et les checksums reçus
restent stricts. Format global 2, reconstruction simultanée des deux Players
requise. Ajout de journalisation des échecs Desktop et diagnostic de capture
sans réseau `-scenePairingEvidence`. Tests EditMode transfert : 23/23 réussis.
La validation réelle après reconstruction reste à effectuer.

### 11 septembre — correction compilée et APK mis à jour

Les deux builds de correction réussissent sous `.artifacts/scene-009` (dossier
d’exécution, toujours qualification SCENE-008). La capture des globaux réels
réussit sans réparer les images ; les six modalités Windows réussissent et
leurs buffers sont identiques bit à bit à la version précédente. Formatage C#
réussi. Nouvel APK signé commun et installé par `adb install -r`, sans
désinstallation ; SHA-256 installé vérifié :
`4110a8d9105ff3a2d3a54a89e6f99ccd655178db7929e224a5d4d8986a5dce68`.
31 références vérifiées, aucun localizer. Le Desktop corrigé est rouvert après
fermeture de l’ancien par le propriétaire. La validation du dialogue contrôleurs
puis le nouvel appairage manuel sont demandés. Preuves courantes dans
`optional-illustrations-fix.json`, distinctes des binaires de la première migration.

### 11 septembre — livraison reçue, préparation interrompue par hbp_math absent

Le propriétaire réussit l’appairage et lance trois envois. Le Desktop termine
l’envoi puis perd l’accusé de publication ; le Quest journalise
`DllNotFoundException: hbp_math` dans `MathDLL.Mean` / colonne statique.
La dépendance Android était absente du catalogue et de l’APK. Compilation
du même commit math que Desktop, sans modification d’algorithme : NDK 27.2,
API 32, ARM64, libc++ statique, segments ELF alignés 16 KiB. Les 17 exports
ABI sont présents. Le probe exécuté directement sur le Quest réussit ses
assertions exactes ; SEM/Pearson/Wilcoxon des cas testés sont identiques aux
résultats Windows. Ce probe ne vaut pas validation de la scène IL2CPP.
Ajout du plugin/meta, provenance Android au lock, contrôle avant build et
inspection de l’APK après build. L’ancien APK est désormais rejeté. Limite
du native updater pour Android math explicitée dans Tools/QuestMathPlugin.md.
Le nouvel APK est en construction ; aucune publication réussie n’est encore
revendiquée.

### 11 septembre — dépendance native installée, nouvel essai demandé

Build Unity Android réussi, quatre tests du contrôle de build réussis.
La première inspection post-build détecte que Gradle a retiré les symboles non
nécessaires du plugin et changé son empreinte. Le script applique désormais
la même opération avant verrouillage ; le plugin final correspond exactement
aux octets déjà présents dans l’APK. Inspection stricte relancée avec succès,
sans assouplir le contrôle de hash. Probe réexécuté sur ce plugin final :
17 exports, assertions exactes réussies et résultats comparatifs inchangés.
APK final de 394 447 911 octets, 31 références standard vérifiées, zéro localizer,
signé avec la clé commune. Installation `adb install -r` réussie et SHA-256
installé vérifié : `b840bd02ed764711f0f1f4a416b4fe3644bb09a6485b7af54dc5e453fe51764e`.
Redirection USB rétablie et ouverture demandée à Android. Le propriétaire est
invité à valider le dialogue Meta éventuel puis refaire la livraison ; aucune
publication réussie de la scène IL2CPP n’est encore revendiquée.

### 11 septembre — builds relancés à la demande du propriétaire

Les builds Windows et Quest sous `.artifacts/scene-011` réussissent. Diagnostic
Windows réussi, comparaison aux buffers scene-009 identique bit à bit. Player
visible rouvert sur les six colonnes. APK signé commun, 31 références vérifiées,
zéro Localizer, bibliothèques core et math conformes au verrou. Installation
`adb install -r` réussie, SHA-256 installé vérifié, redirection USB rétablie et
ouverture Quest demandée. Identités dans `rebuild-scene-011.json`. Le plugin
math de qualification reste celui déjà installé précédemment ; les adaptations
de scripts natifs ne l’ont pas remplacé. Publication physique encore à vérifier.

### 11 septembre — première publication et affichage confirmés

Le propriétaire confirme que la visualisation testée affichait les différentes
colonnes et leurs labels. Le journal Desktop scene-011 contient une livraison
`Published` le 11 septembre à 10:33:02 UTC : 998 083 octets, 806 ms de capture,
15 982 ms de connexion/envoi/accusé et 16 788 ms au total. Cela confirme la
publication réelle après les corrections illustrations et dépendance math.
Cinq erreurs ultérieures `Connection refused` sont également présentes ; leur
contexte n’est pas établi et elles n’annulent pas l’accusé Published précédent.
Le Quest est absent d’ADB au suivi ; aucun diagnostic Quest n’est récupéré.
Précision sur les manipulations et reconnexion proposées au propriétaire.
Preuve : `first-published-scene.json`. Qualification globale encore partielle.

Complément propriétaire : « J’ai essayé tout ça » en réponse à déplacement,
rotation et redimensionnement. Ces manipulations ont donc été essayées dans
ce retour globalement positif. Stabilité après relâchement, indépendance de
chaque colonne et comparaison scientifique restent à confirmer spécifiquement.
