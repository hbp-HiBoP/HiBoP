# HiBoP XR — spécification produit

**Version :** 0.3
**Cible V1 :** Meta Quest 3  
**Statut :** normatif pour les fonctions ; les enveloppes de performance restent soumises aux spikes

## 1. Vision

HiBoP XR étend dans l'espace les visualisations ouvertes dans HiBoP Desktop. Le chercheur garde le PC pour le chargement des projets, les données, les paramètres avancés et les panels 2D ; le Quest affiche et manipule localement les cerveaux, sites, coupes, ROI et résultats dynamiques.

Ce n'est ni un miroir vidéo ni un HiBoP complet embarqué. Le casque ne devient jamais l'autorité scientifique et ne conserve aucune donnée patient après la session.

Le rendu local Quest est la baseline V1, à réévaluer après un prototype end-to-end pseudo-fonctionnel. Ce choix protège l'expérience phare : inspecter un cerveau agrandi dans les moindres détails en tournant autour de lui, avec une perspective et des interactions qui suivent immédiatement la tête et les mains. Il n'impose pas que les calculs scientifiques soient locaux. Aucun second chemin de rendu distant n'est développé en parallèle avant ce gate.

Ordre de priorité produit en cas de compromis :

1. fluidité et absence d'inconfort ;
2. exactitude scientifique ;
3. précision des interactions ;
4. disponibilité de toutes les données demandées ;
5. qualité esthétique ;
6. vitesse du chargement initial ;
7. consommation mémoire, sous réserve de rester dans une enveloppe sûre.

## 2. Utilisateurs et contexte

Utilisateur principal : chercheur ou ingénieur formé à HiBoP, en laboratoire, devant ou à proximité du poste hôte, sur réseau local. Les usages cliniques, le diagnostic, la décision thérapeutique et le fonctionnement sans supervision ne font pas partie de la V1.

Le produit doit fonctionner assis ou debout, avec lunettes si compatibles avec le casque, par mains ou contrôleurs. Les contrôleurs constituent le mode de référence pour les manipulations demandant de la précision.

## 3. Autorités

| Domaine | Autorité |
| --- | --- |
| projet, patients, datasets | Desktop |
| visualisations, colonnes, paramètres fonctionnels | Desktop |
| timeline canonique et état de lecture | Desktop |
| calculs, projections, coupes exactes | Desktop |
| sélection sémantique d'un site | Desktop après commande XR |
| position/rotation/échelle d'une instance XR | Quest |
| disposition et focus des panels XR | Quest |
| assets/résultats affichés | copie révisionnée de l'état Desktop |

Une réponse Quest ne peut pas modifier directement un calcul. Elle exprime une commande ; le Desktop valide, applique, incrémente l'état et renvoie le résultat canonique.

Le Quest peut toutefois appliquer immédiatement un feedback optimiste local — transformation, survol, sélection visuelle ou index déjà admis en mémoire — puis afficher un état `pending`. Le Desktop reste arbitre ; son dernier état validé gagne et provoque si nécessaire un rollback explicite du Quest.

## 4. Démarrage et connexion

### 4.1 Préconditions

- HiBoP Desktop est ouvert avec un projet chargé ;
- le module XR est présent ; HiBoP démarre l'hôte XR sans demander le lancement manuel d'un second logiciel et indique son état ;
- Quest et PC peuvent se joindre sur le réseau local ;
- les versions négocient un protocole compatible.

### 4.2 Premier appairage

1. Le Quest découvre éventuellement les hôtes locaux.
2. L'utilisateur choisit un hôte ou saisit IP/hostname et port.
3. Le desktop affiche un code court à usage unique.
4. L'utilisateur confirme le code sur le Quest.
5. Le Quest épingle l'identité cryptographique de l'hôte.
6. Le Desktop envoie handshake puis snapshot.

La découverte n'est jamais indispensable. Les erreurs distinguent hôte absent, port bloqué, identité modifiée, code invalide, version incompatible et snapshot en échec.

### 4.3 Connexion suivante

