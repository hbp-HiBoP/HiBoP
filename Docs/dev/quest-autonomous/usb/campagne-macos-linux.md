# Passation : validation NCM/mDNS sur macOS et Linux

Établi le 27 octobre 2026. Ce document suffit pour reprendre les essais avec un
agent qui n'a pas l'historique de la discussion. **Ne pas refaire la campagne
Windows : valider les builds réels et les particularités réseau de chaque OS.**

## 1. Mission et limites

Faire une passe courte sur **un Mac ARM64** et **un Linux x86-64**, avec le Quest 3
de l'utilisateur. Prévoir une séance par OS ; construction et préparation des
données se font casque posé. Quest 3S, Mac Intel et les autres distributions /
versions d'OS ne sont pas requis pour cette première validation. Consigner le
système effectivement testé, sans lui attribuer une couverture plus large.

Guider l'utilisateur par petites étapes, attendre ses réponses et observer les
logs. Prévenir lorsqu'il doit porter le casque et dès qu'il peut l'enlever.
L'utilisateur préfère effectuer lui-même les opérations dans l'interface.
Réutiliser un build adapté s'il existe ; sinon construire une fois. Regrouper les
défauts, corriger, puis rejouer seulement les scénarios affectés. Ne pas multiplier
les builds ou les essais préventifs sans échec concret.

Le refus du consentement USB NCM n'est **pas bloquant** s'il ne se provoque pas
facilement. Ne pas effacer les données, réinstaller le casque ou renouveler les
associations pour forcer cette fenêtre. La désactivation du débogage / mode
développeur a déjà été vérifiée sur Windows : ne pas la répéter systématiquement.
Installation et distribution de l'APK restent hors périmètre.

## 2. État acquis et précautions de reprise

Windows 11 + Quest 3 ont été validés sur les Players réels : USB seul, Manual IP
IPv4/IPv6, même casque sur deux chemins, association conservée, préférence USB,
maintien du Wi-Fi au retour du câble, repli avec connexion automatique désactivée,
veille, annulation/déconnexion, transfert interrompu, restauration après relance
Quest et synchronisation dans les deux sens. Repli réseau observé en environ
4,2 à 7,1 s ; la restauration complète d'une scène peut prendre davantage.
Le dernier essai USB a réussi avec Wi-Fi coupé et mode développeur OFF, puis les
réglages ont été rétablis. Refus/retry du consentement NCM : non exercé, accepté
comme limite non bloquante par l'utilisateur.

53 tests ciblés ont réussi (41 PlayMode, 12 EditMode) ; les 3 variantes de reprise
ont ensuite été rejouées après la dernière correction. Ne pas relancer toute cette
suite sans changement qui le justifie. Détails :
[bilan Windows](windows-quest3-checklist-2026-10-27.md).

Contexte technique utile :

- Desktop 6.2.0, Unity **6000.5.2f1** au moment de cette passation ; relire
  `ProjectSettings/ProjectVersion.txt` avant toute construction.
- APK installé : `fr.crnl.hibop.quest`. Réutiliser cet APK pour les deux OS sauf
  correction Android réellement nécessaire. Référence locale Windows, non
  garantie transférée : `.artifacts/quest-ncm/Android/HiBoP.Quest.apk`.
- Quest testé : Android 14, incrémental Horizon `52433670048800520`. La version
  commerciale lue comme « 2.7 » n'a pas été confirmée. Relever un changement de
  firmware s'il y en a un, sans bloquer sur un nom commercial introuvable.
- TCP applicatif **45871**, mDNS **UDP 5353**, service
  `_hibop-quest._tcp.local.`. Les ports propriétaires 45872/45873 et ADB produit
  ont été supprimés. ADB peut servir au diagnostic, jamais au transport HiBoP.
- Les adresses, noms et indices d'interfaces Windows ne sont pas transférables.
  Relever ceux du Mac/Linux ; ne jamais réutiliser un scope annoncé par le Quest.
- Fermer le Player Windows et les autres Desktop avant de changer de poste.
  Un nouveau poste établit normalement sa propre association de référence avec
  un code, une fois. Le code ne doit plus revenir pendant la campagne sur ce poste.
  L'authentification actuelle renouvelle un secret côté Quest : passer à un autre
  Desktop peut donc rendre l'ancien secret Windows inutilisable. Prévenir
  l'utilisateur ; ce renouvellement volontaire n'est pas une panne NCM.
- Ne pas copier les fichiers `.pair` Windows : ils sont protégés par DPAPI pour
  l'utilisateur Windows. Ne jamais joindre codes ou secrets aux preuves.
