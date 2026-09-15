# QUEST-030 — Parité du chargement Desktop / Quest

Date : 2026-09-15. Statut : **analyse et proposition, implémentation non engagée**.

Ce document remplace la proposition de correction précédente, retirée à la
demande du propriétaire. Le [signalement initial](QUEST-030-activity-visibility.md)
reste ouvert. L'analyse porte sur le code restauré à `HEAD`, antérieur à cette
intervention. Elle comprend une revue indépendante en lecture seule du cycle
d'initialisation et du contexte global. Le plan est révisé selon la précision
ultérieure du propriétaire : snapshot fondé sur les configurations existantes,
préférences envoyées uniquement à l'appairage, synchronisation différée.

## 1. Conclusion

Le transfert utilise les mêmes classes scientifiques, mais **pas le même parcours
de chargement que Desktop**. Quest reconstitue lui-même des données et des listes
de ressources, utilise une seconde initialisation de `Base3DScene`, réapplique
ensuite un état partiel, puis impose un calcul pour déclarer la scène prête.

Le défaut comportemental établi est l'appel de `PrepareRenderingAsync` à
`UpdateGenerator()` sans consulter
`PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate`.
Le parcours normal `Base3DScene.Update` consulte cette préférence pour les
calculs automatiques. Cette divergence suffit à expliquer un calcul déclenché
à réception alors que le calcul automatique est désactivé sur Quest.

Les préférences reçues sont celles de l'appairage et restent utilisées aux
envois suivants. **C'est le fonctionnement voulu pour cette étape du projet.**
Une modification Desktop après appairage n'a pas à être synchronisée. Le
chargement doit respecter les préférences globales installées sur chaque
application ; la comparaison de parité suppose un même jeu de préférences.

La cible est donc de réutiliser le chargement des configurations et
l'initialisation commune pour un snapshot de visualisation, sans reproduire
exhaustivement l'état d'une session vivante. Enlever l'appel forcé reste
nécessaire, mais ne suffit pas à supprimer les divergences d'initialisation.

Aucun défaut de `hbp_core` n'est démontré par cette analyse. Le plan ne prévoit
aucune modification native, d'ABI ou de binaire, ni sérialisation de la mémoire
interne des générateurs.

La recette exacte du signalement n'a pas été reproduite dans cette passe : le
projet et la modalité ne sont pas consignés. Les divergences de code sont établies ;
leur contribution à cette recette précise reste à mesurer.

## 2. Nettoyage effectué

Tous les changements de code de mon intervention précédente ont été retirés :

- Préférences propres à la scène/aux colonnes et remplacement des accès globaux.
- Paramètres de projection propres à chaque scène et mécanismes de dérogation.
- Capture/restauration des buffers internes des générateurs, nouveaux exports
  natifs et tests associés dans `hbp_core`.
- DLL Windows et bibliothèque Android issues de ces modifications.
- Ajouts au format de transfert, indicateurs et contournements de restauration.
- Corrections ponctuelles de sélection, calibration, ROI et tests ajoutés à
  cette occasion. Les constats utiles sont conservés ci-dessous pour le plan.

Les modifications de documentation qui précédaient mon intervention ont été
préservées. Le dépôt `hbp_core` est propre ; aucun changement sous `Assets` ne
subsiste. Le formateur du projet a été exécuté : `No C# files to format.`
Les résultats de tests de l'implémentation abandonnée ne valident pas ce plan.

## 3. Parcours actuels

### 3.1 Desktop : référence comportementale

Dans `Module3DMain.LoadAsync` puis `LoadSceneAsync` :

1. Validation des dépendances du projet et de la visualisation.
2. `Visualization.LoadAsync` : budget du cache, résolution et chargement des
   données, normalisation EEG, préparation des colonnes, timelines et icônes.
