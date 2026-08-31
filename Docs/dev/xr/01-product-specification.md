# HiBoP XR — spécification produit

**Version :** 0.2  
**Cible V1 :** Meta Quest 3  
**Statut :** normatif pour les fonctions ; les enveloppes de performance restent soumises aux spikes

## 1. Vision

HiBoP XR étend dans l'espace les visualisations ouvertes dans HiBoP Desktop. Le chercheur garde le PC pour le chargement des projets, les données, les paramètres avancés et les panels 2D ; le Quest affiche et manipule localement les cerveaux, sites, coupes, ROI et résultats dynamiques.

Ce n'est ni un miroir vidéo ni un HiBoP complet embarqué. Le casque ne devient jamais l'autorité scientifique et ne conserve aucune donnée patient après la session.

## 2. Utilisateurs et contexte

Utilisateur principal : chercheur ou ingénieur formé à HiBoP, en laboratoire, devant ou à proximité du poste hôte, sur réseau local. Les usages cliniques, le diagnostic, la décision thérapeutique et le fonctionnement sans supervision ne font pas partie de la V1.

Le produit doit fonctionner assis ou debout, avec lunettes si compatibles avec le casque, par mains ou contrôleurs. Les contrôleurs constituent le mode de référence pour les manipulations demandant de la précision.

## 3. Autorités

| Domaine | Autorité |
| --- | --- |
| projet, patients, datasets | Desktop |
| visualisations, colonnes, paramètres fonctionnels | Desktop |
| timeline et état de lecture | Desktop |
| calculs, projections, coupes exactes | Desktop |
| sélection sémantique d'un site | Desktop après commande XR |
| position/rotation/échelle d'une instance XR | Quest |
| disposition et focus des panels XR | Quest |
| assets/résultats affichés | copie révisionnée de l'état Desktop |

Une réponse Quest ne peut pas modifier directement un calcul. Elle exprime une commande ; le Desktop valide, applique, incrémente l'état et renvoie le résultat canonique.

## 4. Démarrage et connexion

### 4.1 Préconditions

- HiBoP Desktop est ouvert avec un projet chargé ;
- l'hôte XR est démarré explicitement et indique le port/état ;
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

## 5. Espace et rendu

- passthrough par défaut lorsque disponible et autorisé ;
- mode VR activable comme repli ;
- environnement, têtes/mains et tracking rendus par le Quest ;
- visualisations HiBoP rendues localement à la fréquence XR ;
- contenu scientifique mis à jour à sa fréquence propre ;
- recentrage sûr sans modifier l'état Desktop ;
- échelle utilisable de l'ordre de 10 cm à 2 m ;
- transformations indépendantes par `BrainInstance`.

Les caméras 3D Desktop peuvent être désactivées pendant une session, sans arrêter calculs ni panels 2D. Un mode développeur peut garder un miroir, mais il n'appartient pas au protocole scientifique.

## 6. Visualisations et cerveaux

Le Quest présente les visualisations ouvertes et compatibles du projet. L'utilisateur peut demander :

- une instance liée à une visualisation ;
- une instance liée à une colonne ;
- plusieurs instances de la même surface à des transformations différentes ;
- plusieurs visualisations simultanées, dont multi-patient et mono-patient.

Chaque instance conserve son ID, sa transformation locale et sa liaison sémantique. Les surfaces immuables identiques sont partagées. Il n'existe pas de maximum codé de cerveaux ; l'UI avertit en cas de pression de ressources sans masquer arbitrairement des données.

## 7. Représentations

La V1 doit conserver, lorsqu'elles existent dans la visualisation :

- surfaces anatomique et inflated ;
- hémisphères et transparence ;
- sites, couleur, taille, visibilité, sélection et blacklist ;
- atlas, MRI, activité iEEG, fMRI, MEG et CCEP ;
- coupes anatomiques et overlays fonctionnels ;
- ROI sphériques et résultats liés ;
- palettes, seuils, opacité et paramètres de colonne ;
- panels d'information essentiels et matrices explicitement retenues dans le scope V1.

