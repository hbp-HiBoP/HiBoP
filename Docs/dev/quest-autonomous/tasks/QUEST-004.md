# QUEST-004 — Afficher passthrough et contrôleurs Quest

Jalon : [J1](../milestones/J1.md). Type : implémentation ciblée.
Dépendances : [QUEST-003](QUEST-003.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-004).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../03-target-architecture.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-004 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Ancien XR/Assets/HiBoPXR/Runtime et Platform/Meta à lire via feature/xr ; Assets/_Scenes

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Créer le prefab de rig Quest et sérialiser ses références dans QuestBootstrap.
- Configurer OpenXR Android/Meta, passthrough et suivi des contrôleurs ; réutiliser les composants P04 après adaptation.
- Fournir un écran minimal d'état et la procédure de déploiement, sans interface de connexion réseau.

## Hors périmètre

Pas de mains, cerveau, locomotion ou synchronisation.

## Décisions et questions à traiter

Ne pas accepter silencieusement un fond VR opaque comme réussite passthrough ; exposer le défaut si le runtime ne fournit pas la composition attendue.

## Vérifications à réaliser par l'agent

- Construire et lancer l'APK sur Quest 3 ; relever console, loader actif, poses et configuration de rendu.
- Refaire la vérification d'absence d'activation XR du Player Desktop après changement de réglages.

## Validation manuelle du propriétaire

1. Mettre le casque : la pièce doit être visible et les deux contrôleurs doivent suivre les mouvements.
2. Tourner la tête ; signaler instabilité, décalage ou image noire. Le rapport doit identifier l'APK exact.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la hiérarchie du prefab et où les références/paramètres sont sérialisés ; distinguer observation physique et simple compilation.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-004.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Passthrough et contrôleurs validables sur Quest ; J1 peut être qualifié lorsque les essais manuels sont consignés.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
