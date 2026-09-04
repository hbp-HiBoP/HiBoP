# HiBoP XR — registre de décisions

**Version :** 0.3
**Règle :** chaque décision possède une baseline, une preuve et une condition explicite de réouverture.

## Synthèse

| ID | Décision | Statut |
| --- | --- | --- |
| D01 | Deux projets Unity | RESOLVED |
| D02 | Monorepo applicatif + repo `hbp_core` | RESOLVED |
| D03 | Packages UPM partagés, aucune copie | RESOLVED |
| D04 | Sous-ensemble portable, pas tout `HBP.Core`/`HBP.Data` | RESOLVED |
| D05 | `hbp_core` sur Quest hors baseline V1 | REQUIRES_SPIKE |
| D06 | Projection dynamique canonique sur Desktop | RESOLVED |
| D07 | `DynamicFrameBundle` atomique | RESOLVED |
| D08 | Coupes canoniques distantes, latest-wins | RESOLVED |
| D09 | `CutRenderResult` atomique et révisionné | RESOLVED |
| D10 | Kestrel sidecar + HTTPS/WSS appairés | PROVISIONAL |
| D11 | Protobuf + buffers float32 little-endian | PROVISIONAL |
| D12 | Révisions par scope, snapshot et resync | RESOLVED |
| D13 | Sites bufferisés + index spatial | RESOLVED |
| D14 | Assets immuables partagés entre cerveaux | RESOLVED |
| D15 | XRI/OpenXR, Meta derrière adaptateur | PROVISIONAL |
| D16 | Input System obligatoire en XR ; migration Desktop séparée | RESOLVED |
| D17 | Aucune donnée patient persistante sur Quest | RESOLVED |
| D18 | Versions distinctes, handshake de compatibilité | RESOLVED |
| D19 | Canal de distribution pilote Meta | REQUIRES_SPIKE |
| D20 | Enveloppes de performance V1 | REQUIRES_SPIKE |
| D21 | Rendu local Quest interaction-first, réévalué après E11 | PROVISIONAL |
| D22 | Session V1 unique et autorité/feedback XR | RESOLVED |
| D23 | Admission par budget réel, sans plafond de cardinalité | RESOLVED |
| D24 | Module XR optionnel et qualification OS progressive | PROVISIONAL |

## Décisions d'implémentation acceptées

| ID | Décision | Statut |
| --- | --- | --- |
| P02-A | IDs opaques 128 bits, pseudonymes propres à l'epoch, aucun nom/index | RESOLVED |
| P02-B | Huit scopes V1, propriétaire unique et catalogue normatif | RESOLVED |
| P02-C | Champs requis par défaut, `Optional<T>` explicite, évolution additive par capability | RESOLVED |
| P02-D | Duplicate-first, conflit rejeté sans mutation ni rebase implicite | RESOLVED |
| P03-A | Sites linéaires ; surfaces/coupes sample-and-hold ; alpha temporel distinct de l'opacité | RESOLVED |
| P03-B | Assets normalisés en espace Unity XYZ left-handed, millimètres, mapping versionné | RESOLVED |
| P03-C | D0/D5/D6 synthétiques : structure/octets exacts, calcul D5 `maxAbs <= 1e-6` | RESOLVED |
| P03-D | Copie ou transfert explicite, buffers read-only possédés, GC sans pooling V1 | RESOLVED |
| P03-E | Surfaces, sites, coupes et bundles atomiques via primitives génériques | RESOLVED |
| P08-A | Budget mémoire injecté ; LRU des seuls inactifs ; actifs jamais évincés | RESOLVED |
| P08-B | échec explicite et action utilisateur, sans réduction silencieuse | RESOLVED |
| P08-C | reprise 30 s ; purge staging/inactifs ; actifs purge-pending jusqu’au retrait explicite | RESOLVED |
| P08-D | limites d’allocation par type négociées au minimum des pairs | RESOLVED |
| P08-E | hashes propres aux variantes et manifeste inflated → anatomical | RESOLVED |
| P09-A | deux bindings exacts : visualisation suit la sélection, colonne reste épinglée | RESOLVED |
| P09-B | création uniquement sur demande XR, jamais depuis snapshot/resume | RESOLVED |
| P09-C | fermeture de cible ferme et libère explicitement les instances concernées | RESOLVED |
| P09-D | transform local ; apparence canonique ; topologie P08/P05 partagée par hash | RESOLVED |
| P09-E | layout conservé dans le même epoch pour IDs valides, purgé au nouvel epoch | RESOLVED |
| P10-A | positions/attributs en GraphicsBuffer, un RenderMeshPrimitives par set de sites | RESOLVED |
| P10-B | éligibilité visible/rayon positif ; classement géométrique déterministe puis ID opaque | RESOLVED |
| P10-C | positions et rayons en mm locaux ; scale BrainInstance uniforme ; ray min 2 mm, proximité 12 mm | RESOLVED |
| P10-D | BVH médian sur centres statiques, rebuild seulement au changement de hash | RESOLVED |
| P10-E | métadonnées de sélection sur allowlist, transitoires et jamais journalisées/persistées | RESOLVED |
| P11-A | membership canonique filtré par inclusion timeline, manifeste de contenu figé par requête | RESOLVED |
| P11-B | échec d'une colonne rejette le bundle complet et conserve le dernier commit | RESOLVED |
| P11-C | Timeline Desktop autoritaire ; intentions Quest séquencées, pending jusqu'au résultat | RESOLVED |
| P11-D | un actif + un pending latest par scope, annulation et stale-drop sans changer le temps | RESOLVED |
| P11-E | surface, sites et overlays préparés puis publiés par un commit atomique unique | RESOLVED |

