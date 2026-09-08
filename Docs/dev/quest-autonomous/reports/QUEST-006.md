# Rapport QUEST-006 — Capture de la colonne anatomique Desktop

## Résultat

Le contrat HBNA de QUEST-005 n'avait pas de producteur Desktop. L'API
`DesktopAnatomyCapture.CaptureSelectedAsync` capture maintenant le mesh Unity
déjà préparé de la colonne anatomique sélectionnée, ses IDs et son apparence
uniforme. Un diagnostic optionnel exporte deux captures et un JSON de contrôle
depuis le Player Windows ; F8 permet de recommencer sur la sélection courante.

Aucun chargement MNI, changement de sélection, caméra, prefab ou modèle métier
n'est ajouté à la capture. Aucun réseau ni UI de connexion. Les colonnes autres
qu'anatomiques, les surfaces non MNI, un hémisphère isolé, les coupes, triangles
effacés, inflation/extrusion et colorations non représentables par HBNA v1 sont
refusés explicitement. Le diagnostic journalise la raison du refus.

## État et provenance

- Branche `feature/xr-autonomous`, HEAD initial
  `0fb71b89fd644fb4880dc345aa0cb0f991407ca9`, checkout propre au départ.
- Unity `6000.5.2f1` fermé au départ ; tests/build CLI hors sandbox Windows.
- Implémentation **IMPLEMENTEE**, technique **REUSSI**, manuel **VALIDE**.
- Dépendances : fixture et IDs de QUEST-001, contrat immuable HBNA v1 de QUEST-005.
- [Manifeste de preuves](../evidence/QUEST-006/manifest.json) : fichiers source,
  hashes, commandes, versions et résultats. Les gros artefacts sous
  `.test-results/quest-006` et `.artifacts/quest-006` sont locaux et ignorés.
- Aucun commit/push, changement de branche ni modification de dépôt voisin.

