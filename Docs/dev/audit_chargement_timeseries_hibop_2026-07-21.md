# Audit du chargement des séries temporelles dans HiBoP

Date : 21 juillet 2026  
Périmètre : iEEG en priorité, CCEP et MEG continu lorsque le même pipeline ou les mêmes risques s'appliquent.  
Nature de l'audit : analyse statique du code HiBoP, du wrapper EEGFormat et de la projection native `hbp_core`. Aucun code ni test du dépôt n'a été modifié ou exécuté.

## Synthèse

L'intuition de départ est correcte : il existe un potentiel de réduction très important de la mémoire sans sacrifier les performances. Après prise en compte du scénario produit maximal habituel — environ **30 000 sites** (250 patients × 120 sites) et **100 instants projetés** — le poste prioritaire est bien le stockage des enregistrements et des époques, non la projection 3D prise isolément.

La projection 3D native reste un poste clairement identifiable, mais elle n'est vraisemblablement pas dominante dans ce scénario. Pour chaque colonne dynamique, `hbp_core` matérialise un tableau de taille `nombre_de_points_générés × longueur_de_timeline`. Les points générés comprennent les sommets de surface et une grille volumique dont la dimension maximale vaut 80 par défaut. La mémoire native retenue par colonne est donc :

`M_projection = 4 × P × L octets`

avec `P` le nombre de points générés et `L` le nombre d'instants. Avec `L = 100`, cela donne environ **114 Mio pour 300 000 points**, **191 Mio pour 500 000 points** et **229 Mio pour 600 000 points**, par colonne. C'est significatif et linéaire avec le nombre de colonnes, mais très inférieur au chargement de 30 000 séries brutes longues et à leurs époques. Le nombre de sujets n'augmente pas directement cette mémoire native ; il augmente surtout le nombre de sites à projeter et donc le coût du calcul initial.

Cette correction change la recommandation : la matérialisation native `P × L` est un compromis temps/mémoire raisonnable dans le scénario à 100 instants. La recalculer pour chaque coupe ou chaque frame à partir de 30 000 sites risquerait de dégrader fortement les performances. Elle ne doit donc pas être supprimée par principe. Il existe toutefois un cas rare mais important : une fenêtre de 1 500 ms à 2 048 Hz contient environ 3 073 instants lorsque les bornes sont incluses. Le même stockage atteint alors environ 3,4 Gio pour 300 000 points générés, 5,7 Gio pour 500 000 et 6,9 Gio pour 600 000, par colonne. Le scrubbing doit rester exact à cette fréquence ; le stockage devra donc respecter le budget mémoire par pré-calcul complet, compression sans perte ou cache de blocs exact, sans sous-échantillonnage temporel automatique.

Le deuxième poste majeur est l'épochage managé. Pour chaque canal, essai et sous-bloc, HiBoP conserve :

- une copie de la fenêtre brute ;
- une copie de la baseline ;
- une seconde copie complète de la fenêtre destinée aux valeurs normalisées.

La mémoire principale des époques vaut approximativement :

`M_epochs = 4 × C × somme(T_b × somme_s(2W_s + B_s)) octets`

Pour 128 canaux, 100 essais, une fenêtre de 2 001 échantillons et une baseline de 501 échantillons, cela représente environ **220 Mio par bloc et par enregistrement**, hors objets, dictionnaires, événements et statistiques. Multiplié par les patients, ce poste dépasse rapidement la projection d'une colonne.

Enfin, la visualisation matérialise à nouveau les valeurs rééchantillonnées, une matrice aplatie pour la DLL et une copie des valeurs non masquées. Ces coûts sont plus faibles que la projection native, mais deviennent importants avec plusieurs patients, canaux, blocs et colonnes.

La cible recommandée est donc une architecture où :

1. l'enregistrement brut reste le stockage canonique chaud du cache ;
2. les époques de tous les blocs restent accessibles, mais deviennent des descripteurs de plages plutôt que des copies ;
3. normalisation et statistiques travaillent en streaming, sans matrices temporaires ;
4. un curseur temporel commun synchronise des grilles sources et de projection qui restent discrètes et indépendantes ;
5. la projection pré-calculée reste le chemin par défaut à 100 instants, avec une stratégie exacte sous budget pour les longues grilles ;
6. les caches ont une propriété et une taille explicites tout en privilégiant le rechargement instantané des données déjà consultées.

## Flux actuel

1. `Visualization.LoadAsync` détermine les `DataInfo`, appelle `DataManager.GetData` pour chaque enregistrement, normalise toutes les données chargées, puis prépare chaque colonne (`Visualization.cs:200-250`, `423-498`).
2. `DynamicData` ouvre EEGFormat avec `loadData=true`. EEGFormat conserve le signal natif complet, puis `File.Electrodes` alloue et remplit un `float[]` managé complet par canal (`DynamicData.cs:40-108`, `EEG/File.cs:36-52`).
3. `EpochedData` crée immédiatement les données de **tous** les blocs du protocole (`EpochedData.cs:12-28`).
4. Chaque `SubTrial` copie fenêtre et baseline pour tous les canaux, applique les traitements, puis clone encore chaque fenêtre dans `ValuesByChannel` (`SubTrial.cs:32-55`, `115-136`).
5. Le `DataManager` conserve plusieurs index et graphes dérivés : données par bloc, canal, bloc-canal, statistiques et événements (`DataManager.cs:17-35`).
6. Chaque colonne iEEG conserve ses références aux essais/statistiques et matérialise un tableau rééchantillonné par canal sur une timeline à fréquence maximale commune (`Processed/IEEGData.cs:22-38`, `52-105`; `Visualization.cs:476-498`).
7. `Column3DIEEG` reprend ces tableaux, fabrique une matrice aplatie `temps × sites` et une autre copie de toutes les valeurs non masquées (`Column3DIEEG.cs:57-135`).
8. `hbp_core::IEEGGenerator` transforme cette matrice de sites en un tableau complet `temps × points_de_surface_et_volume`, retenu tant que la colonne existe (`hbp_core/src/generators/ieeg_generator.cpp:98-111`).

Nuance importante : les dictionnaires `m_BlocDataByRequest` et certains objets canal ne recopient pas toujours les valeurs numériques ; une partie d'entre eux ne fait que référencer les mêmes `float[]`. Leur coût vient surtout du nombre d'objets, de dictionnaires et de clés. Les copies numériques dominantes se trouvent dans `SubTrial`, la préparation de colonne et la projection native.

## Constats et solutions

### P1 — Projection native : conserver le pré-calcul et gérer les longues grilles

