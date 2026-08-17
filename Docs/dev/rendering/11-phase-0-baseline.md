# Phase 0 — Baseline Built-in reproductible

## Statut

La Phase 0 est implémentée par un collecteur de développement qui produit une
référence structurée du Built-in Render Pipeline sans modifier le rendu de
production. Les sorties sont écrites sous :

```text
.test-results/rendering/baseline-birp/<UTC yyyyMMdd-HHmmss>/
```

Le fichier `latest-run.txt` contient le chemin de la dernière exécution
terminée. Ce répertoire est ignoré par Git.

## Exécution

1. ouvrir HiBoP dans Unity et entrer en Play Mode ;
2. utiliser `Tools > HiBoP > Rendering > Capture Built-in Baseline (Phase 0)` ;
3. attendre le log `Rendering baseline completed` dans la console Unity.

Le menu `Capture Built-in Baseline without 30k sites` exécute la même campagne
sans le cas de charge à 30 000 sites et sert à itérer rapidement sur la fixture.

Par défaut, le collecteur charge :

```text
Documents/HiBoP/Projects/visu_full_test.hibop
visualisation: Small
```

Un autre emplacement peut être fourni avec la variable d'environnement
`HIBOP_RENDERING_BASELINE_PROJECT`. Si `Small` est déjà chargée, le collecteur
réutilise la scène courante.

## Contenu d'une exécution

Le `manifest.json` versionné contient :

- la version Unity, la plateforme, le GPU, l'API graphique, le Color Space, la
  qualité, le VSync et le pipeline actif ;
- la configuration de la scène, des colonnes, des vues, des caméras, des
  RenderTextures, des couleurs, de l'alpha, des Edges, des coupes et des sites ;
- l'inventaire de toutes les captures avec leur famille fonctionnelle ;
- pour chaque PNG, les dimensions et un relevé du canal alpha ;
- les métriques de performance médiane, P95 et P99 après warm-up ;
- des échantillons surface/coupe à des positions monde comparables ;
- les valeurs exactes de la fixture sRGB/Linear/alpha.

Les captures couvrent :

- anatomie et activité opaques ;
- sites ;
- Edges opaques ;
- cerveau transparent avec et sans Edges ;
- atlas Mars ;
- trois coupes opaques et transparentes ;
- ROI normal et sélectionné ;
- export individuel 2048×2048 sur fond transparent pour chaque scénario ;
- exports PNG des textures de coupe ;
- capture PNG de la fenêtre UI 3D complète ;
- composite 1920×1080 sur le fond historique `#282828` ;
- un AVI MJPEG court qui vérifie le chemin d'encodage vidéo ;
- le cas isolé 1 vue × 30 000 sites.

Les objets nécessaires aux scénarios (coupes, ROI et sites de charge) sont
temporaires. Tous les états de rendu, les caméras et les réglages de performance
sont restaurés dans des blocs `finally`.

Le sleep mode de HiBoP est suspendu pendant toute la campagne afin que
`PerformanceManager` ne force pas `Application.targetFrameRate = 1` en cours de
mesure. Son état précédent est restauré dans le `finally` principal, y compris
si la capture échoue. La détection `IdleThrottled` reste active comme garde-fou.

## Fixture colorimétrique

`RenderingBaselinePatches.shader` produit huit patchs déterministes : quatre
sources de couleur, chacune en opaque et alpha 0,5.

Les sources sont :

1. propriété uniforme ;
2. texture importée sRGB ;
3. texture Linear ;
4. vertex color.

Les centres des patchs et leurs octets RGBA sont enregistrés dans le manifeste.
La même fixture reste utilisable après la bascule URP ; elle vérifie donc les
conversions plutôt qu'une ressemblance visuelle globale.

## Mesures de performance

Chaque scénario de performance utilise :

- 120 frames de warm-up ;
- 300 frames mesurées ;
- VSync désactivé et `targetFrameRate = -1` pendant la mesure ;
- restauration des réglages initiaux à la fin.

Les séries enregistrées sont :

- intervalle de frame observé ;
- CPU main thread ;
- CPU render thread ;
- GPU frame time ;
- SetPass ;
- triangles ;
- vertices.

