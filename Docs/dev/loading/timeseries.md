# Séries temporelles, grilles et mémoire

Référence du code courant, vérifiée le 2 novembre 2026. Les mesures anciennes
sont isolées dans [Performance](performance.md). Les contrats spatiaux sont dans
[Projection](../activity-projection.md).

## Du fichier aux époques

[DataManager](../../../Assets/Scripts/HBP/Core/Data/DataManager.cs) possède les
données chargées et les dérivés. Pour iEEG, CCEP et MEG continu,
[RawRecordingCache](../../../Assets/Scripts/HBP/Core/Data/Loaded/RawRecordingCache.cs)
partage le chargement brut par clé de source avec `ConcurrentDictionary` et
`Lazy<DynamicData>`. Une erreur retire l'entrée fautive. La clé inclut le format,
les chemins, tailles et dates de modification des fichiers associés (y compris
les fichiers référencés par un header BrainVision). Elle n'inclut pas le protocole.
La construction EEG/époques se fait hors du verrou de publication du DataManager;
ceci ne décrit pas tous les autres chargeurs, notamment fMRI/statique.

[EpochedData](../../../Assets/Scripts/HBP/Core/Data/Loaded/EpochedData.cs) prépare
tous les blocs du protocole : Trial Matrix et changement de bloc en ont besoin.
[SubTrial](../../../Assets/Scripts/HBP/Core/Data/Loaded/SubTrial.cs) copie une
plage compacte par canal couvrant l'union contiguë fenêtre/baseline, puis expose
des `EpochView`. Les deux plages partagent ce stockage, même lorsqu'elles se
recouvrent. Ce n'est plus l'ancienne proposition de vues conservant nécessairement
l'enregistrement brut complet. Le cache garde seulement le dernier brut non
épinglé après un chargement époché; les époques sont autonomes. Une réouverture
compatible peut réutiliser les caches, mais aucune absence universelle de
relecture disque n'est garantie.

Les [descripteurs](../../../Assets/Scripts/HBP/Core/Data/Loaded/EpochDescriptor.cs)
conservent essais, sous-blocs, événements et bornes inclusives. Pour une fenêtre,
le début est arrondi au premier échantillon admissible (`ceil`) et la fin au
dernier (`floor`). Les sous-blocs successifs sont juxtaposés. Des sous-blocs
alignés de durées différentes utilisent le segment le plus long et leur
alignement événementiel `Before`/`After`.

## Traitements et statistiques

Les traitements s'appliquent dans l'ordre du protocole avec buffers de travail
réutilisés. Les paramètres et dérivés dépendent du protocole et de la normalisation;
un stockage compact d'époques ne peut avoir la durée de vie indépendante du
protocole d'un enregistrement complet.

[StreamingStatistics](../../../Assets/Scripts/HBP/Core/Data/Loaded/StreamingStatistics.cs)
utilise Welford en double pour l'accumulation, puis produit des flottants :
écart-type d'échantillon `sqrt(M2/(n-1))`, nul pour `n <= 1`, et
`SEM = écart-type / sqrt(n)`, nul pour zéro observation. La médiane loue un seul
buffer inter-essais, rendu dans `finally`. Elle conserve le calcul du SEM sur les
observations, sans construire une matrice temporaire échantillons × essais.
Les tests analytiques utilisent notamment `1e-6`, et les chemins complets de
normalisation `1e-4`; ce ne sont pas des tolérances universelles pour tout calcul.

`NormalizationType.Auto` sur un `DataInfo` suit la préférence utilisateur;
`Auto` comme préférence est ramené à `None`. Les changements d'agrégation
invalident les statistiques correspondantes. Le rechargement lié aux préférences
préserve les sources compatibles et suspend temporairement l'éviction pendant
la reconstruction, puis rétablit le budget, y compris en cas d'échec.

## Deux grilles temporelles

La navigation `Timeline` utilise la fréquence maximale des colonnes affichées.
Chaque colonne conserve une `ProjectionTimeline` à sa fréquence maximale interne.
Ainsi `[0; 1500 ms]` contient 97 positions à 64 Hz et 3073 à 2048 Hz : deux
colonnes gardent leurs grilles propres; une colonne mixte utilise 2048 Hz.

