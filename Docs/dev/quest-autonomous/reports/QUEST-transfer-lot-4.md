# QUEST-transfer — lot 4 : codecs natifs et pipeline par blocs

Date : 16 septembre 2026. Référence : `visu_full_test / Small`.
Statut : **lot 4 validé sur deux envois physiques**, tests et builds Release
validés, autotests natifs IL2CPP réussis des deux côtés. Small publiée et rendue
en stéréo, sans anomalie signalée. Clic → confirmation : **20,027 / 20,241 s**,
soit environ **52 % de moins** qu'au lot 3. Voir le
[benchmark complet](QUEST-transfer-result-06-lot-4.md).
Les performances actuelles sont acceptées par l'utilisateur. La cible globale
réseau + 3–5 s et la fluidité parfaite restent des objectifs facultatifs ; les
[pistes futures sont consignées](QUEST-transfer-future-optimizations.md).
L'instrumentation était encore présente dans ces mesures. Son retrait et la
fusion native sont consignés dans le [lot 5 de clôture](QUEST-transfer-final.md).

## Décision et mesures isolées

Corpus exact du deuxième envoi du lot 3 : archive SHA-256
`cf26c192728b9b3ac1648946bbe7c9571adf378d9b505bb5203d2a8580026b2a`.
Sept ressources, **397 025 397 octets décompressés**, dont 139 275 466 pour le
pack et 15 289 823 pour le JSON. Archive ZIP antérieure : 124 501 270 octets.
Copie de travail : `.artifacts/quest-034/corpus/small-lot3.hbscene`.

Microbenchmark natif sur les sept fichiers, sans Wi-Fi ni restauration Unity :

| Codec / bloc | Octets encodés | Compression PC | Décompression Quest |
|---|---:|---:|---:|
| Brut / 256 Kio | 397 025 397 | — | 27 ms de copie |
| LZ4 / 256 Kio | 177 344 327 | 287 ms | 134 ms |
| Zstd 1 / 256 Kio | 136 826 795 | 557 ms | 491 ms |
| **Zstd 3 / 256 Kio** | **125 219 971** | **1 406 ms** | **608 ms** |
| LZ4 / 1 Mio | 177 361 543 | 443 ms | 136 ms |
| Zstd 1 / 1 Mio | 139 507 621 | 932 ms | 403 ms |
| Zstd 3 / 1 Mio | 128 505 476 | 1 681 ms | 511 ms |

Choix : **Zstd niveau 3, blocs indépendants de 256 Kio, repli brut si le bloc
compressé est plus grand**. Environ 0,58 % de données supplémentaires par rapport
au ZIP, avant les petits en-têtes. LZ4 économise du CPU mais ajoute 52 Mo au
transfert. Zstd 3 laisse une marge de production importante par rapport au débit
utile observé et économise du réseau par rapport à Zstd 1.

Le SHA-256 natif des 397 Mo prend environ 180 ms sur PC et 387 ms sur Quest
dans ce banc. Les codecs sont vérifiés octet par octet ; les vecteurs SHA,
réinitialisations et offsets passent sur les deux architectures. Ce sont des
mesures CPU isolées, réalisées une fois par combinaison, sensibles aux caches,
à la température et à la fréquence du processeur. Elles n'incluent pas le coût
P/Invoke/IL2CPP, les écritures disque ou la remise en état de la scène.

Preuves : `.test-results/quest-034/codec-{windows,quest}.jsonl` et
`native-selftest-{windows,quest}.jsonl`.

## Chemin de production

1. Capture détachée sur Unity identique au lot 3 ; les données scientifiques et
   les ressources préparées restent inchangées.
2. Le worker encode les métadonnées et établit le manifeste des ressources.
   La connexion TLS peut démarrer pendant cette préparation.
3. Le worker compresse chaque bloc, calcule son empreinte, écrit le spool puis
   le propose à une file bornée à deux blocs. L'envoi commence sans attendre la
   compression de l'ensemble de la scène.
