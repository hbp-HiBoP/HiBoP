# HiBoP XR — plan de validation

**Version :** 0.2  
**But :** démontrer fidélité, cohérence distribuée, performance, sécurité et utilisabilité sur Quest 3.

## 1. Niveaux de preuve

1. tests unitaires contrats, scopes et codecs ;
2. tests d'intégration host/client simulés ;
3. golden buffers et images Desktop/renderer indépendant ;
4. APK Quest sur réseau local réel ;
5. endurance mémoire/thermique ;
6. pilote utilisateur contrôlé.

Une mesure publie commit, build, appareil/OS, Unity/SDK, réseau, dataset, répétitions, warm-up, p50/p95/max, mémoire et logs redacted.

## 2. Datasets

| ID | Contenu | Usage |
| --- | --- | --- |
| D0 | géométrie synthétique minuscule, IDs connus | protocole, endian, picking déterministe |
| D1 | MNI gris réel, 69 104 sommets/138 216 faces | surfaces, payload, multi-instances |
| D2 | visualisation mono-patient réaliste avec sites/coupes | fidélité et UX courante |
| D3 | 250 patients × 150 sites = 37 500, 8 colonnes dynamiques | stress sans plafond |
| D4 | volumes/grilles et overlays les plus lourds disponibles | calcul, coupes, mémoire |
| D5 | signal synthétique avec `TemporalSample.Alpha` connu | interpolation surface/sites |
| D6 | sentinelles non sensibles dans noms/paths/payloads | redaction et purge |

Les datasets patient réels restent locaux et ne sont jamais joints aux rapports.

## 3. Matrice plateformes

| Composant | Windows | macOS | Linux | Quest 3 |
| --- | --- | --- | --- | --- |
| packages/contrats | tests | tests | tests | IL2CPP smoke |
| Desktop host | build/runtime | build/runtime | build/runtime | — |
| réseau | serveur | serveur | serveur | client |
| renderer | baseline | baseline | baseline | cible |
| Input Desktop | parité | parité | parité | — |
| XR/OpenXR | — | — | — | mains + contrôleurs |

## 4. Fidélité scientifique et visuelle

Pour chaque représentation V1 :

1. fixer projet, scope, paramètres, `TemporalSample` et asset revisions ;
2. capturer buffers après calcul Desktop ;
3. sérialiser/désérialiser par le contrat ;
4. rendre dans la scène indépendante puis Quest ;
5. comparer nombres, masques, textures, contours et image.

Critères :

- IDs, counts, dimensions, unités et repères exacts ;
- float32 bit-identique lorsque l'ordre/opération le permet, sinon tolérance documentée par fonction ;
- aucun pixel/vertex provenant d'une autre révision ;
- quantification seulement après erreur max/RMSE et image diff acceptées par propriétaire scientifique ;
- coupe finale identique au résultat Desktop pour le même plan ;
- D5 définit explicitement l'interpolation attendue et empêche une régression silencieuse.

## 5. Scénarios end-to-end

### E01 — premier appairage

Découverte disponible puis bloquée, IP manuelle, code correct/incorrect, identité changée, certificat expiré. Attendu : confiance explicite, erreurs distinctes, aucun état avant auth.

### E02 — snapshot initial

Projet avec plusieurs visualisations/colonnes/coupes/ROI. Couper la connexion à chaque phase. Attendu : ancien état cohérent ou nouveau snapshot complet, jamais partiel.

### E03 — anatomie et instances

Créer plusieurs instances MNI/patient, anatomical/inflated, transformations indépendantes, close/reopen. Attendu : hashes partagés, pose locale préservée seulement pour IDs valides.

### E04 — sites

D0 pour exactitude puis D3 pour charge ; ray, proche, hover, sélection, blacklist, échelle 10 cm–2 m, mains/contrôleurs. Attendu : 100 % IDs corrects sur targets déterministes, aucun site supprimé.

### E05 — coupe

Geste continu, changement de direction rapide, release, perte réseau, réponses artificiellement réordonnées. Attendu : plan local fluide, jamais de rollback, dernière coupe exacte.

### E06 — timeline

1/3/8 colonnes, autoplay, scrub, pause, vitesse, paramètres simultanés, D5. Attendu : bundles atomiques, backlog borné, tête XR stable.

### E07 — reconnexion

Coupures 1/5/30 s, host redémarré, projet changé, journal disponible/expiré. Attendu : resume ou snapshot annoncé ; nouvel epoch purge les résultats anciens.

