# P01 — topologie monorepo et projet Unity XR

## Objectif et résultat observable

Créer l'isolation physique décidée par D01–D03 : second projet Unity Android/OpenXR et packages partagés, sans casser le projet Desktop ni copier son code.

## Decision gate

**Hérité :** D01 deux projets Unity, D02 monorepo applicatif + repo hbp_core, D03 packages UPM, D04 pas de dépendance complète Core/Data.

**Décisions obligatoires manquantes :**

- `P01-A` : layout physique — conserver le projet Desktop à la racine ou le déplacer sous `Desktop/` ;
- `P01-B` : version Unity initiale du projet XR et politique d'alignement avec Desktop ;
- `P01-C` : emplacement exact des packages communs et forme des références `file:` ;
- `P01-D` : stratégie Git LFS/ignores pour Library, builds et captures XR.

Cette phase commence en `DECISION_ONLY`. Produire un ADR comparant coût de migration, CI, liens Unity et historique. Aucune arborescence existante ne doit être déplacée avant acceptation explicite.

## Périmètre autorisé

- arborescence approuvée ;
- projet XR minimal vide ;
- packages communs vides avec asmdefs/tests ;
- manifests/locks/ignores et validation CI minimale.

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

- racine du repo selon P01-A ;
- `XR/Assets`, `XR/Packages`, `XR/ProjectSettings` ou layout approuvé ;
- dossier packages communs ;
- `.gitignore`, CI et documentation développeur.

## Étapes

1. Résoudre P01-A–D dans un ADR et obtenir acceptation.
2. Créer le projet XR avec la version approuvée.
3. Créer packages/asmdefs/tests sans API métier.
4. Référencer les packages localement depuis les deux projets.
5. Configurer ignores/LFS et éviter tout asset copié.
6. Vérifier ouverture/import et builds/smoke tests des deux projets.
7. Documenter bootstrap développeur et rollback.

## Tests et commandes

- résolution UPM/lock dans les deux projets ;
- compilation des asmdefs vides/tests ;
- build Desktop inchangé ;
- build Android minimal si module installé ;
- scan de dossiers Unity non suivables et doublons de sources.

## Critères de sortie binaires

- [ ] ADR P01-A–D accepté ;
- [ ] deux projets Unity s'ouvrent avec versions enregistrées ;
- [ ] un même package source compile dans les deux ;
- [ ] aucune source/asset HiBoP copié ;
- [ ] projet Desktop et CI existante non régressés ;
- [ ] procédure clone/import/build et rollback documentée.

## Artefacts à remettre

ADR topologie, arborescence, projets/packages minimaux, manifests/locks, ignores/CI et guide développeur.

## Conditions d'arrêt

Arrêter avant tout déplacement si P01-A n'est pas approuvée, si des liens/CI externes inconnus seraient cassés ou si le module Android/version Unity manque.

## Prompt de démarrage

> Exécute P01 depuis `Docs/dev/xr/implementation-packets/P01-topology.md`. Commence en DECISION_ONLY : inspecte le dépôt et produis l'ADR P01-A–D. Ne déplace aucun fichier et ne crée pas le projet XR avant validation explicite de cet ADR. Après validation, implémente uniquement la topologie minimale, vérifie les deux projets et livre les preuves.