**Preuve.** `IEEGGenerator::compute_activity` alloue `_values` avec `timeline_length × generated_vertices.size()` éléments (`ieeg_generator.cpp:98-111`). `GeneratorSurface` ajoute aux sommets du cerveau tous les points de la grille volumique (`generator_surface.cpp:30-48`, `54-65`). La dimension maximale vaut 80 par défaut (`ActivityProjectionSettings.cs:8-15`). Une instance de générateur existe par colonne dynamique.

**Impact corrigé.** Avec 100 instants, le coût est d'environ 114 Mio pour 300 000 points, 191 Mio pour 500 000 points et 229 Mio pour 600 000 points, par colonne. Plusieurs colonnes peuvent rendre ce poste important, mais 30 000 séries brutes longues et leurs époques représentent un volume très supérieur. En revanche, le coût de reconstruction d'une tranche dépend bien des quelque 30 000 sites : supprimer le pré-calcul ferait porter ce calcul au scrubbing et à la lecture.

**Solution recommandée.** Garder le pré-calcul complet comme référence et comme stratégie par défaut pour le cas nominal de 100 instants :

- mesurer et exposer `P`, `L`, `stored_value_count`, le temps de construction et la mémoire réellement retenue par colonne ;
- n'étudier un cache de tranches, une factorisation spatiale ou un calcul GPU que pour des timelines exceptionnellement longues ou un grand nombre de colonnes, derrière une stratégie interchangeable ;
- ne choisir cette stratégie adaptative que si elle respecte le budget de latence sur le scénario 30 000 sites/100 instants.

La mutualisation entre colonnes n'est pas retenue : deux colonnes contenant les mêmes données constituent un usage trop improbable pour justifier la complexité de propriété et d'invalidation, d'autant que leurs paramètres locaux peuvent différer.

Une matrice spatiale creuse peut en outre devenir volumineuse lorsque beaucoup de sites contribuent aux mêmes points. Elle n'est donc plus considérée ici comme une optimisation acquise.

**Validation.** Comparer chaque tranche à la sortie actuelle, checksum et erreur absolue ; mesurer construction, scrubbing, lecture, mise à jour des coupes, export, mémoire privée et `stored_value_count`. Le scénario de référence doit comporter 30 000 sites et 100 instants, puis des variantes avec plusieurs colonnes. Le dépôt possède déjà des scénarios et métriques dans `NativeProjectionLoadBenchmarkScenarios.cs`.

### Contrainte fonctionnelle — Tous les blocs du protocole doivent rester immédiatement accessibles

**Preuve.** `TrialMatrixPreferences.ShowWholeProtocol` vaut `true` par défaut (`VisualizationPreferences.cs:119-124`). Dans ce mode, `TrialMatrixZone.Display` utilise `data.Dataset.Protocol.OrderedBlocs` plutôt que les seuls blocs sélectionnés dans la visualisation (`TrialMatrixZone.cs:23-39`). Le rechargement rapide d'une autre visualisation fondée sur les mêmes enregistrements est également un usage fréquent.

**Conséquence sur l'audit.** Le chargement de tous les blocs n'est pas un défaut à supprimer. La rétention du signal brut par le `DataManager` est un cache fonctionnel utile. Le problème est la matérialisation immédiate des échantillons de chaque bloc, essai, baseline et canal, non l'indexation anticipée de tous les blocs.

**Solution recommandée.** Conserver l'enregistrement brut managé comme stockage canonique chaud et construire immédiatement les descripteurs de tous les blocs du protocole : offsets de fenêtres et de baselines, essais, sous-blocs et événements. La Trial Matrix et une nouvelle visualisation obtiennent alors instantanément des vues sur ce stockage sans nouvelle I/O et sans copie persistante par époque. Une matérialisation contiguë ponctuelle reste possible pour une API qui exige réellement un `float[]`, dans un buffer de travail réutilisable.

Les canaux effectivement absents de toute implantation ou de tout outil pourraient être exclus seulement si cette hypothèse est prouvée au niveau produit. Ce n'est pas un prérequis du plan.

**Validation.** Après le premier chargement, afficher successivement n'importe quel bloc du protocole et rouvrir une visualisation sur le même fichier sans nouvelle lecture disque. La Trial Matrix complète doit conserver son comportement par défaut.

### P0 — Une époque possède trois représentations numériques

**Preuve.** `RawValuesByChannel`, `BaselineValuesByChannel` et `ValuesByChannel` sont trois dictionnaires de tableaux. Fenêtre et baseline sont copiées avec `Array.Copy`, puis la fenêtre est clonée. La baseline est parfois une sous-plage déjà présente dans la fenêtre, mais reste un tableau indépendant.

**Impact.** Coût permanent `2W + B` par canal/essai/sous-bloc, nombreux objets sur le Large Object Heap lorsque les fenêtres sont grandes, fragmentation et coût de nettoyage.

**Solution recommandée.** Séparer index et échantillons tout en gardant le brut :

- un `EpochDescriptor` commun à tous les canaux contient les indices de fenêtre, baseline et événements ;
- les canaux sont identifiés par entier, non par chaîne/dictionnaire dans chaque sous-essai ;
- l'enregistrement brut managé reste la représentation canonique persistante du cache ;
- fenêtre et baseline sont des vues (`offset`, `length`) dans cet enregistrement ;
- les paramètres des traitements et normalisations sont conservés sous forme compacte lorsqu'ils peuvent être appliqués à la lecture ;
- un seul buffer contigu réutilisable est matérialisé lorsque le consommateur ne sait pas lire une vue ou une transformation ;
- les traitements sont compilés dans un pipeline par sous-bloc ; les traitements ponctuels peuvent être appliqués lors de la copie ou à la lecture.

Les traitements qui changent réellement chaque échantillon doivent être classés : transformation ponctuelle, transformation décrite par quelques scalaires, ou opération nécessitant un buffer. Cette classification déterminera où une matérialisation est inévitable, sans réintroduire trois copies pour toutes les époques.

**Validation.** Sorties identiques pour tous les traitements et modes de normalisation ; mesure séparée des octets de signal, des descripteurs et des buffers dérivés.

### P0 — Séparer l'horloge commune des grilles sources et des grilles de projection

**Preuve.** Toutes les colonnes iEEG reçoivent la fréquence maximale observée. `SetTimeline` construit un `List<float>`, crée un tableau interpolé par sous-bloc, puis appelle `ToArray` pour chaque canal. Même lorsque la taille est inchangée, `Interpolate` clone le tableau (`MathDLL.cs:151-157`). Les fillers nécessaires à l'alignement des blocs sont eux aussi stockés pour chaque canal.

**Impact.** Une colonne à 2 kHz force une colonne à 256 Hz à occuper une timeline à 2 kHz. Plusieurs colonnes du même bloc recréent des tableaux identiques. Les temporaires `List`, interpolation et `ToArray` augmentent aussi le pic.

