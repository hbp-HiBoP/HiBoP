# Décisions et questions ouvertes — HiBoP Quest autonome

Date : 2026-09-07. Statut : cadrage et fiches d'exécution ; aucune implémentation
produit lancée. Une demande explicite d'exécuter une tâche autorisera son périmètre.

Ce journal consigne les réponses du propriétaire du produit. Ces réponses priment
sur les propositions initiales de `ANALYSIS-PROMPT.md`. Les documents de
`Docs/dev/xr/` restent des sources de code et de preuves, sans autorité sur les
décisions du nouveau produit.

## Décisions validées

| ID | Décision | Conséquence pour le cadrage |
| --- | --- | --- |
| D01 | Repartir de `origin/develop`. La branche `feature/xr-autonomous` a depuis été créée par le propriétaire depuis develop et vérifiée à `8b868a5b8`. | Utiliser cette branche existante ; aucun merge global du chantier XR. |
| D02 | Un projet Unity, une base de code, des comportements métier et scientifiques communs. | Les différences de plateforme se placent aux frontières observées. |
| D03 | Caméras, vues et manipulations spatiales Desktop et Quest sont indépendantes. | Ne pas imposer un comportement de caméra commun, ni synchroniser les transforms de présentation. |
| D04 | Translation, rotation et changement d'échelle Quest n'affectent pas la vue Desktop, et réciproquement. | Le journal de commandes et le rejeu de ces gestes proposés dans le prompt initial sortent du prototype. |
| D05 | L'échelle Quest s'applique à toute la visualisation, sites compris. | Préserver les relations spatiales entre les éléments lors de l'agrandissement. |
| D06 | Quest 3 avec passthrough ; contrôleurs suffisants au départ. | Le suivi des mains est une extension souhaitée, sans être un critère d'acceptation initial. |
| D07 | HiBoP_HoloLens constitue la référence fonctionnelle des interactions. | Étudier ses gestes et prefabs ; ne pas reproduire sa copie divergente de HiBoP. |
| D08 | Première plateforme Desktop : Windows. | Valider le transfert et l'exploration sur Windows + Quest 3. |
| D09 | Progression : cerveau anatomique complet, électrodes, projection de densité, projection iEEG. | Décomposer le prototype en preuves successives. |
| D10 | Après ces étapes, tester Mac + Quest 3, puis envisager Linux, avant les fonctionnalités ultérieures. | La qualification multiplateforme précède l'élargissement fonctionnel suivant. |
| D11 | Départ depuis une visualisation ouverte via « Envoyer au Quest », après appairage sur le réseau local. | La démonstration passe par le pipeline réel de transfert. |
| D12 | Une minute de déconnexion avec manipulation locale continue et reconnexion suffit comme démonstration initiale. | Vérifier la conservation de la visualisation et de sa présentation locale ; aucun rejeu de gestes spatiaux n'est nécessaire. |
| D13 | Les coupes, la timeline pilotée depuis Desktop et les éditions concurrentes sont ultérieures. | Ne pas construire une réconciliation générique pour le premier cerveau manipulable ; D16 précise l'iEEG initiale. |
| D14 | Le MNI présent dans le dépôt peut servir de base de fixture. | Utiliser l'anatomie de référence ; compléter les jalons électrodes/iEEG par des données synthétiques ou nettoyées. |
| D15 | Les projections de densité et iEEG sont calculées localement sur Quest avec le même `hbp_core` que Desktop. | Qualifier le runtime natif Android avant le jalon densité ; transférer les entrées préparées, pas seulement les résultats projetés. |
| D16 | Le premier iEEG affiche un instant choisi sur Desktop. | Aucune lecture ou navigation temporelle Quest exigée à ce stade. |
| D17 | Le Mac de validation est Apple Silicon. | Cible Desktop et plugins natifs ARM64 ; vérifier les versions macOS dans les fichiers de build. |
| D18 | Préférer un transport embarqué dans Unity si cela convient ; conserver l'auxiliaire seulement si un avantage concret de performance, taille ou faisabilité le justifie. | Qualifier l'embarqué avant de choisir le transport ; P06 est une solution de repli, pas une décision héritée. |
| D19 | Pour les électrodes, un bouton afficher/masquer le cerveau suffit. | La transparence passthrough n'est pas requise pour ce jalon. |
| D20 | Le propriétaire demande des tâches exécutables depuis leur fichier dans une nouvelle conversation, avec rapport, points de review, validation manuelle et questions de décision. | Contrat TASK-WORKFLOW.md, IDs QUEST, fiches par tâche et jalon, statut durable distinct des preuves. |
| D21 | Le propriétaire a supprimé le dossier XR résiduel et ajusté .gitignore pour les artefacts temporaires. | Anciennes sources à consulter via Git ; aucune restauration/nettoyage implicite. |
| D22 | Le 2026-09-07, le propriétaire demande de définir une migration optionnelle de tout HiBoP vers Input System seul, à effectuer juste après le commit de QUEST-002 si recommandée par l'agent. L'audit recommande cette migration et la fiche QUEST-002-A la prépare. | Cible envisagée : New sur Desktop/Android, commandes existantes préservées ; définir la tâche maintenant, l'exécuter séparément après le commit. QUEST-002 reste validée avec Both/New ; la sensibilité verticale préexistante ne se corrige pas implicitement dans cette migration. |
| D23 | Le 2026-09-08, le propriétaire demande d'arrêter le processus HiBoP sur le casque après chaque test validé afin d'économiser la batterie, avec une règle spécifique au développement Quest hors AGENTS.md. | Appliquer la section « Batterie et fin des essais sur Quest » de TASK-WORKFLOW.md : relever les preuves, arrêter uniquement le package HiBoP testé via ADB et vérifier l'absence de PID ; attendre le retour propriétaire pour les validations manuelles. |

