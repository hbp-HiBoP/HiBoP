# HiBoP XR — sources et preuves

**Version :** 0.2  
**Consultation :** 31 août 2026

## 1. Baselines locales

| Source | Révision | Rôle |
| --- | --- | --- |
| HiBoP Desktop | `83a52e4ea8c446046916fe7916d84eb704c3855c`, develop | application, renderer, timelines, sites |
| `hbp_core` | `cf4400bf…`, develop/tag 0.3.1 | ABI et calculs natifs |
| prototype HoloLens fermé | `5a119948…`, master | preuve historique App Remoting/UX |

Le prototype est référencé par chemins logiques dans `hololens/traceability.md` ; aucun code propriétaire ni secret de configuration n'est reproduit.

## 2. HiBoP Desktop

Fichiers structurants :

- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `Packages/manifest.json` ;
- `Assets/Scripts/HBP/Core/HBP.Core.Runtime.asmdef` ;
- `Assets/Scripts/HBP/Data/HBP.Data.Runtime.asmdef` ;
- `Assets/Scripts/HBP/Core/Data/Loaded/Processed/Timeline.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Column3D.cs` et `Column3DDynamic.cs` ;
- `Assets/Scripts/HBP/Data/Module3D/Modules/ImplantationManager.cs` et `DisplayedObjects.cs` ;
- `Assets/Scripts/HBP/Core/DLL/Generators/SurfaceGenerator.cs` et `CutGenerator.cs` ;
- `Assets/Scripts/HBP/Core/Tools/ApplicationState.cs` et `ClassLoaderSaver.cs`.

Constats :

- Unity 6000.5.2f1, URP 17.5.0 ;
- legacy Input Manager actif, packages XR absents ;
- Core/Data trop larges pour un client Quest ;
- un objet/renderer/collider par site dans le chemin courant ;
- topologie/mesh dupliqués par colonne pour attributs mutables ;
- `TemporalSample` et calculs canoniques côté Desktop.

## 3. `hbp_core`

Sources :

- `CMakeLists.txt` ;
- `include/hbp_core.h`, `src/core/hbp_core.cpp` ;
- `src/core/parallel_for.cpp` ;
- `src/generators/cut_generator.cpp` ;
- `.github/workflows/native.yml` et docs build/toolchain.

Preuve locale supplémentaire : configuration et build Release Android NDK 28.2, `arm64-v8a`, API 29, tests/tools désactivés, achevés 77/77. Le résultat AArch64 non stripé pèse environ 19,9 Mo et expose 2 798 symboles dynamiques définis, dont des dépendances vendored. Cette preuve démontre la compilation, pas le runtime Unity/Quest.

Une incohérence de métadonnée existe à la révision auditée : le tag observé est 0.3.1 tandis que `project(VERSION ...)` déclare 0.2.1. Elle doit être corrigée dans le workstream natif avant une matrice de compatibilité fiable.

## 4. Prototype HoloLens

Voir :

- `hololens/architecture.md` ;
- `hololens/protocol.md` ;
- `hololens/feature-matrix.md` ;
- `hololens/reusable-components.md` ;
- `hololens/performance-notes.md` ;
- `hololens/decision-delta.md` ;
- `hololens/traceability.md`.

Conclusion sourcée : processus HiBoP complet sur PC + Microsoft OpenXR App Remoting, aucun protocole de données ou client autonome.

## 5. Documentation officielle Unity

- [XR packages — Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/xr-support-packages.html)
- [XR input options](https://docs.unity3d.com/2022.3/Documentation/Manual/xr-input-overview.html)
- [Android native plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/android-native-plugins-import.html)
- [Plug-in Inspector](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-in-inspector.html)
- [Build Profiles](https://docs.unity3d.com/6000.0/Documentation/Manual/BuildSettings.html)

Faits retenus :

- le plugin OpenXR supporte notamment Meta Quest ;
- Unity OpenXR: Meta fournit les extensions spécifiques Meta ;
- XRI couvre interactions contrôleurs/manipulation/UI et XR Hands expose le tracking des mains ;
- avec le plugin OpenXR, le nouveau Input System est requis et le legacy Input Manager n'est pas supporté ;
- un plugin natif Android doit être compilé/configuré pour la bonne architecture.

## 6. Documentation officielle Meta

- [Unity and OpenXR Compatibility](https://developers.meta.com/horizon/documentation/unity/unity-and-openxr-compatibility/)
- [Get Started with Passthrough](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/)
- [Passthrough sample overview](https://developers.meta.com/horizon/documentation/unity/unity-sample-starter-passthrough/)
- [Distribution Options](https://developers.meta.com/horizon/policy/distribution-options/)
- [Release Channels](https://developers.meta.com/horizon/resources/publish-release-channels/)
- [Organization verification](https://developers.meta.com/horizon/resources/publish-organization-verification/)

Faits retenus au 31 août 2026 :

- Horizon OS est compatible avec Unity OpenXR ;
- les composants Meta peuvent avoir des dépendances aux extensions Meta et doivent rester isolés ;
- Oculus XR Plugin est annoncé déprécié au profit d'OpenXR ;
- passthrough peut être supporté/requis et activé au démarrage ou à l'exécution ;
- Alpha/Beta/RC permettent une distribution limitée mais restent soumis aux exigences de packaging/politiques ;
- sideloading ne fournit pas les mises à jour/présence plateforme ;
- la vérification d'organisation est une condition de publication/mise à jour.

Ces règles externes sont revalidées lors du spike D19 ; elles ne sont pas figées comme comportement interne du produit.

## 7. Nature des chiffres

- nombres de sommets/faces et tailles de fichiers : assets locaux réels ;
- tailles de payload : calculs arithmétiques documentés ;
- mesures projection/coupe Desktop : rapports existants, non extrapolés au Quest ;
- seuils 72 Hz/latence : gates proposées D20 ;
- aucune performance Quest n'est présentée comme acquise.
