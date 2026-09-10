# Généraliser la 3D existante

## Principe

Conserver l’organisation utile de Base3DScene, Column3D et des managers.
Déplacer ou adapter le minimum de code qui dépend indûment de Desktop.
Une classe commune doit porter la vraie implémentation utilisée des deux côtés,
pas masquer deux copies de la même fonctionnalité.

MonoBehaviour, GameObjects, Mesh et composants de rendu peuvent rester communs.
Un modèle .NET pur, une architecture sans renderer et une séparation systématique
données/services/vues ne sont **pas** des exigences. Le code commun doit fonctionner
sans fenêtre, toolbar, View3D/Camera3D Desktop factices ni dépendance réseau.

## Frontières pratiques

| Responsabilité | Placement attendu |
| --- | --- |
| Scène, colonnes, opérations et règles existantes | Classes 3D communes, autant que possible à leur emplacement logique actuel |
| Chargement depuis un projet Desktop | Préparation existante adaptée pour alimenter ces classes |
| Restauration depuis un transfert | Autre voie d’alimentation des mêmes classes et données |
| Générateurs, géométrie, coupes, résultats, matériaux réutilisables | Collaborateurs communs existants ou adaptés |
| Fenêtres, render textures de vues, caméras Desktop, souris et contrôles | Présentation Desktop |
| Caméra du casque, saisie et pose indépendante de chaque cerveau | Présentation Quest |
| Livraison de contenu et progression réseau | Transport/intégration ; aucune logique de fonctionnalité dans le récepteur |

L’héritage, des composants associés ou des méthodes spécialisées sont possibles.
Choisir selon les dépendances observées, sans imposer une classe abstraite pour
chaque classe actuelle. Les variantes de présentation ne possèdent pas une copie
modifiable concurrente des paramètres ou règles scientifiques.

## Ce que montrent les sources inspectées

- Desktop : Column3D.Initialize appelle AddView ; Base3DScene mélange opérations,
  vues, préférences et rendu. Ces couplages doivent être adaptés.
- Quest : QuestAnatomyView possède encore données natives, résultats et calculs.
  Ce chemin spécifique au prototype doit être remplacé par la vraie scène commune.
- Les travaux 018–023 mutualisent des calculateurs et conventions ; l’orchestration
  Desktop et NativeProjectionInputs n’est pas entièrement commune. Ne pas supposer
  qu’un pipeline commun complet existe déjà.
- HoloLens : dans les sources inspectées, il n’y a pas de classes View3D/Camera3D
  actives équivalentes ; la caméra du casque et les interactions de scène/colonne
  remplacent ces responsabilités. Les gestes MRTK sont notamment mêlés à
  Base3DScene/Column3D et les coupes pilotées par HoloLens/UI/CutsController.
- UI Desktop : MoveSites choisit un plan anatomique dans ses callbacks ;
  TimelineSlider applique le changement à des groupes de colonnes.
  Extraire les opérations réutilisables de ces callbacks, conserver leurs contrôles.

Ces constats sont des points de départ, pas un audit exhaustif de chaque fonction.
Consulter HiBoP_HoloLens en lecture seule et l’historique feature/xr sans changer
le checkout. Les commandes historiques figurent notamment au commit 2f0abc4d2
dans Shared/Packages/com.crnl.hibop.contracts/Runtime/Commands.cs ; elles ne
constituent pas une dépendance à intégrer dans cette passe.

## Opérations, configuration et état courant

Une action Desktop ou un futur contrôle Quest appelle la même opération avec une
cible explicite. Les widgets lisent le résultat et adaptent leur présentation.
Les règles de groupe de colonnes, de sélection valide, de masques et d’invalidation
ne doivent pas être recodées dans chaque UI.

Réutiliser VisualizationConfiguration et les configurations des colonnes/sites.
Leur contenu ne correspond pas à tout l’état runtime : certaines valeurs sont
recopiées lors de SaveConfiguration, et certains états temporels sont ailleurs.
La capture doit prendre l’état effectivement courant, pas une ancienne
configuration sauvegardée. Ne pas modifier le projet, sa sélection ou les vues
pour fabriquer l’export.

Les vues et caméras décrites dans la configuration restent des informations
Desktop ; elles ne dictent pas la pose des cerveaux Quest. Étendre la représentation
d’état seulement lorsque nécessaire à une restauration fidèle. Pas de nouvelle
autorité d’état parallèle sous prétexte de préparer les futures commandes.

## Rendu et repères

Le changement de mesh, d’instant ou de coupe par une opération commune doit
actualiser le rendu sur Quest, même en l’absence de bouton ou gizmo.
Réutiliser géométrie, matériaux et shaders lorsqu’ils sont compatibles ; spécialiser
les contraintes de rendu de plateforme lorsqu’il le faut.

Séparer les coordonnées scientifiques de la pose spatiale. Déplacer, tourner ou
agrandir une colonne Quest ne modifie pas les distances de projection, les données
de sites ou les plans scientifiques. Convertir correctement les plans et positions
pour leur affichage dans chaque colonne. Ne pas imposer les layers/culling masks
de vues Desktop comme modèle d’identité ou de disposition Quest.

## Propriété des ressources et travail asynchrone

Identifier les propriétaires réels des volumes, surfaces, sites, générateurs et
buffers. Partager les ressources immuables entre colonnes et scènes quand pertinent ;
garder les paramètres et buffers mutables indépendants. Éviter les copies de
grands volumes, sans introduire un gestionnaire universel de ressources.

Une vue Desktop ou un contrôle retiré ne ferme pas implicitement la scène commune.
La destruction explicite du contenu/colonne Quest peut fermer son objet commun :
une indépendance absolue de tous les GameObjects n’est pas exigée.

Une fermeture ou un remplacement empêche toute publication tardive. Les ressources
utilisées par un calcul natif restent vivantes jusqu’à sa terminaison réelle.
Annuler une attente ne suffit pas à autoriser leur libération. Une scène fermée ne
doit pas arrêter globalement les ressources partagées par les autres.

## Critères à vérifier principalement en stabilisation

| ID | Critère observable |
| --- | --- |
| C01 | Desktop et Quest utilisent les mêmes implémentations des fonctionnalités migrées. |
| C02 | Une scène Quest fonctionne sans objets de présentation Desktop ni connexion après réception. |
| C03 | Les outils Desktop ne sont plus propriétaires des règles nécessaires aux fonctionnalités communes. |
| C04 | État, invalidation et publication sont cohérents, sans résultat tardif ni deuxième autorité. |
| C05 | Plusieurs colonnes restent indépendantes tout en partageant les ressources appropriées. |
| C06 | La manipulation spatiale ne modifie pas les coordonnées ou paramètres scientifiques. |
| C07 | Fermeture, remplacement et échec ne provoquent pas de libération prématurée ni contenu partiel publié. |
| C08 | Projets Desktop, configurations et références Unity existants restent utilisables. |

Ces critères orientent le code dès le départ. Ils ne constituent pas huit portes
de tests intermédiaires. Leur preuve finale peut combiner inspection des appels,
tests ciblés et parcours réels.
