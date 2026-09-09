# Rapport QUEST-013 — Préparer et transférer les contacts synthétiques

## Résultat

La capture anatomique Desktop ajoute désormais tous les contacts préparés de
l'implantation MNI sélectionnée. Le snapshot transporte positions scientifiques,
identités persistantes, ordre natif, associations patients/électrodes, couleur
linéaire, diamètre, visibilité et masques préparés. Le récepteur conserve les
contacts au même point de publication que la surface ; déconnexion et retry
préservent cette collection, remplacement et fermeture la libèrent.

L'anatomie seule reste encodée en **HBNA v1**, octets inchangés. Les contacts et
leurs métadonnées utilisent **HBNA v2**, lu par le nouveau codec. Un ancien
récepteur v1 refuse v2 explicitement : les deux Players doivent être actualisés
pour envoyer des contacts. Le protocole TLS et les reçus restent inchangés.
Aucun contact n'est rendu sur Quest dans cette tâche (QUEST-014).

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : VALIDE.
- Branche `feature/xr-autonomous`, HEAD initial
  `2155f7216f5727497922c224640af6dd4f3c298f`, checkout initial propre.
- Unity `6000.5.2f1`, éditeur fermé au départ, essais CLI hors sandbox.
- Dépendances QUEST-006 à QUEST-012 réutilisées : capture, codec anatomique,
  TLS embarqué, staging/publication atomique, manipulation indépendante.
- Référence historique inspectée :
  `eb26c323e:Assets/Scripts/HBP/RenderModelAdapters/DesktopSiteRenderModelAdapter.cs`.
  Principes retenus : séparation position scientifique/apparence préparée.
  Aucune restauration globale, aucun type RenderModel historique ajouté.
- Code et données locaux non committés ; pas de push ni changement des dépôts
  natifs voisins. [Manifeste des preuves](../evidence/QUEST-013/manifest.json).

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Règle et risque |
| --- | --- | --- |
| 1 | [AnatomyContacts](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomyContacts.cs), `AnatomySite`, `ValidateCoordinates` | Copie des tableaux, IDs uniques, ordre explicite, références patients valides ; même repère MNI que le cerveau. |
| 2 | [AnatomySnapshotCodec](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomySnapshotCodec.cs), `Encode/Decode` ; [AnatomyContactsCodec](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomyContactsCodec.cs) | Compatibilité v1, section v2 dimensionnée avant allocation des buffers surface, compteurs bornés, SHA-256 global couvrant les contacts. |
| 3 | [DesktopContactsCapture.Capture](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopContactsCapture.cs) ; [SiteState.IsEffectivelyMasked](../../../../Assets/Scripts/HBP/Core/Object3D/Site.cs) | Snapshot pris sur thread Unity sans yield ; `DefaultPosition`, index et association recoupés avec l'implantation ; règle de masque partagée avec `Column3D.UpdateDLLSitesMask`. |
| 4 | [QuestAnatomyView](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs), `ApplySnapshot/Clear` ; [AnatomyDeliveryTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyDeliveryTests.cs) | Contacts remplacés après staging réussi, conservés après erreur/retry/déconnexion, vidés pour anatomie seule ou fermeture. |
| 5 | [Fixture et tableau de coordonnées](../fixtures/mni-contacts/README.md), [contacts.json](../fixtures/mni-contacts/contacts.json) | Huit sites synthétiques, deux patients, deux électrodes chacun ; asymétrie et IDs stables. Nombre de fixture distinct des limites techniques. |

## Contrat et correspondance native

La table complète **ID / ordre / patient / index source / coordonnées** est dans
le [README de fixture](../fixtures/mni-contacts/README.md). Les `.patient` sont
les entrées du vrai projet ; `contacts.json` est l'attendu indépendant utilisé
par la review et le test de chargement d'archive.

