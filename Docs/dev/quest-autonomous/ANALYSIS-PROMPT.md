# Prompt de cadrage — nouveau produit HiBoP Quest autonome

## Mode d'emploi

Le présent document est un prompt autonome destiné à être réinjecté dans une nouvelle conversation de conception et d'analyse. Il doit permettre d'ouvrir un nouveau chantier produit sans importer automatiquement les décisions de l'ancienne feature XR.

La prochaine conversation devra commencer par un bref inventaire en lecture seule des éléments immédiatement observables, reformuler sa compréhension, puis ouvrir une courte phase de questions/réponses avec le propriétaire du produit. L'audit approfondi du code peut avancer pendant et après ces réponses. La conversation devra ensuite produire de nouvelles spécifications, une architecture cible, une roadmap et un premier backlog de prototype. Elle ne doit pas commencer par modifier le code.

---

## Prompt à réinjecter

Tu es chargé de cadrer un **tout nouveau produit HiBoP Quest autonome** à partir du logiciel HiBoP existant. Le but de cette conversation est d'aboutir à une vision produit explicite, une architecture réaliste, de nouveaux documents de spécification et une roadmap incrémentale, puis à un plan de réalisation d'un premier prototype volontairement réduit.

Lis entièrement ce prompt avant d'agir. Fais d'abord un inventaire bref et non intrusif de l'état observable des dépôts, résume ta compréhension et pose les questions prioritaires. Poursuis ensuite l'audit approfondi pendant ou après mes réponses. Ne lance pas immédiatement une implémentation et ne transforme pas l'analyse en une migration massive par défaut.

### 1. Nature du chantier

Il s'agit d'un **nouveau projet produit**, et non de la continuation automatique de l'architecture client/serveur de la feature XR actuelle.

Le travail XR déjà réalisé doit être conservé intact comme source d'expérience, de code réutilisable, de mesures et de prototypes. En revanche :

- ne fusionne pas en bloc la branche ou le chantier XR existant dans la nouvelle baseline ;
- ne considère pas que sa topologie, ses ADR, ses gates ou ses politiques sont gravés dans le marbre ;
- ne fais aucun merge, rebase, cherry-pick, reset, suppression ou déplacement destructif pour « repartir proprement » ;
- commence par inspecter l'état Git réel, les branches, les commits et les worktrees ;
- propose ensuite une stratégie sûre pour créer le nouveau chantier depuis la baseline HiBoP appropriée ;
- réutilise sélectivement les concepts, tests, mesures ou fichiers de l'ancien chantier uniquement après avoir démontré qu'ils servent la nouvelle architecture ;
- distingue toujours « reprendre une preuve ou une implémentation utile » de « hériter d'une décision produit ».

« Repartir de zéro » signifie ici **repartir de zéro dans les décisions et l'architecture**, pas effacer le travail existant.

Les documents actuels sous `Docs/dev/xr/`, les sources sous `XR/`, les packages sous `Shared/Packages/` et les artefacts associés sont des références consultables. Ils ne constituent pas le cahier des charges du nouveau produit.

### 2. Dépôts et contexte technique à inspecter

Les dépôts locaux connus sont :

- `C:\HBP\Software\HiBoP` : application Unity HiBoP mature ;
- `C:\HBP\Software\hbp_core` : moteur scientifique natif principal ;
- `C:\HBP\Software\hbp_math` : fonctions mathématiques natives ;
- `EEGFormat` : dépendance native d'import/export EEG, actuellement consommée par HiBoP mais considérée optionnelle pour le produit Quest.

HiBoP utilise actuellement Unity `6000.5.2f1`. Le chantier XR expérimental utilise la même version. Le projet Desktop et le projet expérimental XR utilisent déjà URP `17.5.0`, ce qui constitue un point favorable à une réunification, mais cette compatibilité doit être vérifiée dans l'état réel des manifests et des settings.

Inspecte notamment :

- l'organisation réelle de `Assets/Scripts/HBP` et ses dépendances ;
- les asmdefs actuelles, leurs références et les cycles éventuels ;
- les scènes, prefabs, ScriptableObjects et services globaux ;
- les wrappers C# de `hbp_core`, `hbp_math` et `EEGFormat` ;
- les plugins natifs et leurs import settings par OS/architecture ;
- la sérialisation actuelle des projets et visualisations ;
- la manière dont une visualisation, une colonne, un cerveau et leurs transformations sont représentés ;
- les comportements actuels de déplacement, rotation, sélection et mise à jour du rendu ;
- les scripts de build Desktop existants ;
- les Build Profiles éventuellement déjà présents ;
- le prototype HoloLens et la cause réelle de sa dérive ;
- le code XR expérimental potentiellement réutilisable.

