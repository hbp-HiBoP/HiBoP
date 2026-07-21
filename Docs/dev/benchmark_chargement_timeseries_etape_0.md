# Benchmark de référence du chargement temporel — étape 0

Ce benchmark produit un rapport JSON reproductible sans ajouter de série temporelle
volumineuse au dépôt. Les valeurs d'activité sont générées en mémoire à partir du
quadruplet `(patient, canal, essai, index)`.

## Profils

- `Smoke` est le profil court destiné à la CI.
- `Product` contient la référence produit de 30 000 sites et 100 instants, ainsi
  qu'une variante à trois colonnes. Il est lancé manuellement.
- `Extreme` ajoute notamment les fenêtres inclusives de 1 500 ms à 64 Hz
  (97 positions) et 2 048 Hz (3 073 positions). Il est lancé manuellement.
- `Typical` reste accepté comme alias historique du profil produit.

Chaque scénario rapporte séparément les octets logiques du signal brut managé,
des époques, des dérivés, du tableau de colonne, de la projection native et des
textures, puis les deltas de mémoire privée. Les temps total, calcul natif et mise
à jour des coupes exposent P50 et P95. Les dix répétitions du profil de référence
permettent de comparer le pic et la mémoire retenue après ouverture/fermeture.

## Commandes Windows

Les commandes Unity doivent être exécutées hors sandbox. Ne pas ajouter `-quit` :
la méthode de benchmark termine Unity avec un code de sortie adapté à la CI.

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"
$project = "C:\HBP\Software\HiBoP"
$results = Join-Path $project ".test-results\timeseries-step0"
New-Item -ItemType Directory -Force -Path $results | Out-Null

$arguments = @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", $project,
  "-executeMethod", "HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run",
  "-hbpProjectionOutput", (Join-Path $results "smoke.json"),
  "-hbpProjectionProfile", "Smoke",
  "-hbpProjectionTimeline", "100",
  "-hbpProjectionRepetitions", "10",
  "-hbpProjectionWorkers", "0",
  "-hbpProjectionBatchSites", "0",
  "-forgetProjectPath",
  "-logFile", (Join-Path $results "smoke.log")
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
exit $process.ExitCode
```

Pour un rapport manuel, remplacer `Smoke` par `Product` ou `Extreme` et adapter
le nom du fichier de sortie. `-hbpProjectionFilter` permet de sélectionner un
seul scénario, par exemple `product-reference` ou `high-frequency`.

Le rapport inclut le système, la version Unity, le nombre de processeurs, le
pas d'échantillonnage mémoire et la description complète de chaque charge. Ces
champs doivent être conservés avec toute comparaison avant/après.

La première référence mesurée sur le code de l'étape 0 est consignée dans
`reference_chargement_timeseries_etape_0_2026-07-21.md`.
