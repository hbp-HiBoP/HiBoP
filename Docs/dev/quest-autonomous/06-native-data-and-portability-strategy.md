# Stratégie native, données et portabilité

## Bibliothèques

| Bibliothèque | Stratégie |
| --- | --- |
| hbp_core | Même source complète et ABI scientifique sur Windows, Android ARM64, macOS ARM64, puis Linux si qualifié. |
| hbp_math | Même principe ; sa nécessité immédiate doit suivre les appels réels de la tranche. Les corrélations iEEG l'utilisent, mais ne sont pas dans le prototype d'un instant. |
| EEGFormat | Import/préparation Desktop ; hors Quest initial. Ne pas porter la bibliothèque pour afficher/calculer des données déjà préparées. |

La provenance des binaires doit être reliée au commit source et au hash de
l'artefact. NativePlugins.lock.json épingle actuellement le hbp_core Desktop
à 1f26946. Le checkout cf4400b est une révision de dépôt distincte : vérifier les
arbres/exports avant de choisir les artefacts, et ne pas qualifier la parité
entre versions scientifiques différentes.

## Entrées scientifiques communes

La densité actuelle est construite sur une grille volumique issue de l'IRM.
Préserver les entrées suivantes dès le jalon C :

- volume NIfTI de référence et ses métadonnées spatiales ;
- surface de référence préparée, indexation et normales ;
- dimension et interpolation de la grille ;
- sites dans un ordre explicite, identité, position et masque effectif ;
- rayon d'influence et règle d'influence par distance ;
- paramètres de projection, couverture, opacité et normalisation applicables.

Pour D, ajouter les amplitudes déjà préparées, leurs unités, la provenance de
l'instant, Middle, SpanMin, SpanMax et les paramètres d'apparence scientifique.
Ne pas déduire ces réglages du seul vecteur d'amplitudes reçu.

Le masque actuel intègre IsMasked, blacklist, exclusion de la ROI active et
filtrage. Son calcul doit rester dans une fonction commune ou être capturé comme
état préparé avec ses flags sources ; ne pas réinventer un masque simplifié
dans Quest. La fixture n'a pas besoin d'une UI de ROI pour tester cette règle.

## Reconstruction

Volume.LoadNIFTIFile consomme un fichier. Pour le MNI initial, transférer les
octets NIfTI, vérifier leur hash puis fournir un fichier du cache privé de
session au wrapper existant. Cette écriture technique ne constitue ni un projet
portable ni une garantie de restauration. Le chemin est choisi localement, à
partir d'une identité validée, pas imposé par le chemin Desktop.

Surface.SetBuffers permet déjà de reconstruire une surface native à partir des
buffers reçus. Cette méthode convertit les coordonnées via Vec3 et le winding
via ReferenceSystemConversion. RawSiteList.AddSite attend au contraire des
coordonnées natives explicites. Une conversion commune et testée doit relier
le repère de transport à ces APIs ; ne pas réutiliser une position Unity monde
du renderer comme position scientifique.

Garder le repère de transport existant DesktopUnityMillimetersV1 pour les
géométries qui le respectent, et expliciter le repère natif à la reconstruction.
Vérifier le round-trip surface exportée -> surface reconstruite -> buffers
réexportés, y compris winding, normale et points asymétriques.

Le renderer convertit mm en mètres puis applique le placement/échelle locaux.
Un unique chemin de conversion doit servir aux surfaces et sites. Le calcul
conserve les unités scientifiques indépendamment de cette mise à l'échelle.

## Tranche native

Ordre observable à conserver : reconstruction -> grille -> générateur -> sites
et masques -> calcul -> normalisation -> binding/projection surface selon les
invalidation nécessaires -> résultats cohérents. L'extraction doit reprendre
l'ordre effectif de Base3DScene et ses dépendances, pas appliquer aveuglément
une séquence illustrative.

Le propriétaire de session garde les ressources natives et leur ordre de
libération. Planifier le calcul hors du thread de rendu lorsque les APIs le
permettent, utiliser des résultats préparés puis effectuer l'upload Unity sur
le thread principal. Ne pas ajouter des Task.Run concurrents utilisant les
mêmes handles. Un réglage de concurrence par plateforme est un paramètre de
performance, pas une variante de l'algorithme.

Le pool natif examine hardware_concurrency ; IEEGGenerator expose déjà des
options internes de parallélisme et métriques. Les coupes utilisent un chemin
SSE2 sur x86 et un fallback hors SSE2. Aucun chantier NEON/coupes n'est requis
pour le premier cerveau.

## Android

La compilation Release NDK ARM64 documentée est une preuve historique de
compilation seulement. Ajouter une cible reproductible aux outils natifs
existants, identifier NDK/API/ABI et dépendances ELF, puis qualifier dans un
Player Unity IL2CPP réel : chargement, erreur native, génération, copie, libération.

Ajouter Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so avec import Android
ARM64 uniquement ; désactiver Any Platform et Editor. Les DLL/bundles/so Desktop
restent explicitement exclus d'Android. Examiner les dépendances embarquées
dont le runtime C++ éventuel et la compatibilité de l'alignement du binaire
avec le Player. L'inspection du contenu APK est obligatoire.

Les réglages CPU/plateforme des plugins sont documentés par Unity :
[Import native Android plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/android-native-plugins-import.html).
Leur configuration correcte ne prouve pas l'ABI ou l'exécution.

## Mac et Linux

hbp_core/CMakeLists.txt et hbp_math/CMakeLists.txt fixent un minimum macOS 12.0.
Leurs scripts de build imposent aussi ce minimum et arm64 ; les workflows
utilisent macos-15. La machine de validation est Apple Silicon. Relever son OS
réel au jalon Mac et inspecter les binaires effectivement empaquetés.

La qualification porte sur ouverture HiBoP, import/préparation fixture, réseau,
transfert et résultats Quest. Un cross-publish de serveur ou un bundle compilé
ne valide pas cette expérience. Inclure le lancement du transport embarqué
et les demandes réseau de l'OS dans la démonstration.

Linux est une décision après Mac ; les outils existants ciblent x86_64 et un
environnement Ubuntu22 pour la bibliothèque. Ne pas annoncer un support général
des distributions sans essai.

## Direction ultérieure

Un export portable versionné contenant toutes les données peut réutiliser les
contrats de session, mais demande sa propre spécification de stockage et de
compatibilité. L'import de nouveaux fichiers EEG sur Quest reste un jalon
distinct et optionnel.
