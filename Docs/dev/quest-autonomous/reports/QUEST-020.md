# Rapport QUEST-020 — Capture d'un instant iEEG préparé

## Résultat

Le transfert accepte maintenant une colonne `Column3DIEEG` préparée et capture
l'instant choisi sur sa timeline. HBNA v4 ajoute aux entrées MNI existantes les
amplitudes préparées, unités par canal, disponibilité, identité temporelle et
plages `SpanMin/Middle/SpanMax`. Les snapshots anatomiques/densité continuent
d'être encodés dans leur version précédente ; les lecteurs v1–v3 sont conservés.

La capture conserve deux buffers : entrées surface à `CurrentProjectionSample.Index`
et valeurs sites exactement issues de `TemporalSample.Evaluate`. Une sélection
interpolée n'est ni arrondie ni refusée. La politique Desktop et l'Alpha temporel
sont transportés ; ils sont distincts de l'opacité `ActivityAlpha` déjà transférée.
Les canaux absents ou vides restent distingués d'un zéro mesuré, et leurs masques
restent obligatoires. Les amplitudes présentes mais blacklistées sont conservées.

Quest conserve ces entrées dans la session reçue. Le calcul de densité est
explicitement inhibé pour un snapshot iEEG, afin de ne pas interpréter ses entrées
comme une densité. Le résumé indique l'instant et ses unités. Le calcul/rendu iEEG
local reste le périmètre de QUEST-021 à QUEST-023.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; vérification technique : **REUSSI** ; M1 : **VALIDE**.
- Branche `feature/xr-autonomous`, HEAD initial `195f44e9fc5f49369e53600c972cc1082bfbcb15`, arbre initial propre.
- Modifications non commitées ; aucun push, lancement de CI ou changement des dépôts natifs.
- Unity 6000.5.2f1, plugins issus de `Tools/NativePlugins.lock.json` ; dépendances QUEST-017/018/019 inspectées dans le code et leurs rapports.
- [Manifeste des preuves](../evidence/QUEST-020/manifest.json).
- Aucune reprise de source XR historique ; extension ciblée du contrat et du chemin de capture présents.
- Manuel M1 (résumé de transfert) : **VALIDE**, retour propriétaire du 2026-09-09
  « Ok je valide tout ce que tu as dit », après la recette détaillée d'appairage,
  d'envoi et de lecture des résumés Desktop/Quest. Le propriétaire avait également
  confirmé le cerveau, la navigation et les pics attendus aux indices 10 et 130.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Règle et raison |
| --- | --- | --- |
| 1 | [DesktopIEEGCapture.Capture](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopIEEGCapture.cs) | Lectures sans yield sur le thread Unity ; ordre des canaux, valeurs préparées et buffer natif time-major recoupés. Le SHA-256 identifie toutes les séries préparées, unités et disponibilités, pas seulement l'instant. |
| 2 | [IEEGInstant et IEEGInstantCodec](../../../../Assets/Scripts/HBP/Transfer/Anatomy/IEEGInstant.cs), [AnatomySnapshotCodec](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomySnapshotCodec.cs) | Copies privées, bornes, valeurs finies, liens canal/contact et masques d'absence ; suffixe v4 couvert par le hash de contenu, v1–v3 préservés. |
| 3 | [DesktopAnatomyCapture.CaptureSelectedAsync / ValidateSelection](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs) | Réutilisation du même snapshot anatomie/contacts/volume ; paramètres dynamiques copiés. Les couleurs projetées ne sont pas les entrées scientifiques transférées. |
| 4 | [QuestAnatomyView.ApplySnapshot / RecalculateDensity](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) et [DesktopQuestPanel.SendAsync](../../../../Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs) | Conservation du bloc iEEG jusqu'au remplacement/fermeture ; aucune projection densité déclenchée pour ce contenu ; résumé issu de l'offre immuable. |
| 5 | [Générateur de fixture](../../../../Tools/Prepare-QuestIEEGFixture.py), [lanceur](../../../../Tools/Run-QuestIEEGCapture.ps1) et [CommandLineReader.ApplyActionAsync](../../../../Assets/Scripts/HBP/UI/Tools/CommandLineReader.cs) | Fixture reproductible ; `-questIEEGProtocol` réservé au développement, chargé uniquement en mémoire avant l'archive. Aucune écriture de protocole dans la base personnelle. |

