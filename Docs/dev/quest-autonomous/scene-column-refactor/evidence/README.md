# Preuves du chantier SCENE

Ce dossier contient uniquement les consignes et un modèle à la rédaction initiale.
Aucune preuve d'implémentation, test ou qualification n'est créée par anticipation.

Chaque tâche exécutée produit `SCENE-NNN/manifest.json`, lié depuis son rapport
et le registre local. Le [modèle JSON](manifest-template.json) est un exemple de
structure, pas un manifeste de succès ni un schéma imposé au runtime.

## Contenu attendu

- ID de tâche, dates UTC, états indépendants, baseline et rapport associé.
- Source applicative : branche/commit et modifications non commités identifiées
  par chemins/hashes pertinents ; ne pas attribuer les changements concurrents.
- Unity, packages et binaires natifs réellement utilisés, versions et provenance.
- Commandes exactes, environnement, fixture, exit codes et résultats utiles.
- Artefacts : chemin, taille, SHA-256 et présence dans Git ou uniquement locale.
- Invariants démontrés, preuves réutilisées, justification de leur applicabilité
  et limites non vérifiées. Une preuve issue de 023 n'est pas automatiquement
  une preuve du Player refactorisé.
- Validation manuelle : item, résultat, date et référence de retour explicite.
- Pour un essai Quest : appareil/package identifié, résultat de l'arrêt HiBoP
  après collecte et validation, sans arrêter ADB.

Les gros logs, builds et captures peuvent rester dans les artefacts ignorés,
avec chemins et hashes. Les petites preuves durables peuvent être ajoutées ici.
Ne pas enregistrer de secrets ou de données personnelles inutiles.

Un champ inconnu reste nul ou marqué non vérifié. `null` et une liste vide dans
le modèle ne signifient pas réussite, zéro échec ou absence prouvée de ressources.
Le champ `template` doit être supprimé ou mis à false lors de la création d'un
manifeste réel ; renseigner l'ID et les preuves effectives.

Les entrées de commandes peuvent porter `id`, `command`, `environment`,
`fixture`, `exitCode`, `result` et `evidencePath`. Les artefacts peuvent porter
`path`, `bytes`, `sha256` et `storage`. Les invariants peuvent porter `id`,
`status`, `evidence` et `limitations`. Adapter les champs à la preuve sans inventer
des valeurs pour remplir le modèle.
