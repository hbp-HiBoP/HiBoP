# SCENE-008 — qualification intégrée du Lot C

Campagne du 10 septembre 2026 sur `feature/xr-autonomous`, base `753d3ce7219d66ede83c4fe281b5640b5b71329e`.
Les modifications du Lot C restent locales, sans commit ni changement de branche.

## Résultat et périmètre

**Pause en fin de journée. Les défauts de démarrage Android et de graphiques
Desktop sont corrigés ; les deux nouveaux builds réussissent. Le nouvel APK
n'est pas encore installé/testé. La recette de transfert/rendu reste à valider.**

Reprise sur un autre ordinateur : [prompt de redémarrage](../RESTART-PROMPT.md).
Les preuves les plus récentes sont dans `../evidence/final/pause-2026-09-10/` :
quatre tests GraphZone réussis, diagnostic du Windows corrigé réussi et identités
des nouveaux builds. Le manifeste et les tableaux ci-dessous décrivent la
campagne antérieure à ces dernières corrections ; leurs empreintes et mesures
ne doivent pas être attribuées aux nouveaux Players. L'APK corrigé mesure
2 316 908 952 octets. Le dernier formatage a réussi sur 31 fichiers C#.
Le Quest était éteint au début de la campagne. Les résultats du prototype ne
valident pas automatiquement les nouvelles classes communes.

Le Lot C stabilise les parcours des lots A/B et prépare une campagne unique
pour les besoins toujours pertinents de QUEST-001 à QUEST-023. Les contrats
HBNA et le rendu spécialisé du prototype sont des références historiques ; le
parcours livré utilise la visualisation complète et les classes scientifiques
communes. QUEST-024 conserve son propre statut administratif.

## Architecture et corrections

Le Desktop charge les projets avec les loaders existants. `Base3DScene` et ses
six spécialisations de `Column3D` possèdent les données, générateurs et buffers.
`DesktopScenePresentation` possède les vues/caméras Desktop. Sur Quest,
`QuestAnatomyView` prépare un candidat complet avec `SceneRestoration`, puis
publie ses `QuestColumnPresentation` en une opération ; les poses restent des
transformations de présentation indépendantes des coordonnées scientifiques.

Défauts corrigés pendant la campagne :

- Les surfaces simplifiées sans UV/normales/couleurs ne transmettent plus des
  tableaux vides aux setters natifs qui exigent un élément par sommet.
- La restauration résout la géométrie avant d'appliquer les masques d'effacement.
  Elle évite une surface non initialisée et la remise à zéro ultérieure des masques.
- L'état de boucle est réappliqué après préparation des générateurs. C'est le
  défaut confirmé par la revue indépendante bornée des risques de durée de vie.
- La fermeture tolère les colonnes dynamiques et managers partiellement initialisés.
- La référence au prefab de vue/caméra appartient à la présentation Desktop ;
  les six prefabs communs ne dépendent plus de ce prefab Desktop.
- Le type d'une caméra peut être configuré sur une vue inactive avant son `Awake`.
- Le diagnostic Desktop à exécution unique peut quitter sans dialogue utilisateur,
  comme les diagnostics antérieurs. Son lanceur charge le protocole synthétique
  avant le projet ; la première tentative dans l'ordre inverse a expiré.
- Les références `.gz` sont empaquetées sous `.gz.bytes`, puis installées sous
  leur nom scientifique d'origine. Le premier build Android a révélé que le
  merger d'assets Android les décompressait automatiquement : environ 78 Gio
  d'intermédiaires, saturation de C: et échec de `compressDebugAssets`. Les seules
  copies intermédiaires ont été supprimées. La correction protège les octets et
  les SHA-256 d'origine ; elle ne retire aucun atlas du Data commun.
- À la demande du propriétaire, le build Android référence directement
  `Assets/Data` via `BuildPlayerContext.AddAdditionalPathToStreamingAssets`.
  Aucun dossier `Assets/StreamingAssets` ni copie d'atlas n'est créé dans les
  assets du projet. Seul le petit manifeste généré réside dans
  `Library/HBP/QuestStandardData` ; Unity gère les copies nécessaires dans ses
  sorties de compilation. Le renommage `.gz.bytes` reste limité au paquet.

Les tests de prefabs/présentation suivent cette séparation. Les inventaires de
matériaux, imports natifs et niveaux de qualité ont été actualisés selon les
ressources réellement livrées, sans modifier les conventions numériques. Les
tests d'échec d'aperçu IRM utilisent une IRM valide sans tissu extractible : un
fichier NIfTI corrompu échoue auparavant lors du chargement.

## Fixtures et diagnostic réutilisable

`Tools/Prepare-SceneQualificationFixture.py` construit deux projets synthétiques :

