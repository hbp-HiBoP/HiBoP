# Rapport QUEST-019 — Densité locale et comparaison Windows/Quest

## Résultat

Les entrées HBNA reçues déclenchent maintenant le calcul natif local de densité,
puis la publication des UV scientifiques et alpha sur le mesh Quest. Un clic du
stick droit relance ce calcul, y compris sans réseau. La projection précédente
reste affichée pendant le travail ; le panneau indique calcul, résultat ou erreur.

Le calcul utilise directement les wrappers Desktop `ActivityProjectionGrid`,
`DensityGenerator` et `SurfaceGenerator`, déjà partagés par QUEST-018. Aucune
densité calculée sur Windows n'est envoyée au casque, aucune réduction de grille
ni modification du code C++ n'a été ajoutée. La responsabilité de calcul et de
durée de vie reste dans le propriétaire existant `NativeProjectionInputs`.

## État et provenance

- Implémentation : **IMPLEMENTEE**. Technique : **REUSSI**.
- Parité scientifique : **ACCEPTEE**, décision **D30** du 2026-09-09, limitée au banc et aux versions ci-dessous.
- Manuel : **VALIDE** le 2026-09-09 : « M1 validée, prêt pour l’essai manuel hors ligne », puis « M2 validée ». La confirmation antérieure des contrôleurs prêts n'a pas été assimilée à ces validations.
- Branche `feature/xr-autonomous`, HEAD initial `dd1f8400dc456b0f5ddfe5489e4b86978032656f`, arbre initial propre ; modifications non commitées.
- Unity **6000.5.2f1**, Players IL2CPP Windows x64 et Android ARM64, builds de développement. Versions de packages et plugins verrouillées dans le manifeste.
- hbp_core **0.2.1**, source `ffb7686011f37a21e6c5ab4f88ad6cfcbc53ffc1`, plugins issus du verrou natif existant ; dépôts natifs inchangés.
- Dépendances : QUEST-017 pour HBNA v3 et les entrées natives, QUEST-018 pour la préparation commune Desktop. La preuve QUEST-018 reste une référence Windows, pas une preuve Android réutilisée.
- [Manifeste des preuves](../evidence/QUEST-019/manifest.json), [comparaison complète](../evidence/QUEST-019/parity-final.json), [résumé des mesures](../evidence/QUEST-019/summary.json).

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Changement et invariant |
| --- | --- | --- |
| 1 | [NativeProjectionInputs.ComputeDensityAsync / Dispose](../../../../Assets/Scripts/HBP/Transfer/Projection/NativeProjectionInputs.cs) | Réserve les entrées avant `Task.Run`, appelle les mêmes wrappers que Desktop et libère les générateurs avant de relâcher la réservation. Retrait différé des entrées, sans attente bloquante sur le thread Unity. |
| 2 | [QuestAnatomyView.ComputeAndPublishAsync](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) et [QuestAnatomyInput](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyInput.cs) | Publication Unity après `await`, garde de génération et d'identité pour succès/erreur/finalisation. Un résultat périmé ne touche ni le nouveau mesh ni son état. Clic stick droit ; recalculs simultanés regroupés. |
| 3 | [Shader Quest](../../../../Assets/Shaders/Quest/AnatomySurfaceCommon.hlsl) et [matériau sérialisé](../../../../Assets/Prefabs/Quest/AnatomyOpaque.mat) | UV3 activité, UV2 alpha, sentinelle alpha et texture alpha Desktop ; relief scientifique partagé. Aucun GameObject construit pour compenser une référence de prefab. |
| 4 | [DesktopAnatomyCapture.Capture](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs) | Refuse explicitement `SmoothActivityBoundaries=false`, non représentable par HBNA v3. Empêche une divergence scientifique silencieuse. |
| 5 | [DensityBenchmark](../../../../Assets/Scripts/HBP/Transfer/Projection/DensityBenchmark.cs), [comparateur](../../../../Tools/Compare-QuestDensity.py) et [diagnostic de réception](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestDensityDiagnostic.cs) | Buffers complets et écarts localisés ; le comparateur ne choisit pas de tolérance. Réception TLS réelle, puis recalcul local durant coupure radio observée. |

