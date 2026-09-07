# QUEST-013 — Préparer et transférer les contacts synthétiques

Jalon : [J3](../milestones/J3.md). Type : implémentation ciblée.
Dépendances : [QUEST-012](QUEST-012.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-013).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-013 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Core/Object3D/Site.cs ; Core/Object3D/Implantation3D.cs ; ancien DesktopSiteRenderModelAdapter

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Définir une fixture de contacts déterministe associée au MNI avec positions asymétriques et IDs stables.
- Étendre le snapshot/capture aux positions, ordre, identité et apparence minimale des sites.
- Conserver les champs nécessaires à la future reconstruction native sans ajouter les règles de calcul de densité.

## Hors périmètre

Pas de densité, picking, ROI, mise en évidence interactive ou rendu des sites dans cette tâche.

## Décisions et questions à traiter

Le nombre de contacts est une donnée de fixture consignée, pas une limite produit ; ne pas tronquer une liste reçue.

## Vérifications à réaliser par l'agent

- Round-trip sites et associations, ordre/IDs stables, cas zéro site et données invalides.
- Vérifier la relation des coordonnées au cerveau ; interdire position monde Quest dans le contrat.

## Validation manuelle du propriétaire

1. Examiner le schéma de fixture et les positions repères présentées par le rapport ; aucun test au casque obligatoire.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la correspondance ID/index/coordonnée et les conversions nécessaires pour RawSiteList plus tard.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-013.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Contacts capturés et reçus correctement, contrat compatible avec l'anatomie seule.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
