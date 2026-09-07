# QUEST-028 — Préparer le Player Linux retenu

Jalon : [J7](../milestones/J7.md). Type : implémentation ciblée.
Dépendances : [QUEST-027](QUEST-027.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-028).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-028 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

HBPBuilder ; plugins Linux/x86_64 ; workflow Desktop

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Après décision de tester, adapter profil/packaging et chemins pour la distribution retenue.
- Construire/inspecter puis lancer le Player et le transport sur cette machine.
- Fournir la recette d'ouverture de fixture et les dépendances réellement nécessaires.

## Hors périmètre

Pas de support général Linux ou de changement scientifique.

## Décisions et questions à traiter

Exiger la décision tester de QUEST-027 ; une clôture différer ne satisfait pas cette condition.

## Vérifications à réaliser par l'agent

- Contrôler architecture, dépendances natives et provenance ; ouvrir la fixture.
- Consigner erreurs de transport/permissions et corrections ciblées.

## Validation manuelle du propriétaire

1. Lancer la fixture sur Linux selon les instructions concrètes du rapport.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la frontière adaptée et les dépendances effectives de la distribution.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-028.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Player Linux utilisable sur la cible choisie pour l'essai Quest.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
