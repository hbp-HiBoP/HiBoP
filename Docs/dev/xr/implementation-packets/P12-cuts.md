# P12 — coupes canoniques distantes

## Objectif et résultat observable

Manipuler un gizmo de coupe localement sur Quest, calculer la coupe exacte sur Desktop et appliquer atomiquement le dernier `CutRenderResult` sans retour arrière.

## Decision gate

**Hérité :** D08 remote latest-wins, D09 contrat atomique, D05 hbp_core Quest hors baseline, D20 seuils initiaux.

**À résoudre avant pipeline production :**

- `P12-A` : scope/lifecycle/ownership des coupes et droits de modification Desktop/Quest ;
- `P12-B` : contenu exact requis — mesh, contour, base, overlays, mapping/color space ;
- `P12-C` : comportement visible pending/error/stale et persistance du dernier résultat ;
- `P12-D` : fréquence/coalescence du geste et règle de calcul obligatoire de la dernière séquence ;
- `P12-E` : autorité qui décide après un FAIL des seuils distants.

Un échec distant ne donne pas l'autorisation de porter hbp_core. Il déclenche une décision explicite avant PX2.

## Périmètre autorisé

- gizmo/plan local ;
- commandes interactionId/sequence ;
- calcul Desktop existant + extraction ;
- géométrie/base/overlays par hash ;
- scheduler latest-wins et commit atomique.

## Hors périmètre

- coupe scientifique approximative Quest ;
- backend hbp_core Android ;
- changement des algorithmes scientifiques ;
- suppression d'overlays/colonnes pour latence.

## Hypothèses fixées

- feedback local = plan/gizmo, jamais résultat scientifique ;
- Desktop produit le canonique ;
- dernier état demandé toujours calculé ;
- ancien résultat ne remplace jamais le récent.

## Dépendances et état initial

- P03 CutRenderResult ;
- P07 session ;
- P08 assets ;
- P04/P13 interactions de base ;
- D2/D4 coupes golden.

## Fichiers/modules pressentis

- Desktop cut adapter/scheduler ;
- Protocol cut commands/results ;
- XR gizmo/renderer ;
- tests d'ordre/parité/performance.

## Étapes

1. Résoudre P12-A–E.
2. Implémenter gizmo et conversion de repères P03.
3. Envoyer commandes séquencées/coalescées.
4. Extraire géométrie/base/overlays et hashes.
5. Rejeter stale avant sérialisation/upload.
6. Appliquer résultat complet atomiquement.
7. Mesurer plans courants/extrêmes, 1/3/8 colonnes.
8. Si FAIL D20, optimiser remote puis produire fiche de décision ; ne pas lancer PX2 automatiquement.

## Tests et commandes

- golden exact pour plans connus ;
- réponses retardées/inversées/dupliquées ;
- gesture 20–60 commandes/s et release ;
- perte réseau pendant calcul ;
- réutilisation geometry/base et overlays seuls ;
- command-to-photon p50/p95/max ;
- mains et contrôleurs.

## Critères de sortie binaires

- [ ] P12-A–E enregistrées ;
- [ ] coupe finale égale au golden Desktop ;
- [ ] aucun rollback sous ordre inversé ;
- [ ] dernière séquence converge toujours ;
- [ ] geometry/base dédupliquées conformément au hash ;
- [ ] p95 cible ≤ 150 ms et final ≤ 250 ms, ou FAIL explicite transmis à P12-E ;
- [ ] aucune approximation présentée comme canonique.

## Artefacts à remettre

Gizmo, scheduler/adaptateur/result renderer, tests/goldens, rapport latence et ADR P12 ou fiche FAIL/PX2.

## Conditions d'arrêt

Arrêter si ownership de coupe est ambigu, si repères P03 divergent, si le résultat exige un contrat P03 non décidé ou avant tout port natif non autorisé.

## Prompt de démarrage

> Exécute P12 depuis `Docs/dev/xr/implementation-packets/P12-cuts.md`. Résous P12-A–E avant production. Implémente un gizmo local et un résultat scientifique exclusivement Desktop, séquencé/latest-wins. Mesure les seuils D20. En cas d'échec, arrête-toi avec une fiche de décision ; ne démarre jamais hbp_core Quest sans autorisation explicite.
