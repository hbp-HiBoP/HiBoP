# SCENE-005 — Capturer et restaurer la visualisation complète

Édition du 10 septembre 2026. Lot B.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Exporter tout le contenu requis d’une visualisation et reconstruire les mêmes
classes communes, avec les ressources locales et les données reçues.

## Travail

- Mettre en œuvre le contrat de 03-payload-and-restoration.md après préparation
  complète : colonnes, modalités, relations, états courants et ressources.
- Inclure données des sites à tous les instants, essais/métadonnées nécessaires,
  configurations, ressources alternatives et données propres aux autres modalités.
- Capturer un état cohérent sans modifier les sélections ou le projet source.
  Réutiliser/compléter les configurations pour l’état réellement requis.
- Définir le nouveau format. Aucune conversion des anciennes versions Quest ou
  HoloLens n’est requise ; préserver le format des projets Desktop.
- Dédupliquer les ressources partagées, référencer le Data local vérifié et
  garder les paramètres/mutations de chaque colonne indépendants.
- Adapter les hypothèses de taille et de structure du codec/transport existant.
  Garder authentification, validation de contenu et idempotence utiles.
- Restaurer les objets communs avec données locales utilisables sans projet
  Desktop, chemins source, pointeurs transférés ou renderer Desktop.
- Préparer le nouveau contenu avant publication ; conserver le précédent sur
  erreur/annulation et nettoyer les ressources temporaires après leurs usages.

## Passage à la suite

Capture et restauration complètes sont implémentées et raccordables aux vrais
consommateurs. Le round-trip, les données invalides et les mesures de mémoire
sont vérifiés en 008. Ne pas attendre une campagne codec exhaustive pour intégrer
la réception Quest.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
