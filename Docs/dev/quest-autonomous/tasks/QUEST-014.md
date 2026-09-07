# QUEST-014 — Afficher les contacts et masquer le cerveau

Jalon : [J3](../milestones/J3.md). Type : implémentation ciblée.
Dépendances : [QUEST-013](QUEST-013.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-014).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../04-prototype-specification.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-014 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Ancien XR/Sites via Git ; prefab manipulable et panneau Quest

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Adapter le rendu des sites à partir du snapshot sous le même groupe spatial.
- Ajouter le bouton afficher/masquer la surface ; conserver sites, données et prise du groupe quand le cerveau est masqué.
- Préserver la taille relative des contacts pendant un changement d'échelle globale.

## Hors périmètre

Pas de transparence passthrough, de picking précis ou de règle scientifique de couleur dupliquée.

## Décisions et questions à traiter

D19 est acquis : ne pas rouvrir la transparence pour finir ce jalon.

## Vérifications à réaliser par l'agent

- Vérifier positions/tailles/parentage et indépendance du masque visuel avec le masque scientifique.
- Mesurer frame et mémoire sur la fixture ; tests de chargement/libération des buffers de sites.

## Validation manuelle du propriétaire

1. Masquer le cerveau : les contacts internes restent visibles et manipulables ; réafficher la surface.
2. Agrandir/réduire le groupe et vérifier que les contacts suivent ; observer les repères asymétriques fournis.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Montrer la différence entre visibilité de surface et masque de calcul ; expliquer les références prefab et le maintien de la prise.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-014.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

J3 démontré avec contacts correctement placés et bouton de masquage.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