N'interroge pas l'utilisateur sur une information que le dépôt permet d'observer de manière fiable.

### 3. Vision produit de départ

La cible envisagée est un **HiBoP capable de produire deux applications depuis un seul projet Unity et une seule base de code** :

1. **HiBoP Desktop**, qui conserve l'expérience actuelle, l'import des données, la préparation des projets, les fonctions d'édition avancées et la puissance de calcul du poste ;
2. **HiBoP Quest**, qui reçoit une visualisation préparée, l'explore et la modifie localement avec une interface XR, sans dépendre du Desktop pour chaque interaction ou chaque frame.

Le Quest doit être capable de continuer à fonctionner lorsqu'il perd sa connexion au Desktop. Lorsqu'une connexion existe, les commandes utiles doivent de préférence être transmises en temps réel au Desktop. Lorsqu'elle n'existe plus, elles sont appliquées localement et peuvent être resynchronisées à la reconnexion.

La V1 n'a pas besoin de porter toute l'expérience de gestion de projet Desktop. La direction produit initiale est :

> **Desktop pour préparer ; Quest pour explorer.**

La promesse n'est pas que le Quest aura immédiatement les mêmes performances ou toutes les interfaces que le Desktop. Une version XR plus limitée ou plus lente est acceptable si elle reste scientifiquement cohérente, agréable à utiliser et clairement positionnée comme une expérience mobile/XR.

### 4. Contrainte architecturale numéro 1 : une source comportementale unique

La contrainte la plus importante est la suivante :

> Une modification d'un comportement métier ou scientifique dans HiBoP doit affecter immédiatement les builds Desktop et Quest, sans copie, portage manuel ni réimplémentation parallèle de la feature.

Le nouveau produit doit donc partir de la prémisse suivante :

- **un seul projet Unity** ;
- **un seul `Packages/manifest.json` et un seul lockfile** dans la baseline cible ;
- **deux cibles ou Build Profiles**, Desktop et Quest ;
- **un seul modèle métier** ;
- **une seule logique de visualisation et de calcul** ;
- des adaptateurs différents uniquement aux frontières réellement dépendantes de la plateforme.

Le projet HoloLens historique peut servir d'intuition fonctionnelle — HiBoP complet avec une interaction spatiale — mais pas de modèle de maintenance. Sa principale erreur à ne pas reproduire est d'avoir laissé apparaître une copie divergente de HiBoP.

Le résultat recherché ressemble plutôt à ceci :

```text
Souris / clavier Desktop ─────┐
                             ├─> intentions et commandes communes
Mains / contrôleurs Quest ────┘
                                         ↓
                              comportements HiBoP communs
                                         ↓
                                  état commun du modèle
                                         ↓
                     présentation Desktop / présentation Quest
```

Pour une coupe :

```text
DesktopCutInput / QuestCutGizmo
                ↓
       même commande SetCutPlane
                ↓
      même service de coupe HiBoP
                ↓
          même API hbp_core
                ↓
   résultat affiché par la cible active
```

Les différences d'entrée et de présentation sont normales. La duplication des règles, de l'état, des conversions scientifiques ou du scheduling ne l'est pas.

### 5. Refactorisation minimale d'un logiciel mature

HiBoP est un logiciel ancien et mature. Une réorganisation complète des dossiers, namespaces et assemblies serait coûteuse, risquée et probablement inutile pour valider le produit.

Ne propose pas comme précondition :

- de déplacer tout `Assets/Scripts/HBP` ;
- de redessiner toutes les assemblies ;
- de convertir immédiatement tout le projet en packages UPM ;
- de supprimer tous les singletons ou tous les MonoBehaviours ;
- de réécrire l'UI Desktop ;
- de « nettoyer l'architecture » sans lien direct avec le prototype.

Applique une stratégie de **refactorisation opportuniste et verticale** :

