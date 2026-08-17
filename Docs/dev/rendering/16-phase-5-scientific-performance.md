# Phase 5 — Validation scientifique et performances

## Statut

**État : Gate 5 validée sous Windows.**

La comparaison Built-in/URP a été réalisée avec le même collecteur, sur la
même machine, avec la même visualisation, les mêmes charges, les mêmes tailles
de RenderTexture et 900 frames mesurées après 120 frames de chauffe. La
validation visuelle finale de la DLL optimisée a été réalisée par le
responsable du rendu.

La validation macOS Apple Silicon/Metal et Linux/Vulkan relève de la phase 6.

## Protocole définitif

Machine : Windows 11, NVIDIA GeForce RTX 2070 SUPER, Direct3D 11, Unity
`6000.5.2f1`, qualité `Fantastic`, espace Linear.

Campagnes normatives :

- Built-in :
  `.test-results/rendering/baseline-birp-phase5/20260807-191903/manifest.json` ;
- URP :
  `.test-results/rendering/urp-phase5/20260807-211822/manifest.json`.

Tous les scénarios ont été enregistrés avec l'application focalisée au début
et à la fin, sans mode idle, sans avertissement du collecteur et avec
`Normative = true`.

`RenderingBaselineCapture` recharge la visualisation `Small` depuis
`visu_full_test` avant chaque campagne. Il mesure :

- opaque et transparent, Edges actif et inactif ;
- rotation continue de la caméra ;
- mise à jour temporelle de l'activité ;
- survol d'atlas ;
- déplacement continu d'une coupe ;
- 30 000 sites source dans une vue ;
- 9 colonnes × 3 vues ;
- allocations GC par frame ;
- mémoire Unity, mémoire réservée, mémoire graphique et RenderTextures vivantes.

Les tailles sont forcées uniquement pendant les mesures afin de comparer un
nombre de pixels strictement identique :

- `Small` : 3 × `348×516` = `538 704` pixels ;
- 30 000 sites : 1 × `348×516` = `179 568` pixels ;
- 9×3 : 27 × `112×200` = `604 800` pixels.

Hors campagne, chaque vue reprend exactement la taille physique de son
rectangle à l'écran, sans supersampling ni déformation de l'aspect.

## Comparaison Built-in / URP

Les valeurs ci-dessous proviennent des deux campagnes définitives. Les écarts
sont calculés sur le temps de frame ; une valeur négative signifie que l'URP
est plus rapide.

| Scénario | Frame médiane BIRP / URP | Écart | Frame P95 BIRP / URP | Écart P95 | CPU main médian BIRP / URP | GPU médian BIRP / URP |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Témoin, caméras 3D coupées | 3,249 / 2,850 ms | −12,3 % | 25,757 / 3,455 ms | −86,6 % | 1,881 / 1,946 ms | 0,233 / 0,516 ms |
| Opaque, Edges off | 4,546 / 3,872 ms | −14,8 % | 60,256 / 4,577 ms | −92,4 % | 2,476 / 2,905 ms | 0,694 / 0,625 ms |
| Opaque, Edges on | 4,493 / 4,040 ms | −10,1 % | 15,704 / 4,910 ms | −68,7 % | 2,870 / 3,064 ms | 0,732 / 0,915 ms |
| Transparent, Edges off | 3,975 / 3,859 ms | −2,9 % | 16,806 / 4,472 ms | −73,4 % | 2,462 / 2,888 ms | 0,399 / 0,627 ms |
| Transparent, Edges on | 4,473 / 4,104 ms | −8,2 % | 16,635 / 4,958 ms | −70,2 % | 2,874 / 3,122 ms | 0,658 / 0,922 ms |
| Rotation continue | 4,521 / 4,223 ms | −6,6 % | 17,374 / 5,183 ms | −70,2 % | 2,622 / 3,147 ms | 0,809 / 0,612 ms |
| Activité temporelle | 16,581 / 19,512 ms | +17,7 % | 20,053 / 20,785 ms | +3,7 % | 15,070 / 18,418 ms | 0,902 / 0,801 ms |
| Survol atlas | 4,717 / 4,605 ms | −2,4 % | 28,125 / 5,630 ms | −80,0 % | 6,499 / 6,556 ms | 0,860 / 0,849 ms |
| Déplacement de coupe | 13,308 / 14,355 ms | +7,9 % | 16,260 / 16,584 ms | +2,0 % | 12,294 / 13,521 ms | 6,503 / 1,926 ms |
| 30 000 sites, 1 vue | 38,075 / 17,573 ms | −53,8 % | 58,872 / 19,501 ms | −66,9 % | 26,876 / 16,346 ms | 25,359 / 11,268 ms |
| 9×3, 604 800 pixels | 10,129 / 10,011 ms | −1,2 % | 23,104 / 11,068 ms | −52,1 % | 5,091 / 8,893 ms | 1,283 / 4,548 ms |

