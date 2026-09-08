# Rapport QUEST-008 — Manipulation locale du groupe cerveau

## Ajustements après validation complète — 2026-09-08

Le propriétaire confirme **« Je confirme le bon fonctionnement de tout. »**
après réinsertion de la pile et nouvel essai. **M1, M2 et M3 sont VALIDE pour
l'APK initial** de SHA-256
`a0fa5d0c8e8ffc52b470972bbd2576ec668bb6f5e91d7feca53ef242dfff6fb2`.
Les sections historiques ci-dessous décrivent cette version et ses essais.

À sa demande (D24–D26), la nouvelle version apporte :

- **Gâchettes d'index** gauche/droite pour prendre, déplacer, tourner et
  agrandir/réduire à deux manettes. Les grips latéraux n'agissent plus sur le
  cerveau ; **X** conserve le recentrage.
- **Déplacement libre en passthrough** : le propriétaire a identifié la grille
  système Quest. `QuestBoundaryVisibility` demande sa suppression via
  `XR_META_boundary_visibility` quand HiBoP a le focus et présente le passthrough.
  La caméra doit être active, le fond transparent et le fournisseur prêt.
  Le runtime Meta reste l'autorité sur l'acceptation de la demande.
  Une pause, une perte de focus, une désactivation ou une perte du passthrough
  demande la restauration. La reprise invalide l'ancienne demande et réessaie.
  L'origine reste au sol, avec recentrage autorisé pour utiliser local-floor
  au lieu d'un espace Stage lié à la frontière. Aucun ancrage persistant de
  pièce n'est ajouté.
- **Mode de développement éveillé sur chargeur**, avec
  `Tools/Connect-QuestAdbWifi.ps1 -KeepAwakeWhilePluggedIn`.
  Le réglage Android est relu après application ; la valeur 15 a été observée
  sur ce Quest. La proximité est forcée fermée. Ce mode reste actif entre les
  essais à la demande du propriétaire ; D23 arrête seulement HiBoP après validation.

La fonctionnalité OpenXR `BoundaryVisibilityFeature Android` est activée,
avec **`m_SuppressVisibility=0`** pour laisser HiBoP piloter le contexte.
`QuestRig/QuestBoundaryVisibility.passthrough` référence le fournisseur du même
prefab et `freeMovementInPassthrough=1`. Le garde de build rejette l'activation
de la suppression automatique. La cible Desktop reste désactivée.

Points de review des ajustements :

1. [QuestAnatomyInput](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyInput.cs) : mapping `triggerPressed`, gestes annulés si le suivi est perdu.
2. [QuestBoundaryVisibility](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestBoundaryVisibility.cs) : acceptation seulement sur `XrResult.Success`, état effectif asynchrone, restauration et reprise après veille.
3. [QuestBoundarySetup](../../../../Assets/Scripts/HBP/Quest/Editor/QuestBoundarySetup.cs) et [QuestBuildValidation](../../../../Assets/Scripts/HBP/Quest/Editor/QuestBuildValidation.cs) : câblage sérialisé et suppression contextuelle obligatoire.
4. [QuestBootstrapTests](../../../../Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/QuestBootstrapTests.cs) et [QuestManipulationInputTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestManipulationInputTests.cs) : retour de session, garde de build, absence de geste avec les grips et entrées d'index sur le rig complet.
5. [Connect-QuestAdbWifi.ps1](../../../../Tools/Connect-QuestAdbWifi.ps1) : maintien éveillé vérifié sur alimentation AC et USB, connexion sans relancer automatiquement le serveur ADB.

Vérifications de cette version : **69/69 EditMode et 15/15 PlayMode réussis**,
dont invariance bit-exacte de la fixture MNI et de ses distances, mesh inchangé,
bornes d'échelle et relâchement stable. Logs/XML séparés de la version initiale
dans `.test-results/quest-008/trigger-boundary/`. Format C# exécuté avec succès.
Une revue indépendante ciblée a conduit à invalider la demande acceptée au
retour de focus/session ; les deux cas sont couverts par les tests.

