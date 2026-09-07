# Points d'extension et fonctionnalités futures

Statut : directions documentées, hors tâches du prototype sauf mention explicite.
Ce document ne demande pas d'implémenter des interfaces, commandes ou services
vides pour anticiper ces fonctions. Ajouter une extension quand sa tranche entre
dans le périmètre, avec fiche QUEST dédiée et critères de review.

## Frontières à préserver dès le socle

| Point d'extension | Responsabilité actuelle | Futur consommateur | Ce qu'il faut préserver maintenant | Ce qu'il ne faut pas anticiper |
| --- | --- | --- | --- | --- |
| Entrée spatiale Quest | Contrôleurs → pose/échelle du groupe local | Mains, autres gestes | Intentions locales indépendantes du modèle scientifique ; prefab configurable | Framework universel d'input ou copie des gestes Desktop |
| Présentation | Surface/sites depuis données communes | Transparence, autres backends GPU, plusieurs instances | Couleurs/tailles/masques scientifiques communs ; ownership GPU explicite | Hiérarchie multi-colonnes complète avant besoin |
| Source de session préparée | Capture Desktop ou réception Quest | Fichier portable local | Format/identités/version et entrées explicites, sans appel obligatoire au loader de projet Desktop | File picker, archives et migration de projet complète |
| Pipeline scientifique | Densité/iEEG depuis entrées préparées | Coupe, timeline, ROI | Même implémentation, ressources et conventions pour les deux Players | Service scientifique omnipotent ni refonte de toutes les modalités |
| État scientifique | Snapshot identifié, provenance | Commandes bidirectionnelles | Séparer état scientifique et disposition locale ; IDs durables | Journal générique, CRDT ou politique de fusion implicite |
| Transport | Livraison fiable d'un snapshot | Commandes puis plusieurs sessions | Framing/version et cycle de connexion séparés de la vie du contenu | Protocole multiclient complet ou découverte automatique |
| Ressources/stockage | Cache et fichiers techniques de session | Sauvegarde/export/reprise | Propriétaire explicite et chemins locaux résolus | Persistance après kill déduite de l'existence d'un cache |
| Natif | Sources/ABI épinglées par plateforme | ARM/NEON, nouveaux calculs | Parité et paramètres de performance distincts des paramètres scientifiques | Mini-moteur Quest ou paramètres de science réduits pour accélérer |
| Composition | Profils Desktop/Quest | Autres OS/casques si décidés | Manifeste unique et adaptateurs de plateforme | Autre projet Unity de production |

## Catalogue des fonctions

Les dépendances ci-dessous expriment un ordre technique. L'ordre produit reste :
prototype Windows A–D, qualification Mac, décision Linux, puis priorisation des
fonctions suivantes avec le propriétaire. Aucun ordre détaillé futur n'est validé.

### F01 — Manipulation aux mains

- Valeur : explorer sans contrôleurs, avec déplacement/rotation/échelle du groupe.
- Point d'entrée : adaptateur d'entrée de QUEST-008, prefab rig de QUEST-004.
- Dépendance : gestes aux contrôleurs et socle qualifiés.
- Décisions à poser : gestes proches/lointains, passage mains/manettes, retour de saisie.
- À préserver : même pose de présentation locale, aucun effet sur Desktop.
- Preuve future : mêmes gestes, perte/reprise du suivi, absence de sauts et validation de confort.

### F02 — Transparence du cerveau en passthrough

- Valeur : voir les contacts internes sans masquer complètement la surface.
- Point d'entrée : backend URP/Meta et masque visuel de QUEST-014.
- Dépendance : renderer et composition physique Quest qualifiés.
- Décisions : rendu attendu, compromis lisibilité/profondeur et contrôles d'opacité.
- Preuve : test physique sur passthrough, contacts lisibles, profondeur/occlusion
  cohérentes et coût GPU mesuré. Le golden Desktop P05 ne suffit pas.
- Situation actuelle : remplacé dans le prototype par afficher/masquer, selon D19.

### F03 — Plusieurs colonnes ou instances

- Valeur : comparer conditions, patients synthétiques ou résultats en parallèle.
- Point d'entrée : IDs/bindings du snapshot, ownership des assets et parent spatial.
- Dépendance : transfert/replacement d'une session stable.
- Décisions : disposition, sélection, nombre visé et indépendance des paramètres.
- Preuve : même surface partagée sans copie inutile, libération d'une instance
  sans affecter les autres, masques/états attribués à la bonne colonne.
- Source candidate : P09, sans reprendre automatiquement son autorité canonique.

### F04 — Timeline locale

- Valeur : naviguer/relire une séquence préparée après transfert, hors ligne.
- Point d'entrée : contrat d'entrées iEEG et sampling commun de QUEST-020/021.
- Dépendance : iEEG d'un instant qualifiée.
- Décisions : séquence transférée, calcul/préchargement, lecture et commandes UX,
  mémoire acceptable, comportement des sites entre échantillons.
