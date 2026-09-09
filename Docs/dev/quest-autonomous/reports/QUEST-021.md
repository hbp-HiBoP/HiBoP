# Rapport QUEST-021 — Projection iEEG commune utilisée par Desktop

## Résultat

Le calcul iEEG et sa calibration sont désormais une opération du wrapper existant
`IEEGGenerator.ComputeCalibratedActivity`. La préparation Desktop appelle cette
opération avec les tableaux, dimensions, distance et plages capturés sur Unity.
L'ordre natif est conservé : calcul, lecture des métriques, calibration.

Les masques effectifs passent déjà par `RawSiteList.UpdateMasks` depuis QUEST-018.
Les UV activité/alpha sont déjà produits par `SurfaceGenerator.ComputeActivityUV`
et consommés par le renderer. Ces opérations restent à leur portée réelle ;
aucune classe session ou service relais supplémentaire n'est créée.

`Base3DScene` ne change pas : QUEST-018 y avait déjà remplacé le dispatch
scientifique par la préparation polymorphe des colonnes. Elle conserve le
scheduling, la progression, les paramètres différés, l'invalidation et la durée
de vie. `Column3DDynamic` conserve la timeline, la préparation des données, les
masques issus de l'état Desktop, l'apparence des sites et la publication mémoire.
Son enchaînement redondant calcul/métriques/calibration quitte le chemin sites.
L'override CCEP atlas garde cet enchaînement local, sans port des corrélations.

La surface continue d'utiliser `CurrentProjectionSample.Index` au rendu ; les
sites continuent d'utiliser `TemporalSample.Evaluate`. L'interpolation temporelle
et les statistiques de préparation ne sont pas modifiées. Une entrée à un seul
instant utilise les plages de la préparation complète, sans renormalisation locale.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** ; manuel : **VALIDE**.
- Branche `feature/xr-autonomous`, HEAD initial `8638b7aad166070f467ffb3ef296163e50327ab5`, arbre initial propre.
- Changements locaux non commités ; aucune reprise historique, modification native, opération sur casque, publication ou CI distante.
- Unity `6000.5.2f1`, profil `Assets/Settings/BuildProfiles/DesktopWindows.asset`, plugins de `Tools/NativePlugins.lock.json`.
- Dépendance [QUEST-020](QUEST-020.md) : préparation iEEG et fixture BrainVision synthétique. Enseignements [QUEST-018](QUEST-018.md) appliqués au placement des responsabilités et aux emprunts natifs.
- [Manifeste des preuves](../evidence/QUEST-021/manifest.json). Les Players/logs restent locaux sous `.artifacts/quest-021` et `.test-results/quest-021`.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Changement et règle |
| --- | --- | --- |
| 1 | [IEEGGenerator.ComputeCalibratedActivity](../../../../Assets/Scripts/HBP/Core/DLL/Generators/IEEGGenerator.cs) | Entrées explicites et ordre natif inchangé ; aucune dépendance Data/UI/XR dans l'opération. |
| 2 | [Column3DDynamic.PrepareActivityComputation / PrepareCalibratedSignalComputation](../../../../Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs) | Capture avant le worker ; appel commun réel ; publication mémoire après succès avec taille du tableau capturé. |
| 3 | [Column3DCCEP.PrepareCalibratedSignalComputation](../../../../Assets/Scripts/HBP/Data/Module3D/Column3DCCEP.cs) | Les sites héritent du chemin commun ; l'atlas garde ses appels, son avertissement et ses masques même lorsqu'il est ignoré. |
| 4 | [IEEGProjectionTests](../../../../Assets/Tests/EditMode/HBP.Transfer.Anatomy.Desktop.Tests/IEEGProjectionTests.cs) et [DesktopIEEGCaptureTests](../../../../Assets/Tests/EditMode/HBP.Transfer.Anatomy.Desktop.Tests/DesktopIEEGCaptureTests.cs) | Référence avant extraction, plusieurs instants, masques et sampling ; buffers complets comparés sans sous-échantillonnage. |
| 5 | [IEEGProjectionDiagnostic](../../../../Assets/Scripts/HBP/Dev/IEEGProjectionDiagnostic.cs) | Diagnostic opt-in du Player, référence native indépendante et égalité des streams réellement uploadés au mesh. |

## Ressources et concurrence

Le wrapper est synchrone et ne crée pas de scheduler. Il emprunte les valeurs et
handles jusqu'au retour. Le propriétaire sérialise calcul, calibration, lecture
des UV et destruction. Les builders Desktop remplacent les tableaux : aucune
copie volumineuse ni lecture de l'UI depuis le worker n'est ajoutée.

