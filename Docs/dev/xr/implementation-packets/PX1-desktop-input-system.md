# PX1 — migration Input System du Desktop

## Objectif et résultat observable

Migrer HiBoP Desktop du legacy Input Manager au nouveau Input System avec parité souris/clavier/caméra/raccourcis sur Windows, macOS et Linux, sans dépendance XR dans le shell Desktop.

## Decision gate

**Hérité :** D16 migration Desktop séparée ; le projet XR utilise déjà Input System ; ordre de qualification D24.

**À résoudre avant migration :**

- `PX1-A` : inventaire signé de toutes les actions et comportements existants ;
- `PX1-B` : action maps, bindings par OS et politique de rebinding ;
- `PX1-C` : stratégie de transition `Both` ou migration par tranche et date de désactivation legacy ;
- `PX1-D` : parité/test owner pour caméra, UI et raccourcis ;
- `PX1-E` : compatibilité plugins/UI qui consomment encore les anciens inputs.

Sans carte de parité PX1-A–E, aucune substitution globale de `Input.*`.

## Périmètre autorisé

- package Input System Desktop ;
- action assets/adapters ;
- migration comportement par comportement ;
- tests 3 OS et suppression legacy finale décidée.

## Hors périmètre

- interactions XR ;
- changement des raccourcis/UX sans décision ;
- refactor unrelated ;
- migration big-bang.

## Hypothèses fixées

- parité avant amélioration ;
- workstream indépendant des P02–P12 ;
- anciennes et nouvelles voies comparables temporairement si PX1-C le permet.

## Dépendances et état initial

- baseline P00 ;
- inventaire `Input.*`, KeyCode, EventSystem, UI/plugins ;
- Windows accessible pour l'implémentation initiale ; macOS Apple Silicon/MacBook Air M2 et Ubuntu 24.04 accessibles après E11 pour la qualification D24.

## Fichiers/modules pressentis

- ProjectSettings/Packages Desktop ;
- nouveaux action maps/adapters ;
- caméra, toolbar, raccourcis, tests.

## Étapes

1. Résoudre PX1-A–E.
2. Ajouter/verrouiller Input System selon version Unity.
3. Créer action maps et couche d'adaptation.
4. Migrer par domaine avec A/B/parité.
5. Tester d'abord plugins/UI sur Windows, puis qualifier macOS Apple Silicon et Ubuntu 24.04 après E11.
6. Désactiver legacy seulement après gate explicite.
7. Retirer chemin ancien et documenter bindings.

## Tests et commandes

- tests unitaires/action callbacks ;
- PlayMode caméra/UI/raccourcis ;
- checklist manuelle Windows puis macOS/Linux selon D24 ;
- builds Windows x64, macOS Apple Silicon et Ubuntu 24.04 x64 dans cet ordre ;
- recherche finale `Input.*`/legacy settings ;
- formatter C# obligatoire.

## Critères de sortie binaires

- [ ] PX1-A–E signées ;
- [ ] parité fonctionnelle complète ;
- [ ] trois builds/tests passent ;
- [ ] aucun comportement/raccourci changé sans décision ;
- [ ] legacy désactivé/supprimé selon PX1-C ;
- [ ] aucun package XR ajouté au Desktop.

## Artefacts à remettre

Action maps/adapters, tests, matrice de parité, rapports 3 OS et ADR PX1.

## Conditions d'arrêt

Arrêter si une action existante n'a pas de propriétaire attendu, si un plugin tiers impose une stratégie non décidée ou avant désactivation legacy sans parité signée.

## Prompt de démarrage

> Exécute PX1 depuis `Docs/dev/xr/implementation-packets/PX1-desktop-input-system.md`. Commence par l'inventaire et la décision PX1-A–E. Migre ensuite par tranche avec parité stricte, sans changement UX ni package XR. Valide Windows pendant le prototype, puis macOS Apple Silicon et Ubuntu 24.04 après E11 conformément à D24 ; n'éteins jamais le legacy avant le gate explicite.
