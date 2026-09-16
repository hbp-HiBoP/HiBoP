# Première mesure instrumentée — Small — 16 septembre 2026

## Résultat et validité

`visu_full_test / Small`, trois patients et trois colonnes, est publié avec
succès. L'utilisateur confirme un affichage complet et utilisable. Les deux
players sont non Development, hors éditeur, avec le calcul automatique
d'activité désactivé. Le Quest observe le premier rendu de sa caméra principale
en stéréo. Aucun événement de trace perdu.

**Cet essai utilise le forwarding USB, pas le Wi-Fi.** Le champ `route` provient
de l'adresse effectivement utilisée par le transport (`127.*`). La présence du
casque sur un réseau Wi-Fi ne suffit donc pas à qualifier le chemin de transfert.
Les mesures de préparation, décodage et restauration restent exploitables.

Le chronomètre Desktop, du début du traitement du clic à la fin de l'opération,
mesure **97,286 s**. La réception de l'acquittement est à 97,285 s.
Le premier rendu Quest intervient 32,356 ms après le début d'envoi de cet
acquittement. Le calcul inter-appareils donne deux bornes inversées de seulement
0,613 ms : 97 318,3809 et 97 317,7682 ms. Une différence de cadence d'horloge
est une explication possible, non démontrée. On peut situer le rendu **autour de
97,3 s**, mais pas présenter cet intervalle inversé comme une borne valide à
la milliseconde. Le marqueur est une fin de rendu CPU, pas un temps photon.

## Répartition

| Phase | Temps observé |
| --- | ---: |
| Capture/préparation complète Desktop | 24,096 s |
| Connexion TCP/TLS/autorisation | 0,019 s |
| Deuxième hash complet de l'archive avant envoi | 1,054 s |
| Envoi du payload côté Desktop | 7,592 s |
| Réception du payload côté Quest, incluant hash et écritures | 7,952 s |
| Extraction/validation des ressources du ZIP sur Quest | 23,108 s |
| Désérialisation JSON et chargement des buffers | 14,363 s |
| Validation du modèle décodé | 0,150 s |
| Restauration de la scène Quest | 26,490 s |

L'envoi et la réception se chevauchent : ne pas les additionner. Après réception
du payload, la préparation/publication Quest prend 64,138 s. L'attente de l'ACK
côté Desktop (64,498 s) recouvre ces opérations et la fin de réception ; ce
n'est pas une attente réseau inexpliquée.

### Desktop

- Chargement d'anatomie manquante : **7,316 s**.
- Snapshot sur le thread Unity : **2,826 s**, dont JSON **1,599 s** et
  ressources/surfaces **1,227 s**.
- ZIP sur un worker : **13,119 s**, dont entrées fichiers **6,869 s**, buffers
  **5,924 s** et métadonnées **0,225 s** (durées inclusives).
- Hash final après encodage : **0,826 s**, suivi du second hash de **1,054 s**
  avant envoi. Ces deux lectures complètes sont une piste d'optimisation.
- Plus grand intervalle entre frames : **2,851 s**, aligné sur le snapshot.
  Deux autres intervalles proches d'une seconde surviennent pendant l'attente
  du Quest ; leur cause n'est pas établie par cette trace.

### Quest

- **48 553 entrées ZIP**, dont **48 500 de moins de 4 Kio**.
  Archive : **143 194 969 octets** ; contenu décompressé : **413 780 143 octets**
  (ratio 0,346). Les métadonnées JSON font **34 379 717 octets** avant compression.
- Extraction des `.bin` : **17,373 s**, dont relecture/vérification SHA
  **6,234 s**, lecture/inflation **2,288 s**, appels d'écriture mesurés **0,114 s**.
  La différence inclut notamment ouvertures/fermetures, résolution de chemins,
  allocations et autres coûts non séparés ; ce n'est pas un temps disque pur.