L'endpoint et l'identité appairée peuvent être mémorisés sans données patient. Toute identité cryptographique différente nécessite une nouvelle confirmation explicite.

La V1 supporte une seule relation Desktop–Quest active. La collaboration multi-casque reste hors scope.

## 5. Espace et rendu

- passthrough par défaut lorsque disponible et autorisé ;
- mode VR activable comme repli ;
- environnement, têtes/mains et tracking rendus par le Quest ;
- visualisations HiBoP rendues localement à la fréquence XR ;
- contenu scientifique mis à jour à sa fréquence propre ;
- recentrage sûr sans modifier l'état Desktop ;
- échelle suffisamment large pour l'inspection rapprochée ; aucune limite métier étroite n'est imposée, seulement des bornes techniques de sécurité si les mesures les rendent nécessaires ;
- transformations indépendantes par `BrainInstance`.

Les caméras 3D Desktop peuvent être désactivées pendant une session, sans arrêter calculs ni panels 2D. C'est le fonctionnement recommandé sur les machines modestes, notamment le MacBook Air M2. Un mode développeur pourra ultérieurement garder un miroir vidéo de la vue utilisateur, mais il n'appartient ni au protocole scientifique ni au scope V1.

## 6. Visualisations et cerveaux

Le Quest présente les visualisations ouvertes et compatibles du projet. L'utilisateur peut demander :

- une instance liée à une visualisation ;
- une instance liée à une colonne ;
- plusieurs instances de la même surface à des transformations différentes ;
- plusieurs visualisations simultanées, dont multi-patient et mono-patient.

Chaque instance conserve son ID, sa transformation locale et sa liaison sémantique. Les surfaces immuables identiques sont partagées. Il n'existe pas de maximum codé de cerveaux. Si une nouvelle instance dépasserait le budget de ressources, seule cette création est refusée ; les instances déjà chargées restent utilisables et l'UI explique le refus.

## 7. Représentations

La V1 doit conserver, lorsqu'elles existent dans la visualisation :

- surfaces anatomique et inflated ;
- hémisphères et transparence ;
- sites, couleur, taille, visibilité, sélection et blacklist ;
- atlas, MRI, activité iEEG, fMRI, MEG et CCEP ;
- coupes anatomiques et overlays fonctionnels ;
- ROI sphériques et résultats liés ;
- palettes, seuils, opacité et paramètres de colonne ;
- UI XR pour les manipulations et commandes courantes, y compris timeline et réglages utiles à l'inspection ;
- panels d'information du site sélectionné — graphes, tags, matrices et métadonnées utiles — de très haute priorité V1.

Le chargement de projet/dataset et la configuration scientifique avancée peuvent rester sur le PC. Les noms de patient, libellés de site et noms de colonne nécessaires à la compréhension peuvent être transmis et affichés transitoirement en mémoire sur le Quest ; ils ne sont jamais persistés ni journalisés.

Une fonction non encore supportée est déclarée indisponible avec une raison ; elle n'est jamais remplacée par un rendu approximatif non signalé.

## 8. Sites

Le cas maximal de validation contient 250 patients × 150 sites = 37 500 sites.

Exigences :

- tous les sites compatibles sont visibles, sans plafond logiciel ;
- la sélection ray et la sélection proche renvoient un `siteId` stable ;
- survol et sélection fournissent un feedback local immédiat ;
- la sélection sémantique reste `pending` jusqu'à la réponse Desktop ; un refus déclenche un rollback visible ;
- les couleurs/tailles/visibilités dynamiques d'une frame sont appliquées en groupe ;
- un état filtré explicite est distingué d'une donnée absente.

## 9. Coupes

Le Quest affiche un gizmo manipulable localement. Pendant un geste :

- le plan de contrôle suit immédiatement la main/le contrôleur ;
- les commandes intermédiaires sont coalescées ;
- le Desktop calcule la dernière coupe demandée ;
- les réponses obsolètes ne remplacent jamais une réponse récente ;
- géométrie, contours et textures d'une même révision sont appliqués atomiquement ;
- le dernier résultat canonique converge après la fin du geste.

