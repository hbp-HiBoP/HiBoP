# Interfaces et interactions Quest HiBoP

Décisions produit validées le 7 octobre 2026. Lots 01 à 06 implémentés et validés dans le casque. Lots 07 et 08 implémentés le 8 octobre ; recette de la sonde encore à réaliser. Les lots 09 et suivants restent à implémenter.

Le Quest dispose d'un système de fenêtres réutilisable, d'une toolbar horizontale persistante et d'un curseur universel. La sélection des sites utilise une petite sonde activée par maintien de A. Les coupes sont toutes accessibles dans une fenêtre et manipulables par des poignées de proximité, sans sélection préalable d'une coupe.

La charte HiBoP constitue le premier lot. Les interactions spatiales restent locales ; les changements scientifiques utilisent les opérations communes et leur synchronisation existante. La sélection automatique de scène et colonne après interaction avec un cerveau est explicitement reportée à [T17](sync/tasks/T17.md).

## Périmètre et articulation avec la synchronisation

Ce plan précise une première tranche produit de [T15](sync/tasks/T15.md). Il ne remplace ni la [matrice des opérations](sync/operation-matrix.md), ni les contrats d'autorité, de réplication, de jobs et de cycle de vie.

- T15 relie les contrôles aux opérations et jobs existants, avec application locale optimiste lorsque le contrat le prévoit. Aucun nouveau type d'opération ni calcul métier alternatif n'est introduit.
- Les composants locaux de thème, fenêtre, toolbar, ciblage et feedback peuvent être livrés avant les contrôles scientifiques.
- Les couvertures incomplètes des handlers nécessaires sont complétées avant d'exposer le contrôle correspondant. L'existence d'un DTO ou d'un codec ne constitue pas une preuve de comportement sur les deux drivers.
- La première tranche travaille avec la scène unique publiée actuellement. Elle capture néanmoins l'identité de la cible à chaque geste pour éviter qu'un changement de contexte ne redirige une manipulation.
- T17 ajoute le routage multi-scène et la sélection automatique du contexte sur les deux appareils. Les gestes ne commencent pas à sélectionner arbitrairement une autre scène en attendant T17.

La roue au joystick, le hand tracking, un panel séparé d'information scène/colonne et de nouvelles fonctions scientifiques ne font pas partie de cette tranche. La toolbar conserve des emplacements stables ; une fonction indisponible reste identifiable et désactivée.

## Charte graphique et thème Quest

Le Quest utilise le moteur de thème commun, avec ses propres assets de composition. Aucun comportement ni composant spécifiquement Desktop n'est réutilisé pour les fenêtres ou les interactions Quest.

- Créer de nouveaux `Element`, assets d'état et autres ScriptableObjects nécessaires : fenêtre, barre de contrôle, poignée, toolbar, bouton principal/secondaire/destructif, toggle, dropdown, slider, champ numérique, textes, ligne de coupe et feedback de ciblage.
- Ne pas référencer les `Element` ou états Desktop depuis les compositions Quest. Un bouton Quest ne référence pas `MainButton` ; un nom tel que `QuestMainButton` est indicatif jusqu'à fixation des conventions.
- Seuls les assets `Settings` peuvent être partagés après vérification de leur rôle commun. Les couleurs de la charte sont partagées pour qu'une modification de palette atteigne les deux plateformes.
- Les icônes sont elles aussi des assets `Settings`, de type `HBP.Theme.Image` : le sprite est porté par `SourceImage`, avec les réglages de rendu associés. Les compositions Quest les référencent par leur thème, plutôt que d'assigner un sprite hors de ce système. Un Setting Image existant peut être partagé si son contenu et ses réglages conviennent aux deux plateformes ; sinon créer un Setting Image Quest sans modifier l'existant. La couleur reste pilotée par les Settings de couleur appropriés.
- Créer des réglages Quest pour textes, dimensions, espacements et disposition. Ne pas modifier les réglages Desktop afin de rendre le Quest lisible.
- Respecter les couleurs, polices, sprites, conventions d'icônes, hiérarchie des informations et états HiBoP. Adapter les tailles et zones interactives au casque.
- Utiliser des états distincts et lisibles pour survol, pression, activation, désactivation et erreur. Le feedback des poignées 3D se distingue de celui des boutons sans créer une palette concurrente.
- La couleur du rayon provient du thème Quest. L'orange du prototype n'est pas une exigence à conserver.

Les prefabs et assets Quest sont identifiables et séparés des compositions Desktop. L'emplacement exact est fixé au premier lot selon les conventions du dépôt. Le système commun de thème reste réutilisé ; un second moteur de thème n'est pas nécessaire.

Pour toute nouvelle icône, rechercher d'abord un symbole existant adapté. Si une icône doit être créée ou générée, par exemple pour ouvrir Connexion depuis la toolbar, prendre les icônes HiBoP existantes comme références explicites : même style de silhouette ou de trait, épaisseur, proportions, taille visuelle, marges, traitement du fond et conventions de couleur. La création ou génération ne justifie pas une nouvelle famille graphique. Intégrer le résultat dans un Setting Image et le comparer aux icônes voisines dans la toolbar, dans ses états utiles et à la taille réelle dans le casque, avant livraison.

## Fenêtres spatiales

`QuestWindow` représente le cycle de vie, le contenu et l'interactivité d'une fenêtre. Des composants Quest séparés fournissent déplacement, suivi et redimensionnement facultatif. Ces noms, hormis `QuestWindow`, sont indicatifs ; les responsabilités constituent le contrat.

Chaque fenêtre possède une barre inférieure avec une poignée de déplacement, une fermeture et les actions Suivre/Épingler. Le contenu de pairing ou de coupes ne gère pas lui-même la pose de la fenêtre. Le déplacement s'engage avec la gâchette sur la poignée ; le contrôle à distance constitue la première interaction requise.

### Suivi et épinglage

Le mode Suivre est le comportement initial. La fenêtre reste stable lors des petits mouvements de tête. Si elle sort durablement de la zone confortable, elle revient progressivement vers un emplacement accessible, sans masquer par défaut la zone principale du cerveau.

Le déclenchement et l'arrêt du retour utilisent des seuils différents et une temporisation. Le suivi est suspendu lorsque la fenêtre est ciblée, utilisée, déplacée ou redimensionnée : l'utilisateur doit pouvoir regarder et atteindre sa cible sans qu'elle s'éloigne. La référence est la direction du casque, sans dépendance au suivi oculaire.

Épingler conserve le placement dans l'espace de présentation Quest jusqu'à déplacement ou rappel explicite. Cette première version ne promet pas une ancre persistante entre lancements. Après un déplacement, la fenêtre conserve une orientation lisible, sans roulis libre. Le rappel explicite peut rendre accessible une fenêtre épinglée devenue hors champ.

### Redimensionnement facultatif

Le redimensionnement est un composant ajoutable à une fenêtre. La fenêtre de connexion n'en possède pas. Les poignées de redimensionnement et la taille minimale lisible sont configurables ; le geste ne peut pas se transformer en déplacement ou clic de contenu.

Le composant modifie la largeur ou la hauteur du panneau par quatre poignées de bord, sans changer le scale ; le bord opposé reste fixe. Le contenu utilise des ancres ou un layout adaptés pour occuper la surface gagnée, avec textes et boutons de taille constante. Ce comportement est validé dans le prefab de démonstration, sans ajout au pairing.

### Ouverture et fermeture

Une entrée de toolbar ouvre sa fenêtre si elle est fermée. Si elle est déjà ouverte, elle la rappelle sans créer de doublon et sans la fermer. L'action Fermer reste sur la fenêtre.

Le gestionnaire ne construit pas les objets UI à la volée : il utilise des prefabs authored avec leurs références sérialisées. Les fenêtres peuvent être instanciées depuis ces prefabs ; le choix de conserver ou recréer une instance fermée ne doit pas changer le comportement observable.

### Gestionnaire de fenêtres Quest

Créer un `QuestWindowsManager` propre au Quest dès le lot 03. Cette responsabilité est nécessaire pour centraliser ouverture/rappel sans doublon, registre des fenêtres et recentrage depuis la toolbar ; chaque bouton ou contenu ne doit pas réimplémenter ces comportements. Le gestionnaire Desktop sert de référence de responsabilités, sans réutilisation ni héritage de ses classes, de son registre statique ou de ses conteneurs d'écran.

- Référencer explicitement les prefabs et éventuelles instances déjà présentes dans les prefabs Quest. La fenêtre de connexion initiale est enregistrée dans le même registre que les fenêtres ouvertes ensuite, sans seconde instance créée par un autre chemin.
- Associer chaque fenêtre à une clé explicite ; la première tranche conserve une instance par fonction, y compris fermée, pour garder son contenu et ses références. Centraliser ouvrir, retrouver, rappeler et fermer ; retirer les instances détruites du registre. Les fenêtres fermées sont exclues du rappel global et des interactions.
- Coordonner le placement initial et le rappel des fenêtres ouvertes, en déléguant la pose aux composants spatiaux concernés et en respectant les gestes en cours.
- Fournir les événements locaux d'ouverture/fermeture et l'état d'ouverture nécessaires à la toolbar, sans dépendre d'elle ni du contenu de pairing ou de coupes.

Les paramètres métier, la connexion, les opérations de synchronisation et l'arbitrage du curseur restent dans leurs composants respectifs. Le gestionnaire ne partage aucun état de fenêtres avec le Desktop et ne persiste pas leur disposition entre lancements dans cette tranche.

## Fenêtre de connexion

L'état de connexion, le contenu de la fenêtre et la décision de la montrer sont indépendants.

- Au démarrage sans association, ouvrir la fenêtre de connexion automatiquement.
- Fermer la fenêtre masque uniquement l'interface. Cela n'arrête pas le pairing, ne remplace pas l'association et ne renouvelle pas implicitement le code.
- La toolbar permet de la rouvrir pendant toute la session.
- Avant association, afficher le code et les actions pertinentes. Une fois connecté, afficher l'association et l'état actuel, sans code devenu inutile.
- Le rafraîchissement du statut ne doit pas annuler une fermeture manuelle. L'arrivée d'une visualisation ne rend pas l'interface impossible à consulter.
- Une perte de connexion produit un état discret dans la toolbar et la fenêtre consultable, sans imposer sa réouverture au milieu d'un geste.
- Conserver les confirmations et restrictions métier existantes pour remplacer une association, réessayer et renouveler le pairing.

## Toolbar horizontale persistante

La toolbar est ouverte en permanence, compacte, droite et placée sous la zone principale de travail. Les boutons portent une icône et un libellé visible. Elle reste disponible avant pairing, après publication et lorsqu'une fenêtre est fermée.

