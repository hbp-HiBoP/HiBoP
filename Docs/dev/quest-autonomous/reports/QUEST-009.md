# Rapport QUEST-009 — Transport embarqué qualifié dans les Players

## Résultat et décision

**Retenir `TcpListener` / `SslStream` embarqués, avec BouncyCastle 2.7.0 pour
la création du certificat éphémère.** Le transfert Windows IL2CPP → Quest 3
Android IL2CPP est prouvé sur le LAN réel : 3 870 248 octets MNI, hash identique,
rejet d'une identité incorrecte et d'un mauvais secret, trois interruptions
pendant le payload suivies de trois nouveaux transferts complets.

La première version sans dépendance échouait : `CertificateRequest..ctor`
lève `PlatformNotSupportedException` dans le Player Windows. Le propriétaire
a autorisé l'essai BouncyCastle le 8 septembre 2026, décision **D27**. La création
de certificat seule ne suffisait pas : le lecteur PKCS#12 Mono refusait le
conteneur par défaut. Le conteneur en mémoire utilise désormais PBE SHA/3DES et
DER, compatibles avec ce lecteur. Cela ne change pas le protocole réseau,
qui reste **TLS 1.2**, et ne constitue pas un format de stockage de clés.

BouncyCastle est une bibliothèque de cryptographie existante, employée ici
pour générer une identité X.509 RSA 2048/SHA-256. Le certificat associe une
identité publique à une clé publique ; le PC garde la clé privée qui permet de
prouver qu'il possède cette identité. Un certificat auto-signé ne suffit pas à
reconnaître le bon PC : le Quest compare son empreinte SHA-256 complète au pin
attendu, fourni hors bande avant TLS. Le secret aléatoire d'appairage de
256 bits n'est envoyé qu'après cette vérification. Aucune validation accept-all.

Le code réutilisable reste isolé dans `Spikes/QUEST-009/Runtime`, avant intégration.
Les scènes, prefabs, assemblies, packages et réglages HiBoP existants sont
inchangés. Le banc utilise une scène/prefab générés dans ses propres projets Unity.
Le contrat HBNA est opaque pour le transport ; aucun cerveau n'est affiché et
aucune session produit n'est publiée dans cette tâche.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** ; manuel : **NON_REQUIS**.
- Branche `feature/xr-autonomous`, HEAD initial/final
  `490de58119a3c9ad3c3be618085d09f3f9ea2bc7`, checkout initial propre.
  Modifications non commitées identifiées par hashes dans le manifeste ; aucun push.
- Unity `6000.5.2f1 (eb73d3b415a1)` ; Windows x64 IL2CPP et Android ARM64 IL2CPP,
  .NET Standard, builds de développement. Quest 3, Android 14/API 34.
- QUEST-004 établit la cible Quest ; QUEST-005 le contrat HBNA ; QUEST-006/008
  fournissent la capture MNI de référence de **3 870 248 octets**, sans patient,
  électrode ni EEG. Hash `065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12`.
  Le script impose ce hash avant tout envoi. Il ne peut pas envoyer un fichier
  patient substitué à cette fixture.
- P06 consulté en lecture seule sur `feature/xr` / `eb26c323e2bdf6c138f9e9e3c02f3e9796cae249` :
  README, preuves Windows/Quest, SBOM et revue sécurité. Aucun code P06 repris.
- [Manifeste](../evidence/QUEST-009/manifest.json) et
  [résultats structurés](../evidence/QUEST-009/results.json) versionnables.
  Binaires, logs complets et fichiers reçus restent locaux sous `.artifacts/quest-009`
  et `.test-results/quest-009`.

## Ce que je conseille de reviewer

| Priorité | Entrée | Changement / invariant |
| --- | --- | --- |
| 1 | [TransportIdentity.Create / Matches](../../../../Spikes/QUEST-009/Runtime/TransportIdentity.cs) | Identité éphémère, import compatible Mono en mémoire, pin du certificat complet. La clé privée n'est ni écrite dans un fichier ni envoyée. |
| 2 | [PinnedTlsTransfer.ServeAsync / ReceiveAsync](../../../../Spikes/QUEST-009/Runtime/PinnedTlsTransfer.cs) | TLS vérifié avant secret, une connexion à la fois, 16 tentatives maximum, délai 20 s ; annulation ferme les sockets, arrêt attendu avant disposal du certificat. |
| 3 | [SendPayloadAsync / ReceivePayloadAsync](../../../../Spikes/QUEST-009/Runtime/PinnedTlsTransfer.cs) | Framing v1 borné à 64 Mio et chunks de 64 Kio ; ordre, taille, SHA par chunk et final, reçu serveur ; aucun payload partiel rendu au consommateur. |
| 4 | [TransportQualification](../../../../Spikes/QUEST-009/Runtime/TransportQualification.cs) | Rejets identifiés précisément, trois coupures à 128 Kio, reconnexions ; arrêt actif pendant handshake et port réutilisable ; distinction allocations/rétention. |
| 5 | [Run-QuestTransportProbe.ps1](../../../../Tools/Run-QuestTransportProbe.ps1) et [Build-QuestTransportProbe.ps1](../../../../Tools/Build-QuestTransportProbe.ps1) | Fixture verrouillée, ID d'essai contre logs périmés, quatre livraisons/neuf connexions libérées, configs temporaires supprimées, arrêt des processus ; build Android autonome avec dossier de sockets Java explicite. |

