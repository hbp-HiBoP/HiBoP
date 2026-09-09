# Fixture iEEG synthétique QUEST-020

Générer depuis la racine du dépôt :

```powershell
python Tools/Prepare-QuestIEEGFixture.py
```

Résultat local : `.artifacts/quest-020/fixture/quest-mni-ieeg.hibop`, protocole
`quest-020.prov` et deux triplets BrainVision `synthetic-left/right.vhdr/.vmrk/.eeg`.
Le générateur et les contacts MNI de QUEST-013 sont les sources versionnées.
Le protocole synthétique reprend la fenêtre FACE de VISU (-500 à 1000 ms), sans
traitement, avec des identifiants propres à cette fixture. La normalisation de
chaque source vaut `None`. Aucun signal acquis n'est utilisé.

Ouvrir avec `Tools/Run-QuestIEEGCapture.ps1 -Index 50 -KeepOpen` après le build
Windows QUEST-020. Le lanceur ajoute `-questIEEGProtocol` **avant** `-pf` : ce
diagnostic de développement charge le protocole dans la mémoire du Player,
sans écrire dans la base personnelle. L'archive seule ne contient pas de protocole
importable par le chargeur de projets ; son ouverture sans ce lanceur peut donc
aboutir au mode récupération. Les chemins des signaux sont générés pour le
checkout courant ; régénérer la fixture après déplacement du dépôt.

Si PowerShell bloque l'exécution des scripts, utiliser une autorisation limitée
à ce lancement :

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\HBP\Software\HiBoP\Tools\Run-QuestIEEGCapture.ps1" -Index 50 -KeepOpen
```

Sans `-KeepOpen`, le lanceur utilise le mode batch du Player avec périphérique
graphique et quitte après la capture. `-captureIEEGIndex` demande explicitement
la préparation iEEG si nécessaire ; il ne modifie pas la préférence utilisateur
de calcul automatique et refuse un avertissement de couverture de projection.

La visualisation `MNI iEEG` contient une colonne `Synthetic uV`, les huit contacts
MNI de QUEST-013 et 151 échantillons préparés à 100 Hz. Chaque patient possède
les canaux A1, A2 et B1 ; B2 est absent et masqué. A2 du patient droit est
blacklisté. Pour chaque fichier, l'événement S20 est au sample fichier 100
(position BrainVision 101), soit l'index préparé **50**, temps local **0 ms**.

| Contact par patient | Valeur à 0 ms | Unité | Disponibilité |
| --- | ---: | --- | --- |
| A1 | -1 | uV | disponible |
| A2 | 0 | uV | disponible ; blacklisté à droite |
| B1 | +1 | uV | disponible |
| B2 | 0 de remplacement | non renseignée | absent, masqué |

La préparation contient aussi des valeurs -10 et +10 ; les plages sérialisées
restent **SpanMin=-10, Middle=0, SpanMax=10** à tous les instants. À l'index 75
(250 ms), A1/B1 valent -1,25/+1,25. Le générateur explicite chaque sample et fixe
les dates ZIP ; les hashes des artefacts qualifiés sont dans le manifeste de preuves.

La fixture à une colonne est alignée (Alpha temporel=0). Les tests de capture
utilisent aussi navigation 200 Hz/projection 100 Hz : à 15 ms, index de projection
1 et Alpha=0,5. La surface conserve l'entrée à l'index 1, tandis que les sites
utilisent exactement `TemporalSample.Evaluate`. Les politiques Floor/Round
restent celles du Desktop et sont transportées explicitement ; le transfert
n'ajoute aucun arrondi. `ActivityAlpha` est un autre paramètre : l'opacité.