L'ancien `DesktopSurfaceRenderModelAdapter` a été consulté par `git show
eb26c323e:Assets/Scripts/HBP/RenderModelAdapters/DesktopSurfaceRenderModelAdapter.cs`.
Son accès aux tableaux Unity a servi de référence ; aucun ancien package, type,
GUID ou chemin XR n'est restauré. Les nouveaux fichiers possèdent leurs `.meta`.

## Ce que je conseille de reviewer

| Priorité | Point d'entrée | Règle à vérifier |
| --- | --- | --- |
| 1 | [DesktopAnatomyCapture.CaptureSelectedAsync](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs) | Sélection réelle, refus des cas non représentables, fermeture de la copie avant retour du Task. |
| 2 | [DesktopAnatomyCapture.FrameId et lecture du matériau](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs) | Repère local et apparence effective sans transforms de présentation ni projection scientifique. |
| 3 | [AnatomyCaptureDiagnostic.ExportSelectedAsync](../../../../Assets/Scripts/HBP/Dev/AnatomyCaptureDiagnostic.cs) | Comparaisons bit à bit, répétition, matrices caméra et encodage hors thread principal. |
| 4 | [DesktopAnatomyCaptureTests](../../../../Assets/Tests/EditMode/HBP.Transfer.Anatomy.Desktop.Tests/DesktopAnatomyCaptureTests.cs) | Mutation des sources après appel, refus sans sélection de remplacement, worker thread et annulation. |

## Origine des données et cohérence

`Module3DMain.SelectedScene.SelectedColumn` fournit le couple réel
`Visualization.ID` / `ColumnData.ID`. Le producteur ne fabrique ni ne normalise
ces IDs. Le futur appelant fournit `transferId`, `sessionId` et `revision` ;
ce paramètre de révision ne détecte pas automatiquement les éditions Desktop.
Le diagnostic utilise une livraison neuve par export et une révision 1 ; ses
deux captures partagent les mêmes métadonnées pour comparer tous les octets.

Positions, normales, indices et UV viennent de
`column.BrainMesh.GetComponent<MeshFilter>().sharedMesh`, sans instancier de
mesh, recalculer les normales, décimer ou consulter un collider. Les dimensions
sont confrontées à `MeshManager.BrainSurface`. Le pipeline existant
`MNIObjects.LoadDataAsync` charge et assemble les deux hémisphères, puis
`MeshManager.UpdateMeshesFromDLL` prépare le mesh de colonne. La capture
n'appelle aucune de ces opérations et ne revient jamais au MNI global.

Le repère `hibop-mni-unity-mm-v1` désigne les coordonnées locales Unity après
la conversion native existante : inversion de X des positions/normales et
inversion du winding à la frontière `hbp_surface_copy_unity_mesh` /
`ReferenceSystemConversion`. Unités millimètres, axes gauchers et faces avant
clockwise ; `AssetToBrain` est l'identité. L'origine est conservée telle que
préparée après GII/TRM. Ce nom ne prétend pas identifier une variante scientifique
MNI supplémentaire. Translation d'espacement des scènes, transforms parents,
rotation/zoom caméra sont exclus.

La couleur provient de `_MainTex` du `Renderer.sharedMaterial` réellement
utilisé. Une texture uniforme lisible est nécessaire : RGB sRGB converti en
linéaire, multiplié par `_Color.rgb` selon le color space du Player. L'alpha
de palette n'est pas utilisé par le shader ; l'opacité vient de `_Color.a`.
La capture refuse les property blocks et les contributions atlas/fMRI/densité.
Pour cette dernière, elle vérifie les UV d'opacité effectivement rendus après
`_AoTex_ST`, car le shader ne se fonde pas uniquement sur `_Activity`.
`Visible` décrit l'activation locale du cerveau et du renderer, indépendamment
de la minimisation ou du cadrage Desktop. Éclairage et relief du shader ne
font pas partie de l'apparence minimale HBNA.

La lecture Unity est un bloc synchrone sur le thread principal, sans `await` ni
callback ; un appel depuis un worker est refusé. Les états de préparation,
géométrie sale et transition sont refusés plutôt qu'attendus. Les traitements
Desktop examinés appliquent leurs changements de mesh sur le thread principal.
Une fois les tableaux copiés, `Task.Run` convertit les composantes et construit
le snapshot immuable ; le diagnostic encode, hash et écrit sur un worker.
Aucun verrou long ni handle natif/Unity n'est conservé dans le snapshot.

Une édition pendant l'attente ne change pas la première capture ; la seconde
capture diagnostique peut alors différer, et `RepeatedCaptureBitExact=false`
est un résultat observable, pas une réconciliation. La copie main-thread a un
coût mesuré, tandis que l'encodage et les écritures n'immobilisent pas Unity.

## Vérifications effectuées

La preuve finale est [capture.json](../evidence/QUEST-006/capture.json), copiée
depuis le Player Windows IL2CPP le **2026-09-08 à 07:41:53 UTC** après F8.

| ID | Vérification | Résultat |
| --- | --- | --- |
| T1 | EditMode, `HBP.Transfer.Anatomy.Desktop.Tests;HBP.Transfer.Anatomy.Tests` | 53/53 réussis après formatage, exit 0 : 17 capture, 36 contrat. |
| T2 | `Tools/format-code.cmd` | Exit 0, 3 fichiers C# ; NuGet a nécessité une exécution hors sandbox. |
| T3 | Préparation fixture par `Tools/Prepare-QuestAnatomyFixture.ps1 -OutputDirectory .artifacts/quest-006/fixture` | 1 853 octets, 8 sources vérifiées, SHA-256 identique à QUEST-001. |
| T4 | Revue indépendante concurrence/intégrité | Aucun défaut bloquant après ajout des contrôles GPU, UV d'opacité et complétude. |
| T5 | Build Windows IL2CPP, `HBP.Dev.HBPBuilder.BuildFromCommandLine` | REUSSI, exit 0, `Build Finished, Result: Success.` ; binaire ci-dessous. |
| T6 | Capture depuis la visualisation MNI réellement ouverte, F8 | REUSSI : buffers source/capture bit-exacts, répétition HBNA bit-exacte, encode/decode/réencodage bit-exact ; matrices caméra et présentation inchangées. |
| T7 | Inspection finale du diff, `git diff --check`, liens et SHA-256 | REUSSI ; réécritures Unity de BuildInfo, ProjectSettings et OpenXR inspectées puis rétablies, diff conservé localement. Seule la ligne QUEST-006 du registre est modifiée. |

Mesures finales : **69 104 sommets**, **138 216 triangles** (414 648 indices),
69 104 paires UV. Taille surface 3 869 920 octets ; fichier HBNA **3 870 248
octets**. Copie main-thread **1,9246 ms** ; deux encodages + décodage/réencodage
**325,59 ms** sur le worker. Deux frames Unity ont progressé pendant la
construction des captures. Ces mesures ponctuelles sur RTX 2070 SUPER ne sont
pas un budget garanti de latence ou mémoire.

Les positions s'étendent de `[-69.9079, -105.3945, -50.0656738]` à
`[72.17938, 72.04567, 83.34009]` mm dans le repère local. IDs observés :
`quest-001-visualization` / `quest-001-column` ; noms visualisation et colonne :
`MNI Anatomy`. RGBA linéaire : `[0.830769956, 0.462077051, 0.187820762, 1]`.
SHA-256 commun aux deux captures finales :
`065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12`.

Les exports automatiques au démarrage ont relevé un changement de matrice caméra
entre frames, compatible avec l'initialisation du cadrage/layout existante ;
la cause exacte n'a pas été instrumentée. Ils ne sont pas utilisés comme preuve
de stabilité. Après mise au premier
plan et F8 par le propriétaire, la matrice de vue et la projection de l'unique
caméra sont identiques avant/après, ainsi que le transform du cerveau. La capture
ne modifie aucune de ces valeurs ; le diagnostic conserve honnêtement les
résultats des exports précédents au lieu de remplacer un échec par un succès.

La première compilation de test a signalé un argument de constructeur incorrect,
corrigé avant les runs réussis. Le premier Player produisait bien les deux HBNA
identiques (3 870 248 octets), mais son `capture.json` était vide : les propriétés
anonymes sérialisées par réflexion avaient été supprimées par IL2CPP. Le rapport
est maintenant construit en tokens `JObject` explicites et vérifié dans le Player.
La couverture couleur automatisée utilise une
palette colorée et une teinte blanche ; une teinte non blanche n'a pas été
qualifiée séparément. Aucune preuve Quest/réseau ou nouvelle validation
scientifique n'est revendiquée.

## Validation manuelle reçue

Player livré et lancé :
`C:\HBP\Software\HiBoP\.artifacts\quest-006\player\HiBoP.6.1.0.win64\HiBoP.exe`.
Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop`.
Hashes du Player, GameAssembly, fixture et logs dans le manifeste.
Capture finale :
`C:\HBP\Software\HiBoP\.artifacts\quest-006\captures\20260908-074153-0f0167e7-e0e9-43b5-82c6-38fc381ebfd0\capture.json`.

