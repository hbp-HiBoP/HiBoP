# Chargement des objets HiBoP

Ce dossier rassemble l'audit du chargement des métadonnées HiBoP depuis la
base locale et les projets `.hibop`.

## Documents

- [Audit technique](audit_chargement_objets_hibop_2026-07-23.md) : flux,
  causes de lenteur, contraintes de rétrocompatibilité et IL2CPP, risques et
  recommandations.
- [Baseline et protocole de mesure](baseline_chargement_2026-07-23.md) :
  volumes observés, mesures reproductibles, limites de la session et
  instrumentation à ajouter.
- [Plan d'optimisation](plan_optimisation_chargement.md) : ordre de mise en
  œuvre, critères de validation et stratégie de migration.

## Conclusion courte

Le parseur JSON n'est probablement pas la cause dominante.

Sur un workspace réel de 240 patients, le chargement de 370 918 valeurs de tags
entraîne actuellement environ 71 millions de comparaisons de tags et près de
742 000 reconstructions de `TagCollection.AllTags`. Le même graphe crée aussi
au moins 476 738 identifiants GUID temporaires dans les constructeurs avant que
Json.NET ne les remplace par les identifiants lus.

Le chargement exécute en outre des vérifications `File.Exists` pour les meshes
et IRM depuis les callbacks de désérialisation. Le temps affiché comme
« chargement JSON » contient donc aussi de la résolution de graphe, beaucoup
d'allocations transitoires et potentiellement des accès réseau.

L'ordre recommandé est :

1. instrumenter les phases ;
2. indexer et mettre en cache les tags ;
3. supprimer les GUID temporaires ;
4. séparer désérialisation, résolution des références et validation des
   fichiers ;
5. seulement ensuite ajuster le parallélisme, le streaming ZIP et le format
   JSON ;
6. verrouiller le tout par un test de player IL2CPP.
