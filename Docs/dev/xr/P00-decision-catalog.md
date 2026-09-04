# P00 — fiche de décision datasets et baselines

**Statut :** `GO`
**Date du gate :** 31 août 2026
**Paquet :** [P00](implementation-packets/P00-baselines.md)

## Résultat du Decision gate

P00-A–D ont été résolues explicitement par l'utilisateur le 31 août 2026. P00
utilise exclusivement des fixtures synthétiques versionnables et les assets MNI
déjà suivis par Git. Aucune donnée patient réelle ni aucun chemin externe ne
sera utilisé.

| Décision | État | Élément manquant |
| --- | --- | --- |
| P00-A | RÉSOLUE | chemins versionnés D0–D6 définis ci-dessous |
| P00-B | RÉSOLUE | synthétiques versionnables ; MNI déjà versionné ; aucune donnée réelle externe |
| P00-C | RÉSOLUE | option C1 adaptée, sans catalogue de données réelles |
| P00-D | RÉSOLUE | l'utilisateur/mainteneur HiBoP valide les références visuelles et tolérances |

Avant cette résolution, aucune fixture, donnée réelle, capture, golden output,
hash ou métrique P00 n'avait été produit. Le worktree était propre au moment du
gate initial.

## P00-A/B — catalogue accepté

Les descriptions viennent du plan de validation. Elles ne valent ni chemin
local ni autorisation.

| ID | Contenu attendu | Chemin local exact | Propriétaire | Classification | Tests | Artefacts redacted | Versionnement |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D0 | géométrie synthétique minuscule, IDs connus | `Assets/Tests/Fixtures/XR/Baselines/D0/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui | oui |
| D1 | MNI versionné, 69 104 sommets/138 216 faces | `Assets/Data/IRM/MNI.nii` et trois meshes `MNI_single_hight_B*.obj` consignés au manifeste | HiBoP | `PROJECT_ASSET_VERSIONED` | oui | oui | déjà versionné, sans copie |
| D2 | visualisation synthétique MNI avec sites/coupes | `Assets/Tests/Fixtures/XR/Baselines/D2/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui | oui |
| D3 | 250 groupes synthétiques × 150 sites, 8 colonnes | `Assets/Tests/Fixtures/XR/Baselines/D3/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui | oui |
| D4 | volume/grille/overlays synthétiques lourds | `Assets/Tests/Fixtures/XR/Baselines/D4/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui | oui |
| D5 | signal synthétique avec alpha connu | `Assets/Tests/Fixtures/XR/Baselines/D5/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui | oui |
| D6 | sentinelles non sensibles | `Assets/Tests/Fixtures/XR/Baselines/D6/fixture.json` | HiBoP | `SYNTHETIC_PUBLIC` | oui | oui, après redaction | oui |

Classifications disponibles :

- `SYNTHETIC_PUBLIC` : synthétique, sans donnée réelle, versionnable après revue ;
- `REAL_NON_PATIENT_LOCAL` : réel non patient, local sauf autorisation distincte ;
- `PATIENT_SENSITIVE_LOCAL` : sensible, local uniquement, jamais copié ou journalisé ;
- `UNCLASSIFIED` : interdit pour P00 jusqu'à classification.

L'autorisation doit distinguer lecture locale, tests, statistiques agrégées,
hashes, images redacted et contenu source. Un usage autorisé n'autorise pas les
autres.

## P00-C — options d'emplacement

### C1 — séparation stricte, acceptée

- fixtures synthétiques approuvées :
  `Assets/Tests/Fixtures/XR/Baselines/<D0|D2|D3|D4|D5|D6>/` ;
- manifestes/procédures/rapports redacted : `Docs/dev/xr/baselines/` ;
- golden synthétiques approuvés :
  `Assets/Tests/Fixtures/XR/Baselines/Expected/` ;
- aucune donnée réelle externe ; D1 référence uniquement les assets MNI déjà
  versionnés et D2–D4 deviennent des proxys synthétiques ;
- captures/logs/résultats non publiables sous
  `.test-results/xr/p00/`.

`.test-results/` est déjà ignoré par Git. Le manifeste public utilise uniquement
D0–D6 et des identifiants opaques autorisés, sans chemin réel ni nom
patient/centre/étude/protocole/projet.

**Conséquences :** la CI est reproductible sans projet local. La diversité et
les distributions de vraies visualisations ne sont pas prouvées automatiquement
et restent couvertes par la revue visuelle manuelle de l'utilisateur.

### C2 — tout hors dépôt

Fixtures, golden outputs et catalogue restent dans un stockage externe ; seule
la procédure est versionnée. Confidentialité simple, mais reproductibilité CI
et revue synthétique plus faibles.

### C3 — stockage versionné spécialisé

Un dépôt de données ou stockage d'artefacts à accès contrôlé fournit des
révisions immuables. Il faut alors décider service, droits, rétention et accès
CI. Cela ne rend jamais des données patient publiables.

## P00-D — autorité enregistrée

La décision enregistrée donne :

- autorité : utilisateur/mainteneur HiBoP ;
- périmètre : surface, sites, coupes et interpolation D5 ;
- suppléant : aucun désigné ;
- acceptation traçable : confirmation écrite dans le fil Codex du 31 août 2026,
  puis ajout de la validation visuelle au rapport P00 lorsqu'elle est réalisée ;
- confidentialité P00-B : acceptée par l'utilisateur dans le périmètre limité
  aux synthétiques et assets déjà versionnés.

Cette autorité valide résultats attendus et tolérances par fonction. Aucune
tolérance scientifique ne sera déduite du comportement actuel.

## Décision enregistrée

L'utilisateur a accepté C1, autorisé la génération, les tests et le
versionnement de D0 et D2–D6, autorisé l'usage de D1, et pris la responsabilité
de la validation visuelle. Les tolérances numériques synthétiques doivent être
exactes ou dérivées explicitement de l'arithmétique utilisée ; toute tolérance
visuelle nouvelle reste soumise à son acceptation.

## Preuves produites après GO

- manifeste public D0–D6 sans donnée sensible ni catalogue privé ;
- provenance/version/hash des quatre assets D1 autorisés ;
- golden exact surface, site, coupe et D5 ;
- commandes, commit, Unity/OS, répétitions et métriques Desktop redacted ;
- scan D6 des artefacts/logs sans occurrence ;
- procédure de régénération exécutée deux fois avec sortie byte-identique.

## Fichiers affectés après GO

- `Docs/dev/xr/baselines/` ;
- `Assets/Tests/Fixtures/XR/Baselines/` ;
- tests/outils ciblés de projection et Module3D ;
- `.test-results/xr/p00/` pour les éléments privés uniquement.
