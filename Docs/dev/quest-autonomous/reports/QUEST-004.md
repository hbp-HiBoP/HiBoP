# Rapport QUEST-004 — Passthrough et contrôleurs Quest

## Résultat

La fenêtre Android plane de QUEST-003 devient une composition immersive OpenXR
pour Quest 3. `QuestBootstrap.unity` référence `QuestBootstrap.prefab`, qui
contient le nouveau `QuestRig.prefab`. Le rig suit la tête et les contrôleurs,
avec un repère bleu à gauche et orange à droite. Le panneau minimal affiche
le suivi et l'état technique du passthrough. Aucun écran réseau, main, cerveau,
déplacement ou synchronisation n'est ajouté.

Un défaut de loader, de display, d'extension passthrough, de fournisseur de
composition, de caméra AR, de fond transparent ou de couche de composition est
affiché comme `PASSTHROUGH FAULT` après dix secondes, et journalisé comme erreur.
Le rig reste transparent et réévalue l'état : aucun repli opaque automatique.
`COMPOSITION READY` signifie que les préconditions observables sont réunies ;
le panneau demande toujours de confirmer la visibilité réelle de la pièce.
L'API publique du package ne donne pas une preuve du résultat visuel du compositeur.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : VALIDE.
  Retour propriétaire le 2026-09-07 à 19:34 +02:00 : « M1 OK M2 OK ».
- Branche `feature/xr-autonomous`, HEAD initial
  `0631d53fc039bceebb05d9bd0ec76af4d8da729b`, checkout initial propre.
  Aucun commit, push, changement de branche ou workflow distant.
- Unity `6000.5.2f1`, Input System `1.20.0`, OpenXR `1.18.0`, Meta OpenXR
  `2.4.1`, XR Management `4.7.0`, URP `17.5.0`. Manifest et lock inchangés.
- Dépendance QUEST-003 implémentée et validée dans son rapport.
- Reprise ciblée de `P04DevicePoseTracker`, des contrôles de
  `MetaOpenXRPassthroughProvider` et de la génération `P04ProjectSetup`, lus
  depuis `feature/xr` à `eb26c323e2bdf6c138f9e9e3c02f3e9796cae249`.
  Nouvelles classes/GUID : suppression des mains, environnement VR et bouton
  de bascule ; contrôle de pose complète, diagnostic explicite, composition
  par prefabs imbriqués et rendu Android isolé. Les GUID existants du bootstrap
  et de sa scène sont préservés. Aucun fichier XR historique restauré en bloc.
- [Manifeste des preuves](../evidence/QUEST-004/manifest.json).
  Logs et binaires volumineux restent locaux dans `.test-results/quest-004`
  et `.artifacts/quest-004`.

## Ce que je conseille de reviewer

| Priorité | Entrée | Changement et invariant |
| --- | --- | --- |
| 1 | [QuestRig.prefab](../../../../Assets/Prefabs/Quest/QuestRig.prefab), [QuestBootstrap.prefab](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab) | Objets et références sérialisés ; aucune construction corrective de GameObject dans le code runtime HiBoP. |
| 2 | [QuestPassthroughStatus.GetProblem](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestPassthroughStatus.cs), [QuestBootstrap.Update](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestBootstrap.cs) | Défauts exposés, état technique séparé de l'observation physique, diagnostics de poses toutes les cinq secondes. |
| 3 | [QuestDevicePoseTracker](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestDevicePoseTracker.cs) | Entrées par main ; position ET rotation valides avant affichage ; masque du repère si suivi perdu ; libération des actions et callback before-render à la désactivation. |
| 4 | [QuestBuildValidation](../../../../Assets/Scripts/HBP/Quest/Editor/QuestBuildValidation.cs), [QuestBootstrapSetup](../../../../Assets/Scripts/HBP/Quest/Editor/QuestBootstrapSetup.cs) | Validation Android avant build ; génération explicite, jamais automatique au build ; aucun loader ajouté à Standalone. |
| 5 | [QualitySettings](../../../../ProjectSettings/QualitySettings.asset), [URP Quest](../../../../Assets/Settings/Rendering/HBP-Quest-URP.asset), [tests de configuration](../../../../Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/QuestBootstrapTests.cs), [test de suivi](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestTrackingTests.cs) | Niveau Quest par défaut Android, Desktop conservé ; fond transparent et suivi périmé protégés par tests. |

### Hiérarchie et paramètres effectifs

```text
QuestBootstrap.unity
└─ Quest Bootstrap [QuestBootstrap.prefab, QuestBootstrap]
   └─ QuestRig [QuestRig.prefab, QuestPassthroughStatus]
      ├─ AR Session [ARSession]
      └─ XR Origin [XROrigin, Floor]
         └─ Camera Offset
            ├─ Main Camera [Camera, ARCameraManager, TrackedPoseDriver, QuestDevicePoseTracker Head]
            │  └─ Quest Status [TextMesh, police LegacyRuntime sérialisée]
            ├─ Left Controller [QuestDevicePoseTracker LeftController, repère bleu]
            └─ Right Controller [QuestDevicePoseTracker RightController, repère orange]
```