3. Instanciation du prefab de scène et `Base3DScene.Initialize`.
4. `Base3DScene.InitializeAsync` : ressources MNI, surfaces et IRM, préchargements
   selon les préférences, éventuelles surfaces issues d'IRM, implantations/sites,
   colonnes, initialisation des maillages des colonnes.
5. Enregistrement de la scène et des écouteurs Desktop.
6. `FinalizeInitialization` : sélection initiale, début du complément anatomique.
7. `OnAddScene`, `LoadConfiguration`, restauration de la représentation de surface.
8. `Update` commun : première mise à jour de visibilité, événement de chargement,
   abonnement aux préférences, géométrie, coupes, textures, sites et politique
   commune de calcul automatique ou explicitement demandé.

La présentation Desktop intervient déjà dans certaines étapes. Il faut préserver
les interactions nécessaires tout en empêchant leur absence de changer
l'initialisation scientifique. Déplacer seulement les trois derniers appels
dans une méthode commune ne suffira pas.

### 3.2 Quest : parcours distinct

Dans `SceneRestoration.PrepareAsync` :

1. Validation du payload, de son contexte et des ressources de référence.
2. Instanciation du prefab de contenu commun, sans présentation Desktop.
3. `Initialize`, puis reconstruction directe des IRM/surfaces et remplissage des
   collections de leurs managers.
4. Reconstruction spécifique des ressources FMRI/MEG et des `IconicScenario`.
   `Visualization.LoadAsync` n'est pas appelé.
5. Chargement de certains atlas/localisateurs selon leurs indicateurs d'affichage.
6. `InitializePreparedAsync` : quelques étapes communes sont réutilisées, mais
   l'ordre est propre au transfert : meshes, sites, colonnes, finalisation,
   configuration, géométrie forcée.
7. `ApplyState` : application tardive des sources, sélections, timelines, masques,
   ROI, atlas et calibration ; invalidation de l'activité.
8. `PrepareRenderingAsync` : attente qui **déclenche elle-même le calcul**.
9. Réparation du mode boucle après le calcul ; dans `QuestAnatomyView`, création
   de la présentation Quest, remplacement de la scène et reprise de lecture.

`Scene 3D.prefab` et `Scene 3D Content.prefab` référencent les mêmes classes et
prefabs de colonnes. C'est une base utile, mais les deux prefabs restent des
définitions distinctes : ce partage ne garantit ni l'ordre des appels, ni la
parité future des références et valeurs sérialisées.

### 3.3 Sérialisation actuelle

`DesktopSceneCapture` capture une copie de `Visualization`, sa configuration
courante, les ressources préparées et un complément `SceneState`/`ColumnState`.
La capture et l'écriture se font sans `await` entre les deux, après attente des
travaux suivis. Cette propriété doit être conservée.

Le format n'est pas celui d'une visualisation de projet : `PreparedDataJson`
omet les chemins/données sources de certains objets, sérialise des champs privés
des données traitées et neutralise certains callbacks habituels de sérialisation.
Les références globales sont résolues à partir de l'appairage.

**Conséquence : appeler simplement `Visualization.LoadAsync` sur le payload
actuel ne peut pas constituer la correction.** Le graphe a précisément été
construit pour éviter ce chemin. Il faut donner aux chargeurs communs un accès
aux ressources de l'archive et une entrée explicite pour les données déjà
préparées, avec les mêmes invariants et étapes de finalisation.

## 4. Constats pour cibler les configurations et l'initialisation

