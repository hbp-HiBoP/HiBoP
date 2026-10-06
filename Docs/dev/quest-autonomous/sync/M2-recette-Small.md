# Recette de la synchronisation M2 avec Small

Cette feuille sert à tester les fonctions disponibles dans **Small**, avec HiBoP dans Unity côté Desktop et l’APK Android dans le Quest. Les actions partent du Desktop sauf indication contraire. Elle recueille ton verdict fonctionnel et tes observations ; elle ne vaut pas qualification de toutes les modalités.

## Préparation et mode de remplissage

- Date et testeur :
- Projet : `C:/Users/Zigaroula/Documents/HiBoP/Projects/visu_full_test.hibop`, visualisation **Small**, trois colonnes iEEG.
- Unity : **6000.5.2f1**, scène `Assets/_Scenes/HiBoP.unity`, profil **DesktopWindows**.
- Casque : **Quest 3**, application **HiBoP** (`fr.crnl.hibop.quest`).
- Connexion réellement utilisée : USB / Wi-Fi :
- Référence du build : voir [le manifeste local](../../../../.test-results/sync-m2-small-20261002/session-manifest.json).
- Site repère choisi et colonne :

Dans chaque cellule **Résultat**, inscrire **OK**, **KO**, **Partiel**, **NT** (non testé) ou **NA** (indisponible dans Small). Dans **Observations**, préciser la variante qui échoue, le site/la colonne, le résultat Desktop et le résultat Quest. Une fonction absente de Small est NA, pas OK. Les contrôles Quest absents restent NT côté Quest ; cette passe ne nécessite aucun driver.

Pour chaque essai, comparer le même site, la même colonne et les mêmes paramètres. Les points de vue sont indépendants. Vérifier l’état final **sans renvoyer la scène**, puis revenir à la valeur initiale lorsque possible. Pour un curseur, tester un changement net, un mouvement continu de quelques secondes et sa valeur finale après relâchement. Remplir un verdict partiel si une seule variante a été vérifiée.

1. Ouvrir le projet habituel et Small dans Unity en Play Mode.
2. Lancer HiBoP dans le casque, l’appairer depuis Desktop avec le code affiché.
3. Envoyer Small et attendre la fin du chargement. Ne pas modifier la scène pendant l’envoi.
4. Placer les cerveaux dans le casque à une position reconnaissable et vérifier la connexion avant de commencer.

## Première publication et sélection

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| Départ | Envoyer Small une fois. | Trois colonnes et coupe initiale présentes ; sites et rendu cohérents, application utilisable. | OK | |
| D1 | Sélectionner successivement les trois colonnes. | La colonne active partagée suit ; les actions suivantes ciblent la bonne colonne. | OK | Côté Desktop puisqu'on ne peut pas encore sélectionner côté Quest |
| D2 | Sélectionner un site par liste, clic 3D, clic sur coupe et flèches disponibles ; changer de site puis désélectionner. | Même sélection scientifique ; pas de mauvais site ni de sélection fantôme. | OK | Uniquement côté Desktop puisqu'on ne peut pas encore sélectionner côté Quest |

## Sites et présentation

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D11b | Activer puis retirer le highlight, sur un site puis une sélection multiple. | Même surbrillance ; l’activité existante ne disparaît pas. | OK 1 site, KO plusieurs sites | Je remarque une activation successive en faisant CTRL+SHIFT+H alors que c'est en une seule frame côté Desktop. Quand l'action est appliquée via Site Actions sur tous les sites, il y a une exception puis plus aucune synchronisation |
| D12 | Changer la couleur d’un site, puis de plusieurs sites. | Bonne cible et couleur finale ; pas de recalcul de projection causé par la couleur. | OK 1 site, KO plusieurs sites | Même remarque que highlight |
| D12 | Ajouter deux labels, modifier leur ordre si proposé, puis les retirer. | Labels et ordre partagés, sans doublon. Si non visibles dans le casque, noter cette limite. | OK | Non visible dans le casque pour l'instant |
| D11a | Blacklister puis rétablir un site, puis plusieurs. | Même état et effets scientifiques ; ne pas confondre blacklist et filtre. | OK 1 site, KO plusieurs sites | Même remarque que highlight |
| D13a | Activer/désactiver le masquage des sites blacklistés. | Même visibilité, blacklist conservée. | OK | |
| D13b | Avec une ROI active, activer/désactiver Show all sites. | Même ensemble de sites visibles et même effet de masque. | OK | |
| D13c | Modifier le gain/taille des sites puis revenir. | Changement visible des deux côtés ; bonne valeur finale. | OK | |
| D19 | Tester couleur du cerveau, couleur des coupes, colormap, arêtes, transparence et alpha, une option à la fois. | Chaque réglage partagé suit ; couleur/alpha ne suppriment pas la projection. | OK | |
| D21 | Modifier l’opacité d’activité d’une colonne puis l’action globale si présente. | Bonne opacité par colonne ; les cases globales de toolbar peuvent rester locales. | OK | Mais énorme problème de coaslescing : si je modifie beaucoup l'opacité, ça introduit un giga décalage et tout est replay petit à petit |

