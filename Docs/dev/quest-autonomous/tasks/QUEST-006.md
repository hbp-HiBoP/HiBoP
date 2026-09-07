# QUEST-006 — Capturer la colonne anatomique Desktop réelle

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-005](QUEST-005.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-006).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../04-prototype-specification.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-006 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Core/Object3D/MNIObjects.cs ; Data/Module3D/Base3DScene.cs ; ancien DesktopSurfaceRenderModelAdapter via Git

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Capturer la surface déjà préparée de la colonne sélectionnée, ses IDs, repère et apparence via le contrat commun.
- Assurer une capture cohérente pendant une éventuelle édition Desktop ; ne pas bloquer Unity pendant encodage/envoi.
- Ajouter un point de test ou export diagnostique réutilisable par le futur bouton, sans développer l'UI de connexion.

## Hors périmètre

Pas de réseau ; pas de rechargement silencieux d'un autre MNI que celui de la visualisation sélectionnée.

## Décisions et questions à traiter

Si la sélection n'est pas une colonne prise en charge, la refuser clairement ; ne pas sélectionner automatiquement une autre colonne.

## Vérifications à réaliser par l'agent

- Comparer buffers source/capture, indexation, normales, dimensions et IDs.
- Capturer deux fois un contenu inchangé ; prouver que les données restent cohérentes et que la vue Desktop ne bouge pas.

## Validation manuelle du propriétaire

1. Ouvrir la fixture puis exécuter la capture avec la commande indiquée ; vérifier le nom/ID de colonne et aperçu éventuel.
2. La caméra Desktop doit conserver son cadrage.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer d'où vient chaque donnée et où s'arrête la capture ; expliquer la stratégie de cohérence sans copier le modèle métier.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-006.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Snapshot produit depuis une visualisation réellement ouverte et conforme au contrat.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
