# SCENE-001 — Auditer le prototype et fixer la migration

Type : audit ciblé et références de validation.
Dépendances : [QUEST-023](../../tasks/QUEST-023.md), avec les preuves utiles de
018–022. Suivi : [registre](../TASK-STATUS.md#scene-001).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [le périmètre](../01-objective-and-scope.md),
[l'architecture](../02-target-architecture.md) et [la migration](../04-migration-and-validation.md).
Une demande d'implémenter cette fiche autorise son audit et ses références ; elle
n'autorise pas à commencer l'extraction ni à modifier les documents historiques.

## Points d'entrée à inspecter

Localiser `Base3DScene`, `Column3D` et leurs dérivées dans
`Assets/Scripts/HBP/Data/Module3D`, les modèles `Visualization`/`Column`, les
pipelines effectivement livrés par 018–023, les captures de
`Assets/Scripts/HBP/Transfer`, `QuestAnatomySession`, `QuestAnatomyView`,
`NativeProjectionInputs`, les asmdefs et les tests de ces chemins.
Ce sont des pistes de recherche, pas des noms à recréer s'ils ont changé.

## À réaliser

- Identifier source/commit et modifications locales de la baseline après 023,
  versions natives, fixtures et preuves réutilisables ; distinguer faits et hypothèses.
- Cartographier les responsabilités de scène/colonne, accès aux données, mutations,
  événements, calculs, ressources et dépendances de présentation. Pour chacune,
  indiquer propriétaire actuel, destination commune/adaptateur et consommateurs.
- Inventorier les autres modalités, callers et champs sérialisés affectés par
  l'extraction ; identifier les adaptations minimales de préservation Desktop.
- Fixer le contrat minimal du modèle commun : identités, données disponibles,
  opérations, invalidation, résultats, propriété et restauration. Traiter
  explicitement la destination des types `Base3DScene` et `Column3D`.
- Capturer ou vérifier les références anatomie/sites/densité/iEEG et les scénarios
  sans UI démontrant une opération de colonne et une opération existante de scène
  après restauration. Choisir cette dernière et ses données, par exemple un
  remplacement de référence surface/volume avec invalidation des colonnes concernées.
- Confirmer les huit lots, leur taille et leurs dépendances ; exposer les risques
  pouvant demander un découpage supplémentaire. Mettre à jour seulement ce dossier
  si une précision d'exécution est nécessaire, sans changer les décisions produit.

## Hors périmètre

Pas de nouvelle architecture implémentée, port de modalité, réécriture de tests
déjà pertinents, modification de l'agent 018 ou qualification globale 024.

## Vérifications par l'agent

- Recouper chaque dépendance critique avec le code et un chemin d'appel réel.
- Vérifier que les références couvrent les entrées et résultats, pas seulement
  des captures d'écran ; relever les tolérances existantes et leur justification.
- Préparer une matrice I01–I10 avec scénario, fixture, preuve existante ou test
  manquant. Réexécuter uniquement les références nécessaires non fiables/réutilisables.
- Faire relire indépendamment, de façon bornée, le découpage et les risques
  d'intégrité/concurrence selon AGENTS.md ; consigner conclusions et réponses.

## Validation manuelle

NON_REQUIS pour l'audit technique. L'objectif et l'ordre sont déjà décidés.
Demander uniquement une décision produit nouvelle si l'audit révèle une extension
ou une incompatibilité matérielle ; ne pas présenter l'audit comme approuvé par silence.

## Rapport et critère de fin

Produire `reports/SCENE-001.md` et `evidence/SCENE-001/manifest.json`, mettre à
jour sa ligne locale. Fournir la carte avant/après, les références identifiées,
le découpage confirmé/proposé, les risques et les décisions encore nécessaires.

Terminé lorsque l'extraction minimale est définie à partir de faits, les références
utiles sont disponibles et les obligations de préservation sont identifiées.
Une incertitude fondamentale sur la propriété ou la baseline n'est pas reportée
silencieusement sur SCENE-002. Prochaine tâche : [SCENE-002](SCENE-002.md).
