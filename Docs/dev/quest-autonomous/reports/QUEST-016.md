# Rapport QUEST-016 — Runtime natif de base sur Quest

## Résultat

QUEST-015 livrait le binaire Android sans preuve d'appel depuis le casque.
`QuestNativeDiagnostic` ajoute un diagnostic ponctuel dans le Player de
développement Android IL2CPP. Un marqueur contenant un identifiant d'exécution
est consommé au démarrage ; sans ce marqueur, aucun diagnostic n'est lancé.
Aucun GameObject, prefab ou élément d'interface produit n'est ajouté.

Chaque série exécute 100 cycles séquentiels : initialisation, callback natif
UTF-8, erreur native contrôlée, chargement d'un volume synthétique, création et
copie d'une surface et d'une liste de sites, libération explicite et shutdown.
Les wrappers de `HBP.Core.Runtime` sont utilisés tels quels. Les vérifications
portent sur les dimensions, espacement et extrema du volume, les sommets,
indices et normales de la surface, et le masque des sites sur un plan.
Chaque cycle libère six handles et vérifie l'idempotence de `Dispose`.

Le runner inspecte l'APK, compare son hash à l'APK réellement installé, corrèle
le résultat au nouvel identifiant, récupère les preuves et arrête uniquement
`fr.crnl.hibop.quest` après chaque série. Ses attentes sont bornées à 90 secondes
par série. Les attentes C# rendent la main au PlayerLoop ; aucun thread de rendu
n'est bloqué en attente d'une opération asynchrone.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : NON_REQUIS.
- Branche `feature/xr-autonomous`, HEAD initial
  `b021a023d5e55f156e1c3c0c9d1ad6edd5f3ef1b`, checkout initial propre.
  Aucun commit, push, CI distante ou changement de branche.
- Unity `6000.5.2f1`, profil `Assets/Settings/BuildProfiles/Quest.asset`,
  Player Android ARM64 IL2CPP de développement.
- Dépendances : [QUEST-004](QUEST-004.md) pour le Player Quest et son rig déjà
  validés ; [QUEST-015](QUEST-015.md) pour l'import et la provenance native.
- `Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so` reste inchangé :
  1 450 608 octets, SHA-256
  `96ea8cd31ac5f572560403fc177f53f2dc56009780ad9de91c0d63ddb74c4a8e`.
  Source épinglée `ffb7686011f37a21e6c5ab4f88ad6cfcbc53ffc1`,
  run GitHub `34339388439`, selon `Tools/NativePlugins.lock.json`.
  NDK `27.2.12479018`, API 32, ABI `arm64-v8a`, STL statique.
- Import conservé : Android activé, CPU `ARM64`, `Any Platform` et Editor
  désactivés, cibles Desktop désactivées. Pas de duplication du plugin.
- [Manifeste des preuves](../evidence/QUEST-016/manifest.json).
  Les gros logs et APK restent locaux, sous `.test-results/quest-016`
  et `.artifacts/quest-016` ; leurs hashes figurent dans le manifeste.
- Nouveau diagnostic inspiré du mécanisme de marqueur existant de
  `QuestContactDiagnostic` et du writer NIfTI de
  `NativePerformanceBenchmarkFixtures`. Aucune reprise de branche historique.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Règle à vérifier |
| --- | --- | --- |
| 1 | [QuestNativeDiagnostic.cs](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestNativeDiagnostic.cs), `Initialize`, `RunCycle`, `Release` | Compilation limitée au Player Android IL2CPP de développement ; activation ponctuelle ; appels communs et libérations avant shutdown, y compris en cas d'exception. |
| 2 | [Run-QuestNativeProbe.ps1](../../../../Tools/Run-QuestNativeProbe.ps1) | APK installé identique à l'APK inspecté ; résultats frais, compteurs complets, délai borné, collecte et arrêt vérifié de HiBoP. |
| 3 | [HBP.Quest.Runtime.asmdef](../../../../Assets/Scripts/HBP/Quest/Runtime/HBP.Quest.Runtime.asmdef) | Référence au moteur commun `HBP.Core.Runtime` ; aucune variante des wrappers ou de l'algorithme scientifique. |
| 4 | [libhbp_core.so.meta](../../../../Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so.meta), [Test-QuestApk.ps1](../../../../Tools/Test-QuestApk.ps1) | Configuration et contrôles existants conservés ; seul le natif ARM64 attendu est empaqueté, conforme au lock. |

