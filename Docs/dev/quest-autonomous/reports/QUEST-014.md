# Rapport QUEST-014 — Afficher les contacts et masquer le cerveau

## Résultat

Les contacts préparés par QUEST-013 sont rendus sur Quest sous le même repère
millimétrique que la surface. **A, sur la manette droite**, masque/réaffiche le
cerveau ; le panneau indique `A: hide brain` ou `A: show brain`, même lorsque
ses détails sont cachés avec B. Les gâchettes d'index continuent de déplacer,
tourner et agrandir le groupe, y compris après une nouvelle prise sans surface.

Seul `MeshRenderer.enabled` change : le mesh, les contacts, les masques de calcul
et la prise restent présents. L'affichage est `snapshot.Visible && !SurfaceHidden` :
la commande locale respecte la visibilité préparée sur Desktop. Le choix local
est conservé lors d'un remplacement ; fermer la session le remet à zéro.

Un seul buffer GPU de **32 octets par contact** et un appel instancié dessinent
les sites. Aucun GameObject par contact ni règle scientifique de couleur n'est
créé. Le rayon est le diamètre préparé divisé par deux ; une visibilité préparée
fausse produit un rayon de rendu nul, sans retirer le contact des données.
Les couleurs linéaires sont copiées telles quelles ; l'éclairage de présentation
et la profondeur sphérique reprennent le principe XR historique. Le rendu reste opaque.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : VALIDE.
- Branche `feature/xr-autonomous`, HEAD initial
  `629996e4d7ed82c5a44ab7aa5903f1acd9bcbd2d`, checkout initial propre.
- Unity `6000.5.2f1`, éditeur fermé : compilation, assets, tests et builds via CLI
  hors sandbox. URP `17.5.0`, OpenXR `1.18.0`, Meta OpenXR `2.4.1`, Input System `1.20.0`.
- QUEST-013 fournit le contrat et la capture Player réelle MNI de huit contacts ;
  QUEST-008 fournit les gestes existants. Aucun changement des dépôts natifs.
- Reprise ciblée de
  `eb26c323e:XR/Assets/HiBoPXR/Sites/Runtime/P10SiteRenderer.cs`,
  `Shaders/P10BufferedSites.shader` et `Editor/P10ProjectSetup.cs` sous ce même
  dossier historique. Adaptations : contrat HBNA, un buffer position/rayon/couleur,
  conversion mm→m portée uniquement par le prefab, suppression du picking et des
  effets de sélection/masque, variante procédurale explicite pour l'instanciation.
  Nouveaux assets avec nouveaux GUID ; aucun merge de l'ancien XR.
- Changements locaux non committés ; aucun push. [Manifeste](../evidence/QUEST-014/manifest.json).

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole / objet | Règle à vérifier |
| --- | --- | --- |
| 1 | [QuestAnatomyView](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs), `ApplySnapshot`, `ToggleSurface`, `Clear` | Préparer surface et sites avant publication ; un échec garde l'ancien contenu. Le masquage ne libère aucun buffer et ne modifie pas la science. |
| 2 | [QuestContactRenderer](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestContactRenderer.cs), `Prepare`, `Commit`, `WorldBounds` ; [shader](../../../../Assets/Shaders/Quest/BufferedContacts.shader) | Ordre, positions et RGB linéaires conservés, diamètre/2 ; instanciation et stéréo Unity, profondeur et limites spatiales mises à l'échelle ensemble. `DefaultExecutionOrder(100)` soumet la pose après la manipulation pour éviter une image de retard. |
| 3 | [QuestAnatomy.prefab](../../../../Assets/Prefabs/Quest/QuestAnatomy.prefab) ; [QuestAnatomySetup](../../../../Assets/Scripts/HBP/Quest/Editor/QuestAnatomySetup.cs), `ApplyContacts` | `QuestContactRenderer` sur `Anatomical Frame (mm to m)`, échelle locale `(0.001,0.001,0.001)`, le même Transform que les composants de surface ; références `siteMesh=ContactQuad.asset`, `siteMaterial=ContactsOpaque.mat`, instanciation activée. |
| 4 | [QuestAnatomyInput](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyInput.cs) ; [QuestConnectionPanel](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestConnectionPanel.cs) ; [QuestBootstrap.prefab](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab) | A = `<XRController>{RightHand}/primaryButton`, déclenché avec tracking/focus valides. X/B/Y et gâchettes restent distincts ; la référence `view` du panneau est sérialisée. |
| 5 | [QuestContactRenderingTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestContactRenderingTests.cs) ; [diagnostic physique](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestContactDiagnostic.cs) | Lire les pixels et les buffers, vérifier remplacement/clear/destruction ; distinguer l'injection locale du diagnostic d'un transfert réseau et des gestes manuels. |

## Vérifications effectuées

