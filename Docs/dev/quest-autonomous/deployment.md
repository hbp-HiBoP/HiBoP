# Builds et distribution Quest

La référence des règles de build, version, plugins natifs et signature est [Builds and distribution](../../agent-knowledge/builds-and-distribution.md). Les procédures d'exécution Unity/ADB sont dans le skill [hibop-unity-validation](../../../.agents/skills/hibop-unity-validation/SKILL.md), notamment sa [référence Quest](../../../.agents/skills/hibop-unity-validation/references/quest.md). Cette page indique les frontières produit ; elle ne remplace pas ces procédures.

## Cibles et ressources

Le profil Quest produit un Player Android **ARM64/IL2CPP/OpenXR** depuis le même projet que Desktop. Les profils Desktop n'activent pas le runtime XR Quest. Lire la version Unity dans `ProjectSettings/ProjectVersion.txt` et la version produit dans `codemeta.json`, sans recopier celles des anciennes campagnes. Le point d'entrée CLI partagé est `HBP.Dev.HBPBuilder.BuildFromCommandLine` ; sélectionner explicitement profil, sortie et log.

Quest utilise `libhbp_core.so` et `libhbp_math.so`. Le transfert de données préparées n'exige pas d'embarquer EEGFormat comme importeur de données brutes sur Android. Les couples Desktop/Android qualifiés doivent conserver la même provenance scientifique par bibliothèque ; les autres plateformes gardent leurs propres révisions enregistrées. [Update-NativePlugins.ps1](../../../Tools/Update-NativePlugins.ps1), sa configuration et son verrou sont les sources de vérité, pas un ancien nombre de bibliothèques ou un chemin de workstation.

La première intégration de scène commune avait révélé une absence de `hbp_math` dans l'APK : l'archive arrivait, puis la réinitialisation d'une colonne statique échouait dans `MathDLL.Mean`. Le [contrôle natif conservé](../../../Validation/Quest/retained-results.md#chargement-natif-android) vérifie le chargement et des calculs élémentaires du binaire corrigé, sans qualifier à lui seul une scène Unity. Vérifier la réception réseau ne remplace donc pas la vérification des dépendances natives utilisées après réception.

Les références scientifiques standard sont préparées par le builder Quest. Les fichiers compressés destinés à Unity utilisent notamment `.gz.bytes`. Les atlas Localizers sont distribués séparément et exclus des packages ordinaires ; l'option technique `QuestStandardDataSettings.IncludeLocalizers` est un opt-in, pas le réglage normal.

## APK et identité

Utiliser la clé privée déjà configurée dans le profil Quest/Android Publishing Settings et préserver la signature produite par Unity. Les anciens initialiseurs de signature ne définissent pas l'identité courante. Le déplacement sur une autre machine nécessite la même clé, transférée hors Git et sans exposer ses secrets.

[HBPAndroidPackaging](../../../Assets/Scripts/HBP/Dev/Editor/HBPAndroidPackaging.cs) force la recréation de l'archive finale `PackageApplication` en Debug et Release, tout en gardant la compilation incrémentale. Cela évite des régions ZIP inutilisées ; Gradle conserve alignement et signature. [Test-QuestApk.ps1](../../../Tools/Test-QuestApk.ps1) vérifie l'archive et ses ELF après build direct ou scripté. La marge acceptée de l'enveloppe est **1 Mio + 64 Kio par entrée**, pas une limite sur les données scientifiques.

Une signature incompatible à l'installation ne se corrige pas par `adb install -r`. Retrouver la clé d'origine ; toute réinstallation avec perte potentielle de données suit l'accord explicite et la sauvegarde prévus dans la référence de distribution.

## Réseau et portée des plateformes

Le parcours produit utilise LAN/Wi-Fi ou USB NCM et l'association dans les applications. ADB reste un outil de développement, absent des packages produit. [QuestDistributionValidation](../../../Assets/Scripts/HBP/Dev/Editor/QuestDistributionValidation.cs) contrôle les dépendances mDNS épinglées, retire l'ancien dossier de distribution USB/ADB et refuse les exécutables ADB. macOS reçoit les clés de confidentialité réseau/Bonjour avant sa signature finale.

La CI Desktop cible Windows x64, Linux x64 et macOS ARM64. Quest est un job manuel séparé ; `all` et la publication de release ne signifient pas publication d'un APK. Un build, une inspection ELF ou une signature ad-hoc ne prouvent pas une qualification de connexion, une notarisation publique ou une parité scientifique. La [matrice USB](usb-and-network-connectivity.md#qualification-par-plateforme) conserve la distinction Windows qualifié historiquement et macOS/Linux encore non qualifiés physiquement.