### E08 — incompatibilité

Major, minor, schema hash et capability variants. Attendu : refus lisible pour incompatibles, négociation seulement pour combinaisons testées.

### E09 — vie privée

D6, background, kill, reboot, export logs. Attendu : aucune sentinelle patient dans stockage/log ; endpoint/empreinte seulement dans stockage autorisé.

### E10 — charge et endurance

D3/D4, passthrough puis VR, 30 minutes, cycles ouverture/fermeture. Attendu : pas de fuite, crash, dérive thermique non maîtrisée ou éviction silencieuse.

## 6. Performance

### Rendu XR

- cible baseline 72 Hz : CPU frame p95 et GPU frame p95 chacun < 13,89 ms ;
- mesurer dropped frames, App GPU time, main/render thread, draw calls et mémoire ;
- 90 Hz est un objectif secondaire après réussite 72 Hz.

### Sites

- 37 500 visibles ;
- picking p95 < 50 ms ;
- 100 % exactitude sur cas déterministes ;
- aucun GameObject/collider par site ;
- mise à jour complète et dirty range mesurées.

### Timeline

- p50/p95/max de chaque étape ;
- cible initiale command-to-visible p95 ≤ 100 ms en lecture courante ;
- convergence scrub ≤ 250 ms ;
- profondeur active/pending bornée à la politique ;
- colonnes synchronisées à 100 %.

### Coupes

- command-to-photon p50/p95/max ;
- cible initiale p95 ≤ 150 ms pendant interaction ;
- résultat final exact ≤ 250 ms après release ;
- taux de commandes coalescées, calculs annulés et résultats stale.

### Réseau/reconnexion

- contrôle p95 ≤ 100 ms même pendant bulk ;
- débit utile et retransmissions ;
- reprise/snapshot cible p95 ≤ 5 s sur réseau local nominal ;
- aucun buffer non borné.

Ces seuils sont des gates proposées D20. Tout ajustement est une décision documentée avec test utilisateur, pas une requalification rétroactive d'un échec.

## 7. Mémoire

Rapporter :

- mémoire système/Unity/GPU au démarrage ;
- assets immuables par hash ;
- coût marginal par brain instance et colonne ;
- buffers réseau/pools ;
- cache avant/après fermeture ;
- pic pendant snapshot/coupe/timeline ;
- mémoire et température à 5/15/30 minutes.

Attendu : surface partagée réellement une fois ; libération démontrée ; marge système documentée ; aucun patient persisté.

## 8. Robustesse protocole

- headers tronqués, longueurs extrêmes, type inconnu ;
- NaN/Inf, indices hors bounds, dimensions incohérentes ;
- chunk manquant/dupliqué/corrompu ;
- command duplicate/replay ;
- résultats out-of-order ;
- asset hash conflict ;
- snapshot interrompu ;
- serveur lent ou malveillant ;
- allocation bomb/compression bomb.

Attendu : rejet borné, erreur typée, aucune allocation incontrôlée, aucune modification partielle du renderer.

## 9. Interactions et accessibilité

Tâches : connecter, placer cerveau, changer échelle, sélectionner site, créer/orienter coupe, jouer/scrubber timeline, ouvrir panel, recentrer, récupérer après déconnexion.

Mesures par mains et contrôleurs : réussite, temps, erreurs, reprises, jitter, fatigue déclarée et préférence. Les contrôleurs doivent satisfaire les opérations précises même si les mains sont préférées pour l'exploration.

## 10. Build, distribution et licences

- builds reproductibles trois Desktop + APK ARM64/IL2CPP ;
- package lock et versions archivés ;
- symboles/debug séparés ;
- APK signé sans secret dans repo/log ;
- SBOM et notices Unity/Meta/native/vendor ;
- canal Meta pilote testé avec utilisateur non développeur ;
- update et rollback ;
- aucune augmentation Desktop non expliquée.

## 11. Go/no-go pilote

Go seulement si :

- D01–D18 résolues ou provisional avec gate passée ;
- D19 canal réel validé ;
- D20 mesuré et accepté ;
- E01–E10 passent ;
- fidélité propriétaire scientifique signée ;
- revue sécurité/vie privée/licences terminée ;
- defects restants classés sans contournement par plafond.

Un no-go conserve les artefacts hors Git sous `.artifacts/xr/` et déclenche une décision/spike ; il ne réduit pas silencieusement le nombre de sites, colonnes ou visualisations.