Les gros artefacts sont locaux et ignorés, sous
`C:\HBP\Software\HiBoP\.test-results\quest-014` et `.artifacts\quest-014`.
Le manifeste enregistre leurs chemins, hashes, commandes et résultats.

| ID | Vérification | Résultat / preuve |
| --- | --- | --- |
| T1 | `HBP.Quest.Editor.QuestAnatomySetup.ApplyContacts` | Exit 0 ; `setup.log`. Assets ajoutés dans les prefabs existants avec références sérialisées. |
| T2 | EditMode : `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests`, capture MNI v1 passée par `-questAnatomyFixture` | **112 réussis, 0 échec**, exit 0 ; `editmode.xml/log`. Un test ignoré : `QuestBuildGuard_RejectsWrongArchitecture`, réservé au profil Android. |
| T3 | PlayMode : `HBP.Quest.PlayModeTests`, capture réelle Player v2 passée par `-questContactsFixture` | **44/44**, exit 0 ; `playmode.xml/log`. TLS loopback, publication des huit contacts, buffers GPU, coordonnées/couleurs, parentage, masques, gestes, déconnexion, remplacement et libération. |
| T4 | Pixels avec GPU : surface opaque, contacts masqués scientifiquement mais visibles, contact préparé invisible, puis échelle ×2 | Réussi ; `render/*.png`. Aire du contact rouge **208 → 812 pixels**, ratio **3,903846**. Aucun upload supplémentaire lors du masquage ou de l'échelle. |
| T5 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows/Android -EvidenceId quest-014` | Deux builds **Succeeded**, exit 0 : Windows 25,45 s / 1 warning ; Android 100,71 s / 8 warnings ; aucune erreur. `windows-build.log`, `android-build.log`, rapports de build. |
| T6 | Diagnostic physique : fixture MNI, cycles de chargement/libération, frame et mémoire, captures | **Réussi** : `device-20260909-083327`, `QUEST014_PROBE_PASS`, trois captures récupérées et inspectées ; huit contacts après masquage et échelle ×2. Trois cycles chargement/remplacement/libération, puis libération finale ; `physical-summary.json`, `physical-images.json`. Arrêt automatique confirmé par `pidof` sans PID. |
| T7 | `QuestBuildGuard_RejectsWrongArchitecture`, profil Android | **1/1**, exit 0 ; `android-guard.xml/log`. Le test réservé à Android, ignoré dans T2, a donc bien été exécuté. Total distinct : **157 tests réussis**. |
| T8 | `Tools/Test-QuestApk.ps1` | Réussi : APK de **162 999 056 octets**, sept bibliothèques ELF64 AArch64, aucun runtime scientifique Desktop ; `apk-inspection.json`. |
| T9 | Capture Player Windows + `python .test-results/quest-014/verify-capture.py` | Réussi : 69 104 sommets, 138 216 triangles, huit contacts, **3 870 953 octets HBNA**. Coordonnées/associations exactes et diamètre 2 mm ; hashes, répétition et buffers source vérifiés. `capture-verification.json`, capture après correction du diagnostic et avant correction de l'ordre de rendu Quest ; export Desktop inchangé depuis. |
| T10 | Appairage et envoi produit des Players corrigés, puis gestes du propriétaire | Réussi : `manual-lag-fixed/measurements.log`, publication de 69 104 sommets et huit contacts ; `owner-feedback.json` confirme « Tout est bon ! » après translations/rotations brusques, surface visible puis masquée. Arrêt final confirmé dans `stopped.json`. |

Le premier test de pixels a détecté la variante d'instanciation manquante et
une face de fixture tournée à l'opposé de la caméra. Ces deux points sont
corrigés ; le run final et les captures ci-dessus vérifient le résultat.

`Tools/format-code.cmd` et `git diff --check` réussis. Profil revenu à DesktopWindows ; cinq fichiers générés par les builds inspectés puis rétablis, diffs conservés dans `generated-settings.diff` et `generated-settings-after-lag.diff`.

## Correction du retard lors des mouvements brusques

Après avoir confirmé M1 et M2, le propriétaire a signalé que les contacts
retardaient sur la surface pendant les mouvements brusques. Le groupe était
modifié par `QuestAnatomyInput.LateUpdate`, tandis que `QuestContactRenderer`
copiait sa matrice dans un autre `LateUpdate` sans ordre relatif garanti.
La surface pouvait donc utiliser la pose courante et les contacts une pose précédente.

`QuestContactRenderer` porte maintenant `[DefaultExecutionOrder(100)]`, après
l’entrée/manipulation d’ordre par défaut. La matrice des contacts et leurs bounds
sont ainsi soumis une fois la pose du groupe finalisée. Aucun lissage, nouvelle
copie du buffer de contacts ou changement des coordonnées scientifiques.

Le test GPU existant effectue maintenant six images de translations et rotations
alternées dans un composant de test LateUpdate, puis compare les pixels rouges/verts
aux positions de la même image. Avant correction : échec (pixel attendu rouge,
pixel noir), `lag-before.xml/log` ; après correction : suite PlayMode **44/44**,
`playmode.xml/log`, captures `render/late-pose-*.png`. La requalification automatique du build corrigé réussit dans
`device-20260909-083327`. Le propriétaire confirme le 2026-09-09 : **« Tout est bon ! »**,
après les mouvements brusques, surface visible et masquée (`manual-lag-fixed/owner-feedback.json`). Les preuves du build précédent sont
conservées sous `before-lag-fix/` et ne valident pas le nouvel ordre de rendu.

## Mesures physiques finales

Sur le Quest 3, les fenêtres complètes de cinq secondes à l’intérieur de chaque
phase donnent les observations suivantes (`physical-summary.json`) :

| Phase | Fenêtres / images | Intervalle moyen | Mémoire Unity allouée | PSS au début de phase |
| --- | --- | --- | --- | --- |
| Surface visible | 2 / 722 | 13,88782 ms | 105,30–105,32 Mo | 823 850 Kio |
| Surface masquée | 1 / 361 | 13,88780 ms | 106,89 Mo | 834 098 Kio |
| Surface masquée, échelle ×2 | 2 / 721 | 13,88782 ms | 106,87–106,89 Mo | 834 833 Kio |

Cela correspond à environ 72 Hz sur ces fenêtres, sans constituer un seuil de
performance général ni une mesure GPU. La première fenêtre après chaque changement
de phase est exclue car elle chevauche la transition. Le maximum de toutes les
fenêtres, démarrage et captures compris, est de 472,19 ms ; les captures de diagnostic
perturbent les temps et la mémoire. Le maximum des fenêtres stables est de 13,8921 ms.

Le buffer des huit contacts fait 256 octets ; les buffers anatomiques totalisent
3 870 176 octets. Le compteur d’uploads reste à sept pendant les trois phases.
Chaque libération et la libération finale ramènent les compteurs de buffers à zéro ;
la mémoire globale du processus n’est pas censée revenir à zéro et ces trois cycles
ne démontrent pas l’absence de toute fuite à long terme.

Les captures Android montrent zéro contact rouge à travers la surface opaque,
puis huit composantes rouges quand elle est masquée, conservées à l’échelle ×2.
Leurs aires totales passent de 1 168 à 3 459 pixels ; le point de vue suivi peut varier
entre captures, donc ce ratio ne remplace pas le test à caméra fixe T4.
Le noir des captures Unity n’est pas une preuve d’absence du passthrough dans le casque.

Le premier essai physique (`device-20260909-080358`) réussissait les assertions de
rendu, mais échouait à récupérer les images : `CaptureScreenshot` préfixe déjà le
chemin persistant sur Android. Le diagnostic passe maintenant un nom relatif ;
les deux Players ont été reconstruits et le run final confirme les trois fichiers.
Les 44 tests PlayMode ont été rejoués après la correction du retard ; les 112 tests
EditMode et le test de garde Android portent sur les contrats inchangés. Le chemin
de capture est vérifié par le build et l’exécution Android. L’essai `device-20260909-083047`,
dont les trois images étaient noires, est conservé mais exclu de la qualification visuelle.

## Validation manuelle effectuée et reproduction

Le propriétaire a validé M1/M2, puis le correctif du retard pendant les mouvements
brusques le 2026-09-09. L’APK final a pour SHA-256
`ccb40eb4323dfe20d65882830034c08cd92f89ab952e9275555f2a06d9b250bc`.
HiBoP a ensuite été arrêté sur le Quest, absence de PID vérifiée ; le Player Windows reste ouvert.
Le diagnostic automatique charge le **vrai HBNA exporté par le Player QUEST-014**
mais l'injecte localement ; il ne valide ni l'envoi produit ni les gestes du propriétaire.

Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-014\fixture\quest-mni-contacts.hibop`,
visualisation **MNI Contacts**, surface **MNI Grey matter**, huit contacts synthétiques
de 2 mm, coordonnées asymétriques documentées dans la [fixture](../fixtures/mni-contacts/README.md).

