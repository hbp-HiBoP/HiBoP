# ADR P06 — gate du spike transport et codec

- **Statut :** ACCEPTED — D10/D11 `PROVISIONAL — WINDOWS/QUEST VALIDATED`
- **Date :** 2026-09-02
- **Accepté par :** propriétaire du dépôt HiBoP via validation explicite de P06-A–E, puis de la conclusion D10/D11 le 2 septembre 2026
- **Baseline inspectée :** branche `feature/xr`, commit `72ba9bb3e8afa118fc5527b2e5395ad30793942a`
- **Décisions héritées :** D10 baseline HTTPS + WSS, D11 contrôle AOT-safe et buffers `float32` little-endian, D17 aucune donnée patient persistée sur Quest
- **État d'exécution :** Windows et Quest 3 physique exécutés; macOS/Linux cross-publiés mais non exécutés; aucun candidat ne peut devenir une dépendance de production

Ce document conserve le gate préalable et enregistre sa conclusion. D10/D11 sont acceptées comme baseline provisoire Windows+Quest afin d’autoriser P07+ derrière des interfaces remplaçables. Les exécutions natives macOS/Linux sont explicitement différées et conditionnent uniquement la qualification de ces plateformes et un éventuel passage à `RESOLVED`.

## État initial observé

- `com.crnl.hibop.protocol` est un squelette UPM sans dépendance et n'est pas modifié par ce gate.
- Le dépôt et le projet XR utilisent Unity `6000.5.2f1`; les modules Windows, Linux, macOS et Android sont installés sur l'hôte Windows.
- Au moment du gate, aucun éditeur Unity/MCP n'est connecté, aucun Quest n'est visible par ADB et aucun runner macOS/Linux n'est accessible depuis le workspace. Une compilation croisée ne sera pas présentée comme une mesure d'exécution native.
- Tout prototype devra vivre sous `Spikes/P06/` dans ses propres projets et manifests. Il ne pourra référencer ni `Assets/` de production, ni `XR/Assets/`, ni modifier les manifests des deux projets HiBoP.

## P06-A — candidats, versions et licences

### Transport

Deux piles seront comparées. Elles utilisent le même certificat et le même port TLS, avec des routes logiquement séparées : `/control` en WSS et `/assets/{sha256}` en HTTPS avec ranges.

| ID | Serveur Desktop | Client Unity/Quest | Licence | Rôle dans le spike |
| --- | --- | --- | --- | --- |
| T1 | Kestrel / ASP.NET Core `10.0.11`, publié self-contained | NativeWebSocket branche `upm-2` au commit `c612a4fef60f2ae57614b73202d2d261ba56aa3e` pour WSS; `UnityWebRequest` de Unity `6000.5.2f1` pour HTTPS | .NET MIT; NativeWebSocket MIT; Unity sous licence du projet | candidat recommandé pour maintenance, TLS système et serveur multi-OS; implique un host compagnon plutôt qu'une intégration Unity implicite |
| T2 | `websocket-sharp` au commit `7aed0002451cf70ed74bc2e1ca6504dab5b50a10`, `HttpServer`/`WebSocketServer` | même source `websocket-sharp` | MIT | témoin C# embarquable avec callback certificat; rejet attendu si maintenance, limites d'entrée, TLS ou IL2CPP sont insuffisants |

T1 n'est admissible comme processus compagnon que sous deux contraintes produit :

- l'utilisateur lance et utilise uniquement l'exécutable et l'interface HiBoP; le compagnon est démarré, supervisé et arrêté sans console, fenêtre, installation préalable de runtime ou interaction propre;
- le delta de distribution est mesuré compressé et installé. Le budget provisoire commun est au plus 10 % de l'archive HiBoP et 150 Mio compressés; au-delà, l'ADR doit recommander des distributions Windows standard et XR distinctes.

Le spike démontre la transparence avec un launcher jetable isolé. L'intégration de ce launcher à HiBoP reste hors P06.

Les commits Git sont des versions immuables du spike, pas des dépendances UPM de production. T1 et T2 échouent immédiatement si le client WSS ne peut pas appliquer le même pin SPKI que le client HTTPS. Le comportement par défaut de `websocket-sharp`, qui accepte les certificats serveur, est interdit : le callback du spike doit refuser par défaut et appliquer P06-D.

Sources de version et licence :

- Kestrel/ASP.NET Core `10.0.11` : <https://www.nuget.org/packages/Microsoft.AspNetCore.App.Ref/10.0.11>
- NativeWebSocket et sa licence MIT déclarée par le package verrouillé : <https://github.com/endel/NativeWebSocket>
- websocket-sharp, TLS et callback de validation : <https://github.com/sta/websocket-sharp>