Une fonction non encore supportée est déclarée indisponible avec une raison ; elle n'est jamais remplacée par un rendu approximatif non signalé.

## 8. Sites

Le cas maximal de validation contient 250 patients × 150 sites = 37 500 sites.

Exigences :

- tous les sites compatibles sont visibles, sans plafond logiciel ;
- la sélection ray et la sélection proche renvoient un `siteId` stable ;
- survol et sélection fournissent un feedback local immédiat ;
- la sélection sémantique n'est confirmée qu'après réponse Desktop ;
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

## 10. Timeline et autoplay

Le Desktop possède l'index, le temps logique, la politique d'échantillonnage et la lecture. Le Quest peut commander lecture, pause, position et vitesse.

Pour un instant sémantique :

- toutes les colonnes fonctionnelles attendues forment un `DynamicFrameBundle` ;
- surface, sites et overlays de coupe concernés se rapportent aux mêmes révisions ;
- le Quest applique le bundle atomiquement ;
- le scrubbing privilégie le dernier instant demandé ;
- aucune file de frames anciennes ne s'accumule ;
- la tête et les interactions XR continuent à 72 Hz même si les résultats scientifiques arrivent moins souvent.

La fréquence des données sources n'est ni la fréquence réseau ni la fréquence de rendu.

## 11. Paramètres et scopes

Chaque propriété possède un scope explicite : projet, visualisation, colonne, instance, site, coupe, ROI ou timeline. La spécification du contrat indique source de vérité, persistance, révision et sorties invalidées.

Les transformations XR sont locales. Les réglages scientifiques et d'apparence partagés passent par commande Desktop. Une instance ne doit pas modifier une autre colonne par simple partage involontaire d'un matériau ou mesh.

## 12. Déconnexion et reprise

En cas de coupure :

- le Quest gèle le dernier état cohérent et affiche clairement « déconnecté » ;
- les commandes scientifiques sont désactivées ou mises en attente selon leur idempotence, jamais appliquées localement comme si elles étaient confirmées ;
- les données de session restent en mémoire pour une courte tentative de reprise ;
- la reprise applique deltas ou snapshot complet ;
- aucun mélange d'epochs ou de révisions n'est autorisé ;
- si la session Desktop a changé, les instances invalides sont fermées et le layout local n'est réappliqué qu'aux IDs encore valides.

Une fermeture ou un nouvel epoch purge les payloads patient en mémoire. Le comportement exact pour déconnexion/retry, arrière-plan, timeout, crash et reprise reste une décision de sécurité explicite P14-B ; aucun code de rétention ne doit être écrit avant validation de cette matrice.

## 13. Vie privée, sécurité et logs

- aucune donnée patient persistante sur Quest ;
- aucun nom, chemin de fichier, contenu scientifique ou identifiant direct dans les logs ;
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
- diagnostic/usage médical ;
- hébergement cloud ;
- port complet de `hbp_core` sur Quest ;
- compatibilité wire avec le prototype HoloLens.

## 16. Critères de réussite produit

La V1 est acceptable si :

1. le parcours appairage → snapshot → cerveau visible fonctionne sur les trois OS hôtes ;
2. anatomical/inflated, sites, coupe et timeline sont fidèles à la baseline Desktop ;
3. les 37 500 sites restent visibles et sélectionnables ;
4. plusieurs visualisations et colonnes dynamiques restent cohérentes ;
5. les gestes mains et contrôleurs couvrent le scénario V1 ;
6. reconnexion et versions incompatibles ne créent aucun état mixte ;
7. aucune donnée patient n'est trouvée sur le stockage Quest après la session ;
8. les gates D20 sont mesurées et soit satisfaites, soit font l'objet d'une décision explicite avant pilote.