`QuestBootstrap` sérialise `passthrough`, `head`, `leftController`,
`rightController` et `statusText` vers le rig imbriqué. Le provider sérialise
`cameraManager` et `xrCamera`. Chaque tracker sérialise `role`, `poseTarget`
et son `diagnosticRenderer` (nul pour la tête). Les deux repères sont masqués
jusqu'à une pose valide, et ne sont pas enfants de la caméra.

Caméra : SolidColor, RGBA `(0,0,0,0)`, near `0.05 m`, far `100 m`, HDR et
post-traitement désactivés. Tête : `UpdateAndBeforeRender`. Contrôleurs :
mise à jour normale et before-render, bindings `<XRController>{LeftHand}` /
`{RightHand}`, validité position+rotation. Les repères mesurent `4 × 6 × 12 cm`.
Placement du panneau : `(-0.45, 0.25, 1.25) m` dans le repère de la caméra ;
réglage UX provisoire à qualifier sur casque.

Android : OpenXR seul, initialisation et démarrage automatiques ; Meta Quest
Support (Quest 3 uniquement), Meta Session, Meta Camera, Composition Layers,
Oculus Touch et Meta Touch Plus. Le package active sa feature interne Lifecycle.
Stéréo SinglePassInstanced, Vulkan. Niveau de qualité `Quest` (index 6),
`HBP-Quest-URP`, MSAA 4×, HDR désactivé ; renderer Forward sans effet Desktop,
Intermediate Texture Auto. Standalone conserve son niveau 5 et son renderer.
La couche passthrough dynamique est créée par le package Meta ARCameraManager,
conformément à son fonctionnement ; le code HiBoP ne la reconstruit pas.