Les motivations, règles complètes et conditions de réouverture sont enregistrées dans [ADR P02](adr/P02-contracts.md) et le [catalogue des scopes V1](contracts/P02-scope-catalog.md).

Les décisions de rendu, leurs frontières d'assemblage et les tolérances synthétiques sont enregistrées dans [ADR P03](adr/P03-render-model.md). Les hashes reproductibles sont consignés dans la [preuve de parité P03](evidence/P03/render-model-parity.md).

Les décisions de transfert, pression mémoire, lifecycle du cache et dépendances de variantes sont enregistrées dans [ADR P08](adr/P08-remote-assets.md). P14-B reste propriétaire du raccordement exact aux événements de plateforme et de la matrice sécurité globale.

Les bindings, fermetures et règles de restauration du layout local sont enregistrés dans [ADR P09](adr/P09-multi-brain.md). P09 rouvre P02 uniquement pour rendre explicite le mapping entité/scope nécessaire à ces bindings.

Le backend bufferisé, le BVH, les règles de classement, les unités/seuils et l'allowlist de métadonnées des sites sont enregistrés dans [ADR P10](adr/P10-sites.md). Le choix A/D est fondé sur le benchmark D3 hôte puis validé sur Quest 3 : 37 500 sites sans plafond, picking exact, CPU/GPU sous D20 et endurance 30 minutes sans dérive thermique ou mémoire.

Les règles de membership, d'échec complet, d'ownership temporel, de cadence latest-wins et de commit surface/sites/coupes sont enregistrées dans [ADR P11](adr/P11-timeline.md). La baseline float32 utilise désormais un preload lossless complet sous budget mémoire explicite ; 97 indices est le profil qualifié et non un plafond fonctionnel. La validation physique de cette baseline garde D20 ouvert avant production.

## D01 — topologie Unity

**Statut : RESOLVED.** Deux projets Unity : un shell Desktop et un shell Android/OpenXR. Ils peuvent employer la même version Unity, mais leurs packages, scènes, settings et profils de build restent isolés.

**Preuves.** HiBoP Desktop utilise URP, des plugins natifs desktop, des APIs UI/fichier et le legacy Input Manager. Le prototype HoloLens a démontré l'intérêt de l'isolation XR, mais sa copie complète a créé une forte dérive.

**Réouverture.** Seulement si un prototype à projet unique démontre une isolation de packages et settings équivalente sans contaminer les trois builds Desktop.

## D02 — topologie Git

**Statut : RESOLVED.** Un monorepo applicatif contient les deux projets et les packages UPM partagés ; `hbp_core` reste son repo natif indépendant.

**Alternative rejetée.** Un repo par application avec copies, modèle du prototype, empêche les changements atomiques et dérive rapidement.

**Fallback.** Séparer le repo XR si des permissions, cadences de release ou contraintes de checkout réellement indépendantes l'exigent ; dans ce cas seuls des packages versionnés sont consommés.

## D03 — partage de code et d'assets

