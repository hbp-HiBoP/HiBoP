# Objectif, périmètre et décisions

## Besoin produit

Le propriétaire veut qu'une fonctionnalité ajoutée à la scène ou à une colonne
puisse être utilisée sur Quest avec la même implémentation métier. Sa présentation
peut nécessiter une UI, un gizmo ou un geste différent. Son code ne doit pas
chercher les données dans une structure Desktop d'un côté et dans une structure
Quest de l'autre.

Le prototype actuel établit progressivement transfert, exploration autonome et
calcul local. Les tâches 018–023 mutualisent la projection, y compris certaines
responsabilités de ressources et d'invalidation. Elles ne garantissent pas à
elles seules un modèle Scène/Colonne commun pour toutes les opérations. La
réussite numérique du prototype et l'absence de divergence architecturale sont
deux critères distincts.

## Décisions approuvées le 2026-09-09

Source : discussion avec le propriétaire dans la conversation
`01a085ce-1d2d-72a3-8006-ffd59c832fe4`, suivie de l'autorisation d'écrire ce dossier.

| ID | Décision | Conséquence |
| --- | --- | --- |
| SC-D01 | Conserver une scène et des colonnes scientifiques communes, après extraction des modules spécifiques. | Une simple collection de calculateurs communs ne satisfait pas le chantier. |
| SC-D02 | Viser l'export et la restauration de cette scène, exprimés conceptuellement par `ToPayload` / `FromPayload`. | Le graphe restauré doit être utilisable par les mêmes opérations, pas seulement affichable. |
| SC-D03 | Laisser terminer 018–023 avant ce refactoring ; insérer celui-ci avant 024. | Ne pas interrompre ni élargir 018 ; qualifier ensuite le prototype refactorisé. |
| SC-D04 | Les présentations sont distinctes ; `View3D` et `Camera3D` Desktop ne sont pas imposées au Quest. | Aucun objet factice de présentation pour faire tourner la science. |
| SC-D05 | Accepter une reprise ciblée du prototype, avec un ordre de grandeur discuté autour d'une dizaine de tâches. | Les huit lots proposés sont provisoires, pas un engagement de coût ni une refonte illimitée. |
| SC-D06 | Créer un dossier autonome sans modifier aucun document existant pendant la rédaction. | Le registre, les décisions et les fiches de ce chantier restent locaux ; raccordement historique différé. |

## Périmètre obligatoire de cette migration

- Scène et colonnes portant les données et paramètres du prototype anatomie,
  sites, densité et iEEG. Identités, relations et accès scientifiques communs.
- Opérations locales réellement utilisées : paramètres de projection, masques
  effectifs, préparation, calcul, résultats, remplacement et fermeture.
- Même chemin métier sur une scène issue de la préparation Desktop et sur une
  scène restaurée ; données locales accessibles après déconnexion.
- Propriété des ressources, invalidation, annulation et publication cohérentes.
- Export explicite du sous-ensemble sélectionné et restauration sans dépendance
  au projet Desktop, à son système de fichiers ou à son UI.
- Préservation de l'expérience Desktop existante et des autres modalités qui
  utilisent des portions des classes modifiées.

Le modèle doit représenter la relation scène/colonnes et les ressources partagées
sans être conçu comme « la colonne unique du casque ». Des tests avec deux
colonnes sont requis pour les identités, l'indépendance des paramètres et le
partage de ressources. Cela n'ajoute pas de disposition ou d'interface multicolonne
Quest, ni l'export utilisateur de toutes les colonnes.

## Ce que « commun » garantit

Une correction dans une opération commune bénéficie aux deux Players reconstruits
depuis cette source. Une UI appelle cette opération et consomme son résultat.
La scène restaurée ne reçoit pas une copie différente des règles métier.

Les données sont des instances locales à chaque processus : aucun partage de
mémoire entre Desktop et Quest, aucune dépendance réseau par frame. Le partage
du code n'implique pas une synchronisation des modifications entre appareils.

Une nouvelle fonction qui demande des données absentes nécessite d'étendre la
préparation et le payload. Par exemple, un instant iEEG ne permet pas de parcourir
une séquence entière. Un backend peut aussi nécessiter un rendu adapté. Ces
limites sont explicites et ne justifient pas une seconde logique scientifique.

## Hors périmètre

Pas de nouvelle coupe, timeline Quest, édition de ROI, modalité scientifique,
import EEG Quest, sauvegarde de projet portable, synchronisation bidirectionnelle,
nouvel appairage ou qualification Mac/Linux dans ce chantier. Pas de refonte de
toutes les modalités ni de framework générique d'événements, plugins ou commandes.
Pas d'optimisation changeant algorithmes, normalisation, résolution ou sampling.

Des adaptations minimales d'autres modalités Desktop sont autorisées si nécessaires
pour préserver leur comportement après extraction. Leur port sur Quest est une
autre tâche. Toute extension majeure du périmètre doit être exposée et décidée,
pas absorbée silencieusement pour tenir dans huit fiches.

## Points à résoudre par l'audit, sans nouvelle décision produit par défaut

SCENE-001 localise les responsabilités réelles après 023, choisit le découpage
minimal et précise quels types historiques deviennent communs ou restent des
adaptateurs. Le nom `Base3DScene` doit être examiné explicitement : conserver le
type commun est souhaitable si son extraction le permet. Un renommage peut être
justifié, mais laisser deux modèles métier derrière un nom partagé ne l'est pas.

Restent à établir : stratégie d'adoption des ressources Desktop, APIs de mutation,
notifications, contenu minimal de scène exportable, compatibilité du format et
étendue des tests de non-régression. Aucun nom provisoire d'API dans ces documents
ne justifie à lui seul une classe nouvelle ou une dépendance supplémentaire.