Le bootstrap automatisé transmet le pin complet et un secret à usage de test
par ADB de confiance. Ce n'est pas une implémentation d'interface d'appairage
à code court. Les configs de test sont consommées puis supprimées ; aucun secret
ou jeton n'est enregistré dans le manifeste ou les résultats versionnés.

## Vérifications effectuées

| ID | Commande / scénario | Résultat / preuve |
| --- | --- | --- |
| T1 | `Tools/Build-QuestTransportProbe.ps1 -Target Windows` | REUSSI, Unity exit 0, zéro erreur, build final 23,057 s ; `windows-build.log`. Backend Windows réel : IL2CPP. |
| T2 | `Tools/Build-QuestTransportProbe.ps1 -Target Android` | REUSSI **depuis Codex**, Unity exit 0, zéro erreur, 11,737 s ; `android-build.log`. ARM64 IL2CPP ; aucune bibliothèque scientifique native dans l'APK. |
| T3 | `TransportProbe.exe -batchmode -nographics -logFile C:\HBP\Software\HiBoP\.test-results\quest-009\windows-qualification-final.log` | REUSSI, Player exit 0, **18 résultats** : nominal, identité/secret, trois interruptions/reconnexions, corruption, deux arrêts et rebinds, arrêt handshake, troncature/version/taille. |
| T4 | `Tools/Run-QuestTransportProbe.ps1 -Serial 192.168.1.18:5555 -HostAddress 192.168.1.14` | REUSSI, exit 0, **9 scénarios client**, quatre livraisons validées par reçu serveur, neuf connexions libérées ; run final `20260908-135852`. Aucun tunnel ADB/reverse ne porte le payload : TCP LAN direct. |
| T5 | Hash du fichier reçu récupéré par ADB | Identique à la fixture source sur quatre transferts ; `runs/20260908-135852/received.hbna`. Le transfert des octets a eu lieu sous TLS ; ADB ne sert qu'au bootstrap et à la collecte de preuve. |
| T6 | Arrêt | Serveur physique arrêté et tâche attendue en 0,391 ms ; scénario Windows d'arrêt pendant handshake en 2,834 ms ; ports réutilisables ; pas de processus TransportProbe/Unity restant. Sonde Quest arrêtée et absence de PID vérifiée ; ADB et paramètres D26 conservés. Aucun thread applicatif dédié créé par le transport. |
| T7 | Formatage | `Tools/format-code.cmd` exécuté ; fichiers de spike absents de la solution HiBoP. Même ReSharper 2026.2.0 exécuté sur une solution temporaire contenant ces cinq C#, exit 0 ; `format-spike-solution.log` énumère les cinq fichiers. |
| T8 | Revue indépendante sécurité/cycle de vie | Trois constats corrigés et relus : exception de protocole isolée à sa connexion, ready-file protégé par finally, arrêt handshake actif. Aucun point restant dans cette revue ciblée. |

Les coupures sont la fermeture déterministe de la connexion TCP après 128 Kio
effectivement reçus sur le Quest physique. Elles ne désactivent pas la radio Wi-Fi.
Le transfert reprend depuis zéro, sans prétendre effectuer une reprise par offset.
La démonstration produit avec une minute hors réseau appartient à QUEST-012.

### Mesures finales Windows → Quest

| Cas | Durée client | Octets reçus | Allocations sur les frames de l'opération |
| --- | ---: | ---: | ---: |
| Nominal | 841,312 ms | 3 870 248 | 15 538 751 o |
| Reconnexion 1 | 516,135 ms | 3 870 248 | 19 669 337 o |
| Reconnexion 2 | 515,848 ms | 3 870 248 | 19 712 758 o |
| Reconnexion 3 | 521,103 ms | 3 870 248 | 19 667 364 o |

