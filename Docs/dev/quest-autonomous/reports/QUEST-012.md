# Rapport QUEST-012 — Démonstration anatomique et déconnexion

## Résultat

La recette J2 utilise les deux applications produit et la visualisation **MNI
Anatomy** ouverte depuis la fixture Desktop. Elle distingue premier transfert
du processus, renvoi à chaud, manipulation locale, coupure radio de 60 secondes
et renvoi après reconnexion. **La démonstration J2 est qualifiée**, avec une
coupure radio mesurée de **60,02 secondes**, gestes continus et vues indépendantes
confirmés par le propriétaire. Les six envois réels ont des hashes de surface
identiques ; chaque publication conserve exactement la pose et l'échelle.
Le premier envoi prend 0,975 s, les suivants 0,969–1,083 s. Le propriétaire
valide le confort et juge ces durées « largement ok » le 2026-09-08.

Les 39 tests PlayMode passent, dont un scénario MNI maintenant 60 secondes de
manipulation simulée après déconnexion logique. Il vérifie le PlayerLoop actif,
l'anatomie conservée, le retry sans doublon puis un remplacement avec libération
de l'ancien mesh et conservation exacte de pose/échelle. Ce test ne démontre
pas une coupure de radio sur le casque. Les tests EditMode passent également :
88 réussis, zéro échec, un ignoré réservé au profil Android.

L'instrumentation ajoutée mesure capture/encodage et connexion/envoi/reçu côté
Desktop ; décodage/hash et préparation/soumission du mesh côté Quest. Sur le
casque, elle relève toutes les cinq secondes les intervalles de frame, la
mémoire Unity/managed et la présentation. Elle observe les références existantes
sans créer de GameObject, charger de fixture, modifier les gestes ou forcer le GC.
Aucun défaut produit nécessitant une correction n'a été observé à ce stade.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : VALIDE.
- Branche `feature/xr-autonomous`, HEAD initial
  `4ba923ec03acdec3954bafe6439bbaad4609e332`, checkout initial propre.
- Unity `6000.5.2f1`, même code produit pour Windows IL2CPP et Quest ARM64 IL2CPP.
  Les changements locaux et les hashes des Players sont identifiés dans le
  [manifeste](../evidence/QUEST-012/manifest.json). Aucun commit/push ni changement
  des dépôts scientifiques voisins.
- Dépendances QUEST-006 à QUEST-011 conservées : capture réelle, HBNA v1,
  transport HBT v2/TLS, appairage, publication atomique et gestes locaux.
  La validation physique QUEST-011 reste historique ; elle ne valide pas
  implicitement la nouvelle recette.
- Builds séquentiels avec le lanceur produit existant et sa Library locale ;
  caches entièrement isolés par workspace/cible non qualifiés dans cette recette.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole réel | Règle à vérifier |