| Entrée | Comportement initial |
| --- | --- |
| Connexion | Ouvrir ou rappeler la fenêtre de connexion ; rendre son état accessible. |
| Coupes | Ouvrir ou rappeler la fenêtre de coupes ; désactiver l'entrée sans scène valide. |
| Affichage | Accéder aux commandes d'affichage déplacées depuis les raccourcis, notamment visibilité du cerveau et recalcul de projection. Conserver les règles métier et de jobs propres à chaque action. |
| Recentrer les fenêtres | Rappeler les fenêtres ouvertes et accessibles, sans ouvrir celles que l'utilisateur a fermées ni déplacer les cerveaux. |

La commande existante de recentrage des cerveaux est accessible dans Affichage. Le lot de retrait inventorie les bindings actuels : conserver les gâchettes pour les gestes de déplacement/rotation/échelle, retirer les raccourcis d'affichage et de recalcul, et traiter explicitement le raccourci de recentrage selon son rôle dans les manipulations conservées. Aucun nouveau raccourci de fonction ne remplace la toolbar.

La toolbar utilise un suivi horizontal différé. Regarder vers le bas ne la fait pas descendre davantage : elle conserve une hauteur lisible par rapport à une pose de référence. Un changement durable de direction la ramène progressivement. Elle reste stable lorsqu'elle est ciblée ou utilisée. Les distances, décalages, seuils et vitesses sont des réglages à qualifier dans le casque.

La première version ne la masque pas automatiquement pendant le travail et ne demande pas un geste secret pour la retrouver. Le rayon n'apparaît pas simplement parce qu'elle est ouverte. Une commande de rappel/recentrage ne déplace pas une fenêtre actuellement manipulée : son effet sur cette fenêtre attend la fin du geste ou est indisponible tant qu'il dure.

## Curseur universel et arbitrage des interactions

Le curseur n'est lié ni au pairing, ni à une classe de fenêtre particulière. Il fournit détection, choix de cible, capture du geste et feedback aux fenêtres, à la toolbar et aux interactions 3D futures.

Les gâchettes servent aux boutons, aux poignées et aux manipulations des cerveaux. Une pression n'engage qu'une interaction par main. Une cible capturée reste la même jusqu'au relâchement ou à son invalidation ; traverser une autre cible pendant un geste ne lui transfère pas l'action. Deux mains peuvent participer au geste de cerveau déjà supporté.

La détection au rayon est indépendante de son dessin. L'affichage comporte un curseur sur la cible et un rayon discret uniquement lorsque cela aide à comprendre l'action. Le choix de la cible est visible avant la pression.

| Situation | Feedback et comportement |
| --- | --- |
| Aucune cible accessible | Rayon masqué. |
| Fenêtre, toolbar ou poignée accessible ciblée | Curseur et feedback adapté ; rayon discret si utile. |
| Interaction au rayon engagée | Conserver son feedback jusqu'à la fin du geste, même hors de la zone initiale. |
| Sonde de sites active | Masquer le rayon pour la main concernée et montrer la sonde ; éviter une saisie concurrente par cette main. |
| Cerveau ou poignée de coupe saisi directement | Feedback de manipulation de proximité ; pas de rayon superposé pour cette main. |
| Tracking perdu, application interrompue ou cible supprimée | Retirer le feedback et terminer proprement l'interaction. Une pression déjà maintenue ne redémarre pas une saisie au retour du tracking. |

Une cible de proximité clairement signalée peut primer sur une fenêtre distante. Une UI ne peut pas capter automatiquement toutes les pressions au seul motif qu'elle est ouverte. La politique précise et les seuils sont regroupés dans une configuration modifiable, plutôt que répartis dans les fenêtres. Les paramètres règlent l'ergonomie ; les invariants de capture et d'exclusion des gestes restent obligatoires.

Supprimer les pavés bleus et oranges de diagnostic des contrôleurs sans supprimer leur suivi de pose. Supprimer les raccourcis existants autres que ceux nécessaires aux manipulations des cerveaux, après avoir livré les accès explicites qui remplacent les commandes conservées.

## Sélection immédiate des sites

Le maintien de A affiche une toute petite sphère près du contrôleur droit, bien plus petite qu'un site. Son chevauchement avec la sphère d'un site éligible sélectionne immédiatement ce site. Il n'existe ni préselection, ni délai de validation, ni verrouillage jusqu'au relâchement.

L'utilisateur peut parcourir successivement les sites d'une électrode pendant le même maintien. Sortir d'un site ou relâcher A conserve la dernière sélection. Un bref retour haptique accompagne chaque changement effectif ; rester dans le même site ne republie pas la sélection et ne répète pas la vibration.

Un appui sur A pendant un geste déjà capturé par cette main ne le remplace pas : cet appui est ignoré et la sonde demande un nouvel appui lorsque la main est libre. La perte de tracking masque la sonde, réinitialise son contact et demande un relâchement puis un nouvel appui.

Le contact est géométrique, sans réponse physique. La sélection ne possède pas de zone d'attraction cachée. Si plusieurs sites sont simultanément touchés, une règle déterministe privilégie le centre le plus proche, avec une résolution stable des égalités. Après recette utilisateur, seule la collision à la pose courante est retenue : aucun site traversé entre deux images n'est sélectionné. Sortir de contact conserve la dernière sélection.

La sonde utilise correctement le placement, la rotation et l'échelle locaux du cerveau, sans écrire ces transformations dans le référentiel scientifique. L'identité transmise est celle de la colonne et du site préparés. Les restrictions métier existantes sur les sites inconnus ou masqués continuent de s'appliquer.

La politique contact/sélection est isolée du dessin de la sonde et de la publication métier. Elle pourra être remplacée ultérieurement sans refaire les fenêtres ou le protocole.

## Consultation et modification des coupes

La fenêtre contient toutes les coupes de la scène, chacune avec ses contrôles consultables et modifiables sans étape préalable de sélection. Une liste défilante est admise si le nombre de coupes dépasse la place disponible ; un mode n'affichant que les paramètres d'une coupe sélectionnée ne l'est pas.

Chaque ligne expose orientation, position avec slider et pas fins, flip et suppression. L'ajout crée une coupe par l'opération commune. L'orientation custom donne accès aux valeurs X/Y/Z de la normale du plan, clairement distinguées de sa position. Ces valeurs sont validées ensemble ; un vecteur nul ou une valeur non finie n'est pas publié. La saisie possède un accès utilisable avec les manettes, sans supposer un clavier physique.

Ouvrir la fenêtre active les aides de manipulation des coupes. Une commande visible permet de suspendre ces aides en gardant les paramètres consultables. Fermer la fenêtre termine le geste éventuel, désactive les aides et ne supprime aucune coupe. Rappeler une fenêtre déjà ouverte ne réinitialise pas ses paramètres ni son état de suspension.

Les coupes sont des objets de scène : manipuler leur représentation sur une colonne peut affecter leur rendu dans les autres colonnes de cette scène. La fenêtre indique cette portée sans imposer l'affichage de la scène/colonne sélectionnée. L'éventuel panel d'information de contexte reste distinct.

### Poignées et gizmos de proximité

Toutes les coupes sont accessibles lorsque les aides sont actives. Chaque coupe possède un contour de plan, un rail suivant sa normale et une poignée extérieure saisissable. Le plan entier n'est pas une poignée. Aucune flèche supplémentaire ni sélection persistante de coupe n'est requise.

- Les aides restent masquées ou discrètes à distance, puis deviennent visibles près de la manette. Des seuils distincts d'apparition/disparition évitent le clignotement.
- Le feedback annonce la poignée qui recevra la pression. Approcher ou survoler ne change pas une sélection scientifique.
- La gâchette engage la poignée et capture la coupe, la scène et la représentation utilisée pour convertir le geste.
- Le déplacement est contraint le long de la normale du plan. Déplacer tangentiellement la manette ne décale pas la coupe sur un autre axe.
- Le gizmo demeure visible pendant le geste. Il est possible de relâcher une poignée et d'en saisir immédiatement une autre, sans revenir à la fenêtre.
- Les poignées extérieures et leur feedback permettent de distinguer les coupes qui se croisent. Plusieurs aides peuvent être visibles ; une main ne capture qu'une cible.
- Les deux mains ne modifient pas simultanément la même coupe. La cible occupée est signalée. Une fermeture de scène ou une suppression distante de coupe termine le geste sans le réorienter.

Pendant une saisie de coupe, la représentation de cerveau qui sert de référence n'est pas manipulable simultanément par l'autre main. Réciproquement, une poignée de coupe ne démarre pas un geste sur une représentation déjà déplacée à la main. Cette exclusion conserve une conversion stable entre déplacement de manette et position anatomique ; elle ne désactive pas toutes les interactions de l'autre main.

La présentation des gizmos, leur proximité et leurs poses sont locales. Position, orientation et flip utilisent une définition scientifique complète, identifiée par un `CutId` stable. Le déplacement applique localement les setters communs ; les aperçus sont regroupés selon le contrat existant et la dernière valeur valide est publiée de manière fiable à la fin. Une interruption clôt le flux avec la dernière valeur valide tant que la cible existe ; une cible invalidée ne reçoit pas une nouvelle mutation.

## Frontière entre présentation et mutations

| Contrôle ou événement | Chemin attendu |
| --- | --- |
| Ouvrir, fermer, suivre, épingler, déplacer ou redimensionner une fenêtre | Présentation locale, aucune mutation scientifique. |
| Déplacer ou mettre à l'échelle un cerveau dans le Quest | Transformation locale du wrapper ; sélection de contexte automatique ajoutée en T17. |
| Sonde, contact, haptique et ciblage | Feedback local ; seul un changement effectif de site utilise `SetSelectedSite` et les sélections communes requises. |
| Ajouter ou supprimer une coupe | `CreateCut` / `DeleteCut` existants ; identité stable et publication structurale fiable. |
| Slider, pas fin, orientation, flip, normale custom ou poignée de coupe | `SetCutDefinition` existant, avec définition complète et valeur finale fiable. |
| Affichage et recalcul | Conserver la frontière actuelle entre présentation locale et opération/job scientifique ; documenter chaque commande avant son raccordement. |
| Association et retry | Services de connexion existants, distincts des mutations de visualisation. |

Les mises à jour venues du Desktop rafraîchissent les contrôles sans écho. Les gestes n'introduisent ni capture globale de scène, ni attente réseau bloquante, ni duplication des calculs dérivés.

Une édition de coupe conserve l'intention du contrôle utilisé et construit les définitions complètes depuis l'état courant traité par le driver, plutôt que depuis une copie figée au début du geste. Une modification distante de flip ou d'orientation ne doit pas être annulée involontairement par la dernière valeur d'un slider de position. Les conflits sur le même paramètre, les corrections et les propositions en attente suivent le contrat d'autorité existant ; si le chemin actuel ne fournit pas ce comportement, le lot de qualification le complète avant exposition. Tester ce cas pour slider, saisie custom et poignée, sans inventer une autorité concurrente dans l'UI.

## Étapes d'implémentation

