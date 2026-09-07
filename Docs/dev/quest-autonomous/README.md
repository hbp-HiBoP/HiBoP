# HiBoP Quest autonome — cadrage

Statut : proposition d'architecture et de roadmap, 2026-09-07.
Les décisions produit validées sont distinguées des choix techniques à qualifier.
Aucun code de production ni réglage Unity n'est modifié par cette analyse.

## Lancer une tâche dans une nouvelle conversation

Pour une entrée sans ambiguïté dans une nouvelle conversation, écrire :

> Implémente la tâche décrite dans Docs/dev/quest-autonomous/tasks/QUEST-001.md.

La [fiche correspondante](tasks/QUEST-001.md) demande de lire le
[contrat d'exécution](TASK-WORKFLOW.md) et le [registre de réalisation](TASK-STATUS.md).
La forme courte « Implémente QUEST-001 » donne aussi l'ID exact du fichier à
rechercher dans le dépôt. Les consignes de ce chantier restent dans ce dossier,
sans bloc spécifique dans AGENTS.md global ni nettoyage différé en fin de roadmap.

Le [catalogue de 29 tâches](tasks/README.md) fournit des unités de review plus
petites. Les [8 jalons](milestones/README.md) gardent les démonstrations produit.
Chaque fiche précise périmètre, décisions à poser, tests à faire par l'agent,
recette de validation manuelle et explication attendue. Les rapports suivent
un [modèle commun](reports/TEMPLATE.md) et enregistrent séparément implémentation,
preuve technique et validation utilisateur.

Branche vérifiée : feature/xr-autonomous, issue de develop, à 8b868a5b8 lors du
découpage documentaire. Le dossier XR historique n'est plus dans ce checkout ;
consulter feature/xr via Git pour les reprises ciblées.

Le produit utilise un projet Unity pour Desktop et Quest. Desktop prépare une
visualisation ; Quest reçoit les données et exécute localement les fonctions
admises. Les vues et manipulations spatiales sont indépendantes. Le prototype
progresse du cerveau anatomique aux sites, puis aux projections locales de
densité et d'un instant iEEG, avant qualification Mac Apple Silicon.

## Lecture

1. [Vision et périmètre](00-product-vision.md).
2. [Décisions et questions](01-decisions-and-open-questions.md).
3. [Audit et réutilisation](02-existing-code-and-reuse-audit.md).
4. [Architecture cible](03-target-architecture.md).
5. [Spécification du prototype](04-prototype-specification.md).
6. [État, transfert et synchronisation](05-state-command-and-sync-model.md).
7. [Natif, données et portabilité](06-native-data-and-portability-strategy.md).
8. [Validation et mesures](07-validation-and-measurement-plan.md).
9. [Roadmap et backlog](08-roadmap-and-milestones.md).
10. [Risques](09-risk-register.md).
11. [Points d'extension et fonctionnalités futures](10-extension-points-and-future-features.md).

Le [prompt de départ](ANALYSIS-PROMPT.md) est conservé comme historique. Les
réponses consignées dans le journal le corrigent : aucun partage de caméra ni
rejeu des gestes spatiaux, calcul scientifique local dès les jalons de projection,
iEEG limité à un instant, Mac après ces jalons Windows.

## Portée de la preuve

L'audit a lu les dépôts et rapports existants, comparé la baseline et sollicité
une revue indépendante des frontières scientifiques. Il n'a exécuté aucun
build, test Unity, programme natif, transport ou opération sur casque.
Les succès anciens sont des preuves circonscrites à leurs probes, pas une
validation du nouveau produit.

La présente demande ne lance aucune implémentation produit. Une demande future
« implémente QUEST-NNN » autorisera le périmètre de cette tâche selon le contrat,
sans nouvelle confirmation générique.
Le transport embarqué doit être qualifié ; un bouton afficher/masquer le cerveau
est accepté pour la visibilité des contacts internes.
