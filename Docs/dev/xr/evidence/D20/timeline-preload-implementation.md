# D20 — preload timeline lossless

## Verdict

- décision fonctionnelle : **ACCEPTED** avec budget explicite de payload unique ; 1 à 97 indices est le profil qualifié, pas un plafond ;
- implémentation protocole/runtime GPU : **PASS** en compilation et tests Unity ;
- profil physique Quest 3, 1/3/8 colonnes × 97 indices : **PASS** ;
- scrub aléatoire 60 s et autoplay 10 min sur 8 colonnes × 37 500 sites × 97 indices : **PASS** ;
- sous-gate P11 sélection locale préchargée : **PASS** ;
- production : **NO-GO** jusqu'au binding renderer et au transport/UX P15 réels.

## Ce qui est implémenté

`PreloadedDynamicTimelineBuilder` ingère les bundles complets frame par frame sous un budget maximal explicite d'octets de payload unique. L'identité session/timeline/état, le manifeste ordonné, les assets, les dimensions de coupe et les révisions statiques doivent rester identiques. Chaque canal déduplique uniquement des tranches dont les octets sont rigoureusement identiques ; aucune valeur n'est recalculée ou quantifiée.

`PreloadedDynamicTimelineCodec` écrit et lit une archive complète depuis un `Stream` seekable. Le SHA-256 couvre tous les octets, le round-trip est bit-exact et aucune archive corrompue n'est retournée. Le codec n'exige pas la matérialisation de l'archive dans un unique `byte[]`, vérifie la longueur restante et le budget cumulatif de payload unique avant chaque allocation.

`PreloadedTimelineGpuResources` crée des buffers séparés par colonne et canal. Un canal invariant occupe une tranche ; un canal variable occupe `indexCount × elementCount`. Les overlays variables utilisent un `Texture2DArray`. Le budget total est fourni explicitement par l'appelant et chaque allocation est vérifiée contre `SystemInfo.maxGraphicsBufferSize`.

`PreloadedTimelineGpuController` prépare puis publie l'ensemble une fois. Ensuite une commande valide écrit seulement un index `uint` de 4 octets. L'ordre latest-wins dépend de la séquence et des révisions de commande, jamais de la valeur numérique de l'index : le test couvre notamment 96 → 2. Une commande stale ne touche pas le buffer GPU ; un échec de soumission ne modifie pas l'état atomique courant.

Une tranche qui ferait dépasser le budget explicite fait passer le builder en état fautif. Même si l'appelant intercepte l'exception, `Build()` refuse de produire les indices déjà ingérés : aucune troncature silencieuse n'est possible.

## Validation Unity et build

Unity `6000.5.2f1` :

| Projet / assembly | Résultat |
| --- | ---: |
| Desktop `CRNL.HiBoP.Protocol.Tests` | 56/56 PASS |
| XR `CRNL.HiBoP.XR.Timeline.EditModeTests` | 4/4 PASS |
| APK Android IL2CPP ARM64 | Succeeded |

Les tests sources couvrent 97 indices, des canaux invariants et variables, la déduplication lossless, le round-trip sur flux, des échantillons bit-exacts aux indices 0/48/96, la corruption globale, le refus définitif d'un dépassement de budget et la sélection aléatoire/reverse sans seconde préparation. La campagne 56/56 + 4/4 ci-dessus précède uniquement le remplacement mécanique du plafond de 97 par ce budget ; conformément à la demande, elle n'a pas été rejouée après cet ajustement.

APK de probe final : `.artifacts/xr/d20-timeline/HiBoPXR-D20Timeline.apk`, SHA-256 `4309f4b51b197a0e61c0c42d5b3b5451e7de56629024e226445e8cfac5344b46`.

## Mesure physique Quest 3

Environnement : Quest 3, Android 14/API 34, Vulkan, Adreno 740, Unity `6000.5.2f1`, IL2CPP ARM64. Le test est piloté par ADB USB mais la construction, la lecture, l'upload et toutes les sélections mesurées sont locaux au casque.

