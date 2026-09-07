# QUEST-025 — Préparer le Player Mac Apple Silicon

Jalon : [J6](../milestones/J6.md). Type : implémentation ciblée.
Dépendances : [QUEST-024](QUEST-024.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-025).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../06-native-data-and-portability-strategy.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-025 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

HBPBuilder.cs ; plugins macOS ; CMake/scripts hbp_core et hbp_math ; transport retenu

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Ajouter ou adapter le profil DesktopMac et packaging ARM64 à partir du même code.
- Vérifier chemins Data/plugins, version minimale effective et démarrage du transport embarqué sur le Mac cible.
- Produire le Player et sa recette d'installation/lancement sans créer de variante métier Mac.

## Hors périmètre

Pas de signature/publication commerciale non demandée, support Intel ou qualification complète avant essai physique.

## Décisions et questions à traiter

Demander tôt l'accès ou les informations de la machine si indisponibles ; ne pas remplacer la preuve runtime par CMake min12.

## Vérifications à réaliser par l'agent

- Construire/inspecter le bundle ARM64, provenance native et contenu.
- Relever OS réel, erreurs de lancement et dépendances du transport ; distinguer cross-build et exécution.

## Validation manuelle du propriétaire

1. Sur le Mac fourni, lancer l'application et ouvrir la fixture ; relever les éventuelles demandes OS.
2. L'agent doit donner les commandes/chemins Mac concrets, sans supposer C:/HBP présent.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer les adaptations exclusivement de packaging/plateforme et les preuves de lancement.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-025.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Player Mac ouvrable et prêt pour la qualification Quest sur le matériel identifié.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
