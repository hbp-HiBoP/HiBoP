# P12 — fiche de décision après FAIL D20

## Verdict

**FAIL explicite / arrêt P12 avant production.** Aucun `hbp_core` Quest, build PX2 ou port natif Android n'a été lancé.

La cible D20 est p95 ≤ 150 ms pendant le geste et convergence du résultat final ≤ 250 ms après release. Elle échoue sur une borne optimiste qui exclut déjà le calcul scientifique Desktop, le transport protocolaire réel, l'upload GPU Quest et l'attente de la frame visible. Ces coûts ne peuvent qu'augmenter le command-to-photon.

## Environnement et protocole

- date : 2026-09-07 ;
- Windows, Unity `6000.5.2f1`, test EditMode Mono ;
- base Git : `6c81fcedf93f2ffd2dfdd8c494d4c626a79d5b8c` plus changements P12 non commités ;
- datasets synthétiques autorisés : D2 courant en texture 512², D4 extrême 8 overlays en 2048² ;
- 10 répétitions D2 et 5 répétitions D4 dans le run reproductible final ;
- résultat déjà calculé, géométrie/base supposées en cache, aucune approximation ni compression ;
- borne réseau calculée avec le meilleur débit utile Quest P06 mesuré, `38,873 Mbit/s`.

## Mesures

Toutes les durées sont en millisecondes. Le codec hôte E2E inclut encode SHA-256, copie loopback, vérification SHA-256 et decode. Le réseau est une borne basse de transfert du seul payload, pas une mesure Wi-Fi P12 ni une revendication command-to-photon complète.

| Profil | Payload | Encode p50/p95/max | Copie p50/p95/max | Decode p50/p95/max | Codec hôte p50/p95/max | Réseau optimiste |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| D2, 1 colonne, 512² | 1 048 820 o | 70,712 / 89,004 / 89,004 | 0,223 / 0,309 / 0,309 | 72,203 / 89,441 / 89,441 | 151,271 / 169,007 / 169,007 | 215,845 |
| D2, 3 colonnes, 512² | 3 146 060 o | 204,881 / 241,662 / 241,662 | 0,325 / 0,825 / 0,825 | 203,668 / 207,713 / 207,713 | 411,223 / 445,565 / 445,565 | 647,454 |
| D4, 8 colonnes, 2048² | 134 218 280 o | 8 815,662 / 8 835,114 / 8 835,114 | 35,947 / 40,751 / 40,751 | 8 914,884 / 8 965,098 / 8 965,098 | 17 784,507 / 17 811,200 / 17 811,200 | 27 621,903 |

Le run exploratoire D4 à dix répétitions a atteint environ `3,6 Gio` de working set observé et dépassé le timeout NUnit de 180 s après avoir livré ses métriques. Le reproducer final, borné à cinq répétitions D4 avec un timeout explicite de 360 s, passe en `94,920 s`. Le FAIL D20 est indépendant de ce timeout : la borne réseau D2/1 dépasse déjà 150 ms et D2/3 dépasse déjà 250 ms.

## Couverture fonctionnelle acquise

- tests Desktop P12 : `2/2` PASS ; actif + pending latest, travail non annulable stale, redémarrage de séquence sur nouvelle interaction ;
- tests Protocol/XR P12 : `10/10` PASS ; contrat/codec/hash, corruption/budget, ordre retardé/dupliqué, erreur conservant l'ancien résultat, preload stable, mismatch de plan, ensemble de colonnes exact et prefab XRI ;
- validation finale P11 latest-wins + P12 Protocol/XR : `17/17` PASS ; benchmark D20 reproductible séparé : `1/1` PASS avec verdict de seuil FAIL ;
- run combiné P11/P12 : `67/68`, unique échec hors P12 causé par `RemoteAssetCacheTests` qui résout un chemin de source depuis `XR` au lieu de la racine du dépôt ;
- setup prefab P12 : PASS et log sans exception de sérialisation.

## Invariants non négociables

1. Desktop reste l'unique calculateur scientifique et autorité canonique.
2. Le dernier `(interactionId, sequence)` est calculé et seul son résultat complet peut être publié.
3. Aucun rollback, résultat mixte, suppression de colonne, quantification, paging ou cache scientifique persistant Quest.
4. Le preload P11 reste lossless, exact et admis au pic mémoire D23 ; changement de plan = invalidation et rechargement.
5. Un résultat précédent cohérent peut rester visible avec feedback pending/error, mais ne suit jamais le nouveau gizmo.

## Décision requise de P12-E

Le propriétaire du dépôt et le responsable scientifique doivent choisir explicitement une suite avant production :

1. **Recommandé — optimisation distante et contenu adressé.** Éviter de retransmettre les octets déjà présents : blobs geometry/base/overlay immuables par hash, manifeste atomique de références, cache RAM P08 et preload P11 stable. Pour le geste, ne transmettre que les nouveaux blobs indispensables et le commit final. Refaire ensuite la mesure Wi-Fi Quest command-to-photon 20–60 commandes/s et release. Cette option conserve Desktop et les résultats exacts.
2. **Accepter une UX asynchrone plus lente.** Conserver le gizmo fluide local et afficher explicitement le résultat scientifique comme pending, avec un nouveau seuil validé par test utilisateur. Cela exige une décision D20, pas une requalification rétroactive.
3. **Étudier PX2 ciblé.** Seulement après autorisation explicite séparée et selon `PX2-hbp-core-quest.md`, fonction par fonction, avec parité, confidentialité et bénéfice E2E démontrés. Cette fiche n'accorde pas cette autorisation.

Jusqu'à cette décision et une nouvelle preuve physique, P12 reste **NO-GO production**.
