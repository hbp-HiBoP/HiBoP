# Audit de l'existant et matrice de réutilisation

## État Git et baseline

Cet audit décrit l'ancien checkout feature/xr. Le propriétaire est depuis passé
sur feature/xr-autonomous à 8b868a5b8 et a supprimé le dossier XR résiduel.
Pour les tâches, utiliser l'état courant et les règles de provenance de
[TASK-WORKFLOW.md](TASK-WORKFLOW.md), pas une restauration de l'ancien projet.

Inventaire du 2026-09-07, références locales sans fetch :

| Dépôt | État observé |
| --- | --- |
| HiBoP | feature/xr à eb26c323e, un worktree ; Docs/dev/quest-autonomous non suivi. |
| HiBoP origin/develop | 8b868a5b8 ; ancêtre commun XR 83a52e4ea ; écart non-XR portant sur BugReportRelay. |
| HiBoP_HoloLens | master 5a11994 ; ProjectSettings.asset modifié localement. |
| hbp_core | develop cf4400b, propre ; binaire HiBoP épinglé à 1f26946 via NativePlugins.lock.json. |
| hbp_math | master 646e96e, propre. |
| EEGFormat | master 99b54e9 ; EEGFormat/main.cpp modifié localement. |

La branche cible feature/xr-autonomous a été créée depuis develop par le
propriétaire, conformément à D01, et vérifiée à origin/develop local (8b868a5b8).
Conserver cette branche et vérifier l'état courant avant chaque tâche. Garder
feature/xr comme référence ; ne pas changer de checkout sous un éditeur sans
coordination.
Un workspace de build séparé n'est pas une seconde base de code produit.

L'inspection par git diff confirme que Core/DLL, Core/Object3D et Data/Module3D
consultés sont identiques entre origin/develop et feature/xr. Les dépendances
XR et RenderModelAdapters sont, elles, des ajouts à réintégrer sélectivement.

## Structure et points d'entrée

- Assets/_Scenes/HiBoP.unity est l'unique scène activée du build racine.
- HBPBuilder construit Windows/Linux/macOS et copie Assets/Data dans le dossier
  Data du Player ; macOS utilise Contents/Resources/Data.
- ApplicationState.DataPath connaît Editor et macOS ; sa branche par défaut
  suppose un dossier voisin de l'application. Ce chemin n'est pas un contrat
  de stockage Android.
- HBP.Core.Runtime référence UniTask et TextMeshPro. Il contient déjà les
  wrappers natifs et le modèle ; « Core » n'est donc pas une assembly pure .NET.
- HBP.Data.Runtime dépend de Core, Rendering, Theme et UIExtensions ;
  HBP.UI.Runtime dépend de Data et SFB. Le graphe principal inspecté est orienté
  sans cycle observé entre ces assemblies. Aucun audit exhaustif de tous les
  packages tiers ni des dépendances chargées dynamiquement n'est revendiqué.
- Les asmdefs Runtime principales n'excluent pas Android. Leur présence
  compilable ne garantit ni leur exécution correcte ni leur exclusion du Player.
- Assets/Resources/Themes et les dépendances UI demandent un Build Report :
  une scène Quest minimale ne suffit pas à prouver un APK minimal.
- Aucune paire de Build Profiles Desktop/Quest versionnée n'a été repérée dans
  Assets. Les deux projets courants utilisent Unity 6000.5.2f1 et URP 17.5.0.
- L'entrée racine utilise activeInputHandler=0 et XR activeInputHandler=1.
  Un manifeste commun ne résout pas cette différence de configuration.

## Représentation et science

Visualization et Column héritent de BaseData, qui sérialise un ID stable.
Visualization référence patients, configuration et colonnes : envoyer son JSON
seul ne transporte pas les assets et données nécessaires à la session.
Réutiliser les IDs ; ne pas envoyer le graphe historique complet ou des handles natifs.

Camera3D.HorizontalRotation/VerticalRotation/Zoom modifient la caméra.
La présentation Quest doit donc être indépendante, sans refactorisation forcée
de ce contrôleur Desktop.

Base3DScene.UpdateProjectionResources construit ActivityProjectionGrid depuis
SelectedMRI.Volume, initialise les générateurs, puis lie la surface de référence.
ComputeGeneratorsAsync ajoute masques, influence, normalisation et orchestration.
C'est la tranche à extraire, et non le MonoBehaviour entier.

Column3DDynamic associe les amplitudes aux sites, calcule leur taille/couleur
et les UV de surface. Les sites interpolent éventuellement un TemporalSample ;
la surface utilise son indice inférieur. Reproduire ces règles dans un renderer
Quest créerait une divergence même avec une DLL partagée.

## Réutilisation

Les tests ci-dessous sont ceux présents ou rapportés ; ils n'ont pas été relancés.

