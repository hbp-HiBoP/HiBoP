# QUEST-021 — Partager le pipeline de projection iEEG côté Desktop

Jalon : [J5](../milestones/J5.md). Type : implémentation ciblée.
Dépendances : [QUEST-020](QUEST-020.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-021).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-021 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Base3DScene.ComputeGeneratorsAsync ; IEEGGenerator ; Column3DDynamic

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Étendre la tranche commune au calcul iEEG, masques, normalisation et UV.
- Faire utiliser cette extension par Desktop en préservant son comportement existant sur plusieurs instants.
- Supprimer la branche redondante de cette tranche uniquement et fournir des résultats consommables par le renderer.

## Hors périmètre

Pas de nouvelle timeline XR, port des corrélations ou duplication de scheduling.

## Décisions et questions à traiter

Si une optimisation modifie les calculs ou le sampling, la différer plutôt que l'inclure dans l'extraction.

## Vérifications à réaliser par l'agent

- Comparer Desktop avant/après sur plusieurs échantillons même si Quest n'en reçoit qu'un.
- Cas masqués, valeurs négatives et changements de paramètres ; preuve d'appel au service commun.
- Contrôler ressources/concurrence et absence de dépendances UI/XR dans le pipeline.

## Validation manuelle du propriétaire

1. Sur Desktop, ouvrir la fixture et parcourir quelques instants ; vérifier que le comportement habituel est conservé.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer précisément quelles responsabilités quittent Base3DScene et ce qui reste lié à la présentation Desktop.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-021.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Projection iEEG commune effectivement utilisée par Desktop, avant/après vérifié.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
