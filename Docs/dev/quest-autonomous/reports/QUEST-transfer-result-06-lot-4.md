# Résultat du lot 4 — Small, deux envois Wi-Fi

16 septembre 2026. Premier envoi confirmé sans anomalie par l'utilisateur ;
second envoi terminé, sans anomalie supplémentaire signalée. Les deux traces
confirment `Published`, intégrité concordante et rendu caméra stéréo.

**20,027 s puis 20,241 s du clic à la confirmation Desktop**, contre
41,374 s et 41,982 s au lot 3 : **−51,6 % et −51,8 %**. Le pipeline HBT4 et
ses codecs sont validés sur le casque. La cible globale `B/R + 3–5 s` reste
non atteinte : environ 8,4–8,8 s de travail subsistent après réception.

Références : [implémentation et tests](QUEST-transfer-lot-4.md),
[lot 3](QUEST-transfer-result-05-lot-3.md),
[plan](QUEST-transfer-optimization-plan.md).

## Résultat de bout en bout

| Mesure | Lot 3, premier | Lot 4, premier | Lot 3, second | Lot 4, second |
|---|---:|---:|---:|---:|
| Clic → confirmation Desktop | 41,374 s | **20,027 s** | 41,982 s | **20,241 s** |
| Réception Quest, incluant décodage et vérification HBT4 | 8,390 s | **7,993 s** | 10,183 s | **9,006 s** |
| Débit utile de cette réception | 14,15 Mio/s | 14,95 Mio/s | 11,66 Mio/s | 13,27 Mio/s |
| Fin de réception → début du reçu Quest | 16,671 s | **8,793 s** | 15,600 s | **8,386 s** |
| Plus grand intervalle de frames Desktop | 1,483 s | 1,590 s | 1,014 s | 1,272 s |
| Plus grand intervalle de frames Quest | 0,557 s | 0,501 s | 0,561 s | 0,511 s |

Première fin de rendu caméra **CPU**, bornée sans synchronisation des horloges :
**[18,580 ; 20,060] s**, puis **[18,913 ; 20,262] s**. Les bornes sont plus
larges qu'au lot 3 : `send.payload.begin` précède maintenant l'attente des
premiers blocs produits. Ne pas prendre la borne basse pour une latence exacte,
ni ce jalon pour une mesure GPU ou photons.

Chronologie sur l'horloge Desktop, en secondes après clic :

| Jalon | Premier | Second |
|---|---:|---:|
| Début du worker d'encodage | 1,592 | 1,285 |
| Fin d'encodage des métadonnées/pack | 3,081 | 2,809 |
| Fin d'écriture du payload TLS | 11,198 | 11,842 |
| Reçu de publication traité par Desktop | 20,027 | 20,241 |

L'intervalle d'émission instrumenté vaut 9,437/10,342 s ; il inclut l'attente
initiale du producteur. Celui de réception inclut décompression, écritures et
vérifications chevauchées. **Aucun de ces scopes n'est une mesure isolée de la
radio Wi-Fi**. La durée du scope `desktop.capture.total` a changé de périmètre :
elle ne comprend plus l'encodage ni la compression sur worker. La comparer seule
aux 16 s de capture du lot 3 donnerait un gain artificiellement élevé.

## Ce que le pipeline a supprimé du chemin séquentiel

Scopes inclusifs, imbriqués ou chevauchés : ne pas additionner toutes les lignes.

| Phase | Premier | Second |
|---|---:|---:|
| Snapshot Unity | 1,574 s | 1,266 s |
| Dont détachement du graphe | 1,402 s | 1,255 s |
| Encodage métadonnées/pack sur worker | 1,489 s | 1,525 s |
| Compression Zstd, cumul des blocs | **0,980 s** | **1,026 s** |
| Hash des sources Desktop | 0,110 s | 0,113 s |
| Hash des blocs encodés Desktop | 0,075 s | 0,069 s |
| Écriture du spool Desktop | 0,134 s | 0,147 s |
| Attente du producteur, file pleine | 6,622 s | 7,430 s |
| Écriture TLS Desktop | 8,058 s | 8,957 s |
| Lecture TLS Quest | 6,609 s | 7,706 s |
| Attente du lecteur Quest, file pleine | 0,546 s | 0,489 s |
| Vérification des blocs encodés Quest | 0,316 s | 0,198 s |
| Décompression Quest | **1,003 s** | **0,724 s** |
| Écriture et vérification des ressources Quest | 1,123 s | 0,843 s |
| Métadonnées et validation après réception | **4,621 s** | **4,574 s** |
| Dont désérialisation JSON/buffers | 4,488 s | 4,471 s |
| Dont lecture des tableaux numériques | 0,836 s | 0,856 s |
| Restauration de scène | **4,159 s** | **3,637 s** |
| Application de la vue, remplacement inclus | 4,163 s | 3,779 s |
| Dont chargement natif des trois IRM | 2,350 s | 2,440 s |
| Décodage des surfaces | 0,859 s | 0,474 s |
| Upload natif des surfaces | 0,383 s | 0,366 s |

