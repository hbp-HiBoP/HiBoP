# Preuves des tâches

Créer un sous-dossier QUEST-NNN seulement lors de son exécution. Aucun résultat
factice n'est précréé. Le manifeste décrit au minimum :

```json
{
  "task": "QUEST-NNN",
  "recordedAt": "date UTC réelle",
  "source": {
    "repository": "HiBoP",
    "branch": "branche réelle",
    "commit": "SHA réel",
    "dirty": true,
    "diffEvidence": "chemin et hash du patch ou inventaire exact, y compris sources non suivies"
  },
  "environment": {"unity": "version", "platform": "plateforme", "device": "modèle sans identifiant privé"},
  "nativeArtifacts": [],
  "checks": [
    {"id": "T1", "command": "commande réelle", "exitCode": null, "result": "NON_EXECUTE", "evidence": []}
  ],
  "manual": [
    {"id": "M1", "result": "EN_ATTENTE", "validatedAt": null, "userFeedback": null}
  ]
}
```

Adapter les champs au travail réel. Conserver les commandes nécessaires à la
reproduction, versions, hashes et unités des mesures. Ne pas remplir exitCode
ou validatedAt sans résultat réel. Une comparaison de deux builds non commités
doit identifier leur même état source, pas seulement le SHA de HEAD.

Les APK, logs bruts et captures volumineuses vont dans le dossier d'artefacts
ignoré convenu. Les référencer avec chemin et SHA256, et préciser qu'ils peuvent
ne pas être disponibles dans un autre checkout. Les rapports et manifestes
doivent être ajoutés au contrôle de version lors d'un commit explicitement demandé.