Le compteur Unity `GC Allocated In Frame` était valide. Il est cumulé une fois
par frame ; ces valeurs incluent le Player, TLS et le diagnostic. Ce ne sont
pas des allocations isolées du codec ou des mesures de pic mémoire. La variation
de mémoire managée est consignée séparément ; elle peut être négative après GC.
La durée inclut connexion, TLS, appairage, réception, vérification SHA et écriture
du fichier de preuve. Elle ne mesure ni le décodage HBNA ni le rendu.

### Taille et composants

L'APK final mesure **37 953 005 octets**. Le Player Windows et BouncyCastle sont
embarqués dans un seul processus ; la DLL .NET Standard d'entrée BouncyCastle
mesure **4 885 904 octets** avant IL2CPP. Les tailles globales BuildReport incluent
des artefacts de développement : ne pas les assimiler au coût additionnel de la
bibliothèque dans HiBoP. Ce coût dans le build produit reste à mesurer à l'intégration.

[Inventaire](../../../../Spikes/QUEST-009/components.json),
[licence MIT BouncyCastle](../../../../Spikes/QUEST-009/licenses/BouncyCastle-MIT.md),
package 2.7.0 téléchargé depuis NuGet avec hash imposé
`f091ffccab4d03993e660bace277659a79dee0972f54d7f1f4bd46d680966241`.
La licence est copiée à côté des binaires. Source officielle du composant :
[BouncyCastle.Cryptography](https://www.nuget.org/packages/BouncyCastle.Cryptography/2.7.0).

Le repli historique P06 employait Kestrel/.NET 10.0.11, avec archive Windows
50 583 915 octets et installation 111 554 005 octets. Il nécessiterait un processus
supervisé, un échange HiBoP–auxiliaire et son packaging. Ses preuves Windows–Quest
historiques ne couvrent pas les coupures ni Mac. L'essai embarqué réussissant,
aucun avantage concret n'impose ici ce repli. Pas de changement d'architecture
implicite : D18 et l'essai autorisé D27 sont respectés.

## Construction Android autonome et incidents de qualification

Depuis la session Codex, Gradle échouait sur `Unable to establish loopback connection`
/ `UnixDomainSockets ... Invalid argument: connect`, même hors sandbox. Les
constructions lancées par le propriétaire depuis PowerShell fonctionnaient.
Le défaut BOM du JSON généré sous Windows PowerShell 5 a été corrigé séparément.

Le script définit désormais, uniquement pour son invocation et ses enfants,
`JDK_JAVA_OPTIONS=-Djdk.net.unixdomain.tmpdir=<repo>/.codex-temp/quest009-java`,
puis restaure sa valeur précédente. `JAVA_TOOL_OPTIONS` était supprimée par le
lancement Unity observé et ne résolvait pas le problème. La nouvelle option a
permis le build final depuis Codex, puis l'essai physique de cet APK. Aucun
changement global de politique PowerShell, de sandbox ou de licence nécessaire.
Le choix du répertoire de sockets est une propriété documentée du
[JDK](https://docs.oracle.com/en/java/javase/17/core/java-networking.html).

Un contrôle automatique avait refusé l'export supposé sensible du MNI. La
provenance sans patient documentée depuis QUEST-001/D14 a été vérifiée, le hash
audité imposé dans le script, puis la même action acceptée. Aucune donnée patient
n'a été substituée ni transférée.

## Validation manuelle et suite

**Aucune validation manuelle nécessaire.** Le propriétaire a autorisé D27 et aidé
aux builds Android avant correction du lanceur ; ces actions ne sont pas
présentées comme une validation visuelle ou scientifique.

Binaires finaux :

- `C:\HBP\Software\HiBoP\.artifacts\quest-009\Windows\TransportProbe.exe`
- `C:\HBP\Software\HiBoP\.artifacts\quest-009\Android\TransportProbe.apk`
- Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-008\fixture\quest-anatomy.hbna`

Le [README de la sonde](../../../../Spikes/QUEST-009/README.md) donne les commandes
reproductibles. Le Quest conserve son application HiBoP habituelle ; la sonde a
son propre package et est arrêtée après les essais.

Non qualifiés ici : Mac/Apple Silicon, coupure radio longue, interface d'appairage,
persistance des clés, publication atomique de session, validation applicative HBNA
et sécurité multi-utilisateur. Ces limites ne sont pas masquées par la preuve réseau.
**Prochaine tâche proposée : QUEST-010**, sans exécution automatique.
