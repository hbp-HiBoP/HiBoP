# QUEST-transfer — Transfert Desktop → Quest : lots 0 et 1

Date : 16 septembre 2026.

Résultat de l’essai : [mesure des lots 0 et 1](QUEST-transfer-result-03-lots-0-1.md).
86,592 s jusqu’à confirmation, aucune anomalie utilisateur. Voir les réserves
de comparaison premier transfert / état à chaud et les limites de mesure mémoire.

Référence : [plan d’optimisation](QUEST-transfer-optimization-plan.md),
[baseline Wi-Fi](QUEST-transfer-baseline-02-wifi.md),
[instrumentation initiale](QUEST-transfer-instrumentation.md).

## Changements

Les formats du transport et des archives restent identiques. Ces lots réduisent
les passages complets sur les données et les allocations ; ils ne remplacent
pas encore les 48 553 entrées ZIP par un conteneur de buffers indexé.

| Zone | Modification | Vérification maintenue |
| --- | --- | --- |
| Extraction Quest | Un seul tampon de 64 Kio pour toute l’archive, SHA-256 calculé pendant la copie, instance SHA réinitialisée entre fichiers | Noms, doublons, tailles, budget décompressé, troncature et empreinte de chaque ressource |
| Envoi Desktop | Empreinte finale conservée avec un flux ouvert en lecture ; aucun second hash complet avant l’envoi | Hash par chunk, hash global et reçu de publication inchangés |
| Renvoi / durée de vie | Les envois d’une même archive partagent un sémaphore ; une annulation en attente ne ferme pas le flux d’un envoi actif | Suppression seulement après le dernier envoi, refus d’un nouvel envoi après Dispose |
| Tableaux et surfaces | Lecture par blocs directement dans les tableaux typés | Format little-endian, dimensions, indices, finitude géométrique, masques, absence d’octets supplémentaires, annulation |
| IRM reçues | Provenance fournie par l’extracteur après validation de toute l’archive ; chargement natif sans deux SHA supplémentaires | Ressources détenues par l’archive, flux de garde, membres des paires identifiés séparément, accès temporaire qui retarde la destruction |
| Références standard Quest | Réutilisation des empreintes vérifiées à l’installation, publiées seulement après succès complet | Chaque empreinte attendue est comparée à chaque restauration ; racine identique, fichiers présents et conservés ouverts en lecture |
| Références Desktop | Aucun cache de provenance sur les fichiers utilisateurs | Rehash même si taille et date sont inchangées |

La ressource vérifiée est un contrat interne de propriété du workspace reçu,
pas une autorisation fondée sur un chemin arbitraire ou sa date. Sur Windows,
les flux de garde interdisent également les ouvertures concurrentes en écriture.
Sur Android, le contrat suppose que les fichiers de travail appartenant à
l’application ne sont pas modifiés par un acteur externe ; il ne prétend pas
résister à un appareil compromis ou à une mutation forcée via ADB.

Le cache standard dure le processus Android, pas les lancements successifs.
Un nouveau démarrage vérifie à nouveau les références installées. Le chargement
des références peut donc encore coûter du temps avant ou pendant le premier essai.

## Mesures ajoutées — lot 0

- Marqueur `optimizationBatch = lots-0-1` et nom concret du provider SHA-256.
- `volume.native.load`, `volume.provenance.before/after` pour le chemin général
  instrumenté ; `volume.provenance.reused` pour le chemin des IRM reçues. Dans
  ce dernier, les SHA retirés doivent être absents, pas attribués au lecteur natif.
- `quest.extract.hashInFlight.<extension>` remplace la relecture de vérification.
  Le temps de hash n’a pas disparu : sa lecture disque supplémentaire a disparu.
- `extract.copyBufferAllocatedBytes`, `send.finalHashReused`,
  `standard.verifiedHashReused` pour vérifier les chemins réellement pris.
- Allocations cumulées du thread aux jalons, GC et échantillons mémoire.
  Les différences de compteurs d’allocation ne sont interprétables qu’entre
  événements du même thread ; ce n’est pas l’allocation totale du processus.
- Échantillonnage indépendant du thread Unity, environ chaque seconde : RSS,
  mémoire managée et GC ; sous Android, `/proc/self/status` (dont VmHWM) et
  fréquences CPU accessibles. Un refus de lecture est signalé explicitement.
- Échantillonnage Unity chaque seconde disponible : focus, batterie, mémoire
  Unity allouée/réservée, allocations du thread principal ; sous Android,
  température batterie, branchement et état thermique Android.
- Coût des échantillonneurs agrégé dans `diagnostics.*`. La collecte de puissance
  sur le thread Unity est espacée par les blocages de ce thread ; les mesures
  processus continuent sur un worker. Les pics échantillonnés sont des bornes
  inférieures, et VmHWM couvre toute la vie du processus.

