# Implémentation de l’étape 7 — Trial Matrix pavée et frugale

Date : 21 juillet 2026

## Résultat

La Trial Matrix ne crée plus de texture monolithique supposant que largeur et hauteur tiennent dans `SystemInfo.maxTextureSize`. `TrialMatrixTileBuilder` découpe désormais la matrice sur les deux axes et garantit que chaque texture respecte la limite matérielle.

Le nouveau chemin :

- calcule les limites par statistiques en streaming, sans `List<float>` ni concaténation globale ;
- interpole directement depuis les essais sources, sans matrice lissée complète ;
- produit des pixels `Color32` et les transfère avec `SetPixelData` ;
- ajoute un halo d’un pixel autour des cœurs de tuiles en lissage 2D bilinéaire ;
- recadre ce halo via l’UV de chaque `RawImage`, afin que les jointures disposent des mêmes voisins que la matrice globale ;
- place chaque tuile dans les coordonnées normalisées globales du sous-bloc ;
- conserve donc les indicateurs d’événements dans leur repère temporel global existant ;
- réutilise une texture lorsque ses dimensions sont inchangées ;
- détruit explicitement les textures et objets UI excédentaires lors d’un redimensionnement ou du déchargement ;
- comptabilise les textures actives dans le budget mémoire de l’étape 6.

Il n’y a aucune décimation : une valeur source reste un pixel, et le lissage conserve la même grille interpolée. La comparaison au lisseur natif accepte un écart maximal de **1 niveau sur 255 par canal**, dû uniquement à l’ordre des opérations flottantes avant quantification RGBA32.

## Tests automatisés

`Stage7TrialMatrixTileTests` couvre huit scénarios :

- largeur seule supérieure à la limite simulée ;
- hauteur seule supérieure à la limite ;
- dépassement simultané sur les deux axes ;
- couverture exacte de chaque pixel du cœur ;
- équivalence avec le lissage monolithique 1D ;
- équivalence avec le lissage monolithique 2D à travers plusieurs jointures ;
- présence et contenu exact des halos gauche/bas d’une tuile intérieure ;
- rejet d’une matrice jagged incohérente ;
- limites statistiques en streaming équivalentes à la concaténation de référence.

Validation ciblée : **8 tests réussis sur 8** dans `HBP.Serialization.Tests`. La fixture d’interface partagée avec les graphes a ensuite réussi **18 tests PlayMode sur 18** après l’étape 8. Validation finale cumulée : **355 tests EditMode sur 355**.

## Tests manuels conseillés

1. afficher une matrice dépassant la taille maximale sur la largeur, la hauteur puis les deux axes ;
2. comparer captures et pixels aux mêmes matrices plus petites rendues en une seule tuile ;
3. activer le lissage 1D puis 2D et inspecter toutes les jointures à plusieurs niveaux de zoom ;
4. vérifier les événements principaux et secondaires, notamment ceux situés exactement près d’une jointure ;
5. changer colormap et limites plusieurs fois et confirmer que les textures de même taille sont réutilisées ;
6. ouvrir/fermer dix fois la Trial Matrix et vérifier la libération des textures dans le profiler Unity/GPU ;
7. tester le protocole complet avec sélection, survol, glisser et zoom.

## Gain attendu

Pour `N` pixels lissés, l’ancien pic comprenait notamment une matrice lissée d’environ `4N` octets et un tableau `Color[]` de `16N` octets. Le nouveau chemin remplace ces deux allocations par des buffers de tuiles `Color32` totalisant environ `4N` octets, hors petits halos.

L’économie transitoire est donc d’environ :

- **12 octets par pixel sans lissage**, soit 11,4 Mio par million de pixels ;
- **16 octets par pixel avec lissage**, soit 15,3 Mio par million de pixels.

La copie CPU lisible et la copie GPU RGBA32 des textures restent nécessaires. Le pavage ajoute seulement les pixels de halo aux frontières intérieures, coût généralement faible devant la surface. Le bénéfice fonctionnel majeur est la suppression de toute limite liée à une texture monolithique, sans perte temporelle ni réduction du nombre d’essais.

## Limites et blocages

Aucun blocage. Toutes les tuiles sont actuellement matérialisées, conformément au comportement « protocole complet immédiatement accessible ». Une virtualisation des seules tuiles visibles reste un prolongement conditionnel : elle ne sera justifiée que si un profil produit montre que le nombre total de textures, et non leur taille individuelle ou les copies CPU, demeure coûteux.