**Clarification scientifique.** Un temps continu ne définit pas la valeur du signal entre deux mesures. Interpolation linéaire, maintien de la dernière valeur et plus proche voisin sont trois hypothèses différentes ; aucune ne recrée une mesure qui n'existe pas. Le stockage ne doit donc pas rendre l'interpolation implicite. En revanche, calculer une interpolation linéaire à la demande ou matérialiser à l'avance le même rééchantillonnage produit la même valeur numérique : le doute scientifique porte sur la politique de reconstruction, pas sur le moment du calcul.

**Solution recommandée.** Distinguer trois niveaux simples :

1. Une **horloge de synchronisation** exprime uniquement la position courante en temps physique. Elle n'a ni fréquence scientifique propre, ni tableau de valeurs.
2. Chaque **grille source** reste décrite par son origine, sa fréquence et ses indices natifs. Ces échantillons ne sont jamais remplacés par une prétendue série continue.
3. Chaque consommateur possède, si nécessaire, une **grille de calcul discrète** : grille de projection 3D, pixels d'un graphe ou pas d'un export. Cette grille indique explicitement comment une source est évaluée : échantillon exact, plus proche voisin, maintien, interpolation validée ou rééchantillonnage filtré.

Pour deux colonnes distinctes à 64 Hz et 2 048 Hz pendant 1 500 ms, les grilles de projection peuvent ainsi rester indépendantes. Le curseur temporel est commun, mais la première colonne possède environ 97 positions et la seconde environ 3 073 si toutes les mesures doivent être projetées. Il n'est pas nécessaire de gonfler la colonne 64 Hz à 3 073 valeurs. Lorsqu'un temps du curseur ne correspond pas à une tranche de la colonne 64 Hz, HiBoP doit appliquer une politique scientifique explicite : conserver/choisir un échantillon natif, ou interpoler deux tranches. Ce choix doit être validé avec les utilisateurs scientifiques et ne doit pas être caché dans la classe de timeline.

Si plusieurs enregistrements de fréquences différentes contribuent à une **même** colonne et au même champ spatial, une grille cible commune devient inévitable. Dans l'exemple 64/2 048 Hz, les grilles sont emboîtées avec un rapport exact de 32 si leurs origines sont alignées : les valeurs 2 048 Hz peuvent rester exactes et seule la source 64 Hz est reconstruite aux temps intermédiaires. Pour des fréquences non commensurables ou des origines différentes, le mapping doit utiliser les temps physiques, jamais un simple rapport d'indices. Une réduction de fréquence exige un filtrage anti-repliement validé ; une interpolation linéaire ne constitue pas un downsampling scientifiquement suffisant.

**Limite fondamentale.** Une projection exacte de tous les 3 073 instants conserve nécessairement un terme `P × 3 073` si toutes les tranches doivent être immédiatement disponibles. Il n'existe alors que quatre leviers : conserver toute la RAM, réduire la résolution temporelle avec une méthode validée, compresser les tranches, ou calculer/précharger des blocs temporels avec un cache borné. Le plan ne choisit pas silencieusement entre fidélité temporelle et latence.

**Stratégie proposée.** Garder le pré-calcul complet pour les grilles courtes, notamment le cas nominal de 100 instants. Au-delà d'un seuil en octets :

- conserver tous les instants scientifiques : aucun sous-échantillonnage temporel automatique dans le mode normal ;
- comparer pré-calcul complet, compression sans perte et cache de blocs avec préchargement ;
- considérer le scrubbing exact comme un critère bloquant, y compris à 2 048 Hz ;
- réserver un éventuel mode « performance », qui pourrait réduire la résolution volumique spatiale selon le nombre de points, à un chantier ultérieur explicite.

L'export mentionné dans la version précédente est l'export NIfTI 4D de l'activité projetée (`ExportActivityToNiftiWindow` et `ActivityGenerator.SaveActivityAsNifti`). Il doit suivre la même grille scientifique exacte que la projection et ne justifie pas une politique temporelle différente.

La première version doit préserver exactement la grille et l'interpolation actuelles afin d'isoler la réduction mémoire. Le paramètre de visualisation `floor` / `round` / interpolation peut ensuite être ajouté sans modifier le stockage source ; l'interpolation reste le comportement par défaut.

**Bénéfice attendu.** Les colonnes ne paient plus la fréquence maximale globale. La rareté des fréquences mixtes ne complexifie que le mapping temporel et les colonnes réellement concernées, pas toutes les séries chargées.

### P1 — La colonne 3D conserve jusqu'à trois copies des séries de sites

**Preuve.** `ProcessedValuesByChannel` possède une série par site. `ActivityValuesBySiteID` référence ces séries, ce qui est peu coûteux, mais `ActivityValues` les recopie dans une matrice aplatie et `ActivityValuesOfUnmaskedSites` recopie encore tous les sites actifs.

**Impact.** Jusqu'à trois fois `sites × timeline` en mémoire managée, plus des tableaux zéro complets pour chaque site absent. `ActivityValuesOfUnmaskedSites` n'est utilisé que pour limites, moyenne et histogramme.

**Solution recommandée.** Garder une seule représentation canonique côté managé lorsque les mesures le permettent. Calculer min/max, moyenne, variance et histogramme en streaming sur les sites non masqués et supprimer `ActivityValuesOfUnmaskedSites` si son absence améliore ou maintient les temps. Représenter un site absent par un état ou une série zéro partagée, pas un nouveau `float[L]`. La matrice aplatie destinée à la DLL peut rester le format de transfert final si elle est favorable à la localité ; elle doit alors être remplie directement depuis les vues temporelles, sans représentation rééchantillonnée intermédiaire.

**Garde-fou.** Chaque suppression de copie doit être benchmarkée sur préparation de colonne, statistiques, transfert natif et scrubbing. Une vue plus abstraite n'est acceptée que si elle ne détériore pas les performances.

### P1 — Les caches ne possèdent ni budget ni cycle de vie cohérent

**Preuve.** `Visualization.Unload` vide les objets préparés des colonnes mais ne libère pas le `DataManager`. `DataManager.UnLoad` retire les données et vues de canaux, mais ne retire pas toutes les statistiques, événements et entrées de normalisation. `DataManager.Clear` est principalement déclenché lors de modifications de dataset/protocole/patient et force un `GC.Collect()`.

**Impact.** Les époques restent volontairement disponibles entre visualisations, ce qui favorise les rechargements, mais la mémoire peut croître jusqu'à la taille de toutes les données consultées. Certains dérivés survivent même à `UnLoad`. Le `GC.Collect` synchrone crée une pause et ne constitue pas une politique mémoire.

**Solution recommandée.** Introduire une politique qui conserve explicitement les données utiles au rechargement :

