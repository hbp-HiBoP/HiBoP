# QUEST-011 — Exposer appairage, envoi et progression dans l'application

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-008](QUEST-008.md), [QUEST-010](QUEST-010.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-011).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../04-prototype-specification.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-011 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Assets/Scripts/HBP/UI ; prefabs UI Desktop ; QuestBootstrap et contrôleur de session

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Ajouter le parcours appairage puis « Envoyer au Quest » pour la colonne sélectionnée.
- Ajouter un panneau Quest de connexion/progression/erreur et nouvelle tentative via prefabs ; afficher la progression du calcul comme étape distincte quand présente.
- Retirer le besoin de harness pour le parcours utilisateur tout en conservant les tests ; ne pas charger une fixture APK à la place du réseau.

## Hors périmètre

Pas de découverte automatique, de multicasque ou d'édition scientifique.

## Décisions et questions à traiter

Choisir un emplacement UI cohérent avec le Desktop existant et le montrer ; demander seulement si plusieurs parcours changent réellement l'usage.

## Vérifications à réaliser par l'agent

- Bouton indisponible si sélection invalide, double clic borné et erreurs lisibles.
- Exécuter le parcours avec deux Players sans outil de probe ; vérifier indépendance des vues et absence de blocage du thread principal.

## Validation manuelle du propriétaire

1. Appairer, sélectionner la fixture, envoyer, observer progression puis manipuler sur Quest.
2. Provoquer une erreur d'adresse ou de connexion, corriger puis réessayer : vérifier que l'état est compréhensible.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Expliquer le parcours avant/après, joindre les prefabs et captures utiles, nommer les erreurs prises en charge.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-011.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Le parcours complet est accessible depuis HiBoP et le casque, sans commande diagnostique.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