| Domaine | Constat dans le code actuel | Conséquence / travail nécessaire |
| --- | --- | --- |
| Sélection colonne/site | `ApplyState` sélectionne les sites avant leur colonne ; l'écouteur commun désélectionne les sites des colonnes non sélectionnées | Risque de perdre le site transféré. Sa persistance n'est pas obligatoire dans le périmètre retenu ; si conservée, elle doit passer par la configuration et son chargement commun. |
| Source et calibration CCEP | `Mode` et les setters de source rappellent `SetActivityData` puis `ResetSpanValues`, après `LoadConfiguration` | Une calibration transférée peut être écrasée. Restaurer les dépendances de source avant les paramètres explicites. |
| Ressources fonctionnelles | Quest reconstruit séparément FMRI, MEG et `IconicScenario` ; le chargement Desktop appelle aussi `LoadIcons` | Les données déjà calculées peuvent être réutilisées, mais la reconstruction des objets dérivés/événements doit relever des mêmes méthodes communes. |
| Atlas/localisateurs masqués | La capture enregistre leurs sélections, mais la restauration n'applique certaines valeurs que si l'affichage est actif | Masquer un overlay ne doit pas faire perdre le choix qui sera utilisé à sa réactivation. |
| Valeurs par défaut des overlays | Des getters de `FMRIManager` choisissent le premier élément du cache global ; l'index localisateur devient zéro si les volumes ne sont pas chargés | Le contenu d'une scène précédente et l'ordre de chargement peuvent influencer l'état. Capture sans effet de bord et distinction explicite entre absence de choix et choix par défaut. |
| Surface d'aperçu IRM | `CaptureConfiguration` conserve volontairement l'ancien nom persistant lorsqu'une `RuntimeSingleMesh3D` est sélectionnée ; l'ouverture choisit selon sa politique initiale | La configuration sauvegardée du projet ne suffit pas à décrire la sélection vivante. Si ce choix doit persister, enrichir la configuration avec une référence résoluble dans les ressources du snapshot. |
| ROI | La capture utilise `roi.name` et le rayon animé `Sphere.Radius`, alors que le masque utilise `InfluenceRadius` | Capturer le nom métier et le rayon scientifique ; l'instant d'animation ne doit pas modifier une ROI transférée. |
| Animation des sphères | L'animation passe par `Radius` → `OnChangeRadius` → `OnChangeSphereParameters` → recalcul des masques/invalidation | Une animation de présentation provoque une modification scientifique. Séparer ces notifications dans les objets communs. |
| Géométrie/effacement | Le chemin préparé force `UpdateGeometry` avant de restaurer les masques, pour éviter leur effacement au prochain `Update` | Cette dépendance doit devenir une étape normale commune, avec validation de la topologie avant les masques. |
| Lecture temporelle | Le calcul forcé arrête la navigation, puis Quest réapplique boucle/lecture dans deux endroits | Supprimer la nécessité de réparer les effets du calcul forcé ; seuls les choix temporels retenus comme persistants seront chargés par la configuration commune. |
| Annulation et fermeture | Le chemin normal lie l'initialisation à la durée de vie de la scène ; le chemin préparé ne réalise pas le même lien | Une seule gestion d'initialisation et d'annulation ; conserver l'attente des travaux avant libération des ressources. |

Ces constats ne constituent pas une obligation de tout rendre persistant.
Les défauts qui altèrent une configuration retenue ou changent le comportement
commun sont à corriger. Les informations transitoires peuvent reprendre les
valeurs normales d'ouverture Desktop. On ne rajoute pas de système parallèle
d'état de session pour les conserver.

### Vérification spécifique : site sélectionné

- `VisualizationConfiguration.FirstSiteToSelect` et `FirstColumnToSelect`
  existent, mais sont marqués `[JsonIgnore]` (lignes 126–127). Ils servent
  notamment à une transition en mémoire dans `Module3DMain`.
- `Column3D.CaptureConfiguration` capture alpha et configuration des sites
  (liste noire, surbrillance, couleur, labels), mais pas le site sélectionné.
- Le transfert actuel l'envoie séparément :
  `DesktopSceneCapture` (87) remplit `ColumnState.SelectedSite`.
  Il met aussi `FirstColumnToSelect = -1` pour éviter la sélection initiale
  habituelle avant l'application de son état complémentaire.

Le site sélectionné est donc actuellement envoyé, **mais pas comme un champ
persistant de configuration**. Sa conservation n'est pas un critère de réussite
imposé ici. Si elle est retenue ultérieurement, elle devra être intégrée au
contrat de configuration avec une identité stable et le même chargement Desktop.

