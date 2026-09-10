# Refactor de la 3D commune et transfert d’une visualisation complète

Cadrage réécrit le **2026-09-10**, selon la discussion propriétaire dans la tâche
`01a08a59-a909-7ba0-967b-57fd91ed5183`. Cette édition remplace intégralement
la spécification du 9 septembre. Les IDs SCENE-001 à SCENE-008 sont conservés
pour le repérage, mais **leur contenu a changé**.

## Résultat attendu

Généraliser `Base3DScene`, `Column3D`, leurs dérivées et collaborateurs afin que
Desktop et Quest exécutent les mêmes fonctionnalités 3D. Extraire seulement
les dépendances de présentation et les opérations enfermées dans les outils UI.
Conserver les composants Unity et le rendu commun quand ils conviennent.

Exporter une **visualisation complète**, toutes ses colonnes et les données
nécessaires à leurs fonctionnalités, puis la restaurer localement sur Quest.
Un cerveau par colonne, manipulable indépendamment. Les nouvelles interfaces
scientifiques Quest et la synchronisation de commandes viendront plus tard.

## Priorité de développement

**Implémenter beaucoup entre les validations.** Les fiches sont des repères de
travail, pas huit cycles compilation → tests → builds → essais manuels.
Les régressions ordinaires seront principalement détectées et corrigées après
l’intégration complète. Aucun build Windows/Android, suite longue ou recette
manuelle n’est obligatoire à la fin d’une fiche ou d’un lot d’implémentation.

Les vérifications intermédiaires sont exceptionnelles et ciblées : elles doivent
résoudre une incertitude qui empêche réellement de continuer. Le détail est dans
[la stratégie de réalisation](04-migration-and-validation.md).

## Documents de référence

- [Vision, périmètre et décisions](01-objective-and-scope.md).
- [Généralisation du code existant](02-target-architecture.md).
- [Données, ressources locales et transfert complet](03-payload-and-restoration.md).
- [Grands lots et validation regroupée](04-migration-and-validation.md).
- [Workflow](TASK-WORKFLOW.md), [état du chantier](TASK-STATUS.md),
  [fiches de travail](tasks/README.md).

## Déroulement

| Lot | Fiches | Résultat de travail |
| --- | --- | --- |
| A — Généraliser la 3D | 001–003 | Classes communes utilisées par Desktop et opérations accessibles sans ses contrôles |
| B — Transférer et présenter | 004–007 | Ressources complètes, restauration, colonnes Quest et parcours intégré |
| C — Stabiliser | 008 | Détection/correction des bugs, vérifications ciblées puis campagne finale |

Les lots A et B peuvent s’enchaîner sans campagne intermédiaire. Leur frontière
n’impose ni arrêt ni compilation. Une fiche peut nécessiter des changements
dans une autre : travailler par parcours cohérent plutôt que maintenir des
façades uniquement pour respecter l’ordre des numéros.

Une future demande d’implémenter le chantier autorise ces lots et leurs corrections
nécessaires. Une demande explicitement limitée reste limitée. L’autorisation
actuelle concerne **la réécriture documentaire**, pas le démarrage du code.

## Baseline et suite historique

Le registre Quest indique 018–023 implémentées et validées, dont 023 le 10 septembre.
La lecture initiale du code a été faite sur `feature/xr-autonomous@639e88306`.
Vérifier l’état réel à la reprise, sans rejouer une qualification de baseline.

Le chantier reste placé avant QUEST-024 et les qualifications de plateformes
suivantes. Toutefois les anciennes fiches Quest décrivent un prototype limité :
elles ne définissent pas le nouveau périmètre. SCENE-008 rassemble une campagne
finale réutilisable pour les besoins pertinents de 024, sans doublonner les essais
ni revendiquer son acceptation administrative. Les documents historiques hors de
ce dossier ne sont pas modifiés par cette réécriture.
