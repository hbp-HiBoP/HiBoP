# Rapport QUEST-003 — Deux Players, deux profils

## Résultat

`DesktopWindows` conserve `HiBoP.unity` et la distribution Desktop existante.
`Quest` sélectionne uniquement `QuestBootstrap.unity`, une caméra dans un prefab,
et produit un APK IL2CPP/ARM64. Les deux profils Unity sérialisent leurs réglages
Player avec Input System seul (`activeInputHandler=1`) et leurs defines
`HIBOP_DESKTOP` / `HIBOP_QUEST`. Le projet et les packages restent communs.

La composition Android est volontairement minimale : aucun loader XR, rig,
passthrough, contrôleur ou calcul natif scientifique n’est démarré. L’APK est
une application Android à fenêtre plane sur le casque ; QUEST-004 apportera
la composition immersive. Aucun asset métier n’a été déplacé hors de Resources.

Le point d’entrée `HBPBuilder.BuildFromCommandLine` utilise le profil actif.
Windows conserve la copie de Data, le retrait des OBJ/Localizers et la documentation.
Android produit seulement l’APK et le rapport, sans recopier Assets/Data.
Les chemins Linux/macOS restent pris en charge par le builder historique.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI. Manuel : VALIDE.
  M1 Windows et M2 Quest confirmés par le propriétaire.
- Branche `feature/xr-autonomous`, HEAD `b1ff09bc49c064f9538caec3d3e91bacddb6950c`.
  Checkout initial propre ; aucun commit, push ou workflow distant lancé.
- Unity 6000.5.2f1 ; versions de packages inchangées depuis QUEST-002-A.
- Unity fermé au départ. CLI hors sandbox, `Start-Process -Wait -PassThru
  -WindowStyle Hidden`. Génération initiale des assets par Unity, avec un script
  ponctuel conservé dans `.test-results/quest-003/create-profiles.cs`.
  La réflexion vers `CreatePlayerSettingsFromGlobal` / `SerializePlayerSettings`
  est limitée à cette génération ; le code livré du builder utilise l’API publique.
- [Manifeste](../evidence/QUEST-003/manifest.json). Les gros logs et binaires sont
  locaux sous `.test-results/quest-003` et `.artifacts/quest-003`.
- Dépendances : QUEST-002 et QUEST-002-A, profils New sur les deux cibles.
- Les deux builds finaux utilisent la même implémentation. Snapshot avant builds :
  `.test-results/quest-003/delivery-source.json`, hash du diff suivi
  `b41cffd2e21570497c82e20fae74f479328c22a43bc47c266373f3b7dd5d9f27`.
  Le manifeste conserve aussi les hashes des fichiers nouveaux et du diff livré.
  Unity a ensuite sérialisé les preloads du profil Quest et retiré ses Resources
  temporaires de tests. Les normalisations globales d'import et BuildInfo généré
  ont été restaurés après vérification ; ces différences sont identifiées dans
  le manifeste, elles ne constituent pas un changement de code entre builds.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle / risque |
| --- | --- | --- |
| 1 | [DesktopWindows](../../../../Assets/Settings/BuildProfiles/DesktopWindows.asset), [Quest](../../../../Assets/Settings/BuildProfiles/Quest.asset) | Une scène propre à chaque profil ; overrides Player versionnés, New, ARM64/IL2CPP Android. |
| 2 | [HBPBuilder.BuildFromCommandLine / BuildQuest](../../../../Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs) | Même point d’entrée ; préserver le packaging Desktop, éviter sa copie de données vers Android. |
| 3 | [HBPBuildProfiles.Validate / WriteReport](../../../../Assets/Scripts/HBP/Dev/Editor/HBPBuildProfiles.cs), [tests](../../../../Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/BuildProfileConfigurationTests.cs) | Arrêt avant build si mauvaise scène, mauvais profil, backend legacy, loader Desktop ou natif Desktop dans Quest. |
| 4 | [QuestBootstrap.prefab](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab), [scène](../../../../Assets/_Scenes/QuestBootstrap.unity) | Caméra sérialisée, pas de manager Desktop ni de GameObject construit au runtime. |
| 5 | [workflow](../../../../.github/workflows/build.yml), [Test-QuestApk.ps1](../../../../Tools/Test-QuestApk.ps1) | Quest seulement sur demande explicite, caches et concurrence séparés, aucune publication ; inspection ELF64 AArch64 réelle. |

## Commandes de reproduction

Avec Unity fermé, depuis `C:\HBP\Software\HiBoP` :

