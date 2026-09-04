# P06 — validation Windows et Quest

- **Date :** 2 septembre 2026
- **État :** `P06-W PASS`; `P06-WQ PASS`; `P06-ML NATIVE PENDING`
- **Branche de départ :** `feature/xr`, baseline `72ba9bb3e8afa118fc5527b2e5395ad30793942a`
- **Isolation :** tout code candidat sous `Spikes/P06/`; aucun manifest ou assembly de production modifié

## Environnement et protocole

Windows x64 natif, build OS `10.0.26200`, processeur Intel64 famille 6 modèle 151, 20 processeurs logiques, .NET SDK `10.0.201`. Le host mesuré est publié self-contained avec ASP.NET Core/.NET `10.0.11`. La RAM physique n’était pas exposée par l’environnement de mesure et n’est pas estimée.

Profil N0 loopback : asset déterministe 100 Mio, ranges 1 Mio, plafond host 100 Mbit/s, SHA-256 par chunk et final, 20 commandes/s visées pendant 120 s, cinq répétitions. Le générateur est fermé : il attend l’écho avant de respecter le reste de l’intervalle, donc il n’accumule pas une file artificielle lorsque le scheduler retarde un tick.

## Résultats réseau définitifs

| Run | Succès/échecs | RTT p50 | RTT p95 | RTT max | Bulk utile |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 931 / 0 | 0,2767 ms | 0,5006 ms | 8,0753 ms | 89,762 Mbit/s |
| 2 | 1 930 / 0 | 0,2766 ms | 0,4971 ms | 3,5579 ms | 89,870 Mbit/s |
| 3 | 1 931 / 0 | 0,2788 ms | 0,4834 ms | 2,8828 ms | 89,909 Mbit/s |
| 4 | 1 931 / 0 | 0,2790 ms | 0,4883 ms | 3,2835 ms | 89,805 Mbit/s |
| 5 | 1 931 / 0 | 0,3947 ms | 0,7266 ms | 4,9595 ms | 89,315 Mbit/s |

Agrégat : 9 654 commandes, zéro échec, p50 médian `0,2788 ms`, p95 médian `0,4971 ms`, pire p95 `0,7266 ms`, pire maximum `8,0753 ms`; bulk moyen `89,732 Mbit/s`. Le seuil p95 ≤ 100 ms passe sur Windows N0. Le client a alloué en moyenne `341 413 989` octets sur l’ensemble du scénario et son pic de working set maximal est `85 012 480` octets ; ces totaux incluent les benchmarks codecs, les 100 buffers de chunk et le contrôle pendant 120 s, et ne sont pas des allocations par commande.

Les JSON bruts sont générés sous `Spikes/P06/.artifacts/windows-release/n0-run-1.json` à `n0-run-5.json` et restent volontairement hors Git.

## Codecs contrôle

Chaque valeur suivante est la moyenne de cinq séries de 100 000 encode/decode après 10 000 warm-ups, sur le même échantillon borné.

| Codec | Wire | Temps / 100k | Allocation / opération | Lecture |
| --- | ---: | ---: | ---: | --- |
| Google.Protobuf 3.36.1 | 328 o | 457,290 ms | 2 640 o | référence schema-first, la plus allouante de l’échantillon |
| MessagePack 3.1.8 | 325 o | 263,672 ms | 1 240 o | plus petit wire et allocations minimales |
| MemoryPack 1.21.4 | 444 o | 79,929 ms | 1 360 o | plus rapide, mais wire le plus grand et format C# plus couplé |

Les golden vectors complets sont dans `Spikes/P06/fixtures/control-sample-v1.json`. Leurs SHA-256 sont respectivement `5eea415f…3db8`, `7dc75b5d…d12b` et `13d13f39…bfd3`. Les 17 tests .NET vérifient leur stabilité octet par octet et leur décodage.

## Buffers

Échantillon déterministe de 138 208 valeurs (`552 832` octets en float32).