- des leases/références actives par visualisation et outil ;
- une taille comptable pour chaque entrée ;
- un état « chaud/pinned » pour les enregistrements bruts déjà chargés et encore pertinents dans le dataset courant ;
- des dérivés reconstructibles bornés par budget, sans évincer agressivement le brut qui garantit le rechargement instantané ;
- une invalidation versionnée par normalisation, averaging et protocole ;
- un `Unload` symétrique qui retire tous les dérivés de la même clé ;
- aucune collecte forcée sur le chemin normal, sauf transition majeure démontrée utile par profilage.

Le budget, les priorités d'éviction et la durée de rétention seront définis pendant l'implémentation à partir des mesures. Le point architectural à fixer dès maintenant est la distinction entre le cache brut coûteux à relire et les dérivés bon marché à reconstruire.

Le réglage existant `General.System.MemoryCacheLimit`, exprimé en Mio, devient la source du budget. Il est actuellement utilisé uniquement par `VisualizationGestion` pour un avertissement approximatif fondé sur le nombre de patients ; il ne pilote ni `DataManager`, ni les projections natives, ni les textures. Le futur gestionnaire doit lui donner une sémantique explicite : budget des caches HiBoP, comptabilité managée/native/GPU autant que possible, marge réservée au reste de l'application et avertissement lorsque les données actives non évictables dépassent le plafond. La valeur `0` conserve sa signification actuelle de limite automatique dérivée de la RAM de la machine.

### P1 — Normalisation et statistiques dupliquent temporairement de gros volumes

**Preuve.** Les normalisations Trial/SubBloc/Bloc/Protocol accumulent les baselines dans des `List<float>`, puis les convertissent plusieurs fois en tableaux pour moyenne et écart-type (`DataManager.cs:1028-1239`). `ChannelSubTrialStat` transpose tous les essais dans un jagged array `échantillons × essais` avant de calculer moyenne/SEM (`ChannelSubTrialStat.cs:56-95`). Les graphes répètent encore ce calcul avec une nouvelle `List` à chaque échantillon.

**Impact.** Pics temporaires proches du volume des baselines ou essais, pression GC élevée et temps passé à copier plutôt qu'à calculer. La normalisation Protocol conserve simultanément les listes de tous les canaux.

**Solution recommandée.** Utiliser des accumulateurs en streaming : Welford ou somme/somme des carrés pour moyenne, variance et SEM. Pour la médiane, louer un unique buffer de taille `nombre_d'essais` et le réutiliser pour chaque échantillon ; ne jamais allouer une matrice `W × T`. Les statistiques de graphes sans sélection doivent réutiliser le cache ; avec sélection, utiliser le même worker et son buffer réutilisable.

### P1 — Le chargement fait coexister plusieurs copies complètes et sérialise l'I/O

**Preuve.** EEGFormat charge le signal complet dans ses vecteurs natifs, puis `File.Electrodes` crée tous les tableaux managés. Ensuite l'épochage crée les copies finales. Par ailleurs, `DataManager.Load` garde le verrou d'écriture pendant l'ouverture du fichier et tout l'épochage (`DataManager.cs:467-525`), alors que `Visualization` programme plusieurs tâches.

**Impact.** Le pic comprend au moins signal natif + signal managé pendant la copie, puis signal managé + époques. Le verrou global annule une grande partie du parallélisme de chargement et bloque les simples lectures de cache pendant une I/O longue.

**Solution recommandée.** Construire les données hors verrou, avec un mécanisme « single flight » par clé pour éviter les doubles chargements, puis publier atomiquement le résultat sous un verrou court. Le parallélisme doit être limité par un budget mémoire, pas seulement par un nombre fixe de tâches. Avec l'API actuelle d'EEGFormat, HiBoP peut déjà conserver la copie managée brute comme stockage canonique puis éliminer les copies d'époques ; cela ne nécessite aucune modification immédiate de la DLL partagée.

Une future API additive de lecture par canal/plage reste une optimisation possible du pic de chargement natif + managé. Elle doit constituer un chantier EEGFormat séparé : contrat documenté, compatibilité binaire/versionnée, tests sur les formats utilisés par le laboratoire, benchmarks et validation par les autres consommateurs. Aucun changement cassant d'EEGFormat n'est requis par le plan principal.

### P2 — Trial Matrix crée des copies CPU/GPU disproportionnées

**Preuve.** Le calcul des limites concatène toutes les valeurs dans une liste puis un tableau (`Informations/TrialMatrix/Data.cs:42-61`). Le lissage 2D crée des tableaux plats et jagged supplémentaires. La texture utilise un `Color[]` de 16 octets par pixel, puis `SetPixels`, tandis que le GPU stocke une autre texture RGBA32 (`UI/Informations/TrialMatrix/SubBloc.cs:147-220`).

**Impact.** Pour une matrice essais × échantillons, le pic peut contenir données brutes, données lissées, `Color[]`, copie CPU de la texture et texture GPU. Les textures remplacées ne sont pas explicitement détruites dans cette méthode.

**Solution recommandée.** Commencer par les changements simples et localisés : limites en streaming, réutilisation/destruction explicite des textures, `Color32`/`SetPixelData` si le profilage confirme le gain. Encapsuler ces détails dans un petit composant de rendu afin de ne pas disperser la complexité CPU/GPU. Une texture scalaire et une colormap shader, ou la suppression de l'upsampling CPU, ne doivent être retenues que si le rendu et le filtrage sont strictement identiques et si le code reste plus maintenable que le chemin actuel.

**Cas des matrices dépassant la taille maximale de texture.** `SubBloc.GenerateTexture` crée actuellement une texture monolithique de dimensions `nombre_d'échantillons × nombre_d'essais`, sans vérifier `SystemInfo.maxTextureSize`. Une fréquence et une époque élevées peuvent donc dépasser une dimension supportée par le GPU. La solution de référence est un pavage exact :

- découper la matrice en textures dont chaque dimension respecte une limite sûre dérivée de `SystemInfo.maxTextureSize` ;
- conserver un pixel par valeur afin de ne pas modifier l'interprétation ;
- juxtaposer les tuiles dans le même repère temporel et calculer les événements sur les coordonnées globales ;
- pour le lissage, lire un halo autour de chaque tuile afin d'obtenir aux jointures exactement le même résultat qu'une matrice monolithique ;
- générer ou conserver uniquement les tuiles visibles si le nombre total devient lui-même coûteux.

Ce pavage résout la limite technique sans décimation. Une réduction temporelle de la Trial Matrix pourra être étudiée séparément, mais seulement avec une règle d'agrégation documentée et la tolérance visuelle convenue.

### P2 — Les graphes matérialisent trop tôt tous les points