Le temps CPU/GPU issu des Profiler Recorders est la référence de comparaison.
L'état de focus au début et à la fin est enregistré. Une médiane d'intervalle de
frame supérieure ou égale à 250 ms marque automatiquement la série
`IdleThrottled = true` et `Normative = false`. Cela couvre notamment le mode idle
à environ une frame par seconde observé dans l'Editor. Une telle série reste
conservée pour diagnostic mais ne peut pas servir à accepter ou refuser URP.

Pour le cas 30 000 sites, une seule caméra de vue reste active. Des renderers
temporaires reprennent le mesh et le matériau du site actuel et sont distribués
de façon déterministe autour du cerveau. Ce cas isole le coût historique des
renderers de sites ; il ne représente pas une proposition d'architecture URP.

## Alpha et export transparent

L'export individuel passe par `View3D.GetTexture`, comme le produit actuel. Le
collecteur exige que les quatre coins du fond aient un alpha nul et enregistre
également le nombre de pixels transparents/opaques et les extrema du canal.

La baseline Built-in révèle un comportement historique important : lorsque le
cerveau transparent est exporté seul, les RGB sont présents mais le canal alpha
peut rester nul sur toute l'image. Cette observation est une référence de bug,
pas un contrat à reproduire. La cible URP reste un PNG straight alpha réutilisable
sur un autre fond, avec le cerveau visible et le fond à alpha zéro.

## Continuité surface/coupe

Pour chacune des trois orientations, les cinq sommets les plus proches du plan
et projetables dans la texture de coupe sont relevés. Le manifeste conserve :

- position locale et distance au plan ;
- UV de coupe, UV d'alpha et UV de colormap de la surface ;
- alpha de surface avant/après le multiplicateur historique `×2,5` ;
- couleur de colormap surface et pixel RGBA de coupe ;
- distances RGB et alpha.

Les textures de surface non lisibles sont copiées temporairement vers une
texture Linear lisible via le GPU. Aucun import setting d'asset n'est modifié.

## Gate 0

La gate est validée lorsque la dernière exécution complète possède :

- au moins une capture par famille fonctionnelle ;
- des coins alpha zéro pour chaque PNG individuel ;
- une série réelle et une série 30 000 sites avec médiane/P95/P99 ;
- des séries de performance marquées `Normative = true` ;
- huit patchs non vides ;
- quinze échantillons surface/coupe ;
- un composite PNG et un AVI non vides ;
- aucune entrée dans `Warnings` et aucun `capture-error.txt`.

Les valeurs quantitatives de la machine de référence sont lues directement dans
le `manifest.json` de `latest-run.txt`. Elles ne sont pas recopiées ici afin
d'éviter qu'une nouvelle exécution rende ce document obsolète.

## Exécution de validation initiale

La Gate 0 a été validée le 6 août 2026 par le run `20260806-095821` :

- Unity `6000.5.2f1`, Windows 11, Direct3D 11, RTX 2070 SUPER, projet Linear ;
- Built-in Render Pipeline confirmé dans le manifeste ;
- 43 captures, 45 fichiers et environ 26,8 Mo d'artefacts ;
- 10 exports individuels dont les quatre coins ont tous un alpha nul ;
- 3 PNG de coupe, 1 capture UI complète, 1 composite et 1 AVI non vides ;
- 8 patchs non vides et 15 échantillons surface/coupe ;
- aucune alerte, aucun `capture-error.txt` ;
- 42/42 tests PlayMode du module 3D réussis après la campagne.

| Scénario | Sites | Frame médiane / P95 / P99 | CPU médiane / P95 | GPU médiane / P95 | Normatif |
| --- | ---: | ---: | ---: | ---: | --- |
| `visu_full_test / Small` | 1 299 | 2,514 / 2,857 / 3,005 ms | 1,792 / 2,018 ms | 0,898 / 1,640 ms | oui |
| 1 vue × 30 000 sites | 30 000 | 20,404 / 22,641 / 26,192 ms | 19,243 / 21,432 ms | 11,313 / 13,087 ms | oui |

La suspension du sleep mode a été vérifiée : elle était inactive après la
campagne et `Application.targetFrameRate` était revenu à 60.

L'export Built-in du cerveau transparent sans coupe contient des RGB visibles,
mais `4 194 304 / 4 194 304` pixels ont un alpha nul, avec et sans Edges. Ce
comportement historique est donc formellement identifié comme défaut à corriger
pendant la migration, pas comme référence alpha à reproduire.
