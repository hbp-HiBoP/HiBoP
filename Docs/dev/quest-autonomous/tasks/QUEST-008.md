# QUEST-008 — Manipuler localement le groupe cerveau

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-007](QUEST-007.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-008).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../04-prototype-specification.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-008 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Prefabs Quest créés en QUEST-007 ; référence HiBoP_HoloLens/Assets/Prefabs/3D/Scenes et HoloLens/Module3D

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Brancher saisie/déplacement/rotation aux contrôleurs et échelle uniforme du groupe par geste à deux contrôleurs.
- Prévoir recentrage et prise stable sans dépendance réseau ; exposer les réglages UX dans le prefab.
- Garder les données anatomiques immuables ; aucune commande de geste envoyée à Desktop.

## Hors périmètre

Pas de mains, changement de caméra Desktop, synchronisation ni modification des coordonnées scientifiques.

## Décisions et questions à traiter

Proposer des valeurs initiales pour taille/distance/bornes ; demander un ajustement UX à partir de l'essai, sans le transformer en règle scientifique.

## Vérifications à réaliser par l'agent

- Vérifier qu'un changement de pose/échelle ne modifie ni buffers source ni distances du modèle.
- Vérifier bornes positives de l'échelle, absence de drift après release et conservation du groupe lors du recentrage.

## Validation manuelle du propriétaire

1. Saisir, déplacer, tourner, agrandir/réduire, relâcher puis recentrer le cerveau.
2. Évaluer la facilité du geste et signaler saut, tremblement ou perte de prise ; le rapport décrit les boutons exacts.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer le parent manipulé, le mapping des boutons et l'indépendance du modèle ; comparer aux intentions HoloLens.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-008.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Trois gestes locaux fonctionnent et sont reviewables ; validation de confort consignée séparément.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