| D24 | Le 2026-09-08, après validation complète des gestes QUEST-008, le propriétaire demande l'autre gâchette, plus cohérente selon lui avec les applications Quest. | Saisie, rotation et échelle passent du grip latéral à la gâchette d'index gauche/droite ; X conserve le recentrage. Revalider ce mapping sur le nouvel APK. |
| D25 | Le 2026-09-08, le propriétaire demande de marcher plus librement autour du cerveau en passthrough et confirme voir la grille ou l'avertissement système Quest. | QUEST-008 demande la suppression contextuelle de frontière via Meta OpenXR pendant le passthrough actif ; espace local-floor, restauration demandée à la perte de focus/pause ou si le passthrough cesse. Pas de désactivation globale du Guardian. |
| D26 | Le 2026-09-08, le propriétaire demande un casque de développement toujours éveillé, en ADB Wi-Fi, utilisable sur un chargeur indépendant. | Conserver l'override de proximité et le maintien éveillé sur alimentation entre les essais ; ne plus les rétablir automatiquement en fin d'essai. Désactiver les deux préférences Unity qui arrêtent ADB sur ce poste. D23 continue d'arrêter uniquement HiBoP après validation. Aucun engagement de connexion permanente après redémarrage, perte Wi-Fi ou changement de réseau ; relancer le script si nécessaire. |

| D27 | Le 2026-09-08, après l'échec Player Windows IL2CPP de `CertificateRequest` dans QUEST-009, le propriétaire autorise l'essai de BouncyCastle pour créer l'identité éphémère, en conservant le transport embarqué `TcpListener`/`SslStream`. | Qualifier l'import de clé, TLS et le coût réel dans les Players ; cette autorisation ne valide pas le transport et ne choisit pas l'auxiliaire P06. |

| D28 | Le 2026-09-08, le propriétaire demande de rédiger QUEST-030 après les derniers jalons pour sélectionner un Quest découvert sur le réseau dans une liste, puis saisir uniquement le code affiché dans le casque, sans comparaison manuelle d'empreinte et sans compromettre la sécurité. | Rédiger la fiche maintenant ; exécuter après J0–J7, avec clôture possible de J7 par report Linux explicite. Qualifier le remplacement sécurisé de l'authentification et la découverte avant de revendiquer leur réussite. L'harmonisation menus, fenêtres et charte HiBoP reste différée et distincte de cette tâche. Aucun protocole ou bibliothèque cryptographique n'est encore choisi. |