**Preuve.** MEG continu et iEEG créent un `Vector2[]` pour chaque échantillon, puis `CurveData.Init` appelle encore `ToArray`. `ShapedCurveData` recopie aussi les SEM. Le renderer ne sous-échantillonne qu'ensuite (`Curve.cs:190-194`).

**Impact.** Pour un signal continu, deux tableaux de `Vector2` représentent déjà 16 octets par échantillon en plus des 4 octets du signal. Les moyennes interactives réallouent des buffers par échantillon.

**Solution recommandée révisée.** Ne pas introduire par défaut une décimation min/max, car elle peut modifier la courbe selon le zoom. Conserver implicitement l'abscisse régulière (`start`, `step`) et matérialiser dans un buffer réutilisable exactement les points que le renderer actuel utiliserait pour le viewport et le niveau de zoom courants. Réutiliser les statistiques du `DataManager` et les buffers de rendu. Toute réduction du nombre de points doit passer des comparaisons visuelles de référence à tous les niveaux de zoom, y compris pics, SEM, bords de sélection et exports ; sinon le chemin exact actuel est conservé.

### P2 — Beaucoup d'objets et de dictionnaires représentent une structure intrinsèquement régulière

**Preuve.** Chaque requête crée une classe clé. Chaque canal/bloc/essai/sous-bloc ajoute dictionnaires, wrappers `ChannelTrial`, structs et tableaux. Les clés utilisent chaînes et objets métier.

**Impact.** Ce coût ne domine pas les tableaux numériques mais pénalise le GC, les parcours et la localité mémoire, surtout sur des milliers d'essais/canaux.

**Solution recommandée.** Clés immuables de type struct basées sur IDs/indices, tableaux indexés par bloc/sous-bloc/canal, événements stockés une seule fois dans l'index d'époque. Les caches bloc/canal doivent être des vues légères, pas des graphes d'objets possédant une méthode `Clear` ambiguë.

Cette transformation est secondaire par rapport aux copies numériques. Elle doit être faite après stabilisation des descripteurs d'époques, avec tests d'identité des événements, de l'ordre des blocs/essais et de toutes les résolutions par ID.

### P2 — Quelques chemins de métadonnées ne libèrent pas déterministement EEGFormat

**Preuve.** Les contrôles iEEG/CCEP et l'affichage d'en-tête créent `DLL.EEG.File` avec `loadData=false` sans `Dispose`. Le finalizer finit par libérer la ressource, mais à un moment non déterministe. `File.Electrodes` mélange en outre accès aux métadonnées et récupération des données.

**Impact.** Accumulation temporaire d'objets natifs et dépendance au finalizer. Avec `loadData=false`, `NumberOfSamples` vaut normalement zéro dans EEGFormat, donc ce chemin ne semble pas recopier le signal complet ; il reste néanmoins fragile.

**Solution recommandée.** À court terme, entourer chaque ouverture de métadonnées par `using` ou `try/finally` afin que `Dispose` s'exécute aussi en cas d'exception ; c'est la formalisation concrète attendue. Une API additive distincte `ElectrodeMetadata`, qui ne contient jamais de `Data`, pourra ensuite clarifier le contrat d'EEGFormat sans modifier le chemin de données existant.

### P2 — La mise à jour UV alloue des buffers à chaque appel

**Preuve.** `SurfaceGenerator.ComputeActivityUV` alloue deux `Vec2[]` de taille `nombre_de_sommets` à chaque mise à jour, puis les recopie dans deux `Vector2[]` persistants (`SurfaceGenerator.cs:28-41`).

**Impact.** Plusieurs mégaoctets temporaires possibles par changement de timeline et forte pression GC, même après réduction de la mémoire de chargement.

**Solution recommandée.** Conserver des buffers natifs/managés réutilisables, ou copier directement dans des `NativeArray<Vector2>`/buffers GPU. Ce chemin doit afficher zéro allocation managée en régime stable.

## Architecture cible proposée

```text
RecordingReader
  lecture EEGFormat complète au premier accès
          |
          v
RawRecordingCache (chaud, réutilisé entre visualisations)
  données brutes canoniques + métadonnées de canaux
          |
          +--> EpochIndex (tous les blocs, partagé entre canaux)
          |      essais, sous-blocs, offsets, longueurs, baselines, événements
          |
          +--> Treatment / NormalizationParameters
          |      transformations compactes, buffers de travail si nécessaires
          |
          +--> DerivedCache versionné et comptabilisé
          |      moyennes, médianes, SEM et dérivés reconstructibles
          |
          v
SharedTimeCursor
  temps physique commun, sans fréquence ni valeurs propres
          |
          +--> SourceGrid par enregistrement
          |      origine + fréquence + indices natifs
          |
          +--> ConsumerGrid par colonne/outil
                 politique d'échantillonnage explicite
                 +--> Graphes / Trial Matrix
                 +--> ProjectionGrid discrète
                        pré-calcul natif complet et exact conservé
```

Les API consommatrices devraient demander des vues en lecture (`ReadOnlySpan` ou abstraction équivalente) et non exiger un `float[]` possédé. Les objets UI ne doivent jamais devenir propriétaires des données scientifiques. Le cache brut reste intentionnellement vivant après `Visualization.Unload`; les dérivés possèdent en revanche une clé, une version, un coût et une règle d'invalidation explicites.

## Plan d'implémentation par étapes indépendantes

Les étapes suivantes sont ordonnées, mais chacune doit constituer une modification autonome, mesurable et livrable avec tous les tests au vert. Une étape ne doit pas anticiper la suivante par des abstractions inutilisées. L'ancien chemin peut rester temporairement disponible comme oracle de comparaison, puis être retiré dans l'étape qui atteint la parité.

### Étape 0 — Référence, instrumentation et données synthétiques

**Objectif.** Établir les valeurs de référence avant toute modification et fournir des générateurs de scénarios déterministes réutilisables par les étapes suivantes.

**Travail prévu.**

- mesurer séparément brut managé, époques, dérivés, tableaux de colonne, projection native, textures et mémoire privée ;
- étendre `NativeProjectionLoadBenchmarkScenarios` avec 30 000 sites/100 instants et un profil haute fréquence contrôlé ;
- ajouter des factories de signaux synthétiques dont la valeur est calculable à partir de `(patient, canal, essai, index)` ;
- corriger les ouvertures de métadonnées EEGFormat sans `Dispose` ;
- ne pas modifier encore les représentations de données.

**Tests.**

- EditMode dans `HBP.Serialization.Tests` pour la déterminisme des factories, bornes et checksums ;
- benchmark Smoke exécutable en CI et profils Product/Extreme déclenchés manuellement ;
- répétition de dix ouvertures/fermetures pour distinguer cache attendu et fuite.

**Critères de validation.**