- Extraction des trois NIfTI : **4,647 s**, dont vérification SHA **3,533 s**.
- Dans les **14,363 s** de désérialisation, **49 672 lectures de buffers**
  prennent **8,305 s**, dont **4,903 s** pour leur ouverture. Les résolutions
  de références globales représentent **1,437 s** supplémentaires, également
  incluses dans le total JSON.
- Ressources standard : `ensureInstalled` **3,472 s**, vérification des fichiers
  **3,276 s**, chargement **8,075 s**. `standardAlreadyLoaded=false` : ce résultat
  inclut un coût de premier chargement. Le nom `ensureInstalled` ne prouve pas
  à lui seul une réinstallation physique des fichiers.
- Chargement des trois IRM (provenance et natif) : **9,232 s** ; six meshes personnalisés :
  **1,465 s**. Le grand bloc sur le thread Unity produit un intervalle entre
  frames de **10,778 s**. Autres intervalles : **3,460 s** et **0,515 s**.
- Compteur GC0 : **+1 029** pendant la trace, dont **+770** pendant le décodage.
  Le temps GC n'est pas isolé et les compteurs des générations ne doivent pas
  être additionnés sur ce runtime Unity.

Le code d'extraction alloue actuellement un buffer de 65 536 octets **par
entrée**, soit environ **3,18 Go d'allocations cumulées** pour cet essai, sans
compter les autres objets. C'est un volume cumulé déduit du code, pas une
mémoire simultanément résidente ni une mesure directe des allocations.

Mémoire Quest observée par `dumpsys meminfo` : TOTAL PSS **1 033 844 Kio** avant
le transfert et **2 286 856 Kio** après ; ce ne sont pas les pics. L'état thermique
Android collecté vaut 0 avant/après. La température de batterie après l'essai
est 47 °C ; ces instantanés ne permettent pas d'exclure toute variation de
fréquence pendant le transfert.

## Priorités proposées, sans gain encore mesuré

1. Réduire les allocations par entrée et calculer les empreintes pendant la
   lecture/extraction, en conservant toutes les vérifications d'intégrité.
2. Réduire le nombre de petits fichiers : regrouper/indexer les buffers,
   limiter leurs réouvertures et lire les tableaux par blocs. L'évolution du
   format demandera une validation de compatibilité dédiée.
3. Réutiliser les références standard déjà vérifiées/chargées avec une
   invalidation correcte ; déplacer hors du thread Unity le travail natif
   qui le permet, sans supposer les API Unity utilisables depuis un worker.
4. Examiner la compression et les deux hash complets Desktop, puis le snapshot
   responsable du gel de l'interface.

Un unique deuxième envoi a été demandé en **IP manuelle Wi-Fi**, dans les mêmes
applications, pour mesurer le transport LAN et observer les coûts à chaud.
Comme le chemin réseau et l'état des caches changent ensemble, comparer les
phases séparément : la variation du total ne sera pas attribuable au seul Wi-Fi.
Cet essai est désormais disponible dans le
[rapport Wi-Fi et comparaison](QUEST-transfer-baseline-02-wifi.md).

## Preuves

Répertoire : `.test-results/quest-transfer/player-run-01/`.

- Desktop : `desktop/desktop-7aed2c3983d1416085d1048089fff366.json`.
- Quest : `quest/quest-4d0f073a949c487ab399cd363ca56dde.json`.
- Même transfert : `8412aad7a82d465d9534bd939b7dd6b7`.
- Même SHA-256 : `7d45039d50248bcb9b440c1db9d8907592c1fccdd840795252904c680f745ea9`.
- Analyse complète : `analysis.txt`. Journal Quest/état final : `after/` ; le
  journal Desktop figé de cet essai est `desktop-player-first.log` à la racine
  du répertoire. Le fichier `after/desktop-player.log`, éventuellement copié
  par l'ancien collecteur QUEST-012, n'est pas le journal de cet essai.
- Le lecteur signale explicitement USB et l'inversion des bornes ; il n'applique
  aucune correction silencieuse d'horloge.
