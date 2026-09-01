# P01 — topologie monorepo et projet Unity XR

**Statut : COMPLETE — ADR accepté et topologie minimale implémentée.**

Décision et preuves : [ADR P01](../adr/P01-topology.md) · [validation P01](../evidence/P01/topology-validation.md) · [bootstrap](../P01-bootstrap.md)

## Objectif et résultat observable

Créer l'isolation physique décidée par D01–D03 : second projet Unity Android minimal et packages partagés, sans casser le projet Desktop ni copier son code. OpenXR, XRI et Meta appartiennent à P04.

## Decision gate résolu

**Hérité :** D01 deux projets Unity, D02 monorepo applicatif + repo hbp_core, D03 packages UPM, D04 pas de dépendance complète Core/Data.

**Décisions acceptées :**

- `P01-A` : monorepo, projet Desktop conservé à la racine et second projet sous `XR/` ;
- `P01-B` : Unity `6000.5.2f1` pour les deux projets, mises à niveau coordonnées ;
- `P01-C` : seulement `Contracts`, `RenderModel` et `Protocol` sous `Shared/Packages/`, code Desktop sous `Assets/` et code XR sous `XR/Assets/`, sans déplacement du code HiBoP existant ;
- `P01-D` : aucun Git LFS ; dossiers générés et artefacts ignorés, seuil de contrôle à 50 MiB et workflows XR uniquement manuels ou liés à la publication manuelle d'une release.

La phase `DECISION_ONLY` a été clôturée par validation explicite de l'ADR. Aucune arborescence existante n'a été déplacée.

## Périmètre autorisé

- arborescence approuvée ;
- projet XR minimal vide ;
- packages communs vides avec asmdefs/tests ;
- manifests/locks/ignores et validation statique locale ou lancée manuellement par `workflow_dispatch`.

## Hors périmètre

- contrats métier ;
- renderer, transport ou interaction ;
- migration du projet Desktop ;
- ajout de `hbp_core` au Quest.

## Hypothèses fixées

- aucune copie de `Assets/Scripts/HBP` ;
- scènes/settings/rig XR restent dans le projet XR ;
- packages communs ne référencent ni Desktop ni Meta ;
- toute opération de déplacement est planifiée et réversible.

## Dépendances et état initial

- P00 intégré ou baseline du dépôt explicitement fixée ;
- inventaire des CI, packages Git et plugins Desktop ;
- worktree propre ou changements utilisateur cartographiés.

## Fichiers/modules pressentis

- projet Desktop conservé à la racine ;
- `XR/Assets`, `XR/Packages`, `XR/ProjectSettings` ou layout approuvé ;
- dossier packages communs ;
- `.gitignore`, CI et documentation développeur.

## Étapes exécutées

1. Résoudre P01-A–D dans un ADR et obtenir acceptation.
2. Créer le projet XR avec la version approuvée.
3. Créer packages/asmdefs/tests sans API métier.
4. Référencer les packages localement depuis les deux projets.
5. Configurer les ignores sans Git LFS et éviter tout asset copié.
6. Vérifier ouverture/import et builds/smoke tests des deux projets.
7. Documenter bootstrap développeur et rollback.

## Tests et commandes

- résolution UPM/lock dans les deux projets ;
- compilation des asmdefs vides/tests ;
- build Desktop inchangé ;
- build Android minimal si module installé ;
- scan de dossiers Unity non suivables et doublons de sources.

## Critères de sortie binaires

- [x] ADR P01-A–D accepté ;
- [x] deux projets Unity s'ouvrent avec versions enregistrées ;
- [x] les mêmes packages sources compilent dans les deux ;
- [x] aucune source/asset HiBoP copié ;
- [x] projet Desktop et politique de déclenchement CI existante non régressés ;
- [x] procédure clone/import/build et rollback documentée.

## Artefacts à remettre

ADR topologie, arborescence, projets/packages minimaux, manifests/locks, ignores/CI et guide développeur.

## Conditions d'arrêt historiques

Avant acceptation, il fallait arrêter avant tout déplacement si P01-A n'était pas approuvée, si des liens/CI externes inconnus risquaient d'être cassés ou si le module Android/version Unity manquait. Ces conditions ont été levées sans déplacer le projet Desktop.

## Prompt de démarrage historique

> Exécute P01 depuis `Docs/dev/xr/implementation-packets/P01-topology.md`. Commence en DECISION_ONLY : inspecte le dépôt et produis l'ADR P01-A–D. Ne déplace aucun fichier et ne crée pas le projet XR avant validation explicite de cet ADR. Après validation, implémente uniquement la topologie minimale, vérifie les deux projets et livre les preuves.
