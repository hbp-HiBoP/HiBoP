# QUEST-003 — Produire deux Players avec des profils isolés

Jalon : [J1](../milestones/J1.md). Type : implémentation ciblée.
Dépendances : [QUEST-002](QUEST-002.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-003).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-003 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs ; ProjectSettings/EditorBuildSettings.asset ; Assets/Plugins/Native

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Créer les profils DesktopWindows et Quest et une scène QuestBootstrap minimale ; garder HiBoP.unity pour Desktop.
- Adapter le point d'entrée de build existant pour sélectionner le profil sans cloner le projet ; configurer ARM64/IL2CPP pour Quest.
- Vérifier import settings et contenu réel des Players ; ajouter le job de build Quest aux workflows existants avec Library/caches distincts, sans modifier la release ni publier.

## Hors périmètre

Pas encore de preuve passthrough/contrôleurs, pas de hbp_core Android requis pour la scène vide, pas de publication CI déclenchée automatiquement.

## Décisions et questions à traiter

Traiter les inclusions Resources réellement bloquantes ; demander avant une migration générale d'assets ou un changement de distribution.

## Vérifications à réaliser par l'agent

- Construire les deux Players depuis le même état source ; consigner SHA et hash du diff si non commité.
- Inspecter scènes/plugins/assemblies et taille ; Desktop n'initialise aucun loader XR et l'APK ne contient pas de binaire natif Desktop.
- Vérifier les configurations CI ajoutées ; distinguer validation locale et job distant non exécuté.

## Validation manuelle du propriétaire

1. Lancer le Player Windows fourni et ouvrir la fixture.
2. Lancer l'APK minimal fourni : vérifier seulement qu'il démarre sans quitter/crasher ; aucun cerveau attendu.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer les deux commandes de build et extraits des rapports prouvant les exclusions ; expliquer toute modification du builder Desktop.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-003.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Deux Players construits et contrôlés, profils versionnés et régression Desktop de base absente.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
