# Prompt de reprise — Lot C / SCENE-008

Copier le texte ci-dessous dans une nouvelle tâche ouverte sur le dépôt HiBoP du nouvel ordinateur. État arrêté le 10 septembre 2026 au soir.

## Mise à jour du 11 septembre 2026

Dernier jalon : builds scene-011 installés, première livraison `Published`
confirmée par le journal Desktop et colonnes/labels vus par le propriétaire.
Voir `evidence/final/resume-2026-09-11/first-published-scene.json` et
`rebuild-scene-011.json`, qui remplacent les identités de builds ci-dessous.
Quest absent d’ADB lors du dernier suivi ; interactions et diagnostic
scientifique sur Quest restent à vérifier.

Le texte du 10 septembre ci-dessous reste historique. L’état courant est dans
`evidence/final/resume-2026-09-11/manifest.json`, `optional-illustrations-fix.json`
et `math-deployment-fix.json`. Sur le poste du laboratoire :

- Quest USB `2G0YC5ZHB20370`, app `fr.crnl.hibop.quest`, redirection ADB du
  port 45871 ; adresse Desktop `127.0.0.1`, aucun Wi-Fi requis pour cette recette.
- Migration vers une clé commune terminée et données restaurées/vérifiées.
  Clé privée hors Git : `%LOCALAPPDATA%/HiBoP/Signing/QuestDevelopment`.
  Ne pas désinstaller à nouveau ni exposer les secrets ; mises à jour `install -r`.
- Illustrations facultatives absentes corrigées, sans restaurer les fichiers :
  elles ne conditionnent plus empreinte/appairage/transfert des protocoles.
  Tests de transfert : 23/23 ; nouveaux Players au format global 2.
- Après appairage réussi, trois livraisons ont échoué pendant la préparation
  Quest avec `DllNotFoundException hbp_math`. Plugin Android du même commit que
  Desktop ajouté, probe réel passé, contrôles de build renforcés : 4/4 tests.