Ces choix suivent la [documentation Unity Meta Camera](https://docs.unity.cn/Packages/com.unity.xr.meta-openxr%402.4/manual/features/camera.html)
et les sources/documentations des packages installés, notamment
`Documentation~/get-started/graphics-settings.md` et `MetaOpenXRCameraSubsystem`.

## Vérifications effectuées

| ID | Vérification | Résultat et preuve locale |
| --- | --- | --- |
| T1 | Génération/import des prefabs par Unity CLI hors sandbox | Réussi, exit 0, `setup-final.log`. |
| T2 | EditMode Android, `HBP.PlatformConfiguration.Tests` | 19/19 passent, `android-edit-delivery.xml`, exit 0. |
| T3 | PlayMode Windows, perte de suivi / pose partielle / désactivation | 1/1 passe, `windows-play-final.xml`, exit 0. |
| T4 | Build APK, contenu et manifest Android | Succeeded, 311,91 s, 0 erreur de build, 6 warnings ; `build-quest.log`, `Quest.build-report.json`, exit 0. APK de 102 059 846 octets ; 7 bibliothèques ELF64 AArch64, aucun natif scientifique Desktop ; `quest-apk-content.json`, `quest-manifest.txt`. |
| T5 | Installation/démarrage Quest 3, logs/loader/poses/rendu | ADB install Success ; activité Unity démarrée à froid après résolution du problème système des manettes ; `quest-relaunch.log`, `quest-logcat.txt`, `quest-runtime-proof.json`. Couche native créée, tête et deux contrôleurs suivis, rendu attendu observé. Messages Meta décrits ci-dessous. |
| T6 | EditMode Windows et nouveau Player Windows sans activation XR | 18 tests passent, 1 test Android ignoré ; `windows-edit.xml`, exit 0. Build Succeeded, 40,52 s, 0 erreur, 1 warning ; `build-windows.log`, exit 0. Processus stable, aucun module OpenXR/Meta chargé, aucun settings XR embarqué, assembly HBP.Quest.Runtime exclue ; `windows-runtime-proof.json`. Aucun message d'erreur dans `windows-player.log`. Processus de smoke test fermé par l'agent après relevé. |
| T7 | Format C# et diff | `Tools/format-code.cmd` (8 fichiers C#), exit 0 ; `git diff --check`, réussi. |

Les essais diagnostiques antérieurs ne sont pas les preuves finales : une
erreur de namespace de package corrigée, puis adaptation du test de suivi
(taille du booléen injecté et déplacement en PlayMode pour exercer réellement
le cycle Unity). Une première sélection PlayMode a trouvé zéro test : le
classement de l'assembly a été corrigé avant le run final de 1 test. Les tests
de configuration ont passé dès le premier run.

À `19:33:10.688` (heure casque), les logs indiquent `OpenXRLoader`, runtime
`Oculus`, display actif, `Vulkan`, `HBP-Quest-URP`, HDR false, MSAA 4 et
`SinglePassInstanced`. Les positions locales suivies sont tête
`(0.961, 1.074, -0.241) m`, gauche `(0.956, 0.746, 0.035) m`, droite
`(1.070, 0.747, 0.010) m` ; les échantillons suivants évoluent avec les gestes.
À `19:33:05.751`, `xrCreatePassthroughLayerFB` annonce la création de la couche
native. La perte de suivi ultérieure masque les repères, conformément au test.

Le buffer conservé contient 17 messages Meta
`[XrFuncTable] Failed to look up xrDiscoverSpacesMETA`. Cette fonction de
découverte spatiale n'est pas appelée par le code HiBoP livré ; sa cause dans
le package/runtime n'est pas résolue ici. Ces messages ne sont pas présentés
comme absents ni supprimés des logs. La composition native est créée et la
validation physique M1/M2 a réussi. Aucun crash ni exception C# HiBoP observé.
Les warnings de build incluent notamment des diagnostics de shader liés au
mode sans GPU et un cache de samples absent ; les logs complets sont conservés.

La découverte de descripteurs OpenXR dans le log Windows ne signifie pas une
activation XR : l'inventaire des modules du processus et le rapport d'assets
établissent respectivement l'absence de DLL OpenXR chargée et de settings XR.
Les Resources partagés et les descripteurs des packages restent distribués.

Les normalisations Unity hors tâche (icônes Player, migration OpenXR/WebGL,
BuildInfo et settings d'éditeur) ont été restaurées après vérification. Les
sept activations Android et la sélection Quest 3 sont conservées ; aucune
logique runtime n'a changé entre l'APK testé et le code livré. Les différences
de sérialisation/espaces sont identifiées par `build-source.json`, le snapshot
OpenXR après builds et les hashes finaux du manifeste. Les tests PlayMode ont
été ajustés après le build APK ; ils n'entrent pas dans le Player de livraison.

## Commandes de reproduction

Unity fermé, PowerShell 7.4+, depuis `C:\HBP\Software\HiBoP`, hors sandbox :

```powershell
$questUnity = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$questRoot = 'C:\HBP\Software\HiBoP'
$questTemp = "$questRoot\.test-results\quest-004\tmp"
New-Item -ItemType Directory -Force $questTemp | Out-Null
$questArgs = @('-batchmode','-quit','-nographics','-projectPath',$questRoot,
  '-activeBuildProfile','Assets/Settings/BuildProfiles/Quest.asset',
  '-executeMethod','HBP.Dev.HBPBuilder.BuildFromCommandLine',
  '-buildOutput',"$questRoot\.artifacts\quest-004\Quest",
  '-logFile',"$questRoot\.test-results\quest-004\build-quest.log")
Start-Process -FilePath $questUnity -ArgumentList $questArgs -Wait -PassThru -WindowStyle Hidden -Environment @{TEMP=$questTemp; TMP=$questTemp}
.\Tools\Test-QuestApk.ps1 -Apk .artifacts/quest-004/Quest/HiBoP.Quest.apk -ReportPath .artifacts/quest-004/Quest/quest-apk-content.json
adb -d install -r .artifacts/quest-004/Quest/HiBoP.Quest.apk
adb -d shell am start -S -n fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity
```

Pour Windows : même builder, profil `Assets/Settings/BuildProfiles/DesktopWindows.asset`,
sortie `.artifacts/quest-004/DesktopWindows`. Les arguments exacts des exécutions
sont conservés dans les fichiers `*.command.json` du manifeste.
Les assets sont versionnés et ne nécessitent aucune génération pour construire.
Le menu **Tools > Quest > Rebuild Bootstrap Assets**, avec le profil Quest actif,
recrée explicitement les deux prefabs et leur scène ; il remplace leur contenu.

## Validation manuelle demandée

APK : `C:\HBP\Software\HiBoP\.artifacts\quest-004\Quest\HiBoP.Quest.apk`.
SHA-256 : `17670a734597258a364f60e5a6601cd0a605aa9769b6b43be00a203c72e93495`.
APK installé et lancé sur le Quest 3. Un dialogue système exigeant les
contrôleurs a d'abord empêché le lancement. Le propriétaire a résolu leur
connexion puis rebranché le casque ; la relance finale atteint bien l'activité
`fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity`.
Aucune fixture anatomique nécessaire pour cette tâche.

| ID | Action | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Mettre le Quest 3 et prendre les deux contrôleurs | Pièce réellement visible ; repère bleu gauche et orange droit suivent position et orientation ; panneau lisible. | VALIDE — « M1 OK M2 OK », propriétaire, 2026-09-07 à 19:34 +02:00. |
| M2 | Tourner/incliner la tête et bouger les contrôleurs | Pas d'image noire, instabilité ou décalage anormal. | VALIDE — même retour explicite. |

## Décisions, limites et suite

Aucune décision produit supplémentaire. Les compteurs de sous-systèmes et les
poses ne constituent pas une observation physique de la pièce. La création de
la couche managée ne prouve pas, à elle seule, la composition native finale.
Tout fond opaque observé est un échec passthrough à corriger, même si le panneau
indique `COMPOSITION READY`. Les essais manuels M1/M2 sont maintenant validés
par le propriétaire sur cet APK. Toutes les tâches de J1 disposent désormais
des validations requises ; seule la ligne QUEST-004 du registre est modifiée.
Les messages natifs Meta restent une limite connue à surveiller si une future
tâche active la découverte spatiale ; aucune feature supplémentaire n'a été
activée pour les masquer. Aucune décision ni manipulation utilisateur en attente.
La prochaine tâche proposée est QUEST-005 ; elle n'est pas exécutée ici.
