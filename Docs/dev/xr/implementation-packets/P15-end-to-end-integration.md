# P15 — intégration produit Desktop/Quest end-to-end

## Objectif et résultat observable

Raccorder les briques P06–P14 en un parcours de production réel : depuis une visualisation ouverte dans HiBoP Desktop, l'utilisateur active la session XR selon le parcours décidé, un Quest appairé reçoit l'état et les assets par le transport réel, affiche les représentations retenues et renvoie des commandes que le Desktop valide avant de republier l'état canonique.

Le résultat observable n'est ni une scène de démonstration, ni une injection synthétique, ni une juxtaposition de tests : il s'agit d'une tranche verticale exécutée avec les applications Desktop/XR, le sidecar et le réseau effectivement destinés au pilote.

## Decision gate

**Hérité :** Desktop autoritaire, rendu Quest local, D10/D11 transport provisoire Windows/Quest, D12/D18 session et compatibilité, P08 cache mémoire, P09 bindings, P10 sites, P11 timeline, P12 coupes, P13 UX et P14 sécurité.

**À résoudre avant raccordement production :**

- `P15-A` : action exacte d'activation/désactivation de HiBoP XR, emplacement dans les interfaces Desktop/Quest et états visibles associés ;
- `P15-B` : règle exacte de sélection du contenu envoyé — visualisation, colonne, ensemble ouvert ou autre unité — et autorité de création/fermeture des `BrainInstance` ;
- `P15-C` : cycle de vie du host et du sidecar, supervision, consentement d'exposition LAN, port/firewall, arrêt et récupération après échec ;
- `P15-D` : construction du snapshot réel, mapping des modèles HiBoP, traitement des fonctions incompatibles et comportement lors d'ouverture/fermeture d'une visualisation ;
- `P15-E` : commandes et retours canoniques requis pour prouver la communication bidirectionnelle, ainsi que comportement du rendu Desktop pendant la session ;
- `P15-F` : comportement visible en connexion, appairage, synchronisation, pression mémoire, déconnexion, reprise, incompatibilité et erreur partielle ;
- `P15-G` : scénario vertical minimal signé, représentations obligatoires, datasets autorisés et propriétaires produit/scientifique qui acceptent le résultat.

Un bouton, une entrée de menu, une demande depuis le Quest, une synchronisation automatique ou toute combinaison de ces mécanismes sont des options à décider ; aucun parcours n'est déduit d'un exemple ou du prototype HoloLens. Tant que P15-A–G ne sont pas acceptées, rester en `DECISION_ONLY` et ne promouvoir aucun code de spike vers la production.

## Périmètre autorisé

- UI et orchestration nécessaires au démarrage/arrêt de la session XR dans les deux shells ;
- lancement, supervision et arrêt transparents du sidecar P06 ;
- adaptateurs de transport production reliant P06 au cœur de session P07 ;
- projection du vrai état HiBoP vers snapshot/deltas et assets P03/P08 ;
- orchestration Quest reliant client, cache, instances, sites, timeline, coupes et UX ;
- diagnostics et erreurs end-to-end conformes à P14 ;
- tests d'intégration et scénarios appareil avec réseau local réel.

## Hors périmètre

- nouvelle fonction scientifique ou modification des algorithmes HiBoP ;
- port de `hbp_core` sur Quest sans déclenchement PX2 ;
- choix implicite d'une fonction P13 non signée ;
- renommage transversal, réorganisation générale ou suppression de prototypes, réservés à P16/P17 ;
- signature, upload Meta et distribution pilote, réservés à P18 ;
- multi-client ou collaboration multi-casque.

## Hypothèses fixées

- le Desktop reste l'autorité du projet, des calculs et de l'état fonctionnel ;
- le Quest ne reçoit que contrats, assets de rendu et résultats post-projection autorisés ;
- transformations et disposition ordinaires restent locales au Quest ;
- aucun payload patient n'est persisté sur le Quest ;
- le transport de production reste derrière des interfaces remplaçables ;
- aucun test synthétique seul ne peut fermer P15.

## Dépendances et état initial

- P11 et P12 intégrés avec leurs gates de performance/parité ;
- P13-A–F acceptées et UX XR disponible ;
- P14-A–F acceptées pour le lifecycle réellement exercé ;
- baseline P06 exécutable sur les plateformes déclarées par P15-G ;
- visualisation HiBoP et dataset P15-G accessibles sans copier de donnée patient ;
- Quest 3 autorisé, réseau local et état du firewall confirmés.