## Vérifications effectuées

| ID | Scénario / commande | Résultat et preuve |
| --- | --- | --- |
| T1 | `adb devices -l`, puis `Tools/Connect-QuestAdbWifi.ps1 -KeepAwakeWhilePluggedIn`, hors sandbox | REUSSI, exit 0 ; Quest 3 autorisé à `192.168.1.18:5555`, ADB `37.0.1-15733141`, maintien éveillé vérifié. |
| T2 | Unity CLI, profil Quest, EditMode `HBP.PlatformConfiguration.Tests` | REUSSI, exit 0, 25/25 ; [XML](../evidence/QUEST-016/platform-editmode.xml). |
| T3 | `hbp_core/tools/Test-HbpCoreAndroidArtifact.ps1` sur le plugin installé | REUSSI, exit 0 ; ELF64 AArch64, 226 exports, dépendances `libc.so`, `libdl.so`, `libm.so`, LOAD alignés à 16 Kio ; [résumé](../evidence/QUEST-016/elf-summary.json). |
| T4 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId quest-016` | REUSSI, exit 0 ; build 325,022 s ; APK 162 999 143 octets. |
| T5 | `Tools/Test-QuestApk.ps1`, puis comparaison `adb shell sha256sum` de l'APK installé | REUSSI, exit 0 ; huit bibliothèques ARM64, aucun plugin scientifique Desktop ; hash natif identique au lock ; [contenu APK](../evidence/QUEST-016/apk-content.json). |
| T6 | `Tools/Run-QuestNativeProbe.ps1 -Serial 192.168.1.18:5555` | REUSSI, exit 0 ; trois démarrages IL2CPP, 300 cycles, 1 800 handles libérés, 300 callbacks UTF-8, 300 erreurs natives contrôlées suivies d'appels valides ; [résultats](../evidence/QUEST-016/device-results.json), [commandes et codes de sortie](../evidence/QUEST-016/device-commands.json). |
| T7 | Démarrage sans marqueur, observation 20 s, comparaison du hash du résultat précédent | REUSSI, exit 0 ; `COMPOSITION READY`, aucun message `QUEST016`, résultat précédent inchangé ; [preuve](../evidence/QUEST-016/normal-start.json). |
| T8 | `Tools/format-code.cmd`, parseur PowerShell du runner, `git diff --check` | REUSSI, exit 0. Le formateur a été relancé hors sandbox après restauration NuGet et génération du projet C# par Unity. |

Les trois logs physiques sont conservés : [série 1](../evidence/QUEST-016/run-1-logcat.txt),
[série 2](../evidence/QUEST-016/run-2-logcat.txt),
[série 3](../evidence/QUEST-016/run-3-logcat.txt).
Le runtime retourne la chaîne de version `0.2.1` ; la provenance du binaire
est établie par le commit et le SHA-256 du lock, pas par cette chaîne.
`/proc/self/maps` confirme le chargement de `libhbp_core.so` et `libil2cpp.so`
depuis le répertoire installé `lib/arm64` de ce package.

L'erreur provoquée est `HbpCoreRuntime.SetLogFile("")` : statut
`InvalidArgument`, texte `hbp_core_set_log_file received an empty path`,
callback de type Error. Chaque cycle reprend ensuite avec succès les appels
Volume, Surface et RawSiteList. Les compteurs n'augmentent qu'après vérification.

| Série | Tas natif initial (octets) | Après 10 cycles | Après 100 cycles | Écart 10 → 100 |
| --- | ---: | ---: | ---: | ---: |
| 1 | 26 610 944 | 26 781 160 | 26 709 208 | −71 952 |
| 2 | 26 614 936 | 26 622 880 | 26 586 152 | −36 728 |
| 3 | 26 524 336 | 26 525 960 | 26 491 848 | −34 112 |

Aucune croissance continue du tas natif n'est observée sur ces séries.
Les mesures complètes Unity/managées et les onze échantillons par série sont
dans le JSON ; `dumpsys meminfo` après chaque série est conservé localement.
L'arrêt de HiBoP est confirmé après les trois séries et après le contrôle sans
marqueur (`pidof` vide, exit 1 attendu). ADB Wi-Fi et réglages D26 sont conservés.

Logcat contient également trois diagnostics déjà présents dans les essais
antérieurs : classe Google Play `AssetPackManager` absente, résolution de
`xrDiscoverSpacesMETA`, et `No suitable capture camera found` de Meta OpenXR.
Ils sont conservés dans les logs et ne sont pas corrigés dans cette tâche.
Aucune erreur de chargement `hbp_core`, exception du diagnostic ou panne native
n'est observée. Le contrôle sans marqueur retrouve `COMPOSITION READY` ; il ne
requalifie pas visuellement le passthrough ni les gestes.

Commandes de reproduction depuis `C:\HBP\Software\HiBoP`, dans PowerShell hors sandbox,
éditeur fermé pour le build :

```powershell
./Tools/Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId quest-016
./Tools/Run-QuestNativeProbe.ps1 -Serial 192.168.1.18:5555
```

APK : `C:\HBP\Software\HiBoP\.artifacts\quest-016\Android\HiBoP.Quest.apk`.
SHA-256 : `e035c6a16cf3507b4d83404a380b38323260fe7f77c34b1317c9d1dbca8dad39`.
Le diagnostic écrit sa fixture `synthetic-32x24x16.nii` et `result.json` sous
`/sdcard/Android/data/fr.crnl.hibop.quest/files/quest016` ; le runner les récupère
dans le répertoire local de chaque série. NIfTI float32, affine identité,
32 × 24 × 16 voxels, espacement 1, valeurs 0 à 255. Il s'agit d'une fixture de
test du runtime, pas d'une anatomie ou d'une entrée de projection qualifiée.
SHA-256 de la fixture :
`4ec56fdd3f53c0f2caec05eed465f4e948c6ecd4fd7fa1ac9adf0a5542e8f396`.

Les changements automatiques de BuildInfo, préfiltrage URP, réglages OpenXR et
icônes Player produits par Unity ont été sauvegardés dans
`.test-results/quest-016/unity-generated.patch`, puis restaurés. Ils ne font pas
partie du diff livré. Les réglages d'import natif et les wrappers restent inchangés.

## Validation manuelle

NON_REQUIS : le casque est accessible et les contrôles demandés sont
automatisables. Aucune évaluation scientifique ou manipulation des contrôleurs
n'est demandée au propriétaire pour cette tâche.

## Limites et suite

Les relevés mémoire couvrent le tas natif Android, les allocations Unity et
le tas managé toutes les dix répétitions. Ils incluent le reste du Player XR
et ne constituent pas un détecteur exhaustif de fuites ni un budget de densité.
Le succès de `Dispose` et la remise à zéro des handles vérifient le chemin de
libération ; `Shutdown` ferme l'état du runtime et ses loggers, sans prétendre
décharger le `.so` du processus. La fin du processus est vérifiée séparément.

Aucun calcul de densité, transfert des entrées de projection ou verdict de
parité scientifique n'est couvert. La prochaine tâche proposée est
[QUEST-017](../tasks/QUEST-017.md), sans exécution automatique.