Une approximation locale peut représenter uniquement le plan/gizmo. Elle ne doit pas être présentée comme une coupe scientifique.

Le Desktop calcule la coupe scientifique et envoie son résultat final. Pour une coupe fixe, les overlays dépendant de la timeline peuvent faire partie du preload admis afin que le changement d'index reste instantané. Une modification du plan invalide ces résultats et déclenche un nouveau calcul/chargement signalé par l'UI.

## 10. Timeline et autoplay

Le Desktop possède l'index canonique, le temps logique, la politique d'échantillonnage et la lecture. Le Quest peut commander lecture, pause, position et vitesse.

Avant d'ouvrir une visualisation temporelle en XR, le Desktop estime puis prépare l'ensemble des données scientifiques nécessaires aux indices demandés. L'admission est fondée sur le coût réel après partage/déduplication des données statiques et compactage des canaux absents, et non sur un nombre maximal d'indices ou de colonnes. La valeur 97 est uniquement un profil de validation P11.

Le budget couvre la mémoire CPU et GPU contrôlée par la timeline, avec une marge de sécurité séparée. Sa valeur effective est le minimum entre la limite validée pour le Quest et une estimation conservatrice de la mémoire disponible au moment du chargement. Une valeur personnalisable peut être exposée dans les réglages XR de HiBoP, sans permettre de dépasser une limite validée comme sûre.

Si l'estimation haute dépasse le budget, le Desktop refuse avant le transfert. Le message indique au minimum mémoire requise/permise, nombre d'indices, nombre de colonnes, principaux contributeurs et explication possible. Il ne propose pas de réduire une plage ou des colonnes dans le flux XR ; ce choix relève éventuellement du protocole scientifique sur Desktop.

Un chargement long est acceptable avec progression, durée estimée et annulation, en réutilisant `LoadingManager`. Il est automatique sous un seuil raisonnable et demande confirmation au-delà. La V1 ne persiste pas ces données scientifiques sur le stockage Quest et ne pagine pas depuis le disque ou le réseau : un dépassement est un refus explicite. Le paging reste une extension future.

Après admission et preload, pour un instant sémantique :

- toutes les colonnes fonctionnelles attendues forment un `DynamicFrameBundle` ;
- surface, sites et overlays de coupe concernés se rapportent aux mêmes révisions ;
- le Quest applique le bundle atomiquement ;
- le Quest affiche un index arbitraire admis au plus tard à la frame XR suivante et synchronise optimistement la commande avec le Desktop ;
- un refus Desktop provoque un rollback explicite vers l'index canonique ;
- le scrubbing privilégie le dernier instant demandé ;
- aucune file de frames anciennes ne s'accumule ;
- la tête et les interactions XR continuent à 72 Hz même si les résultats scientifiques arrivent moins souvent.

La fréquence des données sources n'est ni la fréquence réseau ni la fréquence de rendu.

L'autoplay peut sauter des indices devenus obsolètes pour suivre le temps logique, à condition d'afficher clairement que tous les échantillons n'ont pas été présentés. Les colonnes utilisent les correspondances temporelles canoniques déjà calculées sur le Desktop ; une commande dédiée ne sera ajoutée au protocole que si l'intégration démontre qu'elle est nécessaire.

Une représentation compacte ou avec perte peut devenir automatique uniquement après validation de son équivalence visuelle sur le corpus prévu. Avant cette validation, elle peut être proposée explicitement à l'utilisateur après un refus de budget, avec la nature de la dégradation annoncée.

## 11. Paramètres et scopes

Chaque propriété possède un scope explicite : projet, visualisation, colonne, instance, site, coupe, ROI ou timeline. La spécification du contrat indique source de vérité, persistance, révision et sorties invalidées.

Les transformations XR sont locales. Les réglages scientifiques et d'apparence partagés passent par commande Desktop. Une instance ne doit pas modifier une autre colonne par simple partage involontaire d'un matériau ou mesh.

## 12. Déconnexion et reprise

En cas de coupure :

