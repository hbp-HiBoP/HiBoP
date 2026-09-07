# Rapport QUEST-002-A — Input System seul

## Résultat

HiBoP utilise **New (`activeInputHandler=1`) sur Windows et Android**, avec
Input System **1.20.0**, sans bascule Both/New. Les commandes Desktop, les
calculs scientifiques et les caméras métier sont conservés. Le sélecteur
`Tools/Set-QuestInputMode.ps1` est supprimé ; les recettes actives de QUEST-003
et l'architecture décrivent désormais New. Le rapport historique QUEST-002
reste inchangé.

Les lectures clavier/souris applicatives passent aux périphériques Input System,
désormais via la classe statique `DesktopInput`, avec valeurs neutres lorsqu'ils
sont absents (simplification demandée après validation du Player, détaillée ci-dessous). Le bouton et la sortie
de pointeur de `View3DUI`, ainsi que la molette de `ChannelBloc`, utilisent les
événements UI reçus. Les actions UI sont sérialisées dans
`Assets/Settings/DesktopUI.inputactions` : Point, Click, RightClick, MiddleClick,
ScrollWheel, Navigate, Submit et Cancel. Le prefab **Input Manager**, déjà
référencé par `HiBoP.unity`, porte un seul `InputSystemUIInputModule` et ses huit
références ; aucune construction runtime de GameObject ne compense un asset absent.

Les raccourcis contextuels restent lus par `ShortcutManager`, avec leurs règles
de priorité et de maintien existantes. Ils ne sont pas abonnés une seconde fois
aux actions UI. Les listes de touches privées, non sérialisées, utilisent `Key` ;
aucune configuration utilisateur de raccourcis n'est convertie ou supprimée.
Les touches restent physiques, comme `m_UsePhysicalKeys=1` dans la référence.
La priorité de saisie couvre les champs uGUI et TMP. Tab reste géré par
`FieldsSwitcher` et l'autocomplétion des labels, sans binding Tab ajouté à Navigate.

`InputFieldBackend` fournit seulement les propriétés IME/tactiles que les champs
uGUI demandent via `BaseInput`. Sa référence au module est sérialisée ; `OnEnable`
affecte `inputOverride`, qui n'est pas un champ sérialisable d'uGUI. L'édition du
texte continue d'utiliser la file native d'événements de texte/IMGUI d'uGUI.
Les sources des packages ne sont pas modifiées.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI. Manuel : VALIDE.
- Référence livrée : **`f64212b7f3110de9ca30f04a25dd33e0ecc6fa4d` (QUEST-002)**,
  branche `feature/xr-autonomous`, checkout initial propre ; modifications de
  QUEST-002-A non commitées. Aucun commit, push, profil ou rig ajouté.
- Unity **6000.5.2f1**, Input System **1.20.0**, uGUI **2.5.0**. Unity fermé au
  départ ; commandes CLI hors sandbox avec `Start-Process -Wait -PassThru
  -WindowStyle Hidden`. Aucun changement de manifeste/lock ou de package.
- Le Player `.artifacts/quest-002/player/HiBoP.6.1.0.win64/HiBoP.exe` et sa fixture
  sont conservés. La fixture de recette est copiée à l'identique, SHA-256
  `0bd800ec656ed8be03777b85457e47a749bbfd5b450d368185baab1b0e32a61a`.
- [Manifeste de preuves](../evidence/QUEST-002-A/manifest.json) et
  [inventaire avant/après](../evidence/QUEST-002-A/input-inventory.json).
  Logs, XML, assemblies et binaires restent locaux dans les dossiers ignorés
  `.test-results/quest-002-a` et `.artifacts/quest-002-a`.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle / risque |
