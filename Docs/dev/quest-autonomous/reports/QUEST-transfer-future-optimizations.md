# Transfert Desktop → Quest — optimisations futures et clôture

Date : 16 septembre 2026. Référence : `visu_full_test / Small`.

## Décision actuelle

**L'utilisateur considère les performances du lot 4 comme acceptables.** Les
optimisations ci-dessous sont consignées pour une reprise éventuelle ; elles
ne sont pas implémentées et ne conditionnent pas la clôture du chantier actuel.
Le **lot 5 : fusion native, retrait de l'instrumentation détaillée et validation
finale** est clôturé, sans nouveau chantier de performance. L'unique envoi final
prend **24,769 s**, sans anomalie signalée. L'écart avec les 20 s du lot 4 est
consigné sans attribution causale dans le [bilan final](QUEST-transfer-final.md).

Le [benchmark du lot 4](QUEST-transfer-result-06-lot-4.md) mesure
**20,027 s puis 20,241 s** du clic à la confirmation Desktop, contre
41,374 s et 41,982 s au lot 3, soit environ 52 % de moins. Les deux livraisons
sont publiées et rendues en stéréo, sans anomalie signalée. Ce jalon Desktop
n'est pas une mesure exacte de la présentation des photons dans le casque.

Les objectifs initiaux plus ambitieux restent des pistes facultatives. Leur
non-atteinte n'est plus un obstacle à l'acceptation de l'état actuel.

## Coûts résiduels mesurés

| Poste | Premier envoi | Second envoi |
|---|---:|---:|
| Réception Quest, décodage et vérification chevauchés | 7,993 s | 9,006 s |
| Métadonnées et validation après réception | 4,621 s | 4,574 s |
| Dont désérialisation JSON/buffers | 4,488 s | 4,471 s |
| Dont lectures numériques | 0,836 s | 0,856 s |
| Restauration de scène | 4,159 s | 3,637 s |
| Dont chargement natif des trois IRM | 2,350 s | 2,440 s |
| Snapshot sur Unity Desktop | 1,574 s | 1,266 s |

Les sous-postes sont inclus dans leurs parents ; les lignes ne s'additionnent
pas toutes. La durée entre fin de réception et début du reçu Quest est de
8,793/8,386 s. La réception n'est pas une mesure isolée de la radio Wi-Fi.

Pendant l'envoi, le plus grand intervalle entre frames atteint 1,590/1,272 s
Desktop et 0,501/0,511 s Quest. La préparation initiale à Pair reste distincte :
9,314 s Desktop et 5,371 s Quest. Elle est désormais réactive : intervalles
maximaux de 17,78 ms Desktop et 28,59 ms Quest.

## Ordre recommandé si l'optimisation reprend

### 1. Reconstruire le graphe pendant la réception des IRM

L'ordre HBT4 actuel est : JSON, références globales, index, pack de tableaux,
puis ressources externes. La désérialisation appelle immédiatement les lecteurs
numériques : recevoir seulement le JSON ne suffit pas. En revanche, après
fermeture et vérification du pack, la reconstruction pourrait avancer pendant
la réception des IRM restantes, sans nouveau format de transfert.

Introduire un jalon « métadonnées et pack prêts », attacher le pack fermé en
lecture, puis lancer une reconstruction privée unique. Séparer cette construction
de la validation finale, qui doit encore vérifier toutes les ressources,
l'appairage et le transcript avant publication.

Aujourd'hui, `SceneArchive.Blocks.Complete` attache le pack et scelle l'ensemble,
et `SceneArchive.ReadPrepared` exige `verifiedContent`. Il faut conserver ce
contrat pour les consommateurs finaux et créer un chemin préparatoire explicite,
plutôt que supprimer le garde. Les objets construits restent privés et doivent
être détruits si la réception échoue ; aucun accès à des données Unity vivantes.

**Potentiel :** jusqu'à une partie importante des 4,5 s de JSON masquée par la
réception restante. Le maximum théorique n'est pas un gain acquis : le pack
peut arriver tard et le worker peut concurrencer le décodage, les I/O ou le rendu.
Ajouter fin du pack, début/fin de reconstruction et débit pendant cette fenêtre
aux mesures de la prochaine expérimentation éventuelle.

### 2. Préparer les IRM dès que leurs fichiers sont vérifiés

Préparer chaque volume natif après réception complète, fermeture et vérification
de son fichier, ou de tous les membres d'une paire, pendant l'arrivée des suivants.
Commencer avec un seul worker de préparation pour borner la concurrence et la
mémoire. Adapter au besoin l'ordre d'envoi aux dépendances : l'ordre externe
actuel par hash n'est pas un ordre de préparation scientifique.

Les surfaces dépendant d'une IRM attendent son volume préparé. La scène visible
reste inchangée jusqu'à validation globale et bascule finale. Toute erreur,
annulation ou fermeture doit rejoindre les workers avant libération des fichiers
et des objets natifs.

Le contrat actuel `VerifiedResourceScope.Acquire` exige un scellement global ;
les paires natives sont finalisées à `Complete`. Introduire une propriété
explicite des ressources individuelles vérifiées et de leurs lecteurs, sans
affaiblir le contrat existant. Protéger aussi les structures partagées :
`nativePaths` est actuellement un dictionnaire ordinaire, impropre à une lecture
concurrente avec ses écritures.

