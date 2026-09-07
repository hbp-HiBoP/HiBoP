# Rapport QUEST-001 — Référence de travail et fixture anatomique

## Résultat

Les projets de test existants étaient soit vides, soit associés à des patients
synthétiques et plusieurs colonnes. Une préparation dédiée produit maintenant
`quest-mni-anatomy.hibop`, avec une visualisation `MNI Anatomy`, une seule colonne
anatomique et six IDs fixes. Elle utilise le cerveau MNI complet de `Assets/Data`,
sélectionne les deux hémisphères de matière grise et n'ajoute aucun patient,
groupe, dataset, site, signal, coupe ou vue enregistrée.

La préparation est indépendante des chemins de cette machine et refuse des
sources dont les hashes ne correspondent plus au manifeste. Aucun code produit,
prefab, caméra, package XR ou réseau n'est modifié.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI. Manuel : VALIDE.
- Branche conservée : `feature/xr-autonomous`, HEAD
  `b230166204024b5dd350d8fe897aa854160ff384`, un seul worktree
  `C:/HBP/Software/HiBoP`, aucun changement suivi ou non suivi initial.
- `origin/develop` local : `8b868a5b851ed7c920b5847bc83f79114bd0f166` ;
  `feature/xr` local : `eb26c323e2bdf6c138f9e9e3c02f3e9796cae249`.
  Aucun fetch, changement/création de branche, restauration historique, commit
  ou push. Aucun dépôt voisin modifié.
- Unity fermé au départ ; tests/build CLI avec Unity `6000.5.2f1`, hors sandbox
  selon AGENTS.md. Application Desktop `6.1.0`, URP `17.5.0`.
  Les binaires natifs et leur verrou sont identifiés dans le manifeste de preuves.
- D01/D14 et [fiche QUEST-001](../tasks/QUEST-001.md) appliquées. Les GII de
  `Assets/Tests/Fixtures/Native/Meshes` sont synthétiques et ne représentent pas
  le cerveau complet. Aucune reprise depuis `feature/xr`.
- [Manifeste de preuves](../evidence/QUEST-001/manifest.json) : inventaire Git,
  sources finales non commitées, commandes, résultats et SHA-256 des artefacts.
  Les logs, XML et Player sous `.test-results/` et `.artifacts/` sont **locaux et
  ignorés par Git** ; la recette, les métadonnées de fixture et les preuves
  résumées sont destinées à être versionnées.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Changement et règle à vérifier |
| --- | --- | --- |
| 1 | [MNI Anatomy.visualization](../fixtures/mni-anatomy/MNI%20Anatomy.visualization) | Une seule `AnatomicColumn`, patients vides, `Mesh Part=2`, `Mesh=MNI Grey matter`, `MRI=MNI`, représentation anatomique et IDs fixes. |
| 2 | [Prepare-QuestAnatomyFixture.ps1](../../../../Tools/Prepare-QuestAnatomyFixture.ps1) | Résolution depuis le dépôt, vérification préalable des sources, ZIP avec les quatre dossiers structurels requis, encodage/date/ordre déterministes. |
| 3 | [Manifeste de fixture](../fixtures/mni-anatomy/manifest.json) | Huit sources identifiées, hashes texte UTF-8/LF et NIfTI brut ; anatomie existante, sans reprise XR ni fichier patient. |
| 4 | [QuestAnatomyFixtureTests](../../../../Assets/Tests/EditMode/HBP.Serialization.Tests/QuestAnatomyFixtureTests.cs) | Import par le lecteur de projet réel ; appel attendu du vrai `MNIObjects.LoadDataAsync` par réflexion sans modifier son API publique fire-and-forget. Les compteurs vérifient l'assemblage des hémisphères. |
| 5 | [Recette](../fixtures/mni-anatomy/README.md) | Ouverture via les arguments existants `-pf` puis `-v` de `CommandLineReader`, conventions et limites des preuves explicites. |

Le flux est : deux sources JSON → archive `.hibop` → lecteur de projet →
visualisation/configuration existantes → assets MNI de l'application via
[MNIObjects.LoadDataAsync](../../../../Assets/Scripts/HBP/Core/Object3D/MNIObjects.cs).
Le TRM, l'inversion des triangles, l'ordre gauche puis droite et le calcul des
normales sont conservés ; aucune conversion de présentation Quest n'est ajoutée.

## Vérifications effectuées

