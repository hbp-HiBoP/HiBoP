# HiBoP XR — paquets d'implémentation

**Version :** 0.4
**Usage :** un paquet par chat Codex, avec état de départ contrôlé et résultat intégrable indépendamment.

## Règle absolue de décision

Avant toute modification de code, scène, prefab, package, settings, CI ou format persistant, le chat doit exécuter le `Decision gate` du paquet.

Une décision est considérée explicite seulement si elle est :

1. héritée d'une entrée `RESOLVED` du registre D01–D24 ;
2. fixée sans ambiguïté dans le paquet ;
3. produite par une phase de spike, enregistrée dans un ADR ou le registre, puis acceptée par l'autorité indiquée ;
4. fournie explicitement par l'utilisateur.

Une préférence de l'agent, une convention implicite, un exemple de code ou le comportement du prototype HoloLens ne constituent pas une décision.

Si une décision structurante manque :

- ne pas commencer l'implémentation de production ;
- inspecter uniquement les preuves nécessaires ;
- produire une fiche de décision avec options, recommandation, conséquences, preuve attendue et fichiers affectés ;
- demander la décision à l'utilisateur lorsque plusieurs issues changent matériellement le produit ou l'architecture ;
- reprendre le paquet seulement après enregistrement explicite de la décision.

Les choix locaux, réversibles et sans effet sur les contrats publics peuvent être pris par l'agent et documentés dans le handoff.

## Protocole commun d'un chat

1. Lire `AGENTS.md`, le paquet, D01–D24 et ses documents normatifs.
2. Vérifier branche/worktree, modifications préexistantes et dépendances intégrées.
3. Exécuter le `Decision gate` et annoncer `GO`, `DECISION_ONLY` ou `BLOCKED`.
4. Ne toucher qu'au périmètre autorisé.
5. Valider selon les commandes du paquet et les instructions Unity MCP/CLI du dépôt.
6. Mettre à jour tests, ADR/registre, sources et paquet si une hypothèse a changé.
7. Livrer fichiers modifiés, preuves, limites et décision de sortie.
8. Ne pas commit/push sauf demande explicite.

## Graphe de dépendances

```text
P00 Baselines
 ├─ P01 Topologie
 │   ├─ P02 Contracts
 │   │   ├─ P03 RenderModel
 │   │   │   ├─ P05 Renderer statique
 │   │   │   │   ├─ P10 Sites
 │   │   │   │   └─ P09 Multi-cerveaux (après P08)
 │   │   │   ├─ P11 Timeline (après P07/P08)
 │   │   │   └─ P12 Coupes (après P07/P08)
 │   │   └─ P06 Transport spike
 │   │       └─ P07 Session distribuée
 │   │           ├─ P08 Assets distants
 │   │           └─ P14 Sécurité
 │   └─ P04 XR Bootstrap
 └─ PX1 Input System Desktop (voie parallèle)

P09 + P10 + P11 + P12 ──> P13 Interactions V1
P13 + P14 ──────────────> P15 Intégration produit end-to-end
P15 ────────────────────> P16 Architecture de production
P16 ────────────────────> P17 Cleanup et durcissement
P17 ────────────────────> P18 Industrialisation
P12 FAIL distant ───────> décision explicite ──> PX2 hbp_core Quest
```

Après P11, l'ordre séquentiel recommandé est `P12 → fermeture ciblée P05-D → P14 → P13 → P15 → P16 → P17 → P18`. P12 peut avancer malgré P05-D ; P13 ne se ferme pas avant validation de la transparence et intégration des contraintes P14. PX1 reste une voie indépendante et PX2 n'est ouvert qu'après la décision explicite prévue par P12.

## Index

| Paquet | Livrable principal | Décision préalable notable |
| --- | --- | --- |
| [P00](P00-baselines.md) | fixtures et baselines | datasets autorisés |
| [P01](P01-topology.md) | **COMPLETE** — monorepo + projet XR | ADR P01 accepté |
| [P02](P02-contracts.md) | package Contracts | représentation des IDs |
| [P03](P03-render-model.md) | RenderModel fidèle | sémantique d'interpolation |
| [P04](P04-xr-bootstrap.md) | APK OpenXR minimal | matrice packages |
| [P05](P05-static-renderer.md) | **CANDIDATE** — surface statique Quest, transparence P05-D ouverte | baseline shader/passthrough |
| [P06](P06-transport-spike.md) | **ACCEPTED PROVISIONAL** — transport/codec Windows+Quest | macOS/Linux différés |
| [P07](P07-distributed-session.md) | handshake/snapshot/resume | résultats P06 |
| [P08](P08-remote-assets.md) | assets par hash/cache mémoire | politique de pression |
| [P09](P09-multi-brain.md) | instances dédupliquées | bindings V1 |
| [P10](P10-sites.md) | 37 500 sites/picking | backend issu des mesures |
| [P11](P11-timeline.md) | **COMPLETE / QUEST PASS** — bundles atomiques et preload lossless sous budget | interpolation P03 |
| [P12](P12-cuts.md) | coupes canoniques distantes et overlays préchargés | D23 + seuils D20 |
| [P13](P13-v1-interactions.md) | UX V1 complète | inventaire fonctionnel signé + contraintes P14 |
| [P14](P14-security.md) | sécurité/purge/redaction | allowlist transitoire + matrice de cycle de vie |
| [P15](P15-end-to-end-integration.md) | vraie tranche Desktop/Quest | parcours produit signé, Windows + Quest d'abord |
| [P16](P16-production-architecture.md) | architecture et nomenclature finales | revue D21 post-E11 + mapping accepté |
| [P17](P17-cleanup-hardening.md) | cleanup et Players consolidés | inventaire de suppression accepté |
| [P18](P18-industrialization.md) | CI/distribution pilote et module optionnel | D24 + organisation/canal Meta |
| [PX1](PX1-desktop-input-system.md) | migration Input Desktop | carte de parité |
| [PX2](PX2-hbp-core-quest.md) | backend natif ciblé | autorisation après échec P12 |

## Format obligatoire de chaque paquet

Chaque fichier contient : objectif/résultat observable, Decision gate, périmètre/hors périmètre, hypothèses fixées, dépendances/état initial, fichiers pressentis, étapes, tests/commandes, critères de sortie binaires, artefacts, conditions d'arrêt et prompt de démarrage.
