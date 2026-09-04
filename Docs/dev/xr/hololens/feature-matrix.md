# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — matrice fonctionnelle

> **Mise à jour produit du 4 septembre 2026 :** la colonne « Décision Quest » conserve la trace de la spécification 0.2. D21 rend le choix du renderer local provisoire jusqu'à la revue E11 ; les faits observés sur le prototype HoloLens restent inchangés.

| Fonction | Prototype | Preuve/limite | Décision Quest |
| --- | --- | --- | --- |
| connexion casque | oui | App Remoting IP/port/codec | remplacer par appairage HTTPS/WSS |
| client autonome | non | aucun Player UWP/ARM, HiBoP tourne sur PC | créer projet Android Quest |
| passthrough | dépend du runtime HoloLens | aucune abstraction produit | implémenter via capacité Meta isolée |
| plusieurs visualisations | oui | liste de scènes 3D | conserver le comportement |
| plusieurs cerveaux/colonnes | oui | colonnes et modes d'affichage | conserver sans plafond fixe |
| anatomical/inflated | oui | renderer HiBoP copié | adapter au RenderModel partagé |
| manipulation spatiale | oui | MRTK `ObjectManipulator` | réécrire XRI/OpenXR |
| mains | oui | joints MRTK et pinches spécifiques | adapter aux actions mains |
| contrôleurs | non démontré | code centré HoloLens/mains | obligatoire V1 |
| sélection de site | oui | scan O(N) proche ; objets par site | réécrire buffers + index spatial |
| 37 500 sites | non prouvé | aucune mesure ; architecture non scalable | spike/gate explicite |
| coupes exactes | oui dans le processus PC | aucun réseau métier | calcul Desktop distant latest-wins |
| timeline | oui localement | événements/processus unique | bundle atomique multi-colonnes |
| ROI sphériques | oui | sphères MRTK | adapter le concept |
| paramètres de colonnes | oui | état implicite local | formaliser scopes et révisions |
| panels/matrices | partiel/historique | pas de contrat distant | V1 selon spécification produit |
| reconnexion sémantique | non | aucun miroir casque | snapshot + resync |
| versioning protocole | non | aucun protocole | handshake obligatoire |
| cache patient casque | non | conséquence du flux vidéo | maintenir comme invariant explicite |
| rendu local casque | non | images rendues sur PC | baseline provisoire, revue après E11 |
| builds 3 OS hôte | script standalone présent | remoting réellement Windows/HoloLens non prouvé 3 OS | valider serveur sur 3 OS |

## Classification

| Domaine | Réutiliser | Adapter | Réécrire | Abandonner |
| --- | --- | --- | --- | --- |
| intention desktop autoritaire | oui | — | — | — |
| concepts multi-scènes/colonnes | — | oui | — | — |
| logique scientifique | via HiBoP courant | oui | — | — |
| shaders/assets | après audit de portabilité | oui | si non Android | — |
| interactions MRTK | — | concepts seulement | implémentation | — |
| sites GameObjects | — | IDs/états | renderer/picking | objets par site |
| App Remoting | — | — | protocole de données | flux vidéo |
| fork complet HiBoP | — | — | packages communs | copie |
| limite de cinq vues | — | — | capacité mesurée | plafond fixe |

L'absence de mesure est indiquée comme telle. Aucun comportement fluide observé dans un processus PC/HoloLens ne constitue une preuve de performance Quest autonome.
