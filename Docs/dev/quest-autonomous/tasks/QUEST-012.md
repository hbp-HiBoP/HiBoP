# QUEST-012 — Qualifier la démonstration anatomique et la déconnexion

Jalon : [J2](../milestones/J2.md). Type : intégration / qualification.
Dépendances : [QUEST-011](QUEST-011.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-012).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-012 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Implémentation QUEST-002 à QUEST-011 et rapports associés

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Préparer les Players et une recette reproductible de démonstration anatomique depuis la fixture.
- Exécuter la vérification complète, mesurer transfert/frame/mémoire et traiter seulement les défauts nécessaires à cette démonstration.
- Consigner les preuves automatiques et les validations utilisateur ; clôturer J2 uniquement avec résultats réels.

## Hors périmètre

Pas d'électrodes, nouvelle feature ou campagne d'optimisation générale.

## Décisions et questions à traiter

Demander l'acceptation du confort et des durées mesurées ; si une cible provisoire doit devenir critère formel, la faire valider explicitement.

## Vérifications à réaliser par l'agent

- Vérifier provenance identique des Players, buffers, pas de fallback fixture APK.
- Effectuer coupure 60 s et reconnexion, intégrité après renvoi et vue Desktop intacte.
- Collecter froid/chaud et limites des métriques disponibles ; aucun seuil scientifique inventé.

## Validation manuelle du propriétaire

1. Envoyer le cerveau, effectuer les trois gestes et noter leur confort.
2. Couper le réseau pendant 60 s en continuant les gestes, reconnecter puis vérifier que pose/taille et contenu sont conservés.
3. Vérifier que la vue Desktop n'a pas suivi les gestes Quest.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Faire lire d'abord le scénario de démo et ses résultats, puis les défauts corrigés ; ne pas confondre mesures et impression utilisateur.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-012.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

J2 a un rapport de démonstration ; les items manuels non observés restent en attente.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
