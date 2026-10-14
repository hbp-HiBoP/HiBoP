# Display synchronisé Desktop–Quest : chargement unifié et optimisation

Date : 13 octobre 2026. Statut : implémentation réalisée le 14 octobre 2026 ; mesures et qualification consignées dans [le rapport d'implémentation](QUEST-display-synchronise-implementation.md), avec les limites de recette restantes.

Ce document consolide la discussion et les décisions utilisateur. L'implémentation
a été explicitement confiée le 14 octobre 2026, en commençant par les mesures du
lot 0. Ce document conserve le plan approuvé ; le rapport décrit les variantes
retenues, les résultats et les pistes différées selon les mesures.

Référence d'inspection : branche `feature/xr-autonomous`, commit
`16a6a6cf4e12ff28f8ff95ab0963f7c0a41aa8a5`. Réinspecter les fichiers et les
instructions applicables avant de reprendre : les chemins indiquent les points
d'entrée, pas un état immuable du dépôt.

## 1. Objectif et décisions acquises

Réduire le délai entre le clic sur **Display** et la possibilité d'interagir avec
les scènes demandées sur Desktop et Quest, lorsque la synchronisation automatique
des scènes est activée et qu'un Quest est connecté.

- Une demande Display constitue un lot explicite des visualisations sélectionnées.
- Présenter un seul cycle de chargement Desktop, de 0 à 100 %, couvrant chargement
  local, préparation du transfert, transfert, préparation Quest et synchronisation
  initiale. Aucun redémarrage du pourcentage entre scènes ou entre phases.
- Libérer les interactions du lot à la fin effective de cette opération.
- Si le Quest échoue ou se déconnecte, **conserver les scènes Desktop chargées et
  utilisables**, signaler précisément la synchronisation incomplète et proposer
  **Réessayer**. Ne pas laisser le Desktop bloqué en attendant une reconnexion.
- Réessayer uniquement les synchronisations incomplètes. Réutiliser les préparations
  encore valides ; ne pas recharger les scènes Desktop ni renvoyer une scène dont
  la publication et la synchronisation sont confirmées pour la session courante.
- Conserver les données, paramètres et garanties scientifiques existants. Aucun
  gain de vitesse ne doit venir d'une simplification de la scène ou d'un calcul
  initial demandé par les préférences qui serait silencieusement omis.
- La différence de durée entre première ouverture et réouverture de **Small**
  n'a pas encore été comparée par l'utilisateur. La mesurer au début du chantier.

Référence de travail : `visu_full_test / Small`, trois colonnes iEEG et trois
patients. Ajouter ensuite des lots de plusieurs scènes et les autres modalités
concernées par les chemins modifiés.

## 2. Périmètre et limites

Le chantier porte sur l'ouverture synchronisée, ses présentations de chargement,
la préparation et la restauration des scènes, leur publication initiale et leurs
reprises. Les préférences Quest et l'autoconnexion existantes sont réutilisées.
Ne pas retoucher le prefab User Preferences ni les classes de préférences pour
réorganiser ce chantier.

Le chargement sans Quest et l'envoi manuel doivent garder leurs comportements
utiles. Auditer les autres appels à `Module3DMain.LoadAsync` pour éviter une
régression ; leur migration au nouveau parcours ne doit pas devenir une refonte
générale de tous les chargements, rechargements ou dialogues de HiBoP.

Ne pas refaire le pairing, inventer un second moteur scientifique ou remplacer
les formats des projets/archives. Les optimisations de protocole plus importantes
décrites plus bas sont des lots conditionnés par les mesures. Livrer ensemble les
changements Desktop et Quest lorsqu'une extension du protocole est nécessaire.

## 3. État actuel vérifié dans le code

| Point d'entrée | Comportement et implication |
| --- | --- |
| `UI/Main/Visualization/VisualizationGestion.cs`, `Display` | Un `LoadingManager.LoadAsync` attend le chargement Desktop et ferme la fenêtre de gestion. Il ne possède pas l'ensemble des envois automatiques. |
| `Data/Module3D/Module3DMain.cs`, `LoadAsync` | Validation préalable puis chargement séquentiel des visualisations. Chaque scène rejoint la collection pendant son initialisation. |
| `Data/Module3D/Base3DScene.cs`, `CompleteInitializationCoreAsync` | `OnAddScene` est appelé avant chargement de la configuration, application de la géométrie et restauration de la représentation de surface. Cet événement ne constitue pas une frontière de capture prête. |
| `UI/Quest/QuestManager.SceneLifecycle.cs`, `TickSceneLifecycle` | Déclenche les envois indépendamment, sans attente globale du lot Display. Certains chevauchements existent déjà ; le code ne garantit pas une séquence stricte « toutes chargées, puis toutes envoyées ». |
| Même fichier, `SendSceneAsync` / `SendSceneCoreAsync` | Chaque envoi possède une présentation et une annulation, prépare les atlas, capture la scène, transfère, puis attend la réplique initiale. Les progressions peuvent se remplacer. |
| `Data/Module3D/Base3DScene.Transfer.cs`, `CapturePreparedAsync` | Attend initialisation, anatomie, représentation de surface, générateurs, corrélations et colliders, puis capture sous protection. Une partie de la préparation attribuée à l'envoi est du travail Desktop différé. |
| `Transfer/Scene/DesktopSceneCapture.cs` | Capture le graphe vivant sans `await` intermédiaire sur Unity ; des tokens et buffers possédés sont ensuite encodés hors du thread principal. |
| `Transfer/Transport/BlockDelivery.cs` / `BlockContainer.cs` | Le chemin Windows utilise déjà un producteur, une file de deux blocs, compression/transfert chevauchés et un spool immuable pour les reprises exactes. |
| `Quest/Runtime/QuestAnatomySession.cs` / `QuestAnatomyView.cs` | Réception et publication sérialisées ; la vue refuse deux préparations simultanées. Le reçu de livraison suit la préparation/publication de la scène. |
| `Transfer/Scene/SceneRestoration.cs` | Reconstruit les ressources natives, initialise la scène commune, puis attend `PrepareRenderingAsync`, contrairement au retour du chargement Desktop ordinaire. |
| `UI/Quest/DesktopReplicaSession.cs`, `StartAfterPublicationAsync` | Attend l'application de la barrière initiale avant l'état `Live`. La réception des octets n'est pas la fin de la synchronisation. |

Les chemins de ce tableau sont relatifs à `Assets/Scripts/HBP/`.

Des caches de surfaces existent déjà : `SurfaceCapture` conserve des géométries
suivant leurs versions et `ReceivedSurfaceCache` conserve des octets vérifiés.
Ce dernier ne supprime pas la reconstruction d'un objet natif par consommateur.
Les ressources MNI standard sont déjà installées localement sur les deux appareils.
Ne pas présenter ces mécanismes existants comme de nouvelles optimisations.

## 4. Définition de « prêt » et contrat du lot

### 4.1 Identités et états

Définir une opération locale, par exemple `SceneDisplayOperation`, dans la couche
UI d'orchestration. Le nom est indicatif : privilégier les conventions existantes
et une seule abstraction concrète, sans framework générique supplémentaire.

Capturer au début : identifiant du lot, visualisations demandées, identité du
Quest cible, contexte global et génération de connexion, politique de partage,
token d'annulation et budgets de préparation. Maintenir par scène ses identités
de visualisation/incarnation/transfert, sa génération de capture, ses travaux
possédés, son résultat local et son résultat de synchronisation.

États utiles par scène : chargement local, données stables, capture, encodage,
transfert, préparation Quest, synchronisation initiale, prête sur les deux appareils,
locale seulement à réessayer, annulée ou échec local. Les noms exacts peuvent
reprendre les types existants ; éviter deux sources contradictoires de vérité.

### 4.2 Fin effective

Le succès complet exige, pour chaque scène encore demandée :

1. Un état Desktop initial cohérent, les calculs et le rendu initiaux nécessaires
   aux interactions demandées étant prêts.
2. Un snapshot de cet état avec provenance et versions cohérentes.
3. La publication de la bonne incarnation sur le Quest après préparation du rendu.
4. La confirmation d'application de la synchronisation initiale, pour le même
   contexte et la même génération de connexion.

Auditer les politiques de projection publiées à l'ouverture de la réplique :
elles ne doivent pas déclencher un calcul initial requis après un 100 % prématuré.
Réutiliser les barrières existantes et compléter leur portée seulement si cette
inspection révèle un manque. Ne pas confondre présence d'un GameObject, fin des
octets, rendu préparé, première frame affichée et mesure de photons dans le casque.
Un placement spatial manuel éventuel n'entre pas dans la durée technique du lot.

### 4.3 Connexion et préférences pendant l'opération

- Le lot synchronisé reste associé au Quest choisi au début ; aucune bascule
  automatique vers un autre casque au milieu de la demande.
- Une perte de connexion conduit au résultat local utilisable décrit en section 8.
  Une reconnexion ultérieure ne transforme pas ce lot échoué en reprise cachée.
- Une demande commencée sans Quest reste un chargement local. Si un casque arrive
  ensuite, la synchronisation suit son parcours propre ; ne pas rouvrir ni faire
  reculer une progression déjà terminée.
- Une désactivation du partage pendant un lot met fin proprement à sa partie Quest,
  en conservant le résultat Desktop. Les opérations déjà publiées restent cohérentes
  avec la politique existante, sans suppression silencieuse de scènes.
- Une scène déjà ouverte n'est pas rechargée par inadvertance. Une scène fermée ou
  remplacée invalide ses travaux tardifs, sans affecter une nouvelle incarnation.

## 5. Architecture d'orchestration et présentation

### 5.1 Un seul propriétaire

Le point d'entrée Display confie la demande au coordinateur UI. Celui-ci possède
le cycle visible, l'annulation, la collecte des résultats et l'attente finale.
`QuestManager` conserve la propriété de la session et des publications Quest.

Réserver explicitement les scènes du lot auprès de l'auto-synchronisation. Tant
que la réservation existe, `TickSceneLifecycle` et l'inventaire ne doivent pas
lancer un second envoi de ces mêmes scènes. Les demandes identiques rejoignent
le travail existant ; une demande incompatible reçoit un résultat explicite.
La réservation est libérée dans tous les chemins de terminaison.

Cette réservation temporaire est distincte de l'état **reprise utilisateur requise**
des scènes échouées ou annulées. Cet état reste attaché à la publication et à son
incarnation après la fin du lot, y compris lors des reconnexions. Tick, inventaire,
restauration/réconciliation de session et changements de préférence le respectent :
ils ne relancent pas ces scènes dès que la réservation disparaît. Réessayer ou une
autre demande manuelle explicite peut le lever pour les seules scènes concernées ;
leur fermeture le retire. Il ne s'agit pas d'une nouvelle préférence persistée
sur disque. Les scènes déjà prêtes et les scènes extérieures au lot conservent
leur politique normale de reconnexion.

Séparer le travail d'envoi de l'ouverture de son cercle : le coordinateur fournit
progression et token, tandis que l'envoi manuel peut conserver un wrapper visible.
Les phases d'un lot n'ouvrent pas de cercles imbriqués par scène ou par atlas.
Réutiliser `LoadingManager` et ses API actuelles ; aucune refonte de
`PresentationHandle` n'est nécessaire au titre de ce chantier.

### 5.2 Frontière de disponibilité locale

Exposer dans Data une notification ou un callback neutre après l'initialisation
complète utile à la capture. Ne pas déplacer la signification de `OnAddScene`
sans auditer ses consommateurs. Ne pas faire dépendre Data de Quest pour attendre
un envoi. Le callback signale au coordinateur qu'il peut alimenter sa file ; il
ne doit pas attendre la totalité de l'envoi avant d'autoriser le chargement local
suivant, ce qui supprimerait le chevauchement recherché.

Le chargement local renvoie ou expose les scènes réellement créées pour ce lot.
Ne pas déduire son contenu d'une comparaison fragile de la collection globale.
Les recalculs d'apparence ou de géométrie déclenchés par la configuration doivent
être pris en compte avant de déclarer les données stables.

### 5.3 Interaction

Le bloqueur Desktop couvre le cycle unique. Côté Quest, maintenir une présentation
de lot et bloquer les interactions scientifiques des scènes concernées jusqu'à
la terminaison. Conserver tracking, rendu, PlayerLoop et travaux asynchrones actifs.
Ne pas désactiver les GameObjects ou composants dont la préparation dépend.

Pour les scènes Quest préexistantes hors lot, conserver leurs comportements utiles
tant qu'ils ne modifient pas les dépendances communes capturées. Les modifications
globales incompatibles doivent être empêchées ou explicitement coordonnées, sans
élargir arbitrairement le verrouillage de toutes les fonctions du casque.

## 6. Progression unique de 0 à 100 %

Définir des sous-progressions par scène et par phase : validation/chargement
Desktop, capture, encodage, transport, préparation Quest et barrière initiale.
Choisir leurs poids avant de commencer à les publier, à partir des mesures et de
la taille estimée du lot : colonnes, patients, buffers, géométries et ressources.

- Pourcentage borné et monotone ; une mise à jour tardive ne peut pas faire reculer
  l'affichage ni modifier un autre lot.
- Pas de renormalisation des poids en cours de chargement. Les estimations peuvent
  évoluer pour les prochains lots, pas réécrire la progression déjà affichée.
- Les poids représentent un avancement estimé, pas une promesse de durée restante.
  Utiliser des poids de repli explicites lorsqu'un poste est encore mal connu.
- Les phases parallèles contribuent chacune une fois. Ne pas additionner leurs
  durées chevauchées pour fabriquer une durée totale.
- Réserver une part visible à la préparation Quest et à la confirmation finale.
  Ne pas atteindre 100 % au dernier octet ni avancer artificiellement pendant
  une attente sans nouvel avancement observable.
- Afficher des libellés utiles : scène courante, `2/3 scènes prêtes`, préparation
  Quest ou synchronisation initiale. Aucun `...` ajouté aux textes : l'animation
  existante appartient au loading circle, alignée à gauche sur Quest.

Faire remonter les jalons/progressions Quest par le canal de contrôle de session,
si adapté, en les corrélant au lot, à l'incarnation et à la tentative. Préférer
des notifications coalescées et limitées en fréquence à un polling réseau par frame.
Préserver le framing des reçus binaires existants ; ne pas y intercaler des messages
de progression sans extension explicite du protocole.

Pour maintenir un seul cycle Quest dès le début et entre les scènes, définir au
besoin des commandes de début/fin/abandon de lot, idempotentes et liées à une session
authentifiée. Prévoir leur expiration et leur fermeture sur perte de session :
un Desktop disparu ne doit pas laisser le casque bloqué indéfiniment.

Un échec partiel ferme le chargement après drainage et affiche un résultat distinct
du succès complet. Ne pas afficher « 100 % synchronisé » pour une disponibilité
Desktop seule. Le message de résultat indique les scènes prêtes et celles à reprendre.

## 7. Pipeline borné pour plusieurs scènes

Conserver initialement une seule réception/préparation/publication Quest à la fois.
Le chevauchement visé est :

| Desktop | Quest |
| --- | --- |
| Chargement de la scène N+1 ; capture/encodage de N quand les ressources sont stables | Réception ou application de N−1 |
| Travaux Unity sérialisés sur son thread principal ; workers pour les données possédées | Travaux Unity sur son thread principal ; préparation privée sur workers autorisés |

Le récepteur actuel ne permet pas de recevoir librement N pendant l'application
de N−1. Une telle extension n'est pas un prérequis au premier pipeline.

- Commencer avec au plus une scène capturée/encodée en avance de celle traitée par
  le Quest. Ajouter une borne en octets sur les snapshots, pas seulement en nombre
  de tâches. Définir son budget à partir des pics mémoire mesurés.
- Respecter la contre-pression : ne pas retenir les captures de toutes les scènes
  alors que le Quest ne peut en traiter qu'une. Adapter l'admission du chargement
  suivant si nécessaire pour éviter d'accumuler des travaux et buffers natifs.
- La file HBT4 de deux blocs borne le transport, pas le nombre de graphes capturés.
- Conserver l'ordre de présentation demandé. Ne pas réordonner les scènes pour
  afficher un meilleur chiffre de temps sans décision produit.
- Ne pas remplacer les verrous natifs par une concurrence générale. En particulier,
  les lectures GIFTI doivent conserver leur sérialisation effective.
- Annuler et rejoindre chaque worker avant de libérer sa source, son spool ou ses
  handles. Mesurer mémoire, allocations et frame times en plus du délai total.

Le gain de ce pipeline concerne principalement les lots. Pour une seule scène
Small, les optimisations de la section 10 doivent viser son propre chemin critique.

## 8. Échecs, annulation et reprise

### 8.1 Échec Quest, résultat Desktop conservé

Lors d'une déconnexion, d'un timeout ou d'une préparation distante refusée :

1. Arrêter l'admission de nouveaux travaux Quest de ce lot et annuler les travaux
   distants associés encore en cours, avec des échanges bornés.
2. Rejoindre les travaux locaux possédés avant nettoyage ; ne pas annuler un
   chargement partagé utilisé par d'autres consommateurs.
3. Continuer les chargements Desktop demandés tant qu'ils peuvent réussir. Une
   panne Quest ne doit pas supprimer les scènes locales restantes du lot.
4. Conserver les scènes et publications complètes. Relever les statuts des autres
   scènes comme locales seulement, échouées ou inconnues après perte d'ACK.
5. Fermer le bloqueur, libérer les interactions Desktop et présenter les résultats
   avec une action Réessayer. Une nouvelle erreur ne doit pas produire un dialogue
   par scène ni être affichée à la fois par le coordinateur et `LoadingManager`.

Marquer les scènes incomplètes comme nécessitant une reprise utilisateur avant de
libérer la réservation. Une session peut se reconnecter sans relancer leurs envois
ou leur réconciliation en arrière-plan. L'inventaire ne doit pas supprimer ou
remplacer une publication incertaine dans le but de contourner cette attente.

La réconciliation et les conflits existants gardent leur choix explicite. Une
association ou un contexte incompatible renvoie au parcours de connexion approprié,
sans supprimer de scène ni réinstaller silencieusement ses dépendances.

### 8.2 Annulation utilisateur

Annuler met fin à l'admission du reste du lot, puis attend la fin effective de ses
travaux. Conserver les scènes Desktop complètement ouvertes ; fermer proprement
les constructions locales incomplètes. Les scènes distantes déjà publiées restent
cohérentes, avec un statut connu. Empêcher toute réponse tardive de publier un
succès, de rouvrir un cercle ou de relancer automatiquement le lot annulé.
Appliquer également l'état de reprise utilisateur aux scènes incomplètes conservées.

Si une publication distante peut avoir réussi sans reçu, utiliser les identités
et la réconciliation existantes pour déterminer son état lors d'une reprise.
Une annulation réseau ne prouve pas à elle seule que rien n'a été publié.

### 8.3 Réessayer

- Réessayer ne réouvre pas les scènes Desktop, ne renvoie pas les scènes déjà prêtes
  sur la même session et rejoint toute tentative identique encore possédée.
- Vérifier le casque, le contexte, la génération de session et l'état distant. Un
  succès d'une ancienne session n'est pas une preuve suffisante pour la nouvelle.
- Un snapshot réutilisable garde une durée de vie possédée, ses versions, sa
  provenance et les journaux nécessaires à une publication cohérente.
- L'utilisateur peut modifier une scène Desktop après l'échec : réutiliser un
  ancien snapshot seulement si le journal et les versions permettent d'aboutir à
  son état courant. Sinon, recapturer cette scène, sans recharger ses données.
- Borner les préparations retenues pour reprise. Une éviction peut imposer une
  recapture, jamais une réutilisation périmée. Libérer les archives devenues inutiles.
- Un reçu perdu suivi d'une publication confirmée ne crée pas une deuxième scène.
- Une nouvelle tentative possède son propre cycle visible 0–100 % et ne reprend
  aucun ancien callback de progression.

En cas d'échec local, conserver les scènes locales déjà terminées et arrêter les
envois des scènes dont la source n'est pas valide. Respecter le comportement de
chargement existant pour le reste du lot, avec un résultat explicite et sans rollback
des scènes préexistantes. Réserver les dialogues aux erreurs réellement bloquantes.

## 9. Lot 0 : mesures de référence et instrumentation

Mesurer avant toute optimisation, depuis le clic Display jusqu'au déverrouillage
effectif des interactions. Le coût du clic à la scène Desktop affichée n'est pas
le coût du clic à la scène synchronisée utilisable.

Étendre les sondes existantes `SyncTelemetry` / `SyncReceiveTelemetry` et les
marqueurs Unity, plutôt que créer un système parallèle. Corréler lot, scène,
incarnation, capture, tentative et génération de connexion.

| Zone | Mesures à distinguer |
| --- | --- |
| Desktop chargement | Validation partagée, accès disque/cache, données patients/colonnes, ressources anatomiques, initialisation Unity, configuration et calculs initiaux |
| Attente avant capture | Initialisation, anatomie manquante, projection/générateurs, corrélations, colliders, attente de verrous |
| Capture | Copie du graphe, copie native/managée, clonages de surfaces, capture numérique, construction des tokens ; CPU et attente séparés |
| Encodage | Résolution des références, empreintes, sérialisation, copies/spool, compression ; taille utile et volume intermédiaire |
| Transport | Connexion/authentification, premier/dernier octet, débit observé, attente du producteur et contre-pression |
| Quest préparation | Vérification/décompression, parsing JSON et buffers, validation, reconstruction de graphes, volumes et surfaces natifs |
| Quest rendu | Initialisation commune, sites, colliders, projection, textures, uploads Unity/GPU et publication |
| Synchronisation finale | Ouverture de réplique, journal/checkpoint, barrière appliquée, politiques initiales et déverrouillage |

Relever aussi nombre de scènes/colonnes/patients, sommets/triangles, tableaux et
octets capturés, hits/miss/évictions des caches, allocations, GC, pics mémoire
Desktop/Quest, durées de frame et travaux natifs simultanés. Mesurer séparément
premier envoi et reprise. Déclarer indisponibles les métriques non mesurables.

Protocole minimal :

1. Small après lancement neuf des applications ; distinguer processus froid et
   fichiers déjà installés ou cache OS, sans effacer les données utilisateur.
2. Fermeture puis réouverture de Small dans la même session ; relever les caches
   réellement utilisés plutôt que supposer un scénario « chaud ».
3. Lot de plusieurs scènes, dont des ressources communes puis des ressources distinctes.
4. USB et Wi-Fi, mêmes fixtures et paramètres. Distinguer Editor/development et
   Players Release ; le verdict de performance doit inclure les Players cibles.
5. Plusieurs répétitions comparables, idéalement cinq par condition retenue ;
   publier médiane et dispersion, frame times, allocations et mémoire. Consigner
   appareil, température/thermique, versions, hashes de fixture et exécutables.

Ne pas soustraire directement des horloges Desktop et Quest non synchronisées.
Mesurer le délai de bout en bout sur Desktop à partir du retour de disponibilité
distante ; utiliser les durées locales corrélées pour le détail. Une première
frame instrumentée ne constitue pas une mesure command-to-photon.

Les anciens rapports de septembre donnent des pistes, pas la baseline actuelle :
[plan historique](QUEST-transfer-optimization-plan.md),
[pistes différées](QUEST-transfer-future-optimizations.md) et
[bilan historique](QUEST-transfer-final.md). Les évolutions de scènes et de
synchronisation imposent de reprendre les mesures sur le code courant.

## 10. Optimisations à qualifier après la baseline

Toutes les pistes discutées sont conservées ici. Leur ordre final dépend des
mesures ; ne pas implémenter une refonte coûteuse si le poste visé n'est pas sur
le chemin critique. Pour chaque piste : hypothèse, mesure isolée, intégrité,
comparaison avant/après, coût mémoire et décision documentée.

### 10.1 Préparer l'état transférable pendant l'ouverture Desktop

Identifier les données privées et stables disponibles pendant le chargement et
réutiliser leur préparation pour la capture. Éviter de reconstruire depuis les
GameObjects des informations déjà disponibles sous forme possédée et détachée.
Créer une frontière de préparation commune réutilisable, avec provenance et
versions, plutôt qu'un cache permanent du graphe Desktop mutable.

Étudier séparément les attentes de capture : si un collider ou un upload de rendu
n'est pas une dépendance des données transférées, il pourrait se terminer pendant
le transfert et être attendu à la barrière finale. Prouver cette indépendance
avant de déplacer un verrou ; ne pas simplement retirer des attentes existantes.

### 10.2 Réduire copies, clonages et sérialisation répétée

Mesurer les passages natif → tableaux managés → encodage → tableaux managés → natif.
Réutiliser les buffers immuables possédés et contrats de sérialisation par opération
lorsque cela supprime réellement une copie. Maintenir versions de géométrie,
révisions de transfert, masques, représentations gonflées et références globales.

Si l'arbre JSON domine, réduire les conversions et allocations de
`DetachedSceneMetadata` / `PreparedDataJson`. Un DTO de transfert explicite ou des
tables plus compactes est une évolution ultérieure mesurée, distincte du JSON
des projets. Tout changement de schéma implique versionnement et validation des
deux applications. Ne pas partager un objet natif mutable pour éviter sa copie.

La capture actuelle du graphe vivant reste atomique sur Unity. Pour l'étaler sur
plusieurs frames ou déplacer une lecture en worker, définir d'abord un contrat
de version/lease assurant la stabilité complète de la source ; des `await` ajoutés
dans la lecture d'une scène vivante ne constituent pas ce contrat.

### 10.3 Chevaucher préparation privée et réception sur Quest

Le pipeline HBT4 reçoit déjà JSON, références, index et pack avant certaines
ressources externes. Si la reconstruction JSON reste coûteuse, qualifier un jalon
« métadonnées et pack fermés et vérifiés » pour reconstruire un graphe privé
pendant la réception du reste. Le JSON seul ne suffit pas : ses lecteurs utilisent
le pack numérique.

Préparer ensuite, si rentable, les volumes natifs au fur et à mesure de la réception
complète de chaque ressource vérifiée, avec un worker borné. Une ressource composée
exige tous ses membres vérifiés ; une surface dépendante attend son volume.
L'ordre des ressources peut être adapté aux dépendances, sans réordonner les scènes.

Conserver `ReadPrepared`, le scellement global et la validation du transcript comme
contrats des consommateurs finaux. Ajouter un chemin préparatoire privé explicite,
avec propriétaires et publications immuables pour les ressources individuelles.
Ne pas exposer les dictionnaires ordinaires à des lectures/écritures concurrentes.
Une erreur finale détruit les candidats privés après drainage ; rien n'est publié
avant validation complète et cohérence du contexte.

### 10.4 Réutiliser les ressources entre scènes et reprises

Exploiter les caches existants puis mesurer ce qu'ils laissent à refaire : décodage,
reconstruction native, chargement IRM ou upload. Des caches immuables plus riches
peuvent aider les réouvertures ; définir clés de contenu/version, budget, éviction,
propriété des handles et invalidation sur changement de contexte.

Une négociation de ressources déjà disponibles sur Quest peut éviter retransfert
et préparation, mais constitue une extension de protocole distincte. Un hit doit
prouver identité, intégrité et disponibilité actuelle de la ressource. Conserver
un repli complet correct ; ne pas utiliser un simple chemin ou un ID mutable comme
preuve de contenu. Ne pas annoncer cette piste comme un gain du premier envoi.

### 10.5 Réduire les pauses Unity et les calculs redondants

Mesurer les créations de sites, colliders, textures, uploads et recalculs déclenchés
plusieurs fois par l'initialisation/configuration. Regrouper les invalidations pour
calculer une fois l'état final demandé, en conservant le moteur scientifique commun.

Répartir les opérations Unity lorsque cela améliore la réactivité, tout en gardant
une publication cohérente. Distinguer réduction des freezes et réduction du délai
total : ajouter des frames peut améliorer la première et allonger le second.
Ne pas différer après 100 % une préparation indispensable aux interactions prévues.

### 10.6 Transport et compression

Conserver d'abord le streaming HBT4, ses files bornées, sa compression existante
et son spool de reprise. Examiner débit, CPU et contre-pression avant de modifier
codec, taille de blocs, SHA ou parallélisme. Réduire les checksums ou perdre la
reprise exacte ne fait pas partie des optimisations autorisées.

## 11. Assemblies, threads et intégrité

- Le coordinateur dépend des couches Data/Transfer/Quest nécessaires depuis une
  couche UI adaptée. Aucun lien Data → Quest/UI, ni Core → autre assembly `HBP.*`.
- Les contrats neutres de disponibilité de scène restent dans la couche qui possède
  ces scènes ; ne pas déplacer un contrat Quest dans Core pour contourner le graphe.
- Avant toute nouvelle référence `.asmdef`, inspecter les dépendances directes et
  transitives puis exécuter `Tools/check-assembly-dependencies.ps1`.
- Les workers ne reçoivent que snapshots, tokens JSON détachés, buffers possédés et
  ressources retenues. Les accès Unity, mutations des scènes, UI et publications
  restent sur le thread principal.
- Les jobs natifs gardent leurs contraintes de réentrance et propriétaires. Fermer,
  annuler ou évincer attend leur drainage avant libération.
- Conserver l'identité et la cohérence du snapshot à travers les attentes, journaux
  de mutations, changements de session, reçus perdus et retries.
- Préserver la précision, les données de toutes les modalités, filtres, unités,
  normalisation, projection, surfaces et provenance. Aucun allègement scientifique
  implicite, suppression de ressources requises ou seuil de parité inventé.

## 12. Découpage d'implémentation proposé

| Lot | Livrable concret | Validation de sortie |
| --- | --- | --- |
| 0 — référence | Mesures froid/chaud Small et multi-scènes ; carte du chemin critique et des attentes ; budgets mémoire initiaux | Traces corrélées, durées non double-comptées, fixtures/exécutables identifiés |
| 1 — frontières et propriété | Disponibilité locale neutre, résultat de lot, réservation des publications, tokens/générations, distinction travail/présentation | Aucun double envoi ; fermeture/annulation/réponse tardive sûres ; graphe valide |
| 2 — chargement unifié | Un cercle Desktop, progression monotone agrégée, retour de progression Quest, maintien de présentation du lot et vraie barrière finale | Aucun trou ou remise à zéro ; interaction libérée à la bonne frontière ; aucun blocage du PlayerLoop |
| 3 — erreurs et reprise | Desktop conservé et utilisable après panne Quest, résultat par scène, Réessayer ciblé avec validation des snapshots et états distants | Aucun rechargement Desktop inutile, publication double ou reprise automatique cachée |
| 4 — pipeline borné | Chargement suivant et préparation détachée chevauchés avec une seule scène en avance et limites mémoire | Gain multi-scènes mesuré ; mémoire et frames acceptables ; verrous natifs préservés |
| 5 — préparation Small | Optimisations 10.1/10.2/10.5 ciblant les postes effectivement dominants | Gain de bout en bout et de capture démontré ; parité scientifique et intégrité préservées |
| 6 — optimisations supplémentaires | Expérimentations 10.3/10.4, puis 10.6 uniquement si justifiées ; conserver seulement les variantes rentables | Gains propres aux cas froid/chaud établis, coût mémoire connu, protocole et repli validés |
| 7 — qualification | Tests automatisés, builds Desktop/Quest, recettes physiques et rapport final comparatif | Critères section 14, limites et pistes différées explicitement consignées |

Les lots 1–4 constituent le comportement produit cible. Les lots 5–6 ne sont pas
une autorisation de refonte générale : choisir les optimisations d'après le lot 0,
documenter pourquoi une piste est retenue ou écartée et ses résultats. Une piste
non rentable n'est pas implémentée pour remplir une liste. Aucun gain chiffré n'est
promis avant les mesures ; les objectifs anciens de transfert ne deviennent pas
automatiquement les seuils de ce nouveau parcours.

## 13. Validation automatisée et essais physiques

### 13.1 Tests ciblés

- Un lot, plusieurs scènes, poids stables, événements désordonnés ou tardifs :
  progression monotone, un seul cycle visible, pas de 100 % avant disponibilité.
- Réservation d'une scène déjà vue par Tick/inventaire, demandes identiques et
  incompatibles : un seul propriétaire et un seul transfert par tentative.
- Notification de disponibilité après configuration, capture stable pendant les
  mutations, rendu/projection encore occupés : pas de snapshot prématuré.
- Quest absent, option désactivée, connexion tardive, option modifiée, perte ou
  changement de génération : comportements locaux et identité de cible préservés.
- Perte avant transfert, pendant réception, pendant préparation, après publication
  avant ACK, puis pendant barrière de réplication : résultat précis et reprise sûre.
- Échec Quest au milieu du lot : les autres scènes Desktop peuvent terminer ;
  libération du bloqueur et Réessayer limité aux scènes incomplètes.
- Après libération de la réservation d'un lot échoué/annulé, exécuter Tick,
  inventaire, reconnexion et changement de préférence : aucune reprise cachée
  des scènes marquées ; une demande manuelle ne lève que les états concernés.
- Annulation à chaque phase, fermeture de source, destruction du coordinateur :
  fin effective des workers, absence de fuite, de succès tardif et de reprise cachée.
- Modification Desktop après échec, contexte remplacé, caches évincés : reprise
  avec journal valide ou nouvelle capture ciblée, jamais un état périmé.
- Plafonds de snapshots et d'octets, contre-pression, absence de famine et de cycle
  de verrous, respect de la limite de préparation Quest et du parseur natif.
- Corruption, définitions modifiées sous le même ID, ressources remplacées, masques,
  surfaces gonflées, références de buffers et parité scientifique des scènes.
- Si préparation incrémentale : pack non fermé, ressource composée incomplète,
  échec du transcript final, annulation après préparation privée et avant publication.
- Chemins sans Quest, envoi manuel, scènes déjà ouvertes, conflits de réconciliation
  et autres callers touchés par l'extension du chargement.

Utiliser les tests existants de Transfer, Sync, Data, Quest et UI ; ajouter les
scénarios de coordination manquants. Les tests UniTask/Unity restent `async Task`
avec `await` direct. Aucun blocage `.Wait()`, `.Result`, `GetAwaiter().GetResult()`,
busy-wait ou fire-and-forget non observé dans les tests.

### 13.2 Recettes réelles

- Small seule froide puis chaude, même état et paramètres, USB et Wi-Fi.
- Plusieurs scènes, progression jusqu'au déverrouillage, interactions immédiates
  sur Desktop et Quest, rendu et données scientifiques vérifiés.
- Coupure USB/réseau, veille/réveil et erreur de préparation Quest : Desktop utile,
  résultat lisible, reprise volontaire sans duplication ni perte de scènes.
- Modification locale après échec puis reprise ; reconnexion au même casque avec
  contexte conservé et scènes déjà ouvertes.
- Pression mémoire et fermetures successives : pics bornés, ressources relâchées,
  aucune croissance inexpliquée ni crash natif.

Modifier les prefabs nécessaires directement dans Unity MCP, avec les contrôles
et références sérialisées conformes à HiBoP. Aucun GameObject construit au runtime
pour contourner un contenu de prefab manquant, ni script temporaire reconstruisant
les prefabs. Inspecter console, références et rendu des deux présentations.

Builds et validation Unity suivent `AGENTS.md` et le skill
`hibop-unity-validation`. Inspecter les instances avant d'agir ; ne pas lancer un
second éditeur si HiBoP est ouvert. Pour ADB Wi-Fi, utiliser le script du dépôt.
Vérifier les résultats des tests, les logs, le contenu des builds et le registre
de sérialisation si de nouveaux contrats sérialisés le concernent. Exécuter la
gate de dépendances pour toute modification d'assembly.

## 14. Critères d'acceptation et livraison

- Une demande Display synchronisée possède un unique cycle visible, monotone de
  0 à 100 %, incluant toutes les scènes du lot et les préparations locales/distantes.
- Le succès complet libère les interactions après préparation et application de
  la synchronisation initiale, pour les bonnes incarnations et la bonne session.
- L'échec Quest ne bloque pas indéfiniment le Desktop, ne détruit pas ses scènes
  et offre une reprise ciblée explicite. Succès et disponibilité locale partielle
  sont présentés différemment.
- Aucun double envoi, rechargement local inutile, publication périmée, reprise
  cachée ou résultat tardif après annulation/fermeture.
- Le pipeline et les caches respectent les budgets, la propriété des ressources
  et les contraintes Unity/natives ; les garanties scientifiques restent intactes.
- Le rapport avant/après distingue Small seule, lots, cas froids/chauds, transports,
  attente totale, CPU, frames et mémoire. Une optimisation retenue apporte un gain
  reproductible à son cas cible, sans régression matérielle inexpliquée ailleurs.
- Les mesures physiques manquantes sont indiquées comme telles. Ne pas assimiler
  compilation, tests automatisés ou rapports historiques à une validation actuelle.

Livrer le code, les prefabs, tests, extensions Desktop/Quest compatibles et un
rapport avec commandes, versions, artefacts, résultats, limites et pistes différées.
La revue de concurrence et d'intégrité doit examiner propriété du lot, frontières
de capture, verrous, cache et fin réelle des annulations.

Après toute implémentation, validation et revue, exécuter le formatter C# du dépôt
une seule fois, comme dernière action avant le compte rendu, conformément à
`AGENTS.md`. La présente livraison documentaire ne modifie aucun fichier C#,
prefab, réglage Unity ou donnée utilisateur et ne nécessite pas ce formatage.

## 15. Reprise dans une future discussion

Instruction suggérée :

> Implémente le plan décrit dans
> `Docs/dev/quest-autonomous/reports/QUEST-display-synchronise-plan-implementation.md`.
> Commence par les mesures du lot 0, puis réalise le chargement unifié et les
> reprises. Choisis les optimisations selon les mesures, en conservant toutes les
> garanties scientifiques et les conventions actuelles de HiBoP.

Les décisions produit de la section 1 sont acquises. Les noms de classes, poids de
progression, budgets mémoire et variantes d'optimisation peuvent être précisés par
l'implémenteur à partir des preuves, sans demander une confirmation de chaque détail.
Un changement de fidélité scientifique ou de comportement produit exige en revanche
une décision explicite avant de quitter le périmètre convenu.
