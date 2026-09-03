# P07 — validation de la session synthétique Windows et Quest

- **Date :** 3 septembre 2026
- **Résultat :** `PASS — WINDOWS/QUEST`
- **Baseline :** branche `feature/xr`, commit `02dd8226c7dafc37b8eef0722e107d7e8c9051bf`
- **Décision préalable :** ADR P06 `ACCEPTED — PROVISIONAL WINDOWS/QUEST VALIDATED`
- **Périmètre :** cœur de session synthétique en mémoire et indépendant du transport ; aucune donnée patient, aucun asset réel, aucun renderer et aucun multi-client

## Gate et décisions

L'ADR P07 a été écrit avant le code de production et ferme P07-A–E : machines d'état host/client, limites des journaux, heartbeat/timeouts/backoff, refus du second client et remplacement explicite, puis conflit strict conforme à P02-D. La session conserve la pile P06 sans la réimplémenter. Cette preuve ne revendique donc ni un nouvel adaptateur WSS/Kestrel, ni une qualification macOS/Linux, ni une mesure réseau P07.

## Suites EditMode

La même assembly `CRNL.HiBoP.Protocol.Tests` a été exécutée avec Unity `6000.5.2f1` dans les deux projets :

| Projet | Tests | Échecs | Durée |
| --- | ---: | ---: | ---: |
| HiBoP Windows | 29 | 0 | 0,147932 s |
| HiBoP XR | 29 | 0 | 0,1331445 s |

Les tests couvrent notamment :

- compatibilité major/minor, hash de schéma et capabilities obligatoires ;
- preuve d'identité transport avant SAS, comparaison SAS à temps de boucle fixe, fenêtre de 120 s et dix tentatives par minute ;
- transitions légales/illégales des machines d'état, client unique, heartbeat à 1 s, timeout à 3 s et backoff borné ;
- snapshot abandonné, lot invalide/hors ordre, transaction préparée obsolète et enveloppe synthétique supérieure à 64 Kio ;
- lecteur concurrent pendant 10 000 swaps de snapshot sans état mixte ;
- réponse perdue puis replay identique, doublon concurrent, réutilisation d'un `commandId` avec une nouvelle séquence, trou de séquence et outcome expiré ;
- conflit enregistré sans mutation suivi d'une resynchronisation, journal contigu puis fallback snapshot après éviction ;
- coupures synthétiques de 1 s et 5 s, expiration du lease à 30 s, nouvel epoch et purge ;
- journal diagnostic circulaire borné à 256 événements et absence de SAS/token/payload.

Commandes reproductibles, à exécuter hors sandbox lorsque l'éditeur est fermé :

```powershell
C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe `
  -batchmode -nographics -accept-apiupdate `
  -projectPath C:\HBP\Software\HiBoP `
  -runTests -testPlatform EditMode `
  -assemblyNames CRNL.HiBoP.Protocol.Tests `
  -testResults C:\HBP\Software\HiBoP\.test-results\p07\windows-editmode.xml `
  -logFile C:\HBP\Software\HiBoP\.test-results\p07\windows-editmode.log `
  -forgetProjectPath

C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe `
  -batchmode -nographics -accept-apiupdate `
  -projectPath C:\HBP\Software\HiBoP\XR `
  -runTests -testPlatform EditMode `
  -assemblyNames CRNL.HiBoP.Protocol.Tests `
  -testResults C:\HBP\Software\HiBoP\.test-results\p07\xr-editmode.xml `
  -logFile C:\HBP\Software\HiBoP\.test-results\p07\xr-editmode.log `
  -forgetProjectPath
```

## Quest 3 physique

Le build de développement Unity `6000.5.2f1`, Android ARM64/IL2CPP, a été installé puis exécuté sur un Quest 3 Android 14/API 34. L'adresse réseau et l'identifiant du casque ne sont pas consignés.

| Élément | Valeur |
| --- | --- |
| APK | `.artifacts/xr/p07/HiBoPXR-P07.apk` |
| SHA-256 | `a8d80aa56fe60d2c5ee39decd8faff1221f3251f49e5b6fa344d5710e16e68dc` |
| Scène | `Assets/HiBoPXR/DistributedSession/Scenes/P07SyntheticSession.unity` |
| Build | `Succeeded`, ARM64, IL2CPP |
| Rapport runtime | `P07_QUEST_REPORT`, `result=PASS` |

La sonde IL2CPP a produit :

- 10 000 swaps atomiques et `0` lecture incohérente sous lecteur concurrent ;
- un seul effet après perte de réponse puis replay ;
- rejet d'un même `commandId` présenté avec une nouvelle séquence ;
- conflit sans mutation, puis reprise par deltas ;
- second client refusé, remplacement explicite et purge de l'ancien epoch ;
- coupures logiques de 1/5/30 s et heartbeat validés ;
- 200 reprises nominales, p95 `0,0062 ms`, sous le budget de 5 s ;
- diagnostic redacted et aucun code d'échec.

Après qu'une première version fondée sur `OnGUI` n'a pas été visible dans la vue stéréo, la sonde finale utilise un `TextMesh` sérialisé dans le prefab P07, parenté à la caméra XR dans la scène. Le statut P04 est désactivé uniquement dans cette scène afin d'éviter le chevauchement. Le rapport vert head-locked présente explicitement `RESULT: PASS` et les compteurs principaux ; sa visibilité a été confirmée dans le casque par l'utilisateur.

La valeur p95 mesure volontairement le cœur synthétique en mémoire sur Quest, sans délai réseau. La latence et l'identité de la pile réseau réelle restent les preuves P06 ; P07 démontre ici les invariants de session demandés indépendamment du transport. Le rapport brut redacted est conservé dans `quest-session-result.jsonl`.

## Conclusion

L'atomicité est prouvée par publication d'une seule référence après validation complète et par les exécutions concurrentes Windows/Quest sans lecture mixte. L'idempotence est prouvée par le journal `(clientCommandSequence, commandId, outcome)`, la publication atomique outcome/mutation/delta et les scénarios de réponse perdue, doublon concurrent, ID réutilisé, séquence trouée et outcome évincé. Les critères P07 sont satisfaits sur le périmètre synthétique Windows/Quest demandé.