Binaires livrés, hashes dans le manifeste :

- Windows : `C:\HBP\Software\HiBoP\.artifacts\quest-014\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
- Quest : `C:\HBP\Software\HiBoP\.artifacts\quest-014\Android\HiBoP.Quest.apk`, installé sur `192.168.1.18:5555`.
- Capture de qualification : `C:\HBP\Software\HiBoP\.artifacts\quest-014\captures-final\20260909-081217-27b44604-49f2-4463-a630-e3cf6a4e2dc0\anatomy.hbna`.

Lancement Windows avec la fixture :

```powershell
Start-Process -FilePath 'C:\HBP\Software\HiBoP\.artifacts\quest-014\Windows\HiBoP.6.1.0.win64\HiBoP.exe' -ArgumentList '-pf C:\HBP\Software\HiBoP\.artifacts\quest-014\fixture\quest-mni-contacts.hibop -v "MNI Contacts" -screen-fullscreen 0'
```

Reproduction du diagnostic physique, hors sandbox, après rétablissement des manettes :

```powershell
.\Tools\Run-QuestContactProbe.ps1 -Serial 192.168.1.18:5555 -SkipInstall -Fixture 'C:\HBP\Software\HiBoP\.artifacts\quest-014\captures-final\20260909-081217-27b44604-49f2-4463-a630-e3cf6a4e2dc0\anatomy.hbna'
```

Le diagnostic consomme un marqueur réservé au build de développement, masque
le panneau d'appairage pour les captures, puis libère le contenu. Son lanceur
arrête ensuite HiBoP et vérifie l'absence de PID ; il conserve ADB et D26.
Un lancement normal suivant retrouve le panneau et ne rejoue pas le diagnostic.

Préparation produit : ouvrir cette fixture dans le Player Windows QUEST-014 ;
ouvrir **Quest**, entrer `192.168.1.18`, cliquer **Inspect Quest**, comparer les
groupes de l'empreinte avec le panneau du casque, confirmer et saisir son code,
cliquer **Pair**, puis **Envoyer au Quest**. Si le code expire, Y en génère un
nouveau. B réduit le panneau ; X recentre le groupe.

| ID | Action exacte | Attendu | Statut |
| --- | --- | --- | --- |
| M1 | Presser **A droite** pour masquer le cerveau. Saisir avec une gâchette d'index, déplacer/tourner, relâcher puis reprendre. Presser A pour réafficher. | Les huit contacts restent visibles et le groupe reste saisissable ; la surface revient au même endroit. | VALIDE le 2026-09-09 avant correction du retard ; voir M3 |
| M2 | Masquer avec A ; saisir avec les deux gâchettes d'index, écarter/rapprocher les mains. Observer les quatre paires asymétriques, puis réafficher avec A. | Positions, espacement et diamètre des contacts suivent ensemble le groupe ; aucun contact ne reste à une taille fixe dans le monde. | VALIDE le 2026-09-09 avant correction du retard |
| M3 | Sur le build corrigé, saisir avec une gâchette d'index et effectuer des translations/rotations brusques, surface visible puis masquée avec A. | Les contacts restent solidaires du cerveau, sans retard ni sortie transitoire. | VALIDE le 2026-09-09 : « Tout est bon ! » |

Aucune validation manuelle restante. L’arrêt HiBoP prévu par D23 est effectué et
vérifié dans `manual-lag-fixed/stopped.json`. ADB Wi-Fi, préparation hors tête et
maintien éveillé sur alimentation sont conservés (D26).

## Décisions, limites et suite

Une panne des manettes a interrompu les premiers essais : elles étaient appairées
mais sans suivi actif. Les tentatives de récupération, dont l’activation du suivi
des mains, n’ont pas rétabli les interactions. Le propriétaire a redémarré le casque,
puis reporté l’investigation de la cause pour reprendre QUEST-014. Après redémarrage,
les deux manettes ont retrouvé un suivi de position valide ; le lancement manuel
a permis de dépasser l’attente du Guardian et de terminer les validations.

Les preuves sont conservées dans `controller-diagnostic/`. La déconnexion initiale
précède l’installation du nouvel APK ; aucune cause système précise n’est démontrée.
Le suivi des mains a été laissé activé après l’essai de récupération. D26 et le script
de connexion n’ont pas été modifiés sur la base d’une hypothèse. Ce problème résolu
pour la session ne limite plus la qualification de QUEST-014.

D05/D19 sont appliquées. A est une affectation UX locale sur le bouton encore
libre, validée lors de M1/M2 ; aucune décision scientifique supplémentaire.
Pas de transparence passthrough, de picking précis, de reconstruction native
ou de projection ajoutée. Le diagnostic physique ne prétend pas valider le
confort ou la perception stéréoscopique du propriétaire.

Comme dans QUEST-013, le diagnostic Desktop initial relève `CameraUnchanged=false`
pendant son export asynchrone. Il ne démontre pas l'invariance de la caméra du
Player ; la présentation anatomique et les buffers sont conservés, et les
tests vérifient l'indépendance des coordonnées des sites. Ce point préexistant
n'a pas fait l'objet d'une modification hors périmètre.

Le critère J3 de QUEST-014 est démontré et la tâche est clôturée. Prochaine tâche proposée : **QUEST-015**, sans exécution automatique.
