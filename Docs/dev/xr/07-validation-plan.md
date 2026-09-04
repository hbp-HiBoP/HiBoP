# HiBoP XR — plan de validation

**Version :** 0.4
**But :** démontrer fidélité, cohérence distribuée, performance, sécurité et utilisabilité sur Quest 3.

## 1. Niveaux de preuve

1. tests unitaires contrats, scopes et codecs ;
2. tests d'intégration host/client simulés ;
3. golden buffers et images Desktop/renderer indépendant ;
4. APK Quest sur réseau local réel ;
5. endurance mémoire/thermique ;
6. pilote utilisateur contrôlé.

Une mesure publie commit, build, appareil/OS, Unity/SDK, réseau, dataset, répétitions, warm-up, p50/p95/max, mémoire et logs redacted.

## 2. Datasets

| ID | Contenu | Usage |
| --- | --- | --- |
| D0 | géométrie synthétique minuscule, IDs connus | protocole, endian, picking déterministe |
| D1 | MNI gris réel, 69 104 sommets/138 216 faces | surfaces, payload, multi-instances |
| D2 | visualisation mono-patient réaliste avec sites/coupes | fidélité et UX courante |
| D3 | 250 patients × 150 sites = 37 500, 8 colonnes dynamiques | stress sans plafond |
| D4 | volumes/grilles et overlays les plus lourds disponibles | calcul, coupes, mémoire |
| D5 | signal synthétique avec `TemporalSample.Alpha` connu | interpolation surface/sites |
| D6 | sentinelles non sensibles dans noms/paths/payloads | redaction et purge |

Les datasets patient réels restent locaux et ne sont jamais joints aux rapports.

## 3. Matrice plateformes

| Composant | Windows | macOS | Linux | Quest 3 |
| --- | --- | --- | --- | --- |
| packages/contrats | tests | tests | tests | IL2CPP smoke |
| Desktop host | build/runtime | build/runtime | build/runtime | — |
| réseau | serveur | serveur | serveur | client |
| renderer | baseline | baseline | baseline | cible |
| Input Desktop | parité | parité | parité | — |
| XR/OpenXR | — | — | — | mains + contrôleurs |

Ordre de qualification : Windows x64 d'abord ; après réussite du prototype end-to-end, macOS Apple Silicon sur MacBook Air M2 au minimum ; puis Ubuntu 24.04 x64. Quest 3 est la seule cible casque promise en V1.

## 4. Fidélité scientifique et visuelle

Pour chaque représentation V1 :

1. fixer projet, scope, paramètres, `TemporalSample` et asset revisions ;
2. capturer buffers après calcul Desktop ;
3. sérialiser/désérialiser par le contrat ;
4. rendre dans la scène indépendante puis Quest ;
5. comparer nombres, masques, textures, contours et image.

Critères :

- IDs, counts, dimensions, unités et repères exacts ;
- float32 bit-identique lorsque l'ordre/opération le permet, sinon tolérance documentée par fonction ;
- aucun pixel/vertex provenant d'une autre révision ;
- représentation compacte avec perte seulement après erreur max/RMSE et équivalence visuelle acceptées sur le corpus prévu ; automatique après validation, sinon proposée explicitement avec sa dégradation ;
- coupe finale identique au résultat Desktop pour le même plan ;
- D5 définit explicitement l'interpolation attendue et empêche une régression silencieuse.

## 5. Scénarios end-to-end

### E01 — premier appairage

Découverte disponible puis bloquée, IP manuelle, code correct/incorrect, identité changée, certificat expiré. Attendu : confiance explicite, erreurs distinctes, aucun état avant auth.

### E02 — snapshot initial

Projet avec plusieurs visualisations/colonnes/coupes/ROI. Couper la connexion à chaque phase. Attendu : ancien état cohérent ou nouveau snapshot complet, jamais partiel.

### E03 — anatomie et instances

Créer plusieurs instances MNI/patient, anatomical/inflated, transformations indépendantes, close/reopen. Inspecter un cerveau fortement agrandi en se penchant et en tournant autour, assis puis debout. Attendu : hashes partagés, pose locale préservée seulement pour IDs valides, perspective et manipulation locales à 72 Hz.

### E04 — sites

