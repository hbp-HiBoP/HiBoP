# ADR P01 — topologie du monorepo et du projet Unity XR

- **Statut :** ACCEPTED — IMPLEMENTED
- **Date :** 2026-09-01
- **Accepté par :** propriétaire du dépôt HiBoP
- **Baseline inspectée :** branche `feature/xr`, commit `ae911f8bb5361e4f6da1ea6f992744a3ac4c4687`
- **Décisions héritées :** deux applications Unity connectées, HiBoP Desktop autoritaire, aucune copie du code Desktop et aucune dépendance Quest vers l'intégralité de `HBP.Core.Runtime` ou `HBP.Data.Runtime`

## Contexte observé

- Le projet Desktop occupe la racine du dépôt : `Assets/`, `Packages/` et `ProjectSettings/` représentent l'essentiel des fichiers suivis.
- La CI, les caches et plusieurs outils utilisent la racine comme `projectPath`. Déplacer le Desktop produirait une migration massive sans bénéfice fonctionnel.
- Desktop utilise Unity `6000.5.2f1` révision `eb73d3b415a1`. Cette version et ses modules Android Player, SDK, NDK et OpenJDK sont installés localement.
- Les ignores Unity existants sont ancrés à la racine et ne couvrent pas les dossiers générés d'un projet situé sous `XR/`.
- Aucun fichier n'est actuellement suivi avec Git LFS. P01 ne crée que des fichiers texte et ne justifie ni LFS prospectif ni migration historique.

## P01-A — layout physique

### Décision

Conserver le projet Desktop à la racine et créer un second projet Unity sous `XR/` :

```text
HiBoP/
├── Assets/                    # projet Desktop existant
├── Packages/                  # manifest/lock Desktop
├── ProjectSettings/           # settings Desktop
├── Shared/
│   └── Packages/              # uniquement les sources consommées par les deux projets
└── XR/
    ├── Assets/                # code, scènes et assets exclusivement XR
    ├── Packages/              # manifest/lock XR
    └── ProjectSettings/       # settings XR
```

Le projet Desktop ne sera pas déplacé sous `Desktop/`. Un éventuel déplacement futur exige un ADR et une migration séparés.

### Motivation

Le monorepo permet de modifier atomiquement HiBoP, les frontières partagées et le client XR, puis de vérifier les deux applications dans une même exécution de validation lancée manuellement. Les deux projets Unity restent physiquement séparés afin d'isoler leurs manifests, locks, scènes, assets et `ProjectSettings`.

## P01-B — version Unity

### Décision

Les deux projets utilisent exactement Unity **`6000.5.2f1`**.

Toute mise à niveau est coordonnée et vérifiée sur les deux projets. Une divergence exige une contrainte plateforme démontrée, un ADR explicite et une CI compilant les sources partagées sur les deux versions.

Les manifests et locks restent distincts. L'alignement de l'éditeur ne doit pas ajouter OpenXR, XRI, Meta ou une configuration Android au projet Desktop.

## P01-C — propriété et emplacement du code

### Décision

Ne placer sous `Shared/Packages/` que les nouvelles sources réellement consommées par les deux applications :

- `com.crnl.hibop.contracts` ;
- `com.crnl.hibop.render-model` ;
- `com.crnl.hibop.protocol`.

Le code exclusivement Desktop reste sous `Assets/`. Le code exclusivement XR reste sous `XR/Assets/`. Aucun package `desktop-bridge`, `xr-client` ou `rendering` partagé n'est créé par anticipation.

Aucun fichier existant de HiBoP n'est déplacé vers `Shared/Packages/`. Les modèles HiBoP ne reçoivent ni méthode `ToDTO`/`FromDTO`, ni dépendance vers les contrats, le protocole ou XR. Les futurs mappings seront réalisés de l'extérieur par un bridge Desktop.

Références locales, relatives au dossier `Packages` de chaque projet et écrites avec `/` :