Build Android IL2CPP ARM64 réussi en **144,04 s**, **0 erreur / 6 avertissements**.
APK de **102 181 014 octets**, SHA-256
`bb60d1407b8cb07af992689dee47da86b2cac6a6dc0fdc030c0364f72df179e9`.
Audit réussi : 7 bibliothèques ARM64, aucune bibliothèque scientifique Desktop.
`aapt dump permissions` confirme `com.oculus.permission.BOUNDARY_VISIBILITY`.
Installation ADB Wi-Fi : `Success` ; lancement `Status: ok`, COLD 170 ms,
PID 12745. Fixture sur casque vérifiée par SHA-256 inchangé.

Le journal du casque du 2026-09-08 confirme : refus transitoire
`1000528000` à 12:56:16.135, puis `result=Success` à 12:56:16.914 et
**`actual=VisibilitySuppressed` à 12:56:16.927**. COMPOSITION READY et tête suivie.
Les manettes sont encore non suivies pendant cette capture hors tête ; leur
maniement a ensuite été confirmé par M4. Le message Java AssetPackManager déjà
documenté dans la version initiale reste présent, sans erreur C# nouvelle observée.

Les préférences ADB locales ont été sauvegardées puis passées de 1 à 0.
Un cycle Unity complet de validation de configuration s'est terminé avec code 0 :
le serveur conserve le **PID 5072**, les transports USB et Wi-Fi restent présents,
et `QUEST_ADB_SURVIVED_UNITY` est obtenu **sans reconnexion**. Preuves
`adb-before-unity.txt`, `adb-after-unity.txt` et `adb-unity-check.log`.
Le casque rapporte `mWakefulness=Awake`, `mIsPowered=true`, `mStayOn=true`,
`mStayOnWhilePluggedInSetting=15` dans `device-power.txt`.

Après validation de la version initiale, le premier arrêt n'avait pas pu être
vérifié car le casque était hors ligne. Après reconnexion, `am force-stop`
réussit (0) et `pidof` ne retourne aucun PID (1), avant installation du nouvel
APK. Preuve : `validated-baseline-stop.txt`. Après validation M4/M5, le nouvel
APK a également été arrêté : force-stop 0, pidof 1 sans PID. ADB répond encore
et le casque reste Awake, alimenté et StayOn. Preuve : `validated-stop.txt`.

### Recette des ajustements

Nouvel APK : `.artifacts/quest-008/trigger-boundary/Quest/HiBoP.Quest.apk`.
Fixture inchangée : le fichier HBNA MNI déjà présent dans le stockage HiBoP.
Validation initiale conservée. Le propriétaire répond **« Tout me semble ok »**
à la recette M4/M5 le 2026-09-08 : les ajustements sont **VALIDES**.

| ID | Action | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M4 | Approcher une manette du cerveau, maintenir la gâchette d'index et déplacer/tourner ; ajouter l'autre index et écarter/rapprocher les mains ; relâcher puis X. | Gestes identiques à la version validée avec les nouvelles gâchettes ; grips latéraux sans action sur le cerveau. | VALIDE |
| M5 | Marcher autour du cerveau en passthrough dans l'espace physique dégagé, au-delà de l'ancienne grille ; ouvrir le menu Quest puis revenir dans HiBoP. | Pas de grille pendant le passthrough HiBoP, suivi continu ; comportement rétabli au retour dans l'application. Le système conserve la gestion de ses limites hors HiBoP. | VALIDE |

Le journal final confirme le cycle natif : `VisibilityNotSuppressed` à
12:58:49.237 puis `VisibilitySuppressed` à 12:58:50.383 au retour dans HiBoP.
Preuve : `validated-device-logcat.txt`. Aucune validation manuelle restante.

### Connexion durable de développement

Deux préférences Unity locales ont été trouvées à 1 :
`AndroidADBKillServerOnExit` et `AndroidADBKillExternalInstance`.
Elles peuvent arrêter le serveur ADB à la fermeture de Unity ou remplacer un
serveur externe. Le serveur et MQDH utilisaient l'ADB Unity 36.0.0, tandis que
le script utilisait l'ADB autonome 37.0.1. La désactivation ciblée de ces
préférences permet de conserver le serveur ; aucune application MQDH n'est arrêtée.

Ce mode ne garantit pas un Wi-Fi permanent : PC et Quest doivent rester sur un
réseau joignable ; après redémarrage du casque, relancer le script et utiliser
l'USB si ADB TCP n'écoute plus. Brancher sur un chargeur indépendant est possible
tant que l'alimentation et le réseau restent disponibles. La frontière est
supprimée dans le contexte passthrough de HiBoP, pas globalement dans le système.

