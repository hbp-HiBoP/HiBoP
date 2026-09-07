# QUEST-018 — Extraire la densité commune et l'utiliser sur Desktop

Jalon : [J4](../milestones/J4.md). Type : implémentation ciblée.
Dépendances : [QUEST-017](QUEST-017.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-018).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-018 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Data/Module3D/Base3DScene.cs ; Column3DAnatomy.cs ; Core/DLL/Generators

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Extraire uniquement la préparation, masque, calcul, normalisation et projection nécessaires à la densité dans une tranche commune.
- Faire appeler ce chemin par le Desktop existant et retirer sa branche redondante après comparaison avant/après.
- Définir les entrées explicites, l'invalidation et la propriété de ressources ; conserver les autres modalités et l'UI.

## Hors périmètre

Pas de copie Base3DScene côté Quest, refactor général, nouveau calcul natif ou branche Quest spécifique.

## Décisions et questions à traiter

Si l'extraction touche une règle utilisée par d'autres modalités, expliquer l'adaptation minimale et ses tests ; ne pas refactorer ces modalités en bloc.

## Vérifications à réaliser par l'agent

- Comparaison Desktop avant/après sur fixture et cas aucun site/un site/tous masqués.
- Vérifier dépendances du seam et preuve que Desktop l'appelle effectivement.
- Vérifier les calculs async et la libération après terminaison réelle, pas seulement après annulation de l'attente.

## Validation manuelle du propriétaire

1. Sur Desktop, ouvrir la fixture densité et comparer l'affichage avec la référence fournie.
2. Modifier un paramètre d'influence puis revenir à sa valeur initiale : l'affichage doit rester cohérent ; aucune nouvelle UI.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Présenter le chemin avant/après et les règles réellement partagées, avec liens aux appels et à la suppression du chemin redondant.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-018.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

La densité Desktop utilise une tranche commune vérifiée, sans seconde orchestration scientifique.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
