# Décisions closes et règles de mesure

## Statut

Toutes les questions qui bloquaient le démarrage de la migration sont closes.
Ce fichier conserve les réponses afin d'éviter qu'elles soient rouvertes sans
nouvelle décision produit. Les détails opérationnels se trouvent dans
`10-implementation-plan.md`.

## D1 — Espace source des palettes

**Décision :** les couleurs d'atlas, fMRI, iEEG, MEG, préférences et autres
palettes destinées à l'affichage sont sRGB. Le projet reste Linear et effectue
une unique conversion sRGB vers Linear avant composition.

**Preuve à produire :** fixture de patchs texture/vertex/coupe/légende et
tolérance d'une unité 8 bits par canal dans un même environnement.

## D2 — Performance de référence

**Décision :** aucune cible « Intel intégré » n'est imposée. La baseline est la
performance Built-in du même cas réel, sur la même machine et à la même
résolution.

**Verdict :** objectif égal ou meilleur ; investigation au-delà de 5 % ; gate
refusée au-delà de 10 % de régression soutenue, sauf acceptation explicite.

## D3 — Définition des 30 000 sites

**Décision :** il s'agit de 30 000 sites source, donc potentiellement 30 000 par
colonne. Neuf colonnes et trois vues sont possibles, soit jusqu'à 270 000
instances réparties sur 27 vues.

Ce cas extrême doit être fonctionnel, stable et sans fuite, mais il est normal
qu'il soit lent. La gate performance utilise l'usage réel et un cas isolé de
30 000 sites × 1 vue. Pour les sites, la performance prime largement sur la
qualité : un simple cercle coloré est acceptable.

## D4 — VR

**Décision :** la certification VR est reportée dans un chantier séparé. Aucun
casque, runtime ou budget XR ne bloque cette migration desktop.

Les choix URP ne doivent pas rendre un portage futur artificiellement difficile,
mais aucun fallback ou profil VR n'est implémenté sans besoin défini.

## D5 — WebGL

**Décision :** WebGL est hors périmètre. Il ne doit imposer aucun compromis aux
shaders, au renderer de sites, aux Edges ou aux ROI de cette migration.

## D6 — macOS et Linux

**Décision macOS :** Apple Silicon uniquement, Metal, macOS 12.0 minimum selon
les Player Settings actuels. Le geometry shader ROI est remplacé par un
wireframe barycentrique.

**Décision Linux :** Vulkan est essayé et supporté en premier. Si une machine
Linux réellement ciblée échoue pour une raison de driver/backend, OpenGL Core
est testé comme fallback. Il n'est conservé que s'il passe toute la matrice.

Cette règle de décision remplace la nécessité de connaître à l'avance l'API
historique exacte.

## D7 — Budget de fluidité

**Décision :** pas de FPS absolu universel. Le cas réel
`visu_full_test` / `Small` porte la comparaison relative Built-in/URP. Le cas
combiné extrême est un test de robustesse, pas de fluidité.

## D8 — Transparence

**Décision :** ne pas reproduire les artefacts de tri. Conserver le contrôle
d'alpha, le clipping, l'export et la visibilité des sites, coupes et ROI à
travers le cerveau. Utiliser un tri transparent classique, `ZWrite Off` et
`Cull Back` au premier portage.

L'export PNG individuel doit fournir un fond alpha zéro et un straight alpha
correct, sans halo noir lorsqu'il est recomposé sur un autre fond.

## D9 — Edges

**Décision :** les Edges affectent uniquement le cerveau et les coupes. Les
objets opaques ont des contours profondeur/normales ; les transparents ont leur
silhouette extérieure seulement. Sites et ROI sont exclus.

L'état on/off est reproduit dans PNG, composite et vidéo. Sur fond transparent,
la feature ne doit pas noircir les pixels de fond.

## D10 — Cas scientifiques de référence

**Décision :** utiliser le projet `visu_full_test` et la visualisation `Small`
sur la machine où ils sont disponibles. Compléter par des fixtures synthétiques
pour les patchs colorimétriques, l'alpha, le clipping et les cas impossibles à
figer dans un projet réel.

La plupart des verdicts anatomiques et visuels sont donnés à l'œil par le
responsable du projet.

## D11 — Gestion de couleur des captures

**Décision :** mesures sur PNG sRGB brut, SDR, sans HDR système ni traitement
externe. La comparaison pixel par pixel globale du cerveau n'est pas une gate.
Elle est réservée aux patchs scientifiques déterministes et aux invariants
d'alpha/export.

## D12 — Ombres et éclairage

**Décision :** aucune shadow map dans le premier portage. L'anatomie utilise un
éclairage caméra-relatif léger, avec AO et éventuellement un spéculaire discret.
La gate est la lisibilité des sillons sans régression de performance.

## D13 — Projection d'activité

**Décision :** conserver pendant la migration les UV/valeurs par sommet de
`SurfaceGenerator` et les pixels RGBA de `CutGenerator`. Harmoniser palette,
seuils, interpolation, gamma et alpha.

La migration garantit que la discontinuité surface/voxel n'est pas aggravée.
Un échantillonnage direct du volume 3D est reporté et n'est pas obligatoire.

## D14 — Sélection des sites

**Décision :** préserver l'état de sélection, le picking et le retour UI
existant. L'indication active auditée est le texte de toolbar
`SelectedSite.cs`. Les assets `Select ring.prefab` et
`SiteSelectionShader.shader` paraissent legacy et ne justifient pas la création
d'un nouvel indicateur 3D ; ils seront vérifiés dans `Small` avant le nettoyage.

## Condition de réouverture

Une décision ci-dessus ne peut être rouverte que par :

- un besoin produit nouveau ;
- une impossibilité technique prouvée sur une plateforme requise ;
- une mesure reproductible montrant que la décision empêche de passer une gate.

Dans ce cas, mettre à jour simultanément ce fichier et
`10-implementation-plan.md` avant de changer l'architecture.