Les garanties QUEST-018 restent actives : `GeneratorWork` attend la fin réelle
du worker et du suivi de progression ; annuler l'observateur ne permet pas de
libérer les handles ; les calibrations UI différées s'appliquent au retour avec
les dernières valeurs. Le renderer lit les UV après le calcul. Les tableaux UV
réutilisés appartiennent au `SurfaceGenerator`, leur contenu change au prochain
appel ; les tests clonent uniquement leurs références de comparaison.

Revue indépendante ciblée demandée selon AGENTS.md : aucun défaut concret dans
le diff de production ou le scheduler existant. Le diagnostic emprunte la grille
et la surface d'un Player automatisé dédié ; il n'est pas conçu pour une fermeture
ou une modification de scène simultanée par l'utilisateur. Le lanceur `-KeepOpen`
n'active pas ce diagnostic.

## Vérifications effectuées

| ID | Scénario / commande | Résultat et preuve |
| --- | --- | --- |
| T1 | CLI EditMode, Desktop + ActivityProjectionPhase3 + Stage5ProjectionBuffer + ActivityProjectionSettings ; [arguments exacts](../evidence/QUEST-021/editmode-command.json) | **70/70 réussis**, zéro ignoré, sortie **0** ; [XML](../evidence/QUEST-021/editmode.xml). |
| T2 | Dans T1 : cinq samples time-major, aucun/un/huit sites, exclusions/tous masqués, Nearest/Trilinear, Constant/Linear/Quadratic, alpha 0/0,8/1 | UV activité/alpha exactement égaux à l'ancien enchaînement, tous finis ; couverture et comptes de stockage égaux. |
| T3 | Dans T1 : MNI complet et huit contacts QUEST-017, grille 80, cinq samples synthétiques | Tous les UV comparés, sans sous-échantillonnage ; calcul d'un instant avec calibration complète exactement égal au même sample dans le calcul de série. |
| T4 | Dans T1 : rayons 15 → 25 → 15 mm et plages [-10,0,10] → [-8,-1,3] → [-10,0,10], puis recalibration du champ existant | Égalité exacte et retour aux UV initiaux, valeurs négatives incluses. |
| T5 | Dans T1 : vrai `SetActivityData`, navigation 200 Hz / projection 100 Hz, toutes politiques temporelles, ROI active/inactive, buffers et paramètres remplacés après capture | Préparation Desktop exécutée sur worker ; chaque index de navigation reçoit les UV attendus à `CurrentProjectionSample.Index`. |
| T6 | Dans T1 : régressions QUEST-018, annulation observateur, erreur worker, nettoyage, paramètres différés, préparations iEEG/CCEP sites/statique/fMRI/MEG | Réussies ; handles libérés après fin réelle, sorties/préparations préservées. |
| T7 | CLI PlayMode, assemblage `HBP.Module3D.PlayModeTests` ; [arguments](../evidence/QUEST-021/playmode-command.json) | **45/45 réussis**, zéro ignoré, sortie **0** ; [XML](../evidence/QUEST-021/playmode.xml). |
| T8 | Frontière commune et preuve d'appel Desktop | [Contrôles](../evidence/QUEST-021/seam-check.json) réussis ; pas de dépendance UI/XR ou singleton Desktop dans `IEEGGenerator`. |
| T9 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-021` ; [arguments Unity](../evidence/QUEST-021/windows-build-command.json) | **REUSSI**, sortie **0**, Player Windows IL2CPP de développement ; hashes dans le manifeste. |
| T10 | `Tools/Run-QuestIEEGProjection.ps1` ; [arguments Player](../evidence/QUEST-021/player-command.json) | **18/18 comparaisons exactes**, 69 104/69 104 sommets couverts à chaque étape et cinq sites actifs ; upload mesh exact. Indices 0/10/50/75/130/150, paramètres initiaux/modifiés/restaurés ; sortie **0**, [résultat](../evidence/QUEST-021/player-result.json). |
| T11 | Formatage, `git diff --check`, contrôle des liens et hashes | **REUSSI** ; les fichiers générés par Unity ont été sauvegardés dans `.test-results/quest-021/unity-generated.patch`, puis restaurés. |

La référence avant extraction est l'enchaînement conservé dans les tests et le
diagnostic à partir de HEAD `8638b7aad166`. Il ne s'agit pas de captures d'un
ancien binaire. Les différences numériques maximales et RMS des UV comparés
valent zéro sur ce runtime Windows ; aucune tolérance Android n'est définie.
Le test de capture MNI locale suit la convention des tests existants : ignoré
si `-questAnatomyFixture` n'est pas fourni. La campagne T1 fournit explicitement
la capture et n'a aucun test ignoré.

Les premiers tests ont révélé une erreur de
compilation dans le nouveau test (`AnatomyBuffer` doit être converti par
`ToArray()` avant LINQ) ; corrigée avant la campagne finale. Aucun résultat
antérieur à la correction n'est utilisé comme preuve de réussite.
Le formateur a nécessité une restauration NuGet hors sandbox, puis a réussi.
La revue finale a identifié le réglage `SmoothActivityBoundaries` manquant dans
le générateur de référence du diagnostic ; il est copié depuis la préférence
Desktop avant les essais Player. Le code de production n'était pas affecté.
Le premier diagnostic a produit ses 18 comparaisons réussies, mais le dialogue
Desktop interceptait sa fermeture. Ce Player automatisé a été arrêté après
collecte. La condition de sortie dans
[ApplicationManager.OnQuit](../../../../Assets/Scripts/HBP/UI/Tools/ApplicationManager.cs)
autorise désormais explicitement le couple `-ieegEvidenceOnce -ieegEvidence`
dans les builds de développement, comme pour le diagnostic de capture existant.
La fermeture interactive habituelle est conservée ; le Player est reconstruit
et a refait les 18 comparaisons avec une sortie autonome **0**.

Le diagnostic utilise `JObject` pour les preuves IL2CPP et `View3D.GetTexture`
pour les captures, conformément aux corrections QUEST-018. Les images ont été
inspectées : cerveau visible, zone d'activité modifiée puis restaurée. Elles
proviennent du Player final, pas d'un ancien exécutable. Le contrôle visuel du
propriétaire reste distinct de ces assertions automatiques.

## Validation manuelle du propriétaire

Le casque est en recharge et n'est pas nécessaire à cette tâche Desktop.

Player : `C:\HBP\Software\HiBoP\.artifacts\quest-021\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-020\fixture\quest-mni-ieeg.hibop`.
Le lanceur charge le protocole synthétique en mémoire avant l'archive, conformément
à la correction QUEST-020 ; aucun import dans la base personnelle n'est demandé.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\HBP\Software\HiBoP\Tools\Run-QuestIEEGProjection.ps1" -KeepOpen
```

