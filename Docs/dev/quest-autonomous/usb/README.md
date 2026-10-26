# Connexion Quest / Desktop en USB sans ADB

Date : 2026-10-26. Statut : synthèse exploratoire de la discussion ; aucune
implémentation ni validation matérielle de cette solution n'a été réalisée.

## Objectif et périmètre

L'objectif principal est de **supprimer ADB des builds distribués de HiBoP** et
de disposer, idéalement, d'une connexion USB sur Windows, macOS et Linux.
Éviter le mode développeur pour la communication est souhaitable, mais son
activation reste acceptable si une autre contrainte l'impose.

Le parcours utilisateur actuel est à conserver :

- code lors du premier appairage ;
- reconnexion sans code pour une association mémorisée ;
- connexion automatique uniquement si l'option correspondante est activée.

L'installation de l'APK et son futur canal de distribution sont **hors scope**.
La suppression d'ADB des builds n'interdit pas son utilisation comme outil de
développement. Deux transports différents pour USB et Wi-Fi sont acceptables
si leur utilisation reste transparente.

## Fonctionnement actuel observé dans le code

- [QuestUsbDiscovery.cs](../../../../Assets/Scripts/HBP/UI/Quest/QuestUsbDiscovery.cs)
  utilise ADB pour détecter les casques et rediriger leur port TCP vers un port
  local du Desktop.
- `Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs` copie actuellement `adb.exe` et
  ses DLL dans le build Windows.
- Les échanges applicatifs utilisent ensuite le protocole HiBoP, TLS et
  l'appairage : ADB fournit le chemin USB, pas le protocole métier.
- La découverte dans `QuestDiscovery.cs`, la validation des adresses dans
  `QuestPairing.cs` et l'écoute dans `QuestConnectionPanel.cs` reposent
  actuellement sur IPv4.

## Piste privilégiée : USB NCM

Meta documente une liaison réseau directe sur USB utilisant **NCM**, une classe
USB standard. Côté Quest, l'application demande ce réseau via
`ConnectivityManager` et `TRANSPORT_USB`. La liaison est locale en IPv6, sans
routage Internet ; les sockets doivent utiliser le réseau obtenu et le pair
peut être découvert par mDNS. Un consentement système est demandé lorsque
l'application est au premier plan.

Cette voie permet d'envisager la suppression du tunnel ADB. Elle doit encore
être validée dans notre application Unity et sur les versions d'Horizon OS et
modèles de Quest ciblés. Source : [documentation Meta, section Networking](https://developers.meta.com/vr/documentation/android-apps/features-overview/).

| Plateforme Desktop | Éléments documentés | Qualification restante |
| --- | --- | --- |
| Windows 11 | Pilote NCM Microsoft `UsbNcm.sys` intégré. | Connexion réelle au Quest depuis un build HiBoP. |
| Windows 10 | Support exact laissé ouvert dans la discussion. | Vérifier les versions et l'association du pilote avant toute promesse. |
| macOS récent | Support natif NCM, notamment documenté par l'implémentation TinyUSB. | Versions minimales, Apple Silicon/Intel selon les cibles retenues, et interopérabilité Quest. |
| Linux | Pilote noyau `cdc_ncm`. | Disponibilité dans les distributions ciblées, activation de l'interface et IPv6. |

Sources : [Microsoft](https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/supported-usb-classes),
[TinyUSB](https://github.com/hathach/tinyusb/blob/master/examples/device/net_lwip_webserver/src/usb_descriptors.c#L192),
[noyau Linux](https://github.com/torvalds/linux/blob/master/drivers/net/usb/Kconfig#L236).
Le support du standard par ces OS ne constitue pas une certification de la
combinaison Quest / HiBoP sur chacun d'eux.

## Architecture envisagée

Conserver le protocole, TLS, l'identité du casque et les associations existantes.
Adapter principalement la découverte et l'ouverture des connexions :

1. Petit composant Android appelé depuis Unity pour demander et gérer le réseau USB.
2. Support IPv6, y compris l'interface associée aux adresses locales, côté Desktop et Quest.
3. Découverte sur l'interface USB, en complément du mécanisme Wi-Fi actuel.
4. Conservation des règles actuelles d'appairage, de reconnexion et d'automatisation.

Un même Quest doit conserver son identité quel que soit son chemin réseau.
Il n'est donc probablement pas nécessaire d'entretenir deux protocoles métier.
La découverte devrait éviter d'imposer aux utilisateurs un service supplémentaire
à installer, tel qu'Avahi ou Bonjour ; son implémentation reste à choisir.

Préférer l'USB disponible et reprendre par Wi-Fi lors d'un débranchement sont
des possibilités discutées, pas des comportements déjà validés. Une connexion
TCP ne migre pas automatiquement : la reprise doit rétablir la connexion et
la synchronisation sans perturber la session.

Les autorisations réseau et pare-feu restent à qualifier. macOS peut notamment
demander l'accès au réseau local : [note technique Apple TN3179](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

## Autres pistes évoquées

- **Android Open Accessory** : communication sans débogage USB possible sur les
  appareils compatibles, mais support Quest non établi dans cette discussion.
  Moins prioritaire que NCM. [Documentation Android](https://source.android.com/docs/core/interaction/accessories/protocol).
- **USB/MTP** : pertinent pour copier des fichiers, peu adapté à la synchronisation interactive.
- **Quest Link / PCVR** : changerait le modèle d'exécution vers un rendu PC ;
  ce n'est pas la direction retenue pour le Quest autonome.

## Validation proposée avant migration

Un prototype commun Quest, testé avec un build Desktop sur chaque OS cible :

- détection et échanges bidirectionnels sans ADB installé ou lancé côté Desktop ;
- test avec Wi-Fi coupé, puis sans débogage USB et, si possible, sans mode développeur ;
- premier appairage par code, reconnexion sans code, respect de l'option automatique ;
- transfert représentatif, débranchement/rebranchement, veille et réveil ;
- reconnaissance d'un même casque sur plusieurs chemins et de plusieurs casques ;
- vérification des consentements, pilotes, interfaces et règles réseau nécessaires.

La recommandation issue de la discussion est **NCM pour l'USB et réseau local
pour le Wi-Fi, avec un protocole HiBoP commun**, sous réserve de cette qualification.
