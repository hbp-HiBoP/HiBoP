# Roadmap de migration

## 1. Principe général

La migration doit rester réversible et séparer trois types de changement :

1. changement de pipeline ;
2. correction du contrat scientifique ;
3. optimisation ou modernisation.

Mélanger ces trois dimensions rendrait tout écart visuel difficile à diagnostiquer.
Chaque lot doit donc produire une image et des mesures comparables avant de
passer au suivant.

## 2. Organisation Git recommandée

- Créer une branche dédiée préfixée `codex/`, à partir d'un état Built-in
  reproductible.
- Ne pas lancer le Render Pipeline Converter sur la branche principale.
- Faire des commits par sous-système : infrastructure URP, cerveau, coupes,
  contours, sites, export, validation.
- Conserver un tag ou commit de baseline Built-in ouvrable sous la même version
  Unity jusqu'à validation finale.
- Ne pas combiner la migration avec un déplacement massif de dossiers,
  namespaces ou asmdef.

## 3. Étape 0 — Baseline Built-in

### Objectif

Transformer le rendu actuel en référence mesurable avant toute modification.

### Travaux

- Créer les scènes/configurations de référence listées dans
  `05-validation-and-reference-captures.md`.
- Capturer affichage et exports.
- Enregistrer les paramètres de lumière, caméra, matériau, thème et qualité.
- Mesurer CPU main thread, render thread, GPU, batches, draw calls, mémoire
  RenderTexture et allocations.
- Capturer les scénarios 1×1, 8×3 et, si praticable, 12×5.
- Capturer 30 000 sites, y compris sélection et changement d'activité.
- Ajouter des tests de patchs sRGB/Linear sur texture, vertex color et couleur
  uniforme.

### Gate 0

On ne commence pas la bascule tant que :

- au moins un cas anatomie, atlas, activité, coupe, transparence, contours,
  sites et export est reproductible ;
- les captures sont nommées et archivées ;
- le GPU de référence et la résolution sont documentés ;
- l'écart mesh/coupe actuel a été mesuré, pas seulement observé.

## 4. Étape 1 — Squelette URP

### Objectif

Faire démarrer le projet sous URP sans prétendre à la parité visuelle.

### Travaux

- Ajouter la version URP compatible avec Unity 6000.5.2f1.
- Créer les URP Asset et Universal Renderer Data.
- Nettoyer les références de pipeline absentes dans les niveaux de qualité.
- Configurer Forward, Linear, LDR, main light et ombres initiales.
- Convertir uniquement les matériaux standards simples avec le converter, sur
  une copie/branche contrôlée.
- Recenser les matériaux magenta et shaders non supportés.
- Retirer progressivement la dépendance runtime à PPv2 sans encore supprimer le
  package si d'autres usages existent.

### Gate 1

- Le projet compile sans erreur.
- Les scènes principales s'ouvrent.
- Aucun asset critique n'a perdu ses références.
- La liste des shaders/materials encore non migrés est exhaustive.
- Les niveaux de qualité pointent vers des assets valides.

## 5. Étape 2 — Parité technique du cerveau et des coupes

### Objectif

Reproduire toutes les fonctionnalités shader avant d'améliorer leur sens
colorimétrique.

### Ordre

1. anatomie opaque ;
2. extrusion ;
3. clipping jusqu'à 20 plans ;
4. passes depth/normals/shadows ;
5. coupes opaques ;
6. transparence cerveau/coupes ;
7. atlas ;
8. activité iEEG/fMRI/MEG.

### Gate 2

- Aucun mode ne produit de matériau magenta.
- Les plans de coupe coïncident entre couleur, profondeur, normals et ombre.
- Les UV et textures de tous les modes sont corrects.
- Le rendu transparent ne masque pas les sites/coupes attendus.
- Les mêmes configurations se chargent sans migration de données utilisateur.
- Les captures de parité sont acceptées ou leurs écarts sont consignés.

## 6. Étape 3 — Caméras, contours et export

### Objectif

Rétablir le rendu complet de chaque vue.

### Travaux

- Créer le gestionnaire explicite de RenderTexture.
- Reproduire fond, projection, aspect, clear flags et culling masks.
- Implémenter la Renderer Feature de contours.
- Remplacer les références `PostProcessLayer`/PPv2 dans les prefabs concernés.
- Porter le ROI et documenter le fallback sans geometry shader.
- Porter les exports individuels transparents et composites opaques.

### Gate 3

- 24 vues peuvent être créées, redimensionnées et fermées sans croissance
  persistante de mémoire GPU.
