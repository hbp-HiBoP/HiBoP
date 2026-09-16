# Résultat des lots 0 et 1 — Small, Wi-Fi

16 septembre 2026. Essai utilisateur terminé, visualisation complète et utilisable,
**aucune anomalie signalée**.

## Résultat mesuré

**Clic → confirmation Desktop : 86,592 s**, contre **105,533 s** pour la
[baseline Wi-Fi](QUEST-transfer-baseline-02-wifi.md) : **18,941 s de moins,
soit −17,9 %**.

**Clic → première fin de rendu caméra CPU : [86,754 ; 86,768] s**,
contre [105,468 ; 105,553] s. Ce jalon atteste un rendu caméra stéréo côté CPU,
pas l’instant de présentation des photons. L’intervalle provient des causalités
réseau et n’utilise pas de soustraction des horloges système.

La nouvelle exécution est un premier transfert après redémarrage des players.
La baseline Wi-Fi était un deuxième transfert dans la même session. Le nouveau
test supporte **20,420 s de préparation initiale** presque absentes de la baseline :

- Anatomie Desktop manquante : **8,143 s**.
- Installation/vérification initiale des références Quest : **3,652 s**.
- Premier chargement des références Quest : **8,624 s**.

Retrancher ces trois scopes du nouveau total donne **66,172 s**. C’est une
**normalisation arithmétique indicative, pas un transfert à chaud mesuré**.
Elle ne prouve ni un délai réel futur de 66 s, ni un gain causal de 37 % attribuable
au seul code. Fréquences, mémoire, caches et alimentation diffèrent aussi entre
les sessions. Le repère de 60 s du plan n’est donc pas atteint par cet essai.

## Validité de la mesure

- Players Release IL2CPP, hors éditeur et sans Development Build ; marqueur
  `optimizationBatch=lots-0-1` présent des deux côtés.
- Nouvelle capture (`retry=false`), route `lan`, publication `Published`, rendu
  stéréo observé, aucun événement de trace perdu.
- Même nom Small, trois patients, trois colonnes, calcul automatique désactivé,
  grille de projection 80. Même volume développé : **413 780 143 octets**.
- Archive **143 194 965 octets**, soit seulement deux octets de moins que la
  baseline. Toujours **48 553 entrées**, dont **48 500 sous 4 Kio**.
- Les deux nouvelles traces partagent l’empreinte
  `0a361fb5e131b8e00a2b4856c256efb5a3afa4bae1a3a1529b7b2415fa6b902c`
  et le transfert `53f7147581d04285805e9f1c50d1f1fb`.

## Comparaison par phase

Les lignes détaillées sont imbriquées dans leurs phases parentes : ne pas
additionner toute la table. Les pourcentages décrivent les écarts observés
entre ces deux essais, sans isoler parfaitement l’effet du code.

| Phase | Baseline Wi-Fi, à chaud | Lots 0–1, premier transfert | Lecture |
| --- | ---: | ---: | --- |
| Capture Desktop complète | 17,116 s | 25,188 s | +8,143 s d’anatomie initiale |
| Snapshot Desktop | 2,840 s | 2,769 s | Presque inchangé |
| ZIP Desktop | 13,396 s | 13,186 s | Presque inchangé |
| Empreinte finale Desktop | 0,838 s | 1,073 s | Conservée pour les envois |
| Deuxième empreinte avant envoi | 1,074 s | Supprimée | Réutilisation confirmée |
| Réception du payload Quest | 11,808 s | 12,235 s | Pas de gain réseau constaté |
| Extraction et vérification | 36,277 s | **19,274 s** | **−46,9 %** |
| Dont entrées binaires | 27,216 s | 13,262 s | Coût encore dominant |
| Dont fichiers NIfTI | 7,324 s | 4,873 s | Hash et inflation conservés |
| Désérialisation JSON/buffers | 16,839 s | **12,993 s** | **−22,8 %** |
| Dont lecture des tableaux | 8,626 s | 6,737 s | 49 672 ouvertures persistent |
| Dont ouverture des fichiers numériques | 7,075 s | **5,988 s** | 89 % de la lecture des tableaux |
| Restauration complète Quest | 21,961 s | 16,549 s | Inclut maintenant 12,277 s de références initiales |
| Vérification des références à la restauration | 4,831 s | **0,001 s** | 31 empreintes réutilisées |
| Chargement des trois IRM reçues | 14,187 s | **2,494 s** | **−82,4 %** ; ancien scope incluant deux SHA |
| Décodage des surfaces | 2,237 s | **0,805 s** | **−64,0 %**, upload natif inclus |
| Préparation/publication après réception | 75,243 s | **48,966 s** | −26,277 s, malgré les références initiales |
| Plus grand intervalle entre frames Quest | 16,459 s | **3,637 s** | **−77,9 %**, gel encore perceptible possible |