- `.artifacts/scene-008/fixture/scene-008.hibop` : deux patients, six modalités,
  signaux BrainVision synthétiques en uV, réponses CCEP, valeurs statiques,
  fMRI temporelle, MEG canaux et volumes, cerveau MNI complet.
- `.artifacts/scene-008/fixture/scene-008-patient.hibop` : les mêmes modalités
  pour un patient, avec mesh et IRM de la fixture native.

Les petits volumes fonctionnels natifs conservent leurs échantillons. Leur
affine synthétique couvre le champ MNI, afin que la projection sur le cortex
ne teste pas seulement un volume situé hors du cerveau. Ce sont des fixtures
techniques, sans données acquises ni interprétation clinique.

`SceneQualification.RunAsync` agit sur la scène publiée : début/milieu/fin des
timelines, recalcul, coupe et sélections propres aux modalités. Il exporte les
UV scientifiques, points de grille, couleurs, masques, états et coordonnées des
sites, ainsi que les temps et la mémoire Unity échantillonnée chaque frame.
La mémoire rapportée n'est pas un pic RSS/PSS du processus.
Il exerce aussi le changement de mesh et, lorsque disponible, d'IRM, puis revient
à la géométrie d'origine en conservant les masques d'effacement.

Le Desktop l'active avec `Tools/Run-SceneQualification.ps1`. Le build de
développement Quest surveille le fichier `scene008-qualify` dans son répertoire
persistant ; l'agent le crée via ADB après une réception réelle, puis récupère
`scene-008/<horodatage>/result.json` et ses buffers. Aucune interface scientifique
Quest supplémentaire n'est introduite.

`Tools/Compare-SceneQualification.py` vérifie les empreintes et compare les
états/mesures. Il rapporte maximum et RMS des écarts flottants. Aucune tolérance
nouvelle n'est appliquée ; les tolérances historiques D30/D31 ne sont pas
transposées automatiquement à ces nouvelles fixtures.
L'apparence de chaque contact visible est comparée. Les différences de matériau
conservé en cache par deux contacts tous deux désactivés sont consignées à part
(`inactiveMaterialHistoryDifferences`) : elles dépendent de l'historique d'affichage
Desktop et ne changent ni la visibilité ni les données scientifiques.

## Couverture et résultats

Les commandes, sorties et identités utiles sont référencées dans
`evidence/final/manifest.json`.

| Vérification | Résultat et preuve locale |
| --- | --- |
| Campagne EditMode intégrée | 738 cas dans la passe principale ; les quatre échecs restants ont été corrigés et rejoués. État consolidé : 736 réussis, 2 ignorés. `.test-results/scene-008/editmode-2.xml`, `editmode-final.xml`, `prototype-reference.xml`. |
| Tests existants Module3D et Toolbar | 61 réussis dans `.test-results/scene-008/playmode-8.xml` ; le scénario de fixture incomplet de cette passe est remplacé par les cas ci-dessous. |
| Restauration native sans Desktop, six modalités, essais invalides, poses indépendantes, capture et remplacement invalide/annulé | Réussi dans `.test-results/scene-008/scene-integration.xml` ; les deux anciennes erreurs de fixtures Desktop de ce fichier sont résolues dans les passes suivantes. |
| Ouverture/sauvegarde/rechargement Desktop, restauration MNI et patient, diagnostic final mesh/IRM | 2/2 réussis, 170,5 s, `.test-results/scene-008/geometry-final.xml`. |
| Comparaisons Desktop/restauration dans l'éditeur | 72 comparaisons de colonnes pour le projet MNI et 78 pour le patient : buffers bit à bit identiques et états/apparences visibles équivalents, sans tolérance. `.artifacts/scene-008/editor-parity-scene-008.json` et `editor-parity-scene-008-patient.json`. |
| Contrats de transfert après correction de l'empaquetage Android | 17/17 réussis, 4,3 s, dont trois nouveaux cas de chemins compressés ; `.test-results/scene-008/reference-packaging.xml`. |
| APK Android IL2CPP | Build réussi ; `fr.crnl.hibop.quest` 6.1.0, ARM64 uniquement, 2 271 289 556 octets. Les 49 références embarquées correspondent bit à bit au Data Desktop et au manifeste SHA-256 ; `libhbp_core.so` correspond au plugin source. Installation physique non exécutée. |
| Player Windows IL2CPP final, projet MNI | Réussi, diagnostic 8,79 s, maximum mémoire Unity échantillonné 316,8 Mio. `.artifacts/scene-008/windows-six-modalities/result.json`. |
| Même Player, projet patient | Réussi, diagnostic 10,69 s, maximum mémoire Unity échantillonné 278,9 Mio. `.artifacts/scene-008/windows-patient/result.json`. |
| Inclusion directe de Data dans Android | Nouveau build réussi, 49 empreintes identiques ; absence de `Assets/StreamingAssets` après le build. Seul le manifeste de 5 246 octets est généré sous `Library/HBP/QuestStandardData`. |
| Formatage et espaces | `Tools/format-code.cmd` réussi sur 28 fichiers C# ; `git diff --check` sans erreur. |

