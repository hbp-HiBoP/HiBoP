# Instrumentation temporaire du transfert Desktop → Quest

Document historique : l'instrumentation détaillée décrite ici a été retirée au
[lot 5](QUEST-transfer-final.md). Les traces existantes restent consultables ;
les players finaux ne produisent plus ces fichiers ni ces échantillonnages.

Référence : `visu_full_test / Small`. Ajout du 16 septembre 2026, après
[QUEST-030](QUEST-030-capture-freeze.md). Mesures physiques disponibles :
[premier essai USB](QUEST-transfer-baseline-01.md) et
[deuxième essai Wi-Fi](QUEST-transfer-baseline-02-wifi.md).
Le [plan d’optimisation](QUEST-transfer-optimization-plan.md) détaille les
actions proposées, leur ordre, les risques et les critères de validation.

## Players préparés le 16 septembre 2026

- Windows et Android construits depuis l'éditeur Unity 6000.5.2f1 avec les
  builders HiBoP : IL2CPP Release, OptimizeSpeed, non Development, sans connexion
  au Profiler. L'assembly `HBP.Transfer.Diagnostics` figure dans les deux rapports.
- Windows : `.artifacts/quest-transfer/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
  Android : `.artifacts/quest-transfer/Android/HiBoP.Quest.apk`, signé avec
  l'identité épinglée et vérifié par `Test-QuestApk.ps1` (335 928 028 octets).
- APK installée avec `adb install -r`, puis lancée sur le Quest 3 connecté.
  Le journal confirme `COMPOSITION READY`, OpenXR, Vulkan et le suivi de la tête.
  L'éditeur a été remis sur le profil DesktopWindows après les builds.
- Le player Windows est lancé avec `-pf` vers le projet de référence et
  `-v Small`. Les préférences disque avaient `AutomaticEEGUpdate=true` :
  désactiver ce réglage dans l'interface avant l'essai. L'accès Computer Use
  à HiBoP n'étant pas autorisé, le chargement visuel et ces réglages restent
  à confirmer par l'utilisateur.
- État initial et journaux dans `.test-results/quest-transfer/player-run-01`.
  Sélectionner manuellement l'adresse Wi-Fi `192.168.1.18` pour éviter le
  forwarding USB de la découverte automatique. Aucun transfert déclenché
  pendant cette préparation.

Le rapport Android indique `Succeeded` mais compte trois erreurs de nettoyage
éditeur (ressources de simulation XR déjà présentes et cache de samples).
Le journal de démarrage Quest contient aussi des messages Play Asset Delivery
et OpenXR ; ils sont conservés dans `quest-startup.log`. Le processus démarre
et atteint l'état XR prêt, mais cela ne remplace pas la vérification visuelle
du premier transfert. Les résultats collectés ensuite figurent dans les deux
rapports liés en tête de document.

## Premier essai

1. Reconstruire les players Desktop et Quest avec ces sources. L'instrumentation
   est activée aussi dans les builds non Development, pour ne pas manquer le
   premier essai. Elle se désactive dans `TransferTraceRuntime.Enabled`.
2. Ouvrir normalement `visu_full_test / Small`, appairer, puis utiliser
   **Envoyer au Quest** une fois. Conserver le calcul automatique désactivé pour
   comparer à QUEST-030 ; sa valeur est enregistrée. Garder le casque porté et
   les applications actives jusqu'à l'affichage. L'appairage et le chargement
   initial de Small précèdent le clic et ne font pas partie du chrono.
3. Attendre la ligne `QUEST_PERF file=...` des deux côtés. Chaque tentative a son
   propre fichier ; l'observation du rendu peut actualiser le fichier Quest après
   l'acquittement. En cas d'absence de caméra, attendre dix secondes de boucle
   Unity pour enregistrer `noCameraRenderWithin10s`.
4. Récupérer les JSON dans `Application.persistentDataPath/TransferTraces` :
   Desktop normalement `%USERPROFILE%/AppData/LocalLow/CRNL/HiBoP/TransferTraces`,
   Quest normalement `/sdcard/Android/data/fr.crnl.hibop.quest/files/TransferTraces`.
   Le chemin exact est celui de `QUEST_PERF`. Avec le numéro ADB déjà connecté :

   ```powershell
   New-Item -ItemType Directory -Force .test-results/quest-transfer/quest | Out-Null
   & C:/Android/Sdk/platform-tools/adb.exe -s <serial> pull /sdcard/Android/data/fr.crnl.hibop.quest/files/TransferTraces .test-results/quest-transfer/quest
   ```

   Le script existant `Tools/Save-QuestAnatomyEvidence.ps1 -Serial <serial>
   -Stage transfer-small` collecte aussi logcat, mémoire, Wi-Fi, batterie et
   état thermique sans relancer l'application. Le champ `route` Desktop distingue
   une liaison LAN du forwarding USB : ce dernier n'est pas un essai Wi-Fi.
5. Choisir les deux fichiers du même essai, puis :

   ```powershell
   python Tools/Read-QuestTransferTrace.py <desktop.json> <quest.json>
   ```

Un second essai à chaud sera utile seulement si le premier révèle un coût
important de chargement initial. Une nouvelle capture se fait avec **Envoyer** ;
**Réessayer** réutilise l'archive et ne remesure pas sa préparation.

## Ce qui est mesuré

| Partie | Détail |
| --- | --- |
| Préparation Desktop | Attente de l'initialisation et de l'anatomie, anatomie manquante de tous les patients, verrou de représentation, attente des générateurs/corrélations/colliders, première frame |
| Capture | Clonage du modèle, configuration, ressources par catégorie, surfaces, colonnes, JSON, copie des tableaux, empreintes et déduplication, cache des définitions globales, illustrations |
| Encodage | Attente du worker, ZIP par catégorie (JSON/buffers/fichiers), lecture source, hash de provenance, compression + écriture, finalisation ZIP, hash final, nettoyage |
| Connexion/envoi | TCP, TLS, autorisation, second hash complet du fichier, lecture disque, hash par chunk, écriture TLS, publication de la progression, attente de l'acquittement |
| Réception | En-tête, lecture TLS et attente par chunk, hash par chunk et global, écriture disque, fermeture du fichier, envoi de l'acquittement, nettoyage |
| Décodage | Dispatch Unity/worker, extraction par extension, lecture + inflation, écriture disque, relecture pour hash, désérialisation JSON, lectures des buffers, résolution des références globales, validation |
| Restauration | Installation/vérification/chargement des références standard, indication du cache MNI, prefab, volumes NIfTI, surfaces natives, ressources fonctionnelles, icônes/données dérivées, sites, colonnes, activité, configuration |
| Rendu | Géométrie, grille/projection, coupes et textures, surfaces fonctionnelles, sites, colliders, calcul des générateurs, attente des drapeaux de préparation, présentation des colonnes, première fin de rendu de la caméra principale |
| Contexte | Taille des données et archive, petits buffers, taux de déduplication, frames lentes et positions dans la trace, GC, mémoire managée et Unity, CPU/GPU/RAM, build, focus, cadence, calcul automatique |

Les durées sont mesurées avec `Stopwatch` monotone. Chaque métrique conserve
nombre d'appels, durée cumulée/minimale/maximale, octets associés et première/
dernière occurrence. Les grandes phases ont aussi des événements chronologiques
avec threads de début/fin. Les petits appels sont agrégés sans écrire un log par
buffer ou chunk. Les scopes représentent du **temps écoulé**, incluant les
attentes des méthodes async, pas du temps CPU consommé.

`archiveHash` relie les deux appareils sans modifier le protocole ; `transfer`
relie la capture à la publication et `attempt` distingue les essais locaux.
Les reprises ont le même hash : choisir explicitement la paire d'essais.

## Lecture correcte des résultats

- Les scopes imbriqués et travaux parallèles se recouvrent. **Ne pas additionner
  tous les temps**, ni Desktop + Quest : l'envoi et la réception se chevauchent.
- `tlsWrite` inclut chiffrement et pression des buffers réseau ; `tlsRead`
  inclut attente du producteur, réseau, déchiffrement et ordonnancement. Le débit
  calculé est le débit utile du pipeline, pas la vitesse physique de la radio.
- `deflateAndWrite` et `inflateRead` ne prétendent pas isoler compression et
  appels disque internes à `ZipArchive` ; les catégories, lectures sources et
  écritures d'extraction sont séparées pour localiser le coût.
- L'acquittement existant suit la préparation et la publication, sans attendre
  de nouvelle frame. L'observateur indépendant utilise
  `RenderPipelineManager.endCameraRendering` pour `Camera.main`, enregistre
  `renderStereo` et se détache après succès, remplacement, fermeture ou délai.
  Il indique une fin de rendu **côté CPU**, pas la fin GPU ni l'apparition des
  photons. Une caméra hors champ n'atteste pas la visibilité de chaque objet.
- Les horloges Desktop/Quest ne sont jamais soustraites directement. Le script
  borne le clic → rendu avec les causalités début d'envoi de l'en-tête/réception
  et début d'envoi de l'ACK/réception. La fin de `WriteAsync` n'est pas utilisée
  comme ancre : sa continuation peut s'exécuter après la réception par le pair.
  La largeur de l'intervalle comprend transit réseau et ordonnancement ;
  une faible dérive de fréquence des horloges reste possible.
- Les écarts entre callbacks de frame incluent le blocage du thread principal,
  l'attente de cadence et l'éditeur non focalisé ; ce ne sont pas des temps GPU.
- Les compteurs GC et la mémoire sont globaux au processus. L'instrumentation
  ajoute des lectures d'horloge, quelques verrous et allocations. Le JSON est
  écrit sur worker en fin d'opération, sans sérialisation sous le verrou des
  mesures. Son coût n'est pas inclus dans `operationMs` ; une sauvegarde après
  ACK peut encore chevaucher la première frame.
- Les traces sont conservées en mémoire jusqu'à la fin ou l'exception gérée.
  Un arrêt brutal/crash avant sauvegarde ne garantit pas de JSON. Les événements
  détaillés sont plafonnés à 4096 ; `droppedEvents` signale le dépassement, les
  agrégats restent complets. Aucun signal scientifique ni secret d'appairage
  n'est écrit ; le nom de la visualisation et le matériel figurent dans la trace.

`quest.render.waitFlags.N` mesure les intervalles de boucle pendant lesquels
les conditions suivantes sont encore présentes (masque binaire, recouvrements
possibles) : géométrie 1, générateur actif 2, sites 4, coupes 8, textures de base
16, textures fonctionnelles 32, textures GUI 64, surface fonctionnelle 128,
générateur non à jour 256. Ce dernier peut être autorisé si le calcul automatique
est désactivé ; seul, il ne signifie pas un blocage.

## Retrait

Rechercher `TransferTrace`, `TransferDiagnostics` et `.Trace` dans les fichiers
de transfert concernés. L'assembly `HBP.Transfer.Diagnostics` est autonome ; les
références ajoutées dans les asmdef et les paramètres optionnels sont temporaires.
Supprimer les scopes/compteurs, l'observateur de rendu, cet assembly et ses deux
tests dédiés après les optimisations. Les contrats sérialisés, préfabriqués et
formats réseau n'ont pas changé.

## Validation du 16 septembre 2026

- Formatage C# exécuté avec `Tools/format-code.cmd` ; compilation Unity sans
  erreur sur la cible Desktop.
- Les **46 tests EditMode** `HBP.Transfer.Scene.Tests` passent. Ils comprennent
  les contrôles des chunks instrumentés (données valides et corrompues), les
  agrégats concurrents, une sauvegarde après le jalon de rendu et l'échec
  d'écriture des traces sans échec du transfert.
  Preuve : `.test-results/quest-transfer/editmode-instrumentation.json`.
- **Un test PlayMode**, `CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures`,
  passe avec la trace activée sur la restauration native et la recapture. Il
  vérifie aussi le rejet d'un remplacement incompatible, l'annulation et la
  présence des mesures dans le JSON. L'exception de topologie de la console est
  attendue par ce test. Preuve :
  `.test-results/quest-transfer/playmode-instrumentation.xml` (96,05 s).
  Le suivi MCP a expiré avant le démarrage, pendant la correction d'une référence
  d'assembly du test ; Unity a ensuite exécuté le test déjà demandé. Le verdict
  ci-dessus vient du XML NUnit produit par cette exécution, sans relancer le test.
- Le lecteur Python a été vérifié sur des entrées synthétiques : bornes causales,
  rejet de hashes différents et absence de jalon de rendu.
- Les getters utilisés par Json.NET sont préservés via `link.xml` pour IL2CPP.
  Les builds Windows/Android et le transfert physique de Small restent à faire ;
  ces tests ne constituent pas une mesure de performance sur le Quest.