| D29 | Le 2026-09-08, à la fin de QUEST-012, le propriétaire confirme « Tout OK. Oui pour la durée des envois c'est largement ok. », après validation du confort des trois gestes, coupure radio mesurée de 60,02 s, indépendance de la vue Windows et renvois conservant contenu/pose/taille. | La démonstration anatomique J2 et les durées observées de 0,969–1,083 s sont acceptées pour cette fixture et ce prototype. Aucun seuil général de durée, cadence ou tolérance scientifique n'est créé ; 72 Hz reste une cible provisoire. |

Ajustement documentaire après review du propriétaire : le bloc de routage Quest
ajouté au AGENTS.md global a été retiré. Les fiches renvoient directement au
contrat d'exécution ; l'invocation recommandée peut préciser leur chemin. Aucun
nettoyage de consigne globale n'est reporté à une dernière tâche optionnelle.

## D30 — Parité densité QUEST-019 (2026-09-09)

Le propriétaire accepte explicitement (« Accepter pour ce banc et ces versions »)
les critères soumis après les 18 comparaisons complètes Windows/Quest : écart
absolu maximal de **5 × 10⁻⁷** pour les UV activité et **2 × 10⁻⁷** pour les UV
alpha ; égalité stricte des grilles, maxima, couvertures, masques, catégories et
sentinelles. Les maxima observés sont respectivement 2,980232238769531 × 10⁻⁷
et 1,1920928955078125 × 10⁻⁷. Cette acceptation concerne uniquement les fixtures,
paramètres et binaires identifiés dans le [rapport QUEST-019](reports/QUEST-019.md)
et son manifeste, avec Unity 6000.5.2f1 et hbp_core 0.2.1. Elle ne crée aucun
seuil universel pour d'autres données, versions, algorithmes ou modalités ; les
mesures RMS restent descriptives. La validation des gestes au casque reste distincte.

## D31 — Parité d’un instant iEEG QUEST-023 (2026-09-10)

Le propriétaire accepte explicitement **« Accepter pour ce banc et ces binaires »**
les limites absolues **5 × 10⁻⁷ sur les UV d’activité normalisée** et **2 × 10⁻⁷
sur les UV d’opacité** après comparaison des 36 exports Windows/Quest (18 instants,
deux passages). Maxima observés : **4,172325134277344 × 10⁻⁷** et
**1,7881393432617188 × 10⁻⁷**. Entrées, grilles, masques, catégories, sentinelles
et apparence préparée des contacts restent exacts ; répétitions et restauration
des paramètres sont exactes sur chaque plateforme. Les écarts sont compatibles
avec des arrondis float32 selon architecture/compilation, sans attribution
démontrée à une instruction précise. Cette décision concerne uniquement les
fixtures et binaires du [rapport QUEST-023](reports/QUEST-023.md) et de son
[manifeste](evidence/QUEST-023/manifest.json), Unity 6000.5.2f1, hbp_core 0.2.1.
Elle n’étend pas ces seuils à une timeline, d’autres données ou d’autres versions.
La preuve brute reste sans tolérance ; l’[évaluation D31](evidence/QUEST-023/numerical-acceptance.json)
est séparée. La validation manuelle sur casque reste distincte.

## D32 — Parcours QUEST-030 et mémorisation (2026-09-14)

Le propriétaire demande explicitement de réaliser QUEST-030 maintenant, puis
valide le plan dans cette conversation. Cette instruction remplace l'ordre
historique après J7 pour ce travail et étend son périmètre à la mémorisation
durable du Quest et à la reconnexion automatique. Cible présente : Windows +
Quest ; USB pour le test manuel, Wi-Fi à prévoir sans le déclarer qualifié.

