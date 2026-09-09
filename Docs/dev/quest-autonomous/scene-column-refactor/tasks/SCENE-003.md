# SCENE-003 — Faire utiliser le socle par Desktop

Type : migration du consommateur Desktop.
Dépendance : [SCENE-002](SCENE-002.md). Suivi : [registre](../TASK-STATUS.md#scene-003).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [l'architecture](../02-target-architecture.md)
et les rapports 001–002. La présence d'une assembly commune ne prouve pas son
utilisation par le Desktop : suivre les chemins de production.

## Points d'entrée à inspecter

`Base3DScene`, `Column3D` et dérivées, préparation de projet/visualisation,
`Module3DMain`, vues/caméras, événements de paramètres et timeline, prefabs et
sérialisation identifiés par SCENE-001. Réutiliser le socle 002.

## À implémenter

- Faire alimenter le socle par la préparation Desktop : adoption/prêt/copie
  explicitement gérés, sans double propriétaire ni duplication inutile des volumes.
- Faire passer anatomie/sites/densité/iEEG par les données et opérations communes.
  Les interactions Desktop appellent ces opérations et observent leurs résultats.
- Extraire les dépendances caméra, vues, toolbar et présentation qui empêchent
  le fonctionnement autonome de la scène scientifique.
- Préserver les préférences et conventions effectives en les passant au bon
  niveau ; ne pas déplacer des valeurs par défaut divergentes dans les adaptateurs.
- Adapter au minimum les autres modalités Desktop affectées, en conservant leur
  comportement ; documenter les dépendances historiques encore nécessaires.
- Préserver les projets, champs sérialisés, GUID et références de prefabs.
  Identifier toute façade transitoire restante avec sa suppression prévue en 007.

## Hors périmètre

Pas de nouvelle UI, nouvelle modalité, port Quest, nouveau transport ou changement
de science. Pas de nettoyage de code historique non affecté par la migration.

## Vérifications par l'agent

- Prouver qu'ouverture et actions du vrai Desktop appellent le socle ; aucun
  test ne doit passer uniquement par une factory absente du chemin de production.
- Comparer à la baseline surface/sites/densité/iEEG, masques et paramètres,
  zéro site/un site/tous masqués, amplitudes négatives/nulles/positives.
- Préserver les sémantiques temporelles Desktop sur plusieurs instants et les
  règles distinctes de sampling surface/sites établies avant refactoring.
- Ouvrir les fixtures/configurations historiques pertinentes ; vérifier les
  références Unity et exécuter les régressions ciblées des autres modalités touchées.
- Changement de paramètre puis retour, remplacement et fermeture : invalidations,
  notifications et ressources conformes au socle. Build/runtime Desktop approprié.

## Validation manuelle

M1 — Sur le Desktop fourni, ouvrir la fixture, comparer anatomie/densité/iEEG à
la référence, modifier le paramètre d'influence puis revenir, parcourir les
instants déjà disponibles. Attendu : comportement et présentation préservés.

Avant de demander M1, fournir vrais binaires, fixture, contrôles, valeurs et images
de référence. Recueillir un retour daté OK/KO avec observation ; cette description
de fiche ne constitue pas une recette exécutable ni un résultat acquis.

## Rapport et critère de fin

Produire `reports/SCENE-003.md`, `evidence/SCENE-003/manifest.json` et sa ligne
locale. Montrer chemin de production avant/après, préservation Desktop et état
des autres modalités/façades.

Terminé techniquement quand Desktop utilise effectivement l'état et les opérations
communs pour le périmètre, sans régression inexpliquée. Le retour manuel peut
rester distinctement en attente. Prochaine tâche : [SCENE-004](SCENE-004.md).
