# NCM et mDNS : fonctionnement et qualification

## Contrat produit

Desktop et Quest sont mis à jour ensemble. L'APK doit être mis à jour en place,
avec le même identifiant et la même signature ; ne pas désinstaller ni effacer
les données de l'application. Les identités et secrets `.pair` gardent leur
format et leur emplacement. Une annonce DNS, adresse ou interface expirée n'est
jamais une expiration de l'association.

La liste regroupe les chemins par empreinte TLS. USB est préféré à la connexion
et à la reconnexion ; brancher USB ne migre pas un Wi-Fi actif. Une session
manuellement ouverte reprend après une coupure même si l'ouverture automatique
de nouvelles connexions est désactivée. Déconnexion/annulation explicites
suspendent la reprise. L'adresse manuelle accepte IPv4 et IPv6 ; une adresse
link-local demande une interface **Desktop**, pas l'indice affiché sur Quest.

Objectif : reprise en moins de 15 s avec Wi-Fi et application disponibles, hors
transfert/restauration lourde. Après la grâce de synchronisation de 500 ms,
utiliser la réconciliation existante plutôt qu'allonger la durée de vie des files.

## Implémentation

- NetworkMonitor Android possède les demandes USB, les réseaux Wi-Fi et les
  annonces NSD par réseau. Consentement uniquement au premier plan et câble
  présent ; Retry USB après refus. Les requêtes expirées sont déjà désinscrites
  par Android. Les inscriptions tardives NSD obsolètes sont retirées.
- QuestConnectionPanel lie chaque listener à son Network via
  `android_setsocknetwork`. QuestPairing garde un seul propriétaire, ses secrets
  et ses limites 72/64 partagées entre interfaces. Perdre USB ne ferme pas Wi-Fi.
- Service `_hibop-quest._tcp.local.`, TCP 45871. TXT : `v=1`, `id` empreinte base64,
  `name`, `path=usb|lan`. Aucun secret/code. DNS-SD fournit des candidats ; TLS
  vérifie toujours l'empreinte. L'ancien codec de description TLS reste distinct.
- Desktop embarque la variante mDNS documentée dans `Tools/QuestMdns/README.md`.
  Cache par interface, retrait/TTL, résolution des instances puis cibles SRV,
  générations monotones et réception des informations d'interface système.
  Surveillance réseau avec repli par comparaison des interfaces toutes les 2 s.
- Plus aucun lancement ADB par le produit. Les outils ADB de développement sont
  conservés. Les anciens dossiers de sortie StreamingAssets/QuestUsb sont
  supprimés et les builds/ZIP refusent les exécutables ADB résiduels.

## Validation des builds et références de test

Décision du 27 octobre 2026 : privilégier les essais fonctionnels représentatifs
sur les builds réels et éviter de bloquer la livraison pour une matrice exhaustive.
La liste ci-dessous sert de référence ; les cas non exercés restent signalés.
Le refus/retry du consentement USB est non bloquant s'il ne peut pas être provoqué
facilement sur le casque disponible. Ne pas passer du temps à réinitialiser
l'appareil ou les associations pour forcer ce dialogue.

Pour les essais à venir, utiliser la [campagne courte Mac/Linux](campagne-macos-linux.md),
qui précise les acquis Windows, les six scénarios à effectuer et la passation à
un agent sans historique. Les listes ci-dessous restent une référence technique.

Périmètre matériel retenu le 27 octobre 2026 : **Quest 3 uniquement**. Quest 3S
est hors périmètre de cette qualification, à la demande de l'utilisateur ; son
absence ne bloque pas la validation Windows et les preuves ne lui sont pas
étendues. La même limite doit être indiquée dans les preuves Mac/Linux.

`qualification.json` bloque la publication de chaque plateforme tant que
`qualified` est faux ou qu'aucun fichier de preuve n'est référencé. Les builds CI
restent disponibles pour qualification. Mettre true après validation des parcours
essentiels sur les Players IL2CPP réels, en référençant un bilan qui distingue les
réussites et les limites acceptées. La compilation seule ne suffit pas. Windows
peut être validé indépendamment de Mac/Linux ; leurs essais doivent cibler
l'intégration réseau et les particularités de chaque OS, sans rejouer toute la
validation commune systématiquement.

Consigner : révision, versions Desktop/APK, système et architecture, modèle Quest,
Horizon OS/version Android, nom et indice local de l'interface, pilote, câble,
consentements, règles de pare-feu, résultats et délais mesurés. Conserver logs
Desktop/Quest et rapports de build sans codes ni secrets d'association.

Pour chaque OS :

- [ ] Installation/mise à jour en place : association précédente utilisable sans code.
- [ ] USB seul avec Wi-Fi Quest coupé, sans ADB accessible au produit.
- [ ] USB avec débogage désactivé ; sans mode développeur si l'APK déjà installé le permet.
- [ ] Premier appairage par code, redémarrages puis reconnexion sans code.
- [ ] Lancement sans câble puis branchement ; ordre de lancement Desktop/Quest inversé.
- [ ] Consentement accepté, refusé, nouvelle tentative ; absence de demandes en boucle.
- [ ] Même casque visible en USB/Wi-Fi : une entrée ; plusieurs casques distincts.
- [ ] USB prioritaire au départ ; branchement sans migration d'un Wi-Fi actif.
- [ ] Débranchement au repos, en transfert et en synchronisation ; reprise Wi-Fi mesurée.
- [ ] Veille/réveil, retrait/rebranchement et changement d'adresse sans réappairage.
- [ ] Connexion automatique désactivée : pas de nouvelle connexion spontanée,
  mais reprise d'une session ouverte ; annulation/déconnexion restent respectées.