| --- | --- | --- |
| 1 | [Résultats physiques](#résultats-de-la-démonstration-physique) | Transfert produit, coupure radio réelle et retours propriétaire se complètent ; les limites de mesure restent explicites. |
| 2 | [QuestAnatomyMeasurements](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyMeasurements.cs), `Sample` / `Published` ; [QuestAnatomySession.PrepareAsync](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | Observation activée au démarrage seulement dans un build de développement avec le marqueur `quest012-measure`. Hash de surface déjà vérifié par le codec ; pas de nouvelle copie de buffers ni de lecture GPU. |
| 3 | [DesktopQuestPanel.SendAsync](../../../../Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs) | Les durées englobent les phases explicitement nommées ; le hash du reçu doit correspondre à la publication Quest. Aucun code/secret d'appairage journalisé. |
| 4 | [AnatomyDeliveryTests.RealMni_ManipulatesForSixtySecondsDisconnected_ThenRetriesAndReplacesWithoutChangingPose](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyDeliveryTests.cs) | Attente non bloquante, commandes au manipulateur réel, identité dédupliquée et remplacement sans changement de présentation. |
| 5 | [Start-QuestAnatomyOutage.ps1](../../../../Tools/Start-QuestAnatomyOutage.ps1), [Save-QuestAnatomyEvidence.ps1](../../../../Tools/Save-QuestAnatomyEvidence.ps1) | Restauration Wi-Fi exécutée sur l'appareil ; un dispatch n'est pas un succès. Collecte avec commandes, codes de sortie et hashes. Aucune coupure avant le signal du propriétaire. |

## Vérifications effectuées

Les gros logs et binaires restent dans les dossiers locaux ignorés
`C:\HBP\Software\HiBoP\.test-results\quest-012` et
`C:\HBP\Software\HiBoP\.artifacts\quest-012` ; le manifeste est versionné.

| ID | Scénario / commande | Résultat |
| --- | --- | --- |
| T1 | PlayMode `HBP.Quest.PlayModeTests`, `-questAnatomyFixture C:\HBP\Software\HiBoP\.artifacts\quest-008\fixture\quest-anatomy.hbna` | 39/39, exit 0, 71,44 s ; `playmode.xml` / `playmode.log`. |
| T2 | EditMode `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests`, même fixture, profil DesktopWindows | 88 réussis, 0 échec, 1 ignoré Android, exit 0, 26,23 s ; `editmode.xml` / `editmode.log`. Sommets/normales/UV comparés bit à bit, indices exacts, 69 104 sommets et 138 216 triangles. |
| T3 | `Tools/format-code.cmd` | Exit 0, quatre C# formatés, `format.log`. Premier essai sandbox bloqué par accès NuGet ; relance hors sandbox réussie. |
| T4 | Parser PowerShell des trois scripts ajoutés/modifiés ; `adb shell sh -n /data/local/tmp/quest012-outage-syntax.sh` | Aucune erreur de syntaxe, exit 0. Exécution radio réelle ensuite réussie en T7. |
| T5 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-012` puis `-Target Android` | Deux builds réussis, exit 0 : Windows 149,37 s / 42 warnings, Android 253,93 s / 44 warnings, aucune erreur. APK 162 987 971 octets, sept bibliothèques ARM64 vérifiées. Hashes dans le manifeste. |
| T6 | Inspection APK, `adb install -r`, résolution d'activité et `am start -W` | Exit 0, `Status: ok`, lancement d'activité COLD 246 ms. APK installé identique au build (`001375704baa0f992073c89ae78c35d95cff2dfe4b4cdc2e4df1a98d19a27235`). Sept échantillons `ready=false`, zéro vertex/upload malgré l'ancienne fixture persistante ; aucun `quest010-config.json`, passthrough `COMPOSITION READY`. |
| T7 | `Tools/Start-QuestAnatomyOutage.ps1 -Serial 192.168.1.18:5555`, reconnexion et renvois | REUSSI : radio désactivée 60,02 s, ADB observé offline puis device, même PID 2441 avant/après, hash anatomique conservé ; gestes continus et vue Desktop intacte confirmés par le propriétaire. |
| T8 | Six envois produit, captures `cold-empty`, `cold-ready`, `warm-ready`, `after-outage`, `after-resend` | REUSSI, hashes appariés Desktop/Quest et pose identique avant/après chaque publication. Temps, frames et mémoire ci-dessous ; traces partielles conservées sans prétendre à une couverture exhaustive. |
| T9 | Retour au profil DesktopWindows, EditMode `HBP.PlatformConfiguration.Tests` | 23 réussis, 0 échec, 1 ignoré Android ; exit 0, `final-desktop.xml`. |
| T10 | Démarrage Windows avec fixture, puis arrêt du processus de contrôle ; arrêt HiBoP Quest après collecte | Windows initialise Direct3D 11 / RTX 2070 SUPER, sans exception dans son log ; contrôle visuel non revendiqué. Processus caché arrêté après essai. Quest arrêté à 17:54:26 +02:00, `force-stop` exit 0, `pidof` vide / exit 1 ; ADB `device`, maintien éveillé `15`. |
| T11 | Fin de la recette physique, D23/D26 | Après validation finale, Quest arrêté à 18:20:48 +02:00 ; `force-stop` exit 0, `pidof` vide / exit 1, `dumpsys meminfo` indique absence de processus. ADB device et maintien éveillé 15 conservés ; Player Desktop laissé ouvert. |

La mesure du récepteur vide couvre sept fenêtres de cinq secondes, de
17:53:10 à 17:53:40 +02:00. Après la première fenêtre, intervalle Unity moyen
13,8889 ms ; le démarrage contient un maximum de 222,22 ms et des frames
`Stale`, conservés dans les données. Mémoire Unity allouée échantillonnée
103 159 805–105 351 557 octets, managed 6 156 288–7 008 256 octets ; Android
PSS 778 126 KiB, RSS 911 952 KiB. Batterie 27 %, non alimentée, température
batterie 46 °C. Ces nombres sont une **baseline sans cerveau ni gestes**.

Le log Quest conserve l'exception non fatale `AssetPackManager` déjà constatée
dans QUEST-003/007/011 ; l'initialisation atteint ensuite `COMPOSITION READY`.
Les cinq fichiers de réglages réécrits par Unity (BuildInfo, deux URP, OpenXR,
icônes Android) ont été inspectés et rétablis après sauvegarde de
`generated-settings.diff`. Ils ne font pas partie du changement livré.

## Validation manuelle réalisée et recette reproductible

Le 2026-09-08, le propriétaire demande : « Prépare tout ce qu'il faut et dis moi
quand tout est prêt, je te dirai quand je suis prêt ». Les essais manuels et
la coupure radio ont attendu son signal.

Le même jour, à 18:10 +02:00, la recette démarre après « Ok je suis prêt, dis
moi ce que je dois faire ». Après les instructions d'appairage, premier envoi
et essai des trois gestes, le propriétaire confirme **« C'est bon ! »**.
M1 (transfert et confort des trois gestes) est validé. Après « Ok prêt pour la
coupure », la radio est coupée puis rétablie automatiquement. Le propriétaire
confirme ensuite **« Tout OK. Oui pour la durée des envois c'est largement ok. »**
en réponse aux trois contrôles : gestes continus sans disparition/saut,
vue Windows indépendante et renvoi conservant contenu/pose/taille.
Ce dernier retour valide M2, M3 et l'acceptation des durées (D29).

Binaires et fixture exacts :

- Windows : `C:\HBP\Software\HiBoP\.artifacts\quest-012\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
- Android : `C:\HBP\Software\HiBoP\.artifacts\quest-012\Android\HiBoP.Quest.apk`.
- Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop` ; visualisation **MNI Anatomy**, MNI autorisé par D14.
- Casque vérifié : Quest 3, `192.168.1.18:5555` ; package `fr.crnl.hibop.quest`.
- ADB : `C:\Android\Sdk\platform-tools\adb.exe`. Toutes ses commandes sont exécutées hors sandbox.

Au signal du propriétaire, l'agent lance le Player Desktop avec :

```powershell
Start-Process -FilePath 'C:\HBP\Software\HiBoP\.artifacts\quest-012\Windows\HiBoP.6.1.0.win64\HiBoP.exe' -ArgumentList '-pf C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop -v "MNI Anatomy" -screen-fullscreen 0 -logFile C:\HBP\Software\HiBoP\.test-results\quest-012\desktop-player.log'
```

Le marqueur de mesure Quest doit être présent **avant** son lancement :

```powershell
& C:\Android\Sdk\platform-tools\adb.exe -s 192.168.1.18:5555 shell touch /sdcard/Android/data/fr.crnl.hibop.quest/files/quest012-measure
& C:\Android\Sdk\platform-tools\adb.exe -s 192.168.1.18:5555 shell am start -W -n fr.crnl.hibop.quest/com.unity3d.player.UnityPlayerActivity
```

La collecte et les vérifications de logs sont à la charge de l'agent :

1. Collecter `cold-empty` avant le premier envoi avec
   `Tools/Save-QuestAnatomyEvidence.ps1 -Serial 192.168.1.18:5555 -Stage cold-empty`.
2. Après M1, collecter `cold-ready`. Faire un second **Envoyer au Quest** dans les
   mêmes processus, puis collecter `warm-ready`. Le « froid » désigne le premier
   transfert de ces processus, pas un redémarrage OS ni une purge des caches.
3. Après le signal pour M2, exécuter
   `Tools/Start-QuestAnatomyOutage.ps1 -Serial 192.168.1.18:5555`. Le script refuse
   de démarrer sans anatomie prête instrumentée. Il coupe la radio, vérifie
   `Wifi is disabled`, attend 60 s et restaure la radio. La coupure ADB Wi-Fi
   est attendue ; le script Android détaché pilote la restauration.
4. Environ 75 s plus tard, vérifier `adb devices -l` ; si nécessaire une seule
   tentative `adb connect 192.168.1.18:5555`, puis demander le rétablissement au
   propriétaire si le casque reste inaccessible. Ne pas redémarrer ADB/casque.
   Tirer le `deviceLog` exact indiqué par `outage-*/dispatch.json` vers son
   dossier local. Vérifier `DISABLED`, delta d'uptime ≥ 60 avant `RESTORING`,
   `COMPLETE`, Wi-Fi reconnecté et même PID avant/après. Un fichier manquant,
   une erreur ou un simple dispatch laisse ce contrôle non vérifié.
5. Collecter `after-outage`, faire M3, collecter `after-resend`. Apparier chaque
   `QUEST012_SEND` avec `QUEST012_PUBLISH` par transfer/hash ; vérifier cardinalités,
   hash de surface stable entre renvois et pose/échelle `Before == After` à chaque
   publication. Un nouveau clic crée un nouvel ID et donc un hash global différent.
6. Après retour propriétaire et collecte finale, arrêter uniquement HiBoP Quest
   selon D23 et vérifier `pidof` vide. Conserver ADB et le maintien éveillé D26.

| ID | Action propriétaire exacte | Attendu / retour demandé | Statut |
| --- | --- | --- | --- |
| M1 | Menu Desktop **Quest**, adresse `192.168.1.18`, **Inspect Quest** ; comparer les huit groupes de l'empreinte avec le casque, cocher **All groups match…**, saisir le code puis **Pair**. **Envoyer au Quest**. Gâchette d'index près du cerveau : déplacer/tourner ; deux gâchettes et écarter/rapprocher : échelle. | Transfert et confort des trois gestes ; durées acceptées en fin de recette. | VALIDE, propriétaire le 2026-09-08 : « C'est bon ! », puis acceptation explicite des durées. |
| M2 | Continuer les trois gestes pendant la coupure radio annoncée par l'agent ; après reconnexion, relâcher les gâchettes et constater la pose et la taille. | Anatomie et gestes disponibles pendant au moins 60 s ; aucun saut ou perte de contenu au retour réseau. | VALIDE, propriétaire le 2026-09-08 : « Tout OK ». |
| M3 | Sans manipuler la vue Desktop, constater qu'elle est restée inchangée ; refaire **Envoyer au Quest**, puis relâcher les contrôleurs. | Vue Desktop indépendante, contenu Quest complet et pose/taille conservées lors du renvoi. | VALIDE, même retour propriétaire, complété par hashes et pose sérialisée aux publications. |

En cas de pause Android ou de pression sur Y, refaire **Inspect Quest**, comparer
la nouvelle empreinte puis appairer avec le code courant. Consigner cette pause :
elle ne démontre pas une manipulation continue durant la coupure.

## Résultats de la démonstration physique

La recette est exécutée dans les vrais Players livrés, PID Quest **2441**.
Le journal Desktop conserve six clics d'envoi réussis : un à froid, trois à
chaud avant coupure et deux après reconnexion. Chaque clic crée une capture
avec un ID distinct ; les six publications distinctes et le compteur d'uploads
1 à 6 sont attendus, sans doublon de livraison. Les tests T1 couvrent le retry
du même ID sans nouvel upload.

| Envoi | Capture + encodage (ms) | Connexion + envoi + reçu (ms) | Total (ms) | Décodage + hash Quest (ms) | Préparation + soumission mesh (ms) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Premier du processus | 101,36 | 873,69 | 975,04 | 299,46 | 17,24 |
| Chaud 1 | 110,84 | 950,90 | 1 061,74 | 297,94 | 12,94 |
| Chaud 2 | 99,65 | 983,60 | 1 083,26 | 294,67 | 15,81 |
| Chaud 3 | 102,13 | 967,09 | 1 069,22 | 295,00 | 13,46 |
| Après reconnexion 1 | 102,25 | 866,95 | 969,20 | 298,04 | 10,50 |
| Après reconnexion 2 | 99,63 | 900,40 | 1 000,04 | 294,66 | 13,89 |

Chaque envoi contient **3 870 258 octets**, **69 104 sommets**, **414 648 indices**
et un hash de surface
`2024e5e69d3f84bb5ce3cfe545b0b98548217f902a53a53020135fb7685c4114`, identique à
la fixture auditée. Les hashes complets Desktop/Quest concordent pour chaque ID.
La dernière livraison `9152f29ff41f4cafa9ac7e4c084b273d` a pour hash complet
`9ef391a1cc4b8e9bb35cd119aabe3b57193b5ede4f02c5eb9a8c83ed4a39470a`.
Les positions, rotations et échelles avant/après chaque publication sont
exactement égales dans les données JSON. La dernière échelle uniforme vaut
2,44132638. Les mesures Desktop et Quest emploient des chronomètres locaux :
leurs timestamps UTC ne servent pas à calculer une latence entre machines.

La preuve radio est
`.test-results/quest-012/outage-20260908-161532-542/outage.log` : Wi-Fi désactivé,
uptime **23596,61** à `DISABLED`, **23656,63** à `RESTORING`, puis Wi-Fi connecté,
PID 2441 inchangé et `COMPLETE`. ADB a été observé `offline` durant la coupure,
puis `device` après celle-ci. Cinq fenêtres de frames encore présentes pendant
la coupure (18:16:12–18:16:32 +02:00, 1 802 frames représentées) conservent
`ready=true`, hash et quatre uploads. **Le ring logcat a écrasé le début de
la coupure** avant la collecte : ces cinq fenêtres ne prouvent pas seules une
trace continue de 60 s. Le journal radio prouve la durée entière ; le retour
propriétaire valide les gestes continus et l'absence de saut à la reconnexion.

Sur les fenêtres anatomie prête conservées entre 18:11:16 et 18:20:17 +02:00 :
94 échantillons, 33 883 frames représentées, intervalle Unity moyen pondéré
**13,89094 ms**, maximum **27,77792 ms**. Les 471 lignes VrApi conservées après
la première publication indiquent **71–73 FPS pour une cible de 72**, somme
des compteurs `Stale` observés **5**, `App` **0,85–4,36 ms**, `CPU&GPU`
**2,91–6,66 ms**. Il s'agit des valeurs et fenêtres disponibles, pas d'un total
exhaustif des frames manquées ni d'un seuil formel de performance.

| Point de collecte | PSS Android (KiB) | RSS Android (KiB) | Batterie | Température batterie |
| --- | ---: | ---: | ---: | ---: |
| Vide avant envoi | 773 560 | 906 908 | 17 % | 47 °C |
| Premier envoi / gestes | 814 920 | 948 784 | 15 % | 48 °C |
| Renvois à chaud | 836 190 | 970 468 | 13 % | 48 °C |
| Après coupure | 837 311 | 970 304 | 11 % | 49 °C |
| Après derniers renvois | 827 920 | 960 948 | 9 % | 49 °C |

Pendant ces échantillons, mémoire Unity allouée **105 439 877–105 550 805 octets**,
managed **14 340 096–29 577 216 octets** ; buffers mesh estimés **3 869 920 octets**.
Aucun OOM/crash observé ; le heap managed varie, sans preuve de fuite ou de pic
exhaustif. Le casque n'était pas alimenté. Après arrêt, `dumpsys meminfo` confirme
`No process found`. ADB est conservé et le maintien éveillé sur alimentation vaut 15.

Les mesures structurées et les six couples envoi/publication sont conservés
dans `.test-results/quest-012/physical-summary.json`, généré par
`.test-results/quest-012/analyze-final.ps1`, avec hashes dans le manifeste.
Les erreurs initiales du script d'analyse (comparaison UTC/local, hypothèse de
deux uploads alors que quatre clics avaient été reçus) ont été corrigées ;
elles ne constituent pas un défaut produit ni une preuve de continuité complète.

## Mesures et limites

- `QUEST012_SEND` : capture + encodage ; connexion TLS + transfert + reçu après
  préparation ; total. Le débit bytes/durée de cette seconde phase est un débit
  de bout en bout, pas le débit radio brut.
- `QUEST012_PUBLISH` : décodage + hashes ; création du mesh + soumission upload.
  La première frame visible, la fin effective du travail GPU et la latence
  commande-photon ne sont pas mesurées par ces durées.
- `QUEST012_SAMPLE` : intervalles du PlayerLoop moyen/max sur une fenêtre de
  cinq secondes, mémoire Unity allouée/réservée, heap managed, collections gen0,
  buffers estimés et pose. Ce ne sont pas des durées CPU/GPU séparées ni un
  compteur d'allocations par geste. Les pauses et le logging influencent ces valeurs.
- Les logs `VrApi` complètent la cadence et les indicateurs `Stale`, `App`,
  `CPU&GPU`, température. Conserver leurs libellés ; ne pas déduire une mesure
  GPU isolée ou commande-photon. `dumpsys meminfo` fournit PSS/RSS aux points
  collectés ; un maximum échantillonné n'est pas un pic de staging exhaustif.
- Les mesures mémoire après arrêt attestent la fin du processus, pas une preuve
  de libération détaillée de chaque allocation. T1 couvre la libération du mesh
  remplacé ; aucune conclusion d'endurance/fuite générale n'est tirée.
- 72 Hz / 13,89 ms reste une cible provisoire ; aucune tolérance scientifique ou
  durée de transfert limite n'est inventée. L'acceptation du confort et des
  durées est obtenue avec les mesures réelles (D29). La session dure environ dix
  minutes, avec échanges et gestes intermittents ; dix minutes d'exploration
  continue ou trente minutes d'endurance ne sont pas déclarées exécutées.

## Décisions, limites et suite

D12 fixe la coupure d'au moins 60 secondes. D23/D26 règlent l'arrêt de HiBoP et
la disponibilité ADB ; D24 conserve les gâchettes d'index. D29 accepte le confort
et les durées observées pour cette fixture, sans formaliser de seuil général.
Les vérifications physiques et manuelles nécessaires à J2 sont acquises ; les
limites de métriques ci-dessus restent explicites. Aucune validation manuelle
ni décision supplémentaire n'est attendue pour clôturer QUEST-012.

Prochaine tâche proposée : QUEST-013, sans exécution automatique.
