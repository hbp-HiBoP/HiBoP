# ADR P04 — bootstrap OpenXR Quest 3

- **Statut :** ACCEPTED — GATE P04-A–E RESOLVED
- **Date :** 2026-09-01
- **Accepté par :** propriétaire du dépôt HiBoP
- **Baseline inspectée :** branche courante, commit `1703f645d68b4c041de84fbe4344e9ef1815b37a`
- **Projet :** `XR/`, Unity `6000.5.2f1` (`eb73d3b415a1`)
- **Package lock de décision :** `Docs/dev/xr/package-locks/P04.json`

## Contexte et preuves officielles

La documentation a été revalidée le 1er septembre 2026, avant toute installation de package P04.

- Meta confirme que Horizon OS est conforme à OpenXR, recommande le plug-in Unity OpenXR et annonce la dépréciation d'Oculus XR Plugin : <https://developers.meta.com/horizon/documentation/unity/unity-and-openxr-compatibility/>.
- Unity documente OpenXR `1.18.0`, XRI `3.6.0`, XR Hands `1.9.0`, Input System `1.20.0` et XR Plug-in Management `4.7.0` comme versions released compatibles avec Unity 6000.0 :
  - <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.openxr.html>
  - <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.interaction.toolkit.html>
  - <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.hands.html>
  - <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.inputsystem.html>
  - <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.management.html>
- Unity OpenXR: Meta `2.4.1` exige Unity 6+, AR Foundation 6.4+, OpenXR 1.15.1+ et XR Composition Layers 2.1.1+ : <https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.4/manual/install.html>.
- Unity documente le passthrough Quest via le sous-système caméra AR Foundation : `ARCameraManager` activé pour le passthrough, désactivé pour le retour VR, caméra transparente, sans accès Unity aux pixels du passthrough : <https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.4/manual/features/camera.html>.
- Meta exige Android 10/API 29 au minimum, IL2CPP et ARM64, et recommande `Target API Level = Automatic (highest installed)` : <https://developers.meta.com/horizon/documentation/unity/unity-prepare-for-publish/>.
- Unity recommande Vulkan pour OpenXR Meta et précise les réglages de fond transparent nécessaires au passthrough : <https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.4/manual/get-started/graphics-settings.html>.
- Meta exige la protection durable du keystore de publication ; il ne doit pas être traité comme un artefact de développement jetable : <https://developers.meta.com/horizon/resources/publish-overview-appID/>.

Les modules Android Build Support, SDK, NDK et OpenJDK de Unity `6000.5.2f1` sont présents sur l'hôte au moment du gate.

## P04-A — versions et matrice de compatibilité

### Décision

Verrouiller les packages directs suivants, tous en version released, sans plage flottante :

| Composant | Version P04 | Rôle / contrainte |
| --- | --- | --- |
| Unity Editor | `6000.5.2f1` | version P01, supérieure au minimum Unity 6 documenté |
| XR Plug-in Management | `4.7.0` | cycle de vie du loader OpenXR |
| Input System | `1.20.0` | unique backend d'entrée du projet XR |
| OpenXR Plugin | `1.18.0` | provider portable principal |
| XR Interaction Toolkit | `3.6.0` | rig et interactions portables |
| XR Hands | `1.9.0` | sous-système de suivi des mains |
| Unity OpenXR: Meta | `2.4.1` | extensions Quest et provider passthrough |
| AR Foundation | `6.4.3` | ligne 6.4 explicitement documentée par Unity OpenXR: Meta 2.4 |
| XR Composition Layers | `2.4.0` | version released actuelle ; `2.2.0` échoue sous Unity 6000.5 |

La résolution UPM réelle complète reste enregistrée dans `XR/Packages/packages-lock.json`. Toute divergence entre ce lock de décision, le manifest et le lock UPM fait échouer la validation P04.

La première résolution contrôlée a invalidé le pin candidat `2.2.0` : Unity `6000.5.2f1` traite désormais plusieurs appels `Object.GetInstanceID()` de cette version comme erreurs `CS0619`. Le gate P04-A a donc été rouvert avant configuration du projet et corrigé vers XR Composition Layers `2.4.0`, version released officielle publiée en mars 2026 : <https://docs.unity3d.com/Packages/com.unity.xr.compositionlayers@2.4/manual/index.html>.

Meta XR Core SDK et Meta Interaction SDK ne sont pas nécessaires à P04 et ne sont pas installés. Ils restent soumis au spike D15/S06 si une interaction V1 démontre une insuffisance mesurable de XRI.

## P04-B — passthrough et frontière Meta

### Décision

Utiliser le provider `Camera (Passthrough)` de Unity OpenXR: Meta, exposé par AR Foundation.

