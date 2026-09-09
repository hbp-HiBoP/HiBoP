# Rapport QUEST-015 — Build natif Android reproductible et CI

## Livraison GitHub après la release 0.4.0 — 2026-09-09

PowerShell 7.6.6 et GitHub CLI 2.100.0 ont été installés via WinGet officiel.
Après authentification, `Tools/update-native-plugins.cmd` a déclenché les trois
workflows sur les derniers commits de `master`, téléchargé les dix artefacts,
vérifié leurs manifestes et installé l’ensemble avec sauvegarde et nouveau lock.
La requête `hibop-native-20260909-101651-87c5056d` est terminée avec succès.

- hbp_core : [ffb7686 / run 34339388439](https://github.com/hbp-HiBoP/hbp_core/actions/runs/34339388439), quatre plateformes.
- EEGFormat : [run 34339376344](https://github.com/hbp-HiBoP/EEGFormat/actions/runs/34339376344), trois plateformes.
- hbp_math : [run 34339402353](https://github.com/hbp-HiBoP/hbp_math/actions/runs/34339402353), trois plateformes.

Le plugin Android installé provient désormais de GitHub : SHA-256
`96ea8cd31ac5f572560403fc177f53f2dc56009780ad9de91c0d63ddb74c4a8e`, 1,450,608 octets.
Les logs authentifiés confirment la double compilation identique, les neuf
contrôles de l’outillage Android et les 15 tests hbp_core sur chacun des trois OS
Desktop. Une nouvelle inspection locale confirme 226 exports et l’alignement
ELF 16 Kio. Les 16 fichiers des dix artefacts correspondent tous au lock.

Les 25 tests Unity de configuration passent avec ces nouveaux plugins.
Les builds Windows et Quest réussissent, avec zéro erreur. Le contenu de l’APK
et des plugins Windows correspond aux nouveaux hashes du lock ; `zipalign`
valide l’APK à 16 Kio. Les warnings existants sont détaillés dans les rapports.
Les sorties sont sous `.artifacts/quest-015/ci-update`.

Le premier build Quest a échoué à l’initialisation Gradle (connexion locale Java).
La reprise utilise `JDK_JAVA_OPTIONS=-Djdk.net.unixdomain.tmpdir=...` avec le
dossier court `.artifacts/quest-015/java`, comme les scripts Quest existants.
Le chargement, la version, l’initialisation et la fermeture de la DLL Windows
packagée ont également été vérifiés via ctypes, sans erreur native.

Une correction de `.gitattributes` préserve les octets des payloads natifs lors
des checkouts Git : `core.autocrlf=true` pouvait auparavant transformer les XML
signés des bundles macOS. Les `.meta` Unity conservent leur traitement existant.

[Preuve de cette livraison](../evidence/QUEST-015/ci-update.json).
Les sections suivantes conservent la qualification initiale et la comparaison
historique ; leurs anciens SHA et restrictions d’accès ne décrivent plus le
plugin actuellement installé. Aucun test runtime sur casque ni ouverture
interactive du Player Desktop n’a été effectué lors de cette livraison.

## Qualification locale initiale

Le `hbp_core` complet est compilé en Release Android ARM64 depuis le commit
scientifique déjà épinglé sur Desktop, puis installé dans
`Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so`. Deux builds propres avec
l’outillage épinglé donnent le même SHA-256 :
`15a0029148c29b25f9b0fc7a89b3a2096a055e6bc7d3f641b3c5292ae9d2c501`
(1 450 768 octets). Les 226 exports publics correspondent à l’ABI Desktop.

À la précision du propriétaire du 2026-09-09, la voie de livraison future est
également préparée dans GitHub : `hbp_core/native.yml` propose `android`, l’inclut
dans `all`, reconstruit deux fois, vérifie l’égalité des binaires et leurs
exports/dépendances avant d’envoyer l’artefact du run. L’updater HiBoP attend
maintenant dix artefacts : quatre pour `hbp_core`, trois pour chacune des deux
autres bibliothèques. Il les valide et les installe dans sa transaction existante.

Il n’y a aucun changement de calcul, de CMakeLists.txt ou d’artefact Desktop,
aucun port EEGFormat et aucune qualification d’exécution native sur Quest.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI pour la compilation,
  reproductibilité, tests locaux et packaging. Manuel : NON_REQUIS.
  Une comparaison de l’artefact GitHub fourni ensuite par le propriétaire est
  consignée ci-dessous ; les logs et le statut du run privé ne sont pas accessibles.
- HiBoP : `feature/xr-autonomous`, HEAD `caa2c518209577359edf6712abe8c85c64e4b204`.
- hbp_core : `develop`, HEAD `cf4400b168b120641aa69b7120e81a66a899cbd3`.
- Les deux checkouts étaient propres au départ. Changements livrés non commités,
  identifiés par fichiers/hashes et patches dans le [manifeste](../evidence/QUEST-015/manifest.json).
- Source scientifique effective : `1f26946e4d4e95523e637a21be70db0d1af6db8c`,
  issue de `Tools/NativePlugins.lock.json`. Son arbre Git est
  `de89eb89fb1411fef741fe672233ff829e767e58`, exactement celui du HEAD hbp_core
  initial. Le build utilise une archive Git de ce SHA, jamais le checkout sale.
- Les exécutables Windows, Linux et macOS de `hbp_core` correspondent aux hashes
  du lock Desktop. Les XML des bundles macOS ont des fins de ligne CRLF dans ce
  checkout Windows ; leur hash brut diffère du lock historique. Ils ne sont pas
  utilisés pour établir l’identité du code scientifique et n’ont pas été modifiés.
- Outillage : Unity `6000.5.2f1`, NDK `27.2.12479018` (r27c), Clang `18.0.3`,
  CMake SDK `3.22.1-g37088a8-dirty`, Ninja `1.10.2`, API `32`, ABI `arm64-v8a`,
  STL `c++_static`. Le profil Quest a un minimum Android SDK 32.
- L’artefact ajouté au lock est explicitement `origin: local`, `runId: null`,
  `runUrl: null`. Il n’est pas attribué au précédent run GitHub Desktop.
- Dépendance QUEST-003 : profils DesktopWindows/Quest et point d’entrée de build
  existants réutilisés. Leurs preuves anciennes ne servent pas de preuve native.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle / risque |
| --- | --- | --- |
| 1 | [Invoke-HbpCoreAndroidBuild.ps1](../../../../../hbp_core/tools/Invoke-HbpCoreAndroidBuild.ps1), [AndroidBuild.json](../../../../../hbp_core/tools/AndroidBuild.json) | Archive scientifique immuable, versions épinglées, dossiers propres, date et chemins de debug normalisés, bibliothèque complète. Aucun overlay du CMake/source courant. |
| 2 | [native.yml](../../../../../hbp_core/.github/workflows/native.yml), [Invoke-HbpCoreBuild.ps1 / Get-SourceCommit](../../../../../hbp_core/tools/Invoke-HbpCoreBuild.ps1) | Séparation du commit d’outillage et du SHA scientifique ; deux builds Android obligatoires. La provenance Desktop lit le vrai checkout plutôt que `GITHUB_SHA`, qui peut désigner le workflow. Les trois jobs Desktop sont inchangés. |
| 3 | [Update-NativePlugins.ps1](../../../../Tools/Update-NativePlugins.ps1), [configuration](../../../../Tools/NativePlugins.json), [lock](../../../../Tools/NativePlugins.lock.json) | Dix artefacts attendus, même SHA source par bibliothèque, métadonnées Android et ELF64/AArch64 contrôlés ; import local initial sans remplacer Desktop. |
| 4 | [Test-NativePluginAndroidUpdater.ps1](../../../../Tools/Test-NativePluginAndroidUpdater.ps1), [workflow de tests](../../../../.github/workflows/native-plugin-tools.yml) | Exécution du vrai flux d’orchestration avec GitHub simulé ; corruptions, échecs après copie/écriture du lock, reprise d’une DLL absente, Unity ouvert et provenance locale. |
| 5 | [importer Android](../../../../Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so.meta), [HBPBuildProfiles.Validate](../../../../Assets/Scripts/HBP/Dev/Editor/HBPBuildProfiles.cs), [Test-QuestApk.ps1](../../../../Tools/Test-QuestApk.ps1) | Android/ARM64 uniquement, Any Platform et Editor désactivés. Le guard autorise seulement ce chemin natif ; l’APK doit contenir exactement le binaire épinglé. |

L’ABI est vérifiée par [Test-HbpCoreAbi.ps1](../../../../../hbp_core/tools/Test-HbpCoreAbi.ps1).
[Test-HbpCoreAndroidArtifact.ps1](../../../../../hbp_core/tools/Test-HbpCoreAndroidArtifact.ps1)
contrôle ELF64 little-endian/AArch64, SONAME, dépendances, absence de TEXTREL/RPATH/RUNPATH
et alignement des segments LOAD. `libc++` est statique et masqué ; aucun runtime
C++ partagé supplémentaire n’est livré.

Deux corrections adjacentes sont nécessaires à la fiabilité de l’updater : une
DLL absente après un renommage interrompu ne bloque plus sa propre restauration,
et la reprise vérifie Unity avant toute restauration. La première installation
Android sauvegarde explicitement l’absence du fichier afin de pouvoir revenir
à cet état. Les `.meta` sont conservés, y compris dans les tests de rollback.

## Commandes de reproduction

Depuis `C:\HBP\Software\HiBoP`, PowerShell 7, avec des dossiers de sortie neufs :

```powershell
$androidPlayer = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$env:PATH = "$androidPlayer\SDK\cmake\3.22.1\bin;" + $env:PATH
$source = (Get-Content Tools/NativePlugins.lock.json -Raw | ConvertFrom-Json).libraries |
    Where-Object name -eq 'hbp_core'
../hbp_core/tools/Invoke-HbpCoreBuild.ps1 -Platform Android -SourceCommit $source.commit `
    -AndroidNdk "$androidPlayer\NDK" -BuildDir ../hbp_core/out/reproduce-quest-015 `
    -PackageDir ../hbp_core/out/package/reproduce-quest-015
../hbp_core/tools/Test-HbpCoreAndroidBuild.ps1 `
    -PackageDir ../hbp_core/out/package/reproduce-quest-015 `
    -SourceRoot ../hbp_core/out/reproduce-quest-015/source -AndroidNdk "$androidPlayer\NDK"
./Tools/Test-NativePluginUpdater.ps1
./Tools/Test-NativePluginAndroidUpdater.ps1
```

Les deux builds de preuve utilisent `C:\HBP\Software\hbp_core\out\quest-015\run-5`
et `run-6`, avec packages `package-5` et `package-6`. Le package final contient
`libhbp_core.so`, `artifact-manifest.json`, `elf-report.json`, les notices et le
SBOM SPDX. Le binaire non strippé reste dans `run-6/build/libhbp_core.so`.
L’installation locale exécutée est :

```powershell
./Tools/Update-NativePlugins.ps1 -AndroidPackage C:\HBP\Software\hbp_core\out\quest-015\package-6
```

Pour la voie normale GitHub, après intégration des changements natifs dans
`hbp_core/master` et des fichiers HiBoP :

```powershell
./Tools/update-native-plugins.cmd
# Si Unity était encore ouvert lors de l’installation :
./Tools/update-native-plugins.cmd -Resume <identifiant-retourné-par-le-script>
```

Cette commande résout les SHA sur `master`, déclenche trois workflows avec
`platform=all`, télécharge les dix artefacts et les installe ensemble. Le job
Android utilise l’outillage du commit du workflow, et une archive du `source_sha`
demandé. Un workflow hbp_core sans job Android est refusé avant déclenchement.
La documentation d’utilisation est dans le [README CI](../../../../.github/workflows/README.md).

## Vérifications effectuées

| ID | Vérification | Résultat / preuve |
| --- | --- | --- |
| T1 | Deux builds propres Release avec mêmes source/NDK/CMake/Ninja, chemins différents | REUSSI, même SHA-256 `15a00291…e9d2c501`, exit 0 pour les deux ; `native-build-5.log`, `native-build-6.log`. |
| T2 | ABI et ELF final ; comparaison ABI avec DLL Windows réellement épinglée | REUSSI, 226 exports pour chaque cible ; seules dépendances `libc.so`, `libdl.so`, `libm.so`, LOAD alignés 16 Kio ; `elf-report.json`, `windows-abi.log`. |
| T3 | Tests natifs de l’outillage Android | REUSSI, 9 contrôles : artefact valide, ABI valide, mauvaise architecture, baseline altérée, SHA non immuable, Debug, dossier ancien, mauvais NDK, faux `GITHUB_SHA` ; `native-tests-final.log`. |
| T4 | Tests existants de l’updater | REUSSI, suite locale sans réseau, exit 0. |
| T5 | Tests Android de l’updater | REUSSI, 39 contrôles, exit 0 ; manifeste de résultats conservé. Trois dispatches et dix téléchargements sont simulés ; les validations, copies, locks et rollbacks sont réellement exécutés dans les fixtures. |
| T6 | YAML GitHub et contrat des jobs | REUSSI, 9 contrôles ; jobs Desktop inchangés, sélection `all`/`android`, checkout scientifique séparé, pins et portes de validation ; `workflow-checks.json`. |
| T7 | Unity EditMode, profil Quest, assembly `HBP.PlatformConfiguration.Tests` | REUSSI, 25/25, exit 0 ; `android-editmode.xml`, import ARM64 exclusif et guard. |
| T8 | Build APK et inspection du natif épinglé | REUSSI, exit 0, 314,71 s, 0 erreur et 66 warnings sur du code existant ; APK de 104 902 008 octets, huit ELF ARM64, hash natif identique au lock ; `zipalign -c -P 16 -v 4` réussit. |
| T9 | Formatage C# et diff | REUSSI, `Tools/format-code.cmd` sur les deux C# et `git diff --check` dans les deux dépôts, exit 0. |

Les logs, builds et fixtures de tests sont locaux et ignorés sous
`.test-results/quest-015`, `.test-results/native-android-*`, `.artifacts/quest-015`
et `hbp_core/out/quest-015`. Le manifeste donne leurs chemins absolus et hashes.
Les petits résultats d’inspection et manifestes sont conservés sous
`evidence/QUEST-015` pour la review depuis un autre checkout.

APK inspecté : `C:\HBP\Software\HiBoP\.artifacts\quest-015\quest\HiBoP.Quest.apk`,
SHA-256 `74d3a65d89a8ecbd463c9a371177ed26b5677466701cc938b0e508ae92fdc8a6`.
La commande d’inspection exécutée est
`./Tools/Test-QuestApk.ps1 -Apk .artifacts/quest-015/quest/HiBoP.Quest.apk -ReportPath .artifacts/quest-015/quest/quest-apk-content.json`.
L’APK n’a pas été installé ni exécuté sur le casque.

Les modifications automatiques de BuildInfo, préfiltrage URP, réglages OpenXR
et icônes Player générées par les passages Unity ont été conservées dans
`.test-results/quest-015/unity-generated.patch`, puis restaurées à leur état
initial. Elles ne font pas partie de l’implémentation. Les warnings observés
concernent notamment les analyseurs de sérialisation UAC et des APIs obsolètes
dans des fichiers non modifiés.

La première tentative native a révélé le renommage Windows 8.3 de `clang++.exe`
en `CLANG_~1.EXE`, qui sélectionnait le driver C lors du lien. `--driver-mode=g++`
rétablit explicitement le lien STL. Une comparaison intermédiaire a ensuite
révélé les chemins de travail dans le debug ; les deux builds finaux corrigés
sont ceux déclarés reproductibles. Le support 16 Kio suit la
[documentation Android](https://developer.android.com/guide/practices/page-sizes),
sans affirmer une exécution sur un appareil configuré à 16 Kio.

## Validation manuelle

NON_REQUIS : aucune manipulation de casque ni fixture scientifique n’est demandée
pour cette tâche. Les contrôles de compilation et de packaging sont automatisés.

## Comparaison de l’artefact GitHub fourni par le propriétaire

Le 2026-09-09, le propriétaire a fourni
`C:\HBP\Software\HiBoP\.artifacts\quest-015\github\hbp_core-android-arm64-34337066008.zip`
et le [lien du run 34337066008](https://github.com/hbp-HiBoP/hbp_core/actions/runs/34337066008).
Le connecteur GitHub retourne HTTP 404 pour ce dépôt privé et GitHub CLI n’est
pas disponible. Le statut et les logs du run n’ont donc pas été consultés.
La comparaison repose sur l’archive fournie et sur une inspection indépendante
de son binaire, pas sur une simple lecture de son rapport ELF.

[Résultats détaillés](../evidence/QUEST-015/github-comparison.json),
[manifeste reçu](../evidence/QUEST-015/github-artifact-manifest.json),
[inspection indépendante](../evidence/QUEST-015/github-elf-inspection.json).

| Contrôle | Résultat observé |
| --- | --- |
| Intégrité du package | Les tailles et SHA-256 des quatre fichiers déclarés correspondent au manifeste ; aucun fichier supplémentaire. |
| Source du build GitHub | Commit `4e8255bc36ddab9e86f31d56452ab5346ea7315c` (« Android build support »), présent dans le dépôt local ; arbre complet conforme au manifeste. |
| Équivalence scientifique avec Desktop | Les objets Git de `src`, `include`, `third_party`, `baseline` et `CMakeLists.txt` sont identiques à ceux du commit Desktop `1f26946e4d4e95523e637a21be70db0d1af6db8c`. Seuls les outils, le workflow et leur documentation ont changé. |
| Outillage | Les cinq hashes des scripts/configuration du manifeste correspondent aux blobs Git du commit CI. NDK, CMake, Ninja, API, ABI, STL et alignement épinglés sont les mêmes ; hôte CI Linux, hôte de référence Windows. |
| ABI et ELF | 226 exports publics identiques à la baseline Desktop et au binaire local ; dépendances `libc.so`, `libdl.so`, `libm.so` ; LOAD alignés 16 Kio ; inspections indépendantes réussies. |
| Binaire GitHub | 1 450 608 octets, SHA-256 `96ea8cd31ac5f572560403fc177f53f2dc56009780ad9de91c0d63ddb74c4a8e`. |
| Comparaison des sections ELF | Toutes les sections de code, données, symboles et relocations sont identiques. Seules `.comment` (identification du compilateur, 362 contre 204 octets) et `.note.gnu.build-id` diffèrent. Ce résultat ne constitue pas une qualification runtime. |
| Validation par l’updater | Package accepté avec son SHA déclaré `4e8255b…`, mais correctement refusé contre le pin Desktop `1f26946…` : l’updater exige l’égalité du commit complet, pas seulement celle des sous-arbres scientifiques. Aucune installation n’a été effectuée. |

Pour remplacer uniquement le plugin Android tout en conservant le pin Desktop,
le prochain run **Build native library** doit utiliser `platform: android` et
`source_sha: 1f26946e4d4e95523e637a21be70db0d1af6db8c`. Le workflow actuel dispose
précisément de la séparation nécessaire entre son outillage récent et cette
archive scientifique ancienne. Il n’est pas nécessaire de modifier les sources
scientifiques ou de reconstruire les binaires Desktop pour cette comparaison.

L’archive reçue confirme la production d’un artefact Linux/NDK cohérent avec les
sources attendues. Faute d’accès aux logs, l’égalité des deux builds exécutés sur
le runner et les résultats détaillés des tests CI ne sont pas directement
confirmés. La récupération automatique de dix artefacts et leur installation
depuis de vrais runs reste également distincte de cette comparaison hors ligne.

## Limites et suite

- La livraison GitHub complète est maintenant vérifiée et installée ; le lock
  indique le SHA exact résolu sur master pour chaque bibliothèque.
- Les preuves de compilation locale initiale restent historiques. L’identité
  binaire entre hôtes Windows et Linux n’est pas garantie.
- Aucun chargement natif, allocation/libération, projection ou parité scientifique
  Android n’est qualifié ici. Ces vérifications relèvent de QUEST-016 puis QUEST-019.
- Une ouverture interactive du Player Desktop reste à vérifier manuellement.
- Les scripts EEGFormat/hbp_math utilisent encore GITHUB_SHA pour leur manifeste.
  Si master bouge entre résolution et dispatch, l’updater refusera tout package
  dont la provenance ne correspond pas. Les trois runs présents ont le bon SHA.
- Prochaine tâche proposée : QUEST-016, sans exécution automatique.