- rapport de référence reproductible avec mémoire par couche et P50/P95 des chemins importants ;
- checksums identiques entre répétitions ;
- aucune grosse série temporelle ajoutée au dépôt : les charges volumineuses sont créées en mémoire ou dans un répertoire temporaire ;
- les fixtures EEGFormat versionnées restent petites et limitées à la validation des formats.

### Étape 1 — Cache canonique des enregistrements bruts

**Objectif.** Faire de la copie managée brute l'unique propriétaire persistant des échantillons lus et garantir sa réutilisation entre visualisations et versions de protocole.

**Travail prévu.**

- introduire une entrée de cache brute identifiée par la source de données, indépendante du protocole ;
- séparer la clé/version du brut de celles des époques et dérivés ;
- construire hors verrou avec un chargement « single flight », puis publier atomiquement ;
- conserver le brut lors d'une modification de protocole ; invalider uniquement descripteurs, traitements, statistiques et projections ;
- invalider le brut lorsque le fichier/source, le dataset pertinent ou son identité change réellement.

**Tests.**

- deux visualisations du même enregistrement provoquent une seule lecture EEGFormat ;
- une modification sauvegardée du protocole reconstruit les dérivés sans relire le fichier ;
- une modification de source invalide bien le brut ;
- deux demandes concurrentes de la même clé ne créent qu'une entrée ;
- exceptions et annulations ne publient pas d'entrée partielle.

**Critères de validation.**

- rechargement chaud au moins aussi rapide que l'existant ;
- une seule copie managée persistante du signal brut par source ;
- aucune rétention de handle EEGFormat après la lecture ;
- aucune modification des valeurs scientifiques ou du comportement visible.

### Étape 2 — Index d'époques et vues sans copies persistantes

**Objectif.** Remplacer `RawValuesByChannel`, `BaselineValuesByChannel` et les clones structurels par des descripteurs pointant vers le brut, tout en indexant immédiatement tous les blocs.

**Travail prévu.**

- créer un `EpochDescriptor` partagé entre canaux avec fenêtre, baseline, événements et indices d'essai/sous-bloc ;
- conserver les bornes inclusives ;
- représenter fenêtres et baselines par vues ;
- maintenir un buffer de compatibilité réutilisable uniquement pour les API exigeant un tableau possédé ;
- conserver l'accès immédiat à tous les blocs pour la Trial Matrix.

**Tests.**

- comparaison nouveau/ancien chemin échantillon par échantillon sur fenêtres et baselines ;
- fenêtres positives, négatives, baseline incluse dans la fenêtre et baseline extérieure ;
- bornes inclusives et événements exactement sur les bornes ;
- blocs/sous-blocs successifs, alignés et de durées différentes ;
- protocoles multi-blocs et accès à un bloc jamais affiché en 3D.

**Critères de validation.**

- mêmes longueurs, valeurs, événements et ordres d'essais que l'ancien chemin ;
- aucun tableau persistant fenêtre/baseline par canal et par essai hors matérialisation explicitement justifiée ;
- mémoire des époques en `O(nombre_de_descripteurs)` au-dessus du brut, et non en `O(somme_des_fenêtres × canaux)` ;
- temps d'accès à un bloc non encore affiché sans nouvelle I/O.

### Étape 3 — Traitements, normalisation et statistiques dérivés

**Objectif.** Calculer uniquement la normalisation active et supprimer les matrices temporaires de normalisation, moyenne, médiane et SEM.

**Travail prévu.**

- classifier les traitements entre transformations ponctuelles, paramètres scalaires et opérations nécessitant un buffer ;
- appliquer les traitements sur les vues ou dans un buffer de travail réutilisable ;
- conserver uniquement les dérivés de la normalisation active ;
- utiliser des accumulateurs en streaming pour moyenne/variance/SEM et un buffer loué réutilisable pour la médiane ;
- versionner tous les dérivés par protocole, traitement, normalisation et averaging.

**Tests.**

- toutes les valeurs de `NormalizationType`, y compris `Auto` et le changement de préférence suivi d'un rechargement ;
- chaque classe de traitement, seule puis dans plusieurs ordres de pipeline ;
- moyenne, médiane, variance, SEM et statistiques d'événements sur jeux synthétiques à résultat analytique connu ;
- invalidation des statistiques après changement de protocole, normalisation ou averaging ;
- réutilisation inchangée du brut après ces invalidations.

**Critères de validation.**

- identité numérique sous la tolérance documentée par rapport au chemin actuel ;
- un seul mode normalisé actif en mémoire ;
- pic temporaire borné par le plus grand buffer de travail nécessaire, pas par le volume total des essais/baselines ;
- temps de normalisation/statistiques non dégradé, idéalement amélioré.

### Étape 4 — Horloge commune et fréquences mixtes

**Objectif.** Supporter proprement plusieurs fréquences dans des colonnes différentes et dans une même colonne, sans séries rééchantillonnées permanentes inutiles.

**Travail prévu.**

- conserver une grille native par source ;
- utiliser comme grille de navigation commune la fréquence maximale des colonnes affichées, conformément au comportement actuel ;
- conserver une grille de projection propre à chaque colonne ; une colonne mélangeant plusieurs fréquences utilise sa fréquence maximale interne ;
- mapper le temps physique vers chaque grille sans supposer que les fréquences sont commensurables ;
- ajouter le paramètre 3D `floor` / `round` / interpolation, avec interpolation par défaut ;
- conserver le pré-calcul natif complet et exact ; supprimer uniquement les représentations rééchantillonnées antérieures devenues redondantes.

**Tests.**

- 1 500 ms à 64 Hz et 2 048 Hz, dans deux colonnes puis dans une même colonne ;
- vérification des 97/3 073 positions pour une fenêtre `[0 ; 1 500 ms]` inclusive ;
- fréquences non commensurables, origines décalées et fenêtres commençant avant zéro ;
- trois politiques 3D sur temps exact, demi-échantillon et voisinage des bornes ;
- sous-blocs alignés de durées différentes, juxtaposés sur le timing du plus grand ;
- comparaison avec les valeurs interpolées actuelles pour le mode par défaut.

**Critères de validation.**

- chaque échantillon haute fréquence reste accessible exactement au scrubbing ;
- les trois politiques produisent les indices/valeurs attendus et sont sérialisées dans la visualisation ;
- aucune série basse fréquence permanente gonflée à la fréquence maximale globale lorsqu'elle appartient à une autre colonne ;
- dans une colonne mixte, une seule représentation aplatie nécessaire au pré-calcul natif peut subsister ;
- P95 du scrubbing sans régression supérieure à 5 %.

### Étape 5 — Préparation des colonnes et buffers stables

**Objectif.** Supprimer les copies et allocations managées restantes entre les vues temporelles et le pré-calcul natif, sans modifier ce dernier.

