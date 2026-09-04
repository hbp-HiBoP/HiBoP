# HiBoP XR — roadmap d'implémentation

**Version :** 0.5
**Principe :** chaque tranche conserve un HiBoP Desktop fonctionnel et produit un gate vérifiable.

Les phases sont détaillées en paquets autonomes dans [implementation-packets/README.md](implementation-packets/README.md). Chaque paquet possède un prompt de démarrage, un périmètre et un `Decision gate` obligatoire.

## Décisions à fermer dans les paquets

La roadmap peut démarrer, mais les points suivants ne doivent pas être inférés pendant l'implémentation :

- P00 : datasets autorisés et propriétaire scientifique ;
- P01 : **fermé et implémenté** — Desktop à la racine, XR sous `XR/`, trois packages sous `Shared/Packages/`, Unity `6000.5.2f1` ;
- P02 : représentation des IDs et catalogue des scopes ;
- P03 : interpolation temporelle de surface, repères et tolérances ;
- P04 : matrice exacte des packages/paramètres Quest ;
- P05 : **candidate implémentée, gate ouvert** — P05-A–C validées ; transparence passthrough P05-D à fermer avant P13/E11 ;
- P06 : bibliothèque transport et codec, décidés par spike ;
- P08 : **fermé et implémenté** — budget injecté, LRU des seuls inactifs, échec explicite sous pression et lifecycle mémoire sans persistance ;
- P09 : bindings exacts des instances ;
- P10 : sémantique de picking et backend mesuré ;
- P11 : **fermé, implémenté et validé sur Quest** — preload lossless sous budget byte-explicite, 97 indices comme profil et non plafond ;
- P12 : politiques d'échec/atomicité et ownership des coupes ;
- P13 : inventaire fonctionnel et interactions V1 signés ;
- P14 : matrice exacte de purge et propriétaire sécurité ;
- P15 : parcours d'activation, contenu envoyé, lifecycle du sidecar et scénario vertical signé ;
- P16 : namespaces, assemblies, package IDs, APIs et stratégie de migration ;
- P17 : classification production/test/dev/preuve, suppressions et dette résiduelle ;
- P18 : organisation, canal, signature et go/no-go Meta.

Le chat d'une phase peut produire une fiche de décision ou un ADR, mais il ne commence pas le code de production concerné tant que la décision n'est pas explicitement acceptée.

## Règles

- changements chirurgicaux, adaptateurs temporaires et rollback explicite ;
- aucune copie de code/assets entre projets ;
- contrats avant réseau/renderer complet ;
- calculs scientifiques comparés à une baseline ;
- mesures avant quantification, compression ou port natif ;
- aucun plafond fonctionnel pour faire passer un test ;
- aucune donnée patient dans tests publics, logs ou artefacts ;
- migration Input Desktop séparée des preuves XR P0.

## Phase 0 — baselines et hygiène

### XR-000 — figer les baselines

- enregistrer commits Desktop, `hbp_core` et prototype ;
- conserver les datasets D0–D4 et images/buffers attendus ;
- publier D01–D24 et propriétaires.

**Gate :** registre, datasets et hygiène sécurité validés.

## Phase 1 — monorepo et contrats purs

### XR-010 — créer le second projet Unity XR minimal

- projet séparé sous `XR/`, Unity `6000.5.2f1`, Android ARM64/IL2CPP ;
- topologie et Player Android structurels, sans OpenXR, XRI, Meta, scène produit ou fonctionnalité ;
- aucune référence vers Core/Data.

### XR-011 — créer les packages embarqués

- compléter les squelettes `com.crnl.hibop.contracts`, `com.crnl.hibop.render-model` et `com.crnl.hibop.protocol` créés par P01 ;
- tests C# purs et IL2CPP ;
- IDs, scopes, révisions et unités.

### XR-012 — ajouter les adaptateurs Desktop sans changer le renderer

- code exclusivement sous `Assets/`, sans méthode DTO ajoutée aux modèles HiBoP ;
- IDs stables ;
- inventaire des propriétés/scopes/invalidations ;
- capture des sorties actuelles.

**Gate :** les deux projets consomment les mêmes fichiers de package ; APK minimal et build Desktop Windows reproductible. La portabilité des packages reste obligatoire, mais l'exécution native macOS/Linux est qualifiée après E11 selon D24.

## Phase 2 — parité RenderModel

### XR-020 — formaliser les assets/résultats

- SurfaceAsset, SiteAsset/SiteRenderFrame ;
- DynamicFrameBundle ;
- CutRenderResult ;
- matériaux/palettes.

### XR-021 — renderer indépendant

