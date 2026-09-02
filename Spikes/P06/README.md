# P06 — prototype transport isolé

Ce dossier est un spike jetable. Il ne référence pas les projets Unity de production, ne modifie aucun manifest HiBoP/XR et ne constitue pas une dépendance de `com.crnl.hibop.protocol`.

## Contenu

- `P06.Host` : Kestrel 10.0.11 self-contained, HTTPS ranges et WSS sur le même port, TLS 1.2/1.3, SAS à six chiffres et pin SHA-256 du SPKI ;
- `P06.Client` : client Desktop de référence et banc de mesure ;
- `P06.Core` : framing borné, Protobuf/MessagePack/MemoryPack et buffers ;
- `P06.Launcher` : démonstrateur Windows `WinExe` qui démarre, supervise et arrête le sidecar sans shell ni fenêtre ;
- `UnityClient` : projet Unity autonome Android ARM64/IL2CPP avec UI de saisie IP/code, `UnityWebRequest` pour HTTPS et le candidat `websocket-sharp` pour WSS ;
- `fixtures` : golden vectors wire complets ;
- `sbom` : inventaire CycloneDX des composants candidats ;
- `VendorPatches` : unique patch websocket-sharp, qui remplace la version joker par `1.0.2.0` pour la compilation Unity déterministe.

NativeWebSocket au commit verrouillé ne permet pas de fournir un callback de validation du certificat WSS. Il est donc rejeté par le veto P06-D sans contournement `accept all`.

## Commandes principales

```powershell
dotnet test .\Spikes\P06\tests\P06.Tests\P06.Tests.csproj
dotnet test .\Spikes\P06\tests\P06.Launcher.Tests\P06.Launcher.Tests.csproj
.\Spikes\P06\Tools\Build-UnityClient.ps1
```

Host de test accessible au Quest :

```powershell
.\Spikes\P06\.artifacts\publish-final\win-x64\P06.Host.exe `
  --listen 0.0.0.0 --display <IP-DU-PC> --advertise <IP-DU-PC> --port 5443
```

L’adresse et le code affichés par le Desktop sont saisis sur le client Quest. Pour les répétitions instrumentées uniquement, les extras ADB `p06Host`, `p06PairCode` et `p06Mode` (`smoke`, `load`, `identity-reject`, `corruption-reject`) permettent de déclencher la sonde sans interaction VR. Ils ne font partie d’aucun protocole ou produit candidat. Les logs et binaires restent sous `.artifacts`, ignoré par Git.

## Limites intentionnelles

- certificat de test recréé à chaque lancement ; la persistance protégée de l’identité appartient à l’intégration future ;
- aucune découverte, règle firewall ou ouverture automatique de port ;
- aucun cache patient ni état produit ;
- le runtime Quest n’implémente pas `GetECDsaPublicKey()` : le spike extrait le DER SPKI avec un parseur borné avant SHA-256, à revoir avant toute généralisation ;
- websocket-sharp interdit le header utilisateur `Authorization` : le WSS du spike utilise `X-P06-Access-Token` sous TLS ;
- les publications Linux/macOS sont des cross-builds, pas des validations natives.