**Travail prévu.**

- remplir directement la matrice aplatie de projection ;
- supprimer `ActivityValuesOfUnmaskedSites` après remplacement par statistiques en streaming ;
- partager les séries zéro ou représenter explicitement les sites absents ;
- réutiliser les buffers UV et autres tableaux de mise à jour ;
- conserver la disposition contiguë lorsqu'elle améliore le transfert et la localité.

**Tests.**

- comparaison des matrices aplaties, limites, histogrammes et valeurs de sites ;
- sites masqués, absents et mélanges multipatients ;
- mesures d'allocations sur préparation, lecture et scrubbing ;
- benchmark natif inchangé à entrée identique.

**Critères de validation.**

- entrée native et projection numériquement identiques ;
- zéro allocation managée en régime stable lors du scrubbing et des mises à jour UV ;
- temps de préparation, transfert et lecture non dégradés ;
- disparition des copies uniquement utilisées pour calculer limites/statistiques.

### Étape 6 — Budget mémoire et cycle de vie

**Objectif.** Faire de `MemoryCacheLimit` le budget effectif des caches sans casser le rechargement instantané ni refuser une visualisation active exacte.

**Travail prévu.**

- comptabiliser brut, dérivés managés, projections natives et textures lorsque leur taille est connue ;
- épingler les données des visualisations actives ;
- évincer d'abord les dérivés inactifs, puis les bruts froids si nécessaire ;
- autoriser les données actives à dépasser la limite avec avertissement, sans réduction silencieuse ;
- pour `MemoryCacheLimit == 0`, calculer une limite automatique très élevée tout en conservant une petite marge pour Unity et le système ; la formule initiale proposée est 90 % de la RAM physique avec au moins 2 Gio réservés ;
- supprimer les `GC.Collect()` du chemin normal s'ils ne sont pas justifiés par les mesures.

**Tests.**

- comptabilité exacte sur entrées synthétiques de tailles connues ;
- ordre d'éviction, épinglage, réutilisation chaude et reconstruction des dérivés évincés ;
- dépassement par une visualisation active : avertissement sans corruption ni downsampling ;
- valeur explicite et valeur automatique `0` sur plusieurs tailles de RAM simulées ;
- cycles répétés de visualisations avec plateau mémoire attendu.

**Critères de validation.**

- caches inactifs contenus dans le budget ;
- aucune éviction des données actives ;
- rechargement instantané conservé tant que le brut reste chaud ;
- dépassement visible et explicable, jamais silencieux ;
- aucune croissance non comptabilisée après stabilisation.

### Étape 7 — Trial Matrix pavée et frugale

**Objectif.** Supprimer la limite de texture monolithique et réduire les copies CPU/GPU sans changer l'interprétation visuelle.

**Travail prévu.**

- calculer limites et couleurs sans concaténation globale ;
- découper les matrices dépassant la limite sûre en tuiles exactes ;
- ajouter les halos requis par le lissage afin d'éviter les coutures ;
- conserver les événements dans le repère global ;
- utiliser `Color32`/`SetPixelData` si le benchmark confirme le gain ;
- réutiliser/détruire explicitement les textures et virtualiser les tuiles visibles lorsque nécessaire.

**Tests.**

- matrices dont largeur, hauteur, puis les deux dimensions dépassent la limite simulée ;
- comparaison pixel/valeur entre rendu monolithique de référence et rendu pavé ;
- jointures avec et sans lissage 1D/2D ;
- position des événements, changement de colormap/limites et protocole complet ;
- cycles d'affichage/masquage pour vérifier la libération GPU.

**Critères de validation.**

- aucune création de texture supérieure à la limite ;
- aucune couture numérique et tolérance d'image respectée ;
- interprétation, zoom et position des événements inchangés ;
- mémoire CPU/GPU et temps de génération non dégradés ;
- aucune fixture volumineuse : matrices générées à la volée.

### Étape 8 — Graphes et structures secondaires

**Objectif.** Réduire les tableaux de points, objets et dictionnaires secondaires après stabilisation du modèle principal.

**Travail prévu.**

- représenter implicitement les abscisses régulières et réutiliser les buffers de rendu ;
- ne créer que les points exacts nécessaires au viewport sans modifier l'algorithme visuel ;
- remplacer progressivement les clés objet/chaîne très fréquentes par des IDs/indices stables lorsque le gain est mesuré ;
- supprimer les anciens graphes d'objets devenus redondants après `EpochIndex`.

**Tests.**

- tous les niveaux de zoom, pics, SEM, sélections d'essais et changement de canal ;
- identité numérique des points transmis au renderer et comparaison d'images ;
- résolution par ID, ordre des blocs/essais et événements après compactage ;
- allocations par interaction et cycles de création/destruction.

**Critères de validation.**

- tolérance d'image documentée respectée à chaque zoom ;
- aucune perte de pic ni modification de SEM/sélection ;
- zéro allocation répétitive évitable lors des interactions stables ;
- complexité du code égale ou inférieure au chemin remplacé.

La projection factorisée/GPU, la compression ou le cache de blocs natif et l'API EEGFormat par plages ne font pas partie de ces étapes initiales. Ils ne seront ouverts qu'après mesure du pré-calcul exact actuel sur les longues grilles. Les optimisations propres à CCEP et la mutualisation de colonnes restent hors périmètre.

## Protocole de benchmark et critères d'acceptation

### Scénarios minimaux

- scénario produit maximal usuel : 250 patients, environ 120 sites chacun, soit 30 000 sites, et 100 instants de projection ;
- variantes plus petites pour isoler les pentes : 1, 16 et 64 patients, puis extrapolation vérifiée sur le scénario complet ;
- protocole complet dans la Trial Matrix, puis visualisations successives d'un ou plusieurs blocs sans relecture du brut ;
- 1, 3 et 8 colonnes, mêmes blocs puis blocs différents ;
- fréquences homogènes puis mélange 64/2 048 Hz sur 1 500 ms, dans deux colonnes et au sein d'une même colonne ;
- iEEG, CCEP avec plusieurs sources, MEG continu long ;
- Trial Matrix avec/sans lissage, y compris largeur et hauteur supérieures à la taille maximale d'une texture ; graphes complets, sélection d'essais et tous les niveaux de zoom ;
- dix cycles ouverture/fermeture de visualisation pour détecter la rétention.

### Mesures

