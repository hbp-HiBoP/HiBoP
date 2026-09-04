# P01 — bootstrap de la topologie XR

## Prérequis

- Unity `6000.5.2f1` ;
- Android Build Support avec SDK, NDK et OpenJDK pour cette version ;
- Git avec les dépendances Git de HiBoP accessibles.

## Ouvrir les projets

- Desktop : ouvrir la racine du dépôt.
- XR : ouvrir le dossier `XR/`.

Les projets possèdent des manifests et locks distincts. Tous deux consomment les mêmes sources sous `Shared/Packages/` par des références UPM `file:` relatives.

## Validation statique

Depuis la racine :

~~~powershell
./Tools/Validate-XRTopology.ps1
~~~

Ce contrôle vérifie les versions Unity, les références locales, les squelettes de packages, la couverture `.gitignore` des dossiers générés — dont `XR/.utmp/` —, l'absence de dossiers générés suivis, l'absence de copie de sources HiBoP, la limite de 50 MiB et l'absence de trigger GitHub Actions autre que `workflow_dispatch` ou `release`.

Le même contrôle peut être lancé manuellement dans **GitHub Actions > Validate XR topology > Run workflow**. Il n'est déclenché ni par un push ni par une pull request.

## Validation Unity locale

Lorsque l'éditeur est ouvert, utiliser Unity MCP pour exécuter les tests EditMode des assemblies :

- `CRNL.HiBoP.Contracts.Tests` ;
- `CRNL.HiBoP.RenderModel.Tests` ;
- `CRNL.HiBoP.Protocol.Tests`.

Lorsque l'éditeur est fermé, lancer Unity `6000.5.2f1` en batchmode hors sandbox avec `-runTests`. Les logs, XML et APK locaux vont sous `.artifacts/xr/`, qui est ignoré par Git.

Le build Android minimal utilise :

~~~text
-projectPath <repo>/XR
-buildTarget Android
-executeMethod CRNL.HiBoP.XR.Editor.P01Builder.BuildAndroid
-p01BuildOutput <repo>/.artifacts/xr/HiBoPXR-P01.apk
~~~

Le builder crée une scène vide temporaire pour le Player, puis la supprime. Il n'introduit aucune scène ou fonctionnalité XR.

Sur un hôte qui fournit à Java un répertoire temporaire Windows non utilisable par les canaux NIO, définir `TEMP` et `TMP` vers un dossier local ignoré tel que `.artifacts/xr/java-temp/` pour la durée de la commande Unity.

## Politique de code

- Sources communes : uniquement les packages `com.crnl.hibop.contracts`, `com.crnl.hibop.render-model` et `com.crnl.hibop.protocol` sous `Shared/Packages/`.
- Code Desktop uniquement : `Assets/`.
- Code XR uniquement : `XR/Assets/`.
- Aucun fichier HiBoP existant n'est déplacé ou copié.
- Aucun contrat métier concret n'est ajouté avant P02.

## Rollback

1. Retirer `XR/` et `Shared/Packages/`.
2. Restaurer `Packages/manifest.json` et `Packages/packages-lock.json`.
3. Retirer les ajouts P01 de `.gitignore`, de `.github/workflows/` et de `Tools/`.
4. Retirer l'ADR, ce guide et les preuves P01.

Aucun déplacement de fichier Desktop n'est à inverser.