1. choisir une tranche fonctionnelle nécessaire au prototype ;
2. identifier la logique actuellement mêlée à l'entrée ou à l'UI Desktop ;
3. extraire uniquement le seam indispensable ;
4. faire utiliser ce nouveau seam par le Desktop existant ;
5. brancher le Quest sur exactement la même implémentation ;
6. supprimer l'ancien chemin redondant dès que la bascule est vérifiée ;
7. ne passer à la feature suivante qu'après avoir empêché la divergence.

Les asmdefs et packages sont des moyens, pas des objectifs. Il est acceptable de conserver l'essentiel de la structure historique si quelques abstractions ciblées suffisent.

Les frontières probablement nécessaires sont néanmoins :

- entrée utilisateur ;
- composition/bootstrap de la plateforme ;
- file picker et accès aux fichiers ;
- stockage ou source de session ;
- cycle de vie de l'application ;
- capabilities disponibles ;
- présentation Desktop versus XR ;
- réglages de performance par plateforme ;
- sélection du binaire natif correspondant à l'architecture.

Évite un service générique omnipotent du type `IPlatformService`. Préfère quelques interfaces concrètes correspondant à des besoins observés.

### 6. Topologie Unity cible à étudier

La topologie de départ à valider est un seul projet avec deux compositions :

```text
HiBoP/
├── Assets/
│   ├── Scripts/HBP/                 # code HiBoP actuel, modifié avec parcimonie
│   ├── Scripts/HBP/XR               # intégrations strictement XR/Quest si nécessaire
│   ├── _Scenes/
│   │   ├── DesktopBootstrap.unity   # pas nécessairement obligatoire étant donné la présence de HiBoP.unity
│   │   ├── QuestBootstrap.unity
│   │   └── contenu fonctionnel commun
│   └── Plugins/Native/
│       ├── Windows/x86_64/
│       ├── Linux/x86_64/
│       ├── macOS/arm64/
│       └── Android/arm64-v8a/
├── Packages/
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/
└── Assets/BuildProfiles/
    ├── Desktop.asset
    └── Quest.asset
```

Cette arborescence est illustrative. Ne lance pas une migration de dossiers pour la reproduire littéralement. Cherche le plus petit changement permettant de garantir les dépendances souhaitées.

Étudie l'usage de :

- Build Profiles Unity 6 versionnés ;
- listes de scènes distinctes ;
- paramètres Player propres à chaque cible ;
- symboles explicites tels que `HIBOP_DESKTOP` et `HIBOP_QUEST` ;
- asmdefs avec `includePlatforms`, `excludePlatforms` ou `defineConstraints` seulement lorsque cela apporte une frontière utile ;
- import settings précis pour chaque plugin natif ;
- XR Management et OpenXR activés pour Android/Quest uniquement ;
- rapports de build vérifiant les assemblies, scènes, plugins et assets réellement embarqués.

Le manifeste de packages doit de préférence être stable et contenir l'union raisonnable des dépendances Desktop et XR. Ne propose de réécrire automatiquement `Packages/manifest.json` selon la cible que si une incompatibilité concrète empêche le manifeste unique. Dans ce cas, évalue les risques de résolution UPM, de modification du lockfile, de recompile et de divergence CI, et préfère un workspace de build isolé plutôt qu'une mutation silencieuse du projet de travail.

Le fait qu'un package XR soit installé dans le projet ne signifie pas qu'il doit être activé ou inclus dans le Player Desktop. Prouve l'isolation avec les réglages de cible et le Build Report au lieu de la supposer.

### 7. `hbp_core`, `hbp_math` et `EEGFormat`

#### `hbp_core`

La taille de `hbp_core` est considérée négligeable devant les autres enjeux. La stratégie préférée est d'embarquer la bibliothèque complète, pas de créer artificiellement une version « mini Quest » qui deviendrait un nouveau produit natif à maintenir.

La cible est :

```text
même code source hbp_core
  ├─ hbp_core.dll Windows x64
  ├─ libhbp_core.so Linux x64
  ├─ hbp_core.bundle macOS ARM64
  └─ libhbp_core.so Android ARM64
```

Le wrapper C# et l'API scientifique doivent être uniques. Seul le binaire sélectionné par Unity change selon la cible.

Faits déjà observés à revérifier :

- une compilation Release Android ARM64 de `hbp_core` a déjà été obtenue avec le NDK ;
- cela ne prouve pas encore le runtime Unity IL2CPP sur Quest, la parité, les performances ou l'endurance ;
- certaines optimisations de coupe actuelles utilisent SSE2 sur x86 et prennent un fallback scalaire sur ARM ;
- le pool de threads natif peut utiliser de nombreux workers et devra éventuellement être paramétrable sur Quest.