- [ ] Réseau filtrant mDNS : message exploitable et connexion manuelle IPv4/IPv6.
- [ ] Coexistence avec le service mDNS système/autres applications ; fermeture sans socket résiduelle.
- [ ] Transfert représentatif et gestes synchronisés après interruption supérieure à 500 ms.
- [ ] `Tools/Test-NoAdb.ps1` sur le dossier final et l'archive ; console sans nouvelle erreur.

## Windows 11 ; Windows 10 opportuniste

Qualifier Quest 3 sur Horizon OS stable à jour. Relever la première
version Horizon réellement validée, sans déduire une compatibilité NCM du seul
numéro d'API Android. Le bridge utilise les API réseau/NSD disponibles à partir
d'Android 33 ; l'USB indisponible ne doit pas empêcher le Wi-Fi.

Diagnostics PowerShell : `Get-NetAdapter`, `Get-NetIPAddress -AddressFamily IPv6`,
`Get-NetIPInterface -AddressFamily IPv6`, et informations de pilote du Gestionnaire
de périphériques. Attendu : interface NCM active avec IPv6 link-local, routage
local sans passerelle Internet et UDP 5353/TCP 45871 autorisés pour HiBoP.
Contrôler le profil réseau public/privé ; ne pas désactiver globalement le pare-feu.

Windows 10 : tenter les mêmes contrôles et noter la version/pilote exacts.
Aucun pilote personnalisé n'est distribué ; l'échec NCM ne bloque pas la release
Windows 11 et laisse le Wi-Fi disponible.

## TODO obligatoire macOS ARM64 avant sa release

Le code commun est implémenté ; le déploiement cible existant macOS 12 est conservé.

1. Construire avec le job macOS CI et vérifier les bibliothèques ARM64, les licences
   et le contrôle sans ADB. Vérifier que NSLocalNetworkUsageDescription et
   NSBonjourServices (`_hibop-quest._tcp`) existent dans le bundle final **avant**
   sa signature et son archivage.
2. Sur Mac physique, relever `sw_vers`, `networksetup -listallhardwareports` et
   `ifconfig`. Attendu : interface USB NCM active avec adresse `fe80::...`.
3. Vérifier la découverte native avec `dns-sd -B _hibop-quest._tcp local.` puis
   `dns-sd -L <instance> _hibop-quest._tcp local.` ; comparer à HiBoP.
4. Tester sur la version disponible, de préférence macOS 15+ pour exercer la
   confidentialité réseau local. Autoriser l'accès et vérifier la connexion.
   Relever l'OS réellement testé, sans attribuer une validation matérielle au
   minimum macOS 12 s'il n'a pas été exercé. Noter l'identité de signature ; si
   celle de la distribution change ensuite, revérifier lancement et accès réseau.
5. Vérifier les parcours essentiels sur le build réel : connexion USB/Wi-Fi,
   découverte, repli et restauration d'une scène, avec les permissions et scopes
   macOS. Réutiliser les tests communs déjà acquis ; approfondir en cas d'échec.
   Consigner le résultat et les limites avant de lever le verrou macOS.

Ni entitlement multicast iOS ni installation Bonjour externe ne sont requis pour
cette application Desktop ; ne pas transposer les règles iOS au bundle macOS.

## TODO obligatoire Linux x86-64 avant sa release

Référence CI : Ubuntu 24.04 x86-64. Aucune dépendance Avahi ni accès direct à un
périphérique USB par l'application.

1. Construire le Player IL2CPP Linux ; vérifier les dépendances natives avec
   `ldd`, les licences et le contrôle sans ADB.
2. Sur machine physique : `uname -a`, `lsusb -t`, `lsmod | grep cdc_ncm`,
   `ip link`, `ip -6 addr`, `ip -6 route`, et `nmcli device status` si disponible.
   Attendu : pilote noyau cdc_ncm et interface active avec IPv6 link-local.
3. Si nécessaire, charger le module système et corriger le profil réseau de cette
   interface ; vérifier que IPv6 n'y est pas désactivé et qu'aucun DHCP/passerelle
   Internet n'est nécessaire. Ne pas modifier globalement NetworkManager ou IPv6.
4. Contrôler le pare-feu pour UDP 5353 et TCP 45871 sur l'interface concernée.
   Vérifier l'environnement réel du poste sans modifier systématiquement l'état
   d'Avahi. HiBoP doit fonctionner sans privilèges root et sans imposer ce service
   comme dépendance ; approfondir sa coexistence seulement en cas de défaut.
5. Vérifier les parcours essentiels sur le build réel : connexion USB/Wi-Fi,
   découverte, repli et restauration d'une scène, notamment les scopes locaux et
   le renouvellement des interfaces Linux. Réutiliser les tests communs déjà
   acquis ; approfondir en cas d'échec. Consigner résultat et limites avant de
   lever le verrou Linux.

L'installation et le canal de distribution de l'APK restent hors périmètre.