## Fichiers/modules pressentis

- Desktop : adaptateur d'état réel, host, supervision sidecar et point d'entrée UI sous `Assets/` ;
- packages `Contracts`, `RenderModel` et `Protocol` pour les interfaces/mappings strictement nécessaires ;
- Quest : client transport, composition de session et scène/prefab produit sous `XR/Assets/` ;
- scripts de lancement et de test local, sans secret ni donnée patient ;
- documentation opérateur et rapport end-to-end sous `Docs/dev/xr/`.

## Étapes

1. Résoudre P15-A–G et archiver le scénario attendu.
2. Inventorier les frontières entre code de spike, cœur transport-neutral et code de production à écrire ou promouvoir.
3. Raccorder le lifecycle Desktop au sidecar et vérifier arrêt, crash et absence de processus orphelin.
4. Raccorder HTTPS/WSS réels à P07, avec appairage, handshake, heartbeat, snapshot, deltas et reprise.
5. Construire le snapshot depuis une vraie visualisation HiBoP et transférer les assets statiques vers P08/P09.
6. Raccorder sites, timeline et coupes conformément au scénario P15-G, sans injection synthétique dans le chemin produit.
7. Raccorder au moins une commande Quest → Desktop → état/résultat canonique → Quest et prouver sa corrélation.
8. Exécuter erreurs, reconnexion, fermeture, changement d'epoch et pression mémoire sur la tranche réelle.
9. Mesurer le parcours sur réseau local réel et documenter les fonctions explicitement indisponibles.

## Tests et commandes

- tests unitaires des adaptateurs et du lifecycle sidecar ;
- intégration host/client sur processus et sockets réels, pas seulement en mémoire ;
- premier appairage, identité changée, second client, reprise et nouvel epoch ;
- transfert HTTPS d'une vraie surface, validation du hash et partage entre instances ;
- snapshot/deltas d'une vraie visualisation avec ouverture/fermeture en cours de session ;
- sélection de site et au moins une autre commande canonique avec aller-retour complet ;
- timeline/coupe selon P15-G, avec révisions et rejets stale ;
- build Desktop et APK Quest puis exécution physique sur réseau local ;
- scan stockage/logs/processus après fermeture ;
- scénarios E01–E11 applicables du plan de validation.

## Critères de sortie binaires

- [ ] P15-A–G acceptées et scénario vertical signé ;
- [ ] l'activation/désactivation utilise uniquement le parcours produit décidé ;
- [ ] le sidecar est démarré, supervisé et arrêté sans interaction ou processus résiduel non prévu ;
- [ ] le Quest s'appaire et se synchronise par HTTPS/WSS de production ;
- [ ] une vraie visualisation HiBoP devient une `BrainInstance` sans source synthétique dans le chemin produit ;
- [ ] surface et sites réels transitent, sont validés et restent cohérents avec leurs révisions ;
- [ ] les fonctions dynamiques P15-G convergent sans état mixte ni backlog non borné ;
- [ ] au moins une interaction Quest est validée par le Desktop et revient comme état ou résultat canonique ;
- [ ] déconnexion/reprise/fermeture respectent P14 et ne laissent ni donnée persistée ni processus orphelin ;
- [ ] le parcours est réussi sur un Quest 3 physique et ne dépend d'aucune scène de démonstration Pxx.

## Artefacts à remettre

Intégration Desktop/sidecar/Quest, point d'entrée produit, scène et prefabs canoniques candidats, tests sockets/appareil, rapport end-to-end avec mesures et ADR P15. Les données, builds et captures sensibles restent hors Git sous `.artifacts/xr/`.

## Conditions d'arrêt

Arrêter si P15-A–G sont ambiguës, si le transport réel exige d'affaiblir P06/P14, si une fonction requiert un contrat scientifique non décidé, si le chemin produit dépend encore d'une injection synthétique ou avant toute exposition réseau/action externe non autorisée.

## Prompt de démarrage

> Exécute P15 depuis `Docs/dev/xr/implementation-packets/P15-end-to-end-integration.md`. Commence en DECISION_ONLY et fais accepter P15-A–G sans déduire l'UX d'un exemple. Raccorde ensuite les vraies applications Desktop et Quest au transport P06, à la session P07 et aux fonctions P08–P14. Le gate exige une visualisation HiBoP réelle, un Quest physique et une commande aller-retour canonique ; aucune scène ou session synthétique seule ne peut fermer P15.