```jsonc
// Packages/manifest.json — Desktop
"com.crnl.hibop.contracts": "file:../Shared/Packages/com.crnl.hibop.contracts"

// XR/Packages/manifest.json — XR
"com.crnl.hibop.contracts": "file:../../Shared/Packages/com.crnl.hibop.contracts"
```

La même règle s'applique aux deux autres packages. Chaque projet versionne son propre `packages-lock.json`.

P01 crée uniquement les manifests UPM, asmdefs Runtime/Test et tests de compilation minimaux. Il ne définit aucun identifiant métier, DTO de rendu, message réseau, renderer, transport ou interaction. Les responsabilités futures sont :

- **Contracts :** identités, intentions, état logique, résultats et erreurs ;
- **RenderModel :** données minimales prêtes à afficher, indépendantes des modèles scientifiques HiBoP ;
- **Protocol :** enveloppes, compatibilité et sérialisation des échanges.

`RenderModel` dépendra conceptuellement de `Contracts`, et `Protocol` de `Contracts` et `RenderModel`. Les dépendances d'API seront matérialisées seulement lorsque P02/P03 définiront les premiers types.

## P01-D — Git, ignores et artefacts

### Décision

Ne pas utiliser Git LFS dans P01.

- Ajouter les ignores explicites pour les dossiers générés sous `XR/`, notamment `.utmp`, `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`, `MemoryCaptures`, `UserSettings` et `ProfilerCaptures`.
- Ignorer les captures profiler à la racine et sous XR.
- Stocker APK, builds, logs, captures et résultats de tests locaux sous `/.artifacts/xr/`, hors Git.
- Autoriser sous `Docs/dev/xr/evidence/` uniquement de petites preuves textuelles nettoyées et des hashes explicitement promus ; builds, captures, images, buffers, logs et résultats bruts restent hors Git.
- Refuser en CI les dossiers Unity générés suivis, les artefacts XR bruts et tout nouveau fichier d'au moins 50 MiB sous `XR/`, `Shared/` ou `Docs/dev/xr/evidence/`.
- Ne modifier ni les gros fichiers existants ni l'historique Git. Réexaminer LFS seulement si un futur asset réel l'exige.
- Limiter les workflows GitHub Actions XR à `workflow_dispatch` ou à l'événement `release: published` lorsqu'un workflow produit les artefacts d'une release créée manuellement. Aucun déclenchement automatique sur `push`, `pull_request` ou planification n'est autorisé.

## Implémentation autorisée par P01

P01 peut uniquement :

1. créer le projet Unity minimal `XR/` en `6000.5.2f1`, sans OpenXR, XRI, Meta, rig ou fonctionnalité ;
2. créer les trois squelettes UPM partagés sans API métier ;
3. relier les sources uniques aux manifests des deux projets et versionner leurs locks ;
4. ajouter les ignores et contrôles CI minimaux ;
5. vérifier séparément résolution UPM, compilation, tests et builds/smoke tests des deux projets ;
6. documenter le bootstrap, les preuves et le rollback.

Ne sont pas autorisés : migration du Desktop, ajout de `hbp_core` au Quest, package XR, contrat métier, renderer, transport, interaction, donnée réelle ou modification fonctionnelle HiBoP.

## Critères de sortie

- [x] Deux projets Unity s'ouvrent en `6000.5.2f1`.
- [x] Les trois mêmes packages sources sont résolus et compilés par les deux projets.
- [x] Les tests de compilation minimaux passent dans les deux projets.
- [x] Le projet Desktop et sa CI existante ne régressent pas.
- [x] Le projet XR produit un Player Android minimal.
- [x] Aucune source ou asset HiBoP n'est copié.
- [x] Aucun dossier généré ou artefact brut n'est suivi.
- [x] Bootstrap, preuves et rollback sont documentés.

## Rollback

La topologie est additive. Le rollback consiste à retirer `XR/` et `Shared/Packages/`, restaurer le manifest/lock Desktop et supprimer les ajouts P01 aux ignores, à la CI et à la documentation. Aucun déplacement de fichier Desktop n'est à inverser.