Le décodage, le staging et la reconstruction initiale des entrées restent sur le
chemin Unity existant. Seuls préparation de grille, générateurs, calcul et copies
de résultats sont déportés. Le mesh reste lisible en CPU lorsqu'il possède une
projection. Les handles sont détenus jusqu'à la fin du travail natif, même si la
vue est remplacée ou détruite ; aucune annulation forcée du C++ n'est revendiquée.
Les propriétés natives exposées restent des références empruntées, sans droit
de mutation concurrente externe.

## Vérifications effectuées

Toutes les commandes Unity ont été lancées hors sandbox via `Start-Process -Wait
-PassThru -WindowStyle Hidden`, éditeur utilisateur fermé. Logs avant/après examinés.

| ID | Scénario / commande | Résultat et preuve |
| --- | --- | --- |
| T1 | CLI EditMode, trois assemblies anatomie/transfert ; arguments exacts dans [editmode-command.json](../evidence/QUEST-019/editmode-command.json). | **132/132**, code 0 : [XML](../evidence/QUEST-019/editmode.xml). Calcul identique au chemin Desktop, réitération, retraite pendant travail, libération après faute, remplacement/fermeture de prefab et rejet du paramètre non transporté. |
| T2 | CLI PlayMode `HBP.Quest.PlayModeTests`, [arguments](../evidence/QUEST-019/playmode-command.json). | **42 réussis, 3 ignorés** car fixture anatomique historique absente du premier lancement, zéro échec : [XML](../evidence/QUEST-019/playmode.xml). Les trois ont ensuite été exécutés avec la vraie fixture, **3/3**, code 0 : [arguments complémentaires](../evidence/QUEST-019/playmode-mni-command.json), [XML](../evidence/QUEST-019/playmode-mni.xml). Soit **45 tests distincts réussis**, dont manipulation MNI 60 s, réception TLS avec densité et recalcul déconnecté. |
| T3 | `Tools/format-code.cmd` puis `Tools/Build-QuestConnectionPlayers.ps1 -Target Android -EvidenceId quest-019` et `-Target Windows -EvidenceId quest-019`. | Formatage et deux builds finaux **réussis**, code 0. Logs locaux `.test-results/quest-019/android-build.log` et `windows-build.log`, hashes au manifeste. APK **163 030 197 octets**, huit bibliothèques ARM64, contenu contrôlé par `Test-QuestApk.ps1`. |
| T4 | Windows final `-densityBenchmark <fixture HBNA ci-dessous> <sortie>`, puis `Tools/Run-QuestDensityBenchmark.ps1 -Serial 192.168.1.18:5555 -Fixture <même fixture>`. | **18 exécutions par plateforme** ; APK installé vérifié par SHA-256 identique au fichier local. Création/calcul/libération terminés, aucun résidu de volume privé ni erreur de cleanup. Processus de banc arrêtés et arrêt Quest vérifié. |
| T5 | `python Tools/Compare-QuestDensity.py .test-results/quest-019/benchmark-windows-final .test-results/quest-019/benchmark-20260909-140740/Android .test-results/quest-019/parity-final.json` | Code **0** : identités, formes, valeurs finies, catégories, sentinelles et cas analytiques vérifiés. Le code 0 seul n'est pas une acceptation numérique ; **D30** accepte les écarts détaillés ci-dessous. Contrôles négatifs NaN et catégorie altérée détectés, différence numérique rapportée sans seuil implicite. |
| T6 | `Tools/Run-QuestDensityDelivery.ps1 -Serial 192.168.1.18:5555 -HostAddress 192.168.1.14` | Code **0** : capture du vrai Player Windows, réception TLS par `QuestAnatomySession`, densité initiale publiée ; **60,01 s** Wi-Fi désactivé, **3 recalculs exactement identiques**, UV relus sur le mesh identiques, même PID **24710**, réseau restauré. [Résultat](../evidence/QUEST-019/delivery/device-result.json), [radio](../evidence/QUEST-019/delivery/radio.log). Windows arrêté ; Quest laissé ouvert pour validation manuelle. |

Les dernières modifications après les tests Unity portent uniquement sur le garde
de compilation Desktop du diagnostic de livraison et les outils de mesure ; les
deux Players finaux puis les bancs physiques ont été reconstruits/réexécutés.

## Parité numérique et critères acceptés

