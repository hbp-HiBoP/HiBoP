# Transfert Desktop → Quest — lot 5 et bilan final

Date : 16 septembre 2026. Référence : `visu_full_test / Small`.
Statut : **lot 5 clôturé** — fusion native, nettoyage, tests, builds Release et
essai Wi-Fi final réalisés. Fonctionnement confirmé par l'utilisateur ; réserve
explicite sur la comparaison de performance avec le lot 4 ci-dessous.
Les performances du lot 4 sont acceptées par l'utilisateur. Les optimisations
supplémentaires sont [reportées](QUEST-transfer-future-optimizations.md).

## Changements de clôture

- Compression et SHA-256 intégrés à la DLL/SO `hbp_core`, API `hbt_*` conservée.
  Sources, dépendances vendored, tests, ABI, notices et SBOM sont centralisés
  dans le dépôt `hbp_core`. Les algorithmes scientifiques ne sont pas modifiés.
- Retrait des deux plugins `hbp_transfer`, de leurs `.meta`, de
  `Tools/TransferNative`, `Build-TransferNative.ps1` et du verrou séparé.
  Les builds players et le contrôle APK utilisent le verrou natif commun.
  L'APK rejette explicitement une ancienne bibliothèque séparée résiduelle.
- Import local Windows/Android depuis un même snapshot, avec vérification des
  manifestes/hashes et restauration des fichiers précédents en cas d'échec.
  Les plateformes non remplacées conservent leur identité de source propre.
- Retrait de l'assembly `HBP.Transfer.Diagnostics`, de ses dépendances, scopes,
  compteurs par bloc, échantillonnages mémoire/CPU/frames, observateur caméra,
  fichiers JSON par transfert et suivi pendant dix secondes.
- Suppression des tests dédiés aux sondes ; maintien des assertions fonctionnelles
  de corruption, publication, annulation, concurrence, fermeture et recapture.
  Retrait de l'autotest temporaire de démarrage ; codecs qualifiés par tests natifs,
  tests C# et livraison réelle dans les players de clôture.
- Conservation d'un résumé Desktop `QUEST_TRANSFER` après reçu : identité,
  statut, empreinte, octets, route, retry et durée totale. Le Quest journalise
  la publication. Aucune décomposition fine ni mesure photons n'est revendiquée.
- Rapports renommés `QUEST-transfer-lots-0-1.md`, `QUEST-transfer-lot-2.md`,
  `QUEST-transfer-lot-3.md`, `QUEST-transfer-lot-4.md` ; liens mis à jour.
  Les répertoires de preuves historiques gardent leur nom d'origine.

## Validation native et provenance

Windows : **16/16 tests natifs réussis**, dont SHA/codecs, puis contrôle du binaire
packagé. Android : 234 exports publics, dépendances système attendues, libc++
statique et segments alignés à 16 Kio. Deux constructions propres du même
snapshot produisent le même SHA-256. Sur le Quest physique, le benchmark du module
fusionné valide le vecteur SHA, offsets/reset et les roundtrips LZ4/Zstd.

Le snapshot local de build est `4f71e2dc5eebb9c4ce29179d3714f8b9f59eca34`,
créé uniquement dans `hbp_core/out/transfer-final-source`. Il ne crée aucun commit
dans l'historique de travail des dépôts. Les sources Windows ont été comparées
octet par octet à ce snapshot ; Android est construit depuis son archive Git.
Le HEAD natif de départ est `80e94a492210fcb37667a80e0a9a59176debe7d5` ; son
contenu suivi était identique au commit scientifique précédemment distribué
`ffb7686011f37a21e6c5ab4f88ad6cfcbc53ffc1`.

| Binaire intégré | SHA-256 |
|---|---|
| Windows `hbp_core.dll` | `1e77e1675b0cfdbeddbb504f4efaff99acaed541b7caf85f8a3e6ff13de62e45` |
| Android `libhbp_core.so` | `fb401a348af82ccc8c816e7656aebf241ab5959f0332c41da94dc5e3fef9538f` |

Les binaires Unity Linux/macOS gardent leurs versions antérieures et leur chemin
de transfert existant. Les sources et contrôles multiplateformes sont adaptés ;
une compilation/qualification de ces deux plateformes n'a pas été effectuée sur
cet hôte Windows. La validation présente concerne Desktop Windows et Quest.

## Validation Unity et players

