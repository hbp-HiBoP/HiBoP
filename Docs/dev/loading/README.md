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
- [Baseline runtime Editor Mono](baseline_runtime_editor_mono_2026-07-23.md) :
  timings réels, compteurs, limites de l'instrumentation et décisions pour les
  étapes suivantes.
- [Plan d'optimisation](plan_optimisation_chargement.md) : ordre de mise en
  œuvre, critères de validation et stratégie de migration.
- [Instrumentation et benchmark — étape 0](instrumentation_et_benchmark_etape_0.md) :
  emplacement, format des résultats, benchmark opt-in et procédure de retrait.
- [Index stable des tags — étape 1](etape_1_index_tags_2026-07-23.md) :
  implémentation, contrats de mutation, compatibilité JSON et IL2CPP.
- [Résultats de l'étape 1](resultats_etape_1_2026-07-23.md) : validation
  fonctionnelle et comparaison avant/après sur la base et le projet réels.
- [Identifiants paresseux — étape 2](etape_2_ids_paresseux_2026-07-23.md) :
  implémentation, contrats d'identité, rétrocompatibilité et IL2CPP.
- [Résultats de l'étape 2](resultats_etape_2_2026-07-23.md) : validation et
  mesures comparées à l'étape 1.
- [Validation explicite des fichiers — étape 3](etape_3_validation_references_2026-07-23.md) :
  architecture, concurrence, annulation, compatibilité et limites.
- [Résultats de l'étape 3](resultats_etape_3_2026-07-23.md) : séparation
  confirmée, mesures de validation et régression murale observée.
- [Contexte explicite de liaison — étape 4](etape_4_contexte_liaison_2026-07-24.md) :
  index canoniques, scopes base/projet, erreurs regroupées et publication
  atomique.
- [Résultats de l'étape 4](resultats_etape_4_2026-07-24.md) : comparaison
  avant/après sur la nouvelle machine avec `Default` et `full_test`.
- [Lecture et écriture JSON streamées — étape 5](etape_5_json_streame_2026-07-24.md) :
  settings lecture/écriture séparés, suppression des grandes chaînes
  intermédiaires et indentation conservée.
- [Résultats de l'étape 5](resultats_etape_5_2026-07-24.md) : comparaison des
  médianes chaudes, GC et limites des marqueurs streamés.
- [Manifeste projet et lecture ZIP directe — étape 6](etape_6_manifeste_zip_direct_2026-07-24.md) :
  format inchangé, lecture sans extraction, compatibilité et sécurité des
  entrées.
- [Résultats de l'étape 6](resultats_etape_6_2026-07-24.md) : comparaison des
  médianes chaudes et analyse séparée de l'incident Unity.

## Conclusion courte

Le parseur JSON n'est probablement pas la cause dominante.

Dans la baseline d'un workspace réel de 240 patients, le chargement de 370 918
valeurs de tags entraînait environ 71 millions de comparaisons de tags et près
de 742 000 reconstructions de `TagCollection.AllTags`. Avant l'étape 2, le
même graphe créait aussi au moins 476 738 identifiants GUID temporaires dans
les constructeurs avant que Json.NET ne les remplace par les identifiants lus.

Dans la baseline, le chargement exécutait en outre des vérifications
`File.Exists` pour les meshes et IRM depuis les callbacks de
désérialisation. L'étape 3 les place maintenant dans une phase explicite,
après le parsing.

L'ordre recommandé est :

1. instrumenter les phases ;
2. indexer et mettre en cache les tags ;
3. supprimer les GUID temporaires ;
4. séparer désérialisation, résolution des références et validation des
   fichiers ;
5. seulement ensuite ajuster le parallélisme, le streaming ZIP et le format
   JSON ;
6. verrouiller le tout par un test de player IL2CPP.

L'étape 1 est maintenant implémentée et validée : `AllTags` est une vue stable
et les résolutions par ID utilisent un dictionnaire. La médiane chaude baisse
de 49,0 % sur la base et de 39,4 % sur le projet de référence.

L'étape 2 est également implémentée et validée : le constructeur de
`BaseData` ne génère plus de GUID avant la lecture de l'ID JSON. Par rapport à
l'étape 1, la médiane chaude baisse encore de 18,6 % sur la base et de 2,5 %
sur le projet ; le CPU projet baisse de 8,2 %.

L'étape 3 est implémentée et validée fonctionnellement : aucun accès fichier
n'a lieu dans les callbacks JSON et la validation est bornée, annulable et
mesurée séparément. Elle ne réduit toutefois pas le temps total dans la
campagne actuelle : +15,3 % sur la base et +7,5 % sur le projet par rapport à
l'étape 2. Ce résultat est conservé explicitement dans l'audit.

L'étape 4 est implémentée et validée fonctionnellement : les callbacks JSON ne
consultent plus les singletons pour relier les objets, les colonnes conservent
leurs références résolues et la base comme le projet ne publient le nouveau
graphe qu'après liaison et validation. Sur la nouvelle machine, le coût
instrumenté de liaison baisse de 80,2 % sur la base et de 87,2 % sur le projet.
Sur la médiane de trois passes chaudes après implémentation, le temps mural
baisse de 5,1 % sur la base et de 17,1 % sur le projet par rapport à la capture
de référence.

L'étape 5 est implémentée et validée fonctionnellement. Les lectures et
écritures JSON utilisent maintenant les streams Json.NET, sans chaîne complète
intermédiaire, et les contraintes `new()` inutiles ont été retirées. Tous les
fichiers restent indentés afin de préserver leur édition manuelle. Le
benchmark montre une baisse médiane du temps mural de 10,3 % sur la base et de
19,3 % sur le projet. Les collections GC diminuent respectivement de 41,8 % et
25,0 %. La première passe après compilation reste plus lente que la référence
chaude.

L'étape 6 est implémentée et validée fonctionnellement. Le format `.hibop`
écrit reste identique, mais son index, ses settings et ses objets sont
désormais lus directement depuis les streams ZIP. Le dossier d'extraction
n'est plus utilisé pendant le chargement. Les anciens dossiers `Protocols/`
sont acceptés mais entièrement ignorés. Sur `full_test`, la médiane chaude
passe de 2 929,8 ms à 1 792,5 ms (-38,8 %) et la lecture de l'archive de
1 082,9 ms à 105,2 ms (-90,3 %).
