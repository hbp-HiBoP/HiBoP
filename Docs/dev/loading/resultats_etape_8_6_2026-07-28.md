# Résultats de l'étape 8.6 — scheduler adaptatif

## Périmètre

La campagne finale a été exécutée le 28 juillet 2026 dans Unity 6000.5.2f1,
Editor Mono, sur une machine de 20 processeurs logiques. Elle utilise le
workspace `Default` et, puisque `full_test` n'est pas présent sur cette
machine, le projet réel `visu_full_test.hibop`.

La matrice couvre 1, 2, 4, 8 et 20 workers. Après un passage de chauffe écarté,
chaque combinaison base/projet a été exécutée trois fois. Les valeurs
ci-dessous sont les médianes des 30 mesures chaudes.

## Médianes

| Opération | Workers | Ready | Validated | CPU | Pic mémoire managée | GC 0/1/2 |
|---|---:|---:|---:|---:|---:|---:|
| Base | 1 | 4,09 s | 124,31 s | 6,42 s | 61,2 Mio | 20/20/20 |
| Base | 2 | 3,69 s | 64,01 s | 7,80 s | 31,1 Mio | 22/22/22 |
| Base | 4 | 2,98 s | 24,72 s | 25,12 s | 68,0 Mio | 24/24/24 |
| Base | 8 | 3,02 s | 10,82 s | 35,20 s | 35,0 Mio | 27/27/27 |
| Base | 20 | 4,72 s | 11,30 s | 45,44 s | 56,2 Mio | 20/20/20 |
| Projet | 1 | 4,18 s | 113,15 s | 5,36 s | 144,3 Mio | 12/12/12 |
| Projet | 2 | 3,49 s | 58,17 s | 7,94 s | 150,2 Mio | 12/12/12 |
| Projet | 4 | 2,29 s | 16,04 s | 36,31 s | 154,8 Mio | 16/16/16 |
| Projet | 8 | 2,74 s | 9,69 s | 30,70 s | 152,5 Mio | 14/14/14 |
| Projet | 20 | 4,01 s | 9,80 s | 37,53 s | 141,9 Mio | 11/11/11 |

Le niveau 8 fournit le meilleur temps `Validated` mesuré. Le niveau 20 ne
l'améliore pas et dégrade nettement `Ready` et le coût CPU. Le niveau 4 reste
utile pour les travaux moins parallélisables ou plus coûteux par unité.

## Politique retenue

| Catégorie | Plafond |
|---|---:|
| Parsing JSON et ZIP | 8 |
| Validation de chemins | 8 |
| Validation de métadonnées | 4 |
| Appels natifs `hbp_core` | 2 |
| Budget global partagé | 8 |

Ces plafonds restent bornés par `Environment.ProcessorCount`. La désactivation
du multithreading force toutes les catégories à 1. La variable interne
`HIBOP_LOADING_CONCURRENCY_OVERRIDE` permet également de forcer une valeur de
diagnostic, et `HIBOP_BACKGROUND_VALIDATION=false` restaure le mode bloquant.

La campagne de démarrage mesure les chemins `Ready` et `Validated`, mais ne
contient pas un corpus natif représentatif pour éprouver agressivement
`hbp_core`. Le plafond natif de 2 est donc volontairement conservateur compte
tenu de l'instabilité observée pendant les étapes antérieures.

## Validation et cleanup

- matrice : 30/30 mesures, durée 40 min 39 s, sans erreur ;
- tests EditMode `HBP.Serialization.Tests` : 452/452 réussis, console Unity
  sans erreur ;
- scheduler global : budget partagé base/projet, priorité foreground dynamique,
  annulation des travaux en file et ordre des résultats testés ;
- valeurs `20` de production supprimées au profit de la politique centrale ;
- instrumentation temporaire, assembly de benchmark et tests associés retirés
  après la campagne conformément au plan de cleanup.
