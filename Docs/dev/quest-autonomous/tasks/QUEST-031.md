# QUEST-031 — Finaliser la distribution et le parcours USB optionnel avec ADB

Position : préparation de la distribution finale, hors jalons scientifiques.
Type : intégration de plateforme, packaging et documentation utilisateur.
Dépendance : [QUEST-030](QUEST-030.md). Réutiliser les preuves de
[QUEST-025/026](QUEST-026.md) pour Mac et de [QUEST-028/029](QUEST-029.md) si Linux
est retenu ; une plateforme non testée reste explicitement non qualifiée.
Décision : D34 dans les [décisions](../01-decisions-and-open-questions.md).
Statut et preuves : [registre](../TASK-STATUS.md#quest-031).

**Créée le 2026-09-15, non exécutée.** Le propriétaire demande de conserver
pour l'instant le build Windows et son ADB embarqué. La présente fiche prépare
leur évolution ultérieure ; elle n'autorise aucune modification immédiate du
code, du packaging, de la CI ou des réglages du casque.

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), D33/D34, la
[note distribution et ADB](../reports/QUEST-030-distribution-and-adb.md), le
[rapport QUEST-030](../reports/QUEST-030.md) et les rapports des plateformes
disponibles. Une demande future « implémente QUEST-031 » autorisera le périmètre
de cette fiche, y compris la transition Windows différée aujourd'hui.

## Résultat utilisateur attendu

- Parcours principal : installer HiBoP Desktop et HiBoP Quest, puis découvrir,
  appairer et transférer sur le réseau local sans ADB ni mode développeur.
- Option USB : suivre un tutoriel pour installer ADB séparément, configurer le
  mode développeur et autoriser le débogage USB du Quest, puis utiliser les
  boutons HiBoP sans saisir de commandes pour chaque connexion.
- L'absence d'ADB ne bloque ni le lancement du Desktop ni le parcours réseau.

## Points d'entrée à inspecter

- `Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs` et `.github/workflows/build.yml`.
- `Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs`, `QuestUsbDiscovery.cs`
  et `QuestAdbProcess.cs` dans le même dossier.
- `Assets/Prefabs/General/Quest Connection.prefab` et son outil d'édition.
- `Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs` ; documentation
  d'installation Desktop/Quest existante, à localiser avant de la compléter.

## À implémenter

### ADB externe et packaging

- Appliquer la cible D34 : ADB installé séparément par les utilisateurs de
  l'option USB. Fournir des liens officiels ou des paquets de distribution
  identifiés ; ne pas imposer Android Studio ni réhéberger une archive sans
  justification. Un téléchargement automatique n'est pas présupposé.
- Définir les versions/architectures prises en charge et leur politique de
  mise à jour. Détecter une installation existante et permettre de sélectionner
  explicitement le binaire ; mémoriser le choix et afficher sa version.
- Qualifier le lancement, les erreurs, délais et annulations dans les vrais
  Players Windows/macOS/Linux retenus. Réutiliser les adaptations déjà faites
  dans les tâches de plateforme. Éviter les conflits avec une installation ADB
  existante ; ne pas arrêter automatiquement un serveur partagé.
- Retirer à ce stade la copie d'ADB du build Windows et la dépendance de son
  packaging aux Platform-Tools. Adapter la CI pour vérifier que le Desktop
  peut être produit sans SDK Android installé pour cette copie. La chaîne
  Android nécessaire à la compilation de l'APK Quest reste distincte.
- Préserver découverte réseau, IP de secours, appairage sécurisé, mémorisation,
  transfert et reconnexion. Si l'UI évolue, modifier son prefab et ses références.

### Tutoriel d'installation et de configuration

Produire une documentation utilisateur durable, liée depuis l'aide HiBoP,
avec étapes, chemins et libellés système vérifiés à la date d'exécution :

1. Expliquer les deux parcours et préciser que seul l'USB optionnel nécessite ADB.
2. Installer les Platform-Tools seuls sous Windows/macOS, ou une distribution
   ADB identifiée sous Linux ; extraction, dépendances et emplacement à indiquer
   à HiBoP. Traiter les pilotes Windows, droits d'exécution macOS et permissions
   USB/règles `udev` Linux réellement nécessaires sur les cibles retenues.
3. Décrire les prérequis Meta actuels au mode développeur (compte, organisation
   ou vérification si requis), son activation et l'autorisation du débogage USB
   pour l'ordinateur choisi. Revérifier les écrans officiels ; ne pas reprendre
   aveuglément les menus Android génériques. Expliquer ce que l'autorisation
   donne au poste et comment la révoquer/désactiver le mode développeur.