- scène test sans Core/Data ;
- reconstruction anatomical/inflated, sites, activité et coupe ;
- golden buffers/images.

### XR-022 — fermer l'interpolation temporelle

- test `TemporalSample.Alpha != 0` ;
- résultat attendu explicite surface/sites ;
- correction Desktop si le comportement courant est un défaut.

**Gate :** S05 PASS et aucune dépendance Desktop transitive dans le client. La parité de buffers peut autoriser P06–P12, mais P05-D doit être fermée avant l'assemblage UX P13 et E11.

## Phase 3 — transport et état

### XR-030 — spike D10/D11

- sélectionner une baseline provisoire après build/runtime Windows + Quest IL2CPP, en conservant une architecture portable ; qualifier macOS/Linux nativement après E11 selon D24 ;
- TLS/pinning, WSS, HTTPS ranges ;
- schéma binaire et golden vectors.

### XR-031 — machine de session

- handshake/capabilities ;
- appairage + IP manuelle ;
- snapshot transactionnel initial et de reconnexion ;
- full resync comme baseline ; journal/deltas seulement comme capability optionnelle ;
- backpressure et diagnostics.

**Gate :** S01 et tests D12/D18 PASS, gros asset sans blocage du contrôle.

## Phase 4 — anatomie et multi-cerveaux

### XR-040 — assets par hash

- transfert/caching mémoire ;
- surface MNI partagée ;
- brain instances locales ;
- anatomical/inflated et transparence.

### XR-041 — interactions de base

- XRI ray/grab/rotate/two-hand scale ;
- recentrage et inspection rapprochée sans limite métier étroite ;
- mains et contrôleurs ;
- adaptateur passthrough Meta.

**Gate :** une multi-patient et deux mono-patient simultanées, topologie dédupliquée, déplacement de tête autour d'un cerveau fortement agrandi à 72 Hz, S06/S08 documentés.

## Phase 5 — sites

### XR-050 — backend bufferisé

- instancing/GraphicsBuffer ;
- dirty ranges ;
- palette/size/visibility ;
- aucun objet par site.

### XR-051 — picking

- grille/BVH local brain space ;
- ray/proximité ;
- ID stable et feedback pending/canonical ;
- panel du site sélectionné avec graphes, tags, matrices et métadonnées sur allowlist transitoire.

**Gate :** S04 PASS sur 37 500 sites sans plafond.

## Phase 6 — timeline

### XR-060 — bundle canonique

- estimation et admission en octets CPU/GPU uniques, sans plafond de cardinalité ;
- preload lossless de tous les indices acceptés, avec progression/annulation via `LoadingManager` ;
- autoplay toutes colonnes avec signalement des indices sautés ;
- scrub et sélection locale arbitraire visibles en une frame ;
- surface/sites/overlays atomiques ;
- float32 baseline.

### XR-061 — optimisation mesurée

- représentation compacte automatique seulement après équivalence visuelle validée, proposition explicite sinon ;
- pooling et uploads groupés ;
- budget configurable dans une borne Quest validée ; refus précoce détaillé, sans paging ni réduction silencieuse.

**Gate :** S02 PASS, index accepté visible à la frame suivante, aucun backlog/état mixte et refus explicite au-delà du budget.

## Phase 7 — coupes et ROI

### XR-070 — pipeline remote

- gizmo local ;
- interaction IDs/séquences ;
- géométrie/base/overlays dédupliqués ;
- annulation/coalescence/rejet stale.

### XR-071 — ROI

- sphères, paramètres et résultat canonique ;
- manipulation XRI avec contrôleurs précis.

### XR-072 — contingency native

Exécuter S07 uniquement si S03 échoue après optimisation. Ne porter que la fonction justifiée.

**Gate :** S03 PASS ou décision D05 révisée avec preuves Quest.

## Phase 8 — fonctions V1 et UX

Dans une exécution séquentielle, fermer d'abord le gate de transparence P05-D et P14, puis assembler P13. P12 peut avancer avant P05-D. P13 et P14 peuvent avancer en parallèle uniquement si P05-D et les états, métadonnées et transitions de cycle de vie P14 sont intégrés avant la sortie de P13.

### XR-080 — sécurité et cycle de vie P14

- classification des payloads et allowlist des métadonnées humaines transitoires ;
- matrice disconnect/background/timeout/logout/close/crash/reboot ;
- stockage sécurisé limité à l'appairage, purge mémoire et logs redacted ;
- tests sentinelles D6 et acceptation du propriétaire sécurité.

### XR-081 — interactions et UI P13

