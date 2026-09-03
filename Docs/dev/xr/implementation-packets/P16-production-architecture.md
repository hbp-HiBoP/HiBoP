# P16 — architecture de production et normalisation

## Objectif et résultat observable

Transformer la tranche verticale P15 en architecture maintenable et nommée selon les conventions explicitement acceptées : namespaces, assemblies, packages, APIs, classes, fichiers, scènes et prefabs de production deviennent cohérents sans modifier le comportement utilisateur, scientifique ou réseau.

## Decision gate

**Hérité :** frontières D01–D04, trois packages partagés seulement, séparation Desktop/XR, compatibilité D18, parcours P15 fonctionnel et prefab-first.

**À résoudre avant tout renommage transversal :**

- `P16-A` : convention finale des namespaces C# par domaine, y compris le devenir exact de `CRNL.HiBoP.*` et la relation avec les namespaces `HBP.*` existants ;
- `P16-B` : noms finaux et stabilité des assemblies et identifiants UPM, décidés séparément des namespaces C# ;
- `P16-C` : convention des noms de classes/fichiers/assets de production et politique exacte pour les préfixes `Pxx` ;
- `P16-D` : frontières d'API publique/interne, ownership des services et dépendances autorisées entre packages, Desktop et XR ;
- `P16-E` : stratégie de migration des références Unity sérialisées, GUID `.meta`, asmdefs, tests, schemas et consommateurs ;
- `P16-F` : compatibilité wire et versioning requis si un renommage touche contrats, types sérialisés ou schema hash ;
- `P16-G` : inventaire des composants retenus comme production et propriétaire qui accepte la nomenclature/architecture finales.

Un namespace C#, un nom d'assembly et un identifiant UPM sont trois décisions distinctes. Aucun remplacement global, déplacement d'asset ou renommage de type public n'est autorisé avant P16-A–G.

## Périmètre autorisé

- inventaire et graphe de dépendances du chemin P15 ;
- renommage et déplacement contrôlés du code retenu comme production ;
- normalisation namespaces/classes/fichiers/asmdefs selon les décisions ;
- réduction de visibilité et interfaces explicites ;
- migration des références de scènes/prefabs/tests ;
- shims temporaires strictement bornés et documentés ;
- tests de parité avant/après sans changement fonctionnel.

## Hors périmètre

- ajout ou retrait de fonction produit ;
- changement du protocole ou du résultat scientifique pour simplifier les noms ;
- suppression massive de spikes/démonstrations, réservée à P17 après classification ;
- extension de `Shared/Packages/` sans réouverture D03 ;
- distribution/signature Meta ;
- nettoyage unrelated du HiBoP Desktop historique.

## Hypothèses fixées

- P15 fournit un chemin fonctionnel servant d'oracle avant/après ;
- les ADR et preuves P00–P15 conservent leurs noms historiques ;
- les GUID Unity sont préservés lorsque l'identité logique de l'asset ne change pas ;
- les renommages sont effectués par lots vérifiables et réversibles ;
- toute rupture wire ou package exige une version/migration explicite ;
- aucune convention n'est déduite uniquement du souhait de faire disparaître un préfixe.

## Dépendances et état initial

- P15 accepté sur un parcours physique reproductible ;
- worktree et références sérialisées inventoriés ;
- liste des consommateurs externes des packages/assemblies connue ou absence enregistrée ;
- versions protocol/schema/build fixées avant migration ;
- Unity disponible pour valider les assets après déplacement/renommage.

## Fichiers/modules pressentis

- sources de production sous `Assets/`, `XR/Assets/` et `Shared/Packages/` ;
- asmdefs, manifests et tests associés ;
- scènes/prefabs canoniques issus de P15 ;
- ADR P16, table de mapping ancien → nouveau et règles de nomenclature ;
- aucun fichier d'evidence P00–P15 renommé pour embellir l'historique.

## Étapes

1. Cartographier types, namespaces, assemblies, packages, scènes, prefabs et dépendances réellement parcourus par P15.
2. Classifier production, test, outil développeur, preuve et candidat à suppression future.
3. Résoudre P16-A–G et publier le mapping complet avant modification.
4. Établir l'ordre de migration et les shims temporaires, avec rollback par lot.
5. Renommer/déplacer le code de production en préservant GUID et références sérialisées.
6. Normaliser APIs, visibilité et dépendances sans changer les données ni l'ordre d'exécution.
7. Migrer tests et outils nécessaires vers les nouveaux noms.
8. Supprimer les shims de migration dès que leurs consommateurs sont migrés.
9. Rejouer le parcours P15 et comparer comportement, wire, images et métriques.

## Tests et commandes

- compilation et tests de chaque assembly/package après chaque lot ;
- validation asmdef et absence de cycle/dépendance interdite ;
- chargement des scènes/prefabs sans `Missing Script` ni référence perdue ;
- comparaison GUID/références sérialisées pour les assets conservés ;
- tests de compatibilité protocol/schema selon P16-F ;
- recherche des anciens namespaces/noms interdits avec allowlist historique ;
- builds Desktop/APK et parcours P15 avant/après ;
- formatter C# obligatoire et `git diff --check`.

## Critères de sortie binaires

- [ ] P16-A–G acceptées et mapping ancien → nouveau archivé ;
- [ ] tous les composants de production P16-G respectent les conventions décidées ;
- [ ] namespaces, assemblies et package IDs ne sont pas confondus ou modifiés implicitement ;
- [ ] aucun `Missing Script`, GUID perdu ou référence prefab/scène cassée ;
- [ ] aucune dépendance nouvelle ne viole D01–D04 ;
- [ ] wire/schema et consommateurs restent compatibles selon P16-F ;
- [ ] aucun shim temporaire non justifié ne reste dans le chemin production ;
- [ ] le parcours P15 et ses métriques restent dans les seuils acceptés ;
- [ ] aucun comportement Desktop ou XR n'a changé hors migration explicitement décidée.

## Artefacts à remettre

Architecture/nomenclature finales, mapping de migration, code et assets normalisés, tests adaptés, graphe de dépendances, ADR P16 et rapport de parité avant/après.

## Conditions d'arrêt

Arrêter si un consommateur externe est inconnu, si un renommage exige une rupture non acceptée, si Unity perd une référence sérialisée, si le parcours P15 diverge ou si la normalisation nécessite une nouvelle décision fonctionnelle.

## Prompt de démarrage

> Exécute P16 depuis `Docs/dev/xr/implementation-packets/P16-production-architecture.md`. Inventorie d'abord uniquement le chemin production P15, puis fais accepter P16-A–G. Distingue namespaces C#, assemblies et identifiants UPM ; ne lance aucun remplacement global avant le mapping. Migre par lots réversibles, préserve les GUID Unity et prouve que le parcours end-to-end reste strictement équivalent.
