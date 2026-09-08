# QUEST-009 — Sonde de transport embarqué

Essai isolé avant intégration, sans modification des scènes ou assemblies HiBoP.
Le code réutilisable est dans `Runtime/TransportIdentity.cs` et
`Runtime/PinnedTlsTransfer.cs` ; les autres classes sont uniquement un banc
de qualification. Voir `Docs/dev/quest-autonomous/reports/QUEST-009.md`.

## Construire et exécuter

Depuis PowerShell sous Windows, **Unity hors sandbox** :

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Build-QuestTransportProbe.ps1 -Target Windows
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Build-QuestTransportProbe.ps1 -Target Android
```

Les projets générés sont ignorés : `.artifacts/quest-009/WindowsProject` et
`.artifacts/quest-009/UnityProject`. Ils utilisent la version Unity du dépôt,
IL2CPP Windows x64 / Android ARM64, .NET Standard et un prefab de sonde créé
par l'éditeur. Aucun GameObject n'est construit dans le Player.

Sans configuration, `Windows/TransportProbe.exe -batchmode -nographics`
exécute les contrôles loopback avec 3 Mio + 17 octets déterministes.
`-builtinIdentity` reproduit le rejet `CertificateRequest` du runtime.

Pour le test physique, installer `.artifacts/quest-009/Android/TransportProbe.apk`
par ADB, puis :

```powershell
Tools/Run-QuestTransportProbe.ps1 -Serial 192.168.1.18:5555 -HostAddress 192.168.1.14
```

Ces adresses correspondent au poste/casque de qualification. Sur un autre LAN,
fournir les adresses vérifiées. Le script cible uniquement
`fr.crnl.hibop.transportprobe`, exige le hash de la fixture MNI sans patient de
QUEST-008, transmet le bootstrap par ADB de confiance, vérifie neuf scénarios,
quatre reçus serveur, neuf connexions libérées et le hash du fichier récupéré.
Les configs contenant le secret aléatoire sont consommées/supprimées, jamais
archivées dans les preuves. L'identité privée reste en mémoire dans le Player.
Chaque campagne a un ID pour exclure les logs d'une exécution antérieure.
En fin d'essai, le script arrête la sonde, sans arrêter ADB ni remettre le casque
en veille. Aucun réglage de firewall n'est modifié par ces scripts.

## Contrat de sonde

- `SslStream`, TLS 1.2 exclusivement ; RSA 2048/SHA-256, certificat éphémère
  BouncyCastle 2.7.0. Pin SHA-256 du certificat complet imposé hors bande.
- Secret d'appairage aléatoire 32 octets, transmis seulement après TLS vérifié.
- Un client traité à la fois, 16 tentatives maximum, deadline 20 s par connexion,
  durée totale du Player bornée à 3 minutes.
- Header de 40 octets : `HBT`, version 1, longueur int32 LE (1–64 Mio), SHA-256.
  Chaque chunk de 64 Kio maximum : index int32 LE, longueur int32 LE, SHA-256,
  puis octets. Taille attendue et ordre vérifiés avant lecture.
- Après validation de tous les chunks et du hash final, le client renvoie le
  hash comme reçu. Aucun résultat partiel n'est publié. Une interruption entraîne
  un nouvel envoi complet ; aucun cache/reprise par offset n'est promis.
- Le producteur garde ses tableaux immuables pendant `ServeAsync`. L'appelant
  possède le certificat et attend la tâche serveur avant de le disposer.
  L'annulation ferme sockets/listener pour débloquer les APIs TLS historiques.

L'import PKCS#12 reste en mémoire et utilise PBE SHA/3DES + DER pour le lecteur
Mono. Ce choix concerne le conteneur de clé temporaire, **pas le chiffrement TLS**.
Il ne faut pas réutiliser ce conteneur pour une persistance de clés.

## Frontières des preuves

Le bootstrap automatisé avec un pin complet fourni par ADB n'est pas l'interface
d'appairage utilisateur. Le flux contient un HBNA opaque ; validation applicative,
publication atomique de session et UI relèvent de QUEST-010/011. Pas de découverte,
HTTP, cryptographie maison, reprise durable, sécurité multi-utilisateur, validation
Mac ou preuve de coupure radio d'une minute.

Les allocations sont la somme du compteur Unity « GC Allocated In Frame » sur
les frames d'une opération : elles incluent le Player et le diagnostic. La variation
de mémoire managée est une mesure de rétention, pas une allocation, et peut être
négative après un GC. Les durées incluent TLS, appairage, réception, hash et écriture
du fichier côté client lorsqu'elle est activée.

BouncyCastle est téléchargé depuis NuGet et vérifié par SHA-256 ; sa DLL n'est pas
copiée dans le projet HiBoP. Sa licence MIT est dans `licenses/BouncyCastle-MIT.md`
et copiée à côté des binaires par le script de build.
