# P06 — revue sécurité, dépendances et risques résiduels

- **Périmètre :** bootstrap TLS/pinning, exposition réseau, entrées/ressources, dépendances, logs ; ni certification ni pentest complet
- **État :** revue de base et tests P06 terminés; risques résiduels acceptés par le propriétaire, sans référent sécurité externe disponible

## Conclusions

1. Le host utilise uniquement TLS/X.509 ECDSA P-256 et SHA-256 des runtimes. HTTPS et WSS partagent certificat, port et pin SPKI. TLS 1.2/1.3 est imposé.
2. Le SAS à six chiffres est une comparaison de première confiance, pas un secret transmis. Dix tentatives/minute sont autorisées ; la réassociation doit rester une action utilisateur explicite.
3. Le commit NativeWebSocket `c612a4f…` n’expose ni callback de certificat ni accès à `ClientWebSocketOptions`. Il ne peut appliquer P06-D et est **veto/rejeté** dans sa forme verrouillée.
4. `websocket-sharp` `7aed0002…` expose `ServerCertificateValidationCallback`. Son callback a été exécuté sur Quest physique pour WSS avec le même pin que `UnityWebRequest` HTTPS. `GetECDsaPublicKey()` n’étant pas implémenté par le runtime Quest, le spike extrait le TLV DER SPKI avec un parseur borné puis utilise SHA-256 du runtime; le test d’identité changée passe.
5. Le host borne les connexions, upgrades, cadence, tokens, tentatives, headers, ranges et enveloppes avant décodage. Les erreurs et rapports ne contiennent ni donnée patient, chemin patient, adresse MAC, SSID ni identifiant Quest.
6. L’identité X.509 est éphémère et recréée à chaque lancement du spike. Une production devra stocker la clé privée via le mécanisme protégé de l’OS, gérer renouvellement/révocation et distinguer changement légitime d’identité et attaque. Ce point n’est pas résolu par P06.
7. Le host écoute loopback par défaut. Le mode Quest exige une IP explicite et `0.0.0.0`; la règle firewall, le consentement d’exposition LAN et la fermeture du port à l’arrêt restent à concevoir dans P14/P15.

## Inventaire licences runtime

| Composant | Version/commit | Licence | Statut |
| --- | --- | --- | --- |
| ASP.NET Core/.NET runtime | 10.0.11 | MIT | candidat host |
| Google.Protobuf | 3.36.1 | BSD-3-Clause | candidat codec |
| MessagePack | 3.1.8 | MIT | candidat codec |
| MemoryPack | 1.21.4 | MIT | candidat codec |
| K4os.Compression.LZ4 | 1.3.8 | MIT | candidat buffer, désactivé par défaut proposé |
| websocket-sharp | `7aed0002451cf70ed74bc2e1ca6504dab5b50a10` | MIT | candidat WSS Quest |
| NativeWebSocket | 2.0.7, `c612a4fef60f2ae57614b73202d2d261ba56aa3e` | MIT | rejeté faute de callback pinning |
| Microsoft.NET.StringTools | 17.11.4 | MIT | transitif MessagePack |
| System.Collections.Immutable | 8.0.0 | MIT | transitif codecs |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | MIT | transitif codecs |
| Unity | 6000.5.2f1 | licence Unity du projet | outil/runtime Quest uniquement |

`Grpc.Tools 2.76.0` (Apache-2.0) et les générateurs MemoryPack/MessagePack sont build-only. L’inventaire machine-lisible est `Spikes/P06/sbom/p06-sbom.cdx.json`. Les NuSpec locaux ont été lus pour les identifiants de licence ; le package NativeWebSocket verrouillé déclare MIT. L’ADR initial indiquait Apache-2.0 pour NativeWebSocket : cette erreur factuelle est corrigée.

Le scan NuGet en ligne du 2 septembre 2026 n’a remonté aucun package vulnérable pour Host, Client ou Tests. Cela ne couvre pas les CVE futures, Unity, Android ni les commits Git ; P15 devra reproduire SBOM, avis et notices au build distribué.

## Menaces et contrôles

| Menace | Contrôle spike | Risque résiduel / suite |
| --- | --- | --- |
| MITM au premier contact | SAS dérivé du SPKI, vérifié sur le certificat réellement présenté | six chiffres exigent affichage sûr et limite d’essais ; revue UX Quest |
| identité changée | comparaison constante du pin avant payload | politique de réassociation/persistance à définir |
| brute force pairing | 10 essais/minute globalement, tokens aléatoires 256 bits | limiter aussi par interface/IP sans créer un contournement IPv6 |
| allocation bomb | envelope 64 Kio et validation du header avant codec | fuzz natif/AOT et limites internes des trois codecs encore requis |
| flood WSS/HTTP | 16 connexions, deux upgrades, 100 frames/s | quotas par pair/IP et backpressure bulk à généraliser |
| corruption/rejeu | hash chunk/final, messageId/correlationId dans framing | idempotence/reprise D12 à implémenter dans P07/P08 |
| fuite logs | niveau Warning, pas de headers/tokens/payloads journalisés | politique de redaction et rotation P14/P15 |
| supply chain | commits/versions fixes, SBOM, scan NuGet | provenance/signatures et mise à jour continue P15 |

Le client WSS utilise `X-P06-Access-Token` plutôt que `Authorization`, que l’API `SetUserHeader` de websocket-sharp classe comme header restreint. Le token reste opaque, aléatoire, en mémoire et protégé par le même canal TLS; le host accepte ce header uniquement comme seconde représentation du bearer et ne le journalise pas. Cette différence de protocole doit être confirmée pendant la revue sécurité.

## Acceptation et suivi

Le propriétaire du dépôt accepte le 2 septembre 2026 de poursuivre sans revue d’un référent sécurité externe, sur la base des contrôles P06 réussis. Il accepte les risques résiduels concernant le modèle de menace LAN, l’UX de réassociation, la persistance de clé, la portée firewall, les limites par client, le parseur DER SPKI, le header WSS, le fuzz IL2CPP et les logs/SBOM. Ces sujets restent à traiter de manière proportionnée dans P14/P15 avant publication; ils ne bloquent plus P07+ et cette acceptation n’est ni une certification ni un pentest.
