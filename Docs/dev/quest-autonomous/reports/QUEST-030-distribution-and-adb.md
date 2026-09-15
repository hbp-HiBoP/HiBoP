# QUEST-030 — Distribution finale, ADB et plateformes

Date initiale : 2026-09-14. Mise à jour : 2026-09-15.
État : décision D34 consignée ; adaptations non implémentées.
Références : D33/D34 dans les [décisions](../01-decisions-and-open-questions.md).

## Décision actuelle du propriétaire — D34

Le réseau local est le parcours principal, sans ADB ni mode développeur.
L'USB est une option avancée nécessitant ADB installé séparément sur le Desktop,
le mode développeur du Quest et l'autorisation du débogage USB. Cette exigence
USB s'applique aussi à Mac/Linux ; le Wi-Fi applicatif ne dépend pas d'ADB.

Le propriétaire demande de **conserver le build Windows actuel et son ADB
embarqué pour l'instant**. La transition vers ADB externe, les adaptations de
packaging/CI et le tutoriel d'installation des Platform-Tools et de configuration
du casque sont suivis dans [QUEST-031](../tasks/QUEST-031.md), non exécutée.
Le choix ne prouve pas le support Mac/Linux et n'autorise aucune publication.

## Exigence initiale — D33, amendée par D34

Le parcours final doit se limiter à installer HiBoP Desktop, installer HiBoP
Quest, puis utiliser le parcours d'appairage dans les deux applications.
L'utilisateur ne doit ni utiliser ADB, ni activer le mode développeur du casque,
ni autoriser le débogage USB. Embarquer ADB pour masquer les commandes ne suffit
donc pas à satisfaire cette exigence.

Le propriétaire poursuit les tests manuels avec les builds actuels. Cela
n'approuve pas ADB comme dépendance du produit final. À sa demande, ce point
est documenté sans modification de code ni de CI à ce stade.

## État réel de l'implémentation locale

| Cible | État actuel |
| --- | --- |
| Windows | Le builder embarque `adb.exe`, `AdbWinApi.dll`, `AdbWinUsbApi.dll` et `NOTICE.txt` dans `HiBoP_Data/StreamingAssets/QuestUsb`. Cette copie concerne aussi les builds de release. La découverte USB utilise ADB et exige le débogage autorisé sur le Quest. |
| macOS | Aucun ADB embarqué ; transport USB non implémenté. L'option USB exige un ADB externe et le portage du lanceur Windows ; l'installation des outils seule ne suffit pas. Le transport réseau est prévu, sans qualification physique sur Mac. |
| Linux | Aucun ADB embarqué ; transport USB non implémenté. L'option USB exige un ADB externe, les permissions USB adaptées et le portage du lanceur Windows. Le transport réseau est prévu, sans qualification physique sur Linux. |
| Quest | L'APK n'embarque pas ADB. Son installation locale actuelle utilise ADB depuis le PC. |

Le transport Wi-Fi de HiBoP utilise directement le réseau local et ne repose
pas sur ADB Wi-Fi. Il reste à qualifier sur les plateformes et réseaux visés.
Un PC en Ethernet peut rejoindre le même réseau local que le casque en Wi-Fi.

## GitHub Actions

Le [workflow](../../../../.github/workflows/build.yml) appelle le même
[HBPBuilder](../../../../Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs) que les
builds locaux. Une fois ces modifications locales intégrées, le build Windows
cherchera les quatre fichiers sous `ANDROID_HOME/platform-tools`, ou à défaut
dans `C:\Android\Sdk\platform-tools`.

Aucune étape dédiée n'a été ajoutée au job Desktop pour préparer ou fixer la
version des platform-tools. Leur disponibilité dépend donc de l'environnement
du runner. L'absence d'un fichier attendu fait échouer le packaging Windows,
même si la compilation Unity a réussi. Aucune exécution CI n'a validé ces
modifications. Les tests locaux ne valent pas qualification CI.

La copie conditionnée à Windows n'est pas exécutée pour macOS/Linux. Le job
Quest dispose de sa propre installation de la chaîne Android, distincte du job
Desktop Windows ; cela ne prépare pas la dépendance de ce dernier.

## Points à résoudre avant distribution finale

- Mettre en place l'option USB avec ADB externe et son tutoriel selon D34 dans
  QUEST-031. AOA n'est pas retenu par cette décision ; aucune compatibilité
  USB sans mode développeur n'est promise.
- Qualifier le parcours réseau sans ADB : découverte, appairage mémorisé,
  transfert et reconnexion, notamment sous macOS et sur les réseaux cibles.
- Choisir une distribution Quest permettant l'installation sans mode
  développeur, par exemple le Meta Horizon Store. Fournir simplement l'APK
  installé actuellement par ADB ne satisfait pas le parcours demandé.
- Adapter ultérieurement le packaging et la CI dans QUEST-031 pour retirer
  l'ADB embarqué du Desktop Windows ; ne pas modifier le build actuel pendant
  cette consignation. Garder distinct le SDK de compilation de l'APK Quest.

Le réseau principal et l'USB optionnel sont décidés ; le canal de distribution
Quest reste à choisir. Les essais USB actuels ne qualifient pas la procédure
finale d'installation et de distribution suivie dans QUEST-031.

## Sources examinées

- [Meta — configuration d'un appareil pour le développement](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/) : mode développeur et autorisation du débogage pour ADB.
- [Android — accès réseau des applications](https://developer.android.com/develop/connectivity/network-ops/connecting) : communication réseau via les permissions d'application.
- [Meta — canaux de distribution](https://developers.meta.com/horizon/resources/publish-release-channels/) : installation et diffusion via les canaux Meta.

Voir aussi le [rapport d'implémentation et de vérification](QUEST-030.md).
