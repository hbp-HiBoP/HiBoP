# Référence du chargement temporel — 21 juillet 2026

Cette référence a été produite avec Unity 6000.5.2f1 sous Windows 11, sur une
machine à 16 processeurs logiques. Les deux scénarios ont été répétés dix fois.
Les fichiers JSON détaillés sont des artefacts locaux de benchmark ; la commande
qui les régénère est documentée dans `benchmark_chargement_timeseries_etape_0.md`.

## Résultats temporels

| Scénario | Checksum | P50 total | P95 total | P50 calcul | P95 calcul | P50 coupe | P95 coupe |
|---|---:|---:|---:|---:|---:|---:|---:|
| Smoke, 1 000 sites × 100 instants | `F72D0F4CEE55D5F3` | 200,22 ms | 224,07 ms | 58,72 ms | 62,70 ms | 0,227 ms | 0,270 ms |
| Product, 30 000 sites × 100 instants | `2FDA5CD13773B845` | 4 949,91 ms | 5 806,37 ms | 4 828,66 ms | 5 568,66 ms | 0,193 ms | 0,370 ms |

Chaque scénario a produit un seul checksum sur les dix répétitions.

## Mémoire par couche

| Scénario | Brut managé | Époques | Dérivés | Tableau colonne | Projection native | Texture estimée | Pic privé | Privé retenu maximal |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Smoke | 400 000 o | 400 000 o | 400 000 o | 400 000 o | 31 602 496 o | 109 068 o | 40 972 288 o | 4 222 976 o |
| Product | 24 012 000 000 o | 30 024 000 000 o | 24 012 000 000 o | 12 000 000 o | 170 772 416 o | 109 068 o | 191 606 784 o | 6 475 776 o |

Les trois premières colonnes sont les tailles logiques exactes du modèle de
copies actuel pour la charge synthétique déclarée dans le rapport. Elles ne sont
pas allouées par le benchmark de projection. Le tableau de colonne, la projection,
la texture et les deltas de mémoire privée proviennent de l'exécution réelle.

Le scénario Product utilise 250 patients, 120 canaux par patient, 100 essais,
2 001 échantillons par fenêtre et 501 par baseline. Sa projection contient
422 704 points et 42 270 400 valeurs stockées.

Pour les deux scénarios, le delta privé retenu maximal apparaît à la première
répétition uniquement. Les neuf répétitions suivantes reviennent à zéro par
rapport à leur propre ligne de base, ce qui établit le plateau attendu pour cette
séquence d'ouverture/fermeture.