**Statut : RESOLVED.** P01 matérialise uniquement `com.crnl.hibop.contracts`, `com.crnl.hibop.render-model` et `com.crnl.hibop.protocol` sous `Shared/Packages/`. Le code exclusivement Desktop reste sous `Assets/` et le code exclusivement XR — notamment le renderer, le client, OpenXR et Meta — sous `XR/Assets/`. Aucun fichier HiBoP existant n'est déplacé ou copié et les modèles HiBoP ne reçoivent pas de méthodes DTO. Toute extension future de `Shared/Packages/` exige une réouverture explicite de D03 et un ADR distinct.

**Gate.** Une modification commune doit être testée dans les deux projets depuis un seul fichier source.

## D04 — dépendance aux assemblies actuelles

**Statut : RESOLVED.** Le Quest ne référence ni l'intégralité de `HBP.Core.Runtime` ni `HBP.Data.Runtime`.

**Preuves.** Ces assemblies entraînent TMPro, UI, accès fichiers/base de données, wrappers natifs, globals et renderer Desktop. Elles ne constituent pas un contrat AOT/Android propre.

**Projection minimale.** De nouveaux DTO portent IDs et scopes, révisions, valeurs mathématiques, sérialisation, modèle de rendu et interfaces. Des adaptateurs Desktop externes les construisent à partir des modèles HiBoP ; aucun fichier ou type HiBoP existant n'est déplacé, copié ou enrichi de méthodes DTO. Les loaders, DB, préférences, calculs scientifiques et adaptateurs restent Desktop.

## D05 — `hbp_core` Android ARM64

**Statut : REQUIRES_SPIKE. Baseline V1 : Desktop uniquement.**

**Preuve acquise.** La révision auditée se configure et se compile avec Android NDK 28.2 pour `arm64-v8a`, API 29, en Release. Le `.so` AArch64 obtenu est non stripé et expose aussi de nombreux symboles de dépendances vendored.

**Ce que cela ne prouve pas.** Import Unity Android, P/Invoke IL2CPP, parité numérique sur appareil, mémoire, thermique, licence, stabilité et performance Quest.

**Condition d'adoption.** Une fonction seulement peut devenir locale si sa latence distante échoue, que sa parité est validée et que son coût Quest reste dans D20. Le desktop valide toujours la révision canonique.

## D06 — lieu de la projection dynamique

**Statut : RESOLVED.** Le Desktop choisit le `TemporalSample`, exécute la projection canonique et envoie le résultat post-projection minimal. Les données source et le champ volumique complet ne transitent pas par défaut.

**Point à corriger.** Le chemin actuel interpole les sites avec `TemporalSample.Alpha`, mais l'appel de surface observé n'expose pas clairement cet alpha temporel. Un test de parité doit déterminer s'il existe une incohérence actuelle avant extraction.

## D07 — bundle temporel

**Statut : RESOLVED.** Un `DynamicFrameBundle` contient session, timeline, temps logique, index/alpha sémantiques, révisions d'état/assets/paramètres et tous les résultats de colonnes attendus. Il est appliqué atomiquement.

**Politique.** Un bundle en calcul/transfert et un pending latest au maximum par scope ; commandes de scrubbing coalescées ; résultat obsolète rejeté avant allocation ou upload si possible.

## D08 — calcul des coupes

**Statut : RESOLVED.** Le geste et le plan de contrôle sont locaux ; le Desktop calcule la coupe exacte. Les interactions portent `interactionId` et `sequence`, la file est latest-wins et le dernier état demandé doit toujours être calculé.

**Fallback.** Backend local ciblé seulement après échec du spike distant et réussite de D05.

## D09 — contrat de coupe

**Statut : RESOLVED.** `CutRenderResult` transporte identité/révisions, plan canonique, géométrie ou hash inchangé, dimensions/format/espace colorimétrique, texture anatomique partageable et overlays par colonne. Le résultat est complet et remplacé atomiquement.

**Déduplication.** La texture de base et la géométrie stables sont référencées par hash ; un pas de timeline ne renvoie que les overlays fonctionnels modifiés.

## D10 — transport physique

**Statut : PROVISIONAL — WINDOWS/QUEST VALIDATED.** Kestrel/.NET self-contained sert HTTPS + WSS sur un endpoint, un port et un certificat communs. `websocket-sharp` est retenu provisoirement pour WSS Quest et `UnityWebRequest` pour HTTPS. WSS transporte contrôle, état et petits résultats dynamiques ; HTTPS transporte assets immuables découpés, hashés, reprenables et annulables. L'IP manuelle est obligatoire, la découverte locale facultative.

**Sécurité.** TLS via une bibliothèque maintenue ; première confiance par code court liant l'identité cryptographique du poste, puis pinning. Aucun protocole cryptographique maison.

