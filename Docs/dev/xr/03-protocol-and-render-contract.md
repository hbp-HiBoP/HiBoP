# HiBoP XR — protocole et contrat de rendu

**Version :** 0.3
**Statut :** contrat logique normatif ; D10/D11 acceptées provisoirement pour Windows/Quest, qualification macOS/Linux différée

## 1. Principes

- Desktop autoritaire ; Quest envoie des intentions.
- État et résultats sont explicitement versionnés.
- Les gros tableaux ne sont ni JSON ni base64.
- Les assets immuables sont adressés par SHA-256.
- Contrôle et commandes ne sont pas bloqués par un asset volumineux.
- Les résultats dynamiques sont latest-wins, jamais mis en file indéfiniment.
- Le snapshot complet est la baseline transactionnelle de connexion et reconnexion ; les deltas sont une optimisation optionnelle.
- Les contenus temporels admis sont préchargés une fois puis sélectionnés localement par index.
- Le schéma est AOT/IL2CPP-safe et indépendant de Meta.

## 2. Transport proposé

Un endpoint local authentifié expose :

- WSS : handshake, contrôle, commandes, états, erreurs, petits résultats et notifications ;
- HTTPS : téléchargement chunké/reprenable/annulable d'assets et gros résultats immuables.

Découverte locale facultative ; IP/hostname + port toujours disponible. Première confiance par code court et preuve de possession de la clé de l'hôte ; empreinte épinglée ensuite. Le mécanisme TLS et le pinning utilisent une bibliothèque maintenue.

La bibliothèque n'est pas choisie avant le test Windows/macOS/Linux + Quest IL2CPP. QUIC ou plusieurs ports ne sont introduits qu'avec une preuve supérieure.

## 3. Handshake

```text
ClientHello
  protocolMajor, protocolMinor
  supportedSchemaHashes[]
  xrAppVersion, buildCommit
  capabilities[]
  deviceClass
  clientNonce

ServerHello
  selectedProtocol
  selectedSchemaHash
  desktopAppVersion, buildCommit
  hbpCoreVersion
  capabilities[]
  sessionId
  sessionEpoch
  serverNonce
  compatibilityDecision
```

Règles :

- major différent ou aucun schéma commun : refus ;
- minor différent : capability negotiation ;
- version d'app informative, jamais substitut au protocole ;
- appairage/auth avant envoi d'état ;
- toute nouvelle session Desktop crée un nouvel epoch.

## 4. Enveloppe

```text
MessageHeader
  magic
  headerVersion
  protocolMajor/minor
  messageType
  flags
  sessionId
  sessionEpoch
  messageId
  correlationId
  payloadCodec
  payloadLength
  uncompressedLength
  checksum
```

Entiers little-endian et tailles bornées par type de message. Le décodeur vérifie header, longueur, codec, checksum et allocation maximale négociée avant d'allouer. Un message inconnu non critique est ignorable selon son flag ; un message critique inconnu ferme proprement la session.

## 5. IDs, scopes et révisions

```text
Stable IDs
  projectId, visualizationId, columnId, brainInstanceId
  surfaceAssetId, siteAssetId, siteId
  cutId, roiId, timelineId, materialProfileId

Ordering
  stateRevision              transaction globale
  scopeRevision              ordre dans un scope
  assetHash/assetRevision    identité immuable
  renderRevision             résultat produit
  commandId                  idempotence
  interactionId + sequence   coalescence d'un geste
```

Les indices de tableaux sont valides seulement avec l'asset hash auquel ils appartiennent. Ils ne remplacent jamais un ID stable sur le wire.

Pour P09, les scopes Desktop portent explicitement `VisualizationEntity`, `ColumnEntity` et `ColumnVisualization`. Ces propriétés relient les `scopeId` à leurs `visualizationId`/`columnId` opaques sans supposer leur égalité et rendent les bindings `BrainInstance` déterministes.

## 6. Commande et résultat

```text
Command
  sessionId/epoch
  commandId
  interactionId?
  sequence?
  scopeType, scopeId
  baseScopeRevision
  commandType
  payload

CommandOutcome
  commandId
  accepted
  errorCode?
  resultingStateRevision?
  resultingScopeRevision?
  canonicalValue?
```

Commandes V1 : sélection site/colonne, paramètres de représentation, couches, opacité, seuils, coupes, ROI, timeline/playback et demandes/fermetures d'instances. Une commande répétée avec le même `commandId` produit le même outcome logique.

Le client peut afficher une valeur optimiste locale avec un état `pending`. `CommandOutcome` accepte cette valeur ou fournit la valeur canonique qui doit la remplacer visiblement. Le dernier état validé par le Desktop gagne toujours.

