# Display synchronisé — mesures de référence

Date : 14 octobre 2026. Mesures en cours ; aucun gain annoncé avant comparaison.

## Conditions et identité

- Source de départ : `feature/xr-autonomous`,
  `16a6a6cf4e12ff28f8ff95ab0963f7c0a41aa8a5`, complétée uniquement par
  l'instrumentation et le diagnostic du lot 0 avant modification du parcours.
- Unity : `6000.5.2f1 (eb73d3b415a1)`.
- Casque : Quest 3, transport USB ADB pour la première série.
- Fixture : `visu_full_test.hibop`, visualisation `Small`, trois patients et trois
  colonnes. SHA-256 de l'archive projet :
  `3700b7093cdafaf20b1055de40e55f7a2f57e02e7b801f96eb78cec58c87df13`.
- Dossier des données brutes : `.test-results/display-sync-20261014/lot0`.
  Builds : `.artifacts/display-sync-20261014/lot0`.

Le diagnostic ouvre le vrai prefab Visualization gestion, sélectionne Small et
attend son action Display. L'auto-synchronisation reste active. Il ne remplace pas
ce parcours par une capture ou un envoi manuel. Il refuse de commencer avec des
scènes déjà ouvertes et attend que le pairing soit terminé.

Une première passe dans l'éditeur est une première ouverture **dans la session
Play**, pas une preuve de processus froid. Une réouverture conserve les caches
normaux de la session. Un processus Release relancé constitue un autre scénario ;
il ne constitue pas une purge des caches disque du système.

## Mesures et limites

Le diagnostic relève séparément le retour de Display local, la préparation du
rendu Desktop et la fin de publication avec fermeture de sa présentation. Il
échantillonne les intervalles de frames, dont maximum et percentiles, la mémoire
Unity allouée toutes les trente frames et la mémoire managée vivante avant/après.
Ces mesures mémoire ne sont pas des compteurs exhaustifs d'allocations et leur
pic peut manquer un transitoire entre deux échantillons.

`PairingTiming` distingue maintenant attente de l'initialisation, anatomie,
représentation de surface, gate de capture, générateurs, corrélations et colliders,
puis copie du snapshot, encodage, reconstruction native Quest, configuration,
préparation du rendu et synchronisation initiale. Les durées asynchrones sont des
temps écoulés incluant les attentes. Elles ne sont pas du temps CPU et les spans
imbriqués ou chevauchés ne doivent pas être additionnés.

Les traces Sync existantes conservent les jalons de capture, octets et reçu de
publication. Comparer les durées de chaque appareil avec son horloge propre ; ne
pas soustraire directement les timestamps Desktop et Quest.

Inspection préalable : l'ACK initial de réplique suit l'application du checkpoint,
mais la politique de projection est envoyée ensuite. La fin de publication
actuelle n'est donc pas une garantie générale de rendu scientifique stable après
cette politique. Le lot 2 doit compléter cette frontière avant d'afficher 100 %.
Ne pas rebaptiser rétroactivement cette mesure comme une garantie déjà existante.

## Première série Release USB — Small

Les cinq processus Desktop neufs ont chacun ouvert Small puis fermé et rouvert
la scène. La veille Desktop était suspendue via l'API de benchmark existante,
avec une cible de 60 FPS et une fenêtre 1280 × 720. Le Quest a été relancé avant
le premier processus Desktop puis conservé pour les quatre suivants : il ne faut
pas présenter ces cinq premières ouvertures comme cinq processus Quest froids.

| Mesure Desktop | Première ouverture : médiane [min ; max] | Réouverture : médiane [min ; max] |
| --- | --- | --- |
| Retour du chargement local | 1 411 ms [1 390 ; 2 212] | 331 ms [315 ; 352] |
| Rendu local préparé | 1 618 ms [1 597 ; 2 366] | 463 ms [439 ; 534] |
| Fin du chargement et de la publication actuels | 13 453 ms [13 147 ; 13 999] | 11 823 ms [11 058 ; 12 776] |
| Plus grand intervalle de frame | 2 733 ms [1 691 ; 3 049] | 2 154 ms [1 976 ; 2 236] |

