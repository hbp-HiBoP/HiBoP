# ADR P07 — session distribuée synthétique

- **Statut :** ACCEPTED — GATE P07-A–E RESOLVED
- **Date :** 2026-09-03
- **Accepté par :** propriétaire du dépôt HiBoP via l'ordre d'exécution de P07
- **Baseline inspectée :** branche `feature/xr`, commit `02dd8226c7dafc37b8eef0722e107d7e8c9051bf`
- **Décisions héritées :** P02-A–D, D12, D17, D18 et ADR P06 `ACCEPTED — PROVISIONAL WINDOWS/QUEST VALIDATED`
- **Périmètre :** état synthétique en mémoire, un Desktop autoritaire et un client XR ; aucune donnée patient, aucun asset scientifique et aucun multi-client

> **Amendement produit du 4 septembre 2026 :** D12/D22 supersèdent l'obligation de reprise par journal pour la V1. Le code et les preuves de `ResumeWithDeltas` restent valides comme optimisation négociée, mais un client/host V1 doit seulement garantir la reconnexion transactionnelle par snapshot complet. Les sections ci-dessous décrivent la décision et l'implémentation historiques P07 ; elles ne rendent pas la capability delta obligatoire pour P15.

## Gate P06 et contraintes conservées

P06 autorise P07 derrière des interfaces remplaçables avec la baseline T3 : Kestrel self-contained côté Desktop, websocket-sharp WSS et `UnityWebRequest` HTTPS côté Quest, TLS 1.2 minimum, même pin SPKI sur les deux chemins, et Protobuf `3.36.1` pour le contrôle. La qualification native macOS/Linux et le coût de distribution du sidecar restent ouverts ; P07 ne déclare donc qu'une validation Windows/Quest.

P07 ne modifie pas cette pile, n'introduit aucun fallback TLS et ne persiste ni état de session, ni token, ni diagnostic. Le protocole de session est isolé du transport afin que D10 puisse être rouvert sans changer ses invariants.

## P07-A — machines d'état host et client

### Décision host

Le host possède exactement un `SessionEpoch` et sérialise toutes ses transitions et mutations sous une même porte d'exclusion. Ses états sont :

```text
Stopped
  └─ Start(epoch neuf) → Pairing
Pairing
  ├─ preuve TLS + SAS + token → AwaitingHello
  └─ Stop → Closed
AwaitingHello
  ├─ hello compatible → Synchronizing
  ├─ hello incompatible/timeout → Pairing
  └─ remplacement explicite → Replaced
Synchronizing
  ├─ snapshot accusé → Active
  ├─ coupure → Suspended
  └─ validation/timeout échoué → Pairing
Active
  ├─ coupure → Suspended
  ├─ Stop → Closed
  └─ remplacement explicite → Replaced
Suspended
  ├─ resume compatible avant expiration du lease → Synchronizing
  ├─ lease expiré → Pairing
  └─ remplacement explicite/Stop → Replaced/Closed
Replaced/Closed (terminaux)
```

Le handshake précède tout état applicatif. `ClientHello` et `ServerHello` annoncent séparément protocole, schémas, capabilities, versions d'application, commits et version native. Un major différent produit `PROTOCOL_INCOMPATIBLE`; l'absence de hash de schéma commun produit `SCHEMA_INCOMPATIBLE`; un minor différent négocie seulement l'intersection des capabilities. Les capabilities produit V1 requises sont snapshot transactionnel et séquence de commande idempotente ; delta ordonné et reprise par journal sont optionnels depuis l'amendement D12/D22. Aucune version d'application ne remplace ce contrôle.

### Décision client

```text
Idle
  └─ saisie endpoint + SAS → Pairing
Pairing
  ├─ pin/token acquis → Connecting
  └─ échec définitif → Refused
Connecting
  ├─ WSS authentifié → Handshaking
  └─ échec retryable → ReconnectWait
Handshaking
  ├─ compatible → Synchronizing
  ├─ incompatible → Refused
  └─ coupure → ReconnectWait
Synchronizing
  ├─ commit snapshot ou lot de reprise → Active
  └─ coupure/validation échouée → ReconnectWait
Active
  ├─ coupure/heartbeat expiré → ReconnectWait
  ├─ nouvel epoch → Synchronizing après purge
  └─ fermeture → Closed
ReconnectWait
  ├─ délai écoulé → Connecting
  ├─ abandon utilisateur → Closed
  └─ budget 30 s épuisé → Refused
Refused/Closed (terminaux, reprise par action utilisateur)
```

Le host capture l'état immuable à une révision `R` et le curseur du journal sous la même sérialisation que les mutations, puis construit et encode le snapshot hors de l'état actif. Le snapshot synthétique doit tenir dans une seule enveloppe P06 de 64 Kio ; dépasser cette limite est un échec explicite et la stratégie de segmentation d'un futur snapshot réel devra rouvrir l'ADR. Les deltas `R → courant` sont rattrapés avant le passage `Active`, sinon le snapshot recommence.

Côté client, un snapshot ou un lot de deltas de reprise est construit dans un état candidat privé. La référence d'état visible est échangée une seule fois après validation de l'epoch, des révisions, des comptes et de l'intégralité du lot. Une interruption détruit le candidat et laisse l'état visible inchangé.

## P07-B — journal de deltas et fenêtre d'idempotence

### Journal de deltas

Le journal est FIFO, contigu par révision globale, et borné par les trois limites simultanées suivantes :

- 512 deltas ;
- 4 Mio de coût logique sérialisé ;
- 5 minutes depuis le commit host.

