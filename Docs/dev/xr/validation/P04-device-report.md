# P04 — rapport de validation Quest 3

- **Date :** 2026-09-01
- **Unity :** `6000.5.2f1`
- **Application ID :** `fr.crnl.hibop.xr.dev`
- **État :** PASS — bootstrap OpenXR validé sur Quest 3 physique
- **Transport de déploiement :** ADB par Wi-Fi après autorisation USB initiale
- **Confidentialité du rapport :** numéro de série et adresse réseau de l'appareil volontairement non consignés

## Artefact validé

- APK : `.artifacts/xr/p04/HiBoPXR-P04.apk`
- Taille : `36 977 736` octets
- SHA-256 : `a464f948a4d3066c944e794a62b633dd68f41fff392512f7820d6bf4ff47bfe3`
- Backend : IL2CPP, ARM64, Vulkan
- Android : API 32 minimum, target automatique
- Déclaration passthrough : `com.oculus.feature.PASSTHROUGH` avec `android:required="false"`

## Validation hôte

- Gate P04-A–E accepté et enregistré avant installation ; ADR, lock de décision et lock UPM présents.
- Résolution UPM exacte réussie, sans Oculus XR Plugin, Meta XR Core SDK ni Meta Interaction SDK.
- Input System est l'unique backend d'entrée ; l'Input Manager legacy est désactivé.
- OpenXR Android active Meta Quest Support, Camera (Passthrough), Composition Layers Support, XR Hands, Meta Hand Aim, Oculus Touch, Touch Plus et Display Utilities.
- Trois tests EditMode réussis (`3/3`, `0` échec) : réglages verrouillés, intégrité du prefab et scène à instance de prefab unique.
- Le rig est prefab-first. La scène de validation ne contient qu'une instance du prefab bootstrap.
- La caméra principale utilise un `TrackedPoseDriver` sérialisé avec les poses `centerEye` de l'Input System.
- Le build reproductible a produit l'APK ARM64/IL2CPP et son fichier de preuve `.artifacts/xr/p04/build-evidence.json`.

## Validation sur appareil

| Critère | Preuve observée | Résultat |
| --- | --- | --- |
| Installation et lancement | APK installé par ADB Wi-Fi, application lancée et session OpenXR passée à `FOCUSED` | PASS |
| Passthrough par défaut | Environnement réel visible ; création runtime `xrCreatePassthroughFB` et `xrCreatePassthroughLayerFB` réussie | PASS |
| Repli VR et retour MR | Bascule A/X observée vers le décor VR, puis retour au passthrough ; destruction/recréation de la couche confirmée dans logcat | PASS |
| Tête | Vue et mouvement de tête cohérents dans les deux modes ; avertissement de pose caméra absent après ajout du `TrackedPoseDriver` | PASS |
| Deux contrôleurs | Deux cylindres bleus suivent séparément les contrôleurs gauche et droit | PASS |
| Deux mains | Après activation du suivi des mains dans Horizon OS, deux marqueurs verts suivent les mains | PASS |
| Changement de modalité | Passage contrôleurs → mains observé sans redémarrage de l'application | PASS |
| Fréquence | Demande 72 Hz acceptée et baseline VrApi stable à `72/72` ou `73/72` FPS | PASS |
| Données sensibles | Aucun secret, pose, image caméra, numéro de série ou adresse réseau émis par les logs applicatifs P04 | PASS |

Les observations visuelles des contrôleurs, du passthrough/VR et des mains ont été confirmées directement par l'opérateur portant le casque.

## Baseline appareil

Échantillon VrApi relevé après stabilisation de la scène minimale :

- fréquence : `72/72` ou `73/72` FPS ;
- frames périmées (`Stale`) : `0` sur les échantillons relevés ;
- temps GPU applicatif : généralement `0,00–0,10 ms`, maximum observé `0,45 ms` ;
- temps combiné CPU/GPU rapporté : environ `1,30–2,03 ms` ;
- niveaux dynamiques : CPU `4`, GPU `2` ;
- mémoire du processus, instantané : PSS total `323 608 KiB`, RSS total `477 004 KiB` ;
- température observée : `37–39 °C`.

Cette mesure est une baseline de smoke test du bootstrap, pas un budget de performance pour les futures scènes scientifiques.

## Correctif découvert sur appareil

La première installation suivait correctement les deux contrôleurs mais restait en VR. Le provider caméra Meta crée son passthrough au moyen de XR Composition Layers ; la feature OpenXR Android `Composition Layers Support`, exigée par le feature set AR Foundation du package Meta, n'était pas activée. P04 active désormais explicitement cette feature et sa validation de projet échoue si elle manque. Le second APK a validé le passthrough et le repli VR.

## Avertissements non bloquants

- `No suitable capture camera found` concerne l'initialisation facultative de la capture d'images CPU. P04 ne demande aucun accès aux pixels ; la couche passthrough OpenXR est créée et fonctionne indépendamment.
- Le manifeste généré conserve un avertissement de dépréciation Meta sur l'ancien nom de permission hand tracking ; le suivi des mains a néanmoins été validé sur l'appareil.

## Commandes reproductibles

Quand Unity est fermé :

```powershell
$env:TEMP = 'C:\jtmp'
$env:TMP = 'C:\jtmp'
.\XR\Tools\Build-P04.ps1
.\XR\Tools\Deploy-P04.ps1
```

Le chemin temporaire court évite une limitation de socket Gradle/JDK observée dans l'environnement Windows de l'agent. Le déploiement Wi-Fi suppose que le Quest est réveillé, sur le même réseau, déjà autorisé par USB et visible par `adb devices`.

Résultats de tests : `.test-results/unity-cli/p04/editmode-results.xml`  
Journal de build : `.artifacts/xr/p04/build.log`
