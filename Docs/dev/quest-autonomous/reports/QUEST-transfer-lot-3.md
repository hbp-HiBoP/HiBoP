# QUEST-transfer — Snapshot détaché, préparation native et caches

Date : 16 septembre 2026. Référence : `visu_full_test` / `Small`.

**Statut : lot 3 clôturé**, après correction du préchargement Desktop,
90 tests automatisés réussis et reconstruction des deux players Release.
Cette clôture technique ne transforme pas les objectifs de performance encore
non atteints en résultats acquis ; les limites du benchmark restent documentées.

**Mesures réalisées :** [résultat des deux envois](QUEST-transfer-result-05-lot-3.md).
41,374 s puis 41,982 s après clic ; 11,536 s de préparation anticipée.
Validation fonctionnelle réussie. Le benchmark a identifié un préchargement
Desktop encore bloquant pendant Pair ; le correctif post-benchmark est décrit
ci-dessous. Les durées ci-dessus concernent les players antérieurs au correctif.

## Correctif post-benchmark du préchargement Desktop

Le chargement natif anticipé quitte désormais effectivement le thread Unity :

- Unity copie les métadonnées et les listes de ressources existantes. La
  déduplication par nom reste identique, y compris à l'intérieur d'un même lot.
- Un worker construit les nouvelles ressources privées sans chargement implicite,
  les enregistre pour leur nettoyage, puis charge et vérifie chaque maillage et IRM.
  Les dictionnaires de la scène restent inchangés pendant cette préparation.
- La publication revient sur Unity, après réussite de tout le lot. Une fermeture
  empêche cette publication ; les ressources privées sont libérées après la fin
  effective du worker. Les ressources déjà détenues par la scène conservent leur
  durée de vie existante.
- Deux captures concurrentes revérifient sur Unity la préparation suivie avant
  d'en démarrer une autre. Elles ne remplacent plus le suivi d'un worker actif.
- Les deux lectures GIFTI natives — géométrie et labels MarsAtlas — partagent
  un verrou statique dans `Surface`, car giftilib possède un état global de parsing.
  Ce verrou ne couvre ni la simplification, ni les transformations, ni les awaits.

Les nouveaux scopes `desktop.prepare.anatomyWorker` et
`desktop.prepare.anatomyPublish` distinguent travail natif et publication.
L'observateur de frames mesure aussi l'intervalle qui traverse la fin de
l'opération, même lorsque celle-ci finit avant son premier passage. Le suivi
mémoire de dix secondes n'ajoute pas ses frames aux métriques de l'opération.

La revue indépendante a vérifié la propriété des ressources, les attentes de
fermeture, les captures concurrentes et les deux entrées du parseur GIFTI.
Les preuves de validation du correctif sont regroupées dans
`.test-results/quest-033/fix/` ; les builds corrigés utilisent le répertoire
`.artifacts/quest-033/fix/`, distinct des players du benchmark.

Ce correctif retire le chargement synchrone de l'appairage ; il ne supprime pas
son calcul. Aucun nouveau gain en secondes n'est attribué au correctif sans
nouveau benchmark physique. Les pauses résiduelles, le snapshot froid à 1,47 s
et les coûts ZIP/extraction/JSON restent ceux du bilan mesuré.

### Validation automatisée du correctif

- **83 tests EditMode réussis**, 38,7 s : `fix/editmode.xml`.
- **7 tests PlayMode réussis**, 92,9 s : `fix/playmode-final.xml`.
- Le préchargement froid charge de vrais fichiers GIFTI et NIfTI. Un worker
  maintenu en cours par le test permet de vérifier de façon déterministe que
  les frames avancent et qu'aucune ressource privée n'est déjà publiée.
- Les variantes vérifient l'annulation, la fermeture attendant le worker,
  l'absence de capture après fermeture/annulation, deux captures concurrentes,
  les métadonnées homonymes et la réutilisation sans doublon lors d'un nouvel envoi.
- Le test GIFTI compare géométrie et couleurs à une référence séquentielle
  pendant 64 chargements sur huit workers. Géométrie et labels utilisent
  effectivement le même verrou.
- Le test de l'observateur confirme qu'une opération terminée avant son premier
  passage produit quand même un intervalle, sans compter les frames du suivi mémoire.
- La restauration complète des six modalités et la recapture restent validées,
  ainsi que les deux scénarios d'annulation du worker natif du lot 3 initial.
- Formatage exécuté ; `git diff --check` réussi. Les empreintes des sources
  testées sont conservées dans `fix/source-manifest.json`.

