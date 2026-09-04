# HiBoP XR — protocole et contrat de rendu

**Version :** 0.2  
**Statut :** contrat logique normatif ; D10/D11 acceptées provisoirement pour Windows/Quest, qualification macOS/Linux différée

## 1. Principes

- Desktop autoritaire ; Quest envoie des intentions.
- État et résultats sont explicitement versionnés.
- Les gros tableaux ne sont ni JSON ni base64.
- Les assets immuables sont adressés par SHA-256.
- Contrôle et commandes ne sont pas bloqués par un asset volumineux.
- Les résultats dynamiques sont latest-wins, jamais mis en file indéfiniment.
- Snapshot et deltas sont transactionnels.
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

À la reconnexion, `ResumeRequest` fournit epoch, state revision, assets présents et dernière révision par scope. Le serveur répond :

- `ResumeWithDeltas` si son journal borné couvre l'écart ;
- `FullSnapshotRequired` sinon ;
- `NewSession` si l'epoch a changé.

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

`float16` ramène l'estimation à 285 054 octets/colonne, mais reste désactivé jusqu'à validation numérique et visuelle. Les deltas clairsemés ne sont permis que si leur coût total et leur atomicité sont meilleurs qu'un frame complet.

Politique :

- le bundle liste toutes les colonnes attendues ;
- incomplet : non affiché, sauf capability de partial bundle explicitement négociée et sémantiquement sûre ;
- un active + un pending latest maximum par timeline ;
- résultats avec anciennes révisions rejetés ;
- surface/sites/overlays d'un même bundle passent au renderer lors du même commit.

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

Les informations humaines détaillées sont demandées à la sélection et affichées sans persistance. Le renderer fait instancing/buffers ; le picking indexé renvoie `siteId`.

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

Le résultat est rejeté si sa séquence, sa coupe, ses assets ou son scope ne sont plus courants. La géométrie/base peuvent être omises si leurs hashes sont déjà présents. Pour le stencil MNI observé, un overlay RGBA8 fait 109 068 octets ; la texture anatomique de même taille est partageable entre colonnes.

## 13. Backpressure

Priorités décroissantes :

1. auth, close, heartbeat et erreurs ;
2. commandes et outcomes ;
3. deltas d'état ;
4. dernier résultat interactif ;
5. timeline courante ;
6. assets bulk et préfetch.

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

Chaque erreur contient code, correlation ID, caractère retryable et message redacted. Aucun stack trace ou chemin patient n'est envoyé au Quest en production.

## 15. Tests normatifs

- golden vectors du header et de chaque payload ;
- round-trip sous IL2CPP ;
- endian/tailles/valeurs NaN et buffers tronqués ;
- duplicate command et out-of-order results ;
- snapshot transactionnel interrompu ;
- reprise avec/sans journal ;
- hash faux, asset dupliqué et chunk manquant ;
- gros asset simultané avec commandes ;
- fuzz du décodeur avec allocations bornées ;
- matrice protocol N/N et fenêtre N/N-1 explicitement supportée.
