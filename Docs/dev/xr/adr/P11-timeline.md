# ADR P11 — timeline et bundles dynamiques atomiques

- **Statut :** ACCEPTED — P11-A–E RESOLVED ; PRELOAD V1 IMPLÉMENTÉ ; PROBE PHYSIQUE PASS
- **Date :** 2026-09-04
- **Périmètre qualifié :** tests Unity Windows Desktop/XR et Quest 3 synthétique 1/3/8 colonnes × 97 indices
- **Production :** NO-GO tant que le preload n'est pas raccordé au renderer et au transport/UX P15 réels
- **Décisions héritées :** D06, D07, D11, D12 et ADR P03

## Vérification préalable P03

P03 est fermé et reste normatif dans le constructeur de bundle et le décodeur :

- sites : interpolation linéaire selon RenderTemporalSample.TemporalAlpha ;
- surfaces et overlays : SampleAndHold à l'index inférieur ;
- TemporalAlpha est conservé comme provenance et ne devient jamais une opacité.

Un bundle qui annonce une autre application temporelle est rejeté avant commit.

## P11-A — colonnes et contenus attendus

Le scope d'un pipeline est (SessionEpoch, timelineId). Pour une visualisation, l'adaptateur Desktop parcourt VisualizationColumnMembership dans son ordre canonique et conserve exactement les colonnes dont ColumnIncludedInTimeline vaut true.

Avant calcul, le Desktop fige pour chaque colonne un DynamicColumnExpectation : présence requise de surface, présence requise de sites et liste exacte des IDs de coupes attendues. Ce manifeste immuable voyage avec le bundle. Toute colonne, surface, série de sites ou coupe manquante, supplémentaire ou dupliquée invalide le bundle complet. Un changement de membership ou de plan produit une nouvelle révision/requête ; il ne modifie jamais un bundle en vol.

## P11-B — échec d'une colonne

Le premier échec d'extraction, validation, sérialisation, hash, décodage ou préparation rejette le bundle entier. Le dernier bundle complet reste visible. Le client publie un résultat rejeté et un diagnostic redacted ; il n'applique aucun sous-ensemble et n'invente aucune capability partielle.

## P11-C — ownership et concurrence

Le scope Timeline appartient au Desktop. Une action Desktop devient canonique dans l'ordre normal du modèle Desktop. Le Quest envoie play, pause ou scrub comme SetTimelinePlayback avec commandId, correlationId, baseScopeRevision, interactionId et sequence.

Le Quest affiche l'intention comme pending jusqu'au CommandOutcome. Une acceptation fournit les révisions et la valeur canonique ; un rejet restaure cette valeur et expose l'erreur utilisateur. Deux acteurs concurrents ne sont pas fusionnés : le Desktop accepte seulement la commande compatible avec sa révision courante ; l'autre reçoit le conflit D12 et se resynchronise. Les scrubs successifs d'une même interaction sont coalescés par sequence.

## P11-D — cadence et drop

Le Desktop conserve le temps scientifique exact et émet une requête pour chaque état canonique qu'il décide de publier. Chaque scope possède au plus un travail actif et un pending latest. Une nouvelle requête remplace le pending précédent, demande l'annulation de l'actif et reste la prochaine requête obligatoire. Un résultat dont la séquence, la révision de lecture ou la révision d'état régresse est stale et rejeté avant préparation/upload.

La fréquence source, la fréquence réseau et les 72 Hz XR restent indépendantes. La baseline ne fixe ni throttle fonctionnel, ni plafond de colonnes, ni modification du temps logique. Une adaptation future ne pourra que coalescer des états intermédiaires et devra toujours converger vers le dernier état canonique.

## P11-E — surface, sites et overlays

Surface, sites et overlays de chaque colonne sont validés contre le manifeste, encodés dans un payload unique et protégés par un SHA-256 global. Les floats sont IEEE-754 float32 little-endian ; les masques/flags restent en octets et les overlays P03 en RGBA8. Il n'existe ni quantification, ni compression.

Le XR décode puis prépare l'intégralité des buffers dans un état non visible. Un seul échange de référence publie ensuite (bundle, preparedState). Si une préparation échoue, cet échange n'a pas lieu et l'ancien état complet reste visible.

## Mesure Windows synthétique

Unity 6000.5.2f1, 20 répétitions, D2/D3 synthétiques, surface MNI 69 104 sommets par colonne, un overlay 64×64 par colonne. Le transfert mesuré est une copie loopback mémoire ; prepare-upload parcourt et prépare tous les buffers mais n'est pas une mesure GPU Quest.

| Profil | Payload | p50 end-to-end | p95 end-to-end | max |
| --- | ---: | ---: | ---: | ---: |
| D2, 1 colonne, 150 sites | 641 846 o | 132,360 ms | 179,207 ms | 194,900 ms |
| D2, 3 colonnes, 150 sites/colonne | 1 925 358 o | 396,709 ms | 433,535 ms | 528,259 ms |
| D3, 8 colonnes, 37 500 sites/colonne | 11 707 738 o | 2 406,476 ms | 2 652,031 ms | 2 655,718 ms |