**Preuve et limites.** P06-W/P06-WQ passent sur Windows et Quest IL2CPP, y compris pin SPKI, charge nominale, golden vectors et rejets identité/corruption. macOS/Linux natifs restent non qualifiés. Le sidecar ajoute environ 50,6 Mio compressés, soit environ 25 % d'une archive HiBoP de 200 Mio; ce dépassement est accepté pour poursuivre.

**Réouverture.** Qualification macOS/Linux, échec de packaging ou démonstration d'une solution embarquée sensiblement plus petite conservant transparence, TLS/pinning, limites et performances équivalentes. Une édition XR séparée reste le fallback de distribution.

**Intégration et distribution.** P15 possède le raccordement et la supervision du sidecar dans le parcours produit ; P18 possède son packaging dans les artefacts distribués et la qualification des OS déclarés.

## D11 — sérialisation et compression

**Statut : PROVISIONAL — WINDOWS/QUEST VALIDATED.** Protobuf 3.36.1 est retenu pour le contrôle avec framing borné et versionné. Les gros tableaux utilisent des blocs contigus `float32` IEEE-754 little-endian avec type, dimensions, longueur et SHA-256. Pas de JSON ni base64 pour les buffers lourds et aucune compression par défaut.

**Baseline qualité.** `float32` exact. `float16`, 8 bits et compression ne sont activés que représentation par représentation après mesure d'erreur, d'image, CPU, copies et GC.

**Réouverture.** Divergence de golden vector sur une plateforme native, allocations Protobuf problématiques sous charge réelle, ou dataset démontrant un gain net et reproductible pour une compression/quantification compatible avec les tolérances scientifiques.

## D12 — état, snapshot et reconnexion

**Statut : RESOLVED.** `sessionId` définit l'epoch hôte ; `commandId` l'idempotence ; `interactionId/sequence` la coalescence ; révision globale et révisions par scope l'ordre sémantique ; hashes l'identité des assets.

**Reconnexion.** Snapshot initial systématique. Après coupure, un nouveau snapshot complet est la baseline V1. Les deltas d'un journal borné restent une optimisation négociable si déjà disponible, jamais un prérequis produit. Le layout XR local est réappliqué uniquement aux instances encore valides ; tracking, passthrough et transformations locales continuent pendant que l'état scientifique reste gelé et signalé.

## D13 — 37 500 sites

**Statut : RESOLVED pour l'architecture, performance à valider par D20.** Rendu instancié/procédural depuis buffers ; mises à jour groupées ; index spatial CPU dans le repère local du cerveau pour ray et proximité ; mapping stable `siteId`.

**Interdit.** GameObject, MeshRenderer ou collider par site, scan O(N) par frame, limite artificielle de cardinalité.

## D14 — plusieurs cerveaux

**Statut : RESOLVED pour l'architecture.** `SurfaceAsset` immuable et dédupliqué par hash ; chaque `BrainInstance` conserve sa transformation locale et chaque colonne seulement ses buffers mutables. La topologie MNI n'est jamais clonée uniquement pour changer des UV.

**Capacité.** Pas de constante maximale fonctionnelle. Si une nouvelle instance dépasserait l'enveloppe sûre, seule sa création est refusée avec explication ; les instances existantes ne sont ni masquées ni évincées.

## D15 — couche d'interaction

**Statut : PROVISIONAL.** OpenXR + XRI + XR Hands sont la baseline portable. Les capacités Meta, dont passthrough, restent derrière des adaptateurs. Meta Interaction SDK n'est ajouté que si un geste nécessaire est mesurablement insuffisant avec XRI.

**Gate.** Ray, grab, rotation, échelle à deux mains, proximité, UI et coupe avec contrôleurs sur le scénario complet et mains sur les interactions principales, assis/debout, y compris inspection rapprochée d'un cerveau fortement agrandi.

## D16 — Input System

**Statut : RESOLVED.** Le projet XR utilise le nouveau Input System dès sa création, requis par OpenXR. La migration complète Desktop est un workstream séparé avec parité souris/clavier/caméra/raccourcis et trois builds ; elle ne bloque pas les spikes réseau/rendu du second projet.

## D17 — sécurité et vie privée

