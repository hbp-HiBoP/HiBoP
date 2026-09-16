# Résultat du lot 2 — Small, Wi-Fi

16 septembre 2026. L’utilisateur confirme une visualisation complète et utilisable,
**sans anomalie**. Un seul transfert neuf a été nécessaire.

Références : [implémentation du lot 2](QUEST-transfer-lot-2.md),
[mesure des lots 0–1](QUEST-transfer-result-03-lots-0-1.md),
[plan d’optimisation](QUEST-transfer-optimization-plan.md).

## Résultat mesuré

**Clic → confirmation Desktop : 59,914 s**, contre **86,592 s** avec les lots
0–1 : **26,678 s gagnées, soit −30,8 %**. La baseline Wi-Fi initiale était
105,533 s : l’écart cumulé observé est de **−43,2 %**, avec les réserves de
conditions et de chargement initial détaillées ci-dessous.

**Clic → première fin de rendu caméra CPU : [59,895 ; 59,922] s**.
Les bornes utilisent les débuts d’émission et les fins de réception des messages,
dont le reçu de publication ; aucune soustraction des horloges UTC des deux
appareils. Une petite dérive relative des chronomètres reste possible. Ce jalon
ne mesure pas l’instant où les photons sont présentés dans le casque.

Le lot 2 atteint son objectif structurel : les dizaines de milliers de petits
fichiers ont disparu. **L’objectif de 30 s et celui d’un réseau dominant ne sont
pas encore atteints.** La réception représente 8,157 s sur les 59,914 s mesurées,
soit 13,6 % ; elle comprend elle-même plusieurs secondes de calcul d’empreintes.

## Validité et conditions

- Players Release IL2CPP, `editor=false`, `developmentBuild=false`, marqueur
  `optimizationBatch=lots-0-1-2`, scène format 4 sur les deux appareils.
- Capture neuve : `retry=false`, route `lan`, issue `Published`, rendu stéréo,
  aucun événement perdu dans les deux traces. Même transfert et même hash.
- Small : trois patients, trois colonnes, calcul automatique désactivé,
  grille de projection 80 ; 1 669 106 sommets et 3 337 992 triangles capturés.
- Comme l’essai 03, premier transfert après démarrage des players, avec
  `standardAlreadyLoaded=false`. Le Quest a cette fois été rechargé et redémarré.
  La baseline 02 était à chaud ; elle n’est pas un contrôle strictement identique.
- Pendant les échantillons du nouvel essai : focus vrai des deux côtés,
  USB connecté, batterie Quest 83 %, température batterie **40–42 °C**,
  état thermique Android 0. L’essai 03 était à **48–50 °C**, batterie 39–37 %.
  La température batterie n’est pas la température du SoC ; ces données ne
  permettent pas d’isoler un effet thermique causal.
- Fréquences observées : `policy0` 1,920 GHz ; `policy2` 1,3824–2,3616 GHz.
  Ce sont des instantanés, pas la fréquence moyenne d’exécution de chaque phase.

## Comparaison des phases

Les détails sont imbriqués dans les lignes parentes : **ne pas additionner toute
la table**. Les écarts décrivent deux essais, pas une attribution causale parfaite
au code, notamment pour la réception et les traitements dont le code est inchangé.

