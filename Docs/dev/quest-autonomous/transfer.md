# Transfert de scène et Display synchronisé

Contrats relus dans le code le 2 novembre 2026. Les formats de scène, de transport et du prototype HBNA sont indépendants. Les [mesures historiques](validation.md) ne fixent pas un délai garanti.

## Capture et propriété

[Base3DScene.Transfer](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Transfer.cs) attend les ressources de la scène et protège sa représentation avant capture. [DesktopSceneCapture](../../../Assets/Scripts/HBP/Transfer/Scene/DesktopSceneCapture.cs) détache le graphe sur Unity sans mutation intercalée ; sérialisation, empreintes et compression travaillent ensuite sur ce snapshot possédé. Déplacer la lecture du graphe vivant dans `Task.Run`, ou l'étaler par des `await`, ne garantit pas un snapshot cohérent.

Les caches de surfaces conservent des géométries/octets immuables sous versions et budget (128 Mio par cache de capture/réception), avec des références retenues pendant l'usage. Ils ne partagent pas implicitement des masques mutables ou un objet natif entre colonnes. Une référence répétée du pack produit des tableaux indépendants chez les consommateurs qui peuvent les modifier. Les ressources utilisateur gardent leur provenance ; une ressource vérifiée et privée peut réutiliser cette preuve pendant sa durée de vie, pas sur la seule foi d'un nom SHA ou d'un timestamp.

