# Rapport QUEST-023 — Projection locale d’un instant iEEG

## Résultat

Quest calcule désormais l’instant iEEG reçu et affiche sa projection. La réception
prépare ses propres entrées natives, attend le résultat complet puis publie
surface, contacts et provenance ensemble. Pendant le calcul ou en cas d’échec,
la vue précédente reste affichée. L’accusé `Published` suit cette publication.
Le panneau indique `Calculating local iEEG...`, puis `iEEG ready`, ou l’erreur.
Le clic sur le stick droit recalcule la projection depuis les entrées conservées,
sans connexion Desktop.

Trajet scientifique : `IEEGInstant.SurfaceValues` → `IEEGGenerator.ComputeCalibratedActivity`
avec **un seul échantillon** et les `SpanMin/Middle/SpanMax` de la préparation
complète → `SurfaceGenerator.ComputeActivityUV(0, ActivityAlpha)` → `mesh.uv3`
(activité normalisée et catégorie), `mesh.uv2` (opacité et catégorie) → shader
Quest existant. La grille, les générateurs et la normalisation restent dans les
wrappers communs et `hbp_core`. Aucun port EEGFormat ni règle scientifique Quest.

Les contacts conservent le chemin commun QUEST-022 : `SiteValues` déjà échantillonnées
→ `SiteAppearance` sur Desktop → palette existante, gain, visibilité → contacts
HBNA → buffer Quest. Les réglages Desktop nécessaires à recalculer cette apparence
ne sont pas tous dans HBNA ; cette tâche transporte donc les résultats de la règle
commune et ne les reconstruit pas à partir d’hypothèses Quest. Recalculer le même
instant ne modifie pas ces contacts. Le masque natif reste distinct de la visibilité
(notamment pour la blacklist). `IEEGInstant.Alpha` décrit l’échantillonnage temporel
des contacts ; il ne remplace jamais l’opacité `ActivityAlpha` de la surface.

L’annulation après dispatch conserve désormais l’observation de la tâche de
publication : elle ne libère pas prématurément le créneau de réception. Les
générations de préparation et de contenu empêchent les publications périmées.
Le recalcul iEEG charge un mesh candidat avant échange ; une erreur d’upload
ne laisse pas la moitié des UV sur le mesh affiché. La libération des entrées
natives retirées attend toujours la fin de leur worker.

## État et provenance

- Branche `feature/xr-autonomous`, HEAD initial `7e2b13d9f00d241bd67493f058c0624bff07258d`, arbre initial propre.
- Changements locaux non commités, sans reprise historique ni modification des dépôts natifs.
- Unity `6000.5.2f1`, profils `DesktopWindows.asset` et `Quest.asset`.
- `hbp_core` `ffb7686011f37a21e6c5ab4f88ad6cfcbc53ffc1`, `hbp_math` `646e96e7ff72a4e0466eac6a2d3c101d1fb736d7` ; hashes des bibliothèques dans `Tools/NativePlugins.lock.json`.
- Dépendances : QUEST-020 (HBNA v4), QUEST-021 (calcul commun), QUEST-022 (apparence commune), renderer densité QUEST-019.
- [Manifeste des preuves](../evidence/QUEST-023/manifest.json).
- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** avec acceptation numérique **D31** ; manuel : **VALIDE** (M1/M2, retour propriétaire du 2026-09-10).

## Ce que je conseille de reviewer