### Repères dans le code restauré

- [Module3DMain.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Module3DMain.cs) : `LoadAsync` (314), `LoadSceneAsync` (381), `Preload3D` (424).
- [Visualization.cs](../../../../Assets/Scripts/HBP/Core/Data/Visualization/Visualization.cs) : `LoadAsync` (217), normalisation (525), chargement des colonnes (528).
- [Base3DScene.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs) : `Update` (827), sélection site (1326), initialisation/configuration (1736–1826), capture (1930), invalidation (2096), initialisation normale (2344).
- [Base3DScene.Transfer.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Transfer.cs) : `CapturePreparedAsync`, `InitializePreparedContentAsync`, `PrepareRenderingAsync`.
- [SceneRestoration.cs](../../../../Assets/Scripts/HBP/Transfer/Scene/SceneRestoration.cs) : `PrepareAsync`, `RestoreFunctionalAsync`, `LoadStandardFeaturesAsync`, `ApplyState`.
- [DesktopSceneCapture.cs](../../../../Assets/Scripts/HBP/Transfer/Scene/DesktopSceneCapture.cs), [PreparedDataJson.cs](../../../../Assets/Scripts/HBP/Transfer/Scene/PreparedDataJson.cs) : contrat d'archive actuel.
- [Column3DCCEP.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Column3DCCEP.cs) : setters de source, `ResetSpanValues` (290, 429), `LoadConfiguration` (553).
- [FMRIManager.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Modules/FMRIManager.cs) : sélections (74–224).
- [Sphere.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Objects/Sphere.cs), [ROI.cs](../../../../Assets/Scripts/HBP/Data/Module3D/Objects/ROI.cs) : rayons et événements.

## 5. Contrat cible révisé

### 5.1 Snapshot de visualisation fondé sur les configurations

Au moment de l'envoi, capturer les données/ressources de la visualisation et
ses configurations courantes. Le snapshot est autonome pour les ressources
embarquées, avec les références globales de l'appairage existant.

La configuration devient la source des choix que l'on veut retrouver à
l'ouverture, sur Desktop comme sur Quest. Utiliser les classes existantes
(`VisualizationConfiguration`, configurations de colonnes et de sites, ROI,
coupes) et les enrichir de façon ciblée. La capture de ces configurations ne
doit pas imposer une sauvegarde du projet source.

Il n'est pas demandé de transporter chaque sélection temporaire, tâche en
cours, état d'animation, événement ou cache de la scène vivante. L'absence d'un
état transitoire est acceptable si le chargement lui applique le comportement
Desktop normal. La persistance du site sélectionné n'est pas obligatoire.

Les `SceneState` et `ColumnState` actuels seront examinés champ par champ :

- Donnée/ressource nécessaire à la visualisation : conserver dans le payload.
- Choix à rendre persistant : utiliser ou enrichir la configuration appropriée
  et son chargement commun ; éviter deux valeurs concurrentes dans le payload.
- État transitoire : ne pas le promouvoir automatiquement en nouveau contrat
  de persistance. Sa restauration spécifique peut disparaître si le comportement
  normal d'ouverture est celui retenu.

Cette évolution se fera sur le format et les mécanismes existants. Elle ne
requiert pas un nouveau modèle complet de snapshot de session.

### 5.2 Préférences uniquement à l'appairage

Conserver `PairingSnapshot`, le contexte appairé et son installation actuelle.
Toutes les décisions métier lisent les préférences via
`PersistentDataManager.UserPreferences`. Ne créer aucune préférence propre à
une scène ou colonne.

Les nouveaux envois de visualisation ne recapturent ni ne remplacent les
préférences. Les mécanismes de synchronisation arriveront dans un chantier
ultérieur. Ce plan n'introduit donc ni version des globals par livraison, ni
transaction de remplacement scène/préférences, ni nouvelle fermeture de scène
motivée par un envoi de préférences.

