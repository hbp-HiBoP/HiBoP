# SCENE-006 — Vérifier et compléter les opérations communes

Type : compléments ciblés et preuve architecturale après intégration.
Dépendance : [SCENE-005](SCENE-005.md). Suivi : [registre](../TASK-STATUS.md#scene-006).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), les invariants de
[l'architecture](../02-target-architecture.md), [la matrice](../04-migration-and-validation.md)
et les rapports 002–005. Cette tâche complète leurs chemins communs ; elle ne
régularise pas a posteriori deux orchestrations autorisées par défaut.

## Points d'entrée à inspecter

Opérations et révisions du socle, chemins de mutation Desktop, données disponibles
après restauration, invalidation et fin async des pipelines, abonnements des
présentations. Reprendre l'inventaire des déclencheurs de SCENE-001.

## À implémenter ou compléter

- Vérifier chaque mutation du périmètre : volume, surface, implantation, masque,
  paramètres d'influence/normalisation et instant, selon les capacités existantes.
  Raccorder au socle les responsabilités résiduelles effectivement trouvées.
- Garantir le même accès aux données et le même déclenchement de calcul quelle
  que soit la source de scène. Les présentations observent, elles ne décident pas
  séparément des dépendances scientifiques ou de l'ordre des calculs.
- Compléter les règles de résultats/révisions pour mutations rapprochées,
  annulation, erreurs et fermeture. Éviter les recalculs et copies redondants.
- Exprimer les données/operations indisponibles sans données inventées, branche
  spécifique Quest ou appel au Desktop. Préserver les sémantiques temporelles.
- Ajouter seulement les preuves manquantes utiles ; réutiliser celles de 002–005
  lorsque leur chemin reste inchangé et qu'elles démontrent réellement l'invariant.

## Hors périmètre

Pas de fonctionnalité nouvelle, UI/gizmo, timeline complète transférée ou nouveau
framework de commandes. Pas d'optimisation scientifique ni élargissement de
tolérance pour masquer une divergence.

## Vérifications par l'agent

- Exécuter exactement le même scénario par l'API scène/colonne sur une scène
  préparée et une restaurée : modifier le rayon, observer invalidation/progression,
  attendre la fin et lire les résultats. Aucun accès direct au générateur dans le test.
- Exécuter aussi l'opération existante de scène choisie en 001, avec les données
  disponibles des deux côtés : même API, invalidation des bonnes colonnes et
  résultats cohérents. Prouver l'orchestration de scène, pas seulement son rôle
  de conteneur ; aucune fonctionnalité nouvelle n'est ajoutée pour ce test.
- Démontrer que les chemins de production Desktop et Quest consomment ce socle ;
  des tests isolés d'un service non branché ne satisfont pas cette preuve.
- Couvrir mutations rapprochées et résultat ancien, masques/valeurs limites,
  changement de références, iEEG disponible/indisponible et multiples instants Desktop.
- Deux colonnes : changements indépendants, asset partagé, masque mutable isolé,
  fermeture d'une colonne durant calcul de l'autre et libération finale.
- Vue retirée/recréée, session remplacée/fermée : pas d'abonnement résiduel,
  publication à une mauvaise scène ou libération avant terminaison native réelle.
- Revue indépendante bornée de l'invalidation et de la concurrence. Comparaison
  Windows/Android appropriée aux chemins modifiés, avec mesures de ressources utiles.

## Validation manuelle

NON_REQUIS pour la preuve architecturale. Si un correctif modifie un comportement
visible déjà qualifié, fournir et refaire uniquement la recette manuelle affectée,
avec un statut séparé et un retour explicite.

## Rapport et critère de fin

Produire `reports/SCENE-006.md`, `evidence/SCENE-006/manifest.json` et sa ligne
locale. Fournir la matrice mutation → dépendances → calcul → publication, les
appels réels et les indisponibilités dues aux données, avec leur justification.

Terminé lorsque le parcours d'une fonctionnalité existante est commun de bout en
bout, y compris accès mémoire et invalidation, et que les cas limites affectés sont
vérifiés. Une simple égalité de résultats ne suffit pas.
Prochaine tâche : [SCENE-007](SCENE-007.md).