Les lots sont ordonnés et livrables séparément, avec les dépendances ci-dessous. « Indépendant » signifie qu'un lot a son propre résultat vérifiable et peut être arrêté après livraison ; cela ne signifie pas qu'il peut ignorer les fondations précédentes. Aucun lot ne doit laisser le pairing ou la manipulation actuelle inutilisables.

Les composants et prefabs de démonstration restent dans les outils ou fixtures de validation adaptés, sans raccourci temporaire livré aux utilisateurs. Les noms de tests sont fixés pendant l'implémentation, avec preuves de résultats réels dans le compte rendu du lot.

### Lot 01 Thème et composants visuels Quest

**Dépendances :** aucune fonctionnalité nouvelle.

**Livraison :** famille d'assets de thème Quest et prefabs de composants de base, avec références de couleurs partagées, icônes via Settings Image et réglages de texte/disposition propres au casque. Identifier les icônes existantes réutilisables et les symboles manquants ; toute nouvelle icône respecte les références graphiques HiBoP. Présenter une fixture visuelle couvrant tous les états utiles.

**Vérification :** inspecter les références pour exclure `Element`/états Desktop ; vérifier qu'un changement de dimension Quest n'affecte pas Desktop et qu'une couleur partagée est appliquée aux deux compositions. Vérifier les références aux Settings Image et comparer les nouvelles icônes aux références existantes. Dans le casque, vérifier lisibilité, contrastes et cibles avec les manettes. L'absence d'effet sur le Desktop est un critère de livraison.

### Lot 02 Curseur universel et capture des interactions

**Dépendances :** lot 01 pour le feedback.

**Livraison :** détection invisible, feedback contextuel, politique de priorité configurable et capture d'une interaction par main. Adapter le pairing et la saisie des cerveaux à ce routage sans changer leurs commandes métier ni déplacer la saisie vers les grips.

**Vérification :** clic de pairing sans saisie du cerveau derrière, saisie de cerveau malgré une UI distante, absence de transfert de cible pendant maintien, manipulation à deux mains conservée, rayons masqués sans cible, perte/retour du tracking sans reprise involontaire. Vérifier que ces gestes de présentation ne publient pas de mutation scientifique.

### Lot 03 Fenêtre générique et migration du pairing

**Dépendances :** lots 01 et 02.

**Livraison :** `QuestWindow`, `QuestWindowsManager`, prefab de fenêtre, barre/poignée et déplacement ; migration du pairing vers la fenêtre de connexion, avec séparation état/contenu/visibilité et enregistrement de son instance initiale. Une seconde fixture de contenu vérifie la réutilisation sans dépendance au pairing. La fermeture manuelle reste désactivée dans l'interface livrée tant que la toolbar du lot 05 ne fournit pas sa réouverture ; le cycle fermeture/réouverture est déjà vérifiable dans la fixture.

**Vérification :** fermeture maintenue pendant les rafraîchissements de connexion, réouverture avec état actuel, consultation après arrivée de la scène, absence de doublon et conservation des confirmations métier. Vérifier ouverture répétée via le gestionnaire, enregistrement de l'instance initiale, exclusion des instances fermées du rappel/interactions, nettoyage du registre après destruction et absence de dépendance aux classes Desktop. Dans le casque, déplacer la fenêtre sans action du contenu ni saisie d'un cerveau. L'accès de production pour rouvrir arrive au lot 05 ; la fixture vérifie déjà le cycle complet.

### Lot 04 Suivi différé et épinglage

**Dépendances :** lot 03.

**Livraison :** composant de suivi avec zone de tolérance, temporisation, retour progressif, suspension pendant ciblage/geste et mode Épingler. Remplacer le suivi instantané de la fenêtre de connexion.

**Vérification :** petits mouvements sans déplacement, sortie durable déclenchant un retour, absence d'oscillation près du seuil, fenêtre stable pendant lecture/clic/déplacement, épinglage immobile et rappel explicite. Qualifier en casque les seuils et vitesses ; consigner les valeurs retenues.

### Lot 05 Toolbar et retrait des contrôles de prototype

**Dépendances :** lots 01 à 04 et chemins métier/jobs existants des commandes d'affichage conservées.

**Livraison :** toolbar persistante à quatre entrées et suivi horizontal différé, ouverture/rappel des fenêtres via `QuestWindowsManager`, accès Affichage aux commandes conservées. Intégrer les icônes via leurs Settings Image et vérifier leur cohérence côte à côte. Qualifier dans ce lot chaque action scientifique d'Affichage avant activation, en complétant si nécessaire ses seules vérifications/entrées existantes. Activer la fermeture manuelle de la connexion avec sa réouverture désormais disponible. Coupes reste désactivé jusqu'au lot 09. Retirer les pavés des contrôleurs et les raccourcis redondants ; conserver les commandes nécessaires aux manipulations des cerveaux.

**Vérification :** accès Connexion avant/après pairing et après fermeture, rappel sans doublon, recentrage des seules fenêtres ouvertes, cible stable en regardant vers le bas, actions d'affichage/recalcul par leurs chemins existants. Pour chaque action scientifique, inspecter les preuves des drivers/jobs concernés et compléter les cas manquants avant activation ; pour chaque action locale, vérifier l'absence de mutation partagée. Inventorier les bindings conservés/supprimés et vérifier le tracking et la saisie à deux mains après retrait des pavés.

### Lot 06 Redimensionnement optionnel

**Dépendances :** lots 02 à 04 ; indépendant des contrôles scientifiques.

**Livraison :** composant de redimensionnement ajoutable et prefab de démonstration. Fixer pour cette démonstration une politique de taille, ses bornes et son comportement de contenu. Ne pas ajouter ce composant à la connexion.

**Vérification :** geste stable, taille minimale lisible, absence de mouvement de suivi pendant redimensionnement et absence d'action du contenu. Deux fenêtres, avec et sans composant, doivent rester utilisables. Tous les changements de taille restent locaux.

### Lot 07 Qualification des opérations sites et coupes

**Dépendances :** handlers/jobs existants concernés et drivers actuels ; peut être réalisé avant les lots 03 à 06. Les contrôles de sites et coupes s'appuient notamment sur T09/T10.

**Livraison :** cartographie des entrées métier, complétion ciblée des handlers nécessaires et tests manquants requis pour sélection de site, création/suppression et modification complète de coupe. Les actions scientifiques d'Affichage ont leur qualification propre dans le lot 05. Ne pas étendre ce lot aux familles de T09 à T14 que cette tranche n'expose pas.

**Vérification :** driver Quest vers Desktop et sens inverse, application optimiste/correction selon le contrat, absence d'écho, site inconnu/masqué, création acceptée et suppression d'une coupe non finale avec identités conservées. Vérifier une modification distante d'un champ de coupe pendant l'édition locale d'un autre, et les corrections de propositions en attente. Les rejets ne doivent pas rendre le driver inutilisable. La qualification réussie des deux drivers est un prérequis effectif de chaque contrôle concerné aux lots 08 à 10 ; reporter tout handler restant incomplet sans activer son contrôle.

### Lot 08 Sonde de sites

**Dépendances :** lots 02 et 07 pour les opérations correspondantes, et retrait du binding A de visibilité du cerveau au lot 05.

**Livraison :** sonde sur maintien de A, sélection immédiate successive, conservation du dernier site et retour haptique bref. Isoler la politique de contact de la publication et du visuel.

**Vérification :** un contact par site malgré les mouvements rapides, absence de répétition dans le même site, résolution déterministe des chevauchements, identité correcte sous rotation/échelle du cerveau, relâchement conservant la sélection et rejet des sites inéligibles. Tester A pendant une saisie capturée et la perte/retour du tracking pendant maintien de A, sans démarrage ni trajet involontaire. Dans le casque, parcourir une électrode de quinze sites en environ dix secondes comme scénario de recette, sans préselection ni retour au menu. Vérifier la convergence Desktop et la correction/rejet si nécessaire.

### Lot 09 Fenêtre de paramètres des coupes

**Dépendances :** lots 01 à 05 et 07 pour les opérations correspondantes.

**Livraison :** accès Coupes activé, liste de toutes les coupes, ajout/suppression, position et pas fins, flip, orientations et normale custom avec saisie aux manettes. Installer le cycle ouverture/suspension/fermeture du mode d'aide, prêt pour le lot 10.

**Vérification :** chaque contrôle rejoint son handler, édition de plusieurs lignes sans sélection préalable, valeurs custom invalides non publiées, mise à jour Desktop vers UI sans écho, suppression non finale et nouvel ajout avec identités correctes. Dans le casque, consulter et modifier plusieurs coupes, utiliser la saisie numérique et vérifier que fermer la fenêtre ne supprime rien.

### Lot 10 Poignées de coupes et manipulation contrainte

**Dépendances :** lots 02, 07 et 09.

**Livraison :** contour/rail/poignée extérieure pour chaque coupe, feedback par proximité, capture sur gâchette et déplacement suivant la normale. Pas de surface de plan entièrement saisissable, de flèche supplémentaire ni de coupe sélectionnée persistante.

**Vérification :** passer entre plusieurs coupes sans retour au menu ; différencier des plans croisés ; déplacer une coupe oblique sur sa normale ; conserver la bonne conversion sous rotation/échelle du cerveau ; garder le gizmo pendant le geste. Tester l'exclusion d'une saisie concurrente de cerveau ou de la même coupe par l'autre main, la suppression distante et la fermeture de fenêtre/scène. Vérifier les aperçus et la dernière valeur fiable Quest vers Desktop, sans mutation pour un simple survol.

### Lot 11 Recette intégrée et réglages finaux

**Dépendances :** lots 01 à 10.

**Livraison :** réglages de proximité, suivi et feedback retenus à l'issue de la recette et rapport de validation de cette tranche T15. Les ajustements restent dans les composants/configurations concernés.

**Vérification :** enchaîner pairing, fermeture/rappel, déplacement/épinglage, manipulation d'un cerveau, parcours de sites et modification de plusieurs coupes ; vérifier que les modes ne se capturent pas mutuellement. Vérifier une interruption de tracking, les contrôles rafraîchis depuis Desktop et l'état de connexion sans réouverture intrusive. Exécuter les régressions ciblées rendues pertinentes par ces ajustements ; conserver séparément résultats automatisés, recette casque et limites restantes.

## Exécution et preuves par lot

Chaque lot laisse le projet compilable et fournit les fichiers/prefabs modifiés, les contrôles exposés, leur chemin métier et les résultats inspectés. La recette casque valide le confort ; un test de logique ne prouve pas la lisibilité ou la facilité de saisie.