- Preuve : parité temporelle sites/surface, progression fluide, budget mémoire
  et absence d'appel Desktop par frame. P11 est une preuve de preload limitée,
  pas une décision de tout précalculer dans le nouveau produit.

### F05 — Coupes scientifiques locales

- Valeur : manipuler un plan de coupe et calculer sa section sur Quest.
- Point d'entrée : pipeline commun, Volume/Surface et adaptateur gizmo.
- Dépendance : hbp_core runtime et modèle de ressources validés.
- Décisions : nombre/plans, geste, feedback pendant calcul, périmètre anatomique/activité.
- Preuve : même SetCutPlane/service pour les deux plateformes, conventions de
  coordonnées, parité et gestion d'un calcul terminé après un geste plus récent.
- À exclure : transformer le gizmo en copie de science ou hériter du Desktop
  calculateur unique de l'ancien P12.

### F06 — Sites, sélection et ROI

- Valeur : désigner un contact, lire ses informations, sélectionner ou modifier une ROI.
- Point d'entrée : IDs de sites, picking de présentation et règles communes de masque.
- Dépendance : géométrie/sites et pipeline correspondant qualifiés.
- Décisions : portée d'une sélection, attributs éditables, ROI supportées et feedback.
- Preuve : cible correcte malgré échelle/pose, masques scientifiques identiques,
  sélection locale distinguée d'une modification scientifique partagée.
- Source candidate : P10 picking et expérience HoloLens, sans dupliquer les règles.

### F07 — Commandes scientifiques entre Desktop et Quest

- Valeur : changer coupe/timeline/paramètres depuis l'une des applications et
  répercuter la modification sur l'autre, tout en gardant leurs vues indépendantes.
- Point d'entrée : état scientifique/IDs et services communs, pas transform de caméra.
- Dépendance : au moins une fonctionnalité locale modifiable qualifiée.
- Décisions indispensables : source de vérité choisie (Desktop ou Quest), scopes,
  changement de source, édition concurrente et reconnexion.
- Preuve : commande journalisée avant envoi, acquittement, ordre/idempotence,
  accusé perdu, rupture réseau et conflit réel distingués. Aucun latest-wins
  global ne remplace cette politique.
- Direction utilisateur : choix de la source de vérité envisageable, à définir
  à ce moment ; aucune fusion scientifique silencieuse.

### F08 — Projet portable et ouverture locale

- Valeur : ouvrir une visualisation préparée sans Desktop connecté au lancement.
- Point d'entrée : source de session et formats versionnés/manifestes.
- Dépendance : données préparées suffisantes et stables.
- Décisions : format d'archive, capacités incluses, compatibilité de versions,
  parcours d'export/import et emplacement de stockage.
- Preuve : ouverture sur casque sans réseau, assets complets, intégrité,
  refus de version incompatible et absence de chemins de machine d'origine.
- Distinction : un fichier NIfTI technique reçu n'est pas encore une capsule de projet.

### F09 — Sauvegarde, autosave et reprise

- Valeur : conserver les changements d'une session ou les retrouver après arrêt.
- Point d'entrée : modèle commun, état local de travail et stockage.
- Dépendance : état sauvegardable défini ; export portable utile mais pas imposé
  si une autre sauvegarde est explicitement choisie.
- Décisions : ce qui est sauvegardé, geste explicite ou automatique, fréquence,
  reprise après kill/reboot et traitement d'une session Desktop divergente.
- Preuve : contenu atomique et restaurable, perte contrôlée en cas d'arrêt
  interrompu, distinction entre données persistantes et présentation locale.

### F10 — Performances et endurance ARM

- Valeur : réduire calcul/copies et garder une expérience stable sur sessions longues.
- Point d'entrée : options de concurrence et moteur natif commun, upload/render.
- Dépendance : mesures froid/chaud et parité initiale.
- Décisions : profils/volumétries cibles, durée de session et compromis graphiques.
- Preuve : amélioration mesurée sans modification de signification scientifique ;
  parité des chemins scalaires/optimisés, thermique, mémoire et batterie.
- NEON ou scheduling sont des options motivées par le profiling, pas des prérequis.

### F11 — Import EEG directement sur Quest

- Valeur : créer de nouvelles entrées sans préparation Desktop.
- Point d'entrée : import/source de données et éventuel EEGFormat multiplateforme.
- Dépendance : besoin produit explicite et formats ciblés ; hors V1 initiale.
- Décisions : formats, UI d'import, volume et qualité des métadonnées.
- Preuve : fidélité import/export par format, native runtime et erreurs utilisateur.
- Ne pas confondre autonomie d'exploration, ouverture de projet et autonomie d'import.

## Passage d'une extension à une tâche

Lorsqu'une extension est demandée : lire cette entrée et les contrats réellement
implémentés, poser seulement ses décisions ouvertes, créer des fiches QUEST
suivant TASK-WORKFLOW.md et réviser les dépendances. Le modèle de rapport reste
identique. Les points d'extension ne doivent jamais justifier une tâche plus
large que la fonctionnalité explicitement choisie.