Fixture du banc :
`C:/HBP/Software/HiBoP/.artifacts/quest-017/captures/20260909-111610-0c6c7cfd-cb1a-4477-b6d4-18243f162f1a/anatomy.hbna`,
SHA-256 `2cd71c1236a6e43793b036bfe1500aceca744d13a405b75165d72350df775a26`.
Neuf variantes, deux exécutions chacune : MNI, rayon 25 mm, voisin le plus proche,
influence linéaire, influence constante, aucun contact, un contact, tous masqués,
surface contenant un sommet hors volume. Le cas MNI utilise **69 104 sommets**,
**8 contacts**, **353 600 points de grille** (65 × 80 × 68), résolution demandée 80.
La couverture MNI est complète : 69 104 valides, zéro invalide.

Les hashes des HBNA de chaque variante sont égaux sur les deux plateformes.
Les fichiers binaires contiennent tous les float32 little-endian : maximum,
2 UV activité par sommet, 2 UV alpha, 3 coordonnées par point de grille,
3 coordonnées par sommet. Longueur contrôlée : `1 + 7N + 3G` floats.
La grille comparée est sa géométrie complète ; ce banc n'exporte pas séparément
le champ scalaire interne de densité à chaque voxel.

| Grandeur | Plus grand écart absolu | RMS du cas portant ce maximum | Critère D30 |
| --- | ---: | ---: | --- |
| Points de grille et maximum de densité | 0 | 0 | Égalité stricte |
| Couverture, masques, positions, catégories UV et tuples sentinelles | 0 | 0 | Égalité stricte |
| UV activité | 2,980232238769531 × 10⁻⁷ | 1,3842878707934336 × 10⁻⁸ | ≤ 5 × 10⁻⁷ absolu |
| UV alpha | 1,1920928955078125 × 10⁻⁷ | 3,931518142142266 × 10⁻⁹ | ≤ 2 × 10⁻⁷ absolu |

Le RMS est calculé sur les deux composantes de tous les sommets, y compris les
sentinelles ; le JSON donne chaque cas, pas uniquement le pire. Les maxima UV
surviennent dans `constant-0` (également reproduits au second passage) :

- Activité, sommet **4497**, composante x, position transport en mm
  **(3,1554565 ; 10,0846481 ; 41,9407730)** : Windows **0,8473547101020813**,
  Android **0,8473544120788574** ; 4 702 composantes diffèrent dans ce cas.
- Alpha, sommet **1627**, composante x, position
  **(19,8907700 ; 22,9814987 ; 46,2902565)** : Windows **0,8135561943054199**,
  Android **0,8135563135147095** ; 2 966 composantes diffèrent dans ce cas.

Ces différences valent cinq et deux ULP aux valeurs citées. Le code natif partagé
effectue interpolation et combinaison pondérée en float32 dans
`hbp_core/src/generators/surface_generator.cpp:90`. Avec entrées, grilles,
catégories et maxima exacts, les écarts sont compatibles avec des arrondis selon
architecture/compilation ; aucune attribution certaine à une instruction FMA
n'a été démontrée. Les marges soumises encadrent ces mesures (environ 1,68 fois
les maxima observés), sans changement d'algorithme. Le propriétaire les a
**acceptées explicitement pour ce banc et ces versions**, pas universellement.

Sans sites et avec tous les sites masqués : densité maximale zéro, UV activité
`(0.5,1)` et alpha `(0.01,1)` exacts. Avec un site, maximum positif inférieur à 1.
Le cas frontière utilise trois points autour de l'origine et `(1000,0,0)` mm hors
volume ; couverture partielle et sentinelles exactes. Il ne constitue pas une
campagne exhaustive au voisinage de chaque frontière ou epsilon natif.

## Temps, copies et mémoire

| MNI, millisecondes | Windows premier passage | Windows second | Quest premier | Quest second |
| --- | ---: | ---: | ---: | ---: |
| Préparation grille et générateurs | 5,967 | 5,493 | 14,667 | 10,043 |
| Calcul densité | 5,548 | 5,515 | 12,193 | 9,183 |
| Projection et copies de résultats | 9,146 | 8,133 | 22,359 | 19,479 |