| --- | --- | --- |
| 1 | [Input Manager.prefab](../../../../Assets/Prefabs/Managers/Input%20Manager.prefab), [actions](../../../../Assets/Settings/DesktopUI.inputactions) | Un module, huit références d'actions ; molette à 1, répétition à 0,5 s puis 0,1 s ; aucune référence legacy dans les dépendances livrées. |
| 2 | [ShortcutManager](../../../../Assets/Scripts/HBP/UI/Tools/ShortcutManager.cs) | Ctrl gauche/droit, Maj, Alt, pressions ponctuelles et maintenues ; priorité de la saisie, sans double abonnement UI/raccourcis. |
| 3 | [View3DUI](../../../../Assets/Scripts/HBP/UI/Module3D/View3DUI.cs), [ChannelBloc](../../../../Assets/Scripts/HBP/UI/Informations/TrialMatrix/ChannelBloc.cs) | Deltas de pointeur et échelle UI inchangés ; conversion de la molette de sélection via l'événement, sans facteur brut 120. |
| 4 | [InputFieldBackend](../../../../Assets/Scripts/HBP/UI/Tools/InputFieldBackend.cs), [réglages d'entrée](../../../../Assets/Settings/DesktopInputSettings.asset) | Adaptation ciblée uGUI ; remise à zéro/désactivation des périphériques à la perte de focus ; molette normalisée par le package sur chaque plateforme. |
| 5 | [tests d'entrée](../../../../Assets/Tests/PlayMode/HBP.UI.PlayModeTests/DesktopInputPlayModeTests.cs), [tests de configuration](../../../../Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/DesktopInputConfigurationTests.cs) | Périphériques injectés, vrais handlers View3DUI et caméras ; restauration des réglages/périphériques/singleton du harnais ; audit scène et Resources. |

## Inventaire et exceptions

L'inventaire initial est confirmé : **33 fichiers, 147 occurrences `Input.*`**
dans `Assets/Scripts`, commentaires inclus. Après migration, **aucune lecture
active de l'ancien backend** n'y subsiste. Les 16 occurrences restantes sont des
expériences commentées dans `DevDebug.cs` ; sa lecture F1 active a été migrée.

L'audit porte aussi sur les scripts d'Assets, alias/références qualifiées,
EventSystem sérialisés et dépendances des scènes de build et de **tous les
Resources**. Les exceptions conservées sont explicites :

- `Assets/TextMesh Pro/Examples & Extras` : scripts de démonstration et module
  legacy de la scène `12a - Text Interactions`, absents des dépendances livrées.
- `Assets/ThirdParty/UIExtensions/Scripts/Controls/ColorPicker/TiltWindow.cs` :
  comportement de démonstration non attaché aux assets livrés ; son assembly
  reste compilée. Aucun usage runtime livré identifié.
- Les API d'événements IMGUI/texte et les types `KeyCode` Editor ne sont pas
  des lectures du backend legacy. Dans uGUI, les quatre propriétés réellement
  demandées par InputField sont adaptées ; les autres lecteurs virtuels legacy
  de `BaseInput` ne sont pas appelés par les chemins UI livrés.

La liste des dépendances auditée est enregistrée dans
`.test-results/quest-002-a/delivered-dependencies.txt`. Les exceptions ne sont
pas masquées par un fallback Both. Aucune source du PackageCache ni aucun exemple
tiers inutilisé n'a été migré.

## Vérifications effectuées

| ID | Scénario | Résultat / preuve locale |
| --- | --- | --- |
| T1 | Windows New : configuration, compilation Player et fixture MNI | **10/10**, exit 0 ; `windows-edit.xml` / `.log`. |
| T2 | PlayMode UI et Module3D, incluant les sept nouveaux tests d'entrée | **80/80**, exit 0 ; `ui-module3d.xml` / `.log`. |
| T3 | Import Android New, configuration et compilation Player | **8/8**, exit 0 ; `android-edit.xml` / `.log` ; **59 assemblies Player**. |
| T4 | Retour Windows New, configuration, compilation Player et MNI | **10/10**, exit 0 ; `windows-return.xml` / `.log` ; **60 assemblies Player**. |
| T5 | Build Windows IL2CPP | **REUSSI**, exit 0, `Build Finished, Result: Success.` ; `build-windows.log` ; 254,451 s pour le build Unity. |
| T6 | Format C# et contrôle du diff | **REUSSI**, exit 0 ; `format-delivery.log` et `git diff --check`. Les réglages d'icônes vides générés à l'import et BuildInfo sont restaurés après build ; seul New reste modifié dans ProjectSettings.asset. |

Les noms de preuves de ce tableau sont relatifs à `.test-results/quest-002-a/`.
T1/T3/T4 utilisent réellement `PlayerBuildInterface.CompilePlayerScripts`.
Les tests contrôlent `ENABLE_INPUT_SYSTEM` présent,
`ENABLE_LEGACY_INPUT_MANAGER` absent et l'absence de loader XR Desktop.
Android est une qualification d'import/assemblies, pas un build APK/IL2CPP.

Les échecs intermédiaires concernent la préparation des références Unity et du
harnais, puis ont été corrigés : références d'actions persistantes, singleton
de test, focus de batchmode, nouvelle pression de S après relâchement, et
Submit/Cancel qui exigent les mises à jour Dynamic en 1.20. Les résultats retenus
ci-dessus correspondent au code final des tests. La restauration NuGet du
formateur a nécessité l'exécution hors sandbox.

### Mesures et limites des simulations

Le harnais injecte `MouseState` et `KeyboardState`, puis laisse le module UI
produire les événements. Les tests passent dans `View3DUI.OnDrag` / `OnScroll`,
`View3D` et `Camera3D`, avec `CanvasScalerHandler` aux échelles 1 et 0,5. Les
formules de référence sont celles de QUEST-002 ; `Camera3D` et `View3D` sont
inchangés.

| Entrée | Référence et résultat New |
| --- | --- |
| Translation de 20 pixels, échelle 1 | X : −4 unités ; Y : −20 unités |
| Translation de 20 pixels, échelle 0,5 | X : −2 unités ; Y : −10 unités |
| Rotation de 20 pixels par axe | 20° à échelle 1 ; 10° à échelle 0,5 ; signe vertical conservé |
| Molette +1 cran | +5 unités vers la cible |
| Molette −0,5 cran | −2,5 unités ; delta fractionnaire conservé |
| Répétition liste | Pression initiale unique ; délai 0,5 s puis pas 0,1 s, inchangés |

Le rapport vertical/horizontal **5** signalé par le propriétaire est conservé.
Il provient de `HorizontalStrafe` (facteur 0,2) et `VerticalStrafe` (facteur 1),
antérieurs à QUEST-002. Ce défaut n'est pas corrigé dans cette migration.

Le module 1.20 propose un facteur de molette par défaut de **6** ; le prefab
livré le fixe à **1**, comme le module legacy. Les deltas sont normalisés par
Input System, sans supposer des unités brutes identiques sur Windows/Mac/Linux.
Voir les références Unity [migration](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/migrate-from-old-input-system.html)
et [module UI](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/introduction-ui-input-module.html),
complétées par le code installé d'`InputSystemUIInputModule.OnScrollCallback`.

Les autres tests couvrent clic unique, droite/milieu, glisser X/Y, fenêtres,
redimensionnement X/Y, sélection Maj, Ctrl/Maj gauche et droite, Tab, Entrée,
Échap, et blocage des raccourcis pendant la saisie. Le caractère accentué est
injecté dans la file de traitement native d'uGUI via `InputField.ProcessEvent` ;
ce n'est pas une simulation de la disposition physique du clavier ni d'un IME.
La remise à zéro du clavier/souris est simulée avec `ResetDevice`, et la politique
de perte de focus est vérifiée séparément dans l'asset. L'aller-retour de focus
OS, le DPI physique, les trackpads et IME réels restent des contrôles manuels.

## Validation manuelle reçue

Le nouveau Player et la fixture sont prêts. Lancer :

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-002-a\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-002-a\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy'
```

Le manifeste contient les hashes du Player, de GameAssembly, de la fixture et
des fichiers de la distribution. Le build embarque son BuildInfo daté du
2026-09-07, conservé comme preuve locale dans `build-info.json` ; le SHA affiché
désigne HEAD, complété par les modifications non commitées du manifeste.

Player de référence préservé :

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-002\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-002\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy'
```

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Dans la vue MNI, glisser bouton droit, puis bouton milieu horizontalement/verticalement, puis molette ; comparer avec le Player QUEST-002, au même DPI. | Rotation/translation/zoom comparables ; asymétrie verticale préexistante conservée. | VALIDE — 2026-09-07 16:34 +02:00 |
| M2 | Utiliser les menus et champs existants, Tab/Entrée/Échap, la sélection avec Maj/Ctrl, le déplacement par l'en-tête et le redimensionnement des fenêtres. Essayer Ctrl+S puis Ctrl+Maj+S hors saisie et pendant la saisie. | Navigation habituelle ; texte prioritaire, aucun raccourci doublé. | VALIDE — 2026-09-07 16:34 +02:00 |
| M3 | Maintenir une touche ou un bouton, changer de fenêtre avec Alt+Tab, relâcher ailleurs puis revenir dans HiBoP. | Pas de touche/bouton restant actif, pas d'action doublée au retour. | VALIDE — 2026-09-07 16:34 +02:00 |

Le propriétaire valide **M1, M2 et M3 le 2026-09-07 à 16:34 +02:00** :
« Je valide les 3 résultats à la date de maintenant. » Cette validation concerne
le Player fourni ci-dessus. Aucune validation physique Mac/Linux ou Quest
n'est revendiquée ; les dispositions de clavier/IME et matériels non essayés
ne sont pas qualifiés par ce retour.

## Simplification après validation — DesktopInput

À la demande du propriétaire le 2026-09-07, les lectures sont centralisées dans
`Assets/Scripts/HBP/Input/DesktopInput.cs`. Les composants utilisent par exemple
`DesktopInput.MousePosition`, `DesktopInput.IsControlPressed` et
`DesktopInput.WasPressedThisFrame(Key.S)`. Les vérifications de présence de
`Keyboard.current` et `Mouse.current` sont réalisées uniquement dans cette
classe ; le backend IME conserve ses accès nécessaires aux abonnements clavier.
Les périphériques ne sont pas mis en cache : une absence renvoie zéro ou faux,
et un remplacement est visible dès la lecture suivante.

L'assembly partagé `HBP.Input.Runtime` permet aux modules UI, Data, Theme et Dev
d'utiliser la même classe sans ajouter de dépendance Input System au cœur
scientifique. Les règles contextuelles restent dans `ShortcutManager` et les
deltas provenant des événements UI restent inchangés. Cette façade limitée
répond à la demande de lisibilité ; elle ne reproduit pas toute l'API legacy.

Les preuves de cette révision sont séparées dans
[input-facade.json](../evidence/QUEST-002-A/input-facade.json), avec les hashes
des sources et les nouveaux résultats de tests. Le Player précédemment livré
et validé n'est pas reconstruit ni remplacé par cette simplification.

Vérification de cette révision : **81/81 tests PlayMode UI/3D**, dont le retrait
et remplacement des périphériques injectés ; **8/8 tests Android** avec
compilation de 60 assemblies Player ; **10/10 tests de retour Windows** avec
compilation de 61 assemblies Player et recette de la fixture anatomique.
Formatage C# exécuté avec succès. Packages inchangés ; projet laissé sur Windows.

## Décisions, limites et suite

D22 et la demande explicite d'implémentation autorisent cette migration seule.
Pas de gain de performance annoncé, pas de rebinding, refonte caméra ou nouvelle
architecture Desktop/Quest. La validation manuelle reste distincte des tests
techniques. La prochaine tâche proposée est QUEST-003, avec New dans les deux
profils ; elle n'est pas exécutée ici.
