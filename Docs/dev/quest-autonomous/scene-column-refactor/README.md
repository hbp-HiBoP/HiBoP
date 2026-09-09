# Scène et colonnes communes — chantier de refactorisation

Statut : spécification et tâches rédigées, aucune implémentation de ce chantier
revendiquée. Décision du propriétaire le **2026-09-09**, conversation
`01a085ce-1d2d-72a3-8006-ffd59c832fe4`.

## Objectif

Faire fonctionner la même scène scientifique et les mêmes colonnes sur Desktop
et Quest. Les fonctionnalités communes retrouvent leurs données, modifient leur
état et déclenchent leurs calculs sans connaître la provenance des données ni
la présentation. Desktop et Quest fournissent les interactions et le rendu.

La vision retenue est `Base3DScene.ToPayload()` → transfert →
`Base3DScene.FromPayload()`, après extraction des responsabilités de présentation
de `Base3DScene`, `Column3D` et de leurs collaborateurs. Cette notation décrit
un contrat de comportement ; elle ne fige ni une signature synchrone ni le
placement du codec dans un MonoBehaviour.

## Ordre d'exécution décidé

**QUEST-018 à QUEST-023 → SCENE-001 à SCENE-008 → QUEST-024 → autres
qualifications de plateformes et suite du chantier Quest.**

- [QUEST-018](../tasks/QUEST-018.md) continue dans son périmètre actuel.
- [QUEST-023](../tasks/QUEST-023.md) fournit le prototype de référence : anatomie,
  sites, densité et un instant iEEG calculé localement sur Quest.
- [SCENE-008](tasks/SCENE-008.md) établit les preuves permettant de reprendre
  [QUEST-024](../tasks/QUEST-024.md) sur l'architecture refactorisée.
- La rédaction présente n'autorise l'exécution d'aucune tâche. Une demande
  explicite d'implémentation autorise la tâche correspondante, pas toute la suite.

Les documents historiques restent intacts : leur dépendance directe 023 → 024
ne reflète donc pas encore cette insertion. Ce dossier consigne la décision
complémentaire récente ; une mise à jour des index historiques sera coordonnée
séparément. Il n'existe aucun routage automatique vers ce nouveau dossier.
Pour une nouvelle conversation, fournir le chemin de la fiche concernée.

## Lire et exécuter

1. [Objectif, périmètre et décisions](01-objective-and-scope.md).
2. [Architecture cible et invariants](02-target-architecture.md).
3. [Payload et restauration](03-payload-and-restoration.md).
4. [Migration, dépendances et validation](04-migration-and-validation.md).
5. [Contrat d'exécution](TASK-WORKFLOW.md), [registre](TASK-STATUS.md) et
   [index des tâches](tasks/README.md).

Exemple de reprise : « Implémente SCENE-001 décrite dans
Docs/dev/quest-autonomous/scene-column-refactor/tasks/SCENE-001.md ».

Les huit fiches sont des lots provisoires, à confirmer en SCENE-001 après 023.
Elles ne constituent ni une estimation de durée garantie ni une migration de
toutes les fonctionnalités historiques. Une tâche trop large doit être découpée
explicitement, sans masquer du travail supplémentaire sous un seul identifiant.

## Isolation du travail en cours

Au moment de la rédaction, un autre agent travaille sur QUEST-018. Cette livraison
ajoute seulement ce dossier : aucun code, document existant, état Unity, build,
appareil ou branche n'est modifié. Les changements de cet agent ne constituent
pas une baseline stable à auditer ou à corriger ici.

Les futures tâches commenceront depuis l'état réel après 023 et préserveront les
travaux concurrents. Les rapports et preuves de ce chantier seront produits dans
[reports](reports/TEMPLATE.md) et [evidence](evidence/README.md), avec suivi dans
le registre local. Les rapports historiques conservent leur valeur de provenance,
sans devenir des preuves de la nouvelle architecture.