4. Brancher un câble de données, ouvrir HiBoP sur le casque, sélectionner ADB si
   nécessaire, puis suivre liste → code initial → Pair → Envoyer au Quest.
5. Fournir un dépannage actionnable : ADB absent/incompatible, casque non
   autorisé ou hors ligne, câble de charge seul, permissions USB, application
   Quest fermée, plusieurs casques, coupure et reconnexion. Préserver l'accès réseau.

### Installation Quest et licences

- Documenter une installation de HiBoP Quest sans mode développeur pour le
  parcours principal, via un canal Meta approprié à décider/valider. Une recette
  reposant uniquement sur `adb install` ne satisfait pas ce parcours.
- Distinguer l'installation de l'APK et le transport des visualisations ; ne
  pas imposer ADB aux utilisateurs réseau pour résoudre implicitement la première.
- Vérifier les conditions applicables aux sources de téléchargement choisies.
  ADB est sous Apache 2.0 ; les licences des dépendances et des binaires exacts
  restent à examiner si des composants sont finalement redistribués. La présence
  d'un `NOTICE.txt` ne vaut pas audit complet. Documenter provenance et versions.

## Hors périmètre et décisions restantes

Pas d'AOA, de nouveau protocole scientifique, de qualification générale de
toutes les distributions Linux, ni de protection des secrets Mac/Linux ajoutée
implicitement. Linux reste conditionné à QUEST-027. Le canal de distribution
Quest et les détails de packaging restent à préciser ; la cible ADB externe
est déjà choisie par D34. Aucune publication Store, push ou CI distante n'est
implicitement autorisée par cette fiche.

## Vérifications à réaliser par l'agent

- Sur un environnement sans ADB, démarrage et parcours réseau fonctionnels ;
  option USB indisponible avec une action d'installation compréhensible.
- Depuis une installation neuve des outils documentés, découverte USB,
  appairage, transfert vérifié et reconnexion après coupure de 60 secondes.
- Vérifier les erreurs du tutoriel, chemins contenant des espaces, sélection
  entre appareils et coexistence avec ADB déjà installé. Ne pas publier de
  secrets d'appairage, clés ADB ou informations de compte dans les preuves.
- Vérifier le contenu des builds et la séparation du SDK de compilation Quest.
  Distinguer tests locaux et CI réellement exécutée, réseau et USB, ainsi que
  chaque OS/version/architecture testés.
- Tester le parcours principal avec mode développeur désactivé et installation
  Quest via le canal choisi. Une preuve manquante demeure non vérifiée.

## Validation manuelle du propriétaire

Préparer d'abord binaires, guide et fixture, puis fournir les liens, chemins,
boutons et résultats attendus pour les cibles effectivement disponibles :

1. Suivre le parcours réseau sans ADB ni mode développeur et envoyer la fixture.
2. Suivre le tutoriel USB depuis l'installation des outils jusqu'au premier envoi.
3. Débrancher/rebrancher puis relancer HiBoP et vérifier la reconnexion mémorisée.
4. Confirmer les messages de dépannage proposés et la possibilité de revenir au réseau.

Recueillir OK/KO et observations séparément ; un essai réalisé par l'agent ne
remplace pas le retour utilisateur sur la clarté du guide.

## Rapport et critère de fin

Produire `reports/QUEST-031.md`, `evidence/QUEST-031/manifest.json`, le guide
utilisateur et la mise à jour du registre selon le contrat. Expliquer le
packaging avant/après, les dépendances exactes et les limites de plateforme.

Le parcours réseau sans ADB et l'option USB avec ADB externe sont documentés
et vérifiés sur les plateformes retenues, le tutoriel est validé, la transition
Windows est réalisée et les preuves de packaging/installation sont présentes.
Une plateforme différée reste explicitement hors qualification.

## Sources à revérifier lors de l'exécution

- [Android — Platform-Tools](https://developer.android.com/tools/releases/platform-tools).
- [Android — connexion d'un appareil et prérequis par système](https://developer.android.com/studio/run/device).
- [Meta — configuration d'un appareil pour le développement](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/).
- [Meta — canaux de distribution](https://developers.meta.com/horizon/resources/publish-release-channels/).
- [ADB — licence Apache 2.0](https://android.googlesource.com/platform/packages/modules/adb/+/refs/heads/main/NOTICE).
- [Google — conditions SDK, notamment 3.4/3.5](https://developer.android.com/studio/terms).
