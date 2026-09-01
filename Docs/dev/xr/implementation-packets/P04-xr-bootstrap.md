# P04 — bootstrap Quest 3 OpenXR

## Objectif et résultat observable

Produire un APK ARM64/IL2CPP minimal qui démarre sur Quest 3, affiche une scène de diagnostic en passthrough par défaut avec bascule VR, suit tête/mains/contrôleurs et journalise uniquement des données non sensibles.

## Decision gate

**Hérité :** cible Quest 3, D01 projet séparé, D15 OpenXR/XRI baseline et Meta isolé, D16 Input System XR, D17 confidentialité, D20 baseline 72 Hz.

**À résoudre avant configuration de projet :**

- `P04-A` : versions exactes Unity/OpenXR/XRI/XR Hands/Unity OpenXR: Meta/Meta XR nécessaires et matrice de compatibilité ;
- `P04-B` : provider de passthrough et frontière exacte de l'adaptateur Meta ;
- `P04-C` : min/target Android API, backend graphique et paramètres ARM64/IL2CPP ;
- `P04-D` : application ID de développement, keystore temporaire et propriétaire des secrets ;
- `P04-E` : passthrough déclaré `Required` ou `Supported` dans le build.

Ces décisions sont vérifiées depuis les sources officielles actuelles et consignées dans un ADR/package lock. Ne pas installer de packages avant résolution.

## Périmètre autorisé

- projet XR créé en P01 ;
- packages XR approuvés, settings Android/OpenXR ;
- bootstrap, rig, scène diagnostic et build script minimal ;
- mains/contrôleurs sans UX HiBoP.

## Hors périmètre

- réseau, RenderModel, données patient ;
- Meta Interaction SDK hors décision P04-B ;
- renderer scientifique ;
- distribution Meta production.

## Hypothèses fixées

- Input System uniquement ;
- OpenXR provider principal ;
- passthrough derrière une interface plateforme ;
- VR de repli reste disponible ;
- cible initiale 72 Hz, sans revendiquer 90 Hz.

## Dépendances et état initial

- P01 topologie intégrée ;
- Android Build Support/SDK/NDK disponibles ou manque déclaré avant modification ;
- Quest 3 de développement autorisé et état de connexion confirmé par l'utilisateur si nécessaire.

## Fichiers/modules pressentis

- projet XR Packages/ProjectSettings/Assets ;
- assemblies XR sous `XR/Assets/` pour OpenXR et l'adaptateur Meta ;
- build/CI smoke et documentation device.

## Étapes

1. Résoudre P04-A–E.
2. Installer/verrouiller seulement les packages approuvés.
3. Configurer Android ARM64/IL2CPP/OpenXR et validations projet.
4. Créer rig/scène diagnostic prefab-first.
5. Implémenter passthrough/VR par capability.
6. Afficher tracking tête, mains et contrôleurs sans données biométriques loggées.
7. Construire/déployer APK et capturer métriques de base.

## Tests et commandes

- Unity project validation OpenXR ;
- EditMode des adaptateurs/capabilities ;
- build APK ARM64/IL2CPP ;
- lancement Quest, bascule passthrough/VR, perte/reprise tracking ;
- Profiler frame CPU/GPU baseline et scan logcat.

## Critères de sortie binaires

- [ ] P04-A–E et package lock enregistrés ;
- [ ] APK installable/démarrable sur Quest 3 ;
- [ ] passthrough par défaut et VR de repli fonctionnent conformément à P04-E ;
- [ ] tête, mains et deux contrôleurs détectés ;
- [ ] Input Manager legacy absent du projet XR ;
- [ ] logs exempts de données sensibles ;
- [ ] réglages et commande de build reproductibles.

## Artefacts à remettre

Projet/configuration XR, rig/scène, adaptateurs, APK de test local, package lock, ADR P04, rapport device/profiler.

## Conditions d'arrêt

Arrêter si versions officielles incompatibles, module Android absent, appareil/permissions inconnus ou si P04-E exige une décision produit non fournie.

## Prompt de démarrage

> Exécute P04 depuis `Docs/dev/xr/implementation-packets/P04-xr-bootstrap.md`. Vérifie d'abord P04-A–E avec documentation officielle actuelle et enregistre l'ADR/package lock. N'installe aucun package avant ce gate. Construis ensuite uniquement le bootstrap OpenXR Quest 3, prefab-first, et valide sur appareil avec passthrough/VR, mains et contrôleurs.
