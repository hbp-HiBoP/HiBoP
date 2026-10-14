# Display synchronisé Desktop–Quest — implémentation et qualification

Date : 14 octobre 2026. Implémentation, contrôles ciblés et recette physique terminés ; limites de couverture indiquées ci-dessous.

Une régression découverte ensuite pendant la recette utilisateur (crash éditeur
Small + Single) et l'admission trop stricte de Display sont corrigées et documentées
dans [le rapport de correctifs](QUEST-display-synchronise-correctifs.md).

## Comportement livré

`SceneDisplayOperation` possède le lot demandé par Display : un seul chargement
Desktop couvre les scènes locales, leurs snapshots, les transferts, la restauration
Quest et la préparation scientifique initiale. La progression agrégée ne régresse
pas et n'atteint 100 % qu'après confirmation de la libération du lot Quest.
Les transitions interpolent les valeurs reçues sur 250 ms Desktop / 350 ms Quest ;
elles n'inventent pas une estimation de temps restant.
Le chargement local garde ses poids par modalité/patient. Le lot synchronisé réserve
15 % au chargement local et 85 % aux publications, avec un poids de repli égal par
scène ; ces poids sont fixes pendant l'opération. Les jalons réels restent discrets.

Le Quest conserve une présentation pour tout le lot. Les entrées scientifiques
concernant ces scènes restent bloquées pendant sa préparation, tandis que suivi,
rendu, transport et PlayerLoop continuent. La barrière initiale de réplication est
complétée par un acquittement de préparation du rendu, corrélé à la politique de
projection appliquée sur le même transport ordonné.

L'admission précède l'ajout des scènes à la collection ; Tick et inventaires ne
peuvent donc pas lancer leurs propres envois. Une ouverture sans Quest reste locale,
avec une protection temporaire contre une synchronisation opportuniste au milieu
du chargement. Le contexte, le casque et la génération sont épinglés pour le lot.

Une panne Quest conserve les scènes Desktop terminées. Le résultat propose une
reprise explicite des scènes incomplètes, via le même coordinateur et ses heartbeats.
Les publications échouées ne repartent pas automatiquement. La reprise conserve
les reçus, journaux et offres encore exploitables ; un reçu incertain sans snapshot
reprenable demande une récupération explicite au lieu de créer une publication
concurrente. L'annulation draine les travaux possédés avant fermeture du chargement.

## Optimisations retenues

- Le JSON vérifié est analysé une seule fois côté Quest : désérialisation et manifeste
  réutilisent le même arbre, avec les mêmes validations et empreintes.
- Les convertisseurs sont résolus par contrat dans chaque opération de sérialisation,
  en conservant les contrôles par instance et par capture. Aucun cache permanent sur
  les objets scientifiques mutables n'est ajouté.
- Une scène suivante peut être chargée et son snapshot préparé pendant l'envoi et
  la restauration de la précédente. Une seule préparation en avance est admise.
  Son budget estimé est de 512 Mio, contrôlé avant copie numérique et avant clonage
  des surfaces ; une scène plus grande attend la fin de l'envoi puis se prépare
  séquentiellement. Le TCS de publication reste partagé pendant ce repli.
- Le producteur HBT4 libère sa fermeture de préparation après encodage des ressources.
  La file de deux blocs, le spool immuable et les reprises exactes sont conservés.
- Un bail de maintien d'activité évite la veille Desktop à 1 FPS pendant Display,
  sans modifier les préférences sauvegardées.

Le budget de snapshot est une estimation conservatrice des ressources détenues,
pas une limite de mémoire totale du processus. Il ne couvre pas toute la scène
Desktop, les caches préexistants, le moteur Unity ou les allocations natives.
La capture atomique du graphe vivant reste sur Unity ; déplacer cette capture sur
un worker aurait violé ses garanties de cohérence et d'accès aux objets.

La préparation incrémentale avant validation de l'archive et un changement de codec
ou de protocole de données ne sont pas retenus : les mesures désignent la préparation
JSON plutôt que le transport. Les formats d'archives ne changent pas. Les extensions
Display/rendu nécessitent de livrer ensemble Desktop et Quest.

## Référence et pilote intermédiaire

La référence est détaillée dans [le rapport du lot 0](QUEST-display-synchronise-lot0.md).
Les mesures sont effectuées sur Players Release Unity 6000.5.2f1, même fixture
`visu_full_test`, avec deux ouvertures par nouveau processus Desktop. Le processus
Quest est conservé entre cas sauf indication contraire.

