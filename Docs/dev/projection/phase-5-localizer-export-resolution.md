# Phase 5 — Résolution d'export Localizer indépendante

## Statut

Implémentée et validée le 13 août 2026.

## Résultat

L'export Localizer ne lit plus `ActivityProjectionSettings`. La fenêtre possède
désormais son propre réglage de grille, encapsulé par
`LocalizerExportGridSettings`, et transmet directement sa dimension maximale et
son interpolation à une `ActivityProjectionGrid` dédiée à l'export.

La livraison minimale prévue dans le plan a été retenue :

- un champ entier « Maximum grid dimension » dans le prefab de la fenêtre ;
- une valeur par défaut de 80, indépendante de la résolution interactive ;
- une interpolation d'export trilineaire explicite et indépendante ;
- une validation comprise entre 2 et 512 ;
- un aperçu des dimensions, du nombre de voxels, du poids non compressé par
  instant temporel et du masque ;
- une confirmation au-delà de 8 000 000 voxels.

Le choix n'est volontairement pas mémorisé entre deux ouvertures de fenêtre.
Cette possibilité était optionnelle dans le plan et aurait nécessité un nouveau
contrat de préférences sans améliorer le découplage recherché.

## Calcul annoncé

`Volume.Dimensions` expose maintenant l'ABI native existante
`hbp_volume_get_dimensions`. `LocalizerExportGridSettings.CalculateDimensions`
reproduit la règle native de `ActivityProjectionGrid` pour chaque axe :

```text
dimensionAxe = max(2, truncate(dimensionMax * dimensionVolumeAxe / dimensionVolumeMax))
```

L'aperçu ne construit donc aucune grille native et son coût reste constant. Il
ne s'exécute que lorsque la fenêtre est initialisée ou que la saisie change.

Avec l'IRM MNI fournie par HiBoP (`208 × 256 × 219`), la valeur historique 80
annonce et produit `65 × 80 × 68`, soit 353 600 voxels. L'activité représente
environ 1,35 MiB non compressé par instant et le masque environ 0,34 MiB. Les
fichiers `.nii.gz` peuvent être plus petits ; leur taux de compression ne peut
pas être prédit avant le calcul.

La confirmation de grande taille dépend uniquement du nombre de voxels
spatiaux. Le message précise que la mémoire et la durée augmentent ensuite avec
le nombre d'instants, qui n'est connu qu'après le chargement des données.

## Parcours utilisateur et sécurité

- Une saisie vide, non entière ou hors de l'intervalle `[2, 512]` affiche une
  erreur sous le champ et désactive le bouton d'export.
- La valeur est revalidée dans `OK()` avant toute fermeture de visualisation ou
  tout lancement du chargement.
- Une grille dépassant 8 000 000 voxels demande une confirmation avant de
  fermer d'éventuelles visualisations ouvertes.
- Les dimensions et estimations restent visibles avant le lancement.
- Le champ et le texte d'aperçu sont créés et référencés dans le prefab ; aucun
  objet UI de remplacement n'est créé au runtime.

## Indépendance obtenue

La grille Localizer est initialisée avec :

```text
m_ExportGridSettings.MaximumDimension
m_ExportGridSettings.Interpolation
```

Elle ne dépend plus de :

```text
ActivityProjectionSettings.VolumeGridDimension
ActivityProjectionSettings.VolumeInterpolation
```

Modifier la résolution interactive ne change donc plus l'export. Modifier le
champ de la fenêtre d'export ne déclenche aucun événement de réglage de la
visualisation et n'invalide aucune activité affichée.

## Validation automatisée

- `HBP.Serialization.Tests` en EditMode : 479 tests réussis, 0 échec ;
- test PlayMode ciblé de `ExportLocalizerAtlasWindow` : 1 test réussi, 0 échec ;
- le test NIfTI iEEG initialise la grille avec
  `LocalizerExportGridSettings`, compare les dimensions annoncées à celles de
  la grille native, puis à celles des volumes activité et masque relus depuis
  les fichiers produits ;
- les tests de réglages couvrent le défaut historique, l'indépendance vis-à-vis
  de `ActivityProjectionSettings`, l'anisotropie, le seuil de confirmation et
  les bornes invalides ;
- le test PlayMode couvre les références sérialisées du prefab, le défaut à 80,
  l'indépendance avec une résolution interactive forcée à 96, la désactivation
  sur une valeur invalide et la réactivation sur une valeur valide.

L'inventaire managé passe de 258 à 259 imports, et de 204 à 205 imports
`hbp_core`, uniquement parce que le wrapper utilise maintenant le symbole déjà
présent `hbp_volume_get_dimensions`. Aucun changement natif ni nouvelle DLL
n'ont été nécessaires pour cette phase.

## Gate de sortie

- Résolutions interactive et Localizer indépendantes : satisfaite.
- Dimensions annoncées identiques à la grille et aux fichiers relus : satisfaite.
- Erreurs de saisie bloquées avant calcul : satisfaite.
- Défaut historique de 80 et estimation de poids explicite : satisfaite.
