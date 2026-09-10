# Réalisation rapide et stabilisation finale

## Priorité explicite du propriétaire

La demande du 10 septembre est de concentrer le temps sur du code fonctionnel,
en implémentant davantage avant de tester. Les bugs ordinaires seront surtout
détectés et corrigés à la fin de l’implémentation.

Cette politique remplace les obligations intermédiaires de l’ancien dossier et
des fiches Quest historiques qui imposeraient compilation, batteries de tests,
builds Windows/Android ou recettes manuelles à chaque étape.
Les règles de sécurité d’exécution des AGENTS.md restent applicables.

## Trois grands lots, aucune porte de qualification intermédiaire

| Lot | Travail | Validation planifiée |
| --- | --- | --- |
| A : SCENE-001–003 | Inventaire ciblé, généralisation scène/colonnes/managers, extraction des opérations UI et raccordement Desktop | Lecture du code et contrôle des références utiles ; aucune campagne obligatoire |
| B : SCENE-004–007 | Ressources, MNI local, transfert complet, restauration, colonnes Quest et suppression des chemins remplacés | Intégration du parcours complet ; aucune campagne obligatoire à chaque fiche |
| C : SCENE-008 | Compilation, détection des bugs, corrections, vérification des fonctionnalités et qualification finale | Campagne regroupée et proportionnée sur la version intégrée |

Les lots A et B peuvent s’enchaîner. Leurs frontières ne nécessitent pas d’arrêt.
Des états intermédiaires non compilés, incomplets ou temporairement cassés sont
acceptés dans le chantier. Ne pas écrire du code de transition uniquement pour
garder chaque sous-étape présentable. Préserver les fichiers/projets de l’utilisateur.

## Pendant l’implémentation

Par défaut :

- inspecter les sources et suivre les appels ; contrôler les références lors des
  déplacements et tenir une courte liste des risques/défauts à reprendre ;
- implémenter plusieurs changements cohérents avant toute vérification coûteuse ;
- ne pas lancer une compilation Unity, changer de cible ou construire un Player
  uniquement parce qu’une fiche vient de se terminer ;
- ne pas lancer toutes les suites EditMode/PlayMode ni écrire un test par méthode ;
- ne pas demander de validation manuelle intermédiaire ;
- ne pas réexécuter les campagnes du prototype pour établir une baseline déjà
  documentée ; réutiliser fixtures et résultats applicables comme références ;
- corriger une erreur évidente quand elle est rencontrée, sans déclencher pour
  autant une campagne de non-régression.

Une vérification intermédiaire est justifiée uniquement si elle débloque la suite :
par exemple un contrat natif incertain dont dépend l’implémentation, ou un problème
de chargement/compilation empêchant un travail nécessaire dans l’éditeur.
Expliquer brièvement ce qu’elle doit trancher, choisir le plus petit contrôle
utile, puis reprendre l’implémentation. Ce n’est pas un prétexte pour requalifier
les deux plateformes.

Il n’y a **pas de smoke Android intermédiaire obligatoire**, même au premier
parcours intégré. Le décider seulement si un obstacle concret le justifie.
La compilation automatique déclenchée par Unity ne doit pas être confondue avec
une demande de validation : ne pas multiplier refresh, reload et changements de
cible ; regrouper les éditions lorsque le workflow de l’éditeur le permet.

Les règles d’intégrité et de durée de vie guident le code dès le départ. Une revue
indépendante bornée, conformément aux AGENTS.md pour l’architecture/concurrence,
peut repérer les erreurs de conception par lecture. Ne pas la répéter pour
chaque fiche ni la transformer en batterie de tests.

## Stabilisation après l’intégration

1. Formater les C# concernés selon AGENTS.md, vérifier les références et compiler
   la version intégrée. Corriger les erreurs de compilation en regroupant les
   changements autant que possible.
2. Exécuter les tests existants ciblant les chemins modifiés : chargement et
   sauvegarde Desktop, données/calculs, capture/restauration, cycle de vie.
   Ajouter seulement les tests qui protègent un risque réel non couvert.
3. Exercer les fonctionnalités via les APIs communes : mesh, timeline, coupe,
   sites/ROI et cas propres aux modalités. Un petit scénario automatisé ou
   diagnostic suffit lorsque l’UI Quest n’existe pas ; pas de framework générique
   de commandes à développer pour tester.
4. Corriger les défauts par groupes cohérents, puis rejouer les contrôles affectés.
   Une correction locale n’impose pas de recommencer toute la campagne.
5. Construire Windows et Android depuis les mêmes sources intégrées et vérifier
   le parcours réel Desktop → transfert complet → rendu/calcul autonome Quest.
6. Fournir une recette manuelle regroupée : résultat initial, colonnes indépendantes,
   lisibilité et manipulation. Les opérations sans UI sont vérifiées par l’agent,
   pas demandées au propriétaire comme des gestes impossibles.

Les essais réels doivent couvrir les modalités existantes avec des fixtures
représentatives, dont plusieurs colonnes ; éviter le produit cartésien de toutes
les options et plateformes. Réserver les cas combinatoires automatisables aux
contrôles ciblés. Si une fixture manque, en préparer une minimale valide ; demander
des données utilisateur seulement si l’incertitude ne peut pas être résolue ainsi.

Comparer les résultats à des références adaptées en conservant unités, repères et
tolérances scientifiques justifiées. Ne pas recopier aveuglément des assertions
liées à l’ancien format ou au prototype mono-instant.

## Scénarios prioritaires de la campagne finale

- Projets Desktop existants ouverts, utilisés, sauvegardés et rechargés.
- Modalités anatomie/densité, iEEG, CCEP, MEG, fMRI et statique réellement restaurées.
- Plusieurs colonnes et ressources alternatives ; données de sites/essais/instants préservées.
- Changement local de mesh, de temps et de coupe avec rendu correspondant.
- Manipulation indépendante, MNI local et exploration après déconnexion.
- Remplacement valide/invalide, annulation/fermeture durant préparation ou calcul,
  absence de publication tardive et partage/libération corrects.
- Temps de préparation/transfert et pics mémoire sur les fixtures utilisées,
  afin d’identifier les limites concrètes du contenu complet.

Écrire les critères et résultats manquants comme non vérifiés. Une compilation
réussie ne prouve pas le fonctionnement sur casque ; un contrôle différé ne doit
pas empêcher de poursuivre le code.

## Rapports, compilations et qualification historique

Un journal court pendant A/B, un rapport et un manifeste final en C.
Aucun rapport détaillé ou paquet de preuves imposé par sous-tâche.
Pas de recompilation pour le seul changement d’un numéro de rapport.

La campagne finale couvre aussi les besoins encore pertinents de QUEST-024 sur
les mêmes binaires. Fournir la correspondance et les écarts ; ne pas rejouer
une campagne identique pour clôturer séparément 024. Les qualifications Mac/Linux
restent ultérieures. Ne pas présenter leurs résultats comme acquis.
