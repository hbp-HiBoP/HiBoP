# Protocole de synchronisation

Le wire transporte des opérations absolues et résultats bornés sur le flux duplex authentifié de l'appairage. Il ne transporte ni événements UI ni snapshots différentiels par interaction. Les codecs cités ici sont l'autorité exécutable ; valeurs vérifiées le 2 novembre 2026.

## Formats et compatibilité

| Couche | Format courant et source |
| --- | --- |
| Mutation | `HBS2`, protocole 2, header 136 octets ; [MutationEnvelopeCodec / MutationPayloadCodec](../../../../Assets/Scripts/HBP/Sync/MutationCodec.cs). Payload schema **2** uniquement, borne logique 16 Mio. |
| Transport auxiliaire | `HBT2`, protocole 2, header 136 octets, payload maximal 64 Kio ; [TransportFrameCodec](../../../../Assets/Scripts/HBP/Transfer/Transport/TransportProtocol.cs). |
| Checkpoint de publication | Schema **7** uniquement ; [SceneMutationCheckpointCodec](../../../../Assets/Scripts/HBP/Sync/Scene/PublicationJournal.cs). |
| Corrélation dans checkpoint | Record schema **2** uniquement ; [CorrelationCheckpointRecord](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs). |
| Progrès applicatif | `HBRP`, schema **1**, 86 octets ; [RetentionProgress](../../../../Assets/Scripts/HBP/Sync/RetentionProgress.cs). |
| Réconciliation | `HBR3` ; [SceneReconciliationProtocol](../../../../Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs). |

Le retrait des anciens noms V2 n'a pas renuméroté ces protocoles. Les schémas expérimentaux, anciens checkpoints et ancien payload timeline ne sont plus acceptés ; déployer les pairs compatibles ensemble. Le format de [livraison complète](../transfer.md) et les fichiers projets Desktop sont des contrats séparés.

Le header mutation encode, dans l'ordre : magic/version/kind/flags/longueurs (16 octets), UUID session/scene/incarnation/opération/stream (5 × 16), séquences reliable/origin/canonical/observed (4 × 8), origine (1), puis sept octets réservés nuls. Offsets : 16/32/48/64/80 pour les UUID, 96/104/112/120 pour les séquences, 128 pour l'origine. Flags bits 0/1 indiquent canonical/observed ; valeur absente = zéro. Desktop transmet canonical non nul, Quest observed (zéro initial autorisé). `HBT2` utilise ses champs lane/chunk/schema dans les octets 129–135 ; ne pas appliquer le layout réservé `HBS2` à ces frames.

Entiers et binary32 sont little-endian. Les UUID utilisent les octets `Guid.ToByteArray()` du codec, sans conversion implicite vers l'ordre réseau d'une chaîne UUID. Identités texte : UTF-8 strict, NFC, non vides, sans contrôles, au plus 256 octets. Les flottants doivent être finis ; les contrats concernés rejettent `-0`. La frontière de coupe canonicalise les zéros issus des normales natives en `+0`, sans détendre le wire. Une coupe transmet orientation/flip/nombre/position normalisée/normal complet, avec normal non nul et position dans [0,1]. Les contrôles de longueur/version/réserves ont lieu avant allocation du corps ; octets finaux inattendus refusés. TLS fournit l'intégrité de trame ; les hashes de contenu valident les dépendances/bodies, pas une seconde passe systématique par frame.

## Ordre et ordonnancement

[Scheduler](../../../../Assets/Scripts/HBP/Sync/Scheduler.cs) distingue lanes de priorité et streams fiables. Il existe un stream de contrôle par direction, un stream d'opérations par incarnation/direction, et un stream bulk indépendant par body. Une opération structurelle reste dans l'ordre du stream scène même si elle utilise une priorité contrôle. Seule une fermeture invalidante peut dépasser cet ordre en déclarant les watermarks à appliquer/abandonner.

Les clés de coalescing, clés touchées et barrières viennent des handlers validés, jamais d'une liste libre de l'UI. Un remplacement n'affecte qu'un slot non envoyé de même clé ; une barrière structurelle/job/transaction scelle les slots précédents. La séquence d'origine est attribuée à l'engagement wire après coalescing, donc aucun faux trou pour une preview remplacée. Une frame déjà écrite garde octets, ID et séquences jusqu'à ACK ou expiration de grâce.

Les gros corps utilisent un petit descripteur ordonné (identités, schema, longueur, digest, stream et barrières), puis chunks bulk. Validation complète avant une application atomique. Les opérations déjà décodées indépendantes peuvent progresser ; les clés qui se chevauchent attendent leur barrière. Cela inclut les grosses corrections autoritaires, reconnues par [SceneOperationBulkReceiver](../../../../Assets/Scripts/HBP/Sync/Scene/SceneOperationBulkReceiver.cs), et pas seulement les mutations.

