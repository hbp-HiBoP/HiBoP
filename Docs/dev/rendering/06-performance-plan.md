# Plan de performance

## 1. Objectif

La migration doit au minimum ne pas dégrader l'usage courant et doit préserver
un fonctionnement acceptable sur un chipset Intel intégré. L'absence historique
de problème GPU ne prouve pas que le GPU est le seul budget disponible : avec
30 000 sites et jusqu'à 60 vues, le main thread, le render thread, le culling,
la mémoire et les allocations peuvent dominer.

## 2. Machines de référence

Définir au minimum :

- **Low desktop** : chipset Intel intégré représentatif, mémoire et CPU
  documentés ;
- **Typical desktop** : machine courante de développement/utilisateur ;
- **macOS** : Intel ou Apple Silicon selon parc réel ;
- **Linux** : machine/API réelle ;
- **VR** : machine + casque + runtime.

Ne pas choisir le low-end à partir du seul nom « Intel intégré ». La génération,
le driver et la résolution changent fortement les performances.

## 3. Scénarios

| Niveau | Colonnes × vues | Sites | Usage |
| --- | ---: | ---: | --- |
| Minimal | 1 × 1 | petit jeu | diagnostic |
| Courant | configuration réelle médiane | réel | régression utilisateur |
| Réaliste haut | 8 × 3 | élevé | cible principale |
| Sites stress | 1 × 1 puis 8 × 3 | 30 000 | isolation + système |
| Théorique | 12 × 5 | élevé | robustesse, pas nécessairement 60 FPS |

Chaque scénario doit être testé :

- statique ;
- rotation/zoom caméra ;
- activité temporelle ;
- changement atlas/hover ;
- ajout/déplacement de coupe ;
- redimensionnement ;
- contours on/off ;
- transparence on/off.

## 4. Métriques

Collecter :

- frame time CPU main thread, render thread et GPU ;
- médiane, P95 et P99 ;
- batches, SetPass, draw calls, triangles et vertices ;
- temps de culling ;
- mémoire RenderTexture, textures, meshes et matériaux ;
- allocations GC par frame et lors d'une interaction ;
- nombre de `GameObject`, `Renderer`, `Collider` et matériaux ;
- coût de chaque caméra ;
- temps de génération/copie de textures de coupe ;
- temps de mise à jour des couleurs atlas ;
- temps de lecture/export PNG.

Les FPS seuls sont insuffisants, en particulier si VSync masque les marges.

## 5. Budgets initiaux

Les budgets finaux doivent être calibrés sur la baseline Built-in. Avant cette
mesure, utiliser des objectifs relatifs :

- scénario courant : URP P95 <= Built-in P95 × 1.10 ;
- scénario réaliste haut : aucune interaction > Built-in × 1.20 sans
  justification ;
- frame statique : zéro allocation GC récurrente due au rendu ;
- redimensionnement : allocations autorisées seulement lorsque le descripteur
  change, puis stabilisation ;
- mémoire : retour à un plateau après fermeture des vues ;
- couleur/atlas hover : ne doit pas provoquer une reconstruction globale si un
  changement uniforme suffit.

Pour la VR, le budget sera déterminé par la fréquence du casque ; aucune
reprojection permanente ne doit être considérée comme une réussite.

## 6. Priorité 1 — Multi-caméras

Le nombre de vues multiplie culling, passes, contours et coût de la scène.

Mesurer et expérimenter :

1. coût marginal d'une vue supplémentaire ;
2. coût des depth/depth-normals ;
3. coût des ombres par caméra ;
4. coût du full-screen contour par pixel et par caméra ;
5. gain en désactivant une vue minimisée/masquée ;
6. rendu à la demande quand caméra et données sont stables ;
7. réduction de résolution temporaire pendant un redimensionnement ;
8. pooling/réutilisation des RenderTextures.

Ne pas activer une opaque texture ou une copie couleur globale si aucun effet ne
l'utilise.

## 7. Priorité 2 — Sites

### Baseline structurelle

Mesurer séparément :