```powershell
$questUnity = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$questCommon = @('-batchmode','-quit','-nographics','-projectPath','C:\HBP\Software\HiBoP','-executeMethod','HBP.Dev.HBPBuilder.BuildFromCommandLine')
$questDesktopArgs = $questCommon + @('-activeBuildProfile','Assets/Settings/BuildProfiles/DesktopWindows.asset','-buildOutput','C:\HBP\Software\HiBoP\.artifacts\quest-003\desktop','-logFile','C:\HBP\Software\HiBoP\.test-results\quest-003\build-windows.log')
Start-Process -FilePath $questUnity -ArgumentList $questDesktopArgs -Wait -PassThru -WindowStyle Hidden
$questAndroidArgs = $questCommon + @('-activeBuildProfile','Assets/Settings/BuildProfiles/Quest.asset','-buildOutput','C:\HBP\Software\HiBoP\.artifacts\quest-003\quest','-logFile','C:\HBP\Software\HiBoP\.test-results\quest-003\build-quest.log')
$questTemp = 'C:\HBP\Software\HiBoP\.test-results\quest-003\tmp'
New-Item -ItemType Directory -Force -Path $questTemp | Out-Null
Start-Process -FilePath $questUnity -ArgumentList $questAndroidArgs -Wait -PassThru -WindowStyle Hidden -Environment @{TEMP=$questTemp; TMP=$questTemp}
.\Tools\Test-QuestApk.ps1 -Apk .artifacts/quest-003/quest/HiBoP.Quest.apk -ReportPath .artifacts/quest-003/quest/quest-apk-content.json
```

Les profils sont aussi accessibles dans **File > Build Profiles**. Le menu
**Tools > Build HiBoP** conserve ses sélections Desktop ; la voie CLI ci-dessus
fournit l’APK minimal avec rapport et sans installer l’application.

## Vérifications effectuées

Les preuves finales sont sous `C:\HBP\Software\HiBoP\.test-results\quest-003` ;
les rapports de contenu sont sous `.artifacts/quest-003`. Le manifeste donne
leurs chemins, SHA-256 et arguments CLI exacts. Tous les runs ci-dessous ont
terminé avec exit code 0.

| Vérification | Résultat | Preuve locale |
| --- | --- | --- |
| EditMode Android, `HBP.PlatformConfiguration.Tests` | 15/15 passent, y compris refus mauvaise scène/architecture et retrait ciblé du preload OpenXR | `android-edit-final.xml` |
| EditMode Windows, filtre `HBP.Tests.PlatformConfiguration;HBP.Tests.Serialization.QuestAnatomyFixtureTests` | 16 passent, 1 ignoré car spécifique Android | `windows-edit-final.xml` |
| PlayMode Windows, `HBP.UI.PlayModeTests;HBP.Module3D.PlayModeTests` | 81/81 passent | `windows-play.xml` |
| Build Windows IL2CPP x64 | Succeeded, 31,15 s, 0 erreur, 1 warning | `build-windows-delivery.log`, `DesktopWindows.build-report.json` |
| Build Quest IL2CPP ARM64 | Succeeded, 71,66 s, 0 erreur, 2 warnings | `build-quest-delivery.log`, `Quest.build-report.json` |
| APK ZIP et en-têtes natifs | 4 bibliothèques ELF64 AArch64 ; aucun natif Desktop | `quest-apk-content.json` |
| Player Windows avec fixture en arguments | Processus stable, aucun script manquant ni erreur de sérialisation dans le log ; contrôle visuel confirmé par le propriétaire (« M1 OK ») | `windows-player-delivery.log` |
| APK installé et relancé sur Quest 3, Android 14 / SDK 34 | ADB Success, activité Unity démarrée, fenêtre stable confirmée par le propriétaire | `quest-install.log`, `quest-relaunch.log`, `quest-confirmed.json` |
| Format C# et diff | `Tools/format-code.cmd` et `git diff --check` réussis | Vérification locale |
| CI | YAML analysé ; sélection manuelle Quest, séparation caches/concurrence, absence de publication vérifiées | `.github/workflows/build.yml` ; job distant NON_EXECUTE |

Extraits des rapports finaux : Windows a `scenes=[Assets/_Scenes/HiBoP.unity]`,
Quest a `scenes=[Assets/_Scenes/QuestBootstrap.unity]`. Aucun ne contient
`OpenXR Package Settings.asset` dans `packedAssets`. Le champ `plugins.included`
décrit la compatibilité de l'importeur, pas une preuve de présence finale :
les inventaires du dossier distribué et du ZIP APK servent à cette dernière.

| Contenu réel | Windows | Quest |
| --- | --- | --- |
| Taille livrée | 631 062 184 octets, 106 fichiers | APK : 98 637 675 octets |
| Assemblies après stripping, avant conversion IL2CPP | 109 | 104 |
| Natif scientifique | Une copie x86_64 de hbp_core, hbp_math, EEGFormat, hashes identiques aux sources et lock | Aucun |
| Autres natifs | Unity/Burst ; XRSimulationSubsystem du package reste présent en ARM64 et x86_64, sans loader initialisé | lib_burst_generated, libil2cpp, libmain, libunity, tous ARM64 |
| Assets partagés | Resources conservés ; copie Data historique sans OBJ ni Localizers | Resources : 153 566 035 octets non compressés ; aucune copie externe de Data |

