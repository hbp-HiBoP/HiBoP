# QUEST-transfer — Transfert Desktop → Quest : lot 2

Date : 16 septembre 2026.

Résultat physique : [mesure du lot 2](QUEST-transfer-result-04-lot-2.md).
**59,914 s** jusqu’à confirmation, soit **−30,8 %** par rapport aux lots 0–1 ;
visualisation complète et utilisable, aucune anomalie signalée.

Références : [plan d’optimisation](QUEST-transfer-optimization-plan.md),
[résultat des lots 0 et 1](QUEST-transfer-result-03-lots-0-1.md).

## Objectif et périmètre

Le lot 2 remplace les fichiers individuels de tableaux par un pack indexé et
compacte les références JSON. L’essai précédent comportait 48 553 entrées ZIP,
dont 48 549 buffers. La restauration ouvrait 49 672 fichiers de tableaux en
5,988 s, sur 6,737 s de lecture numérique. L’extraction prenait 19,274 s,
la désérialisation JSON 12,993 s et la création ZIP Desktop 13,186 s.
Ces durées imbriquées ne doivent pas être additionnées.

Ce lot conserve ZIP/Deflate Fastest, TLS, les hashes des chunks et de l’archive,
ainsi que la restauration native et les optimisations des lots 0 et 1. Il ne
modifie pas les valeurs scientifiques, la précision, ni les réglages du rendu.
Le gain réel et la nouvelle taille compressée restent à mesurer sur Small.

## Format de scène 4

Le Desktop produit désormais une scène version 4 contenant :

| Entrée | Contenu |
| --- | --- |
| `visualization.json` | Graphe de scène ; tableaux numériques et définitions globales référencés par indices entiers |
| `buffers.index` | Signature HBPI, version 1, nombre d’éléments ; pour chaque élément : SHA-256 binaire, offset 64 bits, longueur 64 bits, en little-endian |
| `buffers.pack` | Concaténation des buffers dédupliqués, sans en-tête par tableau |
| `globals.refs.json` | Contexte d’appairage et table unique des couples identité/empreinte des définitions globales |
| Ressources natives | Fichiers IRM, éventuels descripteurs de paires et autres ressources nécessitant un chemin natif |

Les surfaces binaires utilisent aussi le pack, avec leur identité de contenu
conservée dans les références de surfaces. Les références standard demeurent
externes à l’archive. Le nombre exact d’entrées du prochain Small dépendra de
ses ressources natives ; il ne faut pas annoncer quatre entrées pour toute scène.

La lecture des anciennes scènes version 3 reste disponible. Une scène hybride,
une version inconnue, manquante ou dupliquée sont rejetées. Les données globales
d’appairage gardent leur format 2. Les deux players doivent être mis à jour
ensemble : un ancien Quest ne sait pas lire une scène 4.

## Chemins optimisés et garanties

- Desktop : buffers capturés en mémoire puis concaténés par blocs de 64 Kio
  vers une seule entrée compressée. Suppression de l’ouverture/finalisation
  d’une entrée ZIP par tableau. Aucun grand tableau contigu supplémentaire
  de la taille du pack n’est construit.
- Quest : index borné et validé avant extraction ; vérification SHA-256 de
  chaque plage pendant la décompression/copie, y compris les buffers vides
  et les frontières traversant plusieurs blocs. Aucun hash de tableau retiré.
- Un seul flux de lecture du pack reste ouvert. Chaque accès obtient une vue
  bornée ; le positionnement et la lecture du flux partagé sont protégés.
  Les tableaux restaurés gardent leur propre stockage modifiable, même quand
  les octets étaient dédupliqués dans l’archive.
- Les offsets doivent couvrir exactement le pack, sans trous ni chevauchements.
  Tailles négatives, dépassements, doublons, troncatures et index invalides sont
  rejetés. Budgets conservés : 4 Gio décompressés, 100 000 ressources logiques,
  128 Mio de métadonnées cumulées, dont index et table globale.
- Définitions globales validées une fois dans la table reçue, puis résolution
  des indices avec contrôle de type et conservation des instances canoniques.
  Chaque nouvel objet source est toujours comparé à la définition appairée.
  Les indices restent stables entre captures successives d’une même archive ;
  un changement de contexte ou de définition ne peut réinterpréter silencieusement
  une capture antérieure.
- Les vues ouvertes retiennent le pack jusqu’à leur fermeture. Une demande de
  destruction interdit les nouvelles vues et diffère la suppression du workspace.
  Les ressources natives conservent le mécanisme de provenance du lot 1.

## Mesures du prochain essai

Le marqueur est `optimizationBatch = lots-0-1-2`. Les traces ajoutent
`containerVersion`, `pack.buffers`, `pack.bytes`, `pack.indexBytes`,
`pack.fileOpens`, `pack.rangeReads`, `globals.tableEntries`, `globals.tableBytes`,
`archive.pack.write`, `archive.pack.deflateAndWrite`, `quest.extract.packHash`,
`quest.global.tableValidation` et `quest.numeric.openRange`.
Les compteurs d’extraction par extension distinguent `.pack`, `.index`, `.json`
et les fichiers natifs. Les anciennes ouvertures numériques de fichiers doivent
disparaître du chemin version 4, remplacées par les vues du pack.

Corrections de l’instrumentation après l’essai précédent :

