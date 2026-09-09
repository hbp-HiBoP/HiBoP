# Payload de scène et restauration

## Contrat observable

Une scène scientifique exportable produit une description cohérente des colonnes
sélectionnées et des données dont elles dépendent. Sa restauration construit une
autre instance du même modèle, capable d'exécuter les mêmes opérations pour les
données disponibles. Un simple mesh accompagné d'un calculateur ne remplit pas
ce contrat.

`ToPayload` et `FromPayload` sont des noms conceptuels. Le modèle peut déléguer
la capture, l'encodage et la restauration à des collaborateurs dédiés. La
restauration peut être asynchrone. Aucun travail long ou UniTask ne doit être
transformé en attente synchrone pour correspondre à cette notation.

## Informations requises

| Ensemble | Contenu à établir après 023 | Invariant |
| --- | --- | --- |
| Enveloppe | Version, identités de transfert et de contenu, longueurs, hashes, provenance applicative/native | Réutiliser le transport existant ; ne pas confondre déduplication réseau et identité métier. |
| Scène | Identité source, révision capturée, périmètre sélectionné, références des colonnes | Une sélection d'une colonne est déclarée comme telle, pas présentée comme le projet complet. |
| Colonnes | IDs, type/capacités, références d'assets, paramètres, masques, état scientifique requis | Associations stables ; paramètres propres à chaque colonne. |
| Assets | Volumes, géométrie, sites et données nécessaires, avec formats, unités, repères et intégrité | Références explicites et déduplication des données immuables partagées. |
| iEEG | Échantillon(s) nécessaires à l'instant, sémantique temporelle, amplitudes, unités et plages préparées | Préserver les règles surface/sites et la normalisation établies en 020–023. |
| Disponibilité | Données et opérations représentables, absences explicites | Absence distincte de zéro ou d'une liste vide valide ; aucune invention de données. |

Ne pas figer ici un schéma binaire nouveau. SCENE-001 audite le format effectif
après 023 ; SCENE-004 réutilise ou fait évoluer ce format au minimum nécessaire.
Les conversions, vérifications et fixtures HBNA existantes restent des actifs
à conserver, pas des éléments à réécrire par principe.

## Éléments exclus

Le payload ne contient ni pointeur/handle natif, ni GameObject/MonoBehaviour
sérialisé comme graphe exécutable, ni caméra, renderer, connexion, callback,
thread ou chemin absolu Desktop nécessaire à la restauration. Les fichiers
techniques sont recréés dans le stockage local approprié.

Une identité source peut être conservée sans réutiliser un Unity instance ID.
Deux restaurations du même contenu ont leurs ressources et états locaux propres.
Des métadonnées d'origine peuvent documenter la provenance, mais ne constituent
pas des chemins de résolution ou une autorité scientifique parallèle.

Les résultats calculés éventuellement conservés pour comparaison ne remplacent
pas les entrées et le calcul local requis. La validation d'intégrité du contenu
ne remplace pas l'authentification déjà assurée par le transport.

## Capture cohérente

1. Déterminer explicitement scène, colonnes sélectionnées et dépendances requises.
2. Capturer une version cohérente des données et paramètres, avec copies ou prêts
   immuables dont la durée est bornée et vérifiée.
3. Encoder hors du chemin d'interaction lorsque possible. Un changement concurrent
   ne produit pas un mélange de versions ; documenter la stratégie réellement utilisée.
4. Livrer le contenu au transport existant. Ne pas changer l'état de la scène,
   sa sélection ou sa présentation pour fabriquer l'export.

Le codec applique les limites et validations structurelles avant les allocations
coûteuses. Les invariants métier sont validés par le socle commun : ne pas créer
deux jeux de règles selon que les données proviennent de Desktop ou du réseau.

## Restauration transactionnelle

1. Vérifier format, version, limites, hashes, IDs, références, types et disponibilité.
   Toute déclaration de capacité doit être cohérente avec les données effectivement présentes.
2. Préparer un modèle temporaire et ses ressources locales avec les mêmes
   constructeurs/factories et validations métier que le chemin Desktop.
3. Préparer les résultats et éléments de présentation nécessaires à une publication
   complète, selon le comportement qualifié du prototype.
4. Publier la nouvelle scène uniquement si sa préparation aboutit et si la demande
   reste courante. L'ancienne scène reste utilisable en cas d'échec ou d'annulation.
5. Libérer les ressources remplacées après la fin réelle de leurs utilisateurs,
   et les ressources temporaires de toute restauration abandonnée.

La publication ne laisse pas la vue et le modèle pointer vers deux livraisons
différentes. Une fermeture pendant restauration empêche une publication tardive.
La perte d'un acquittement ne provoque pas une nouvelle scène à chaque nouvelle
tentative du même transfert : préserver l'idempotence existante.

## Versions et compatibilité

Documenter une matrice « version de contenu × lecteur » fondée sur l'état après
023. Préserver les contrats antérieurs encore supportés ; s'ils représentent un
sous-ensemble, les convertir vers le même modèle commun avec des capacités
explicites. Ne pas déduire un volume, une séquence ou une règle absente.

Refuser clairement une version inconnue ou une donnée requise non prise en charge,
en conservant le contenu courant. Une rupture de compatibilité ou une suppression
de support n'est pas une conséquence implicite autorisée du refactoring : exposer
son impact et obtenir la décision de périmètre nécessaire. Aucun second modèle
métier historique ne doit survivre simplement pour décoder un ancien format.

## Preuves spécifiques

- Round-trip d'une scène du prototype : identités, relations, valeurs, unités,
  coordonnées, masques et plages préparées préservés.
- Deux colonnes partageant un asset immuable : relation préservée, absence de
  copie inutile, indépendance des paramètres et des données mutables.
- Même opération de colonne sur scène préparée et restaurée, sans lecture métier
  du payload après chargement.
- Contenu tronqué, hash invalide, référence pendante, ID dupliqué, version inconnue
  et données requises absentes : rejet déterministe sans remplacement partiel.
- Nouvelle tentative, déconnexion, fermeture et échec partiel d'allocation : pas
  de fuite, double destruction, accès à une ressource libérée ou publication tardive.

L'absence d'UI multicolonne ou de navigation temporelle Quest n'autorise pas une
perte silencieuse dans le modèle ou le format. Un périmètre exporté limité reste
explicitement annoncé et testé.
