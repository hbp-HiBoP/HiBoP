# P01 — preuves de validation

- **Date :** 2026-09-01
- **Baseline :** `ae911f8bb5361e4f6da1ea6f992744a3ac4c4687`
- **Unity :** `6000.5.2f1 (eb73d3b415a1)`
- **Hôte :** Windows, Unity CLI hors sandbox

## Résultats

| Vérification | Desktop | XR |
| --- | --- | --- |
| Résolution des trois packages locaux | Réussie, trois entrées `source: local` | Réussie, trois entrées `source: local` |
| Tests EditMode partagés | 3/3 réussis | 3/3 réussis |
| Compilation Player | Windows x64 IL2CPP réussie | Android IL2CPP réussie |

Le contrôle `Tools/Validate-XRTopology.ps1` réussit. Il vérifie notamment les versions Unity, les chemins UPM, les squelettes de packages, la couverture `.gitignore` des dossiers générés dont `XR/.utmp/`, les artefacts, la limite de 50 MiB, l'absence de copie des sources HiBoP et la restriction des triggers GitHub Actions à `workflow_dispatch` ou `release`.

## Artefacts locaux ignorés

### HiBoPXR Android

- Fichier : `.artifacts/xr/HiBoPXR-P01.apk`
- Taille : `14 355 532` octets
- SHA-256 : `D3355B1CF037B8032C1995DEAFE5E39A6A33229033E129E19A79641B852F5A53`
- Rapport Unity : `Build Finished, Result: Success`
- La scène vide de build a été supprimée après production.

### HiBoP Desktop Windows

- Fichier principal : `.artifacts/xr/desktop-build/HiBoP.6.1.0.win64/HiBoP.exe`
- Taille : `667 648` octets
- SHA-256 : `AAB370FC59ED240ED50AF0C9461D0D3E83548253AC11C820420D4D37BBA64250`
- Backend : IL2CPP
- Rapport Unity : `Build Finished, Result: Success`

Plugins natifs vérifiés dans `HiBoP_Data/Plugins/x86_64` :

| Plugin | Occurrences | Taille | SHA-256 |
| --- | ---: | ---: | --- |
| `hbp_core.dll` | 1 | 1 378 816 | `98AF2B1924F3C7A19A8A98400FA25CA63E8C93AE066118696E461E2D8B47947B` |
| `hbp_math.dll` | 1 | 246 784 | `D16619C4A967E0771505E472E898E1E7CD8420FAC699B43BDEBB3A8BB7836E9B` |
| `EEGFormat.dll` | 1 | 1 005 056 | `3B886CF1A31C67E7B8DDB2B38E93A634B56F8694F80DEB3D9312839E2B69D448` |

Aucun ancien plugin `hbp_export`, OpenCV, Boost ou runtime MSVC supplémentaire n'a été trouvé dans le Player.

## Contrôles de dépôt

- `Tools/format-code.cmd` : 7 fichiers C# examinés, aucun changement demandé.
- `Tools/Validate-XRTopology.ps1` : réussite.
- `git diff --check` : réussite.
- Aucun `Assets/Scripts/HBP` sous `XR/`.
- Aucun fichier HiBoP existant dupliqué par hash sous `XR/` ou `Shared/Packages/`.
- Aucun fichier LFS et aucune réécriture d'historique.
- Le workflow XR ne possède que le déclencheur manuel `workflow_dispatch`; aucun workflow du dépôt n'est déclenché sur `push`, `pull_request` ou planification.
- Les effets de sérialisation et le stamp `BuildInfo.json` produits par les builds locaux ont été restaurés.

Les logs et XML détaillés restent sous `.artifacts/xr/` et ne sont pas versionnés.

## Observation non bloquante

L'import Desktop journalise un échec de parsing existant dans `ProjectSettings/TagManager.asset` à la ligne 48. La compilation, les tests et le Player Windows terminent néanmoins avec succès ; P01 ne modifie pas ce fichier.