La revue indépendante n'a identifié aucun défaut concret bloquant ; elle précède
les derniers correctifs du diagnostic et de la fixture et reste distincte des
tests ci-dessous. Aucun nouvel objet UI ou prefab
n'a été nécessaire : les textes des panneaux existants portent le résumé.

## Sémantique et exemple numérique

Une préparation contenant des valeurs de -10 à +10 uV et un instant montrant
`[-1, 0, +1]` conserve `[-10, 0, +10]` comme plages. Ramener les plages à celles
du seul instant multiplierait par dix les amplitudes normalisées et modifierait
l'interprétation des couleurs/tailles. La capture ne recalcule aucune statistique
ni aucune normalisation ; elle copie les paramètres actifs de la colonne.

Pour le test non aligné, navigation 200 Hz et projection 100 Hz : à **15 ms**,
`ProjectionIndex=1`, `Alpha=0,5`. Les entrées surface sont `[-1, 0, +1]`, les
valeurs sites `[-2, +1, +2]` après l'interpolation Desktop des séries du test.
Les positions de contact, flags et masques effectifs restent ceux capturés dans
le même passage synchrone. Les valeurs des deux buffers ne sont pas interchangeables.

La provenance temporelle inclut IDs dataset/bloc/sous-bloc, nom des données,
index/longueurs/fréquences utilisées des deux timelines et temps **local au
sous-bloc en ms**. Le digest des valeurs préparées reste identique lorsque seul
l'instant change. Il ne prétend pas être un hash des fichiers EEG bruts.

## Vérifications effectuées

Les commandes Unity passent par `Start-Process -Wait -PassThru -WindowStyle Hidden`
hors sandbox ; aucun éditeur utilisateur n'était ouvert. Le manifeste contient
les arguments exacts, codes de sortie, XML, logs et hashes des artefacts.

- Codec et régressions : round-trip binaire déterministe, paramètres, unités,
  masques, absence/vide, ordre, Alpha invalide, NaN/Inf et corruption, avec et sans
  hash d'enveloppe recalculé ; anciennes captures MNI relues.
- Capture : vrais `SetActivityData`, buffer time-major, sélection alignée,
  interpolée et dernier échantillon, stabilité du digest, copies indépendantes,
  rejet d'un buffer natif divergent ou de sites réordonnés.
- Fixture : chargement de l'archive et des signaux BrainVision par le pipeline
  Desktop ; index 50, valeurs -1/0/+1 uV, 151 samples, canaux absents et plages conservées.
- **150 tests EditMode distincts réussis** sur les exécutions archivées :
  143 réussites dans le lot principal de 149, puis 7/7 dans le lot corrigé
  (les six captures et le chargement réel de la fixture). Le premier lot avait
  141 réussites et deux tests ignorés faute de fichier MNI ; le lot principal
  suivant a également exécuté ces deux cas avec le fichier fourni.
  Le lot final corrigé a été rejoué : **7/7 réussis**, zéro ignoré, sortie 0.
- Les six tests de capture ont d'abord échoué dans leur **setup**, faute de
  collection d'alias dans leur environnement isolé. Le setup a été corrigé et
  les six ont ensuite réussi. Aucun échec de production n'a été masqué.
- Le premier Player ouvrait le projet en récupération car le VISU du dépôt
  n'était pas présent dans la base locale du propriétaire. Le retour et la
  capture d'écran du 2026-09-09 ont permis de l'identifier. La fixture utilise
  maintenant son propre protocole chargé en mémoire, avec test de chargement réel.
- Deux autres défauts du lanceur ont été identifiés : l'attente d'une base alors
  qu'aucun workspace n'était sélectionné, puis une course avec le rechargement
  des protocoles au démarrage. Le diagnostic attend désormais l'initialisation
  de l'application avant d'ajouter son protocole ; la base n'est attendue que
  si un workspace est sélectionné. Le second screenshot propriétaire montre
  cette course, pas une validation manuelle de la correction.