Les deux cas EditMode ignorés sont le garde-fou spécifique au profil Android
(exécuté dans la première passe Android) et l'ancien export HBNA de QUEST-006,
non fourni à cette campagne. Les tests paramétrés d'inférence de tags ont trois
occurrences partageant un nom NUnit ; le fichier consolidé par nom en conserve
735 noms distincts, sans échec résiduel, tandis que la passe principale compte
bien 738 cas. Aucun test scientifique n'a été supprimé pour rendre les résultats verts.

| Besoin historique | Vérification avec le nouveau système | État physique Quest |
| --- | --- | --- |
| QUEST-001, 002, 002-A | Ouverture Desktop, entrées, projets, sauvegarde/rechargement | Non exécuté |
| QUEST-003, 004 | Profils Windows/Quest, dépendances, XR, passthrough | Non exécuté |
| QUEST-005 à 011 | Capture complète, appairage/contexte global, livraison, restauration et remplacement | Non exécuté |
| QUEST-012 | Manipulation et poses indépendantes des colonnes | Non exécuté |
| QUEST-013 à 016 | Sites, repères, états, filtrage et visibilité | Non exécuté |
| QUEST-017 à 019 | Calcul natif autonome, grille, densité et recalcul | Non exécuté |
| QUEST-020 à 023 | iEEG complète, timelines/essais, projection et aspect des sites | Non exécuté |
| Périmètre complet SCENE | CCEP, statique, fMRI, MEG, ressources patient, coupes, multicolonne | Non exécuté |

## Recette manuelle unique

Après vérification des binaires, l'agent prépare le projet Desktop et installe
l'APK `.artifacts/scene-008/Android/HiBoP.Quest.apk` sur le Quest identifié.
Le Player Windows est `.artifacts/scene-008/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
Le manifeste de build atteste la présence et les empreintes des binaires.

1. Dans le Desktop, ouvrir le projet synthétique et vérifier que les six colonnes
   sont affichées. Appairer le Quest et envoyer la visualisation complète.
2. Dans le casque, vérifier le passthrough, le placement des colonnes, les surfaces
   et contacts, les contrastes et la lisibilité. Comparer le résultat initial au
   Desktop, en tenant compte des données et paramètres propres à chaque colonne.
3. Manipuler une colonne à la fois, puis une autre : translation, rotation, échelle,
   relâchement et reprise. Vérifier l'indépendance et la stabilité des autres colonnes.
4. L'agent lance le diagnostic de timelines/ressources/coupes et récupère les preuves.
   Le propriétaire n'a pas à réaliser des opérations sans contrôles Quest.
5. Après déconnexion du Desktop, continuer l'exploration/manipulation. L'agent
   relance le diagnostic pour vérifier les calculs autonomes hors connexion.
6. L'agent exerce le remplacement de scène et le projet patient, puis rassemble
   les résultats. Le propriétaire signale les défauts visuels et confirme la fin.

L'application Quest reste ouverte pendant la recette. Après confirmation de fin,
l'agent récupère les preuves et arrête HiBoP sur le casque identifié sans couper ADB.

## Limites et suite

Le premier essai physique (`.test-results/scene-008/device`) a installé l'APK sur
le Quest 3 USB `2G0YC5ZHB20370`. OpenXR/Vulkan atteint `COMPOSITION READY` et les
manettes sont suivies après validation du dialogue système par le propriétaire.
L'initialisation des préférences échoue cependant en créant des chemins de projets
Desktop sur Android (`IOException: Read-only file system`). Le Desktop visible
révèle aussi un pool de graphiques trop petit avec MEG et une recherche de réponses
CCEP chez un patient autre que celui de la stimulation. Ces défauts ont été corrigés
et les Players recompilés ; la confirmation Android reste à faire.
Le succès des diagnostics scientifiques précédents
ne couvrait pas ces erreurs d'interface/démarrage.

Le rendu stéréoscopique, les contrôleurs, l'exploration déconnectée, la réception
sur appareil, les temps/PSS Android et le retour manuel restent à mesurer sur
les nouveaux binaires. Les builds seuls ne qualifient pas ces comportements.
Les autres plateformes restent hors campagne. Les preuves finales sont
réutilisables pour QUEST-024 sans répéter artificiellement la même recette.
