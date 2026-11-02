# Validation et mesures Quest

Cette référence consolide des preuves **historiques** et leurs limites. La révision documentaire du 2 novembre 2026 n'a lancé ni Unity, ni test, ni build, ni recette casque. Les sources des tests ont été inspectées ; aucun résultat ancien n'est présenté comme une validation du checkout courant.

## Parité scientifique : portée des acceptations

Les fixtures de densité et iEEG ont comparé Desktop Windows et Quest 3 avec Unity **6000.5.2f1** et `hbp_core` **0.2.1**, sur les binaires/paramètres identifiés par leurs manifestes.

| Campagne | Domaine et acceptation historique |
| --- | --- |
| [Densité](../../../Validation/Quest/scientific-parity/density/measurement-context.json) / décision D30 | 18 comparaisons de projection densité. UV : tolérance absolue 5×10⁻⁷, maximum observé ≈2,98×10⁻⁷ ; alpha : 2×10⁻⁷, maximum ≈1,19×10⁻⁷. Masques, grilles et catégories restent exacts. |
| [iEEG](../../../Validation/Quest/scientific-parity/ieeg/measurement-context.json) / décision D31 | 36 comparaisons iEEG. Mêmes tolérances UV/alpha ; maxima ≈4,17×10⁻⁷ / 1,79×10⁻⁷. L'[acceptation numérique](../../../Validation/Quest/scientific-parity/ieeg/numerical-acceptance.json) est séparée de la preuve brute. |

Ces seuils ne couvrent pas arbitrairement tous les calculs, toutes les plateformes ou de nouvelles versions natives. Les rapports de comparaison conservent les écarts exacts par cas ; les buffers complets étaient des artefacts locaux, pas des données distribuées dans Git. L'acceptation est une décision distincte. Compilation Android, tests Editor, égalité de paramètres et observation visuelle ont chacun une portée différente.

Le [banc densité](../../../Validation/Quest/scientific-parity/density/comparison-results.json) couvre **neuf variantes × deux répétitions sur chaque plateforme**, soit 18 comparaisons Windows/Quest, source native `ffb7686`, MNI **69 104 sommets** pour la surface complète (certaines variantes utilisent des surfaces synthétiques réduites), grille **353 600 cellules**, huit contacts : maxima d'erreur absolue et de RMS (calculés séparément) pour l'activité **2,980232238769531×10⁻⁷ / 1,3842878707934336×10⁻⁸**, alpha **1,1920928955078125×10⁻⁷ / 7,538859779568176×10⁻⁹**. Le [banc iEEG](../../../Validation/Quest/scientific-parity/ieeg/comparison-results.json) couvre **18 fixtures × deux répétitions sur chaque plateforme**, soit 36 comparaisons Windows/Quest : maxima d'erreur absolue et de RMS pour l'activité **4,172325134277344×10⁻⁷ / 2,113383258724899×10⁻⁸**, alpha **1,7881393432617188×10⁻⁷ / 7,256360437146824×10⁻⁹**. Il s'agit de comparaisons de buffers numériques, pas d'une preuve d'égalité pixel à pixel ou de latence incluant NIfTI/export/GPU.

La campagne de [scènes multimodales](../../../Validation/Quest/scientific-parity/multimodal-scenes/measurement-context.json) révision 2 étend la recette à **six modalités**, MNI puis patient synthétique corrigé : **72 / 78 comparaisons**, **12 textures de coupe exactes par fixture**, maxima activité/alpha **3,5762786865234375×10⁻⁷ / 1,1920928955078125×10⁻⁷**. L'acceptation du **14 septembre 2026** couvre cette recette USB, pas Wi-Fi, des patients réels ou l'endurance. Un reliquat de **4 / 6 meshes Desktop vides par cycle** restait sans explication définitive ; aucune résolution ultérieure n'est déduite ici. Les premiers résultats `Passed` de T13/T15 avaient été invalidés parce que la Task était annulée : conserver les résultats finaux et le drainage effectif de la préparation native, pas le seul libellé du runner.

Les [résultats retenus](../../../Validation/Quest/retained-results.md) incluent les répétitions finales à chaud et après coupure, ainsi que les comptages de ressources. La recette patient avait d'abord été refusée visuellement : une transformation singulière provenait d'un fichier identité 4×4 interprété au format `.trm`, puis l'orientation des triangles avait dû être corrigée. La recette finale applique réellement `MNI.trm` avant déformation synthétique et conserve une validation visuelle distincte de la parité numérique.

## Transfert Small : septembre 2026

Référence `visu_full_test / Small`, Windows + Quest 3, Players IL2CPP, Unity 6000.5.2f1. Les premiers essais USB et Wi-Fi avaient des états d'alimentation/cache différents ; les mesures ne forment pas une étude statistique.

