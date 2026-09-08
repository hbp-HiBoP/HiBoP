# Rapport QUEST-010 — Livraison et publication d'une session anatomique

## Résultat

La capture Desktop produit désormais une offre immuable réutilisable lors des
tentatives. Le transport TLS qualifié dans QUEST-009 livre ses chunks vérifiés
au récepteur du prefab Quest, qui décode HBNA sur un worker puis prépare et
publie le mesh sur le thread Unity. L'accusé part **après** cette publication.
Avant ce changement, le reçu confirmait seulement la réception des octets.

La session possède son contenu indépendamment du réseau. Une erreur de
réception, une désactivation ou une pause n'efface pas une publication prête.
La fermeture explicite et la destruction du propriétaire libèrent les ressources.
Une répétition de livraison déjà publiée n'effectue aucun nouvel upload ; une
répétition ancienne après remplacement/fermeture ne ressuscite pas son contenu.

Le diagnostic local précédent cède la vue au récepteur réseau. Son `OnDisable`
ne peut plus effacer un mesh dont il n'est pas propriétaire. Aucun nouvel objet
n'est construit au runtime pour réparer une référence : `QuestAnatomySession.view`
est sérialisé dans `QuestAnatomy.prefab`, composant `726012340010000001`, vers
le composant vue `510045443892623503`. Le générateur de prefab conserve ce lien.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** ; manuel : **NON_REQUIS**.
- Branche `feature/xr-autonomous`, HEAD initial
  `0e5f99208c5ba315bd47f457de3f2d427387a993`, checkout initial propre.
  Modifications locales non commitées identifiées dans le manifeste ; aucun push.
- Unity `6000.5.2f1 (eb73d3b415a1)`, URP/OpenXR/Input System existants.
  Aucun dépôt scientifique voisin ni plugin natif scientifique modifié.
- Dépendances relues : contrats QUEST-005, capture MNI QUEST-006/008,
  renderer QUEST-007, transport QUEST-009.
- Reprise de `PinnedTlsTransfer.cs` et `TransportIdentity.cs` depuis
  `Spikes/QUEST-009/Runtime` au HEAD initial. Les sources/preuves du spike restent
  inchangées. Le transport intégré utilise le framing **HBT v2**, incompatible
  explicitement avec le reçu d'octets v1 du spike.
- BouncyCastle 2.7.0, DLL .NET Standard 2.0, licence MIT, intégrée sous
  `Assets/Plugins/Managed/BouncyCastle`. SHA-256 DLL :
  `d61c1f2ba929a230a58e101ccd850e21f2675fa6b9814ec279633e8a089c3495`.
  Provenance : archive NuGet vérifiée en QUEST-009, hash
  `f091ffccab4d03993e660bace277659a79dee0972f54d7f1f4bd46d680966241`.
  Import explicite uniquement dans l'assembly `HBP.Transfer.Transport`.
  Création de certificat BouncyCastle autorisée par D27, TLS 1.2 et pin complet
  inchangés. L'API .NET `CertificateRequest` non supportée dans les Players est
  retirée de la copie de production.
- Ancien `Shared/Packages/com.crnl.hibop.protocol/Runtime/RemoteAssets.cs`
  consulté via Git à `eb26c323e`. `ApplyLifecycle` purgeait le cache sur
  expiration de lease/background ; cette politique n'est pas reprise. La nouvelle
  propriété de session applique D12, sans cache durable ni lease réseau sur le mesh.
- [Manifeste des preuves](../evidence/QUEST-010/manifest.json). Logs, APK et
  exécutables restent locaux sous `.test-results/quest-010` et `.artifacts/quest-010`.

## Ce que je conseille de reviewer