### Schéma de contrôle

| ID | Version figée | Licence | Configuration testée |
| --- | --- | --- | --- |
| C1 | `Google.Protobuf 3.36.1` | BSD-3-Clause | types C# générés par `protoc`; aucun descriptor dynamique requis au runtime |
| C2 | `MessagePack 3.1.8` | MIT, composants LZ4 BSD-2-Clause | source generator AOT, clés numériques explicites, `UntrustedData`; typeless et Unity blit resolvers interdits |
| C3 | `MemoryPack 1.21.4` | MIT | source generator AOT, mode version-tolerant; aucun type connu comme incompatible entre .NET et Unity |

Sources :

- Protobuf `3.36.1` et licence : <https://www.nuget.org/packages/Google.Protobuf/3.36.1>, <https://github.com/protocolbuffers/protobuf/blob/main/LICENSE>
- MessagePack `3.1.8`, support Unity/source generator et durcissement des données non fiables : <https://www.nuget.org/packages/MessagePack/3.1.8>, <https://github.com/MessagePack-CSharp/MessagePack-CSharp>
- MemoryPack `1.21.4`, Unity/IL2CPP et limites de compatibilité : <https://www.nuget.org/packages/MemoryPack/1.21.4>, <https://github.com/Cysharp/MemoryPack>

Les versions MessagePack antérieures à `3.1.7` sont exclues à cause des avis de sécurité 2026, notamment les allocations non bornées du resolver Unity. Même avec `3.1.8`, le spike n'utilise pas de resolver blit et borne l'enveloppe avant désérialisation.

### Buffers et compression

- B0, baseline : blocs contigus `float32` IEEE-754 little-endian, sans compression.
- B1, mesure de quantification séparée : `float16` IEEE-754 little-endian, jamais combiné à une compression pendant la comparaison de précision.
- B2, mesure de compression séparée : B0 compressé en bloc LZ4 avec `K4os.Compression.LZ4 1.3.8` (MIT), sans quantification.
- Zstd et les codecs natifs sont hors matrice initiale : ils augmenteraient la surface AOT/native sans être nécessaires pour décider si la baseline non compressée tient le budget.

Source LZ4 : <https://www.nuget.org/packages/K4os.Compression.LZ4/1.3.8>, <https://github.com/MiloszKrajewski/K4os.Compression.LZ4>

## P06-B — charges et réseaux de test

### Plateformes obligatoires

| Plateforme | Binaire et rôle | Exécution requise |
| --- | --- | --- |
| Windows 11 x64 | host self-contained + client de référence | machine Windows native |
| macOS ARM64 | host self-contained + client de référence | runner/mac physique natif; une simple cross-compilation ne suffit pas |
| Ubuntu 24.04 x64 | host self-contained + client de référence | runner Linux natif |
| Quest 3 Android ARM64 | client Unity `6000.5.2f1`, IL2CPP, Vulkan | casque physique, APK sideloadé, même point d'accès que le host |

Chaque rapport brut porte OS/runtime, CPU, RAM, commit, candidat, hash du binaire, réseau, répétition et horodatage. Les trois Desktop exécutent le même scénario; Quest exécute le client contre le host Windows de référence.

La validation est progressive :

| Gate | Preuve | Autorisation |
| --- | --- | --- |
| `P06-W` | host et client Windows natifs, sécurité et mesures locales | poursuivre le spike et produire l'APK |
| `P06-WQ` | Windows + Quest 3 physique IL2CPP | retenir une décision provisoire D10/D11 et avancer P07+ derrière des interfaces remplaçables |
| `P06-ML` | exécutions natives macOS ARM64 et Ubuntu 24.04 | rendre D10/D11 éligibles à l'acceptation finale |

Après `P06-WQ`, les paquets suivants peuvent construire le prototype Windows+Quest même si D10/D11 portent encore `PROVISIONAL — WINDOWS/QUEST VALIDATED`. Ils ne doivent annoncer ni publier un support macOS/Linux qualifié. Une incompatibilité native ultérieure rouvre D10/D11 et peut imposer l'adaptation ou le remplacement du transport derrière ses interfaces.

### Profils de charge

