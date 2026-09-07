# Rapport QUEST-002 — Packages communs et entrée Desktop/Quest

## Résultat

Le manifeste racine installe maintenant les dépendances XR avec les dépendances
Desktop existantes. Un seul `packages-lock.json` conserve leur résolution.
Les contrôles Desktop, les scènes et les asmdefs métier ne sont pas migrés.

`ProjectSettings/ProjectSettings.asset: activeInputHandler` vaut **2 (Both)**
dans le checkout livré. Sans Build Profile, c'est un réglage **global**, pas une
valeur indépendante par cible. Le nouveau script
[Set-QuestInputMode.ps1](../../../../Tools/Set-QuestInputMode.ps1) sélectionne
explicitement **1 (Input System)** pour Quest ou **2 (Both)** pour Desktop,
avant d'ouvrir Unity. Il refuse de modifier le fichier lorsqu'un éditeur Unity
est ouvert et ne change ni la cible de build ni les packages.

Ce choix conserve `UnityEngine.Input` sur Desktop et évite le mode Both sur
Android. Unity documente le [redémarrage nécessaire et les deux symboles
d'entrée](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Installation.html) ;
sa [limite Android](https://issuetracker.unity.com/issues/10034/the-side-back-gesture-does-not-exit-the-player-when-using-gesture-navigation-mode)
est également explicite. Le code installé
`InputSystem/Editor/Settings/EditorPlayerSettingHelpers.cs` confirme les valeurs
0/1/2 et la prise en compte possible d'overrides de profil. Les profils complets,
leur sélection et la protection des builds relèvent de QUEST-003 ; aucune
migration Desktop n'est nécessaire pour cette étape.

### Dépendances ajoutées

| Package direct | Version | Raison |
| --- | --- | --- |
| `com.unity.inputsystem` | 1.20.0 | Actions et périphériques requis par XRI/OpenXR. |
| `com.unity.xr.interaction.toolkit` | 3.6.0 | Interactions aux contrôleurs, à composer dans QUEST-004. |
| `com.unity.xr.management` | 4.7.0 | Configuration et cycle de vie des loaders XR par cible. |
| `com.unity.xr.openxr` | 1.18.0 | Fournisseur OpenXR Android. |
| `com.unity.xr.meta-openxr` | 2.4.1 | Extensions Quest, dont le futur passthrough. |

Résolution transitive observée : AR Foundation **6.5.0**, Composition Layers
**2.4.0**, XR Core Utils **2.6.0**, XR Legacy Input Helpers **3.0.1** et Editor
Coroutines **1.1.0**. AR Foundation et Composition Layers sont des dépendances
de Meta OpenXR, conservées sans les redéclarer au premier niveau. Le package
XR Hands n'est pas ajouté. URP reste **17.5.0** ; les versions des dépendances
Desktop déjà verrouillées sont conservées.

Installer ces packages n'active pas un runtime XR : aucun loader n'est configuré.
Unity a généré les assets de réglages OpenXR, XRI et Composition Layers, livrés
avec leurs GUID pour stabiliser les imports. Toutes les features OpenXR restent
désactivées, le simulateur XRI ne s'instancie pas automatiquement et l'émulation
Composition Layers Desktop est désactivée. Le registre
`XRGeneralSettingsPerBuildTarget.asset: m_SettingsPerBuildTarget` est vide : les
assets de loaders générés ne sont affectés à aucune cible. Les réglages temporaires
AR Foundation sous `Assets/XR/Temp` sont ignorés par Git. Aucun rig/prefab Quest
n'est créé.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI. Manuel : VALIDE.
- Branche `feature/xr-autonomous`, HEAD initial
  `bba6dce634d0374d0dd00ece76a7b58104b4e892`, checkout initial propre.
- Unity `6000.5.2f1 (eb73d3b415a1)`, éditeur fermé au départ ; exécutions CLI
  hors sandbox avec `Start-Process -Wait -PassThru -WindowStyle Hidden`.
- Versions directes reprises comme références depuis
  `eb26c323e2bdf6c138f9e9e3c02f3e9796cae249:XR/Packages/manifest.json` ;
  aucun ancien code, asset, package Shared ou GUID historique restauré.
  AR Foundation est résolu à 6.5.0, contre 6.4.3 dans l'ancien lock.
- Dépendance QUEST-001 : fixture anatomique et recette existantes. Son ancien
  Player ne sert pas de preuve de cette modification des packages.
- [Manifeste de preuves](../evidence/QUEST-002/manifest.json).
  Logs, XML, assemblies compilées et Player sont locaux, sous les dossiers
  ignorés `.test-results/quest-002` et `.artifacts/quest-002`.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle / risque |
| --- | --- | --- |
| 1 | [manifest.json](../../../../Packages/manifest.json) et [lock](../../../../Packages/packages-lock.json) | Cinq dépendances directes, transitives résolues par Unity ; aucun remplacement des packages Desktop ni dépendance XR Hands. |
| 2 | [Set-QuestInputMode.ps1](../../../../Tools/Set-QuestInputMode.ps1) et [ProjectSettings.asset](../../../../ProjectSettings/ProjectSettings.asset) | Modification explicite du seul champ global d'entrée, éditeur fermé ; restaurer Desktop après une qualification Quest. |
| 3 | [QuestPackageConfigurationTests](../../../../Assets/Tests/EditMode/HBP.PlatformConfiguration.Tests/QuestPackageConfigurationTests.cs) | Contrôle des versions effectivement installées, des defines Player et compilation de toutes les assemblies Player pour la cible active. Références XR limitées à une assembly Editor de test. |
| 4 | [OpenXR Package Settings](../../../../Assets/XR/Settings/OpenXR%20Package%20Settings.asset), [simulateur XRI](../../../../Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset) et [Composition Layers](../../../../Assets/CompositionLayers/UserSettings/Resources/CompositionLayersRuntimeSettings.asset) | Réglages générés sans loader ni activation des fonctionnalités ; aucune création automatique de simulateur Desktop. |

## Vérifications effectuées

| ID | Vérification | Résultat / preuve locale |
| --- | --- | --- |
| T1 | Import Windows après ajout des packages | REUSSI, exit 0 ; `import-windows.log`. |
| T2 | Android, entrée New, assembly `HBP.PlatformConfiguration.Tests` | REUSSI, exit 0 : 5 tests passent, 1 test Desktop ignoré ; **59 assemblies Player Android** compilées ; `android-tests.xml` et `android-tests.log`. |
| T3 | Retour Windows, entrée Both, configuration + fixture QUEST-001 | REUSSI, exit 0 : **8/8 tests** ; **60 assemblies Player Windows** compilées et chargement MNI réel ; `windows-delivery.xml` et `windows-delivery.log`. |
| T4 | Build Windows IL2CPP par `HBP.Dev.HBPBuilder.BuildFromCommandLine` | REUSSI, exit 0, `Build Finished, Result: Success.` ; `build-windows.log`. |
| T5 | Manifeste/lock avant bascule, après Android et après retour Windows | REUSSI : SHA-256 identiques, consignés dans les trois fichiers `packages-*.json`. Aucun changement de version Desktop. |
| T6 | `Tools/format-code.cmd` | REUSSI, exit 0 ; `format-delivery.log`. Restauration NuGet hors sandbox et génération du projet nécessaires. |
| T7 | Contrôle final du diff, aller-retour du sélecteur, réglages XR et comparaison aux sources initiales | REUSSI ; `final-checks.json`, `git diff --check` exit 0. Code Desktop, scène, axes et versions Desktop inchangés ; retour à Both identique octet par octet. |

Tous ces chemins de logs/XML sont relatifs à `.test-results/quest-002/`.
T2/T3 appellent `PlayerBuildInterface.CompilePlayerScripts` : ce sont de vraies
compilations des assemblies Player, distinctes de la compilation Editor et d'un
build APK/IL2CPP Android. Les appels legacy sont exercés sans exception sur
Windows ; les gestes dans le Player restent dans la validation manuelle.

Les premiers essais du nouveau test ont identifié une référence Newtonsoft
manquante, deux homonymes Unity et une simulation d'entrée traitée dans la boucle
Editor. Le test final vérifie la liaison d'une action à un contrôleur virtuel ;
il ne prétend pas simuler une interaction physique Quest.

Reproduire les tests depuis la racine du dépôt, Unity fermé (lancer ce script
hors sandbox conformément à AGENTS.md). Pour Android : `$questPlatform='Quest'`,
`$questTarget='Android'`. Pour Windows : `Desktop` et `Win64`.

```powershell
$questPlatform = 'Desktop'
$questTarget = 'Win64'
$questRoot = (Get-Location).Path
& ./Tools/Set-QuestInputMode.ps1 -Platform $questPlatform
$questResult = Join-Path $questRoot '.test-results/quest-002'
New-Item -ItemType Directory -Force $questResult | Out-Null
$questArgs = @('-batchmode', '-nographics', '-projectPath', $questRoot,
    '-buildTarget', $questTarget, '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'HBP.PlatformConfiguration.Tests',
    '-testResults', (Join-Path $questResult "$questPlatform-tests.xml"),
    '-logFile', (Join-Path $questResult "$questPlatform-tests.log"), '-forgetProjectPath')
$questProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe' -ArgumentList $questArgs -Wait -PassThru -WindowStyle Hidden
$questProcess.ExitCode
# Après la campagne Android et la fermeture de Unity :
& ./Tools/Set-QuestInputMode.ps1 -Platform Desktop
```

T3 utilise à la place de `-assemblyNames` le filtre exact
`-testFilter 'HBP.Tests.PlatformConfiguration.QuestPackageConfigurationTests;HBP.Tests.Serialization.QuestAnatomyFixtureTests'`.
Si Unity est ouvert, utiliser MCP selon AGENTS.md après avoir configuré le backend
et redémarré l'éditeur si nécessaire ; ne pas lancer un deuxième éditeur sur le projet.

Commande T4 : même exécutable Unity et même `Start-Process -Wait -PassThru
-WindowStyle Hidden`, avec les arguments :

```powershell
$questArgs = @('-batchmode', '-nographics', '-quit', '-projectPath', 'C:\HBP\Software\HiBoP',
    '-buildTarget', 'Win64', '-executeMethod', 'HBP.Dev.HBPBuilder.BuildFromCommandLine',
    '-buildOutput', 'C:\HBP\Software\HiBoP\.artifacts\quest-002\player',
    '-scriptingBackend', 'IL2CPP', '-logFile', 'C:\HBP\Software\HiBoP\.test-results\quest-002\build-windows.log',
    '-forgetProjectPath')
```

Les réécritures automatiques de BuildInfo, URP, ShaderGraph et des autres champs
PlayerSettings sont rétablies après le build ; leur diff est conservé dans
`generated-settings.diff`. Seul `activeInputHandler=2` est conservé dans
PlayerSettings. Les avertissements de sérialisation/d'obsolescence préexistants
ne sont pas corrigés. Aucun test de geste physique n'est déduit de la compilation.

## Validation manuelle reçue

Fixture reconstruite par `Tools/Prepare-QuestAnatomyFixture.ps1 -OutputDirectory
.artifacts/quest-002/fixture` : **1 853 octets**, SHA-256
`0bd800ec656ed8be03777b85457e47a749bbfd5b450d368185baab1b0e32a61a`.
Nouveau Player construit :
`C:\HBP\Software\HiBoP\.artifacts\quest-002\player\HiBoP.6.1.0.win64\HiBoP.exe`.
SHA-256 de l'exécutable :
`aab370fc59ed240ed50af0c9461d0d3e83548253ac11c820420d4d37bba64250`.
SHA-256 de `GameAssembly.dll`, qui contient le code IL2CPP :
`29c24f0343bfd628edfb5be6e223415e223585778af5a7c29d0d20fe485789d7`.
Le lancement graphique et les gestes n'ont pas été observés par l'agent.

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Lancer le nouveau Player avec la commande ci-dessous. | Une colonne `MNI Anatomy`, cerveau complet et deux hémisphères. | VALIDE — essai interactif et accord global du propriétaire le 2026-09-07 ; les contrôles structurels sont couverts par les tests. |
| M2 | Dans la vue du cerveau, maintenir le **bouton droit** et déplacer la souris ; utiliser ensuite la **molette**. Glisser avec le **bouton du milieu** pour vérifier aussi la translation. | Rotation, zoom et translation habituels, sans exception ni changement de commandes. | VALIDE — rotation, translation horizontale et zoom confirmés le 2026-09-07 ; réserve verticale préexistante, détaillée ci-dessous. |

Ces commandes proviennent de `View3DUI.OnDrag` et `OnScroll`, inchangés.
Le propriétaire indique le 2026-09-07 : « La rotation, la translation horizontale
et le zoom marchent très bien. » Il signale une sensibilité verticale excessive
et accepte de valider la tâche si elle n'a pas introduit ce comportement.

L'audit de suivi retrouve l'écart dans le code **antérieur à QUEST-002**, à
`bba6dce634d0374d0dd00ece76a7b58104b4e892` :
[Camera3D.HorizontalStrafe](../../../../Assets/Scripts/HBP/Data/Module3D/Camera3D.cs)
utilise `amount * m_Speed * 0.2f`, tandis que `VerticalStrafe` utilise
`amount * m_Speed`. Le déplacement vertical est donc cinq fois plus sensible
à delta de souris égal. `View3DUI.OnDrag` applique le même facteur d'échelle
aux deux axes avant `View3D.StrafeCamera`. Ces trois fichiers, la scène et les axes
Input Manager sont inchangés (`git diff --exit-code`, exit 0) ; uGUI reste 2.5.0.
Le facteur expliquant l'asymétrie n'est pas introduit par le changement de backend.
Ce constat de code ne prétend pas être une mesure physique comparative des Players.

La condition d'acceptation est satisfaite : QUEST-002 est validée, avec ce défaut
préexistant consigné et **non corrigé**. Aucun nouveau build n'a été nécessaire
pour ce suivi documentaire. Aucun geste Quest n'est à valider.

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-002\player\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-002\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy'
```

## Décisions, limites et suite

Lors de l'implémentation initiale, aucune décision produit supplémentaire.
Le sélecteur d'entrée est une solution
explicite avant les Build Profiles de QUEST-003. La sélection de cible Unity seule
ne sélectionne pas le backend d'entrée : exécuter le script avant le démarrage
ou régler Active Input Handling et redémarrer l'éditeur. Le test refuse une
combinaison cible/backend incorrecte.

Cette tâche qualifie les imports, les assemblies Player et le Desktop. Elle ne
qualifie ni un APK autonome, ni OpenXR sur casque, ni les bibliothèques natives
Android. Ces dernières restent dans leur tâche dédiée. Aucune validation de
gestes Quest n'est demandée.

Après le retour du propriétaire, D22 prépare l'option
[QUEST-002-A — Input System seul](../tasks/QUEST-002-A.md), prévue juste après le
commit de QUEST-002, avant QUEST-003. L'intérêt est de supprimer la dépendance au
backend legacy et la bascule globale Both/New. L'audit initial compte 33 fichiers
applicatifs utilisant `Input.*` : la migration doit couvrir l'UI, les raccourcis,
le focus et les sensibilités, pas seulement la caméra. Aucun gain de performance
n'est annoncé. Cette tâche est définie, mais ni exécutée ni commitée par ce suivi.
Si elle est différée, QUEST-003 reste possible avec la stratégie actuelle.
