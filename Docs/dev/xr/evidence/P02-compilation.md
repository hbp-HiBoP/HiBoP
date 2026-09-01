# Preuves P02 — Contracts

**Date :** 2026-09-01  
**Baseline :** `feature/xr` à `7363ee729015590955194e0e545350becad16bd1`  
**Unity :** `6000.5.2f1` (`eb73d3b415a1`)

## Compilation C# pure

Commande exécutée sur toutes les sources `Runtime/*.cs` depuis un projet temporaire ignoré ciblant `netstandard2.1`, C# 9 et `TreatWarningsAsErrors=true` :

```powershell
dotnet build .test-results\p02-pure\CRNL.HiBoP.Contracts.Pure.csproj --configuration Release --nologo
```

Résultat : **PASS**, `0 Warning(s)`, `0 Error(s)`.

Le scan des sources Runtime ne trouve aucune référence à Unity, HBP Core/Data, RenderModel, Protocol, Newtonsoft, `System.IO`, `DllImport`, `File`, `Directory` ou `Path`. L'asmdef Runtime conserve `references: []`, `allowUnsafeCode: false` et `noEngineReferences: true`. Le test de pureté inspecte aussi les références de l'assembly compilée.

## Tests EditMode Desktop et XR

Les deux exécutions utilisent Unity CLI hors sandbox, `-batchmode -nographics -accept-apiupdate`, `-runTests -testPlatform EditMode -assemblyNames CRNL.HiBoP.Contracts.Tests`, sans `-quit`.

| Projet | Résultat | Total | Passés | Échecs | Ignorés | Durée NUnit |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Desktop `C:\HBP\Software\HiBoP` | PASS | 36 | 36 | 0 | 0 | 0,0857381 s |
| XR `C:\HBP\Software\HiBoP\XR` | PASS | 36 | 36 | 0 | 0 | 0,0785299 s |

Les logs finaux ne contiennent ni `error CS`, ni `Compilation failed`, ni `Test run failed`, ni `Aborting batchmode`. Les résultats XML et logs bruts restent sous `.test-results/unity-cli/` et ne sont pas versionnés.

## Format et intégrité

```powershell
.\Tools\format-code.cmd
git diff --check
```

Résultat final : **PASS**. Le premier lancement du formateur, avant régénération des projets C# Unity, ne trouvait pas les sources du package ; après import Unity, le lancement obligatoire a réussi. La réécriture automatique hors périmètre des icônes Android dans `ProjectSettings/ProjectSettings.asset` a été retirée.

## Limite déclarée

Le smoke Player IL2CPP/AOT reste différé jusqu'à P04, conformément à « dès que P04 le permet » dans P02. Les preuves présentes couvrent la portabilité C# pure, la compilation Unity dans les deux shells et les tests EditMode ; elles ne sont pas présentées comme une preuve IL2CPP.