| Phase | Lots 0–1 | Avec lot 2 | Lecture |
| --- | ---: | ---: | --- |
| Capture Desktop complète | 25,188 s | 24,133 s | Gain limité côté Desktop |
| Dont anatomie manquante | 8,143 s | 8,618 s | Premier chargement toujours présent |
| Dont snapshot | 2,769 s | 2,713 s | Presque inchangé |
| Dont création ZIP | 13,186 s | 11,851 s | −10,1 % |
| Dont empreinte finale | 1,073 s | 0,918 s | Archive plus petite |
| Réception du payload Quest | 12,235 s | 8,157 s | Moins d’octets et débit utile supérieur |
| Extraction et validation | 19,274 s | **7,442 s** | **−61,4 %** |
| Dont buffers binaires / nouveau pack | 13,262 s | **2,691 s** | Une seule entrée pour 48 549 buffers |
| Dont fichiers NIfTI | 4,873 s | 4,339 s | Trois entrées natives conservées |
| Désérialisation JSON et buffers | 12,993 s | **4,042 s** | **−68,9 %** |
| Dont lecture numérique | 6,737 s | **0,766 s** | Tableaux indépendants conservés |
| Dont ouverture fichiers / vues numériques | 5,988 s | **0,031 s** | 49 672 vues, aucun fichier par tableau |
| Résolution globale répétée | 1,469 s | **0,025 s** | Table validée séparément en 0,006 s |
| Décodage complet Quest | 32,392 s | **11,593 s** | Extraction, JSON et validation |
| Restauration complète Quest | 16,549 s | 15,726 s | Désormais dominée par les références initiales |
| Dont installation/vérification standard | 3,652 s | 3,578 s | Coût initial conservé |
| Dont chargement standard | 8,624 s | 8,148 s | Coût initial conservé |
| Dont chargement natif des trois IRM reçues | 2,494 s | 2,536 s | Pas de gain mesuré ici |
| Dont décodage des surfaces | 0,805 s | 0,600 s | 42 surfaces |
| Préparation/publication après réception | 48,966 s | **27,332 s** | −44,2 % |
| Plus grand intervalle de frames Quest | 3,637 s | 3,557 s | Gels persistants |
| Plus grand intervalle de frames Desktop | 2,791 s | 2,741 s | Snapshot encore bloquant |

## Effet du pack et des références compactes

| Indicateur | Lots 0–1 | Avec lot 2 |
| --- | ---: | ---: |
| Entrées ZIP | 48 553 | **7** |
| Buffers uniques | 48 549 | 48 549 |
| Octets des buffers uniques | 139 275 466 | 139 275 466 |
| Métadonnées de scène JSON | 34 379 717 octets | **15 289 960 octets** |
| Index du pack | — | 2 330 364 octets |
| Table globale | Références répétées dans le JSON | 36 entrées, 4 784 octets |
| Taille développée totale | 413 780 143 octets | **397 025 534 octets** |
| Archive compressée | 143 194 965 octets | **124 495 308 octets** |

Les sept entrées sont les quatre entrées du nouveau format et les trois fichiers
NIfTI. Les tailles des buffers et le total des ressources natives (240 124 960
octets) restent identiques ; la réduction développée provient des métadonnées.
Ces compteurs seuls ne constituent pas une comparaison bit à bit des scènes :
la parité est couverte par les tests de format/restauration et le contrôle visuel.

L’archive diminue de **18 699 657 octets, soit 13,1 %**. Le pack utilise un seul
flux ouvert (`pack.fileOpens=1`) et 49 714 vues : 49 672 lectures numériques et
42 surfaces. Le scope `quest.numeric.openFile` a disparu. Les 110 877 résolutions
globales passent par les indices de la table. Les empreintes des 48 549 plages
restent vérifiées pendant l’extraction, en **2,040 s**.

## Pourquoi il reste environ une minute

Trois scopes initiaux coûtent encore **20,344 s** : anatomie Desktop 8,618 s,
installation/vérification standard Quest 3,578 s et chargement standard 8,148 s.
Leur somme était 20,420 s dans l’essai 03 : le gain total n’est donc pas expliqué
par leur disparition.

Soustraire ces scopes donne **39,571 s**, contre 66,172 s dans l’essai 03.
Il s’agit d’une **normalisation arithmétique, pas d’un transfert à chaud mesuré**.
Même ce chiffre indicatif reste au-dessus du palier de 30 s.

Les principaux coûts à traiter sont maintenant :

1. **Préparation initiale et gels — lot 3.** Les références initiales dominent
   la restauration. Réutiliser les préparations dont la validité est démontrée
   et déplacer les travaux natifs compatibles hors du thread Unity ; conserver
   les contrats de propriété, d’annulation et de publication. Un travail avancé
   avant le clic devra être comptabilisé explicitement, sans le déclarer supprimé.
2. **Encodage Desktop — compression et pipeline des lots suivants.** Le ZIP
   prend encore 11,851 s : le pack coûte 4,642 s, dont 4,612 s de compression/
   écriture ; les trois fichiers natifs coûtent 7,043 s, dont 5,733 s de
   compression/écriture et 1,230 s de hash. Le retrait des entrées ZIP individuelles
   n’élimine donc pas le coût du codec, surtout pour les NIfTI.
