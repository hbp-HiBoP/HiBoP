# Implémentation de l’étape 8 — graphes frugaux et buffers réutilisables

Date : 21 juillet 2026

## Résultat

Les séries temporelles régulières iEEG, CCEP et MEG ne matérialisent plus une abscisse dans un `Vector2` pour chaque échantillon. `CurveData` peut maintenant conserver :

- le tableau canonique des ordonnées ;
- l’abscisse de départ ;
- le pas régulier calculé depuis les bornes et le nombre d’échantillons.

`GetPoint(index)` reconstruit exactement le point à la demande, sans allocation. Les courbes irrégulières — notamment les localizers — conservent leur représentation explicite historique. L’accesseur `Points` reste disponible pour compatibilité et ne matérialise une courbe régulière que si un consommateur externe le demande explicitement ; les renderers, calculs de limites et exports internes utilisent tous l’accès indexé.

Les créations de courbes depuis iEEG/CCEP/MEG utilisent désormais `CreateRegular`. Les tableaux d’ordonnées et de SEM déjà calculés sont conservés par référence, sans copie supplémentaire. Les limites verticales des graphes sont calculées en streaming, sans liste ni concaténation globale.

Le renderer :

- détermine le sous-intervalle visible avant toute matérialisation ;
- conserve la règle de sous-échantillonnage existante en fonction du viewport et du zoom ;
- remplit seulement les points effectivement remis au renderer ;
- réutilise des buffers dont la capacité croît par puissances de deux ;
- transmet séparément capacité et nombre de points actifs aux renderers de ligne et de SEM ;
- réutilise de la même façon les épaisseurs SEM ;
- vide explicitement le maillage lorsque moins de deux points sont visibles.

Aucune décimation min/max ni nouvelle règle d’agrégation n’a été introduite. Les exports SVG/CSV parcourent tous les points scientifiques par index et restent complets, indépendamment du sous-échantillonnage visuel.

## Tests automatisés

`Stage8CurveDataTests` couvre huit scénarios :

- abscisses implicites exactes aux bornes et aux positions intermédiaires ;
- absence de matérialisation lors des lectures indexées ;
- conservation du tableau canonique d’ordonnées par référence ;
- absence de seconde copie pour une courbe explicite déjà fournie comme tableau ;
- matérialisation de compatibilité exacte et mise en cache unique ;
- série à un seul échantillon ;
- valeurs et SEM régulières conservées sans copie ;
- indices invalides ;
- **zéro octet alloué sur 10 000 lectures indexées** après initialisation.

Validation ciblée EditMode : **8 tests réussis sur 8**.

La fixture produit `InformationGraphPlayModeTests` a ensuite exercé les graphes, Trial Matrix, sélections d’essais, limites personnalisées et panneaux associés avec un périphérique graphique réel : **18 tests réussis sur 18**.

Validation finale cumulée : **355/355** dans `HBP.Serialization.Tests` et **15/15** dans `HBP.ProjectWorkflow.Tests`.

## Tests manuels conseillés

1. comparer iEEG/CCEP moyen, essai unique et SEM à chaque niveau de zoom ;
2. vérifier les points juste avant/après les bords du viewport et les sélections d’essais ;
3. afficher un MEG continu long, zoomer/dézoomer en boucle et surveiller `GC.Alloc` ;
4. comparer les exports SVG/CSV échantillon par échantillon à la version de référence ;
5. vérifier les courbes irrégulières de localizers ;
6. profiler plusieurs changements de limites : après croissance initiale, les buffers de points et de SEM doivent rester stables.

## Gain attendu

L’ancien chemin allouait un premier `Vector2[]` dans le producteur, puis `CurveData.Init` en créait une seconde copie. Cela représentait **16 octets par échantillon au pic**, dont 8 octets persistants, en plus du signal `float[]`.

La représentation régulière n’ajoute plus de tableau par échantillon. Pour une courbe d’un million d’échantillons, l’économie est donc d’environ :

- **7,6 Mio de mémoire persistante** ;
- **15,3 Mio au pic de construction**.

Pour une courbe avec SEM, la copie supplémentaire du tableau de formes, soit environ 3,8 Mio par million d’échantillons, est également supprimée. Les buffers de viewport restent proportionnels aux points réellement rendus et cessent d’être réalloués à taille stable.

## Limites et blocages

Aucun blocage. Le renderer tiers conserve une allocation de secours si son option interne d’augmentation de résolution est activée avec un buffer dont la capacité dépasse le nombre actif. Le chemin HiBoP normal utilise le mode de résolution existant sans cette transformation ; ce cas doit seulement être re-mesuré si cette option est activée dans un prefab futur.
