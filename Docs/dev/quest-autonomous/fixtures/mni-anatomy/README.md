# Fixture anatomique QUEST-001

Projet `quest-mni-anatomy`, visualisation et colonne `MNI Anatomy`.
Une seule `AnatomicColumn`, aucun patient, groupe, dataset, site, coupe ni vue
enregistrée. La configuration sélectionne explicitement `MNI Grey matter`,
`MeshPart.Both`, `SurfaceRepresentation.Anatomical` et l'IRM `MNI`.
Les six IDs sont fixes et listés dans [manifest.json](manifest.json).

## Préparer depuis le dépôt

Dans PowerShell, depuis la racine de HiBoP :

```powershell
.\Tools\Prepare-QuestAnatomyFixture.ps1
```

Le script résout ses entrées depuis son propre emplacement, vérifie les huit
hashes du manifeste et écrit `.artifacts/quest-001/fixture/quest-mni-anatomy.hibop`.
`-OutputDirectory` accepte un autre dossier ; les chemins relatifs se résolvent
depuis le dépôt, même si le terminal est ailleurs. Aucun alias ni chemin machine
n'est enregistré dans l'archive. Rejouer la commande remplace uniquement cette
archive dans le dossier choisi.

L'archive contient les quatre répertoires exigés par `ProjectManifest.Read`
(`Patients/`, `Groups/`, `Datasets/`, `Visualizations/`) et les deux fichiers texte
versionnés ici. Les trois premiers répertoires restent vides. L'ordre des entrées,
la date ZIP et l'encodage sont fixés ; aucun UUID ni horodatage courant n'est généré.
Les gros assets anatomiques restent ceux de `Assets/Data`, chargés par Desktop.

## Reconstruire sur un autre PC Windows sans `.artifacts`

Après avoir commité puis récupéré les fichiers QUEST-001, aucun ancien fichier
de `.artifacts`, `.test-results` ou `Library` n'est nécessaire. Le dépôt contient
les sources Unity, les assets MNI, les DLL natives Windows, leur verrou
`Tools/NativePlugins.lock.json`, les manifests de packages, le builder et les
sources de cette fixture. Les dépôts natifs voisins ne sont pas nécessaires pour
ce build utilisant les DLL déjà versionnées.

Installer Unity dans la version de `ProjectSettings/ProjectVersion.txt`
(`6000.5.2f1` pour cette validation), le support Windows IL2CPP et sa chaîne de
compilation C++/Windows SDK. Unity doit pouvoir restaurer les packages déclarés
dans `Packages/manifest.json` et `Packages/packages-lock.json` : connexion et accès
aux dépôts de packages requis au premier import. Conserver ces deux fichiers.

Dans PowerShell, depuis la racine du clone, Unity fermé :

```powershell
$questRepository = (Get-Location).Path
$questVersion = ((Get-Content 'ProjectSettings/ProjectVersion.txt' | Select-String '^m_EditorVersion:').Line -split ':', 2)[1].Trim()
$questUnity = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$questVersion/Editor/Unity.exe"
# Adapter seulement questUnity si Unity Hub utilise un autre dossier d'installation.
$questBuildOutput = Join-Path $questRepository '.artifacts/quest-001/player'
$questLogs = Join-Path $questRepository '.test-results/quest-001'
New-Item -ItemType Directory -Force -Path $questLogs | Out-Null
.\Tools\Prepare-QuestAnatomyFixture.ps1
$questBuildArgs = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"{0}"' -f $questRepository), '-buildTarget', 'Win64', '-executeMethod', 'HBP.Dev.HBPBuilder.BuildFromCommandLine', '-buildOutput', ('"{0}"' -f $questBuildOutput), '-scriptingBackend', 'IL2CPP', '-logFile', ('"{0}"' -f (Join-Path $questLogs 'build.log')), '-forgetProjectPath')
$questBuildProcess = Start-Process -FilePath $questUnity -ArgumentList $questBuildArgs -Wait -PassThru -WindowStyle Hidden
if ($questBuildProcess.ExitCode -ne 0) { throw "Build Unity en échec : $($questBuildProcess.ExitCode). Voir $questLogs/build.log" }
$questPlayerVersion = ((Get-Content 'ProjectSettings/ProjectSettings.asset' | Select-String '^  bundleVersion:').Line -split ':', 2)[1].Trim()
$questPlayer = Join-Path $questBuildOutput "HiBoP.$questPlayerVersion.win64/HiBoP.exe"
& $questPlayer -pf (Join-Path $questRepository '.artifacts/quest-001/fixture/quest-mni-anatomy.hibop') -v 'MNI Anatomy'
```