Références : [Unity Boundary visibility](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.4/manual/features/boundary-visibility.html),
[Meta MQDH](https://developers.meta.com/horizon/documentation/unity/ts-mqdh-basic-usage/),
[préférences Android Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/android-sdksetup.html).

## Version initiale validée — résultat et historique

Le cerveau de QUEST-007 était affiché à une pose fixe. Le groupe `QuestAnatomy`
peut maintenant être saisi avec le **grip latéral gauche ou droit**, déplacé et
tourné. Maintenir les deux grips permet de déplacer/tourner le groupe et de
changer uniformément son échelle en écartant ou rapprochant les manettes.
**X**, sur la manette gauche, recentre le cerveau devant la tête en conservant
son échelle. Le relâchement immobilise immédiatement le groupe, sans inertie.

Les gestes ne modifient que le parent de présentation. L'enfant
`Anatomical Frame (mm to m)` conserve sa conversion unique `0.001`, sa pose
locale et son mesh. Les repères diagnostiques restent dans ce même parent.
Aucune coordonnée anatomique, donnée source, caméra Desktop ou commande réseau
n'est modifiée. Les boutons diagnostiques droits **A** (recharger) et **B**
(retirer la surface) restent disponibles.

## État et provenance

- Branche `feature/xr-autonomous`, HEAD initial
  `2918139dc499ae050f335a37b3ab1512189bfbfd`, checkout initial propre.
- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** pour les contrôles
  automatisés et le build ; manuel initial : **VALIDE** après retour complet du propriétaire.
- Unity `6000.5.2f1`, fermé au départ ; CLI hors sandbox. Packages inchangés :
  Input System `1.20.0`, OpenXR `1.18.0`, Meta OpenXR `2.4.1`, URP `17.5.0`.
- Dépendances réutilisées : rig de QUEST-004, contrat HBNA de QUEST-005 et
  rendu/fixture de QUEST-007, issus de la capture Desktop QUEST-006.
- [Manifeste](../evidence/QUEST-008/manifest.json). Les APK, fixtures et logs
  dans `.artifacts/quest-008` et `.test-results/quest-008` sont des preuves
  locales ignorées par Git. Les sources, rapport et manifeste sont à versionner.
  Aucun commit, push ou changement de dépôt voisin effectué.

## Points prioritaires de review

| Priorité | Entrée | Règle ou risque à vérifier |
| --- | --- | --- |
| 1 | [QuestAnatomyManipulator.Step / Anchor](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyManipulator.cs) | Pose relative capturée à la prise ; recalage sans saut lors des transitions une/deux manettes ; échelle uniforme bornée ; arrêt exact au relâchement. |
| 2 | [QuestAnatomyInput.LateUpdate](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyInput.cs) | Entrées après le suivi ; grips gauche/droit et X ; annulation sur perte de suivi/focus, pause, désactivation ou remplacement du mesh. Aucun chemin Desktop/réseau. |
| 3 | [QuestAnatomy.prefab](../../../../Assets/Prefabs/Quest/QuestAnatomy.prefab) et [QuestBootstrap.prefab](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab) | `QuestAnatomyManipulator` sur le parent ; références view/head/left/right/manipulator sérialisées dans `Local Anatomy Diagnostic/QuestAnatomyInput`. Aucun GameObject de secours créé au runtime. |
| 4 | [AnatomyManipulationTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyManipulationTests.cs) et [QuestManipulationInputTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestManipulationInputTests.cs) | Fixture MNI réelle bit-exacte, distances source conservées, mesh non reconstruit ; boutons et suivi simulés sur le prefab complet. |
| 5 | [QuestAnatomySetup.Apply / AttachToBootstrap](../../../../Assets/Scripts/HBP/Quest/Editor/QuestAnatomySetup.cs) et [QuestAnatomyDiagnostic.Run](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyDiagnostic.cs) | Régénération explicite des références et affichage des commandes exactes. Le placement initial appartient désormais à l'entrée Quest et fonctionne aussi avec le diagnostic désactivé. |

### Prise et réglages UX provisoires

Valeurs effectives dans `QuestAnatomy.prefab`, composant `QuestAnatomyManipulator` :

| Champ | Valeur | Effet |
| --- | --- | --- |
| Transform / Scale | `(1,1,1)` | Taille initiale du MNI : environ 14,21 × 17,74 × 13,34 cm avant orientation. |
| `minimumScale` / `maximumScale` | `0.25` / `4` | Bornes uniformes ; la plus grande dimension varie d'environ 4,44 à 70,98 cm. |
| `grabPaddingMeters` | `0.12` | Première prise dans les bounds orientées du cerveau ou jusqu'à 12 cm de celles-ci. |
| `minimumHandDistanceMeters` | `0.08` | Sous 8 cm de séparation, geler le geste à deux manettes ; recaler la référence lorsque les manettes se séparent. |
| `recenterDistanceMeters` | `0.65` | Centre des bounds 65 cm devant la tête, dans la direction horizontale du regard. |
| `recenterHeightMeters` | `-0.12` | Centre 12 cm sous la tête. |
| `recenterEuler` | `(-90,0,0)` | Orientation anatomique initiale de QUEST-007, relative à la direction horizontale du regard. |

La première prise exige un nouvel appui à proximité. Entrer dans le cerveau
avec le grip déjà maintenu ne le capture pas. Une fois la première manette
attachée, la seconde peut engager son grip à distance pour régler l'échelle.
Chaque changement de nombre de prises recapture les références à la pose
courante, sans déplacement instantané. La rotation à deux manettes suit leur
axe relatif ; la rotation du poignet avec une seule manette couvre la rotation
libre. Le suivi perdu ne réattache pas automatiquement une manette avec son grip
maintenu : relâcher puis presser à nouveau.

Le recentrage annule la prise, conserve le mesh et l'échelle choisie, puis
replace le centre des bounds. Le placement initial ne se répète pas lors d'un
rechargement diagnostique A. Il n'existe aucun lissage retardé, Rigidbody ni
vitesse de lancer pouvant produire une dérive après release.

Ces valeurs sont des propositions UX pour l'essai, pas des règles scientifiques.
La distance, la zone de prise et les bornes peuvent être ajustées d'après le
retour de confort du propriétaire.

### Comparaison aux intentions HoloLens

Référence versionnée consultée en lecture seule dans le dépôt voisin
`HiBoP_HoloLens`, commit `5a119948df337454c1dc9a053faf375f024a02b9` :
`Assets/Prefabs/3D/Scenes/Scene 3D.prefab` et
`Assets/Scripts/HBP/HoloLens/Module3D/Base3DScene.cs`.
Le prefab emploie `ObjectManipulator`, un `hostTransform`,
`manipulationType=3`, `twoHandedManipulationType=7` et des constantes de lissage
`0.001`. Le code propose une position initiale à 0,5 m devant la caméra.
L'audit historique a également été consulté par
`git show eb26c323e:Docs/dev/xr/hololens/reusable-components.md`.

QUEST-008 reprend l'intention fonctionnelle de manipuler un parent commun,
avec une ou deux prises. Son adaptation utilise les contrôleurs OpenXR déjà
installés et un état spatial local ; aucune copie MRTK, logique scientifique,
interaction de mains, manipulation lointaine par rayon ou dépendance réseau
historique n'est importée. Aucun GUID historique n'est repris.

## Vérifications effectuées

| ID | Scénario | Résultat / preuve |
| --- | --- | --- |
| T1 | Génération par `HBP.Quest.Editor.QuestAnatomySetup.Apply` | CLI exit 0, `.test-results/quest-008/setup.log`. |
| T2 | PlayMode `HBP.Quest.PlayModeTests`, fixture réelle | **15/15 réussis**, 0 ignoré, exit 0, `playmode.xml` et `playmode.log`. |
| T3 | MNI après prise, rotation, échelle, release et recentrage | 69 104 sommets ; HBNA de 3 870 248 octets strictement identique ; paire testée à `3.84311461 mm`, distance inchangée ; même mesh/bounds, aucun upload supplémentaire. Inclus dans T2. |
| T4 | Bornes, dérive et groupe | Échelle positive uniforme 0,25–4 ; aucune modification de pose pendant 100 échantillons après release ; enfant mm→m et mesh conservés au recentrage ; transitions, proximité et perte de suivi testées. Inclus dans T2. |
| T5 | Input System sur le prefab complet | Grips, X, focus, perte de suivi partielle, reprise exigeant release et remplacement pendant la prise testés ; diagnostic désactivé. Inclus dans T2. |
| T6 | EditMode final : anatomie, contrat et plateforme | **65/65 réussis**, 0 ignoré, exit 0 ; `editmode.xml` / `editmode.log`. |
| T7 | APK IL2CPP ARM64, profil Quest | **Succeeded**, 235,97 s, 0 erreur, 6 warnings ; exit 0, `build-quest.log`. Audit APK exit 0 : 7 bibliothèques ARM64, aucune bibliothèque scientifique Desktop, 102 166 750 octets. |
| T8 | Formatage | `Tools/format-code.cmd`, exit 0 ; `format.log`. Restauration NuGet exécutée hors sandbox. |
| T9 | Essai physique des gestes/confort | **PARTIEL** : retour propriétaire du 2026-09-08, essai à une manette concluant ; grossissement non testé, gauche indisponible aussi dans le menu Quest. M2/M3 restent ouverts. |
| T10 | Installation et démarrage sur Quest 3 Android 14 / API 34 | `adb install -r` : Success, exit 0 ; hash de la fixture inchangé. Relance `am start -W` : Status ok, COLD, TotalTime 182 ms, PID 31116. OpenXR/Vulkan/SinglePassInstanced, COMPOSITION READY et MNI à 69 104 sommets observés dans `quest-logcat.txt`. |

Le log physique conserve la `ClassNotFoundException` Android concernant
`com.google.android.play.core.assetpacks.AssetPackManager`, déjà documentée
dans QUEST-007/QUEST-003. Le démarrage atteint ensuite OpenXR FOCUSED et
COMPOSITION READY ; aucune nouvelle exception C# des composants de manipulation
n'est présente dans cet échantillon.

`git diff --check` réussit. Après inspection, les changements générés par Unity
dans BuildInfo, les icônes Player, la migration OpenXR et les préfiltrages URP
ont été remis à leur version initiale. Les sources de gestes testées et compilées
restent inchangées ; le BuildInfo effectivement embarqué est conservé dans
`.artifacts/quest-008/Quest/BuildInfo.json`. Le prefab Bootstrap régénéré possède
des fileIDs renouvelés pour ses enfants diagnostiques, en plus des références
du nouveau composant d'entrée ; leurs poses, textes, matériaux et parentés sont
conservés. Les blancs de fin de ligne générés ont été normalisés.

Les premières exécutions ont révélé des problèmes de montage des tests : profil
Quest non activé, cycle de vie MonoBehaviour indisponible en EditMode et format
des boutons simulés. Les tests de gestes ont été déplacés en PlayMode ; le layout
de test utilise des états flottants pour éviter la normalisation BYTE et les
événements delta sur bits. Les sorties antérieures sont conservées localement
comme tentatives échouées, distinctes des résultats finaux. Aucune assertion
bloquante async n'a été ajoutée.

Commandes reproductibles depuis `C:\HBP\Software\HiBoP`, Unity fermé,
dans PowerShell hors sandbox :

```powershell
$questUnity = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$questCommon = @('-batchmode','-accept-apiupdate','-projectPath','C:\HBP\Software\HiBoP',
  '-activeBuildProfile','Assets/Settings/BuildProfiles/Quest.asset','-forgetProjectPath')
$questTests = $questCommon + @('-runTests','-testPlatform','PlayMode',
  '-assemblyNames','HBP.Quest.PlayModeTests',
  '-questAnatomyFixture','C:\HBP\Software\HiBoP\.artifacts\quest-008\fixture\quest-anatomy.hbna',
  '-testResults','C:\HBP\Software\HiBoP\.test-results\quest-008\playmode.xml',
  '-logFile','C:\HBP\Software\HiBoP\.test-results\quest-008\playmode.log')
Start-Process $questUnity -ArgumentList $questTests -Wait -PassThru -WindowStyle Hidden
# EditMode : ajouter -nographics, utiliser -testPlatform EditMode et
# -assemblyNames 'HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests;HBP.Transfer.Anatomy.Tests'.
# Changer les chemins de sortie en editmode.xml et editmode.log.
$questTemp = 'C:\HBP\Software\HiBoP\.test-results\quest-008\tmp'
New-Item -ItemType Directory -Force $questTemp | Out-Null
$questBuild = $questCommon + @('-nographics','-quit',
  '-executeMethod','HBP.Dev.HBPBuilder.BuildFromCommandLine',
  '-buildOutput','C:\HBP\Software\HiBoP\.artifacts\quest-008\Quest',
  '-logFile','C:\HBP\Software\HiBoP\.test-results\quest-008\build-quest.log')
Start-Process $questUnity -ArgumentList $questBuild -Wait -PassThru -WindowStyle Hidden -Environment @{TEMP=$questTemp; TMP=$questTemp}
.\Tools\Test-QuestApk.ps1 -Apk .artifacts/quest-008/Quest/HiBoP.Quest.apk -ReportPath .artifacts/quest-008/Quest/quest-apk-content.json
```

## Validation manuelle demandée

APK construit et installé :
`C:\HBP\Software\HiBoP\.artifacts\quest-008\Quest\HiBoP.Quest.apk`, SHA-256
`a0fa5d0c8e8ffc52b470972bbd2576ec668bb6f5e91d7feca53ef242dfff6fb2`.
Fixture locale :
`C:\HBP\Software\HiBoP\.artifacts\quest-008\fixture\quest-anatomy.hbna`,
copie inchangée de QUEST-007, SHA-256
`065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12`.
Sur le casque, chemin existant :
`/sdcard/Android/data/fr.crnl.hibop.quest/files/quest-anatomy.hbna`.
Il s'agit toujours d'une injection locale MNI, pas d'un transfert applicatif.

Le propriétaire a demandé ADB Wi-Fi avec capteur de proximité désactivé pour
laisser le casque branché au secteur. Le script
`Tools/Connect-QuestAdbWifi.ps1 -AdbPath C:\Android\Sdk\platform-tools\adb.exe`
a reconnecté `192.168.1.18:5555` et envoyé le broadcast Meta `prox_close`,
exit 0 (`adb-wifi.log`). Après une perte de connexion pendant les opérations
Unity, le même script a rétabli le transport, exit 0 (`adb-wifi-final.log`).
L'installation a réussi à 12:19:38 heure du casque le 2026-09-08.
La première commande de lancement a été acceptée, mais l'activité
`com.oculus.guardian/com.oculus.vrguardianservice.guardiandialog.GuardianDialogActivity`
était signalée au premier plan et aucun PID HiBoP n'était présent. Le propriétaire
a précisé **« Je n'ai pas la fenêtre »** : ce relevé Android n'établissait donc
pas la présence d'une fenêtre visible. Aucun Guardian n'a été désactivé.
La relance avec attente explicite `am start -W` a réussi : lancement à froid,
182 ms, PID 31116, le 2026-09-08 vers 12:23 heure casque (`relaunch.txt`).
Le journal `quest-logcat.txt` confirme COMPOSITION READY, le suivi de la tête
et de la manette droite, le chargement MNI et les commandes A/B. La manette
gauche n'était pas encore suivie dans cet échantillon ; cela ne remplace pas
M2/M3. Après le retour d'essai partiel ci-dessous, HiBoP a été arrêté selon D23 ;
absence de PID vérifiée. ADB Wi-Fi reste disponible.

Commandes exécutées sur ce transport :

```powershell
adb -s 192.168.1.18:5555 shell sha256sum /sdcard/Android/data/fr.crnl.hibop.quest/files/quest-anatomy.hbna
adb -s 192.168.1.18:5555 install -r C:\HBP\Software\HiBoP\.artifacts\quest-008\Quest\HiBoP.Quest.apk
adb -s 192.168.1.18:5555 shell am start -n fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity
adb -s 192.168.1.18:5555 shell am start -W -n fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity
adb -s 192.168.1.18:5555 shell pidof fr.crnl.hibop.quest
```

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Placer une manette près du cerveau, appuyer et maintenir son grip latéral ; déplacer le bras puis tourner le poignet ; relâcher. Répéter de l'autre main. | Prise sans saut, translation et rotation suivies ; pose immobile après release. | VALIDE : confirmation complète après réinsertion de la pile, 2026-09-08. |
| M2 | Prendre le cerveau avec un grip, puis maintenir le grip de l'autre manette. Écarter/rapprocher les mains en restant au-delà de 8 cm ; relâcher une prise puis continuer avec l'autre. | Taille uniforme du cerveau et de ses repères ; bornes positives ; transitions sans saut ni perte injustifiée de prise. | VALIDE : confirmation complète du propriétaire, 2026-09-08. |
| M3 | Après déplacement, rotation et agrandissement, relâcher les grips puis presser X sur la manette gauche. | Le cerveau revient centré 65 cm devant la tête et 12 cm plus bas ; taille conservée, repères solidaires. | VALIDE : confirmation complète du propriétaire, 2026-09-08. |

Retour demandé : **M1/M2/M3 OK ou KO**, observations de saut, tremblement ou
perte de prise, et ajustement souhaité de taille/distance/zone de saisie.
Après validation, récupérer les logs, arrêter `fr.crnl.hibop.quest` via ADB et
vérifier l'absence de PID selon D23. Ne pas arrêter l'application pendant la
recette manuelle ; conserver désormais le mode éveillé conformément à D26.

### Retour du propriétaire et incident manette — 2026-09-08

Retour exact : « L'essai est concluant mais j'ai encore eu une manette
déconnectée (celle de gauche cette fois) donc je n'ai pas pu tester le
grossissement ». À la question de son fonctionnement dans le menu système,
réponse : **« Elle disparaît aussi du menu Quest »**.

Ce symptôme existe donc hors HiBoP. Il oriente le diagnostic vers la chaîne
casque/manette (alimentation, appairage, suivi ou logiciel système), sans
permettre d'affirmer une panne matérielle précise. Le log HiBoP ne distingue
pas à lui seul une perte de liaison d'une perte de pose complète : `not tracked`
combine `isTracked` et la disponibilité position+rotation. Aucun correctif
spéculatif aux gestes n'est introduit pour ce symptôme système.

Preuve locale : `.test-results/quest-008/quest-manual-partial-logcat.txt`.
Fin d'essai vers 12:26 +02:00 : `am force-stop fr.crnl.hibop.quest` exit 0,
`pidof` exit 1 sans PID ; broadcast
`com.oculus.vrpowermanager.automation_disable` terminé avec résultat 0.
Le capteur de proximité reprend son fonctionnement normal ; aucun autre
processus ni serveur ADB n'a été arrêté.
Preuve : `.test-results/quest-008/manual-partial-stop.txt`.

Piste de résolution proposée : piles neuves, puis si nécessaire désappairage et
réappairage de la manette depuis Meta Horizon, conformément aux
[consignes du support Meta](https://communityforums.atmeta.com/discussions/PairingConnection/3s-vr-right-controller-not-visible/1354415/).
Vérifier ensuite la stabilité dans l'accueil Quest et après plusieurs mises en
veille avant de refaire M2/M3. Si les pertes persistent dans le système avec
piles neuves et appairage refait, faire diagnostiquer le matériel par Meta.
Ces manipulations physiques et ce réappairage n'ont pas été exécutés par l'agent.

### Reprise du test d'échelle — 2026-09-08, 12:30 +02:00

Le propriétaire indique que retirer puis remettre la pile a rétabli la manette
et demande un nouvel essai de changement d'échelle. Ce rétablissement ne démontre
pas encore une résolution durable de l'incident.
Le même APK a été relancé via ADB Wi-Fi avec `am start -W` : Status ok,
lancement COLD en 178 ms, PID 10108. Aucune réinstallation ni modification
du code ; capteur de proximité en fonctionnement normal pour cet essai porté.
Preuve : `.test-results/quest-008/scale-retest-launch.txt`.
Le journal `scale-retest-logcat.txt` confirme COMPOSITION READY, tête suivie et
**les deux manettes TRACKED** pendant plusieurs échantillons de 12:30:52 à
12:31:07. Cela confirme le rétablissement du suivi, pas encore la validation UX.
À ce stade historique, M2/M3 attendaient le retour utilisateur. HiBoP était ouvert pendant ce
nouvel essai ; l'arrêt D23 sera effectué après son retour.

## Décisions, limites et suite

Aucune décision scientifique supplémentaire. Les réglages de confort restent
provisoires en attendant le retour. Aucun nouveau Player Windows n'est nécessaire
à la manipulation locale ; aucun changement Desktop ou code natif scientifique
n'entre dans ce diff. La preuve de déconnexion réseau du jalon reste à réaliser
après intégration du transport ; QUEST-008 n'émet déjà aucune commande réseau.

Suite proposée : **QUEST-009**, qualification du transport embarqué, sans
l'exécuter automatiquement. La validation manuelle de QUEST-008 reste séparée
de l'implémentation et des tests automatisés.
