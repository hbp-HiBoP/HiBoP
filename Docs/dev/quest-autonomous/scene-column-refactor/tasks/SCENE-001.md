# SCENE-001 — Inventaire ciblé et choix concrets

Édition du 10 septembre 2026. Lot A.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Établir rapidement où généraliser les classes existantes et quelles données
doivent accompagner une visualisation complète. Réutiliser l’analyse de cette
discussion, sans lancer un audit exhaustif de tout HiBoP.

## Travail

- Inspecter Base3DScene, Column3D et toutes les dérivées, managers, objets de sites,
  View3D/Camera3D, Module3DMain et les outils Desktop qui les pilotent.
- Comparer aux interactions HoloLens : scène/colonnes spatiales, coupes et sites.
  Ne pas supposer l’existence de View3D/Camera3D HoloLens ni copier leur arborescence.
- Écrire une petite matrice fonctionnalité → opération → données → rendu →
  interaction, couvrant anatomie/densité, iEEG, CCEP, MEG, fMRI et statique,
  meshes/volumes, timelines, sites, coupes, ROI, atlas et états associés.
- Localiser les opérations enfermées dans les toolbars, les dépendances de vues
  obligatoires et les ressources/calculs encore propres au prototype Quest.
- Identifier les ressources et données de chaque modalité, dont essais,
  statistiques, ressources alternatives et états non sauvés dans les configurations.
- Proposer les déplacements/spécialisations minimaux, propriétaires et références
  sérialisées à préserver. Choisir une stratégie concrète, pas un catalogue d’interfaces.
- Référencer les fixtures/tests utiles existants et noter les manques pour 008.
  Faire une revue indépendante bornée du découpage et des risques selon AGENTS.md.

## Passage à la suite

Une carte concise permet de commencer le code et les points inconnus sont
localisés. Ne pas bloquer pour compléter un tableau de tous les callers.
L’investigation détaillée continue au fil de l’implémentation. Aucun build,
test de baseline ou retour manuel requis.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