| Priorité | Entrée | Règle / risque |
| --- | --- | --- |
| 1 | [QuestAnatomySession.ReceiveAsync / Publish / CloseSession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | Dispatch main-thread annulable, historique borné, publication unique et indépendance connexion/contenu. Aucun Clear dans le traitement d'erreur réseau. |
| 2 | [PinnedTlsTransfer.ReceivePayloadAsync / SendPayloadAsync](../../../../Assets/Scripts/HBP/Transfer/Transport/PinnedTlsTransfer.cs) | Chunks et SHA final vérifiés avant callback, accusé après callback réussi ; erreur d'accusé sans rollback. |
| 3 | [QuestAnatomyView.ApplySnapshot / Clear](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) | Mesh et propriétés préparés avant le commit synchrone ; libération de l'ancien mesh après échange, matériau asset partagé. |
| 4 | [CaptureDeliverySelectedAsync](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopAnatomyCapture.cs) et [AnatomyDelivery](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Delivery/AnatomyDelivery.cs) | Capture sélectionnée, encodage hors thread Unity, identité et octets figés pour les répétitions. |
| 5 | [AnatomyDeliveryTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/AnatomyDeliveryTests.cs) et [harness physique](../../../../Spikes/QUEST-010/README.md) | Vérifications reproductibles avec le transport et renderer produits ; bien distinguer loopback, LAN et démonstration utilisateur. |

## Transitions et accusé perdu

```text
CaptureDeliverySelectedAsync -> AnatomyDelivery conservée par Desktop
  -> ServeAsync (TLS, secret après pin, HBT v2, chunks 64 Kio)
  -> QuestAnatomySession.ReceiveAsync
       Connecting -> Receiving -> Preparing -> Idle
       HBNA validé sur worker -> Publish -> QuestAnatomyView.ApplySnapshot
       ACK = statut + SHA-256 complet du HBNA

publication A -> ACK perdu : A reste prêt, connexion fermée
répétition A / même hash  -> AlreadyPublished ; même mesh, même présentation
nouvelle livraison B     -> B prêt ; mesh A libéré
répétition A             -> Superseded ; B reste prêt
fermeture B              -> ressources libérées
répétition B             -> Closed ; aucun nouveau mesh
```

Le SHA-256 couvre le HBNA entier, donc aussi `transferId`, `sessionId`, IDs de
visualisation/colonne et révision. Le serveur compare le hash du reçu à son
offre ; une identité de transfert réutilisée avec un autre hash est rejetée.
Le lecteur de trames ne reçoit jamais le secret avant validation du pin TLS.

`ReceptionState`, `IsConnected`, octets reçus et erreur sont séparés de `IsReady`.
Le transport étant ponctuel, l'état normal après réception est **prêt hors
connexion**. Une reconnexion n'oblige pas à recharger ce contenu pour le manipuler.
Il n'y a aucun transfert de caméra, transform ni geste spatial.

Le registre retient au maximum **256 identités** par vie du récepteur, uniquement
leurs métadonnées. Il refuse ensuite de nouvelles identités plutôt que d'oublier
une ancienne livraison et permettre sa résurrection. Les répétitions connues
restent possibles. `CloseSession` conserve ces entrées ; la destruction du
récepteur les libère. Aucun engagement après kill/reboot.

L'offre accepte au maximum 16 connexions, chaque tentative étant bornée à 20 s,
avec expiration/arrêt par son propriétaire. Le certificat reste vivant jusqu'à
terminaison du serveur. Payload limité explicitement à 64 Mio, même si HBNA
autorise 128 Mio ; pas de troncature ni de reprise par plages. Pas de protocole
de découverte ou d'appairage à code court ajouté dans cette tâche.

## Vérifications effectuées

| ID | Scénario / commande | Résultat / preuve |
| --- | --- | --- |
| T1 | Unity CLI PlayMode, assembly `HBP.Quest.PlayModeTests`, `-questAnatomyFixture C:\HBP\Software\HiBoP\.artifacts\quest-008\fixture\quest-anatomy.hbna` | **27/27 réussis**, exit 0, `playmode.xml`, `playmode-retry.log`. |
| T2 | Corruption de chunk, troncature transport/HBNA, versions HBT/HBNA inconnues, préparation renderer refusée et collision ID/hash | Sept cas TLS réels : session précédente et présentation conservées, aucun reçu de publication, aucune fuite de mesh, transfert suivant réussi. Inclus dans T1. |
| T3 | Accusé perdu, répétition, A→B puis répétition A, disable/enable, fermeture puis répétition B | Statuts attendus et compte d'uploads vérifiés ; mesh remplacé/fermé effectivement détruit après frame. Inclus dans T1. |
| T4 | Annulation / fermeture au milieu de la réception, tentative concurrente | Pas de publication tardive ; conservation ou libération selon l'action explicite ; tentative concurrente rejetée. Inclus dans T1. |
| T5 | Unity CLI EditMode : `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests` et fixture MNI | **86 réussis, 0 échec, 1 ignoré** (test réservé au profil Android), exit 0, `editmode.xml`. Comprend le nouveau test capture→offre immuable malgré modification ultérieure du mesh Desktop. |
| T6 | `Tools/Build-QuestDeliveryProbe.ps1 -Target Android` ; `-Target Windows` | Deux builds IL2CPP réussis, exit 0 : APK HiBoP Android ARM64 117 474 128 octets, 320,73 s, 0 erreur, 65 warnings ; harness Windows 61,76 s, 0 erreur. `android-build.log`, `windows-build.log`, rapports de build. |
| T7 | `Tools/Run-QuestDeliveryProbe.ps1 -Serial 192.168.1.18:5555 -HostAddress 192.168.1.14` | **Réussi sur Quest 3 physique**, TLS LAN direct depuis Windows IL2CPP : hash MNI identique, 69 104 sommets, un upload, conservation hors connexion, libération effective à fermeture et reçu Closed à la répétition. Résultat JSON versionné avec le manifeste. |
| T8 | `Tools/format-code.cmd`, puis même ReSharper 2026.2.0 sur une solution temporaire contenant le C# du serveur | Réussi hors sandbox, exit 0 ; solution Unity régénérée pour inclure les nouveaux fichiers. Logs `format-final.log`, `format-server-final.log`. |
| T9 | Revue indépendante sécurité/intégrité/cycle de vie | Aucun défaut bloquant constaté. Limites des injections détaillées ci-dessous. |
| T10 | EditMode profil Android : contrats, renderer et configuration | **69/69 réussis**, exit 0, `android-editmode.xml`. |
| T11 | Retour au profil DesktopWindows et tests de configuration | **22 réussis, 1 ignoré** (réservé Android), exit 0, `final-desktop.xml`. |
| T12 | Diff final, assets et APK | Réécritures Unity de BuildInfo, icônes Android, préfiltrage URP et migrations OpenXR inspectées puis rétablies ; diff de ces réécritures conservé. APK : bibliothèques sous arm64-v8a uniquement, aucun hbp_core/hbp_math/EEGFormat Desktop. |

Essai physique final : **`20260908-124323`**, lanceur et serveur exit **0**.
Les trois reçus serveur portent le même hash que le résultat Quest.
`offlineRetained`, `replacementNotDuplicated` et `closedReleased` valent `true`.
HiBoP a été arrêté par `am force-stop fr.crnl.hibop.quest`, puis l'absence de PID
a été vérifiée ; le code 1 normal de `pidof` ne fait plus échouer le lanceur.
La connexion ADB Wi-Fi et les réglages de veille D26 sont conservés.

Binaires reproductibles :

- `C:\HBP\Software\HiBoP\.artifacts\quest-010\Windows\DeliveryServer.exe`
- `C:\HBP\Software\HiBoP\.artifacts\quest-010\Android\HiBoP.Quest.apk`

Les hashes des binaires, sources effectivement copiées dans le harness et
sources finales figurent dans le manifeste. Le formatage des deux fichiers du
harness après leur copie ne change que les espaces, comparaison vérifiée.
Les warnings Android comprennent des dépréciations d'API Unity (dont
`FindFirstObjectByType` dans le diagnostic), sans erreur de compilation.

