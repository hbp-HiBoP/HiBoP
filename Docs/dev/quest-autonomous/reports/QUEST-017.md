# Rapport QUEST-017 — Entrées de projection transférées et reconstruites

## Résultat

La capture Desktop produit désormais un snapshot HBNA v3 contenant la surface
préparée, les contacts et leurs masques effectifs, les octets du volume NIfTI
de référence et les paramètres de grille, d'influence et d'opacité. Le volume
est nécessaire parce que la grille de projection de `Base3DScene` est construite
depuis l'IRM sélectionnée : la surface seule ne fournit ni cette grille ni son
affine spatiale.

Sur Quest, `QuestAnatomyView.ApplySnapshot` prépare un lot
`NativeProjectionInputs` avant de publier le nouveau contenu. Le lot possède
un `Volume`, une `Surface`, un `RawSiteList` et son fichier privé. Une erreur de
préparation conserve la publication précédente ; après succès, les anciennes
ressources sont libérées. Déconnexion et suspension du récepteur conservent le
lot, fermeture/remplacement/destruction de la vue le libèrent. Aucun calcul de
densité, grille native, générateur ou shutdown global n'est ajouté.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ;
  manuel : VALIDE ; revue documentaire M1 confirmée le 2026-09-09.
- Branche `feature/xr-autonomous`, HEAD initial
  `1796e1d3859283e75b3b6a56ffa2437192734e33`, checkout initial propre.
  Travail non commité, sans push, CI distante ou changement de branche.
- Unity `6000.5.2f1`, profils existants `DesktopWindows.asset` et `Quest.asset`.
- Dépendances [QUEST-014](QUEST-014.md) : surface/contacts et durée de vie de la
  vue ; [QUEST-016](QUEST-016.md) : wrappers Android IL2CPP et libérations.
- Binaire natif inchangé, `Tools/NativePlugins.lock.json`, source hbp_core
  `ffb7686011f37a21e6c5ab4f88ad6cfcbc53ffc1`. Les exports de lecture des sites et
  masques existaient déjà ; seuls leurs wrappers C# sont ajoutés.
- [Manifeste des preuves](../evidence/QUEST-017/manifest.json).
  Les gros logs, Players et exports restent locaux sous `.test-results/quest-017`
  et `.artifacts/quest-017`, avec hashes dans le manifeste.
- Aucune reprise de la branche historique XR, aucun nouveau prefab/GameObject.

## Points de review

| Priorité | Entrée | Règle à vérifier |
| --- | --- | --- |
| 1 | [AnatomyProjection.cs](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomyProjection.cs), [AnatomySnapshotCodec.cs](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomySnapshotCodec.cs) | Sections v3 bornées, SHA-256 du volume et de l'enveloppe, tailles exactes, paramètres et cohérence des masques ; v1/v2 restent des anatomies sans promesse de projection. |
| 2 | [DesktopAnatomyCapture.cs](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs), [Volume.cs](../../../../Assets/Scripts/HBP/Core/DLL/Volume.cs) | Lire l'IRM réellement sélectionnée et déjà chargée ; vérifier la provenance de ses octets, refuser un fichier modifié, préserver le retour booléen sur erreur de lecture. |
| 3 | [NativeProjectionInputs.cs](../../../../Assets/Scripts/HBP/Transfer/Projection/NativeProjectionInputs.cs), [Electrodes.cs](../../../../Assets/Scripts/HBP/Core/DLL/Electrodes.cs) | Une conversion par frontière, ordre/indices/masques conservés ; propriétaires explicites, nettoyage complet et fichier limité au sous-répertoire local généré. |
| 4 | [QuestAnatomyView.cs](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs), `ApplySnapshot`, `Clear` | Préparer avant permutation, préserver l'ancien lot sur échec, le libérer seulement après publication ; conserver les ressources hors connexion. |
| 5 | [NativeProjectionInputTests.cs](../../../../Assets/Tests/EditMode/HBP.Quest.Anatomy.Tests/NativeProjectionInputTests.cs), [QuestProjectionDiagnostic.cs](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestProjectionDiagnostic.cs) | Preuves de round-trip et de durée de vie ; sonde Android ponctuelle activée par marqueur, sans traitement scientifique de remplacement. |

