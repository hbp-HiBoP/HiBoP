# P09 — instances multiples et assets partagés

## Objectif et résultat observable

Afficher et manipuler plusieurs `BrainInstance` simultanées, liées à des visualisations/colonnes explicites, avec transformations locales indépendantes et topologies réellement partagées.

## Decision gate

**Hérité :** plusieurs visualisations/cerveaux sans plafond, D12 layout local réappliqué aux IDs valides, D14 asset immuable + buffers par colonne.

**À résoudre avant modèle d'instance public :**

- `P09-A` : bindings V1 exacts (`VisualizationBound`, `ColumnBound`, autres) et comportement de changement de colonne ;
- `P09-B` : création automatique ou uniquement sur demande XR ;
- `P09-C` : durée de vie d'une instance lorsque sa visualisation/colonne ferme côté Desktop ;
- `P09-D` : portée de la représentation anatomical/inflated et des propriétés locales/partagées ;
- `P09-E` : règles de restauration du layout après resume/new epoch.

Si l'inventaire fonctionnel ne répond pas à P09-A–D, demander une décision produit avant d'implémenter les bindings.

## Périmètre autorisé

- modèle BrainInstance et gestionnaire local ;
- transformations/recentrage/scale ;
- bindings avec miroir P07 ;
- partage SurfaceAsset P08 ;
- métriques mémoire/draw calls.

## Hors périmètre

- sites/timeline/coupes ;
- persistance durable du layout ;
- maximum codé d'instances ;
- modification des caméras Desktop.

## Hypothèses fixées

- pose/rotation/scale sont Quest-locales ;
- asset/topologie ne sont pas clonés ;
- fermeture Desktop invalide le binding selon P09-C ;
- unités P03.

## Dépendances et état initial

- P05 renderer statique ;
- P07 session ;
- P08 assets distants ;
- scénario D1/D2 multi-visualisations.

## Fichiers/modules pressentis

- XR BrainInstance/registry/layout ;
- Contracts si P09-A révèle un binding manquant, avec réouverture P02 ;
- Rendering pour propriétés par instance.

## Étapes

1. Résoudre P09-A–E.
2. Définir lifecycle et mapping IDs.
3. Créer/fermer/rebinder instances depuis état canonique.
4. Appliquer transformations locales sans commande Desktop.
5. Partager assets et isoler matériaux/buffers mutables.
6. Implémenter resume/layout selon P09-E.
7. Profiler 1/3/8 instances et cycles de fermeture.

## Tests et commandes

- unit tests lifecycle/bindings ;
- plusieurs instances sur même hash ;
- changement/fermeture colonne/visualisation ;
- resume et new epoch ;
- memory profiler, draw calls et absence de clone Mesh ;
- mains/contrôleurs pour transform de base.

## Critères de sortie binaires

- [ ] P09-A–E enregistrées ;
- [ ] bindings V1 déterministes ;
- [ ] transformations indépendantes et locales ;
- [ ] topologie partagée physiquement ;
- [ ] fermeture/reconnexion sans instance fantôme ;
- [ ] aucun maximum métier codé ;
- [ ] métriques 1/3/8 archivées.

## Artefacts à remettre

Registry/lifecycle BrainInstance, tests, scène multi-instance, rapport mémoire/performance et ADR P09.

## Conditions d'arrêt

Arrêter si un binding ou scope de propriété est ambigu, si une fermeture Desktop peut perdre silencieusement un layout ou si le renderer exige de cloner la topologie.

## Prompt de démarrage

> Exécute P09 depuis `Docs/dev/xr/implementation-packets/P09-multi-brain.md`. Résous P09-A–E avant le modèle public. Implémente uniquement le lifecycle, les bindings et transformations locales des BrainInstance en partageant réellement les assets P08. Prouve fermeture, reprise, indépendance et mémoire.
