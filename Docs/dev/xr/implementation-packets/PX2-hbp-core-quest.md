# PX2 — contingency hbp_core Quest ciblée

## Objectif et résultat observable

Évaluer puis, seulement si autorisé, intégrer sur Quest une fonction native précisément ciblée dont la chaîne distante a échoué malgré optimisation, tout en conservant le Desktop comme autorité canonique.

## Decision gate

**Précondition absolue :** rapport P12/S03 `FAIL` et autorisation explicite de lancer PX2. Un build Android réussi seul ne suffit pas.

**Hérité :** D05 hbp_core hors baseline, build ARM64 statiquement prouvé, Desktop valide le canonique.

**À résoudre avant toute intégration production :**

- `PX2-A` : fonction exacte à porter et bénéfice attendu ;
- `PX2-B` : tolérance/parité et protocole de validation Desktop ;
- `PX2-C` : budgets CPU/mémoire/thermique/batterie et durée ;
- `PX2-D` : version/ABI/importer/IL2CPP et compatibilité ;
- `PX2-E` : données minimales autorisées sur Quest et lifecycle P14 ;
- `PX2-F` : licences, symbol visibility, stripping et maintenance CI ;
- `PX2-G` : comportement en cas de divergence locale/canonique.

Sans toutes ces décisions et autorisation, statut `BLOCKED` ; aucune modification native/Unity production.

## Périmètre autorisé

- plugin ARM64 ciblé ;
- wrapper P/Invoke AOT-safe ;
- fonction PX2-A seulement ;
- tests de parité/performance/thermique ;
- validation Desktop du résultat.

## Hors périmètre

- port complet hbp_core ;
- chargement du projet/données patients complet sur Quest ;
- remplacement du canonique Desktop ;
- adoption d'autres fonctions par opportunisme.

## Hypothèses fixées

- code source hbp_core reste dans son repo ;
- ABI C versionnée ;
- données en mémoire selon P14 ;
- tout résultat local est provisoire jusqu'à validation Desktop.

## Dépendances et état initial

- P12 FAIL + décision ;
- P14 security/lifecycle ;
- toolchain NDK/Unity Quest ;
- golden P00/P03.

## Fichiers/modules pressentis

- hbp_core CMake/CI/exports ;
- plugin importer XR ;
- wrapper/adaptateur backend ciblé ;
- tests native/Unity/device.

## Étapes

1. Vérifier autorisation et résoudre PX2-A–G.
2. Nettoyer version ABI, exports et build Android.
3. Importer plugin ARM64 et appeler version/init.
4. Implémenter uniquement PX2-A.
5. Comparer Desktop bit-à-bit/tolérance.
6. Profiler 30 min et cycles lifecycle.
7. Implémenter validation canonique/fallback remote.
8. Décider adoption/rejet ; retirer le plugin si rejet.

## Tests et commandes

- tests natifs Desktop/Android ;
- IL2CPP P/Invoke/version/init/error ;
- parité golden ;
- CPU/mémoire/thermique/batterie 30 min ;
- lifecycle/background/reconnect ;
- symbol exports/strip/SBOM/licences ;
- formatter/tests des wrappers C#.

## Critères de sortie binaires

- [ ] FAIL P12 et autorisation archivés ;
- [ ] PX2-A–G acceptées ;
- [ ] fonction ciblée seulement ;
- [ ] parité et validation Desktop passent ;
- [ ] budgets device passent ;
- [ ] lifecycle/confidentialité passent ;
- [ ] ABI/CI/licences maintenables ;
- [ ] décision finale adoption ou suppression explicite.

## Artefacts à remettre

ADR d'autorisation, build/plugin/wrapper ciblé, tests/parité/perf, SBOM et décision finale.

## Conditions d'arrêt

Arrêter immédiatement sans autorisation, si le scope s'élargit, si des données non autorisées sont requises, si la parité/budgets échouent ou si le Desktop ne peut valider le résultat.

## Prompt de démarrage

> Exécute PX2 depuis `Docs/dev/xr/implementation-packets/PX2-hbp-core-quest.md` uniquement si un FAIL P12 et une autorisation explicite sont présents. Sinon arrête-toi sans modifier le code. Résous PX2-A–G, porte une seule fonction, prouve parité/performance/confidentialité et conserve le Desktop comme autorité.