4. Le Quest lit les blocs sur TLS ; un worker les vérifie, les décompresse et les
   écrit directement dans les fichiers de restauration. Le ZIP reçu et son
   extraction ultérieure disparaissent de ce chemin.
5. Les empreintes de chaque ressource native et de chaque élément du pack sont
   vérifiées pendant l'écriture. Le manifeste et tous les en-têtes de blocs sont
   liés par une empreinte finale. L'archive n'est scellée qu'après validation.
6. La désérialisation JSON, les contrôles scientifiques et la restauration
   commune existants s'exécutent ensuite. La publication atomique précède l'ACK.

La mémoire du pipeline dépend de quelques blocs, pas de la taille totale du
transfert. Le snapshot détaché et ses tableaux conservent leur coût propre ;
le lot 4 ne prétend pas les supprimer. Les files appliquent une contre-pression
au producteur ou à la lecture réseau lorsque l'étape suivante est plus lente.

Les compteurs `blocks.*` distinguent compression, empreintes, source/disque,
écriture du spool, attente de file, TLS, décodage et écriture/vérification finale.
Les durées de phases qui se chevauchent **ne doivent pas être additionnées**.
Les marques `send.payload.*` et `receive.payload.*` restent disponibles, ainsi que
les mesures de frames, mémoire, JSON et restauration du lot 3.

## Contrat HBT4

Le schéma de scène reste **4** ; seule l'enveloppe de transport passe de HBT3 à
HBT4. Le préfixe est constitué de `H`, `B`, `T`, puis de l'octet numérique `4`.
Tous les entiers sont little-endian.

- Préfixe, longueur int32 du manifeste, puis manifeste : nombre int32 de
  ressources ; pour chacune longueur de nom sur un octet, nom ASCII et taille
  décompressée int64. Noms uniques, 80 caractères maximum, 100 000 ressources,
  manifeste 16 Mio maximum, données décompressées 4 Gio maximum.
- Ordre obligatoire pour une scène : `visualization.json`, `globals.refs.json`,
  `buffers.index`, `buffers.pack`, puis ressources nommées par leur SHA-256.
  Budget combiné des trois métadonnées : 128 Mio. Le total des ressources
  logiques, entrées du pack comprises, reste limité à 100 000.
- En-tête DATA de 64 octets : type 1 en 0, codec en 1 (0 brut, 2 LZ4, 3 Zstd),
  séquence int32 en 4, ressource int32 en 8, offset int64 en 12, longueur brute
  int32 en 20, longueur encodée int32 en 24, SHA-256 encodé en 28–59.
  Octets réservés nuls. Blocs reçus au plus 1 Mio ; tailles et offsets exacts.
- En-tête END de 64 octets : type 2, nombre de blocs en 4, total brut int64 en
  12, total encodé int64 en 20, empreinte du transcript en 28–59. Le transcript
  couvre préfixe, longueur, manifeste et tous les en-têtes DATA dans l'ordre.
  Le SHA de chaque bloc contenu dans ces en-têtes lie aussi tous les octets
  compressés. END est obligatoire et doit correspondre aux totaux vérifiés.
- Zstd : une seule trame, taille explicite, pas de dictionnaire, fenêtre au plus
  1 Mio, taille décodée exacte. Aucun accès natif scientifique avant validation.
- L'ACK existant de 33 octets conserve statut et empreinte, désormais celle du
  transcript HBT4. TLS et l'authentification existants restent nécessaires.

HBT3 reste supporté pour les données globales d'appairage, les archives et les
tests antérieurs. Les données globales n'acceptent pas HBT4. Un ancien Quest
rejette explicitement cette nouvelle version ; installer les deux players du
lot 4 ensemble. Le chemin automatique HBT4 concerne Windows x64 → Quest ARM64 ;
les autres Desktop conservent leur chemin antérieur.

## Propriété, échecs et Retry

Le delivery possède le snapshot jusqu'à la fin du worker. Le spool est ouvert
sans partage en écriture et conservé jusqu'à la destruction du delivery. Après
finalisation, Retry relit exactement ces octets ; il ne recapture ni ne relit
les fichiers source. Une perte d'ACK peut donc être rejouée sans double
publication. Les identités déjà remplacées ou fermées conservent leurs tombstones.