1. **Contrôle seul** : warm-up 30 s, puis 20 commandes echo/s pendant 120 s; payloads 64, 256, 1 024 et 16 384 octets.
2. **Bulk seul** : asset synthétique déterministe de 100 MiB, ranges de 1 MiB, SHA-256 par chunk et final.
3. **Concurrence nominale** : le même bulk pendant 20 commandes/s et un flux B0 de 561 470 octets à 10 Hz; cinq répétitions de 120 s après warm-up.
4. **Concurrence maximale V1** : huit colonnes, bundle de 4,28 MiB à 5 Hz, avec politique latest-wins et files bornées; trois répétitions de 120 s.
5. **Ruptures** : coupures réseau de 1, 5 et 30 s à 25 %, 50 % et 90 % du bulk; reprise par range, reconnexion WSS et absence de double application.
6. **Adversarial** : certificat/SPKI modifié, mauvais code d'appairage, chunk et hash final corrompus, header tronqué, longueur négative/surdimensionnée, nesting maximal, champ inconnu et allocation bomb.
7. **Codec** : 10 000 warm-ups puis 100 000 encode/decode de `ClientHello`, `Command`, `CommandOutcome`, delta et des payloads invalides; mesures séparées C1–C3.
8. **Buffer** : B0, B1 et B2 sur rampes, bruit pseudo-aléatoire déterministe et buffers synthétiques D5/D6; taille, encode/decode, copies, allocations et erreur max/RMS. B1 reste inéligible sans tolérance scientifique ultérieure.

### Réseaux

- N0 : loopback, uniquement pour le coût logiciel et les allocations.
- N1 : LAN filaire Desktop nominal, sans émulation.
- N2 : Wi-Fi local réel du Quest, AP et canal consignés sans adresse ni identifiant d'appareil.
- N3 : réseau dégradé reproductible, 25 ms RTT ajouté, 1 % de perte et 100 Mbit/s.
- N4 : réseau contraint, 50 ms RTT ajouté, 2 % de perte et 20 Mbit/s.

N3/N4 doivent employer le mécanisme natif documenté de chaque OS ou un routeur de test commun; ils ne remplacent pas N2. Une plateforme sans exécution native comparable déclenche l'arrêt P06 au lieu de recevoir un résultat estimé.

### Mesures et sorties brutes

- RTT contrôle p50/p95/max et taux d'échec;
- débit bulk utile, durée et octets retransmis;
- temps d'appairage et de reprise p50/p95/max;
- allocations par opération, octets alloués, collections GC et pic mémoire;
- copies explicites par chemin critique, instrumentées dans le candidat;
- tailles et temps encode/decode des codecs;
- rejet attendu de chaque entrée adverse, sans OOM, crash ni allocation au-delà de la limite;
- JSON Lines brut plus un résumé Markdown, sans donnée patient ni identifiant réseau/appareil.

## P06-C — règles de schéma et codec AOT-safe

Les trois candidats représentent le même schéma wire distinct des types P02. Le mapping vers `Contracts` reste dans le spike et ne modifie pas les types publics.

- unions fermées par discriminant numérique; `0` reste `Unknown`;
- champs numérotés stables, absence explicite et champs inconnus tolérés seulement selon la capability négociée;
- aucune réflexion dynamique, génération IL, typeless, polymorphisme ouvert ou recherche de type par nom;
- enveloppe contrôle maximale : 64 KiB; profondeur maximale : 16; chaîne : 4 KiB; collection : 1 024 éléments sauf limite plus basse propre au message;
- validation de `payloadLength` et de la limite par type avant appel au codec;
- les tableaux lourds ne passent jamais par C1–C3 : le codec ne transporte que leur descriptor/hash;
- golden vectors octet-par-octet pour chaque codec et chaque version de schéma, vérifiés sous .NET et IL2CPP;
- C1–C3 sont rejetés sur exception non rattrapable, allocation non bornée, dépendance au codegen runtime ou divergence de golden vector.

## P06-D — preuve de possession et pinning

Le spike teste exclusivement TLS/X.509 et SHA-256 fournis par les runtimes; aucune primitive ou signature propriétaire n'est introduite.