## Tableau des entrées, repères et propriétaires

| Entrée | Capture et transport | Reconstruction / propriété |
| --- | --- | --- |
| Surface | Mesh anatomique MNI complet, deux hémisphères, sans inflation/coupe ; `hibop-mni-unity-mm-v1`, gauche, mm, matrice identité, winding clockwise. Cette sélection correspond à `MeshManager.ReferenceSurface`. | `Surface.SetBuffers` reçoit ces positions, normales, indices et UV. Le wrapper inverse X et le winding pour le natif ; ne pas inverser une seconde fois. La surface appartient au lot. |
| Sites | `DefaultPosition`, patient/order/source index explicites. Aucun transform monde ou placement de caméra. | `TransportToNativeSite` applique X → −X, Y/Z inchangés, en mm. `SetPatients`, `AddSite` puis `UpdateMask` dans l'ordre du snapshot ; `RawSiteList` appartient au lot. |
| Masques | `Site.State.IsEffectivelyMasked(roi)` existant ; flags sources conservés. Formule : Masked OU Blacklisted OU (OutOfRoi ET ROI active) OU NON Filtered. `Filtered` signifie retenu par le filtre. | Le booléen préparé est appliqué, sans utiliser la visibilité graphique pour la science. Le contrat refuse les flags contradictoires. Les nouveaux getters recopient positions et masques depuis le natif pour les vérifier. |
| Volume | Octets intégraux du fichier de l'IRM sélectionnée, SHA-256 de provenance enregistré au chargement, relu/vérifié hors thread Unity à la capture. Aucun chemin émetteur sérialisé. | En-tête/affine/voxels inchangés, fichier `reference.nii` dans un dossier GUID propre au candidat. Sur Android, racine canonique fournie par `getFilesDir()`, sous `projection-sessions`. SHA-256 relu avant `Volume.LoadNIFTIFile`. Fichier gardé jusqu'à libération du lot. |
| Grille | `ActivityProjectionSettings.VolumeGridDimension`, `.VolumeInterpolation` ; les états de grille/surface en cours d'invalidation sont refusés. | Conservés comme paramètres ; aucune grille n'est calculée dans QUEST-017. Interpolation 0 = Nearest, 1 = Trilinear. |
| Influence | `column.AnatomyParameters.InfluenceDistance` en mm ; préférences `SiteInfluenceByDistance`. | Aucun facteur de présentation appliqué. Règle 0 = Constant, 1 = Linear, 2 = Quadratic. |
| Apparence / normalisation | `column.ActivityAlpha` conservé avec les couleurs/opacité déjà présentes dans l'anatomie. | Couverture et normalisation par densité maximale restent celles du moteur commun existant, sans option nouvelle ni calcul dans cette tâche. |
| Présentation | État propre à Desktop ou Quest. | La vue conserve son placement/rotation/échelle ; le prefab existant applique uniquement mm → m au rendu, jamais aux ressources scientifiques. |

Le lot est construit et utilisé séquentiellement sur le thread Unity. Le décodage
et les lectures de fichiers Desktop utilisent des buffers privés hors de ce thread.
Aucun `Task.Run` concurrent n'accède aux mêmes handles. `Dispose` tente les trois
libérations puis le nettoyage du fichier, même si une libération échoue ; l'erreur
est journalisée et accessible via `CleanupError`, sans transformer une publication
effective en rejet de transfert. Aucun appel n'arrête le runtime natif partagé.

## Contrat et limites explicites

HBNA v3 ajoute après la section contacts une longueur int32 puis : dimension
de grille int32, interpolation int32, distance float32, règle int32, alpha float32,
longueur NIfTI int32, SHA-256 de 32 octets, octets NIfTI. L'enveloppe globale couvre
toutes ces données. Une v3 sans volume est invalide. Les anciennes v1/v2 restent
lisibles pour les anatomies historiques, avec `ProjectionInputs == null`.

