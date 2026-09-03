# P10 — validation du backend production

- **Date :** 2026-09-03
- **Unity :** 6000.5.2f1
- **Backend :** positions statiques et attributs dynamiques en
  `GraphicsBuffer`, un `RenderMeshPrimitives` par ensemble de sites
- **Index :** BVH médian statique partagé par hash de `SiteAsset`

## Ordre du gate de décision

Les règles P10-B/C/E ont été enregistrées dans l'ADR avant les prototypes. Les
prototypes jetables ont ensuite comparé matrices/buffers et grille/BVH sur D3.
P10-A/D ont été enregistrées à partir de ces mesures avant l'ajout du backend
production. Le détail chiffré est dans `prototype-comparison.md`.

## Validation automatisée

Commande Unity EditMode, avec les assemblies sites, prototypes, P09 et P05 :

```text
CRNL.HiBoP.XR.Sites.EditModeTests;
CRNL.HiBoP.XR.BrainInstances.EditModeTests;
CRNL.HiBoP.XR.StaticRendering.EditModeTests;
CRNL.HiBoP.XR.SitePrototypes.EditModeTests
```

Résultat final : **45/45 PASS**, zéro échec. Le test D3 charge exactement
37 500 sites dans huit instances partageant le buffer statique, sans constante
de plafond, et obtient **2 000/2 000 IDs exacts** pour ray + proximité :

| Mesure hôte D3 | Résultat |
| --- | ---: |
| picking p50 | 6,7 µs |
| picking p95 | 0,0217 ms |
| picking max | 0,5027 ms |
| draws attendus, 8 instances | 8 |
| objets/renderers/colliders par site | 0 |
| buffer statique partagé | 600 000 octets |
| buffer dynamique par instance | 600 000 octets |

Les tests couvrent aussi les égalités déterministes par ID opaque, les sites
invisibles ou de rayon nul, le scale uniforme du `BrainInstance`, la validation
statique bornée à la première frame sans rétention, l'atomicité avant mutation,
l'identité IDs/positions/bounds sous un hash, la réduction du rayon maximum,
l'initialisation complète avant dirty ranges, le cycle
hover/pending/canonical corrélé par `commandId`, le rejet des outcomes tardifs,
l'effacement des métadonnées aux changements de contexte et le chemin
production `BrainInstanceRegistry` pour appliquer les frames, piloter le hover,
émettre la commande P07 `SelectSite` et appliquer son outcome. Le transport de
timeline reste volontairement hors périmètre P10.

## Prefabs et scènes

- `P10SiteSet.prefab` possède un renderer bufferisé et un contrôleur de
  sélection, sans `MeshRenderer` ni `Collider` ;
- `P09BrainInstance.prefab` sérialise exactement un `P10 Site Set` sous le
  repère `Surface` ;
- `P10D0.unity` fournit le cas déterministe ;
- `P10D3.unity` fournit 37 500 sites et les phases 1/3/8 instances ;
- le profiler capture frame/main/render/GPU, GC, mémoire, dirty range 256 et
  picking, puis écrit seulement des mesures synthétiques.

## APK Android

`XR/Tools/Build-P10.ps1` a produit un build Development Android valide :

| Champ | Valeur |
| --- | --- |
| résultat | Succeeded |
| APK | 77 718 726 octets |
| SHA-256 | `4f73ee60eb7ce9bcf8f91a542f1f750650ce23b23864d2b0b51b8ae1e86e9368` |
| scène | `Assets/HiBoPXR/Sites/Scenes/P10D3.unity` |
| sites | 37 500 |
| ensembles bufferisés | 8 |
| objets individuels | 0 |

La preuve JSON locale est générée sous
`.artifacts/xr/p10/build-evidence.json` avec l'APK correspondant.

## Profil Quest 3

Le profil a été exécuté sur Quest 3, Android 14/API 34, Vulkan/Adreno 740. Chaque
phase contient exactement 37 500 sites par instance, zéro objet par site et 721
frames mesurées. `cpuFrameMs` est le maximum du travail main thread hors attente
de présentation et du render thread ; `gpuFrameMs` vient du compteur Meta OpenXR
`perfmetrics.appgputime`.

| Instances | Exactitude | frame p95 | CPU p95 | GPU p95 | picking p95 | dirty 256 p95 |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 721/721 | 13,8879 ms | 2,7428 ms | 0,9218 ms | 0,0428 ms | 0,1897 ms |
| 3 | 721/721 | 13,8879 ms | 2,9339 ms | 0,9209 ms | 0,0518 ms | 0,4430 ms |
| 8 | 721/721 | 13,8879 ms | 3,4701 ms | 0,9511 ms | 0,0405 ms | 1,0050 ms |

Le compteur générique Unity des draw calls n'est pas disponible sur ce player
Meta. Les soumissions P10 sont néanmoins comptées au point unique
`RenderMeshPrimitives` : 1/3/8 appels pour 1/3/8 ensembles, sans split en lots
et sans objet individuel. Le log système confirme 72–73 fps pendant l'endurance.

La première mesure cible avec une sphère de 120 triangles par site avait échoué
avec des frames p95 de 41,66 / 111,10 / 1 374,90 ms. Le backend a donc conservé
les buffers et les 37 500 sites, mais remplacé la géométrie par un imposteur
sphérique caméra-facing de deux triangles qui écrit la profondeur de la sphère.
Le picking exact est resté inchangé.

## Endurance 30 minutes

`XR/Tools/Profile-P10Quest.ps1 -EnduranceMinutes 30` a maintenu huit instances
actives, avec dirty update de 256 sites et picking exact à chaque frame, et
capturé les checkpoints suivants :

| Temps | PSS | RSS | mémoire graphique | thermique | max CPU/GPU pertinent |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 5 min | 384 734 KiB | 521 396 KiB | 95 264 KiB | 0 | 58,60 °C |
| 15 min | 385 162 KiB | 521 888 KiB | 95 264 KiB | 0 | 58,74 °C |
| 30 min | 386 591 KiB | 523 176 KiB | 95 344 KiB | 0 | 58,81 °C |

Le workload a validé **129 600/129 600** pickings exacts pendant toute
l'endurance. Sur 232 échantillons VrApi du processus, le framerate reste entre
72 et 73 fps, le temps application p95 vaut 0,98 ms et le temps CPU+GPU p95
2,97 ms. La dérive PSS entre 5 et 30 minutes est de 1 857 KiB. Le log ne
contient aucun crash, ANR ou OOM. Après arrêt, `dumpsys meminfo` confirme qu'aucun
processus HiBoP ne subsiste.

Après cette endurance, la revue de fermeture a supprimé une rétention non
bornée de frames, remis le hover à zéro aux changements de contexte/asset et
raccordé l'API sites/outcomes/commandes au `BrainInstanceRegistry`. Ces
changements de lifecycle et d'intégration ne modifient ni shader, ni buffers
GPU, ni BVH mesurés sur Quest. Ils sont couverts par le run final 45/45, dont
100 frames distinctes sans rétention, et par la reconstruction Android finale ;
l'endurance appareil n'a donc pas été répétée.

## Verdict

**PASS.** Les critères P10 sont fermés : 37 500 sites sans plafond fonctionnel,
aucun objet/renderer/collider par site, picking exact et p95 largement sous 50 ms,
budgets CPU/GPU 72 Hz respectés et endurance sans dérive mémoire ou thermique.
