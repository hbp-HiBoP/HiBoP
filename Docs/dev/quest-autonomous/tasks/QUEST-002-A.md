# QUEST-002-A — Migrer HiBoP vers Input System seul (optionnel)

Jalon : [J1](../milestones/J1.md). Type : migration d'entrée optionnelle.
Dépendance : [QUEST-002](QUEST-002.md), validée et commitée avant exécution.
Ordre souhaité par le propriétaire le 2026-09-07 : juste après le commit de
QUEST-002, avant QUEST-003. Aucun commit ni lancement de migration n'est implicite
dans la rédaction de cette fiche. Cette extension ne rouvre pas QUEST-002.
Statut et preuves : [registre](../TASK-STATUS.md#quest-002-a).

## Instructions de reprise

Lire le [contrat](../TASK-WORKFLOW.md), la décision D22 dans le
[journal](../01-decisions-and-open-questions.md), le
[rapport QUEST-002](../reports/QUEST-002.md) et l'[architecture](../03-target-architecture.md).
Relever le commit de référence effectivement livré ; conserver son Player et sa
fixture pour la comparaison. Une demande « Implémente QUEST-002-A » autorise cette
migration, pas les profils ou le rig Quest. Si elle est différée, QUEST-003 reste
possible avec la sélection d'entrée documentée dans QUEST-002.

## Objectif et état de départ

Utiliser `activeInputHandler=1` (Input System Package) sur Desktop et Android,
sans modifier les commandes ou les interactions métier existantes. Supprimer le
besoin de basculer Both/New, tout en gardant les présentations Desktop/Quest
indépendantes (D03). Aucun gain de performance n'est présumé.

Inventaire initial du 2026-09-07 : `rg -l '\bInput\.' Assets/Scripts -g '*.cs'`
trouve **33 fichiers**, soit 147 occurrences de lectures `Input.*` (51 positions
de souris, 76 lectures clavier, sept axes, etc.). Ce n'est pas l'inventaire complet :
auditer aussi les alias, scènes/prefabs, tiers, samples embarqués et tests.

Points d'entrée :

- `Assets/Scripts/HBP/UI/Tools/ShortcutManager.cs` : raccourcis, modificateurs,
  sélection et priorité de la saisie dans les champs.
- `Assets/Scripts/HBP/UI/Module3D/View3DUI.cs`,
  `Assets/Scripts/HBP/Data/Module3D/View3D.cs` et `Camera3D.cs` : événements de
  pointeur, rotation, translation et zoom.
- `Assets/Scripts/HBP/UI/Tools/Window/`, `List/`, `Tooltip.cs`,
  `Assets/Scripts/HBP/UI/Informations/`, `Assets/Scripts/HBP/Theme/Settings/Cursor.cs` :
  saisie, focus, drag, sélection, survol et curseur.
- `Assets/_Scenes/HiBoP.unity`, ses prefabs et les EventSystem sérialisés ;
  `Assets/Tests/PlayMode/HBP.PlayModeTestUtilities/PlayModeWindowHarness.cs` et
  `Assets/Tests/PlayMode/HBP.UI.PlayModeTests/UiPlayModeArchitectureTests.cs`.
- `Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/QuestPackageConfigurationTests.cs`,
  `Tools/Set-QuestInputMode.ps1`, `ProjectSettings/ProjectSettings.asset`.

## À implémenter

1. Inventorier les dépendances réelles à l'ancien backend ; distinguer lectures
   runtime, tests, IMGUI Editor et code tiers non livré. Documenter chaque reste.
2. Migrer les lectures runtime vers Input System 1.20.0 et les événements UI
   existants quand ils apportent déjà l'information. Utiliser des actions pour
   les commandes qui le justifient ; éviter une copie générique de UnityEngine.Input
   ou un framework commun Desktop/Quest. Conserver les données sérialisées de
   raccourcis si elles existent : la présence du type KeyCode, à elle seule,
   n'impose pas l'ancien backend.
3. Remplacer les modules legacy des EventSystem concernés par
   `InputSystemUIInputModule`, avec actions et références sérialisées dans les
   scènes/prefabs appropriés. Ne pas créer des GameObjects runtime pour combler
   des références absentes. Prévenir le double traitement UI/raccourcis.
4. Préserver les boutons, modificateurs, pressions ponctuelles/maintenues,
   navigation clavier, saisie de texte, focus et perte de focus. Mesurer les unités
   de molette, deltas de souris, échelles UI/DPI et répétitions clavier : ne pas
   substituer des valeurs brutes en supposant des sensibilités équivalentes.
5. Passer le réglage global à New seulement lorsque les chemins runtime sont
   migrés. Adapter les tests et harnais ; supprimer le sélecteur Both/New devenu
   inutile et actualiser les recettes actives sans réécrire les preuves historiques.
   Adapter seulement les références d'assemblies nécessaires.

## Hors périmètre et défaut préexistant

Pas de refonte des caméras, des calculs scientifiques, des asmdefs historiques,
de nouveau rig, de rebinding utilisateur ou de mise à niveau des packages.
Ne pas modifier les sources du PackageCache. Traiter les tiers réellement
utilisés par adaptation ciblée ; ne pas migrer tous les exemples inutilisés.

Le propriétaire a signalé une translation verticale trop sensible. Le code
antérieur à QUEST-002 multiplie l'horizontale par 0,2 et la verticale par 1,
soit un rapport 5 à déplacement de pointeur égal. Ce défaut est indépendant
du backend. Le documenter dans la comparaison et ne pas le corriger implicitement
pendant la migration : un réglage de sensibilité reste une modification distincte
à demander explicitement. Ne pas prendre cette asymétrie pour une régression New.

## Vérifications à réaliser par l'agent

- Contrôler les références runtime restantes : aucune lecture de l'ancien backend
  ni module legacy dans les scènes/prefabs livrés ; expliquer les occurrences
  Editor/tiers inactives sans les masquer avec un fallback Both.
- Tests avec événements Input System injectés via un harnais adapté à PlayMode
  ou aux outils de test du package : clics, glisser horizontal/vertical, molette,
  modificateurs, raccourcis pendant la saisie, tabulation, sélection et focus.
  Restaurer l'état global entre tests et respecter les règles async d'AGENTS.md.
- Exécuter les tests de configuration, UI et Module3D affectés, ainsi que la fixture
  MNI. Comparer les deltas de caméra aux valeurs de référence, axe par axe, et la
  réponse de la molette. Ne pas se limiter à vérifier qu'une API ne lève pas d'exception.
- Importer et compiler Windows puis Android puis revenir à Windows avec le même
  manifeste/lock et **New** partout ; vérifier `ENABLE_INPUT_SYSTEM` présent et
  `ENABLE_LEGACY_INPUT_MANAGER` absent. Aucun loader XR sur Desktop.
- Construire un nouveau Player Windows IL2CPP et préparer la fixture MNI pour la
  recette. Pas d'APK fonctionnel ni de gestes Quest exigés avant QUEST-003/004.
- Formater les C# et contrôler le diff. Préserver la portabilité des entrées
  Desktop ; ne pas annoncer de qualification physique Mac/Linux sans exécution.

## Validation manuelle du propriétaire

Fournir les chemins exacts du nouveau Player, de la fixture, les commandes de
lancement et les contrôles réellement présents :

1. Comparer rotation bouton droit, translations bouton du milieu et zoom molette
   avec le Player QUEST-002 ; signaler toute différence, y compris selon le DPI.
2. Vérifier menus, champs de texte, Tab/Entrée/Échap, sélection avec modificateurs,
   déplacements/redimensionnements de fenêtres et raccourcis effectivement migrés.
3. Vérifier sortie/retour de focus sans touche restant active ni action doublée.

Attendre un retour daté OK/KO ; la compilation seule ne valide pas ces gestes.

## Rapport et critère de fin

Produire `reports/QUEST-002-A.md` selon le modèle et
`evidence/QUEST-002-A/manifest.json` : commit de référence, inventaire avant/après,
fichiers/actions/modules modifiés, preuves et exceptions explicites. Actualiser
sa ligne du registre et les instructions de QUEST-003 concernées.

Fin technique : HiBoP Desktop fonctionne et compile en New, Android compile avec
le même réglage, aucune dépendance runtime au backend legacy ne subsiste dans les
chemins livrés, tests de comportement réussis et Player fourni. Manuel séparé.
En cas d'incompatibilité bloquante, présenter les chemins concernés et deux
solutions concrètes avant d'élargir le périmètre.

Références Unity 1.20 : [migration](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/migrate-from-old-input-system.html)
et [module UI](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/introduction-ui-input-module.html).
