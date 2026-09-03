# HiBoP XR — roadmap d'implémentation

**Version :** 0.3
**Principe :** chaque tranche conserve un HiBoP Desktop fonctionnel et produit un gate vérifiable.

Les phases sont détaillées en paquets autonomes dans [implementation-packets/README.md](implementation-packets/README.md). Chaque paquet possède un prompt de démarrage, un périmètre et un `Decision gate` obligatoire.

## Décisions à fermer dans les paquets

La roadmap peut démarrer, mais les points suivants ne doivent pas être inférés pendant l'implémentation :

- P00 : datasets autorisés et propriétaire scientifique ;
- P01 : **fermé et implémenté** — Desktop à la racine, XR sous `XR/`, trois packages sous `Shared/Packages/`, Unity `6000.5.2f1` ;
- P02 : représentation des IDs et catalogue des scopes ;
- P03 : interpolation temporelle de surface, repères et tolérances ;
- P04 : matrice exacte des packages/paramètres Quest ;
- P06 : bibliothèque transport et codec, décidés par spike ;
- P08 : **fermé et implémenté** — budget injecté, LRU des seuls inactifs, échec explicite sous pression et lifecycle mémoire sans persistance ;
- P09 : bindings exacts des instances ;
- P10 : sémantique de picking et backend mesuré ;
- P11/P12 : politiques d'échec/atomicité et ownership ;
- P13 : inventaire fonctionnel et interactions V1 signés ;
- P14 : matrice exacte de purge et propriétaire sécurité ;
- P15 : organisation, canal, signature et go/no-go Meta.

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
- publier D01–D20 et propriétaires.

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

**Gate :** les deux projets consomment les mêmes fichiers de package ; APK minimal et trois builds Desktop reproductibles.

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

**Gate :** S05 PASS et aucune dépendance Desktop transitive dans le client.

## Phase 3 — transport et état

### XR-030 — spike D10/D11

- sélectionner bibliothèque après build 3 OS + Quest IL2CPP ;
- TLS/pinning, WSS, HTTPS ranges ;
- schéma binaire et golden vectors.

### XR-031 — machine de session

- handshake/capabilities ;
- appairage + IP manuelle ;
- snapshot transactionnel et deltas ;
- journal borné, resume et full resync ;
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
- recentrage, 10 cm–2 m ;
- mains et contrôleurs ;
- adaptateur passthrough Meta.

**Gate :** une multi-patient et deux mono-patient simultanées, topologie dédupliquée, S06/S08 documentés.

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
- panel d'information redacted.

**Gate :** S04 PASS sur 37 500 sites sans plafond.

## Phase 6 — timeline

### XR-060 — bundle canonique

- autoplay toutes colonnes ;
- scrub coalescé ;
- surface/sites/overlays atomiques ;
- float32 baseline.

### XR-061 — optimisation mesurée

- encodage/compression seulement après parité ;
- pooling et uploads groupés ;
- fréquence scientifique adaptative sans retirer de colonnes.

**Gate :** S02 PASS, aucun backlog ou état de colonnes mixte.

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

- paramètres de colonne avec scopes ;
- sélection/blacklist/highlight ;
- panels et matrices retenus ;
- erreurs et états pending/canonical/stale ;
- perte tracking, background, reconnexion ;
- passthrough/VR et accessibilité.

**Gate :** scénarios end-to-end de `07-validation-plan.md` sur mains et contrôleurs.

## Phase 9 — migration Input Desktop

Workstream indépendant :

1. inventorier tous les `Input.*`, raccourcis, caméra et tests ;
2. action maps Desktop ;
3. migration par domaine avec parité ;
4. builds Windows/macOS/Linux ;
5. legacy Input Manager désactivable puis supprimé.

Il peut démarrer tôt, mais ne bloque que l'intégration finale des commandes communes — pas le projet XR minimal.

## Phase 10 — industrialisation

- CI packages, Desktop 3 OS, APK et tests protocole ;
- déclenchements limités à `workflow_dispatch` ou à `release: published` pour une release créée manuellement ; aucun déclenchement sur `push`, `pull_request` ou planification ;
- signature et gestion des secrets ;
- SBOM/licences tierces ;
- diagnostics réseau utilisateur ;
- redaction/purge auditée ;
- tests thermique/mémoire 30 min ;
- canal Meta pilote et procédure update/rollback.

**Gate :** S09/S10 PASS, D20 mesuré, revue sécurité/licences et installation par un pilote non développeur.

## Définition de terminé

Une tâche est terminée lorsque code et contrats compilent sur leurs plateformes, tests ciblés passent, métriques sont archivées, données sensibles absentes, docs/decision register mis à jour, rollback connu et comportement Desktop non prévu inchangé.

## Paquets dispatchables

P00–P15 et les voies parallèles PX1/PX2 sont indexés dans [le dossier des paquets](implementation-packets/README.md). Un chat ne doit recevoir qu'un paquet principal ; tout besoin hors scope devient une dépendance ou un nouveau paquet, pas une extension implicite.
