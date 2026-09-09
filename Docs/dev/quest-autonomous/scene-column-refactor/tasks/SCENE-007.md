# SCENE-007 — Retirer les chemins remplacés

Type : suppression ciblée de redondances et non-régression.
Dépendance : [SCENE-006](SCENE-006.md). Suivi : [registre](../TASK-STATUS.md#scene-007).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [l'architecture](../02-target-architecture.md),
les inventaires de SCENE-001 et les listes de façades/résidus des rapports 002–006.
Ne pas considérer tout code Desktop historique comme obsolète.

## Points d'entrée à inspecter

Callers de scène/colonnes et pipelines, propriétaires scientifiques Quest retirés,
accès aux données par payload/vue, abonnements et caches, asmdefs, prefabs et
diagnostics affectés. Localiser usages sérialisés et réflexion avant suppression.

## À implémenter

- Retirer les branches scientifiques, stocks d'état modifiables, propriétaires
  et façades de transition remplacés pour anatomie/sites/densité/iEEG.
- Un adaptateur de présentation légitime peut rester ; il délègue au socle sans
  contenir règles métier, recherche alternative de données ou scheduling scientifique.
- Préserver les conversions de formats antérieurs encore supportés en les faisant
  alimenter le modèle commun ; ne pas retirer une compatibilité par nettoyage.
- Conserver les chemins historiques nécessaires aux autres modalités Desktop et
  documenter exactement leur périmètre, sans promettre leur exécution sur Quest.
- Corriger les références de code/prefabs/tests/diagnostics réellement orphelines
  de ces suppressions, en préservant sérialisation et fonctionnement existants.
- Produire un inventaire final des responsabilités et des chemins restants dans
  ce dossier, sans modifier les rapports et registres historiques.

## Hors périmètre

Pas de nettoyage global du dépôt, suppression d'anciens chantiers, port d'autres
modalités, migration générale de namespaces ou suppression de compatibilité non
décidée. Aucun travail appartenant à un autre agent n'est inclus dans le nettoyage.

## Vérifications par l'agent

- Rechercher les anciens points d'accès/branches et inspecter les références
  effectives ; une absence de texte seule ne prouve pas la suppression d'un usage sérialisé.
- Vérifier dépendances d'assemblies et chemins métier : pas d'appel scientifique
  vers toolbar, caméra, renderer, connexion ou payload après chargement.
- Prouver que les deux Players utilisent toujours le socle après retrait des
  façades ; exécuter les tests pertinents de 003–006.
- Ouvrir/charger les scènes, prefabs et projets représentatifs ; vérifier les
  modalités hors migration réellement affectées et les diagnostics conservés.
- Contrôler builds utiles, ressources et abonnements sur les chemins modifiés.
  Ne pas effacer un test de non-régression pour faire passer la suppression.

## Validation manuelle

NON_REQUIS si les suppressions ne changent aucun comportement visible et que
les preuves correspondantes restent valides. Si un comportement est affecté,
préparer une recette ciblée et recueillir le retour ; ne pas déduire le succès
manuel de la compilation.

## Rapport et critère de fin

Produire `reports/SCENE-007.md`, `evidence/SCENE-007/manifest.json` et sa ligne
locale. Montrer ce qui a été supprimé, les callers remplacés, les adaptateurs
légitimes et les modalités historiques volontairement conservées.

Terminé lorsque le périmètre migré n'a plus deux chemins métier ou autorités
d'état, et que les suppressions sont vérifiées. Un résidu obligatoire non traité
reste un écart explicite, pas un commentaire TODO de fin de chantier.
Prochaine tâche : [SCENE-008](SCENE-008.md).