Le propriétaire a confirmé le 2026-09-08 dans cette conversation :
« C'est fait, j'ai mis le player en premier plan et appuyé sur F8 ».
Le geste est donc effectué ; les données résultantes sont techniquement
vérifiées. Le propriétaire a ensuite confirmé le 2026-09-08 dans cette même conversation : « Je confirme le retour visuel sur le player desktop ». Cette confirmation valide M1 et M2 : cerveau attendu affiché et cadrage conservé lors de F8.

| ID | Action / observation attendue | Statut |
| --- | --- | --- |
| M1 | Confirmer que le cerveau attendu apparaît dans la colonne `MNI Anatomy` ; `quest-001-column` est vérifié dans le JSON produit. | VALIDE — confirmation visuelle du propriétaire le 2026-09-08 ; IDs également vérifiés techniquement. |
| M2 | Confirmer visuellement que F8 conserve le cadrage Desktop ; retourner OK/KO + observation. | VALIDE — confirmation visuelle du propriétaire le 2026-09-08 ; stabilité des matrices également vérifiée techniquement. |

Le Player visible a été laissé ouvert pour cette appréciation. Les deux processus de
test masqués précédents ont été fermés par l'agent avant les relances. Aucun
autre Player utilisateur ni Editor n'a été arrêté.

## Commandes reproductibles

Depuis `C:\HBP\Software\HiBoP`, Unity fermé, lancer tests/build hors sandbox :

```powershell
$questUnity = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
$questRoot = 'C:\HBP\Software\HiBoP'
$questCommon = @('-batchmode', '-nographics', '-projectPath', $questRoot, '-forgetProjectPath')
$questTests = $questCommon + @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'HBP.Transfer.Anatomy.Desktop.Tests;HBP.Transfer.Anatomy.Tests', '-testResults', "$questRoot\.test-results\quest-006\editmode-results.xml", '-logFile', "$questRoot\.test-results\quest-006\editmode.log")
$questResult = Start-Process -FilePath $questUnity -ArgumentList $questTests -Wait -PassThru -WindowStyle Hidden
$questResult.ExitCode
.\Tools\Prepare-QuestAnatomyFixture.ps1 -OutputDirectory .artifacts/quest-006/fixture
$questBuild = $questCommon + @('-quit', '-buildTarget', 'Win64', '-executeMethod', 'HBP.Dev.HBPBuilder.BuildFromCommandLine', '-buildOutput', "$questRoot\.artifacts\quest-006\player", '-scriptingBackend', 'IL2CPP', '-logFile', "$questRoot\.test-results\quest-006\build.log")
$questResult = Start-Process -FilePath $questUnity -ArgumentList $questBuild -Wait -PassThru -WindowStyle Hidden
$questResult.ExitCode
```

Après lancement, la capture ne charge aucun autre projet : les options `-pf` et
`-v` ci-dessous utilisent le parcours Desktop préexistant. Le diagnostic est
désactivé si `-captureAnatomy` n'est pas fourni.

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-006\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy' -captureAnatomy 'C:\HBP\Software\HiBoP\.artifacts\quest-006\captures' -logFile 'C:\HBP\Software\HiBoP\.test-results\quest-006\player-manual.log' -screen-fullscreen 0
```

## Décisions, limites et suite

Aucune décision produit supplémentaire attendue. Le périmètre initial couvre
la surface anatomique MNI complète, à apparence uniforme. Les surfaces patient,
coupes et projections ne sont pas transformées silencieusement en un autre
contenu. La capture construit seulement le snapshot ; session réseau, bouton
d'envoi et rendu Quest restent leurs tâches respectives.

La prochaine tâche proposée est QUEST-007, sans l'exécuter ici.