[Column3DDynamic](../../../Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs)
lit `EEGPreferences.TemporalSampling` : `Floor`, `Round` (`Mathf.RoundToInt`) ou
`Interpolate` par défaut. Le mapping utilise le temps physique, y compris origines
décalées, temps négatifs et fréquences non commensurables. Les sites et
l'implantation évaluent `TemporalSample.Evaluate`, donc interpolent entre les
valeurs lorsque demandé. La surface et les textures de coupe utilisent seulement
`CurrentProjectionSample.Index` : elles ne combinent pas les deux instants avec
son alpha temporel. `ActivityAlpha` transmis au générateur règle l'opacité,
pas l'interpolation temporelle. L'ancien champ
`Temporal Sampling` d'une configuration est ignoré sans migration de préférence.
L'export NIfTI suit exactement la grille de projection.

L'interpolation est une reconstruction explicite entre mesures, pas une nouvelle
mesure scientifique. Aucun sous-échantillonnage temporel automatique n'est utilisé
pour faire tenir les données dans le budget. Le pré-calcul natif exact reste
proportionnel au nombre de points × instants; cache de blocs/compression/GPU ne
sont pas des stratégies actives à déduire des anciens plans.

## Préparation et restitution

[ProjectionBufferBuilder](../../../Assets/Scripts/HBP/Core/Data/Loaded/ProjectionBufferBuilder.cs)
remplit le tableau contigu `temps × sites` et calcule les statistiques des sites
non masqués. Les valeurs préparées par site servent aux tooltips; la matrice
aplatie est une copie distincte nécessaire au natif. Les séries zéro sont
partagées par colonne. Limites et histogrammes n'exigent pas une concaténation
persistante de toutes les valeurs non masquées.

[TrialMatrixTileBuilder](../../../Assets/Scripts/HBP/Core/Data/Loaded/TrialMatrixTileBuilder.cs)
pave les deux axes selon la taille maximale de texture. Le lissage bilinéaire
emploie un halo d'un pixel recadré dans les UV, avec coordonnées et événements
globaux. `Color32` et `SetPixelData` évitent le tableau `Color[]`; les textures
sont réutilisées à taille égale et libérées à la destruction. Toutes les tuiles
sont matérialisées : il n'y a pas de virtualisation des seules tuiles visibles.
La comparaison de référence admet un niveau RGBA32 sur 255 par canal dû à
l'arrondi, sans décimation scientifique.

[CurveData](../../../Assets/Scripts/HBP/Data/Informations/Graph/CurveData.cs)
représente une série régulière par ordonnées, origine et pas. `GetPoint` reconstruit
l'abscisse; `Points` peut matérialiser une copie de compatibilité. Les courbes
irrégulières restent explicites. Le renderer prépare le viewport et réutilise ses
buffers; les exports CSV/SVG parcourent tous les points, indépendamment de la
réduction visuelle du renderer.

## Budget et limites

[MemoryCacheBudget](../../../Assets/Scripts/HBP/Core/Data/Loaded/MemoryCacheBudget.cs)
interprète `MemoryCacheLimit` en Mio. Zéro sélectionne
`min(floor(0,9 × RAM_Mio), max(0, RAM_Mio - 2048))`; RAM inconnue donne une limite
non bornée. L'éviction porte sur les entrées inactives, dans l'ordre dérivés
managés, projections natives, textures, brut, puis LRU dans chaque catégorie.
Les entrées actives restent épinglées; un dépassement est signalé sans altérer
leurs valeurs. Ce budget mesure les buffers connus de HiBoP, pas toute la RAM
du processus, du pilote ou d'Unity. Le nettoyage normal ne force pas de GC.

Les très longues grilles exactes et toutes les tuiles actives peuvent dépasser le
budget. Les profils GPU, plateaux mémoire et acceptations visuelles sur données
produit restent des vérifications distinctes des tests numériques. L'ancienne
estimation de gains mémoire ne constitue pas une mesure du checkout actuel.

## Vérifier une modification

Les tests sous
[HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests)
incluent `RawRecordingCacheTests`, `EpochIndexTests`, `DataLoadingProcessingCacheTests`
et `Stage3StreamingStatisticsTests` à `Stage8CurveDataTests`. Garder ces noms
historiques pour retrouver les tests; ils ne sont pas une chronologie documentaire.
Ils couvrent cache, bornes, identité des valeurs, temporalité, buffers et budget.
Compléter selon le changement par les scénarios UI de graphes/Trial Matrix et
les mesures de [performance](performance.md). Aucun test Unity n'a été exécuté
pour cette migration documentaire.