1. **M1 — VALIDE le 2026-09-09.** Ouvrir l'onglet **Activity** dans la visualisation **MNI iEEG**, colonne **Synthetic uV**, et utiliser le bouton de calcul (objet `Compute IEEG` dans `Assets/Prefabs/3D/UI/3D Menu.prefab`, composant `ComputeActivity`) si la projection n'est pas calculée. Dans **Timeline**, déplacer le curseur (`Timeline Slider`) et utiliser les boutons précédent/suivant : parcourir notamment les pics à **−400 ms** (index 10) et **800 ms** (index 130), puis **0 ms** (index 50) et **250 ms** (index 75). Le cerveau et les contacts restent en place, l'activité évolue et revenir au même instant retrouve la même projection. Retour propriétaire : **M1 validée**.

Confirmation explicite du propriétaire le **2026-09-09** : « Très bien laisse comme ça alors. J'ai fait les tests et je valide M1. »
([preuve](../evidence/QUEST-021/manual-validation.json)). Les appels aux métriques
restent inchangés à sa demande ; le problème de comptage mémoire natif signalé
dans la conversation reste distinct et n'est pas corrigé dans cette tâche.

Références à 0 ms : [paramètres initiaux](../evidence/QUEST-021/ieeg-0-index50.png),
[paramètres modifiés](../evidence/QUEST-021/ieeg-1-index50.png),
[paramètres restaurés](../evidence/QUEST-021/ieeg-2-index50.png).

## Décisions, limites et suite

Aucune décision scientifique nouvelle. Les comparaisons avant/après concernent
le même runtime Windows et le même noyau natif. Elles ne qualifient pas la parité
Android et n'étendent pas les tolérances densité D30 à l'iEEG. Le chemin CCEP atlas
n'est pas requalifié sur un jeu de données atlas réel.

QUEST-021 est terminée : implémentation et vérifications techniques réussies, M1 validée.

Suite proposée : **QUEST-022**, apparence scientifique des sites. Non engagée.