Cette tranche accepte les volumes scalaires 3D NIfTI-1 `.nii`, non compressés,
little-endian, dont les dimensions/type/espacement/offset correspondent exactement
aux octets reçus. Les formats gzip, NIfTI-2, big-endian et multifichiers ne sont
pas convertis implicitement. Leur transfert est refusé avec un message demandant
une référence prise en charge. Le MNI fourni utilise ce format. Aucun voxel ou
site n'est supprimé pour rendre le transfert plus petit.

Limites inchangées : codec 128 Mio, transport 64 Mio. Le message de dépassement
chiffre total, surface, volume et limite ; les options indiquées sont sélectionner
un jeu source plus petit ou qualifier explicitement un budget de transfert/mémoire
plus grand. Il ne réduit ni précision, ni grille, ni contacts automatiquement.

## Vérifications effectuées

| ID | Scénario / commande | Résultat et preuve |
| --- | --- | --- |
| T1 | Unity CLI EditMode, profil Quest, assemblages `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests`, `-questAnatomyFixture` sur l'export v3 ci-dessous | REUSSI, exit 0, **136/136**, aucun ignoré ; [XML](../evidence/QUEST-017/editmode.xml). |
| T2 | Round-trip HBNA3, buffers privés, réglages, 32 combinaisons ROI/flags, volume absent, hash interne corrompu avec enveloppe resignée, longueurs/types/dimensions invalides | REUSSI dans T1 ; en particulier les nouveaux tests `AnatomyProjectionTests`. Aucune donnée native n'est allouée par le décodeur pur. |
| T3 | `NativeProjectionInputTests`, vrai MNI et triangle asymétrique à normales non axiales | REUSSI dans T1 : sommets/normales/winding/UV, sites X non nul et masque `[0,1]` relus depuis le natif ; dimensions/espacement/centre/orientations et trois échantillons volumiques comparés à l'original. Handles nuls et fichier supprimé après Dispose, seconde libération sans effet. |
| T4 | Capture Desktop : volume manquant, fichier modifié à dimensions identiques, fichier verrouillé puis rechargé | REUSSI dans T1 : capture refusée, provenance contrôlée, `LoadNIFTIFile` renvoie false sans laisser une exception de lecture bloquer `MRI3D.Load`. Les fixtures Desktop sont désormais préparées avec une IRM et des états de grille/surface prêts. |
| T5 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-017` et même commande avec `-Target Android` | REUSSI, exit 0, Players IL2CPP ; chemins/hashes dans le manifeste. |
| T6 | Player Windows, `-pf` sur l'archive synthétique et `-v "MNI Contacts" -captureAnatomy ...` | REUSSI pour les entrées : **15 532 677 octets**, 69 104 sommets, 138 216 triangles, huit sites, volume 11 661 664 octets. Captures répétées/source/round-trip bit-exacts ; [capture](../evidence/QUEST-017/capture.json), [vérification indépendante des hashes](../evidence/QUEST-017/capture-verification.json). |
| T7 | `Tools/Run-QuestProjectionProbe.ps1 -Serial 192.168.1.18:5555 -Fixture <export ci-dessous>` | REUSSI, exit 0, Quest 3 physique, **trois cycles IL2CPP** ; [résultat](../evidence/QUEST-017/device-result.json), [commandes et arrêt](../evidence/QUEST-017/device-commands.json). |
| T8 | `Tools/Test-QuestApk.ps1` et comparaison SHA-256 de l'APK installé par le runner | REUSSI, huit bibliothèques ARM64, APK **162 997 710 octets**, hash installé identique ; [contenu APK](../evidence/QUEST-017/apk-content.json). |
| T9 | Unity CLI PlayMode, profil Quest, `HBP.Quest.PlayModeTests`, `-questContactsFixture` sur l'export v3 | REUSSI, exit 0, **42 tests exécutés réussis**, trois tests historiques nécessitant `-questAnatomyFixture` ignorés ; [XML](../evidence/QUEST-017/playmode.xml). Le nouveau test TLS v3 dure 6,33 s et vérifie corruption interne avec enveloppe valide, conservation du lot, retry idempotent, remplacement et fermeture. |
| T10 | `Tools/format-code.cmd`, parseur PowerShell du runner et `git diff --check` après retrait des artefacts Unity | REUSSI, exit 0 ; 15 C# formatés avant la dernière campagne PlayMode/build. |

T7 vérifie les 69 104 sommets et normales et tous les indices après réexport par
`Surface.UpdateMeshFromDLL`, les positions et masques de tous les sites par les
exports natifs de copie, ainsi qu'un changement temporaire de masque suivi de sa
restauration. Les trois cycles préservent l'ancien lot lors d'une publication
refusée, gardent les ressources pendant une pause sans réseau, puis vérifient
les trois handles nuls et l'absence du fichier à chaque remplacement/fermeture.
Il s'agit d'une injection locale de l'export Desktop par ADB ; ce résultat seul
ne constitue pas une preuve de TLS entre deux Players.
T9 utilise le vrai code de transport TLS et la session Quest en boucle locale
dans Unity Editor. Le transfert TLS Windows physique → Quest n'a pas été rejoué
dans cette tâche ; les trois tests historiques ignorés concernent surtout les
gestes et la manipulation de 60 secondes déjà qualifiés dans les tâches précédentes.

Le chemin interne observé est
`/data/data/fr.crnl.hibop.quest/files/projection-sessions/bb3e21c49cf9420a80aefd9fbc8321f9/reference.nii`.
Il a été supprimé à la fermeture de son lot. Dimensions 208 × 256 × 219,
espacement float32 0,72 mm sur chaque axe, centre Unity mm
(0,4799957 ; −15,199997 ; 8,980003). Grille 80, interpolation Trilinear,
rayon 15 mm, influence Quadratic, alpha 0,8.

Le runtime affiche `0.2.1` ; la provenance du binaire est établie par le lock et
ses hashes, pas par cette chaîne. Le log Android conserve les diagnostics Meta
déjà observés dans QUEST-016 (`AssetPackManager`, `xrDiscoverSpacesMETA`, caméra
de capture introuvable). Aucune erreur du diagnostic ou du plugin scientifique.
HiBoP a été arrêté, `pidof` vide vérifié ; ADB Wi-Fi reste disponible.

Le diagnostic Desktop initial a observé un changement de matrice de caméra
pendant ses 20 frames d'attente : `CameraUnchanged=false`. L'échantillon ne
requalifie donc pas cet invariant visuel. Le transform du cerveau est inchangé
et aucune instruction de placement/caméra n'est ajoutée par cette tâche.
La copie initiale sur le thread Unity prend 12,8 ms ; deux encodages et le
round-trip diagnostic prennent 1 200,49 ms. Ces valeurs ne sont pas un budget
de performance de projection.

Deux écarts dans les tests de développement ont été corrigés avant T1 final :
normalisation du chemin Windows dans l'assertion, puis préparation explicite des
flags de grille/surface dans l'ancienne fixture Desktop. La revue indépendante
a identifié le cas de fichier verrouillé ; son correctif et son test sont inclus.

Le contrôle d'approbation automatique a initialement refusé l'installation et le
transfert. Le propriétaire a explicitement autorisé cet essai sur
`192.168.1.18:5555` le 2026-09-09 dans cette conversation ; T7 a été exécuté
ensuite. La fixture contient uniquement le MNI du dépôt et des contacts
synthétiques, selon son manifeste, sans patient réel ni EEG.

### Reproduction et artefacts locaux

Depuis `C:\HBP\Software\HiBoP`, PowerShell hors sandbox, éditeur fermé pour les builds :

```powershell
./Tools/Prepare-QuestAnatomyFixture.ps1 -Fixture mni-contacts -OutputDirectory .artifacts/quest-017/fixture
./Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-017
./Tools/Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId quest-017
./Tools/Run-QuestProjectionProbe.ps1 -Serial 192.168.1.18:5555 -Fixture 'C:/HBP/Software/HiBoP/.artifacts/quest-017/captures/20260909-111610-0c6c7cfd-cb1a-4477-b6d4-18243f162f1a/anatomy.hbna'
```

- Projet : `C:\HBP\Software\HiBoP\.artifacts\quest-017\fixture\quest-mni-contacts.hibop`.
- Player Windows : `C:\HBP\Software\HiBoP\.artifacts\quest-017\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
- APK : `C:\HBP\Software\HiBoP\.artifacts\quest-017\Android\HiBoP.Quest.apk`, SHA-256
  `a5867786444caccd58e18e604684ad5bfae8ee759a808a3eb962319bd2cb4d58`.
