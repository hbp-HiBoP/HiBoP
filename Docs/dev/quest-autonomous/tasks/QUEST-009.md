# QUEST-009 — Qualifier le transport embarqué avant intégration

Jalon : [J2](../milestones/J2.md). Type : essai technique et décision.
Dépendances : [QUEST-004](QUEST-004.md), [QUEST-005](QUEST-005.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-009).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-009 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Spikes/P06 à consulter sur feature/xr ; preuves P06 historiques ; APIs disponibles dans les Players actuels

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Construire un essai minimal de connexion chiffrée/appairage et transfert d'un payload représentatif dans les Players Unity.
- Mesurer intégrité, durée, allocations et comportement interruption/arrêt ; rejeter une identité incorrecte, sans accept-all.
- Conclure sur une solution embarquée et un code réutilisable ; si elle échoue, comparer l'auxiliaire P06 avec obstacle et coût concret.

## Hors périmètre

Pas d'interface produit complète, découverte réseau, persistance de clés, pile HTTP ou cryptographie maison.

## Décisions et questions à traiter

Si l'embarqué ne convient pas, présenter les preuves et demander le choix entre auxiliaire et alternative ciblée ; ne pas changer seul l'architecture pour gagner du temps.

## Vérifications à réaliser par l'agent

- Essai Windows–Quest physique, payload/hash identiques, coupure/reconnexion et arrêt sans processus/threads perdus.
- Compiler le candidat dans le backend Desktop réellement retenu et Android IL2CPP ; distinguer preuve réseau du contrat applicatif.
- Archiver versions/licences des composants choisis, sans secrets ni jetons.

## Validation manuelle du propriétaire

1. Si nécessaire, comparer le code d'appairage affiché sur les deux appareils ; le rapport fournit les étapes exactes.
2. Aucune review visuelle de cerveau n'est nécessaire pour conclure sur ce test.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Expliquer pourquoi le candidat répond au nouveau transfert ponctuel, et ce que le test ne prouve pas encore sur Mac.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-009.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Décision de transport argumentée par une preuve Player ; si décision propriétaire requise, statut bloqué sur décision, pas faux succès.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
