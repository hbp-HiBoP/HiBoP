# P10 — comparaison des prototypes D3

- **Date :** 2026-09-03
- **Projet :** `XR/`, Unity 6000.5.2f1
- **Hôte :** Windows 11, Direct3D 12, NVIDIA GeForce RTX 2070 SUPER
- **Dataset :** D3 synthétique, 37 500 sites, 2 000 requêtes déterministes,
  seuil proximité 12 mm et rayon ray 2 mm
- **Commande :** assembly EditMode `CRNL.HiBoP.XR.SitePrototypes.EditModeTests`
- **Résultat :** 1/1 PASS, exactitude grid/BVH 100 % sur proximité et ray

## Mesures

| Mesure | p50 | p95 | max |
| --- | ---: | ---: | ---: |
| grille build | 4,7512 ms | 7,1903 ms | 7,1903 ms |
| BVH build | 116,2792 ms | 149,7928 ms | 149,7928 ms |
| grille proximité | 53,3 µs | 86,5 µs | 764,0 µs |
| BVH proximité | 4,2 µs | 6,6 µs | 472,3 µs |
| grille ray | 33,3 µs | 58,1 µs | 1 541,6 µs |
| BVH ray | 3,7 µs | 6,9 µs | 670,2 µs |
| matrices update + soumission | 2,7729 ms | 3,4495 ms | 4,5523 ms |
| buffers update complet + soumission | 0,0151 ms | 0,5557 ms | 1,3269 ms |
| buffers dirty 256 + soumission | 0,0012 ms | 0,0016 ms | 0,0044 ms |

Le prototype matrices produit 37 draws et 2 400 000 octets de matrices CPU. Le
prototype bufferisé produit un draw et 1 200 000 octets de buffers statique et
dynamique.

Le shader du chemin matrices utilise réellement les matrices d'instance Unity,
tandis que le variant bufferisé lit les deux buffers structurés. L'exactitude
ray est comparée à une référence exhaustive, car avec un rayon réel de 2 mm un
site placé sur le ray n'est pas nécessairement le premier volume intercepté.

## Décision

P10-A retient les buffers structurés et `RenderMeshPrimitives`. P10-D retient le
BVH statique, dont le build plus coûteux est amorti sur la durée de vie du hash
et dont la proximité est plus de dix fois plus rapide. Ces mesures ne remplacent
pas le profil GPU/thermique Quest D20.
