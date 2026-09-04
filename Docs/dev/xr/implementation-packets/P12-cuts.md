# P12 — coupes canoniques distantes

## Objectif et résultat observable

Manipuler un gizmo de coupe localement sur Quest, calculer la coupe exacte sur Desktop et appliquer atomiquement le dernier `CutRenderResult` sans retour arrière.

## Decision gate

**Hérité :** D08 remote latest-wins, D09 contrat atomique, D05 hbp_core Quest hors baseline, D23 admission par ressources, P11 preload temporel, D20 seuils initiaux.

**À résoudre avant pipeline production :**

- `P12-A` : scope/lifecycle/ownership des coupes et droits de modification Desktop/Quest ;
- `P12-B` : contenu final calculé par Desktop — mesh/contour si requis, image/texture de base, overlays, mapping/color space — et éléments réellement nécessaires au renderer Quest ;
- `P12-C` : comportement visible pending/error/stale et conservation en mémoire du dernier résultat cohérent ;
- `P12-D` : fréquence/coalescence du geste et règle de calcul obligatoire de la dernière séquence ;
- `P12-E` : autorité qui décide après un FAIL des seuils distants.

Un échec distant ne donne pas l'autorisation de porter hbp_core. Il déclenche une décision explicite avant PX2.

## Périmètre autorisé

- gizmo/plan local ;
- commandes interactionId/sequence ;
- calcul Desktop existant + extraction ;
- géométrie/base/overlays par hash ;
- overlays temporels préchargés sous budget pour un plan stable et invalidation explicite lorsque le plan change ;
- scheduler latest-wins et commit atomique.

## Hors périmètre

- coupe scientifique approximative Quest ;
- backend hbp_core Android ;
- changement des algorithmes scientifiques ;
- suppression d'overlays/colonnes pour latence.

## Hypothèses fixées

- feedback local = plan/gizmo, jamais résultat scientifique ;
- Desktop produit le canonique ;
- le Quest reçoit le résultat final prêt à rendre, pas les données nécessaires pour recalculer scientifiquement la coupe ;
- dernier état demandé toujours calculé ;
- ancien résultat ne remplace jamais le récent.

## Dépendances et état initial

- P03 CutRenderResult ;
- P07 session ;
- P08 assets ;
- P11 preload/admission timeline ;
- P04 bootstrap/XRI ; P12 fournit son gizmo/prefab test minimal et P13 consomme ensuite ce résultat, sans dépendance circulaire ;
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
4. Extraire uniquement le résultat final nécessaire — géométrie éventuelle, base, contours, overlays et hashes.
5. Rejeter stale avant sérialisation/upload.
6. Appliquer résultat complet atomiquement.
7. Pour un plan stable, raccorder les overlays par index au preload P11 sous budget ; invalider et recharger explicitement lorsque le plan change.
8. Mesurer plans courants/extrêmes, 1/3/8 colonnes et refus de budget.
9. Si FAIL D20, optimiser remote puis produire fiche de décision ; ne pas lancer PX2 automatiquement.

## Tests et commandes

- golden exact pour plans connus ;
- réponses retardées/inversées/dupliquées ;
- gesture 20–60 commandes/s et release ;
- perte réseau pendant calcul ;
- réutilisation geometry/base et overlays seuls ;
- changement de timeline sur plan stable, puis changement de plan avec invalidation/rechargement ;
- admission/refus des overlays selon D23 sans dégrader les coupes déjà actives ;
- command-to-photon p50/p95/max ;
- mains et contrôleurs.

## Critères de sortie binaires

- [ ] P12-A–E enregistrées ;
- [ ] coupe finale égale au golden Desktop ;
- [ ] aucun rollback sous ordre inversé ;
- [ ] dernière séquence converge toujours ;
- [ ] geometry/base dédupliquées conformément au hash ;
- [ ] overlays d'un plan stable suivent instantanément un index P11 admis ; un changement de plan invalide et recharge sans afficher un résultat périmé ;
- [ ] un dépassement de budget refuse la nouvelle coupe/preload avec feedback explicite, sans paging ni suppression de colonnes ;
- [ ] p95 cible ≤ 150 ms et final ≤ 250 ms, ou FAIL explicite transmis à P12-E ;
- [ ] aucune approximation présentée comme canonique.

## Artefacts à remettre

Gizmo, scheduler/adaptateur/result renderer, tests/goldens, rapport latence et ADR P12 ou fiche FAIL/PX2.

## Conditions d'arrêt

Arrêter si ownership de coupe est ambigu, si repères P03 divergent, si le résultat exige un contrat P03 non décidé ou avant tout port natif non autorisé.

## Prompt de démarrage

> Exécute P12 depuis `Docs/dev/xr/implementation-packets/P12-cuts.md`. Résous P12-A–E avant production. Implémente un gizmo local et un résultat scientifique final exclusivement calculé par Desktop, séquencé/latest-wins. Pour un plan stable, intègre les overlays temporels au preload P11 sous budget D23 ; invalide-les lorsque le plan change. Mesure les seuils D20. En cas d'échec, arrête-toi avec une fiche de décision ; ne démarre jamais hbp_core Quest sans autorisation explicite.
