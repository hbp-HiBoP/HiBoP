# ADR P08 — assets distants et cache mémoire

- **Statut :** ACCEPTED — GATE P08-A–E RESOLVED
- **Date :** 2026-09-03
- **Accepté par :** propriétaire du dépôt HiBoP via l’ordre d’exécution de P08
- **Baseline inspectée :** branche `feature/xr`, commit `2f0abc4d2`
- **Décisions héritées :** D14, D17, ADR P03, P06 et P07
- **Périmètre :** `SurfaceAsset` P03 ; descriptors et cache génériques préparés pour les autres primitives P03

## Invariants

Un payload distant n’existe que dans des tableaux mémoire possédés par le provider Desktop, le staging XR ou le cache XR. Aucun composant P08 n’emploie une API de fichier, `persistentDataPath`, PlayerPrefs ou un cache HTTP persistant. Le renderer ne reçoit un `SurfaceAsset` qu’après validation de la longueur, de tous les chunks, du SHA-256 final et des dimensions annoncées.

La publication du staging vers le cache est un échange de propriété sous une même section critique. Un payload incomplet, annulé ou corrompu est remis à zéro et n’est jamais acquérable. Un même hash rejoint le transfert déjà en cours puis partage un unique stockage validé et, via P05, un unique `Mesh` Unity.

## P08-A — pression mémoire et éviction

Le budget mémoire est injecté par le shell depuis son enveloppe mesurée ; il ne représente ni un nombre de cerveaux, ni un nombre de sites. `ResidentBytes` compte simultanément staging et payloads validés. La taille complète est réservée avant le premier chunk, après validation du descriptor.

Sous pression, le cache évince en LRU uniquement les assets validés dont le refcount vaut zéro. Un asset actif n’est jamais évincé. Un staging concurrent n’est pas annulé implicitement. Si les inactifs ne suffisent pas, le nouveau transfert ne démarre pas et retourne `MemoryPressureRequiresUserAction`, sans mutation de l’affichage existant.

## P08-B — comportement utilisateur

Deux états fermés doivent être rendus par l’UI P13 :

- `AssetExceedsBudget` : « Cet asset ne tient pas dans la mémoire disponible sur ce casque. Il n’a pas été chargé. » ; l’utilisateur peut annuler ou changer d’appareil/configuration, mais aucun contenu courant ne disparaît ;
- `MemoryPressureRequiresUserAction` : « Mémoire insuffisante pour charger ce contenu. Fermez explicitement un contenu ou annulez un autre chargement, puis réessayez. »

P08 ne choisit jamais à la place de l’utilisateur quel contenu actif fermer. Après éviction d’un inactif, une acquisition ultérieure redemande simplement ses ranges et affiche l’état de rechargement prévu par P13.

## P08-C — lifecycle du cache

La matrice du cache est exacte ; P14-B reste propriétaire du raccordement des événements Android/application à ces entrées et de la politique sécurité globale.

| Événement reçu par P08 | Staging | Inactifs validés | Actifs |
| --- | --- | --- | --- |
| `ConnectionInterrupted` pendant le lease P07 de 30 s | conservé pour reprise par ranges | conservés | conservés |
| `ResumeLeaseExpired` | annulé et remis à zéro | purgés | marqués purge-pending, nouveaux leases refusés |
| `NewEpoch` / session remplacée | annulé et remis à zéro | purgés | purge-pending jusqu’au retrait explicite par leur owner |
| `Backgrounded` | annulé et remis à zéro | purgés | purge-pending jusqu’au retrait explicite par leur owner |
| `Closed` | annulé et remis à zéro, cache fermé | purgés | purge-pending ; libérés au dernier `Dispose` |

Le résultat de lifecycle expose le nombre d’assets actifs qui exigent un retrait explicite. Le cache invalide leurs nouvelles acquisitions mais garde le payload et le rendu existants jusqu’à ce que le binding renderer appelle `ReleaseActiveContent`. Un process tué/rebooté ne peut rien restaurer puisqu’il n’existe aucune persistance P08.

## P08-D — limites de sécurité et négociation

Les maxima absolus sont des bornes d’allocation par payload, pas des plafonds de cardinalité métier :

| Type | Taille encodée maximale | Dimensions contrôlées avant allocation |
| --- | ---: | --- |
| surface | 256 Mio | 2 000 000 vertices, 12 000 000 indices triangulaires, UV 0 ou égales aux vertices, longueur exacte |
| sites | 64 Mio | 1 000 000 entrées par asset |
| texture | 256 Mio | 16 384 × 16 384 et produit RGBA borné |
| géométrie de coupe | 128 Mio | 2 000 000 vertices, 12 000 000 indices triangulaires |
| chunk | 1 Mio | bornes et taille exacte de chaque range |

Chaque pair annonce `RemoteAssetCapabilities`. Les maxima effectifs sont le minimum champ par champ entre host et client. Le cache refuse un descriptor dépassant cette intersection avant staging. Le budget mémoire runtime, distinct, peut être inférieur et produit alors un état utilisateur explicite. Les descriptors sont limités à 16 dépendances, sans hash nul, doublon ou auto-référence.

## P08-E — variantes anatomical/inflated

Chaque représentation reste un payload P03 complet et possède son propre SHA-256 ; aucune identité de topologie ni morph implicite n’est supposée. L’inflated déclare exactement une dépendance `VariantBase` vers le hash anatomical. `SurfaceVariantSetDescriptor` vérifie rôles, hashes distincts et version de schéma commune, puis calcule un hash de manifeste canonique sur `anatomicalHash || inflatedHash || schemaVersion`.

Le chargement et la déduplication restent par hash de contenu. Le manifeste lie la paire logique sans faire du hash anatomical une identité de l’inflated et sans autoriser l’exposition d’une variante partielle comme paire complète.

## Frontières d’assemblage

- `CRNL.HiBoP.Protocol` porte descriptors, négociation, provider mémoire, staging/ranges, cache/refcounts et lifecycle ; il reste BCL + Contracts, sans Unity ni API réseau/fichier.
- `HBP.RenderModelAdapters.Runtime` encode et publie les surfaces côté Desktop.
- `CRNL.HiBoP.RenderModel` porte le codec binaire déterministe du payload surface.
- `CRNL.HiBoP.XR.RemoteAssets` acquiert et revalide le payload, partage un unique `SurfaceAsset` décodé par hash entre bindings puis le remet à P05 ; P05 reste sans dépendance au protocole.

## Réouverture

Réouvrir P08 pour compression, cache disque, eviction d’un actif, pooling, changement de hash, chunks non alignés, manifestes multi-variantes ou limites supérieures. P14-B doit encore valider le raccordement plateforme de `Backgrounded`, close/crash/reboot et les propriétaires de purge ; il ne peut pas affaiblir l’absence de persistance ni rendre une éviction active silencieuse.