EditMode : **162 succès, zéro échec**, dont 100 tests de transfert de scène et
62 tests d'anatomie Desktop. Deux tests additionnels sont ignorés explicitement
car ils nécessitent des captures historiques QUEST-017 absentes ; ils ne sont
pas comptés comme réussis. Compilation après retrait des références temporaires.
Preuves : `.test-results/quest-transfer-final/editmode.xml` et `editmode.log`.

PlayMode : **7/7 succès** : préparation froide concurrente avec annulation et
fermeture, propriété des résultats natifs, restauration complète six modalités
sur ZIP et HBT4, recapture et remplacement invalide. Preuves : `playmode.xml`
et `playmode.log` dans le même répertoire. Les avertissements de parsing initial
de `TagManager.asset` étaient déjà présents dans les runs du lot 4 ; ce fichier
n'a pas été modifié par ce chantier.

Formatage officiel exécuté. Une passe de réduction du diff rétablit la
présentation d'origine des déclarations inchangées avec contrôle d'équivalence
syntaxique Roslyn ; elle ne modifie pas leur comportement.
Player Windows Release IL2CPP construit avec succès : zéro erreur de build,
254,11 s. Le contrôle du package confirme la DLL fusionnée conforme au verrou,
l'absence de `hbp_transfer` et de l'assembly `HBP.Transfer.Diagnostics`.
Artefact : `.artifacts/quest-transfer-final/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
SHA-256 de `GameAssembly.dll` :
`5b480e03a9db638a52cba556e0c99532d1448e9e2cde80be4c2624a27e1fdc9a`.
Preuves : `windows-build.log`, `windows-package-verification.json` et
`.artifacts/quest-transfer-final/Windows/DesktopWindows.build-report.json`.

Player Android Release IL2CPP construit avec succès : zéro erreur de build,
403,83 s. APK signée avec l'identité épinglée, neuf bibliothèques ARM64,
336 263 974 octets. `hbp_core` et `hbp_math` conformes au verrou ; aucun plugin
`hbp_transfer`. Artefact : `.artifacts/quest-transfer-final/Android/HiBoP.Quest.apk`.
SHA-256 de l'APK ayant servi à l'essai physique, avant correction documentaire
LZ4 décrite ci-dessous :
`0635c45bf03d44fd7c223e1b9e4a7f8f0e4c548043b9e612caae9dc584a9319c`.
Preuves : `android-build.log`, `apk-content.json` et
`.artifacts/quest-transfer-final/Android/Quest.build-report.json`.
Les modifications de cache de shaders URP produites par le build sont annulées.

## Essai physique final

Les deux players finaux ont été relancés ; les fichiers de cache persistants
ont été conservés. APK installée avec `adb install -r`. Sur le Desktop, Small
est visible ; dans le Quest, OpenXR confirme `COMPOSITION READY` et le suivi de
tête actif. Un seul envoi après Pair, via l'IP manuelle `192.168.1.18`, câble
USB conservé pour récupérer les journaux. Le reçu confirme bien `route=lan`,
`retry=False`, `delivery=Published`.

- Identité du transfert : `5b8a81ecdf374d599836da438e971f91`.
- Contenu encodé : **125 317 658 octets** ; SHA-256
  `2823d2a1947f2d1837eec5ef2ba7b2baa0e03470288aa1baa2ee2053215b1bf4`.
- Durée de l'opération Desktop : **24 769,1 ms**, soit **24,769 s**. Le
  chronomètre démarre dans l'action d'envoi et s'arrête après le reçu. Il inclut
  capture, livraison et attente de publication ; il exclut la préparation à Pair.
  L'affichage au dixième de milliseconde ne constitue pas une garantie de précision
  physique, ni une mesure de la première image réellement présentée.
- Publication Quest de la même identité à `18:42:01.928`, heure du journal Quest.
  Cette horloge n'est pas soustraite à celle du Desktop.
- Confirmation utilisateur : **« Terminé, aucune anomalie »**, pour une scène
  complète et utilisable, sans anomalie visuelle ni blocage.
- Inventaires des fichiers `TransferTraces` strictement identiques avant/après
  sur les deux appareils : aucune nouvelle trace détaillée.

Preuves : `.test-results/quest-transfer-final/final-smoke.json`,
`desktop-player.log`, `quest-player.log`, et inventaires `*-traces-before/after`.
Les journaux système/OpenXR préexistants restent présents. Les messages
`AssetPackManager` au démarrage et `xrDiscoverSpacesMETA` après la publication
étaient également présents dans les journaux du lot 4 ; aucune erreur de liaison
`hbp_core` ni erreur de transfert n'a été observée.

## Repères de performance

| Étape | Clic → fin d'opération Desktop |
|---|---:|
| Baseline Wi-Fi instrumentée | 105,533 s |
| Lot 2 | 59,914 s |
| Lot 3, deux envois | 41,374 / 41,982 s |
| Lot 4, deux envois | 20,027 / 20,241 s |
| Lot 5 sans instrumentation détaillée, un envoi | **24,769 s** |

Le lot 4 représente environ 81 % de moins que la baseline Wi-Fi après clic.
L'essai final représente **76,5 % de moins**, soit une durée divisée par environ
**4,26** par rapport à 105,533 s. Il est toutefois **4,53–4,74 s plus lent**
que les deux essais du lot 4, soit environ **22–24 % de plus**. Cet écart reste
inexpliqué : aucun gain n'est attribué au retrait des sondes ou à la fusion,
et l'absence de régression de performance par rapport au lot 4 n'est pas établie.
Un essai unique avec des conditions non contrôlées ne permet ni d'attribuer
cet écart au code, ni de l'écarter comme une simple fluctuation. Le bilan retient
donc **20–25 s observées** sur les dernières versions, sans garantie à 20 s.
Les conditions de caches et de température diffèrent : ce tableau est un bilan
des observations, pas une expérience contrôlée isolant chaque changement.
La préparation anticipée à Pair reste comptée séparément dans les rapports
intermédiaires (lot 4 : 9,314 s Desktop + 5,371 s Quest). Son retrait des sondes
ne signifie pas suppression de son coût.

Les traces anciennes et `Tools/Read-QuestTransferTrace.py` sont conservés pour
relire les preuves du benchmark ; cet outil ne s'exécute pas dans les players.
Les pauses résiduelles et la mémoire relevées au lot 4 restent des limites
acceptées, sans nouvelle promesse de délai réseau seul.

## Correction des notices LZ4 après vérification des licences

Le manifeste référençait `third_party/lz4/LICENSE`, qui décrit la répartition des
licences du dépôt amont, au lieu de `third_party/lz4/lib/LICENSE`, qui contient
le texte BSD-2-Clause applicable à la bibliothèque compilée. Cette référence est
corrigée ; les notices complètes et le SBOM sont régénérés, et leur cohérence
est validée par `New-HbpCoreThirdPartyDocumentation.ps1 -Check`.

La copie Unity `Assets/StreamingAssets/Licenses/hbp_core.txt` et celle du player
Windows final contiennent maintenant le texte intégral et le copyright LZ4.
L'APK au même emplacement de livraison est corrigée, alignée puis signée à
nouveau avec la même identité épinglée. Une comparaison de toutes ses entrées
confirme que seul `assets/Licenses/hbp_core.txt` change, hors données de signature :
les 74 autres entrées de contenu sont identiques, notamment tout le code compilé.
Contrôles de signature, d'alignement et de contenu APK réussis.

**SHA-256 de l'APK livrée après correction :**
`53cdcbae4d9d796d4a05d81a76eea765d34bbcd16b0c655d255f273b6cbcc031`.
Le SHA-256 des notices corrigées est
`72380ebb892d5a16d73dcf44f263d6972a217af0ce2aad2f92d0a83752a27334`.
Preuves : `.test-results/quest-transfer-final/lz4-notice/verification.json`,
APK originale et ancien rapport conservés dans ce même répertoire ; le rapport
`apk-content.json` principal correspond désormais à l'APK corrigée.

Aucune recompilation, réinstallation sur le casque ni nouveau benchmark :
la correction concerne uniquement les notices de redistribution. L'installation
utilisée pour l'essai reste celle d'origine ; le prochain déploiement de l'APK
livrée embarquera la notice corrigée. Les sources/manifestes des futurs builds
natifs sont corrigés ; les packages natifs intermédiaires et le snapshot de build
archivés restent les preuves historiques de la compilation initiale.

## Conclusion

Les trois demandes de clôture sont réalisées : module natif centralisé dans
`hbp_core`, rapports renommés sans identifiants de tâche artificiels, instrumentation
temporaire retirée. Les optimisations fonctionnelles des lots 0–4 restent présentes.
Les tests, les packages et l'unique essai physique confirment le fonctionnement
des players nettoyés. Le chantier est clos sur cette base, avec l'écart de durée
ci-dessus consigné explicitement ; aucune nouvelle optimisation ni campagne de
benchmark n'est engagée. Les pistes futures restent documentées séparément.
