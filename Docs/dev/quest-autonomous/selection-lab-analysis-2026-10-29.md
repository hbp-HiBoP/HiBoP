# Analyse de la campagne Quest du 29 octobre 2026

## Données et limites

Extraction USB : `Logs/SelectionLab/campaign-20261029/selection-lab`.
Session analysée : `20261029-101735-39ea06`, build `6.2.0/selection-lab-2`.
Les 107 événements de la première version, affectée par le défaut de rafraîchissement
des dispositions, sont exclus. La v2 contient 1 093 événements : 173 débuts et
173 fins d'essais, 622 transitions de sélection, 105 avances dans une rangée,
14 changements de disposition, 5 événements de note et un démarrage de session.
Il n'y a pas d'essai sans fin enregistrée.

Il s'agit d'une exploration par un testeur, avec ordre des modes non contrebalancé,
quelques modifications de réglages et des orientations différentes selon les
exercices. L'échelle du cerveau reste constante à environ 2,277 pendant les essais
chronométrés. Les résultats décrivent cette session et ne démontrent pas un
classement général ni un effet causal indépendant de l'apprentissage.

## Le geste de validation doit être distingué de l'acquisition

Les fins d'essais se répartissent ainsi : 57 `success`, 102 `next-target`,
6 `wrong-target`, 3 `settings-changed`, 3 `restarted`, 2 `layout-changed`.

`Cible suivante` clôt l'essai puis en démarre immédiatement un autre. Son motif
`next-target` ne signifie donc pas que la sélection a échoué. Pour 98 de ces
102 essais, la bonne cible est encore sélectionnée à la fin. Un autre a bien
atteint la cible avant de finir sur un voisin ; trois n'ont pas acquis la cible.

Les six validations `wrong-target` portent toutes une sélection vide, sans
transition de sélection et sans première acquisition correcte. Elles durent
environ 0,9 à 2,3 secondes et surviennent sur les dispositions profondeur/dense.
Cela est compatible avec la clôture d'un essai supplémentaire ouvert par
`Cible suivante`. Ce n'est pas une preuve de sélection d'un mauvais site.

Sur l'ensemble des 173 fins, 158 présentent la bonne identité, 14 une sélection
vide et une seule une autre identité. Ces états finaux ne doivent pas être
transformés en taux de précision sans distinguer les interruptions et le parcours.
Une transition intermédiaire vers un voisin peut être volontaire. Les journaux
ne contiennent pas l'intention de l'utilisateur ni les poses continues des mains.
Ils ne permettent pas de mesurer un taux de faux positifs complet.

## Temps observés

Les quatre premières colonnes donnent la médiane de la première acquisition
correcte, reconstruite depuis les événements, y compris les essais terminés par
`Cible suivante`. Les essais sans acquisition ne fournissent pas de temps de réussite.
Les nombres entre parenthèses indiquent le nombre d'acquisitions. La dernière
colonne donne la durée totale médiane des trois rangées complètes R1–R8.
Les six essais initiaux de profil `Personnalisé` sur S1 restent séparés.

| Profil | Isolé, s | Voisins, s | Deux profondeurs, s | Dense, s | Rangée complète, s |
| --- | --- | --- | --- | --- | --- |
| Référence | 1,624 (6) | 1,493 (4) | 1,041 (7) | 1,188 (12) | 5,167 (3) |
| Visibilité | 1,548 (6) | 1,062 (8) | 0,951 (8) | — | 5,333 (3) |
| Profondeur | 1,389 (6) | 0,973 (7) | 0,847 (9) | 1,014 (12) | 3,541 (3) |
| Stabilité | 1,202 (6) | 1,055 (8) | 0,929 (8) | — | 3,431 (3) |
| Rayon | 0,778 (4) | 0,791 (6) | 0,610 (9) | 0,958 (12) | 3,527 (3) |

Le rayon est rapide dans les acquisitions enregistrées, ce qui concorde avec le
retour positif sur les cibles bien visibles. Sur la rangée, profondeur, stabilité
et rayon sont proches ; la stabilité ne présente pas de gain systématique sur
chaque exercice. Le groupe dense n'a pas de comparaison complète à cinq modes.
Son temps médian ne décrit pas à lui seul la difficulté ressentie dans une vue oblique.

La durée moyenne des images enregistrée pendant les essais est proche de
13,888 ms, soit environ 72 images/s. Elle n'indique pas la marge GPU/CPU disponible
et ne préjuge pas des performances du futur scénario à 2 000 sites.

Les cinq événements de notes concernent uniquement Facilité et reviennent à 3/5.
Confiance et Effort restent à leur valeur par défaut. Ces valeurs ne constituent
pas un questionnaire comparatif complet ; les remarques écrites sont la principale
source qualitative de cette campagne.

## Profondeur et stabilité

Le profil Profondeur étend la zone de recherche suivant l'axe tête–sonde. Avec
10 mm, le segment s'étend de 10 mm devant à 10 mm derrière la sonde. Les rayons de
contact et des sites s'y ajoutent : ce n'est pas une boîte bornée exactement à ±10 mm.

