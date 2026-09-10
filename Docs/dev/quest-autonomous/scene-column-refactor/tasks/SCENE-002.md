# SCENE-002 — Généraliser scène, colonnes et présentation

Édition du 10 septembre 2026. Lot A.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Faire des classes 3D existantes la véritable implémentation commune, tout en
gardant Desktop raccordé. Adapter les objets et références qui imposent
actuellement ses vues ou contrôles.

## Travail

- Conserver Base3DScene/Column3D et leurs collaborateurs autant que possible.
  Généraliser les dépendances à View3D, Camera3D, fenêtres et sélection Desktop.
- Séparer les responsabilités caméra/viewport/entrées de la gestion commune de
  scène et de colonnes. Choisir héritage ou composants selon les besoins concrets.
- Conserver GameObjects, meshes, matériaux et rendu réutilisable ; aucun socle
  .NET pur ou fonctionnement sans renderer n’est imposé.
- Permettre initialisation et utilisation du contenu sans instancier de faux
  objets de présentation Desktop. Adapter les prefabs et sérialiser leurs références.
- Raccorder le chargement Desktop aux classes communes dans ce même lot, sans
  maintenir un deuxième modèle de données modifiable.
- Clarifier partage et propriété des ressources, fermeture de scène/colonne,
  dépendances async et fin réelle des utilisateurs natifs.
- Préserver les types/champs persistants et références Unity utiles. Les façades
  temporaires éventuelles doivent avoir une suppression identifiée en 007.

## Passage à la suite

La structure commune et son utilisation Desktop sont implémentées, suffisamment
pour poursuivre les opérations et le chargement. Une compilation indépendante
de cette fiche et une recette Desktop ne sont pas exigées. Documenter les
points à reprendre pendant l’intégration, sans coder des adaptateurs provisoires
uniquement pour rendre cette frontière compilable.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
