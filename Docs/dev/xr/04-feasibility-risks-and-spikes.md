# HiBoP XR — faisabilité, risques et spikes

**Version :** 0.2  
**Règle :** un spike produit des données reproductibles et ferme une décision ; il ne devient pas du code de production par défaut.

## 1. Échelle

- **P0** : bloque le choix d'architecture ou le chemin critique V1.
- **P1** : bloque la qualité du pilote.
- **P2** : optimisation ou extension post-baseline.

Chaque rapport contient commit, Unity/SDK/OS, modèle et version Quest, réseau, dataset, 10 minutes de warm-up, au moins 30 minutes pour thermique, p50/p95/max, mémoire, traces et décision `PASS/FAIL/INCONCLUSIVE`.

## 2. Risques

| Risque | Prob. | Impact | Preuve actuelle | Fermeture |
| --- | --- | --- | --- | --- |
| bibliothèque réseau incompatible IL2CPP/3 OS | haute | critique | aucune implémentation historique | S01 |
| payload timeline trop volumineux | haute | critique | 0,5355 MiB/colonne float32 MNI | S02 |
| coupe distante trop lente | moyenne | élevée | calcul Desktop rapide, E2E inconnu | S03 |
| 37 500 sites trop coûteux | haute | critique | architecture actuelle par objets/O(N) | S04 |
| renderer partagé non fidèle | moyenne | critique | extraction non faite | S05 |
| interpolation surface divergente | moyenne | élevée | alpha temporel non explicite | S05 |
| OpenXR/XRI insuffisant pour mains | moyenne | moyenne | MRTK historique seulement | S06 |
| `hbp_core` Android instable | moyenne | moyenne | compilation ARM64 seulement | S07 |
| pression mémoire multi-cerveaux | haute | élevée | mesh actuel cloné par colonne | S08 |
| persistance/logs patient | moyenne | critique | Core actuel écrit sur disque | S09 |
| distribution Meta non prête | moyenne | élevée | règles/organisation externes | S10 |
| migration Input casse Desktop | moyenne | élevée | legacy Input actif | workstream séparé |

## S01 — transport sécurisé 3 OS + Quest

**Priorité : P0. Décisions : D10, D11, D12.**

**Environnement.** Host minimal Windows/macOS/Linux ; client Editor puis APK IL2CPP Quest 3 ; Wi-Fi local avec et sans multicast ; firewall nominal.

**Procédure.** Tester les bibliothèques candidates avec TLS/pinning, WSS contrôle et HTTPS ranges. Transférer simultanément un asset 100 MiB, 20 commandes/s et un flux dynamique. Couper Wi-Fi 1, 5 et 30 s ; changer l'identité serveur ; corrompre chunks et headers.

**Mesures.** réussite build/AOT, temps d'appairage/reprise, RTT p50/p95/max, débit, latence contrôle sous bulk, allocations/GC, mémoire, détection d'identité et hash.

**Gate.** quatre plateformes fonctionnelles ; aucune commande p95 > 100 ms à cause du bulk ; reprise cohérente cible p95 5 s ; identité modifiée bloquée ; aucun payload patient en clair. Sinon changer bibliothèque ou séparation des canaux.

## S02 — timeline multi-colonnes

**Priorité : P0. Décisions : D06, D07, D11, D20.**

**Environnement.** MNI réel et mesh patient ; 1/3/8 colonnes ; cas simple/courant/maximal ; float32 puis candidats float16/compression ; réseau représentatif.

**Procédure.** Autoplay 10 min, scrub continu 60 s, pause/reprise, changement de paramètres en vol. Comparer Desktop et Quest frame par frame aux mêmes `TemporalSample`.

**Mesures.** calcul, extraction, sérialisation, octets, débit, RTT, décodage, upload GPU, p50/p95/max command-to-visible, backlog, stale drops, erreur numérique/image.

**Gate.** backlog toujours borné, aucune colonne mixte, p95 proposé ≤ 100 ms en lecture courante, convergence scrub ≤ 250 ms. Quantification acceptée uniquement dans la tolérance scientifique/visuelle documentée.

## S03 — coupe canonique distante

**Priorité : P0. Décisions : D08, D09, D05.**

**Environnement.** coupes MNI/patient ; 1/3/8 colonnes ; mains et contrôleurs ; plans avec petite/grande topologie ; Wi-Fi courant/dégradé.

**Procédure.** Manipuler 60 s, produire 20–60 commandes/s, coalescer, relâcher à une position connue. Comparer résultat final à Desktop. Tester réutilisation geometry/base et overlay seul.

**Mesures.** calcul, transfert, apply, command-to-photon p50/p95/max, taux coalescé/annulé/stale, octets, exactitude finale.

**Gate.** jamais de retour arrière ; dernier résultat exact ; cible p95 ≤ 150 ms pendant geste et convergence ≤ 250 ms. Échec après optimisation déclenche S07 pour coupe ciblée, pas un calcul approximatif silencieux.

## S04 — 37 500 sites

**Priorité : P0. Décisions : D13, D20.**

**Environnement.** Quest 3, 37 500 sites, 1/3/8 instances, tailles/visibilités dynamiques, passthrough et VR.