Avec Codex sous Windows, la commande qui démarre Unity s'exécute hors sandbox,
comme prescrit dans AGENTS.md. Les chemins entre guillemets prennent en charge un
clone situé dans un dossier contenant des espaces. Le nom du Player suit la
version du checkout ; après intégration d'une 6.1.1, ne pas chercher le dossier 6.1.0.

La fixture est déterministe. Le Player peut être reconstruit fonctionnellement,
mais un hash binaire identique n'est pas garanti : le builder inscrit notamment
la date du build. Les logs/XML originaux sont des preuves historiques locales,
pas des entrées de compilation ; relancer les vérifications produit de nouvelles
preuves. Les résumés, hashes et la validation du propriétaire sont conservés dans
le rapport/manifeste versionnés. Le build complet sur un second PC n'a pas été
exécuté dans cette tâche ; la préparation de fixture a été testée dans une racine
neuve déplacée, sans recopier aucun artefact.

## Ouvrir la version Windows livrée pour cette tâche

Depuis la racine du dépôt, après la préparation :

```powershell
& '.\.artifacts\quest-001\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf "$PWD\.artifacts\quest-001\fixture\quest-mni-anatomy.hibop" -v 'MNI Anatomy'
```

`CommandLineReader` ouvre le fichier avec `-pf`, puis la visualisation nommée avec
`-v`. Aucun menu supplémentaire n'est nécessaire. Attendre la fin du chargement :
une visualisation anatomique complète avec une seule colonne doit apparaître.
La confirmation visuelle par le propriétaire reste distincte des tests automatisés.
Les chemins absolus de livraison et les hashes du Player sont dans le
[rapport](../../reports/QUEST-001.md) et son manifeste de preuves.

## Provenance et conventions

- D14 autorise l'anatomie MNI déjà versionnée. Les GII de
  `Assets/Tests/Fixtures/Native/Meshes` sont de petites surfaces **synthétiques**
  générées par `generate_native_test_fixtures.py`, malgré leurs noms MNI : elles
  ne constituent pas le cerveau complet. Les projets de test existants sont soit
  vides, soit associés à des patients et plusieurs colonnes.
- La référence retenue est donc `Assets/Data/IRM/MNI.nii`, `MNI.trm` et les quatre
  GII gris/blanc de `Assets/Data/Meshes`. Leur provenance garantie ici est le
  contenu du dépôt à `b230166204024b5dd350d8fe897aa854160ff384`. L'origine scientifique
  amont exacte du template n'est pas redéduite de son seul nom.
- Aucun fichier n'est repris de `feature/xr`. Aucun fichier de patient ni signal
  réel ou synthétique n'est ajouté. Le volume de référence n'est pas qualifié de
  synthétique ; l'absence de données patient concerne les entrées de cette fixture.
- `MNIObjects.LoadDataAsync` charge NIfTI et GII/TRM, inverse les triangles de
  chaque hémisphère, clone la gauche puis ajoute la droite et calcule les normales.
  Le test appelle directement et attend ce chargeur existant.
- Les transformations restent celles du Desktop : distances anatomiques en mm,
  TRM décrit dans le manifeste et conversion native/Unity existante
  (`ReferenceSystemConversion.InvertX = true`, adaptation du winding).
  Aucune conversion Quest mm→m, caméra, package XR ou ressource réseau n'est ajoutée.
- Les hashes texte portent sur UTF-8 sans BOM avec fins de ligne LF ; celui du
  NIfTI porte sur les octets bruts. Cette convention tolère `core.autocrlf` sans
  modifier les données. L'archive embarque elle aussi du texte UTF-8/LF.

## Vérification Unity ciblée

Exécuter `HBP.Tests.Serialization.QuestAnatomyFixtureTests` en EditMode. Le test
du projet construit une archive temporaire à partir des deux sources texte,
utilise le vrai lecteur de projet et attend sa validation. Le test anatomique
charge les assets complets par `MNIObjects`, contrôle les deux hémisphères et
l'assemblage gauche + droite, pour les matières grise et blanche.
Ces tests ne constituent pas une observation du rendu dans le Player.
