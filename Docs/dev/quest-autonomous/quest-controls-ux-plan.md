# Interfaces et interactions Quest HiBoP

Décisions produit validées le 7 octobre 2026. Implémentation à réaliser par étapes.

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

Le choix entre agrandissement proportionnel et réorganisation du contenu est propre à la fenêtre qui active ce composant. Il est qualifié avec un prefab de démonstration, sans imposer un comportement supplémentaire au pairing.

### Ouverture et fermeture

Une entrée de toolbar ouvre sa fenêtre si elle est fermée. Si elle est déjà ouverte, elle la rappelle sans créer de doublon et sans la fermer. L'action Fermer reste sur la fenêtre.

Le gestionnaire ne construit pas les objets UI à la volée : il utilise des prefabs authored avec leurs références sérialisées. Les fenêtres peuvent être instanciées depuis ces prefabs ; le choix de conserver ou recréer une instance fermée ne doit pas changer le comportement observable.

### Gestionnaire de fenêtres Quest

Créer un `QuestWindowsManager` propre au Quest dès le lot 03. Cette responsabilité est nécessaire pour centraliser ouverture/rappel sans doublon, registre des fenêtres et recentrage depuis la toolbar ; chaque bouton ou contenu ne doit pas réimplémenter ces comportements. Le gestionnaire Desktop sert de référence de responsabilités, sans réutilisation ni héritage de ses classes, de son registre statique ou de ses conteneurs d'écran.

- Référencer explicitement les prefabs et éventuelles instances déjà présentes dans les prefabs Quest. La fenêtre de connexion initiale est enregistrée dans le même registre que les fenêtres ouvertes ensuite, sans seconde instance créée par un autre chemin.
- Associer chaque fenêtre à une clé explicite ; la première tranche conserve une instance ouverte par fonction. Centraliser ouvrir, retrouver, rappeler et fermer, puis retirer du registre les instances fermées ou détruites.
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

Un appui sur A pendant un geste déjà capturé par cette main ne le remplace pas : cet appui est ignoré et la sonde demande un nouvel appui lorsque la main est libre. La perte de tracking masque la sonde, efface son trajet précédent et demande un relâchement puis un nouvel appui ; le retour du tracking ne traverse pas artificiellement les sites entre ancienne et nouvelle pose.

Le contact est géométrique, sans réponse physique. La sélection ne possède pas de zone d'attraction cachée. Si plusieurs sites sont simultanément touchés, une règle déterministe privilégie le centre le plus proche, avec une résolution stable des égalités. Le trajet entre deux positions doit être pris en compte pour ne pas manquer un petit site traversé entre deux images, sans transformer chaque sortie de contact en désélection.

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

**Vérification :** fermeture maintenue pendant les rafraîchissements de connexion, réouverture avec état actuel, consultation après arrivée de la scène, absence de doublon et conservation des confirmations métier. Vérifier ouverture répétée via le gestionnaire, enregistrement de l'instance initiale, nettoyage du registre après fermeture/destruction et absence de dépendance aux classes Desktop. Dans le casque, déplacer la fenêtre sans action du contenu ni saisie d'un cerveau. L'accès de production pour rouvrir arrive au lot 05 ; la fixture vérifie déjà le cycle complet.

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
