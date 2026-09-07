# QUEST-010 — Livrer et publier une session complète

Jalon : [J2](../milestones/J2.md). Type : implémentation ciblée.
Dépendances : [QUEST-006](QUEST-006.md), [QUEST-007](QUEST-007.md), [QUEST-009](QUEST-009.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-010).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../05-state-command-and-sync-model.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-010 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Contrat/capture/renderer des tâches précédentes ; ancien Protocol/RemoteAssets via Git

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Relier le transport retenu à la capture et au récepteur ; vérifier les chunks et préparer en staging avant publication atomique.
- Dissocier connexion et propriété des ressources ; conserver le contenu prêt hors ligne.
- Ajouter identité de livraison/acquittement et répétition idempotente ; recommencer un transfert interrompu est acceptable.

## Hors périmètre

Pas de journal des gestes, fusion de données, cache durable ni reprise par plages obligatoire.

## Décisions et questions à traiter

Si la politique de cache historique purge sur lease/background, l'adapter à D12 ; ne pas demander de réapprouver l'autonomie.

## Vérifications à réaliser par l'agent

- Corruption/incomplétude/version inconnue : contenu précédent conservé.
- Accusé perdu puis retransmission : une seule instance ; coupure après publication : session conservée.
- Remplacement réussi/échoué et fermeture : vérifier propriété/libération ; état de connexion sans purge implicite.

## Validation manuelle du propriétaire

1. Aucune recette produit définitive : l'agent effectue les essais via le harness et joint les résultats.
2. La démonstration utilisateur complète appartient à QUEST-012.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Illustrer les transitions connecté/réception/prêt/hors ligne avec un scénario accusé perdu ; montrer le point unique de publication.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-010.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Transfert réel raccordé au renderer avec comportements d'échec et duplication vérifiés.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
