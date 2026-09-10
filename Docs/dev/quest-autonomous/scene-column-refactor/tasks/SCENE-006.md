# SCENE-006 — Afficher les colonnes communes sur Quest

Édition du 10 septembre 2026. Lot B.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Faire utiliser la visualisation restaurée par Quest et afficher un cerveau par
colonne, chacun manipulable indépendamment.

## Travail

- Remplacer la scène scientifique propre à QuestAnatomyView/NativeProjectionInputs
  par les objets communs et leurs opérations ; réutiliser le code utile.
- Raccorder réception, préparation et publication à la visualisation complète.
  La connexion ne devient pas une dépendance de vie du contenu.
- Créer/adopter les prefabs de présentation Quest et leurs références. Afficher
  toutes les colonnes dans une disposition initiale simple, non superposée,
  avec leur nom ou un repère lisible si nécessaire pour les distinguer.
- Donner à chaque colonne une pose et une manipulation indépendantes en réutilisant
  les gestes existants. Pas de fusion/séparation HoloLens.
- Réutiliser le rendu commun et adapter ses contraintes Android/XR ; respecter
  coordonnées scientifiques, plans de coupe et unités malgré les poses locales.
- Faire fonctionner le rendu initial complet et les mises à jour provoquées par
  code : changements de mesh, instant, coupe et opérations des modalités.
- Adapter le résumé/progrès du transfert aux vraies visualisations ; retirer les
  restrictions UI obsolètes « MNI complet/colonne unique/instant unique ».
- Conserver exploration hors connexion, remplacement cohérent et fermeture.
  Ne pas construire de gizmos, timeline UI, matrice d’essais ou commandes réseau.

## Passage à la suite

Le vrai parcours de réception consomme les classes communes et la présentation
multicolonne est implémentée. Essais physiques, comparaison visuelle et recette
manuelle sont différés à 008 ; ne pas interrompre le lot pour qualifier cette fiche.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
