# Index des tâches SCENE

Lire le [contrat local](../TASK-WORKFLOW.md) et le [registre](../TASK-STATUS.md).
Ce chantier s'intercale après QUEST-023 et avant QUEST-024. La présente liste
n'active aucune tâche et ne remplace pas la vérification de ses dépendances.

| ID | Titre | Dépendance | Référence principale |
| --- | --- | --- | --- |
| [SCENE-001](SCENE-001.md) | Auditer le prototype et fixer la migration | QUEST-023 | [Migration](../04-migration-and-validation.md) |
| [SCENE-002](SCENE-002.md) | Extraire le socle Scène/Colonne et son cycle de vie | SCENE-001 | [Architecture](../02-target-architecture.md) |
| [SCENE-003](SCENE-003.md) | Faire utiliser le socle par Desktop | SCENE-002 | [Architecture](../02-target-architecture.md) |
| [SCENE-004](SCENE-004.md) | Exporter et restaurer la scène scientifique | SCENE-003 | [Payload](../03-payload-and-restoration.md) |
| [SCENE-005](SCENE-005.md) | Raccorder Quest au modèle commun | SCENE-004 | [Architecture](../02-target-architecture.md) |
| [SCENE-006](SCENE-006.md) | Vérifier et compléter les opérations communes | SCENE-005 | [Migration](../04-migration-and-validation.md) |
| [SCENE-007](SCENE-007.md) | Retirer les chemins remplacés | SCENE-006 | [Architecture](../02-target-architecture.md) |
| [SCENE-008](SCENE-008.md) | Vérifier l'intégration et préparer QUEST-024 | SCENE-007 | [Migration](../04-migration-and-validation.md) |

Les huit lots sont provisoires. SCENE-001 vérifie leur faisabilité sur le code
après 023 ; les noms de types et les points d'entrée sont à résoudre sur cet état.
Une tâche ne peut être déclarée terminée en reportant un de ses critères obligatoires
sur une suivante sans décision explicite et traçable.
