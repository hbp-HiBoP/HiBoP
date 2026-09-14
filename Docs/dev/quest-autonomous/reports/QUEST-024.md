# QUEST-024 — Qualification du prototype Windows + Quest

## Résultat actuel

Campagne réalisée sur le refactor SCENE-001 à SCENE-008. Les anciennes classes
HBNA spécialisées restent des références historiques ; la qualification porte
sur les scènes communes et les six modalités actuelles.

Le transfert Windows → Quest par USB a réussi sur les binaires **révision 2**.
Les trois passages physiques MNI couvrent douze états et six colonnes, avec
mémoire et nombres de meshes/textures/matériaux stables sur Quest. Le recalcul
pendant une coupure de 60,292 s est exact par rapport au passage connecté.
Le propriétaire confirme la réception des six colonnes, la lisibilité et les
gestes indépendants pendant la coupure. La première campagne SCENE-011 avait
déjà validé M1/M2/M3, dont les poses après reconnexion.

**QUEST-024 implémentée : prototype Windows + Quest validé pour la recette
MNI et patient synthétique corrigé, sur les binaires révision 2 via USB.**
Les tests d’intégration et les comparaisons Desktop/restauré réussissent. Les
Players révision 2 ont été éprouvés sur MNI et sur la fixture patient synthétique
corrigée : trois passages Quest par fixture, répétitions exactes, ressources
scientifiques stables et gestes validés, y compris pendant une coupure USB.
Le propriétaire a confirmé « Petient ok » puis « M3 patient OK ».
Le propriétaire accepte les seuils numériques pour cette recette uniquement.
Le petit reliquat
de meshes vides Desktop est documenté ; il n’apparaît pas sur les trois passages
Quest de chacune des deux fixtures.
La qualification Wi-Fi n'a pas été exécutée,
le poste et le casque disposant uniquement d'une liaison USB pour cette campagne.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** dans le périmètre de recette décrit.
- Manuel : **VALIDE** sur MNI et patient synthétique corrigé, y compris
  réception, gestes indépendants et stabilité pendant la coupure sur révision 2.
- Source : `feature/xr-autonomous@03cd45220b2082902a33b59682086f43ab03cb93`,
  arbre initial propre, Unity `6000.5.2f1`.
- Les premières mesures ci-dessous utilisent les Players SCENE-011. Les corrections
  QUEST-024 de durée de vie des ressources et le diagnostic enrichi ont ensuite
  été construits et qualifiés séparément sur les Players révision 2. Les premiers
  retours M1/M2/M3 restent attribués aux seuls binaires SCENE-011.