- Export utilisé sur casque : `C:\HBP\Software\HiBoP\.artifacts\quest-017\captures\20260909-111610-0c6c7cfd-cb1a-4477-b6d4-18243f162f1a\anatomy.hbna`, SHA-256
  `2cd71c1236a6e43793b036bfe1500aceca744d13a405b75165d72350df775a26`.
- Volume source : `Assets/Data/IRM/MNI.nii`, SHA-256
  `d7ba082a644a0334c140b760e1df607655e85b8f4a0bc63139b0680911ba0c96`.
- Capture Windows : lancer le Player avec
  `-pf C:/HBP/Software/HiBoP/.artifacts/quest-017/fixture/quest-mni-contacts.hibop -v "MNI Contacts" -captureAnatomy C:/HBP/Software/HiBoP/.artifacts/quest-017/captures -screen-fullscreen 0`.
  Le diagnostic exporte après préparation puis sur F8. Le flux produit conserve
  l'action existante « Envoyer au Quest » ; aucun nouveau bouton n'est ajouté.

L'export physique a été produit par le premier build Windows de cette tâche,
avant l'ajout du refus explicite d'une sélection sans IRM et les derniers
compléments de tests. La capture valide utilise le même chemin de production ;
la suite finale vérifie le refus nouveau. Les hashes du Player de capture sont
conservés séparément dans le manifeste, avec ceux du Player final reconstruit.
Les modifications Unity automatiques (BuildInfo, préférences de build, préfiltrage
des shaders, ressources de tests) sont conservées dans un patch local puis retirées
du diff livré. Aucune source native ni aucun binaire de plugin n'a été modifié.

