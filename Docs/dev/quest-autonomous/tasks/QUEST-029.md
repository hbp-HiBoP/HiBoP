# QUEST-029 — Qualifier Linux vers Quest

Jalon : [J7](../milestones/J7.md). Type : intégration / qualification.
Dépendances : [QUEST-028](QUEST-028.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-029).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-029 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Player Linux et scénario QUEST-024

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Rejouer les quatre niveaux et la coupure réseau sur Linux réel avec Quest.
- Comparer résultats au socle qualifié et consigner le périmètre exact supporté.
- Fermer le jalon ou enregistrer un report/écart explicite.

## Hors périmètre

Pas d'élargissement fonctionnel ni de support d'autres distributions par inférence.

## Décisions et questions à traiter

Une impossibilité runtime doit être rapportée, pas masquée par la réussite du build.

## Vérifications à réaliser par l'agent

- Vérifier transferts, intégrité, calcul local, indépendance des vues et parité.
- Mesurer les différences de plateforme et archiver les preuves.

## Validation manuelle du propriétaire

1. Effectuer le parcours des quatre niveaux et déconnexion/reconnexion ; donner le verdict utilisateur.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Comparer aux rapports Windows/Mac et limiter les conclusions à l'environnement testé.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-029.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Qualification Linux explicite ou défauts/report documentés, jamais support implicite.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