Conséquence assumée : après un changement de préférences Desktop non envoyé,
les applications peuvent prendre des décisions différentes. Ce n'est pas une
dérogation Quest : c'est le même comportement commun avec des entrées globales
différentes. Pour tester la parité, fixer des préférences identiques à
l'appairage. Tester séparément que les changements Desktop ultérieurs ne sont
pas propagés.

### 5.3 Réouverture et résultat calculé antérieurement

La décision confirmée reste : **« Comportement d'une réouverture Desktop »**.
Elle s'applique au snapshot de données et aux configurations capturées, avec
le contexte de préférences installé sur le destinataire.

Un ancien résultat d'activité, sa validité historique et les calculs en cours
ne sont pas à transférer. L'état dérivé se reconstruit par le cycle commun.
Avec auto compute désactivé dans les préférences Quest reçues à l'appairage,
le transfert ne déclenche aucun calcul supplémentaire. Avec auto compute
activé, les conditions normales communes déterminent le calcul.

Les données traitées déjà transportées font partie des entrées du snapshot.
Il ne s'agit pas de refaire l'importation et tous les traitements bruts pour
simuler une nouvelle ouverture du projet source. La finalisation des objets
qui les consomment doit en revanche rester commune et complète.

### 5.4 Présentation

Caméras, disposition, pose/échelle XR, fenêtres et interactions restent propres
à chaque présentation. Les seuils, couleurs de données, calibration, ROI et
coupes qui figurent dans les configurations conservent leur sens commun.

Une interaction appelle les méthodes communes. L'attachement d'une vue ou une
animation ne doit pas modifier les paramètres scientifiques ni demander un
calcul. Les coordonnées de présentation ne remplacent pas les coordonnées
scientifiques des ressources.

## 6. Implémentation proposée

### 6.1 Compléter les configurations utiles

Partir des champs déjà capturés et des écarts constatés au §4. Ne pas transformer
l'inventaire de tous les champs runtime en une liste d'ajouts obligatoires.

Les priorités proposées sont :

1. Garantir la capture et le rechargement corrects des configurations existantes :
   calibration, influence, visibilité/alpha, sites, ROI et coupes. Pour les ROI,
   utiliser le nom métier et le rayon scientifique, indépendamment de l'animation.
2. Intégrer aux configurations les choix actuellement appliqués après leur
   chargement et qui déterminent l'interprétation de la visualisation :
   source/mode CCEP, ressource fonctionnelle choisie, choix d'overlay et de
   surface lorsqu'ils doivent être retrouvés à l'ouverture.
3. Pour chacun de ces ajouts, définir sa valeur à l'ouverture d'une ancienne
   configuration, son identité de ressource et son ordre d'application.
   Un overlay masqué peut garder son choix configuré sans exiger une capture
   de tout l'état de son manager.
4. Ne pas ajouter par défaut la sélection du site, la sphère sélectionnée, le
   mode d'édition ROI ou la lecture en cours. Leur conservation n'est pas
   nécessaire pour corriger la parité du chargement. Un choix temporel jugé
   persistant suivra le même mécanisme de configuration.

Étendre ensemble capture, clone, sérialisation et chargement des configurations
concernées. Les nouveaux champs devront être lisibles avec leurs valeurs par
défaut dans les anciens projets ; les éventuelles migrations du format de
transfert resteront ciblées. Ne pas supprimer indistinctement les champs
existants avant d'avoir identifié leur rôle.

### 6.2 Partager l'initialisation existante

Le point central reste la suppression des deux orchestrations scientifiques.
Factoriser les étapes existantes de Desktop et les faire utiliser par l'entrée
archive, à partir d'une visualisation et de ses configurations.

Deux provenances de données restent légitimes :

- Desktop prépare les données à partir du projet.
- Le transfert fournit les données et ressources déjà préparées du snapshot.

