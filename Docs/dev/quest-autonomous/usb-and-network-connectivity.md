# USB NCM et découverte réseau Quest

Référence du code au 2 novembre 2026. L'USB produit utilise **NCM (interface réseau)** et le même protocole applicatif que LAN/Wi-Fi ; il ne dépend ni d'un tunnel ADB ni du mode développeur. ADB demeure un outil de build/diagnostic séparé.

## Découverte et connexion

Le service mDNS est `_hibop-quest._tcp.local` sur UDP 5353 ; la connexion TLS utilise TCP 45871. Les annonces exposent version, identité, nom et chemin réseau, jamais code d'association ou secret. La découverte n'authentifie pas le casque : association par code/J-PAKE, confirmation liée à TLS et identité épinglée établissent la confiance.

[QuestDiscovery](../../../Assets/Scripts/HBP/Transfer/Transport/QuestMdnsDiscovery.cs) observe les interfaces et reconstruit son écoute après changement réseau. [QuestEndpoint](../../../Assets/Scripts/HBP/Transfer/Transport/QuestEndpoint.cs) conserve adresse, chemin et génération d'interface. Plusieurs chemins sont regroupés sous la même identité de casque. USB est préféré à la connexion/reconnexion ; brancher USB pendant une session Wi-Fi saine ne migre pas cette session.

Une IPv6 link-local exige le scope de **l'interface Desktop**, jamais celui lu sur Android. La saisie manuelle permet une adresse complète et la sélection de l'interface locale ; conserver tous les groupes de l'adresse. Les scopes peuvent changer après rebranchement. Une annonce expirée ne doit pas remplacer un endpoint actuel d'une nouvelle génération.

Les interruptions réseau éligibles reprennent automatiquement les scènes, contrairement à une annulation utilisateur ou une erreur de données : voir [Display](transfer.md#un-lot-display). Désactiver `AutomaticallyConnect` empêche le démarrage automatique initial ; cela ne signifie pas interrompre la récupération d'une connexion déjà établie. Disconnect explicite suspend les tentatives. Une nouvelle association depuis un autre Desktop peut remplacer le credential accepté par le Quest ; la persistance Windows protégée localement n'est pas un fichier portable.

Dans [QuestPairing.ConnectAsync](../../../Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs), l'annulation ferme le **Socket capturé**, attend la fin de Connect/TLS, puis dispose le TcpClient. Fermer d'abord le TcpClient peut vider sa propriété Client pendant le couple Begin/EndConnect de Unity. Les timeouts réseau restent des erreurs, pas des succès d'annulation.

Limite actuelle du stockage : [PairingStorage](../../../Assets/Scripts/HBP/Transfer/Transport/PairingStorage.cs) protège les fichiers par DPAPI lié à l'utilisateur Windows, mais retourne les octets inchangés hors Windows. La persistance fonctionnelle macOS/Linux ne démontre donc pas un chiffrement au repos. Sur Android, les appelants utilisent le stockage privé sans sauvegarde automatique. Ne pas publier les fichiers d'association ni leurs secrets.

[TransportIdentity](../../../Assets/Scripts/HBP/Transfer/Transport/TransportIdentity.cs) génère le certificat via BouncyCastle pour la compatibilité IL2CPP, où le chemin `CertificateRequest` du spike n'était pas utilisable. Le choix SHA/3DES du conteneur PKCS#12 répond à la lecture Mono ; ce n'est pas le cipher TLS négocié. L'empreinte du certificat complet est vérifiée avant l'envoi du credential.

## Qualification par plateforme

La matrice ci-dessous décrit les qualifications matérielles documentées, sans constituer une nouvelle recette du code courant. Ces résultats ne conditionnent pas la publication des plateformes dans la CI.

| Plateforme | Preuve et portée |
| --- | --- |
| Windows 11 + Quest 3 | [Campagne du 27 octobre 2026](../../../Validation/Quest/connectivity/windows-quest3-qualification-2026-10-27.md), builds identifiés, USB sans Wi-Fi et mode développeur désactivé fonctionnels. Refus/retry du consentement NCM non exercé, accepté non bloquant. |
| macOS ARM64 | Non qualifié physiquement ; contrôles de packaging et CI ne suffisent pas. |
| Linux x64 | Non qualifié physiquement ; contrôles de packaging et CI ne suffisent pas. |
| Quest 3S | Hors de la campagne Windows ; aucune extrapolation depuis Quest 3. |

Sur macOS, les clés `NSLocalNetworkUsageDescription` et `NSBonjourServices` sont ajoutées au bundle final avant signature. Lancer par Finder pour exercer les autorisations utilisateur ; une exécution depuis Terminal n'est pas équivalente. La signature ad-hoc CI ne remplace pas Developer ID/notarisation. Sous Linux, vérifier interface NCM (`cdc_ncm`) et IPv6 locale ; le produit ne requiert ni exécution root ni démon Avahi. Il n'est pas nécessaire d'activer IPv6 globalement sur Internet. Ne pas désactiver globalement le pare-feu ou IPv6 pour contourner un diagnostic.

Les dépendances mDNS sont préparées et épinglées par [Tools/QuestMdns](../../../Tools/QuestMdns/README.md) et son verrou ; ne pas substituer un package arbitraire. Les contrôles sans ADB et de bundle sont décrits dans [déploiement](deployment.md).

## Recette ciblée sur un nouvel OS

Conserver modèles/OS, version système exacte du Quest, versions/hashes des builds et logs locaux. Utiliser les mêmes scènes et distinguer délai de connexion TLS, publication de scène et synchronisation initiale.

| Cas | Observation attendue |
| --- | --- |
| C1 — USB seul | Sans Wi-Fi, association puis scène et mutation dans les deux sens ; aucun ADB produit. |
| C2 — IPv6 manuelle | Adresse complète et scope Desktop courant ; connexion puis reconnexion. |
| C3 — USB + Wi-Fi | USB préféré à une nouvelle connexion ; session saine non migrée. Retirer USB vérifie le repli, puis rebrancher sans imposer une migration active. |
| C4 — Interruption pendant Sending | Retirer le câble, observer la reprise jusqu'à 30 s sans Retry/Send/Reconnect ; vérifier la scène finale et absence de duplication. |
| C5 — Redémarrage Quest | Relancer l'application et vérifier récupération de session/scènes et mutations, sans réassociation injustifiée. |
| C6 — Desktop/Disconnect | Redémarrage et préférence d'autoconnexion distingués ; après Disconnect explicite, observer au moins 10 s sans reprise spontanée. |

Le refus de consentement NCM ne doit pas être forcé par une réinitialisation du casque. S'il n'est pas aisément reproductible, le consigner non exercé ; la décision Windows ne qualifie pas ce cas sur les autres systèmes. Un test en veille doit vérifier le réveil effectif et le casque porté avant d'attribuer une absence réseau au protocole.