Ces points sont des travaux normaux d'optimisation et de qualification du nouveau produit, pas des raisons de rejeter l'architecture. Si nécessaire, ajoute un chemin NEON ou ARM optimisé dans le même `hbp_core`, avec les mêmes contrats scientifiques.

#### `hbp_math`

`hbp_math` est également petit. Il peut être embarqué intégralement si les fonctionnalités communes l'utilisent. Il doit suivre la même règle de source unique et de build multiplateforme.

#### `EEGFormat`

`EEGFormat` est **optionnel et hors du périmètre initial**. Ne fais pas de son portage une condition de la V1 ni du premier prototype.

À plus long terme, deux modèles sont possibles :

1. Desktop exporte des données déjà normalisées dans un format HiBoP portable ; Quest est autonome pour l'exploration sans connaître EDF, Micromed, Elan, BrainVision ou FIF ;
2. Quest importe lui-même les formats EEG d'origine ; `EEGFormat` doit alors être porté et qualifié.

Le premier modèle est préféré pour les premières versions. Une autonomie forte à l'exécution ne nécessite pas une autonomie d'import.

### 8. Projet portable « tout embarqué » : direction ultérieure, pas V1

La possibilité d'exporter un fichier projet contenant toutes les données nécessaires et de l'ouvrir directement sur le Quest est une direction produit attractive, mais elle n'est pas requise pour le premier prototype et ne doit pas alourdir artificiellement la V1.

À terme, un export du type « Exporter pour HiBoP Quest » pourrait produire une capsule versionnée contenant notamment :

- définition de projet et de visualisation ;
- surfaces, sites et repères ;
- volumes nécessaires ;
- données préparées nécessaires aux features admises ;
- protocoles et timelines ;
- paramètres initiaux ;
- caches coûteux éventuellement précalculés ;
- versions des schémas et moteurs scientifiques ;
- hashes d'intégrité.

Mais la première version peut simplement transférer depuis un Desktop connecté l'état et les assets de la visualisation courante vers la mémoire ou le cache du Quest.

Ne confonds pas :

- autonomie de la session une fois les données reçues ;
- capacité à ouvrir ultérieurement un projet autonome depuis le stockage ;
- capacité à importer de nouveaux fichiers EEG directement sur Quest.

Ce sont trois jalons distincts.

### 9. Modèle connecté, déconnecté et resynchronisé

Le modèle doit supporter deux fonctionnements complémentaires.

#### Lorsque la connexion est disponible

- l'interaction est appliquée immédiatement sur le Quest ;
- toute commande synchronisable est inscrite dans le journal local avant sa première tentative d'envoi ;
- la commande sémantique correspondante est envoyée en temps réel au Desktop ;
- le Desktop applique cette commande au même modèle ou service partagé ;
- le Desktop refait localement les calculs nécessaires à son propre affichage ;
- les textures ou pixels produits par le Quest ne sont pas considérés comme la source de vérité du Desktop ;
- l'accusé de réception confirme que le Desktop a intégré la commande ;
- le Quest conserve la commande jusqu'à cet accusé, même si elle a été envoyée alors que la connexion paraissait saine.

Exemples de commandes :

- `TranslateBrainInstance` ;
- `RotateBrainInstance` ;
- `SetCutPlane` ;
- `SetTimelineIndex` ;
- `SetColumnVisibility` ;
- `SetThreshold` ;
- `CreateOrUpdateRoi`.

#### Lorsque la connexion disparaît

- le Quest continue à utiliser exactement les mêmes commandes localement ;
- les commandes synchronisables déjà journalisées restent dans un journal ordonné en mémoire pour la durée de la session ;
- le tracking, le rendu et les manipulations ne dépendent pas de la reconnexion ;
- l'interface indique simplement l'état connecté ou déconnecté.

#### À la reconnexion

- le Quest et le Desktop comparent l'identité et la révision de départ de la visualisation ;
- les commandes non acquittées sont transmises de manière idempotente ;
- une commande déjà reçue ne produit pas un second effet ;
- un accusé perdu ne transforme pas une commande déjà intégrée par le Desktop en conflit ou en double modification ;
- la réconciliation distingue une révision Desktop avancée par une commande Quest déjà reçue d'une véritable édition concurrente ;
- si le Desktop n'a pas divergé, les commandes sont rejouées dans l'ordre ;
- si le Desktop a divergé, le produit propose une règle explicite : conflit, import dans une copie, ou fusion limitée aux scopes reconnus comme sûrs ;
- aucune fusion scientifique silencieuse ne doit être inventée.