| Élément / emplacement | Responsabilité et dépendances | Qualité / preuve existante | Choix | Coût ou risque |
| --- | --- | --- | --- | --- |
| Core/DLL et hbp_core | ABI C, ressources scientifiques ; UniTask/Unity dans certains wrappers | Tests natifs, fonctionnels et sérialisation ; source commune déjà Desktop | Conserver, qualifier Android | ABI, durée de vie et threading IL2CPP. |
| Base3DScene, Column3DDynamic | Orchestration scientifique et UI/événements mêlés | Tests fonctionnels Desktop | Extraire la tranche densité/iEEG | Dupliquer les appels natifs ne suffit pas. |
| Core/Data/Visualization et BaseData | Identités et modèle sérialisé | Tests HBP.Serialization.Tests | Conserver ; projection de transport explicite | JSON historique ne constitue pas une capsule. |
| MNIObjects et Assets/Data | Chargement GII/TRM/NIfTI et préparation anatomique | Assets MNI, fixtures natives | Réutiliser la préparation Desktop | Conversions et ordre des sommets à préserver. |
| HoloLens/Module3D et prefabs 3D/Scenes | Manipulation MRTK proche/lointaine, scène/colonnes | Source/prefabs inspectés ; aucune exécution récente | Adapter l'expérience, abandonner copie des classes | MRTK2/Unity2021 ; comportement réel à requalifier avec XRI. |
| Shared/Packages/...contracts | IDs, hashes, versions, valeurs | PurityTests, IdentifierAndRevisionTests | Réutiliser types utiles ; adapter les contrats | Ne pas importer CommandGate/autorité Desktop par principe. |
| Shared/Packages/...render-model | Buffers, surfaces, repères, codec | Round-trips, temporal/golden/pureté | Adapter et conserver les primitives | RenderModel décrit des résultats ; il manque les entrées scientifiques. |
| RenderModelAdapters/DesktopSurfaceRenderModelAdapter | Extrait surface préparée et UV ; dépend de Data/Core | Tests de parité P03 | Adapter l'extraction de géométrie | UV scientifiques transférés seulement comme oracle de validation. |
| DesktopSiteRenderModelAdapter | Capture des sites déjà présentés | Golden P03 et P10 | Adapter | Ne remplace pas la liste native et le masque de calcul. |
| Spikes/P06 | Framing, TLS/appairage, bulk ; Kestrel .NET, UnityWebRequest, websocket-sharp | Réseau physique Windows–Quest ; Mac/Linux cross-publish seulement | Réutiliser preuves/framing ; transport embarqué à qualifier | Pas de serveur intégré HiBoP existant. Sidecar candidat de repli. |
| Protocol/RemoteAssets et XR/RemoteAssets | Hashes, chunks, cache mémoire, leases | RemoteAssetCacheTests et P08 intégration | Adapter | Anciennes politiques de purge/lease/background incompatibles avec autonomie si reprises aveuglément. |
| XR/StaticRendering | Mesh upload, cache, shaders URP | P05 opaque et Quest physique ; transparence passthrough ouverte | Adapter | Buffers mm→m ; ne pas modifier après UploadMeshData(true) sans chemin prévu. |
| XR/BrainInstances | Identité, binding, disposition locale | P09BrainInstanceTests | Réduire/adapter | Le registry multi-instance n'est pas requis ; ses dépendances amènent Sites/Protocol. |
| XR/Sites | Instancing, buffers, BVH/picking | P10 tests et endurance rapportée | Adapter rendu ; différer picking | Couleurs et tailles scientifiques doivent venir du commun. |
| XR/Timeline | Preload, ressources GPU, sélection d'indice | Probe Quest 97 indices, sélection prête frame suivante | Différer | Un instant iEEG ne nécessite pas de timeline complète. |
| XR/Cuts | Gizmo et résultat distant | P12 tests ; seuil réseau échoué | Différer gizmo ; abandonner calcul distant obligatoire | Hors prototype et contraire au calcul local cible. |
| Protocol/SessionState, Journals | Autorité, séquence, reprise | P07 tests synthétiques | Ne pas intégrer au prototype | Aucun journal des gestes spatiaux demandé. Revoir lors des commandes scientifiques. |
| XR/Tools et P04 bootstrap | APK OpenXR, déploiement et mesure | APK physique Quest 3 | Adapter profils/chemins, sérialiser prefabs | Scripts actuels ciblent le projet XR distinct et parfois des probes. |
| Tools/NativePlugins* et CI Desktop | Épinglage artefacts et builds OS | Manifests/CI existants | Étendre Android au jalon natif | Ne pas associer DLL Desktop et .so Android de sources différentes sans traçabilité. |

## Commits utiles

- 83a52e4ea : ancêtre commun ; 8b868a5b8 : baseline origin/develop inspectée.
- ae911f8bb : anciennes spécifications et audit HoloLens.
- 7363ee729 : ancienne topologie à deux projets, à ne pas reprendre.
- 43e589db0 : contrats P02 ; 1703f645d : modèle de rendu/parité P03.
- 43afdbf18 : bootstrap OpenXR P04.
- 83b187757 : cache P08 ; 94de284ce : instances P09 ;
  417acd746 : rendu/picking des sites P10.
- 91983fcd7 : timeline P11 ; eb26c323e : P12.
- HoloLens 7c462e5 : interactions sites ; 5a11994 : report de changements HiBoP.

Ces commits sont des repères de revue, pas une liste de cherry-picks approuvée.

## Limites des preuves historiques

P06 mesure un host synthétique et un client probe, pas « Envoyer au Quest »
depuis HiBoP. P11 démontre la sélection locale après preload sur ses profils.
P12 mesure codec Windows et borne de transfert fondée sur P06, pas la chaîne
command-to-photon complète. Le build Android natif rapporté dans SOURCES.md
(NDK 28.2, API29) n'établit ni parité ni runtime IL2CPP ; son artefact n'a pas
été réinspecté dans cet audit. Le défaut de cache RemoteAssetCacheTests signalé
par P12 est lié à un chemin racine/XR, à corriger dans les tests réutilisés.

Sources locales : Docs/dev/xr/evidence/P06/windows-validation.md,
Docs/dev/xr/evidence/P05/static-renderer-validation.md,
Docs/dev/xr/evidence/D20/timeline-preload-implementation.md,
Docs/dev/xr/evidence/P12/P12-D20-fail-decision.md,
hbp_core/docs/activity_projection_architecture.md.