1. Le host crée une identité X.509 ECDSA P-256 de test, `CA=false`, EKU serverAuth et SAN explicites, et présente le même certificat sur HTTPS et WSS.
2. Le Desktop affiche un code décimal à six chiffres dérivé des 20 premiers bits de `SHA-256(SubjectPublicKeyInfo DER)`. Ce code est un SAS à comparer, pas un mot de passe transmis. L'inversion de saisie est exclue de P06 : afficher le code sur Quest puis le saisir sur Desktop ne permettrait pas au Quest de valider directement l'identité du host sans protocole de bootstrap supplémentaire.
3. En mode d'appairage explicite uniquement, l'utilisateur saisit le SAS sur le client avant la connexion. Le callback TLS calcule le SAS du certificat effectivement présenté et refuse la négociation s'il diffère. La preuve de possession de la clé privée est celle de la négociation TLS standard.
4. Après succès, le client épingle `SHA-256(SPKI DER)` pour la session de spike. Toute connexion HTTPS ou WSS suivante compare le pin en temps constant et refuse avant payload applicatif si l'identité change.
5. Une réassociation exige une action utilisateur explicite côté Desktop et Quest. Aucun fallback `accept all`, aucune empreinte de certificat entier dépendante du renouvellement et aucun secret patient ne sont permis.
6. TLS 1.2 minimum, TLS 1.3 préféré; compression WebSocket désactivée pour le contrôle authentifié. Le host limite origins, taille, cadence, connexions et timeouts avant décodage.

Le spike doit démontrer que les APIs de certificat de T1/T2 exposent le SPKI réel sous IL2CPP. L'impossibilité d'appliquer exactement le même pin à WSS et HTTPS est un veto, pas une invitation à contourner la validation.

## P06-E — critères pondérés et autorité

### Vetos binaires

Un candidat est inéligible s'il échoue à l'un des points suivants : build et exécution sur les quatre plateformes; contrôle p95 supérieur à 100 ms sous charge nominale; identité modifiée ou corruption acceptée; codec non AOT; crash/OOM/allocation non bornée; licence incompatible; données applicatives en clair; files ou reprise non bornées.

### Score après vetos

Chaque axe reçoit une note de 0 à 5, multipliée par son poids. Le meilleur total n'est retenu que si l'écart avec le suivant est supérieur ou égal à 5 points sur 100; sinon l'option la plus simple et la mieux maintenue l'emporte, avec justification explicite.

| Axe | Poids | Preuve attendue |
| --- | ---: | --- |
| sécurité TLS/pinning et robustesse aux entrées hostiles | 30 | tests négatifs, limites, audit de surface |
| portabilité Windows/macOS/Linux/Quest IL2CPP | 25 | builds et exécutions natifs, mêmes golden vectors |
| priorité contrôle, débit et reprise | 20 | p50/p95/max, ranges, coupures, hash final |
| mémoire, GC et copies | 10 | compteurs instrumentés et pics par plateforme |
| maintenance, supply chain et licence | 10 | activité upstream, version immuable, SBOM/licences |
| simplicité d'exploitation et d'intégration future | 5 | ports, firewall, packaging, volume de glue |

### Autorité proposée

L'autorité d'acceptation est le **propriétaire du dépôt HiBoP**. Aucun référent sécurité externe ne sera disponible pour ce projet : le propriétaire accepte de s'appuyer sur la revue de base P06-D, les tests adversariaux automatisés et la revue interne prévue dans P14, sans certification ni pentest complet. Les risques résiduels documentés sont acceptés pour poursuivre le prototype, mais restent des contraintes d'implémentation et de publication.

## Autorisation de prototyper

P06-A–E ont été explicitement acceptées le 2026-09-02 avec les contraintes de taille, transparence et progression ci-dessus. Le spike est autorisé sous `Spikes/P06/`. Le Quest est désormais mesuré; l’indisponibilité actuelle de machines macOS/Linux natives limite encore l’acceptation finale mais ne bloque pas le prototype Windows+Quest ni la préparation portable.

## Forme de l'ADR final après mesures

Le présent ADR est accepté au niveau `PROVISIONAL — WINDOWS/QUEST VALIDATED`. Le registre D10/D11 peut donc quitter `REQUIRES_SPIKE` et P07+ peut implémenter la baseline derrière des interfaces remplaçables. Le passage à `RESOLVED`, la qualification macOS/Linux et toute publication multi-plateforme exigent encore les validations natives correspondantes.

## Résultats acquis

Les preuves reproductibles et limites sont détaillées dans [la validation Windows/Quest build](../evidence/P06/windows-validation.md) et [la revue sécurité/licences](../evidence/P06/security-license-review.md).

| Gate | Résultat | Conclusion |
| --- | --- | --- |
| P06-W | cinq runs Windows N0, 9 654 commandes, 0 échec, pire p95 `0,7266 ms`, bulk moyen `89,732 Mbit/s` | PASS local Windows |
| P06-WQ | Quest 3 Android 14/API 34, cinq runs N2 de 120 s, 12 000 commandes, 0 échec, pire p95 `19,1079 ms`, bulk moyen `38,159 Mbit/s`, golden vectors et rejets identité/corruption | PASS Windows+Quest |
| P06-ML | self-contained Linux x64 et macOS ARM64 cross-publiés | packaging PASS, natif INCONCLUSIVE |