## 7. Snapshot, deltas et reprise

`SessionSnapshot` contient :

- epoch et révision transactionnelle ;
- visualisations/colonnes/timelines disponibles ;
- propriétés sémantiques avec scopes ;
- coupes/ROI/sélections ;
- inventaire d'assets par hash ;
- instances proposées, sans disposition XR ;
- capabilities effectives.

Le client construit un nouvel état hors ligne puis effectue un swap atomique. Les deltas sont appliqués uniquement sur la révision de base attendue.

À la reconnexion V1, le serveur répond par défaut `FullSnapshotRequired` et le client reconstruit son miroir hors ligne avant swap atomique. `ResumeRequest` peut annoncer epoch, révisions et assets encore présents afin d'éviter les retransferts byte-identiques. `ResumeWithDeltas` reste une capability optionnelle si un journal borné est disponible ; aucun déploiement V1 ne peut l'exiger. `NewSession` est renvoyé si l'epoch a changé.

Pendant la coupure, tracking, passthrough et transformations spatiales restent locaux. Les commandes et changements scientifiques sont gelés et la déconnexion est visible.

## 8. Transfert d'assets

```text
AssetDescriptor
  assetId, sha256, assetType
  schemaVersion
  byteLength, uncompressedLength
  codec
  elementType, dimensions, stride
  coordinateSystem
  dependencies[]

AssetChunk
  hash
  offset
  length
  checksum
  bytes
```

Le cache Quest est en mémoire. Un téléchargement peut reprendre sur des ranges validés ; un hash final incorrect invalide l'asset entier. Les assets identiques ne sont pas retransmis entre colonnes/instances.

## 9. `SurfaceAsset`

```text
SurfaceAsset
  assetId/hash
  representation            anatomical | inflated | other
  coordinateSystem
  positions float32[vertex,3]
  normals float32[vertex,3]
  indices uint32[triangle,3]
  optional static UV/attributes
  bounds
  materialProfileId
```

Le MNI gris combiné de référence contient 69 104 sommets. Positions/normales/indices sont partagés ; les attributs dynamiques restent hors asset.

## 10. `DynamicFrameBundle`

```text
DynamicFrameBundle
  sessionId/epoch
  timelineId
  playbackRevision
  logicalTime
  temporalIndex
  temporalAlpha
  sourceStateRevision
  expectedColumnIds[]
  columnFrames[]

ColumnFrame
  columnId
  surfaceAssetHash
  visualParametersRevision
  surfaceFrame?
  siteFrame?
  cutOverlays[]

SurfaceFrame
  vertexCount
  value0 float32[vertex]
  value1 float32[vertex]
  activeMask bit[vertex]
  encoding/calibration
```

Pour le shader actuel, deux composantes `x` et un masque permettent de reconstruire les deux `Vector2`. Baseline MNI : 561 470 octets/colonne en float32, soit environ 0,5355 MiB ; huit colonnes représentent environ 4,28 MiB par bundle complet avant framing/compression.

`float16` ramène l'estimation à 285 054 octets/colonne, mais reste désactivé jusqu'à validation numérique et visuelle. Une représentation compacte avec perte ne devient automatique qu'après validation d'équivalence visuelle ; avant cela elle peut seulement être proposée explicitement après un refus de budget.

```text
TimelinePreloadManifest
  requestId, timelineId
  sourceStateRevision, playbackRevision
  temporalIndices[]
  expectedColumnIds[]
  uniqueContentDescriptors[]
  cpuBytesRequired, gpuBytesRequired
  safetyMarginBytes
  dominantContributors[]

TimelineAdmission
  accepted
  effectiveBudgetBytes
  estimatedAvailableBytes
  failureReason?
```

Le Desktop calcule une estimation haute avant transfert. Le coût est celui des octets uniques réellement conservés après partage/déduplication statique et représentation explicite des canaux absents. Le budget effectif ne dépasse ni la limite Quest validée ni l'estimation conservatrice de la mémoire courante. Aucun champ de protocole n'impose un nombre maximal d'indices, de colonnes, de sites ou de cerveaux ; 97 indices est un profil P11, pas une limite.

Une admission acceptée transfère et prépare les contenus une fois. Une commande d'index ultérieure ne transmet que l'intention/index et sélectionne atomiquement les tranches résidentes au plus tard à la frame XR suivante. La commande peut être appliquée optimistement ; un rejet Desktop restaure l'index canonique avec feedback utilisateur.

