# QUEST-019 — Calculer et qualifier la densité locale Quest

Jalon : [J4](../milestones/J4.md). Type : intégration / qualification.
Dépendances : [QUEST-018](QUEST-018.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-019).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-019 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Pipeline commun QUEST-018 ; entrées QUEST-017 ; renderer Quest

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Brancher les entrées reçues au même pipeline puis à l'affichage Quest ; séparer calcul hors rendu et publication Unity.
- Ajouter le banc de comparaison Windows/Android de la grille, couverture, densité maximale, UV et masques.
- Effectuer le calcul après déconnexion et collecter temps/copies/mémoire ; traiter les écarts avant tout verdict de parité.

## Hors périmètre

Pas de résolution scientifique réduite silencieusement, seuil permissif inventé ou résultats Desktop substitués.

## Décisions et questions à traiter

Soumettre toute tolérance nouvelle avec justification et exemples ; demander un arbitrage si performance insuffisante, sans modifier l'algorithme seul.

## Vérifications à réaliser par l'agent

- Comparer mêmes sources/entrées/paramètres ; cas analytiques et MNI, écarts max/RMS localisés.
- Valider catégories/masques exactement ; proposer les tolérances par grandeur avant acceptation scientifique.
- Charger/recalculer/libérer sans crash ni croissance inexpliquée.

## Validation manuelle du propriétaire

1. Envoyer la fixture, vérifier projection visible puis manipuler le groupe et masquer la surface.
2. Suivre la recette de recalcul hors ligne fournie et vérifier que le résultat reste affichable ; examiner le rapport de parité, pas seulement les couleurs.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer que Quest appelle exactement le pipeline Desktop ; expliquer les écarts numériques et les limites des mesures.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-019.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

J4 calcul local démontré, parité acceptée selon critères explicites ; acceptation scientifique non inventée par l'agent.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
