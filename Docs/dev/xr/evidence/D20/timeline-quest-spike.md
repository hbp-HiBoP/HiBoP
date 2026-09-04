# D20 — spike timeline float32 sur Quest 3

> Ce document conserve la preuve du pipeline par-frame initial. La décision demandée ci-dessous a depuis été prise : preload lossless complet sous budget mémoire explicite ; 1–97 indices est le profil qualifié, pas un plafond. Voir [l'implémentation et son statut de validation](timeline-preload-implementation.md).

## Verdict

- optimisation lossless par copies mémoire contiguës : implémentée et validée sans changer le format filaire ;
- bundle complet float32, hash SHA-256 global et commit atomique : conservés ;
- Quest 3 local, p95 décodage→préparation/upload→commit : PASS sous 100 ms pour 1 et 3 colonnes, FAIL pour 8 colonnes ;
- budget 72 Hz : FAIL avec le traitement synchrone actuel sur le thread principal ;
- transport réseau du bundle complet : FAIL par borne basse dès 1 colonne au meilleur débit Quest P06 mesuré ;
- D20 timeline : **REQUIRES_DECISION / NO-GO production**.

Aucun bundle partiel, aucune quantification, aucune compression, aucune suppression de donnée et aucun plafond fonctionnel n'ont été introduits.

## Changement mesuré

Le codec conserve exactement les octets float32 little-endian existants mais copie les tableaux contigus par blocs. `Float3` et `Rgba32` ont un layout mémoire explicite ; les tests vérifient leurs tailles ainsi que le round-trip bit-exact des surfaces, sites et overlays. Le SHA-256 global, le manifeste complet et le rejet du bundle entier restent inchangés.

Sur Windows, le dernier run après optimisation mesure un p95 end-to-end loopback de `151,702 / 334,455 / 2 121,770 ms` pour 1/3/8 colonnes, contre `179,207 / 433,535 / 2 652,031 ms` avant optimisation. Le hash SHA-256 géré représente encore respectivement `43,400 / 132,068 / 790,354 ms` p95 dans cette mesure synthétique.

## Mesure physique Quest 3

Environnement : Unity `6000.5.2f1`, Android 14/API 34, IL2CPP ARM64, Vulkan, Adreno 740, 20 répétitions par profil. L'APK de probe emploie le package isolé `fr.crnl.hibop.xr.d20timeline` afin de ne pas remplacer l'application HiBoP existante.

Le probe effectue une copie de payload locale, le décodage et le SHA, crée et remplit de vrais `GraphicsBuffer` et `Texture2D`, puis publie l'ensemble par un commit atomique unique. La phase prepare-upload mesure la soumission CPU aux API Unity ; elle ne mesure ni l'achèvement GPU, ni le binding au renderer de production.

| Profil | Payload | Copie p50/p95/max | Decode p50/p95/max | Prepare-upload p50/p95/max | Commit p50/p95/max | Local end-to-end p50/p95/max | Frame interval p50/p95/max |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| D2, 1 col., 150 sites | 641 846 o | 0,079 / 0,206 / 0,297 | 9,336 / 10,672 / 10,960 | 1,126 / 3,926 / 4,017 | 0,005 / 0,006 / 0,040 | 10,350 / 13,636 / 14,041 | 13,889 / 27,778 / 222,222 |
| D2, 3 col., 150 sites/col. | 1 925 358 o | 0,232 / 0,643 / 1,255 | 29,349 / 30,704 / 34,881 | 2,614 / 2,860 / 2,868 | 0,006 / 0,008 / 0,011 | 32,136 / 34,225 / 37,861 | 41,667 / 41,667 / 83,333 |
| D3, 8 col., 37 500 sites/col. | 11 707 738 o | 5,923 / 6,315 / 6,529 | 170,000 / 172,306 / 173,561 | 12,946 / 14,589 / 14,862 | 0,008 / 0,011 / 0,013 | 188,781 / 191,583 / 193,047 | 194,444 / 194,445 / 402,778 |

Toutes les durées sont en millisecondes. Le maximum de frame interval inclut la mise en route du premier profil ; le p95 suffit déjà à constater l'échec 72 Hz. La mesure ponctuelle rapporte `77 913 965` octets alloués et `222 298 112` octets réservés par Unity ; `dumpsys meminfo` rapporte `389 648 KiB` de PSS et `535 748 KiB` de RSS. Le statut thermique Android était `0`. Ce run court n'est pas une preuve d'endurance mémoire/thermique ; la preuve P10 séparée couvre déjà 30 minutes pour le rendu des sites.

## Borne réseau

Le casque était autorisé en USB mais son Wi-Fi était déconnecté lors de ce run. Aucun résultat réseau P11 n'est donc revendiqué. La preuve physique P06 existante fournit néanmoins un meilleur débit Quest utile de `38,873 Mbit/s` et un RTT WSS p95 de l'ordre de 18–19 ms.

À ce débit, le seul transfert des payloads complets, avant encodage, décodage, upload ou attente de frame, a pour borne basse :

| Profil | Borne transfert seul |
| --- | ---: |
| 1 colonne | 132,091 ms |
| 3 colonnes | 396,236 ms |
| 8 colonnes | 2 409,433 ms |

La cible command-to-visible p95 proposée à 100 ms est donc physiquement incompatible avec le renvoi intégral du bundle actuel à chaque pas, même pour une colonne, sur le réseau Quest déjà mesuré.

## Décision requise

Fermer D20 exige une décision explicite. L'option recommandée est de distinguer le commit visible atomique du message filaire : le client peut reconstruire un état complet à partir de contenus immuables ou inchangés déjà vérifiés et mis en cache, puis ne publier qu'une fois toutes les références résolues. Cela préserve l'atomicité visible et la précision float32, mais modifie la règle P11 selon laquelle chaque payload transporte systématiquement tous les octets ; ce n'est donc pas une optimisation silencieuse.

Les autres options sont de réviser la cible par profil, ou d'autoriser explicitement une compression/quantification. Un plafond de colonnes ou un état visible partiel reste interdit.

Après décision, il restera à intégrer le transport P15, refaire la mesure Wi-Fi command-to-visible réelle et exécuter l'autoplay physique 10 minutes ainsi que le scrub physique 60 secondes.

## Artefacts reproductibles

- build : `.artifacts/xr/d20-timeline/build-evidence.json` ;
- APK : `.artifacts/xr/d20-timeline/HiBoPXR-D20Timeline.apk`, SHA-256 `93b328486599ad728c6692675bce7a5cd72f12b2e9843ffa75f0ad9ea7da0d50` ;
- profil Quest : `.artifacts/xr/d20-timeline/quest/d20-timeline-profile.json` ;
- benchmark Windows final : `.test-results/unity-cli/d20/final-xr-results.xml` ;
- mémoire, thermique et logcat : `.artifacts/xr/d20-timeline/quest/` ;
- scripts : `XR/Tools/Build-P11Timeline.ps1` et `XR/Tools/Profile-P11TimelineQuest.ps1`.