La gate initiale command-to-visible p95 ≤ 100 ms échoue dès une colonne. Les coûts dominants sont la sérialisation et le décodage élément par élément. D20 est donc rouvert. Aucune compression, quantification, suppression de données, réduction de colonnes ou limite fonctionnelle n'est autorisée par cette conclusion.

## Réouverture et suite

D20 doit décider et mesurer une optimisation float32 sans changement scientifique — en priorité copies bloc/mémoire contiguë et upload GPU réel — puis exécuter Windows + réseau nominal + Quest 3, autoplay physique 10 minutes et scrub physique 60 secondes. Toute proposition de compression/quantification ou de plafond exige un ADR séparé et une acceptation explicite.

Le [spike D20 timeline sur Quest 3](../evidence/D20/timeline-quest-spike.md) a depuis validé les copies contiguës et mesuré l'upload soumis aux API Unity. Le p95 local Quest vaut `13,636 / 34,225 / 191,583 ms` pour 1/3/8 colonnes. La borne de transfert issue du meilleur débit Quest P06 vaut déjà `132,091 / 396,236 / 2 409,433 ms`, avant tout autre coût. Ces résultats ont conduit à la décision explicite suivante.

## Décision D20 — preload lossless V1

Les accès timeline pouvant être aléatoires et l'autoplay étant secondaire, le Quest reçoit et prépare la timeline dérivée entière avant de la rendre disponible. Ce coût initial peut être long ; il ne fait plus partie de chaque changement d'index. Après publication atomique du preload, un scrub choisit directement une tranche déjà en mémoire et n'écrit que l'index courant dans un `GraphicsBuffer` de 4 octets. La sélection doit être visible au plus tard à la frame suivante.

L'archive reste un état complet : surface, sites et overlays de toutes les colonnes du manifeste, floats IEEE-754 float32, RGBA8 et masques/flags exacts. Les tranches byte-identiques sont dédupliquées losslessly ; les tranches variables sont conservées bit pour bit. Le SHA-256 couvre l'archive entière. Une archive corrompue ou une préparation GPU incomplète n'est jamais publiée.

Le profil qualifié V1 couvre **1–97 indices inclus**, dont 8 colonnes × 37 500 sites × 97 indices, mais 97 n'est pas un plafond fonctionnel. L'admission du builder et du décodeur dépend d'un budget maximal explicite d'octets de payload unique fourni par l'appelant. Dès qu'une tranche unique ferait dépasser ce budget, l'opération échoue et le builder devient fautif : aucune troncature ni archive partielle ne peut être publiée. Les profils très volumineux non mesurés, notamment 8 colonnes × 37 500 sites × 3 073 indices, restent différés et devront recevoir un budget et une validation mémoire/temps explicites.

Les budgets payload CPU et GPU ne sont pas des constantes fonctionnelles cachées : l'appelant les fournit explicitement. Le décodeur compare cumulativement la taille lossless des tranches uniques avant leur allocation ; le GPU compare une estimation déterministe avant toute allocation et vérifie chaque buffer contre la limite du device. Aucun fallback ne supprime une colonne, ne quantifie, ne compresse ni ne rend un sous-ensemble.

L'implémentation comprend le builder streaming, l'archive sur `Stream` seekable sans `byte[]` géant, la reconstruction bit-exacte, les ressources GPU par colonne/canal, les overlays en `Texture2DArray`, le sélecteur latest-wins et le contrôleur de publication. Le raccord aux shaders de production, au cache statique P10 et au transport P15 reste une étape d'intégration. Voir [preuve d'implémentation et mesure physique](../evidence/D20/timeline-preload-implementation.md).

## Validation physique de la décision

Le Quest 3 a validé 1/3/8 colonnes × 97 indices, puis le profil maximal 8 colonnes × 37 500 sites pendant 60 s de scrub aléatoire et 10 min d'autoplay. Sur ce profil, les 4 322 sélections aléatoires et 43 203 sélections séquentielles ont toutes un delta de frame maximal de 1. La soumission de l'index vaut `0,0506 ms` p95 en scrub et `0,0529 ms` p95 en autoplay ; commande locale → fin de frame vaut `14,2364 / 14,2538 ms` p95.

L'archive maximale mesure `467 008 086` octets après déduplication byte-exacte, contre `1 135 536 320` octets naïfs, et les ressources GPU estimées `469 235 460` octets. Le pic RSS du processus est `1 129 353 216` octets, sans swap ni OOM. Après 10 min, le statut thermique Android reste `0`, avec GPU à environ `57,7 °C` et SoC à `59,3 °C`.

Le sous-gate D20 de sélection locale préchargée est donc PASS. Le transfert initial n'est pas instantané : au débit P06 de `38,873 Mbit/s`, sa borne basse vaut `5,9 / 17,8 / 96,1 s`, puis le Quest consomme `0,46 / 1,31 / 7,04 s` pour lecture/hash + upload. Ces durées doivent devenir une phase de chargement explicite en P15 ; elles ne sont pas requalifiées en latence de scrub.