- Si le nouvel appairage reste verrouillé, fermer l'ancien Desktop et relancer
  HiBoP dans le Quest avant d'approfondir. Ne pas enchaîner Forget association
  comme dépannage ; un éventuel changement d'association doit être explicite.
- « Paired » dans le Quest indique une association, pas nécessairement une session
  active. Le mode « Automatic » de la fenêtre Quest est la découverte ; la
  **connexion automatique** est une préférence distincte dans User Preferences.

**Sources à transférer :** la migration comportait des changements locaux et des
fichiers non suivis lors de cette passation. Inspecter `git status` et les fichiers
présents ; ne pas supposer que HEAD, une branche distante ou un ancien artefact CI
contient ces corrections. Les dossiers `.artifacts` sont locaux. Faire parvenir
le même état source au poste cible avant de construire. Ne pas commit/push sans
demande de l'utilisateur. Respecter les AGENTS.md du poste utilisé.

## 3. Préparer un seul build et une scène

Préférer le workflow `.github/workflows/build.yml`, **Build HiBoP**, déclenché
manuellement avec `platform=macos` ou `platform=linux`, une fois les sources
disponibles dans la révision construite. Il fournit déjà :

| Cible | Artefact | Vérifications existantes |
| --- | --- | --- |
| macOS ARM64 | `HiBoP-macOS-arm64` | tests réseau EditMode du host, IL2CPP, bundles ARM64, signature ad hoc, absence d'ADB, ZIP |
| Linux x86-64 | `HiBoP-Linux` | tests réseau EditMode du host, IL2CPP, ELF/dépendances natives, absence d'ADB, ZIP |

Les résultats ciblés sont dans l'artefact `Quest-network-tests-macos` ou
`Quest-network-tests-linux`. Inspecter rapports et logs, pas seulement le statut
du job. Le nom de cible Unity `OSXUniversal` dans la CI ne signifie pas que Mac
Intel est dans le périmètre : le produit prévu ici est ARM64.

Si construction locale nécessaire, utiliser `HBPBuilder` et les conventions
existantes, pas un nouveau pipeline. Linux requiert `com.unity.sdk.linux-x86_64` ;
la construction Linux depuis Windows utilise aussi le toolchain déjà référencé
par `HBPBuilder`. Ne pas installer de gestionnaire NuGet dans Unity. Éditeur
ouvert : MCP ; sinon suivre la skill `hibop-unity-validation` disponible sur le
poste, ou les procédures CLI du dépôt. Ne pas lancer deux éditeurs sur le projet.

Préparer `visu_full_test.hibop` avec ses données et la visualisation **Small**
(3 colonnes dans les essais Windows), ou une scène équivalente si ce projet n'est
pas disponible sur le poste. Ne pas sauvegarder le projet original. Une copie
complète des données doit être accessible : le seul fichier `.hibop` peut ne pas
suffire. Un échec de chemin de données n'est pas une panne USB.

Tester le Player IL2CPP de release, **pas uniquement l'éditeur**. Ne pas rebâtir
l'APK pour ce changement de Desktop. Les tests host ne qualifient pas à eux seuls
les appels natifs `recvmsg` / les scopes dans le Player final.

## 4. Campagne courte commune aux deux plateformes

Avant C1 : casque porté, HiBoP au premier plan, câble de données direct et poste
sur le même LAN que le Quest. Désactiver la préférence Desktop de connexion
automatique pendant les essais, sans désactiver la synchronisation des scènes.
Sur le nouveau poste, un code initial est normal ; conserver ensuite l'association.