Le plus grand intervalle Desktop reste voisin : 2,865 → 2,791 s.

## Ce que les nouveaux compteurs confirment

1. **Extraction** : `extract.copyBufferAllocatedBytes=65536`. Le tampon de copie
   n’est plus alloué une fois par entrée. Pour ce jeu, cela retire environ
   **3,18 Go d’allocations cumulées de tampons**, pas 3,18 Go de mémoire résidente.
   La relecture complète de chaque fichier pour son hash a disparu.
2. **Archive finale** : `send.finalHashReused=1`, sans scope `send.fileHash`.
   Les contrôles par chunk et le contrôle global côté réception sont conservés.
3. **IRM** : `volume.provenance.reused=3`. Les appels natifs isolés coûtent
   **2,493 s** au total. Les deux recalculs SHA par IRM reçue sont absents.
   L’écart complet avec l’ancien scope ne peut toutefois pas être attribué
   exclusivement aux hashes : les conditions d’exécution ont changé.
4. **Références** : `standard.verifiedHashReused=31`. Le premier contrôle
   d’installation subsiste ; seule sa répétition à la restauration est évitée.
5. **Petits fichiers** : toujours 48 549 buffers binaires et 49 672 ouvertures
   numériques. Le lot 1 a réduit leur coût, pas leur nombre.

## Transport et coûts restants

Le payload est reçu en **12,235 s**, débit utile **11,16 Mio/s** (environ
93,6 Mbit/s). Ce n’est pas le débit radio isolé.

Les hashes de réception prennent **2,828 s par chunk + 2,797 s pour l’archive**,
soit **5,625 s**, environ 46 % de l’intervalle. La lecture TLS prend 5,142 s,
l’attente des en-têtes 0,861 s et les écritures 0,559 s. Les attentes TLS incluent
le producteur, le réseau et l’ordonnancement.

Le provider observé sur Desktop et Quest est **SHA256Managed**. Cela identifie
l’implémentation utilisée ; la trace ne démontre pas à elle seule les instructions
CPU effectivement exécutées. Un provider utilisant les primitives natives reste
une piste à évaluer avec tests d’intégrité et mesure sur appareils.

Les hashes de réception, dont l’algorithme n’a pas changé, sont environ 18 % plus
rapides que dans la baseline Wi-Fi. C’est une raison supplémentaire de ne pas
attribuer tous les écarts de phases à nos modifications.

Le pipeline reste loin d’être limité par le réseau : **extraction + JSON =
32,267 s** côté Quest, et **ZIP = 13,186 s** côté Desktop. Même en retirant
arithmétiquement la préparation initiale, environ 54 s du total restent en dehors
de l’intervalle de réception de 12,235 s. Ces durées ne mesurent pas un scénario
où les étapes seraient déjà mises en parallèle.

## Contexte et limites de l’instrumentation

Pendant le transfert Quest : 55 échantillons de puissance/focus et 62 échantillons
processus/fréquences. Focus vrai dans tous les échantillons des deux players.
USB connecté (`plugged=2`), batterie 39 → 37 %, température batterie **48 → 50 °C**,
état thermique Android **0**. Les fréquences lues vont de 1,3824 à 2,208 GHz pour
`policy2` ; `policy0` reste à 1,920 GHz. Ce sont des instantanés, pas des fréquences
moyennes des tâches, et l’état thermique 0 n’exclut pas une régulation de puissance.

