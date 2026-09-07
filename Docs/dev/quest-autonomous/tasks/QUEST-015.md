# QUEST-015 — Rendre le build natif Android reproductible

Jalon : [J4](../milestones/J4.md). Type : implémentation ciblée.
Dépendances : [QUEST-003](QUEST-003.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-015).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-015 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

hbp_core/CMakeLists.txt ; hbp_core/tools/Invoke-HbpCoreBuild.ps1 ; Tools/NativePlugins*

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Épingler la source scientifique utilisée par Desktop et ajouter Android ARM64 aux outils natifs existants.
- Produire la bibliothèque complète avec provenance NDK/API/ABI, exports et dépendances ELF ; créer l'artefact et son manifeste.
- Étendre le packaging/épinglage Android sans changer les artefacts Desktop ni lancer une publication distante.

## Hors périmètre

Pas de qualification runtime, de mini hbp_core, de port EEGFormat ou d'optimisation NEON.

## Décisions et questions à traiter

Si un changement d'ABI/source impose une mise à jour Desktop, expliquer la nécessité et coordonner cette modification au lieu de comparer des versions différentes.

## Vérifications à réaliser par l'agent

- Compiler reproductiblement la configuration Release, vérifier exports ABI et dépendances du .so.
- Vérifier que les artefacts comparés Desktop/Android proviennent du même code scientifique ; ne pas confondre checkout et binaire épinglé.

## Validation manuelle du propriétaire

1. Aucune manipulation obligatoire. Examiner provenance, ABI et commandes de reproduction dans le rapport.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer les changements ciblés des outils natifs et du manifeste ; expliciter ce que la compilation ne prouve pas.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-015.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Artefact Android reproductible et traçable, sans affirmation de runtime.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