| ID | Opérations à guider | Résultat attendu |
| --- | --- | --- |
| C1 — USB réel | Wi-Fi Quest coupé. Connecter le casque découvert, établir l'association initiale si nécessaire, ouvrir/envoyer Small. Modifier un réglage dans chaque sens. | Une entrée casque ; scène visible et synchronisée ; TCP 45871 sur l'interface USB. HiBoP s'exécute sans root et sans tunnel ADB. |
| C2 — Scope IPv6 local | Toujours en USB seul : Disconnect, Manual, adresse link-local **actuelle** du Quest sans zone ni port ; choisir l'interface USB du poste. Connect. | Le sélecteur présente les interfaces locales et la connexion réussit sans code. Vérifier l'adresse complète dans le champ/log si timeout. |
| C3 — Deux chemins | Repasser en découverte Automatic. Réactiver le Wi-Fi Quest, retirer USB et laisser reprendre la session. Rebrancher le câble sans commande de connexion. Enfin Disconnect puis Connect. | Reprise LAN sans code ; le retour du câble garde le Wi-Fi actif ; la connexion explicite suivante préfère USB. Une entrée par casque. L'interface renouvelée est prise en compte. |
| C4 — Une coupure pendant transfert | Fermer la scène partagée côté Desktop, Disconnect, rouvrir Small sans connecter. Avec USB et Wi-Fi disponibles, Connect puis Send si nécessaire. Débrancher **pendant Sending, avant la fin**. Attendre jusqu'à 30 s, sans Retry/Send/Reconnect. | La session reprend sur le LAN, puis la scène revient automatiquement sans code ni fenêtre de bug report. Viser moins de 15 s pour la reprise de session, hors restauration de scène. Vérifier un réglage dans les deux sens. |
| C5 — Relance Quest | Après C4 réussi, quitter complètement HiBoP dans le casque, puis le relancer. Garder le casque porté, sans commande Windows/Mac/Linux pendant 30 s. | Small revient automatiquement sans code, puis un réglage synchronisé fonctionne. Ce test vérifie la restauration sur le runtime cible. |
| C6 — Persistance Desktop et arrêt explicite | Fermer proprement la scène partagée côté Desktop, puis le Player sans sauvegarder ; le relancer, ouvrir Edit → Quest et Connect. Après connexion, Disconnect et attendre 10 s. | Connexion sans code sur ce poste ; puis déconnexion respectée. Avec connexion automatique désactivée, aucune connexion spontanée au démarrage ou après Disconnect. |

Le casque peut rester posé pendant les préparations et le chargement local ; le
remettre avant Connect/Send et les observations. Ne pas conclure à un échec réseau
si HiBoP est endormi/en arrière-plan. Si C4 est interrompu trop tard, noter l'essai
non concluant et le refaire une fois, pas le compter comme reprise pendant transfert.

Il n'est pas demandé de rejouer plusieurs casques, tous les refus, tous les ordres
de lancement, une matrice exhaustive de veille, tous les réglages de sécurité ou
toutes les variantes IPv4/IPv6. Ajouter un essai uniquement pour un défaut observé
ou une particularité du poste. Ne pas prolonger la grâce de synchronisation de
500 ms pour masquer un défaut de restauration.

## 5. Particularités macOS : lancement, permissions et signature

Commencer sur le Mac ARM64 disponible. macOS 15+ est utile pour exercer la
permission de réseau local ; si un autre OS est utilisé, noter cette limite.
Le minimum déclaré dans le projet est macOS 12 : ne pas prétendre l'avoir testé
si seul un Mac plus récent était disponible.

