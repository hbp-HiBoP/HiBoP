# Architecture de développement

Référence consolidée au 2 novembre 2026. Elle décrit les responsabilités observables dans le code, les contraintes de modification et les frontières encore couplées. Les anciennes propositions de découpage ne constituent pas une feuille de route approuvée.

## Assemblies et sens des dépendances

Les `.asmdef` sont la source de vérité. Les dépendances directes HBP autorisées sont versionnées dans [assembly-dependency-policy.json](../../Tools/assembly-dependency-policy.json). Le contrôle [check-assembly-dependencies.ps1](../../Tools/check-assembly-dependencies.ps1) vérifie les références et interdit les cycles ou les nouvelles arêtes non approuvées. Lors de cette consolidation, le contrôle statique a réussi sur **44 assemblies HBP et 164 arêtes directes** ; aucun test Unity n'a été exécuté.

| Frontière | Responsabilité actuelle et contraintes |
|---|---|
| `HBP.Core.Runtime` | Modèles, configuration, sérialisation, préférences, gestion des données et wrappers natifs. Fondation sans référence vers une autre assembly `HBP.*`. Elle utilise néanmoins Unity, UniTask et TextMeshPro : ce n'est pas un domaine indépendant de Unity. |
| `HBP.Data.Runtime` | Orchestration des données, Module3D, graphiques et BIDS. Dépend de Core, Input, Rendering et Theme. Le nom Data ne signifie donc pas « modèles seuls ». |
| `HBP.Input.Runtime` | Adaptation des entrées Unity Input System. |
| `HBP.Rendering.Runtime` | Infrastructure URP, dépendante des packages Render Pipelines Core et Universal. Les modèles scientifiques restent dans leurs propriétaires existants. |
| `HBP.Theme.Runtime` | Thème et présentation partagés ; dépend de Core et Input. |
| `HBP.UI.Runtime` | Fenêtres, contrôleurs et workflows utilisateur ; compose Core, Data, Rendering, Theme, Input, Transfer et Sync. |
| `HBP.Sync.Runtime` / `HBP.Sync.Scene` | Contrats et mécanique de synchronisation / adaptation aux scènes. Runtime dépend seulement de Core parmi les assemblies HBP ; Scene rejoint Data et les adaptateurs Transfer. |
| `HBP.Transfer.Codecs`, `.Transport`, `.Scene` | Encodage, transport, puis adaptation des scènes. Transport dépend de Core, Codecs et Sync.Runtime ; Scene dépend aussi de Data. |
| `HBP.Quest.Runtime` | Composition autonome Quest, UI, XR, rendu, transfert et synchronisation. Ses dépendances ne doivent pas remonter dans Core. |
| Assemblies Editor, Dev et Tests | Outils d'édition/build, instrumentation et preuves ciblées. Leurs références plus larges ne justifient pas les mêmes dépendances dans le runtime produit. |

`noEngineReferences: true` dans certaines assemblies Transfer/Sync décrit leurs références propres ; leur dépendance à Core ne prouve pas une fermeture transitive indépendante de Unity. Les assemblies de support Anatomy/Projection et les utilitaires de tests ont leurs propres frontières dans les `.asmdef` et la politique, sans être de nouveaux points d'entrée produit.

Avant de changer une référence, examiner les dépendances directes **et transitives**, justifier l'arête, puis exécuter le contrôle statique. Une fonctionnalité de couche supérieure doit observer Core par un événement ou un port neutre du domaine. Déplacer ses diagnostics, états ou interfaces spécialisés dans Core pour contourner cette règle violerait la frontière.

## Orchestration et propriété

[ApplicationState](../../Assets/Scripts/HBP/Core/Tools/ApplicationState.cs), `PersistentDataManager`, `DatabaseManager`, `DataManager` et `Module3DMain` restent des points d'état partagés. Ils donnent accès au projet courant, aux chemins, aux préférences, aux caches et aux scènes ; un test ou un traitement asynchrone doit expliciter ce qu'il emprunte et restaure. Les chemins des données varient entre Editor, Desktop et Android : ne pas remplacer les propriétés d'ApplicationState par un chemin de machine ou un chemin relatif supposé commun.