Le `latest-wins` reste pertinent pour coalescer les positions intermédiaires d'un geste ou d'un gizmo. Il ne doit pas servir de règle générale pour fusionner deux sessions ayant divergé.

Pour le prototype, ne surconçois pas immédiatement un système distribué générique. Commence par les transformations d'une seule instance de cerveau, avec :

- identifiant stable de la visualisation et de l'instance ;
- numéro de séquence ;
- identifiant de commande ;
- révision de départ ;
- accusé de réception ;
- rejeu idempotent après une courte déconnexion ;
- scénario où le Desktop a appliqué la commande mais où l'accusé a été perdu.

Étends le modèle seulement lorsque de nouveaux scopes le nécessitent.

### 10. Persistance attendue

La V1 n'exige pas une meilleure résilience que HiBoP Desktop.

- L'état non sauvegardé peut rester en mémoire.
- Si l'application Quest est tuée, les modifications non sauvegardées peuvent être perdues.
- Une sauvegarde, un export ou une synchronisation explicite peut devenir le point de durabilité.
- Un autosave ou une restauration après crash pourra être ajouté ultérieurement s'il apporte une valeur réelle.

Ne fais donc pas de la reprise après kill ou reboot une condition du premier prototype.

Si un fichier projet portable est ajouté plus tard, distingue le fichier de données persistant de l'état de travail non sauvegardé. Ne réimporte pas automatiquement les anciennes politiques de persistance du chantier XR ; définis celles du nouveau produit au moment où cette feature entre réellement dans le périmètre.

### 11. Fidélité et performance

Une version Quest peut être explicitement plus lente ou plus limitée que la version Desktop. Les différences acceptables peuvent inclure :

- temps de calcul plus long ;
- chargement initial plus long ;
- résolution ou qualité graphique adaptée ;
- moins de visualisations ou colonnes simultanées ;
- certaines interfaces d'édition indisponibles ;
- certaines features annoncées pour une version ultérieure.

En revanche, une différence scientifique silencieuse n'est pas acceptable. Il faut distinguer :

- paramètres scientifiques, qui doivent avoir le même sens ;
- paramètres de présentation ou de performance, qui peuvent être propres à la cible.

Le futur travail devra mesurer sur Quest :

- temps de calcul natif ;
- copies native/managed ;
- mise à jour et upload GPU ;
- commande jusqu'à la frame visible ;
- stabilité du framerate avec passthrough ;
- mémoire totale ;
- température et throttling sur une session prolongée ;
- consommation énergétique ;
- première exécution et cache chaud.

Ces mesures servent à optimiser et à définir le produit. Elles ne doivent pas empêcher de construire un premier prototype de manipulation locale qui n'exécute pas encore toutes les fonctions scientifiques.

### 12. Enseignements réutilisables du chantier XR précédent

Inspecte au minimum les éléments suivants comme candidats à réutilisation, sans accepter automatiquement leurs décisions :

- contrats et IDs stables ;
- représentation de rendu minimale ;
- protocole, framing, transport et appairage ;
- cache d'assets et contenu adressé par hash ;
- renderer Quest ;
- instances de cerveau et partage de surfaces ;
- rendu de sites et picking ;
- timeline préchargée ;
- gizmo de coupe local ;
- séquencement, coalescence et latest-wins ;
- mesures réseau, mémoire, GPU, frame et thermique ;
- scripts de build et validation Quest ;
- tests de sérialisation, golden et parité.

Faits expérimentaux particulièrement utiles :

- P12 a mesuré le codec côté Windows et calculé une borne optimiste de transfert à partir du meilleur débit physique P06 ; cette borne échouait déjà aux objectifs choisis avant calcul scientifique, transport protocolaire réel, upload GPU et frame visible. P12 n'a pas mesuré la chaîne command-to-photon complète sur Quest et ne prouve donc pas une impossibilité générale de tout transport lossless ;
- une timeline complètement préchargée sur Quest a permis une sélection locale visible à la frame suivante sur le profil testé ;
- un build Android ARM64 de `hbp_core` a déjà été produit, sans validation runtime scientifique sur Quest ;
- un APK Unity Android ARM64/IL2CPP avec OpenXR a déjà fonctionné sur Quest 3 ;
- le renderer, les modèles de transfert et les outils existants peuvent réduire fortement le temps du nouveau prototype s'ils sont compatibles avec le projet unique.