- Le code portable dépend de `IPassthroughProvider` et `IXRPlatformCapabilities` seulement.
- L'adaptateur Meta reste sous `XR/Assets/HiBoPXR/Platform/Meta/` dans une assembly dédiée.
- L'adaptateur pilote un `ARCameraManager` sérialisé dans le prefab : activé en MR, désactivé en VR.
- Aucun pixel caméra, image CPU, mesh de scène, ancre ou donnée environnementale n'est demandé, conservé ou journalisé.
- Aucun type Meta ne traverse Contracts, Protocol, RenderModel ou le renderer générique.
- Oculus XR Plugin, `OVRCameraRig`, `OVRManager`, Meta XR Core SDK et Meta Interaction SDK sont exclus.

## P04-C — Android, rendu et compilation

### Décision

- Android minimum : API 32. La documentation Meta générale autorise API 29, mais la règle de validation livrée par `com.unity.xr.meta-openxr@2.4.1` exige API 32 dès que `ARCameraFeature` est activé, y compris sans accès aux images CPU. Le lock retient la contrainte réelle la plus stricte du provider installé.
- Android target : `Automatic (highest installed)`, matérialisé par la valeur Unity correspondante et relevé dans le rapport de build.
- Backend graphique : Vulkan uniquement pour ce bootstrap Quest 3.
- Stereo : Single Pass Instanced / Multi-View selon l'option exposée par OpenXR sur Android.
- Scripting backend : IL2CPP.
- Architecture : ARM64 uniquement ; ARMv7 interdit.
- Fréquence de référence : demande de 72 Hz seulement si l'extension l'autorise, sans échec si le runtime choisit autrement.

## P04-D — identité, signature et secrets

### Décision

- Application ID de développement : `fr.crnl.hibop.xr.dev`.
- P04 utilise la signature debug générée par l'outillage Android/Unity pour le sideload local ; aucun keystore ou mot de passe n'est créé, copié ou versionné dans le dépôt.
- Le propriétaire des futurs secrets de signature est le propriétaire de release HiBoP/CRNL, via le coffre de secrets de release et la CI autorisée.
- Le keystore de publication, son alias et ses mots de passe sont hors P04. Leur création exige le gate D19/P15 et une procédure de sauvegarde/restauration validée.

## P04-E — déclaration du passthrough

### Décision

Déclarer le passthrough **Supported**, pas Required.

Le produit demande le passthrough par défaut lorsqu'il est disponible et autorisé, mais exige un mode VR de repli. `Required` contredirait ce repli et empêcherait une dégradation par capability. Au démarrage, l'application tente le passthrough ; si le provider est indisponible ou échoue, elle reste en VR et affiche un diagnostic non sensible.

`com.unity.xr.meta-openxr@2.4.1` génère actuellement `com.oculus.feature.PASSTHROUGH` avec `android:required="true"` dès que `ARCameraFeature` est activé. P04 maintient la feature OpenXR optionnelle et applique après génération Gradle un hook local qui remplace cette valeur par `false`; le build échoue si la déclaration est absente. Cette adaptation reste dans l'assembly Editor du bootstrap et ne modifie pas le package UPM.

## Retour de validation appareil

La première validation physique a révélé une contrainte d'intégration absente de la configuration initiale : la feature OpenXR Android `Composition Layers Support` doit être activée avec `Camera (Passthrough)`. Le feature set AR Foundation fourni par Unity OpenXR: Meta la déclare requise et le provider caméra Meta l'utilise pour créer la couche passthrough. P04 l'active et la vérifie désormais explicitement ; aucun changement de package ou de frontière architecturale n'en résulte.

La caméra du prefab reçoit également un `TrackedPoseDriver` sérialisé, alimenté par les contrôles `centerEye` de l'Input System. Le tracker diagnostic P04 observe toujours l'état de la tête, mais ne concurrence plus ce composant pour écrire sa pose.

Le second APK a validé sur Quest 3 : passthrough par défaut, bascule vers le mode VR et retour, suivi de tête, deux contrôleurs, deux mains et demande 72 Hz. Le détail et le hash de l'APK testé sont consignés dans `Docs/dev/xr/validation/P04-device-report.md`.

## Périmètre d'implémentation autorisé après le gate

1. Installer uniquement les packages verrouillés ci-dessus et leurs dépendances UPM.
2. Configurer Android/OpenXR/Quest 3 selon P04-C.
3. Créer un prefab de bootstrap sérialisant rig, caméra, adaptateur passthrough et diagnostics tête/mains/contrôleurs.
4. Créer une scène minimale qui instancie ce prefab, sans UX HiBoP, réseau, donnée patient ou renderer scientifique.
5. Produire, sideloader et valider un APK de développement local sur Quest 3.

## Conditions de réouverture

- incompatibilité observée entre un package verrouillé et Unity `6000.5.2f1` ;
- besoin démontré de Meta XR Core/Interaction SDK ;
- besoin d'accès aux images caméra ou aux données spatiales ;
- publication Meta, entitlement, App ID Dashboard ou signature de release ;
- abandon du repli VR ou changement de `Supported` vers `Required`.
