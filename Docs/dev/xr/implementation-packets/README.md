# HiBoP XR — paquets d'implémentation

**Version :** 0.2  
**Usage :** un paquet par chat Codex, avec état de départ contrôlé et résultat intégrable indépendamment.

## Règle absolue de décision

Avant toute modification de code, scène, prefab, package, settings, CI ou format persistant, le chat doit exécuter le `Decision gate` du paquet.

Une décision est considérée explicite seulement si elle est :

1. héritée d'une entrée `RESOLVED` du registre D01–D20 ;
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

1. Lire `AGENTS.md`, le paquet, D01–D20 et ses documents normatifs.
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
P13 + P14 ──────────────> P15 Industrialisation
P12 FAIL distant ───────> décision explicite ──> PX2 hbp_core Quest
```

## Index

| Paquet | Livrable principal | Décision préalable notable |
| --- | --- | --- |
| [P00](P00-baselines.md) | fixtures et baselines | datasets autorisés |
| [P01](P01-topology.md) | **COMPLETE** — monorepo + projet XR | ADR P01 accepté |
| [P02](P02-contracts.md) | package Contracts | représentation des IDs |
| [P03](P03-render-model.md) | RenderModel fidèle | sémantique d'interpolation |
| [P04](P04-xr-bootstrap.md) | APK OpenXR minimal | matrice packages |
| [P05](P05-static-renderer.md) | surface statique Quest | baseline shader |
| [P06](P06-transport-spike.md) | **ACCEPTED PROVISIONAL** — transport/codec Windows+Quest | macOS/Linux différés |
| [P07](P07-distributed-session.md) | handshake/snapshot/resume | résultats P06 |
| [P08](P08-remote-assets.md) | assets par hash/cache mémoire | politique de pression |
| [P09](P09-multi-brain.md) | instances dédupliquées | bindings V1 |
| [P10](P10-sites.md) | 37 500 sites/picking | backend issu des mesures |
| [P11](P11-timeline.md) | bundles atomiques | interpolation P03 |
| [P12](P12-cuts.md) | coupes canoniques distantes | seuils D20 |
| [P13](P13-v1-interactions.md) | UX V1 complète | inventaire fonctionnel signé |
| [P14](P14-security.md) | sécurité/purge/redaction | matrice de cycle de vie |
| [P15](P15-industrialization.md) | CI/distribution pilote | organisation/canal Meta |
| [PX1](PX1-desktop-input-system.md) | migration Input Desktop | carte de parité |
| [PX2](PX2-hbp-core-quest.md) | backend natif ciblé | autorisation après échec P12 |

## Format obligatoire de chaque paquet

Chaque fichier contient : objectif/résultat observable, Decision gate, périmètre/hors périmètre, hypothèses fixées, dépendances/état initial, fichiers pressentis, étapes, tests/commandes, critères de sortie binaires, artefacts, conditions d'arrêt et prompt de démarrage.
