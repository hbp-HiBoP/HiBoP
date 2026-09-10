# Visualisation complète, ressources et restauration

## Contenu à transférer

Exporter une visualisation, toutes ses colonnes et les dépendances nécessaires
à leurs fonctionnalités existantes. Le payload est un format de transfert du
contenu préparé, distinct du format des projets Desktop.

| Ensemble | Contenu attendu |
| --- | --- |
| Visualisation | Identité, colonnes ordonnées, relations, patients/métadonnées utiles et état courant |
| Colonnes | Type, paramètres/configurations, états de sites, références de ressources et données de leur modalité |
| Anatomie | Tous les meshes, représentations et volumes disponibles dans cette visualisation, implantations et repères |
| Données temporelles | Données de chaque site à chaque instant, timelines, unités, masques, plages et conventions scientifiques |
| Données d’exploration | Essais, statistiques et métadonnées de sites nécessaires aux outils existants, même sans leur UI Quest |
| Autres modalités | Données CCEP, MEG, fMRI, statiques et dépendances réellement nécessaires à leur fonctionnement |
| Configuration | Coupes, ROI, atlas, seuils, coloration, sélection de ressources et autres états existants nécessaires à restituer le contenu |
| Ressources standard | Références identifiées vers le contenu local commun, dont le MNI |
| Intégrité | Version du format, identités, dimensions, relations et contrôles d’intégrité adaptés au transport |

L’inventaire de SCENE-001 précise les types concrets et leurs propriétaires.
Ne pas inclure le projet entier sans discernement : une dépendance appartenant à
la visualisation doit être disponible, un dataset sans lien avec elle n’a pas à
être transféré. Ne pas réduire les signaux à leur moyenne si les essais sont
nécessaires aux outils de sites.

Les caches et résultats dérivables peuvent être reconstruits localement.
Les données sources utiles et les conventions de calcul ne doivent pas être
perdues. Les ressources ne doivent pas dépendre de chemins absolus Desktop,
de handles natifs, de callbacks ou d’une sérialisation exécutable des GameObjects.

## Attendre le chargement complet

Le code actuel lance LoadMissingAnatomy après FinalizeInitialization, en arrière-plan.
L’apparition de la scène ou son indicateur CompletelyLoaded ne prouve pas que
toutes ses anatomies sont prêtes.

Stratégie initiale : rendre ce chargement attendable, préparer les ressources
manquantes de la visualisation et attendre leur disponibilité avant la capture.
Signaler clairement un échec de préparation ; ne pas publier un sous-ensemble
comme une visualisation complète. Éviter de créer des fenêtres ou contrôles
Desktop pour forcer un chargement.

L’envoi progressif d’un mesh/volume est une évolution possible, pas une exigence
de cette passe. Ne pas introduire un protocole de mise à jour partielle pour
contourner l’absence de suivi du chargement.

## MNI et données standard locales

Livrer avec l’installation Quest les ressources MNI de référence nécessaires,
issues du Data du projet, et les charger localement. Pas d’envoi initial spécial
au premier appairage, ni copie du MNI à chaque transfert.

Adapter le packaging et l’accès aux fichiers Android : ApplicationState.DataPath
ne fournit pas actuellement une stratégie Quest dédiée. Les chargeurs natifs
nécessitant des fichiers doivent recevoir des chemins locaux utilisables.
Réutiliser MNIObjects et les chargeurs existants après adaptation.

Les données standard nécessaires aux atlas ou autres fonctionnalités suivent le
même principe lorsque pertinent. Livrer les fichiers réellement nécessaires,
sans recopier aveuglément des caches, sorties de développement ou doublons.

Le contenu reçu identifie les ressources attendues par une identité/version ou
empreinte vérifiable. Un nom « MNI » seul n’autorise pas à substituer silencieusement
une autre version. Si une ressource locale requise est absente ou incompatible,
conserver la scène précédente et expliquer l’écart. Une ressource personnalisée
qui n’est pas une référence standard identique doit être transportée.

## Capture et restauration

1. Identifier la visualisation et l’ensemble de ses dépendances.
2. Attendre la préparation nécessaire et capturer un état cohérent. Une mutation
   pendant l’encodage ne doit pas mélanger les versions des données/paramètres.
3. Encoder le contenu et les références aux ressources locales. Dédupliquer les
   données partagées sans aliasser les états mutables des colonnes.
4. Utiliser le transport existant, adapté si les tailles réelles le nécessitent.
5. Valider structure, limites, références et disponibilité locale avant les
   allocations coûteuses, puis reconstruire les objets communs.
6. Préparer le rendu requis avant publication ; conserver l’ancien contenu si
   la restauration échoue ou est annulée.
7. Libérer les ressources remplacées et temporaires au terme réel de leurs usages.

ToPayload/FromPayload restent une notation de comportement. Des collaborateurs
dédiés sont possibles ; ne pas imposer des signatures synchrones à un travail async.

Préserver les protections de transport utiles : authentification, intégrité,
idempotence d’une livraison répétée, annulation et conservation hors connexion.
Les anciennes limites de taille, fixtures et hypothèses « MNI/colonne/instant
uniques » doivent être revues avec le contenu complet.

Ne pas imposer que tout soit dupliqué en mémoire dans un unique tableau d’octets.
Mesurer les pics lors de la stabilisation ; si nécessaire adapter encodage,
staging et lecture. Une contrainte matérielle réelle produit un échec explicite,
pas une décimation ou perte de données implicite.

## Formats et sauvegardes

Aucun ancien payload Quest/HoloLens n’est à maintenir. Créer une nouvelle version
claire et reconstruire Desktop/Quest ensemble. Réutiliser les conversions et
contrôles existants quand utiles, sans conserver les anciennes restrictions.

Préserver les fichiers de projets Desktop. Les données chargées exclues de leurs
JSON peuvent être incluses dans le transfert via un contrat distinct.
Les modifications runtime ne doivent pas changer les noms de champs, identités
ou mécanismes de résolution persistants sans nécessité.

## Preuves finales utiles

- Restauration des modalités existantes et d’une visualisation multicolonne.
- Préservation des données temporelles, essais et paramètres, pas seulement de l’image.
- Changement de mesh, d’instant et de coupe via les opérations communes après réception.
- Utilisation du MNI local correct sans retransfert ; erreur explicite si incompatible.
- Colonnes indépendantes, ressources partagées, fonctionnement hors connexion.
- Échec de réception/restauration conservant le contenu précédent, fermeture et nettoyage.

Regrouper ces vérifications en SCENE-008. Aucun round-trip exhaustif n’est requis
avant de commencer l’intégration Quest.