[DataManager](../../Assets/Scripts/HBP/Core/Data/DataManager.cs) gère caches, lectures et ressources de données, avec synchronisation explicite. `Clear` et le nettoyage final ne sont pas interchangeables : vider les données d'un projet doit permettre une nouvelle utilisation du gestionnaire ; la durée de vie de ses verrous et ressources doit rester cohérente. Les règles de chargement, partage des opérations et publication sont détaillées dans les références du domaine [loading](loading/loading-and-persistence.md).

[Base3DScene](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs) reste un coordinateur de scène substantiel. Ses fichiers partiels isolent notamment configuration, opérations, durée de vie, densité, atlas et transfert ; ils ne constituent pas des services indépendants. Une scène possède ses représentations et ses ressources temporaires. L'anatomie de référence, le maillage affiché et les champs scientifiques ont des durées de vie distinctes ; voir [projection](activity-projection.md), [inflation](surface-inflation.md) et [mesh](mesh-generation-and-cuts.md).

`SpecificSiteLocationFilterCondition.SceneLocationEvaluator` est toujours un point d'adaptation statique entre un filtre du modèle et la scène ; son absence produit `false`. Cette frontière requiert installation et restauration maîtrisées, notamment en test. Les callbacks et événements métier sont préférables à une référence Core vers la scène ou l'UI, mais cette observation n'autorise pas une refonte générale des filtres.

Les GameObjects authored sont définis dans les prefabs et leurs références sérialisées. La construction runtime est réservée aux objets intrinsèquement dynamiques. Les fenêtres et contrôleurs orchestrent encore certains exports : la présence de cette responsabilité ne signifie pas que les services d'export proposés dans l'ancien audit existent déjà.

## Persistance et workflows de projet

[ProjectArchiveReader](../../Assets/Scripts/HBP/Core/Data/Project/ProjectArchiveReader.cs) traite la lecture d'archive, tandis que le modèle `Project` garde des opérations de chargement/sauvegarde. [ProjectWorkflowService](../../Assets/Scripts/HBP/UI/Tools/ProjectWorkflowService.cs) constitue une frontière de workflow effective : confirmations, projet courant, emplacement, configurations des modules, scènes et fenêtres passent par des dépendances contrôlables. Ce service permet de tester les annulations et restaurations d'état sans ouvrir toute l'application.

Les archives `.hibop`, les noms de membres sérialisés, identités et références sont des contrats de compatibilité. Les protocoles sont résolus dans la base de protocoles et ne doivent pas être recréés comme entrées autonomes d'archive parce qu'un ancien plan le suggérait. Les chemins portables et alias ne doivent pas divulguer les répertoires temporaires dans la sauvegarde. Une ouverture annulée ne doit pas détruire prématurément les scènes, fenêtres et données existantes ; une erreur doit conserver ou restaurer un état cohérent. Les archives partiellement lisibles peuvent être ouvertes en récupération lecture seule selon les contrôles actuels, sans présenter cette récupération comme une sauvegarde saine.

Le déplacement ou renommage d'un type doit prendre en compte les anciens `$type`, alias de sérialisation, GUID Unity et références de prefabs/scènes. Un changement de namespace n'est donc pas une simple opération cosmétique. Les audits de contrats et fixtures de compatibilité dans [testing](testing.md) protègent cette frontière ; mettre à jour une empreinte attendue sans analyser la différence ne constitue pas une migration.

## Décisions et limites

L'ancien audit proposait un domaine pur, une assembly Module3D distincte, des services Persistence/BIDS/Localizer et plusieurs extractions de contrôleurs. Ces propositions ne décrivent pas l'architecture livrée et ne sont pas reconduites comme tâches à réaliser. Une extraction future doit répondre à une modification concrète, conserver les contrats et se justifier par une frontière testable, sans multiplier les abstractions.

Les constats historiques « pas de tests », « Built-in/Post Processing Stack comme rendu actif », « GeneratorSurface en production » et « UserSettings/solution générée versionnés » ne sont plus une base valide pour décider. Les tests sont répartis par responsabilité, le rendu actif est URP, la projection utilise un champ volumique, et la vérification Git de cette consolidation n'a trouvé ni `HiBoP.slnx` ni `UserSettings` suivis.

Les couplages restants — Core dépendant de Unity, état global, coordination large de Data/UI — sont des caractéristiques observées, pas une preuve de défaut fonctionnel. Pour intervenir : partir du comportement à modifier, identifier son propriétaire et ses contrats, choisir la preuve la plus étroite, puis étendre seulement si une dépendance réelle l'exige.
