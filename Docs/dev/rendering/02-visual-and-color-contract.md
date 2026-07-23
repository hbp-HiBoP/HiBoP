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

À valeur scientifique et alpha identiques, la couleur produite **DOIT** rester
constante :

- quelle que soit la normale de la surface ;
- quelle que soit l'orientation de la caméra ;
- quelle que soit la direction ou l'intensité de la lumière ;
- sur la surface du mesh et sur une coupe ;
- dans une vue à l'écran et dans son export PNG ;
- entre colonnes affichant le même état ;
- entre plateformes, dans la limite des tolérances définies plus bas.

### Composition recommandée

Le modèle cible est :

```text
anatomie = éclairage(anatomie_de_base)
résultat = lerp(anatomie, couleur_scientifique_linear, alpha_scientifique)
```

L'overlay est donc composité après l'éclairage de l'anatomie. Sa teinte ne
reçoit ni Lambert, ni spéculaire, ni ambient occlusion issue du matériau.

L'alpha peut laisser voir le relief anatomique par transparence, mais ne doit
pas moduler différemment la teinte selon la lumière. Toute option future de
modulation du relief devra être explicitement distincte, désactivée par défaut
et interdite pour les modes exigeant une couleur strictement constante.

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
- Les seuils positifs/négatifs et valeurs hors intervalle doivent avoir un
  comportement déterministe.
- L'interpolation temporelle ne doit pas introduire de conversion
  colorimétrique supplémentaire.
- La légende doit représenter le même mapping que le shader, pas une
  approximation indépendante.

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

La phase de parité doit caractériser le tri actuel. Une solution de type
transparent classique est acceptable au premier jalon. Une amélioration
ultérieure peut employer depth prepass, dither ou une autre technique, mais elle
doit être évaluée sur la lisibilité scientifique et sur la VR.

## 8. Contours, sélection et ROI

- Les contours sont une aide visuelle et peuvent être modernisés.
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
12. 1×1 vue, 8×3 vues et scénario de stress 12×5.

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