La préparation native reste privée jusqu'à validation complète de l'archive. Le pipeline actuel ne publie pas un objet à partir de métadonnées partiellement reçues. GIFTI conserve sa sérialisation et les opérations Unity restent sur le thread principal. Voir les [règles de durée de vie](architecture.md#durée-de-vie-et-concurrence).

## Archive de scène

[ScenePayload](../../../Assets/Scripts/HBP/Transfer/Scene/ScenePayload.cs) utilise le format **4**, avec lecture du format **3** conservée. [SceneArchive](../../../Assets/Scripts/HBP/Transfer/Scene/SceneArchive.cs) et [BufferPack](../../../Assets/Scripts/HBP/Transfer/Scene/BufferPack.cs) sont les références des validations.

| Élément | Contrat |
| --- | --- |
| Métadonnées | Visualisation et données préparées ; types/références autorisés, propriétés JSON dupliquées rejetées. |
| Références globales | Table compacte contre le contexte possédé ; deux objets de même ID ne doivent pas masquer des définitions incompatibles. |
| `buffers.index` | Magic `HBPI`, version 1, little-endian : en-tête 12 octets, puis entrées de 48 octets : SHA-256 (32), offset (uint64), longueur (uint64). |
| `buffers.pack` | Plages bornées avec couverture exacte et contiguë ; pas de chevauchement, trou ni octets surnuméraires. Lectures coordonnées, pas de curseur partagé sans verrou. |
| Fichiers natifs | Ressources nécessaires aux API par chemin, identités et compagnons vérifiés ; aucun chemin arbitraire accepté depuis le réseau. |

Les plafonds courants sont 4 Gio décompressés, 128 Mio de métadonnées et 100 000 ressources/entrées selon le conteneur. Les noms wire sont ASCII bornés à 80 caractères ; les noms de ressources natives sont validés par le contrat d'archive. Ces plafonds d'entrée ne sont pas des budgets de RAM : ancienne scène, candidate, volumes convertis, caches et GPU coexistent potentiellement.

Les références standard MNI/atlas sont installées localement et résolues par identité logique et disponibilité. Ne pas remplacer ce contrat par une comparaison globale supposée des hashes des deux installations. Le packaging Unity des ressources compressées utilise notamment `.gz.bytes`.

## Streaming HBT4

[BlockContainer](../../../Assets/Scripts/HBP/Transfer/Transport/BlockContainer.cs) commence par les octets `H`, `B`, `T`, `4` (dernier octet numérique), puis un manifeste borné à 16 Mio. [SceneArchive.Blocks](../../../Assets/Scripts/HBP/Transfer/Scene/SceneArchive.Blocks.cs) impose l'ordre métadonnées de visualisation, références globales, index, pack, puis ressources externes.

Les enregistrements DATA/END ont un en-tête de 64 octets. Séquences, indices de ressource, offsets, tailles et octets réservés sont contrôlés ; les hashes de blocs et le transcript final lient le contenu et son ordre. Corruption, troncature, expansion hors borne ou incohérence empêchent la publication. TLS et l'association authentifiée restent nécessaires : un SHA ne prouve pas l'identité de l'émetteur.

La production utilise **Zstd niveau 3 par blocs indépendants de 256 Kio**, avec bloc brut si la compression n'est pas plus petite ; files bornées à deux blocs. Le lecteur accepte les codecs autorisés brut/LZ4/Zstd avec sortie bornée à 1 Mio. Le décodeur natif Zstd refuse dictionnaire, fenêtre supérieure à 1 Mio, contenu de taille inattendue et frames supplémentaires. [TransferCodec](../../../Assets/Scripts/HBP/Transfer/Codecs/TransferCodec.cs) appelle `hbp_codec_encode/decode` de `hbp_core` ; les primitives SHA y sont également intégrées. Une ancienne bibliothèque autonome de transfert n'est pas requise.

[BlockDelivery](../../../Assets/Scripts/HBP/Transfer/Transport/BlockDelivery.cs) produit un spool finalisé pour reprendre **les mêmes octets et la même identité**. Un producteur incomplet doit être annulé et rejoint ; on ne relance pas le graphe mutable sous une ancienne identité. [SceneDelivery](../../../Assets/Scripts/HBP/Transfer/Scene/SceneDelivery.cs) conserve les utilisateurs actifs, sérialise les envois et reporte la suppression jusqu'au drainage. Un reçu perdu ne prouve pas que la publication distante a échoué ; les incarnations, reçus et tombstones empêchent duplication et résurrection tardive. Le marqueur de publication n'est pas une mesure de photons affichés dans le casque.

## Un lot Display

[SceneDisplayOperation](../../../Assets/Scripts/HBP/UI/Main/Visualization/SceneDisplayOperation.cs) possède le cycle visible et [QuestManager.Display](../../../Assets/Scripts/HBP/UI/Quest/QuestManager.Display.cs) réserve les publications auprès de l'auto-synchronisation. L'admission précède les envois ; plusieurs consommateurs peuvent attendre un travail partagé sans en devenir tous propriétaires.

Le succès attend données/rendu locaux prêts, publication de la bonne incarnation sur Quest, synchronisation initiale et libération confirmée du lot. Les scènes sont traitées dans l'ordre demandé, avec une seule publication distante à la fois. Une capture peut être préparée en avance, sous **estimation de 512 Mio** ; une scène plus grande repasse en préparation séquentielle. Cette borne ne limite pas toute la mémoire du processus.

Le pourcentage est monotone, pondéré actuellement à 15 % chargement local et 85 % publications, plafonné à 99 % avant fin effective. Il indique un avancement estimé, pas un temps restant. Une demande commencée sans Quest reste locale ; un casque connecté ensuite ne rouvre pas ce cycle. La cible, le contexte et la génération de connexion sont fixés pour le lot.

En cas de panne distante, les scènes Desktop terminées restent utilisables. Les reprises explicites ciblent les scènes incomplètes et valident leur état courant, sans recharger systématiquement les données. **Annulation utilisateur et interruption réseau sont distinctes** : l'annulation peut présenter `Transfer` / `Close` pour résoudre les copies incomplètes quand le partage automatique reste actif ; une interruption de transport éligible conserve la reprise automatique de connexion. Une erreur de données exige une action. Fermeture et recapture attendent les acquittements des incarnations concernées ; un ancien callback ne doit ni publier un succès ni fermer une nouvelle scène.

Les anciens plans imposant une reprise manuelle après toute coupure sont donc dépassés. Les tests source [SceneDisplayOwnershipTests](../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/SceneDisplayOwnershipTests.cs), [QuestNetworkTests](../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/QuestNetworkTests.cs) et [BufferPackTests](../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/BufferPackTests.cs) décrivent notamment annulation, admission, génération, reprise et indépendance des buffers. Ils n'ont pas été exécutés pour cette consolidation documentaire.

## HBNA : contrat du harness de prototype

Le codec reste dans [Assets/Tests/Support/TransferPrototype](../../../Assets/Tests/Support/TransferPrototype/Anatomy/AnatomySnapshotCodec.cs), avec lecteurs v1/v2/v3/v4. Il sert aux fixtures/diagnostics de parité, pas au parcours de scène courant : v1 anatomie, v2 contacts, v3 projection densité, v4 iEEG. Son format little-endian transporte float32/uint32, unités et repères explicites, matrices row-major, couleur linéaire et hashes surface/contenu. Il borne le fichier à **128 Mio**, les sommets à **2 000 000**, les indices à **12 000 000** et chaque identifiant UTF-8 à **1 024 octets**. Le nombre d'indices doit être multiple de trois ; les UV sont absents ou correspondent aux sommets.

Une limitation du harness, telle que HBNA v3 exigeant `Smooth activity boundaries`, ne devient pas une limitation générale du produit actuel. Les [fixtures](validation.md#fixtures-et-preuves) et leurs preuves numériques gardent leurs formats et leurs binaires d'origine.