Le cas courant statique est égal ou meilleur en temps de frame. Les sites,
priorité de performance de la migration, sont nettement plus rapides. Le cas
9×3 conserve une médiane équivalente et un P95 meilleur, malgré une charge CPU
et GPU supérieure due aux 27 caméras URP.

Deux charges dynamiques ont une médiane plus élevée : activité `+17,7 %` et
coupe `+7,9 %`. Elles ne constituent pas une régression P95 soutenue supérieure
à 10 % : leurs P95 respectifs sont `+3,7 %` et `+2,0 %`. Le rendu de l'activité
est principalement limité par le calcul CPU natif du volume, pas par le GPU
URP ; son GPU médian est même légèrement meilleur.

## Optimisation native de l'activité

Le lissage scientifiquement validé opère sur une grille volumique `80³`. Une
première DLL fonctionnelle effectuait les passes activité, poids et support
séparément, avec des allocations temporaires répétées. Sur la campagne
`20260807-201832`, le scénario activité atteignait `38,816 ms` au P95.

L'implémentation finale de `hbp_core` :

- réutilise trois buffers temporaires persistants ;
- traite activité, poids et support dans les mêmes parcours ;
- parcourt chaque axe selon un ordre mémoire contigu ;
- conserve les noyaux, le nombre de passes, l'ordre des opérations flottantes
  et le résultat visuel.

Le P95 activité tombe ainsi de `38,816 ms` à `20,785 ms`, soit une réduction de
46,5 %, et revient à `+3,7 %` du Built-in.

Le support ne peut pas être mis en cache globalement : pour l'activité
volumique, sa présence dépend de la timeline et des seuils masqués. Une
spécialisation propre à l'iEEG ajouterait de la complexité sans gain démontré ;
elle est donc volontairement écartée de cette migration.

## Allocations et mémoire

Le témoin URP mesure `10 513` octets de GC par frame. Les vues statiques opaque
et transparentes mesurent exactement la même médiane : le rendu 3D statique
n'ajoute donc aucune allocation récurrente. Les allocations communes viennent
de la boucle application/éditeur et du collecteur.

Après fermeture du fixture 9×3 et rechargement de `Small` :

- les RenderTextures globales reviennent de 51 à 21 ;
- les RenderTextures possédées par les vues HiBoP reviennent de 27 à 3 ;
- les trois caméras de `Small` possèdent chacune une unique cible ;
- la mémoire graphique revient de 354,6 Mo à 318,7 Mo, sous les 322,3 Mo
  observés avant le cas haut.

La mémoire réservée Unity est un high-water mark et n'est pas censée revenir
immédiatement à sa valeur initiale. Les ressources graphiques possédées par
HiBoP, elles, sont bien libérées.

## Contrôles scientifiques et fonctionnels

La campagne produit 47 captures couvrant cerveau, activité, sites, atlas,
coupes, transparence, Edges, ROI, export individuel, export composite, export
vidéo et interface complète. Les exports PNG individuels conservent un fond
alpha nul.

La validation humaine couvre :

- lecture du volume, des sillons et des couleurs scientifiques ;
- continuité activité mesh/coupes ;
- masque IRM et lissage limité aux bords de l'activité ;
- silhouette transparente ;
- Edges limités au cerveau et aux coupes ;
- cage analytique des ROI et états de sélection ;
- exports PNG et vidéo ;
- rendu final avec la DLL native optimisée.

Validation automatique finale :

- `HBP.Rendering.Tests` : 79/79 ;
- `HBP.Module3D.PlayModeTests` : 43/43 ;
- tests natifs `hbp_core` : 13/13 ;
- contrat ABI Windows : 212 symboles ;
- dépendances natives inspectées avec succès ;
- zéro avertissement dans les deux manifestes normatifs.

Une `MissingReferenceException` isolée a été observée après l'arrêt manuel du
Play Mode : une mise à jour asynchrone de collider a repris pendant la
destruction de la scène. Elle n'est pas apparue pendant la campagne, ne se
répète pas une fois l'éditeur revenu au repos et n'affecte ni les résultats ni
le produit exécuté. Elle relève du cycle de vie général de `Base3DScene`, pas du
pipeline de rendu.

## Décision Gate 5

La Gate 5 est validée sous Windows :

- aucun défaut scientifique ou fonctionnel de rendu n'est ouvert ;
- la validation humaine finale est acquise ;
- le cas courant est égal ou meilleur en temps de frame ;
- aucune régression P95 soutenue ne dépasse 10 % ;
- les écarts dynamiques sont mesurés, expliqués et acceptés ;
- le rendu statique n'ajoute pas d'allocation GC récurrente ;
- la mémoire des vues revient à son plateau après le cas 9×3.

Les phases 6 et 7 restent reportées conformément à la décision de projet.