| Cas | Référence détaillée, première / réouverture | Pilote sans préparation en avance |
| --- | ---: | ---: |
| Small USB | 15 257 / 12 432 ms | 14 237 / 10 572 ms |
| Small + Single USB | 16 966 / 14 803 ms | 18 058 / 16 207 ms |
| Small Wi-Fi | 13 608 / 12 867 ms | 13 090 / 11 560 ms |

Ce pilote (`implementation3`) a révélé la régression multi-scènes du parcours
entièrement séquentiel, d'où la préparation en avance ajoutée ensuite. Il ne
constitue pas la qualification finale de ce pipeline.

Sur Small USB, le manifeste Quest passe d'environ 2 024 / 2 004 ms à 311 / 329 ms.
Le décodage total passe de 6 148 / 6 251 ms à 5 270 / 4 783 ms. La capture JSON
Desktop reste coûteuse : environ 1,6–2 s dans ce pilote. Les freezes ne sont donc
pas supprimés. Ces cas isolés ne constituent pas une distribution statistique.

La frontière historique de la référence était le reçu de publication ; la nouvelle
frontière inclut aussi le rendu scientifique après politique et la libération du
lot Quest. Le champ historique `localDisplayReturnMs` attend désormais tout le lot ;
il ne doit plus être comparé comme une mesure du seul chargement local.

## Validation

- 72 tests EditMode d'archives, protocoles Display, blocs et snapshots : réussis.
  Incluent budget numérique avant copie, budget surfaces et repli sans changement
  des données scientifiques.
- 5 tests PlayMode de propriété : réussis, dont maintien de la présentation Quest
  jusqu'au drainage, génération/préférence, progression monotone, exclusion des
  reprises automatiques et conservation de la jointure pendant repli séquentiel.
- 2 tests PlayMode de restauration scientifique Desktop / Quest : réussis avec
  calcul automatique activé et désactivé, paramètres, calibrations, atlas, masques
  et modalités conservés. La série finale PlayMode totalise 7 réussites.
- Reconnexion de la barrière initiale et deux variantes de politique de projection :
  réussies isolément. Une exécution groupée a eu un timeout de politique ; sa relance
  isolée a réussi. Une fixture de reconnexion a été complétée avec sa scène manquante.
- Les séries antérieures ont également validé les mutations en ligne et les
  préférences de session ; leurs résultats ne remplacent pas une recette physique.

La série EditMode finale est le job `930ad22f243841fe97cf159855c06e57` (72/72),
la série PlayMode finale `b45ece1d400b406e9e0d20e63f493868` (7/7). Les résultats
complets sont enregistrés dans `implementation5/final-editmode.json` et
`implementation5/final-playmode.json`. Une relance du runner après rechargement
d'assemblies a nécessité de libérer son job orphelin avant cette série complète.
Le contrôle de dépendances passe : 44 assemblies, 162 arcs directs.
Aucun changement de référence d'assembly n'est nécessaire.
Les préfabs de chargement existants sont réutilisés ; les préférences utilisateur
et leur prefab ne sont pas modifiés.

## Limites à expliciter à la fin de la qualification

Les compteurs mémoire disponibles mesurent la mémoire Unity échantillonnée toutes
les 30 frames et la mémoire managée vivante. Ils ne constituent ni un total
d'allocations ni une borne des pics natifs. Ne pas additionner les phases asynchrones
qui se chevauchent et ne pas soustraire des horloges Desktop / Quest.

Le pilote physique n'avait montré aucun défaut visuel évident selon l'utilisateur,
mais ses scènes étaient fermées trop vite pour tester les meshes. La série finale
les a maintenues affichées temporairement pour cette vérification.

## Qualification physique finale — implementation5

Players Release construits ensemble depuis les corrections revues :

- Android : job `build-784a5395d2`, 80,76 s, zéro erreur, 49 avertissements.
  APK installé et hash comparé au fichier embarqué :
  `884fc8a7a5fc47e757666779bb4fd7073cf6063aaddc56cc3dd736e6253fdc64`.
- Windows : job `build-e436e6e287`, 156,72 s, zéro erreur, 47 avertissements.
  GUID `041a2ce6784741faa482b18f5a521b30`, GameAssembly SHA-256
  `32d014a612c18ec1639a703529c884a8ccc059596ed5f24858dec6e4a9c2eedf`.
  Données et outils ADB copiés par le workflow existant après BuildPlayer.

Les cinq nouveaux processus Desktop ont tous terminé avec code 0. Le Quest garde
le PID 23905 entre cas, après installation et lancement du nouvel APK. Les dix
ouvertures et douze publications ont réussi, sans scope Quest manquant.