- Authored UI et gizmos sont édités dans les prefabs appropriés directement dans Unity, avec références sérialisées.
- Utiliser l'éditeur HiBoP connecté via MCP. Si un démarrage CLI devient nécessaire, suivre la version du projet et les règles d'exécution hors sandbox du dépôt.
- Respecter la direction des assemblies ; toute modification de référence `.asmdef` passe le gate statique et ne crée aucune dépendance feature vers Core en sens inverse.
- Exécuter les vérifications EditMode/PlayMode ciblées réellement nécessaires, attendre leurs jobs et inspecter leurs résultats ainsi que les erreurs de console. Aucun résultat antérieur ne vaut validation du nouveau lot.
- Les tests Unity/UniTask restent asynchrones et non bloquants.
- Pour un lot modifiant du C#, exécuter le formatter du dépôt une fois, après toute implémentation, validation et revue, comme dernière action avant le compte rendu.

## Réglages à qualifier sans rouvrir les décisions produit

Restent à mesurer dans le casque : distances et dimensions des fenêtres/toolbar, taille physique de la sonde, zones de proximité des poignées, seuils/délais de suivi, vitesses de retour, intensité haptique, longueur du rayon visible et dimensions des contrôles.

Les noms finaux des assets et composants, la politique du prefab de démonstration redimensionnable et la présentation de la saisie numérique sont des choix d'implémentation dans les limites ci-dessus. Ils ne réintroduisent pas une roue, une préselection de site, une sélection persistante de coupe ou une dépendance aux compositions Desktop.

## Références de conception