## Coupes

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D3 | Créer trois coupes, supprimer celle du milieu, modifier chacune des deux restantes ; réordonner si proposé. | Même nombre et ordre ; chaque modification reste attachée à la bonne coupe. | OK | |
| D4 | Déplacer nettement une coupe avec curseur, puis boutons plus/moins. Faire aussi un drag continu et relâcher. | Position et valeur finale suivent, sans longue file de positions après relâchement. | OK | |
| D4 | Tester axial/coronal/sagittal, Flip séparément, puis orientation personnalisée X/Y/Z si disponible. | Orientation, côté visible et textures cohérents ; pas de coupe bloquée. | OK | |
| D5 | Basculer Strong/Soft cuts. | Même politique de coupe sur les deux appareils. | OK | |
| D6 | Activer les coupes automatiques ; sélectionner plusieurs sites ; désactiver. | Les trois coupes suivent le site ; pas de coupes dupliquées ni de sélection erronée. | KO | Fonctionne pour certains sites, pas pour tous |

## ROI et positions

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D7 | Créer deux ROI, renommer, supprimer la première ; importer une ROI si un fichier est disponible. | Même contenu et noms ; la ROI restante garde son identité. | Partiel Desktop, KO Quest | ROI non visible sur le Quest. Côté Desktop, exception avec la ROI "None" (et on ne devrait pas pouvoir changer son nom) |
| D8 | Activer une ROI, changer de ROI, puis retirer la sélection. | Même ROI active et mêmes sites masqués. | OK Desktop, KO Quest | |
| D9 | Ajouter trois sphères ; sélectionner, déplacer et redimensionner ; supprimer celle du milieu puis modifier les restantes. | Même géométrie et même masque ; la bonne sphère reste sélectionnée. | OK Desktop, KO Quest | |
| D14 | Déplacer les sites à gauche, à droite, puis Reset. | Positions anatomiques concordantes, sur toutes les colonnes. **Parité encore à confirmer sur matériel.** | OK | |

## Activité iEEG et timeline

Utiliser une colonne où l’activité est effectivement visible. Si la projection échoue, noter la timeline comme bloquée plutôt que la déclarer fonctionnelle sur la seule animation d’un curseur.

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D32 | En mode manuel, calculer la projection puis la retirer après la fin du calcul. Recommencer. | Activité visible sur les deux appareils ; suppression partagée, UI débloquée à la fin des deux calculs. | OK | |
| D22/D24 | Modifier distance d’influence disponible et span iEEG min/milieu/max. | Bonne colonne et rendu cohérent ; les paramètres finaux sont conservés. | OK | Petite remarque : la projection sur la coupe ne disparait pas lors d'un recalcul direct (elle disparait bien lors d'un désaffichage) |
| D32 | En manuel, changer un paramètre scientifique ; recalculer explicitement. Puis activer l’automatique et refaire le changement. | Manuel : pas de recalcul automatique. Automatique : nouveau calcul ; résultat final cohérent. | OK | |
| D27 | Seek à trois instants nets et pas avant/arrière en pause. | Index final exact et même activité aux instants choisis. | OK | Petit problème de coaslescing/debouncing comme partout |
| D27 | Play/Pause ; Seek et Step pendant la lecture ; changer le pas ; activer/désactiver Loop et atteindre la fin. | Lecture cohérente ; pause/seek/step/loop gardent l’index demandé ; pas de rebond après arrêt. | OK | Léger problème de desynchro après le seek, mais largement suffisant |
| D32 | Pendant une projection assez longue, modifier couleur, highlight, coupe et un span sûr. | Ces actions continuent ; le dernier paramètre sûr est visible après le calcul. | NA | |
| D32 | Pendant la projection, tenter blacklist, ROI, influence, changement de mesh/MRI. | Actions sensibles désactivées ou refusées proprement ; aucune divergence persistante. | NA | |
| D32 | Annuler une projection si possible, puis lancer une nouvelle. | Pas de résultat ancien réapparu ; progression et verrouillage se terminent correctement. Si trop rapide, noter NT. | OK | |