| Champ | Sens / utilisation ultérieure |
| --- | --- |
| `PatientIds[i]` | Ordre exact de `Visualization.Patients` pour `RawSiteList.SetPatients`. Même les patients sans site peuvent être conservés. |
| `AnatomySite.Id` | `SiteData.ID` persistant, distinct d'un nom affiché, d'un InstanceID Unity ou du `FullID` concaténé. |
| `Name`, `Electrode` | Nom brut du contact et groupe d'électrode de `Implantation3D.SiteInfo`. A1 peut exister chez deux patients. |
| `Order` | Position contiguë dans la liste reçue, identique à `Site.Information.Index` et à l'ordre d'ajout natif. |
| `PatientIndex`, `SourceIndex` | Index dans `PatientIds` et index dans `Patient.Sites`. `SourceIndex` n'est pas le numéro extrait du nom A1/A2. Arguments de `RawSiteList.AddSite`. |
| `Position` | XYZ float32 dans `hibop-mni-unity-mm-v1`, gauche, millimètres, mapping v1 identité. Source `Information.DefaultPosition`, jamais `transform.position` ou position de miroir Desktop. |
| `Diameter`, `Color`, `Visible` | Diamètre en mm de la sphère source de rayon 1 × gain ; RGBA linéaire et visibilité préparées. Les transforms parents et la caméra ne sont pas capturés. |
| `Flags`, `RoiActive`, `EffectiveMasked` | Flags sources Masked=1, Blacklisted=2, OutOfRoi=4, Filtered=8 ; `Filtered=true` signifie inclus. Masque scientifique préparé, distinct de `Visible`. |

Conversion future vers `RawSiteList.AddSite` : pour une position transportée
`(x,y,z)`, fournir `(ReferenceSystemConversion.ConvertX(x),y,z)`, en mm.
`AddSite` appelle `Vec3.FromVector3(..., convertReferenceSystem:false)` : ne pas
appliquer une deuxième réflexion. Exemple fixture ordre 0 : natif
`(-31.25,-18.5,26.75)` → transport `(31.25,-18.5,26.75)` → même natif.
Sur Quest, le futur rendu utilisera le même parent millimétrique que la surface
(`0.001`), puis le placement/rotation/échelle locaux indépendants de la science.
Cette tâche ne crée pas de wrapper de reconstruction ni de calcul de densité.

Le masque commun reste exactement :
`IsMasked || IsBlackListed || (IsOutOfROI && roiActive) || !IsFiltered`.
Il est évalué sur Desktop et stocké sans recalcul scientifique dans Quest.
Les sites invisibles/masqués ne sont jamais retirés de la collection.

**Ordre Desktop préexistant** : le regroupement hiérarchique par électrode peut
réordonner une source entrelacée A1,B1,A2 alors que les index natifs suivent la
source. La capture refuse un désalignement plutôt que réassocier silencieusement
les masques. La fixture est groupée A1,A2,B1,B2. Corriger l'ordre général Desktop
ne fait pas partie de QUEST-013 ; le contrôle de rejet est testé.

### Extension binaire HBNA v2

Tout reste little-endian. Après les buffers surface v1 et avant le SHA-256
final : longueur de section int32, implantation UTF-8 (préfixe int32), bool ROI,
count patients int32 et leurs IDs UTF-8, count sites int32, puis les records
ordonnés. Un record contient trois textes (ID/nom/électrode), trois int32
(ordre/patient/index source), XYZ float32, RGBA float32, diamètre float32,
visibilité byte, flags byte, masque effectif byte. Soit 47 octets fixes par
record en plus des trois textes préfixés. Le hash surface reste limité à la
surface ; le hash global couvre aussi toute cette extension.

Le plafond existant de 128 MiB reste appliqué. Une borne technique de 100 000
sites et 100 000 patients protège les allocations du décodeur ; dépasser une
borne produit une erreur explicite, jamais une troncature. Les tests incluent
257 contacts. Les tailles de section, compteurs impossibles, boolean/flags
inconnus, coordonnées non finies et associations invalides sont rejetés.
IDs patients avec `?`/NUL et noms avec NUL sont refusés pour rester compatibles
avec les chaînes C utilisées par les wrappers natifs.

## Vérifications effectuées

Les logs, captures et binaires sont locaux et ignorés, sous
`C:\HBP\Software\HiBoP\.test-results\quest-013` et
`C:\HBP\Software\HiBoP\.artifacts\quest-013` ; hashes dans le manifeste.
Les commandes exactes Unity et leurs exit codes sont conservés dans les fichiers
`*-command.json` et le lanceur local `run-checks.ps1`.

