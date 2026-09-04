# P11 — validation du pipeline timeline

## Résultat

- décisions P11-A–E : enregistrées dans l'ADR P11 ;
- interpolation P03 : PASS, sites linéaires et surface/coupes sample-and-hold ;
- atomicité, intégrité, latest-wins et stale-drop : PASS synthétique ;
- tests Desktop P11 : 12/12 PASS ;
- suite RenderModel incluant P03 : 17/17 PASS ;
- golden tests Desktop P03 : 2/2 PASS ;
- tests XR : 3/3 PASS ;
- gate performance timeline : FAIL, D20 rouvert avant production ;
- bundle partiel, quantification, compression et plafond : absents.

## Environnement et commandes

- date : 2026-09-04 ;
- OS : Windows 11 ;
- Unity : 6000.5.2f1 ;
- projet Desktop : C:\HBP\Software\HiBoP ;
- projet XR : C:\HBP\Software\HiBoP\XR ;
- répétitions benchmark : 20 par profil ;
- sorties brutes finales non versionnées : .test-results/unity-cli/p11-final/.

Le projet Desktop a exécuté les assemblies CRNL.HiBoP.RenderModel.Tests, CRNL.HiBoP.Protocol.Tests et HBP.Serialization.Tests avec le filtre P11. Le projet XR a exécuté CRNL.HiBoP.XR.Timeline.EditModeTests.

## Couverture fonctionnelle

| Preuve | Résultat |
| --- | --- |
| manifeste exact colonnes/surface/sites/coupes | rejet d'un élément absent, supplémentaire ou dupliqué |
| float32 little-endian | round-trip bit-identique des activités, opacités, positions et tailles |
| intégrité | modification d'un octet rejetée par SHA-256 avant retour d'un bundle |
| P03 | surface/coupes non sample-and-hold et sites non linéaires rejetés |
| latest-wins | un actif + un pending, pending intermédiaire remplacé, actif stale rejeté |
| autoplay logique | 6 000 demandes, profondeur maximale 1+1, convergence sur la dernière |
| scrub logique | 3 600 demandes, profondeur maximale 1+1, convergence sur la dernière |
| commit XR | échec de préparation conserve l'ancien bundle complet |
| retard/révision | une séquence retardée n'appelle pas la préparation et ne rollback pas |
| concurrence UI | play/pause/scrub encodés comme commande Timeline Desktop avec interaction/sequence |

Les scénarios autoplay/scrub sont des simulations déterministes de durée logique, pas une endurance murale ni un test Quest physique.

## Profils

- D2 1/3 colonnes : 69 104 valeurs de surface, 150 sites et un overlay 64×64 par colonne ;
- D3 8 colonnes : 69 104 valeurs de surface, 37 500 sites et un overlay 64×64 par colonne ;
- extraction : génération post-projection synthétique complète ;
- transfert : copie loopback mémoire, sans latence réseau ;
- prepare-upload : parcours et préparation des buffers, sans mesure du driver/GPU Quest.

## Mesures p50 / p95 / max

| Profil | Payload | Extract | Serialize | Copy | Decode | Prepare | Commit | End-to-end |
| --- | ---: | --- | --- | --- | --- | --- | --- | --- |
| D2 1 col. | 641 846 o | 5,303 / 9,778 / 11,796 | 62,479 / 85,112 / 100,642 | 0,067 / 0,212 / 0,249 | 55,454 / 64,112 / 84,323 | 8,786 / 10,625 / 13,353 | 0,003 / 0,007 / 0,207 | 132,360 / 179,207 / 194,900 |
| D2 3 col. | 1 925 358 o | 15,816 / 21,307 / 31,557 | 187,102 / 220,210 / 258,740 | 0,199 / 0,565 / 0,685 | 166,434 / 196,863 / 210,073 | 26,292 / 27,614 / 42,605 | 0,005 / 0,008 / 0,009 | 396,709 / 433,535 / 528,259 |
| D3 8 col. | 11 707 738 o | 93,966 / 96,527 / 222,953 | 1 165,901 / 1 423,482 / 1 430,089 | 1,325 / 4,620 / 64,578 | 1 013,310 / 1 038,297 / 1 077,684 | 120,002 / 129,797 / 133,638 | 0,006 / 0,007 / 0,008 | 2 406,476 / 2 652,031 / 2 655,718 |

Toutes les durées sont en millisecondes. Le p95 end-to-end est calculé sur chaque itération complète, pas par addition des percentiles de phases.

## Conclusion D20

Le p95 cible de 100 ms n'est pas atteint. Ce résultat interdit le passage en production de cette implémentation élément-par-élément, mais ne remet pas en cause les décisions d'atomicité et de fidélité. D20 est rouvert pour optimiser la baseline float32 contiguë, puis mesurer le vrai transport et l'upload Quest.

Aucune décision n'autorise à ce stade :

- un bundle partiel ;
- une quantification float16/8 bits ;
- une compression ;
- une suppression de surface, sites, overlays ou colonnes ;
- un plafond de cardinalité ou de fréquence modifiant le temps scientifique.

Les validations physiques Windows/réseau/Quest, 10 minutes d'autoplay mural, 60 secondes de scrub mural, mémoire, GPU et thermique restent requises avant un GO production.

La validation physique locale Quest et la borne réseau acquises ensuite sont consignées dans le [spike D20 timeline](../D20/timeline-quest-spike.md). Elles confirment que le contrat filaire complet actuel ne peut pas satisfaire 100 ms sans nouvelle décision.