Les conventions spatiales Meta inspirent la barre et les poignées, sans remplacer la charte HiBoP ni imposer une migration de SDK : [fenêtres](https://developers.meta.com/vr/design/windows/), [boutons et rangées d'accès](https://developers.meta.com/vr/documentation/spatial-sdk/spatial-sdk-ui-button/), [UI dans une scène 3D interactive](https://developers.meta.com/vr/design/hands-3d-best-practices/) et [détection au rayon et feedback](https://developers.meta.com/vr/design/raycasting_specs/).


## Livraison des lots 01 à 05 — 7 octobre 2026

Implémentation et qualification ciblée dans Unity 6000.5.2f1, sans casque connecté. Les rendus de fixtures ont été inspectés dans l'éditeur ; ils ne qualifient pas le contraste en passthrough ni le confort réel. Aucun APK construit ou déployé pendant cette passe. T15 reste partiellement livré : sonde, coupes et qualification de leurs opérations restent aux lots suivants.

### Assets et composants livrés

| Lot | Résultat concret |
| --- | --- |
| 01 | `Assets/Resources/Themes/Quest/` contient les Elements/états Quest, typographies et dimensions propres, couleurs HiBoP partagées et icônes via Settings Image. `Assets/Prefabs/Quest/UI/` contient bouton, toggle, slider, dropdown, champ texte et galerie de thème. Aucun Element/état Desktop référencé par les compositions Quest. |
| 02 | `QuestPointerInput`, `QuestInteractionPolicy`, capture par main : gâchettes universelles, feedback bleu contextuel, priorité de proximité configurable, annulation au tracking perdu/interruption/fermeture. Le popup du dropdown possède sa caméra et son raycaster pour manettes. |
| 03 | `QuestWindow`, `QuestWindowsManager`, `QuestWindowDragger`, prefab générique, fenêtre Connexion et seconde fenêtre Affichage. Références sérialisées dans `QuestBootstrap.prefab`. Fenêtres indépendantes de la caméra, fermeture sans arrêter la connexion, réouverture sans doublon, déplacement capturé par une seule main. |
| 04 | `QuestWindowFollower` : suivi différé, hystérésis, suspension au ciblage/manipulation, épinglage et rappel explicite. Placement initial repris lorsque le suivi du casque devient disponible. |
| 05 | Toolbar Connexion/Coupes/Affichage/Recentrer, état de connexion, suivi horizontal ; fermeture du pairing disponible. Actions Affichage raccordées aux entrées existantes. Pavés de contrôleurs et ancien `QuestPairingUIInput` retirés. Coupes désactivé jusqu'au lot 09. |

Les instances fermées restent enregistrées et leur racine reste active ; Canvas, contenu et raycaster sont désactivés. Cela conserve le rafraîchissement de connexion, ferme les popups et annule les poignées, sans fenêtre invisible interactive. La destruction retire l'instance du registre. Le catalogue permet d'ajouter d'autres prefabs de fenêtres.

L'icône Connexion est une silhouette de lien blanche sur la grille Material 24 dp utilisée par HiBoP, avec SVG source et PNG dans `Assets/Sprites/Google Material/`. Les autres icônes reprennent la bibliothèque existante. Le Setting Image Quest porte le sprite ; aucun sprite n'est imposé par le comportement de toolbar.

### Commandes conservées et chemins d'exécution

| Contrôle | Chemin et portée |
| --- | --- |
| Gâchettes gauche/droite | UI ou manipulation du cerveau selon la capture ; déplacement/rotation/échelle à deux mains conservés, transformations de présentation locales. |
| X gauche | Recentrage des cerveaux conservé comme commande de manipulation ; également accessible dans Affichage. |
| Ancien A et clic du joystick droit | Raccourcis visibilité/recalcul retirés. A est libre pour la future sonde du lot 08. |
| Affichage : visibilité | `QuestAnatomyView.ToggleSurface()` ; masque local du rendu de surface, sans mutation scientifique partagée. |
| Affichage : recalcul | `QuestAnatomyView.RecalculateProjection()` → `Scene.InvalidateActivityField()` et `Scene.UpdateGenerator()` ; chemin existant du job, pas de calcul alternatif ni nouvelle opération. |
| Affichage : recentrer les cerveaux | `QuestAnatomyInput.RecenterBrains()` ; placement local. |
| Toolbar : Recentrer | `QuestWindowsManager.RecenterOpen()` ; rappelle les seules fenêtres ouvertes, y compris épinglées, ignore celles en manipulation. |
| Connexion | Réessayer/remplacer l'association gardent leurs confirmations et restrictions existantes. Fermer/rouvrir ne change ni l'identité ni le code. |

Les actions d'Affichage sont désactivées sans scène ou pendant sa préparation. L'état Connected/Offline utilise le transport de synchronisation V2, distinct de la fin d'un transfert de publication.

### Vérifications automatiques exécutées

Derniers runs ciblés : **18 EditMode + 18 PlayMode réussis, 0 échec, 0 test ignoré**. Ce n'est pas une exécution de toute la suite du dépôt.

- EditMode : `HBP.Tests.PlatformConfiguration.QuestBootstrapTests`, `HBP.Tests.PlatformConfiguration.QuestUIAssetTests` et les deux tests `HBP.Tests.Transfer.Scene.V2SceneMutationBoundaryTests.ExplicitGeneratorUpdate_RequestsProjectionWhileAutomaticStaleStateRemainsGated` / `QuestRecalculateProjection_RequestsAnExplicitUpdateWhenAutomaticPolicyIsDisabled`.
- PlayMode : `HBP.Tests.Quest.QuestWindowInteractionTests`, `HBP.Tests.Quest.QuestUniversalPointerTests`, les quatre tests de prefab/carte/initialisation/géométrie de `QuestPairingTests`, et `HBP.Tests.PlayMode.Module3D.Module3DScenePlayModeTests.PrepareRendering_WaitsForExplicitProjectionRequestedFromReadyUntilItCompletes`.
- Les tests vérifient les références des prefabs, l'isolation des compositions Quest, les captures et priorités, le clic UI réel et la manipulation à deux mains avec contrôleurs simulés, l'absence de mutation du mesh scientifique, le retour de tracking sans reprise, le déplacement/fermeture de fenêtre, le suivi et l'épinglage, la toolbar sans doublon et un choix de dropdown au rayon.
- `Tools/check-assembly-dependencies.ps1` réussit : 44 assemblies HBP, 158 dépendances directes. Quest dépend du moteur Theme commun, sans dépendance inverse de Core ni ajout de composants UI Desktop.
- Console Unity inspectée après les tests : aucune erreur.

Résultats JSON des derniers runs : `Logs/QuestUI/EditMode-results.json`, `Logs/QuestUI/PlayMode-results.json`. Captures de fixtures inspectées : `Logs/QuestUI/Quest Theme Gallery Runtime.png` et `Logs/QuestUI/Quest Windows and Toolbar.png`. Ces fichiers de preuve sont locaux, dans le répertoire de logs ignoré par Git.

Pour rejouer, utiliser Unity Test Runner avec les classes/méthodes ci-dessus, ou les filtres MCP correspondants, puis inspecter les résultats et la console. Exécuter le contrôle de dépendances après une modification d'asmdef. La galerie de thème est une fixture, pas une fenêtre de production ; la saisie numérique avec manettes reste à livrer au lot 09.

### Réglages initiaux à qualifier dans le casque

Les valeurs sont sérialisées et modifiables dans les assets/prefabs, sans modifier les contenus des fenêtres.

| Réglage | Valeur initiale |
| --- | --- |
| Fenêtres : seuil de retour / seuil d'arrêt / délai / vitesse exponentielle | 45° / 15° / 0,8 s / 2,5 s⁻¹, autour de leur emplacement relatif souhaité |
| Toolbar : mêmes paramètres, suivi horizontal | 35° / 10° / 1 s / 2 s⁻¹ |
| Placement Connexion / Affichage | (-0,47 ; 0,05 ; 1,15) m / (0,47 ; 0,05 ; 1,15) m par rapport au casque |
| Placement toolbar | (0 ; -0,5 ; 1,15) m, rotation horizontale indépendante de l'inclinaison du casque |
| Échelle UI / textes Body, Small, Title, Code | 0,0015 m par unité / 28, 22, 36, 58 |
| Politique de rayon | portée 3 m, largeur 2 mm, préférence au cerveau proche activée, couleur partagée HiBoP |

### Recette manuelle dans le Quest

Installer une build intégrant ces changements lorsque le casque sera reconnecté ; aucune présence du casque n'est nécessaire pour poursuivre la revue du code ou les tests ci-dessus.

1. **Démarrage et charte** : Connexion et toolbar visibles, Affichage fermé ; vérifier lisibilité du code, labels/icônes, tailles des cibles et contraste sur plusieurs fonds réels. Aucun pavé de contrôleur. Ouvrir la galerie en fixture séparée si nécessaire pour vérifier toggle, slider, dropdown et états des boutons ; vérifier également que le Desktop garde son apparence.
2. **Connexion complète** : fermer la fenêtre, laisser expirer/renouveler le code puis rouvrir via toolbar ; son contenu doit être actuel. Associer depuis Desktop et publier une scène avec la fenêtre fermée ; rouvrir après réception. Vérifier les confirmations de remplacement et le Retry dans les états où ils sont autorisés. Couper/rétablir la connexion : statut Offline/Connected sans réouverture imposée ni arrêt de la visualisation.
3. **Fenêtres** : ouvrir/rappeler plusieurs fois sans doublon ; déplacer par la poignée avec chaque gâchette, sortir de la poignée en gardant l'appui, relâcher. La seconde main ne déplace pas la même fenêtre. Fermer ne clique pas le contenu ou le cerveau derrière ; rappeler une fenêtre épinglée doit rester possible.
4. **Suivi** : petits mouvements de tête sans poursuite instantanée ; rotation prolongée déclenchant un retour doux ; pas d'oscillation près du seuil. Pointer une fenêtre doit arrêter son mouvement. Tester Pin/Follow. Regarder la toolbar vers le bas ne doit pas la faire descendre ; tourner durablement doit la ramener.
5. **Curseur et cerveaux** : aucun rayon sans cible ; feedback annoncé avant pression, UI utilisable avec chaque main. Cerveau proche prioritaire sur une UI distante. Déplacement/rotation/échelle à deux mains conservés. Traverser un bouton pendant une saisie ne le clique pas. Masquer un contrôleur puis le retrouver en maintenant la gâchette : pas de reprise avant relâchement/nouvel appui. Tester aussi veille/reprise et menu système.
6. **Affichage et recentrage** : boutons désactivés sans scène ; après publication, visibilité locale sans modification du Desktop, recalcul de projection aboutissant par le job existant, recentrage des cerveaux. X reste disponible ; A et clic joystick droit ne déclenchent plus ces actions. Recentrer dans la toolbar ne déplace aucun cerveau, ne rouvre aucune fenêtre fermée et ne déplace pas celle en cours de manipulation. Coupes reste désactivé.

Noter les réglages à ajuster après cette recette, surtout placement/tailles, délais et priorité de proximité. Le lot 06 est livré séparément ci-dessous ; les lots 07 et suivants restent à implémenter.

## Ajustements après première recette dans le casque — 7 octobre 2026

- Suppression du feedback sphérique autour des cerveaux proches et pendant leur saisie. La petite cible de rayon UI reste disponible sur les interfaces ; la saisie distante du cerveau affiche uniquement le rayon.
- Les surfaces raycastables des fenêtres gérées maintiennent le feedback entre les boutons, comme la toolbar.
- Saisie distante activée dans `Quest Interaction Policy` (`EnableDistantAnatomy`, désactivable). Une pression de gâchette capture le cerveau visé ; le point de saisie suit le rayon à la profondeur initiale jusqu'au relâchement. Une fenêtre devant le cerveau a priorité. La cible reste identique pendant l'appui ; tracking perdu/interruption demande un relâchement avant une nouvelle saisie. Les gestes proches à deux mains restent disponibles ; rejoindre un geste distant exige de viser le cerveau capturé ou de s'en approcher réellement.
- Le prefab `QuestColumn` porte un collider de ciblage propre au Quest, raccordé au mesh de la colonne. Le remplacement du mesh et le changement de représentation invalident cette cible. Aucun collider ajouté aux prefabs scientifiques Desktop.
- Première proposition de chargement remplacée après recette : conserver le design original cercle/cerveau et sa bande de statut, avec des dimensions adaptées au Quest (voir correction ci-dessous). `ILoadingPresenter` permet au `LoadingManager` existant de garder les mêmes tâches et la même annulation, avec le présentateur Desktop inchangé.
- Chargement placé au centre à 1,15 m à son ouverture ; petits mouvements de tête sans poursuite instantanée. Retour doux après une déviation supérieure à 25° pendant 0,4 s, arrêt dans 8°, vitesse exponentielle 3 s⁻¹ ; gel sans tracking. Aucun pin ni contrôle de fenêtre.

Qualification ciblée : **17 EditMode + 15 PlayMode réussis**, console sans erreur, contrôle des dépendances réussi (44 assemblies, 158 dépendances). Classes `QuestUIAssetTests`, `QuestBootstrapTests`, `QuestUniversalPointerTests`, `QuestWindowInteractionTests`, `QuestLoadingTests`. Couverture : références Quest/Desktop, charte indépendante, fond de fenêtre ciblable, vraie capture distante de mesh, jonction de seconde main, non-mutation scientifique, priorité de fenêtre, tracking perdu, invalidation de géométrie en place, progression/annulation et suivi différé. Rendu du chargement inspecté dans `Logs/QuestUI/Quest Loading.png`.

Recette complémentaire dans le casque :

1. Approcher puis saisir un cerveau : aucune sphère bleue. Vérifier les gestes proches de déplacement/rotation/échelle avec les deux gâchettes.
2. Pointer les espaces entre les boutons d'Affichage : rayon continu ; appuyer dans le fond ne déclenche aucune action.
3. À distance, viser un cerveau puis maintenir la gâchette ; déplacer/orienter la manette, sortir du mesh en gardant l'appui, relâcher. Vérifier l'absence de saut, la capture stable et la précision. Une fenêtre devant lui doit bloquer cette saisie. La seconde manette ne rejoint pas le geste distant si elle ne vise pas le cerveau ; tester aussi une jonction volontaire.
4. Tester perte/reprise de tracking avec gâchette maintenue, changement de représentation anatomique/gonflée et nouvelle publication de scène ; la cible doit suivre le mesh actuel.
5. Pendant transfert/recalcul suffisamment long : vérifier statut lisible, cercle et cerveau progressifs, annulation si proposée. Petites rotations de tête sans suivi immédiat ; tourner durablement pour constater le retour doux au centre. Qualifier la fluidité sur un cerveau dense et le confort du déplacement distant.

Build de cette recette : `.artifacts/quest-feedback-20261007/HiBoP.Quest.apk`, Unity 6000.5.2f1, build Development réussie (0 erreur, 48 avertissements), APK vérifié par `Tools/Test-QuestApk.ps1` (353 574 537 octets, 9 bibliothèques ARM64). Déploiement initialement différé, puis installation USB demandée et réussie, sans effacer les données d'association. Profil DesktopWindows réactivé dans l'éditeur pour la recette avec synchronisation.

## Correction du chargement et des volumes de saisie — 7 octobre 2026

- Le chargement Quest reprend le prefab original : cercle avec cerveau animé par les mêmes images de progression, bande de statut séparée et petite croix ronde pour annuler. Le cercle passe de 200 à 260 unités, la bande à 780 × 110 et les textes à 28. Aucun pourcentage ajouté ni panneau de fenêtre. Le suivi différé reste à 25° / 8° / 0,4 s / 3 s⁻¹, sans pin.
- Les ThemeElements et Settings Image/Text/Layout de cette présentation sont propres au Quest ; seules les couleurs sont partagées. Les prefabs et compositions Desktop d'origine restent inchangés.
- La saisie proche utilise exactement `Mesh.bounds` dans le repère du mesh du cerveau, sans marge, en tenant compte de sa rotation et de son échelle. Parmi les volumes contenant la manette, le centre de mesh le plus proche gagne, indépendamment de l'ordre des colonnes. La cible reste capturée pendant l'appui. Le ciblage distant conserve l'intersection du rayon avec le mesh.

Qualification ciblée : **9 EditMode + 4 PlayMode réussis**, classes `QuestUIAssetTests`, `QuestLoadingTests` et `QuestUniversalPointerTests`. Les tests couvrent notamment les dimensions et références du chargement, ses images de progression et son annulation, le suivi différé, les limites exactes d'un mesh transformé et le choix du centre le plus proche après inversion de l'ordre des colonnes. Contrôle des dépendances réussi (44 assemblies, 158 dépendances). Rendu réel de fixture inspecté dans `Logs/QuestUI/Quest Loading.png` ; la lisibilité et le confort restent à qualifier dans le casque.

Recette complémentaire : vérifier le design d'origine et le statut agrandi pendant un transfert/recalcul ; bouger légèrement la tête puis tourner durablement pour comparer immobilité et retour doux. Approcher une manette juste à l'extérieur puis à l'intérieur des limites du cerveau ; dans une zone de chevauchement, saisir alternativement près du centre de chaque cerveau. Relâcher entre les essais pour acquérir une nouvelle cible, puis vérifier les gestes à deux mains et la saisie distante déjà livrés.

Build corrigée : `.artifacts/quest-loading-bounds-20261007/HiBoP.Quest.apk`, Development, 0 erreur et 45 avertissements. APK vérifié (353 544 241 octets, 9 bibliothèques ARM64), installation USB réussie avec conservation des données. Profil DesktopWindows réactivé, éditeur hors Play Mode, scène inchangée et console sans erreur après le retour au profil Desktop.

Contrôle de démarrage matériel incomplet : la demande de lancement est interceptée par l'écran système Quest « contrôleurs requis » ; aucun processus HiBoP n'est lancé lors du contrôle. Réveiller les manettes et lancer l'application pour la recette. Trace locale : `.artifacts/quest-loading-bounds-20261007/startup-logcat.txt`.

### Feedback du rayon à portée de saisie directe

Le rayon et son réticule sont masqués lorsqu'un cerveau peut être saisi directement avec la manette, y compris si le rayon était capturé pour une saisie distante. Ce masquage ne change ni la cible capturée ni le geste ; le feedback distant revient en sortant de portée. Une interaction UI déjà capturée conserve son feedback jusqu'au relâchement. La préférence UI/anatomie continue de déterminer l'action de la gâchette.

Qualification : les deux tests de `QuestUniversalPointerTests` réussissent, console sans erreur. Le test d'interaction vérifie le masquage proche, la conservation de la capture et le retour du feedback distant ; résultat XML local dans `Logs/QuestUI/NearGrabRay-PlayMode-results.xml`. Recette : viser un cerveau à distance, entrer dans sa bounding box puis en sortir, saisir directement avec chaque main et vérifier les interactions UI.

Build : `.artifacts/quest-near-grab-ray-20261007/HiBoP.Quest.apk`, Development, 0 erreur et 45 avertissements ; APK vérifié (9 bibliothèques ARM64) et installation USB réussie sans effacement des données. Profil DesktopWindows réactivé, hors Play Mode, scène inchangée et console sans erreur.

La demande de lancement reste interceptée par l'écran système « contrôleurs requis » ; le contrôle du comportement dans le casque attend le réveil des manettes.

### Diagnostic du contact après nouvelle recette

Le problème persiste dans le casque, avec et sans appui sur la gâchette. L'utilisateur confirme la règle souhaitée : rayon visible à distance, masqué en contact. La capture récupérée (`Logs/QuestUI/fr.crnl.hibop.quest-20261007-173744.jpg`) montre un rayon court devant le cerveau de la colonne 2 ; elle ne permet pas de déterminer les coordonnées du point de saisie.

La revue relève deux origines distinctes : la détection proche utilise `devicePosition` (grip), le rayon utilise `pointerPosition` (aim), dans le même repère de tracking. Un relevé ponctuel dans la build Android Development, demandé via `pointer-status`, enregistre les deux mains sur une même frame de `QuestPointerInput.Process` : positions/contrôles, coordonnées locales et `Contains` de chaque mesh, candidat, capture, décision UI et feedback. Aucun relevé par frame hors demande, aucun changement supplémentaire de règle ni élargissement des bounds. La cause reste à confirmer par la mesure dans le casque avant une nouvelle correction.

Build de diagnostic : `.artifacts/quest-pointer-diagnostic-20261007/HiBoP.Quest.apk`, compilation Android Development réussie (0 erreur, 45 avertissements), APK vérifié (9 bibliothèques ARM64) et installation USB réussie. Profil DesktopWindows restauré, hors Play Mode, scène inchangée et console sans erreur. Le système Quest réclame les manettes au lancement ; la mesure attend la reproduction de la position dans l'application.

Mesure obtenue dans `Logs/QuestUI/pointer-contact-01.json` : gâchette gauche relâchée, grip à 1,25 mm hors des bounds du cerveau repositionné, aim à l'intérieur ; aucune capture ni cible UI, candidat distant à 4,63 cm et rayon visible. Le décalage de pose explique donc le classement distant à cette position.

Correction : considérer les deux points grip/aim contre les mêmes bounds exacts. Le candidat est toujours départagé par la distance entre grip et centre du mesh. Si seul aim est dedans au début de la capture, son origine devient le point de déplacement direct pour toute la durée du geste ; sinon, le comportement grip reste inchangé. Le choix ne bascule pas pendant l'appui et est effacé au relâchement/annulation. Le mode distant reste indépendant, sans marge autour du cerveau.

Les deux tests de `QuestUniversalPointerTests` passent avec poses grip/aim distinctes et repère de tracking réel de la fixture ; console sans erreur. Ils couvrent contact aim seul, capture directe effective sans rayon, origine stable lorsque grip entre ensuite, gestes à deux mains, capture distante et perte de tracking. La fixture précédente n'avait pas de repère de tracking et utilisait uniquement la pose grip, ce qui masquait cette distinction.

Build corrigée : `.artifacts/quest-contact-aim-20261007/HiBoP.Quest.apk`, Development, compilation réussie en 222,84 s (0 erreur, 45 avertissements). APK vérifié (353 589 649 octets, 9 bibliothèques ARM64), installation USB réussie avec conservation des données. Profil DesktopWindows restauré, éditeur hors Play Mode, scène inchangée et console sans erreur. Le système demande de réveiller les manettes au lancement ; validation matérielle de la nouvelle règle encore à effectuer.

La recette dans le casque est ensuite validée par l'utilisateur : le feedback de contact/distance est accepté.

### Première livraison du lot 06 — version remplacée après recette

`QuestWindowResizer` est un composant de poignée indépendant, raccordé par référence sérialisée à une `QuestWindow`. Il utilise le pointeur universel et la gâchette, sans binding supplémentaire ni composant Desktop. Il n'est ajouté à aucune fenêtre existante, ni enregistré dans le bootstrap ou la toolbar. Seul le nouveau prefab `Assets/Prefabs/Quest/UI/Quest Resizable Window Demo.prefab` en possède un.

La première politique est volontairement simple : mise à l'échelle uniforme autour du pivot de la fenêtre, proportions conservées et contenu agrandi ensemble, sans reflow ni modification de ses Settings Text/Layout. Les facteurs minimum/maximum sont réglables dans l'inspecteur, relativement à l'échelle de la fenêtre lors de l'activation initiale du composant. Dans la démonstration, le minimum est 1 et le maximum 1,75 : la fenêtre ne peut pas devenir plus petite que sa taille lisible d'origine. La poignée carrée de 48 × 48 unités est placée au coin inférieur droit et possède un ThemeElement indépendant `QuestResizeHandle` ; ses Settings Image et Color restent partagés avec la charte.

Le geste projette le rayon sur le plan de fenêtre figé au début de la capture ; la projection sur la direction initiale de la poignée donne le facteur de taille. Aucun saut initial ni accumulation entre frames. Une seule main détient le geste, le déplacement et le suivi sont suspendus via `QuestWindow.IsManipulated`, et la poignée ne déclenche aucune action du contenu. Relâchement, fermeture et désactivation libèrent la capture ; la perte de tracking/focus utilise le chemin d'annulation existant du pointeur. La taille reste strictement locale, sans opération de synchronisation.

Pour réutiliser le composant, ajouter dans le prefab de la future fenêtre une poignée UI avec `Image` ciblable, `QuestWindowResizer` et ThemeElement Quest, puis sérialiser la référence `window` et les bornes souhaitées. La démonstration donne le raccordement complet. Aucun objet de poignée n'est construit à l'exécution.

Qualification : **1 EditMode + 4 PlayMode réussis**, résultats dans `Logs/QuestUI/Resize-EditMode-results.xml` et `Resize-PlayMode-results.xml`. Vérifiés : références du prefab, absence du composant dans tous les autres prefabs Quest, limites, proportions/pivot/contenu, geste sans accumulation, suspension du suivi, exclusion de l'autre main et du déplacement, annulation fermeture/désactivation et rayon parallèle. Les deux tests de déplacement/suivi existants passent également. Rendu inspecté dans `Logs/QuestUI/Quest Resizable Window.png`. Trois exceptions transitoires des inspecteurs Unity sont apparues à la fermeture du Prefab Stage ; leurs stacks concernent uniquement `GameObjectInspector`, `RectTransformEditor` et `CanvasScalerEditor`. Après relevé et nettoyage de console, le run PlayMode n'a produit aucune erreur.

Recette ultérieure dans une scène de démonstration Quest : instancier ce prefab à côté d'une fenêtre ordinaire, viser la poignée et maintenir la gâchette pour agrandir/réduire, dépasser les deux bornes, puis vérifier relâchement, fermeture, suivi et tentative avec l'autre main. Le confort du geste reste à qualifier en casque lors de son intégration ; aucune nouvelle build n'est déployée pour ce composant inutilisé dans les fenêtres de production.

### Première scène de prise en main Quest du lot 06

`Assets/_Scenes/QuestUIDemo.unity` est une scène autonome, assemblée dans Unity : rig Quest existant avec passthrough, pointeur universel à deux mains, fenêtre standard à gauche et démonstration redimensionnable à droite. Les panneaux affichent les consignes. La toolbar propose Standard, Taille et Recentrer pour rouvrir/rappeler les fenêtres. Aucun pairing, driver de données ou contenu scientifique n'est initialisé ; les prefabs de production restent inchangés.

Pour construire cette scène, activer le profil Quest puis appeler `HBP.Dev.HBPBuilder.BuildQuestUIDemo(dossier)`. Ce point d'entrée utilise le builder existant en Development et remplace temporairement la scène, le nom du produit et l'identifiant Android. L'application s'appelle **HiBoP UI Demo**, identifiant **fr.crnl.hibop.quest.uidemo**, afin de coexister avec HiBoP. Les paramètres du profil Quest sont restaurés dans `finally`, y compris en cas d'échec. Le contrôle de build n'autorise la scène de démo que pendant cet appel explicite et vérifie son identifiant et son mode Development ; les builds ordinaires restent limités à QuestBootstrap.

Le test PlayMode `DemoScene_WiresTrackedPointerAndReopensBothWindowsFromToolbar` passe : chargement réel de la scène, références de tracking/pointeur/caméra, présence du resizer sur une seule fenêtre, absence de pairing, réouverture des deux fenêtres par les boutons et raycast de la poignée après réactivation du Canvas. Console sans erreur. Résultat dans `Logs/QuestUI/UIDemo-PlayMode-results.xml`, rendu inspecté dans `Logs/QuestUI/Quest UI Demo.png`.

Build `.artifacts/quest-ui-demo-20261007/HiBoP.Quest.apk` réussie en 341,02 s (0 erreur, 45 avertissements). APK vérifié : 353 592 953 octets, 9 bibliothèques ARM64 ; manifeste contrôlé avec `aapt`, nom « HiBoP UI Demo », identifiant `fr.crnl.hibop.quest.uidemo`, seule scène QuestUIDemo. Installation USB réussie. ADB confirme la coexistence des deux packages HiBoP et UI Demo. La demande de lancement est retenue par l'écran système « contrôleurs requis » ; réveiller les manettes et lancer la démo dans les sources inconnues. Le confort réel reste à qualifier par l'utilisateur.

Après build, le nom HiBoP, l'identifiant `fr.crnl.hibop.quest` et la scène QuestBootstrap ont été contrôlés restaurés ; `HBPBuildProfiles.Validate(Android)` réussit, et le profil Quest n'a aucun diff. Profil DesktopWindows réactivé, scène HiBoP inchangée, hors Play Mode et console sans erreur.

### Correction du lot 06 — surface disponible et quatre bords

La recette confirme le confort du geste mais précise l'objectif : augmenter la surface de contenu, sans agrandir ses textes ou ses contrôles. La mise à l'échelle uniforme décrite dans la première livraison est remplacée.

`QuestWindowResizer` modifie maintenant les dimensions du `RectTransform` de la fenêtre, jamais son `localScale`. Chaque poignée possède une direction sérialisée (`Left`, `Right`, `Top`, `Bottom`) et agit uniquement sur cet axe ; le bord opposé reste fixe, y compris avec un pivot décentré et une fenêtre tournée. Le plan et les matrices sont capturés au début du geste pour éviter les sauts et l'accumulation. Les règles de capture exclusive, suspension du suivi et annulation restent celles de la première livraison.

Le prefab de démonstration possède quatre poignées au milieu des bords : 24 × 96 unités à gauche/droite, 96 × 24 en haut/bas, avec le ThemeElement indépendant `QuestResizeHandle`. Les bornes sérialisées portent sur la largeur et la hauteur en unités de layout : minimum 500 × 350, maximum 1200 × 1000 ; taille initiale 600 × 500. Aucun resizer n'est appliqué aux fenêtres de production.

Le titre et la barre de contrôle suivent la largeur, tout en conservant leurs tailles de police et de boutons. Le contenu occupe un viewport étirable avec `RectMask2D` : la largeur adapte le retour à la ligne et la hauteur révèle la suite du texte. Le message de démonstration est volontairement trop long au départ et affiche « FIN DU CONTENU » lorsque la surface suffit. Une future fenêtre devra également définir ses ancres ou son layout pour utiliser l'espace gagné ; le composant ne réorganise pas arbitrairement son contenu.

Qualification : **1 EditMode + 6 PlayMode réussis**, console sans erreur. Vérifiés : raccordement des quatre poignées, absence du composant dans les autres prefabs Quest, chaque direction, axe orthogonal constant, bord opposé fixe, pivot décentré, rotation, limites, absence d'accumulation, scale et taille des textes constants, suspension du suivi, exclusion des gestes concurrents et annulation. La scène réelle vérifie les quatre cibles au rayon après réouverture et le contenu entièrement visible après agrandissement. Résultats : `Logs/QuestUI/ResizeEdges-EditMode-results.xml` et `ResizeEdges-PlayMode-results.xml`. Rendus inspectés : `Quest UI Edges Initial.png` et `Quest UI Edges Expanded.png` dans le même dossier.

Recette casque : dans **HiBoP UI Demo**, maintenir une gâchette sur chacune des quatre poignées, déplacer son bord puis relâcher. Vérifier que seul cet axe change, que le bord opposé reste immobile et que les textes/boutons gardent leur taille. Agrandir en hauteur pour révéler la fin du contenu, puis varier la largeur pour observer le retour à la ligne. Tester les limites, fermeture/réouverture, suivi et tentative avec l'autre main.

Build corrigée : `.artifacts/quest-ui-edges-20261007/HiBoP.Quest.apk`, Development, réussie en 320,97 s (0 erreur, 10 avertissements). APK vérifié : 353 603 425 octets, 9 bibliothèques ARM64, nom « HiBoP UI Demo », identifiant `fr.crnl.hibop.quest.uidemo`, seule scène QuestUIDemo. Installation USB réussie ; les deux packages HiBoP et UI Demo sont présents. Le lancement est intercepté par l'écran système « contrôleurs requis », sans processus de démo : réveiller les manettes et lancer la démo pour la recette.

Après build, restauration contrôlée du nom HiBoP, de l'identifiant `fr.crnl.hibop.quest` et de QuestBootstrap ; validation du profil Android réussie. Profil DesktopWindows réactivé, scène HiBoP propre, hors Play Mode, compilation terminée et console sans erreur.

La recette de ce redimensionnement par quatre bords est ensuite validée dans le casque par l'utilisateur ; le jalon des lots 01 à 06 est clôturé.

## Livraison des lots 07 et 08 — 8 octobre 2026

### Lot 07 : qualification des opérations communes

Les handlers et drivers existants satisfont le périmètre après ajout des cas de qualification manquants ; aucun nouveau type d'opération, codec, contrat d'autorité ou algorithme métier n'a été introduit.

| Entrée | Chemin et preuve inspectée |
| --- | --- |
| Sélection d'un site préparé | `Base3DScene.SelectSite(column, site)` → événements communs → `V2SceneMutationBoundary` → `SetSelectedSite` et sélection commune de colonne requise. Les tests T09 des deux drivers vérifient convergence, application optimiste et absence d'écho. |
| Site inconnu ou invisible | Validation de `SetSelectedSite` alignée sur la visibilité Desktop. Les cinq cas `QuestControls_InvalidSiteCannotPublishAndDriverAcceptsNextSelection` couvrent inconnu, masque, blacklist cachée, filtre et exclusion ROI sans affichage de tous les sites ; aucune nouvelle sélection/proposition invalide, puis sélection valide acceptée. |
| Proposition de sélection devenue inéligible | `QuestControls_PendingSiteBecomingMaskedIsCorrectedWithoutEchoAndNextSelectionWorks` vérifie correction vers l'état Desktop, pending vidé et driver réutilisable. |
| Création et suppression de coupe | `CreateCut` / `DeleteCut` ; le test existant `ApplicationRetirement_RepeatedCutCreationDeletionKeepsBothScenesAndHistoriesBounded`, réexécuté, accepte ces opérations dans les deux sens sur 2 048 cycles. |
| Définition complète et identité de coupe | `SetCutDefinition` ; tests existants de définitions Custom/non-Custom, suppression intermédiaire et ordre stable, réexécutés. |
| Édition distante pendant une édition locale | `QuestControls_StaleCutPreviewCorrectsThenCurrentPositionSetterPreservesRemoteFields` rejette une vieille proposition complète, applique sa correction et vérifie qu'un nouveau setter de position conserve flip, normale et nombre de coupes reçus. |

Une coupe possède une clé de conflit par `CutId`, et non par champ. Les futurs contrôles des lots 09–10 doivent reconstruire chaque définition complète depuis l'objet courant ; une vieille définition en attente reste soumise au rejet/correction existant. La chaîne optimiste création/modification/suppression rejetée est également réexécutée, sans coupe fantôme ni perte de disponibilité du driver.

### Lot 08 : sonde de sélection immédiate

- `QuestSiteProbe`, sérialisé dans `QuestBootstrap`, utilise le maintien de **A sur la manette droite**. La petite sphère est authored dans ce prefab, sans collider ni réponse physique. Son matériau est celui du feedback Quest existant et sa couleur vient du Setting Color HiBoP commun via `Quest Interaction Policy` ; aucun élément Desktop n'est ajouté ou modifié.
- Réglages initiaux dans cet asset : rayon de sonde 0,5 mm, impulsion haptique d'amplitude 0,2 pendant 25 ms, modifiables dans l'inspecteur. Après réévaluation utilisateur, la sonde est placée exactement à l'origine du rayon universel : même pose aim et même conversion du repère de tracking, ou même pose de repli grip si aim est indisponible. Aucun décalage estimé vers le bouton A n'est conservé.
- `QuestSiteContactPolicy` calcule une collision à la pose courante entre la sonde et les `SphereCollider` réels des sites, avec leur centre, leur rayon et leurs transformations. Le balayage initial entre deux poses est supprimé après recette. Au maximum un site est choisi par image ; dans un chevauchement, le centre le plus proche gagne, puis les identités préparées de colonne/site départagent les égalités, sans priorité à l'ancienne colonne.
- `QuestSiteSelection` et la validation des nouvelles sélections synchronisées utilisent la même règle `SiteAppearance.IsVisible` que le rendu Desktop. Un site blacklisté visible est sélectionnable lorsque « masquer les sites blacklistés » est décoché ; un site hors ROI est sélectionnable lorsque tous les sites sont affichés. Les sites masqués, filtrés, cachés/inactifs, inconnus ou appartenant à une ancienne scène restent exclus. Cette règle ne modifie pas leur exclusion des calculs scientifiques. Les corrections, canoniques et checkpoints peuvent restaurer une sélection retenue devenue invisible, tout en validant son identité ; ils ne créent pas une nouvelle sélection utilisateur.
- `QuestSiteSelectionRing`, sérialisé dans `QuestColumn`, affiche la sélection commune locale ou reçue du Desktop. Il réutilise le sprite et le contrôleur d'animation Desktop (rotation et pulsation), avec un ThemeElement Quest indépendant, son propre Setting Image et son matériau stéréoscopique ; seul le Setting Color blanc est partagé. Le billboard suit la caméra et le centre du site, avec le même dégagement relatif de 70 %, compensé pour les transformations du cerveau, et un diamètre angulaire minimal de 0,2°. Il disparaît lorsque le site ou la présentation sont cachés, sans effacer la sélection retenue. Aucun raycast ni interaction n'est ajouté à cet indicateur.
- Le maintien permet les sélections successives, sans préselection ni délai. Rester dans le même contact ne republie pas et ne répète pas le feedback. Sortir du site ou relâcher A conserve la dernière sélection. Chaque contact utilise explicitement sa colonne propriétaire, indépendamment de la sélection Desktop courante.
- `QuestPointerInput` arbitre la main droite avant le rayon : sonde active → rayon/réticule masqués et aucune nouvelle capture de gâchette par cette main. A pendant une capture UI/cerveau existante est ignoré jusqu'à un relâchement et un nouvel appui. Relâcher A avec la gâchette encore tenue ne déclenche pas de saisie/clic tardif.
- Perte de tracking du casque ou de la manette, focus/pause, désactivation, fermeture/remplacement de scène : sonde masquée, contact réinitialisé et nouvel appui requis. La scène est capturée à l'activation ; elle ne peut pas être remplacée au milieu du maintien.
- La sonde ne modifie aucune pose scientifique et n'ajoute pas le comportement automatique de sélection de scène après manipulation d'un cerveau, toujours réservé à T17.

### Vérification et recette restante

Résultats inspectés : **27 EditMode et 18 PlayMode réussis**, console sans erreur. Le gate de dépendances passe (44 assemblies, 158 dépendances), sans modification de `.asmdef`. Les tests d'assets ont été rejoués après la revue finale du prefab : 11 cas réussis. Résultats XML dans `Logs/QuestUI/Lots07-08-EditMode-results.xml`, `Lots07-08-PlayMode-results.xml` et `Lots07-08-Assets-EditMode-results.xml`.

Les quatre nouveaux tests `QuestSiteProbeTests` couvrent quinze contacts traversés entre deux poses, absence d'attraction, rotation/échelle du cerveau, chevauchements et égalités indépendantes de l'ordre, éligibilité et non-répétition, ainsi que les véritables actions Input System A/gâchette avec UI, relâchement, tracking, focus et disparition de scène. Les régressions du pointeur universel, déplacement de fenêtres et manipulation anatomique sont incluses. L'édition Unity a normalisé les références de thème du prefab et retiré des overrides orphelins du chargement ; aucun changement de son design n'est demandé.

Recette manuelle après installation de la nouvelle build HiBoP (la démo de redimensionnement reste distincte) :

1. Publier depuis Desktop une scène contenant une électrode avec plusieurs sites visibles. Viser une cible pour observer le départ du rayon, puis maintenir A à droite sans déplacer la manette : petite sphère exactement à cette origine, rayon droit absent ; relâcher : sphère masquée. Sans scène ou sans tracking, aucune sonde.
2. Toucher un site : sélection immédiate, brève impulsion et anneau blanc animé autour du site. Rester immobile : pas de vibration répétée ni clignotement. Sortir puis relâcher A : dernière sélection et anneau conservés. Vérifier son rendu dans chaque œil, sa rotation/pulsation et sa taille après rotation/redimensionnement du cerveau, puis après sélection depuis Desktop.
3. Parcourir quinze sites en environ dix secondes, puis traverser plusieurs sites rapidement pendant le même maintien. Vérifier réactivité, précision, absence de zone d'attraction et sélection finale attendue. Seuls les sites effectivement touchés sur une image sont sélectionnés ; les sites sautés entre deux images ne le sont plus.
4. Déplacer, tourner et redimensionner un cerveau avec les gestes existants, relâcher les gâchettes, puis refaire le parcours. Vérifier que les contacts suivent les sphères visibles et que la colonne visée fournit le bon site, même si une autre colonne est active au Desktop.
5. Dans des sites/colonnes superposés, vérifier la priorité au centre le plus proche et la stabilité à l'arrêt. Faire des allers-retours entre colonnes : la colonne du site touché devient active dans Quest et Desktop, le site de l'ancienne colonne est désélectionné, sans retour spontané après confirmation réseau. Tester les différentes tailles de sites réellement utilisées.
6. Masquer/filtrer un site : la sonde ne le sélectionne pas. Blacklister un site en laissant son affichage activé : sélection possible ; cocher « masquer les sites blacklistés » : sélection impossible et anneau masqué. Pour un site hors ROI, comparer « afficher tous les sites » activé (sélection possible) et désactivé (impossible). Un site redevenu visible doit être sélectionnable après un nouveau contact/appui ; s'il était déjà sélectionné, son anneau réapparaît sans nouvelle sélection. Vérifier ces cas dans les deux sens de synchronisation et après reconnexion.
7. Maintenir A pendant une saisie UI ou de cerveau : le geste continue, sans sonde. Relâcher la gâchette en gardant A : pas de démarrage tardif ; relâcher puis réappuyer A. Avec la sonde active, presser la gâchette ne clique rien et ne saisit aucun cerveau ; relâcher A avec la gâchette maintenue ne capture rien avant un nouveau cycle de gâchette. Vérifier les interactions de l'autre main et les gestes ordinaires après relâchement.
8. Perdre/retrouver le tracking ou ouvrir/fermer le menu système avec A maintenu : sonde masquée, pas de nouvelle sélection au retour ; relâchement/nouvel appui nécessaire. Répéter lors du remplacement/fermeture de la scène, sans balayage entre les anciennes et nouvelles poses.
9. Avec Desktop connecté, sélectionner plusieurs sites dans le Quest puis depuis Desktop : convergence des identités et de l'affichage, sans ping-pong. Tester une exclusion devenue distante et un contact suivant valide. Déconnecter le réseau, sélectionner localement, puis reconnecter selon le flux existant ; vérifier l'état final après correction éventuelle.
10. Régression : pairing, toolbar, fenêtres, chargement et saisies proches/distantes des cerveaux. Modifier les coupes depuis Desktop et vérifier le rendu reçu ; aucun éditeur de coupes Quest n'est encore exposé. Les tests de concurrence/création/suppression du lot 07 sont automatisés ; la recette des contrôles de coupes attend les lots 09–10.

Le casque n'était pas connecté pendant l'implémentation. La facilité à atteindre la petite sphère, sa visibilité réelle, la sensation haptique, les performances avec une implantation dense et la synchronisation sur deux appareils restent des validations matérielles, distinctes des résultats automatisés.

Build Android Development prête pour cette recette : `.artifacts/quest-controls-20261008/HiBoP.Quest.apk`, réussie en 458,75 s, 0 erreur et 10 avertissements. Le premier essai a échoué faute de mots de passe de signature ; l'utilisateur les a renseignés avant la relance. Les avertissements du build réussi concernent les recommandations Meta (Game Activity et polling), les assets temporaires XR Simulation, la compilation de grosses méthodes TextMeshPro et le cache des samples ; ils ne constituent pas des erreurs de compilation des lots 07–08. Rapport : `Quest.build-report.json` dans le même dossier.

`Tools/Test-QuestApk.ps1` a vérifié le contenu : 353 631 425 octets et 9 bibliothèques ARM64. L'identité inspectée par `aapt` est « HiBoP », `fr.crnl.hibop.quest`, version 6.2.0 ; seule la scène QuestBootstrap est incluse. Aucun déploiement n'a été tenté. Le profil DesktopWindows est réactivé, la scène HiBoP est propre, hors Play Mode et hors Prefab Stage, compilation terminée. Les réglages temporaires du profil Quest ont été restaurés et enregistrés après le build.

Cette première build est ensuite installée en USB et la sonde est validée par l'utilisateur. Son ajustement vers le bouton A est qualifié par 11 tests EditMode et 4 PlayMode réussis (`Logs/QuestUI/Probe-AButton-EditMode-results.xml` et `Probe-AButton-PlayMode-results.xml`), sans erreur console. Build corrigée : `.artifacts/quest-probe-button-20261008/HiBoP.Quest.apk`, réussie en 143,79 s (0 erreur, 12 avertissements, dont deux attentes supplémentaires de manifests de packages). Contenu vérifié : 353 631 377 octets, 9 bibliothèques ARM64, identité HiBoP inchangée. Installation USB réussie et date de mise à jour vérifiée ; profil DesktopWindows restauré, scène propre et compilation terminée. Le placement près de A est ensuite rejeté et remplacé par l'origine exacte du rayon décrite ci-dessus.

### Ajustements de sonde, visibilité et feedback — 8 octobre 2026

Qualification inspectée : **38 EditMode et 30 PlayMode réussis**, sans erreur console ; un test supplémentaire de fixture MNI réelle est ignoré faute de paramètre `-questAnatomyFixture`. Résultats : `Logs/QuestUI/Probe-Visibility-EditMode-results.xml` et `Probe-Visibility-PlayMode-results.xml`. Le gate de dépendances passe (44 assemblies, 158 dépendances), sans changement de `.asmdef`.

Les nouveaux cas vérifient les poses aim/grip distinctes dans un repère de tracking transformé, la sélection des sites blacklistés visibles et hors ROI affichés dans les deux sens, les checkpoints et corrections de sélection retenue devenue cachée, et le rendu/animation/taille de l'anneau après transformations et changements de visibilité. Le rendu réel de fixture est inspecté dans `Logs/QuestUI/Probe-Selection-Ring.png`. Le confort et le rendu stéréoscopique restent à qualifier dans le casque avec la recette ci-dessus.

Build Android Development : `.artifacts/quest-probe-feedback-20261008/HiBoP.Quest.apk`, réussie en 272,90 s, 0 erreur et 69 avertissements. La première tentative était trop précoce après activation du profil, Unity compilant encore les scripts ; elle a été relancée après stabilisation. Les avertissements inspectés concernent les analyseurs de sérialisation existants, une variable inutilisée, les recommandations Meta, les assets temporaires XR Simulation et TextMeshPro. Le nouveau shader de sélection a compilé ses variantes Vulkan sans erreur. APK vérifié : 353 639 681 octets, 9 bibliothèques ARM64, identité HiBoP `fr.crnl.hibop.quest` 6.2.0 et seule scène QuestBootstrap.

Installation USB réussie avec conservation des données ; `lastUpdateTime=2026-10-08 11:15:18` vérifié sur le Quest. Profil DesktopWindows restauré et sauvegardé, scène HiBoP propre, hors Play Mode et hors Prefab Stage, compilation terminée et console sans erreur. Les réglages temporaires du profil Quest sont restaurés sans diff résiduel.

### Contact courant et stabilité de la sélection entre colonnes — 8 octobre 2026

Après nouvelle recette, le balayage est supprimé : seule l'intersection des deux sphères à la pose courante sélectionne un site. Aucun historique de trajet n'est conservé et au maximum une cible est sélectionnée par image. Le centre le plus proche départage les chevauchements, sans priorité à la colonne précédemment active ; les égalités exactes restent déterministes.

Le setter commun sélectionnait déjà la colonne et désélectionnait les sites des autres colonnes, comme Desktop. Le défaut provenait aussi de ses callbacks : ils pouvaient publier une désélection temporaire et un changement de colonne avant la sélection finale. Ces propositions pouvaient ensuite être rejetées/corrigées comme obsolètes. Le port commun neutre `SiteSelectionRouter`, raccordé par la boundary, fait désormais appliquer et publier une seule intention `SetSelectedSite` par action, en conservant tous les callbacks UI et les règles Desktop. Le fallback sans boundary reste disponible ; aucune référence Sync n'est ajoutée à Data/Core.

Le driver Quest sérialise les propositions de sites au niveau de la scène : une en vol et une seule dernière intention locale en attente, remplaçable. Le retour visuel reste immédiat. À la confirmation/correction, il applique l'état confirmé puis rejoue la dernière intention éligible et l'envoie avec l'observation canonique actualisée. Un site devenu invisible n'est pas renvoyé ; l'état confirmé est conservé. Un ancien message d'une autre colonne ne peut pas remplacer une sélection confirmée plus récente, y compris après checkpoint. Une désélection explicite d'une ancienne colonne ne la réactive pas. Les opérations, codecs et règles d'autorité restent inchangés.

Qualification inspectée : **145 EditMode et 9 PlayMode réussis**, sans erreur console. Suites : `V2SceneMutationBoundaryTests`, `V2OnlineMutationDriverTests`, `QuestUIAssetTests`, `SiteAppearanceTests`, `QuestSiteProbeTests`, `QuestUniversalPointerTests`. Résultats dans `Logs/QuestUI/Probe-Contact-EditMode-results.xml` et `Probe-Contact-PlayMode-results.xml`. Contrôle des dépendances réussi : 44 assemblies, 158 dépendances, aucune modification de `.asmdef`. La revue indépendante a également vérifié les états intermédiaires, l'éligibilité du contact différé et l'arrivée hors ordre des sélections. Un test supplémentaire a reproduit puis vérifié la correction du cas site/colonne reçus dans l'ordre inverse pour une même colonne : le changement de colonne seul conserve son site, donc une valeur de site tardive pour cette même colonne reste applicable si sa propre version n'est pas dépassée. Les checkpoints complets restent prioritaires sur tous les états de sélection antérieurs.

Les tests reproduisent trente changements locaux avant confirmation, les allers-retours A→B→A dans une colonne ou entre deux colonnes avec callbacks exclusifs, les corrections avec intention différée valide ou devenue cachée, l'écho correspondant après sélection distante intermédiaire, les anciens canoniques intercolonnes, le checkpoint, la reconnexion courte et la sortie hors ligne. La fixture PlayMode vérifie les sites superposés, la stabilité pendant vingt images, le changement de colonne et l'absence de sélection d'un site traversé entre deux poses.

Recette casque : parcourir une électrode rapidement puis rester immobile ; superposer deux colonnes et alterner les contacts au voisinage des centres de leurs sites ; vérifier dans Desktop la colonne active et l'unique site sélectionné, puis attendre quelques secondes sans retour spontané. Refaire après une brève coupure réseau. Un site simplement traversé entre deux images peut maintenant être manqué, conformément au choix utilisateur ; il suffit de le toucher à une pose effective.

Build finale Android Development : `.artifacts/quest-contact-selection-20261008/HiBoP.Quest.apk`, réussie en 111,49 s, 0 erreur et 45 avertissements existants (Meta, XR Simulation, analyseurs de sérialisation et UI). Une première build avait été conservée localement puis remplacée après le dernier test d'ordre site/colonne ; seule la build finale est déployée. Contenu vérifié : 353 613 389 octets, 9 bibliothèques ARM64, nom HiBoP, package `fr.crnl.hibop.quest`, version 6.2.0, seule scène QuestBootstrap. Installation USB réussie avec conservation des données et date vérifiée : `2026-10-08 11:57:21`. Profil DesktopWindows restauré et sauvegardé, scène propre, hors Play Mode et Prefab Stage, compilation terminée et console sans erreur.

### Validation utilisateur des lots 07 et 08 — 8 octobre 2026

L'utilisateur valide complètement les lots 07 et 08, y compris les ajustements du placement de la sonde, des règles de visibilité, de l'indicateur animé et de la stabilité de sélection entre colonnes. Les lots **01 à 08 sont désormais validés et clôturés**. Les comptes rendus précédents restent l'historique des livraisons ; leurs mentions d'une validation en attente sont levées par cette acceptation.

La suite prévue concerne les contrôles de coupes des lots **09 et 10**. La sélection automatique de scène/colonne à partir du dernier cerveau manipulé reste explicitement reportée à **T17**. Cette validation ne clôture donc pas T15 et ne déclenche pas l'implémentation des lots suivants.
