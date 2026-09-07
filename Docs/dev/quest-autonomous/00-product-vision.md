# Vision et périmètre

## Produit

HiBoP Desktop conserve la préparation des projets, l'import EEG, les calculs et
l'expérience existante. HiBoP Quest permet d'explorer spatialement une
visualisation préparée, avec des calculs locaux lorsque la fonctionnalité est
admise. Il continue sa session sans réseau après réception des données.

Le partage concerne le modèle scientifique, les paramètres, leurs règles et les
calculs. Chaque application possède sa caméra, sa disposition et ses
interactions. Tourner ou agrandir le cerveau sur Quest ne change ni la caméra
Desktop ni les coordonnées scientifiques.

Un seul projet Unity, un manifeste et un lockfile sont maintenus. Les builds
proviennent du même commit applicatif et des mêmes révisions natives épinglées.
HiBoP_HoloLens guide les gestes ; sa duplication de l'application ne doit pas
être reproduite.

## Parcours minimal

Dans HiBoP Windows, ouvrir une visualisation représentative à une colonne,
appairer le Quest 3 sur le LAN, puis utiliser « Envoyer au Quest ». Le casque
affiche le contenu reçu dans la pièce en passthrough. Les contrôleurs permettent
la saisie, le déplacement, la rotation et l'agrandissement uniforme du groupe
cerveau/sites. Les mains sont une extension ultérieure souhaitée.

La connexion ne porte aucune dépendance par frame. Une coupure d'une minute
laisse le contenu manipulable. La reconnexion rétablit la capacité de transférer ;
elle ne remet pas la disposition spatiale à zéro.

## Séquence de valeur validée

| Étape | Preuve attendue |
| --- | --- |
| Anatomie | Cerveau MNI complet réellement transféré et manipulable. |
| Électrodes | Contacts synthétiques associés, correctement placés et agrandis avec le cerveau. |
| Densité | Projection calculée sur Quest à partir des sites et des données de référence reçues. |
| iEEG | Projection locale d'un instant choisi sur Desktop, avec mêmes conventions et paramètres scientifiques. |
| Mac | Même parcours depuis un Desktop Apple Silicon, avec le même APK lorsque son protocole n'a pas changé. |
| Linux | Évaluation puis qualification éventuelle avant l'élargissement fonctionnel suivant. |

Les premiers jalons démontrent le pipeline complet, sans exiger d'emblée
l'expérience complète HiBoP. Les temps de préparation peuvent être plus longs
sur Quest ; la signification des résultats doit rester cohérente.

## Hors périmètre initial

Import EEG sur Quest, fichier projet portable complet, multi-colonnes,
coupes locales, lecture/timeline interactive, ROI éditables, synchronisation
scientifique bidirectionnelle, conflit concurrent, sauvegarde après arrêt,
perfection graphique et réorganisation générale du code.

Un bouton afficher/masquer le cerveau est accepté pour voir les contacts internes.
La transparence sur passthrough pourra être traitée ultérieurement.

## Principes d'acceptation

- Une correction scientifique commune affecte les deux applications.
- Aucune copie de Base3DScene ou Column3DIEEG dans une arborescence Quest.
- Les coordonnées et masques scientifiques ne changent pas avec la taille VR.
- Une donnée non prise en charge est explicitement refusée ; pas de perte ou
  simplification scientifique silencieuse.
- La fixture utilise le MNI de référence et des sites/signaux synthétiques.
- La preuve finale appartient aux Players sur appareils ; un test Editor ou
  une compilation ne les remplace pas.
