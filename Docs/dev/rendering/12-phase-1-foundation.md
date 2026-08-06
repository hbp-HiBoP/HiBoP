# Phase 1 — Fondation URP

**Statut :** Gate 1 validée le 6 août 2026 (`36/36` tests de rendu et
`516/516` tests EditMode sur l'ensemble des assemblages concernés).

## Portée du jalon

La phase 1 installe URP et prépare la bascule sans modifier le rendu actif. Le
pipeline global reste Built-in jusqu'à la fin de la phase 2, lorsque le cerveau,
les coupes et les matériaux incompatibles auront leur shader cible.

Assets sérialisés :

- `Assets/Settings/Rendering/HBP-Desktop-URP.asset` ;
- `Assets/Settings/Rendering/HBP-Desktop-Renderer.asset` ;
- `Assets/Settings/Rendering/HBP-Desktop-URP-GlobalSettings.asset`.

Le renderer est Forward avec Render Graph actif. HDR, MSAA, shadow maps,
additional lights, opaque texture et depth texture globale sont désactivés. Le
SRP Batcher est activé. Les anciennes références SRP invalides des niveaux de
qualité et l'ancienne entrée HDRP invalide ont été supprimées.

## Contrats partagés

`RenderingColorUtility` et `HBPColor.hlsl` portent les mêmes règles :

- conversion sRGB/Linear IEC 61966-2-1 ;
- normalisation clampée, avec `0.5` pour une plage dégénérée ;
- mapping divergent `minimum -> 0`, `middle -> 0.5`, `maximum -> 1` ;
- index de palette arrondi au plus proche ;
- composition scientifique après l'anatomie ;
- `alpha = saturate(alpha_source_normalisé × alpha_utilisateur)`.

La fabrique de descripteurs produit RGBA8 sRGB, depth/stencil 24/8 et MSAA 1×.
Elle n'est branchée aux vues qu'en phase 4 : l'utiliser avant l'audit complet de
la durée de vie des RenderTextures modifierait le comportement actif hors du
périmètre de cette phase.

## Shaders minimaux

- `HBP/Cut` : unlit, RGBA explicite, états opaque/transparent pilotables par le
  matériau ;
- `HBP/Site` : couleur et alpha uniquement, compatible instancing ;
- `HBP/ROI/Wireframe` : wireframe barycentrique sans geometry shader.

Ils sont compilés dès cette phase mais ne remplacent pas encore les matériaux
actifs. Le maillage barycentrique ROI sera produit et branché en phase 3.

## Inventaire des matériaux actifs

La source de vérité machine est
`Assets/Settings/Rendering/HBP-Material-Migration-Inventory.json`. Elle contient
les 30 matériaux actifs découverts par l'union suivante :

- dépendances des scènes de build activées ;
- dépendances des prefabs sous `Assets/Prefabs` ;
- matériaux `.mat` sous `Assets/Resources`.

Les familles et stratégies sont :

| Famille actuelle | Nombre | Cible | Phase |
| --- | ---: | --- | ---: |
| Cerveau opaque/simplifié | 2 | `HBP/Brain` | 2 |
| Cerveau transparent | 1 | `HBP/Brain/Transparent` | 2 |
| Coupes | 2 | `HBP/Cut` | 2 |
| Matériaux déjà URP/Lit | 2 | conserver et vérifier les états | 2 |
| Aides GL unlit | 4 | `HBP/Utility/UnlitColor` | 2 |
| ROI | 2 | `HBP/ROI/Wireframe` | 3 |
| Sites | 13 | `HBP/Site` | 3 |
| Anneau de sélection | 1 | `HBP/Site/Selection` | 3 |
| UI custom | 3 | `HBP/UI/Texture` ou `HBP/UI/Mask` | 2 |

Les matériaux créés à l'exécution sont des clones de `Brain`, `Cut`,
`TransparentBrain`, `TransparentCut` ou `Sites/Basic` et sont donc couverts par
ces entrées. Les sous-assets de fontes et les exemples TextMesh Pro ne font pas
partie de l'inventaire `.mat` actif ; leur rendu reste pris en charge par les
shaders UI/TMP fournis avec Unity.

Un test EditMode recalcule ce périmètre et échoue si un matériau apparaît sans
entrée, si un shader source change sans mise à jour ou si une stratégie est
vide.

## Critère de bascule

L'asset URP est référencé par son renderer et les réglages globaux URP sont
enregistrés dans les Graphics Settings. L'assignation du pipeline à Graphics et
aux niveaux de qualité est volontairement la dernière étape de la phase 2 : la
faire ici rendrait les shaders Built-in actifs magenta avant leur migration.