## Filtres et corrélations

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D10 | Appliquer un filtre simple dans Site Filters ; le réinitialiser dans cette fenêtre. Faire l’essai avec une ROI masquant quelques sites. | Même filtre sur sites non masqués ; ceux masqués conservent leur état de filtre précédent. | OK | |
| D10 | Utiliser le Reset des filtres de la toolbar. Tester le filtrage par canaux depuis Informations s’il est accessible. | Reset toolbar : tous inclus, même masqués. Canaux : liste choisie identique sur les deux appareils. | OK | |
| D15 | Calculer les corrélations ; activer/désactiver leur affichage ; réinitialiser. | Même résultat et affichage final ; reset partagé. Une réussite du calcul seule ne valide pas les autres variantes. | NT | |
| D15 | Exporter puis charger un résultat de corrélation avec les outils disponibles. | Données importées présentes dans le casque sans fichier Desktop à y copier. Si aucun outil/fichier, noter NA. | NT | |
| D10/D15 | Sur un job assez long, observer progression, annuler puis relancer ; tenter l’autre job pendant le premier. | Progression et fin correctes sur les deux appareils ; second job incompatible refusé ; aucun ancien résultat tardif. | NT | |

## Ressources et options disponibles dans Small

Tester seulement les ressources **déjà préparées et envoyées**. Si une option charge une nouvelle ressource lourde, noter qu’un nouvel envoi complet est nécessaire ; ce n’est pas un essai de synchronisation incrémentale. Les trois colonnes enregistrées dans Small sont iEEG : ne pas ajouter de modalité pour cette passe.

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D16 | Changer hémisphère/partie, mesh préparé et représentation anatomique/inflated si disponibles. | Même surface finale ; pas de mauvais masque après changement. | OK pour la plupart, KO inflated | |
| D17 | Modifier contraste MRI min/max ; changer de MRI préparée si disponible. | Textures et calibration cohérentes, sans effacer l’activité pour un simple contraste. | OK | |
| D18 | Changer d’implantation préparée si proposée. | Sites cohérents ; anciennes références invalides nettoyées. | OK | |
| D20 | Effacer quelques triangles, étendre, inverser, reset ; sauver/charger le masque et Undo si proposés. | Même masque final sur la bonne surface ; pas d’effacement involontaire d’une autre topologie. | OK | Mais pas de preview des triangles effacés côté Quest |
| D28 | MarsAtlas/JuBrain : activer/désactiver et changer alpha si disponibles. | Même overlay et alpha. | Mars Atlas OK, Jubrain KO | |
| D29 | IBC/DiFuMo : sélection de source/aire et affichage si disponibles. | Même aire/source et affichage. | KO | Pas chargés par défaut |
| D30 | Localizer : source, instant et seuils si disponibles. | Même état final ; les sélecteurs de fenêtre peuvent rester locaux. | NA | |
| D31 | Seuils/opacité d’atlas fMRI si disponibles comme overlay. | Même calibration et rendu. | NA | |

**Hors de cette passe par choix :** D25 et partie CCEP de D24 ; colonnes statiques D23 et colonnes fMRI/MEG D26 absentes de cette Small. D24 iEEG reste testé ci-dessus. Les overlays accessibles restent dans la liste conditionnelle. Ces exclusions ne constituent ni des échecs ni des validations.

## Imports et configurations

