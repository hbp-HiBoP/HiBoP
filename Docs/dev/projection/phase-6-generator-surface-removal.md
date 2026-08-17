# Phase 6 — Retrait de GeneratorSurface

## Statut

Implémentée et validée le 13 août 2026.

## Décision de compatibilité

`hbp_suite` et HiBoP HoloLens ne sont plus d'actualité depuis la migration vers
`hbp_core`. Aucun consommateur maintenu ne dépend donc de l'ancienne ABI et
aucune façade ou fenêtre de dépréciation n'est nécessaire.

Les classes sous `HBP.Tests.Serialization.LegacyNative` restent uniquement des
adaptateurs d'oracle pour comparer ponctuellement le binaire historique
`hbp_export`. Leur nom `GeneratorSurface` décrit ce contrat historique. Pour le
backend `hbp_core`, elles utilisent désormais `ActivityProjectionGrid` et les
nouveaux symboles d'initialisation ; elles n'appellent aucun symbole supprimé.

## Architecture finale

- `ActivityProjectionGrid` possède le volume, la géométrie de grille, les
  points volumiques, l'interpolation et les caches spatiaux.
- `ActivityGenerator` dépend uniquement de cette grille volumique.
- `SurfaceGenerator` reçoit explicitement la surface cible et construit un
  binding d'échantillonnage mis en cache.
- changer de surface ne reconstruit ni la grille ni le champ d'activité.
- l'export NIfTI repose sur la géométrie exacte de la grille et ne dépend plus
  d'une surface.

La concaténation historique des sommets de surface et des points de grille a
été supprimée. Les valeurs et poids d'activité ne sont stockés que pour les
voxels de la grille.

## Nettoyage réalisé

Dans HiBoP :

- suppression du wrapper de production `GeneratorSurface.cs` et de son `.meta` ;
- suppression des propriétés et overloads d'initialisation legacy dans
  `ActivityGenerator` et `SurfaceGenerator` ;
- migration des tests fonctionnels, tests de cycle de vie et benchmarks vers
  `ActivityProjectionGrid` ;
- retrait des tests qui figeaient la concaténation surface + grille ou exigeaient
  que l'export volumique conserve les dimensions de l'ancien objet hybride ;
- mise à jour de l'inventaire à 254 `DllImport`, dont 200 pour `hbp_core` après
  l'ajout du préflight de couverture.

Dans `hbp_core` :

- suppression des types et sources `GeneratorSurface` ;
- suppression des symboles `hbp_generator_surface_*` ;
- suppression de `hbp_activity_generator_initialize` et
  `hbp_surface_generator_initialize` ;
- conservation exclusive des variantes explicites
  `*_initialize_projection_grid` ;
- migration du smoke test, des tests fonctionnels et du benchmark natif ;
- mise à jour de la baseline ABI à 219 symboles.

## Validation

- compilation Release de `hbp_core` réussie ;
- 13/13 tests natifs CTest réussis ;
- baseline ABI validée contre le header public et la DLL Windows : 219 symboles ;
- DLL copiée dans `Assets/Plugins/x86_64/Windows/hbp_core.dll`, SHA-256
  `8D8CA2F1F0E6504C2AA0A302E9127F8AE4408E1C5AA6FF68E97666A92D339ADB` ;
- formatage C# appliqué avec `Tools/format-code.cmd` ;
- compilation Unity réussie ;
- 472/472 tests `HBP.Serialization.Tests` EditMode réussis.

Le benchmark natif synthétique (`8³`, 1 000 sites, 10 instants, 3 répétitions)
confirme un stockage strictement volumique de 512 points : 5 120 valeurs, 512
poids, 22 528 octets de champ et une médiane de 3,676 ms. Le cache spatial est
réutilisé lors de la dernière répétition.

Les tests de couverture de surface, de géométrie NIfTI, de séparation de la
résolution Localizer, d'iEEG, de densité, de fMRI et de MEG continuent à couvrir
les responsabilités déplacées. Le rendu CCEP reste volontairement inchangé et
pourra faire l'objet d'une refonte dédiée.

Après validation manuelle, le diagnostic de couverture a été déplacé avant le
calcul : une demande par le bouton propose désormais « Continue / Cancel »,
tandis que la projection automatique attend silencieusement un couple
surface/volume compatible. Le préflight construit et met en cache le même
binding natif que le rendu, sans calculer le champ d'activité.

## Gate de sortie

- aucun consommateur maintenu de `GeneratorSurface` : satisfaite ;
- aucune concaténation surface + grille : satisfaite ;
- responsabilités natives et C# explicites : satisfaite ;
- compilation, ABI, tests natifs et tests Unity : satisfaits.
