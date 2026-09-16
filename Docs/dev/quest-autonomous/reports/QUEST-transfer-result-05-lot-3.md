# Résultat du lot 3 — Small, deux envois Wi-Fi

16 septembre 2026. Premier envoi confirmé **sans anomalie visible** par
l’utilisateur ; deuxième envoi terminé, sans anomalie supplémentaire signalée.
Les deux livraisons sont publiées et atteignent le rendu caméra stéréo.

**Mise à jour après analyse :** le défaut de préchargement identifié dans ce
rapport a été corrigé dans les sources ; voir le
[correctif et sa validation](QUEST-transfer-lot-3.md#correctif-post-benchmark-du-préchargement-desktop).
Les résultats ci-dessous restent ceux des players mesurés avant ce correctif.

Références : [implémentation](QUEST-transfer-lot-3.md),
[résultat du lot 2](QUEST-transfer-result-04-lot-2.md),
[plan](QUEST-transfer-optimization-plan.md).

## Résultat et limites de validation

| Mesure | Lot 2 | Lot 3, premier envoi | Lot 3, caches remplis |
| --- | ---: | ---: | ---: |
| Clic → confirmation Desktop | 59,914 s | **41,374 s** | **41,982 s** |
| Snapshot sur Unity | 2,713 s | **1,468 s** | **0,996 s** |
| Plus grand intervalle de frames Desktop pendant l’envoi | 2,741 s | **1,483 s** | **1,014 s** |
| Plus grand intervalle de frames Quest pendant l’envoi | 3,557 s | **0,557 s** | **0,561 s** |
| Réception du payload | 8,157 s | 8,390 s | 10,183 s |
| Réception, débit utile | 14,56 Mio/s | 14,15 Mio/s | 11,66 Mio/s |

Le délai après clic diminue de **18,541 s, soit 30,9 %** au premier envoi.
Mais **11,536 s de préparation ont été avancées à l’appairage** : les soustraire
du parcours utilisateur serait trompeur. L’addition des phases instrumentées
préparation + premier envoi donne **52,909 s**, soit **7,005 s / 11,7 %** de moins
que les 59,914 s précédentes. Cette somme exclut le reste de l’appairage et
l’attente humaine ; ce n’est pas une mesure directe d’un clic Pair → rendu.

**La validation de fluidité des players mesurés est partielle.** La cible de snapshot ≤ 1 s est atteinte
sur le deuxième envoi, mais pas sur le premier. Les gels multi-secondes ont
disparu de la fenêtre d’envoi Quest, mais une pause d’environ 0,56 s reste visible
dans la mesure. Surtout, l’analyse identifie un **chargement Desktop bloquant
pendant l’appairage**, détaillé plus bas. L’objectif de fluidité sur tout le
parcours n’est donc pas atteint. Le réseau n’est toujours pas le coût dominant.

Première fin de rendu caméra CPU, bornée sans synchroniser les horloges :
**[41,388 ; 41,405] s** au premier envoi, **[41,968 ; 41,997] s** au deuxième.
Les ancres sont les débuts d’émission/fin de réception des messages correspondants
et l’émission/réception du reçu. Ce jalon peut se produire après la confirmation
Desktop et ne mesure pas la présentation des photons dans le casque.

## Conditions et validité

- Deux nouvelles captures, `retry=false`, route `lan`, même processus Desktop
  **31064** et Quest **12142**, aucun nouvel appairage entre les envois.
- Players Release IL2CPP, `editor=false`, `developmentBuild=false`, marqueur
  `lots-0-1-2-3`. Identités de transfert et empreintes concordantes de chaque côté.
- Small : trois patients, trois colonnes, activité automatique désactivée,
  grille 80 ; 1 669 106 sommets et 3 337 992 triangles.
- Focus vrai pendant les deux opérations sur les deux appareils. La perte de
  focus Desktop enregistrée après le deuxième envoi commence **4,37 s après**
  sa confirmation, dans la fenêtre de suivi mémoire, pas pendant le transfert.
- Quest : batterie 100 %, USB branché, température batterie 40–42 °C au premier
  envoi et 40–45 °C au deuxième, état thermique Android 0. Le lot 2 était à
  40–42 °C et 83 %. Ce ne sont ni des températures SoC ni un contrôle thermique.
- Aucun événement perdu, aucune erreur diagnostique déclarée dans les traces.
  Les compteurs d’allocations par thread restent indisponibles sous IL2CPP.
- Le deuxième envoi remplace une scène déjà affichée : il ajoute son coût de
  rendu concurrent, de remplacement et de libération. Ce n’est pas une expérience
  isolant uniquement l’effet du cache. Deux observations ne donnent pas une variance.

## Préparation déplacée et calcul réellement supprimé

| Phase initiale | Lot 2, après clic | Lot 3, pendant Pair |
| --- | ---: | ---: |
| Anatomie Desktop manquante | 8,618 s | 6,391 s |
| Installation/vérification standard Quest | 3,578 s | 3,822 s |
| Chargement standard Quest | 8,148 s | **1,293 s** |
| Traces complètes de préparation Desktop + Quest | — | **6,415 + 5,120 = 11,536 s** |

La réutilisation du manifeste installé est confirmée. Le chargement standard
Quest diminue de **6,855 s**, cohérent avec la suppression des hashes redondants.
Le coût de vérification initiale des fichiers reste payé une fois. À l’envoi,
`standardAlreadyLoaded=true`, `standardInstalledHashesReused=true`, 31 références
réutilisées ; les scopes ensure/load ne coûtent plus que quelques microsecondes.
La préparation Quest n’a pas de gel mesuré : intervalle maximal **27,66 ms**.

### Défaut découvert dans la préparation Desktop

La trace Desktop de préparation dure 6,415 s mais **ne contient aucun intervalle
de frames**. Son premier échantillon de la boucle Unity arrive à 6,418 s, après
la fin de l’opération. Cela ne signifie pas zéro blocage.

L’inspection confirme le défaut : `LoadMissingAnatomyAsync` revient sur Unity
avant d’enregistrer les ressources. Or `MeshManager.AddPreloaded` construit
`LeftRightMesh3D/SingleMesh3D(..., load: true)` et `MRIManager.AddPreloaded`
construit `MRI3D(..., true)`. Ces constructeurs chargent immédiatement les
ressources natives, avant le passage au worker. Le scope de 6,391 s débute
et finit sur le thread 1. Le changement du lot 3 a ainsi déplacé ce chargement
bloquant à Pair ; il ne l’a pas rendu asynchrone.

**Correction demandée à l'issue de ce benchmark, désormais implémentée :** enregistrer les ressources
sans chargement immédiat, puis effectuer leurs chargements privés sur worker,
avec attente de fin et publication sur Unity. Conserver les garanties de fermeture
et éviter tout parallélisme GIFTI non réentrant. Un test automatique doit observer
la progression de la boucle Unity pendant ce préchargement froid.

Compléter aussi la mesure de frames pour enregistrer le premier intervalle
retardé même si l’opération se termine avant le premier passage de l’observateur.
Ces corrections ne sont **pas incluses dans les players mesurés** ici.

## Où passe le temps après le clic

Les lignes détaillées sont imbriquées : ne pas additionner toute la table.

| Phase | Lot 2 | Lot 3, envoi 1 | Lot 3, envoi 2 |
| --- | ---: | ---: | ---: |
| Capture Desktop complète | 24,133 s | 16,082 s | 16,006 s |
| Copie des surfaces Desktop | 1,129 s | **0,127 s** | **0,007 s** |
| Snapshot total sur Unity | 2,713 s | 1,468 s | 0,996 s |
| Dont parcours du graphe détaché | — | 1,324 s | 0,985 s |
| Encodage du snapshot sur worker | inclus dans l’ancien chemin | 1,906 s | 2,022 s |
| ZIP Desktop | 11,851 s | 11,781 s | 12,025 s |
| Hash final Desktop | 0,918 s | 0,903 s | 0,925 s |
| Réception Quest | 8,157 s | 8,390 s | 10,183 s |
| Dont hashes chunk + archive | 4,107 s | 4,189 s | 4,198 s |
| Dont lecture TLS | 2,931 s | 3,139 s | 4,723 s |
| Extraction et validation | 7,442 s | 8,089 s | 7,496 s |
| Désérialisation JSON/buffers | 4,042 s | 4,371 s | 4,169 s |
| Décodage complet Quest | 11,593 s | 12,566 s | 11,757 s |
| Restauration complète Quest | 15,726 s | **4,082 s** | **3,689 s** |
| Dont natif des trois IRM reçues | 2,535 s | 2,416 s | 2,430 s |
| Décodage des 42 surfaces Quest | 0,600 s | 0,637 s | 0,449 s |
| Préparation/publication après réception | 27,332 s | 16,671 s | 15,600 s |

Le déplacement sur worker améliore surtout la réactivité. Par exemple, snapshot
Unity + encodage worker représente maintenant 3,374 s au premier envoi contre
2,713 s pour l’ancien snapshot : il ne faut pas annoncer que tout ce travail
est devenu plus rapide. Le parcours JSON généraliste reste dominant sur Unity.

La restauration à 4,082 s profite surtout des références préparées pendant Pair.
À titre indicatif, le lot 2 moins ses scopes standard donnait déjà environ
3,999 s : cette soustraction est une normalisation arithmétique, pas un essai chaud.
Le chargement natif des IRM n’a pas disparu ; il n’immobilise plus Unity.

Les pauses Quest d’environ 0,56 s recouvrent le chargement des sites/colonnes,
la configuration, la mise à jour géométrique et les premières ressources de rendu.
Leur chevauchement temporel ne permet pas d’attribuer toute la pause à un seul
scope. Au deuxième envoi, une autre frame de 157 ms recouvre notamment la
libération de l’ancienne scène, mesurée à 121 ms.

## Caches, fidélité et mémoire

- Premier envoi : **42 misses Desktop et 42 misses Quest**, aucun hit.
- Deuxième : **42 hits de chaque côté**, aucun miss. Pas d’éviction signalée.
- Résidence des caches après remplissage : **100,04 Mio Desktop**, **112,77 Mio
  Quest**, budgets de 128 Mio chacun. Les hits n’envoient pas moins de bytes :
  il ne s’agit pas encore d’un protocole différentiel.
- La copie des surfaces Desktop gagne environ 120 ms à chaud ; le décodage de
  surfaces Quest gagne environ 188 ms entre les deux observations. Ces caches
  fonctionnent, mais leur bénéfice est modeste au regard des quelque 42 s totales.
- Le deuxième envoi gagne 1,185 s hors réception, tandis que la réception perd
  1,793 s : le total augmente finalement de 0,608 s. La lecture TLS/attente dépend
  du transport et de la disponibilité des deux applications ; elle n’isole pas
  le débit radio. On ne peut pas attribuer l’écart au seul Wi-Fi ni au seul cache.
- Format conservé : sept entrées ZIP, 48 549 buffers uniques, 139 275 466 octets
  de buffers ; 240 124 960 octets natifs. Métadonnées 15 289 823 octets.
  Archives : **124 501 260** puis **124 501 270 octets**, quasiment identiques
  au lot 2 (124 495 308). Ces tailles ne prouvent pas seules une parité bit à bit ;
  les tests automatiques et le contrôle visuel complètent cette vérification.

| Pic échantillonné | Envoi 1 | Envoi 2 |
| --- | ---: | ---: |
| RSS Desktop | 2,099 Gio | 2,495 Gio |
| RSS processus Quest (`/proc`) | 1,388 Gio | 2,068 Gio |
| Mémoire Unity Quest | 139,0 Mio | 169,6 Mio |

Le remplacement conserve temporairement l’ancienne scène et ses ressources.
Le pic plus élevé ne prouve pas une fuite ; deux envois ne suffisent pas pour
qualifier une dérive mémoire. Les allocations/GC et buffers temporaires doivent
être surveillés avant d’augmenter le parallélisme.

`dumpsys meminfo` après le deuxième envoi indique TOTAL RSS 3 985 068 Kio,
dont Graphics 2 622 936 Kio. Ce périmètre inclut des allocations graphiques
différentes du RSS `/proc` ; ne pas confondre ces deux indicateurs. Le premier
instantané, collecté avant le deuxième envoi, indiquait TOTAL RSS 3 679 804 Kio,
dont Graphics 2 305 900 Kio. Ce sont des instantanés postérieurs, pas des pics.

Le journal collecté contient quatre erreurs `xrDiscoverSpacesMETA`, après les
fenêtres de mesure, comme dans l’essai précédent. Aucun crash ou échec de
publication n’est observé ; ces messages sont conservés dans les preuves.

## Suite recommandée

1. **Terminer le lot 3** : corriger le préchargement Desktop bloquant et son angle
   mort de mesure ; réduire le parcours de capture généraliste encore exposé
   pendant 1–1,5 s. Utiliser des copies spécialisées respectant les identités et
   versions, sans sérialiser le graphe vivant sur un worker.
2. **Lot 4, priorité débit de préparation** : ZIP reste à environ 12 s, extraction
   à 7,5–8,1 s et JSON à 4,2–4,4 s. Comparer codecs et niveaux sur le corpus réel,
   améliorer le chemin SHA, puis chevaucher production/transfert/décodage avec
   des blocs indépendants et une mémoire bornée.
3. **Réduire le travail répété à chaud** : éviter de réencoder/recompresser des
   ressources inchangées avec un cache validé par contenu ; les caches de surfaces
   actuels ne suppriment ni compression, ni réception, ni intégrité entrante.
4. **Fluidité finale Quest** : répartir ou préparer autrement les associations et
   initialisations Unity qui produisent encore environ 0,56 s de pause, tout en
   conservant une publication atomique et complète.

Il reste **32,984 s**, puis **31,798 s**, hors de l’intervalle de réception.
La réception contient elle-même 4,19 s de SHA, soit 49,9 % puis 41,2 % de son
temps. Un transport plus rapide seul ne suffira pas à atteindre le but demandé.
Les deux envois réalisés suffisent à établir ces priorités ; aucun autre essai
manuel n’est demandé pour cette analyse.

## Preuves

Dossier : `.test-results/quest-transfer/player-run-05/`.

- `desktop/` et `quest/` : deux traces de transfert par appareil et leurs traces
  de préparation initiale ; `comparison.json`, `analysis.txt`, `analyze.py`.
- `desktop-player.log`, `quest-logcat.txt`, instantanés batterie/mémoire,
  `run-setup.json` et `evidence-hashes.json`.
- Envoi 1 : `b479735adb8b4eb0b1647cc65da4f630`, archive
  `772f2f5fa870ebb35e82e175fe93825a8deccb8de0c4699f7b0210eaefafd5bd`.
- Envoi 2 : `e05fa96f3e134cf8acf3c8719d502989`, archive
  `cf26c192728b9b3ac1648946bbe7c9571adf378d9b505bb5203d2a8580026b2a`.

L’analyse ne modifie pas les players mesurés ni leur instrumentation.