- instanciation/clonage par colonne ;
- nombre réel d'instances pour 30 000 sites source ;
- coût des `MonoBehaviour` ;
- coût des renderers individuels ;
- coût des colliders et raycasts ;
- coût du dictionnaire de matériaux par couleur ;
- draw calls par matériau/état ;
- mise à jour de l'activité ;
- culling par vue.

### Prototypes possibles

#### A. Optimisation conservatrice

- conserver GameObjects et colliders ;
- réduire les matériaux ;
- activer instancing si les conditions de batching le permettent ;
- regrouper les états ;
- éviter les mises à jour inutiles.

Faible risque, gain potentiellement limité par le nombre de renderers.

#### B. Rendu instancié, picking séparé

- buffer de position/taille/couleur/état ;
- rendu par lots ;
- structure CPU spatiale ou picking GPU ;
- GameObject uniquement pour le site sélectionné si nécessaire.

Gain potentiel élevé ; complexité élevée pour filtres, transparence, colonnes,
VR et WebGL.

#### C. BatchRendererGroup / solution data-oriented

À évaluer si la version Unity et les plateformes offrent un chemin stable. Ne
pas l'adopter uniquement parce qu'il est moderne : comparer coût
d'implémentation, support et maintenabilité.

### Critère de décision

Choisir la solution la plus simple qui atteint la baseline et le budget. Si
l'architecture actuelle reste suffisante sous URP, reporter la refonte complète
après la release.

## 8. Priorité 3 — Atlas et activités

L'écriture complète de `mesh.colors` sur le cerveau principal et chaque colonne
est proportionnelle au nombre de vertices et de colonnes. L'invalidation des
coupes ajoute des copies.

Prototype cible :

- identifiant de région stable ;
- palette 1D ;
- sélection/hover comme paramètres ;
- mise à jour O(1) ou proportionnelle à la palette, pas au mesh.

Pour l'activité, comparer :

- UV/vertex data actuels ;
- texture scalaire + LUT ;
- buffer par vertex ;
- fréquence réelle de mise à jour.

Le changement doit préserver l'interpolation et la précision scientifique.

## 9. Priorité 4 — Coupes

Instrumenter chaque étape :

1. calcul natif ;
2. copie native -> tableau managé ;
3. `SetPixels32` ;
4. `Apply` ;
5. copie base -> fonctionnelle ;
6. rendu.

Optimisations possibles :

- `SetPixelData`/`NativeArray` pour éviter une copie ;
- réutilisation de buffers ;
- séparation stricte base anatomique / overlay ;
- mise à jour partielle ;
- composition GPU ;
- suppression de `GetPixels32` suivi de `SetPixels32`.

Ne pas déplacer un calcul sur GPU sans vérifier disponibilité VR/WebGL et coût
de maintenance.

## 10. Shader et variantes

- vérifier la compatibilité SRP Batcher dans le Frame Debugger ;
- mesurer les variantes réellement compilées ;
- limiter les multi_compile ;
- utiliser des précisions `half` uniquement après validation colorimétrique ;
- garder `float` pour conversions ou valeurs scientifiques lorsque l'erreur
  `half` n'est pas acceptable ;
- tester le clipping de 20 plans sur Intel iGPU ;
- comparer branche uniforme et variante pour les modes atlas/activité.

## 11. Ordre d'optimisation recommandé

1. corriger les fuites et allocations RenderTexture ;
2. éviter le rendu inutile des vues ;
3. conserver un site shader minimal et mesurer l'architecture ;
4. réduire les invalidations atlas/coupes ;
5. optimiser les copies de texture ;
6. seulement ensuite introduire indirect draw, buffers complexes ou rendu à la
   demande généralisé.

## 12. Format d'un résultat

```text
Hypothèse :
Commit :
Machine / OS / GPU / API / driver :
Build :
Scène :
Colonnes / vues / sites / résolution :
Interaction :
Baseline médiane / P95 / P99 :
Candidate médiane / P95 / P99 :
CPU main / render / GPU :
Draw calls / batches / SetPass :
Mémoire et GC :
Résultat visuel :
Conclusion : garder / rejeter / approfondir
```