| Format | Taille | Encode moyen | Decode moyen | Erreur max / RMS | Décision candidate |
| --- | ---: | ---: | ---: | ---: | --- |
| float32 little-endian | 552 832 o | 1,416 ms | 1,612 ms | 0 / 0 | baseline |
| float16 little-endian | 276 416 o | 1,530 ms | 2,374 ms | 0,00024414 / 0,00011125 | mesure séparée, non admissible sans tolérance scientifique |
| float32 + LZ4 | 555 001 o | 3,787 ms | 1,183 ms | 0 / 0 | rejet comme défaut : +2 169 o et copie/CPU supplémentaires sur ce signal |

## Sécurité et adversarial Windows

| Cas | Résultat |
| --- | --- |
| mauvais SAS | handshake TLS refusé par le callback avant `/pair` |
| nouvelle identité avec ancien pin | handshake TLS refusé avant payload applicatif |
| frame nul/tronqué | close WSS `InvalidPayloadData`; aucune exception non gérée après durcissement |
| chunk volontairement corrompu | `InvalidDataException: Asset chunk hash mismatch` avant hash final |
| longueur wire incohérente/surdimensionnée | tests unitaires : rejet avant codec |
| dix tentatives d’appairage/minute | tentatives 1–10 bornées; 11 et 12 reçoivent HTTP 429 |
| vulnérabilités NuGet connues | aucune remontée par `dotnet list package --vulnerable --include-transitive` le 2 septembre 2026 |

Le host borne aussi : enveloppe contrôle 64 Kio, range 1 Mio, 16 connexions HTTP, deux upgrades WSS, 100 frames/s/socket, quatre tokens actifs de 30 minutes, headers 10 s et keep-alive 30 s. Les tokens et l’identité restent en mémoire. La revue détaillée est dans `security-license-review.md`.

## Packaging et transparence

| RID | Cross/native | Installé | Zip | SHA-256 zip |
| --- | --- | ---: | ---: | --- |
| `win-x64` | publié et exécuté natif | 111 554 005 o | 50 583 915 o | `400bd03f4f5ae91b0fcd72fbcc088658f15739216200a3f84ac714f17a8cbbbe` |
| `linux-x64` | cross-publish seulement | 110 956 651 o | 50 427 135 o | `34eecbd2ee3d8029bd66f3b464b008c18c7225be25d3e8fdf6a27c7c94170ded` |
| `osx-arm64` | cross-publish seulement | 118 081 703 o | 46 481 680 o | `95407156989b2ad6791ad3fa2fb6b4ab938e96fb16b2e2888894bb6347cc5755` |

Le plafond absolu provisoire de 150 Mio compressés passe. Le propriétaire estime l’archive HiBoP complète à environ 200 Mio compressés : les `50 583 915` octets du sidecar représentent donc approximativement `25 %`, nettement au-dessus du budget relatif de 10 %. Ce dépassement est accepté comme non bloquant pour le prototype. Avant distribution, il faudra choisir entre une édition XR séparée et une solution embarquée moins coûteuse offrant les mêmes garanties; cette seconde piste reste explicitement ouverte.

Le launcher `WinExe` a démarré le sidecar avec `UseShellExecute=false`, `CreateNoWindow=true`, `WindowStyle=Hidden`, stdout/stderr redirigés, `MainWindowHandle=0`, puis a arrêté tout l’arbre de processus. Le contrat de fichier `ready` permet au futur UI HiBoP d’afficher le SAS sans console ; cette intégration produit reste hors P06.

## Quest physique — P06-WQ

L’APK Unity `6000.5.2f1` a compilé avec IL2CPP, ARM64, Vulkan, min API 32/target 36. Le build qualifiant mesure `41 869 895` octets et porte le SHA-256 `ac679b66585dfb57e7caef96ac8cd520bae7fc69a9ed8d81a9f6213dad25e64a`. Il a été sideloadé sur un Quest 3, Android 14/API 34, puis exécuté contre le host Windows relié en Ethernet au même LAN que le Wi-Fi réel du casque (profil N2). Les SSID, BSSID, adresses MAC et identifiants du casque ne sont ni consignés ni journalisés.