**Statut : RESOLVED pour l'absence de persistance.** Données patient en mémoire de session seulement ; les noms patient, libellés de site et noms de colonne nécessaires à l'UI sont autorisés transitoirement sur allowlist, mais jamais persistés ni journalisés. Les logs restent redacted et les IDs opaques. Seuls endpoint et matériau d'appairage peuvent être persistés dans le stockage sécurisé plateforme. P08 fixe les effets internes du cache lorsqu'il reçoit interruption, expiration du lease, nouvel epoch, background ou fermeture. P14-B doit encore décider et valider le raccordement exact de ces événements aux transitions Android/application, ainsi que logout, crash et reboot.

## D18 — versions et compatibilité

**Statut : RESOLVED.** Apps Desktop/XR, protocole, schéma et `hbp_core` ont des versions distinctes. Le handshake annonce protocol major/minor, schema hash, capabilities, versions d'app, commit et version native. Major ou schéma incompatible : refus ; minor : négociation par capability.

**Release.** Une paire coordonnée est distribuable en V1 sans imposer le même SemVer aux deux applications.

## D19 — distribution Quest

**Statut : REQUIRES_SPIKE.** Pilote recommandé via canal Meta privé Alpha/Beta, après vérification de l'organisation et conformité packaging. Le sideload reste réservé au développement et ne fournit pas les mises à jour de plateforme.

**Gate.** Vérifier au moment du pilote les règles Meta, le nombre d'utilisateurs, la signature, les permissions, la politique de données et le parcours d'installation réel.

Ce gate appartient à P18 après intégration P15, normalisation P16 et cleanup P17.

## D20 — enveloppes et gates de performance

**Statut : REQUIRES_SPIKE.** Les seuils initiaux sont des critères d'acceptation à confirmer sur Quest 3, pas des résultats acquis :

- rendu 72 Hz : CPU et GPU frame p95 chacun sous 13,89 ms, sans dérive thermique soutenue ;
- sites : 37 500 visibles, picking correct à 100 %, latence p95 inférieure à 50 ms ;
- timeline : après preload, aucune croissance de backlog, colonnes atomiques et tout index admis visible au plus tard à la frame XR suivante ; confirmation Desktop et rollback mesurés séparément ;
- coupe : dernier résultat canonique toujours affiché, p95 proposé à 150 ms pendant le geste et convergence finale sous 250 ms ;
- reconnexion locale : reprise ou snapshot explicite, sans état mixte, cible p95 5 s ;
- mémoire : aucune éviction silencieuse de données, marge système mesurée après 30 minutes et sous contrainte thermique.

Chaque mesure publie environnement, dataset, p50, p95, maximum, mémoire et décision. Un seuil non atteint entraîne optimisation, changement de stratégie documenté ou nouvelle décision — jamais un plafond fonctionnel caché.

**Réouverture P11 du 4 septembre 2026.** Le pipeline float32 géré synthétique dépasse déjà la cible timeline sur Windows : p95 end-to-end loopback de 179,207 ms pour 1 colonne, 433,535 ms pour 3 et 2 652,031 ms pour 8. La baseline reste inchangée et sans quantification/compression/plafond ; l'optimisation float32 contiguë, le réseau réel et l'upload Quest doivent être mesurés avant tout GO production.

**Spike Quest P11 du 4 septembre 2026.** Les copies contiguës réduisent le p95 Windows du dernier run à 151,702 / 334,455 / 2 121,770 ms. Sur Quest 3, décodage, soumission des uploads Unity et commit atomique locaux valent 13,636 / 34,225 / 191,583 ms p95 pour 1/3/8 colonnes. Au meilleur débit Quest P06 mesuré, le transfert seul impose déjà 132,091 / 396,236 / 2 409,433 ms. La cible timeline de 100 ms ne peut donc pas être fermée avec un payload complet à chaque pas. D20 reste `REQUIRES_SPIKE` jusqu'à une décision explicite sur la reconstruction lossless depuis des contenus inchangés mis en cache, la cible, ou une autre stratégie autorisée. Voir la [preuve D20 timeline](evidence/D20/timeline-quest-spike.md).