Une admission refusée arrive avant le transfert et ne modifie aucun contenu déjà actif. Elle fournit mémoire requise et permise, nombres d'indices/colonnes et principaux contributeurs. La V1 ne négocie pas automatiquement une plage réduite, ne pagine pas depuis disque/réseau et ne persiste pas le preload sur le Quest.

Politique :

- le bundle liste toutes les colonnes attendues ;
- incomplet : non affiché, sauf capability de partial bundle explicitement négociée et sémantiquement sûre ;
- un active + un pending latest maximum par timeline ;
- résultats avec anciennes révisions rejetés ;
- surface/sites/overlays d'un même bundle passent au renderer lors du même commit.
- l'autoplay peut sauter les indices obsolètes pour suivre le temps logique, mais annonce au client qu'il ne présente pas chaque échantillon.

## 11. `SiteAsset` et `SiteRenderFrame`

```text
SiteAsset
  hash, coordinateSystem
  siteIds[]
  positions float32[count,3]
  patientGroupIds opaques?
  staticCategory[]

SiteRenderFrame
  siteAssetHash
  sourceStateRevision
  color rgba8[count] ou paletteIndex
  size float16/float32[count]
  visibility bit[count]
  selected/hover flags
```

Les informations humaines utiles — notamment noms patient, libellés de site et noms de colonne — sont demandées à la sélection, placées sur allowlist et affichées transitoirement en mémoire sans persistance ni journalisation. Le renderer fait instancing/buffers ; le picking indexé renvoie `siteId`.

## 12. `CutRenderResult`

```text
CutRenderResult
  cutId
  interactionId, sequence
  cutRevision, renderRevision
  sourceStateRevision
  planeCanonical
  geometryHash
  geometry?:
    positions, normals, uv, indices, bounds
  baseTextureHash
  baseTexture?:
    width, height, RGBA8, colorSpace
  overlays[]:
    columnId, width, height, RGBA8, mappingRevision
  contour?
  complete=true
```

Le Desktop produit et envoie le résultat scientifique final ; seul le plan/gizmo est approché localement pendant le geste. Le résultat est rejeté si sa séquence, sa coupe, ses assets ou son scope ne sont plus courants. La géométrie/base peuvent être omises si leurs hashes sont déjà présents. Pour le stencil MNI observé, un overlay RGBA8 fait 109 068 octets ; la texture anatomique de même taille est partageable entre colonnes. Pour un plan stable, les overlays par index peuvent appartenir au preload temporel admis ; changer le plan invalide ce contenu et impose un nouveau calcul/chargement explicite.

## 13. Backpressure

Priorités décroissantes :

1. auth, close, heartbeat et erreurs ;
2. commandes et outcomes ;
3. deltas d'état ;
4. dernier résultat interactif ;
5. sélection locale de timeline déjà préchargée ;
6. assets bulk, preload et préfetch.

Un nouveau `interactionId/sequence` annule le pending précédent. Si un calcul natif n'est pas annulable, sa sortie est jetée avant sérialisation. Si un asset bulk monopolise la connexion, le spike D10 échoue.

## 14. Erreurs

```text
AUTH_FAILED
IDENTITY_CHANGED
PROTOCOL_INCOMPATIBLE
SCHEMA_INCOMPATIBLE
STATE_CONFLICT
SCOPE_NOT_FOUND
ASSET_MISSING
HASH_MISMATCH
COMMAND_INVALID
COMPUTE_FAILED
RESOURCE_PRESSURE
RATE_LIMITED
SESSION_REPLACED
```

Chaque erreur contient code, correlation ID, caractère retryable et message redacted. `RESOURCE_PRESSURE` contient aussi budget effectif, octets CPU/GPU requis, marge, cardinalités et principaux contributeurs. Aucun stack trace ou chemin patient n'est envoyé au Quest en production.

## 15. Tests normatifs

- golden vectors du header et de chaque payload ;
- round-trip sous IL2CPP ;
- endian/tailles/valeurs NaN et buffers tronqués ;
- duplicate command et out-of-order results ;
- snapshot transactionnel interrompu ;
- reconnexion par snapshot complet ; reprise par journal testée seulement si la capability est annoncée ;
- admission/refus preload sur coût byte-exact, sans plafond de cardinalité ;
- index local arbitraire visible en une frame, rollback sur refus et signalement des indices sautés en autoplay ;
- hash faux, asset dupliqué et chunk manquant ;
- gros asset simultané avec commandes ;
- fuzz du décodeur avec allocations bornées ;
- matrice protocol N/N et fenêtre N/N-1 explicitement supportée.