Ces provenances convergent vers les mêmes méthodes de construction/finalisation
des données utilisables et des objets possédés par `Base3DScene`. La reprise
des données FMRI/MEG, timelines, icônes et événements doit satisfaire les mêmes
invariants, sans réécrire une finalisation autonome dans Quest.

Conserver le format d'archive, ses ressources préparées et ses validations.
Adapter les points d'entrée des chargeurs seulement là où nécessaire. Appeler
aveuglément `Visualization.LoadAsync` sur le payload actuel ne convient pas :
les données sources du projet en sont volontairement absentes. Inversement,
cela ne justifie pas de sauter l'initialisation des objets communs.

Le parcours commun doit couvrir :

1. Disponibilité des ressources et construction de leurs objets dépendants.
2. Initialisation des managers, implantations/sites, colonnes et timelines.
3. Chargement des configurations dans l'ordre de leurs dépendances :
   sources avant calibration, topologie avant masques/coupes, ressources avant
   indices éventuels.
4. Finalisation, événements et premier `Update`, sans seconde remise à zéro
   des valeurs configurées.
5. Cycle normal de calcul, invalidation et fermeture.

Extraire une méthode ou un service commun de taille adaptée à ces besoins,
sans imposer une nouvelle architecture générique de chargement. Faire évoluer
Desktop et Quest vers ce même parcours ; garder les hooks de présentation et
les comportements Desktop vérifiés.

### 6.3 Retirer le calcul forcé et adapter l'attente

`PrepareRenderingAsync` ne doit plus appeler `UpdateGenerator` de sa propre
initiative. La décision de calcul reste celle du cycle commun et des commandes
utilisateur.

Sa condition de disponibilité doit accepter une scène correctement initialisée
avec une activité non calculée lorsque les préférences le demandent. Une
simple suppression de l'appel, sans revoir l'attente qui exige un générateur
à jour, risquerait de bloquer indéfiniment auto compute désactivé.

Attendre les travaux nécessaires effectivement démarrés, propager leurs erreurs
et respecter l'annulation. Retirer les réparations tardives rendues nécessaires
par ce calcul forcé ; charger les paramètres persistants via leurs configurations.

### 6.4 Préserver transport, appairage et durée de vie

Conserver le fonctionnement actuel de réception, validation, candidat et
publication, ainsi que les identités de livraison, retry et ACK. Il n'y a pas
de chantier de synchronisation des globals dans cette correction.

Préserver la capture et l'écriture sans mutations intercalées après les attentes
de préparation. Cela capture les réglages au point de snapshot retenu, sans
transformer l'envoi en synchronisation continue avec Desktop.

Conserver `Base3DScene.Lifetime` : attendre les travaux avant de libérer les
ressources et archives, lier toute initialisation à la durée de vie de la scène.
Les changements d'appairage conservent leur fermeture/installation existante ;
un envoi normal continue à utiliser son contexte déjà installé.

Garder les prefabs et adaptateurs de présentation existants. Vérifier leurs
références communes et ne modifier leur structure que si une différence
effective empêche le chargement commun. Aucune refonte des prefabs n'est un
prérequis à cette correction.

## 7. Jalons et critères de sortie

| Jalon | Travail prévu | Critère de sortie |
| --- | --- | --- |
| 1. Configurations et référence | Cartographier configuration existante / complément de transfert / transitoire ; caractériser l'ouverture Desktop avec les préférences d'appairage | Liste ciblée des champs à conserver ou enrichir ; aucune obligation de capturer tous les états runtime. |
| 2. Configuration commune | Étendre les configurations nécessaires et leur capture/clone/chargement ; corriger les pertes ou écrasements de valeurs persistantes | Les mêmes configurations se rechargent correctement sur Desktop et à partir du snapshot. |
| 3. Initialisation commune | Factoriser les étapes existantes et y faire converger données de projet et données préparées ; supprimer les finalisations et applications de paramètres parallèles | Même initialisation des objets de scène, mêmes événements et mêmes défauts pour les champs non persistés. |
| 4. Politique de calcul | Retirer le calcul forcé de préparation et adapter la disponibilité ; conserver appairage et transport | Auto compute désactivé sur Quest : aucun calcul induit par réception, aucune attente infinie. |
| 5. Validation | Tests de configurations et de cycle commun, puis recette Windows IL2CPP / Quest ARM64 | Parité des paramètres retenus et du comportement sous mêmes préférences, y compris second envoi et annulation. |