**Procédure.** Comparer baseline objets uniquement comme témoin, instancing/GraphicsBuffer, grille puis BVH ; ray, proche, changement d'échelle 10 cm–2 m ; updates complètes et dirty ranges.

**Mesures.** CPU/GPU frame, draw calls, mémoire, allocations, upload, picking p50/p95/max, exactitude, thermique 30 min.

**Gate.** tous les sites présents ; picking 100 % sur cas déterministes, p95 < 50 ms ; budgets 72 Hz ; aucun objet/collider par site ni plafond codé.

## S05 — extraction RenderModel et parité

**Priorité : P0. Décisions : D03, D04, D06.**

**Environnement.** scène Unity indépendante consommant seulement Contracts/RenderModel/Rendering ; datasets de chaque représentation V1.

**Procédure.** Capturer sorties du Desktop, reconstruire sans Core/Data, comparer buffers et images. Créer un signal où `TemporalSample.Alpha` est non nul pour tester surface et sites.

**Mesures.** couverture de propriétés, écarts numériques, image diff, dépendances transitives, allocations et taille package.

**Gate.** reproduction complète sans données source ; scopes documentés ; interpolation attendue définie et identique ou défaut Desktop enregistré/corrigé avant baseline.

## S06 — OpenXR/XRI/Meta

**Priorité : P1. Décision : D15.**

**Environnement.** OpenXR + XRI + XR Hands ; variante Meta Interaction SDK derrière adaptateur ; mains/contrôleurs ; assis/debout.

**Procédure.** Ray, grab, two-hand scale, UI, sélection proche, cut gizmo, perte/reprise tracking, passthrough/VR.

**Mesures.** réussite tâche, temps, erreurs, jitter, fatigue qualitative structurée, CPU/GPU, dépendances/build size.

**Gate.** XRI reste baseline sauf fonction V1 non atteinte dont la variante Meta démontre un gain net. Aucun type Meta dans Contracts/Protocol.

## S07 — `hbp_core` Quest ciblé

**Priorité : P1 conditionnelle. Décision : D05.**

**Acquis.** CMake/NDK ARM64 Release compile ; cela ne passe pas le gate.

**Environnement.** plugin `arm64-v8a` stripé/importé, Unity IL2CPP, Quest 3 ; mêmes vectors que Desktop.

**Procédure.** appeler version/init, surface simple et coupe ; comparer Desktop ; répéter sous charge et 30 min ; auditer symboles exportés/licences.

**Mesures.** chargement/PInvoke, erreurs, p50/p95/max, mémoire, température/fréquence, batterie, écart numérique, taille APK.

**Gate.** parité dans tolérance, stabilité, enveloppe D20, symbol visibility nettoyée et bénéfice E2E supérieur à S03. Adoption par fonction seulement.

## S08 — multi-cerveaux et assets partagés

**Priorité : P1. Décisions : D14, D20.**

**Environnement.** MNI anatomical/inflated, surface patient, 1/3/8 instances et colonnes, transparence/sites.

**Procédure.** Vérifier hash/déduplication, transformations indépendantes, changements de colonne et libération. Mesurer après cycles ouverture/fermeture.

**Mesures.** mémoire par asset/instance/colonne, draw calls, upload, CPU/GPU, fuite, cache hit.

**Gate.** topologie physiquement partagée ; mémoire marginale expliquée ; 72 Hz sur scénario V1 ; aucune limite fonctionnelle fixe.

## S09 — sécurité, persistance et resync

**Priorité : P1. Décisions : D12, D17, D18.**

**Environnement.** APK release-like, Android storage/logcat capturés, hôte redémarré/mis à jour, versions compatibles/incompatibles.

**Procédure.** sessions avec données sentinelles, background/kill/reboot, coupures à chaque phase snapshot, replay/duplicate/out-of-order, endpoint malveillant.

**Mesures.** fichiers/logs/mémoire après purge, décisions handshake, état final, commandes dupliquées, délai reprise.

**Gate.** aucune sentinelle patient persistée/loggée ; incompatibilités refusées ; snapshot transactionnel ; pas de commande rejouée ni état mixte.

## S10 — build et distribution pilote

**Priorité : P1. Décision : D19.**

**Environnement.** organisation Meta vérifiée, APK signé, canal privé, comptes test non développeurs.

**Procédure.** CI → signature → upload Alpha/Beta → invitation → installation → update → rollback documenté ; vérifier permissions et politiques au jour du pilote.

**Mesures.** reproductibilité, taille, délai, erreurs packaging, parcours utilisateur, mises à jour.

**Gate.** utilisateur pilote installe et met à jour sans ADB ; licences/notice/politiques validées ; secret de signature protégé.

## 3. Ordre

1. S05 contrat/parité et S01 transport en parallèle logique.
2. S04 sites et S02 timeline.
3. S03 coupe.
4. S06, S08, S09.
5. S07 seulement si S03 échoue.
6. S10 avant pilote.

La migration Input System Desktop avance séparément ; elle ne doit pas retarder les preuves P0 du projet XR autonome.