| ID | Scénario / commande | Résultat / preuve |
| --- | --- | --- |
| T1 | `git branch --show-current`, `git rev-parse HEAD origin/develop feature/xr`, `git worktree list --porcelain`, `git status --porcelain=v1 --untracked-files=all` | REUSSI, exit 0 ; état initial ci-dessus. |
| T2 | `Tools/Prepare-QuestAnatomyFixture.ps1`, puis même commande avec `-OutputDirectory .artifacts/quest-001/replay` | REUSSI, exit 0 à chaque fois ; huit sources vérifiées, archives identiques de 1 853 octets, SHA-256 `0bd800ec656ed8be03777b85457e47a749bbfd5b450d368185baab1b0e32a61a`. |
| T3 | Inspection ZIP et comparaison de ses deux contenus texte aux sources LF ; vérification des chemins du manifeste | REUSSI ; quatre dossiers dont trois vides, une visualisation/colonne, aucun patient, tous les chemins relatifs au dépôt ; `.test-results/quest-001/preparation.json`. |
| T4 | Unity EditMode, filtre `HBP.Tests.Serialization.QuestAnatomyFixtureTests` | REUSSI, exit 0, **2/2** tests ; import sans récupération structurelle, six IDs conservés, MRI et quatre GII chargés. XML/log sous `.test-results/quest-001/`. |
| T5 | `Tools/format-code.cmd` | REUSSI, exit 0 après restauration NuGet hors sandbox et génération du projet C# par Unity ; `.test-results/quest-001/format.log`. |
| T6 | Build Windows via `HBP.Dev.HBPBuilder.BuildFromCommandLine`, IL2CPP | REUSSI, exit 0, `Build Finished, Result: Success.` ; Player livré au chemin ci-dessous, assets anatomiques copiés identiques aux sources ; `.test-results/quest-001/build.log`. |
| T7 | Comparaison Git finale à T1, contrôle des liens/chemins et `git diff --check` | REUSSI ; seuls les ajouts QUEST-001 et sa ligne du registre subsistent. Les réécritures Unity de BuildInfo, ProjectSettings et des deux assets URP sont rétablies à leur état initial ; diff généré conservé localement pour traçabilité. |
| T8 | Repréparation dans une racine neuve sous `.codex-temp/quest-001-relocation-*`, contenant uniquement les huit sources, le manifeste et le script ; aucun ancien artefact copié | REUSSI, 2026-09-07, exit 0 ; mêmes 1 853 octets et SHA-256 que T2. Résultat local : `.test-results/quest-001/relocation.json`. Les assets et DLL requis pour reconstruire le Player sont suivis dans Git. Build sur un second PC non exécuté. |

Mesures produites par T4 :

| Surface | Gauche : sommets / triangles | Droite : sommets / triangles | Assemblée : sommets / triangles |
| --- | --- | --- | --- |
| Matière grise | 34 733 / 69 470 | 34 371 / 68 746 | 69 104 / 138 216 |
| Matière blanche | 33 036 / 66 068 | 33 263 / 66 522 | 66 299 / 132 590 |

Les erreurs du premier essai de compilation étaient limitées à une référence
`Ionic.Zip` indisponible dans l'assembly de test ; le test utilise désormais
`System.IO.Compression`. Ce premier essai n'est pas compté comme réussi.
Les avertissements Unity de sérialisation/d'obsolescence préexistants n'ont pas
été corrigés dans cette tâche.

Commande des tests, exécutée depuis `C:\HBP\Software\HiBoP` hors sandbox :

```powershell
$questUnityArgs = @('-batchmode', '-nographics', '-projectPath', 'C:\HBP\Software\HiBoP', '-runTests', '-testPlatform', 'EditMode', '-testFilter', 'HBP.Tests.Serialization.QuestAnatomyFixtureTests', '-testResults', 'C:\HBP\Software\HiBoP\.test-results\quest-001\editmode-results.xml', '-logFile', 'C:\HBP\Software\HiBoP\.test-results\quest-001\editmode.log', '-forgetProjectPath')
$questUnityProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe' -ArgumentList $questUnityArgs -Wait -PassThru -WindowStyle Hidden
$questUnityProcess.ExitCode
```

Commande du build, même exécutable Unity et mêmes options `Start-Process`
(`-Wait -PassThru -WindowStyle Hidden`, hors sandbox) :