Les champs choisis comme persistants enrichissent les configurations existantes.
Le payload conserve son rôle d'enveloppe de visualisation et de ressources.
Il n'y a ni nouveau système de sérialisation exhaustive de session, ni révision
des préférences à chaque livraison, ni modification de `hbp_core`.

## 8. Vérification prévue

### Référence

Comparer le chargement commun des configurations du snapshot sous les mêmes
préférences, avec présentation Desktop et Quest. Le parcours Desktop de
référence ne doit pas appeler une préparation qui force le calcul pour
fabriquer artificiellement la même sortie.

Vérifier les valeurs persistantes, les défauts des états non conservés, les
événements et les actions suivantes. Ne pas comparer systématiquement tous
les champs de la scène vivante source. Un site sélectionné différent est
acceptable lorsqu'il ne fait pas partie du contrat de configuration retenu.

### Scénarios

- Les six modalités et scènes mono/multi-patients : initialisation des mêmes
  objets communs avec les ressources préparées adéquates.
- Auto compute false puis true dans deux appairages de test : valeur identique
  à celle capturée à l'appairage, absence/présence de calcul selon le cycle normal,
  recalcul manuel fonctionnel.
- Modification des préférences Desktop après appairage : un nouvel envoi
  conserve les préférences Quest appairées. Cette absence de propagation
  constitue le résultat attendu, pas un échec de parité.
- Configurations actuelles et enrichies : calibration CCEP avec source non
  initiale, ressources fonctionnelles, influence, alpha, ROI/coupes, choix
  d'overlays actifs ou masqués et surface choisie.
- Anciennes configurations sans nouveaux champs : mêmes valeurs par défaut
  Desktop/Quest, absence de sélection spécifique acceptée.
- Sources jamais calculées, calculées manuellement ou périmées : aucun ancien
  buffer de projection transporté, comportement de réouverture selon les
  préférences installées.
- Animation d'une ROI ou déplacement de présentation : aucun changement de
  rayon scientifique ni calcul supplémentaire.
- Second envoi de configurations différentes sous le même appairage, cache
  déjà rempli par une scène précédente, retry après ACK perdu, annulation,
  fermeture pendant chargement et déconnexion.
- Après réception : changer source/calibration, modifier une ROI, réactiver un
  overlay, demander un calcul et fermer. Les transitions restent communes.

L'égalité des paramètres persistants et décisions de calcul est stricte sous
les mêmes entrées. Les résultats numériques Windows/ARM64 sont comparés avec
les tolérances scientifiques justifiées, sans modifier les paramètres pour
obtenir la parité. Les états de présentation ne sont pas comparés.

Les tests Unity resteront non bloquants : `async Task` et `await`, aucun
`Wait`/`Result` ou assertion NUnit asynchrone bloquante sur UniTask. Exécution
par MCP si l'éditeur est ouvert, CLI officielle sinon, conformément à
`AGENTS.md`. Formatage C# avant revue et recette sur appareil avant de déclarer
le défaut corrigé.

## 9. État de cette passe

Le retrait de l'implémentation précédente reste acquis. Cette révision modifie
uniquement le plan et le rapport du signalement. Aucune correction de code,
modification de prefab, compilation ou installation sur casque n'a été réalisée.

La synchronisation des préférences est explicitement différée. Le travail à
implémenter porte sur les configurations du snapshot et le comportement commun
de leur chargement.