Un échec avant finalisation annule et rejoint le producteur, même si la connexion
échoue avant le premier appel à SendAsync. Cette offre incomplète est détruite ;
il faut alors un nouvel envoi complet. Une annulation durant réception rejoint
le décodeur avant de supprimer ses ressources. L'archive conserve le token de
session après la fin du pipeline pour que JSON et restauration restent annulables.
Une perte d'ACK après publication ne détruit pas la scène affichée.

La revue indépendante a identifié les cas « connexion refusée avant SendAsync »
et « token lié détruit avant ReadPrepared » ; corrections et régressions ajoutées.

## Dépendances et distribution

Plugin dédié `hbp_transfer`, séparé des versions scientifiques verrouillées.
Zstandard 1.5.7, LZ4 1.10.0, SHA-256 BCrypt sur Windows et Mbed TLS 3.6.4 sur
Android avec sélection matérielle ARMv8 à l'exécution. Les hashes des sources,
versions, wrappers et binaires sont consignés dans `Tools/TransferNative.lock.json`.
`Tools/Build-TransferNative.ps1` reconstruit depuis les archives vérifiées ; les
builds players et l'inspection de l'APK contrôlent les pins. Bibliothèque Android
ARM64 alignée à 16 Kio ; licences redistribuées dans StreamingAssets/Licenses.