- Les contours fonctionnent sur toutes les vues sans fuite d'état entre caméras.
- L'export individuel a un alpha de fond nul.
- Affichage et export passent les comparaisons prévues.
- Les prefabs portent toutes les références nécessaires.

## 7. Étape 4 — Contrat scientifique

### Objectif

Faire de la constance scientifique la vérité de production, même si elle
diffère volontairement de la baseline historique.

### Travaux

- Déclarer l'espace source de chaque palette.
- Corriger les conversions sRGB/Linear.
- Composer les overlays après l'éclairage anatomique.
- Unifier atlas mesh/coupes/légendes.
- Unifier fMRI, iEEG, MEG et autres mappings.
- Ajouter les tests de patchs et invariance à la lumière.
- Refaire les captures de référence qui deviennent la nouvelle baseline.

### Gate 4

- Modifier la direction/intensité de la lumière ne modifie pas le RGB d'un patch
  scientifique opaque.
- La même région/valeur donne le même RGB sur mesh et coupe.
- Les légendes et exports utilisent le même mapping.
- Le responsable humain valide les gammes de couleurs.

Cette gate marque la cible fonctionnelle recommandée pour une première release
URP.

## 8. Étape 5 — Sites

### Objectif

Conserver d'abord les performances, puis éliminer les coûts structuraux
dominants.

### Travaux

- Port minimal du shader.
- Baseline Built-in/URP avec mêmes objets, matériaux et caméras.
- Profilage du nombre de renderers, colliders, matériaux, batches et callbacks.
- Prototype d'une seule optimisation à la fois :
  - cache/réduction des matériaux ;
  - instancing ;
  - culling ;
  - picking data-oriented ;
  - remplacement des GameObjects individuels.
- Test fonctionnel de sélection, filtrage, surbrillance et activité.

### Gate 5

- Le chemin URP minimal n'est pas significativement plus lent que Built-in sur
  la machine de référence, ou l'écart est expliqué et accepté.
- Le scénario 30 000 sites reste interactif selon le budget calibré.
- Toute nouvelle architecture dispose d'un fallback pour les plateformes qui ne
  supportent pas ses primitives GPU.

## 9. Étape 6 — Optimisations atlas, coupes et multi-vues

### Candidats, dans l'ordre de preuve

1. ne pas rendre les vues invisibles/minimisées ;
2. réutiliser les RenderTextures ;
3. éviter les invalidations de coupe lors d'un simple survol ;
4. palette/identifiants atlas GPU ;
5. réduire les copies `GetPixels32`/`SetPixels32` ;
6. rendu à la demande des vues statiques ;
7. réglage ombres, profondeur, normals et MSAA par profil.

Chaque optimisation doit avoir un benchmark avant/après et un test de
non-régression visuelle.

## 10. Étape 7 — Plateformes

### Desktop

- Windows sur Intel iGPU de référence ;
- macOS Intel et/ou Apple Silicon à préciser ;
- Linux avec GPU/API de référence.

### VR

- Fixer casque, runtime, API et mode stéréo ;
- tester latence, confort, transparence, contours et overlays ;
- mesurer par œil et avec le mode single-pass retenu.

### WebGL

Effectuer un prototype de faisabilité seulement après décision produit. Les
points bloquants potentiels sont le geometry shader ROI, les buffers/indirect
draw des sites, les plugins natifs et la mémoire. WebGL ne doit pas être déclaré
supporté sur la seule base d'une compilation.

## 11. Définition de fini globale

La migration est terminée lorsque :

- toutes les gates applicables sont passées ;
- le contrat visuel est couvert par tests et validation humaine ;
- Windows, macOS, Linux et la cible VR définie passent la matrice ;
- les exports sont validés ;
- aucune dépendance active à PPv2 ne subsiste ;
- les assets Built-in devenus inutiles sont supprimés dans un changement séparé
  et vérifiable ;
- la documentation reflète l'implémentation réelle ;
- un rollback vers la baseline reste possible jusqu'à la release.

## 12. Estimation et incertitude

Une première estimation raisonnable, pour une personne familière du projet :

- prototype cerveau + URP : 1 à 2 semaines ;
- migration desktop fonctionnelle : 4 à 7 semaines ;
- optimisation sites, VR, plateformes et durcissement : variable et
  potentiellement du même ordre.

Ces chiffres ne sont pas un engagement. Les principaux facteurs d'incertitude
sont les 30 000 sites, le tri transparent, les plateformes VR/WebGL et le nombre
de configurations scientifiques à valider.