- le passthrough, le tracking, la perspective et les manipulations spatiales locales continuent sans dépendre du réseau ;
- le Quest conserve le dernier état scientifique cohérent et affiche clairement « déconnecté » ;
- les mises à jour et commandes scientifiques sont gelées, jamais présentées comme confirmées ;
- les données de session restent en mémoire pour une courte tentative de reprise ;
- la reprise V1 demande par défaut un nouveau snapshot complet ; une reprise par deltas peut rester une optimisation négociée si elle est déjà disponible, mais n'est pas une exigence produit ;
- aucun mélange d'epochs ou de révisions n'est autorisé ;
- si la session Desktop a changé, les instances invalides sont fermées et le layout local n'est réappliqué qu'aux IDs encore valides.

Les transformations locales peuvent survivre à cette courte reconnexion dans la session courante. Leur persistance durable entre sessions n'est pas requise en V1.

Une fermeture ou un nouvel epoch purge les payloads patient en mémoire. Le comportement exact pour déconnexion/retry, arrière-plan, timeout, crash et reprise reste une décision de sécurité explicite P14-B ; aucun code de rétention ne doit être écrit avant validation de cette matrice.

## 13. Vie privée, sécurité et logs

- aucune donnée patient persistante sur Quest ;
- aucun nom, chemin de fichier, contenu scientifique ou identifiant direct dans les logs ; les libellés autorisés dans l'UI restent uniquement en mémoire ;
- IDs réseau opaques et propres à la session lorsque possible ;
- transport authentifié et chiffré ;
- appairage explicite et révocable ;
- aucune dépendance cloud pour la session ;
- diagnostics exportables seulement après redaction ;
- le Desktop doit pouvoir couper immédiatement la session.

## 14. Accessibilité et sécurité d'usage

- limites de portée évitant les gestes excessifs ;
- panels lisibles, taille et contraste configurables ;
- action de recentrage et récupération d'objet perdu ;
- feedback distinct pour pending, canonical, stale, erreur et déconnexion ;
- pas de locomotion artificielle nécessaire au scénario nominal ;
- avertissement d'environnement et respect du guardian système ;
- contrôles critiques réalisables avec contrôleurs.

## 15. Hors scope V1

- exécution sans HiBoP Desktop ;
- stockage ou consultation persistante de dossiers patient sur Quest ;
- rendu vidéo du Desktop comme mode principal ;
- collaboration multi-casque ;
- édition complète d'un projet HiBoP ;
- pagination disque/réseau des données de timeline ;
- miroir spectateur de la vue du casque ;
- diagnostic/usage médical ;
- hébergement cloud ;
- port complet de `hbp_core` sur Quest ;
- compatibilité wire avec le prototype HoloLens.

## 16. Critères de réussite produit

La V1 est acceptable si :

1. le parcours appairage → snapshot → cerveau visible est d'abord validé sur Windows x64, puis fonctionne sur macOS Apple Silicon avec un MacBook Air M2 comme minimum de test et sur Ubuntu 24.04 x64 ;
2. anatomical/inflated, sites, coupe et timeline sont fidèles à la baseline Desktop ;
3. les 37 500 sites restent visibles et sélectionnables ;
4. plusieurs visualisations et colonnes dynamiques restent cohérentes ;
5. les contrôleurs couvrent entièrement le scénario V1 et les mains les interactions principales ;
6. la reconnexion par snapshot et les versions incompatibles ne créent aucun état mixte ;
7. aucune donnée patient n'est trouvée sur le stockage Quest après la session ;
8. les gates D20 sont mesurées et soit satisfaites, soit font l'objet d'une décision explicite avant pilote.

Le build HiBoP standard peut contenir un pont et un point d'entrée XR très légers. L'hôte, les assets et dépendances volumineuses restent optionnels, sauf si la mesure démontre qu'une intégration complète n'augmente pas drastiquement la taille du build. Si le module manque, HiBoP affiche une proposition d'installation discrète seulement lorsqu'un canal d'installation fiable existe ; sinon l'entrée XR est masquée. Une mise à jour de compatibilité peut être automatique lorsque le module est déjà installé.
