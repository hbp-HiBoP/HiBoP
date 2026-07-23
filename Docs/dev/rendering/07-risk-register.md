# Registre de risques

## Échelle

- **Probabilité** : faible, moyenne, élevée.
- **Impact** : faible, moyen, élevé, critique.
- Un risque est fermé uniquement lorsqu'une preuve reproductible existe.

## R1 — Divergence colorimétrique mesh/coupe

**Probabilité :** élevée  
**Impact :** critique

**Cause :** éclairage différent et conversions sRGB/Linear différentes entre
vertex colors, textures générées et couleurs uniformes.

**Signaux :** même palette visiblement plus sombre, plus claire ou plus saturée
sur une représentation ; écart dépendant de l'orientation.

**Mitigation :**

- fixture de patchs avant migration ;
- espace source documenté ;
- overlay composité après éclairage ;
- tests d'invariance lumière/normale ;
- palette partagée.

**Preuve de fermeture :** tests du contrat colorimétrique + validation humaine.

## R2 — Perte fonctionnelle des shaders custom

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** Surface Shaders incompatibles URP ; clipping, extrusion et UV
multiples non convertis automatiquement.

**Mitigation :**

- réécriture HLSL dédiée ;
- port fonction par fonction ;
- passes depth/normals/shadows testées ;
- RenderDoc/Frame Debugger si nécessaire.

**Preuve :** matrice cerveau opaque/transparent, atlas, activité et 20 plans.

## R3 — Régression massive avec 30 000 sites

**Probabilité :** moyenne à élevée  
**Impact :** critique

**Cause :** renderers/colliders/GameObjects clonés par colonne, matériaux et
culling multipliés par les vues ; shader URP générique plus complexe.

**Mitigation :**

- port minimal du shader existant ;
- benchmark structurel ;
- aucun changement d'architecture sans A/B ;
- prototype instancing/picking séparé ;
- fallback plateforme.

**Preuve :** scénario stress sur Intel iGPU, fonctions de sélection incluses.

## R4 — Coût multiplié par 24 à 60 caméras

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** depth, normals, contours, ombres et culling exécutés par vue.

**Mitigation :**

- mesurer coût marginal ;
- désactiver vues invisibles ;
- réduire les passes optionnelles ;
- RenderTextures réutilisées ;
- rendu à la demande après parité.

**Preuve :** profils 1×1, 8×3 et 12×5 sans fuite.

## R5 — Fuite ou churn de RenderTextures

**Probabilité :** moyenne  
**Impact :** élevé

**Cause :** création au redimensionnement, `Release` sans destruction explicite,
ressources temporaires d'export.

**Mitigation :**

- propriétaire unique des textures ;
- descripteurs comparés avant allocation ;
- destruction déterministe ;
- test de 100 cycles ;
- profil mémoire.

**Preuve :** plateau mémoire stable après retour à l'état initial.

## R6 — Contours incompatibles ou trop coûteux

**Probabilité :** élevée  
**Impact :** moyen à élevé

**Cause :** PPv2 incompatible URP ; Full Screen Pass répété par vue.

**Mitigation :**

- Renderer Feature URP dédiée ;
- qualité/résolution configurables ;
- depth/normals seulement si requis ;
- benchmark 24/60 vues ;
- modernisation validée humainement.

**Preuve :** contours fonctionnels et dans le budget.

## R7 — Transparence et tri

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** cerveau, coupes, sites et ROI transparents avec profondeur et ordres
différents.

**Mitigation :**

- caractériser l'ordre Built-in ;
- fixtures superposées ;
- séparer par queues/passes ;
- évaluer depth prepass ou dither seulement après parité ;
- tester en VR.

**Preuve :** cas transparents validés sous plusieurs angles.

## R8 — Export différent de l'écran

**Probabilité :** moyenne  
**Impact :** critique

**Cause :** format RT, alpha, conversion sRGB, post-process ou chemin de caméra
différent.

**Mitigation :**

- même renderer/shaders ;
- état d'export minimal ;
- tests RGB/alpha ;
- restauration `try/finally` ;
- aucun color grading spécifique.

**Preuve :** comparaisons automatisées et alpha de fond nul.

## R9 — Geometry shader ROI non portable

**Probabilité :** élevée si WebGL, moyenne sinon  
**Impact :** moyen

**Cause :** geometry shader non disponible ou peu souhaitable selon API/VR.

**Mitigation :**

- barycentriques ou mesh d'arêtes ;
- matrice de capacités ;
- fallback explicite.

**Preuve :** ROI sur chaque backend livré.

## R10 — Explosion de variantes shader

**Probabilité :** moyenne  
**Impact :** moyen à élevé

**Cause :** combinaisons atlas, activité, clipping, transparence, ombres et
contours.

**Mitigation :**

- matrice de variantes ;
- branches uniformes quand adaptées ;
- stripping contrôlé ;
- test de build et temps de compilation.

**Preuve :** rapport de variantes et builds de toutes plateformes.

## R11 — Références pipeline/qualité incohérentes

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** GUID SRP absents et matériaux partiellement URP déjà présents.

**Mitigation :**

- inventaire GUID ;
- assets URP recréés proprement ;
- vérification de chaque niveau de qualité ;
- test de build propre, pas seulement Library existante.

**Preuve :** aucune référence manquante et build depuis cache propre.

## R12 — Migration automatique destructive

**Probabilité :** moyenne  
**Impact :** élevé

**Cause :** converter modifiant les matériaux en place sans couvrir les shaders
custom.

**Mitigation :**

- branche dédiée ;
- commit avant converter ;
- conversion par scope ;
- revue du diff ;
- jamais considérer « sans erreur » comme « visuellement correct ».

**Preuve :** inventaire matériaux avant/après et captures.

## R13 — Invalidation atlas/coupes trop large

**Probabilité :** élevée  
**Impact :** moyen à élevé

**Cause :** hover réécrivant les vertex colors de chaque colonne et invalidant
les textures de base.

**Mitigation :**

- marqueurs profiler ;
- distinguer base, overlay, hover et sélection ;
- identifiants + palette GPU ;
- dirty flags plus fins.

**Preuve :** temps de hover indépendant du nombre de vertices, ou gain mesuré
accepté.

## R14 — VR insuffisamment spécifiée

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** casque, runtime, API, fréquence et mode stéréo non encore fixés.

**Mitigation :**

- décision produit avant gate plateforme ;
- prototype early XR ;
- tests transparence/contours ;
- budgets adaptés à la fréquence.

**Preuve :** matrice XR remplie et test matériel.

## R15 — WebGL influence prématurément l'architecture

**Probabilité :** moyenne  
**Impact :** moyen

**Cause :** cible non décidée imposant des fallbacks complexes.

**Mitigation :**

- statut provisoire ;
- interfaces/fallback documentés ;
- pas de compromis de production sans décision ;
- spike séparé.

**Preuve :** décision go/no-go et liste de capacités.

## R16 — Modernisation esthétique non maîtrisée

**Probabilité :** moyenne  
**Impact :** moyen à élevé

**Cause :** changement simultané de contours, transparence, éclairage et
anti-aliasing.

**Mitigation :**

- jalon de parité ;
- une variable à la fois ;
- captures A/B ;
- validation humaine ;
- nouvelles baselines versionnées.

**Preuve :** chaque différence volontaire est identifiée et approuvée.

