# Rapport QUEST-007 — Rendu du snapshot anatomique sur Quest

## Résultat

Le bootstrap Quest peut désormais afficher le snapshot HBNA produit par la
colonne Desktop. `QuestAnatomyView.ApplySnapshot` est l'unique point d'entrée
pour un snapshot décodé, utilisable par le futur récepteur réseau sur le thread
principal Unity. Le renderer crée un mesh opaque statique, conserve les buffers
préparés et possède explicitement ses ressources. Aucun calcul natif Android,
transport, transparence ou manipulation de QUEST-008 n'est ajouté.

La démonstration utilise **une injection locale diagnostique**, activée seulement
par la présence de `quest-anatomy.hbna` dans `Application.persistentDataPath`.
La fixture n'est pas embarquée dans l'APK. Sa copie par ADB ne démontre pas le
transfert applicatif prévu au jalon J2.

## État et provenance

- Branche `feature/xr-autonomous`, HEAD initial
  `34753e645d36b621ce5007bf491ef6f32a901256`, checkout initial propre.
- Unity `6000.5.2f1`, fermé au départ ; compilation/tests/build par CLI hors
  sandbox, conformément à AGENTS.md. URP `17.5.0`, OpenXR `1.18.0`, Meta OpenXR
  `2.4.1` et Input System `1.20.0` existants, sans changement de packages.
- Dépendances : rig/passthrough de QUEST-004, contrat de QUEST-005 et capture
  MNI réelle validée en QUEST-006.
- Implémentation **IMPLEMENTEE**, technique **REUSSI**, manuel **VALIDE**.
- Reprises ciblées depuis `eb26c323e` : `SurfaceMeshUploader`, principes de
  `P05StaticSurfaceRenderer`, shader `P05SurfaceOpaque` et éclairage de
  `P05SurfaceCommon`, sous `XR/Assets/HiBoPXR/StaticRendering`. Nouveaux GUID et
  types ; aucun ancien package, cache de meshes ou backend transparent repris.
- L'éclairage historique est uniquement un effet de présentation fondé sur la
  normale et la vue. Aucune normalisation scientifique, projection ou correction
  de coordonnées n'est déplacée dans le shader.
- [Manifeste des preuves](../evidence/QUEST-007/manifest.json). Les binaires,
  fixture et logs volumineux restent locaux dans `.artifacts/quest-007` et
  `.test-results/quest-007`, ignorés par Git. Aucun commit/push ni dépôt voisin modifié.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle à vérifier |
| --- | --- | --- |
| 1 | [AnatomyMeshUploader.CreateMesh](../../../../Assets/Scripts/HBP/Quest/Runtime/AnatomyMeshUploader.cs) | Positions/normales/UV conservés ; indices uint32 pour MNI ; CW conservé, CCW inversé ; refus des repères et opacités non pris en charge. |
| 2 | [QuestAnatomyView.ApplySnapshot / Clear](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) | Livraison main-thread, remplacement après validation, propriété du mesh et libération ; RGB linéaire envoyé par SetVector. |
| 3 | [QuestAnatomy.prefab](../../../../Assets/Prefabs/Quest/QuestAnatomy.prefab) et [QuestBootstrap.prefab](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab) | Groupe spatial indépendant et unique conversion mm→m ; références et matériau sérialisés. |
| 4 | [QuestAnatomyDiagnostic](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyDiagnostic.cs) | Activation par fichier local, décodage borné sur worker, trois cycles de ressources et boutons A/B uniquement diagnostiques. |
| 5 | [AnatomyMeshTests](../../../../Assets/Tests/EditMode/HBP.Quest.Anatomy.Tests/AnatomyMeshTests.cs) et [QuestAnatomyLifetimeTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestAnatomyLifetimeTests.cs) | Comparaison bit à bit de la fixture complète ; destruction après frame, remplacement invalide, absence de reconstruction, livraison worker refusée. |

