# QUEST-022 — Partager l'apparence scientifique des sites

Jalon : [J5](../milestones/J5.md). Type : implémentation ciblée.
Dépendances : [QUEST-021](QUEST-021.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-022).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-022 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Column3DDynamic.UpdateSitesSizeAndColorOfSites ; paramètres et états des sites

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Extraire la règle de taille/couleur/visibilité scientifique des sites en fonction des données préparées.
- Faire appeler cette règle par Desktop ; fournir ses résultats au backend Quest sans dépendance aux Renderer Desktop.
- Conserver séparées apparence scientifique et sélection/placement locaux.

## Hors périmètre

Pas de nouveau thème, sélection XR ou changement de palette scientifique.

## Décisions et questions à traiter

Les paramètres graphiques peuvent différer, pas la signification des couleurs ; exposer toute impossibilité de représentation fidèle.

## Vérifications à réaliser par l'agent

- Comparer tailles/couleurs/flags avant/après sur valeurs limites, négatives et masquées.
- Vérifier les sémantiques temporelles et la règle commune ; aucune copie de formule dans le backend Quest.

## Validation manuelle du propriétaire

1. Comparer la fixture Desktop à ses captures de référence, en particulier sites négatifs/positifs et masqués.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Présenter entrées/sorties de la règle et les deux consommateurs ; montrer la différence avec le matériau/instancing.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-022.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Apparence scientifique commune, Desktop préservé, résultat disponible pour Quest.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
