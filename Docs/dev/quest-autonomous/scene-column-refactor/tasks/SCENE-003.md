# SCENE-003 — Partager les opérations de toutes les modalités

Édition du 10 septembre 2026. Lot A.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Rendre les fonctionnalités 3D existantes accessibles par les mêmes opérations
sur Desktop et Quest, y compris celles qui n’auront pas encore d’interface Quest.

## Travail

- Migrer les responsabilités nécessaires à toutes les modalités : anatomie/densité,
  iEEG, CCEP, MEG, fMRI, statique et leurs collaborations.
- Extraire des callbacks UI les règles de scène/colonne nécessaires : opérations
  sur sites, groupe de colonnes, temps, coupes, ROI, ressources et paramètres.
  Laisser les widgets, labels et gestion purement visuelle dans la présentation.
- Conserver les opérations déjà bien placées ; ne pas ajouter une couche devant
  chaque méthode sans besoin.
- Réutiliser calculateurs, normalisation, sampling, masques et représentations ;
  unifier l’orchestration encore distincte dans les chemins du prototype.
- Raccorder invalidation et rendu aux mêmes mutations. Un mesh, instant ou plan de
  coupe modifié par code doit produire son effet sans toolbar active.
- Préserver les données nécessaires aux outils futurs, y compris les essais, sans
  porter leurs fenêtres ni développer les nouveaux contrôles scientifiques Quest.
- Faire utiliser ces opérations par les vrais outils Desktop. Ne pas préparer
  un modèle commun uniquement exercé par des tests.
- Identifier les états/configurations qui doivent être capturés en 005. Préparer
  des cibles/opérations explicites sans framework de commandes réseau.

## Passage à la suite

Les fonctionnalités et leurs appels Desktop sont raccordés à l’implémentation
commune ; les résidus sont suivis pour 007. Les vérifications de chaque modalité
et des mutations sont regroupées en 008. Aucun test obligatoire par fonction,
aucune recette ni build à cette frontière.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
