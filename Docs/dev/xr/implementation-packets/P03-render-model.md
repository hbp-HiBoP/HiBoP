# P03 — RenderModel et parité scientifique

## Objectif et résultat observable

Définir le plus petit ensemble de nouveaux DTO de rendu capable de reproduire hors de `HBP.Core/Data` les surfaces, sites, coupes et frames dynamiques du Desktop. Des adaptateurs externes projettent les modèles HiBoP vers ces DTO sans déplacer, copier ou modifier leurs classes. Une scène indépendante reconstruit les golden outputs P00.

## Decision gate

**Hérité :** D03/D04 frontières, D06 calcul Desktop, D07 bundle, D09 coupe, D13/D14 assets/sites.

**À résoudre avant API publique :**

- `P03-A` : sémantique attendue de l'interpolation temporelle de surface lorsque `TemporalSample.Alpha != 0` ;
- `P03-B` : repères, handedness, unités et matrices canoniques par asset ;
- `P03-C` : tolérances numériques/visuelles par représentation, validées via P00 ;
- `P03-D` : propriété et durée de vie des buffers, afin d'éviter copies implicites ;
- `P03-E` : liste exacte des représentations couvertes par le contrat V1.

P03-A et P03-C sont des décisions scientifiques. Sans validation explicite, seule l'instrumentation/capture est autorisée.

## Périmètre autorisé

- package `com.crnl.hibop.render-model`, assembly `CRNL.HiBoP.RenderModel` ;
- adaptateur de capture Desktop minimal sous `Assets/` ;
- scène/test renderer indépendant ;
- golden comparisons dont les sorties générées restent sous `.artifacts/xr/`.

## Hors périmètre

- transport/sérialisation réseau ;
- optimisation Quest ;
- modification UX ;
- correction silencieuse d'un résultat Desktop.

## Hypothèses fixées

- résultats canoniques produits sur Desktop ;
- surfaces immuables séparées des frames dynamiques ;
- `float32` baseline ;
- géométrie/base de coupe dédupliquées des overlays ;
- IDs issus de Contracts.

## Dépendances et état initial

- P00 golden outputs intégrés ;
- P02 Contracts intégré ;
- propriétaire scientifique disponible pour P03-A/C.

## Fichiers/modules pressentis

- package RenderModel + tests ;
- adaptateurs autour de `Timeline`, `Column3DDynamic`, `SurfaceGenerator`, `CutGenerator` ;
- scène/test de reconstruction, sans dépendance Core/Data.

## Étapes

1. Tracer les buffers réellement consommés par le renderer actuel.
2. Résoudre P03-A–E et enregistrer ADR/tests attendus.
3. Définir `SurfaceAsset`, `SurfaceFrame`, `SiteAsset/Frame`, `CutRenderResult`, `DynamicFrameBundle`.
4. Définir formats, dimensions, repères et dépendances d'assets.
5. Capturer depuis Desktop sans changer son rendu.
6. Reconstruire chaque golden dans une scène indépendante.
7. Comparer buffers/images et documenter toute divergence.

## Tests et commandes

- tests de counts/bounds/indices/dimensions ;
- D5 avec alpha temporel non nul ;
- round-trip mémoire sans wire codec ;
- golden buffers et image diff ;
- tests de durée de vie/pooling et absence d'alias mutable ;
- EditMode ciblé via MCP lorsque Unity est ouvert.

## Critères de sortie binaires

- [ ] P03-A–E explicitement résolues ;
- [ ] toutes les représentations P03-E ont un contrat ;
- [ ] scène indépendante ne référence ni Core ni Data ;
- [ ] golden outputs passent dans les tolérances approuvées ;
- [ ] interpolation surface/sites est testée ;
- [ ] propriété/lifetime des buffers documentés et testés.

## Artefacts à remettre

Package RenderModel, adaptateurs de capture, tests, ADR P03 et rapport textuel de parité avec hashes. Les captures, images, buffers et goldens générés restent sous `.artifacts/xr/`.

## Conditions d'arrêt

Arrêter si la baseline Desktop paraît incorrecte sans décision scientifique, si une représentation exige des données sources non prévues ou si un repère/unité reste implicite.

## Prompt de démarrage

> Exécute P03 depuis `Docs/dev/xr/implementation-packets/P03-render-model.md`. Commence par prouver/résoudre P03-A–E ; ne fige aucune API publique avant validation scientifique de l'interpolation et des tolérances. Définis un RenderModel indépendant de Core/Data et projette les modèles HiBoP par des adaptateurs externes, sans modifier leurs classes. Reconstruis les golden outputs P00 et livre les preuves de parité.