```powershell
$questBuildArgs = @('-batchmode', '-nographics', '-quit', '-projectPath', 'C:\HBP\Software\HiBoP', '-buildTarget', 'Win64', '-executeMethod', 'HBP.Dev.HBPBuilder.BuildFromCommandLine', '-buildOutput', 'C:\HBP\Software\HiBoP\.artifacts\quest-001\player', '-scriptingBackend', 'IL2CPP', '-logFile', 'C:\HBP\Software\HiBoP\.test-results\quest-001\build.log', '-forgetProjectPath')
$questBuildProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe' -ArgumentList $questBuildArgs -Wait -PassThru -WindowStyle Hidden
$questBuildProcess.ExitCode
```

## Validation manuelle reçue

Fixture préparée :
`C:\HBP\Software\HiBoP\.artifacts\quest-001\fixture\quest-mni-anatomy.hibop`.
Player construit et présent :
`C:\HBP\Software\HiBoP\.artifacts\quest-001\player\HiBoP.6.1.0.win64\HiBoP.exe`.
Le lancement graphique et le rendu Player n'ont pas été observés par l'agent.
Le propriétaire a confirmé le 2026-09-07 dans cette conversation :
« Je confirme que HiBoP 6.1.0 s'ouvre bien avec le cerveau MNI ».
Ce retour valide M1 ; les contrôles d'une colonne et des deux hémisphères sont
également couverts par les preuves techniques ci-dessus. Aucun casque requis.

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Ouvrir le Player 6.1.0 livré avec la fixture MNI (commande fournie ci-dessous). | Le cerveau anatomique MNI attendu apparaît, avec les deux hémisphères et une seule colonne `MNI Anatomy`. | VALIDE — confirmation du propriétaire le 2026-09-07, citée ci-dessus. |

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-001\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-001\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy'
```

## Décisions, limites et suite

Aucune décision supplémentaire demandée : D14 identifie la référence MNI.
L'origine scientifique amont exacte du template n'est pas déduite de son nom ;
les octets du dépôt sont la provenance établie ici. L'en-tête NIfTI observé porte
`spm - algebra`, `aux_file=none` ; les métadonnées GII identifient la bibliothèque
GIFTI. Aucun enregistrement patient n'entre dans le projet créé.

Les tests démontrent le chargement technique de l'anatomie et du projet ; le
retour du propriétaire confirme l'ouverture graphique du cerveau attendu.
Cela ne constitue pas une nouvelle validation scientifique du template.
QUEST-001 ne qualifie aucun casque/transfert/calcul Android. La prochaine tâche
proposée est QUEST-002, sans l'exécuter ; la validation M1 de J0 est acquise.

## Reprise sur une autre machine

La [recette de reconstruction](../fixtures/mni-anatomy/README.md#reconstruire-sur-un-autre-pc-windows-sans-artifacts)
recrée la fixture et le Player depuis le checkout, avec des chemins calculés à
partir de sa racine. Aucun contenu ancien de `.artifacts` n'est une entrée requise.
Les sources ajoutées doivent être incluses dans le commit de la tâche. Unity,
la chaîne IL2CPP et les packages restent des prérequis externes ; les DLL natives
Windows et les assets MNI sont déjà suivis dans Git. Les binaires reconstruits ne
sont pas garantis identiques octet par octet, contrairement à la fixture vérifiée.
Les logs historiques locaux ne se reconstituent pas à l'identique ; leurs
résumés/hashes et la confirmation manuelle restent dans les preuves versionnées.

Lors de ce suivi documentaire du 2026-09-07, `git fetch origin master develop`
a été exécuté pour répondre à la question sur le hotfix. Aucune fusion ni commit
n'a été effectué. Les preuves 6.1.0 ci-dessus restent datées de ce build : une
intégration ultérieure ne les transforme pas en qualification d'une 6.1.1.

Précision après confirmation du propriétaire : le tag distant annoté `6.1.1`
(`ff7b0524857b1a00adef13139f33c788a77f7212`) désigne bien le hotfix
`3c72c260c1b7c70675e2ae482adcd87d6e282fde` (relais Cloudflare).
Ce commit est déjà intégré dans `develop` par `8b868a5b8` et est ancêtre de la
branche QUEST actuelle, vérifié par `git merge-base --is-ancestor` (exit 0).
Le Player testé inclut donc déjà ce correctif. `bundleVersion` est resté à
`6.1.0` dans les sources ; le tag Git ne modifie pas automatiquement ce champ.
Aucun merge ni changement de version n'est nécessaire pour récupérer ce code
dans QUEST-001. Les preuves conservent le numéro effectivement affiché au build.
