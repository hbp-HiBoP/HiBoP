# P08 — validation des assets distants et du cache mémoire

- **Date :** 3 septembre 2026
- **Résultat :** `PASS — SYNTHETIC DESKTOP/XR EDITMODE`
- **Baseline :** branche `feature/xr`, commit `2f0abc4d2`
- **Unity :** `6000.5.2f1`
- **Périmètre :** payloads synthétiques et surface D0 ; aucune donnée patient, aucun cache disque, aucune mesure physique Quest

## Gate et implémentation

L'ADR P08 ferme P08-A–E avant l'implémentation. Le cœur partagé réserve la taille complète annoncée avant staging, reçoit des chunks alignés, conserve la bitmap des ranges reçus, calcule le SHA-256 final puis transfère atomiquement la propriété du tableau vers le cache. Le cache est indexé par hash, partage un transfert concurrent et un stockage validé, compte ses leases et évince uniquement les inactifs LRU.

Le codec surface recalcule le hash et vérifie la longueur/dimensions avant de construire le `SurfaceAsset`. Le store XR conserve un seul objet décodé par hash pendant ses leases et le binding ne modifie le renderer P05 qu'après cette validation. Deux bindings sur le même hash reçoivent le même `SurfaceAsset` et le même `Mesh` partagé par P05.

## Résultats automatisés

| Projet / assemblies | Tests | Échecs | Durée |
| --- | ---: | ---: | ---: |
| HiBoP — Protocol, RenderModel, Serialization | 633 | 0 | 33,9554133 s |
| XR — RemoteAssets + StaticRendering | 11 | 0 | 0,2812999 s |

La suite P08 couvre :

- payload incomplet non acquérable ; corruption finale rejetée et staging remis à zéro ;
- chunk dupliqué identique dédupliqué, chunk dupliqué conflictuel annulant le transfert ;
- interruption conservant le staging pendant le lease P07 et reprise des seuls offsets manquants ;
- deux demandes rejoignant le même transfert, puis deux leases partageant un unique payload ;
- deux renderers partageant un unique payload, un unique objet `SurfaceAsset` et un unique mesh, puis remplacement incomplet laissant le mesh actif inchangé ;
- LRU limité aux inactifs, pression bloquée avec `RequiresUserAction` quand les actifs/stagings occupent le budget ;
- expiration de lease, nouvel epoch, background et fermeture : staging annulé, inactifs purgés, actifs conservés purge-pending jusqu'au retrait explicite ;
- 256 cycles création, publication, acquisition, release et close revenant chacun à `ResidentBytes = 0` ;
- allocation bomb rejetée par dimensions avant staging et négociation au minimum des capabilities host/client ;
- hashes indépendants anatomical/inflated, topologies potentiellement différentes et manifeste canonique liant l'inflated au hash anatomical.

## Mémoire et absence de persistance

Les compteurs `StagingBytes`, `CommittedBytes`, `ResidentBytes`, nombre d'assets, transferts et évictions sont calculés sous le même verrou que les mutations. Les tests montrent une seule longueur de payload résidente avec deux consommateurs, zéro octet après corruption/annulation/purge, et zéro octet à la fin de chacun des 256 cycles.

Le test `RuntimeCacheContainsNoFilesystemPersistenceApi` inspecte le runtime P08 et interdit `File`, `Directory`, `FileStream` et `persistentDataPath`. Une recherche complémentaire ne trouve ni `PlayerPrefs`, ni API réseau, ni chemin patient dans le provider/cache/binding. Les seuls fichiers produits par la validation sont les rapports XML/log du runner sous `.test-results/`, jamais des payloads applicatifs.

## Commandes reproductibles

Ces commandes Unity doivent être lancées hors sandbox lorsque les éditeurs sont fermés :

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"

Start-Process -FilePath $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", "C:\HBP\Software\HiBoP",
  "-runTests", "-testPlatform", "EditMode",
  "-assemblyNames", "CRNL.HiBoP.Protocol.Tests;CRNL.HiBoP.RenderModel.Tests;HBP.Serialization.Tests",
  "-testResults", "C:\HBP\Software\HiBoP\.test-results\unity-cli\p08\hibop-regression-results.xml",
  "-logFile", "C:\HBP\Software\HiBoP\.test-results\unity-cli\p08\hibop-regression.log",
  "-forgetProjectPath"
)

Start-Process -FilePath $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
  "-batchmode", "-nographics", "-accept-apiupdate",
  "-projectPath", "C:\HBP\Software\HiBoP\XR",
  "-runTests", "-testPlatform", "EditMode",
  "-assemblyNames", "CRNL.HiBoP.XR.RemoteAssets.EditModeTests;CRNL.HiBoP.XR.StaticRendering.EditModeTests",
  "-testResults", "C:\HBP\Software\HiBoP\.test-results\unity-cli\p08\xr-p08-results.xml",
  "-logFile", "C:\HBP\Software\HiBoP\.test-results\unity-cli\p08\xr-p08.log",
  "-forgetProjectPath"
)
```

## Limites et suite

Cette validation ne revendique ni transfert HTTPS/WSS réel supplémentaire par rapport à P06, ni profiling physique Quest, ni validation Android background/kill/reboot. Le mapping exact des callbacks plateforme vers `Backgrounded`/`Closed`, la responsabilité UI de `ReleaseActiveContent` et la preuve D6 sur appareil appartiennent à P14-B/P14-E. P08 garantit déjà que ce raccordement ne pourra ni persister un payload, ni acquérir un ancien asset purge-pending, ni évincer silencieusement un contenu actif.