Faire cette partie après les essais précédents, car un reset change plusieurs réglages à la fois. Ne pas écraser le projet habituel avec les modifications de recette.

Deux petits fichiers sont prêts : [sites-toolbar.csv](M2-Small-inputs/sites-toolbar.csv) pour l’import d’état de la toolbar et [sites-tools.csv](M2-Small-inputs/sites-tools.csv) pour l’import CSV des outils de sites. Ils ciblent deux sites existants de Small : le premier devient magenta et surligné, le second cyan et blacklisté ; chacun reçoit deux labels. Choisir « toutes les colonnes » si proposé, puis vérifier les deux sites sur chaque colonne. Les formats diffèrent (`ID` et `Site`) : utiliser le fichier correspondant à l’outil. Pour le test de fusion, ajouter auparavant un label `avant_import` ; il doit rester quand la fusion est activée. L’export réalisé avant l’essai permet de restaurer les valeurs initiales.

| Repère | Action | Ce qui doit fonctionner | Résultat | Observations |
| --- | --- | --- | --- | --- |
| D34 | Exporter l’état de quelques sites ; modifier leurs états ; réimporter le fichier. Répéter via le second outil d’import s’il est accessible. | Blacklist/highlight/couleur/labels restaurés ensemble. Filtre et positions ne font pas partie de cet import. | OK | Petit souci purement Desktop lors de l'export : la box pour mettre le fichier revient en boucle jusqu'à ce qu'on annule, et tous les fichiers sont créés |
| D34 | Import CSV partiel : désactiver couleur ; essayer remplacement puis fusion des labels. | Champs désactivés conservés ; labels fusionnés sans doublon et dans le même ordre. | OK | |
| D33 | Sauver configuration de colonne puis de scène ; modifier plusieurs champs ; recharger ; tester Reset colonne et Reset scène. | Même état final complet des deux côtés ; bonne colonne et pas de résultat partiellement appliqué. | OK | |

## Contrôles Quest et préservation du local

Refaire depuis le Quest uniquement les opérations scientifiques qu’il expose réellement. Décrire ci-dessous chaque contrôle utilisé ; ne pas assimiler les gestes de présentation à des propositions scientifiques.

| Action depuis le Quest | Résultat attendu | Résultat | Observations |
| --- | --- | --- | --- |
| Contrôle scientifique disponible n°1 : | Même état final sur Desktop ; pas de retour à l’ancienne valeur. | OK | |
| Contrôle scientifique disponible n°2 : | Même état final sur Desktop ; bonne colonne/site. | OK | |
| Autres contrôles scientifiques : | Même état final sur Desktop. | OK | |
| D36 : saisir, déplacer, redimensionner et recentrer les cerveaux. | Gestes locaux fluides ; aucune modification de caméra Desktop. | OK | |
| D35/D36 : après placement Quest, changer coupe/couleur/activité sur Desktop, puis tourner caméra Desktop. | Placement/échelle Quest conservés ; caméra Desktop reste indépendante. | OK | |
| D37 : hover, tooltip, panneau et modes d’outil. | États de présentation locaux ; aucune mutation scientifique involontaire. | OK | |

## Fin de session et verdict

Faire ces essais en dernier. La réconciliation avec choix Desktop/Quest et le multi-scène appartiennent aux milestones suivantes.

| Action | Résultat attendu | Résultat | Observations |
| --- | --- | --- | --- |
| Perte de connexion confirmée, avec la méthode habituelle (déconnexion USB si USB, suspension Quest si Wi-Fi). | La visualisation n’est pas détruite ; les jobs en cours sont annulés et ne redémarrent pas seuls hors ligne. Une suspension peut arrêter le rendu. | NT | |
| Rétablir la connexion. | Noter le comportement observé. Ne pas exiger ici la future UI de choix Desktop/Quest ; si besoin, relancer puis réappairer et renvoyer Small. | NT | |

- Feature utilisable avec Small : **Oui / Oui avec réserves / Non / Inconclusif** :
- Fluidité des changements continus :
- Cohérence du rendu et des résultats scientifiques :
- Fonctions KO ou partielles prioritaires :
- Fonctions NA ou NT, avec raison :
- Remarques libres :

Pour chaque anomalie, copier et remplir :

