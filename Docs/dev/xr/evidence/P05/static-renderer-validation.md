# P05 — validation du renderer statique candidat

## Portée

- date : 2026-09-02;
- Unity : `6000.5.2f1`;
- URP : `17.5.0`;
- projet : `XR/`;
- données : `SurfaceAsset` P03 locaux exportés des GIFTI MNI de référence;
- sorties brutes : `.artifacts/xr/p05/` et `.test-results/p05-final/` (hors Git).

## Résultats automatiques

| Contrôle | Résultat | Preuve |
| --- | --- | --- |
| Tests EditMode | PASS, 10/10 | `editmode-results.xml`, 0 échec |
| Repère/bounds/normales | PASS | rejet des repères non P03, bounds incohérents et normales non unitaires |
| Index 16/32 bits | PASS | bascule contrôlée à 65 536 sommets |
| Cache partagé | PASS | un mesh pour deux leases, destruction au dernier release |
| Cycles de durée de vie | PASS | 256 créations/libérations, 0 mesh résident |
| Frontière runtime | PASS | aucune référence Core/Data/Protocol/Bootstrap, réseau ou P/Invoke |
| Prefab-first | PASS | deux renderers et toutes leurs références sérialisés dans le prefab |
| Build Android | PASS | Android/IL2CPP/ARM64, URP, Linear, Vulkan |
| Intégrité binaire | PASS | SHA-256 du payload vérifié; corruption injectée rejetée |

Commande de tests :

```text
Unity.exe -batchmode -nographics -accept-apiupdate -projectPath C:\HBP\Software\HiBoP\XR -runTests -testPlatform EditMode -assemblyNames CRNL.HiBoP.XR.StaticRendering.EditModeTests -testResults C:\HBP\Software\HiBoP\.test-results\p05-final\editmode-results.xml -logFile C:\HBP\Software\HiBoP\.test-results\p05-final\editmode.log -forgetProjectPath
```

## Sources GIFTI et SurfaceAsset P03

L'exporteur Desktop charge les surfaces GIFTI avec `MNI.trm`, applique le même retournement des triangles et calcul des normales que le chemin Desktop, fusionne les hémisphères, puis appelle `DesktopSurfaceRenderModelAdapter`. Le runtime XR ne charge aucun GIFTI et ne référence ni Core ni Data : il lit uniquement les buffers P03 locaux produits par cet export.

| Source | SHA-256 |
| --- | --- |
| `MNI_Lhemi.gii` | `1e5c67969dbea35e1316c9b0a16e098179be4629a91c6cd60e4852892554a348` |
| `MNI_Rhemi.gii` | `3aab5ac60f7c179a5b67d8d09e47c5f38c76b4a8e9e1560d0a5cee98c73a9fd6` |
| `MNI_Lwhite_inflated.gii` | `05fd152d02aacfd466240246967bdc9db39c9a579fb862a0f99c9f21cbf905b8` |
| `MNI_Rwhite_inflated.gii` | `49a9405935b484582b9b141e0c22270808b68adfea900627218d62c86d6c29dc` |
| `MNI.trm` | `1a99d0241b7809d1e3d0410687f001e84f2dc19e60381f1547da1e2fc464c3ee` |

| Surface P03 | Sommets | Triangles | SHA-256 payload |
| --- | ---: | ---: | --- |
| Anatomical | 69 104 | 138 216 | `ab8794d4bd5ecb3daa20d26d74f35b1be533f0a3cf6c02ec5215592bc795d135` |
| Inflated | 66 299 | 132 590 | `fd029198b7fbcaf7b60987f2c772ac78ce4654359967b4d1aa6d327df4c3a7c8` |

## Golden D1 Desktop/XR

Le shader Desktop `HBP/Brain` est la référence et `HiBoP XR/P05/Surface Opaque` le candidat. Les deux consomment les mêmes buffers P03, la même couleur, la même caméra orthographique et l'espace Linear sur Direct3D 12. Les six couples de vues 512 × 512 sont archivés sous `.artifacts/xr/p05/d1-golden/` avec leurs RGBA bruts.

| Mesure sur 6 vues | Résultat | Seuil |
| --- | ---: | ---: |
| Vues passées | 6/6 | 6/6 |
| IoU silhouette minimal | 1,0 | ≥ 0,995 |
| Erreur canal maximale | 0,00784314 (2/255) | ≤ 0,03137255 (8/255) |
| Erreur canal p99 maximale | 0,00392157 (1/255) | ≤ 0,02 |
| Erreur canal moyenne maximale | 0,000141817 | ≤ 0,003 |
| Couverture non noire | 15,46 % à 23,94 % | ≥ 1 % |

Le fichier brut `d1-golden-evidence.json` contient les hashes PNG et les métriques de chaque vue. Le seuil de couverture empêche qu'un rendu `Null` ou entièrement noir soit accepté.

## Shader Android et APK

- APK : `.artifacts/xr/p05/HiBoPXR-P05.apk`;
- taille : `77 395 361` octets;
- SHA-256 : `5e604605986d68fea92cf5d0b4bee12c384e44bf4447278f363247203fc12224`;
- build Unity : succès;
- les shaders `HiBoP XR/P05/Surface Opaque` et `Transparent` compilent chacun leur pass `UniversalForward` pour Vulkan, quatre programmes internes uniques;
- `Fallback Off` dans les deux sources; aucune erreur shader dans le build.
- golden transparent hôte sur fond vert : alpha configuré `0,25`, erreur maximale et p99 `1/255`, PASS.

Reproduction :

```powershell
.\XR\Tools\Build-P05.ps1
.\XR\Tools\Profile-P05Quest.ps1
```

## Validation physique Quest 3

| Mesure | Résultat |
| --- | ---: |
| Appareil / OS | Oculus Quest 3, Android 14 / API 34 |
| GPU / API / espace couleur | Adreno 740, Vulkan, Linear |
| Fenêtre mesurée | 360 frames de warmup + 720 frames |
| Intervalle frame p50 / p95 / max | 13,8889 / 13,8890 / 13,8890 ms |
| Main thread p50 / p95 / max | 13,8516 / 14,3800 / 14,9299 ms |
| GC/frame p50 / p95 / max | 128 / 600 / 29 192 octets |
| Mémoire Unity allouée / réservée | 85,98 / 208,84 Mo |
| PSS / RSS avant fermeture | 438 706 / 588 220 Ko |
| Meshes actifs | 2 |
| Utilisation GPU système, 15 échantillons | 50,34 % moyenne, 49,61–50,79 % |
| Thermique | statut 0, CPU ~49 °C, GPU ~47 °C, aucun throttling |
| Après fermeture | `No process found`, mémoire libérée |

Les recorders Unity render-thread et GPU-frame ne sont pas exposés sur ce player; la charge GPU est donc archivée séparément depuis `kgsl-3d0/gpubusy`. Le run d'endurance 30 minutes n'a pas été exécuté.

Le contrôle visuel physique valide l'orientation de type View3D, les deux cerveaux côte à côte, l'anatomical opaque et l'absence d'accumulation triangulaire après ajout de la prépasse profondeur. Il **échoue** sur le dernier point P05-D : malgré un alpha shader `0,25`, un golden hôte correct, un framebuffer avec alpha, la sortie alpha URP activée et un blend prémultiplié, l'inflated bleu reste perçu opaque sur le passthrough. Le renderer reste candidat et ne doit pas être gelé avant une investigation dédiée de la composition OpenXR/Meta.
