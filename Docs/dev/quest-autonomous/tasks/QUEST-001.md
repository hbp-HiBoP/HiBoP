# QUEST-001 — Établir la référence de travail et la fixture anatomique

Jalon : [J0](../milestones/J0.md). Type : implémentation ciblée.
Dépendances : Aucune tâche préalable. La branche de feature existe déjà.
Statut et preuves : [registre](../TASK-STATUS.md#quest-001).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../02-existing-code-and-reuse-audit.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-001 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Assets/Scripts/HBP/Core/Object3D/MNIObjects.cs ; Assets/Data/Meshes ; Assets/Tests/Fixtures/Native/manifest.json ; branche feature/xr à eb26c323e

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Relever branche/SHA, worktrees et modifications ; utiliser feature/xr-autonomous déjà créée, sans recréer ni renommer la branche.
- Définir une recette reproductible pour ouvrir une visualisation anatomique MNI complète à une colonne sur Desktop ; réutiliser une fixture si elle convient, sinon ajouter seulement sa préparation déterministe.
- Consigner les fichiers sources, hashes, conventions et éventuelles reprises historiques ; créer un manifeste de fixture indépendant des chemins absolus de cette machine.

## Hors périmètre

Pas de packages XR, réseau, changement de caméra ni nettoyage des restes XR non suivis.

## Décisions et questions à traiter

Demander seulement si la fixture MNI ne permet pas d'identifier le cerveau de référence souhaité ; ne pas redemander la baseline validée.

## Vérifications à réaliser par l'agent

- Rejouer la préparation de fixture et vérifier une colonne, deux hémisphères, IDs et absence de données patient réelles.
- Vérifier que les chemins documentés sont résolus depuis le dépôt ; comparer le nouvel état Git à l'inventaire.

## Validation manuelle du propriétaire

1. Ouvrir la fixture par la recette fournie : vérifier qu'il s'agit bien du cerveau anatomique attendu, avec une seule colonne.
2. Aucun casque requis. Indiquer précisément la commande ou les clics réellement utilisés dans le rapport.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la recette de préparation et la provenance des données ; signaler chaque fichier repris de feature/xr.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-001.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

La fixture anatomique est reproductible, identifiée et ouvrable ; l'état préexistant est conservé.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