- Players réutilisés pour la campagne initiale :
  - Windows : `C:/HBP/Software/HiBoP/.artifacts/scene-011/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
  - Quest : `C:/HBP/Software/HiBoP/.artifacts/scene-011/Android/HiBoP.Quest.apk`,
    SHA-256 `6f782773e4793970200753a7982a4481fb5574f8c4685bb64946b5e87f56e1a0`.
- Lors de la campagne initiale, l'APK installé possédait cette même empreinte. Ses neuf bibliothèques ARM64
  passent `Test-QuestApk.ps1` ; `hbp_core` et `hbp_math` correspondent au verrou
  `Tools/NativePlugins.lock.json`.
- Binaires corrigés : `.artifacts/quest-024/revision-2/Windows/HiBoP.6.1.0.win64/HiBoP.exe`
  et `.artifacts/quest-024/revision-2/Android/HiBoP.Quest.apk` ;
  [empreintes et builds](../evidence/QUEST-024/players-revision-2.json).
  APK installé vérifié SHA-256 `7da5d0a50f3ca52fab755005978e56de07e9dcb438c891865a8f9304139b79ce`.
  Les trois bibliothèques natives Windows et les neuf bibliothèques ARM64 de l'APK
  passent leurs contrôles. Le formatage final ne change qu'une fin de ligne de
  `SceneQualification.cs` par rapport au point de contrôle compilé, vérifiée octet
  par octet (`source-final-format-verification.json`).
- [Preuve manuelle durable](../evidence/QUEST-024/manual-validation.json).
- [Manifeste des preuves](../evidence/QUEST-024/manifest.json).
- Dossier local de campagne : `C:/HBP/Software/HiBoP/.test-results/quest-024`.

## Vérifications effectuées

| ID | Vérification | Résultat / preuve locale |
| --- | --- | --- |
| T1 | Diagnostic Player Windows MNI, 12 états × 6 colonnes | Réussi, 12,113 s ; `windows-mni-visible/result.json`. |
| T2 | Comparaison au dernier diagnostic Windows SCENE-011 | 72 comparaisons exactes, états équivalents ; `windows-regression.json`. |
| T3 | Diagnostic Player Windows patient, mesh/IRM alternatifs | Réussi, 20,434 s ; `windows-patient/result.json`. |
| T4 | Publication réelle via appairage et bouton Envoyer au Quest | `Published`, 998 099 octets, capture 767,724 ms, total 17 123,791 ms ; `windows-mni-visible/player.log`. |
| T5 | Premier diagnostic Quest MNI après réception | Réussi, 16,911 s ; `quest-mni-first/result.json`. |
| T6 | Passage chaud Quest dans le même processus | Réussi, 19,975 s ; `quest-mni-awake/pass-1/result.json`. |
| T7 | Diagnostic pendant retrait de la redirection USB | Réussi, 19,995 s ; coupure 60,894 s, PID 24182 conservé, redirection restaurée ; `quest-mni-usb-outage/run.json`. |
| T8 | Comparaison passage chaud connecté / passage hors liaison | 72 comparaisons exactes ; `mni-offline-repeat.json`. |
| T9 | Comparaison Windows / premier diagnostic Quest | États catégoriels équivalents, écarts flottants à examiner ; `mni-parity-first.json`. |
| T10 | Tests du comparateur, tailles incohérentes, corruption, preuves vides et textures RGBA | 6 tests Python réussis. |
| T11 | Première campagne Unity PlayMode `HBP.Transfer.Scene.PlayModeTests` | Échec : deux dépassements du délai Unity de 180 s et une NullReference entre les deux ; `unity/playmode.xml`. |
| T12 | Coupes, ressources et trois cycles de remplacement/suppression | 10/10 tests PlayMode réussis ; `unity/cuts-playmode-fixed.xml`. Le nettoyage des meshes source à la fermeture est ensuite étendu, à revérifier. |
| T13 | Restauration native, recapture et remplacement annulé/refusé, cas isolé | **Preuve invalidée** malgré le statut Passed de Unity : durée 458,197 s compatible avec préparation 328 s + CTS 120 s, absence du marqueur final ; `unity/complete-scene.xml`. |
| T14 | EditMode élargi des contrats de transfert et des coupes | 168 réussis, 2 échecs de montage de test, 2 ignorés sur 172 ; `unity/editmode.xml`. Les 23 tests de scènes communes et les 4 tests de coupes réussissent. |
| T15 | Coupes finales et parcours Desktop MNI, premier essai isolé | Les 10 tests des coupes passent. **Parcours MNI incomplet malgré Passed** : export Desktop réussi, export restauré annulé après 9 états, `unity/desktop-mni.xml` et snapshot de métadonnées `editor-mni-incomplete-result.json`. |
| T16 | Deux variantes de libération après correction du montage de test | 2/2 réussies en EditMode, 3,477 s ; `unity/lifetime-editmode.xml`. |
| T17 | Comparaison complète Desktop/restauré MNI dans l'éditeur après protection contre l'annulation | 72 comparaisons de buffers et 12 textures de coupe exactes, états équivalents ; [preuve](../evidence/QUEST-024/editor-mni-parity.json). Diagnostic restauré achevé à 250,6 s, sauvegarde/rechargement vérifiés à 250,8 s. |
| T18 | Comparaison complète Desktop/restauré patient dans l'éditeur | 78 comparaisons de buffers et 12 textures de coupe exactes, états équivalents ; [preuve](../evidence/QUEST-024/editor-patient-parity.json). Diagnostic restauré achevé à 271,2 s, sauvegarde/rechargement vérifiés à 271,3 s. |
| T19 | Campagne finale PlayMode avec protection contre les faux succès | 4/4 réussies : scénario natif complet, Desktop MNI, Desktop patient, annulation imprévue convertie en échec ; `unity/scene-final.xml`, exit 0. Les marqueurs finaux et les exports complets ont été vérifiés séparément. |
| T20 | Construction du Player Windows corrigé QUEST-024 | Réussie, 252,821 s, aucune erreur ; `.artifacts/quest-024/Windows/DesktopWindows.build-report.json`. Les 13 fichiers du point de contrôle source correspondent toujours à leurs empreintes. |
| T21 | Trois passages MNI dans ce Player Windows | 3 × 12 états réussis, passages 2/3 exacts, régression des surfaces avec SCENE-011 exacte ; `windows-mni-fixed/`, `windows-mni-fixed-repeat.json`, `windows-mni-fixed-regression.json`. **Mémoire non validée** : 56/78/100 meshes, 376 868 870 / 493 116 982 / 609 368 486 octets Unity en fin de passage ; textures 306 et matériaux 80 stables. Correctif complémentaire vérifié en T23 à T29. Durées sous compilation Android concurrente, impropres à un benchmark. |
| T22 | Coupes après remplacement des lectures instanciantes et contrôle du nombre de meshes | 10/10 réussies, `unity/resource-sharedmesh-fixed.xml`, exit 0. Un filtre initial vide et une erreur de compilation du relevé ont été corrigés ; ils ne valent pas preuve. Stabilité du Player mesurée ensuite en T23 à T29. |
| T23 | Révision 2 Windows, trois passages MNI | 3 × 12 états réussis ; répétition et régression v2 des buffers/textures exactes. Mémoire finale 228 964 848 / 228 987 168 / 229 006 712 octets, textures 306 et matériaux 80 constants. Meshes 32/36/40 : les nouveaux objets retenus sont quatre meshes vides de 1 080 octets par passage ; les six meshes scientifiques sont remplacés et les anciens libérés. `windows-mni-revision-2/` et comparaisons `windows-mni-revision-2-{repeat,regression}.json`. |
| T24 | Révision 2 Windows, trois passages patient | 3 × 13 états réussis ; répétition v2 et régression des surfaces avec SCENE-011 exactes. Mémoire finale 178 795 080 / 178 832 888 / 178 847 152 octets, textures 308 et matériaux 80 constants, meshes 34/40/46. `windows-patient-revision-2/` et comparaisons correspondantes. |
| T25 | Révision 2 Quest, trois passages MNI | 3 × 12 états réussis ; 167 232 934 / 167 227 430 / 167 235 590 octets Unity, 21 meshes / 48 textures / 56 matériaux constants. PSS chaud 2 381 568 / 2 382 336 / 2 382 600 KiB. Répétition exacte, `quest-mni-revision-2/` et `quest-mni-revision-2-repeat.json`. |
| T26 | Révision 2, MNI Windows/Quest et coupure | Catégories, grilles, couleurs et 12 textures exactes ; écarts UV activité ≤ 3,5762786865234375 × 10⁻⁷ et alpha ≤ 1,1920928955078125 × 10⁻⁷, seuils acceptés pour cette recette. Coupure 60,292 s, PID 11507 conservé, recalcul exact, liaison rétablie ; propriétaire « Oui » pour lisibilité/gestes, puis arrêt Quest vérifié. `mni-parity-revision-2.json`, `quest-mni-revision-2-outage/`, `mni-outage-revision-2-repeat.json`. |
| T27 | Patient synthétique corrigé, Windows et publication | 3 × 13 états, 6 modalités ; répétition chaude exacte. Publication de 5 653 993 octets en 17,674 s, hash `4fe82e4884f4d4924b3062efb26b8df639614bd30274d7b68a3b0486888dca35`. `patient-final-13994-7835/`, `patient-final-delivery.json`. |
| T28 | Patient corrigé, trois passages Quest | 25,018 / 27,190 / 27,403 s, 13 états chacun. 167 128 664 / 167 134 928 / 167 140 512 octets Unity ; 21 meshes, 48 textures et 56 matériaux constants. Répétition chaude exacte. `quest-patient-final/pass-1`, `quest-patient-final-warm/pass-{1,2}`. |
| T29 | Patient corrigé, parité et coupure | 78 comparaisons : catégories, grilles, couleurs et 12 textures exactes ; activité max 3,5762786865234375 × 10⁻⁷, alpha max 1,1920928955078125 × 10⁻⁷. Coupure 60,163 s, PID 16121 conservé, redirection rétablie, recalcul exact. Propriétaire « Petient ok » et « M3 patient OK ». `patient-final-parity.json`, `quest-patient-final-outage/`, `patient-final-outage-repeat.json`. |
| M3 | Deuxième coupure, gestes observés par le propriétaire | 60,723 s, même PID, liaison restaurée, retour « Oui tout est bon » ; `manual-m3-20260914-075748/`. |

Le premier diagnostic Windows lancé dans le sandbox a expiré avant chargement.
Le Player visible puis le diagnostic patient en batchmode réussissent hors
sandbox ; ce premier essai reste un échec enregistré, sans attribution à un
défaut scientifique. Le premier lanceur Quest a expiré pendant la veille du
casque ; la demande a été consommée après réveil et son résultat récupéré
séparément. D26 (maintien éveillé sur alimentation et proximité) a ensuite été
appliquée en USB. Aucun réglage ADB Wi-Fi n'a été activé.

## Comparaison scientifique et mémoire — campagne initiale

Le comparateur distingue désormais activité, opacité, grilles et couleurs,
avec localisation des maxima. Il contrôle aussi que les tailles déclarées
correspondent aux fichiers et refuse les preuves vides ou aux IDs dupliqués.
Le verdict brut reste strict : aucune tolérance n'est ajoutée pour produire
un succès artificiel.

Sur les 72 comparaisons MNI Windows/Quest : grilles et couleurs exactes,
états/sites/masques équivalents, aucun désaccord non fini, aucune taille
incohérente. Maximum activité : **3,5762786865234375 × 10⁻⁷**, fMRI,
état `resource-2`, sommet 6148, composante x ; RMS de ce buffer
**3,1857805100623954 × 10⁻⁸**. Maximum opacité :
**1,1920928955078125 × 10⁻⁷**, anatomie, sommet 1627, composante x ; RMS
**3,880615954263899 × 10⁻⁹**. Les acceptations D30/D31 concernaient d'autres
bancs et ne sont pas automatiquement étendues ici.

Sur les premiers binaires SCENE-011, les échantillons mémoire Unity augmentaient entre passages. Le compteur par frame
peut manquer le dernier état ; le lanceur rassemble également les observations
par état sans les présenter comme un pic exact. Le relevé final Android atteint
environ 3,79 Gio de PSS et 2,36 Gio de Graphics. Une analyse des ressources
créées/libérées lors des diagnostics a conduit aux corrections et aux nouvelles
mesures T23 à T29 ; ces anciennes valeurs ne décrivent pas la révision 2.

## Validation manuelle réalisée

Fixture : `C:/HBP/Software/HiBoP/.artifacts/scene-008/fixture/scene-008.hibop`,
protocole `scene-008.prov`, visualisation **SCENE-008 six modalities**.
Appairage Windows dans **Quest** avec `127.0.0.1`, **Inspect Quest**, comparaison
de l'empreinte complète au casque, code courant, **Pair**, puis **Envoyer au Quest**.

| ID | Critère | Verdict propriétaire |
| --- | --- | --- |
| M1 | Lisibilité des six colonnes, surfaces et sites | VALIDE : « M1 OK ». |
| M2 | Translation, rotation et échelle d'une colonne indépendantes des autres et de Windows | VALIDE : « M2 OK ». |
| M3 | Gestes pendant la coupure, stabilité après relâchement et poses conservées après reconnexion | Première observation partielle ; deuxième essai validé : « Oui tout est bon ». |

Après M3, les journaux et la mémoire ont été récupérés ; arrêt ciblé de
`fr.crnl.hibop.quest`, absence de PID vérifiée. ADB et la redirection USB restent
disponibles. Aucun verdict Wi-Fi ou patient physique n'est déduit de ces retours.

## Recette des binaires corrigés

### Correction de la fixture patient visuelle

Le propriétaire a refusé la première recette patient : aucune surface visible
de face, puis confirmation qu'en tournant la caméra la surface apparaît comme
un plan. Le fichier de transformation hérité de la fixture native contient une
identité 4×4 sous extension `.trm`. Le lecteur TRM en extrait la matrice linéaire
`[[0,0,1],[0,0,0],[0,1,0]]`, de déterminant nul : un axe est supprimé. La petite
taille des deux tétraèdres natifs (1 mm) était une explication initiale incomplète.
Les succès numériques précédents ne valident donc pas le rendu patient.

`Tools/Prepare-QuestPrototypePatientFixture.py` produit une recette séparée
`scene-008-patient-visual.hibop`, avec surfaces dérivées de MNI à taille anatomique
et IRM synthétique, chargées comme ressources patient. Les coordonnées sont déjà
dans le repère voulu ; la transformation externe y est désormais vide. Les
69 104 sommets sont finis et les trois dimensions de chaque hémisphère sont
non nulles (`patient-visual-transform-fix.json`). Cette correction concerne la
fixture, sans modification des binaires. Le propriétaire a effectué les
rechargements et validations visuelles, après avoir retiré l'autorisation
générale de contrôler l'interface et de lancer/arrêter les applications.

Après rechargement, le propriétaire confirme le volume 3D mais signale un
problème de normales. Retirer le TRM incorrect avait aussi omis la transformation
de référence `MNI.trm`, dont le déterminant est négatif. Le générateur applique
maintenant cette transformation directement aux points avant la déformation
synthétique, avec le même ordre des triangles que MNI ; les deux chargeurs
exécutent ensuite les mêmes `FlipTriangles` et `ComputeNormals`. Les volumes
orientés des deux hémisphères ont le même signe que la référence MNI et le rapport
attendu 0,92 × 0,96 (`patient-visual-normal-fix.json`). Cette seconde correction
de fixture a été rechargée et validée sur Desktop par le propriétaire :
« C'est bon ». Les mesures Windows/Quest et le visuel Quest ont ensuite été
refaits sur cette géométrie corrigée (T27 à T29). Le lanceur manuel
`Tools/Run-QuestPrototypePatientValidation.cmd` ouvre la fixture et réalise trois
passages Windows dans un nouveau dossier `patient-final-*`, puis laisse HiBoP
ouvert pour l'envoi. Le propriétaire a ensuite explicitement autorisé l'agent à
lancer ce script après avoir fermé HiBoP. Le premier lancement a été bloqué par
la politique PowerShell ; le lanceur utilise maintenant `-ExecutionPolicy Bypass`
uniquement pour son processus, sans changer la politique système. Les trois
passages suivants ont réussi : `patient-final-13994-7835/pass-{1,2,3}`, chacun
avec treize états et six modalités, sur la géométrie corrigée. La fenêtre reste
ouverte pour l'envoi manuel au Quest. Cette autorisation ponctuelle ne rétablit
pas le contrôle général de l'interface.

Les binaires retenus sont sous `.artifacts/quest-024/revision-2/`, construits
depuis le point de contrôle `source-revision-2.json`. La première révision sous
`.artifacts/quest-024/Windows` et `Android` conserve le défaut de T21 et ne doit
pas servir à la recette finale. Les deux constructions utilisent Unity CLI,
les profils `DesktopWindows.asset` et `Quest.asset`,
`HBP.Dev.HBPBuilder.BuildFromCommandLine`, `-developmentBuild` et un
`-buildOutput` propre à la cible sous `revision-2`. Les journaux
`windows-build-revision-2.log` et `android-build-revision-2.log` conservent les
commandes et résultats. Unity s'exécute hors sandbox conformément au dépôt.

Pour la recette finale (`scene-008`, puis `scene-008-patient-visual`), utiliser
`Tools/Run-SceneQualification.ps1` avec :

- `-Player C:/HBP/Software/HiBoP/.artifacts/quest-024/revision-2/Windows/HiBoP.6.1.0.win64/HiBoP.exe` ;
- `-FixtureName` correspondant à la fixture ;
- `-Evidence` vers un **nouveau** dossier sous `.test-results/quest-024` ;
- `-Passes 3` pour le relevé répété ou `-KeepOpen` pour le transfert visible.

Après installation de l'APK signé et publication de cette même fixture via le
panneau Quest, exécuter `Tools/Run-QuestSceneQualification.ps1` avec
`-Serial 2G0YC5ZHB20370`, `-Evidence` vers un nouveau dossier et `-Passes 3`.
Comparer Windows/Quest avec `Tools/Compare-SceneQualification.py`, puis les
passages chauds 2/3 séparément. Le format v2 compare par défaut les textures des
coupes. L'option explicite `--surface-only` sert uniquement à une comparaison
de régression avec les anciens exports v1 ; son absence de couverture des
textures est inscrite dans le résultat.

Conserver les retours visuels du propriétaire avec la version testée. D23 prévoit
l’arrêt du seul processus Quest après collecte des preuves. Le propriétaire a
ensuite retiré le contrôle des applications : cette instruction plus récente
prime. Il a autorisé ponctuellement le lancement des Players et les mesures
avec coupure USB ; l’arrêt final lui a été laissé et reste non vérifié.

## Recette finale patient et limites de qualification

La fixture `scene-008-patient-visual.hibop` est synthétique, dérivée de MNI et
chargée via le parcours patient avec un mesh déformé et une IRM différente.
Elle ne constitue pas une qualification de données de patients réels. Sa
[provenance](../evidence/QUEST-024/prototype-patient-provenance.json) identifie
les transformations et les empreintes des fichiers utilisés.

Les trois passages Windows finaux consomment 248 715 619 / 249 881 588 /
249 922 268 octets Unity ; 313 textures et 87 matériaux restent constants.
Les meshes sont au nombre de 34/38/44 : le reliquat Desktop reste une limite
connue. Sur Quest, les trois passages conservent 21/48/56 meshes/textures/matériaux
et la mémoire Unity varie de moins de 12 Ko. Les deux relevés PSS chauds donnent
2 639 376 puis 2 617 207 KiB. Ce contrôle couvre trois recalculs, pas une
qualification d’endurance ou une mesure exhaustive de la fluidité à 72 Hz.

Le premier script de collecte s’est arrêté sur le message de progression de
`adb pull`, émis sur stderr malgré un code de sortie nul. Le calcul et les
91 fichiers transférés étaient complets ; le comparateur a validé les tailles,
empreintes, 13 états et six modalités. Le résultat d’échec du lanceur est conservé
et distingué du diagnostic récupéré dans `patient-final-first-pass-recovery.json`.
Le lanceur capture désormais les deux flux et décide selon le code de sortie
ADB ; les deux passages suivants et la coupure ont réussi avec cette correction.
Aucune modification C# ni reconstruction des Players n’a été nécessaire.

La [synthèse numérique](../evidence/QUEST-024/scientific-acceptance.json) conserve
les maxima et RMS par grandeur pour MNI et patient corrigé. Les seuils acceptés
sont 5 × 10⁻⁷ pour l’activité et 2 × 10⁻⁷ pour l’opacité, uniquement sur cette
recette et ces binaires. Le propriétaire les a acceptés explicitement le
14 septembre 2026 : « Oui, pour cette recette uniquement ». Les deux fixtures
respectent ces critères. Les comparaisons brutes restent strictes et signalent
les différences flottantes ; l’acceptation est enregistrée séparément et
n’étend pas D30/D31 à d’autres données ou binaires.

Les essais restent limités à Windows et Quest 3 via USB. Wi-Fi, Mac/Linux,
patients réels, endurance longue et finition des fonctionnalités/UI ne sont pas
qualifiés par cette campagne. L’application a été laissée au propriétaire à la
fin des essais, conformément à sa restriction de contrôle ; l’arrêt final du
processus Quest n’est pas présumé.

## Points à examiner et suite

1. [Comparateur](../../../../Tools/Compare-SceneQualification.py) : séparation des
   grandeurs, exactitude catégorielle et absence de tolérance implicite.
2. [Lanceur Quest](../../../../Tools/Run-QuestSceneQualification.ps1) : résultat
   frais, six modalités obligatoires, PID constant, restauration USB dans `finally`.
3. [Tests du comparateur](../../../../Tools/Test-SceneQualificationComparator.py) :
   rejet des preuves corrompues ou incomplètes.
4. [Diagnostic commun](../../../../Assets/Scripts/HBP/Transfer/Scene/SceneQualification.cs) :
   ressources et restauration à vérifier, notamment lors d'une annulation.

Corrections engagées après la revue indépendante : suivi explicite des meshes
possédés par les colonnes et leur scène source, libération à leur remplacement
et à la fermeture, nettoyage des trois textures par coupe, libération du plan
natif supprimé et `MaterialPropertyBlock` pour éviter de cloner les matériaux.
Les destructions des nouvelles ressources respectent EditMode et PlayMode.

La répétition T21 a révélé une rétention restante. Les accès au mesh déjà possédé
utilisent désormais `sharedMesh` pour lire et modifier les buffers, y compris
les sources des clonages explicites. L'acquisition initiale par `DisplayedObjects`
reste distincte. Le test contrôle aussi l'identité du mesh source et la stabilité
du nombre total de meshes entre cycles chauds. Le diagnostic conserve un
inventaire final (identifiant, nom, sommets et taille) pour localiser toute
rétention persistante dans le Player.

La révision 2 élimine la croissance d'environ 116 Mo par cycle de T21. Il reste
un petit nombre croissant de meshes vides sur Desktop (quatre par cycle MNI,
six sur patient). La signature est compatible avec les meshes de caret des
`InputField` uGUI ; l'attribution aux contrôles précis n'est pas démontrée.
Ce reliquat est conservé comme limite connue, sans prétendre à une absence
totale de rétention ni engager une refonte de l'UI. Les textures et matériaux
restent constants. Les relevés patient ont partiellement chevauché la construction
Android ; leurs durées ne constituent pas un benchmark isolé.

Le diagnostic restaure la géométrie avec un token distinct de celui de la
campagne annulée et vérifie les masques d'effacement restaurés. Son format v2
exporte les pixels RGBA des textures anatomiques et fonctionnelles de chaque
coupe 3D et contrôle leur association au renderer. Les aperçus GUI dépendent de
la sélection Desktop et ne font pas partie du rendu commun Quest. Le comparateur sépare ces
quantités des buffers flottants ; les preuves v1 conservent une couverture des
textures explicitement absente. Les nouveaux relevés comptent également meshes,
textures et matériaux après les destructions différées. Le lanceur Windows
accepte `-sceneEvidencePasses 3` pour répéter dans le même processus.

La première campagne de tests avait un délai NUnit de 180 s inférieur au CTS
Desktop de 240 s. La préparation standard non annulable pouvait continuer après
que Unity abandonnait le test, puis son scope réinitialisait les singletons du
test suivant. Les tests attendent désormais explicitement cette préparation
avant de démarrer le délai coopératif (4 minutes pour le scénario natif, 6 pour
les deux diagnostics Desktop/restauré), avec une garde NUnit de 15 minutes et
des durées de phase. Les cas sont relancés pour ne pas mélanger les
preuves. Aucune empreinte scientifique n'est retirée pour accélérer le chargement.

Le contrôle des exports a révélé un second défaut du harnais : la version
installée de Unity Test Framework ne teste que `Task.IsFaulted` et peut déclarer
réussi un `Task` annulé. Les premiers statuts Passed de T13/T15 ne valent donc
pas validation. Les scénarios transforment maintenant une annulation imprévue
en `AssertionException`, avec un test ciblé de cette protection et des marqueurs
finaux après les dernières assertions. Le comparateur a correctement refusé
l'export restauré incomplet de T15.

La campagne EditMode élargie a également révélé que les deux variantes de
`DensityProjectionTests.DestructionDefersHandlesUntilActualWorkerCompletion`
injectaient par réflexion un `UniTask<bool>` dans un champ `UniTask`. Le montage
attend désormais la tâche non générique. Les deux tests historiques ignorés
nécessitent une capture HBNA QUEST-017, remplacée pour cette qualification par
les fixtures de scènes communes. Le nettoyage des ressources Unity des colonnes
revient explicitement sur le thread principal après l'attente du worker natif.

Ces corrections ont été éprouvées sur les nouveaux binaires révision 2, selon
les campagnes MNI et patient corrigé détaillées ci-dessus.
Le refactor prime sur les exigences obsolètes
du prototype à un seul instant ; aucune nouvelle feature/UI ni qualification
Mac/Linux n'est engagée.