`libil2cpp.usym.so` est une table de symboles Unity (`sym-`, version 2), validée
séparément : ce n'est pas un ELF. Les tailles `BuildReport.totalSize`
(1 955 199 424 Windows ; 1 432 315 262 Android) incluent des intermédiaires
IL2CPP ; elles ne doivent pas être présentées comme tailles de livraison.

Deux problèmes ont été corrigés avant les builds finaux. Le helper du package
OpenXR ajoutait ses settings aux preloads même sans loader, provoquant des
erreurs de sérialisation au lancement Windows. Le callback d'ordre 1000 retire
uniquement ces settings quand OpenXR est inactif ; un test vérifie la conservation
des autres assets. Les logs de démarrage finaux ne reproduisent plus ces erreurs.
Le profil Quest peut conserver un preload OpenXR sérialisé par Unity ; le retrait
est appliqué au build et son exclusion effective est prouvée par le rapport.

Gradle échouait localement sur une socket Unix Windows (`Invalid argument`).
Le TEMP/TMP ordinaire transmis uniquement au processus Unity résout ce problème,
sans changement système. La commande ci-dessus nécessite PowerShell 7.4+ pour
`-Environment`. `JAVA_TOOL_OPTIONS` seul était filtré par Unity et insuffisant.
Les anciens logs `build-quest.log` / `build-quest-final.log` sont des échecs
diagnostiques ; leurs blocs de variables d'environnement ont été expurgés.
Voir [Java 17 Networking](https://docs.oracle.com/en/java/javase/17/core/java-networking.html)
et [discussion OpenJDK sur les répertoires temporaires Windows](https://mail.openjdk.org/pipermail/nio-dev/2023-March/013297.html).

## Validation manuelle demandée

La fixture est reconstruite par `Tools/Prepare-QuestAnatomyFixture.ps1`, avec
SHA-256 `0bd800ec656ed8be03777b85457e47a749bbfd5b450d368185baab1b0e32a61a`.
Aucun résultat manuel n’est déduit du succès d’un build.

1. **M1 — Windows, VALIDE.** Commande fournie pour la validation :

   ```powershell
   & 'C:\HBP\Software\HiBoP\.artifacts\quest-003\desktop\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-003\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy'
   ```

   Attendu : HiBoP reste ouvert et la vue `MNI Anatomy` affiche le cerveau de la
   fixture. Retour reçu du propriétaire : « M1 OK ».
   Le processus caché du smoke test a été fermé par l'agent avant remise.

2. **M2 — Quest, VALIDE.** APK fourni :
   `C:\HBP\Software\HiBoP\.artifacts\quest-003\quest\HiBoP.Quest.apk`,
   SHA-256 `bc0352351b6afebc428264e803233368bc79647dbb36d9796a4bc65f0d768542`.
   Installation et relance ADB déjà réalisées, package `fr.crnl.hibop.quest`,
   activité `com.unity3d.player.UnityPlayerActivity`.
   Le propriétaire a confirmé la splash HiBoP puis : « La fenêtre sombre est
   visible et reste ouverte ». Aucun cerveau, rig ni contrôleur attendu.

## Décisions, limites et suite

Le job distant n’est pas exécuté. Les signatures de distribution et la publication
ne sont pas dans cette tâche. Le backend hbp_core Android reste pour les tâches
natives prévues. Les `Resources` communs restent inclus et mesurés ; aucune
migration générale d’assets ni modification de distribution n’est nécessaire.
Le log Android initial contient une `ClassNotFoundException` non fatale pour
`com.google.android.play.core.assetpacks.AssetPackManager` ainsi qu'un warning
d'orientation ; le démarrage et l'affichage continuent. Aucun crash n'a été
observé. Cette dépendance de distribution Play n'a pas été ajoutée à l'APK minimal.
Les logs récupérés après relance étaient vides ; la preuve finale d'affichage
est le retour explicite du propriétaire, complété par le PID actif via ADB.
Aucune décision de périmètre ni validation manuelle ne reste en attente.
La prochaine tâche proposée est QUEST-004, sans exécution automatique.

Références d’API : [BuildProfile Unity 6000.5](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Build.Profile.BuildProfile.html),
[action unity-setup](https://github.com/marketplace/actions/unity-setup).
Les méthodes et la sérialisation utilisées ont aussi été observées dans
l’installation locale Unity 6000.5.2f1.
