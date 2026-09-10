# SCENE-007 — Achever l’intégration et retirer le prototype remplacé

Édition du 10 septembre 2026. Lot B.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Terminer le code du parcours complet avant la phase de stabilisation, et supprimer
les doublons rendus inutiles par la migration.

## Travail

- Parcourir Desktop → préparation → capture → transport → restauration → rendu
  commun Quest dans les sources et terminer les raccordements manquants.
- Reprendre la matrice 001 : toutes les modalités et fonctionnalités du périmètre
  ont leur opération, leurs données et leur rendu ; absence d’UI Quest explicite.
- Retirer les anciens chemins scientifiques Quest, états en double, façades et
  propriétaires remplacés. Conserver le transport et les adaptations légitimes.
- Mettre à jour les références de prefabs, assemblies, diagnostics et tests
  réellement affectées ; ne pas supprimer un test scientifique pour cacher une régression.
- Vérifier par lecture les appels à la présentation Desktop depuis les opérations
  communes et corriger les dépendances restantes.
- Préparer les fixtures et scénarios manquants de la campagne finale, sans
  multiplier les outils de test ni remettre une campagne par fonctionnalité.
- Mettre à jour le journal avec les défauts/risques à investiguer en 008.
  Corriger immédiatement les erreurs évidentes, sans chercher à qualifier ici
  chaque branche de code.

## Passage à la suite

L’implémentation de bout en bout est prête pour la stabilisation, les chemins
remplacés sont retirés et les incertitudes restantes sont explicites. Ce jalon
ne signifie pas que l’application est déjà compilée ou validée. Passer à 008
sans exiger un rapport ou un manifeste par fiche.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
