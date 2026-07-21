# Implémentation de l’étape 3 — traitements, normalisation et statistiques dérivés

Date : 21 juillet 2026

## Résultat

L’étape 3 remplace les collections et matrices temporaires proportionnelles au volume total des essais par des accumulateurs en flux et des buffers de travail bornés. Les valeurs normalisées persistantes restent limitées au mode actif.

Les changements principaux sont les suivants :

- classification explicite des traitements en opérations ponctuelles, scalaires ou nécessitant un buffer ;
- moyenne de traitement calculée en flux, sans sous-tableaux fenêtre/baseline ;
- médiane de traitement calculée dans le buffer réutilisable de l’époque ;
- moyenne, variance et SEM calculés avec l’algorithme incrémental de Welford ;
- médiane inter-essais calculée dans un tableau loué à `ArrayPool`, réutilisé pour chaque échantillon ;
- normalisations `Trial`, `SubBloc`, `Bloc` et `Protocol` calculées sans `List<float>` ni doubles `ToArray()` des baselines ;
- statistiques d’événements calculées en flux pour la moyenne, ou avec un unique buffer loué pour la médiane ;
- courbes complètes et simplifiées branchées sur le même calcul statistique borné ;
- invalidation automatique des caches statistiques lorsque l’averaging des valeurs ou des événements change ;
- réévaluation de `Auto` au prochain chargement/normalisation après changement de préférence.

L’écart-type et le SEM conservent la convention historique de la DLL native : écart-type d’échantillon, division par `n - 1`, puis SEM divisé par `sqrt(n)`. La tolérance numérique des tests est de `1e-6` pour les calculs analytiques et de `1e-4` pour les chemins complets de normalisation.

## Tests automatisés

La classe `Stage3StreamingStatisticsTests` ajoute 12 scénarios :

- identité avec la moyenne, l’écart-type et le SEM natifs ;
- moyenne et médiane analytiques avec SEM ;
- rejet des séries de longueurs incohérentes ;
- sélection des essais valides ;
- moyenne et médiane des événements ;
- classification de toutes les classes de traitement ;
- application individuelle de chaque traitement ;
- combinaison fenêtre/baseline pour moyenne et médiane ;
- pipeline ordonné avec buffer réutilisé.

Trois scénarios complètent `DataLoadingProcessingCacheTests` : changement de préférence `Auto`, invalidation du cache de statistiques de canal et invalidation du cache de statistiques d’événements.

Validation Unity EditMode : **310 tests réussis sur 310** dans `HBP.Serialization.Tests`.

## Tests manuels conseillés

Les tests automatisés couvrent les résultats numériques. Avant diffusion produit, vérifier aussi :

1. ouvrir une visualisation iEEG avec moyenne, passer à médiane dans les préférences, fermer puis rouvrir la visualisation et comparer courbes/SEM ;
2. répéter avec chaque normalisation, notamment `Auto`, puis revenir à `None` ;
3. ouvrir un protocole contenant plusieurs traitements dans un ordre non trivial et comparer les valeurs exportées à la version de référence ;
4. profiler un protocole de 250 patients pendant normalisation et construction des graphes, en relevant pic managé, octets alloués et temps CPU ;
5. effectuer dix cycles ouverture/fermeture afin de confirmer l’absence de rétention des tableaux loués.

## Gain attendu

Le gain principal concerne le pic temporaire et la pression GC, pas les tableaux de sortie finaux :

- statistiques inter-essais : passage de `O(nombre_essais × nombre_échantillons)` temporaires à `O(nombre_essais)` pour la médiane et `O(1)` pour moyenne/variance/SEM, en plus des deux tableaux de sortie ;
- normalisation agrégée : passage d’une copie de toutes les baselines à un accumulateur constant ;
- traitements moyenne/médiane : suppression de deux sous-tableaux et d’un tableau concaténé par application ;
- graphes : suppression d’une liste et de deux `ToArray()` par échantillon.

Pour 250 séries et une fenêtre de 1 501 échantillons, une seule agrégation évite environ **1,4 Mio** de valeurs temporaires, hors surcoût des objets, et n’utilise plus qu’environ **1 Kio** de buffer médian plus les sorties. La réduction du temporaire de cette opération est donc proche de **99 %**. À l’échelle d’un chargement classique de 250 patients, le gain de pic managé dépend du nombre de canaux et d’agrégations simultanées, mais plusieurs dizaines à centaines de Mio d’allocations cumulées et les collections GC associées doivent disparaître. Le temps de calcul devrait être stable ou meilleur, typiquement de l’ordre de **10 à 30 %** sur les phases statistiques dominées par les allocations ; cette plage reste à confirmer par le benchmark produit.

## Limites et blocages

Aucun blocage fonctionnel n’a été rencontré. La mesure de performance sur la fixture produit complète de 250 patients reste un test manuel/benchmark, car cette fixture volumineuse ne doit pas être ajoutée à la suite unitaire.
