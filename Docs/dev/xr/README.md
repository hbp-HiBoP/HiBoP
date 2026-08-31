# HiBoP XR — dossier de spécification

**Version :** 0.2  
**Date de consolidation :** 31 août 2026  
**Périmètre :** extension mixed reality autonome sur Meta Quest 3, connectée à HiBoP Desktop  
**Statut :** spécification consolidée après audit du prototype HoloLens ; revue humaine requise avant implémentation

## Résumé exécutable

HiBoP XR est une application Android autonome qui rend localement sur Quest 3 les visualisations produites par un HiBoP Desktop déjà ouvert. Elle ne diffuse pas l'écran du PC. Le desktop reste l'autorité sur le projet, les données patient, les calculs scientifiques et l'état fonctionnel ; le Quest possède la disposition spatiale et l'interaction XR.

La baseline V1 est :

- deux projets Unity isolés, dans un même monorepo applicatif ;
- packages UPM partagés pour contrats, modèle de rendu, protocole et code réellement portable ;
- `hbp_core` et les calculs canoniques sur le desktop ;
- rendu Quest local à partir d'assets immuables et de résultats post-projection ;
- HTTPS pour les gros assets et WebSocket sécurisé pour contrôle, état et résultats dynamiques, sous réserve d'un spike IL2CPP/3 OS pour la bibliothèque ;
- OpenXR, XR Interaction Toolkit et Input System dans le projet Quest ; extensions Meta isolées pour le passthrough et seulement lorsque nécessaires ;
- aucun cache persistant de données patient sur le Quest ;
- aucun plafond logiciel arbitraire sur le nombre de sites, colonnes ou cerveaux.

Le prototype HoloLens ne validait pas cette architecture. Il exécutait une copie complète de HiBoP sur le PC et utilisait Microsoft OpenXR App Remoting pour transmettre vidéo, audio et entrées. Il ne contenait aucun protocole applicatif, client ARM autonome, cache Quest, snapshot, révision ou reprise de session réutilisable.

## Invariants produit

1. Quest 3 est la cible V1 ; passthrough par défaut, mode VR de repli.
2. HiBoP Desktop doit être ouvert et reste utilisable pour ses panels 2D.
3. Les résultats scientifiques affichés sont canoniques et proviennent du desktop.
4. Les transformations spatiales des cerveaux et panels sont locales au Quest.
5. Plusieurs visualisations et plusieurs cerveaux peuvent coexister.
6. L'autoplay fait avancer atomiquement toutes les colonnes fonctionnelles concernées.
7. Le cas de référence maximal comprend environ 37 500 sites, tous affichables et sélectionnables.
8. Mains et contrôleurs sont tous deux supportés ; les contrôleurs servent de référence de précision.
9. Les données patient ne sont ni persistées ni journalisées sur le Quest.
10. Le produit est un outil de recherche et de visualisation, non un dispositif médical.

## Documents normatifs

| Document | Rôle |
| --- | --- |
| [01-product-specification.md](01-product-specification.md) | comportement produit, scénarios et exigences |
| [02-technical-architecture.md](02-technical-architecture.md) | frontières Desktop/Quest, topologie et calcul |
| [03-protocol-and-render-contract.md](03-protocol-and-render-contract.md) | transport logique, état, révisions et payloads |
| [04-feasibility-risks-and-spikes.md](04-feasibility-risks-and-spikes.md) | preuves, risques et expériences de fermeture |
| [05-implementation-roadmap.md](05-implementation-roadmap.md) | ordre d'exécution et gates |
| [06-hololens-audit-checklist.md](06-hololens-audit-checklist.md) | résultat et contrôles de l'audit historique |
| [07-validation-plan.md](07-validation-plan.md) | validation fonctionnelle, scientifique et performance |
| [08-decision-register.md](08-decision-register.md) | décisions D01–D20 et statut de preuve |
| [implementation-packets/README.md](implementation-packets/README.md) | paquets P00–P15/PX1/PX2 prêts pour des chats distincts |
| [SOURCES.md](SOURCES.md) | sources locales et officielles |

Les constats publiables du prototype sont sous [hololens](hololens/architecture.md). Les notes détaillées, commandes et preuves sensibles restent volontairement non suivies dans `.codex-temp/hibop-xr-hololens-audit/`.

## Portée des statuts

- `RESOLVED` : choix architectural arrêté ; l'implémentation reste à valider par ses gates.
- `PROVISIONAL` : baseline choisie, dépendante d'une mesure ou d'une contrainte externe.
- `REQUIRES_SPIKE` : aucune implémentation ne doit être généralisée avant l'expérience définie.
- `BLOCKED` : information externe indispensable absente.

Une décision `RESOLVED` ne transforme pas une estimation en preuve. Les chiffres historiques et les calculs de payload sont distingués des mesures Quest dans tous les documents.

Avant toute phase d'implémentation, le paquet correspondant impose un `Decision gate`. Une décision architecturale, scientifique, produit, sécurité ou externe manquante interdit les modifications de production jusqu'à sa résolution et son enregistrement explicites.

## Ce qui n'est pas autorisé

- reprendre App Remoting comme architecture produit ;
- copier `HBP.Core`, `HBP.Data`, scènes ou prefabs entre projets ;
- envoyer des pixels vidéo à la place des données de rendu ;
- recalculer silencieusement un résultat scientifique approximatif sur le Quest ;
- créer un GameObject, MeshRenderer ou collider par site dans le chemin de production ;
- accumuler des frames de timeline ou de coupe obsolètes ;
- persister des données patient sur le casque ;
- réduire le périmètre fonctionnel par un plafond codé afin de faire passer un benchmark.

## Baselines auditées

| Composant | Révision observée |
| --- | --- |
| HiBoP Desktop | `83a52e4e`, branche `develop`, Unity `6000.5.2f1` |
| `hbp_core` | `cf4400bf`, branche `develop`, tag `0.3.1` |
| prototype HoloLens | `5a119948`, branche `master`, Unity `2021.3.13f1` |

Ces identifiants rendent l'audit reproductible ; ils ne constituent pas une matrice de versions supportées pour la future V1.