Les libérations sont garanties aux frontières normales de session ; aucun
mécanisme de restauration après crash ni nettoyage global de caches abandonnés
n'est ajouté. Le cache technique n'est pas une capsule persistante.

## Validation manuelle

M1 — VALIDE le 2026-09-09 par le propriétaire, retour explicite « M1 OK » dans
la conversation `01a085ce-1d2d-72a3-8006-ffd59c832fe4`, après clarification des
quatre comportements attendus :

1. Quest reçoit le volume de référence, la surface et les sites de la visualisation Desktop sélectionnée.
2. Les exclusions Desktop restent appliquées sur Quest via les masques effectifs (blacklist, ROI et filtres).
3. Les manipulations du cerveau préservent les positions scientifiques et les distances en millimètres.
4. Les données de la session restent disponibles hors connexion ; un envoi invalide conserve la session précédente, tandis qu'un remplacement réussi ou une fermeture libère ses ressources.

Ce retour valide les comportements attendus et reste distinct des preuves
automatiques. Aucun test manuel au casque ni validation de parité scientifique
visuelle n'est revendiqué ; les limites techniques documentées restent applicables.

## Suite

QUEST-018 pourra consommer ces entrées pour extraire la densité commune et l'utiliser
sur Desktop. Cette tâche suivante n'est pas exécutée automatiquement. Aucun verdict
de parité scientifique de densité n'est revendiqué ici.
