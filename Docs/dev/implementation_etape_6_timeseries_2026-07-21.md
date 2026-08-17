# Implémentation de l’étape 6 — budget mémoire et cycle de vie

Date : 21 juillet 2026

## Résultat

`MemoryCacheLimit` pilote maintenant un budget commun aux données brutes, aux dérivés managés et aux buffers natifs de projection. Une valeur explicite est interprétée en Mio. La valeur `0` applique la formule automatique proposée par l’audit : le minimum entre 90 % de la RAM physique et la RAM totale moins 2 Gio.

Le gestionnaire de budget :

- comptabilise chaque entrée avec sa catégorie, sa taille, son état épinglé et sa dernière utilisation ;
- évince en priorité les dérivés managés inactifs, puis les projections natives et enfin les enregistrements bruts froids ;
- départage les entrées d’une même catégorie par ancienneté LRU ;
- n’évince jamais une entrée active ;
- signale explicitement un dépassement causé uniquement par des entrées actives, sans sous-échantillonnage ni altération des données.

Les visualisations iEEG et CCEP épinglent leur enregistrement brut pendant leur cycle de vie. Le déchargement le désépingle et permet son éviction si le budget est dépassé. Les tableaux préparés des visualisations actives et les buffers natifs des colonnes 3D sont également enregistrés comme actifs puis retirés lors de leur destruction.

Les époques, statistiques et événements conservés par `DataManager` sont comptabilisés comme dérivés. L’éviction d’une entrée inactive la retire du cache et vide explicitement ses tableaux afin de libérer leurs références. Les objets utilisés par une visualisation active restent épinglés et ne sont donc jamais vidés. Le prochain accès à une entrée évincée reconstruit le dérivé à partir du brut encore chaud, ou recharge le brut si celui-ci a lui aussi été évincé.

Après l’ouverture d’une visualisation, un dépassement dû aux seules données actives affiche maintenant une boîte de dialogue avec la mémoire HiBoP comptabilisée et la limite configurée. Les données restent exactes. Lors d’un rechargement provoqué par un changement de normalisation, l’éviction est suspendue pendant la transition afin que le brut ne soit pas relu depuis le disque.

Le `GC.Collect()` synchrone a été retiré du chemin normal de `DataManager.Clear` : la limite est désormais appliquée par une politique explicite plutôt que par une collecte forcée.

## Tests automatisés

`Stage6MemoryBudgetTests` couvre dix scénarios :

- conversion exacte d’une limite explicite ;
- formule automatique sur trois tailles de RAM simulées ;
- ordre d’éviction par catégorie ;
- ordre LRU au sein d’une catégorie ;
- dépassement par une entrée active avec avertissement et sans éviction ;
- éviction différée au désépinglage ;
- réutilisation d’un enregistrement brut chaud puis reconstruction après éviction froide ;
- rejet d’une taille négative.

Validation ciblée : **10 tests réussis sur 10**. Validation cumulée après l’étape 6 : **339 tests réussis sur 339** dans `HBP.Serialization.Tests`.

## Tests manuels conseillés

1. régler une limite basse, ouvrir plusieurs visualisations successives et vérifier que les données inactives sont évincées tandis que la visualisation courante reste exacte ;
2. revenir immédiatement sur une visualisation dont le brut est encore chaud et confirmer l’absence de relecture disque ;
3. provoquer l’éviction du brut froid puis revenir sur la visualisation et confirmer sa reconstruction complète ;
4. charger une visualisation active plus grande que le budget et vérifier l’avertissement sans réduction silencieuse ;
5. profiler dix cycles ouverture/fermeture et vérifier le plateau des catégories comptabilisées ;
6. comparer les valeurs, statistiques et projections avant/après une éviction.

## Gain attendu

Le gain principal est une **borne sur les caches inactifs**, plutôt qu’une économie fixe : après stabilisation, la mémoire comptabilisée non active est ramenée sous `MemoryCacheLimit`. Avec la valeur automatique, HiBoP conserve au moins 2 Gio de marge système et n’utilise jamais plus de 90 % de la RAM physique pour ces caches.

Le rechargement reste instantané tant que le brut tient dans le budget. Sous pression, les dérivés les moins coûteux à reconstruire partent d’abord ; les lectures disque ne reviennent qu’après éviction des bruts froids. La suppression de la collecte forcée retire aussi une pause synchrone du chemin de remise à zéro.

## Limites et blocages

Aucun blocage fonctionnel. La comptabilité repose sur les buffers dont la taille est connue par HiBoP ; la mémoire interne de Unity, du pilote graphique et des bibliothèques natives externes n’entre pas dans ce total. Les textures de Trial Matrix seront intégrées à ce budget lors de l’étape 7.
