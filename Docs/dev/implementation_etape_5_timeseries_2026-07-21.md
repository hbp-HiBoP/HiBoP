# Implémentation de l’étape 5 — préparation des colonnes et buffers stables

Date : 21 juillet 2026

## Résultat

La préparation iEEG/CCEP remplit désormais la matrice aplatie temporelle dans une passe unique avec `ProjectionBufferBuilder`. Cette même passe calcule moyenne, écart-type, minimum et maximum des sites non masqués.

Les changements appliqués sont :

- suppression de `ActivityValuesOfUnmaskedSites` pour les colonnes dynamiques ;
- calcul en flux des limites utilisées par le seuil et la colormap ;
- histogramme construit directement en bins depuis les séries référencées, sans concaténation persistante ;
- un seul tableau zéro partagé par colonne pour tous les sites absents/masqués ;
- conservation de la disposition native temporelle contiguë `temps × sites` ;
- réutilisation des tableaux `Vec2` natifs et des tableaux Unity `Vector2` du `SurfaceGenerator` ;
- réutilisation du tableau d’UV null lorsque la topologie ne change pas.

Les séries par site restent des références vers les valeurs préparées nécessaires aux tooltips et aux électrodes. La matrice aplatie reste l’unique copie supplémentaire exigée par le pré-calcul natif.

## Tests automatisés

`Stage5ProjectionBufferTests` ajoute cinq scénarios :

- disposition exacte `temps × sites` ;
- exclusion des sites masqués des statistiques ;
- moyenne, écart-type, min et max analytiques ;
- fallback stable lorsque tous les sites sont masqués ;
- validation des dimensions de masque et de séries.

Le test fonctionnel natif du `SurfaceGenerator` vérifie maintenant par identité de référence que les buffers UV natifs survivent à plusieurs calculs.

Validation cumulée : **329 tests réussis sur 329** dans `HBP.Serialization.Tests`.

## Tests manuels conseillés

1. comparer matrice aplatie, limites, seuils et histogramme sur une visualisation de référence ;
2. masquer/démasquer tous les sites, puis tester une colonne sans correspondance de canaux ;
3. alterner sites, aire MarsAtlas et sources CCEP ;
4. profiler dix secondes de lecture et de scrubbing : vérifier l’absence d’allocation des buffers UV par frame ;
5. changer de maillage pour confirmer que les buffers sont redimensionnés une fois puis réutilisés ;
6. comparer les métriques natives et les checksums de projection avant/après à entrée identique.

## Gain attendu

Pour 30 000 sites et 100 instants, la suppression de la concaténation persistante des valeurs non masquées économise environ **11,4 Mio** par colonne dynamique. À 3 073 instants, cette économie atteint environ **352 Mio**.

Le partage des zéros économise `nombre_sites_absents × nombre_instants × 4` octets. Par exemple, 5 000 sites absents sur 3 073 instants représentent environ **58,6 Mio** évités.

Pour un maillage de 200 000 sommets, les deux anciens tableaux natifs temporaires d’UV représentaient environ **3,1 Mio alloués à chaque mise à jour**. Ils sont maintenant conservés et réutilisés, ce qui doit supprimer une source importante de pression GC pendant le scrubbing. Le coût CPU de préparation devrait également diminuer, car statistiques, limites et aplatissement partagent une seule lecture des valeurs.

## Limites et blocages

Aucun blocage. Les comparaisons visuelles complètes et les métriques d’allocation par frame nécessitent la scène produit et restent dans la liste des tests manuels.
