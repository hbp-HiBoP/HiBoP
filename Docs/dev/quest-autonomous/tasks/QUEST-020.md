# QUEST-020 — Capturer un instant iEEG et ses paramètres préparés

Jalon : [J5](../milestones/J5.md). Type : implémentation ciblée.
Dépendances : [QUEST-019](QUEST-019.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-020).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../04-prototype-specification.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-020 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Column3DIEEG.SetActivityData ; Column3DDynamic.CurrentProjectionSample ; paramètres Middle/SpanMin/SpanMax

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Construire la fixture iEEG synthétique et capturer un instant choisi sur Desktop avec amplitudes, unités, masques et provenance.
- Transférer les plages de normalisation déjà préparées ; ne pas les recalculer sur le seul instant.
- Définir explicitement les sémantiques surface/site ; la fixture initiale peut utiliser Alpha=0 mais le comportement des sélections non alignées doit être documenté.

## Hors périmètre

Pas de navigation temporelle Quest, import EEG local ou arrondi silencieux de l'instant.

## Décisions et questions à traiter

Si l'instant demandé n'est pas aligné, présenter soit un traitement préservant les sémantiques existantes, soit un refus explicite du prototype ; demander avant d'en réduire l'usage.

## Vérifications à réaliser par l'agent

- Round-trip des valeurs/paramètres ; cas négatif/nul/positif, canal absent/masqué.
- Vérifier la correspondance entre indice choisi et données reçues, avec absence de renormalisation.

## Validation manuelle du propriétaire

1. Choisir l'instant de la recette Desktop ; vérifier que son identité et ses unités figurent dans le résumé de transfert.
2. Aucun nouveau rendu iEEG Quest attendu avant QUEST-023.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Présenter un petit exemple numérique montrant pourquoi Middle/Span ne viennent pas de cet instant seul.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-020.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Entrées d'un instant iEEG transmises avec provenance et sémantique explicites.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