**Décision preload P11 du 4 septembre 2026.** La timeline dérivée float32 complète est transférée et préparée une fois, puis les accès aléatoires ne transmettent plus de frame et sélectionnent une tranche GPU via un index de 4 octets. L'admission est régie par un budget explicite d'octets lossless uniques ; 1–97 indices, y compris 8 colonnes × 37 500 sites × 97, est le profil qualifié et non un plafond. Un dépassement échoue sans troncature ; les cas extrêmes non qualifiés tels que 8 × 37 500 × 3 073 restent différés. La déduplication est byte-exacte, sans quantification, compression ni bundle partiel. Sur Quest 3, le profil maximal passe 60 s de scrub aléatoire et 10 min d'autoplay : soumission p95 `0,0506 / 0,0529 ms`, fin de frame p95 `14,2364 / 14,2538 ms`, delta maximal une frame, zéro swap/OOM et statut thermique 0. Le sous-gate P11 sélection locale préchargée est PASS. D20 global reste `REQUIRES_SPIKE` pour les autres fonctions et P11 reste NO-GO production jusqu'au raccord renderer command-to-photon et transport/UX P15. Voir la [preuve preload](evidence/D20/timeline-preload-implementation.md).

## D21 — stratégie de rendu orientée interaction

**Statut : PROVISIONAL.** La baseline V1 conserve un renderer local Quest alimenté par des assets et résultats scientifiques calculés sur Desktop. Elle protège le tracking, la perspective, le passthrough et la manipulation lorsqu'un utilisateur agrandit un cerveau et tourne physiquement autour de lui. Un flux vidéo distant principal est rejeté à ce stade, car il devrait réintroduire profondeur, reprojection et picking côté casque pour fournir la même expérience.

**Gate de réouverture.** Après E11, le prototype end-to-end pseudo-fonctionnel fait l'objet d'une revue centrée sur fluidité, confort, fidélité et complexité réellement observée. La décision peut alors être simplifiée ou rouverte. Aucun second système de rendu de production n'est développé en parallèle avant ce gate ; un miroir spectateur reste une extension indépendante hors V1.

## D22 — session et autorité d'interaction V1

**Statut : RESOLVED.** Une session relie un HiBoP Desktop à un Quest. Les transformations des cerveaux/panels, le tracking et le feedback immédiat sont locaux. Les commandes scientifiques, la sélection canonique et la timeline canonique restent validées par Desktop ; le Quest peut afficher un état optimiste `pending`, puis accepter ou rollback vers la dernière valeur Desktop.

**UX retenue.** Le casque contient l'UI des interactions courantes. Les graphes, tags, matrices et panels du site sélectionné sont de très haute priorité. Les contrôleurs couvrent tout le scénario V1 ; les mains couvrent les interactions principales. La disposition peut survivre à une reconnexion courte du même epoch, mais n'a pas à persister entre sessions.

## D23 — admission et politique de ressources

**Statut : RESOLVED.** Timeline, cerveaux et coupes sont admis selon leur coût CPU/GPU réel après déduplication, partage statique et représentation des canaux absents. Aucun nombre d'indices, colonnes, sites ou cerveaux n'est un plafond fonctionnel. Pour la timeline, le budget effectif est le minimum de la limite Quest validée et d'une estimation conservatrice de la mémoire courante, avec marge de sécurité séparée ; l'UI Desktop peut permettre une valeur utilisateur dans cette borne sûre.

**Refus.** Un dépassement est détecté avant transfert et refuse seulement la nouvelle ressource. Le message donne requis/permis, cardinalités, principaux contributeurs et explication. Il ne propose pas de réduire la plage ou les colonnes dans le flux XR. La V1 ne pagine pas et ne persiste pas de données scientifiques sur le Quest.

**Représentations compactes.** Elles deviennent automatiques seulement après validation d'équivalence visuelle. Avant validation, elles peuvent être proposées explicitement après refus, avec feedback sur la dégradation. Toute extension paging/disque exige une nouvelle décision et des preuves de latence, sécurité et usure.

## D24 — empreinte Desktop et qualification des plateformes

**Statut : PROVISIONAL.** Le build HiBoP standard peut contenir un pont et un point d'entrée XR très légers. Le host, ses runtimes et les assets volumineux restent un module optionnel lancé/supervisé par HiBoP, sauf si une mesure démontre qu'une intégration plus complète n'augmente pas drastiquement le build. Si le module manque, l'entrée propose discrètement son installation lorsqu'un canal fiable existe ; sinon elle est masquée. Un module déjà installé peut être mis à jour automatiquement avec contrôle de compatibilité.

**Ordre de validation.** Windows x64 est la première plateforme du prototype. Après E11, macOS Apple Silicon est qualifié avec un MacBook Air M2 comme machine minimale de test, puis Ubuntu 24.04 x64. Quest 3 est la cible casque V1 ; Quest 3S n'est pas promis.

**Gate.** P17/P18 publient la taille du pont, du module et de l'intégration complète, le comportement d'installation/update et les mesures runtime sur chaque OS avant de figer le packaging final.
