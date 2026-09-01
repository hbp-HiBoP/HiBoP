# P02 — catalogue des scopes V1

**Statut :** ACCEPTED — baseline P02-B  
**Source :** spécification produit V1, D03/D04/D12/D17/D18 et inventaire HiBoP au commit `7363ee729015590955194e0e545350becad16bd1`

## Règles de lecture

- `Desktop/project` signifie que la valeur canonique est persistée dans le projet HiBoP.
- `Desktop/session` signifie que la valeur canonique vit en mémoire de la session et peut être reconstruite depuis l'état Desktop.
- `Quest/session` signifie locale au Quest, en mémoire seulement, purgée à la fermeture ou au changement d'epoch.
- `Dérivé` désigne une sortie non commandable. Elle cite les révisions de ses entrées et n'ouvre pas un second propriétaire.
- Tous les IDs transmis sont des pseudonymes opaques propres à l'epoch. Les colonnes « état observé » ne rendent aucun type Desktop partageable.

## Catalogue normatif

| Scope | Famille de propriétés V1 | État HiBoP observé | Source de vérité | Persistance | Révision / invalidations |
| --- | --- | --- | --- | --- | --- |
| Project | disponibilité du projet et topologie opaque des visualisations compatibles | `ApplicationState.LoadedProject`, `Project.Visualizations`; noms/patients/datasets également présents mais exclus | Desktop | project pour le contenu ; snapshot session pour les pseudonymes | changement de contenu incrémente Project et l'état global ; invalide membership, assets et instances devenues orphelines |
| Visualization | existence/fermeture, membership ordonné par IDs, surface/mesh/MRI/implantation sélectionnés, anatomical/inflated, hémisphères, edges, transparence/alpha, colormap, politiques sites, coupe automatique | `Visualization` et `VisualizationConfiguration` (`MeshName`, `MRIName`, `SurfaceRepresentation`, `MeshPart`, `ShowEdges`, `TransparentBrain`, `BrainAlpha`, `HideBlacklistedSites`, `ShowAllSites`, etc.) | Desktop | project | incrémente Visualization + global ; invalide assets de surface/site si la source change, frames et coupes pour l'apparence/calibration |
| Column | existence/type, liaison à la visualisation, sélection de colonne, alpha activité, seuils/gain/palette/masques fonctionnels, label statique, inclusion dans bundle temporel | `Column`/configurations (`ActivityAlpha`, `MaximumInfluence`, `SpanMin/Middle/SpanMax`, bornes fMRI/MEG, flags hide) et `Column3D` | Desktop | project pour configuration ; session pour sélection/runtime | incrémente Column + global ; invalide `ColumnFrame`, `SiteRenderFrame` et overlays de coupe de cette colonne |
| BrainInstance | binding vers visualization/column, pose, rotation, échelle, visibilité locale, focus/disposition | absent du modèle Desktop ; création prévue dans le client XR | Quest | session uniquement | révision locale BrainInstance ; invalide transform/render local uniquement, jamais l'état scientifique Desktop |
| Site | sélection canonique ; blacklist, highlight et couleur par état de colonne ; réponse d'information détaillée transitoire | `Column3D.SelectedSite`; `SiteStateBySiteID` est propre à chaque colonne ; `SiteState` contient blacklisted/highlighted/color/labels et états dérivés | Desktop | project pour blacklist/highlight/color via `SiteConfiguration`; session pour sélection | scopeId distinct du siteId et propre à l'état colonne/site ; invalide `SiteRenderFrame`, sélection et calculs explicitement liés |
| Cut | existence, plan canonique (normale/orientation/position/flip), visibilité/binding et dernière intention acceptée | `VisualizationConfiguration.Cuts`, `Base3DScene.Cuts`, générateurs et textures de coupe | Desktop | project pour définition ; session pour interaction/résultat | incrémente Cut + global ; invalide géométrie, texture anatomique, contours et overlays ; le résultat porte aussi interaction/sequence |
| Roi | existence, sphères/paramètres, sélection et activation | `VisualizationConfiguration.RegionsOfInterest`; `ROI.Spheres` et `SelectedSphereID` | Desktop | project pour définition ; session pour sélection | incrémente Roi + global ; invalide appartenance ROI, visibilité sites et résultats/frames dépendants |
| Timeline | index/temps logique, sample index+alpha, lecture/pause, looping, vitesse/step et politique d'échantillonnage | `Timeline` (`CurrentIndex`, `IsPlaying`, `IsLooping`, `Step`) et `TemporalSample(Index, Alpha)` | Desktop | session ; configuration de visualisation si explicitement sauvegardée plus tard | incrémente Timeline + global ; invalide le `DynamicFrameBundle` attendu et tous ses résultats de colonne/coupe liés |

## États dérivés et données explicitement hors snapshot commandable

| État | Classification | Justification |
| --- | --- | --- |
| `SiteState.IsMasked`, `IsOutOfROI`, `IsFiltered`, visibilité/couleur/taille GPU | dérivé dans `SiteRenderFrame` | dépend des données, filtres, ROI et paramètres ; n'est pas une autorité indépendante |
| positions/normales/indices, site positions, textures anatomiques | asset immuable par SHA-256 | identité par hash, jamais par index seul |
| projections surface/site, overlays, coupes calculées | résultat dérivé révisionné | cite toutes les révisions d'entrée et est rejeté s'il devient stale |
| hover, feedback de gizmo, focus/panels et tracking | présentation Quest locale | ne modifie pas l'état scientifique ; seule une intention explicitement commandée traverse la frontière |
| noms patient/site/visualisation/colonne, chemins, labels libres, contenu source | exclu du snapshot V1 | D17 interdit les identifiants humains/logs ; les détails nécessaires sont transitoires et non persistés |
| données source, matrices patient, volumes temporels complets | Desktop seulement | D04/D06 : le Quest reçoit des résultats post-projection minimaux |

## Mapping des scopes

Un `ScopeKey` est `(scopeType, scopeId)`. `scopeId` identifie une cellule d'autorité, pas nécessairement une entité métier :

- une visualisation a un `visualizationId` et un `scopeId` de visualisation ;
- une colonne a un `columnId` et un `scopeId` de colonne ;
- un site a un `siteId`, mais chaque état colonne/site possède son propre `scopeId` de site ;
- un résultat transporte les IDs métier nécessaires et les révisions de tous les scopes d'entrée.

Cette règle ferme l'ambiguïté observée dans `SiteStateBySiteID` sans exposer le `FullID` humain ni stabiliser un index.

## Couverture et dépendances futures

Le catalogue couvre toutes les familles d'état V1 exigées par la spécification produit. P03 précisera les valeurs mathématiques et buffers dérivés ; P09 l'UX de création/fermeture d'instances ; P11 la sémantique temporelle ; P12 les payloads de coupe ; P13 signera l'inventaire fin des interactions. Ces paquets peuvent ajouter des propriétés optionnelles/capabilities, mais ne peuvent changer le propriétaire ou la règle de conflit sans rouvrir P02.