Chaque passage crée de nouveaux générateurs et bindings : le second est un
passage à processus chaud, pas un cache de grille conservé. L'export diagnostique
des points de grille est hors de ces trois mesures. Après réception réelle, les
trois calculs hors réseau prennent **11,893 / 9,652 / 9,373 ms** ; la publication
Unity des UV prend **2,243 / 1,704 / 0,969 ms**. Ce dernier temps mesure la
soumission CPU, pas la fin du transfert GPU ni la latence bouton-à-photon.

Pour 69 104 sommets : **1 105 664 octets** de deux tableaux UV natif → managé par
résultat, puis autant de conversion des structs par les wrappers existants.
L'affectation des UV au mesh et les copies internes Unity/GPU s'ajoutent ; leur
nombre exact n'est pas mesuré. Les tableaux de résultats et le mesh CPU restent
en mémoire pour recalcul/rendu. Les snapshots, staging, NIfTI et tableaux du banc
ajoutent leurs allocations. `BufferBytes` historique décrit l'anatomie et ne
doit pas être pris pour le total incluant la densité.

Réception + trois cycles : mémoire Unity allouée **101 659 171 → 109 311 387 →
111 525 307 octets**. Le diagnostic retient les premiers UV pour comparaison
(1 105 664 octets) ; des buffers récents peuvent attendre le GC. Sur les onze
fenêtres suivantes (~35–85 s), Unity reste entre **111 451 419 et 111 501 147
octets**, mémoire managée entre **39 817 216 et 48 361 472**, avec collections
observées. PSS Android **556 843 → 547 614 KiB**, RSS **693 312 → 685 204 KiB**.
Pas de croissance monotone observée après stabilisation ni de crash ; ces
mesures agrégées ne prouvent pas l'absence de fuite à longue durée et n'isolent
pas le pic exact des allocations natives.

Sur ces onze fenêtres au repos, intervalle moyen de frame **13,888879–13,888897
ms** (~72 Hz), maximum **13,891086 ms**. Cette cadence observée sur un casque
immobile ne qualifie ni tous les mouvements ni le confort utilisateur. Quest 3,
batterie **66 %**, non alimenté au relevé final, statut thermique Android **0**.

## Validation manuelle demandée

APK final installé :
`C:/HBP/Software/HiBoP/.artifacts/quest-019/Android/HiBoP.Quest.apk`, SHA-256
`84da8a239919d9aded21b2b3fc842045c47b697fb18e27f9e3552b4626db95d4`.
La vue utilisée pour la validation provenait d'une nouvelle capture réelle du Player
final, fichier `.test-results/quest-019/delivery-20260909-140855/received-source.hbna`,
hash de contenu reçu `3690428569930978e96f3b5d25c3a826bc3bd51dced9bd15fb1117c826f0f3c8`.
Ce hash de contenu n'est pas le SHA-256 du fichier HBNA entier.

Pour refaire l'envoi par l'interface, lancer ce Player avec la fixture fournie :

```powershell
& 'C:/HBP/Software/HiBoP/.artifacts/quest-019/Windows/HiBoP.6.1.0.win64/HiBoP.exe' -pf 'C:/HBP/Software/HiBoP/.artifacts/quest-018/fixture/quest-mni-contacts.hibop' -v 'MNI Contacts' -screen-fullscreen 0
```