**Potentiel :** masquer une partie des 2,35–2,44 s de chargement natif. La dernière
IRM ne bénéficie pas nécessairement d'une réception restante suffisante ; ce
gain ne s'ajoute pas intégralement à celui du JSON.

### 3. Réduire le coût du graphe et du snapshot

Si la reconstruction reste sur le chemin critique ou ralentit le transport,
remplacer progressivement les parties les plus coûteuses de la sérialisation
généraliste par un schéma explicite de transfert : tables d'objets, identifiants
et références de buffers. Le format des projets sur disque reste distinct.

Cibler les familles d'objets à partir de compteurs de durée et de volume.
Optimiser uniquement les lectures du pack est insuffisant : elles représentent
moins d'une seconde dans les 4,5 s de désérialisation. Réduire aussi l'arbre JSON
intermédiaire créé par `DetachedSceneMetadata` pour diminuer les allocations et
le parcours synchrone Desktop.

Préserver les références globales canoniques, les identités internes du graphe,
l'indépendance des colonnes et toutes les modalités. Un snapshot étalé sur
plusieurs frames exige un contrat de cohérence face aux mutations ; ajouter des
attentes au milieu de la lecture d'une scène vivante ne suffit pas.

### 4. Réduire les pauses de publication

Mesurer puis répartir les uploads et opérations Unity qui peuvent être étalés,
en gardant une publication cohérente de la scène complète. Distinguer amélioration
de fluidité et réduction du délai total : répartir une opération sur plusieurs
frames peut améliorer la première sans améliorer le second.

### 5. Envisager un cache natif pour les renvois

Les hits actuels des surfaces n'évitent pas tout upload ni le chargement des IRM.
Un cache de ressources natives immuables pourrait aider les renvois, avec clés
d'identité, invalidation, budget, éviction et propriété des handles explicites.
Ne pas partager des objets natifs mutables entre scènes sans contrat adapté.

Cette piste vient après les gains du premier envoi : les pics RSS échantillonnés
Quest sont déjà de 1 476 puis 2 169 Mio, suivi après confirmation compris. Deux
essais ne permettent pas de conclure sur une fuite ou un plateau mémoire. Un
cache ne doit pas transformer un gain de latence en pression mémoire excessive.

## Objectifs éventuels et validation

Une cible de travail de **12–15 s pour un envoi complet** pourrait être étudiée
si ces travaux reprennent, dans les conditions réseau observées. Ce n'est ni
une promesse ni un nouveau critère d'acceptation. Un premier incrément limité
au JSON pourrait viser environ 16–18 s, à confirmer selon le chevauchement réel.
Ne pas additionner les économies théoriques des étapes concurrentes.

Regrouper les changements cohérents, tester automatiquement parité, corruption,
annulation, fermeture et remplacement avant de solliciter le casque. Une reprise
des optimisations justifierait alors une validation physique ciblée, comprenant
les fins de réception par ressource, le travail restant après le dernier octet
et la mémoire. Aucun nouvel essai n'est demandé pour cette simple consignation.

## Périmètre du lot 5

Le lot 5 n'est **pas uniquement** une suppression de logs. Il clôt le chantier
sur les performances acceptées du lot 4 :

1. **Retirer l'instrumentation temporaire de production** : scopes détaillés,
   échantillonnages CPU/mémoire/frames, traces de benchmark et autotests de mesure
   au démarrage. Identifier leurs dépendances avant retrait. Conserver les tests
   automatisés, les rapports et les preuves brutes déjà collectées.
2. **Conserver les fonctions de production** : codecs, caches, workers, pipeline,
   contrôles d'intégrité, annulation et propriété des ressources. Garder les
   erreurs utiles au diagnostic ; une nouvelle télémétrie légère est facultative,
   pas un prérequis de clôture.
3. **Vérifier le résultat nettoyé** : formatage, compilation, tests de régression
   pertinents, builds Release Windows et Quest et vérification de leur contenu.
4. **Faire une validation physique finale proportionnée**, sur Small avec les
   mêmes conditions, pour confirmer fonctionnement et délai sans instrumentation
   détaillée. Un seul passage prévu sauf anomalie ; définir un repère de durée
   global minimal ou une mesure externe et en annoncer la précision. Ne pas
   prétendre conserver la décomposition fine une fois ses sondes retirées.
5. **Finaliser le bilan avant/après**, identifier les builds livrés et consigner
   les limites acceptées ainsi que les pistes reportées dans ce document.

Le lot 5 ne comprend pas les optimisations futures décrites plus haut.
Mise à jour de clôture : fusion native et retrait des sondes réalisés, tests et
builds Release validés, unique essai physique réussi fonctionnellement en
24,769 s. Voir le [bilan final](QUEST-transfer-final.md), qui précise la limite
de comparaison avec le lot 4. Les décompositions précédentes restent les mesures
des players instrumentés du lot 4 ; aucune nouvelle décomposition n'est disponible.

Références : [plan initial et suivi](QUEST-transfer-optimization-plan.md),
[implémentation du lot 4](QUEST-transfer-lot-4.md),
[mesures et preuves](QUEST-transfer-result-06-lot-4.md).