- RSS Windows obtenu par l’API native `GetProcessMemoryInfo`, car
  `Process.WorkingSet64` échouait dans le player IL2CPP. Un échec RSS n’empêche
  plus la collecte de mémoire managée et des compteurs GC.
- Compteur d’allocations du thread indiqué indisponible sous IL2CPP (`-1` et
  indicateur explicite), au lieu d’interpréter un zéro non pris en charge.
- Échantillons mémoire poursuivis dix secondes après fin et rendu ; les
  statistiques de frames restent limitées à la période du transfert/rendu.
  Les jalons mémoire contribuent aussi au maximum Unity. Les pics RSS restent
  des pics échantillonnés et le VmHWM Android couvre la vie du processus.

## Validation et préparation

- Formatage : `Tools/format-code.cmd` exécuté ; `git diff --check` réussi.
- 78 tests EditMode réussis : `.test-results/quest-032/editmode.xml`.
  Couverture : parité v3/v4, tableaux vides et nulls autorisés, indépendance des
  tableaux dédupliqués, identité canonique, corruption, bornes et débordements,
  annulation, durée de vie des vues, captures successives et changement de
  définitions d’appairage, formats incomplets/hybrides.
- Revue indépendante intégrité/durée de vie : corrections de la stabilité des
  indices entre captures et du rejet d’une version racine dupliquée ; vérification
  du hash lors de la réutilisation d’un indice global après réappairage.

- Test PlayMode `CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures`
  réussi en 93,1 s : `.test-results/quest-032/playmode.xml`. Restauration native
  des six modalités, rendu, recapture, remplacement rejeté, annulation et maintien
  de la scène publiée contrôlés. Le premier passage avait atteint ces contrôles
  fonctionnels mais détecté l’absence du compteur `archive.zip.finalize` ; ce
  compteur a été rétabli avant le passage réussi.

- Build Windows Release IL2CPP réussi : zéro erreur, 66 avertissements,
  275,4 s. Rapport : `.artifacts/quest-032/Windows/DesktopWindows.build-report.json`.
  Player relancé sur `visu_full_test / Small` après rebranchement, PID 34228, journal dédié
  `.test-results/quest-032/desktop-player.log`.

- Build Quest Release IL2CPP réussi : zéro erreur, 67 avertissements,
  411,8 s. Rapport : `.artifacts/quest-032/Android/Quest.build-report.json`.
- APK signé avec l’identité existante et vérifié : 336 014 044 octets,
  9 bibliothèques ARM64, artefacts natifs conformes au verrou du dépôt.
  SHA-256 : `c097f98997ae0cfbc4e5df55da19a59e52b9c3f1ee87826c570a3001fa15063a`.
  Preuve : `.test-results/quest-032/apk-content.json`.
- Sources identifiées par `.test-results/quest-032/source-manifest.json`.
  Les modifications automatiques de BuildInfo et des profils URP dues aux builds
  ont été remises à leur état précédant ces builds ; les players compilés et
  leurs rapports conservent les informations de cette version.

Installation `adb install -r` réussie après recharge, redémarrage et rebranchement
du Quest par l’utilisateur. Les données sont conservées. Batterie observée :
83 %, alimentation USB, température batterie 39 °C ; adresse Wi-Fi 192.168.1.18.
Preuves : `quest-installed-package.txt`, `quest-battery-before-test.txt` et
`quest-network-before-test.txt` dans `.test-results/quest-032/`.

Le premier lancement après installation a été intercepté par Horizon OS :
`common_system_dialog_app_launch_blocked_controller_required`. Ce n’est pas un
crash HiBoP : le processus n’a pas démarré. Après intervention de l’utilisateur,
HiBoP a démarré (PID 7382). Contrôle XR réussi : `COMPOSITION READY; problem=none`,
OpenXR/Oculus, Vulkan, stéréo SinglePassInstanced, suivi de tête actif.
Preuve : `.test-results/quest-032/quest-startup.log`. Desktop PID 34228 actif ;
adresse Wi-Fi du Quest reconfirmée à 192.168.1.18. Benchmark physique terminé,
traces collectées et analysées dans le rapport de résultat lié ci-dessus.
Journal : `.test-results/quest-032/quest-launch-diagnostic.log`.

## Procédure du benchmark

1. Garder le câble USB branché et les deux appareils sur le même Wi-Fi.
2. Utiliser les nouveaux players Release, attendre le chargement de
   `visu_full_test` / **Small**, calcul automatique d’activité désactivé.
3. Porter le Quest avec HiBoP actif. Dans le panneau Desktop, cocher
   **Enter an IP address manually**, saisir **192.168.1.18** (adresse LAN vérifiée),
   puis **Pair**. Saisir le code affiché si demandé ; **Y** dans le casque
   permet de renouveler un code expiré.
4. Cliquer une seule fois sur **Envoyer au Quest**. Ne pas utiliser
   **Retry same snapshot**, qui saute la préparation d’une nouvelle archive.
5. Garder le casque porté et HiBoP actif jusqu’à ce que Small soit complète
   et utilisable. Attendre encore **10 secondes**.
6. Répondre **« terminé »**, en indiquant toute anomalie. Garder les applications
   ouvertes et le câble connecté pour la collecte automatique des traces.

Un seul transfert neuf est prévu. La comparaison distinguera les coûts de
premier chargement de ceux du transfert ; aucune estimation corrigée à chaud
ne sera présentée comme une mesure réelle.