Au lot 3, le ZIP coûtait 11,781/12,025 s, suivi de 0,903/0,925 s pour le hash
final Desktop. Le nouveau cumul de compression est proche d'une seconde et
s'effectue pendant l'émission. Côté Quest, l'ancien passage d'extraction et de
validation de 8,089/7,496 s après réception est remplacé par le traitement en
flux ci-dessus. Les contrôles d'intégrité restent présents.

Les files atteignent leur capacité de **deux blocs** des deux côtés. Le producteur
passe beaucoup plus de temps à attendre l'aval qu'à compresser ; la réception
attend principalement des octets TLS. Cela est cohérent avec un pipeline dont
le transport domine en régime établi, avec des pauses du récepteur. Cela ne
démontre pas une saturation de la capacité physique du Wi-Fi. Le parcours complet
reste presque autant pénalisé par le travail après réception que par la réception.

## Préparation initiale et fluidité

| Préparation pendant Pair | Lot 3 | Lot 4 |
|---|---:|---:|
| Desktop, trace complète | 6,415 s | **9,314 s** |
| Dont anatomie sur worker | chargement alors bloquant | 9,274 s |
| Plus grand intervalle Desktop | observateur alors bloqué | **17,78 ms** |
| Quest, trace complète | 5,120 s | **5,371 s** |
| Dont vérification/installation standard | 3,822 s | 4,110 s |
| Dont chargement standard | 1,293 s | 1,259 s |
| Plus grand intervalle Quest | 27,66 ms | **28,59 ms** |

Le correctif post-lot-3 est confirmé : la boucle Unity Desktop avance pendant le
préchargement, sans le gel multi-seconde antérieur. En revanche, sa durée augmente
de 2,90 s dans cette observation. Le déplacement sur worker a rendu l'interface
réactive ; il n'a pas supprimé le coût. Ces deux mesures ne permettent pas
d'isoler l'effet du worker, des caches système ou de la contention.

Somme comptable des préparations et du premier envoi :
**9,314 + 5,371 + 20,027 = 34,712 s**, contre 52,909 s au lot 3, soit environ
**34,4 % de moins**. Ce n'est pas un chronométrage direct Pair → rendu : elle
exclut le reste de l'appairage et l'attente humaine, et additionne des scopes
distincts. Le gain après clic ne masque donc pas la préparation initiale.

Pendant l'envoi, le snapshot Desktop bloque encore 1,27–1,59 s et le Quest
présente une pause proche de 0,51 s. La fluidité parfaite et la cible de snapshot
≤ 1 s ne sont pas atteintes, malgré l'absence d'anomalie visible signalée.

## Conditions, contenu et mémoire

- Deux nouvelles captures, `retry=false`, route `lan`, mêmes processus Desktop
  30108 et Quest 24242, players Release IL2CPP, marqueur `lots-0-1-2-3-4`.
- Trois patients, trois colonnes, activité automatique désactivée ; 1 669 106
  sommets, 3 337 992 triangles. Focus vrai pendant les deux opérations.
- 397 025 397 octets décompressés, 1 520 blocs ; payloads de 125 317 652 et
  125 317 677 octets, soit environ **0,66 % de plus** que le ZIP du lot 3.
  Métadonnées 15 289 823 octets, pack 139 275 466 octets, 48 549 buffers uniques.
- Première capture : 42 misses de surfaces des deux côtés ; seconde : **42 hits**
  des deux côtés. Pas de delta réseau : le contenu est intégralement renvoyé.
  Le remplacement de la scène précédente fait partie du deuxième essai.
- Batterie 100 %, USB branché ; température batterie pendant les opérations
  40–41 °C puis 38–40 °C, état thermique Android 0. Ce n'est pas une température
  SoC ni la preuve d'une fréquence CPU constante.
