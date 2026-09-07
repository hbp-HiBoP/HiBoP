# QUEST-027 — Décider de la qualification Linux

Jalon : [J7](../milestones/J7.md). Type : décision produit.
Dépendances : [QUEST-026](QUEST-026.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-027).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../00-product-vision.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-027 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Rapport QUEST-026 ; build Linux Desktop existant

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Présenter l'état du socle et l'effort spécifique Linux observable.
- Demander si le propriétaire souhaite tester maintenant et sur quelle machine/distribution/architecture.
- Consigner tester ou différer, la cible et les contraintes ; activer les tâches Linux uniquement si retenues.

## Hors périmètre

Pas de lancement Linux, commande sur machine inconnue ou élargissement à des distributions non demandées.

## Décisions et questions à traiter

Cette décision produit est requise ; le silence n'est pas une autorisation de qualification Linux.

## Vérifications à réaliser par l'agent

- Vérifier l'existence des outils/artefacts Linux sans conclure à leur fonctionnement.
- Vérifier cohérence de la décision avec le registre et la roadmap.

## Validation manuelle du propriétaire

1. Répondre tester ou différer et identifier l'environnement si le test est retenu.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Rapport court : preuve Mac acquise, incertitudes Linux et choix à trancher.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-027.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Décision explicite enregistrée ; si report, J7 peut se terminer sans support Linux revendiqué.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