## Chaîne et conventions

```text
quest-anatomy.hbna (fichier local optionnel)
  -> AnatomySnapshotCodec.Decode (worker)
  -> QuestAnatomyView.ApplySnapshot (thread principal)
  -> AnatomyMeshUploader.CreateMesh (positions en mm)
  -> Quest Anatomy Spatial Group (présentation locale)
     -> Anatomical Frame (mm to m), scale = (0.001, 0.001, 0.001)
        MeshFilter + MeshRenderer + AnatomyOpaque.mat
```

Le repère accepté est `hibop-mni-unity-mm-v1`, gaucher, millimètres,
`MappingVersion=1`, `AssetToBrain` identité. Le producteur Desktop a déjà appliqué
la conversion native X et winding via `Surface.UpdateMesh` /
`hbp_surface_copy_unity_mesh`. Le renderer ne l'applique pas une seconde fois.
Les matrices non identité, autres unités/repères et alpha inférieur à 1 sont
refusés explicitement ; le mesh précédent reste présent après ce refus.
Un snapshot invisible reste chargé avec son renderer désactivé.

La conversion d'unités est uniquement le scale sérialisé de l'enfant. Le groupe
spatial conserve sa position, rotation et échelle lors des livraisons. Pour le
diagnostic seulement, il est placé une fois à 0,65 m devant la tête suivie et
0,12 m plus bas, avec une rotation locale X de -90° : +Z anatomique vers le haut,
+Y vers l'observateur initial, +X à sa droite. Il ne suit ensuite plus la tête.
Les labels X rouge, Y vert et Z cyan sont placés à +100 mm de l'origine ; ils
décrivent les axes du snapshot, sans attribuer une nouvelle convention clinique.
Le rendu et la lisibilité sont validés par les confirmations manuelles ci-dessous.

Le mesh reste en millimètres : bounds source de
`[-69.9079, -105.3945, -50.0656738]` à `[72.17938, 72.04567, 83.34009]`.
Les dimensions métriques attendues sont environ
`0.14208728 × 0.17744017 × 0.133405764 m`, avant rotation de présentation.
Le mesh utilise 69 104 sommets, 138 216 triangles et 69 104 UV ; ses buffers
représentent 3 869 920 octets. Le HBNA mesure 3 870 248 octets, SHA-256
`065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12`.

`UploadMeshData(true)` abandonne la copie CPU du mesh. Les tableaux temporaires
d'upload deviennent collectables ; `Clear`, le remplacement et `OnDestroy`
détruisent le mesh qui possède les buffers GPU. La destruction runtime Unity
est différée à la fin de frame. Le matériau asset est partagé et reste vivant.
Le renderer ne conserve pas le snapshot ; le diagnostic le conserve pour A et
le relâche à sa désactivation. Il ne faut donc pas attendre que B libère cette
copie CPU diagnostique. Aucun mesh n'est reconstruit dans Update.

## Vérifications effectuées