Produis une matrice de réutilisation avec, pour chaque élément :

- responsabilité ;
- emplacement ;
- dépendances ;
- qualité et tests existants ;
- compatible tel quel / à adapter / à réécrire / à abandonner ;
- raison ;
- coût ou risque d'intégration dans le projet unique.

### 13. Premier prototype recherché

L'objectif est d'obtenir relativement rapidement une preuve verticale très simple :

> Depuis HiBoP Desktop, transférer une visualisation contenant une seule colonne vers un Quest, afficher le cerveau, puis permettre à l'utilisateur de déplacer et tourner localement ce cerveau en XR.

Le prototype doit chercher à valider les choix structurants, pas à démontrer tout le produit.

#### Périmètre minimum proposé

1. Un seul projet Unity ouvre et compile le code commun.
2. Un profil Desktop conserve l'application HiBoP existante.
3. Un profil Quest produit un APK Android ARM64/IL2CPP/OpenXR.
4. Desktop extrait depuis une visualisation réelle ou une fixture représentative :
   - une instance de cerveau ;
   - une surface ;
   - son matériau ou ses attributs visuels minimaux ;
   - son repère et sa transformation ;
   - l'identité de la visualisation, de la colonne et de l'instance.
5. Ces données sont transférées au Quest.
6. Le Quest affiche la colonne de manière autonome après réception.
7. L'utilisateur peut saisir, déplacer et tourner le cerveau localement.
8. Les contraintes et mutations de transformation passent par un comportement commun, également utilisé par Desktop.
9. Si la connexion est active, les commandes de transformation sont envoyées immédiatement au Desktop et s'y reflètent.
10. Après une courte coupure, le Quest reste manipulable et les commandes non acquittées peuvent être rejouées à la reconnexion sans double application.

#### Explicitement hors du premier prototype

- EEGFormat sur Quest ;
- import direct de données EEG ;
- fichier projet autonome complet ;
- portage de toutes les interfaces Desktop ;
- coupes scientifiques locales ;
- timeline complète ;
- plusieurs cerveaux ou plusieurs colonnes ;
- persistance des modifications après kill ;
- fusion générique multi-utilisateur ;
- perfection visuelle ou optimisation finale ;
- réorganisation générale de l'ensemble du code HiBoP.

#### Critères de réussite à préciser après questions/réponses

- les deux builds viennent du même projet et du même commit ;
- aucune logique de manipulation n'est copiée entre Desktop et Quest ;
- modifier une règle commune de transformation affecte les deux cibles ;
- le build Desktop n'active pas le runtime XR ;
- le build Quest n'embarque pas les plugins natifs Desktop ;
- une colonne est transférée et affichée avec le bon repère ;
- translation et rotation locales sont fluides sur le matériel cible ;
- la manipulation ne s'arrête pas lors d'une coupure réseau ;
- la synchronisation connectée et le rejeu court ne dupliquent pas les commandes ;
- les mesures minimales de frame, mémoire, taille de transfert et latence de synchronisation sont consignées ;
- la procédure de démonstration est reproductible.

Ne fixe pas arbitrairement toutes les valeurs numériques avant d'avoir observé l'existant et interrogé le propriétaire. Propose des seuils provisoires argumentés, puis fais-les valider.

### 14. Roadmap de départ à challenger

Construis une roadmap incrémentale à partir de cette esquisse, en la corrigeant selon les résultats de l'audit et des questions.

#### Jalon 0 — cadrage et baseline

- clarifier la vision et le vocabulaire ;
- identifier la baseline Git du nouveau projet ;
- inventorier le code réutilisable ;
- écrire les décisions initiales ;
- définir le prototype et ses critères de succès ;
- ne modifier encore aucun code de production.

#### Jalon 1 — projet unique, deux builds

- créer ou définir les Build Profiles Desktop et Quest ;
- réunifier uniquement le bootstrap XR nécessaire dans le projet racine ;
- stabiliser le manifeste de packages ;
- configurer OpenXR uniquement pour la cible Quest ;
- configurer scènes, symboles, plugins natifs et paramètres par cible ;
- prouver que les deux Players se construisent depuis le même projet ;
- vérifier l'absence de régression Desktop de base.

