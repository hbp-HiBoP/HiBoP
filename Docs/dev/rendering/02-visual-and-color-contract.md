# Contrat visuel et colorimétrique

## 1. Objet

Ce document définit le rendu attendu indépendamment de son implémentation. Il
est normatif : si l'ancienne image, un shader existant et ce contrat sont en
désaccord, l'ordre de priorité indiqué dans `README.md` s'applique.

Les termes **DOIT**, **NE DOIT PAS**, **DEVRAIT** et **PEUT** expriment le niveau
d'exigence.

## 2. Séparation sémantique du rendu

Le rendu de HiBoP doit distinguer trois familles :

1. **Anatomie** : forme et relief du cerveau, coupes anatomiques, contexte
   spatial. L'éclairage peut modifier sa luminance.
2. **Données scientifiques** : atlas, fMRI, iEEG, MEG, activité et toute
   projection colorée représentant une valeur ou une classe. La couleur est une
   donnée.
3. **Aides visuelles** : contours, sélection, wireframe ROI, survol, fond,
   transparence de présentation.

Un effet appliqué à l'anatomie ne doit pas modifier implicitement une donnée
scientifique.

## 3. Espace colorimétrique

- Le projet **DOIT** rester en espace Linear.
- Toute couleur de palette ou de configuration **DOIT** avoir un espace source
  documenté.
- Sauf décision contraire, les couleurs saisies par l'utilisateur, stockées
  comme couleurs d'interface ou publiées dans une palette de référence sont
  interprétées comme des valeurs **sRGB**.
- La conversion sRGB vers Linear **DOIT** être effectuée une seule fois avant
  le calcul de composition GPU.
- Une texture contenant des couleurs scientifiques et un vertex color portant
  la même couleur **DOIVENT** produire le même résultat à l'écran.
- Les textures de données scalaires, masques, indices de région, profondeur et
  valeurs d'activité **NE DOIVENT PAS** être importées ou créées comme textures
  sRGB.
- Les textures qui contiennent directement une image colorée sRGB **DOIVENT**
  être déclarées comme telles.