Parcours : HiBoP ouvert sur les deux appareils, onglet Quest, sélection dans
une liste USB/Wi-Fi, saisie IP de secours toujours accessible, code au premier
appairage si nécessaire, bouton Pair puis Envoyer au Quest. La liaison doit se
rétablir après une interruption ; les scènes déjà reçues restent utilisables.
L'agent implémente, compile les deux Players, déploie le Quest et lance les deux
applications avant de rendre la main pour la recette manuelle.

Le casque recharge actuellement. Le propriétaire demande de continuer
l'implémentation et annoncera quand le déploiement et les tests physiques
pourront commencer. Ne pas confondre ce délai matériel avec une validation.

## D33 — Distribution sans ADB ni mode développeur (2026-09-14)

**Évolution du 2026-09-15 :** D34 ci-dessous conserve cette exigence pour le
parcours réseau principal et autorise une option USB avancée avec ADB.
Les paragraphes suivants conservent la décision initiale.

Le propriétaire exige pour le produit final : installer HiBoP Desktop,
installer HiBoP Quest, puis appairer via les applications, sans utilisation
d'ADB, activation du mode développeur ou autorisation du débogage USB.
Le transport USB actuel via ADB est une limite de l'implémentation de test,
même si ADB est embarqué et ses commandes automatisées.

Les essais manuels actuels continuent ; ils ne valident pas cette dépendance
pour la distribution finale. À ce stade, consigner uniquement les limites et
les points ouverts, sans modifier le code ou la CI pour les résoudre.
L'USB sans ADB reste à étudier ; ni son abandon, ni un transport final uniquement
réseau, ni une publication Store ne sont décidés.

La [note ADB, distribution et plateformes](reports/QUEST-030-distribution-and-adb.md)
détaille Windows, macOS/Linux, l'APK Quest et la dépendance non préparée
explicitement dans la CI GitHub Actions.

## D34 — Réseau principal et USB optionnel avec ADB externe (2026-09-15)

Le propriétaire accepte le parcours réseau local comme parcours principal,
sans ADB ni mode développeur pour l'utilisateur. L'USB devient une option
avancée nécessitant ADB installé séparément sur le Desktop, le mode développeur
du Quest et l'autorisation du débogage USB. Ce choix vaut pour Windows, macOS
et Linux lorsqu'ils sont qualifiés ; il ne déclare pas leur support acquis.

Le propriétaire demande explicitement de **ne pas modifier le build Windows
actuel** : ADB y reste embarqué pour l'instant. La transition du packaging,
la détection d'ADB externe et le tutoriel final d'installation/configuration
relèvent de [QUEST-031](tasks/QUEST-031.md), créée mais non exécutée.
Documenter dès maintenant le prérequis ADB pour les recettes USB Mac/Linux ;
le Wi-Fi applicatif n'en dépend pas. Une installation d'ADB seule ne porte pas
le lanceur Windows actuel sur ces plateformes.

D34 remplace l'interdiction générale d'ADB de D33 par cette distinction entre
parcours principal et option avancée. Elle n'impose pas AOA et ne supprime pas
l'USB. L'installation de HiBoP Quest sans mode développeur pour le parcours
principal reste à organiser ; aucune publication Store n'est autorisée par
cette décision. La qualification Linux reste conditionnée à QUEST-027.

## Orientations ultérieures, non figées

- Permettre de modifier des données ou paramètres scientifiques (coupes,
  timeline, etc.) depuis Desktop et de répercuter le résultat dans Quest, tout
  en conservant l'indépendance de leurs vues.
- Pour les conflits scientifiques futurs, envisager un choix explicite de
  source de vérité Desktop ou Quest. La granularité, la durée de ce choix et
  les règles de reconnexion seront définies à l'entrée de cette fonctionnalité.
- Conserver la direction du prompt : Desktop prépare les données ; les
  fonctionnalités scientifiques admises sur Quest utilisent une implémentation
  commune et peuvent fonctionner localement. D15 fixe désormais cette exigence
  dès les premiers jalons de projection.

## Questions résolues et points techniques ouverts