Le sidecar Windows self-contained mesure `111 554 005` octets installé et `50 583 915` octets compressé. Pour une archive HiBoP estimée à environ 200 Mio compressés par le propriétaire, le delta est d’environ `25 %` : le budget relatif de 10 % échoue, même si le plafond absolu de 150 Mio passe. Ce dépassement est accepté comme non bloquant pour le prototype. Avant distribution, il faudra choisir entre une édition XR séparée et une solution embarquée moins coûteuse; D10 reste explicitement réouvrable si une pile embarquée démontre le même pinning, les mêmes limites et des performances équivalentes avec un coût de distribution inférieur. Le launcher jetable a démontré un démarrage sans shell/fenêtre et un arrêt de l’arbre de processus; l’intégration UI HiBoP reste hors P06 et appartient désormais à P15.

## Comparaison observée et candidats rejetés

### Transport

- **T1 verrouillé est rejeté par veto P06-D.** L’inspection du commit NativeWebSocket montre qu’il instancie un `ClientWebSocket` privé sans exposer `RemoteCertificateValidationCallback` ni ses options. Le même pin SPKI ne peut donc pas être appliqué à WSS et HTTPS sans fork.
- **T2 client franchit l’exécution IL2CPP.** `websocket-sharp` expose le callback certificat, négocie WSS sur Quest et applique le même pin que HTTPS. Il compile avec un patch local limité à `AssemblyVersion("1.0.2.0")`. Son serveur alternatif n’a pas été mesuré : T2 complet reste inéligible.
- **T3 hybride est la baseline provisoire acceptée :** Kestrel self-contained pour le host, `UnityWebRequest` HTTPS et websocket-sharp WSS côté Quest, avec un seul certificat/pin. Le runtime Quest impose deux adaptations isolées et documentées : extraction DER bornée du SPKI, car `GetECDsaPublicKey()` n’est pas implémenté, et header WSS `X-P06-Access-Token`, car websocket-sharp interdit `Authorization` dans ses headers utilisateur. Une alternative embarquée plus petite reste recevable comme réouverture de D10, pas comme travail bloquant P07+.

Aucun score final P06-E n’est attribué : la portabilité vaut 25 % et les vetos exigent quatre exécutions. Donner une note complète avec trois plateformes manquantes créerait une fausse précision.

### Codecs et buffers

Sur 100 000 aller-retours Windows après warm-up : Protobuf `328` o / `457,290 ms` / `2 640` o alloués par opération; MessagePack `325` o / `263,672 ms` / `1 240` o; MemoryPack `444` o / `79,929 ms` / `1 360` o. Les trois golden vectors ont aussi été reproduits octet par octet dans l’APK IL2CPP physique, aux tailles respectives `328`, `325` et `444` octets.

La recommandation D11 reste **Protobuf pour le contrôle** : son coût absolu est négligeable devant le réseau et son contrat schema-first, sa maturité et son découplage l’emportent provisoirement sur les gains microbenchmark. MessagePack reste le second candidat si les allocations Protobuf deviennent mesurables sur Quest. MemoryPack n’est pas recommandé malgré sa vitesse Windows, en raison du wire plus gros et du couplage C# plus fort.

Pour les gros buffers, la recommandation est **float32 IEEE-754 little-endian non compressé par défaut**. Float16 divise la taille par deux mais introduit une erreur max `0,00024414` et exige une décision scientifique par représentation. LZ4 a agrandi l’échantillon de `552 832` à `555 001` octets tout en ajoutant encode/copie; il reste une capability opt-in uniquement si un dataset réel démontre un gain.

## Décision D10/D11 acceptée

- **D10 provisoire accepté :** Kestrel/.NET 10.0.11 self-contained, HTTPS ranges + WSS même port/certificat, websocket-sharp côté Quest, SAS Desktop → Quest puis pin SHA-256(SPKI), limites bornées et launcher invisible.
- **D11 provisoire accepté :** Protobuf 3.36.1 pour le contrôle, framing P06 borné et versionné; gros buffers float32 little-endian avec descriptor, longueur et SHA-256, aucune compression par défaut.

Le propriétaire accepte cette conclusion et les risques résiduels le 2 septembre 2026. P07+ est autorisé derrière des interfaces remplaçables. macOS/Linux restent préparés mais non qualifiés; aucune promesse de support ne doit être faite avant leurs exécutions natives. Cette acceptation ne généralise pas automatiquement le code de spike vers la production.
