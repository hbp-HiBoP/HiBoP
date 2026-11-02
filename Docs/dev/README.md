# Développement HiBoP

Références techniques du checkout `feature/xr-autonomous`, consolidées le
2 novembre 2026 à partir du code, des tests et des preuves disponibles. Commencer
par [l'architecture](architecture.md), puis choisir le domaine concerné.

| Domaine | Référence et questions couvertes |
|---|---|
| Chargement et persistance | [Pipeline](loading/loading-and-persistence.md) : archives, JSON, liaison, disponibilité, validation et concurrence |
| Données scientifiques | [Séries temporelles](loading/timeseries.md) : époques, statistiques, grilles, buffers et mémoire |
| Métadonnées | [Tags](loading/tags.md) : import, types, enums, migration et récupération |
| Performance du chargement | [Mesures et reproduction](loading/performance.md), avec configurations et limites des comparaisons |
| Projection | [Calcul et restitution](activity-projection.md), calibration, couverture et propriété des ressources |
| Géométrie | [Repères natifs/Unity](hbp_core_coordinate_system.md), [meshes et coupes](mesh-generation-and-cuts.md), [inflation](surface-inflation.md) |
| Rendu | [URP et rendu scientifique](rendering/scientific-rendering.md), [validation visuelle](rendering/rendering-validation.md) |
| Quest | [Architecture, transfert, interactions et déploiement](quest-autonomous/README.md) |
| Synchronisation | [État partagé et reconnexion](quest-autonomous/sync/synchronization-architecture.md), [opérations](quest-autonomous/sync/synchronized-operations.md), [protocole](quest-autonomous/sync/wire-protocol.md) |
| Validation | [Tests et preuves](testing.md), puis les recettes propres au domaine |

## Lire les références

Les descriptions d'implémentation se rapportent au code vérifié lors de cette
consolidation. Les résultats datés qualifient uniquement leurs sources, binaires,
fixtures et appareils. La présence d'un test n'atteste pas son exécution récente;
une comparaison numérique ne vaut pas validation visuelle ni qualification de
toutes les plateformes. Aucun build Unity ni essai matériel n'a été lancé pour
cette migration documentaire.

Les limites se trouvent auprès du contrat concerné : mémoire et grilles dans
les séries temporelles, compatibilité dans la persistance, transport et reprise
dans Quest/sync, couverture scientifique et matérielle dans les références de
validation. Les anciennes suggestions d'audit ne constituent pas un backlog actif.

## Instructions, procédures et preuves

[AGENTS.md](../../AGENTS.md) porte les invariants du dépôt;
[agent-knowledge](../agent-knowledge/INDEX.md) fournit les conventions opérationnelles
autonomes. Les skills existants portent les procédures d'exécution, notamment
[la validation Unity](../../.agents/skills/hibop-unity-validation/SKILL.md) et
[l'édition UI](../../.agents/skills/hibop-ui-authoring/SKILL.md). Les présentes
références expliquent les sous-systèmes et leurs décisions techniques.

Les références géométriques d’inflation sont regroupées avec leurs outils dans
[Tools/InflationReference](../../Tools/InflationReference/reference-corpus.md).
Les résultats Quest retenus sont regroupés par sujet dans
[Validation/Quest](../../Validation/Quest/retained-results.md).
Les [fixtures Quest](../../Validation/Quest/retained-results.md#fixtures-reproductibles)
sont regroupées avec la validation ; leurs guides décrivent leur portée. Des manifestes peuvent
identifier des gros artefacts locaux qui ne sont pas distribués dans Git.
Les chemins et rapports d'une ancienne campagne sont des éléments de provenance,
pas des commandes à rejouer sans vérifier les outils actuels.

Les journaux, plans achevés et rapports successifs ont été fusionnés dans ces
références. Leur texte original reste consultable dans l'historique Git à son
ancien chemin; aucun dossier d'archives documentaires parallèle n'est nécessaire.
Lors d'une évolution, modifier la référence propriétaire du contrat et compléter
les preuves utiles, sans recréer un journal de tâches dans la documentation générale.
