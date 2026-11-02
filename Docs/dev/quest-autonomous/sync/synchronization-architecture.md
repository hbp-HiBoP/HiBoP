# Synchronisation Desktop–Quest

Le runtime échange des opérations métier typées entre un Desktop et un Quest autonome. Desktop séquence les opérations acceptées ; Quest applique immédiatement ses propres gestes puis reçoit leur confirmation ou correction. Les deux appareils exécutent le même modèle scientifique. La livraison initiale des ressources et la synchronisation courante ont des propriétaires distincts.

État vérifié par lecture du checkout le 2 novembre 2026. L'implémentation est présente ; les [limites de qualification](synchronization-validation.md) distinguent tests, observations historiques et matériel non requalifié.

- [Opérations, invalidation et calculs coordonnés](synchronized-operations.md).
- [Protocole, ordonnancement et rétention](wire-protocol.md).
- [Vérification, mesures et limites](synchronization-validation.md).
- [Scènes et colonnes communes](../shared-scientific-scenes.md), [livraison complète](../transfer.md).

## Propriétaires et chemin courant

| Propriétaire | Responsabilité |
| --- | --- |
| [SceneMutationBoundary](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs) | Observe les opérations ciblées, valide/applique les familles typées, exporte et restaure leurs checkpoints. |
| [DesktopReplicaSession](../../../../Assets/Scripts/HBP/UI/Quest/DesktopReplicaSession.cs) / [QuestReplicaSession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestReplicaSession.cs) | Séquençage/propositions, réception, confirmations applicatives et coordination des jobs d'une incarnation. |
| [OnlineMutationDriver](../../../../Assets/Scripts/HBP/Sync/Scene/OnlineMutationDriver.cs) | Autorité Desktop, état optimiste Quest, corrections et rebasage des modifications suivantes. |
| [PersistentTransport](../../../../Assets/Scripts/HBP/Transfer/Transport/PersistentTransport.cs) | Lecture, écriture, reprise et liveness du flux authentifié ; aucune manipulation Unity depuis les boucles réseau. |
| [QuestManager.SceneLifecycle](../../../../Assets/Scripts/HBP/UI/Quest/QuestManager.SceneLifecycle.cs) / [QuestAnatomySession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | Inventaire, publication, routage multiscène, fermeture, sélection et reprise. |

La frontière métier produit un petit payload détaché. L'application distante appelle les mêmes handlers et garde leurs invalidations, avec un contexte supprimant la réémission. Un écho équivalent confirme la proposition optimiste ; une valeur différente emprunte la correction autoritaire. Une modification ordinaire ne capture pas toute la scène et n'attend ni rendu stable ni acquittement visible. Le registre de clés détecte les conflits concurrents réels : Desktop ne rejette pas systématiquement une proposition Quest ancienne portant sur une clé indépendante.

Les DTO/codecs et le scheduler sont dans `HBP.Sync.Runtime`, la composition scientifique dans `HBP.Sync.Scene`. Les événements de Core restent neutres. Les contraintes de dépendances et les règles de développement ont pour propriétaire [AGENTS](../../../../AGENTS.md) et les [connaissances agents](../../../agent-knowledge/synchronization.md) ; ce dossier explique leur mise en œuvre.

## Identités et publication

`sessionId` identifie la session, `sceneId` la visualisation logique, `incarnationId` son ouverture concrète, `resourceManifestId` les ressources préparées. Les colonnes, sites, coupes, ROI et sphères ont des identités stables ; un index de liste ou un nom affiché ne remplace pas cette identité. Une fermeture/réouverture ou un remplacement crée une nouvelle incarnation. Les messages et calculs tardifs de l'ancienne ne peuvent pas s'y appliquer.

[PublicationJournal](../../../../Assets/Scripts/HBP/Sync/Scene/PublicationJournal.cs) commence à la capture cohérente. Pendant la livraison, les mutations représentables sont retenues puis rejouées après publication Quest. Un débordement impose un checkpoint courant ; une mutation de ressource/topologie non représentable fait recommencer la livraison. La scène n'est déclarée live qu'après le rejeu/checkpoint et l'acquittement de sa barrière initiale. Le heartbeat ne doit pas remplacer le propriétaire d'une publication encore en cours ; une réponse d'inventaire obsolète est réévaluée contre son identité et sa tâche.

Le checkpoint contient les états typés partagés, masques, résultats canoniques disponibles et placement scientifique Original/Left/Right. Il exclut fichiers sources, GameObjects et poses locales. Sa capture est permise aux frontières de publication/reconnexion, jamais comme détecteur de changement ordinaire. Les ressources utilisateur nouvelles nécessitent une nouvelle livraison complète ; les atlas installés suivent l'exception ci-dessous.

## Coupure et réconciliation

Pendant la grâce de **500 ms**, la vue reste utilisable et les trames fiables sont réessayées avec les mêmes identités/octets. Après expiration : avertissement de déconnexion, abandon de l'historique de retry, annulation des jobs coordonnés et poursuite locale. Il n'existe pas de reprise persistante après arrêt/crash du processus.

[SceneReconciliationProtocol](../../../../Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs) restaure d'abord les préférences Desktop, puis réconcilie les incarnations communes avec leur manifeste. [SceneCheckpointMerge](../../../../Assets/Scripts/HBP/Sync/Scene/SceneCheckpointMerge.cs) compare les états courants à la dernière base commune retenue :

- modifications identiques : aucune question ; attributs/entités indépendants : combinaison ;
- conflit sur un même champ, suppression contre modification/sélection dépendante, ressource contre paramètres dépendants : choix Desktop/Quest pour le groupe cohérent ;
- base commune indisponible : choix d'une scène entière.

L'avance naturelle d'une timeline n'est pas une édition explicite. Les poses restent locales. Le dialogue de conflit impose Desktop ou Quest, sans bouton Cancel ; annulation de transport/application demeure possible.

Les interactions sont gardées pendant choix, préparation et application. Toute la candidate est validée et ses représentations locales préparées avant commit. Un échec avant commit ne modifie rien ; un échec après son début conserve la candidate exacte et la garde, avec état désynchronisé et reprise explicite. Une reprise ne devient pas un nouvel envoi écrasant les éditions hors ligne. La réussite établit une nouvelle base commune. Il n'y a pas de rollback transactionnel entre plusieurs scènes.

## Inventaire, visibilité et scène patient

Le réglage Desktop `AutomaticallySynchronizeScenes`, activé par défaut et commun aux casques, gouverne seulement l'appartenance des scènes :

| Réglage | Comportement |
| --- | --- |
| Activé | L'inventaire Desktop est autoritaire à la connexion/reconnexion : livraison des scènes manquantes, fermeture des copies Quest supplémentaires, fermetures connectées dans les deux sens. Une fermeture Quest est traitée avant comparaison des inventaires. |
| Désactivé | Les scènes communes continuent à synchroniser mutations, sélection et visibilité ; les fermetures suivantes restent locales. Une copie Quest dont la source Desktop disparaît devient orpheline, jamais rattachée à une nouvelle ouverture de la même source. |
| Réactivé | L'inventaire Desktop est réappliqué, y compris fermeture des orphelines. |

Une fermeture transporte les watermarks d'opérations/bulk à terminer ou abandonner et attend les utilisateurs de cette incarnation. Elle ne bloque pas les autres scènes. Une scène cachée conserve ressources et synchronisation, mais suspend rendu/interactions et termine les gestes ; la réafficher ne reprend pas une capture ancienne.

Une interaction intentionnelle sur cerveau/site/coupe sélectionne son contexte scène/colonne ; le survol ne le fait pas. Le geste conserve sa cible initiale même si la sélection change. Pose, rotation, échelle et disposition Single/Split restent locales. Une nouvelle scène est placée devant le casque avant de devenir visible, sans déplacer les scènes précédentes.

Depuis un site d'une scène multi-patients partagée, Quest peut demander **en ligne** l'ouverture du patient à Desktop. Le chemin commun réutilise une ouverture enfant existante pour `(ouverture source, patient)`, la montre et la sélectionne ; sinon il la crée et la livre aux deux appareils, même avec appartenance automatique désactivée. Les définitions des colonnes et préférences anatomiques sont conservées, mais les masques d'effacement de la topologie source et la sélection d'aperçu IRM transitoire sont abandonnés. Parent et enfant évoluent ensuite indépendamment ; fermer le parent ne ferme pas ses enfants.

## Préférences et atlas de session

[DesktopSessionPreferences](../../../../Assets/Scripts/HBP/UI/Quest/DesktopSessionPreferences.cs) et [SessionPreferencesReceiver](../../../../Assets/Scripts/HBP/Sync/Scene/SessionPreferencesReceiver.cs) synchronisent **toutes** les `UserPreferences`, même sans scène. Les définitions tags/protocoles/alias sont également actualisées après appairage. Le snapshot est validé avant effet, appliqué sur Unity en conservant l'objet/listeners Quest, puis acquitté ; aucune sauvegarde implicite du fichier Quest. Génération de connexion, UUID d'opération et révision monotone empêchent les applications obsolètes. Une opération abandonnée à la déconnexion n'est pas rejouée : reconnect installe l'état actuel Desktop.

Les préférences exigeant rechargement capturent les vues sur Unity, ferment/attendent les anciennes incarnations des deux côtés, appliquent les réglages, puis rechargent et livrent de nouvelles incarnations. Les ouvertures patient indépendantes sont préservées. Connexion absente, transfert/réconciliation en cours ou scène Quest locale/orpheline non rechargeable imposent un refus avant application/sauvegarde. Les erreurs restent par scène avec reprise ; une capture échouée libère aussi la garde.

Les atlas/MNI installés sont des références locales : identité logique et disponibilité native, **sans comparaison globale de hashes des contenus installés**. L'utilisateur assure leur cohérence. Cette exception ne retire ni provenance lors du chargement FMRI/Volume ni intégrité des données utilisateur transférées. Aucun fichier atlas ne voyage par synchronisation ; le manifeste Android sert à l'installation, pas à l'autorisation runtime.

Les chargements sont séquencés/partagés par atlas, publication après préparation native complète ; Load d'un atlas déjà prêt ne rescane pas le disque. Unload puis Load prend en compte des fichiers changés. Les succès partiels restent disponibles. Unload réserve les deux côtés et attend l'absence d'utilisateurs ; la réservation distante expire après 30 s. Les retries sont bornés à trois tentatives, pauses annulables 500/1000 ms, puis erreur Desktop unique. Déconnexion/annulation abandonne les retries. Les boutons agissent immédiatement et Cancel de la fenêtre ne les annule pas ; les flags auto-load s'appliquent au démarrage/nouvelle session, pas à chaque sauvegarde.
