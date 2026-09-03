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

- [x] P09-A–E enregistrées ;
- [x] bindings V1 déterministes ;
- [x] transformations indépendantes et locales ;
- [x] topologie partagée physiquement ;
- [x] fermeture/reconnexion sans instance fantôme ;
- [x] aucun maximum métier codé ;
- [x] métriques 1/3/8 archivées.

## Résultat d'exécution — 3 septembre 2026

**PASS sur le périmètre lifecycle/bindings/transforms local.** L'[ADR P09](../adr/P09-multi-brain.md) ferme P09-A–E avant le modèle public et rouvre P02 uniquement pour les mappings entité/scope manquants. `VisualizationBound` suit la sélection canonique ; `ColumnBound` reste épinglé. Les instances ne naissent que sur demande XR, ferment explicitement avec leur cause lorsque leur cible disparaît, conservent leur layout pendant une reprise du même epoch et sont toutes purgées au nouvel epoch.

La suite P09 passe `13/13`. Avec la vraie surface anatomique D1, 1/3/8 instances conservent exactement un `SurfaceAsset`, un `Mesh`, 3 317 125 octets de payload P08 et 6 635 760 octets de mémoire mesh mesurée ; seuls les renderers/draw calls structurels passent de 1 à 3 à 8. Les 256 cycles create/close ne laissent aucun mesh référencé et la fermeture ramène `ResidentBytes` à zéro. Les régressions P09/P08/P05 passent `24/24`, et la suite Desktop partagée + serialization passe `670/670`.

Les commandes, métriques et limites sont consignées dans [la validation P09](../evidence/P09/multi-brain-validation.md). La preuve est Windows EditMode ; la capture GPU et les gestes mains/contrôleurs sur Quest restent à P13.

## Artefacts à remettre

Registry/lifecycle BrainInstance, tests, scène multi-instance, rapport mémoire/performance et ADR P09.

## Conditions d'arrêt

Arrêter si un binding ou scope de propriété est ambigu, si une fermeture Desktop peut perdre silencieusement un layout ou si le renderer exige de cloner la topologie.

## Prompt de démarrage

> Exécute P09 depuis `Docs/dev/xr/implementation-packets/P09-multi-brain.md`. Résous P09-A–E avant le modèle public. Implémente uniquement le lifecycle, les bindings et transformations locales des BrainInstance en partageant réellement les assets P08. Prouve fermeture, reprise, indépendance et mémoire.