Les dix ouvertures ont produit un résultat de succès. Les quatre processus du
runner ont aussi fourni un code de sortie 0 ; le premier, lancé séparément, a
laissé un log de fermeture normale mais son code de sortie n'a pas été conservé.
Les durées incluent le parcours actuel, dont la frontière de publication décrite
plus haut ; elles ne prouvent pas encore une barrière scientifique distante finale.

Le premier couple transfère environ 12,2 Mo par scène, sur 0,5–0,6 s entre premier
et dernier octet écrit. Son reçu arrive environ 6–7 s après le dernier octet. Les
sondes Quest montrent environ 5–6 s de décodage/validation et moins d'une seconde
de reconstruction et préparation commune du rendu. La capture synchrone Desktop
reste un autre poste majeur, avec des pauses de l'ordre de 2 s. L'encodage et
l'ouverture de la réplique sont également mesurés séparément dans les logs.

APK installé et vérifié sur le casque : SHA-256
`19202f26f0dcbcfb8f2f0ea67d6e2d991ceddb266782bd970018dbc05d52832f`.
Build Windows : GUID `34b9160fe5e04ce9861614636354b9ee` ; les hashes des exécutables,
plugins et de la fixture sont conservés avec les données brutes.

La première tentative dans l'éditeur est conservée dans `editor-usb`, mais exclue
du verdict : sa deuxième passe a déclenché le mode veille à 1 FPS. Ce diagnostic a
été corrigé avant le build Release, sans modifier la préférence utilisateur.

Une sonde de décodage manque pour la première scène du deuxième processus : le
buffer logcat l'avait déjà écrasée, et le filtre temporel initial du runner n'a
pas conservé sa copie. Les timings Desktop correspondants sont complets. Le filtre
et son contrôle d'erreur ont été corrigés ; aucune valeur Quest n'est inventée pour
combler ce trou. Les captures complètes du premier processus et du reste de la
série restent disponibles.

Les timings des rapports anciens ne sont pas réutilisés comme baseline.

## Sondes détaillées et cas complémentaires

La seconde paire de Players Release est conservée dans
`.artifacts/display-sync-20261014/lot0-fine`. L'APK installé a pour SHA-256
`64978fdc5cad2cfa85b55ef181ad1bf654ccad366f5e3ecd2ee379862cb69929`.
Les trois cas ont chacun exécuté deux ouvertures dans un nouveau processus
Desktop, avec succès et code de sortie 0. Les scopes Quest sont tous présents.

| Cas | Première ouverture | Réouverture |
| --- | ---: | ---: |
| Small, USB prioritaire | 15 257 ms | 12 432 ms |
| Small + Single, USB prioritaire | 16 966 ms | 14 803 ms |
| Small, endpoint Wi-Fi explicitement sélectionné | 13 608 ms | 12 867 ms |

Ces valeurs incluent l'instrumentation détaillée ; ne pas les mélanger aux
médianes de la première cohorte. Le casque reste le même processus entre les
trois cas. L'endpoint Wi-Fi est `192.168.1.18` ; ADB reste relié en USB pour les logs.

Sur Small USB, la construction du journal prend 15/7 ms et la capture du modèle
et des ressources 6/3 ms. La capture des tokens JSON prend 2 131/2 326 ms. Elle
copie 22 028 944 octets dans 49 672 buffers ; le JSON fait 15 276 885 octets.
Côté Quest, le graphe d'objets prend 4 010/4 157 ms, la validation 108/88 ms et
le manifeste 2 024/2 004 ms, pour un décodage total de 6 148/6 251 ms.

Le manifeste relit intégralement le JSON qui vient d'être désérialisé. Réutiliser
le même arbre JSON est donc la première optimisation à qualifier, en conservant
exactement les hashes et validations. La capture JSON synchrone et les conversions
par type sont le second axe à mesurer. Les lectures des 49 672 petits buffers
sont une hypothèse à instrumenter, pas encore un gain prouvé. Le transport et la
reconstruction native ne justifient pas une refonte au vu de cette référence.

Les données brutes, logs, CSV et résumés par cas sont conservés dans
`.test-results/display-sync-20261014/lot0-fine`. Les médianes de sous-phases d'un
lot Small + Single ne doivent pas être présentées comme des mesures de Small seul.
