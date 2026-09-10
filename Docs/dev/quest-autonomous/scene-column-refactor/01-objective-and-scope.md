# Vision, périmètre et décisions

## Intention du propriétaire

Une fonctionnalité ou une correction 3D doit être développée une seule fois,
sauf si elle concerne effectivement la présentation propre à une plateforme.
La priorité est de généraliser l’existant avec des extractions minimales.
Il n’est pas demandé de reconstruire la 3D autour d’un nouveau modèle abstrait.

La référence HoloLens concerne le fonctionnement d’une vraie scène 3D spatiale,
avec ses colonnes et fonctionnalités. Sa copie divergente de HiBoP ne doit pas
être reproduite. Desktop et Quest restent dans le même projet Unity ;
Quest exécute et calcule localement après réception.

## Décisions de cette discussion — 10 septembre 2026

| ID | Décision |
| --- | --- |
| D01 | Généraliser Base3DScene, les colonnes et leurs collaborateurs ; extraire uniquement ce qui empêche le partage ou constitue une interaction spécifique. |
| D02 | Migrer la plupart des fonctionnalités 3D existantes ; toute modalité de visualisation existante doit être exportable, et pas seulement anatomie/densité/iEEG. |
| D03 | Transférer les données complètes utiles : toutes les colonnes, les données des sites et des instants, y compris celles destinées à des outils non encore présents sur Quest. |
| D04 | Les opérations et leur rendu doivent fonctionner après restauration ; les nouvelles UI/gizmos scientifiques Quest sont différés. |
| D05 | Tous les meshes et volumes disponibles dans la visualisation font partie de son contenu. Un envoi progressif serait possible, mais attendre le chargement complet est la stratégie initiale retenue. |
| D06 | Privilégier les ressources MNI locales issues de Data sur Quest, livrées avec l’installation, plutôt qu’un transfert à la première connexion. |
| D07 | Afficher un cerveau par colonne, manipulable indépendamment ; pas de fusion/séparation à la HoloLens. |
| D08 | Aucune compatibilité à maintenir avec les anciens transferts Quest ou le prototype HoloLens. Préserver le contrat des projets Desktop. |
| D09 | La synchronisation future des opérations/configurations Desktop–Quest est différée. Ne pas construire maintenant son protocole de commandes. |
| D10 | Favoriser de grands lots de code ; regrouper principalement détection, correction et validation à la fin. Accepter des états intermédiaires incomplets et non qualifiés. |

Ces décisions remplacent les anciennes SC-D01–SC-D06 et les exclusions associées
dans la version précédente du dossier.

## Fonctionnalités concernées

L’inventaire initial doit couvrir anatomie/densité, iEEG, CCEP, MEG, fMRI et
colonnes statiques, ainsi que leurs dépendances : surfaces et représentations,
volumes, implantations, sites et états, timelines, projections, coupes, ROI,
atlas, coloration, seuils et effacement de triangles selon les fonctions existantes.

Pour chaque fonction, distinguer opération commune, données, rendu et contrôles.
Une fonction n’est pas « migrée » si ses règles restent enfermées dans une toolbar
Desktop. L’absence de contrôle Quest ne justifie ni la suppression des données
ni une deuxième implémentation de l’opération.

L’affichage de matrices d’essais ou d’autres fenêtres Desktop sur Quest n’est pas
demandé maintenant. Les données nécessaires appartenant à la visualisation
doivent néanmoins être conservées dans le transfert, notamment les essais et
métadonnées nécessaires aux outils de sites existants.

« N’importe quel type de visualisation » signifie couvrir les modalités et
structures existantes, dont les visualisations à plusieurs colonnes et patients.
Cela ne promet pas une capacité mémoire illimitée : traiter les limites réelles
sans réduction silencieuse des données ni exclusion implicite d’une modalité.

## Préservation Desktop

Conserver l’ouverture, l’utilisation et la sauvegarde des projets existants.
Le refactor des objets runtime et le format réseau n’exigent pas de changer le
contrat de sauvegarde. Préserver champs sérialisés, IDs et résolution des références.
Si une modification de type sérialisé est réellement nécessaire, maintenir la
lecture historique et exposer son impact ; ne pas déduire une rupture du refactor.

Préserver également les références Unity des scènes/prefabs et leurs GUID utiles.
Ces références d’assets sont distinctes du contrat des fichiers projets.

## Hors de cette passe

- Nouvelles interactions scientifiques Quest : gizmos de coupe, contrôles de
  timeline, sélection de sites, matrices d’essais et nouvelles fenêtres.
- Synchronisation continue de configurations ou commandes entre appareils,
  collaboration, gestion de conflits et reprise de journaux réseau.
- Import ou préparation de projets EEG directement sur Quest.
- Sauvegarde/reprise de la session Quest après arrêt et format de projet portable.
- Fusion/séparation des colonnes, qualification Mac/Linux et perfection graphique.
- Nouveaux algorithmes ou changements scientifiques destinés à alléger le contenu.

L’adaptation nécessaire du rendu existant au Quest fait partie de la migration.
Un obstacle constaté doit être corrigé ou présenté comme un écart précis ;
il ne permet pas de déclarer la fonctionnalité terminée sans son rendu.

## Règle de portée

Réutiliser le code et les conventions existantes, mais ne pas limiter l’extraction
aux seuls chemins du prototype. Les changements adjacents nécessaires dans
managers, outils, prefabs, chargement ou bibliothèques natives font partie du
chantier. Éviter les nettoyages sans rapport et les architectures anticipant des
besoins non demandés. Demander une décision seulement pour une véritable nouvelle
incertitude produit ; les choix ordinaires d’implémentation restent autonomes.
