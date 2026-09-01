# P15 — CI, packaging et distribution pilote

## Objectif et résultat observable

Produire une paire Desktop/XR reproductible, signée, versionnée, licenciée et installable par un pilote non développeur via le canal Meta explicitement choisi, avec update/rollback documentés.

## Decision gate

**Hérité :** D18 versions distinctes/compatibilité, D19 distribution à spiker, D20 gates mesurées.

**Décisions externes obligatoires :**

- `P15-A` : organisation Meta propriétaire, vérification et responsables ;
- `P15-B` : canal pilote exact, population et politique d'accès ;
- `P15-C` : application ID final, nom produit, signature/keystore et custody ;
- `P15-D` : matrice OS Desktop/Quest supportée et fenêtre de compatibilité ;
- `P15-E` : versioning/release/rollback et cadence coordonnée ;
- `P15-F` : licences/notices/SBOM et responsables de validation ;
- `P15-G` : critères go/no-go et autorité de lancement.

Revalider les règles Meta officielles au moment de la phase. Sans organisation, signature ou canal explicitement fournis, statut `BLOCKED` et aucune distribution externe.

## Périmètre autorisé

- CI packages/Desktop/APK ;
- version stamping, protocol matrix et artefacts ;
- signature via secrets approuvés ;
- SBOM/notices ;
- upload canal pilote, install/update/rollback ;
- diagnostics support.

## Hors périmètre

- publication Production Store sans nouvelle autorisation ;
- achat/création de comptes ;
- exposition de clés dans repo/log ;
- modification fonctionnelle pour contourner un gate.

## Hypothèses fixées

- P13/P14 sont acceptés ;
- D20 mesuré ;
- pilote limité ;
- sideload réservé au développement ;
- aucun secret fourni dans un prompt/document.
- les workflows sont lancés uniquement par `workflow_dispatch` ou par `release: published` pour une release créée manuellement ; aucun trigger `push`, `pull_request` ou planifié.

## Dépendances et état initial

- toutes phases fonctionnelles intégrées ;
- P14 sécurité signée ;
- machines de build 3 OS et environnement Android disponibles ;
- comptes/permissions confirmés par utilisateur.

## Fichiers/modules pressentis

- CI/workflows/build scripts ;
- manifests/version files ;
- packaging/licences/SBOM ;
- docs installation/support/release.

## Étapes

1. Résoudre P15-A–G et vérifier règles Meta actuelles.
2. Définir version matrix et stamping.
3. Construire CI packages, Desktop 3 OS et APK.
4. Intégrer signature via secret store.
5. Générer SBOM/notices/symbols.
6. Exécuter suite go/no-go P00–P14.
7. Uploader sur canal choisi avec autorisation.
8. Faire installer/update/rollback par pilote non développeur.
9. Archiver checksums, release notes et runbook.

## Tests et commandes

- clean builds reproductibles et checksums expliqués ;
- tests protocol N/N et fenêtre P15-D ;
- APK install/update/rollback ;
- scan secrets/dependencies/licences ;
- validation Meta packaging/policies ;
- smoke end-to-end pilote et diagnostics.

## Critères de sortie binaires

- [ ] P15-A–G approuvées ;
- [ ] builds Desktop 3 OS + APK reproductibles ;
- [ ] signature sans fuite de secret ;
- [ ] compatibilité version testée ;
- [ ] SBOM/notices complets ;
- [ ] installation/update/rollback par pilote réussis ;
- [ ] go/no-go signé ;
- [ ] aucune publication Production non autorisée.

## Artefacts à remettre

CI, checksums, SBOM/notices, version matrix, runbooks, rapport pilote et ADR/release record. Les builds et symboles restent des artefacts de workflow ou de release, jamais des fichiers du dépôt.

## Conditions d'arrêt

Arrêter si compte/organisation/permission/signature manque, si les règles Meta ont changé sans décision, si D20/P14 échoue ou avant toute action externe non explicitement autorisée.

## Prompt de démarrage

> Exécute P15 depuis `Docs/dev/xr/implementation-packets/P15-industrialization.md`. Vérifie d'abord P15-A–G et les règles Meta officielles actuelles. N'effectue aucun upload ni action externe sans autorisation explicite et ne manipule aucun secret en clair. Construis ensuite CI, packaging, licences, version matrix et parcours pilote avec update/rollback.
