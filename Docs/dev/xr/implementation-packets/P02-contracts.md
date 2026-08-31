# P02 — contrats purs, IDs, scopes et révisions

## Objectif et résultat observable

Produire un package C# pur et AOT-safe décrivant identifiants, scopes, révisions, commandes, outcomes, erreurs et snapshot logique, sans Unity, IO, UI, native ou transport.

## Decision gate

**Hérité :** D03 package Contracts, D04 sous-ensemble pur, D12 modèle de révisions, D17 confidentialité, D18 compatibilité séparée.

**À résoudre avant les types publics :**

- `P02-A` : représentation wire/mémoire des IDs et règles de génération/stabilité ;
- `P02-B` : catalogue initial des scopes et propriété de chaque état V1 ;
- `P02-C` : convention d'optionalité/versionnement des contrats ;
- `P02-D` : comportement exact d'un conflit `baseScopeRevision`.

L'inventaire peut être réalisé en lecture seule. Aucun type public ne doit être figé tant que P02-A–D ne sont pas enregistrées.

## Périmètre autorisé

- package `HBP.Visualization.Contracts` ;
- types immuables/purs, validations et tests ;
- adaptateurs de test, pas adaptateur Desktop production.

## Hors périmètre

- sérialiseur wire concret ;
- Unity structs dans l'API publique ;
- RenderModel, transport et host ;
- modification du comportement Desktop.

## Hypothèses fixées

- `sessionId/epoch`, `commandId`, `interactionId/sequence`, révision globale et par scope existent ;
- IDs opaques sur le réseau ;
- noms patient séparés des IDs et exclus des logs ;
- erreurs sont codées et retryability explicite.

## Dépendances et état initial

- P01 packages partagés opérationnels ;
- D01–D20 et product spec accessibles ;
- inventaire de l'état actuel réalisable depuis HiBoP.

## Fichiers/modules pressentis

- package Contracts et son assembly de tests ;
- documentation générée ou tableaux de scopes ;
- `08-decision-register.md`/ADR P02.

## Étapes

1. Inventorier propriétés, source de vérité, scope, persistance et invalidations.
2. Résoudre P02-A–D.
3. Implémenter value types, equality/hash et validations.
4. Implémenter commandes/outcomes et erreurs V1.
5. Implémenter structure logique du snapshot et des deltas sans codec.
6. Ajouter tests de limites, duplication, ordre et incompatibilité.
7. Vérifier compilation sans Unity et dans les deux projets.

## Tests et commandes

- tests .NET/C# purs si le package le permet ;
- tests EditMode des deux projets ;
- analyse des dépendances asmdef ;
- tests equality/hash, wrap/overflow, duplicate command et conflict ;
- compilation AOT/IL2CPP smoke dès que P04 le permet.

## Critères de sortie binaires

- [ ] P02-A–D enregistrées ;
- [ ] package ne dépend d'aucune assembly Unity/UI/IO/native ;
- [ ] catalogue des scopes V1 complet ou toute absence déclarée bloquante ;
- [ ] tests IDs/révisions/commandes/erreurs passent ;
- [ ] compilation dans Desktop et XR ;
- [ ] aucune donnée humaine dans ToString/log helpers.

## Artefacts à remettre

Package Contracts, tests, catalogue des scopes, ADR P02 et notes de compatibilité.

## Conditions d'arrêt

Arrêter si un scope fonctionnel reste ambigu, si l'ID dépend d'un index d'ordre non stable ou si une propriété Desktop doit changer de propriétaire sans décision produit.

## Prompt de démarrage

> Exécute P02 depuis `Docs/dev/xr/implementation-packets/P02-contracts.md`. Lis AGENTS.md, D03/D04/D12/D17/D18 et inspecte l'état HiBoP. Commence par le Decision gate P02-A–D ; aucun type public ne doit être implémenté avant leur résolution explicite. Reste dans un package C# pur et livre tests, catalogue de scopes et preuves de compilation.