| ID | Vérification / commande | Résultat / preuve |
| --- | --- | --- |
| T1 | EditMode `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests` | **112 réussis, 0 échec, 1 ignoré** réservé à Android, exit 0. `editmode-final.xml/log`. V1/v2, zéro site, 257 sites sans troncature, mémoire privée, invalides rehashés, coordonnées, associations, 16 combinaisons de flags × état ROI de la règle partagée ; hash surface v1/v2. |
| T2 | EditMode `HBP.Serialization.Tests`, filtre `HBP.Tests.Serialization.QuestAnatomyFixtureTests` | **3/3**, exit 0, `fixture.xml/log`. Vrai chargeur projet : fixture anatomique, MNI du dépôt et nouvelle archive avec ses deux patients/huit contacts ; aucun problème de récupération structurelle. |
| T3 | PlayMode `HBP.Quest.PlayModeTests`, options `-questAnatomyFixture` (ancienne capture v1) et `-questContactsFixture` (capture Player v2 finale) | **41/41**, exit 0, 72,67 s, `playmode.xml/log`. TLS réel loopback ; le payload Player de 3 870 953 octets est publié avec les huit contacts exacts ; retry/déconnexion, mauvais remplacement, anatomie seule et fermeture vérifiés. |
| T4 | `Tools/format-code.cmd` et `git diff --check` | Exit 0. Seize C# formatés. Première restauration NuGet bloquée dans la sandbox ; formateur relancé hors sandbox avec succès. `format.log`, `final-checks.json`. |
| T5 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-013` | Player final Windows IL2CPP **Succeeded**, exit 0, 102,60 s, 26 warnings / 0 erreur ; `windows-build.log`, `DesktopWindows.build-report.json`. |
| T6 | Même lanceur avec `-Target Android` | Quest ARM64 IL2CPP **Succeeded**, exit 0, 350,66 s, 64 warnings / 0 erreur. APK **162 985 941 octets**, sept ELF ARM64 et un fichier de symboles `.usym.so` (distinct d'une bibliothèque ELF), vérifiés par `inspect-apk.py` / `apk-inspection.json`. |
| T7 | Player Windows final, fixture finale, `-captureAnatomy` ; `python .test-results/quest-013/verify-capture.py` | Capture **HBNA v2**, 69 104 sommets, 138 216 triangles, **8 contacts**, **3 870 953 octets**. Tous les IDs/index/coordonnées correspondent à `contacts.json`, diamètre 2 mm, visibilité vraie et masque effectif faux. Buffers source, captures répétées et round-trip bit-exacts ; hashes vérifiés indépendamment. |
| T8 | Inspection des modifications Unity après builds/tests, retour au profil DesktopWindows | Quatre fichiers générés inspectés et rétablis : BuildInfo, URP Quest, migrations OpenXR et icônes Android. Diff conservé dans `generated-settings.diff`. Éditeur CLI terminé ; seuls les Players de contrôle de cette tâche ont été arrêtés. |

**156 tests réussis, aucun échec**, un test ignoré propre au profil Android.
Le hash surface MNI reste
`2024e5e69d3f84bb5ce3cfe545b0b98548217f902a53a53020135fb7685c4114`.
La capture complète finale vaut
`09483e2f1cee779158734e591272aa2909422d1fff8107b880e57d2cbb7d1cab` ;
l'APK vaut
`71aa1f5ae54c7c25a9d8a184c98a5d1a08d9252ad86d14f96f6d6cc47ae33f04`.
Les binaires natifs Desktop et le lock existant sont identifiés dans le
manifeste ; aucun calcul natif Android n'est qualifié ici.


## Validation manuelle et clôture

**M1 — VALIDE ; tâche clôturée le 2026-09-09.** Premier retour du même jour : Le propriétaire confirme la
convention de coordonnées : « Je ne sais pas ce que je dois valider exactement,
mais je vois bien en effet que l'axe X est inversé sur les coordonnées entre
natif et unity, donc sur ce point c'est ok pour moi ».

Le changement de signe de X, `(x,y,z) → (-x,y,z)`, est donc **validé par le
propriétaire**. Ce retour est limité à ce point : il ne vaut pas validation
visuelle des contacts, ni approbation explicite de tous les choix de fixture.
Après clarification du périmètre des contrôles automatiques et de la future
validation visuelle, le propriétaire confirme : « Ok on peut cloturer cette tâche
alors ». Cette acceptation explicite clôture M1 et QUEST-013. Aucun contrôle
manuel ne reste attendu pour cette tâche ; le rendu au casque reste QUEST-014.

La demande initiale était trop générale. Les IDs, leur ordre, les associations,
les huit positions et leur conservation au transfert relèvent des tests
techniques déjà réussis ; aucun recalcul manuel n'est demandé au propriétaire.
La fixture contient des points arbitraires synthétiques : aucune plausibilité
clinique n'est à certifier. L'examen visuel du placement et du rendu dans le
casque relève de QUEST-014, pas de cette tâche.

Pour reproduire la fixture depuis le dépôt :

```powershell
.\Tools\Prepare-QuestAnatomyFixture.ps1 -Fixture mni-contacts -OutputDirectory .artifacts/quest-013/fixture
```

Projet : `C:\HBP\Software\HiBoP\.artifacts\quest-013\fixture\quest-mni-contacts.hibop`.
Visualisation : **MNI Contacts**, colonne **MNI Contacts**.

Binaires locaux finaux, construits avec `Tools/Build-QuestConnectionPlayers.ps1
-Target Windows -EvidenceId quest-013`, puis `-Target Android` (Windows reconstruit
après l'ajustement de métrologie) :

- Windows : `C:\HBP\Software\HiBoP\.artifacts\quest-013\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
- Quest : `C:\HBP\Software\HiBoP\.artifacts\quest-013\Android\HiBoP.Quest.apk`.
- Capture : `C:\HBP\Software\HiBoP\.artifacts\quest-013\captures\20260909-064824-e9d980ed-0165-41d4-9928-0da1662c2ae1\anatomy.hbna`.
- Rapport de capture : même dossier, `capture.json` ; contrôle indépendant
  `C:\HBP\Software\HiBoP\.test-results\quest-013\capture-verification.json`.

