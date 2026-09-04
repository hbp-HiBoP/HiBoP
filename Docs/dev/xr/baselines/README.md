# HiBoP XR P00 — régénération des baselines

Cette procédure régénère et vérifie uniquement les fixtures synthétiques
autorisées et les assets MNI déjà versionnés. Elle ne lit aucun projet HiBoP
local et aucune donnée patient réelle.

## Prérequis

- worktree inspecté ;
- Unity `6000.5.2f1` ;
- éditeur Unity fermé pour les commandes CLI ci-dessous ;
- Unity exécuté hors sandbox selon `AGENTS.md`.

Si l'éditeur est ouvert, utiliser Unity MCP pour les tests et le menu
`Tools > HiBoP > XR > Regenerate P00 Synthetic Golden Buffers` pour le golden.

## 1. Régénérer les golden buffers synthétiques

```powershell
$Project = "C:\HBP\Software\HiBoP"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"
$ResultRoot = Join-Path $Project ".test-results\xr\p00"
New-Item -ItemType Directory -Force -Path $ResultRoot | Out-Null

$Arguments = @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", $Project,
  "-executeMethod", "HBP.Tests.Serialization.P00BaselineGoldenCli.Run",
  "-logFile", (Join-Path $ResultRoot "generate-golden.log"),
  "-forgetProjectPath"
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
exit $Process.ExitCode
```

La sortie versionnée est
`Assets/Tests/Fixtures/XR/Baselines/Expected/golden-buffers.json`. Elle contient
les buffers golden surface, sites, coupe et D5. Elle ne contient ni date,
machine, chemin absolu ou donnée externe.

## 2. Vérifier les fixtures

```powershell
$Arguments = @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", $Project,
  "-runTests", "-testPlatform", "EditMode",
  "-assemblyNames", "HBP.Serialization.Tests",
  "-testCategory", "XR.P00",
  "-testResults", (Join-Path $ResultRoot "p00-fixtures-results.xml"),
  "-logFile", (Join-Path $ResultRoot "p00-fixtures.log"),
  "-forgetProjectPath"
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
exit $Process.ExitCode
```

Ces tests vérifient le manifeste, les hashes/counts MNI, les buffers D0, les
descripteurs D2–D4, l'interpolation D5 et la redaction D6.

## 3. Vérifier les oracles Desktop existants

Utiliser le filtre suivant avec la même commande EditMode :

```text
HBP.Tests.Serialization.Stage4TemporalGridTests;
HBP.Tests.Serialization.ActivityGeneratorFunctionalTests;
HBP.Tests.Serialization.NativeParityWorkflowArtifactTests;
HBP.Tests.Serialization.NativeParityCutVisualTests
```

Ces tests couvrent le sampling temporel, la projection surface, les UV, les
pixels de coupe et la parité du workflow natif.

## 4. Mesurer la projection Desktop

```powershell
$Arguments = @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", $Project,
  "-executeMethod", "HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run",
  "-hbpProjectionOutput", (Join-Path $ResultRoot "projection-product.json"),
  "-hbpProjectionProfile", "Product",
  "-hbpProjectionTimeline", "100",
  "-hbpProjectionRepetitions", "3",
  "-hbpProjectionWorkers", "0",
  "-hbpProjectionBatchSites", "0",
  "-hbpProjectionVolumeInterpolation", "Trilinear",
  "-hbpProjectionFilter", "projection.product-reference.d80",
  "-logFile", (Join-Path $ResultRoot "projection-product.log"),
  "-forgetProjectPath"
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
exit $Process.ExitCode
```

Le rapport local contient des informations machine et des chemins absolus ; il
reste sous `.test-results/`, déjà ignoré par Git. Seules les métriques redacted
du rapport P00 peuvent être publiées.

## 5. Vérifier la stabilité et la confidentialité

Régénérer le golden vers un chemin local avec
`-hbpP00GoldenOutput .test-results/xr/p00/golden-repeat.json`, puis comparer son
SHA-256 au golden versionné. Les fichiers doivent être byte-identiques.

Le scan D6 porte sur `.test-results/xr/p00`, le golden, le manifeste public et
les rapports. Les quatre sentinelles définies dans `D6/fixture.json` doivent y
être absentes. Le fichier D6 source est exclu du scan puisqu'il constitue
l'oracle positif.

## Politique de comparaison

- D0 et D5 : égalité exacte des nombres synthétiques ;
- hashes/counts D1 : égalité exacte ;
- pixels de coupe natifs : tolérance historique d'un octet dans l'oracle
  existant, déjà explicitée par le test ;
- aucune tolérance d'image globale n'est définie par P00 ;
- toute nouvelle tolérance visuelle doit être acceptée par le mainteneur HiBoP.