D0 pour exactitude puis D3 pour charge ; ray, proche, hover, sélection, blacklist, échelles d'inspection petites à très agrandies, mains/contrôleurs. Attendu : 100 % IDs corrects sur targets déterministes, aucun site supprimé.

### E05 — coupe

Geste continu, changement de direction rapide, release, perte réseau, réponses artificiellement réordonnées, timeline sur plan stable puis changement de plan. Attendu : plan local fluide, jamais de rollback du gizmo, dernière coupe Desktop exacte, overlays préchargés instantanés pour le plan stable et invalidation/rechargement explicites après modification.

### E06 — timeline

1/3/8 colonnes et plusieurs nombres d'indices sans plafond codé ; admission juste sous et juste au-dessus des budgets CPU/GPU ; autoplay, scrub arbitraire, pause, vitesse, paramètres simultanés, D5. Attendu : coût réel après partage/déduplication, refus avant transfert avec détail requis/permis et contributeurs, preload progressif annulable, bundle atomique visible au plus tard à la frame suivante, rollback sur refus Desktop, tête XR stable et signal visible lorsque l'autoplay saute des indices.

### E07 — reconnexion

Coupures 1/5/30 s, host redémarré et projet changé. Attendu : passthrough/tracking/manipulations locales continus, état scientifique gelé et signalé, reconnexion par snapshot complet, nouvel epoch purgeant les résultats anciens. Si la capability delta est annoncée, elle est testée séparément sans devenir un prérequis V1.

### E08 — incompatibilité

Major, minor, schema hash et capability variants. Attendu : refus lisible pour incompatibles, négociation seulement pour combinaisons testées.

### E09 — vie privée

D6, background, kill, reboot, export logs. Attendu : aucune sentinelle patient dans stockage/log ; endpoint/empreinte seulement dans stockage autorisé.

### E10 — charge et endurance

D3/D4, passthrough puis VR, 30 minutes, cycles ouverture/fermeture et créations dépassant le budget. Attendu : pas de fuite, crash, dérive thermique non maîtrisée ou éviction silencieuse ; seule la nouvelle ressource est refusée et les cerveaux déjà actifs restent utilisables.

### E11 — tranche verticale produit Desktop/Quest

Depuis une vraie visualisation ouverte dans HiBoP, activer la session par le parcours P15 décidé, appairer un Quest physique puis synchroniser le snapshot sans injection synthétique. Transférer une surface et ses sites par HTTPS, maintenir contrôle/état par WSS, créer l'instance selon P15-B et exécuter les fonctions dynamiques retenues par P15-G. Effectuer au moins une interaction Quest dont le Desktop valide l'intention avant de republier l'état ou le résultat canonique. Tester ensuite déconnexion/reprise, fermeture de la visualisation et arrêt complet.

Attendu : le même chemin de production supervise le sidecar, transporte les données réelles autorisées, conserve les révisions, affiche les états P15-F et ne laisse ni donnée persistée ni processus orphelin. Il permet aussi de sélectionner un site et d'ouvrir dans le casque ses informations prioritaires. Une scène de démonstration, un host en mémoire ou un adaptateur synthétique ne peut pas remplacer ce scénario.

À l'issue d'E11, une revue utilisateur documentée confirme ou rouvre le rendu local Quest. Elle vérifie en priorité l'inspection rapprochée, la fluidité, la précision et la fidélité avant toute étude d'un second système de rendu.

## 6. Performance

### Rendu XR

- cible baseline 72 Hz : CPU frame p95 et GPU frame p95 chacun < 13,89 ms ;
- mesurer dropped frames, App GPU time, main/render thread, draw calls et mémoire ;
- 90 Hz est un objectif secondaire après réussite 72 Hz.

### Sites

- 37 500 visibles ;
- picking p95 < 50 ms ;
- 100 % exactitude sur cas déterministes ;
- aucun GameObject/collider par site ;
- mise à jour complète et dirty range mesurées.

### Timeline

- p50/p95/max d'estimation, calcul Desktop, transfert, préparation CPU/GPU et sélection locale ;
- après preload, chaque index admis devient visible dans un délai maximal d'une frame XR ;
- latence de confirmation Desktop et rollback mesurée séparément du feedback local ;
- zéro croissance de backlog pendant scrub/autoplay ;
- colonnes synchronisées à 100 %.

