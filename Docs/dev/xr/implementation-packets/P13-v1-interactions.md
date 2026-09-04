# P13 — interactions et UX fonctionnelle V1

## Objectif et résultat observable

Assembler les fonctions validées en un parcours V1 cohérent avec mains et contrôleurs : connexion, instances, représentation, sites, coupes, timeline, ROI et panels explicitement retenus.

## Decision gate

**Hérité :** product spec, D15 XRI/OpenXR baseline, D21 rendu local, D22 autorité/UX, D23 ressources, P09–P12 comportements techniques.

**Décision produit obligatoire avant tout assemblage :**

- `P13-A` : inventaire signé des fonctions V1 restantes, sachant que graphes/tags/matrices et panels du site sélectionné sont déjà fixés comme très haute priorité ;
- `P13-B` : mapping actions mains et contrôleurs par tâche ;
- `P13-C` : hiérarchie des menus, états pending/canonical/stale/error et terminologie ;
- `P13-D` : règles de recentrage, tailles, placement initial et récupération d'objet ;
- `P13-E` : exigences accessibilité/assis-debout et critères utilisateur ;
- `P13-F` : décision finale XRI seul ou complément Meta issue des mesures P04/P13.

Cette phase est `DECISION_ONLY` tant que P13-A–F ne sont pas acceptées. « Reprendre l'UI Desktop » ou « comme HoloLens » n'est pas une décision.

## Périmètre autorisé

- orchestration UX des fonctions déjà validées ;
- prefabs/UI XR, actions XRI, feedback et accessibilité ;
- graphes/tags/matrices et panels du site sélectionné ; ROI et autres panels seulement s'ils figurent dans P13-A ;
- tests utilisateurs structurés.

## Hors périmètre

- nouvelle fonction scientifique ;
- refactor transport/renderer non requis ;
- point d'entrée Desktop, supervision du sidecar et raccordement interprocess de production, réservés à P15 ;
- fonctions absentes de P13-A ;
- dépendance Meta directe hors adaptateur.

## Hypothèses fixées

- contrôleurs couvrent le scénario V1 complet ;
- mains couvrent les interactions principales ;
- passthrough défaut/VR repli ;
- transformations locales ;
- tracking, passthrough et transformations locales continuent pendant une coupure ; l'état scientifique gèle avec feedback explicite ;
- chaque état scientifique confirmé par Desktop.

## Dépendances et état initial

- P09 multi-cerveaux ;
- P10 sites ;
- P11 timeline ;
- P12 coupes ;
- P14 peut avancer en parallèle mais ses contraintes doivent être intégrées avant sortie.

## Fichiers/modules pressentis

- XR scenes/prefabs/action maps/UI ;
- adaptateurs OpenXR/Meta ;
- orchestration Client/BrainInstance ;
- tests PlayMode/device et guide UX.

## Étapes

1. Produire inventaire/tâches et obtenir P13-A–F.
2. Créer prefabs et action maps, sans construction runtime compensatoire.
3. Implémenter navigation, menus et feedback d'état.
4. Assembler instances/sites/coupes/timeline.
5. Ajouter en priorité les informations du site sélectionné, puis ROI/autres panels retenus, un par scénario/test.
6. Valider mains puis contrôleurs, assis/debout.
7. Tester erreurs, déconnexion, perte tracking et recentrage.
8. Conduire sessions utilisateur et corriger dans le scope signé.

## Tests et commandes

- tests action maps et UI ;
- PlayMode/device pour chaque tâche P13-A ;
- mains/contrôleurs, assis/debout, inspection rapprochée de petite à très grande échelle ;
- perte tracking/réseau et états pending/error ;
- frame timing et fatigue/temps/erreurs utilisateur ;
- scan prefab-first et dépendances Meta.

## Critères de sortie binaires

- [ ] P13-A–F signées ;
- [ ] chaque fonction V1 possède scénario automatisé ou protocole device ;
- [ ] tâches critiques réussies avec contrôleurs ;
- [ ] tâches nominales mains réussies selon P13-E ;
- [ ] graphes, tags, matrices et métadonnées autorisées du site sélectionné sont consultables dans le casque ;
- [ ] pending/canonical/stale/error distinguables ;
- [ ] aucun élément hors inventaire implémenté implicitement ;
- [ ] budgets D20 revalidés end-to-end.

## Artefacts à remettre

Inventaire signé, prefabs/action maps/UI, tests/scénarios, rapport utilisateur et ADR P13.

Le parcours interprocess avec une vraie visualisation HiBoP, le transport P06 et le sidecar de production est fermé par P15 ; les harnais utilisés ici doivent rester remplaçables et ne peuvent pas être présentés comme cette preuve end-to-end.

## Conditions d'arrêt

Arrêter si P13-A–F manquent, si une fonction exige un nouveau contrat scientifique, si le SDK Meta est nécessaire sans décision ou si des critères utilisateur doivent être inventés.

## Prompt de démarrage

> Exécute P13 depuis `Docs/dev/xr/implementation-packets/P13-v1-interactions.md`. Commence exclusivement par faire accepter P13-A–F ; aucune UX de production avant cette signature. Assemble ensuite seulement les fonctions retenues, prefab-first, avec mains et contrôleurs, feedback d'état et tests utilisateur mesurés.
