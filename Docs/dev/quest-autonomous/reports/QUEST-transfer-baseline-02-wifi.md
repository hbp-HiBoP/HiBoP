# Mesure Wi-Fi — Small — 16 septembre 2026

Le deuxième envoi est confirmé sur le LAN (`route=lan`), après saisie manuelle
de l'IP du Quest. Les deux traces ont le même identifiant de transfert et la
même empreinte d'archive. Résultat `Published`, caméra principale rendue en
stéréo, aucun événement de trace perdu. Même session applicative et mêmes
players IL2CPP non Development que le [premier essai USB](QUEST-transfer-baseline-01.md).
Calcul automatique désactivé, trois patients et trois colonnes.

**Clic → première fin de rendu CPU : entre 105,468 et 105,553 s.**
L'intervalle utilise les causalités d'envoi/réception, sans soustraire les
horloges système des deux appareils. Il reste soumis à une petite dérive
relative des chronomètres et ne mesure pas la présentation des photons.
Le Desktop termine son opération à **105,533 s**.

## Comparaison par phase

| Phase | Essai 1 : USB, premier chargement | Essai 2 : Wi-Fi, même session |
| --- | ---: | ---: |
| Capture/préparation Desktop complète | 24,096 s | 17,116 s |
| Dont anatomie manquante | 7,316 s | 0,003 s |
| Dont snapshot Unity | 2,826 s | 2,840 s |
| Dont ZIP | 13,119 s | 13,396 s |
| Deuxième hash complet avant envoi | 1,054 s | 1,074 s |
| Réception du payload Quest | 7,952 s | 11,808 s |
| Extraction/validation des ressources ZIP | 23,108 s | 36,277 s |
| Désérialisation JSON/buffers | 14,363 s | 16,839 s |
| Restauration Quest | 26,490 s | 21,961 s |
| Dont vérification des références standard | 3,276 s | 4,831 s |
| Dont premier chargement standard | 8,075 s | < 0,001 s |
| Dont chargement des trois IRM (provenance + natif) | 9,232 s | 14,187 s |
| Traitement/publication après réception complète | 64,138 s | 75,243 s |
| Plus grand intervalle entre frames Quest | 10,778 s | 16,459 s |
| Total Desktop jusqu'à fin d'opération | 97,286 s | 105,533 s |

Les lignes « dont » sont imbriquées ; ne pas additionner toute la table.
Le gain Desktop correspond au chargement d'anatomie déjà réalisé. Sur Quest,
`standardAlreadyLoaded=true` et `ensureInstalled`/chargement standard deviennent
quasiment gratuits : le cache fonctionne bien pour ces étapes.

En revanche, les hashes, l'extraction et le chargement natif sont plus lents :
IRM ×1,54, extraction ×1,57, vérification des références ×1,47, hashes des chunks
×1,53. Ce ralentissement affecte des traitements locaux, y compris après réception
complète. Il ne peut pas être attribué au seul Wi-Fi. Conditions de puissance,
fréquences CPU, température, activité concurrente et état mémoire sont des
hypothèses, pas des causes démontrées. Les deux essais ne constituent pas une
comparaison contrôlée du seul transport : caches et connexion USB ont aussi changé.

L’inspection ultérieure de `Volume.LoadNIFTIFile` montre que le scope IRM inclut
un SHA-256 avant et après l’appel natif. Ce n’est donc pas un temps natif pur.
Le [plan d’optimisation détaillé](QUEST-transfer-optimization-plan.md) développe
cette piste et les autres actions proposées.

## Transport Wi-Fi et limites

Archive : **143 194 967 octets** (143,2 Mo ; 136,56 Mio), soit pratiquement la
même taille que le premier essai. Réception en **11,808 s**, débit utile
**12,13 Mo/s**, soit **11,56 Mio/s** et environ **97 Mbit/s**.
Ce débit inclut traitement et stockage côté réception.

Dans les chunks reçus : hash par chunk **3,423 s**, hash global **3,434 s**,
lecture TLS **3,512 s**, attente des en-têtes **0,838 s**, écritures **0,551 s**.
Les deux hashes représentent à eux seuls environ **58 %** de l'intervalle de
réception. La lecture TLS inclut attente, chiffrement et ordonnancement ; elle
ne mesure pas exclusivement le temps radio. Optimiser uniquement la liaison
Wi-Fi ne résoudrait donc pas l'essentiel du délai actuel.

Après le transfert et le rebranchement USB pour la collecte : Wi-Fi 5 sur
5 GHz, débit radio annoncé 866 Mbit/s, RSSI −45 dBm. Ces valeurs sont des
instantanés après l'essai, pas un enregistrement pendant le transfert.
Même réserve pour l'état thermique Android 0, la température batterie 48 °C
et la mémoire TOTAL PSS 2 295 810 Kio. Aucun échantillonnage de fréquence CPU
pendant l'essai n'est disponible ; absence d'alerte thermique ne prouve pas
l'absence de réduction de fréquence.

## Priorités établies pour l'optimisation

1. **Extraction et petits buffers** : 48 553 entrées, dont 48 500 sous 4 Kio.
   L'extraction prend 36,277 s ; la désérialisation provoque encore 49 672 lectures
   de buffers (8,626 s, dont 7,075 s d'ouvertures). Réutiliser les buffers de
   travail, éviter les relectures de hash et regrouper les petites ressources
   sont les premières pistes. Conserver l'intégralité des contrôles d'intégrité.
2. **Reconstruction des IRM et gel du casque** : 14,187 s pour les volumes,
   16,459 s sans nouvelle frame. Séparer le traitement natif des opérations
   Unity, vérifier les contraintes de thread et la durée de vie des objets
   avant tout déplacement vers un worker.
3. **Vérification des données et transport** : les empreintes ont un coût
   significatif, à l'extraction, pour les références standard et pendant la
   réception. Étudier leur calcul en flux, leur implémentation et un cache
   invalidable plutôt que supprimer la vérification.
4. **Desktop** : ZIP autour de 13,4 s, snapshot autour de 2,84 s et deux hashes
   complets successifs. Les gains de cache ne réduisent pas ces trois coûts.

Les deux essais suffisent pour établir cette hiérarchie. Aucun nouvel essai
utilisateur n'est nécessaire à ce stade ; un prochain passage devra mesurer
des changements concrets, avec le chemin LAN et l'état d'alimentation conservés.
Aucune optimisation de production n'a été appliquée dans cette analyse.

## Preuves

Répertoire : `.test-results/quest-transfer/player-run-02/`.

- Desktop : `desktop/desktop-04d1fcb1cab049ae9c8416ab5b93c735.json`.
- Quest : `quest/quest-7090bd9b5359425daa1a13ba7e3128c3.json`.
- Transfert : `7903119ce40e4a5396375b394ac0a968`.
- SHA-256 : `3b1ee7e6814b2217d127a430a1fe63bd813f698ca3620a7340080c0af29224bd`.
- `analysis.txt` : toutes les métriques et intervalle causal.
- `run-setup.json`, `measurement-files.json` : contexte et empreintes des preuves.
- `desktop-player.log`, `after/logcat.txt`, `after/{wifi,thermal,battery,memory}.txt`.
  La collecte Quest a eu lieu après rebranchement USB, sans nouveau transfert
  ni redémarrage (PID Quest inchangé : 18733).
