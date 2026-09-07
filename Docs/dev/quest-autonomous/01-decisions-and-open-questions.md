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

Ajustement documentaire après review du propriétaire : le bloc de routage Quest
ajouté au AGENTS.md global a été retiré. Les fiches renvoient directement au
contrat d'exécution ; l'invocation recommandée peut préciser leur chemin. Aucun
nettoyage de consigne globale n'est reporté à une dernière tâche optionnelle.

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