Le coût propre des nouveaux échantillonneurs est agrégé : **130 ms sur Quest**
(43,9 ms worker, 85,8 ms thread principal) et **11,8 ms sur Desktop**. Ces scopes
ne mesurent pas le surcoût de toute l’instrumentation historique, ni tous les
effets indirects des allocations et du GC.

Trois limites sont établies et devront être corrigées au prochain build :

- **RSS Desktop indisponible** : `Process.WorkingSet64` produit
  `NotSupportedException` dans ce player IL2CPP. Les mesures périodiques processus
  Desktop manquent ; les jalons GC/managés et les mesures Unity restent présents.
  Prévoir un lecteur Windows compatible et séparer l’échec RSS des autres mesures.
- **Compteurs d’allocation par thread inutilisables** : tous les compteurs
  `GetAllocatedBytesForCurrentThread` valent zéro sur les deux players. Ne pas
  interpréter cela comme une absence d’allocations. Le compteur GC Quest augmente
  de 98 à 602, mais ses générations ne doivent pas être additionnées.
- **Fin de la fenêtre mémoire trop courte** : le pic RSS échantillonné Quest est
  de 1,387 Go, avant la première frame publiée. Après cette frame, la collecte ADB
  relève environ 2,39 millions de Kio PSS et 2,49 millions de Kio RSS. De même,
  le maximum Unity périodique (106 Mo) est inférieur au jalon de rendu (145 Mo).
  Ces maxima échantillonnés ne sont donc pas des pics mémoire de bout en bout.
  Étendre la collecte après le premier rendu et intégrer les mesures des jalons
  dans les maxima. Aucune réduction de mémoire maximale n’est démontrée ici.

Précision établie lors de l’[essai du lot 2](QUEST-transfer-result-04-lot-2.md) :
`/proc/self/status` et `dumpsys meminfo TOTAL RSS` ont des périmètres différents,
notamment pour la mémoire graphique/driver. Leur écart ci-dessus ne suffit donc
pas à démontrer une hausse tardive de mémoire résidente du processus. L’extension
de la fenêtre reste utile, mais ces compteurs ne doivent pas être comparés
directement comme deux mesures du même pic.

Les traces sont exploitables pour les temps et les chemins optimisés. Aucun
nouvel essai utilisateur n’est nécessaire uniquement pour ces limites de diagnostic.

## Suite recommandée

**Priorité : lot 2, pack binaire indexé.** Regrouper les petits buffers et lire
par offsets pour supprimer les dizaines de milliers de créations/ouvertures et
leurs contrôles répétés par fichier, tout en conservant une validation d’intégrité
complète. Les coûts encore mesurés justifient directement cette étape.

Conserver ensuite les priorités du plan : réduire la préparation initiale et
sortir les travaux natifs adaptés du thread principal, puis évaluer hashing,
compression et chevauchement des étapes. Les améliorations IRM et de provenance
du lot 1 sont effectives ; elles ne justifient plus de traiter les 14 s de l’ancien
scope IRM comme un coût incompressible du lecteur natif.

Pas de nouveau transfert demandé à ce stade. Un futur essai devra mesurer le
prochain lot concret et distinguer explicitement premier envoi et état à chaud.

## Preuves

Répertoire : `.test-results/quest-transfer/player-run-03/`.

- `desktop/desktop-7c93f64bc68c4a61accb40e5143b306a.json`.
- `quest/quest-c8fe490c7c8548a3a5d217bc078ef0c6.json`.
- `analysis.txt` : analyse standard et borne du premier rendu.
- `comparison.json` et `compare.py` : sélection de la baseline Quest **par hash
  correspondant au Desktop**, tableaux et synthèse des échantillons.
- `after/` : logcat, mémoire, batterie, thermique et Wi-Fi après essai.
- `measurement-files.json` : empreintes des fichiers conservés et confirmation utilisateur.

La baseline sélectionnée est bien `quest-7090bd9b5359425daa1a13ba7e3128c3.json`.
Le dossier historique contient aussi la trace du premier essai USB : ne pas
sélectionner simplement le premier fichier de ce répertoire.