| Borne par défaut | Valeur |
| --- | --- |
| Inline / chunk / rafale interactive | 4 Kio / 16 Kio / 8 opérations avant service bulk |
| File scène | 256 records, 256 Kio ; réserve contrôle 16 records/16 Kio |
| File contrôle session | 64 records, 64 Kio ; réserve 8 records/8 Kio |
| Retry fiable | 128 frames, 256 Kio ; réserve 8 frames/16 Kio |
| Bodies bulk retenus | 8 transferts ; 16 Mio chacun, 32 Mio cumulés |
| Clés touchées d'un record | 128 |
| Décisions non terminées / clés de conflit / watermarks scène | 4096 / 65536 / 4096 |
| Réception transport | 256 records / 4 Mio ; 128 records hors ordre |
| Reprise | 128 streams dont 126 ordinals bulk non retirés par direction |

Ces plafonds sont des bornes d'admission, pas une promesse de performance ou une taille scientifique conseillée. Sous pression, previews non envoyées coalescent et bulk attend ; le dernier état et le contrôle requis ne sont jamais silencieusement perdus. L'impossibilité de retenir un travail fiable devient une erreur explicite. Unity n'attend pas la capacité socket. Reader/writer indépendants, writer réveillé par signal, TCP NoDelay ; le liveness ne dépend pas du PlayerLoop.

## Acquittements et mémoire bornée

L'ACK réseau signifie admission dans la file bornée, **pas application Unity**. Les ACK/pings éphémères ne consomment pas de séquence fiable et ne créent pas de boucle ACK-d'ACK. Les jobs et checkpoints possèdent leurs barrières métier séparées.

HBRP encode magic (4), schema/origine (1+1), UUID session/scene/incarnation (48), puis quatre `u64` : `AppliedOriginThrough`, `MinimumObservedCanonicalSequence`, `RetiredCanonicalThrough`, `CurrentCanonicalSequence`. Scope, direction, monotonie et bornes connues sont vérifiés.

- Quest annonce un **préfixe contigu terminé**, pas le maximum reçu. Bulk incomplet, correction ou checkpoint non committé laisse un trou.
- Le minimum observé comprend toutes les propositions Quest, différées ou déjà envoyées. Desktop ne retire une clé/tombstone de conflit qu'après cette preuve et conserve un plancher empêchant une vieille proposition de contourner le conflit.
- Desktop renvoie le préfixe appliqué et le plancher canonique sûr. Quest peut alors libérer identités reçues et clés devenues inutiles, en conservant ce qui protège rollback et replay.
- Un seul progrès immuable est en vol par direction, avec la dernière valeur non envoyée et une notification reçue remplaçable. Chaque progrès reçu est contrôlé avant remplacement.

Ainsi 4096 borne le travail **non résolu**, pas le total d'éditions d'une session. La réutilisation d'un UUID avec payload différent est refusée dans la fenêtre active ; aucun registre universel des UUID de toute la vie du processus n'est promis.

Les records courants, lookahead de réduction des previews et bodies reconstitués appartiennent à la session Quest jusqu'au succès. Une coupure après ACK ne peut les faire disparaître. L'enregistrement de fin d'application sur Unity est indépendant de l'annulation de cette connexion ; les pompes de réception ne se chevauchent pas.

Les bulk IDs portent un ordinal par session/origine. Après achèvement ou annulation, le préfixe retiré est transmis dans ResumeHello/`BulkStreamRetirement` ; il permet d'ignorer les anciens duplicatas sans conserver leurs GUID indéfiniment. Pendant la grâce de 500 ms, chaque stream rejoue ses frames non acquittées avant ses nouvelles séquences, en conservant la priorité interactive/contrôle par rapport au bulk. Après grâce, le [protocole de réconciliation](synchronization-architecture.md#coupure-et-réconciliation) prend le relais.

## Rejets et contrôle de session

Un rejet métier contient ID et raison bornée : cible/valeur invalide, dépendance absente, schema non pris en charge ou opération sensible occupée. Il ne termine pas les opérations indépendantes. Un défaut de framing/authentification/négociation peut fermer la session ; la [réserve sur la classification des erreurs Quest](synchronization-validation.md#points-à-qualifier) demeure explicitement documentée.

Les préférences utilisent le contrôle authentifié de session, séparé des opérations scène : commande pairing 15, corps maximal 1 Mio, résultat 1024 caractères, atlas 128 caractères, génération de connexion/UUID/révision. Historique Quest et file de sauvegardes Desktop bornés à 64. L'ACK suit l'application ; une réutilisation UUID avec contenu changé est rejetée. Voir [SessionControlProtocol](../../../../Assets/Scripts/HBP/Transfer/Transport/SessionControlProtocol.cs) et les [contrats de session](synchronization-architecture.md#préférences-et-atlas-de-session).
