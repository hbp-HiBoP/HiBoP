# QUEST-023 — Afficher la projection iEEG calculée sur Quest

Jalon : [J5](../milestones/J5.md). Type : implémentation ciblée.
Dépendances : [QUEST-022](QUEST-022.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-023).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-023 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

QUEST-020 à QUEST-022 ; intégration densité QUEST-019

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Brancher l'instant reçu au calcul et à l'apparence communs, puis au renderer Quest.
- Afficher la progression et publier le résultat complet ; conserver le contenu précédent en cas d'échec.
- Qualifier numériquement l'instant et le calcul sans réseau.

## Hors périmètre

Pas de lecture/timeline, synchronisation live, calcul distant de remplacement ou port EEGFormat.

## Décisions et questions à traiter

Toute divergence numérique inexpliquée reste un échec ; ne pas masquer un problème en élargissant la tolérance.

## Vérifications à réaliser par l'agent

- Comparer valeurs normalisées, UV, opacité, tailles/couleurs et masque avec Windows.
- Vérifier provenance temporelle, moteur identique et calcul après déconnexion.
- Mesurer copies, temps et pic mémoire sans généraliser à une timeline entière.

## Validation manuelle du propriétaire

1. Choisir l'instant Desktop, envoyer, vérifier projection et contacts puis manipuler/masquer le cerveau.
2. Déconnecter et suivre le scénario de calcul local fourni ; vérifier absence de dépendance Desktop.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Présenter le trajet amplitudes → pipeline commun → buffers → rendu, puis les preuves de comparaison.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-023.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Un instant iEEG calculé localement et comparé, sans nouvelle logique scientifique Quest.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