Un premier test de concurrence GIFTI a échoué à cause d'une donnée de test
contenant de la géométrie à la place des labels. Le test emploie désormais un
fichier explicite de quatre labels ; la suite finale passe entièrement.
Ce diagnostic intermédiaire est conservé dans `fix/editmode-invalid-fixture.*`.

### Players corrigés

- Windows Release IL2CPP : build réussi en **219,5 s**, zéro erreur,
  66 avertissements, comme le build du benchmark. Player :
  `.artifacts/quest-033/fix/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
- Quest Release IL2CPP : build réussi en **447,0 s**, zéro erreur,
  67 avertissements, comme le build du benchmark. APK signé et vérifié :
  `.artifacts/quest-033/fix/Android/HiBoP.Quest.apk`, **336 063 196 octets**,
  neuf bibliothèques ARM64 et artefacts natifs conformes au verrou.
  SHA-256 : `e3c7cccbdf684542d376a2bd32acfbe127238ba6c49fc73657f83d41ac6003e9`.
- Les players du benchmark restent dans `.artifacts/quest-033/Windows` et
  `.artifacts/quest-033/Android`. Les nouveaux builds sont produits séparément ;
  leur compilation ne remplace pas les applications déjà lancées ou installées.
- BuildInfo et les réglages modifiés automatiquement pendant les builds ont été
  restaurés à leurs versions sauvegardées avant compilation. Les sources testées
  et compilées n'ont pas changé ; contrôle dans `fix/final-verification.json`.

Pas de nouveau transfert physique demandé pour ce correctif. Sa validation repose
sur les tests du chemin réel de préparation et de la boucle Unity, complétés par
la restauration/recapture complète et les builds IL2CPP. Les players corrigés sont
prêts pour le prochain essai ; ils n'ont pas été installés ou relancés à la clôture.

## Point de départ et objectif

Le [résultat du lot 2](QUEST-transfer-result-04-lot-2.md) mesure **59,914 s**
du clic au reçu de publication : snapshot Desktop 2,713 s, ZIP 11,851 s,
réception Quest 8,157 s, décodage 11,593 s et restauration 15,726 s.
Les coûts initiaux atteignent 20,344 s : anatomie Desktop 8,618 s,
installation/vérification standard Quest 3,578 s et chargement standard 8,148 s.
Les plus longues frames restent de 2,741 s sur Desktop et 3,557 s sur Quest.

Ce lot vise à réduire le travail sur le thread Unity, réutiliser des données
immuables et préparer les références avant l’envoi. Il conserve le format 4,
ZIP/Deflate, le transport TLS et toutes les vérifications d’intégrité.
Le codec et le pipeline de transport relèvent du lot 4.

Les [résultats physiques et leurs limites](QUEST-transfer-result-05-lot-3.md)
sont désormais disponibles.
Déplacer du calcul avant le clic ou sur un worker n’est pas, à lui seul,
une réduction du travail total. La comparaison doit montrer ces trois effets
séparément : suppression de calcul, anticipation et réactivité.

## Modifications

### Capture Desktop

- Le graphe vivant est parcouru sous la protection de capture existante,
  sans `await` pendant sa copie. Les valeurs JSON et les tableaux numériques
  copiés constituent le snapshot détaché.
- Le worker calcule ensuite les empreintes et indices des buffers, encode
  les surfaces et produit le JSON UTF-8, avant le ZIP. Il ne consulte plus
  les données de la visualisation vivante.
- La copie native directe des buffers de surface remplace la création
  transitoire d’un `UnityEngine.Mesh`. Elle utilise exactement le même copier
  natif, donc les mêmes conversions de coordonnées et d’orientation.
- Un cache LRU de géométrie, plafonné à **128 Mio**, utilise l’identité faible
  de la surface, sa version géométrique et une révision supplémentaire pour
  les normales, couleurs d’atlas et échanges de handles. Il ne retient aucun
  handle natif. Les masques et la disponibilité d’atlas sont relus à chaque capture.
- Les références globales sont toujours contrôlées lors de chaque capture ;
  la provenance des fichiers natifs utilisateur est toujours vérifiée à l’encodage.

### Restauration Quest

- Le chargement des volumes NIfTI et la reconstruction des maillages se font
  sur des workers, avec des objets natifs privés. Les références de scène sont
  résolues sur Unity avant ce travail ; l’association à la scène revient sur Unity.
- Ces travaux restent séquentiels. Aucun parallélisme supplémentaire de lecture
  GIFTI n’est introduit : la bibliothèque utilise un état global de parsing.
- L’annulation attend la fin de l’appel natif synchrone. Le résultat privé est
  alors libéré ou remis à la scène ; aucun fichier ni handle utilisé par le worker
  n’est libéré de manière anticipée.
- Un cache LRU de **128 Mio** conserve les bytes immuables des surfaces reçues,
  identifiés par version d’encodage et SHA-256. Chaque lecture reconstruit son
  propre objet natif. Les tableaux et masques mutables ne sont pas partagés.
- Le cache ne court-circuite jamais la validation de l’archive entrante, ni
  l’existence de sa référence. Il est vidé à la fermeture et au nouvel appairage.
  Une génération attachée à l’archive empêche un ancien travail de le repeupler.
  Les surfaces dépassant le budget sont lues sans mise en cache.

### Références standard et préparation initiale

- Le hachage des fichiers installés sur Android passe hors du thread Unity.
- Après installation vérifiée et protection des fichiers en lecture, le manifeste
  peut être réutilisé par le chargement MNI. Le volume MNI utilise également
  le chemin natif vérifié. Cela supprime des passages de hash redondants.
- Desktop conserve la vérification des bytes des fichiers modifiables avant et
  après chargement. Le cache Android n’est pas un cache de chemins arbitraires.
- L’appairage prépare les références Quest, puis les ressources manquantes de
  la visualisation sélectionnée sur Desktop. Le bouton d’envoi devient disponible
  après cette préparation. Si aucune visualisation n’est sélectionnée à ce moment,
  l’envoi ultérieur garde sa préparation normale.
- Cette anticipation possède ses propres traces `quest-preparation` et
  `desktop-preparation`. Son coût doit être ajouté lorsqu’on compare un parcours
  complet depuis une application froide ; il ne doit pas être présenté comme
  un gain de calcul.
- Le suivi des objets natifs de l’éditeur utilise une collection protégée et
  fournit une copie à l’inspecteur, pour éviter les accès concurrents avec les workers.

## Mesures ajoutées

Les traces portent `optimizationBatch = lots-0-1-2-3`.

| Mesure | Interprétation |
| --- | --- |
| `desktop.snapshot.total` | Fenêtre de copie exposée sur Unity |
| `desktop.snapshot.detachGraph` | Parcours du graphe et copie des tableaux |
| `desktop.snapshot.encodeWorker` | Encodage du snapshot détaché |
| `desktop.snapshot.numericWorker` | Hash/déduplication des tableaux sur worker |
| `desktop.snapshot.surfaceWorker` | Encodage des surfaces copiées |
| `desktop.surfaceCache.*` | Hits, misses, évictions, bytes retenus et contournements |
| `quest.mri.nativeWorker`, `quest.mesh.nativeWorker` | Travail natif hors Unity ; marqueur `.queued` avant lancement |
| `quest.surfaceCache.*` | Hits, misses, évictions, bytes retenus et contournements |
| `standardInstalledHashesReused` | Réutilisation du manifeste installé protégé |
| traces `*-preparation`, `trigger=pairing` | Travail initial anticipé, rapporté séparément |

Les mesures de frames, mémoire, réception, extraction, restauration, publication
et rendu CPU des lots précédents restent actives. Le premier rendu CPU ne constitue
pas une mesure des photons effectivement affichés dans le casque.

## Validation autonome

- Formatage du code exécuté et `git diff --check` réussi.
- **82 tests EditMode réussis**, 39,5 s : `.test-results/quest-033/editmode.xml`.
- **3 tests PlayMode réussis**, 94,0 s : `.test-results/quest-033/playmode.xml`.
  Restauration complète des six modalités, recapture et les deux scénarios
  d’attente du worker (avec et sans annulation).
- Build **Windows Release IL2CPP réussi** : 236,7 s, zéro erreur,
  66 avertissements (même nombre que le lot 2).
  Rapport : `.artifacts/quest-033/Windows/DesktopWindows.build-report.json`.
- Build **Android Release IL2CPP réussi** : 519,1 s, zéro erreur,
  67 avertissements (même nombre que le lot 2).
  Rapport : `.artifacts/quest-033/Android/Quest.build-report.json`.
- APK signé et vérifié : **336 055 004 octets**, neuf bibliothèques ARM64,
  identité de signature existante et artefacts natifs conformes au verrou.
  SHA-256 : `bbb629c9d1730161584bc9da574a42175c7198c08cf6a23a99d71310e8e94306`.
  Preuve : `.test-results/quest-033/apk-content.json`.
- Sources identifiées dans `.test-results/quest-033/source-manifest.json`.
  Les 40 fichiers C# recensés sont inchangés depuis les tests.
  BuildInfo et les profils URP modifiés automatiquement par les builds ont
  été restaurés à leur état pré-build ; les artefacts compilés sont conservés.

Tests ajoutés ou étendus :

- mutation des tableaux, métadonnées et collections après capture, puis encodage
  sur worker ; maintien des valeurs capturées et des identités globales ;
- parité des buffers copiés directement avec l’export Unity, invalidation des
  normales/géométries/handles, et masque frais sur un cache hit ;
- lecture immuable du cache reçu, éviction sans casser un lecteur actif,
  invalidation et rejet des insertions provenant d’une génération ancienne ;
- handles natifs indépendants sur cache hit, et rejet d’une archive corrompue
  même avec un cache préalablement rempli ;
- annulation pendant un worker natif : attente effective, absence de publication
  et libération du résultat exactement une fois.

Une revue indépendante de concurrence et de propriété des ressources n’a identifié
aucun défaut bloquant dans les changements. Cela ne remplace ni les tests exécutés,
ni la validation visuelle sur le casque.

### Mise en place matérielle

Installation `adb install -r` réussie, sans effacement des données :
`.test-results/quest-033/quest-install.txt`.
Le nouveau Desktop est lancé sur `visu_full_test / Small`, PID **31064**, avec
le journal `.test-results/quest-033/desktop-player.log`. Le calcul automatique
d’activité est toujours désactivé dans les préférences.

Le Quest est connecté en USB, IP Wi-Fi **192.168.1.18**. Les conditions de batterie,
réseau, version installée et inventaires des traces antérieures sont conservées
dans `.test-results/quest-033/`.

Le lancement Android initial est intercepté par Horizon OS avec
`common_system_dialog_app_launch_blocked_controller_required` : aucun processus
HiBoP n’a démarré à ce stade. Après réveil des contrôleurs et ouverture de HiBoP
par l’utilisateur, le processus **12142** a démarré. Le journal confirme
`COMPOSITION READY; problem=none`, OpenXR/Oculus, Vulkan, stéréo
SinglePassInstanced et suivi de tête actif.
Preuve : `.test-results/quest-033/quest-launch-diagnostic.log`.
Contrôle XR : `.test-results/quest-033/quest-startup.log`.
Deux erreurs `xrRequestBoundaryVisibilityMETA` apparaissent ensuite lors du passage
XR en `STOPPING/IDLE` et de l’arrêt de l’activité. Elles sont conservées dans le
journal ; aucune mesure de transfert n’a encore été lancée à ce moment.

## Procédure du benchmark

Deux envois complets dans **la même session** permettent de mesurer misses et hits
sans répéter l’installation ou la mise en route. Ne pas utiliser le bouton Retry,
qui renverrait simplement l’archive précédente sans refaire la capture.

1. Laisser le Quest branché en USB pour la collecte, chargé et allumé. Réveiller
   les contrôleurs et ouvrir HiBoP dans le casque.
2. Dans le nouveau player Desktop, vérifier que `visu_full_test` / `Small`
   est bien ouvert (le lancement automatique le demande).
   Attendre son affichage complet, sans changer les paramètres de référence.
3. Dans le panneau Quest, choisir le mode IP manuelle Wi-Fi et saisir l’IP
   actuelle du casque. Cliquer sur Pair et attendre « Paired and connected ».
   La première préparation est incluse dans cette attente et enregistrée séparément.
4. Porter le casque et cliquer une seule fois sur **Envoyer au Quest**.
   Attendre Small complète et utilisable ; vérifier le contenu et les interactions.
5. Attendre encore **10 secondes**. Sans fermer Small ni relancer HiBoP, sans nouvel appairage ni modification de la scène, cliquer
   une deuxième fois sur **Envoyer au Quest**. Attendre la visualisation complète.
6. Attendre encore **10 secondes**, puis signaler « terminé » et toute anomalie, en précisant si elle concernait le
   premier ou le deuxième envoi. Laisser USB branché pour la récupération des traces.

La comparaison physique présentera séparément préparation initiale, premier envoi,
deuxième envoi, fenêtre de snapshot et plus longues frames. Les objectifs
0,5–1 s de snapshot et absence de gels multi-secondes sont évalués dans le
rapport de résultat : seul le snapshot du deuxième envoi atteint 1 s ;
le blocage du préchargement Desktop a ensuite fait l'objet du correctif décrit
en tête de ce document. Ces chiffres ne sont pas ceux d'un nouveau benchmark.