#### Jalon 2 — première tranche fonctionnelle commune

- identifier le comportement de transformation actuel ;
- extraire le seam minimal ;
- faire passer Desktop par ce comportement commun ;
- implémenter l'adaptateur d'entrée XR ;
- utiliser la même représentation de transformation ;
- supprimer tout chemin temporaire dupliqué.

#### Jalon 3 — transfert d'une visualisation à une colonne

- réutiliser ou adapter le RenderModel et le cache d'assets existants ;
- extraire une colonne depuis Desktop ;
- transférer surface, identité et transformation ;
- afficher la colonne sur Quest ;
- gérer progression, erreur et nouvelle tentative de façon minimale.

#### Jalon 4 — synchronisation live et courte reconnexion

- envoyer les commandes de transformation en temps réel ;
- journaliser chaque commande avant son premier envoi et la conserver jusqu'à l'accusé ;
- appliquer ces commandes sur Desktop ;
- ajouter séquence, idempotence et accusés ;
- maintenir la manipulation Quest hors ligne ;
- rejouer les commandes manquantes après une courte coupure ;
- démontrer l'absence de double effet, y compris lorsque le Desktop a appliqué une commande mais que son accusé a été perdu.

#### Jalon 5 — prototype qualifié

- exécuter la démonstration de bout en bout sur Quest ;
- mesurer fluidité, transfert, mémoire et stabilité ;
- dresser la liste des obstacles réels ;
- décider si l'architecture est confirmée, corrigée ou abandonnée ;
- seulement ensuite détailler la V1 produit.

#### Jalons produit ultérieurs possibles

- davantage de colonnes et de visualisations ;
- sites, sélection et ROI ;
- timeline locale ;
- `hbp_core` complet chargé et qualifié sur Quest ;
- coupes scientifiques locales ;
- réglages de concurrence et optimisations ARM/NEON ;
- synchronisation de davantage de scopes ;
- export de projet portable avec données préparées ;
- persistance et ouverture autonome depuis le casque ;
- `EEGFormat` sur Quest uniquement si un besoin produit futur le justifie.

Pour chaque jalon, fournis :

- objectif observable ;
- décisions préalables ;
- tâches ;
- dépendances ;
- risques ;
- critères d'acceptation binaires ;
- mesures à collecter ;
- artefacts à produire ;
- condition d'arrêt ou de révision ;
- estimation seulement après inspection suffisante, avec hypothèses explicites.

### 15. Questions/réponses attendues

Commence la collaboration par des questions ciblées. L'objectif n'est pas de poser cent questions ni de concevoir toute la V1 avant le prototype.

Procède par petits groupes de questions prioritaires, idéalement cinq à huit questions au premier tour, puis un second tour uniquement si les réponses modifient réellement le prototype.

Les sujets à clarifier en priorité sont notamment :

1. matériel Quest cible exact pour le prototype et la première version ;
2. plateforme Desktop initiale à prendre en charge pour le prototype ;
3. chemin utilisateur envisagé pour lancer le transfert depuis HiBoP ;
4. nature exacte de la « colonne » minimale à afficher ;
5. transformations qui doivent être synchronisées et espace de coordonnées attendu ;
6. comportement souhaité si Desktop et Quest manipulent simultanément la même instance ;
7. durée de déconnexion que le prototype doit démontrer ;
8. niveau visuel minimal acceptable pour le premier cerveau ;
9. besoin ou non de sauvegarder explicitement les changements du prototype ;
10. critères subjectifs permettant au propriétaire de dire « ce prototype confirme la direction ».

Pose d'abord les questions qui peuvent changer l'architecture ou réduire fortement le périmètre. Pour les détails secondaires, formule une hypothèse raisonnable et consigne-la comme telle.

Chaque réponse validée doit alimenter un journal de décisions. Si une réponse reste ouverte, explique son impact concret sans bloquer tout le travail.

### 16. Documents attendus à l'issue de l'analyse

Crée un nouvel espace documentaire distinct de `Docs/dev/xr/`, avec un nom de travail explicite à valider. Une structure possible est :

```text
Docs/dev/quest-autonomous/
├── 00-product-vision.md
├── 01-decisions-and-open-questions.md
├── 02-existing-code-and-reuse-audit.md
├── 03-target-architecture.md
├── 04-prototype-specification.md
├── 05-state-command-and-sync-model.md
├── 06-native-data-and-portability-strategy.md
├── 07-validation-and-measurement-plan.md
├── 08-roadmap-and-milestones.md
├── 09-risk-register.md
└── README.md
```