Les jalons de publication, envoi du reçu et premier rendu caméra CPU sont
conservés. Aucune nouvelle synchronisation d’horloge entre machines n’est
supposée. `droppedEvents` doit rester à zéro. Le JSON est resauvegardé après
l’arrêt de l’observateur processus afin d’inclure son dernier échantillon.

## Validation

- Formatage avec `Tools/format-code.cmd`.
- 51 tests EditMode réussis : `.test-results/quest-031/editmode.xml`.
- Test PlayMode `CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures`
  réussi (88,9 s), avec restauration des six modalités, recapture, rejet d’un
  remplacement incompatible et annulation : `.test-results/quest-031/playmode.xml`.
- Builds Windows et Android Release IL2CPP réussis, zéro erreur de build :
  `.artifacts/quest-031/Windows/DesktopWindows.build-report.json` et
  `.artifacts/quest-031/Android/Quest.build-report.json` (66 et 67 avertissements).
- APK signé avec l’identité existante, contrôle du contenu réussi : 335 973 084
  octets, 9 bibliothèques ARM64. SHA-256 :
  `733a4f6feb9e5b401f871c121696ff3e1b33acacfa5b35ec16fdf13baf847c9c`.
  Preuve : `.test-results/quest-031/apk-content.json`.
- Installation `adb install -r` réussie sur le Quest `2G0YC5ZHB20370`, puis
  lancement de HiBoP. Le player Desktop a été lancé avec `-pf visu_full_test.hibop
  -v Small` et un journal dédié `.test-results/quest-031/desktop-player.log`.
- Démarrage Quest contrôlé : `COMPOSITION READY; problem=none`, OpenXR/Vulkan,
  stéréo active, processus 23704. Journal : `.test-results/quest-031/quest-startup.log`.
  Le message Unity concernant `AssetPackManager` absent est toujours présent au
  démarrage, comme lors de la préparation précédente ; il n’empêche pas l’état XR
  prêt. La vérification visuelle de Small reste à faire pendant le test utilisateur.
- Revue indépendante ciblée sur intégrité et concurrence : correction de la
  branche de mesure RSS Android et acquisition des ressources natives à partir
  de chemins déjà matérialisés, sans copie après demande de destruction.

Les durées gagnées restent à mesurer sur le casque. Le repère de 60 secondes du
plan n’est pas un résultat obtenu ni une garantie de ces changements.

## Procédure utilisateur — un seul transfert neuf

1. Garder le Quest relié en USB, sur le même Wi-Fi que le PC. Le câble sert à la
   collecte ; le choix manuel de l’adresse garantit la route LAN. Ne pas modifier
   le branchement pendant la mesure.
2. Utiliser les players **Release reconstruits**, pas l’éditeur. Sur le Desktop,
   vérifier le projet `visu_full_test`, la visualisation **Small** sélectionnée
   et le calcul automatique d’activité désactivé. Attendre la fin du chargement.
3. Porter le casque et vérifier que HiBoP est ouvert. Sur le Desktop, ouvrir le
   panneau Quest, cocher **Enter an IP address manually**, saisir **192.168.1.18**, puis cliquer
   **Pair**. Si un code est demandé, saisir celui du casque ; si expiré,
   presser **Y** dans le casque et recommencer l’appairage. Attendre la confirmation.
4. Cliquer **Envoyer au Quest** **une seule fois**. Ne pas utiliser
   **Retry same snapshot**, qui réutilise une archive et ne mesure pas la préparation.
5. Garder HiBoP actif sur le PC et le casque porté jusqu’à l’apparition complète
   de Small. Éviter d’ouvrir d’autres applications ou de changer les réglages.
6. Quand la visualisation est utilisable, attendre encore **10 secondes**, puis
   répondre **« terminé »**, en précisant tout élément manquant ou anomalie.
   Laisser les players ouverts et le câble connecté pour la récupération des traces.

Pas de chronométrage manuel nécessaire. Un second essai à chaud ne sera demandé
que si le premier montre un coût initial qu’il faut séparer. La baseline Wi-Fi
était à chaud : les coûts d’initialisation d’un premier essai neuf devront être
isolés avant de comparer le total.

## Collecte autonome après le test

Les traces Desktop sont dans
`%USERPROFILE%/AppData/LocalLow/CRNL/HiBoP/TransferTraces` et les traces Quest dans
`/sdcard/Android/data/fr.crnl.hibop.quest/files/TransferTraces`.
Relier les deux traces par `archiveHash`, contrôler `route = lan`, le marqueur
des lots et l’état `Published`, puis analyser avec `Tools/Read-QuestTransferTrace.py`.
Conserver aussi logcat, état batterie/thermique et mémoire après essai.

Comparer séparément capture, ZIP, réseau/chunks, extraction, tableaux/JSON,
références, lecteur IRM natif, surfaces et premier rendu. Les scopes imbriqués
et les étapes Desktop/Quest concurrentes ne doivent pas être additionnés.