L'entrée la plus ancienne est évincée dès qu'une limite est dépassée. `ResumeWithDeltas` n'est retourné que si toutes les révisions de `clientRevision + 1` à la révision courante sont présentes et contiguës. Une révision future, un trou, une base de scope incohérente ou une limite dépassée produit `FullSnapshotRequired`. Un epoch différent produit `NewSession` et purge l'état client avant le snapshot.

### Idempotence exacte et bornée

Chaque commande transporte, en plus du `commandId` P02, un `clientCommandSequence` strictement monotone, commencé à 1 et conservé lors de tout retry. Le host conserve les 4 096 derniers outcomes pendant au plus 15 minutes, avec leur paire `(sequence, commandId)`, et un high-water mark par epoch.

- `sequence == highWater + 1` : la commande peut franchir le gate P02-D ; outcome, mutation et delta sont publiés dans la même section critique ;
- `sequence <= highWater` et entrée présente : le même `commandId` retourne le même outcome logique sans mutation ; une paire différente est `COMMAND_INVALID` non retryable ;
- `sequence <= highWater` mais outcome évincé : `COMMAND_INVALID` non retryable, sans exécution ;
- `sequence > highWater + 1` : `COMMAND_INVALID` retryable, sans exécution, afin de récupérer la commande manquante.

Le client ne change jamais l'ID ni la séquence d'une commande en retry. Après conflit et resynchronisation, une intention retentée par l'utilisateur devient une nouvelle commande avec la séquence suivante. Cette règle empêche un rejeu ancien d'avoir un double effet malgré l'éviction des outcomes.

## P07-C — heartbeat, timeouts et retry/backoff

Les temps utilisent une horloge monotone injectée :

- heartbeat applicatif toutes les 1 s en `Active` ; trois réponses successives manquantes ou 3 s sans trafic reçu suspendent la connexion ;
- connexion TLS/WSS : 3 s ; handshake : 3 s ; snapshot : 10 s ; commande : 5 s ;
- lease de reprise host après coupure : 30 s ; il conserve l'epoch, l'état, le journal et l'idempotence en mémoire pendant ce lease ;
- retry automatique avec full jitter sur plafonds 250 ms, 500 ms, 1 s, 2 s puis 4 s ; plafond 4 s, un seul essai en vol ;
- après 30 s sans reprise, arrêt des retries automatiques et diagnostic actionnable. Une action utilisateur peut recommencer l'appairage.

Auth refusée, identité changée, protocole/schéma incompatible et remplacement de session ne sont jamais retentés automatiquement. Une coupure, un timeout ou un conflit d'état le sont selon la politique ci-dessus. P07 ne décide pas le stockage sécurisé ni le comportement Android background/kill/reboot réservé à P14-B : le prototype garde tout en mémoire et purge lors de la fermeture applicative.

## P07-D — second client et remplacement

V1 est strictement mono-client et applique **premier client actif conservé** :

- dès qu'un client possède le lease (`AwaitingHello`, `Synchronizing`, `Active` ou `Suspended`), toute autre identité/token reçoit `SESSION_BUSY`, non retryable, avant tout snapshot ;
- une coupure ne libère pas le lease avant 30 s, afin qu'un second client ne vole pas une reprise ;
- aucun takeover automatique, aucune file d'attente et aucun merge ne sont permis ;
- le remplacement exige l'action explicite « Remplacer le casque » côté Desktop ; elle ferme l'ancien client avec `SESSION_REPLACED`, invalide tous les tokens, purge journaux/outcomes, crée un nouvel epoch, puis rouvre l'appairage ;
- message du second client : « Une autre session XR utilise ce Desktop. Fermez-la ou choisissez “Remplacer le casque” sur le Desktop. »

## P07-E — conflits de commande et message utilisateur

Le host applique exactement P02-D sous la même section critique que la mutation. Un `baseScopeRevision` obsolète produit `STATE_CONFLICT`, `retryable = true`, les révisions globale et de scope courantes, aucun changement, aucun delta et aucun incrément. Ce rejet est lui-même inscrit dans le journal d'idempotence avant de répondre.

À réception, le client :

1. marque le scope concerné en réconciliation et bloque les nouvelles commandes de ce scope ;
2. affiche : « L'état a changé sur le Desktop. HiBoP XR se resynchronise ; réessayez votre action. » ;
3. demande une reprise depuis sa révision visible, avec fallback snapshot déterministe ;
4. débloque le scope après commit atomique ;
5. ne rejoue jamais automatiquement la commande conflictuelle. Une nouvelle action crée un nouveau `commandId` et une nouvelle séquence.

Il n'existe ni merge implicite, ni last-write-wins, ni rebase de payload. `interactionId/sequence` ne sert qu'à coalescer un calcul et ne contourne pas le gate de révision.

## Diagnostics et confidentialité

Le host et le client exposent un journal circulaire de 256 événements et des compteurs : état de machine, epoch opaque, révisions, dernier code d'erreur, heartbeats, retries, décision de reprise, profondeur/évictions des deux journaux et latence de reprise. Les événements utilisent des codes fermés et des correlation IDs opaques. Endpoint, SAS, token, pin complet, payload, valeur de propriété, nom, chemin et stack trace n'y apparaissent jamais.

## Gate d'implémentation

P07-A–E étant résolues ci-dessus et P06 étant accepté pour Windows/Quest, l'implémentation production de la session synthétique est autorisée. Elle doit rester transport-neutral dans le package partagé, connecter seulement des adaptateurs P06, et ne pas introduire assets réels, renderer, données patient, calcul scientifique ou multi-client.

## Réouverture

Réouvrir cet ADR pour tout multi-client, takeover automatique, persistance de session, changement de fenêtre d'idempotence, commande hors ordre, merge de conflit, comportement Android background/kill, ou modification de la pile P06.
