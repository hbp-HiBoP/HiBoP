# SCENE-008 — Stabiliser et qualifier la version intégrée

Édition du 10 septembre 2026. Lot C.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Détecter et corriger les bugs après l’implémentation complète, puis vérifier le
résultat sur Desktop et Quest avec une campagne finale regroupée.

## Travail

- Appliquer 04-migration-and-validation.md : formatage, compilation intégrée,
  corrections, tests ciblés existants et compléments réellement nécessaires.
- Vérifier les opérations communes et la couverture des modalités, données
  temporelles/essais, ressources alternatives, coupes et autres fonctions migrées.
- Vérifier ouverture/sauvegarde/rechargement des projets Desktop existants et
  conservation du comportement de leurs fonctionnalités.
- Vérifier capture/restauration cohérentes, MNI local identifié, plusieurs colonnes,
  indépendance des paramètres et poses, invalidation et ressources partagées.
- Reprendre annulation, fermeture et remplacement durant chargement/calcul :
  aucune publication tardive, libération prématurée ou scène partiellement remplacée.
- Construire Windows/Quest depuis les mêmes sources ; exercer les vrais parcours,
  la restauration des modalités représentatives et le fonctionnement hors connexion.
- Mesurer les temps et pics mémoire utiles ; corriger les défauts bloquants sans
  réduction silencieuse des données ni changement des conventions scientifiques.
- Fournir une recette manuelle unique pour rendu, lisibilité et manipulation
  indépendante des colonnes. Les opérations sans contrôles Quest sont exercées
  par l’agent via un diagnostic ou appel de test ciblé.
- Corriger par groupes et rejouer les vérifications affectées. Ne répéter toute
  la campagne que si l’étendue d’une modification le justifie réellement.
- Produire reports/FINAL.md et evidence/final/manifest.json ; mettre à jour le
  registre et fournir les preuves réutilisables pour les besoins pertinents de 024.

## Passage à la suite

Le résultat est qualifié sur les parcours et plateformes effectivement testés,
ou les écarts restant à résoudre sont explicitement listés. Aucune modalité
obligatoire absente ne peut être masquée derrière le succès anatomie/iEEG.
Les retours manuels et preuves non exécutées restent distincts. La clôture
administrative de QUEST-024 et les autres plateformes ne sont pas implicites.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