Le probe construit 97 bundles float32 complets, écrit/hash l'archive, la relit/vérifie, prépare toutes les ressources GPU puis mesure l'écriture de l'index et l'attente jusqu'à la fin de frame. Le « preload local » ci-dessous additionne construction, écriture/hash, lecture/hash et upload ; en production, construction/écriture appartiendront au Desktop et lecture/upload au Quest.

| Profil | Archive | Payload unique / naïf | GPU estimé | Preload local | Soumission index p95 | Fin de frame p95 | Pic RSS |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 col., 150 sites | 28 870 923 o | 46,4 % | 29 073 236 o | 1 127,9 ms | 0,0289 ms | 14,3717 ms | 495 357 952 o |
| 3 col., 150 sites | 86 609 541 o | 46,4 % | 87 219 700 o | 3 142,1 ms | 0,0323 ms | 14,2619 ms | 658 255 872 o |
| 8 col., 37 500 sites | 467 008 086 o | 41,1 % | 469 235 460 o | 17 161,3 ms | 0,0506 ms | 14,2364 ms | 1 129 353 216 o |

Pour le profil maximal :

- scrub pseudo-aléatoire : 60,012 s, 4 322 sélections, soumission p50/p95/max `0,0232 / 0,0506 / 0,1260 ms` ;
- autoplay : 600,012 s, 43 203 sélections, soumission p50/p95/max `0,0231 / 0,0529 / 0,1608 ms` ;
- temps commande locale → fin de frame p95 : `14,2364 ms` en scrub et `14,2538 ms` en autoplay ;
- delta de frame maximal : `1` pour les 47 525 sélections ;
- aucune croissance de backlog, aucun stale accepté, aucun crash, ANR ou OOM ;
- `VmRSS` chargé `1 015 865 344` o, `VmHWM` `1 129 353 216` o ; après libération, `dumpsys` rapporte PSS `900 668 KiB`, RSS `1 055 576 KiB`, swap `0` ;
- statut thermique Android `0` après l'endurance, GPU maximal observé `57,7 °C`, SoC `59,3 °C`.

Les ~14,2 ms incluent l'attente jusqu'à `WaitForEndOfFrame` à 72 Hz ; la soumission elle-même reste sous 0,053 ms p95. Le delta maximal de 1 confirme que le nouvel index est prêt pour la frame suivante dans le probe. Cette preuve ne remplace pas un test command-to-photon du shader de production, puisque son binding appartient encore à P05/P10/P12/P15.

## Coût du preload réseau

Le run USB ne mesure pas le futur transport P15. À titre de borne basse, avec le meilleur débit Quest P06 existant de `38,873 Mbit/s`, les archives seules nécessiteraient au minimum `5,9 / 17,8 / 96,1 s` pour 1/3/8 colonnes. Après réception complète, lecture/hash + upload Quest ont mesuré `0,46 / 1,31 / 7,04 s`.

Ce délai est un coût initial explicite avant disponibilité de la timeline, pas un coût par changement d'index. L'UX de progression/annulation, le transport réel et sa mesure Wi-Fi restent à traiter en P15.

## Artefacts

- profil : `.artifacts/xr/d20-timeline/quest/d20-timeline-profile.json` ;
- mémoire : `.artifacts/xr/d20-timeline/quest/meminfo.txt` ;
- thermique : `.artifacts/xr/d20-timeline/quest/thermal.txt` ;
- logcat : `.artifacts/xr/d20-timeline/quest/logcat.txt` ;
- build : `.artifacts/xr/d20-timeline/build-evidence.json`.

## Limites explicitement différées

- profils dépassant le budget choisi ou encore non qualifiés, notamment 8 colonnes × 37 500 sites × 3 073 indices ;
- compression ou quantification ;
- remplacement complet des positions statiques de sites par le cache P10 afin d'éviter leur duplication GPU ;
- binding des nouveaux buffers dans les shaders/renders P05/P10/P12 et validation command-to-photon ;
- transport réel et UX de progression/annulation du preload P15 ;
- picking précalculé par index si les positions deviennent temporelles.