- Le mode batch a ensuite atteint la scène mais attendait une préparation qui
  nécessite ici une demande de projection. Le diagnostic `-captureIEEGIndex`
  demande désormais ce calcul par `Base3DScene.UpdateGenerator`, après disponibilité
  des ressources et contrôle des avertissements de couverture. Il ne change pas
  les préférences de calcul automatique. L'envoi normal continue à refuser une
  préparation périmée au lieu de recalculer implicitement.
- La première capture interactive a révélé que la clé de blacklist de la fixture
  était l'ID du contact, alors que Desktop attend `patientID_nomDuCanal`. Le
  générateur et les assertions de chargement/réception ont été corrigés. Cette
  capture intermédiaire n'est pas la preuve finale du cas disponible et masqué.

Captures finales du Player Windows IL2CPP, avec calcul explicite et sortie 0 :

| Instant | Index navigation/projection | A1 / A2 / B1 (uV), par patient | Plages | Copie sur thread Unity |
| --- | --- | --- | --- | --- |
| 0 ms | 50 / 50, Alpha 0 | -1 / 0 / +1 | -10 / 0 / +10 | 7,035 ms |
| 250 ms | 75 / 75, Alpha 0 | -1,25 / 0 / +1,25 | -10 / 0 / +10 | 4,980 ms |

Chaque HBNA fait **15 533 236 octets** ; les buffers source, captures répétées et
round-trip sont exacts, les caméras et la présentation inchangées. Disponibilité
par patient : `[2,2,2,0]`. Les B2 absents et A2 droit blacklisté ont leur masque
effectif actif ; ce dernier garde une disponibilité 2 et sa valeur mesurée 0.
Le hash de la préparation est identique aux deux instants :
`760d6a7314ef7a3c73662fe786d4d1cb8435e3d1035e07f82255d39f7f8a5ba9`.
Ces durées sur huit contacts ne qualifient pas les gros jeux de données.

**46/46 tests PlayMode réussis**, zéro ignoré, sortie 0. Le cas
`IEEG_PlayerCaptureReachesSessionWithoutBeingRenderedAsDensity` envoie le HBNA
final à 0 ms par une vraie connexion TLS vers le prefab/session Quest sous Unity
Windows. Valeurs, unités, disponibilités, hash de préparation, plages et masques
arrivent inchangés. Le calcul de densité reste inhibé, la relance renvoie
`AlreadyPublished`, et une anatomie de remplacement libère les entrées iEEG.
Le lot couvre également les régressions anatomie/contacts/projection existantes.
Avec les lots EditMode, **196 tests distincts ont réussi**.

Les builds Windows IL2CPP et Android ARM64 finaux ont tous deux terminé avec
succès. Le contrôle APK a réussi ; la réception physique et son résumé sont
validés par le retour propriétaire M1. Aucun nouveau rendu iEEG Quest ni parité
de projection iEEG n'est revendiqué.

## Validation manuelle

La [recette de fixture](../fixtures/mni-ieeg/README.md) décrit les valeurs et
les identifiants. Utiliser le lanceur fourni, car l'ouverture directe de l'archive
seule ne charge pas le protocole synthétique en mémoire.