| Cas | Première ouverture | Réouverture |
| --- | ---: | ---: |
| Small USB, médiane de 3 processus | 13 141 ms | 10 511 ms |
| Small USB, intervalle observé | 12 358–13 700 ms | 10 288–10 852 ms |
| Small + Single USB, 1 processus | 16 809 ms | 14 065 ms |
| Small Wi-Fi, 1 processus | 12 585 ms | 11 763 ms |

La médiane Small USB baisse d'environ 2 % / 11 % par rapport à la cohorte initiale
de cinq processus (13 453 / 11 823 ms). Le gain à froid est modeste et les intervalles
se recouvrent. Par rapport à la référence détaillée isolée, la baisse est de 14 % /
15 % ; ces échantillonnages différents ne donnent pas un intervalle de confiance.
La nouvelle frontière de fin est plus exigeante que celle de la référence.

Le lot multi-scènes améliore le pilote séquentiel de 7 % / 13 % et retrouve un temps
légèrement inférieur à la référence détaillée (16 966 / 14 803 ms). Dans le même
chronomètre Desktop, Small commence sa capture 2,96 / 3,21 s avant le reçu de la scène
précédente ; la capture se termine 1,95 / 2,33 s avant ce reçu. Les captures estimées
du lot représentent environ 76 Mio et 324 Mio, sous le budget d'avance de 512 Mio.

Sur les six ouvertures Small USB, le décodage Quest médian est de 5 032 ms : JSON
1 552 ms, graphe 3 238 ms, manifeste 307 ms. La capture JSON Desktop médiane reste à
1 844 ms. L'encodage/ressources sur worker est à 1 461 ms ; ces temps se chevauchent
et ne doivent pas être additionnés.

La médiane du maximum de pause Desktop est de 2 247 / 1 578 ms sur Small USB,
contre 2 733 / 2 154 ms dans la cohorte initiale. Le P95 des intervalles de frame
est de 17,06 / 16,76 ms, le P99 de 26,55 / 23,64 ms. La capture atomique explique
encore une pause sensible : ce chantier ne supprime pas tous les freezes.

La mémoire Unity maximale échantillonnée médiane est d'environ 216 / 218 Mio sur
Small USB. La mémoire managée vivante en fin d'ouverture varie approximativement
de 1,27 à 1,47 Gio. Les dumps Quest après chaque cas donnent 2,31–3,40 Gio PSS ;
ils ne sont pas des mesures du pic de préparation. Ces chiffres ne prouvent pas
l'absence de fuite et ne bornent pas la mémoire native.

L'utilisateur confirme que le cycle de chargement et la saisie/déplacement des
meshes étaient corrects pendant le maintien de douze secondes du lot. Un petit
défaut d'affichage des edges a été signalé comme sans rapport ; il est hors périmètre.

Les coupures physiques à chaque phase, la pression mémoire extrême et la parité
visuelle de toutes les modalités n'ont pas fait l'objet d'une nouvelle recette
sur casque. Les tests ciblés et les garanties de reprise ne remplacent pas ces
essais ; aucune réussite n'est revendiquée pour eux.

Traces, CSV, dumps et résumés : `.test-results/display-sync-20261014/implementation5`.
Exécutables : `.artifacts/display-sync-20261014/implementation5`.

## Correction finale de compatibilité de l'envoi manuel

L'inspection finale a repéré une restriction ajoutée involontairement : reprendre
manuellement une publication existante exigeait d'activer la synchronisation
automatique. La reprise explicite est maintenant admise avec cette option désactivée,
en épinglant sa valeur initiale ; un changement pendant l'opération invalide toujours
la tentative. Le parcours Display automatique conserve son admission habituelle.

Les 5 tests de propriété repassent après correction, dont la nouvelle assertion de
reprise manuelle avec option désactivée : job `0a9ea370bc024f55aaa58d9c0ff06ff2`,
résultat `implementation5/manual-retry-preference.json`. Le Desktop est reconstruit
dans `implementation6/windows`. Le protocole et le code exécuté sur Quest restent
ceux de l'APK `implementation5` installé et qualifié. Les mesures ci-dessus sont
celles du couple `implementation5`, avant cette correction limitée à l'admission
manuelle Desktop ; aucune nouvelle mesure physique n'est revendiquée pour elle.

Build Desktop final : job `build-020a3b747f`, 85,20 s, zéro erreur et 47 avertissements ;
GUID `4b2f424510b04c50a9ee5d52aaacdd2b`, GameAssembly SHA-256
`03f402bf0bfd138de3d0bc4194d1287b7fad8bbda135113ca47abe47d3ff864b`.
Les données et outils ADB sont empaquetés comme pour les Players mesurés.
L'éditeur est remis sur la scène HiBoP ; les changements automatiques de réglages
URP issus des builds ne font pas partie de la livraison.
