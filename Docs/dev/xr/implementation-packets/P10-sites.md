# P10 — 37 500 sites, rendu bufferisé et picking

## Objectif et résultat observable

Afficher tous les sites du dataset D3, mettre à jour leurs attributs en groupe et sélectionner un `siteId` exact par ray ou proximité, sans GameObject/renderer/collider par site ni scan O(N) par frame.

## Decision gate

**Hérité :** D13 buffers/instancing + index spatial, 37 500 sans plafond, D17 IDs opaques, D20 picking p95 < 50 ms proposé.

**À résoudre avant backend production :**

- `P10-A` : backend GPU retenu après comparaison mesurée ;
- `P10-B` : règles de désambiguïsation/ranking quand plusieurs sites sont sélectionnables ;
- `P10-C` : unités de taille, scaling avec BrainInstance et seuils ray/proximité ;
- `P10-D` : structure spatiale retenue et politique de rebuild ;
- `P10-E` : métadonnées affichables au Quest lors d'une sélection.

Des variantes jetables peuvent être mesurées avant P10-A/D. Le chemin production ne commence qu'après enregistrement des choix et validation produit de P10-B/C/E.

## Périmètre autorisé

- SiteAsset/SiteRenderFrame ;
- instancing/GraphicsBuffer ou backend mesuré ;
- dirty ranges ;
- grille/BVH et picking local brain space ;
- feedback hover/pending/canonical.

## Hors périmètre

- timeline réseau complète, sauf frames synthétiques ;
- fiche patient persistée ;
- filtre qui retire des sites pour performance ;
- GameObject/collider par site.

## Hypothèses fixées

- positions statiques par SiteAsset ;
- mapping index↔siteId valide seulement avec hash ;
- contrôleurs référence précision ;
- sélection sémantique confirmée par Desktop.

## Dépendances et état initial

- P03 SiteAsset/Frame ;
- P05 renderer ;
- P09 BrainInstance ;
- D0 puis D3 ;
- P07 pour outcome de sélection lors de l'intégration finale.

## Fichiers/modules pressentis

- Rendering site backend ;
- XR spatial index/picking ;
- adaptateur Desktop site IDs/frames ;
- tests/performance scenes.

## Étapes

1. Résoudre P10-B/C/E.
2. Prototyper backends GPU et grid/BVH avec D3.
3. Mesurer puis décider P10-A/D.
4. Implémenter buffers statiques/dynamiques et dirty ranges.
5. Implémenter ray/proximité dans le repère BrainInstance.
6. Ajouter hover local et sélection commandée/canonique.
7. Tester transparence, scaling, instances multiples et endurance.

## Tests et commandes

- D0 exactitude déterministe ;
- D3 37 500 sites, updates complètes/partielles ;
- picking ray/proche p50/p95/max et 100 % expected IDs ;
- profiler CPU/GPU/mémoire/draw calls ;
- scan hierarchy/components pour absence d'objets par site ;
- 30 min thermique avec 1/3/8 instances.

## Critères de sortie binaires

- [ ] P10-A–E enregistrées ;
- [ ] 37 500 sites présents sans plafond ;
- [ ] aucun objet/renderer/collider individuel ;
- [ ] exactitude 100 % sur D0 déterministe ;
- [ ] picking p95 < 50 ms sur D3 cible ;
- [ ] budgets 72 Hz D20 mesurés ;
- [ ] metadata/logs conformes D17.

## Artefacts à remettre

Backend sites, spatial index/picking, scènes D0/D3, mesures comparatives, tests et ADR P10.

## Conditions d'arrêt

Arrêter avant production si P10-B/C/E sont ambiguës, si aucun backend respecte les budgets sans retirer des sites ou si une metadata patient non autorisée devrait transiter.

## Prompt de démarrage

> Exécute P10 depuis `Docs/dev/xr/implementation-packets/P10-sites.md`. Résous d'abord les sémantiques P10-B/C/E, puis mesure des prototypes pour décider P10-A/D. N'implémente le backend production qu'après ces décisions. Le gate exige 37 500 sites, aucun objet par site, picking exact et aucun plafond fonctionnel.
