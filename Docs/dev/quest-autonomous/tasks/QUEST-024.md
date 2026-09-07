# QUEST-024 — Qualifier le prototype Windows complet

Jalon : [J5](../milestones/J5.md). Type : intégration / qualification.
Dépendances : [QUEST-023](QUEST-023.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-024).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-024 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Rapports J1 à J5 et builds courants

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Assembler une recette unique anatomie/sites/densité/iEEG avec binaires et fixtures identifiés.
- Exécuter la campagne utile de bout en bout, mesures froid/chaud, déconnexion et libération.
- Corriger seulement les défauts bloquant cette preuve ; consigner l'acceptation avant qualification Mac.

## Hors périmètre

Pas de feature future ou optimisation finale.

## Décisions et questions à traiter

Si une condition de réussite n'est pas satisfaite, proposer correction/report explicite ; ne pas déclarer J5 validé par simple compilation.

## Vérifications à réaliser par l'agent

- Vérifier même source applicative/native et contenu des Players.
- Rejouer les tests pertinents après corrections ; distinguer régression, nouvelle preuve et test non exécuté.
- Produire tableau des résultats, ressources, seuils convenus et limites.

## Validation manuelle du propriétaire

1. Suivre la recette des quatre niveaux et donner un verdict sur lisibilité, gestes et attente.
2. Effectuer la coupure de 60 s et vérifier indépendance des vues.
3. Confirmer ou refuser les points manuels explicitement numérotés dans le rapport.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Rapport lisible en premier, liens vers les diffs par comportement ensuite ; séparer acceptation scientifique et confort.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-024.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Prototype Windows qualifié ou liste précise des écarts restant à lever avant Mac.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