Le choix exact des formats (`UNorm`, `sRGB`, float, texture d'indices) sera
précisé lors du prototype par des tests de patchs connus.

## 4. Couleurs scientifiques

### Règle principale

À valeur scientifique et alpha identiques, la teinte et la chroma issues de la
palette **DOIVENT** rester stables. La surface du mesh **PEUT** moduler leur
luminance de façon contrôlée pour rendre le relief 3D perceptible ; la coupe
et la légende restent les références non éclairées. Cette règle vaut :

- quelle que soit la normale de la surface ou l'orientation de la caméra, hors
  modulation contrôlée de luminance ;
- quelle que soit la direction ou l'intensité des lumières de scène, qui ne
  pilotent pas les données scientifiques ;
- sur la surface du mesh, une coupe et la légende, avec le même mapping de
  palette et la même composition d'alpha ;
- dans une vue à l'écran et dans son export PNG ;
- entre colonnes affichant le même état ;
- entre plateformes, dans la limite des tolérances définies plus bas.

### Composition recommandée

Le modèle cible est :

```text
alpha_palette = 1 - (1 - alpha_scientifique)²
anatomie_mesh = éclairage_anatomique(anatomie_de_base)
science_mesh = couleur_scientifique_linear × relief_luminance_contrôlé
mesh = lerp(anatomie_mesh, science_mesh, alpha_palette)
coupe = lerp(anatomie_coupe, couleur_scientifique_linear, alpha_palette)
```

L'anatomie et la donnée scientifique sont éclairées séparément avant leur
composition. L'overlay ne reçoit aucun spéculaire blanc additif : son relief
est une modulation multiplicative de sa propre couleur et indépendante des
lumières de scène. Les ombres suivent librement l'ambient du matériau afin de
conserver la profondeur ; les hautes lumières diffuses restent bornées et ne
doivent pas écrêter un canal. Un reflet neutre très localisé peut légèrement
désaturer son sommet afin de donner à l'activité la même nature de surface que
l'anatomie, sans modifier la couleur diffuse scientifique.

La courbe d'alpha conserve exactement les extrémités 0 et 1, mais rapproche les
zones actives de la palette : l'alpha usuel `0,8` devient un poids perceptuel
`0,96`. Le relief modifie la luminance, jamais le mapping valeur -> palette ni
la teinte par mélange avec un reflet neutre.

### Atlas

- Une région d'atlas **DOIT** utiliser exactement la même palette sur mesh,
  coupes, légendes et exports.
- Le survol ou la sélection **PEUT** changer alpha, bordure ou indication
  dédiée, mais ne doit pas remplacer silencieusement la couleur scientifique.
- Si une couleur de sélection est utilisée, son caractère non scientifique
  **DOIT** être identifiable et réversible.

### fMRI, iEEG et autres activités

- La normalisation valeur -> position dans la colormap doit être partagée ou
  couverte par les mêmes tests pour mesh, coupe et légende.
- Le lissage spatial éventuel **DOIT** s'appliquer aux valeurs scientifiques et
  à leurs poids avant la LUT, dans un champ volumique commun au mesh et aux
  coupes. Flouter séparément leurs images RGB après composition est interdit.
- Le mode continu de référence utilise deux passes d'un noyau binomial
  séparable à cinq échantillons par axe, puis une interpolation trilinéaire
  pondérée. Le résultat est mis en cache par instant afin que chaque coupe
  réutilise le calcul du mesh. Le mode nearest reste disponible comme référence
  discrète non lissée.
- Le plancher d'opacité ne doit pas transformer toute valeur non nulle en bord
  opaque. Un champ de support lissé séparément module ce plancher à la frontière
  de l'activité, avec exactement le même échantillonnage sur le mesh et les
  coupes. Une seule passe de rayon deux crée une bande compacte autour de la
  frontière : les deux couches intérieures sont progressivement atténuées et le
  halo peut s'étendre sur au plus deux cellules extérieures. L'intérieur plus
  profond reste pleinement opaque et le support retombe à zéro au-delà de cette
  bande. Ce lissage ne modifie pas la position dans la colormap.
- Sur le mesh cérébral, l'éligibilité d'un sommet à l'activité dépend du champ
  d'activité, pas de la valeur du voxel MRI le plus proche. Le mesh définit déjà
  le domaine anatomique ; utiliser l'intensité MRI comme second masque crée des
  trous triangulaires artificiels.
- Sur une coupe, les faibles intensités jusqu'au plus strict du seuil
  utilisateur et du minimum tissulaire robuste définissent des candidats de
  fond. Lorsque ce minimum se confond avec le minimum brut, le seuil candidat
  retombe à `5 %` de la plage jusqu'au maximum robuste recalculé. Seuls les
  candidats reliés aux bords de la coupe par une connexité à huit voisins sont
  considérés comme extérieurs ; une faible intensité enfermée dans l'anatomie
  reste donc coloriable. Les valeurs non finies restent toujours exclues.
  Le masque intérieur binaire est flouté séparément du RGB, puis multiplié par
  sa version brute après projection afin d'adoucir la frontière sans recréer de
  halo dans l'extérieur.
- Les seuils positifs/négatifs et valeurs hors intervalle doivent avoir un
  comportement déterministe.
- L'interpolation temporelle ne doit pas introduire de conversion
  colorimétrique supplémentaire.
- La légende doit représenter le même mapping que le shader, pas une
  approximation indépendante.

### Préférences de projection de l'activité

- `Mask activity on MRI background`, activé par défaut, limite l'activité des
  coupes au support anatomique calculé depuis l'IRM. Cette option ne concerne
  pas le mesh, dont la géométrie définit déjà le domaine anatomique.
- `Smooth activity boundaries`, activé par défaut, adoucit le support alpha à
  la frontière du volume d'activité sur le mesh et les coupes afin de masquer
  la structure des voxels. Son action est limitée à une bande de deux cellules
  de part et d'autre de la frontière, ce qui autorise un halo extérieur compact
  sans atténuer tout le volume. Désactivé, le support est binaire (« voxel
  exact »), sans changer les valeurs scientifiques, la colormap ou leur
  interpolation.
- Les deux préférences sont indépendantes. Leur désactivation ne modifie pas
  le mapping valeur -> palette.

## 5. Anatomie

Pendant la première livraison, l'anatomie **DEVRAIT** conserver :

- la gamme globale de gris et de luminance du rendu actuel ;
- la lisibilité des sillons ;
- un éclairage caméra-relatif proche du comportement historique ;
- un niveau de spéculaire discret ;
- les plans de clipping et l'extrusion existants.

La reproduction n'est pas pixel perfect. Les différences acceptables concernent
principalement l'anti-aliasing, la reconstruction des normales, les contours et
de petites variations du modèle d'éclairage. Une dérive globale de contraste,
de saturation ou de température de couleur n'est pas acceptable sans
validation.

## 6. Coupes

- Une coupe anatomique **DOIT** rester lisible et cohérente avec le volume.
- Une couleur scientifique sur coupe **DOIT** respecter exactement le même
  contrat qu'une couleur scientifique sur mesh.
- Les bords de coupe ne doivent pas présenter de halo lié à une divergence
  entre pass Forward, depth et normals.
- Le filtrage des textures de coupe doit être explicitement choisi. Une texture
  d'indices d'atlas doit utiliser un filtrage point ; une texture anatomique ou
  continue peut utiliser un filtrage bilinéaire si cela correspond au besoin.

## 7. Transparence

La transparence peut être modernisée, sous réserve de préserver :

- le contrôle d'alpha ;
- la perception de la géométrie interne utile ;
- la visibilité des sites et des coupes ;
- le clipping ;
- l'export.

La cible utilise un transparent classique avec `ZWrite Off` et `Cull Back`.
Sites, coupes et ROI doivent rester lisibles à travers le cerveau. L'ordre exact
et les artefacts de superposition du Built-in ne sont pas normatifs.

## 8. Contours, sélection et ROI

- Les contours sont une aide visuelle et peuvent être modernisés.
- Ils concernent uniquement le cerveau et les coupes.
- Ils utilisent profondeur/normales sur les objets opaques et uniquement la
  silhouette extérieure sur les objets transparents.
- Ils ne doivent pas modifier les pixels internes d'une zone scientifique.
- Leur épaisseur doit rester visuellement stable entre les résolutions usuelles.
- Ils doivent pouvoir être désactivés.
- Les indications de sélection/survol doivent rester distinctes des palettes de
  données.
- Le wireframe ROI doit conserver son sens spatial ; son implémentation peut
  changer selon la plateforme.

## 9. Fond et export

### Affichage

Le fond 3D par défaut reste `#282828`, sauf thème ou préférence explicitement
appliqué.

### Export PNG individuel

- Le RGB de la scène **DOIT** correspondre à la vue affichée pour le même état,
  la même caméra, la même résolution logique et les mêmes réglages.
- Le fond **DOIT** être transparent.
- L'alpha du fond **DOIT** être zéro.
- Les objets transparents doivent produire un alpha cohérent et ne pas être
  prémultipliés deux fois.
- Le PNG final **DOIT** être en straight alpha, sans halo sombre lorsqu'il est
  recomposé sur un fond clair ou foncé.
- Les Edges **DOIVENT** suivre l'état de la vue et ne jamais rendre le fond
  transparent noir ou opaque.
- Aucun tone mapping ou color grading spécifique à l'export n'est autorisé.

### Export composite

Les composites existants utilisant un fond `#282828` doivent conserver ce
comportement, sauf évolution fonctionnelle séparée.

## 10. Tolérances d'acceptation

Les seuils ci-dessous sont des objectifs initiaux à calibrer sur les captures de
référence :

- patchs de palette sans transparence : écart maximal de 1 unité par canal
  8 bits dans le même environnement ;
- mesh vs coupe pour une couleur scientifique plate : même objectif après
  échantillonnage hors bords ;
- export vs vue offscreen équivalente : égalité par canal hors pixels affectés
  par l'anti-aliasing ;
- comparaison Built-in vs URP de l'anatomie : validation perceptuelle humaine,
  histogrammes et métriques d'image comme aides, jamais comme seul verdict ;
- cross-platform : cible de `ΔE00 <= 2` sur patchs scientifiques, avec
  investigation obligatoire au-delà. Le pipeline de capture doit éviter la
  gestion de couleur du système d'exploitation lors de la mesure.

## 11. Cas de référence minimaux

Le corpus doit contenir au moins :

1. anatomie opaque sans overlay ;
2. anatomie transparente ;
3. atlas avec plusieurs régions saturées et désaturées ;
4. atlas avec région survolée puis sélectionnée ;
5. fMRI positif, négatif, seuil et valeur nulle ;
6. activité iEEG à plusieurs alphas et deux instants interpolés ;
7. une et plusieurs coupes, fortes et normales ;
8. sites : 1, charge usuelle et stress 30 000 ;
9. ROI normal et sélectionné ;
10. contours actifs/inactifs ;
11. export transparent individuel et export composite ;
12. 1×1 vue, 8×3 vues et 9×3 vues ; le cas combiné extrême est un test de
    robustesse sans objectif de fluidité.

## 12. Validation humaine obligatoire

Codex peut automatiser les captures, mesures et comparaisons. Un humain doit
valider :

- la lisibilité anatomique ;
- le caractère non ambigu des couleurs scientifiques ;
- la transparence et la profondeur perçue ;
- la qualité des contours ;
- les écarts visuels jugés acceptables entre Built-in et URP.

Chaque validation humaine doit consigner scène, configuration, capture, verdict
et commentaire, même lorsque le verdict est « accepté sans réserve ».