Le profil Stabilité utilise cette même géométrie, puis conserve l'ancien candidat
si son score n'est que légèrement moins bon que celui du nouveau. Le score est
principalement une distance à l'axe, divisée par le rayon de capture local ; une
petite contribution de distance départage les cas proches. La marge 15 % signifie
**0,15 point de ce score normalisé**, pas 15 % de probabilité ni une attente de 15 %.
Par exemple, un ancien score de 0,40 est conservé face à 0,35 ; il cède face à 0,20.

Deux protections s'appliquent : un ancien candidat sorti du volume est abandonné,
et un contact direct avec un site a priorité sur cette stabilisation. Avec un
rayon de contact porté à 3 mm, le contact direct devient plus fréquent ; la
différence entre profondeur et stabilité peut donc être peu visible.
La v2 ne comptait pas les images où cette règle conservait un candidat : on ne
peut pas mesurer rétrospectivement combien de bascules elle a évitées. La v3 ajoute
le compteur `StabilizedFrames` par essai pour le prochain test.

## Pourquoi le rayon devient difficile en vue oblique

Un réseau vu de biais présente des séparations projetées plus petites et davantage
de superpositions. Le sélecteur conserve une recherche en volume autour du rayon :
il peut donc avoir plusieurs candidats géométriquement proches de l'axe.
Le score ne reconstitue pas l'intention ni la visibilité pixel par pixel depuis les yeux.

En mode Rayon, l'attraction comprend aussi le demi-angle du cône, ici 1°.
À 50 cm, ce seul angle ajoute environ 8,7 mm au rayon de capture, auxquels
s'ajoutent Contact, Attraction et le rayon du site. « Attraction 3 mm » n'est donc
pas la largeur totale de l'aide à distance. C'est une explication plausible du
confort face au réseau et de la difficulté lorsque les projections se serrent.

Pour le prochain test dense, comparer le demi-angle actuel de 1° à 0,5° puis
0,25°, à pose et réglages identiques. Réduire l'angle peut réduire la concurrence
entre voisins, mais demandera un pointage plus précis. Cette piste reste à tester.
La tolérance Profondeur ne s'applique pas lorsque la méthode est Rayon.

## Réglages préférés

Le fichier sauvegardé confirme : méthode Profondeur ; Contact 3 mm ; Boule visible
1 mm ; Profondeur 10 mm ; Attraction activée à 3 mm ; Stabilité activée avec marge
15 % ; nom activé ; contour et liaison désactivés ; diagnostic désactivé.

Contact et Boule visible sont des **rayons** : les diamètres sont respectivement
6 mm et 2 mm. La boule visible n'influence pas la sélection. Avec l'attraction,
le rayon de capture latéral de la sonde devient 3 + 3 = 6 mm, avant ajout du rayon
du site. Le contact direct prioritaire conserve son rayon de 3 mm.

Ce réglage final apparaît dans 57 transitions en exploration libre, sans essais
chronométrés. Il constitue un bon candidat issu du ressenti, sans preuve de
supériorité quantitative sur les préréglages. Le preset v3 **Préféré (sonde)**
permet de le retrouver sans modifier la référence de comparaison.

Les emplacements A et B de la sauvegarde finale sont tous deux identiques au
profil Référence. Ils ne constituent donc pas une comparaison sauvegardée entre
profondeur et stabilité. Pour le prochain passage, enregistrer le même réglage
préféré avec la stabilité désactivée dans A, puis activée dans B.

## Évolution du laboratoire

- Labels à 40 % de leur ancienne taille, soit une division par 2,5. La valeur
  devient réglable page 1 : 50 % pour une division par 2, 33 % pour environ 3.
- Sixième disposition : 2 000 sites uniformes dans la bbox du cortex MNI complet.
  La graine `20261029` fixe les mêmes positions pour chaque profil et lancement.
  Il n'y a ni espacement minimal ni rejet des positions hors du maillage cortical :
  le domaine demandé est la bbox, qui contient aussi de l'espace extérieur au cerveau.
- La coupe axiale reste présente. Les 2 000 sites sont actifs ; 1 120 sont du côté
  dégagé et servent de cibles guidées. Les autres restent dans le volume, avec
  l'occlusion naturelle du cerveau conservé. Le choix guidé ne réduit pas le nuage.
- Les journaux v3 ajoutent la graine, l'état du repère de cible et le compteur
  d'images stabilisées. Les réglages sauvegardés et les anciennes dispositions sont conservés.

Le prochain passage le plus utile compare le preset préféré avec stabilité
activée/désactivée, puis le rayon à plusieurs demi-angles sur le même nuage et en
vue oblique. Vérifier l'accès aux voisins, la facilité à quitter un site et la
lisibilité des noms. L'évaluation reste nécessaire sur le Quest, particulièrement
pour les performances à 2 000 sites.

Validation de la v3 : 18 tests PlayMode ciblés réussis, contrôle des dépendances
d'assemblies réussi, APK vérifié puis installé sur le Quest 3 en conservant les
données. Le démarrage `20261029-111404-c63d86` confirme `selection-lab-3`, le
chargement MNI et la reprise des réglages préférés avec des labels à 40 %.
Le nuage complet a été vérifié dans l'Editor ; ses performances pendant une
sélection interactive sur le Quest restent à mesurer.

Résultats détaillés : `Logs/SelectionLab/campaign-20261029/analysis/summary.json`,
`trials-analyzed.csv` et `acquisitions.csv`. Le script d'analyse local est
`Logs/SelectionLab/analyze_campaign.py`.