Dans la fenêtre **Quest** de Desktop, adresse `192.168.1.18`, **Inspect Quest**,
comparer les groupes avec le panneau casque (**B** l'affiche), cocher
**All groups match the fingerprint displayed in my Quest**, saisir le code
affiché, **Pair**, puis **Envoyer au Quest** sur la colonne **MNI Contacts**.
Le code/empreinte sont ceux affichés au moment du test, pas ceux d'une ancienne
capture. Le chemin complet du Player, ses DLL et données sont hashés au manifeste.

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M1 | Sur la vue reçue, **B** masque le panneau ; une gâchette d'index déplace/tourne, les deux agrandissent, **A** masque/réaffiche le cerveau, **X** recentre. Tourner vers la région des contacts. | Projection colorée visible, contacts et groupe manipulables ; masquer la surface conserve les contacts. L'envoi est déjà effectué automatiquement via le vrai transport ; l'envoi par bouton peut être rejoué avec la recette ci-dessus. | **VALIDE**, retour propriétaire « M1 validée, prêt pour l’essai manuel hors ligne ». Pas de revendication d'un nouvel envoi manuel par bouton. |
| M2 | Après réception, lancer la commande de coupure ci-dessous ; dans les 60 secondes, cliquer le **stick droit**, attendre **Density ready**, manipuler puis masquer/réafficher avec **A**. Examiner le tableau numérique ci-dessus. | Résultat affichable sans réseau, même maximum et aucune perte du groupe. | **VALIDE**, retour « M2 validée », après coupure manuelle mesurée **60,02 s** ; trois recalculs automatiques et revue numérique D30 également réussis. |

```powershell
Set-Location 'C:/HBP/Software/HiBoP'
.\Tools\Start-QuestAnatomyOutage.ps1 -Serial '192.168.1.18:5555'
```

Ce script existant vérifie un processus prêt, coupe la radio après deux secondes
et programme sa restauration autonome après 60 secondes ; il écrit ses preuves
sous `.test-results/quest-012/outage-*` (nom historique). Après environ 75 s,
reprendre l'ADB à cette même adresse et récupérer le chemin de log indiqué par
le script ; vérifier `COMPLETE`, la reconnexion et le même PID. L'agent peut
effectuer cette commande quand le propriétaire confirme être prêt. Une simple
commande envoyée n'est pas un succès. **Ne pas arrêter HiBoP pendant la validation** ;
après le retour utilisateur, collecter les preuves puis arrêter uniquement
`fr.crnl.hibop.quest` et vérifier l'absence de PID selon D23.

Exécution manuelle réalisée : `.test-results/quest-012/outage-20260909-141602-203`,
radio désactivée de l'uptime **22730,58 à 22790,60 s** ; `COMPLETE`, réseau
restauré, PID **24710** conservé. Après les retours M1/M2, HiBoP a été arrêté à
**14:18:08 UTC** : `force-stop` code 0, `pidof` code 1 et aucun PID. L'ADB
Wi-Fi reste connecté. [Preuves manuelles](../evidence/QUEST-019/manual-validation.json),
[arrêt vérifié](../evidence/QUEST-019/quest-stop.json).

## Décisions, limites et suite

- **D30** accepte la parité du banc ; aucune tolérance n'est enfouie dans le pipeline ni appliquée automatiquement à d'autres versions. L'évaluation de ces critères est archivée séparément du comparateur brut.
- HBNA v3 ne transporte pas `SmoothActivityBoundaries` : la valeur false est rejetée explicitement. Le chemin true qualifié reste celui par défaut. La palette Quest est le Matlab par défaut ; le choix de palette Desktop n'est pas transmis. Aucune parité pixel de captures écran n'est revendiquée.
- L'image ADB montre anatomie, contacts et état **Density ready / max 1,3416**. L'angle initial et le panneau masquent la région colorée ; cette image seule ne suffit pas à valider la projection visuelle. Le retour M1 ultérieur du propriétaire confirme la projection et les gestes.
- Le log Android contient `ClassNotFoundException: com.google.android.play.core.assetpacks.AssetPackManager` au démarrage, déjà présent dans la preuve QUEST-017. Les diagnostics terminent ensuite avec succès. Le Player Windows signale une saturation temporaire de son Graphics Ring Buffer au chargement. Aucun de ces messages préexistants n'est transformé en preuve d'absence d'erreur globale ; aucune correction hors périmètre n'a été faite.
- Premier lancement du banc interrompu par l'écran système « contrôleurs requis » ; le propriétaire a réveillé les contrôleurs, puis les exécutions finales ont réussi.
- L'auto-review a refusé de réappliquer les réglages persistants de maintien éveillé/proximité D26, jugés hors périmètre. Aucun contournement : la connexion ADB existante a suffi ; les réglages n'ont pas été modifiés. La coupure temporaire nécessaire au test a été restaurée.
- Les changements de ressources/configuration produits automatiquement par les builds Unity ont été sauvegardés dans `.test-results/quest-019/unity-generated.patch`, puis restaurés à leur état initial ; les sources fonctionnelles et preuves restent dans le diff.
- Pas de commit, push, CI distante ni travail QUEST-020 engagé. Implémentation, qualification bornée et validations M1/M2 terminées ; J4 dispose de la démonstration de calcul local et de la parité acceptée selon D30.
