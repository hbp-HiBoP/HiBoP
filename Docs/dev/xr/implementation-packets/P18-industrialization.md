# P18 — CI, packaging et distribution pilote

## Objectif et résultat observable

Produire une paire Desktop/XR reproductible, signée, versionnée, licenciée et installable par un pilote non développeur via le canal Meta explicitement choisi, avec update/rollback documentés.

## Decision gate

**Hérité :** D18 versions distinctes/compatibilité, D19 distribution à spiker, D20 gates mesurées, parcours produit P15, architecture P16 et cleanup P17.

**Décisions externes obligatoires :**

- `P18-A` : organisation Meta propriétaire, vérification et responsables ;
- `P18-B` : canal pilote exact, population et politique d'accès ;
- `P18-C` : application ID final, nom produit, signature/keystore et custody ;
- `P18-D` : matrice OS Desktop/Quest supportée et fenêtre de compatibilité ;
- `P18-E` : versioning/release/rollback et cadence coordonnée ;
- `P18-F` : licences/notices/SBOM et responsables de validation ;
- `P18-G` : critères go/no-go et autorité de lancement.

Revalider les règles Meta officielles au moment de la phase. Sans organisation, signature ou canal explicitement fournis, statut `BLOCKED` et aucune distribution externe.

## Périmètre autorisé

- CI packages/Desktop/APK ;
- version stamping, protocol matrix et artefacts ;
- signature via secrets approuvés ;
- SBOM/notices ;
- upload canal pilote, install/update/rollback ;
- diagnostics support ;
- validation release-like du parcours P15 sur la base nettoyée P17.

## Hors périmètre

- publication Production Store sans nouvelle autorisation ;
- achat/création de comptes ;
- exposition de clés dans repo/log ;
- modification fonctionnelle pour contourner un gate ;
- restauration d'un spike, d'une scène de démonstration ou d'une dépendance retirée pour faire passer le packaging.

## Hypothèses fixées

- P15–P17 sont acceptés ;
- P13/P14 sont acceptés ;
- D20 mesuré ;
- pilote limité ;
- sideload réservé au développement ;
- aucun secret fourni dans un prompt/document ;
- les workflows sont lancés uniquement par `workflow_dispatch` ou par `release: published` pour une release créée manuellement ; aucun trigger `push`, `pull_request` ou planifié.

## Dépendances et état initial

- toutes phases fonctionnelles et d'intégration intégrées ;
- P14 sécurité signée ;
- P17 cleanup accepté et composition roots finales gelées ;
- machines de build 3 OS et environnement Android disponibles ;
- comptes/permissions confirmés par utilisateur.

## Fichiers/modules pressentis

- CI/workflows/build scripts ;
- manifests/version files ;
- packaging/licences/SBOM ;
- docs installation/support/release.

## Étapes

1. Résoudre P18-A–G et vérifier règles Meta actuelles.
2. Définir version matrix et stamping.
3. Construire CI packages, Desktop 3 OS et APK.
4. Intégrer signature via secret store.
5. Générer SBOM/notices/symbols.
6. Exécuter suite go/no-go P00–P17, dont le parcours P15 release-like.
7. Uploader sur canal choisi avec autorisation.
8. Faire installer/update/rollback par pilote non développeur.
9. Archiver checksums, release notes et runbook.

## Tests et commandes

- clean builds reproductibles et checksums expliqués ;
- tests protocol N/N et fenêtre P18-D ;
- APK install/update/rollback ;
- scan secrets/dependencies/licences ;
- validation Meta packaging/policies ;
- smoke end-to-end P15 sur artefacts release-like ;
- vérification que le contenu des Players reste conforme à P17 ;
- diagnostics support sans réintroduire de contenu sensible.

## Critères de sortie binaires

- [ ] P18-A–G approuvées ;
- [ ] builds Desktop 3 OS + APK reproductibles ;
- [ ] signature sans fuite de secret ;
- [ ] compatibilité version testée ;
- [ ] SBOM/notices complets ;
- [ ] parcours P15 réussi avec les artefacts effectivement distribués ;
- [ ] contenu des Players conforme à P17 ;
- [ ] installation/update/rollback par pilote réussis ;
- [ ] go/no-go signé ;
- [ ] aucune publication Production non autorisée.

## Artefacts à remettre

CI, checksums, SBOM/notices, version matrix, runbooks, rapport pilote et ADR/release record. Les builds et symboles restent des artefacts de workflow ou de release, jamais des fichiers du dépôt.

## Conditions d'arrêt

Arrêter si compte/organisation/permission/signature manque, si les règles Meta ont changé sans décision, si D20/P14/P15/P17 échoue ou avant toute action externe non explicitement autorisée.

## Prompt de démarrage

> Exécute P18 depuis `Docs/dev/xr/implementation-packets/P18-industrialization.md`. Vérifie d'abord P18-A–G et les règles Meta officielles actuelles. N'effectue aucun upload ni action externe sans autorisation explicite et ne manipule aucun secret en clair. Construis ensuite CI, packaging, licences et version matrix, puis valide le parcours P15 sur les artefacts release-like et le parcours pilote avec update/rollback.
