# QUEST-010 — Harness de livraison au renderer

Le code exécuté est celui de `Assets/Scripts/HBP/Transfer` et du prefab
`Assets/Prefabs/Quest/QuestAnatomy.prefab`. Le serveur de harness Windows IL2CPP compile
directement ces sources de transport/contrat ; il utilise le transport
embarqué du produit dans un Player Unity dédié à la qualification. L'interface produit relève de
QUEST-011. Aucun endpoint ni secret n'est embarqué dans l'APK.

## Construire

Depuis `C:\HBP\Software\HiBoP`, Unity fermé, dans PowerShell hors sandbox :

```powershell
.\Tools\Build-QuestDeliveryProbe.ps1 -Target Windows
.\Tools\Build-QuestDeliveryProbe.ps1 -Target Android
```

Ces commandes construisent le serveur Windows IL2CPP et l'APK **HiBoP complet** avec son
profil Quest, en développement/Android ARM64 IL2CPP. `-Target Windows` construit
uniquement le serveur. Les sources de harness et son builder restent versionnés ; projets générés,
objets et binaires sont sous `.artifacts/quest-010`.

- Serveur : `.artifacts/quest-010/Windows/DeliveryServer.exe`.
- APK : `.artifacts/quest-010/Android/HiBoP.Quest.apk`.
- Fixture : `.artifacts/quest-008/fixture/quest-anatomy.hbna`, capture Desktop
  MNI réelle de QUEST-006/008, SHA-256
  `065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12`.
  Le serveur et le lanceur refusent une substitution de fixture.

## Essai physique automatisé

```powershell
.\Tools\Run-QuestDeliveryProbe.ps1 -Serial 192.168.1.18:5555 -HostAddress 192.168.1.14
```

Relever les adresses actuelles avant de réutiliser cette commande. Le lanceur
installe l'APK, échange par ADB le pin complet et le secret aléatoire de test,
puis démarre HiBoP. Les configs sont consommées et supprimées. **Le payload
voyage par TCP/TLS sur le LAN**, sans copie ADB de l'anatomie ni tunnel reverse.

`QuestDeliveryDiagnostic` s'active exclusivement avec
`Application.persistentDataPath/quest010-config.json` dans un build Android de
développement. Il appelle le récepteur sérialisé dans le prefab, puis vérifie :

1. Publication MNI : 69 104 sommets et 3 869 920 octets estimés de buffers.
2. Déconnexion/désactivation du récepteur, contenu et présentation conservés.
3. Répétition de la même livraison : `AlreadyPublished`, un seul upload.
4. Fermeture : destruction effective du mesh après progression des frames.
5. Répétition après fermeture : `Closed`, aucun nouveau mesh.

Les résultats et logs sont conservés dans
`.test-results/quest-010/runs/<runId>/`. Le serveur doit observer exactement
`Published, AlreadyPublished, Closed`. Le lanceur arrête HiBoP en fin d'essai
et vérifie l'absence de PID, sans modifier ADB ni les réglages D26.

## Tests de défaillance

`Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyDeliveryTests.cs` exerce
de vrais sockets TLS loopback et le prefab produit : perte d'accusé injectée à
la lecture serveur, corruption de chunk, troncature, versions transport/HBNA
inconnues, HBNA incomplet, métadonnées de rendu refusées, collision ID/hash,
remplacement, coupure pendant réception et fermeture. Les tests attendent
directement les tâches ; la création de certificat du OneTimeSetUp reste
synchrone pour ne pas bloquer les continuations Unity via NUnit.

Passer `-questAnatomyFixture` avec le chemin absolu ci-dessus aux tests Unity
pour inclure le MNI réel. Commandes et résultats observés dans le
[rapport](../../Docs/dev/quest-autonomous/reports/QUEST-010.md).