| ID | Vérification | Résultat |
| --- | --- | --- |
| T1 | Génération/import explicite par `HBP.Quest.Editor.QuestAnatomySetup.Apply` | Réussi, exit 0, `setup.log`. |
| T2 | EditMode Windows : `HBP.Quest.Anatomy.Tests;HBP.Transfer.Anatomy.Tests;HBP.PlatformConfiguration.Tests`, avec `-questAnatomyFixture` | 64 réussis, 0 échec, 1 ignoré car réservé au profil Android ; exit 0, `editmode.xml`. Les 10 tests d'anatomie, dont la fixture réelle, passent. |
| T3 | PlayMode Windows : `HBP.Quest.PlayModeTests` | 3/3 réussis, exit 0, `playmode.xml`. Destruction, répétition et garde main-thread exercées. |
| T4 | Formatage C# et revue indépendante | `Tools/format-code.cmd`, exit 0 hors sandbox après restauration NuGet ; défaut de réactivation du diagnostic corrigé par OnEnable/OnDisable symétriques. |
| T5 | EditMode profil Quest : mêmes trois assemblies et fixture | 65/65 réussis, exit 0, `android-editmode.xml`, garde Android comprise. |
| T6 | Build APK IL2CPP et inspection des bibliothèques | Premier build : Succeeded, 289,19 s, 0 erreur, 8 warnings. Build final avec textes agrandis : Succeeded, 61,98 s, 0 erreur, 6 warnings, exit 0 ; APK de 102 162 514 octets, sept bibliothèques ELF64 ARM64, aucun natif scientifique Desktop. |
| T7 | Player Quest 3, trois cycles charger/remplacer/libérer | Réussi : après chaque chargement/remplacement, 1 mesh et 3 869 920 octets ; après chaque libération, 0 mesh et 0 octet. `quest-logcat.txt`, observations détaillées ci-dessous. |
| T8 | Capture stéréo sur Quest | Le cerveau opaque et le passthrough apparaissent dans les deux vues de `quest-screen.png`. Textes diagnostiques trop petits observés, agrandis ensuite dans les prefabs ; ne vaut pas validation propriétaire. |
| T9 | Vérification des prefabs finaux régénérés, profil DesktopWindows | 28 réussis, 0 échec, 1 test Android ignoré, exit 0, `final-prefabs.xml` ; retour de la cible locale à DesktopWindows. |
| T10 | Diff final et provenance | Réécritures Unity de BuildInfo, icônes Player, migration OpenXR et préfiltrage URP inspectées et rétablies ; diff généré conservé localement. Espaces de fin de ligne des nouveaux assets normalisés après build, sans changement de valeurs. `git diff --check` réussit ; seule la ligne QUEST-007 du registre est modifiée. |
| T11 | Reprise ADB Wi-Fi et APK final, 2026-09-08 vers 11:46 +02:00 | Script de connexion, installation et lancement : exit 0. Fixture distante de hash inchangé ; Player actif, `COMPOSITION READY`, deux manettes suivies et événements A/B présents dans `quest-wifi-final-logcat.txt`. Lisibilité et M2 confirmés par le propriétaire. |
| T12 | Arrêt après validation, règle D23 | Journaux finaux relevés, `am force-stop fr.crnl.hibop.quest` réussi (exit 0), aucun PID restant (`pidof` exit 1). [Preuve d'arrêt](../evidence/QUEST-007/final-validation-and-stop.json). |

Les mesures `meshRuntimeBytes` proviennent du Profiler Unity ; `bufferBytesEstimate`
est une estimation comptable, pas une mesure indépendante du pilote GPU.

Au premier run physique, le 2026-09-08 à 10:46:24–25 heure casque, le compteur
d'uploads progresse de 0 à 7, avec retour à 0 mesh après chaque `clear-0/1/2`.
Les allocations Unity totales relevées sont de 91 939 519 octets au départ et
91 954 975 octets après chargement final ; la mémoire managée reste entre
12 443 648 et 12 455 936 octets pendant ces échantillons. Les compteurs ne
constituent pas un budget garanti ni une étude longue de fuite. `dumpsys meminfo`
indique 766 773 KiB de PSS et 495 272 KiB de Graphics pour tout le processus,
incluant XR, les textures et le compositeur ; ce n'est pas le coût du seul cerveau.

Le Player atteint OpenXR `FOCUSED`, Vulkan, `SinglePassInstanced` et
`COMPOSITION READY`. Le log conserve `ClassNotFoundException` concernant
`com.google.android.play.core.assetpacks.AssetPackManager`, déjà présent dans
le log QUEST-003, et `Failed to look up xrDiscoverSpacesMETA`, déjà documenté
en QUEST-004. Ces messages de démarrage ne sont pas corrigés dans QUEST-007 ;
aucune exception C# du nouveau pipeline n'est observée et les cycles réussissent.

## Commandes reproductibles

Depuis `C:\HBP\Software\HiBoP`, Unity fermé, lancer Unity hors sandbox :

```powershell
$questUnity = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$questRoot = 'C:\HBP\Software\HiBoP'
$questCommon = @('-batchmode','-nographics','-projectPath',$questRoot,'-forgetProjectPath')
$questTests = $questCommon + @('-runTests','-testPlatform','EditMode',
  '-assemblyNames','HBP.Quest.Anatomy.Tests;HBP.Transfer.Anatomy.Tests;HBP.PlatformConfiguration.Tests',
  '-questAnatomyFixture',"$questRoot\.artifacts\quest-007\fixture\quest-anatomy.hbna",
  '-testResults',"$questRoot\.test-results\quest-007\editmode.xml",
  '-logFile',"$questRoot\.test-results\quest-007\editmode.log")
Start-Process -FilePath $questUnity -ArgumentList $questTests -Wait -PassThru -WindowStyle Hidden
# PlayMode : même commande, -testPlatform PlayMode et -assemblyNames HBP.Quest.PlayModeTests.
$questBuild = $questCommon + @('-quit','-activeBuildProfile','Assets/Settings/BuildProfiles/Quest.asset',
  '-executeMethod','HBP.Dev.HBPBuilder.BuildFromCommandLine',
  '-buildOutput',"$questRoot\.artifacts\quest-007\Quest",
  '-logFile',"$questRoot\.test-results\quest-007\build-quest.log")
$questTemp = "$questRoot\.test-results\quest-007\tmp"
New-Item -ItemType Directory -Force $questTemp | Out-Null
Start-Process -FilePath $questUnity -ArgumentList $questBuild -Wait -PassThru -WindowStyle Hidden -Environment @{TEMP=$questTemp; TMP=$questTemp}
.\Tools\Test-QuestApk.ps1 -Apk .artifacts/quest-007/Quest/HiBoP.Quest.apk -ReportPath .artifacts/quest-007/Quest/quest-apk-content.json
```

Les assets livrés sont déjà sérialisés. Le menu **Tools > Quest > Rebuild Anatomy
Assets** les régénère explicitement ; `QuestBootstrapSetup.Apply` réattache le
prefab anatomique existant après reconstruction du bootstrap. Ces opérations ne
s'exécutent jamais automatiquement au build.

## Validation manuelle demandée

APK final : `C:\HBP\Software\HiBoP\.artifacts\quest-007\Quest\HiBoP.Quest.apk`,
SHA-256 `ffb61b6d3c0342156e72e559c4965203d2bc36f3e845796f77cd5b0f4526bfac`.
Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-007\fixture\quest-anatomy.hbna`.
Hashes des fichiers livrés dans le manifeste. La fixture est la capture réelle
QUEST-006, recopiée sans modification depuis
`.artifacts/quest-006/captures/20260908-074153-0f0167e7-e0e9-43b5-82c6-38fc381ebfd0/anatomy.hbna`.

Installation et lancement effectués après autorisation explicite du propriétaire
le 2026-09-08 (« Oui j'autorise »). Le contrôle automatique avait refusé deux fois
la copie ADB faute d'autorisation explicite du fichier et de la destination,
malgré les vérifications de provenance MNI sans données patient et d'absence
d'écrasement ; aucun contournement n'a été utilisé.

**Pause matérielle puis reprise le 2026-09-08** : après épuisement de la batterie,
le propriétaire a reconnecté le casque et demandé la connexion ADB Wi-Fi et la
relance de HiBoP XR. Le premier APK a servi aux preuves physiques et à M1.
L'APK final avec les textes agrandis est maintenant **installé et lancé par
ADB Wi-Fi**, avec vérification préalable du hash de la fixture déjà présente.
La lisibilité et M2 ont été confirmés par le propriétaire. Les tailles
sérialisées finales sont `characterSize=0.012` pour le panneau et `0.006` pour
les quatre repères, contre `0.002` initialement. Aucune logique runtime de
rendu, conversion ou libération n'a changé entre ces deux builds.

Le script [Connect-QuestAdbWifi.ps1](../../../../Tools/Connect-QuestAdbWifi.ps1),
récupéré à l'identique depuis `eb26c323e`, a été exécuté avec
`-AdbPath C:\Android\Sdk\platform-tools\adb.exe -NoProximityOverride`.
Il a réactivé TCP/IP via le câble USB puis vérifié `192.168.1.18:5555` ; sortie 0.
Installation et lancement Wi-Fi : sortie 0, `wifi-resume-install.txt`.
La connexion ADB Wi-Fi est un outil de développement et ne démontre pas le
transport applicatif de snapshots du jalon J2.

Pour reproduire la validation, vérifier la lisibilité et effectuer M2 avec la manette droite
connectée. Conformément à D23 et au contrat Quest, après validation manuelle,
récupérer les derniers journaux puis arrêter `fr.crnl.hibop.quest` avec
`adb -s 192.168.1.18:5555 shell am force-stop fr.crnl.hibop.quest` et vérifier
l'absence de PID. Cet arrêt a été effectué et vérifié après le retour final.

```powershell
adb -d install -r .artifacts/quest-007/Quest/HiBoP.Quest.apk
adb -d push .artifacts/quest-007/fixture/quest-anatomy.hbna /sdcard/Android/data/fr.crnl.hibop.quest/files/quest-anatomy.hbna
adb -d shell sha256sum /sdcard/Android/data/fr.crnl.hibop.quest/files/quest-anatomy.hbna
adb -d shell am start -S -n fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity
```

La copie locale persiste après redémarrage du casque. Son retrait désactive le
diagnostic au prochain lancement. A/B sont des commandes de diagnostic, pas
une interface de réception ou de manipulation spatiale.

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Mettre le Quest devant le cerveau après apparition de « LOCAL HBNA DIAGNOSTIC (no network) ». Observer les hémisphères et les labels X/Y/Z/O. | Cerveau complet opaque, trois repères et origine visibles dans le passthrough. | VALIDE : premier retour propriétaire le 2026-09-08, puis lisibilité des textes agrandis confirmée sur l'APK final. |
| M2 | Avec la manette droite connectée, appuyer sur B, puis A ; A peut être répété. | B retire le cerveau, A recharge la même surface au même emplacement ; les repères restent présents. | VALIDE le 2026-09-08 : « Oui tout est ok » en réponse à la demande explicite de confirmation de la lisibilité et de M2. |

Retour reçu : « Je confirme le cerveau avec les 2 hémisphères, les 3 repères
ainsi que un 0 blanc au centre. Par contre pour M2 je ne peux pas vérifier je
n'arrive pas à connecter la manette : je vais peut-être devoir redémarrer le
casque ». Le caractère central est la lettre `O` (origine). Aucune action de
redémarrage ni réinitialisation du casque n'a été effectuée par l'agent.

Après reprise Wi-Fi, le propriétaire a répondu « Oui tout est ok » à la
confirmation explicite des textes agrandis et de M2. Les derniers journaux ont
été conservés dans `quest-wifi-validated-logcat.txt`, puis HiBoP a été arrêté
pour économiser la batterie. La connexion ADB reste disponible.

## Décisions, limites et suite

Aucune décision produit supplémentaire. Le support de transformations anatomiques
autres que le repère préparé Desktop devra être explicite, sans correction
scientifique GPU implicite. Pas de nouvelle preuve réseau, scientifique ou
de transparence. La prochaine tâche proposée est QUEST-008, sans l'exécuter ici.
La pause matérielle est levée ; l'APK final est installé, la lisibilité et M2
sont validés, et l'application est arrêtée conformément à D23. Aucune validation
supplémentaire n'est attendue pour QUEST-007.