- Aucun événement de trace perdu. Compteur d'allocations par thread indisponible
  sous IL2CPP. Les traces incluent dix secondes de suivi après confirmation.
- Pics RSS échantillonnés, suivi compris : Desktop **2 762 / 3 217 Mio**,
  Quest **1 476 / 2 169 Mio**. Les files de transport sont bornées, mais le graphe
  détaché, les caches et le remplacement conservent d'autres allocations.
  Deux essais ne permettent pas de conclure sur une fuite ou un plateau mémoire.
- Aucun échec de livraison constaté. Le logcat disponible contient des messages
  OpenXR Meta (`xrDiscoverSpacesMETA`, visibilité de frontière) ; ils ne sont pas
  des erreurs du pipeline. Le tampon logcat ne couvre pas toute la première
  opération ; les traces persistantes restent la preuve principale.

Deux observations confirment le gain important ; elles n'établissent ni variance
ni garantie de délai. Les microbenchmarks de codecs restent distincts de ces
mesures IL2CPP réelles, incluant P/Invoke, disque, réseau et rendu concurrent.

## Décision et pistes reportées

**Lot 4 validé pour son périmètre codecs, intégrité et pipeline.** Le palier de
30 s est dépassé ; le résultat se situe autour de 20 s. Les objectifs plus
ambitieux de délai réseau + 3–5 s et de fluidité restent ouverts.

**Décision utilisateur après ce bilan : performances actuelles acceptées.**
Les pistes ci-dessous sont reportées et ne conditionnent plus le lot 5 de
nettoyage et validation finale. Le document
[optimisations futures et clôture](QUEST-transfer-future-optimizations.md)
précise leur ordre de reprise et le périmètre du lot 5. Aucune de ces pistes
n'est implémentée ici :

1. **Métadonnées : 4,5 s.** Réduire la reconstruction du graphe JSON généraliste
   par un schéma explicite et des lectures groupées du pack ; préserver identités,
   indépendance des colonnes et validations. Les lectures numériques seules
   représentent moins d'une seconde : les optimiser seules ne suffit pas.
2. **Préparer plus tôt les ressources privées du Quest.** Exploiter la réception
   vérifiée ressource par ressource pour chevaucher préparation et transport,
   puis publier uniquement après validation du transcript final. Prévoir abandon,
   annulation et propriété des objets natifs ; ne jamais publier une scène partielle.
3. **IRM : 2,35–2,44 s.** Évaluer un cache de ressources natives immuables avec
   identité, invalidation, budget mémoire et ownership explicites. Les hits de
   surfaces actuels n'évitent pas ces chargements ni tout upload.
4. **Snapshot et publication.** Réduire le parcours synchrone du graphe et étaler
   les uploads/publication Unity compatibles avec la cohérence du snapshot.
   Traiter les pauses mesurées indépendamment de la durée totale.
5. **Préparation et mémoire.** Garder le coût de Pair visible et contrôler le
   pic du remplacement dans la prochaine validation. Optimiser le réseau ensuite
   à partir de mesures TLS/radio plus fines, sans attribuer à la radio tout scope TLS.

## Preuves reproductibles

Répertoire local : `.test-results/quest-034/benchmark/`.

| Envoi | Trace Desktop | Trace Quest |
|---|---|---|
| Premier | `desktop-2f99430470384da2a82780b945cb2063.json` | `quest-a5292596b1ce46b5b2396ec6ca8a3718.json` |
| Second | `desktop-0823fc405dc341fbbaec732cfdb790c1.json` | `quest-83017673c7194c528a03f0a5910049a9.json` |

Empreintes de transcript concordantes Desktop/Quest :

- Premier : `529c74ee488204222ec017c757620fac12ee881c48bb42052ea3e031ef2b77a1`.
- Second : `592f7ebc158c055067d9ff3b51568eb3b7de883d7ac2190c3191e659519597c8`.

Préparations : `desktop-preparation-5894bc81321c461bb2489934c8361db2.json` et
`quest-preparation-fe6076bfdfb049d8afaf5d99fc13229e.json`.
`analysis.json` contient les faits, jalons, conditions et SHA-256 des six fichiers.
`first-summary.txt` et `second-summary.txt` sont produits par
`Tools/Read-QuestTransferTrace.py` avec la paire correspondante ; les journaux
players sont archivés à côté. Les versions, hashes des players et résultats des
tests automatisés figurent dans le rapport d'implémentation.
