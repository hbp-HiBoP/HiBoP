# P17 — cleanup, consolidation et durcissement

## Objectif et résultat observable

Retirer ou isoler l'échafaudage accumulé pendant P01–P15, réduire la surface embarquée et livrer deux projets dont les entrées produit, outils développeur, tests et preuves historiques sont clairement séparés. Le comportement P15 et l'architecture P16 restent inchangés.

## Decision gate

**Hérité :** classification initiale P16-G, architecture/nomenclature P16, invariants de sécurité P14 et parcours P15.

**À résoudre avant toute suppression ou dépublication :**

- `P17-A` : classification finale de chaque scène, prefab, sonde, bootstrap, spike, fixture, outil et dépendance en production/test/dev/preuve/obsolète ;
- `P17-B` : politique de conservation et niveau de reproductibilité requis pour les preuves P00–P15 ;
- `P17-C` : scènes, prefabs et composition roots canoniques des Players Desktop/XR ;
- `P17-D` : scripts et commandes canoniques de test, build, lancement du host/sidecar et déploiement Quest ;
- `P17-E` : dépendances, données de démonstration et symboles autorisés dans les Players finaux ;
- `P17-F` : critères de suppression, méthode de rollback et propriétaire qui accepte la disparition de chaque catégorie ;
- `P17-G` : budget final de dette résiduelle et allowlist documentée des noms/artefacts historiques conservés.

La présence d'un préfixe Pxx, d'un nom `Synthetic`, `Demo`, `Probe` ou `Spike` ne suffit pas à autoriser une suppression. Aucun fichier n'est retiré avant classification et vérification de ses références.

## Périmètre autorisé

- suppression ou déplacement des éléments classés obsolètes ;
- isolation Editor/Test/Development des outils et preuves conservés ;
- retrait des injections synthétiques et données de démonstration du Player produit ;
- consolidation des scènes, prefabs, composition roots et scripts de build ;
- suppression de dépendances et APIs mortes ;
- réduction de visibilité, code mort et chemins concurrents après preuve d'inutilisation ;
- durcissement des validations empêchant le retour de l'échafaudage dans les builds.

## Hors périmètre

- changement d'UX, de contrat scientifique ou de comportement réseau ;
- nouvel algorithme ou optimisation non nécessaire au retrait de dette ;
- réécriture des ADR/preuves historiques ;
- suppression de données utilisateur, de projets ou d'artefacts non classifiés ;
- industrialisation, signature et upload Meta ;
- cleanup général du Desktop sans lien démontré avec XR.

## Hypothèses fixées

- la tranche P15 et les mappings P16 constituent la référence fonctionnelle ;
- les fichiers sous `.artifacts/xr/` restent hors Git et ne sont pas des dépendances produit ;
- les preuves documentaires peuvent rester historiques même si leur code de génération est isolé ;
- tout asset conservé mais non produit est exclu des scènes/builds finaux ;
- les suppressions sont ciblées, revues et vérifiables ;
- aucune baisse de couverture n'est acceptée sans remplacement ou décision P17-B.

## Dépendances et état initial

- P16 accepté et aucun shim de migration injustifié restant ;
- inventaire complet des références code/asmdef/scène/prefab/build ;
- parcours P15 reproductible avant cleanup ;
- tailles des Players, dépendances et contenu embarqué capturés comme baseline ;
- worktree utilisateur cartographié avant toute suppression.

## Fichiers/modules pressentis

- scènes/prefabs/sondes P04–P10 et leurs outils de build/profiling ;
- `Spikes/P06/` et dépendances candidates, selon classification P17-A/B ;
- données synthétiques/locales embarquées et assemblies de validation ;
- manifests/asmdefs/build settings des deux projets ;
- composition roots et scripts produit décidés en P17-C/D ;
- ADR P17, inventaire de suppression et rapport de contenu des Players.

## Étapes

1. Capturer la baseline fonctionnelle, taille, dépendances et contenu des builds.
2. Construire l'inventaire référencé et résoudre P17-A–G.
3. Retirer d'abord des build settings et Players les éléments non produit, sans les supprimer immédiatement.
4. Vérifier le parcours P15 avec les seules composition roots P17-C.
5. Isoler sous Editor/Test/Development les outils et preuves conservés.
6. Supprimer par lots les éléments obsolètes, après recherche de références et contrôle des chemins exacts.
7. Retirer dépendances, APIs, shims et code mort devenus sans consommateur.
8. Consolider les commandes P17-D et ajouter des validations anti-régression.
9. Refaire builds, scans, tests, appareil et comparaison de taille/métriques.

## Tests et commandes

- recherche de références avant/après chaque lot et validation des GUID ;
- compilation/tests de tous les packages et assemblies touchés ;
- ouverture/validation de toutes les scènes et prefabs canoniques ;
- scan `Missing Script`, dépendances asmdef/UPM/NuGet et code inaccessible ;
- inspection du contenu Desktop/Quest pour scènes, données, symboles et assemblies interdits ;
- comparaison des tailles avant/après et justification de toute hausse ;
- scénarios P15 réels, P14 lifecycle et régressions P05/P08/P09/P10/P11/P12 ;
- build Desktop et APK release-like puis test Quest physique ;
- formatter C# obligatoire et `git diff --check`.

## Critères de sortie binaires

- [ ] P17-A–G acceptées et inventaire de chaque suppression archivé ;
- [ ] une seule composition root produit existe par application selon P17-C ;
- [ ] aucun spike, demo, probe, fixture ou payload synthétique non autorisé n'entre dans les Players ;
- [ ] aucun code, assembly ou package sans consommateur n'est conservé hors allowlist P17-G ;
- [ ] les preuves P00–P15 restent reproductibles au niveau décidé P17-B ;
- [ ] scripts de build/test/lancement canoniques documentés et vérifiés ;
- [ ] aucun `Missing Script`, référence cassée, secret ou donnée patient dans les builds/logs ;
- [ ] le parcours P15 passe sur Quest après cleanup ;
- [ ] la couverture fonctionnelle, scientifique et sécurité ne régresse pas ;
- [ ] taille, dépendances et dette résiduelle sont mesurées et acceptées.

## Artefacts à remettre

Inventaire classifié, liste des suppressions et rollback, projets consolidés, validations de contenu des Players, scripts canoniques, métriques avant/après, ADR P17 et rapport final de cleanup.

## Conditions d'arrêt

Arrêter si un élément n'est pas classifiable, si une référence ou un propriétaire manque, si une preuve exigée deviendrait irréproductible, si le parcours P15 régresse ou avant toute suppression large dont les cibles exactes et le rollback ne sont pas validés.

## Prompt de démarrage

> Exécute P17 depuis `Docs/dev/xr/implementation-packets/P17-cleanup-hardening.md`. Commence par capturer la baseline et faire accepter P17-A–G. Ne supprime rien sur la seule base d'un nom Pxx/Demo/Synthetic : classe et vérifie chaque référence. Isole d'abord le non-produit, supprime ensuite par lots ciblés, puis prouve sur un Quest physique que le parcours P15 et toutes les garanties P14/P16 sont inchangés.