- APK courant installé : `.artifacts/scene-010/Android/HiBoP.Quest.apk`, SHA-256
  `b840bd02ed764711f0f1f4a416b4fe3644bb09a6485b7af54dc5e453fe51764e`.
  Desktop corrigé : `.artifacts/scene-009/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
  31 références standard vérifiées ; aucun Localizer embarqué. Leur distribution
  séparée reste une exigence explicite, y compris en développement.
- Un nouvel essai manuel est demandé au propriétaire après cette installation.
  Publication des six colonnes, interaction physique et qualification complète
  IL2CPP restent à prouver. Ne pas confondre probe natif et scène validée.
- HiBoP n’est pas autorisé dans l’outil de contrôle UI. Le propriétaire effectue
  l’appairage et valide le dialogue Meta « contrôleurs requis » si présent.
- Aucun commit ni push. Sauvegarde de migration conservée sous
  `.test-results/scene-008/resume-20260911/device-backup`.

---

Reprends la qualification du Lot C (SCENE-008), dans `Docs/dev/quest-autonomous/scene-column-refactor`. L'implémentation est présente ; l'objectif reste de revalider globalement QUEST-001 à QUEST-023 avec le système commun de scènes et les six modalités, puis de corriger les défauts trouvés. Ne considère pas les succès du prototype comme une validation du nouveau système.

Je reprends sur un autre ordinateur. Adapte les chemins au checkout courant. Inspecte le dépôt et les prérequis avant de compiler ; demande-moi tôt les informations concernant le Quest, les éditeurs ouverts ou une interaction physique. Ne relance pas une investigation déjà résolue sans nouvel indice. Ne committe et ne pousse rien sans demande.

## Documents à lire

- `AGENTS.md`, puis le `README.md` du chantier, `01-objective-and-scope.md`, `02-target-architecture.md`, `03-payload-and-restoration.md`, `04-migration-and-validation.md`.
- `TASK-WORKFLOW.md`, `TASK-STATUS.md`, les fiches SCENE-001 à SCENE-008, notamment les critères de SCENE-008.
- `reports/FINAL.md`, `reports/JOURNAL.md` et les références historiques QUEST/D30/D31 qu'ils citent pour les critères et tolérances.
- `evidence/final/pause-2026-09-10/resume-state.json` et ses fichiers associés : état le plus récent. `evidence/final/manifest.json` est un instantané antérieur aux dernières corrections physiques ; ses empreintes ne désignent PAS les nouveaux builds. Les chemins de preuves ignorés par Git peuvent être absents ici.

## État acquis

- Branche utilisée : `feature/xr-autonomous`, base de travail `753d3ce7219d66ede83c4fe281b5640b5b71329e`. Les modifications du Lot C n'étaient pas encore committées pendant la session ; identifier le commit réellement récupéré ici, sans réinitialiser le dépôt sur cette ancienne base.
- Scène commune, capture/restauration, géométrie, cycle de vie, références de prefabs et diagnostics des six modalités implémentés. Statut honnête : **IMPLEMENTEE / PARTIELLE / EN_ATTENTE**.
- Campagne EditMode consolidée : 736 réussis, 2 ignorés. Tests existants Module3D/Toolbar : 61 réussis. Deux scénarios Desktop/restauration MNI et patient réussis. Comparaisons éditeur : 72 et 78 comparaisons de colonnes avec buffers identiques bit à bit. Contrats de transfert/empaquetage : 17/17 réussis. Voir le rapport pour les échecs anciens remplacés par des passes corrigées.
- Derniers tests de graphiques : 4/4 réussis, XML et commande conservés dans le dossier de pause. Dernier formatage C# : 31 fichiers, réussi.
- Les **deux Players Windows et Android corrigés ont été compilés avec succès**. Le Windows corrigé a exécuté le diagnostic des six modalités avec `success: true`, sans exception dans le journal vérifié. Le nouvel APK n'a **pas encore été installé ni testé sur Quest**. Il faut reconstruire sur cet ordinateur.
- Les builds, fixtures générées et journaux complets dans `.artifacts` et `.test-results` sont ignorés par Git. Les petites preuves de pause sont versionnables ; les buffers scientifiques complets et les APK ne sont pas inclus. Ne pas prétendre avoir relu des preuves absentes ; les régénérer si nécessaires.

## Décisions et dernières corrections à préserver

1. `Base3DScene.Transfer` appelle `UpdateGeometry` avant `ApplyState` : la géométrie reçue est initialisée, puis les masques d'effacement sont réappliqués. Le transfert ne doit pas faire réapparaître les triangles effacés. Un test de restauration couvre ce masque ; vérifier aussi lors de la recette physique.
2. Aucun doublon scientifique sous `Assets/StreamingAssets`. `QuestStandardDataBuild : BuildPlayerProcessor` utilise `BuildPlayerContext.AddAdditionalPathToStreamingAssets` directement depuis `Assets/Data`. Seul le manifeste SHA-256 est généré dans `Library/HBP/QuestStandardData`. Les références `.gz` sont empaquetées en `.gz.bytes`, puis installées sous leur nom scientifique normal : cela empêche Gradle de les décompresser lors du build. Ne pas rétablir la copie des données sous Assets. Le dernier build laisse `Assets/StreamingAssets` absent.
3. Premier essai physique : `IOException: Read-only file system` pendant l'initialisation de `GeneralPreferences`. Correction Android : chemin par défaut à partir de `UserPreferences.PATH` déjà initialisé ; pas de création des répertoires Desktop reçus dans le constructeur. Ne pas appeler `Application.persistentDataPath` depuis la désérialisation en worker. Correction compilée, confirmation physique à faire.
4. Premier Desktop visible : pool de graphiques insuffisant avec MEG et recherche CCEP chez un autre patient que celui de la stimulation. `GraphZone` agrandit le pool avec le prefab existant et limite les réponses CCEP au patient de la source. Deux tests ajoutés, quatre cas GraphZone réussis. Correction vérifiée dans le Player Windows.

## Préparation sur ce poste

Vérifier Unity `6000.5.2f1` (lire `ProjectSettings/ProjectVersion.txt`), le support Windows IL2CPP et Android SDK/NDK/OpenJDK, Python, `Assets/Data`, les fixtures natives et les plugins ARM64/Windows. Les entrées sont dans Git, notamment `Assets/Tests/Fixtures/Projects/Generated/native-fixture-reference.hibop` ; vérifier leur présence réelle. Comparer les plugins à `Tools/NativePlugins.lock.json` avant de décider s'il faut les récupérer. Ne pas reconstruire arbitrairement les bibliothèques natives ni changer leurs versions.

Avec l'éditeur fermé, lancer Unity CLI hors sandbox, avec `Start-Process -Wait -PassThru -WindowStyle Hidden`, conformément à AGENTS. Si l'éditeur est ouvert, suivre le workflow Unity MCP. Ne pas lancer un second éditeur sur le même projet. Régénérer les fixtures, qui contiennent des chemins absolus :

```powershell
python .\Tools\Prepare-SceneQualificationFixture.py
.\Tools\Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId scene-008
.\Tools\Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId scene-008
.\Tools\Run-SceneQualification.ps1 -KeepOpen -Evidence .artifacts/scene-008/device-desktop
```

Le générateur appelle lui-même `Prepare-QuestIEEGFixture.py` et crée les deux projets MNI/patient. Lire les scripts avant exécution. La variante patient s'obtient avec `-FixtureName scene-008-patient`. Les builds sont des builds de développement afin d'activer les diagnostics. Vérifier les journaux réels et l'absence d'erreurs d'interface, en plus du JSON scientifique.

## Recette Quest encore à faire

Le précédent appareil était un Quest 3, série USB `2G0YC5ZHB20370`, package `fr.crnl.hibop.quest`. L'ancienne IP `192.168.1.18` est indicative : redécouvrir les appareils autorisés et l'IP sur ce poste. Aucune empreinte ni aucun code d'appairage précédent ne doit être réutilisé. L'ancien APK installé contient encore le défaut de préférences ; ne pas le prendre pour le build corrigé.

1. Installer le nouvel APK avec `adb -s <série> install -r <apk>` sans effacer les données. Lancer HiBoP et vérifier le journal du nouveau processus, en particulier l'absence de l'IOException de préférences. Lors du premier essai, OpenXR/Vulkan atteignait `COMPOSITION READY`, mais cela ne prouvait pas le bon rendu. Le message OpenXR Meta `No suitable capture camera found` reste à examiner si le rendu/passthrough pose problème. Si le système affiche « contrôleurs requis », me demander de réveiller les manettes et de valider.
2. Préparer le panneau Quest du Desktop, inspecter l'adresse réelle, comparer l'empreinte complète avec le casque et réaliser l'appairage. Le code expire après cinq minutes ; Y sur la manette gauche le renouvelle. Me laisser accomplir l'étape d'authentification si le skill de contrôle UI l'impose. **Aucun appairage ni transfert réel n'a été accompli dans la session précédente.**
3. Envoyer la visualisation complète par le flux normal. Vérifier la publication effective des six colonnes et l'installation des références. Me demander les observations visuelles : passthrough, surfaces, contacts, contraste, lisibilité ; manipulation indépendante, translation/rotation/échelle, relâchement/reprise et stabilité des autres colonnes.
4. Après publication, déclencher le diagnostic commun par `adb -s <série> shell touch /sdcard/Android/data/fr.crnl.hibop.quest/files/scene008-qualify`. Le build de développement attend une scène réellement publiée, exécute le diagnostic et écrit `scene-008/<horodatage>` dans ce même répertoire persistant. Suivre `SCENE008_EVIDENCE` dans logcat, récupérer le dossier complet et comparer au Desktop avec `Tools/Compare-SceneQualification.py` (lire son aide). Conserver les écarts max/RMS et les états ; n'inventer aucune tolérance pour déclarer réussi un résultat différent.
5. Tester la déconnexion Desktop avec scène conservée et recalcul autonome, le remplacement de scène et la variante patient, les coupes, ressources mesh/IRM, timelines/essais, masques et poses. Respecter la recette intégrée de FINAL.md et expliciter toute couverture historique manquante. Mesurer temps et mémoire Android/PSS ; la mémoire Unity échantillonnée n'est pas un pic PSS.
6. Laisser l'application ouverte pendant ma recette, recueillir mon retour, corriger et retester les défauts. Arrêter HiBoP sur le casque seulement après confirmation de fin de recette, sans désactiver ADB ni supprimer les anciennes preuves.

Terminer en mettant à jour le rapport, le journal, les statuts et les preuves correspondant aux binaires réellement testés. Le script historique de collecte situé dans `.test-results/scene-008/collect-final.py` était local et n'est pas disponible par Git ; ne pas en dépendre sur ce poste. Le manifeste antérieur doit être remplacé ou complété explicitement, sans attribuer ses anciens succès aux nouveaux binaires. Exécuter le formateur avant remise de nouvelles modifications C#, puis `git diff --check`. Ne déclarer la qualification complète qu'avec les résultats physiques et mon retour effectif.