| Priorité | Point d’entrée | Règle à vérifier |
| --- | --- | --- |
| 1 | [NativeProjectionInputs.ComputeIEEGAsync](../../../../Assets/Scripts/HBP/Transfer/Projection/NativeProjectionInputs.cs) | Instant sample-and-hold, calibration reçue, mêmes wrappers Desktop, réservation native avant worker et release différé. |
| 2 | [QuestAnatomyView.ApplySnapshotAsync / PrepareIEEGAsync / RecalculateIEEGAsync](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) | Aucun remplacement visible avant résultat complet ; erreur, annulation, remplacement et fermeture conservent les invariants de propriété. |
| 3 | [QuestAnatomySession.PublishAsync / OnUnityThreadAsync](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | ACK après commit, échec réessayable, aucune publication détachée lors d’une annulation après dispatch. |
| 4 | [IEEGCalculationTests](../../../../Assets/Tests/EditMode/HBP.Quest.Anatomy.Tests/IEEGCalculationTests.cs) et [AnatomyDeliveryTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyDeliveryTests.cs) | Référence Desktop indépendante, échec natif, staging invalide, Clear/remplacement, UV hors réseau et réception TLS. |
| 5 | [IEEGProjectionDiagnostic](../../../../Assets/Scripts/HBP/Dev/IEEGProjectionDiagnostic.cs), [IEEGBenchmark](../../../../Assets/Scripts/HBP/Transfer/Projection/IEEGBenchmark.cs), [QuestIEEGDiagnostic](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestIEEGDiagnostic.cs) | Comparaison à la colonne Desktop réelle, exports complets et calcul après coupure radio ; les diagnostics restent opt-in. |

## Vérifications effectuées

| ID | Vérification | Résultat / preuve |
| --- | --- | --- |
| T1 | CLI EditMode, trois assemblies anatomie/projection/transfert, [arguments](../evidence/QUEST-023/editmode-command.json) | **168/168 réussis**, code 0, [XML](../evidence/QUEST-023/editmode.xml). |
| T2 | CLI PlayMode Desktop/Quest avec HBNA iEEG du Player QUEST-020, [arguments](../evidence/QUEST-023/playmode-command.json) | **87 réussis, 0 échec, 5 ignorés**, code 0, [XML](../evidence/QUEST-023/playmode.xml). |
| T3 | Après correction de dispatch : réception PlayMode, [arguments](../evidence/QUEST-023/playmode-final-command.json) | **14 réussis, 0 échec, 4 ignorés**, code 0, [XML](../evidence/QUEST-023/playmode-final.xml). Un nouveau test, soit **88 tests PlayMode distincts réussis**. |
| T4 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-023` ; `Tools/Run-QuestIEEGProjection.ps1 -EvidenceId quest-023` | Build et Player **code 0** ; **18 comparaisons exactes** entre instant reçu et série complète Desktop, [résultat](../evidence/QUEST-023/player-result.json), [arguments](../evidence/QUEST-023/player-command.json). |
| T5 | Captures Desktop avant/après | Les cinq PNG sont identiques byte pour byte à QUEST-022 : [hashes](../evidence/QUEST-023/reference-images.json). |
| T6 | Comparateur : identité, NaN, catégorie altérée, variation d’un ULP | Contrôles réussis, aucune tolérance appliquée : [preuve](../evidence/QUEST-023/comparator-check.json). |
| T7 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId quest-023`, puis `Tools/Run-QuestIEEGBenchmark.ps1 -Serial 2G0YC5ZHB20370 -Fixture .test-results/quest-023/player-20260910-071223/fixtures -SkipInstall` | Build et banc **code 0**, 36 calculs Quest. APK installé **163 079 207 octets**, hash vérifié, huit bibliothèques ARM64 ; [contrôle APK](../evidence/QUEST-023/benchmark-apk-content.json), [hash natif épinglé](../evidence/QUEST-023/native-check.json). |
| T8 | `python Tools/Compare-QuestIEEG.py .test-results/quest-023/player-20260910-071223/Windows .test-results/quest-023/benchmark-20260910-072512/Android Docs/dev/quest-autonomous/evidence/QUEST-023/parity.json` | Code **0**, 36 comparaisons complètes ; [mesures brutes](../evidence/QUEST-023/parity.json) et [acceptation D31](../evidence/QUEST-023/numerical-acceptance.json). |
| T9 | `Tools/Run-QuestIEEGDelivery.ps1 -Serial 2G0YC5ZHB20370 -HostAddress 192.168.1.14` | Code **0**, capture réelle Windows, TLS, publication iEEG et contacts GPU exacts ; **60,02 s** de coupure Wi-Fi, trois recalculs exacts, même PID **1919**, Wi-Fi rétabli. [Résultat](../evidence/QUEST-023/delivery/device-result.json), [radio](../evidence/QUEST-023/delivery/radio.txt), [mesures](../evidence/QUEST-023/delivery/measurements.json). |
| T10 | `Tools/format-code.cmd`, contrôle des dépendances et `git diff --check` | Formatage réussi après restauration NuGet hors sandbox ; [frontière commune](../evidence/QUEST-023/seam-check.json). Les réglages générés par Unity sont sauvegardés puis restaurés. |

Les tests couvrent valeurs négatives/nulles/positives, canal absent masqué,
instant interpolé distinct pour les sites, échec du natif, libération, remplacement,
annulation et échec de préparation du rendu. Les tests PlayMode ignorés demandent
les fixtures des anciens scénarios anatomiques/contacts et ne sont pas des
validations iEEG déléguées au propriétaire.

La première tentative EditMode a signalé une référence d’assembly manquante du
diagnostic ; `HBP.Dev.Runtime` référence maintenant `HBP.Transfer.Projection`.
Les résultats ci-dessus sont ceux des exécutions corrigées. Les commandes Unity
utilisent `Start-Process -Wait -PassThru -WindowStyle Hidden` hors sandbox.

## Parité numérique

Les 18 fixtures proviennent de la colonne réelle **Synthetic uV**, avec six index
**0, 10, 50, 75, 130, 150** et trois configurations (initiale, rayon 25 mm/plage
−8/−1/3, restauration). Deux passages par fixture et plateforme. Les tableaux
binaires contiennent **4N + 3G float32 little-endian** : UV d’activité, UV d’opacité
et points de grille complets. Les JSON et SHA-256 identifient les amplitudes,
provenances, calibrations dans HBNA, contacts et masques sans quantification.

MNI : **69 104 sommets**, **353 600 points** de grille (65 × 80 × 68), huit contacts.
À 0 ms, amplitudes reçues `[-1, 0, 1, 0, -1, 0, 1, 0]`, calibration **−10/0/10**,
masques `[0, 0, 0, 1, 0, 1, 0, 1]`. Six contacts sont visibles ; le contact blacklisté
reste visible mais exclu du calcul. Le diagnostic physique relit leurs rayons et
RGBA dans le buffer GPU et les compare exactement à la capture commune.

| Grandeur | Plus grand écart absolu | RMS du cas correspondant | Critère D31 |
| --- | ---: | ---: | --- |
| Grilles, entrées, masques, couvertures, catégories, sentinelles, apparence préparée | 0 | 0 | Égalité stricte |
| UV activité normalisée | 4,172325134277344 × 10⁻⁷ | 1,3673855223051369 × 10⁻⁸ | ≤ 5 × 10⁻⁷ |
| UV opacité | 1,7881393432617188 × 10⁻⁷ | 7,256360437146824 × 10⁻⁹ | ≤ 2 × 10⁻⁷ |

Maximum activité : `config0-index10-0`, sommet **12937**, x, Windows
**0,9999861717224121**, Quest **0,9999857544898987** (sept ULP). Maximum opacité :
`config1-index0-0`, sommet **7402**, x, Windows **0,792373776435852**, Quest
**0,7923735976219177** (trois ULP). Les deux passages sont identiques sur chaque
plateforme ; les paramètres restaurés retrouvent exactement les buffers initiaux.
Le RMS inclut les deux composantes et les sentinelles, sans sélection de sommets.

Les accumulations pondérées, divisions et interpolation trilineaire s’effectuent
en float32 dans `ieeg_generator.cpp`, `volume_grid_sampling.h` et
`surface_generator.cpp` du **commit natif épinglé**. Ces fichiers ont été comparés
à ce commit, le checkout natif local ayant un HEAD plus récent. Les écarts sont
compatibles avec des arrondis d’architecture/compilation, mais aucune instruction
FMA particulière n’a été démontrée responsable. **D31** accepte ces mesures et
limites pour ce banc ; le comparateur brut n’a pas été élargi pour rendre un PASS.

## Temps, copies et mémoire

| 0 ms, millisecondes | Windows passage 1 | Windows passage 2 | Quest passage 1 | Quest passage 2 |
| --- | ---: | ---: | ---: | ---: |
| Préparation grille/générateurs | 5,264 | 5,530 | 11,088 | 9,686 |
| Calcul iEEG/calibration | 5,982 | 6,018 | 11,402 | 11,197 |
| Projection/copies UV | 7,253 | 7,075 | 19,203 | 18,194 |

Ces passages reconstruisent les générateurs ; le second est à processus chaud,
sans cache de grille. Ils excluent le chargement initial du NIfTI et l’export
diagnostique. Lors de la réception réelle : décodage/hash **818,05 ms**, préparation
jusqu’à publication (calcul et upload compris) **754,98 ms**. Le champ historique
`meshPrepareAndUploadSubmissionMs` couvre donc ici cette préparation asynchrone
complète. Le transfert satisfait le délai courant ; aucun seuil universel déduit.
Les trois calculs hors réseau prennent **12,741 / 14,771 / 9,283 ms**, et les
soumissions des UV **3,343 / 2,261 / 1,708 ms**. Ni fin GPU ni bouton-à-photon mesurés.

Par instant : **32 octets** pour huit amplitudes managées copiées vers le calcul,
puis **1 105 664 octets** d’UV natif → managé, et autant pour la conversion des
structs dans les wrappers. S’ajoutent le marshaling, mesh candidat, staging HBNA,
NIfTI, copies internes Unity/GPU ; leur nombre exact n’est pas instrumenté.
Le banc exporte aussi la géométrie complète de grille, hors timings ci-dessus.

Mémoire Unity allouée : **102 454 052 → 111 730 772 → 118 915 668 octets** avant
réception, après publication et après trois cycles. **118 915 668 octets** est le
maximum des observations de ce parcours, pas un pic natif exact. Le champ
`peakAllocatedBytes` du diagnostic (**110 175 332**) échantillonne seulement les
frames avant le retour de réception et manque sa dernière frame : il ne doit
pas être présenté comme pic global. Les snapshots complètent cette limite.
Ensuite, sur les fenêtres ~15–80 s, Unity reste entre **113 894 964 et
113 963 036 octets**, avec collections managées observées. PSS Android
**922 250 → 944 978 KiB**, RSS **1 051 452 → 1 075 320 KiB**. Aucun OOM/crash,
pas de croissance Unity monotone au repos sur cette fenêtre ; aucune conclusion
de fuite/endurance générale à partir de ces deux instantanés PSS.

Au repos, intervalles moyens proches de **13,889 ms** ; quelques fenêtres à
**14,045 ms**, avec maxima de **41,667 ms** et **27,778 ms**. Le chargement initial
présente une frame à **694,45 ms** : le NIfTI/staging restent sur le chemin Unity.
La compilation/export du banc n’est pas un test de fluidité. Statut thermique
Android **0**. Le confort de manipulation reste une validation propriétaire.

## Validation manuelle du propriétaire

APK installé : `C:/HBP/Software/HiBoP/.artifacts/quest-023/Android/HiBoP.Quest.apk`,
SHA-256 `87a4941a6b22da3eb7a1978019a4ab2b429a75490e39670a41f5db241945eb96`.
La vue **0 ms** issue de la réception réelle a été laissée ouverte pour les essais, puis HiBoP a été arrêté après validation M1/M2.
Le Player automatique Windows est arrêté. L’image ADB locale
`.test-results/quest-023/delivery-20260910-072831/quest-screen.png` montre `iEEG ready`
et le cerveau ; le panneau masque une partie de la surface et ne valide pas
à lui seul les gestes ou la zone colorée. Elle reste locale (passthrough/bureau).

Pour choisir et envoyer soi-même l’instant, lancer :

```powershell
Set-Location 'C:/HBP/Software/HiBoP'
.\Tools\Run-QuestIEEGProjection.ps1 -EvidenceId quest-023 -KeepOpen
```

Ce lanceur utilise le Player
`C:/HBP/Software/HiBoP/.artifacts/quest-023/Windows/HiBoP.6.1.0.win64/HiBoP.exe`,
la fixture `C:/HBP/Software/HiBoP/.artifacts/quest-020/fixture/quest-mni-ieeg.hibop`
et son protocole `quest-020.prov`. Dans **MNI iEEG**, colonne **Synthetic uV**,
onglet **Activity**, utiliser **Compute IEEG** si nécessaire ; dans **Timeline**,
choisir **0 ms** (index 50). Fenêtre **Quest**, adresse **192.168.1.18**,
**Inspect Quest**, comparer l’empreinte actuellement affichée au casque, cocher
**All groups match the fingerprint displayed in my Quest**, saisir son code,
**Pair**, puis **Envoyer au Quest** sur la colonne. **Y** renouvelle le code
si expiré ; ne pas reprendre les anciennes valeurs des captures.

| ID | Action / résultat attendu | Statut |
| --- | --- | --- |
| M1 | Vérifier `iEEG ready` et `0 ms` ; **B** masque le panneau. Tourner le cerveau avec une gâchette d’index, agrandir avec les deux, **A** masque/réaffiche la surface et permet de voir les contacts, **X** recentre. La projection reste attachée au cerveau, les contacts gardent leur taille/couleur relatives. Rejouer l’envoi Desktop ci-dessus pour valider le choix manuel d’instant. Retour demandé : **M1 OK/KO** avec observation. | **VALIDE — propriétaire, 2026-09-10** |
| M2 | Après M1, indiquer être prêt pour la coupure ci-dessous. Pendant les 60 s hors réseau, cliquer le **stick droit**, attendre `iEEG ready`, manipuler et masquer/réafficher avec **A**. Résultat et contacts restent disponibles ; retour **M2 OK/KO**. Trois recalculs automatiques ont déjà passé ce scénario ; l’observation manuelle reste distincte. | **VALIDE — propriétaire, 2026-09-10** |

```powershell
.\Tools\Start-QuestAnatomyOutage.ps1 -Serial '2G0YC5ZHB20370'
```

Cette commande cible le Quest USB et programme la restauration du Wi-Fi.
L’agent peut l’exécuter au retour « prêt pour M2 », puis récupérer le log indiqué
par le lanceur historique (`.test-results/quest-012/outage-*`). Après validation
manuelle, récupérer les preuves, arrêter **uniquement** `fr.crnl.hibop.quest`
et vérifier l’absence de PID selon D23. Aucune validation manuelle présumée.

Le 2026-09-10 à 07:44:14 UTC, après reconnexion USB par le propriétaire,
la coupure M2 a été exécutée : **60,02 s** sans Wi-Fi, puis restauration et
reconnexion confirmées, même PID **1919** avant/après, journal `COMPLETE`.
La [preuve M2 et validation propriétaire](../evidence/QUEST-023/manual-m2.json)
conserve les mesures radio, les retours et la vérification d'arrêt.

Le propriétaire a indiqué **« M1 et M2 semblent ok »**, avec une réserve sur
l'absence de `iEEG ready`. Après invitation à réafficher le panneau complet
avec **B**, il a confirmé **« Oui je vois exactement ça »**, en référence à
`iEEG ready | Available offline`. **M1 et M2 sont validés le 2026-09-10.**
Le libellé final persiste dans le panneau détaillé ; seul l'état
`Calculating local iEEG...` peut passer entre deux rafraîchissements (200 ms).
Aucune correction de code nécessaire pour cette observation.

Après ce retour, arrêt ciblé de **fr.crnl.hibop.quest** selon **D23** :
`am force-stop` code **0**, puis `pidof` vide, code **1**. Le Quest USB reste
présent dans `adb devices -l` ; ADB est conservé. Aucun essai manuel restant.

## Décisions et limites

- Aucun changement de protocole, de prefab, de palette, de timeline ou de synchronisation live.
- Shader scientifique Quest existant, palette MatLab. Les buffers sont comparés avant shaders ; aucune identité de pixels passthrough/Desktop n’est revendiquée. Le shader des contacts reste opaque, même si le buffer conserve RGBA.
- Les échéances transport existantes (20 s pour le probe TLS, 30 s pour l’appairage) incluent maintenant le calcul initial et l’ACK. Leur dépassement annule le candidat et conserve l’ancien contenu ; elles ne constituent pas une promesse de durée sur d’autres données.
- Le pic Unity échantillonné par frame, les instantanés PSS/RSS et les compteurs de copies ont des portées distinctes ; aucune extrapolation à une timeline complète, aucun pic natif exact ni mesure GPU/bouton-à-photon annoncé.
- La revue automatique a refusé `Connect-QuestAdbWifi.ps1 -KeepAwakeWhilePluggedIn` (activation ADB TCP et réglages persistants jugés insuffisamment autorisés). Aucun contournement ni réglage appliqué : l’USB connecté par le propriétaire suffit. Le casque était déjà éveillé/alimenté au relevé.
- Le premier lancement du banc a rencontré l’écran système « contrôleurs requis ». Après confirmation du propriétaire **« C’est fait »**, relance sans réinstallation (`-SkipInstall`), puis réussite et arrêt vérifié du processus de banc. Le test de livraison a d’abord été refusé par auto-review pour sensibilité supposée du payload ; la [vérification des formules synthétiques et patients factices](../evidence/QUEST-023/fixture-provenance.json) a permis l’autorisation du même test, sans contournement.
- Message Android préexistant `ClassNotFoundException: com.google.android.play.core.assetpacks.AssetPackManager`, et avertissement Windows Graphics Ring Buffer au chargement ; les diagnostics ont terminé avec succès. Aucun correctif hors périmètre.
- Les réglages/assets générés par Unity ont été sauvegardés dans `.test-results/quest-023/unity-generated.patch` puis restaurés à leur état initial. Aucun commit, push ni publication de distribution.
- Suite prévue : chantier SCENE-001 intercalé avant QUEST-024 selon le catalogue, sans l’engager.
