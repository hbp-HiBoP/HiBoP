# QUEST-002 — Unifier les packages et préserver les deux systèmes d'entrée

Jalon : [J1](../milestones/J1.md). Type : implémentation ciblée.
Dépendances : [QUEST-001](QUEST-001.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-002).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-002 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Packages/manifest.json ; Packages/packages-lock.json ; ProjectSettings/ProjectSettings.asset ; ancien XR/Packages via Git

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Ajouter l'ensemble minimal de packages XR requis au manifeste racine, en conservant les dépendances Desktop et un lock unique.
- Résoudre l'entrée historique Desktop et l'entrée XRI Quest via les réglages effectivement disponibles dans cette version Unity ; consigner les réglages globaux éventuels.
- Ajouter seulement les frontières de compilation nécessaires et une vérification ciblée de configuration ; ne pas migrer les contrôles Desktop.

## Hors périmètre

Pas de rig complet, de transfert ou de refonte des asmdefs historiques.

## Décisions et questions à traiter

Si un réglage global impose une migration Desktop ou une dépendance incompatible, présenter l'obstacle et deux solutions concrètes avant d'élargir le périmètre.

## Vérifications à réaliser par l'agent

- Importer/compiler la configuration Desktop puis Android et relever les versions réellement résolues.
- Vérifier que la sélection de cible ne réécrit pas le manifeste et que l'entrée souris/clavier Desktop reste fonctionnelle.

## Validation manuelle du propriétaire

1. Dans Desktop, ouvrir la fixture, tourner et zoomer avec les commandes habituelles ; vérifier qu'aucune commande n'a changé.
2. Aucune validation de gestes Quest à ce stade.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Expliquer chaque dépendance ajoutée et la différence entre installer un package XR et activer son runtime ; montrer les réglages d'entrée effectifs.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-002.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Les deux configurations compilent avec un manifeste unique et l'entrée Desktop préservée.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
