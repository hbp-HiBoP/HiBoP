# Spécification du prototype

Statut : proposition technique fondée sur D01–D19. Les critères numériques
provisoires sont identifiés dans le plan de validation.

## Capacités successives

| Niveau | Contenu reçu | Travail local Quest | Interaction |
| --- | --- | --- | --- |
| A — anatomie | Une surface MNI complète préparée, identité, repère, apparence minimale | Reconstruction du mesh et rendu | Déplacer, tourner, agrandir le groupe |
| B — sites | A + contacts synthétiques ordonnés, positions et apparence | Rendu des sites associés | A + afficher/masquer le cerveau |
| C — densité | B + volume de référence, masques et paramètres de projection | Grille, densité, projection et apparence communes | Manipulation de l'ensemble |
| D — iEEG | C + amplitudes préparées d'un instant et réglages scientifiques | Projection iEEG commune pour cet instant | Manipulation de l'ensemble, sans timeline |

La surface complète peut résulter de l'assemblage des deux hémisphères par le
chargement MNI existant. « Une surface » décrit une représentation complète
avec une indexation stable, pas la suppression d'un hémisphère.

L'instant iEEG conserve sa provenance temporelle. Pour la première fixture,
choisir explicitement un échantillon de projection exact (Alpha=0) afin de
comparer sites et surface sans ambiguïté. Ne pas arrondir silencieusement une
sélection arbitraire de l'utilisateur : si l'instant se situe entre échantillons,
préserver les règles existantes (surface sample-and-hold, sites interpolés) en
transférant les entrées nécessaires, ou refuser clairement ce cas dans ce
prototype. L'acceptation initiale porte sur la fixture alignée.

## Parcours

1. Ouvrir la fixture à une colonne dans HiBoP Desktop.
2. Ouvrir l'application Quest et établir l'appairage local. Saisie adresse/code
   acceptable ; découverte automatique et persistance d'identité non exigées.
3. Cliquer « Envoyer au Quest ». Desktop fige un snapshot cohérent de la colonne.
4. Afficher la progression du transfert et de la préparation ; l'application
   ne publie qu'un contenu complet et validé.
5. Afficher le cerveau en passthrough, à portée des contrôleurs.
6. Saisir et déplacer/tourner avec un contrôleur ; agrandir uniformément via un
   geste à deux contrôleurs. Ajuster ce geste lors du test utilisateur.
7. Au niveau B, masquer le cerveau permet de voir les contacts internes.
   Cette commande masque seulement sa présentation, sans supprimer les données,
   les sites ni leur calcul, et sans perdre la possibilité de manipuler le groupe.
8. Couper le réseau une minute. Continuer à manipuler le groupe.
9. Rétablir le réseau. La présentation reste à sa position, orientation et taille.
10. Envoyer éventuellement un nouveau snapshot ; ne remplacer l'ancien qu'une
    fois le nouveau complètement prêt.

Proposition pour un renvoi de la même visualisation/colonne : garder la disposition
locale. Pour un contenu différent, effectuer un remplacement explicite ; aucune
seconde colonne implicite. Un échec laisse la précédente session utilisable.

## Fixture

- Anatomie : MNI Grey matter préparé depuis les GII/TRM du dépôt ; mêmes buffers
  de référence que Desktop après conversions.
- Sites : génération déterministe, positions asymétriques repérables et groupes
  d'électrodes synthétiques. Consigner le nombre et les distances ; ne pas inventer
  qu'une fixture existante contient déjà un jeu iEEG complet autorisé.
- Densité : MNI.nii reçu pour la grille, sites synthétiques et paramètres Desktop.
- iEEG : amplitudes synthétiques négatives/nulles/positives, unités explicites,
  normalisation définie sur une préparation représentative et conservée au transfert.
- Les assets reçus portent hashes et provenance. Le MNI peut exister dans le
  dépôt Quest pendant le développement, mais son inclusion dans l'APK ne valide
  pas le transfert : la démonstration doit charger les octets reçus.

## Invariants vérifiables

- Une seule source des calculs et règles scientifiques du périmètre.
- Vues Desktop/Quest sans effet mutuel.
- Toutes les données géométriques partagent le même repère anatomique.
- L'échelle VR affecte cerveau/sites ensemble, pas les distances de calcul.
- Pas de colonne partiellement appliquée après erreur ou coupure.
- Aucun import EEG sur Quest ni appel au binaire EEGFormat.
- Pas de journal réseau des gestes spatiaux.
- Pas de sauvegarde ou reprise après kill exigée.
- Pas de textures projetées Desktop utilisées comme résultat scientifique Quest
  aux niveaux C et D ; elles peuvent servir d'oracle dans le banc de validation.

## Fin du prototype Windows

Les niveaux A à D sont démontrés sur Quest 3 face à un Player Windows issu du
même commit, avec rapport de transfert, parité et mesure. Les anomalies réellement
bloquantes sont corrigées avant qualification Mac ; l'optimisation complète et
les futures fonctions ne sont pas ajoutées à ce critère.