- paramètres de colonne avec scopes ;
- sélection/blacklist/highlight ;
- UI XR des commandes d'inspection courantes ;
- graphes, tags, matrices et panels du site sélectionné traités en très haute priorité ;
- erreurs et états pending/canonical/stale ;
- perte tracking, background, reconnexion ;
- passthrough/VR et accessibilité.

**Gate :** P05-D et P14 acceptés ; scénario complet avec contrôleurs et interactions principales avec mains. Le raccordement interprocess à une vraie visualisation HiBoP reste le gate E11 de P15.

## Phase 9 — migration Input Desktop

Workstream indépendant :

1. inventorier tous les `Input.*`, raccourcis, caméra et tests ;
2. action maps Desktop ;
3. migration par domaine avec parité ;
4. build/parité Windows pendant le prototype ; qualification macOS/Linux après E11 selon D24 ;
5. legacy Input Manager désactivable puis supprimé.

Il peut démarrer tôt sur Windows, mais ne bloque que l'intégration finale des commandes communes — pas le projet XR minimal. Ses validations natives macOS/Linux suivent le gate E11 et l'ordre D24.

## Phase 10 — intégration produit end-to-end

- point d'entrée Desktop/Quest décidé, sans déduire l'UX d'un exemple ;
- host et sidecar démarrés, supervisés et arrêtés par le chemin produit ;
- transport HTTPS/WSS P06 raccordé au cœur de session P07 ;
- snapshot réel construit depuis une visualisation HiBoP ;
- surfaces, sites, timeline et coupes retenus traversent le réseau réel ;
- au moins une commande Quest est validée par le Desktop puis republiée au Quest ;
- déconnexion, snapshot de reconnexion, erreur et fermeture suivent P14.

**Gate :** E11 PASS sur un Quest physique, sans scène, host ou injection synthétique dans le chemin produit.

Après E11, une revue explicite confirme ou rouvre la baseline de rendu local Quest à partir de l'expérience réelle. Aucun second moteur de rendu distant n'est construit avant ce gate.

## Phase 11 — architecture de production et normalisation

- cartographier le chemin réellement exercé par P15 ;
- décider séparément namespaces C#, assemblies et identifiants UPM ;
- normaliser classes, fichiers, APIs, scènes et prefabs de production ;
- migrer les références Unity en préservant GUID et sérialisation ;
- conserver les noms historiques des ADR et preuves ;
- rejouer P15 avant/après sans changement fonctionnel.

**Gate :** mapping accepté, aucune référence cassée et parité P15 démontrée.

## Phase 12 — cleanup et durcissement

- classifier chaque spike, demo, probe, fixture, outil et dépendance ;
- isoler Editor/Test/Development de ce qui peut rester utile ;
- retirer des Players les données et composants non produit ;
- supprimer par lots seulement les éléments classés obsolètes ;
- consolider composition roots et scripts de build/test/lancement ;
- mesurer taille, dépendances et dette résiduelle.

**Gate :** Players sans échafaudage non autorisé, preuves conservées au niveau décidé et E11 toujours PASS sur Quest.

## Phase 13 — industrialisation

- validation Windows x64 en premier, puis macOS Apple Silicon avec MacBook Air M2 et Ubuntu 24.04 x64 ;
- CI packages, Desktop 3 OS, APK et tests protocole ;
- mesurer séparément le pont XR du build standard et le module host/assets optionnel ; intégrer davantage seulement si le coût est non drastique ;
- proposition d'installation discrète si un canal fiable existe, entrée XR masquée sinon ; mise à jour automatique du module déjà installé ;
- déclenchements limités à `workflow_dispatch` ou à `release: published` pour une release créée manuellement ; aucun déclenchement sur `push`, `pull_request` ou planification ;
- signature et gestion des secrets ;
- SBOM/licences tierces ;
- diagnostics réseau utilisateur ;
- redaction/purge auditée ;
- parcours P15 rejoué sur les artefacts release-like issus de P17 ;
- tests thermique/mémoire 30 min ;
- canal Meta pilote et procédure update/rollback.

**Gate :** S09/S10 et E11 PASS, D20 mesuré, revue sécurité/licences et installation par un pilote non développeur.

## Définition de terminé

Une tâche est terminée lorsque code et contrats compilent sur leurs plateformes, tests ciblés passent, métriques sont archivées, données sensibles absentes, docs/decision register mis à jour, rollback connu et comportement Desktop non prévu inchangé.

## Paquets dispatchables

P00–P18 et les voies parallèles PX1/PX2 sont indexés dans [le dossier des paquets](implementation-packets/README.md). Un chat ne doit recevoir qu'un paquet principal ; tout besoin hors scope devient une dépendance ou un nouveau paquet, pas une extension implicite.