Adapte cette liste si un document serait artificiel ou redondant. L'important est de produire un ensemble cohérent comprenant au minimum :

- vision et périmètre produit ;
- décisions prises et questions ouvertes ;
- audit de l'existant et matrice de réutilisation ;
- architecture du projet Unity unique ;
- frontière comportement commun/adaptateurs plateforme ;
- stratégie native `hbp_core`/`hbp_math`/`EEGFormat` ;
- modèle connecté/déconnecté/reconnexion ;
- spécification du premier prototype ;
- plan de validation et métriques ;
- roadmap, jalons et backlog initial ;
- risques et conditions de révision.

Ces nouveaux documents doivent être autosuffisants. Ils peuvent citer les preuves de `Docs/dev/xr/`, mais ne doivent pas obliger le lecteur à interpréter les anciennes ADR pour comprendre le nouveau produit.

### 17. Exigences de qualité de l'analyse

- Appuie les conclusions sur le code et les mesures observables.
- Distingue clairement fait, hypothèse, décision proposée et question ouverte.
- Ne présente pas une compilation réussie comme une preuve de runtime ou de performance.
- Ne transforme pas une dette générale de HiBoP en prérequis du prototype.
- Ne crée pas une architecture abstraite plus grande que le problème observé.
- Préserve le fonctionnement Desktop existant autant que possible.
- Donne la priorité à une tranche verticale démontrable sur Quest.
- Identifie explicitement toute logique qui risquerait d'être dupliquée.
- Prévois des tests d'architecture interdisant les références du commun vers les adaptateurs de plateforme.
- Prévois une CI par cible depuis le même commit, avec workspaces ou caches Unity séparés si nécessaire.
- Vérifie les contenus réels des builds plutôt que de supposer que le stripping retire tout.
- N'utilise aucune donnée patient réelle pour le prototype initial ; utilise une fixture synthétique ou nettoyée.
- Ne lance aucune opération destructive ou externe sans autorisation adaptée.

### 18. Format de collaboration demandé

Commence par :

1. faire un bref inventaire en lecture seule de l'état Git, des projets Unity et des fichiers de configuration immédiatement visibles ;
2. résumer en quelques paragraphes ta compréhension du nouveau produit ;
3. signaler les éventuelles contradictions que tu vois dans ce prompt ;
4. poser un premier groupe limité de questions prioritaires ;
5. proposer une courte liste des inspections approfondies que tu vas mener pendant que je réponds.

Après mes réponses :

1. inspecte suffisamment le projet pour répondre avec des faits ;
2. propose les décisions structurantes une par une ;
3. expose les compromis de manière simple ;
4. produis les nouveaux documents ;
5. termine par une roadmap ordonnée et un backlog du prototype ;
6. demande une validation explicite avant de passer de l'analyse à l'implémentation.

Le résultat attendu de cette conversation n'est pas encore un produit fini. C'est un cadrage assez précis pour pouvoir lancer rapidement un prototype à une colonne sans recréer une seconde implémentation de HiBoP.

### 19. Résumé impératif

Si une décision ultérieure entre en conflit avec une phrase secondaire de ce prompt, conserve en priorité les principes suivants :

1. nouveau produit et nouvelles spécifications ;
2. ancien chantier XR conservé, jamais fusionné en bloc ni détruit ;
3. possibilité de réutiliser sélectivement son code et ses preuves ;
4. un seul projet Unity et deux builds ;
5. une seule implémentation des comportements métier et scientifiques ;
6. refactorisation minimale, déclenchée par des tranches fonctionnelles ;
7. Desktop prépare, Quest explore ;
8. calcul et interaction locaux sur Quest dès que la feature est admise ;
9. commandes synchronisées en temps réel lorsque connecté et rejouées après reconnexion ;
10. fichier projet tout embarqué plus tard, pas dans le premier prototype ;
11. EEGFormat optionnel et hors V1 initiale ;
12. premier objectif : transférer une visualisation à une colonne, afficher le cerveau, le déplacer et le tourner sur Quest ;
13. questions/réponses courtes et prioritaires avant de figer le prototype ;
14. nouvelles spécifications, roadmap, jalons et tâches avant toute implémentation importante.