- pic et mémoire retenue managée, native, GPU, mémoire privée et working set ;
- nombre d'octets par couche : signal canonique, époques, statistiques, timeline, projection ;
- temps mural/CPU de chargement, normalisation, statistiques, préparation de colonne et projection ;
- P50/P95/P99 du déplacement de timeline et lecture ;
- P50/P95/P99 de mise à jour des coupes 3D, changement de bloc et réouverture d'une visualisation utilisant le même enregistrement ;
- allocations GC par frame et nombre/durée des collections ;
- métriques natives existantes : `generated_point_count`, `stored_value_count`, poids et cache spatial ;
- checksum des sorties, indices d'événements et erreur numérique maximale.

### Critères proposés

- aucune croissance inexpliquée après les cycles ouverture/fermeture ; le cache brut retenu doit être identifié et comptabilisé ;
- zéro allocation managée par frame pendant lecture/scrubbing stable ;
- résultats événementiels exacts et identité numérique sous une tolérance flottante documentée ;
- rechargement d'une visualisation sur des données chaudes au moins aussi rapide qu'actuellement ;
- premier chargement non dégradé et idéalement amélioré ;
- P95 de scrubbing/lecture sans régression supérieure à 5 % ;
- rendu Trial Matrix et graphes équivalent à chaque zoom sous une tolérance d'image documentée, sans différence susceptible de modifier l'interprétation ;
- aucune limite liée à une texture monolithique pour les matrices d'essais longues ; absence de couture visible ou numérique entre les tuiles ;
- maintien du pré-calcul `P × L` pour le scénario nominal tant qu'aucune alternative n'égale sa latence ;
- mémoire des époques proche d'une seule représentation canonique plus dérivés actifs, et non `2W+B` par défaut.

## Décisions fonctionnelles actées

1. **Normalisation.** La question concernait bien le changement de normalisation dans les préférences. Un recalcul lors du rechargement de la visualisation est acceptable ; le changement n'a pas besoin d'être instantané dans une scène déjà préparée. Le cache conserve donc le brut et seulement les paramètres/dérivés nécessaires à la normalisation active, pas une copie persistante pour chaque mode.
2. **Traitements de protocole.** Un traitement est immuable pendant l'utilisation d'une version du protocole. Sauvegarder sa modification change le protocole et entraîne le rechargement des visualisations et données. Les descripteurs d'époques, paramètres de traitement, statistiques et projections sont donc versionnés par protocole et invalidés ensemble. Le brut, qui ne dépend pas du protocole, reste réutilisable : le rechargement fonctionnel ne relit pas le fichier tant que sa source n'a pas changé.
3. **Bornes et juxtaposition.** L'épochage est inclusif aux deux bornes. Les groupes de sous-blocs successifs sont juxtaposés. Lorsque deux colonnes contiennent des sous-blocs alignés de durées différentes, le segment commun prend le timing du sous-bloc le plus grand ; le plus court est aligné dans ce segment selon les règles existantes de `Before`/`After`. Cette sémantique devient le contrat des tests de mapping temporel.
4. **Évaluation temporelle de la projection 3D.** Une même colonne peut contenir plusieurs patients/signaux de fréquences différentes. La navigation commune conserve la fréquence maximale des colonnes affichées ; une colonne mixte utilise sa propre fréquence maximale pour sa grille de projection. Ajouter un paramètre de visualisation avec trois politiques explicites : index inférieur (`floor`), index le plus proche (`round`) et interpolation. L'interpolation est le choix logique par défaut et reproduit l'intention actuelle. Les graphes continuent de relier visuellement leurs points. L'export NIfTI 4D utilise la grille discrète exacte de l'activité projetée et n'introduit pas une quatrième politique.
5. **Haute fréquence.** Le scrubbing 3D doit être exact sur tous les instants, y compris environ 3 073 positions pour 1 500 ms à 2 048 Hz. Aucun downsampling temporel automatique n'est admis dans le mode normal. Un futur mode « performance » pourra éventuellement réduire la résolution spatiale du volume selon le nombre de points, mais il est hors du présent plan.
6. **Budget mémoire.** Le réglage existant `MemoryCacheLimit` pilote le futur budget. Il faut remplacer son usage actuel limité à un avertissement approximatif par une comptabilité réelle des caches et projections. Les caches inactifs respectent la limite ; une visualisation active exacte peut la dépasser avec avertissement. La valeur `0` reste un mode automatique volontairement très élevé, avec seulement une marge de sécurité limitée pour Unity et le système.
7. **Mutualisation de colonnes.** Abandonnée : le scénario de colonnes réellement identiques est trop rare pour justifier la complexité.
8. **Équivalence visuelle.** Le contrat est une identité numérique sous tolérance flottante documentée, suivie d'une tolérance d'image documentée. Une différence ne doit jamais modifier l'interprétation. La Trial Matrix doit en outre supporter les époques/fréquences dépassant la taille maximale d'une texture, de préférence par pavage exact.
9. **CCEP.** Aucune optimisation spécifique CCEP n'est prévue dans cet audit. CCEP bénéficiera seulement des améliorations génériques du stockage brut, des époques et des temporaires tant que cela ne demande pas de chantier spécialisé.

## Arbitrages techniques non bloquants restant à mesurer

1. Mesurer le pré-calcul complet exact sur plusieurs milliers d'instants. Compression sans perte et cache de blocs préchargés ne seront étudiés que si la mesure justifie un chantier ultérieur ; ils ne bloquent pas les premières étapes.
2. Valider sur les machines cibles la formule automatique proposée pour `MemoryCacheLimit == 0` — 90 % de la RAM physique avec au moins 2 Gio réservés — puis ajuster uniquement si elle cause des problèmes.
3. Fixer la taille des tuiles de Trial Matrix, leur cycle de vie et la stratégie de halo de lissage à partir de `SystemInfo.maxTextureSize` et des mesures GPU.

## Conclusion

Le gain prioritaire ne viendra pas d'une micro-optimisation des dictionnaires ni de la suppression systématique du pré-calcul 3D. Il viendra du remplacement des copies persistantes fenêtre/baseline/normalisé par des vues sur un enregistrement brut conservé, puis de la suppression des matrices temporaires et des séries rééchantillonnées par colonne.

L'horloge temporelle commune proposée ne transforme pas les signaux en fonctions continues : les mesures restent sur leurs grilles natives et la politique de reconstruction 3D est un paramètre explicite. Les consommateurs coûteux gardent des grilles discrètes adaptées à leur usage. Le pré-calcul complet reste efficace pour 100 instants ; le cas rare de plusieurs milliers d'instants doit conserver un scrubbing exact grâce à une stratégie de stockage respectant autant que possible `MemoryCacheLimit`. Ainsi, HiBoP peut réduire fortement la mémoire managée sans masquer une perte d'information, tout en conservant l'accès instantané à tous les blocs et le rechargement rapide entre visualisations. La projection par blocs/compression et la lecture EEGFormat par plages restent des options à décider sur mesures ; les optimisations spécifiques CCEP sont exclues du chantier.