3. **SHA et traitement de réception.** Réception en 8,157 s, **14,56 Mio/s** de
   débit utile, contre 11,16 Mio/s précédemment. Les hashes chunk + archive
   coûtent encore **4,107 s**, soit **50,4 %** de cet intervalle. TLS prend
   2,931 s, les attentes d’en-têtes 0,691 s et les écritures 0,393 s. Le provider
   reste `SHA256Managed`. Ce débit n’est pas le débit radio isolé ; le gain de
   réception n’est pas attribuable à la seule réduction de 13,1 % des octets.
4. **Extraction résiduelle et JSON.** Extraction + JSON totalisent encore
   11,484 s. Les hashes du pack et des NIfTI représentent déjà 5,252 s dans
   l’extraction ; la désérialisation restante prend 4,042 s. Accélération des
   primitives et chevauchement des étapes restent pertinents, sans supprimer
   les validations ni exposer une scène partielle comme publiée.

Environ **51,757 s restent en dehors de l’intervalle de réception** dans le
pipeline séquentiel actuel. Réduire uniquement le temps radio aurait donc
un effet limité. Le lot 3 est la suite cohérente du plan ; aucun nouveau test
utilisateur n’est nécessaire avant de nouveaux changements concrets.

## Instrumentation et mémoire

Les corrections de diagnostic fonctionnent : RSS Windows disponible sans erreur,
allocations du thread explicitement indisponibles (`-1`) sous IL2CPP, échantillons
poursuivis environ dix secondes après la fin, et jalon du premier rendu inclus
dans le maximum Unity. Coût agrégé des échantillonneurs : **2,5 ms Desktop** et
**100,1 ms Quest** ; cela ne mesure pas tout le surcoût de l’instrumentation.

Pics échantillonnés : RSS Desktop **2,524 Go**, RSS processus Quest **1,467 Go**,
mémoire allouée Unity Quest **145,645 Mo**. Le VmHWM Android atteint 1 465 080 Kio
sur la vie du processus. Les mesures managées et Unity ne sont pas additives
avec le RSS. Aucun gain de pic mémoire total n’est démontré par cette comparaison.

**Précision sur le rapport précédent :** le RSS de `/proc/self/status` et le
`TOTAL RSS` de `dumpsys meminfo` ne sont pas des compteurs interchangeables.
Le contrôle rapproché après cet essai donne respectivement **1 370 948 Kio** et
**2 785 508 Kio**, avec **1 463 416 Kio** dans la rubrique graphique de dumpsys.
La comptabilité graphique/driver explique une différence de périmètre importante.
L’écart cité dans le rapport 03 ne prouvait donc pas, à lui seul, une hausse tardive
du RSS du processus. La fenêtre prolongée était utile, mais elle ne remplace pas
un suivi de mémoire graphique ; ne pas comparer directement ces deux maxima.

## Preuves

Répertoire : `.test-results/quest-transfer/player-run-04/`.

- Desktop : `desktop/desktop-798b53f04c6b4eb78ae662f8ac7e3d77.json`.
- Quest : `quest/quest-4749f18c2341426f89db6e8b410e0ff1.json`.
- Transfert : `ca942c7a3dd74600965b859c63652fd8`.
- Archive SHA-256 : `edcb38e83acadfae70b51fabbf05bf61c561f4a8d5306f942bf727daa6fedc08`.
- `analyze.py`, `analysis.txt`, `comparison.json`, `summary.json` : calculs
  reproductibles, sélection des traces par hash et vérification du transfert.
- `run-setup.json`, `measurement-files.json` : contexte, confirmation utilisateur
  et empreintes des traces et journaux archivés.
- Journaux des players, `quest-meminfo-after.txt`, `quest-proc-status-after.txt`,
  `quest-meminfo-crosscheck.txt`, `quest-battery-after.txt` : contrôles après essai,
  à distinguer des échantillons enregistrés pendant celui-ci.

Le journal Android contient après la mesure, à 13:27:12–13, quatre messages
Unity indiquant que `xrDiscoverSpacesMETA` n’est pas disponible pour les extensions
OpenXR activées. Aucun blocage n’a été signalé par l’utilisateur ; ce diagnostic
XR distinct est conservé dans le journal et n’a pas été corrigé dans ce lot.

L’instrumentation reste en place pour les lots suivants. Aucun nouveau changement
de production n’a été appliqué pendant l’analyse de cet essai.