La confidentialité réseau local existe à partir de macOS 15. Lancer l'application
depuis **Finder**, comme l'utilisateur ; l'exécution directe depuis Terminal peut
ne pas exercer la même permission. Autoriser le réseau local ; si l'accès avait
été refusé, le réactiver dans Réglages Système → Confidentialité et sécurité →
Réseau local, puis relancer l'essai. Ne pas réinitialiser tout le système de
permissions pour provoquer une fenêtre. [Référence Apple TN3179](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

Vérifier dans l'application finale `NSLocalNetworkUsageDescription` et
`NSBonjourServices` contenant `_hibop-quest._tcp`, puis la signature. La CI signe
actuellement **ad hoc** : ne pas appeler cela une signature Developer ID ou une
notarisation. Apple recommande une identité de signature stable pour le suivi des
autorisations. Si l'artefact est ensuite signé autrement pour distribution,
revérifier seulement lancement et accès réseau sur cet artefact, pas toute la
campagne. [Référence Apple](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

Diagnostics, en remplaçant les chemins / noms par ceux observés :

```sh
sw_vers
uname -m
networksetup -listallhardwareports
ifconfig
plutil -p '/chemin/HiBoP.app/Contents/Info.plist'
codesign --verify --deep --strict --verbose=2 '/chemin/HiBoP.app'
codesign -dv --verbose=4 '/chemin/HiBoP.app'
```

Attendu : ARM64, bundles `hbp_core`, `hbp_math`, `EEGFormat` valides, interface NCM
active avec IPv6 link-local. La CI vérifie les architectures natives : ne pas
refaire ce travail manuellement si ses rapports sont disponibles et le bundle
n'a pas changé. Aucun installateur Bonjour supplémentaire n'est requis.

Si découverte en panne, comparer ponctuellement avec :

```sh
dns-sd -B _hibop-quest._tcp local.
dns-sd -L 'INSTANCE_OBSERVEE' _hibop-quest._tcp local.
lsof -nP -iUDP:5353
lsof -nP -a -p PID_HIBOP -iTCP:45871
```

Arrêter `dns-sd` avec Ctrl+C avant le retest de réussite : ses requêtes ne doivent
pas masquer une découverte qui ne fonctionne qu'avec cet outil actif.

## 6. Particularités Linux : pilote, IPv6 et profil réseau

Cible : Linux x86-64, Ubuntu 24.04 comme référence CI. Employer une machine avec
une session graphique adaptée au Player. Ni Avahi, ni des règles udev donnant
accès USB à HiBoP, ni l'exécution du Player avec sudo ne sont des prérequis.

Diagnostics de départ :

```sh
uname -a
cat /etc/os-release
lsusb -t
lsmod | grep cdc_ncm
ip -br link
ip -6 addr
ip -6 route
nmcli device status
ss -ulpn
ss -tnp | grep ':45871'
```

`nmcli` est à utiliser si NetworkManager est installé. Identifier l'interface USB
actuelle, souvent `enx...` ou `usb...`, sans supposer son nom. Attendu : pilote
`cdc_ncm` et adresse `fe80::...` utilisable. Un pilote compilé dans le noyau peut
être absent de `lsmod` : ce n'est pas une preuve de panne. Voir le
[Kconfig Linux](https://github.com/torvalds/linux/blob/master/drivers/net/usb/Kconfig).

Si l'interface n'apparaît pas, inspecter d'abord `lsusb -t` et le journal noyau.
Si le module système est disponible mais non chargé, `sudo modprobe cdc_ncm`
peut être un correctif local à consigner. Ne pas développer de pilote personnalisé.

Si le lien est présent mais attend indéfiniment DHCP / une route Internet, vérifier
le profil **de cette interface** : IPv6 actif, link-local possible, pas de route
par défaut nécessaire. NetworkManager propose `ipv6.method=link-local` et
`ipv6.never-default`. Sauvegarder le profil et demander l'accord pour une
modification persistante du poste ; consigner le changement / retour arrière.
Ne pas modifier globalement IPv6 ou NetworkManager.
[Référence NetworkManager](https://www.networkmanager.dev/docs/api/latest/nm-settings-nmcli.html).

Contrôler les règles qui affectent mDNS UDP 5353 et le TCP sortant vers le Quest
sur 45871. Ne pas ouvrir inutilement un serveur TCP 45871 sur le Desktop ni
désactiver globalement le pare-feu. Laisser le service Avahi tel qu'il est sur le
poste : une passe avec l'environnement réel suffit, pas une matrice avec/sans
daemon. Si `avahi-browse` est déjà disponible, il peut aider au diagnostic ;
l'arrêter avant de valider HiBoP seul. Ne pas installer Avahi pour faire marcher
le produit. Vérifier `ldd` des bibliothèques seulement si la CI ne l'a pas fait,
si le bundle a changé ou en cas de chargement natif en échec.

## 7. Si un scénario échoue : diagnostic court et corrections groupées

| Symptôme | Première vérification / fichiers utiles |
| --- | --- |
| Aucun lien USB | Câble de données, casque réveillé/porté, HiBoP au premier plan, interface du système et pilote. Ne pas réintroduire ADB dans le produit. |
| Lien présent, aucune annonce | Permission macOS / pare-feu Linux ; paquets UDP 5353 sur la bonne interface ; comparer avec l'outil système seulement pour diagnostiquer. |
| Outil système découvre, HiBoP non | `Tools/QuestMdns/MdnsNativeReceiver.cs` : chemins Darwin/Linux `recvmsg`, contrôle des interfaces et structures natives distinctes. `MulticastClient.cs` : groupes multicast et renouvellement. Inspecter DllNotFoundException/EntryPointNotFoundException dans le Player IL2CPP. |
| Adresse découverte, TCP impossible | Port fixe, adresse actuelle complète, scope de l'interface du **poste** et génération. Pas de `%8` ou `%54` copiés du bilan Windows. |
| Reconnexion bloquée sur ancienne USB | `QuestManager.Discovery.cs`, `QuestManager.cs` : un heartbeat ne doit pas concurrencer la découverte quand la session est déconnectée ; une interface disparue est une erreur de transport. |
| Retry imposé après coupure de transfert | `QuestManager.SceneLifecycle.cs`, `QuestManager.Display.cs`, `SceneDisplayOperation.cs` : préserver TransportRecoveryPending à travers le nettoyage Display, sans contourner une annulation utilisateur. |
| Scène absente après relance Quest | Retirer l'ancienne publication par CloseScene exact et acquittement avant nouvelle capture. Ne pas simplement remettre OfferWasSent à false après succès : le nettoyage pourrait disposer un propriétaire encore actif. |

Les DLL mDNS sont adaptées et épinglées. **Ne pas remplacer la DLL par le NuGet
amont** ni modifier un binaire manuellement. Lire `Tools/QuestMdns/README.md` ;
`Tools/Prepare-QuestMdns.py` restaure/reconstruit avec SDK .NET 10.0.201 et contrôle
les empreintes dans `ManagedPlugins.lock.json`. Si le transport commun doit changer,
mettre à jour la source reproductible et les empreintes de manière cohérente,
tester le défaut sur l'OS cible et faire une vérification Windows proportionnée
à cette modification. Pas de campagne Windows complète par défaut.

En cas de changement C#, utiliser les suites existantes, des tests asynchrones
non bloquants, contrôler la console et les dépendances d'assemblies, puis exécuter
le formateur une seule fois en dernière action selon AGENTS.md. Aucun besoin de
refaire ces contrôles manuellement sur une passe sans changement de code déjà
couverte par la CI.

## 8. Bilan minimal et levée du verrou de la plateforme

Un seul compte rendu Markdown par OS, dans ce dossier, suffit. Y consigner :

- date, version/architecture OS, Quest/firmware, état source et provenance du
  build (inclure les changements locaux si HEAD ne les représente pas), signature
  macOS et résultats CI disponibles ;
- C1 à C6 : réussi / échec / non concluant, chemin réseau réellement observé,
  temps de reprise approximatif séparé du temps de restauration de scène ;
- corrections de code ou réglages locaux nécessaires et limites acceptées ;
- un log Desktop et les informations d'interface / rapports utiles. Logs Quest
  seulement s'ils sont faciles à récupérer ; ne pas retarder le bilan pour ADB.

Logs Player attendus ici : macOS `~/Library/Logs/CRNL/HiBoP/Player.log` ; Linux
`~/.config/unity3d/CRNL/HiBoP/Player.log`. Vérifier les noms du produit si le build
diffère. Copier le log avant la relance suivante.
[Référence Unity](https://docs.unity.com/en-us/engine/6000.5/manual/scripting/debugging-and-diagnostics/log-files).
Une requête TCP courte peut échapper à `lsof`/`ss` : privilégier session ouverte,
logs d'ouverture/publication et un réglage visible synchronisé ; « Paired » seul
ne suffit pas. Ne conserver aucun secret / code d'appairage dans les preuves.

Si les parcours essentiels passent dans le Player réel et qu'aucun défaut
bloquant propre à l'OS ne subsiste, mettre à jour **seulement** `macos` ou `linux`
dans `qualification.json` avec `qualified: true` et le chemin relatif du bilan.
Laisser les autres entrées telles quelles. Windows est déjà validé ; aucun
changement du verrou Windows n'est nécessaire pour commencer Mac/Linux. Lever un
verrou ne publie rien : commit, push et publication demandent une instruction.

Modèle de bilan :

```markdown
# Validation NCM/mDNS — <macOS ARM64 ou Linux x86-64> — <date>
Build / sources : ...
Machine / OS / Quest : ...
Signature / CI : ...
| Cas | Résultat | Observation utile |
| --- | --- | --- |
| C1 USB + scène | ... | ... |
| C2 IPv6 local | ... | ... |
| C3 deux chemins | ... | ... |
| C4 transfert interrompu | ... | ... |
| C5 relance Quest | ... | ... |
| C6 relance Desktop / Disconnect | ... | ... |
Corrections et réglages locaux : ...
Preuves : ...
Limites non bloquantes : ...
Conclusion sur cette configuration : ...
```

Prompt de reprise à donner au prochain agent :

> Lis AGENTS.md puis Docs/dev/quest-autonomous/usb/campagne-macos-linux.md.
> Valide le build réel pour la plateforme disponible avec mon Quest 3. Vérifie
> d'abord que les sources incluent la migration et ses corrections, puis prépare
> un seul build si nécessaire. Guide-moi étape par étape et préviens-moi quand
> porter/enlever le casque. Exécute la courte campagne C1–C6 et les vérifications
> propres à cet OS. Ne refais pas toute la campagne Windows ; conserve les limites
> explicites, regroupe les corrections et ne rejoue que les scénarios affectés.
> Produis le bilan et mets à jour le verrou de cette plateforme en cas de succès.
> Ne commit, push ou publie rien sans ma demande.
