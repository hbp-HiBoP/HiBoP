# QUEST-026 — Qualifier Mac vers Quest

Jalon : [J6](../milestones/J6.md). Type : intégration / qualification.
Dépendances : [QUEST-025](QUEST-025.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-026).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-026 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Player Mac et recette Windows QUEST-024

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Rejouer les quatre niveaux depuis le Mac réel vers Quest 3.
- Comparer transfert, provenance et résultats scientifiques au Windows de référence.
- Corriger uniquement les défauts de frontière Mac nécessaires et consigner limites/acceptation.

## Hors périmètre

Pas de nouveau protocole spécifique Mac ou affirmation de support de toutes les versions macOS.

## Décisions et questions à traiter

Si l'environnement empêche le test, préciser exactement ce qui manque sans marquer Mac qualifié.

## Vérifications à réaliser par l'agent

- Mesurer transfert/calcul, vérifier parité et reconnexion ; même APK si protocole inchangé.
- Vérifier les conséquences des permissions réseau OS et l'arrêt du transport.

## Validation manuelle du propriétaire

1. Appairer/envoyer depuis Mac, tester gestes et bouton masquer, puis coupure 60 s.
2. Comparer l'expérience à Windows et consigner tout écart visible.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Rapporter les différences Windows/Mac utiles et leur cause ; aucune répétition de toute l'architecture.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-026.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

J6 validé sur Mac identifié, avec résultats réels et accord manuel enregistré.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
