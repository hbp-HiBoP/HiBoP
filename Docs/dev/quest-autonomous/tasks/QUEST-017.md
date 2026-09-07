# QUEST-017 — Transférer et reconstruire les entrées de projection

Jalon : [J4](../milestones/J4.md). Type : implémentation ciblée.
Dépendances : [QUEST-014](QUEST-014.md), [QUEST-016](QUEST-016.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-017).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-017 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Core/DLL/Volume.cs ; Surface.SetBuffers ; RawSiteList.AddSite ; Base3DScene.UpdateProjectionResources

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Étendre le snapshot aux octets du volume de référence, paramètres de grille/influence et masques effectifs des sites.
- Écrire le volume reçu dans un chemin privé de session vérifié et reconstruire Volume/Surface/RawSiteList via les wrappers existants.
- Centraliser les conversions entre repère de transport et APIs natives ; définir propriété/libération, sans calcul de densité dans cette tâche.

## Hors périmètre

Pas de chemin Desktop utilisé sur Quest, import EEG, capsule persistante ou conversion scientifique de remplacement.

## Décisions et questions à traiter

Les entrées scientifiques ne peuvent pas être omises pour réduire la taille ; exposer un dépassement réel et ses options.

## Vérifications à réaliser par l'agent

- Round-trip surface via SetBuffers, sites natifs et volume ; comparer orientation, dimensions et masques.
- Rejeter manque de volume, incohérence de hash/longueurs et données invalides ; préserver la session précédente.
- Vérifier nettoyage des ressources après échec, sans libération anticipée.

## Validation manuelle du propriétaire

1. Aucune validation scientifique visuelle obligatoire ; revoir le tableau des entrées et repères fourni.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Expliquer pourquoi le volume est nécessaire et comment surface/sites/volume retrouvent le même repère ; détailler qui possède chaque ressource.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-017.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Entrées identiques et reconstruisibles sur Quest, indépendantes du réseau après réception.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
