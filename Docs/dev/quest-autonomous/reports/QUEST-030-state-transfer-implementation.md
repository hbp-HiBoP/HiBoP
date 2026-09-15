# QUEST-030 — Implémentation et validation du chargement commun

Date : 2026-09-15.
Statut : **CLOS — acceptation fonctionnelle du propriétaire le 2026-09-15**.

## Résultat

Le [plan approuvé](QUEST-030-state-transfer-analysis.md#9-implémentation-du-plan-approuvé)
est implémenté. Le transfert contient la visualisation préparée et ses
configurations enrichies. L’ouverture Desktop et la restauration utilisent la
même initialisation scientifique et la même politique de calcul automatique.
Les préférences restent globales, envoyées uniquement à l’appairage.

La disponibilité n’impose plus un calcul. Les tests vérifient qu’une ouverture
avec auto compute désactivé ne calcule pas l’activité et qu’un calcul manuel
reste possible. Les résultats calculés historiques et les états transitoires
ne sont pas transportés, conformément au contrat de réouverture.

Le format de transfert est maintenant **3** : Desktop et Quest doivent utiliser
les nouvelles builds. Les configurations de projet antérieures restent lisibles.
Aucun changement de `hbp_core`, des bibliothèques natives, des préférences par
colonne, des prefabs ni du protocole d’appairage.

## Vérifications automatisées

Unity **6000.5.2f1**, CLI avec boucle PlayerLoop active :

| Ensemble | Résultat | Preuve locale |
| --- | --- | --- |
| Sérialisation/configuration, constructeurs, migration CCEP et archives | 35 réussites | `.test-results/quest-parity/config-editmode.xml` |
| Restauration sur les deux présentations, auto false/true, couleurs et identité atlas | 2 réussites | `.test-results/quest-parity/config-playmode.xml` |
| Six modalités, recapture, remplacement refusé et annulation | 1 réussite | même fichier |
| Projets Desktop complets MNI et anatomie patient : ouverture, transfert, sauvegarde/relecture | 2 réussites | même fichier |

Bilan de la révision : **40 tests réussis, aucun échec**.
`.test-results/quest-parity/config-test-summary.json` recense ces résultats.
Les résultats de la première passe, comprenant les tests de prévisualisation et
la sauvegarde avant géométrie, sont conservés dans `test-summary.json` et leurs
XML d’origine. Le formateur C# du dépôt et `git diff --check` ont été exécutés.

La revue indépendante a conduit à vérifier l’ordre source CCEP/calibration,
la géométrie avant overlays, la propagation des erreurs de masques, l’ordre
surface/hémisphère et la conservation des couleurs atlas. Le test d’archive
incompatible vérifie que la scène précédemment publiée reste en place.

## Builds et recette sur appareil

La build Windows IL2CPP est compilée et son player a passé le diagnostic à six
modalités : **12 états, résultat réussi**. Preuves :
`.test-results/quest-030/windows-build.log` et
`.test-results/quest-parity/config-windows-player/result.json`.
Binaire : `.artifacts/quest-030/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.

La build Android IL2CPP ARM64 a également réussi (sortie Unity 0). Son contenu
a passé `Tools/Test-QuestApk.ps1` : 9 bibliothèques ARM64. Preuves :
`.test-results/quest-030/android-build.log` et
`.test-results/quest-030/apk-content.json`.
APK signé et vérifié : `.artifacts/quest-030/Android/HiBoP.Quest.apk`.

Le propriétaire a fourni l’archive de signature partagée. Elle a été extraite
vers `%LOCALAPPDATA%/HiBoP/Signing/QuestDevelopment` avec accès limité au compte
Windows courant. `Tools/Sign-QuestApk.ps1 -ValidateOnly` a confirmé le certificat
épinglé du projet, puis l’archive fournie a été supprimée. Aucun contenu privé
n’a été ajouté au dépôt ni aux preuves. La signature ne bloque plus la recette.
Les deux builds ont été renouvelées après la révision de configuration ci-dessous.
Le script normal de build Android a signé et vérifié l’APK avec l’identité
partagée. L’installation en Wi-Fi a réussi et l’application a été lancée sur le
Quest 3. Preuve : `.test-results/quest-parity/config-device-deployment.json`.
Le propriétaire réalisera la recette interactive plus tard.

Les changements automatiques de BuildInfo et des assets URP créés par les
builds ont été retirés du diff source.

La connexion ADB Wi-Fi a été établie et vérifiée sur le Quest 3, puis le
propriétaire a pu retirer le câble USB du PC. Il participera à l’appairage et à
l’envoi de la visualisation.

Le [signalement initial](QUEST-030-activity-visibility.md) a ensuite été clos
sur acceptation fonctionnelle explicite du propriétaire le 2026-09-15. Les tests
locaux restent distincts de ce retour utilisateur ; aucun détail de recette
non fourni n’est déduit de cette acceptation.

## Révision demandée après revue du code

- `CCEPConfiguration` hérite de `DynamicConfiguration` et regroupe source et
  paramètres dynamiques dans `CCEPColumn.CCEPConfiguration`. Les anciens JSON
  `DynamicConfiguration`, avec ou sans type explicite, sont lus via le registre
  d’alias de propriétés et une conversion de désérialisation externe à la classe ;
  leur ID et leur calibration sont conservés.
  Les nouvelles sauvegardes écrivent uniquement `CCEPConfiguration`.
- `AtlasConfiguration` hérite de `BaseData`. Les deux classes possèdent les
  constructeurs, `Clone` et `Copy` habituels ; la capture conserve l’ID d’une
  configuration d’atlas existante.
- Les constructeurs des configurations de visualisation, anatomie, FMRI, MEG
  et Static prennent les nouveaux paramètres. Les anciens appels gardent leurs
  valeurs par défaut. Les masques sont copiés à la construction.
- `Base3DScene.Configuration.cs` regroupe chargement, sauvegarde, capture,
  réinitialisation et application des paramètres configurés. Les méthodes
  communes de finalisation et de disponibilité sont dans `Base3DScene.cs`,
  avec l’initialisation générale. `Base3DScene.Transfer.cs` conserve les helpers
  de capture/préparation pour le transfert.
- Le registre IL2CPP a été régénéré. Les 35 tests EditMode et les 5 scénarios PlayMode de cette révision passent.
  La nouvelle build Windows et son diagnostic IL2CPP (12 états) ont réussi ;
  la build Android ARM64, sa signature et les contrôles de contenu ont réussi.

## Taille de l’APK après empaquetage incrémental

L’APK signé mesure 2 271 300 647 octets, mais ses entrées référencées ne
représentent que 349 406 921 octets compressés. L’inspection des en-têtes ZIP
identifie 1921871986 octets d’entrées virtuelles sans nom, caractéristiques
du remplissage des espaces libres de Zipflinger. Le surplus est déjà présent
dans `launcher-debug.apk` avant la signature partagée. Il relève donc de
l’empaquetage incrémental Android, pas du contenu scientifique actuel.
Preuve : `.test-results/quest-parity/apk-size-analysis.json`.
Correction appliquée dans `HBPAndroidPackaging` : le callback Unity configure
les tâches Gradle `PackageApplication` avec `outputs.upToDateWhen { false }`.
Gradle demande alors à AGP un empaquetage complet, qui recrée l’APK avant son
alignement et sa signature. Les caches de compilation restent utilisables.
Ce callback couvre les builds par Build Profiles, les scripts locaux, les
exports Gradle et le workflow GitHub ; il ne dépend pas du script de signature.

`Tools/Test-QuestApk.ps1`, déjà appelé par le script local et la CI avant
publication, rejette désormais un surplus dépassant 1 Mio + 64 Kio par entrée.
Ce seuil réserve de la place aux métadonnées ZIP, à l’alignement et aux signatures
sans imposer de plafond aux données scientifiques.

Validation de la correction :

- L’ancien APK est rejeté : 1 921 893 726 octets de surplus pour
  349 406 921 octets de contenu compressé (`apk-size-rejected.txt`).
- Une vraie build Unity Android avec le cache existant produit un APK signé de
  **349 424 679 octets**, dont seulement **17 772 octets** de surplus.
  Gradle conserve 65 tâches sur 69 à jour.
- Une nouvelle invocation Gradle `assembleDebug assembleRelease` conserve les
  caches et confirme un empaquetage complet pour les deux variantes. Les APK
  Debug (349 415 367 octets) et Release (340 264 437 octets) passent le contrôle.
  Cette vérification Release porte sur l’empaquetage Gradle des entrées Unity
  existantes ; le workflow distant GitHub n’a pas été déclenché.
- Les noms des entrées sont conservés ; tous les CRC sont valides et les données
  scientifiques ainsi que les bibliothèques natives sont identiques octet pour
  octet à celles de l’ancien APK. La signature partagée et l’alignement Android
  du nouvel APK sont valides.
- L’APK corrigé a été installé en Wi-Fi sur le Quest 3 puis lancé avec succès.
  La recette interactive de visualisation reste différée par le propriétaire.

Preuves complémentaires sous `.test-results/quest-parity/` :
`apk-packaging-unity-build.log`, `apk-packaging-repeat-gradle.log`,
`apk-packaging-debug.json`, `apk-packaging-release.json`,
`apk-packaging-content-comparison.json`, `apk-packaging-install.log`,
`apk-packaging-launch.log` et `apk-packaging-device-deployment.json`.
Rapport final signé : `.test-results/quest-030/apk-content.json`.


## Clôture fonctionnelle et point de performance restant

Le 2026-09-15, après les corrections de parité, d’empaquetage APK et de préparation
Desktop, le propriétaire accepte les fonctionnalités et demande la clôture.
La correction du gel Desktop, ses 43 tests EditMode, son test de restauration
complète et ses mesures sur Small sont détaillés dans le
[rapport de préparation](QUEST-030-capture-freeze.md).

**Point restant : préparation Quest trop longue après réception.** Aucun profilage
par phase sur appareil n’a encore établi la répartition du temps. L’acceptation
fonctionnelle ne valide pas cette performance et aucune correction supplémentaire
n’est appliquée lors de la clôture.

Le chemin observé dans le code explique pourquoi cette phase n’est pas une simple
affectation de champs :

1. `QuestAnatomySession.PrepareAsync` affiche l’état Preparing avant `SceneArchive.Read`.
   Cette lecture extrait chaque entrée ZIP dans un fichier, vérifie son empreinte,
   désérialise le JSON et rouvre les buffers numériques. Les petits fichiers
   restent donc matérialisés côté réception, malgré leur suppression du chemin
   de capture Desktop. C’est un candidat concret au coût d’E/S, pas un goulet
   d’étranglement mesuré sur Quest.
2. `SceneRestoration.PrepareAsync` vérifie les fichiers scientifiques de référence,
   assure la disponibilité des ressources standard, recharge les volumes NIfTI
   propres à la scène et reconstruit les surfaces natives. Les pointeurs natifs,
   objets Unity et ressources GPU du PC ne sont pas transférables tels quels.
3. `Base3DScene.InitializeContentAsync` reprend ensuite l’initialisation commune :
   ressources configurées, sites, colonnes et meshes. La finalisation applique
   la configuration et prépare géométrie, grille de projection et liaisons aux
   surfaces. Ces structures peuvent être nécessaires même avec le calcul
   automatique d’activité désactivé ; leur préparation ne signifie pas que
   l’activité est recalculée.
4. `PrepareRenderingAsync` attend les mises à jour nécessaires au premier rendu,
   puis `QuestAnatomyView` crée la présentation des colonnes et publie la scène.
   L’accusé de publication n’est envoyé qu’après cette préparation.

Les données iEEG préparées évitent de relancer leur lecture et leur traitement
sources. Elles ne représentent pas une image mémoire exécutable de la scène.
Le contrat reste celui d’une réouverture Desktop avec initialisation commune.
Cela justifie l’existence d’un travail local, pas sa durée actuelle. Une reprise
sur ce point devra mesurer extraction/vérification, désérialisation, reconstruction
native, initialisation commune et première présentation séparément, puis cibler
les coûts dominants sans modifier le contrat fonctionnel.


## Révision finale avant commit : alias de propriétés

La compatibilité `CCEPColumn.DynamicConfiguration` est déplacée hors de la classe :
déclaration dans `Assets/SerializationTypeAliases.json`, enregistrement généré,
résolveur commun projet/transfert et conversion C# dédiée. Les anciens fichiers
avec ou sans `$type` conservent ID et calibration ; les nouvelles écritures
utilisent uniquement `CCEPConfiguration`. Un document présentant les deux noms
est rejeté dans les deux ordres. Les autres colonnes gardent leurs propriétés.

Validation Unity 6000.5.2f1 : **634 tests EditMode réussis, aucun échec**, suites
`HBP.Serialization.Tests` et `HBP.Transfer.Scene.Tests`, sortie CLI 0. Les tests
couvrent également null, lectures concurrentes, réutilisation par Populate,
registre généré et migration à travers une archive de transfert. Le formateur
C# et `git diff --check` passent. Une revue indépendante du mécanisme de contrat
partagé et de la compatibilité IL2CPP n'a pas relevé de défaut ; aucune nouvelle
build IL2CPP n'est nécessaire à cette validation EditMode et n'a été produite.
Preuves : `.test-results/property-aliases/editmode-verified.xml` et `summary.json`.

Les manifestes de contrat ont été actualisés pour les configurations et champs
acceptés dans ce chantier. La suppression de ces seules évolutions dans les
manifestes reconstitue exactement les empreintes approuvées précédentes
(540 membres et 376 contrats de cycle de vie) ; les références courantes sont
573 et 384. Preuve : `.test-results/property-aliases/manifest-review.json`.