```text
Repère du test :
Heure approximative :
Colonne / site / ressource :
Action et valeur avant → après :
Attendu :
Observé Desktop :
Observé Quest :
Reproductible : toujours / parfois / une fois
Erreurs Unity, capture ou vidéo :
Contournement nécessaire, par exemple renvoi complet :
```

Références : [matrice des opérations](operation-matrix.md), [contrat de vérification](07-verification.md), [périmètre des milestones](06-implementation-stages.md#milestones). Les résultats de cette recette concernent Small et les variantes effectivement essayées ; ils ne ferment pas les lignes exclues ou non observables.

## Reprise des corrections

Les corrections, reports et résultats supplémentaires sont consignés dans [M2-corrections-et-reports.md](M2-corrections-et-reports.md). Les verdicts et observations originaux ci-dessus sont conservés.

### Retour manuel — 5 octobre 2026 : première publication

| Repère | Résultat supplémentaire | Observation |
| --- | --- | --- |
| Départ / préparation des ressources | KO — essai interrompu | Au premier envoi de la visualisation de reprise, la phase « Preparing visualization resources » est jugée anormalement longue. Durée non mesurée : l’utilisateur a arrêté le processus avant la fin pour signaler le problème. Les envois suivants sans modification ne sont pas encore testés. Cette tentative ne qualifie pas les corrections qui nécessitent une session publiée. |

Le verdict original « Départ : OK » reste inchangé. Le retour et le correctif supplémentaires ci-dessous précisent désormais la cause principale identifiée.


### Retour et correctif supplémentaires — 5 octobre 2026 : anatomies de la scène multi

L’utilisateur identifie le chargement inutile des maillages et IRM individuels des patients de la visualisation multi, alors que le préchargement Desktop est décoché. Ces ressources servent à de futures scènes single ; leur ouverture sur Quest est prévue à un jalon ultérieur. La visualisation envoyée ne contient aucune coupe.

Le chemin d’envoi prépare et transfère désormais uniquement l’anatomie de la scène courante, sans ajouter les caches individuels déjà présents sur Desktop. Les logs temporaires du diagnostic sont retirés. Le transfert d’une scène single conserve son anatomie. Résultats automatisés détaillés dans [le document des corrections](M2-corrections-et-reports.md#correctif--anatomies-individuelles-hors-du-périmètre-dun-envoi-multi).

Nouvelle reprise physique du premier envoi et mesure de sa durée : **non exécutées** à ce stade. Le KO de l’essai interrompu et les verdicts originaux restent conservés.


### Reprise manuelle confirmée — 5 octobre 2026

Retour du propriétaire après reprise des corrections sur la visualisation locale équivalente. Ces verdicts supplémentaires complètent les observations originales sans les remplacer. Le succès des essais nécessitant la synchronisation confirme qu’une session publiée a pu être utilisée ; la durée du premier envoi n’a pas été communiquée.

| Correction reprise | Résultat manuel | Observation |
| --- | --- | --- |
| D11a/D11b/D12 — actions multi-sites | OK | Toutes les corrections d’actions multi-sites sont confirmées. L’affichage des labels dans le casque reste dans les reports précédents. |
| D21 et curseurs apparentés | OK | Corrections des valeurs continues confirmées. |
| D27 — timeline | OK | Corrections de timeline confirmées. |
| D6 — coupes automatiques | OK | Fonctionnement des coupes automatiques confirmé. |
| D7 Desktop — ROI « None » | OK | Protection de la sélection « None » confirmée. Les aides visuelles ROI Quest restent reportées. |
| D22/D24 — textures pendant recalcul | OK avec réserve | Fonctionnement global confirmé, avec l’anomalie projection/suppression/reprojection décrite ci-dessous. |
| Site Actions / export CSV | OK | Correction de l’export confirmée. |

D16/inflated reste à reprendre dans le chantier convenu. Ce retour ne qualifie pas les lignes NT/NA ni les modalités exclues de Small.

Anomalie résiduelle : une première projection s’affiche normalement ; après suppression de l’activité puis nouvelle projection, l’activité ne s’affiche plus **sur Desktop et sur Quest**. La réinitialisation des configurations permet de projeter à nouveau. Occurrence rapportée, fréquence et cause non déterminées ; aucune erreur console ni heure précise communiquée. Reprise : répéter projection → suppression → projection sans modifier les paramètres, distinguer calcul terminé et affichage, puis comparer les états avant/après reset. Critère de fermeture : activité de nouveau visible des deux côtés sans reset, y compris après plusieurs cycles.


### Nouvelle reprise D16 — inflation locale après publication

Un nouveau job prépare inflated localement sur Desktop et Quest après l'envoi initial. Les deux préparations doivent réussir avant publication du changement de représentation ; les passages suivants réutilisent le cache. Les verdicts originaux de D16 restent conservés.

Essai manuel à reprendre avec les deux exécutables corrigés :

1. Envoyer une visualisation encore anatomical, sans avoir préparé inflated auparavant.
2. Activer inflated ; observer la progression, puis la représentation des deux côtés, sans renvoyer la scène.
3. Revenir à anatomical, puis réactiver inflated : le cache doit être réutilisé.
4. Reprendre gauche/droite/deux hémisphères, et vérifier les triangles effacés, l'activité projetée et le placement du cerveau dans le casque.
5. Sur une autre ressource sans cache, annuler pendant la préparation : l'ancien affichage doit rester utilisable et une nouvelle demande doit fonctionner.

Résultats automatisés et état de la reprise physique : voir [les corrections](M2-corrections-et-reports.md#inflation-locale-coordonnée--5-octobre-2026). Cette extension ne qualifie pas les autres lignes NT/NA.

Observation physique supplémentaire du 5 octobre 2026 : sur la fixture MNI envoyée sans cache inflated, le propriétaire confirme avoir vu le cerveau inflated. Il relève l'absence de la petite animation du Desktop. L'essai demandait une bascule sans animation ; la coordination a ensuite été étendue pour conserver aussi l'animation demandée par le bouton. Aucun verdict original n'est remplacé ; les critères activité/triangles/placement et l'annulation physique restent à qualifier séparément.

Nouvelle reprise USB du 5 octobre 2026 : le propriétaire confirme que l'animation fonctionne pendant les allers-retours. Il demande de retirer le cercle de chargement pendant cette animation tout en le conservant pour le calcul du mesh. Une première modification masquait seulement le visuel et ne répondait pas à sa demande. Le chemin est ensuite corrigé : la préparation seule passe par LoadingManager, puis l'animation et la publication sont exécutées après son retour, sur Desktop et Quest. L'option de masquage ajoutée au LoadingManager est retirée. L'essai manuel de cette séparation reste à faire ; aucun test automatique supplémentaire n'est exécuté, conformément à sa demande. Le passthrough reste absent après relance et est reporté à un chantier ultérieur, détaillé dans [le document des reports](M2-corrections-et-reports.md#report--passthrough-quest-absent-après-relance).

### Reprise D16 — coordonnées anatomiques en mémoire, version 2 (6 octobre 2026)

Le propriétaire reprend lui-même l'essai matériel plus tard. La réussite des sessions Quest simulées dans l'éditeur Windows ne qualifie pas le calcul ARM. Déployer ensemble Desktop et Quest avec `AlgorithmVersion = 2`, puis envoyer une nouvelle livraison sans inflated préparé pour forcer un calcul local sur chaque appareil.

Reprendre le MNI gris, le MNI blanc et une surface patient transformée, avec les mêmes options d'inflation des deux côtés. Vérifier les deux hémisphères puis leur fusion, les bascules avec cache, l'annulation, les triangles effacés, couleurs/UV/atlas et le placement local. Comparer les buffers complets dans le repère anatomique, avant les transformations d'affichage : même ordre de sommets, même topologie et attributs identiques, écart maximal de positions **0,001 mm**. Une comparaison visuelle seule ne mesure pas cette tolérance. Diagnostiquer tout dépassement (options, buffers d'entrée, rapports natifs et plateforme) sans augmenter le seuil.

Les résultats préparés historiques `NativeGifti` / `NativeGiftiThenTransformed` doivent demander une nouvelle préparation/livraison. Aucune source GIFTI ou transformation dédiée à l'inflation ne doit apparaître dans le payload.
