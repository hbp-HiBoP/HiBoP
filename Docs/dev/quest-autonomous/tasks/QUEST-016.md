# QUEST-016 — Charger et libérer hbp_core dans un Player Quest

Jalon : [J4](../milestones/J4.md). Type : implémentation ciblée.
Dépendances : [QUEST-004](QUEST-004.md), [QUEST-015](QUEST-015.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-016).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-016 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Assets/Plugins/Native ; Core/DLL/HbpCore ; wrappers Volume/Surface/RawSiteList

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Importer le .so Android avec settings CPU/plateforme précis et preuve de contenu APK.
- Ajouter un harness borné de chargement, appels simples, erreurs et cycles de libération dans le Player.
- Vérifier que les wrappers communs fonctionnent via IL2CPP sans appeler un plugin Desktop.

## Hors périmètre

Pas de pipeline densité complet, interprétation scientifique ou nouvelle UI produit.

## Décisions et questions à traiter

Si aucun casque autorisé n'est accessible, demander la connexion après le contrôle ADB ; ne pas remplacer la preuve par un PASS Editor.

## Vérifications à réaliser par l'agent

- Sur Quest physique, créer/utiliser/libérer les ressources et provoquer une erreur native contrôlée.
- Inspecter logcat, contenu APK et dépendances ; relever mémoire et répétitions du test.

## Validation manuelle du propriétaire

1. Pas d'évaluation scientifique à la main. Fournir l'accès au casque si nécessaire ; l'agent exécute et rapporte les contrôles.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer settings du plugin, source/hash et résultats par type d'appel ; distinguer chargement réussi et parité future.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-016.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Runtime de base vérifié sur Quest ou état explicitement bloqué matériel, jamais validé par compilation seule.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