Q04 (transport) est résolue par la préférence conditionnelle D18 ; Q05
(contacts internes) est résolue par D19. Aucune réponse produit supplémentaire
n'est nécessaire pour spécifier le premier jalon.

Restent à qualifier : APIs du transport embarqué dans les Players, union des
packages et réglages d'entrée, runtime hbp_core Android, tolérances de parité
par grandeur et cibles de performance du prototype. Il s'agit de preuves à
produire, pas d'informations à demander au propriétaire à la place de l'audit.

Q01 (calcul local), Q02 (un instant) et l'architecture de Q03 (Apple Silicon)
sont résolues par D15–D17. Les deux CMake et leurs lanceurs définissent macOS
12.0 comme minimum ; les workflows utilisent un runner macOS 15. La version
réellement installée sur le Mac n'a pas encore été observée ; elle sera relevée
lors de la qualification. Ne pas annoncer la compatibilité de tout HiBoP avec
macOS 12 sur la seule base de ces valeurs natives.

## Hypothèses de travail à distinguer des décisions

- L'agrandissement est uniforme et modifie la présentation, sans modifier les
  coordonnées anatomiques ni les distances scientifiques des données sources.
- Pas de sauvegarde explicite ni de reprise après arrêt de l'application dans
  le prototype, conformément au périmètre initial proposé.
- Le cerveau MNI doit être extrait et transféré depuis Desktop pour la preuve
  de bout en bout ; un asset déjà inclus dans l'APK ne suffit pas à cette preuve.
- Le dossier documentaire de travail reste `Docs/dev/quest-autonomous/`.

## Premiers faits observés en lecture seule

Les constats ci-dessous décrivent l'inventaire initial sur feature/xr. Après le
changement de branche, le 2026-09-07 : HEAD feature/xr-autonomous = 8b868a5b8,
merge-base avec origin/develop identique ; XR absent ; seules la modification
.gitignore du propriétaire et la documentation apparaissaient avant l'ajout
du routage de tâches dans AGENTS.md. Les packages/scènes XR de l'audit ne sont
donc plus supposés présents dans la branche de développement courante.

- HiBoP : branche `feature/xr`, commit `eb26c323e`, un seul worktree lors de
  l'inventaire. `origin/develop` local pointe sur `8b868a5b8` ; aucun fetch n'a
  été exécuté pour cet inventaire.
- `origin/develop` déclare déjà Unity `6000.5.2f1` et URP `17.5.0`.
- Desktop et `XR/` déclarent actuellement cette même version Unity et URP,
  mais conservent deux manifests distincts.
- `Assets/Scripts/HBP/Data/Module3D/Camera3D.cs` implémente la rotation autour
  d'une cible et le zoom par déplacement de caméra.
- HiBoP_HoloLens utilise Unity `2021.3.13f1`, MRTK `2.8.3` et des
  `ObjectManipulator` dans les prefabs de scène et de colonne.
- `MNIObjects.LoadDataAsync` charge les surfaces GII et `MNI.trm`, inverse les
  triangles, assemble les hémisphères et calcule les normales. Un transfert du
  résultat préparé doit préserver ces conventions.
- `Column3DAnatomy` utilise `DensityGenerator`, dont les appels passent par
  l'API native `hbp_core`. `IEEGGenerator` reçoit les sites, valeurs préparées,
  paramètres d'influence et dimensions temporelles.
- `Column3DIEEG.SetActivityData` consomme des valeurs déjà traitées par canal,
  puis prépare les buffers de projection. Cela fournit un point d'inspection
  concret pour séparer préparation Desktop et calcul commun, sans conclure
  encore que cette méthode est réutilisable telle quelle sur Quest.
- Le builder Desktop existant possède des chemins Windows, Linux et macOS ;
  sa configuration macOS vise ARM64 dans l'éditeur macOS. Ce constat ne prouve
  pas le fonctionnement du nouveau transfert sur Mac.

Les modifications locales préexistantes de HiBoP_HoloLens et EEGFormat sont
conservées. Aucune compilation, opération sur casque ou modification de code
de production n'a été effectuée pendant ce cadrage.
