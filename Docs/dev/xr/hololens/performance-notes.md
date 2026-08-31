# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — performances et dimensionnement

## Niveau de preuve historique

Aucun benchmark reproductible Quest ou HoloLens n'a été trouvé dans le prototype. App Remoting exécutait le rendu et les calculs sur PC ; ses sensations ne permettent pas d'estimer CPU, GPU, mémoire, thermique ou batterie d'un Quest autonome.

Les goulets d'étranglement visibles statiquement sont néanmoins certains :

- un GameObject et MeshRenderer par site ;
- une hiérarchie de sites recréée par colonne ;
- sélection proche par scan O(N) à chaque frame de pinch ;
- Mesh cloné par colonne afin de modifier ses UV ;
- orchestration principale et application des buffers sur le main thread ;
- aucune file latest-wins, car aucun calcul n'était distant.

## Données mesurées ou calculées dans la baseline actuelle

| Élément | Valeur | Nature |
| --- | --- | --- |
| surface MNI grise combinée | 69 104 sommets, 138 216 faces | asset réel |
| fichier OBJ correspondant | 4 784 818 octets | asset réel |
| deux `Vector2 float32` dynamiques | 1 105 664 octets/colonne | calcul exact |
| contrat exact reconstruit : 2 scalaires float32 + bitmask | 561 470 octets/colonne | calcul exact |
| variante float16 + bitmask | 285 054 octets/colonne | estimation, fidélité à valider |
| overlay de coupe MNI RGBA8 | 109 068 octets/colonne/coupe | calcul sur 27 267 pixels |
| cas sites de référence | 250 × 150 = 37 500 | exigence produit |

Pour huit colonnes, le contrat float32 compact représente environ 4,28 MiB par frame s'il est renvoyé intégralement. C'est un dimensionnement, pas un débit observé. La fréquence réseau doit être découplée de la fréquence du casque et contrôlée par coalescence/latest-wins.

## Pipeline actuel utile à l'extraction

HiBoP calcule le champ de projection lourd en amont et conserve valeurs/poids côté `hbp_core`. Un pas de timeline de surface produit deux attributs par sommet. Dans le code natif observé, la composante `x` porte la valeur et `y` est un masque binaire ; le contrat réseau peut donc reconstruire les deux `Vector2` depuis deux scalaires et un bit de masque, sans envoyer 16 octets/sommet.

Les coupes séparent géométrie, texture anatomique et overlay fonctionnel. La géométrie et la texture de base changent avec le plan ; un pas de timeline ne doit renvoyer que l'overlay par colonne si le plan est inchangé.

## Gates Quest obligatoires

- 72 Hz : budgets CPU/GPU p95 sous 13,89 ms ;
- 37 500 sites visibles et sélectionnables, sans GameObjects individuels ;
- 1, 3 et 8 colonnes dynamiques sans backlog ;
- coupes avec p50/p95/max command-to-photon et taux d'annulation ;
- sessions de 30 minutes pour mémoire et thermique ;
- comparaison mains/contrôleurs ;
- Wi-Fi institutionnel avec découverte bloquée et IP manuelle.

Les chiffres historiques ne ferment aucune de ces gates.
