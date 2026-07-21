# Audit final d’implémentation time-series — étapes 3 à 8

Date : 21 juillet 2026

## Conclusion

Les étapes 3 à 8 de `audit_chargement_timeseries_hibop_2026-07-21.md` sont implémentées et aucune régression automatisée n’est détectée. Les transformations restent exactes : aucune décimation scientifique, aucune réduction silencieuse sous pression mémoire et aucune modification du repère temporel des événements.

Les validations finales sont :

- **355/355** tests réussis dans `HBP.Serialization.Tests` ;
- **15/15** tests réussis dans `HBP.ProjectWorkflow.Tests` ;
- **18/18** tests réussis dans `InformationGraphPlayModeTests`, avec périphérique graphique réel ;
- zéro erreur de compilation ;
- `git diff --check` sans erreur d’espacement ;
- contrat de sérialisation validé par la suite complète.

Unity 6000.5.2f1 a été lancé hors sandbox conformément aux instructions du projet. Les logs montrent la connexion réussie à `LicensingClient`; les messages de handshake antérieurs à cette connexion sont transitoires et n’ont affecté aucune suite.

## État par étape

### Étape 3 — statistiques en streaming

- Welford pour moyenne, écart-type échantillon et SEM ;
- buffers loués/réutilisés pour les médianes ;
- traitements, normalisations et statistiques événementielles sans concaténations intermédiaires ;
- invalidation des dérivés dépendant des préférences d’agrégation.

Document détaillé : `implementation_etape_3_timeseries_2026-07-21.md`.

### Étape 4 — grilles temporelles

- navigation commune séparée de la grille discrète de projection ;
- politiques `Floor`, `Round` et `Interpolate`, avec interpolation par défaut ;
- projection mixte multi-fréquence évaluée en temps physique ;
- export NIfTI fondé sur la grille exacte de projection.

Document détaillé : `implementation_etape_4_timeseries_2026-07-21.md`.

### Étape 5 — préparation et buffers

- aplatissement temporel direct en une passe ;
- statistiques/limites calculées pendant le remplissage ;
- suppression de la concaténation dynamique persistante ;
- séries zéro partagées ;
- buffers UV natifs et Unity réutilisés.

Document détaillé : `implementation_etape_5_timeseries_2026-07-21.md`.

### Étape 6 — budget mémoire

- budget réel piloté par `MemoryCacheLimit` ;
- formule automatique à 90 % de la RAM avec au moins 2 Gio réservés ;
- catégories, priorité d’éviction et LRU ;
- données actives épinglées et dépassement averti sans downsampling ;
- réutilisation chaude/reconstruction froide du brut ;
- suppression de la collecte forcée sur le chemin normal.

Document détaillé : `implementation_etape_6_timeseries_2026-07-21.md`.

### Étape 7 — Trial Matrix

- limites en streaming ;
- pavage sur largeur et hauteur selon `SystemInfo.maxTextureSize` ;
- interpolation directe sans matrice lissée globale ;
- halos bilinéaires, coordonnées globales et événements inchangés ;
- `Color32`/`SetPixelData` ;
- textures réutilisées, détruites explicitement et comptabilisées.

Document détaillé : `implementation_etape_7_timeseries_2026-07-21.md`.

### Étape 8 — graphes

- abscisses régulières implicites ;
- tableaux d’ordonnées et SEM conservés par référence ;
- limites verticales en streaming ;
- matérialisation limitée au viewport et au niveau de zoom ;
- buffers ligne/SEM extensibles et réutilisés ;
- exports SVG/CSV complets par accès indexé.

Document détaillé : `implementation_etape_8_timeseries_2026-07-21.md`.

## Revue statique finale

Sur les chemins modifiés, la recherche ne trouve plus :

- de `new Vector2[values.Length]` pour les séries régulières ;
- de `Color[width * height]` ni `SetPixels` dans la Trial Matrix ;
- de lissage 2D global dans le rendu Trial Matrix ;
- de concaténation `ToArray().CalculateValueLimit()` pour les limites Trial Matrix/graphes ;
- de `GC.Collect()` dans `DataManager`.

Les occurrences restantes sont hors périmètre ou intentionnelles : captures/compositions d’images dans `Scene3DWindow`, données statiques 3D, localizers irréguliers et outil de debug de DLL.

## Réserves non bloquantes

1. La politique temporelle de l’étape 4 est sérialisée et utilisable par API/fichier, mais aucun nouveau dropdown dédié n’a été ajouté dans l’éditeur de configuration. La valeur par défaut `Interpolate` préserve le comportement attendu.
2. Le budget de l’étape 6 couvre les buffers dont HiBoP connaît la taille. Les allocations internes de Unity, du pilote GPU et de bibliothèques natives externes restent mesurables uniquement par profiler.
3. Toutes les tuiles Trial Matrix sont matérialisées pour conserver l’accès immédiat au protocole complet. La virtualisation visible reste conditionnée à une mesure produit démontrant un coût résiduel.
4. Le renderer de courbe tiers possède un chemin de secours allouant si son option d’augmentation de résolution est activée avec une capacité supérieure au nombre actif. Les prefabs HiBoP testés n’utilisent pas ce chemin.
5. Les comparaisons visuelles longues, les mesures GPU et les plateaux mémoire sur données réelles nécessitent les datasets/scènes produit et restent des validations manuelles recommandées dans chaque document d’étape.

Aucune de ces réserves ne bloque l’usage ou les tests actuels.

## État Git

Aucun commit n’a été créé, conformément à la demande. Les changements restent disponibles dans l’arbre de travail pour revue et mesures produit.
