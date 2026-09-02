# P05 — renderer statique de surface

## Objectif et résultat observable

Afficher sur Quest une surface anatomical puis inflated depuis un `SurfaceAsset` P03, avec géométrie, normales, matériaux, transparence et repères fidèles, sans réseau ni Core/Data.

## Decision gate

**Hérité :** D03 renderer XR consommant le RenderModel partagé, D14 asset immuable partagé, P03 repères/tolérances, P04 stack XR.

**À résoudre avant renderer production :**

- `P05-A` : shader/material de référence et comportement URP Android ;
- `P05-B` : color space, formats de vertex/index et stratégie 16/32 bits ;
- `P05-C` : règle anatomical/inflated — assets distincts ou attribut/variant explicitement défini ;
- `P05-D` : baseline de transparence/ordre de rendu acceptable.

Si les shaders Desktop ne compilent pas Android, produire une comparaison et une décision Adapter/Réécrire ; ne substituer aucun shader visuellement différent sans accord.

## Périmètre autorisé

- renderer et asmdef exclusivement XR sous `XR/Assets/` ;
- application d'un SurfaceAsset local/synthétique ;
- matériaux/shaders statiques et profiling ;
- tests de repères/bounds.

## Hors périmètre

- transport/cache distant ;
- sites, timeline, coupes ;
- interactions autres que pose/recentrage bootstrap ;
- clonage de mesh par instance.

## Hypothèses fixées

- aucune donnée scientifique source ;
- asset immuable ;
- unités/repères P03 ;
- Android/Quest est le gate principal, Desktop sert à la parité.

## Dépendances et état initial

- P03 SurfaceAsset disponible ;
- P04 APK bootstrap validé ;
- golden D1 et tolérances P00/P03.

## Fichiers/modules pressentis

- assembly de rendu XR sous `XR/Assets/` ;
- shaders/materials portables ;
- scène de test XR et tests renderer.

## Étapes

1. Résoudre P05-A–D par compilation et comparaison.
2. Implémenter validation puis upload du SurfaceAsset.
3. Rendre anatomical, inflated et hémisphères nécessaires.
4. Implémenter matériaux/transparence sans état partagé accidentel.
5. Vérifier repères, bounds, normals et scale.
6. Comparer golden image et profiler Quest.
7. Documenter limitations GPU/shader.

## Tests et commandes

- validation indices/buffers/bounds ;
- shader compilation Android ;
- golden image Desktop/Quest ;
- profiler CPU/GPU/draw calls/mémoire ;
- cycles create/dispose sans fuite ;
- build APK P05.

## Critères de sortie binaires

- [x] P05-A–D enregistrées ;
- [x] anatomical/inflated fidèles dans tolérances ;
- [x] aucun accès Core/Data/native ;
- [x] aucun clone de topologie requis pour une instance ;
- [x] shader Android sans erreur/fallback caché ;
- [x] ressources libérées après fermeture ;
- [x] métriques de référence archivées.

## Artefacts à remettre

Renderer statique, shaders/materials approuvés et scène/test APK. Les rapports profiler et golden comparisons bruts restent hors Git sous `.artifacts/xr/`; seuls le rapport textuel et les hashes sont promus dans la documentation.

## Conditions d'arrêt

Arrêter si fidélité exige une décision scientifique/artistique absente, si le shader tombe en fallback ou si le format d'asset P03 doit changer sans réouverture de P03.

## Prompt de démarrage

> Exécute P05 depuis `Docs/dev/xr/implementation-packets/P05-static-renderer.md`. Résous P05-A–D avant de figer le renderer. Affiche uniquement des SurfaceAsset P03 locaux, sans réseau/Core/Data, compare aux golden outputs et valide shader, mémoire et performance sur Quest 3.
