# QUEST-005 — Définir le contrat de snapshot anatomique

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-001](QUEST-001.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-005).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../05-state-command-and-sync-model.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-005 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Core/Data/BaseData.cs ; Core/Data/Visualization ; anciens Shared/Packages contracts et render-model via Git

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Définir identité/version, repère, surface complète, apparence minimale, longueurs et hashes du snapshot anatomique.
- Réutiliser sélectivement les types/codecs purs existants ; préciser qui détient les buffers et quand ils deviennent immuables.
- Fournir encode/decode et validation bornée, sans transport ; garder les IDs Visualization/Column existants.

## Hors périmètre

Pas de graphe projet complet, Unity instanceID comme ID métier, masque/volume iEEG ou commandes distribuées.

## Décisions et questions à traiter

Fixer une version initiale explicite ; si un contrat historique impose une politique d'autorité Desktop, isoler les primitives utiles sans hériter de cette politique.

## Vérifications à réaliser par l'agent

- Round-trip bit-exact des buffers, IDs et repères ; rejeter version, longueur, index ou hash invalide.
- Vérifier l'absence de dépendance vers UI, scène et XR dans les contrats.
- Documenter les limites d'allocation sans tronquer les données.

## Validation manuelle du propriétaire

1. Aucune manipulation manuelle obligatoire : examiner l'exemple de snapshot et ses champs dans le rapport.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Présenter un exemple lisible et la raison de chaque champ ; montrer les cas invalides couverts plutôt qu'une liste de tous les fichiers.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-005.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Contrat et codec anatomiques testés, sans dépendance réseau ou présentation.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
