# P11 — timeline, autoplay et bundles atomiques

## Objectif et résultat observable

Synchroniser autoplay, pause et scrubbing depuis le Desktop vers toutes les colonnes fonctionnelles concernées, avec `DynamicFrameBundle` atomique, latest-wins et aucun backlog.

## Decision gate

**Hérité :** D06 projection Desktop, D07 bundle atomique, D11 float32 baseline, D12 révisions ; sémantique temporelle P03 résolue.

**À résoudre avant pipeline production :**

- `P11-A` : règle exacte déterminant les colonnes attendues dans un bundle ;
- `P11-B` : comportement en cas d'échec d'une colonne — rejet complet, état d'erreur ou partial capability ;
- `P11-C` : ownership et retour utilisateur pour play/pause/scrub concurrent Desktop/Quest ;
- `P11-D` : politique de cadence/adaptation et condition de drop, sans modifier le temps scientifique ;
- `P11-E` : inclusion atomique des sites et overlays de coupe avec la surface.

Une capability de bundle partiel ne peut être inventée pour masquer une performance insuffisante. Sans P11-A–E, rester en instrumentation/spike.

## Périmètre autorisé

- extraction post-projection Desktop ;
- DynamicFrameBundle et scheduler latest-wins ;
- transfert/décodage/upload ;
- surface/sites/overlays synthétiques puis réels ;
- diagnostics de pipeline.

## Hors périmètre

- envoi des données source/volume temporel complet ;
- calcul scientifique Quest ;
- quantification/compression non validée ;
- réduction du nombre de colonnes.

## Hypothèses fixées

- fréquence source ≠ fréquence réseau ≠ fréquence XR ;
- Quest rend la tête à 72 Hz indépendamment ;
- un active + un pending latest maximum par scope ;
- résultat stale rejeté avant upload ;
- float32 complet est la baseline de fidélité.

## Dépendances et état initial

- P03 RenderModel/interpolation ;
- P07 session ;
- P08 assets ;
- P10 sites pour intégration finale ;
- P12 overlay cut peut être simulé si P12 n'est pas intégré.

## Fichiers/modules pressentis

- Desktop projection adapter/scheduler ;
- Protocol DynamicFrameBundle ;
- XR frame decoder/commit ;
- Rendering dynamic buffers ;
- tests D5/D2/D3.

## Étapes

1. Résoudre P11-A–E.
2. Instrumenter calcul→extract→serialize→transfer→decode→upload.
3. Implémenter bundle complet float32 et commit atomique.
4. Implémenter active/pending latest, annulation et stale drop.
5. Brancher play/pause/scrub et outcomes.
6. Intégrer surface, sites et overlays décidés.
7. Mesurer 1/3/8 colonnes, autoplay et scrub.
8. Proposer compression/quantification seulement si baseline échoue, avec ADR.

## Tests et commandes

- D5 interpolation index/alpha ;
- ordre/révisions et bundles artificiellement retardés ;
- 1/3/8 colonnes sur D2/D3 ;
- autoplay 10 min et scrub 60 s ;
- p50/p95/max de chaque étape, backlog/stale drops ;
- golden buffer/image par bundle ;
- build/test 3 OS host + Quest.

## Critères de sortie binaires

- [ ] P11-A–E enregistrées ;
- [ ] aucun bundle mixte/partiel hors décision ;
- [ ] aucune croissance de backlog ;
- [ ] toutes les colonnes attendues avancent atomiquement ;
- [ ] interpolation P03 respectée ;
- [ ] lecture courante p95 cible ≤ 100 ms et scrub converge ≤ 250 ms, ou décision D20 réouverte ;
- [ ] aucune quantification non approuvée.

## Artefacts à remettre

Scheduler/bundles, adaptateurs/render commit, tests/goldens, rapport de pipeline et ADR P11.

## Conditions d'arrêt

Arrêter si P03 interpolation n'est pas fermée, si le bundle partiel devient nécessaire sans décision ou si les seuils nécessitent de supprimer colonnes/données.

## Prompt de démarrage

> Exécute P11 depuis `Docs/dev/xr/implementation-packets/P11-timeline.md`. Résous P11-A–E et vérifie l'interpolation P03 avant production. Implémente la baseline float32 atomique avec latest-wins, mesure 1/3/8 colonnes et n'introduis ni bundle partiel, quantification ni plafond sans décision explicite.
