# QUEST-007 — Rendre un snapshot anatomique sur Quest

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-004](QUEST-004.md), [QUEST-006](QUEST-006.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-007).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-007 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Ancien XR/StaticRendering via Git ; Core/DLL/Surface.cs pour conventions

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Adapter l'uploader/renderer opaque pour consommer le snapshot à travers le même point d'entrée qui recevra le réseau.
- Créer un prefab de visualisation avec groupe spatial et repère mm→m ; sérialiser matériau et références.
- Fournir une injection locale de fixture strictement diagnostique et la libération explicite meshes/buffers.

## Hors périmètre

Pas de transport réel prouvé, de calcul natif Android ou de transparence.

## Décisions et questions à traiter

Si l'ancien shader impose une correction scientifique, séparer cette règle du backend GPU avant réutilisation.

## Vérifications à réaliser par l'agent

- Comparer dimensions, orientation et buffers ; vérifier conversions et winding.
- Charger/remplacer/libérer plusieurs snapshots et relever ressources GPU/mémoire ; ne pas reconstruire le mesh à chaque frame.

## Validation manuelle du propriétaire

1. Sur Quest, observer les deux hémisphères et l'orientation à partir des repères fournis.
2. Le rapport doit préciser que cette démonstration utilise une injection locale, pas un transfert réseau.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la chaîne snapshot → mesh → prefab, ainsi que l'unique conversion d'unités et la libération.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-007.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Une surface complète est rendue correctement depuis le contrat, avec un cycle de vie explicite.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
