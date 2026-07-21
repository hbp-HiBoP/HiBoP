# Implémentation de l’étape 4 — horloge commune et fréquences mixtes

Date : 21 juillet 2026

## Résultat

Les colonnes dynamiques possèdent désormais deux grilles distinctes :

- `Timeline` reste la grille de navigation commune, à la fréquence maximale des colonnes affichées ;
- `ProjectionTimeline` est la grille discrète exacte de la colonne, à sa fréquence native maximale interne.

Une colonne 64 Hz affichée avec une colonne 2 048 Hz conserve donc les 3 073 positions de navigation communes sur 1 500 ms, mais ne stocke et ne pré-calcule que ses 97 positions de projection. Une colonne contenant elle-même des signaux 64 Hz et 2 048 Hz utilise 2 048 Hz pour sa projection interne.

Le mapping passe par le temps physique du sous-bloc courant. Il ne suppose pas que les fréquences sont commensurables et respecte les fenêtres négatives, les origines décalées, les bornes inclusives et les segments de sous-blocs alignés.

Le nouveau paramètre sérialisé `Temporal Sampling` appartient à `DynamicConfiguration` :

- `Floor` sélectionne l’index inférieur ;
- `Round` sélectionne l’index le plus proche selon `Mathf.RoundToInt` ;
- `Interpolate`, valeur par défaut et valeur des anciens fichiers, transmet l’index inférieur et le coefficient linéaire au moteur natif.

Le même couple `(index, alpha)` est utilisé par les surfaces, les coupes, les valeurs affichées sur les sites et l’implantation. L’export NIfTI reste sur la grille discrète de projection et n’applique pas de reconstruction supplémentaire.

## Tests automatisés

La classe `Stage4TemporalGridTests` contient 14 scénarios couvrant :

- 97 positions à 64 Hz et 3 073 positions à 2 048 Hz pour `[0 ; 1 500 ms]` inclusif ;
- temps exact, position fractionnaire et trois politiques ;
- interpolation analytique entre deux voisins natifs ;
- première et dernière bornes ;
- fenêtre commençant avant zéro et origine événementielle décalée ;
- fréquences 1 000/333 Hz non commensurables ;
- sous-bloc court aligné sur le segment temporel d’un sous-bloc plus long ;
- colonne basse fréquence séparée : navigation 3 073, projection et série persistante 97 ;
- colonne mixte : projection à sa fréquence interne maximale ;
- sérialisation, clonage et valeur par défaut de la politique.

Le manifeste de contrat de sérialisation a été mis à jour de 515 à 516 membres avec l’empreinte correspondant uniquement au nouveau champ approuvé.

## Tests manuels conseillés

1. afficher côte à côte une colonne 64 Hz et une colonne 2 048 Hz, parcourir les 3 073 positions et vérifier que la colonne haute fréquence ne saute aucun échantillon ;
2. tester successivement `Floor`, `Round` et `Interpolate` autour d’un demi-échantillon et aux deux bornes ;
3. comparer surfaces, coupes et valeur de tooltip d’un même site au même instant ;
4. rouvrir une visualisation sauvegardée avec chaque politique et un ancien fichier sans le champ ;
5. exporter un NIfTI 4D de la colonne 64 Hz et confirmer fréquence, nombre de volumes et temps initial ;
6. mesurer le P95 de scrubbing sur un mélange 64/2 048 Hz et vérifier une régression inférieure à 5 %.

## Gain attendu

Pour une colonne 64 Hz sur 1 500 ms affichée avec une colonne 2 048 Hz, la série de projection passe de 3 073 à 97 valeurs par site, soit une réduction de **96,8 %** pour cette colonne basse fréquence.

Dans le cas classique de 250 patients et environ 30 000 sites au total, si ces sites appartiennent à une telle colonne basse fréquence, chaque représentation flottante évite environ `(3 073 - 97) × 30 000 × 4`, soit **341 Mio**. Le chemin actuel possède la série préparée par canal et la matrice aplatie transmise au natif ; l’économie potentielle est donc proche de **680 Mio par colonne basse fréquence**, avant surcoût des objets. Le gain réel dépend de la distribution des fréquences et du nombre de colonnes basses fréquences.

Le mapping temporel est constant et sans allocation. Le coût de scrubbing devrait rester équivalent ; l’interpolation native existante reçoit désormais l’alpha temporel exact au lieu d’une série globalement gonflée.

## Limites et blocages

Aucun blocage d’implémentation. Le contrôle du paramètre est présent dans le modèle sérialisé et l’API des colonnes ; l’ajout éventuel d’un sélecteur graphique dédié dans l’éditeur de visualisation nécessitera un choix de placement UX et une modification de prefab. Cela n’empêche ni l’utilisation par défaut, ni la configuration par fichier/API, ni les trois chemins de rendu.