Sources amont : [Zstandard](https://github.com/facebook/zstd/releases/tag/v1.5.7),
[LZ4](https://github.com/lz4/lz4/releases/tag/v1.10.0),
[SHA-256 Mbed TLS](https://github.com/Mbed-TLS/mbedtls/blob/v3.6.4/library/sha256.c).

## Validation et limites

### Validation automatisée

- **102/102 EditMode** : `.test-results/quest-034/editmode.xml`. Parité SHA-256
  (vide, offsets, reset, plusieurs blocs), codecs LZ4/Zstd, données compressibles
  et repli brut, ordre/offsets, codec inconnu, corruption des octets et du hash
  final, troncature, dimensions hors budget, manifeste modifié, pack logique
  corrompu malgré un transport valide, références globales canoniques,
  annulation après réception, file pleine, connexion refusée avant SendAsync
  et replay identique après perte d'ACK.
- **8 scénarios PlayMode validés** : six scénarios de préparation native/desktop
  et le scénario six modalités dans ses deux variantes ZIP/HBT4. Premier run :
  `playmode.xml` (7 succès ; nouveau cas bloqué par une archive de fixture
  manquante pour les essais ultérieurs de remplacement invalide). Fixture
  corrigée ; `playmode-final.xml` : **2/2 succès** pour les deux variantes complètes.
  Préservation des colonnes indépendantes, recapture, géométrie, activité,
  annulation et non-remplacement de la scène en cas de configuration invalide.
- Autotest de démarrage ajouté à l'instrumentation temporaire : SHA « abc » et
  vide, reset, roundtrip Zstd via P/Invoke. Il produit `QUEST_CODEC_READY` et sera
  vérifié dans les deux players IL2CPP et dans le run PlayMode final.
- Une revue indépendante de concurrence et de propriété, puis une relecture
  des corrections : défauts initiaux corrigés, aucun autre blocage relevé.
- `Tools/format-code.cmd` et `git diff --check` exécutés.

### Résultat du test physique

Ce lot chevauche compression, transport et décodage/écriture/vérification.
Il ne chevauche pas encore la désérialisation JSON et la création des objets
natifs avec la réception. Ces étapes et le snapshot demeurent sur le chemin
critique ; même un pipeline limité par le Wi-Fi ne rendrait pas toute la durée
après clic égale au seul temps réseau.

Deux envois complets par IP manuelle Wi-Fi ont été mesurés, calcul automatique
désactivé et casque porté. Les caches passent de 42 misses à 42 hits des deux
côtés. La compression coûte 0,980/1,026 s, la décompression 1,003/0,724 s,
chevauchées avec la réception de 7,993/9,006 s. Après réception, il reste
8,793/8,386 s : métadonnées puis restauration. Le pipeline suit le débit utile,
mais le parcours complet n'est pas encore dominé par le seul transfert.

La préparation Pair est comptée séparément : 9,314 s Desktop et 5,371 s Quest.
Le correctif post-lot-3 est confirmé : intervalle maximal Desktop de 17,78 ms
pendant Pair, malgré une préparation plus longue que lors du benchmark antérieur.
Pendant l'envoi, des pauses de 1,27–1,59 s Desktop et environ 0,51 s Quest
subsistent. Détails, mémoire, réserves et prochaines priorités dans le
[résultat du lot 4](QUEST-transfer-result-06-lot-4.md).

## Players et mise en place

- Desktop Release IL2CPP :
  `.artifacts/quest-034/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
  `GameAssembly.dll` SHA-256 :
  `b0e5f77bd99b07f8c3125b3fcef50c0e6adaea8c5c40ca2a156b21a8acd64690`.
  Codec installé conforme au pin ; licences incluses. Lancé avec
  `-p visu_full_test -v Small`, PID 30108. Le journal
  `.test-results/quest-034/desktop-player.log` confirme `QUEST_CODEC_READY`.
- Quest Release IL2CPP ARM64 : `.artifacts/quest-034/Android/HiBoP.Quest.apk`.
  SHA-256 `e7cb55b2a8aea80596e2aa87b7f933d1ed3fda2140c06e4ec3754d65df3a6e38`.
  336 489 336 octets, dix bibliothèques ARM64 vérifiées, pins scientifiques et
  codec conformes, licences incluses, signature habituelle conservée.
  Preuve : `.test-results/quest-034/apk-content.json`.
- Le premier build Android a révélé la liste fermée des plugins autorisés dans
  `HBPBuildProfiles.Validate`. Ajout ciblé de `libhbp_transfer.so` aux plugins
  ARM64 requis ; les contrôles de plateforme restent appliqués. Build suivant
  réussi, zéro erreur dans le rapport de build. Les avertissements XR de fichiers
  de simulation temporaires étaient déjà présents dans les builds du lot 3.
- Installation `adb install -r` réussie, données conservées. Horizon OS intercepte
  le lancement avec `LaunchCheckControllerRequiredDialogActivity`. Réveil des
  deux contrôleurs demandé à l'utilisateur. Après son signal « prêt », démarrage
  du processus **24242**, `QUEST_CODEC_READY` confirmé à 17:07:45 et
  `COMPOSITION READY; problem=none`, OpenXR/Oculus, Vulkan, SinglePassInstanced,
  suivi de tête actif. Journal : `.test-results/quest-034/quest-startup.log`.
  Ce contrôle a précédé les deux transferts mesurés dans le rapport de résultats.
- USB connecté, batterie 100 %, Wi-Fi `192.168.1.18`. Préférence Desktop
  `AutomaticEEGUpdate=false` vérifiée. Inventaires des traces précédentes conservés
  dans `.test-results/quest-034/`, sans effacement.

### Procédure utilisée pour le benchmark (terminé)

1. Garder le câble USB branché, réveiller les contrôleurs et ouvrir HiBoP.
2. Attendre Small complètement chargée sur le nouveau Desktop. Dans le panneau
   Quest, choisir l'IP manuelle **192.168.1.18**, puis **Pair**. Attendre
   **Paired and connected** ; cette étape mesure séparément la préparation initiale.
3. Casque porté et actif, cliquer **Envoyer au Quest**. Attendre Small complète
   et utilisable, puis **10 secondes supplémentaires**.
4. Sans changer la scène, fermer l'application ou refaire Pair, cliquer une
   deuxième fois **Envoyer au Quest**. Ne pas utiliser Retry. Attendre à nouveau
   l'affichage complet, puis 10 secondes.
5. Signaler « terminé » et toute anomalie, en précisant l'envoi concerné. Laisser
   le câble branché pour la collecte ; aucun troisième envoi n'est nécessaire.
