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

**Preuve :** cas courant + scénario isolé 30 000 sites, fonctions de sélection
incluses. Le cas multi-colonnes extrême est une preuve de robustesse, pas de FPS.

## R4 — Coût multiplié par 24 à 27 caméras

**Probabilité :** élevée  
**Impact :** élevé

**Cause :** depth, normals, contours et culling exécutés par vue.

**Mitigation :**

- mesurer coût marginal ;
- désactiver vues invisibles ;
- réduire les passes optionnelles ;
- RenderTextures réutilisées ;
- rendu à la demande après le port fonctionnel.

**Preuve :** profils 1×1, 8×3 et 9×3 sans fuite.

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
- benchmark 24/27 vues ;
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
- évaluer depth prepass ou dither seulement après le port transparent classique.

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
- composition interne correcte et conversion unique vers PNG straight alpha ;
- test de recomposition sur fond blanc et `#282828` sans halo ;
- restauration `try/finally` ;
- aucun color grading spécifique.

**Preuve :** comparaisons automatisées et alpha de fond nul.

## R9 — Geometry shader ROI incompatible Metal

**Probabilité :** certaine avec le chemin historique  
**Impact :** élevé sur macOS

**Cause :** Metal ne prend pas en charge le geometry stage utilisé par le
shader historique.

**Mitigation :**

- barycentriques dans le mesh partagé ;
- shader fragment URP ;
- aucun geometry shader dans le chemin livré.

**Preuve :** ROI normal/sélectionné sous Windows, Metal et Linux.

## R10 — Explosion de variantes shader

**Probabilité :** moyenne  
**Impact :** moyen à élevé

**Cause :** combinaisons atlas, activité, clipping, transparence et
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

## R14 — VR reportée

**Statut :** fermé pour cette migration.

La certification XR est un chantier séparé. Elle ne constitue ni une gate ni
une contrainte non spécifiée du port desktop.

## R15 — WebGL hors périmètre

**Statut :** fermé pour cette migration.

WebGL ne doit imposer aucun fallback ou compromis au renderer livré.

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