Le runtime Quest ne met pas en œuvre `X509Certificate2.GetECDsaPublicKey()`. La sonde isolée extrait donc le bloc DER `SubjectPublicKeyInfo` avec un parseur borné, puis applique le SHA-256 du runtime ; aucune primitive cryptographique n’est réimplémentée. Le pin calculé sur Quest a correspondu exactement au pin Kestrel. `websocket-sharp` interdit par ailleurs `Authorization` dans ses headers utilisateur : WSS transporte le jeton opaque dans `X-P06-Access-Token`, sous le même TLS, tandis que HTTPS conserve `Authorization: Bearer`. Le host accepte ces deux formes sans journaliser le jeton.

Les cinq répétitions qualifiantes exécutent simultanément 20 commandes/s pendant 120 s et l’asset déterministe de 100 Mio en ranges de 1 Mio, avec SHA-256 par chunk et final. Une répétition exploratoire antérieure, limitée à 1 407 commandes parce que la pause de 50 ms s’ajoutait au temps d’envoi synchrone, a été exclue avant fixation de l’horloge à 20 Hz.

| Run | Succès/échecs | RTT p50 | RTT p95 | RTT max | Bulk utile | Durée bulk |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2 400 / 0 | 6,9309 ms | 19,1079 ms | 90,5805 ms | 37,708 Mbit/s | 22,246 s |
| 2 | 2 400 / 0 | 6,7134 ms | 19,1010 ms | 86,6849 ms | 37,356 Mbit/s | 22,456 s |
| 3 | 2 400 / 0 | 6,7725 ms | 18,1151 ms | 62,7674 ms | 38,572 Mbit/s | 21,748 s |
| 4 | 2 400 / 0 | 7,0159 ms | 18,2115 ms | 84,4781 ms | 38,285 Mbit/s | 21,911 s |
| 5 | 2 400 / 0 | 6,4853 ms | 18,5238 ms | 84,1650 ms | 38,873 Mbit/s | 21,580 s |

Agrégat Quest : 12 000 commandes, zéro échec, p50 médian `6,7725 ms`, p95 médian `18,5238 ms`, pire p95 `19,1079 ms`, pire maximum `90,5805 ms`; bulk moyen `38,159 Mbit/s`. Le seuil p95 ≤ 100 ms passe avec le contrôle prioritaire pendant le bulk. Les lignes JSONL reproductibles sont dans `quest-n2-results.jsonl` ; leur horodatage est celui de la transcription post-run, signalé explicitement, car la première version du rapport compact ne portait pas encore d’horodatage par répétition.

Les trois fixtures ont été encodées, encadrées et comparées octet par octet sous IL2CPP : Protobuf `328` octets, MessagePack `325` octets et MemoryPack `444` octets, avec les mêmes SHA-256 que les tests .NET. Un certificat régénéré avec l’ancien SAS a produit `P06_QUEST_NEGATIVE {"case":"identity_changed","result":"PASS"}` avant payload ; un premier chunk altéré après calcul du header SHA-256 a produit `P06_QUEST_NEGATIVE {"case":"corrupted_chunk","result":"PASS"}`.

Les suites finales passent : 17/17 tests Core/protocole et 1/1 test launcher. Le test certificat doit être exécuté hors sandbox local pour accéder au magasin de clés utilisateur Windows ; son premier échec sandbox n’était pas un défaut du candidat.

Les cross-publishes macOS/Linux préparent le fonctionnement, y compris `EphemeralKeySet` hors Windows. Ils ne remplacent pas l’exécution native, les golden vectors natifs, le firewall ou les profils N3/N4. Les coupures N2 de 1/5/30 s et la concurrence maximale V1 ne sont pas couvertes par cette série nominale. `P06-ML` et la qualification complète restent ouverts conformément à la décision Windows-first.
