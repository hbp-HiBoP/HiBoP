# Fixture anatomique MNI

Projet `quest-mni-anatomy`, visualisation et unique colonne `MNI Anatomy`, sans patient, groupe, dataset, site, coupe ou vue enregistrée. Elle sélectionne `MNI Grey matter`, les deux hémisphères, la représentation anatomique et l'IRM `MNI`. Les six IDs fixes et huit hashes source sont dans [manifest.json](manifest.json).

Depuis la racine du dépôt :

```powershell
.\Tools\Prepare-QuestAnatomyFixture.ps1
```

Sortie : `.artifacts/quest-001/fixture/quest-mni-anatomy.hibop`. `-OutputDirectory` change le dossier ; les chemins relatifs sont résolus depuis le dépôt. Le script vérifie les hashes et crée les quatre répertoires de projet requis, dont Patients/Groups/Datasets vides. Les dates ZIP sont fixes (2000-01-01), l'ordre déterministe et les textes UTF-8 sans BOM/LF. Le NIfTI est hashé sur ses octets bruts. Aucun ancien cache ou artefact local n'est une entrée nécessaire.

La référence est celle de `Assets/Data/IRM/MNI.nii`, `MNI.trm` et des quatre GII gris/blanc de `Assets/Data/Meshes` au commit `b230166204024b5dd350d8fe897aa854160ff384`. L'origine scientifique amont exacte n'est pas déduite du nom MNI. Les petits GII sous `Assets/Tests/Fixtures/Native/Meshes` sont synthétiques et ne constituent pas ce cerveau complet. Cette fixture n'ajoute aucun signal ni dossier patient ; cela ne qualifie pas le volume de référence de synthétique.

Le chargeur commun `MNIObjects.LoadDataAsync` charge NIfTI/GII/TRM, adapte le winding, assemble les hémisphères et calcule les normales. Les distances restent en mm avec conversion X native/Unity existante ; cette fixture n'ajoute ni conversion Quest mm→m ni caméra XR.

Construire/ouvrir un Player selon [déploiement](../../../../Docs/dev/quest-autonomous/deployment.md), puis utiliser `-pf <archive> -v "MNI Anatomy"`. Les tests EditMode `HBP.Tests.Serialization.QuestAnatomyFixtureTests` lisent le projet et chargent l'anatomie complète ; ils ne prouvent pas le rendu matériel. Une archive déterministe n'implique pas un Player bit-identique, notamment à cause de la date de build. Le build complet sur un second PC n'avait pas été exécuté lors de la campagne d'origine ; son manifeste `QUEST-001` reste [consultable dans Git](../../retained-results.md#retrouver-une-ancienne-preuve).