Lancement reproductible du Player avec export automatique :

```powershell
Start-Process -FilePath 'C:\HBP\Software\HiBoP\.artifacts\quest-013\Windows\HiBoP.6.1.0.win64\HiBoP.exe' -ArgumentList '-pf C:\HBP\Software\HiBoP\.artifacts\quest-013\fixture\quest-mni-contacts.hibop -v "MNI Contacts" -captureAnatomy C:\HBP\Software\HiBoP\.artifacts\quest-013\captures -screen-fullscreen 0 -logFile C:\HBP\Software\HiBoP\.test-results\quest-013\desktop-player.log'
```

Le contrôle de l'agent a lancé le Player caché, puis arrêté uniquement son PID
après collecte. Aucun ancien Player utilisateur ni serveur ADB arrêté.

L'export diagnostic ajoute les contacts dans `capture.json` et `anatomy.hbna`.
Le bouton produit existant **Envoyer au Quest** utilise la même capture après
appairage. Les contacts sont reçus et conservés mais leur rendu est QUEST-014.
La recette M1 ne nécessite ni appairage ni casque.

## Décisions, limites et suite

La métrologie existante a nécessité une correction adjacente :
`QuestAnatomyMeasurements.Published` utilise désormais
`AnatomySnapshotCodec.GetSurfaceHashOffset` plutôt qu'un décalage depuis la fin
du fichier. La section contacts v2 rendait cet ancien calcul incorrect. Le test
v1/v2 compare le hash stocké au SHA-256 effectif des octets surface.

La première capture avait les huit contacts invisibles avec `ShowAllSites=false`.
La fixture finale utilise `ShowAllSites=true` : les huit enregistrements sont
visibles, sans retirer de site de la liste. Flags observés : 12
(`Filtered | OutOfRoi`), ROI inactive et masque effectif faux. Couleur préparée
RGBA linéaire : `(0.2428668, 0.0196066462, 0.0196066462, 0.5)` ; diamètre 2 mm.

Le diagnostic de lancement observe `CameraUnchanged=false` pendant l'export
asynchrone. Cette mesure ne démontre donc pas l'invariance de la caméra dans le
Player. La capture ne modifie aucune caméra ; les tests ciblés vérifient
l'indépendance des positions anatomiques et des transforms. La présentation du
cerveau, les buffers et la répétition bit-exacte sont vérifiés dans la capture.
Aucune validation visuelle de la caméra n'est revendiquée.


D14 autorise le MNI ; les contacts sont synthétiques. Aucun nouveau choix produit
ou scientifique demandé. La review indépendante a porté sur l'intégrité des
associations, les conversions, le codec et la publication ; elle ne remplace
pas les résultats d'exécution.

Pas de densité, picking, ROI interactive, surbrillance interactive ou rendu de
contacts Quest ajouté. Le masque effectif est une entrée préparée pour la suite.
Aucune qualification physique Windows→Quest du nouveau payload n'est revendiquée
sur la seule base du TLS loopback et des builds ; aucun casque manipulé dans
cette tâche. Aucun contrôle clinique ou parité de densité revendiqué.

Prochaine tâche proposée : **QUEST-014**, sans exécution automatique.