| État historique | Délai observé | Interprétation |
| --- | ---: | --- |
| Baseline Wi-Fi | 105,533 s | Livraison séquentielle, 48 553 entrées ZIP ; réception seule ≈11,81 s, incluant hashes/TLS/stockage. |
| Pack indexé, lot 2 | 59,914 s | Sept entrées au lieu de dizaines de milliers, sans suppression scientifique. |
| Snapshot/caches, lot 3 | 41,374 / 41,982 s | Préparation initiale anticipée distincte : 11,536 s. |
| HBT4/Zstd, lot 4 | 20,027 / 20,241 s | Préparation initiale distincte : 9,314 s Desktop + 5,371 s Quest ; reste 8,4–8,8 s après réception. |
| Version finale sans traces détaillées | 24,769 s, un envoi Wi-Fi | Small utilisable, mais **22–24 % plus lent que le lot 4**, cause non établie. Aucune absence de régression revendiquée. |

Le corpus codec isolé comportait **397 025 397 octets / sept ressources**, dont JSON 15 289 823 et pack 139 275 466. Zstd niveau 3, blocs 256 Kio : **125 219 971 octets**, encodage PC ≈1 406 ms, décodage Quest ≈608 ms ; LZ4 était plus rapide mais ajoutait environ 52 Mo. Ce microbenchmark explique le compromis choisi, sans prédire une latence réseau ou un autre corpus.

Les objectifs anciens « moins de 30 s », « 10–20 s » ou « réseau +3–5 s » étaient des cibles conditionnelles ; ils ne sont pas des garanties produit. Le gain du pack venait principalement du nombre d'ouvertures/allocations et de l'enveloppe, pas d'une preuve que tout le stockage ou tout le natif était intrinsèquement lent. Les allocations cumulées de plusieurs Go n'étaient pas une occupation simultanée de RAM.

## Display Small : 14 octobre 2026

Le nouveau délai court du clic Display au déverrouillage après préparation et synchronisation initiale. **Il n'est pas directement comparable au benchmark de transfert de septembre.** Sur USB, la campagne finale utilise trois nouveaux processus Desktop avec Quest conservé, contre cinq dans la baseline ; « froid » signifie nouveau processus Desktop, pas effacement du cache disque ou redémarrage du Quest.

| Cohorte | Médiane ouverture / réouverture |
| --- | ---: |
| Baseline Display | 13 453 / 11 823 ms |
| Implémentation finale mesurée | 13 141 / 10 511 ms |

Le snapshot atomique gardait environ **1 844 ms** de JSON médian ; pauses Desktop observées **2 247 / 1 578 ms**. La suppression de tous les gels n'est donc pas démontrée. Les rapports RSS processus, PSS et mémoire graphique `dumpsys` n'ont pas la même portée et ne s'additionnent pas. Une mémoire relevée en fin d'essai n'est ni un pic ni une preuve d'absence de fuite. Les mesures prolongées thermiques, énergétiques et les grandes charges restent distinctes d'une recette Small.

## Fixtures et preuves

| Référence | Ce qu'elle reproduit |
| --- | --- |
| [MNI anatomy](../../../Validation/Quest/fixtures/mni-anatomy/anatomy-fixture.md) | Une colonne anatomique, références MNI du dépôt, hashes et archive déterministe. Les petits GII de tests natifs nommés MNI sont synthétiques et ne représentent pas ce cerveau complet. |
| [MNI contacts](../../../Validation/Quest/fixtures/mni-contacts/contacts-fixture.md) | Huit contacts synthétiques, deux patients, noms volontairement répétés et IDs stables ; coordonnées natives mm puis conversion Unity X. Huit n'est pas une limite produit. |
| [MNI iEEG](../../../Validation/Quest/fixtures/synthetic-ieeg-fixture.md) | BrainVision synthétique préparé sur 151 échantillons/100 Hz, canaux absents/blacklist et temporalité explicités. |
| [Résultats de référence](../../../Validation/Quest/retained-results.md) | Comparaisons finales, acceptations et contexte, classés par sujet hors de `Docs/dev`. Les traces intermédiaires restent consultables dans Git. |
| [Laboratoire de sélection](selection-lab-guide.md) | Harness indépendant, six dispositions et métriques d'acquisition ; campagne limitée à un sujet, sans classement général des profils. |
| [USB](usb-and-network-connectivity.md) | Qualification datée Windows 11/Quest 3 ; macOS ARM64/Linux x64 et Quest 3S non couverts par cette preuve. |

Certains logs/XML/APK historiques sont uniquement référencés sous `.artifacts`, `.test-results` ou `Logs` ignorés ; leur absence d'un autre clone n'autorise pas à reconstruire leur résultat de mémoire. Les manifestes versionnés identifient la source, les binaires, les commandes et la portée quand ils existent.

Pour une nouvelle campagne, figer contenu, paramètres, versions et hashes ; distinguer cache processus/disque, première ouverture/réouverture, USB/Wi-Fi et temps anticipé/après clic. Corréler les durées locales sans soustraire deux horloges non synchronisées. Conserver erreurs, répétitions, dispersion, mémoire et frames ; une première frame CPU n'est pas une mesure command-to-photon. Les procédures opérationnelles restent dans le [skill de validation](../../../.agents/skills/hibop-unity-validation/SKILL.md).