Deux défauts du banc ont été corrigés avant le succès physique : activité
Android héritée de la sonde incorrecte (l'activité réelle est
`com.unity3d.player.UnityPlayerActivity`, désormais résolue depuis le package),
puis échec de handshake du premier serveur expérimental .NET 10 avec Quest.
Le harness livré utilise exclusivement le serveur **Unity Windows IL2CPP** et
le transport embarqué qualifié ; aucune source de l'essai .NET n'est conservée
dans le changement. Les logs des essais échoués restent identifiés dans le
manifeste et ne comptent pas comme succès. La cause interne de l'échec .NET
n'a pas été qualifiée : aucune conclusion sur son TLS en général n'est tirée.

Le contrôle automatique a initialement refusé l'installation/envoi, puis a
maintenu ce refus après vérification de D14 et du hash. Le propriétaire a
explicitement répondu **« Oui, j'autorise cet essai »** le 2026-09-08 pour cet APK,
ce MNI et son Quest `192.168.1.18:5555`. L'essai a ensuite été autorisé et exécuté.
Cette confirmation porte sur l'opération de test, pas sur une recette visuelle.

Le premier run PlayMode a été arrêté car son `OneTimeSetUp async Task` attendait
une continuation Unity avant le démarrage du PlayerLoop. Le certificat est
maintenant créé synchroniquement par calcul pur dans ce setup ; tous les tests
de transport attendent leurs tâches directement. Le run relancé termine en
5,09 s. Aucune assertion async bloquante ni attente synchrone de Task ajoutée.

La perte d'accusé est injectée côté émetteur après sa lecture TLS et avant son
observation applicative. Elle prouve l'idempotence après publication ; elle ne
prétend pas reproduire une panne radio ni une exception d'écriture côté Quest.
L'annulation testée coupe le payload en cours de réception ; la garde d'une
publication déjà en file Unity après fermeture a été relue, sans injection
déterministe distincte à cette frontière. Les erreurs renderer testées sont
des refus de métadonnées, sans simulation d'épuisement mémoire/GPU.

## Validation manuelle et limites

**Aucune validation manuelle nécessaire pour QUEST-010.** Les essais de livraison
sont automatisés. La démonstration produit avec sélection/appairage/envoi par UI
et une minute sans réseau appartient à QUEST-011/012.

Le harness physique réutilise le HBNA MNI déjà capturé depuis la vraie colonne
Desktop en QUEST-006/008. Il ne recapture pas une visualisation interactive
pendant cet essai. Son serveur Windows est un Player Unity IL2CPP avec les sources du transport
produit, construit dans un projet de harness isolé ; il ne constitue pas un
processus auxiliaire de HiBoP. Le raccord capture→offre est vérifié dans l'Editor ici. Mac, reprise durable et appairage UX restent hors scope.

**Prochaine tâche proposée : QUEST-011**, sans exécution automatique.
