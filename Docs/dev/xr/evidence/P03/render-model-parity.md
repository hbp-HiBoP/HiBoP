# P03 — preuve de parité RenderModel synthétique

## Portée

Cette preuve reconstruit le profil synthétique des goldens P00 nécessaire à P03. Elle ne constitue pas une validation visuelle sur données patient.

- branche : `feature/xr` ;
- commit parent : `43e589db05614ca64f0d4c6c34623e438037a7b9` ;
- Unity : `6000.5.2f1` ;
- datasets automatiques : D0, D5, D6 ;
- sorties brutes : `.artifacts/xr/p03/` (non versionnées) ;
- manifeste brut : `.artifacts/xr/p03/manifest.json`.
- baseline approuvée et non réécrite par les tests : `Docs/dev/xr/fixtures/P00/synthetic-render-goldens.json`.

## Commandes exécutées

Tests purs du package :

```text
Unity.exe -batchmode -nographics -accept-apiupdate -projectPath C:\HBP\Software\HiBoP -runTests -testPlatform EditMode -assemblyNames CRNL.HiBoP.RenderModel.Tests -testResults .test-results/unity-cli/p03-render-model-final-results.xml -logFile .test-results/unity-cli/p03-render-model-final.log -forgetProjectPath
```

Reconstruction des goldens :

```text
Unity.exe -batchmode -nographics -accept-apiupdate -projectPath C:\HBP\Software\HiBoP -runTests -testPlatform EditMode -assemblyNames HBP.Serialization.Tests -testFilter HBP.Tests.Serialization.P03RenderModelGoldenTests -testResults .test-results/unity-cli/p03-golden-final-results.xml -logFile .test-results/unity-cli/p03-golden-final.log -forgetProjectPath
```

Le même assembly pur a été exécuté dans `C:\HBP\Software\HiBoP\XR`, avec les résultats dans `p03-xr-render-model-final-results.xml`. Le contrôle ciblé des frontières d'assemblies est dans `p03-integrity-final-results.xml`.

## Résultats

| Suite | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `CRNL.HiBoP.RenderModel.Tests` — Desktop | 14 | 14 | 0 | 0 |
| `CRNL.HiBoP.RenderModel.Tests` — XR | 14 | 14 | 0 | 0 |
| `P03RenderModelGoldenTests` | 2 | 2 | 0 | 0 |
| frontière d'assemblies ciblée | 1 | 1 | 0 | 0 |

## Hashes de parité

| Golden | Octets | SHA-256 Desktop | SHA-256 RenderModel | Parité |
| --- | ---: | --- | --- | --- |
| D0 surface | 168 | `19149b6a21d4f9df69bd500deacae220caeafb4f480c410de6021c6c7d0e5ea1` | identique | exacte |
| D0 sites | 60 | `963960768007cee67c2af64009575400ad18fa56ba9dd531d3fc490461d2ab17` | identique | exacte |
| D0 géométrie de coupe | 168 | `19149b6a21d4f9df69bd500deacae220caeafb4f480c410de6021c6c7d0e5ea1` | identique | exacte |
| D0 overlay RGBA8 | 16 | `95f19c05a9b81c838492ff9b0b577cda63c89d28acf61bd00cfdc982557adab0` | identique | exacte |
| D0 image PNG | 82 | `f664a5439f7d8199680aab3cbd7f4c62dcdedc3aff314633e717ec9272d3ba69` | identique | exacte |
| D5 surface sample-and-hold | 32 | `86337c3bb82fc0a4fffbca319b39f9c89bf1843b1beddd67c4d50b77a2903938` | identique | exacte |
| D5 site linéaire | 16 | `32c252c88e2627a87d75bdcecbd42325be8df3858f858f3f679dcfa91e0fbc21` | identique | exacte |

Le D5 utilise `index = 0`, `TemporalAlpha = 0.75`, et les valeurs `0`/`10`. Le chemin Desktop exécute `HBP.Core.Data.TemporalSample.Evaluate` ; le chemin indépendant exécute `RenderTemporalSample.EvaluateLinear` ; les deux donnent `7.5` avec une différence `<= 1e-6`. Surface et coupe déclarent `SampleAndHold` et conservent l'échantillon inférieur. L'oracle indépendant reconstruit exactement les deux streams UV Desktop. Une divergence entre leurs sentinelles d'activité, ou une sentinelle hors de `{0,1}`, est maintenant rejetée plutôt que normalisée silencieusement.

Chaque sortie Desktop et RenderModel est comparée au hash P00 versionné avant d'être comparée à l'autre. Le test ne modifie jamais cette baseline. Pour le PNG, l'image candidate est reconstruite depuis les pixels du `CutOverlayFrame`, et non depuis un clone du tableau Desktop.

## Ownership, atomicité et pureté

- une mutation du tableau source après `CopyFrom` ne modifie pas le buffer capturé ;
- l'API publique ne publie ni tableau, ni `Memory`, ni `Span` adossé au stockage interne ; `ToArray` crée une copie explicite ;
- tous les types publics sont sans setter public et sans propriété tableau mutable ;
- le package compilé ne référence ni Unity, ni `HBP.*`, ni IO/native/serializer ;
- un bundle incomplet ou dupliquant une colonne est rejeté ;
- identité de coupe, colonne, sample et révision source des overlays doivent correspondre au résultat et au bundle atomiques ; doublons et dimensions incohérentes sont rejetés.

## D6

La fixture utilise des noms sentinelles côté objets Desktop. Après génération, le test scanne récursivement chaque fichier sous `.artifacts/xr/p03/` et échoue si les octets UTF-8 d'une sentinelle sont présents. Les IDs transmis sont opaques.

## Limites explicites

Cette preuve automatique ne couvre pas la perception visuelle sur D1–D4 réels, les shaders P05, les différences GPU/plateforme ou les performances Quest. Ces validations restent manuelles ou appartiennent aux paquets ultérieurs.