### Coupes

- command-to-photon p50/p95/max ;
- cible initiale p95 ≤ 150 ms pendant interaction ;
- résultat final exact ≤ 250 ms après release ;
- taux de commandes coalescées, calculs annulés et résultats stale.

### Réseau/reconnexion

- contrôle p95 ≤ 100 ms même pendant bulk ;
- débit utile et retransmissions ;
- reprise/snapshot cible p95 ≤ 5 s sur réseau local nominal ;
- aucun buffer non borné.

Ces seuils sont des gates proposées D20. Tout ajustement est une décision documentée avec test utilisateur, pas une requalification rétroactive d'un échec.

## 7. Mémoire

Rapporter :

- mémoire système/Unity/GPU au démarrage ;
- assets immuables par hash ;
- coût marginal par brain instance et colonne ;
- buffers réseau/pools ;
- cache avant/après fermeture ;
- pic pendant snapshot/coupe/timeline ;
- octets timeline CPU/GPU uniques avant/après déduplication et marge de sécurité séparée ;
- estimation disponible, budget Quest validé, budget utilisateur demandé et budget effectif retenu ;
- exactitude et précocité des refus juste au-dessus du budget ;
- mémoire et température à 5/15/30 minutes.

Attendu : surface partagée réellement une fois ; libération démontrée ; marge système documentée ; aucun patient persisté ; aucun plafond d'indices/colonnes/cerveaux ; pas de paging V1 ; les ressources déjà actives survivent au refus d'une nouvelle allocation.

## 8. Robustesse protocole

- headers tronqués, longueurs extrêmes, type inconnu ;
- NaN/Inf, indices hors bounds, dimensions incohérentes ;
- chunk manquant/dupliqué/corrompu ;
- command duplicate/replay ;
- résultats out-of-order ;
- asset hash conflict ;
- snapshot interrompu ;
- serveur lent ou malveillant ;
- allocation bomb/compression bomb.

Attendu : rejet borné, erreur typée, aucune allocation incontrôlée, aucune modification partielle du renderer.

## 9. Interactions et accessibilité

Tâches : connecter, placer cerveau, changer fortement d'échelle, tourner autour, sélectionner site, consulter graphes/tags/matrices du site, créer/orienter coupe, jouer/scrubber timeline, ouvrir panel, recentrer, récupérer après déconnexion.

Mesures par mains et contrôleurs : réussite, temps, erreurs, reprises, jitter, fatigue déclarée et préférence. Les contrôleurs doivent couvrir le scénario V1 complet ; les mains couvrent au minimum les interactions principales. Les panels du site sélectionné sont évalués comme fonction de très haute priorité, pas comme polish facultatif.

## 10. Build, distribution et licences

- builds reproductibles Windows x64, macOS Apple Silicon, Ubuntu 24.04 x64 + APK ARM64/IL2CPP ;
- package lock et versions archivés ;
- symboles/debug séparés ;
- APK signé sans secret dans repo/log ;
- SBOM et notices Unity/Meta/native/vendor ;
- canal Meta pilote testé avec utilisateur non développeur ;
- update et rollback ;
- poids mesuré du pont XR dans le build standard, du module host/assets optionnel et d'une éventuelle intégration complète ;
- si module absent, proposition discrète seulement avec canal d'installation fiable, entrée masquée sinon ; update automatique vérifié pour un module déjà installé ;
- aucune augmentation Desktop non expliquée ou qualifiée de non drastique.

## 11. Go/no-go pilote

Go seulement si :

- D01–D18 et D21–D24 résolues ou provisional avec gate passée ;
- D19 canal réel validé ;
- D20 mesuré et accepté ;
- E01–E11 passent ;
- fidélité propriétaire scientifique signée ;
- revue sécurité/vie privée/licences terminée ;
- defects restants classés sans contournement par plafond.

Un no-go conserve les artefacts hors Git sous `.artifacts/xr/` et déclenche une décision/spike ; il ne réduit pas silencieusement le nombre de sites, colonnes ou visualisations.

En cas de compromis, la décision respecte cet ordre : fluidité/inconfort, exactitude scientifique, précision d'interaction, disponibilité des données, esthétique, chargement initial, puis mémoire dans l'enveloppe sûre.