L'APK final de **163 083 480 octets** a été vérifié (huit bibliothèques ARM64)
puis installé sur le Quest 3 `2G0YC5ZHB20370` : [preuve d'installation](../evidence/QUEST-020/device-install.json).
À la demande du propriétaire après reconnexion USB, le script Wi-Fi avec
`-KeepAwakeWhilePluggedIn` a confirmé `192.168.1.18:5555`, le maintien éveillé sur
alimentation et l'override de proximité D26. Cette installation n'est pas une
preuve de réception iEEG physique ; l'application n'a pas été lancée par l'agent.
Après la fin des tests Unity, la connexion Wi-Fi a été revérifiée et aucun PID
HiBoP n'était présent : [état final du casque](../evidence/QUEST-020/device-final-state.json).

1. Exécuter
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\HBP\Software\HiBoP\Tools\Run-QuestIEEGCapture.ps1" -Index 50 -KeepOpen`.
   Cette autorisation d'exécution est limitée au processus lancé ; cette commande
   a été fournie après le blocage du script par la politique PowerShell locale.
   Le Player est `.artifacts\quest-020\Windows\HiBoP.6.1.0.win64\HiBoP.exe`,
   l'archive `.artifacts\quest-020\fixture\quest-mni-ieeg.hibop`.
2. Dans `MNI iEEG`, vérifier la sélection à **0 ms**. Le JSON `capture.json`
   produit sous le chemin annoncé par le lanceur indique `NavigationIndex=50`,
   `ProjectionIndex=50`, `Alpha=0`, unités `uV` et plages `[-10,0,10]`.
   Choisir un autre instant dans la timeline puis appuyer sur **F8** crée un
   autre résumé correspondant à cette sélection. Les plages restent inchangées.
3. Pour contrôler le **résumé de transfert** dans l'interface, utiliser l'APK
   `C:\HBP\Software\HiBoP\.artifacts\quest-020\Android\HiBoP.Quest.apk`.
   Ouvrir **HiBoP** sur le casque. Dans le panneau **Quest** de Desktop,
   saisir `192.168.1.18` (adresse vérifiée lors de l'installation),
   cliquer **Inspect Quest**, comparer tous les groupes d'empreinte avec le
   panneau casque (**B** l'affiche), cocher **All groups match the fingerprint
   displayed in my Quest**, saisir le code actuel puis **Pair**.
   Sur la colonne **Synthetic uV**, cliquer **Envoyer au Quest**. Le résumé
   doit indiquer `QUEST-020 synthetic`, `0 ms`, `index 50 -> 50`, `alpha 0`, `uV`
   et `unit unspecified` pour les canaux absents, puis **Inputs received;
   iEEG rendering pending.**
4. Retour attendu pour **M1** : **OK/KO** sur l'identité et les unités du résumé,
   avec l'observation si KO. Aucun nouveau rendu iEEG au casque n'est attendu ici.
   La lecture du JSON seul ne valide pas ce geste d'interface. Après ce retour,
   arrêter uniquement HiBoP sur le casque et vérifier son absence selon D23.

**M1 validée le 2026-09-09** par « Ok je valide tout ce que tu as dit » :
identité `QUEST-020 synthetic`, `0 ms`, indices `50 -> 50`, `alpha 0`, unités
`uV` / `unit unspecified` et message de réception avec rendu iEEG en attente.
Cette preuve est le retour utilisateur sur la recette, sans journal de transfert
physique récupéré : HiBoP était déjà arrêté lors du contrôle final. La commande
`am force-stop fr.crnl.hibop.quest` a réussi ; à **15:57:34 UTC**, `pidof` retournait
1 sans PID, et l'ADB Wi-Fi restait connecté. Voir la [validation M1](../evidence/QUEST-020/manual-validation.json)
et le [contrôle d'arrêt](../evidence/QUEST-020/manual-stop.json).

## Décisions, limites et suite

Pas de décision produit nouvelle : les sélections non alignées sont préservées,
sans réduction d'usage. L'acceptation scientifique de la densité D30 ne s'étend
pas à l'iEEG ; aucune parité de projection iEEG n'est revendiquée.

La capture garde les limitations MNI existantes (surface complète anatomique,
volume NIfTI pris en charge, absence de coupes/déformation, frontières lissées).
La lecture des séries pour leur hash est synchrone et parcourt toute la
préparation : les mesures de la petite fixture ne qualifient pas son coût sur
des datasets volumineux. Le projet synthétique est un outil de développement.

Le formatage C# prescrit a réussi. Les changements automatiques des paramètres
Unity et ressources de build ont été sauvegardés dans
`.test-results/quest-020/unity-generated.patch` puis restaurés à l'état initial.
Les captures et gros binaires restent locaux ; le manifeste en contient les
chemins et SHA-256, et les JSON/XML de preuve sont conservés dans le dépôt.

Suite proposée : **QUEST-021**, partage du pipeline iEEG Desktop. Non engagée.
