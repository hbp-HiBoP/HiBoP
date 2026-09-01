# HiBoP XR — registre de décisions

**Version :** 0.2  
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
| D10 | HTTPS + WSS ; bibliothèque à valider | REQUIRES_SPIKE |
| D11 | Enveloppe binaire + buffers typés | REQUIRES_SPIKE |
| D12 | Révisions par scope, snapshot et resync | RESOLVED |
| D13 | Sites bufferisés + index spatial | RESOLVED |
| D14 | Assets immuables partagés entre cerveaux | RESOLVED |
| D15 | XRI/OpenXR, Meta derrière adaptateur | PROVISIONAL |
| D16 | Input System obligatoire en XR ; migration Desktop séparée | RESOLVED |
| D17 | Aucune donnée patient persistante sur Quest | RESOLVED |
| D18 | Versions distinctes, handshake de compatibilité | RESOLVED |
| D19 | Canal de distribution pilote Meta | REQUIRES_SPIKE |
| D20 | Enveloppes de performance V1 | REQUIRES_SPIKE |

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

Les motivations, règles complètes et conditions de réouverture sont enregistrées dans [ADR P02](adr/P02-contracts.md) et le [catalogue des scopes V1](contracts/P02-scope-catalog.md).

Les décisions de rendu, leurs frontières d'assemblage et les tolérances synthétiques sont enregistrées dans [ADR P03](adr/P03-render-model.md). Les hashes reproductibles sont consignés dans la [preuve de parité P03](evidence/P03/render-model-parity.md).

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

**Statut : REQUIRES_SPIKE. Baseline proposée : HTTPS + WSS sur un endpoint appairé.** WSS transporte contrôle, état et petits résultats dynamiques ; HTTPS transporte assets immuables découpés, hashés, reprenables et annulables. L'IP manuelle est obligatoire, la découverte locale facultative.

**Sécurité.** TLS via une bibliothèque maintenue ; première confiance par code court liant l'identité cryptographique du poste, puis pinning. Aucun protocole cryptographique maison.

**Gate.** Même bibliothèque serveur/client validée sur Windows, macOS, Linux et Quest IL2CPP, avec priorité du contrôle pendant un gros transfert, reconnexion et firewall documentés.

## D11 — sérialisation et compression

**Statut : REQUIRES_SPIKE.** Contrôle schématisé/AOT-safe ; gros tableaux en blocs contigus little-endian typés avec type, dimensions, longueur, checksum, compression et calibration. Pas de JSON ni base64 pour les buffers lourds.

**Baseline qualité.** `float32` exact. `float16`, 8 bits et compression ne sont activés que représentation par représentation après mesure d'erreur, d'image, CPU, copies et GC.

## D12 — état, snapshot et reconnexion

**Statut : RESOLVED.** `sessionId` définit l'epoch hôte ; `commandId` l'idempotence ; `interactionId/sequence` la coalescence ; révision globale et révisions par scope l'ordre sémantique ; hashes l'identité des assets.

**Reconnexion.** Snapshot initial systématique. Après coupure, demande de reprise avec révisions connues ; deltas d'un journal borné si possible, sinon snapshot complet. Le layout XR local est réappliqué uniquement aux instances encore valides.

## D13 — 37 500 sites

**Statut : RESOLVED pour l'architecture, performance à valider par D20.** Rendu instancié/procédural depuis buffers ; mises à jour groupées ; index spatial CPU dans le repère local du cerveau pour ray et proximité ; mapping stable `siteId`.

**Interdit.** GameObject, MeshRenderer ou collider par site, scan O(N) par frame, limite artificielle de cardinalité.

## D14 — plusieurs cerveaux

**Statut : RESOLVED pour l'architecture.** `SurfaceAsset` immuable et dédupliqué par hash ; chaque `BrainInstance` conserve sa transformation locale et chaque colonne seulement ses buffers mutables. La topologie MNI n'est jamais clonée uniquement pour changer des UV.

**Capacité.** Pas de constante maximale fonctionnelle. L'enveloppe mesurée peut réduire la fréquence des mises à jour, jamais masquer des données.

## D15 — couche d'interaction

**Statut : PROVISIONAL.** OpenXR + XRI + XR Hands sont la baseline portable. Les capacités Meta, dont passthrough, restent derrière des adaptateurs. Meta Interaction SDK n'est ajouté que si un geste nécessaire est mesurablement insuffisant avec XRI.

**Gate.** Ray, grab, rotation, échelle à deux mains, proximité, UI et coupe avec mains et contrôleurs, assis, sur des objets de 10 cm à 2 m.

## D16 — Input System

**Statut : RESOLVED.** Le projet XR utilise le nouveau Input System dès sa création, requis par OpenXR. La migration complète Desktop est un workstream séparé avec parité souris/clavier/caméra/raccourcis et trois builds ; elle ne bloque pas les spikes réseau/rendu du second projet.

## D17 — sécurité et vie privée

**Statut : RESOLVED pour l'absence de persistance.** Données patient en mémoire de session seulement ; logs redacted et IDs opaques. Seuls endpoint et matériau d'appairage peuvent être persistés dans le stockage sécurisé plateforme. Fermeture et nouvel epoch imposent une purge ; la matrice exacte déconnexion/retry, arrière-plan, timeout, crash et reprise doit être décidée en P14-B avant implémentation.

## D18 — versions et compatibilité

**Statut : RESOLVED.** Apps Desktop/XR, protocole, schéma et `hbp_core` ont des versions distinctes. Le handshake annonce protocol major/minor, schema hash, capabilities, versions d'app, commit et version native. Major ou schéma incompatible : refus ; minor : négociation par capability.

**Release.** Une paire coordonnée est distribuable en V1 sans imposer le même SemVer aux deux applications.

## D19 — distribution Quest

**Statut : REQUIRES_SPIKE.** Pilote recommandé via canal Meta privé Alpha/Beta, après vérification de l'organisation et conformité packaging. Le sideload reste réservé au développement et ne fournit pas les mises à jour de plateforme.

**Gate.** Vérifier au moment du pilote les règles Meta, le nombre d'utilisateurs, la signature, les permissions, la politique de données et le parcours d'installation réel.

## D20 — enveloppes et gates de performance

**Statut : REQUIRES_SPIKE.** Les seuils initiaux sont des critères d'acceptation à confirmer sur Quest 3, pas des résultats acquis :

- rendu 72 Hz : CPU et GPU frame p95 chacun sous 13,89 ms, sans dérive thermique soutenue ;
- sites : 37 500 visibles, picking correct à 100 %, latence p95 inférieure à 50 ms ;
- timeline : aucune croissance de backlog, colonnes atomiques, command-to-visible p95 proposé à 100 ms en lecture courante ;
- coupe : dernier résultat canonique toujours affiché, p95 proposé à 150 ms pendant le geste et convergence finale sous 250 ms ;
- reconnexion locale : reprise ou snapshot explicite, sans état mixte, cible p95 5 s ;
- mémoire : aucune éviction silencieuse de données, marge système mesurée après 30 minutes et sous contrainte thermique.

Chaque mesure publie environnement, dataset, p50, p95, maximum, mémoire et décision. Un seuil non atteint entraîne optimisation, changement de stratégie documenté ou nouvelle décision — jamais un plafond fonctionnel caché.
