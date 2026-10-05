# Corrections et reports de la recette M2

Références : [recette Small](M2-recette-Small.md), [matrice des opérations](operation-matrix.md), [jalons](06-implementation-stages.md#milestones), [vérification](07-verification.md).

Cette reprise du 5 octobre 2026 conserve les verdicts originaux de la recette. Les résultats ci-dessous sont des essais supplémentaires ; ils ne remplacent ni les observations sur casque ni les verdicts NT/NA. Les exclusions de modalités de Small restent inchangées.

## Corrections et nouveaux essais

| Ligne | Correction | Vérification supplémentaire |
| --- | --- | --- |
| D11a, D11b, D12 | Un seul `SetSiteConfigurationBatch` par geste des raccourcis et de Site Actions. Valeurs finales préparées, affectations identiques ignorées, validation et rollback existants. | OK automatisé : raccourcis avec 2/600 sites ; prefab Site Actions avec 2/300 sites sur trois colonnes ; répétition sans changement et couleur suivante convergentes. |
| D21, D27 et curseurs | Émission des previews limitée par frame et clé ; drainage reçu limité à 256 valeurs, réduction dans les segments remplaçables. Les frames fiables émises restent disponibles pour les retries. Play/Pause/Step/Loop arrêtent la fusion des seeks. | OK automatisé : rafales, dernière valeur, barrières timeline, retries immuables après perte d’ACK, contrôles réseau pendant attente de frame et erreurs d’application retentables. Une correction de batch A reste applicable après un batch disjoint B plus récent. |
| D6 | Reproduction sur deux scènes préparées, sélection de sites dans trois colonnes, désélection et réactivation, application v2 réelle. Les coupes restent dérivées localement. | OK automatisé sur la fixture à trois colonnes et sur `full_test.hibop / Unknown(2)` à deux colonnes : tous les sites sélectionnables, changements rapprochés, changement de colonne, désélection/réactivation, trois identités et plans correspondants. Le KO original n’est pas reproduit ; aucune correction spéculative de D6. |
| D16 | Divergence reproduite : Desktop régénérait la représentation par défaut malgré un inflated déjà préparé, alors que Quest utilisait la représentation capturée. Desktop réutilise désormais cette représentation valide, sans génération supplémentaire côté Quest. | OK automatisé : inflated préparé avec quatre itérations avant capture, restauration puis aller-retour via les sessions v2 de production ; vertices affichés identiques, masque de triangles et placement Quest conservés. Passe aussi sur le projet local. |
| D7 Desktop | Renommage/suppression disponibles uniquement avec une ROI sélectionnée ; callbacks protégés si « None » devient la sélection. | OK automatisé : ROI existante avec « None », renommage/suppression désactivés, callbacks retardés protégés, puis sélection/suppression normales. |
| D22/D24 | Invalidation des textures de coupe lorsque l'activité devient périmée, avant publication du nouveau résultat. Les overlays conservent leur chemin indépendant. | OK automatisé sur le rendu natif : pendant l’invalidation, les pixels des textures affichées retrouvent ceux des textures anatomiques de base. |
| Site Actions / export CSV | Une seule exécution simultanée ; bouton désactivé pendant l'opération et rétabli dans `finally`. Sites, états, positions, colonne et options capturés avant la boîte de fichier. | OK automatisé : succès, annulation et erreur ; un seul dialogue, au plus un fichier, bouton rétabli et options/états capturés malgré leur modification pendant l’attente. Aucun branchement erroné démontré dans le prefab. |

## Résultats Unity supplémentaires — 5 octobre 2026

Unity MCP, instance `HiBoP@77bfedb67d0f947c`, Unity `6000.5.2f1` :

| Contrôle | Résultat inspecté | Preuve |
| --- | --- | --- |
| `Sync.Fast`, `Sync.Loopback`, `Sync.SceneFocused` en EditMode | 246 tests terminés, aucun échec ; environ 93 s de durée totale du job, démarrage compris. | Job `f92aa49cf2ee42b091275fdcf79edc9f`. |
| Entrées Desktop, export, ROI, textures et sessions v2 en PlayMode | 10/10 passés, aucun ignoré ; exécution 15,19 s, job 22,12 s. | Job `05a2f329edf44fbb9111dd2733aa475b`. |
| Projet local confirmé par le propriétaire | 1/1 passé ; `full_test.hibop / Unknown(2)`, deux colonnes iEEG, 435 canaux traités par colonne, tous les sites sélectionnables parcourus ; environ 325 s chargement compris. Projet original non sauvegardé. | Job explicite `39583a000ac24e25b846500d7e066b54`, méthode `M2_LocalProjectAutomaticCuts_ConvergeThroughProductionV2Sessions`. |
| Architecture | OK : 44 assemblies HBP, 154 liens directs, aucun changement d’asmdef. | `Tools/check-assembly-dependencies.ps1`. |
| Console après les tests ciblés finaux | Aucune entrée d’erreur. | `read_console`, après le job PlayMode. |

Les premières passes ont exposé des erreurs de fixture (sélection masquée, manager de sélection absent), un défaut réel de régénération inflated et une attente de frame inadaptée à EditMode. Ces problèmes ont été corrigés avant les résultats ci-dessus. Les tests de corrections après échec et de batchs disjoints passent également. Le chargement du projet local a émis des messages natifs GIFTI pour des chemins vides de ressources patient ; la visualisation MNI et les essais ont abouti. Cette observation ne qualifie pas ces ressources patient.

## Reprise physique USB

Le Quest 3 est détecté et autorisé en USB. Le build Quest corrigé a abouti en 440 s ; le contrôle du contenu APK passe (335 289 488 octets, neuf bibliothèques ARM64). Le rapport du build compte 69 warnings et deux erreurs de liaison texture multisamplée/sampler non multisamplé : le build est réussi, mais sa console n’est pas vierge. Aucun shader n’a été modifié dans cette passe.

L’APK produit directement par Unity porte le même certificat que l’application installée (`d5dcf678…3b081acf`). La signature ajoutée après build par le script utilisait un autre certificat et empêchait la mise à jour. Après comparaison des certificats et nouveau contrôle du contenu, `adb install -r` a réussi avec l’APK Unity, sans désinstallation ; les données et l’appairage sont conservés. Une sauvegarde préalable de l’APK installé et des données accessibles reste dans `.artifacts/m2-fixes/installed-before`.

Deux premières tentatives se sont arrêtées avant publication physique : appairage absent, puis presets de filtre manquants dans le montage du test. Le montage capture désormais les presets comme le pairing de production, mémorise l’appairage Desktop et attend la disponibilité de l’observateur avant les gestes. La reprise USB suivante a passé : job explicite `06a0036b2185497bb211dcbd9accaa6f`, 1/1 test, 523 s de durée totale incluant le chargement et l’attente de l’observateur. Le Quest a confirmé `Published` pour `Unknown(2)` (deux colonnes), puis le test a envoyé un batch de 600 sites, une couleur suivante, des rafales d’alpha et de seeks, Play/Pause, des sélections et un aller-retour inflated/anatomical. La session était toujours active et le ping authentifié passait à la fin.

La console Desktop et les logs d’erreur Unity/Android du processus Quest pendant cette dernière passe ne contiennent aucune erreur. Le profil DesktopWindows et les options initiales d’entrée en PlayMode sont restaurés ; l’automatisation de proximité du casque est désactivée. Les preuves locales ignorées sont `.test-results/m2-fixes/validation.json`, `physical-desktop.json`, `physical-quest-errors.log` et `apk-content.json`.

Ce résultat valide la publication et la continuité du protocole physique. Il ne vérifie pas par assertion l’état final rendu dans le casque. L’utilisateur a vu des coupes, sans pouvoir confirmer leur centrage sur les sites ; les couleurs étaient trop rapides pour être qualifiées, l’aller-retour inflated n’a pas été observé et la timeline n’a pas été distinguée pendant l’activité projetée. Ces quatre points restent non validés visuellement. L’utilisateur propose une reprise manuelle plus lente. Les lignes NT/NA restent inchangées.

### Reprise manuelle proposée

Utiliser le Desktop corrigé depuis Unity avec le profil DesktopWindows, en PlayMode, et l’APK corrigé déjà installé. Charger `full_test.hibop / Unknown(2)`. Préparer inflated sur Desktop une fois, revenir à anatomical, puis envoyer la visualisation au Quest pour ouvrir une nouvelle session : la session automatisée précédente est terminée. L’appairage Desktop est désormais mémorisé.

- D6 : activer les coupes automatiques, sélectionner plusieurs sites dans chaque colonne en laissant le rendu se stabiliser ; changer de colonne, désélectionner puis réactiver. Vérifier les trois plans autour du site courant et l’absence de doublons.
- D11/D12 : faire les actions sur deux sites, puis plusieurs centaines ; vérifier couleur/highlight/blacklist et une action suivante. Les labels restent reportés pour leur affichage casque.
- D21/D27 : déplacer puis arrêter les curseurs ; vérifier la dernière valeur stable et l’absence de replay prolongé. Pour la timeline, utiliser un seek vers un temps identifiable, Play/Pause, Step avant/arrière et Loop ; vérifier les indices Desktop et l’activité correspondante sur Quest.
- D16 : alterner les représentations préparées, attendre à chaque étape et comparer réellement la géométrie ; ne pas renvoyer la scène entre deux changements.
- Desktop : reprendre ROI « None », export réussi/annulé/échoué et disparition de l’ancienne activité des coupes pendant recalcul.

Pour chaque essai physique, annoncer quand le casque doit être équipé et quand il peut être retiré. Ajouter les résultats datés ici séparément des verdicts originaux.


## Reports

| Chantier | Symptôme | Raison du report | Reprise | Critère de fermeture |
| --- | --- | --- | --- | --- |
| Préférences et disponibilité des atlas | JuBrain préchargé sur Desktop n'est pas garanti disponible sur Quest. IBC et DiFuMo chargés après publication n'ont pas de disponibilité partagée. MarsAtlas fonctionne actuellement grâce à son chargement systématique à la réception ; JuBrain n'a pas cette garantie. | Le contrat doit suivre les préférences sauvegardées et les ressources réellement chargées, y compris après publication. | Synchroniser lors de la sauvegarde Desktop, interpréter au pairing, suivre les atlas chargés. Inclure JuBrain, IBC, DiFuMo et la politique MarsAtlas. | Sauvegarde/pairing et chargement tardif rendent chaque atlas attendu disponible ; les réglages scientifiques suivants s'appliquent sans ressource manquante. |
| Aides visuelles Quest | Sphères ROI et fantôme des triangles effacés non affichés ; le masque ROI fonctionne. | Ces aides sont liées aux interactions locales Quest et à leurs modes d'édition. | Reprendre avec l'édition ROI/triangles et les interactions locales Quest. Aucun état supplémentaire de toolbar Desktop ajouté au contrat. | Aides visibles dans les modes locaux appropriés, masque scientifique inchangé et aucune dépendance au mode d'édition Desktop. |
| Historique limité à 4096 opérations | Une session assez longue peut atteindre le plafond d'opérations mémorisées. Le regroupement des previews ne règle pas la durée de vie de l'historique canonique. | La correction exige une rétention sûre, bornée en mémoire, compatible avec retries, propositions, checkpoints et reconnexion. | Concevoir une rétention bornée permettant une durée de session quelconque ; une augmentation du plafond ne suffit pas. | Essai prolongé bien au-delà de 4096 opérations, mémoire bornée, déduplication/retries/reconnexion corrects, aucune fermeture liée à la durée de session. |
| Labels dans le casque | Labels synchronisés mais invisibles sur Quest. | Présentation locale Quest à compléter. | Ajouter leur affichage et qualifier ordre, absence de doublons et lisibilité. | Labels visibles, ordre conforme à l'état canonique et modifications visibles sans renvoi de scène. |
| Contrôles scientifiques Quest | Certaines commandes restent absentes côté casque. | Les entrées locales Quest appartiennent aux jalons d'interaction suivants. | Implémenter ces commandes puis reprendre chaque ligne NT concernée. | Commandes locales disponibles et comportement scientifique validé dans les deux sens. |
| Essais NT/NA | Les lignes non exécutées et modalités exclues de Small restent non qualifiées. | L'absence d'échec observé ne constitue pas un essai. | Reprendre NT avec les entrées nécessaires et NA avec une visualisation de la modalité concernée. | Résultat daté et documenté pour chaque variante réellement exécutée. |
| Reconnexion avec choix Desktop/Quest | Choix de l'état à conserver non qualifié comme flux produit. | Rattaché aux jalons futurs de reconnexion. | Implémenter le choix utilisateur et la reprise depuis l'état retenu. | Choix des deux origines testé, sans mélange d'incarnations ni perte silencieuse. |
| Réapparition de l’activité après suppression | Projection initiale visible, suppression puis reprojection invisible sur Desktop et Quest ; reset des configurations rétablit le fonctionnement. | Occurrence manuelle sans cause ni fréquence déterminées ; ne pas appliquer un correctif spéculatif. | Reproduire le cycle et vérifier calcul, génération publiée, états de rendu et paramètres avant/après reset. | Plusieurs cycles projection/suppression/reprojection affichent l’activité des deux côtés sans reset. |
| Multi-scène | Synchronisation simultanée de plusieurs scènes hors portée M2 Small. | Rattachée aux jalons futurs multi-scène. | Isolation des sessions, ressources et commandes par scène. | Plusieurs scènes fonctionnent indépendamment, y compris fermeture, sélection et reconnexion. |

## Anomalie supplémentaire — durée de préparation du premier envoi

Retour manuel du 5 octobre 2026 : premier envoi interrompu par l’utilisateur pendant « Preparing visualization resources », jugé anormalement long ; durée non mesurée et renvoi sans modification non essayé. Voir le résultat supplémentaire de la recette.

L’inspection du code et de l’historique confirme que les corrections M2 n’ont pas modifié `DesktopSceneCapture` ni `CapturePreparedAsync`. Le chemin existant attend l’initialisation, charge les anatomies manquantes de tous les patients pour une scène multi-patients, attend la disponibilité de la représentation et la fin des travaux de projection/corrélation/colliders avant capture. Le chargement d’un maillage à deux hémisphères produit trois surfaces simplifiées ; les chargements sont séquentiels pour protéger le parser natif GIFTI.

Ces opérations constituent des pistes de coût, pas une cause mesurée de ce retour. Chantier de reprise : reproduire l’envoi et isoler l’attente dominante, vérifier la réutilisation des caches sur un second envoi, puis corriger le premier surcoût démontré sans retirer les ressources promises par le manifeste. Critère de fermeture : premier envoi abouti avec préparation expliquée et acceptable, second envoi sans travail lourd redondant, annulation fonctionnelle et publication complète. Aucun correctif supplémentaire appliqué à ce stade.

### Limite du premier diagnostic et audit du diff

L’absence de modification dans les fichiers de capture ne permet pas d’exclure un effet indirect du diff non stagé. Un changement concret de `SetGeneratorUpToDate(false)` marque désormais les textures anatomiques des coupes comme périmées ; cette invalidation entraîne aussi celles des textures fonctionnelles et GUI, reconstruites sur le thread Unity. Son coût et son éventuel effet sur la progression des attentes de capture n’ont pas encore été mesurés. Le contrôle du diff ne doit donc pas être présenté comme une preuve d’absence de régression.

Audit complémentaire en lecture seule : les dépôts hbp_core et hbp_math sont propres ; les DLL Windows EEGFormat, hbp_core et hbp_math dans Assets correspondent aux hashes du manifeste. Les modifications voisines constatées concernent le main de diagnostic EEGFormat et des références de splash de HiBoP_HoloLens. Le nouveau pacing de previews est exécuté après le démarrage du transport v2 ; le constructeur de session intervient dans le callback de capture, après les attentes de préparation. Aucun blocage causé par les modifications M2 n’est démontré, mais il n’est pas exclu par une comparaison de performances. La prochaine reproduction doit relever l’attente effective et comparer le comportement à HEAD sans écraser les changements de travail.

### Instrumentation temporaire — ajoutée puis retirée

L’utilisateur précise que la visualisation envoyée ne contient aucune coupe ; la piste de reconstruction des textures de coupe ne correspond donc pas à cette reproduction. La cause de l’attente reste à mesurer.

Des traces temporaires `[M2-PREP]` sont ajoutées au chemin Desktop : identifiant propre à chaque capture, temps écoulé en millisecondes et thread, débuts/fins des attentes d’initialisation, d’anatomie, de représentation, de projection/corrélation/colliders, chargements de chaque maillage/IRM, capture par ressource et colonne, copie des métadonnées, encodage et construction des blocs/manifeste. Le dernier `BEGIN` sans `END` correspondant localise l’étape en cours ; les écarts de temps donnent sa durée. Aucun log par frame n’est ajouté.

Cette instrumentation a servi au diagnostic puis a été entièrement retirée avec ses paramètres temporaires dans `DesktopSceneCapture.cs`, `Base3DScene.Transfer.cs` et `Base3DScene.cs`. Les traces conservées dans les journaux précédents sont historiques. Les verdicts de la recette restent inchangés jusqu’au nouvel essai manuel.

Validation de cette instrumentation : compilation/rechargement Unity MCP terminé ; test PlayMode ciblé `M2_AutomaticCutsAndPreparedInflation_ConvergeThroughProductionV2Sessions` passé (1/1, job `defeea0d06324bf8aa432df5df4fd116`). Les traces de début/fin sont effectivement présentes dans `Logs/Editor.log` jusqu’à la livraison de l’archive de la fixture ; aucune erreur console après le test. Cette fixture ne reproduit pas encore le premier envoi manuel bloqué.


### Correctif — anatomies individuelles hors du périmètre d’un envoi multi

Le propriétaire a identifié le chargement des maillages et IRM de tous les patients pendant l’envoi multi. Ces anatomies ne sont pas affichables dans la scène multi courante : leur préchargement Desktop sert à l’ouverture ultérieure de scènes single. L’ouverture de ces scènes depuis le Quest appartient à un jalon futur. Le chemin d’envoi forçait néanmoins ce travail même lorsque l’option de préchargement Desktop était décochée.

`CapturePreparedAsync` prépare désormais uniquement les ressources des listes de maillages et IRM de la scène courante. Le chargement de ressources individuelles depuis les métadonnées des patients est supprimé. `DesktopSceneCapture` exclut les caches anatomiques individuels du contenu transféré et ne les valide plus pour l’envoi ; cela vaut aussi lorsque ces caches sont déjà chargés sur Desktop. Le manifeste reflète les seules ressources effectivement envoyées. Le préchargement Desktop et l’ouverture single Desktop gardent leur fonctionnement ; l’anatomie d’une scène single appartient à ses listes courantes et reste transférée. Les données scientifiques, ressources MNI et masques restent dans le transfert.

Tous les logs temporaires du diagnostic sont retirés. Les constats et hypothèses précédents décrivent l’état avant ce correctif ; aucune mesure de gain sur la visualisation manuelle n’est encore disponible. Reprise attendue : premier envoi de `full_test.hibop / Unknown(2)`, puis second envoi sans changement, et confirmation d’une publication complète dans un délai acceptable.

Validation du correctif via Unity MCP : 8/8 tests PlayMode passés, job `a955d2786f004435a3693a613fcae721`, environ 43 s démarrage compris. Quatre cas de capture/restauration : multi sans préchargement, cache individuel non chargé, cache individuel déjà chargé, et single avec anatomie courante initialement non chargée. Assertions sur le manifeste, l’absence de caches individuels sur le destinataire, la conservation des ressources courantes, des masques et de l’activité iEEG. Trois cas couvrent la progression des frames, les captures concurrentes, la réutilisation des ressources et les annulations/fermetures pendant un chargement courant. La reproduction de géométrie M2 sur sessions v2 passe aussi. Aucun log d’erreur console après exécution ; recherche statique sans trace temporaire restante dans `Assets/Scripts`. Aucun essai physique ni chronométrage de `Unknown(2)` supplémentaire exécuté dans cette passe.


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

Inspection ciblée après ce retour : les commandes de suppression et de nouvelle demande rétablissent les flags de projection ; la publication d’un calcul valide réactive le matériau d’activité et invalide les surfaces/textures fonctionnelles. Aucun défaut certain permettant d’expliquer cette occurrence n’a été identifié en lecture seule. Aucun correctif de code ni nouveau test Unity exécuté dans cette passe ; le report reste ouvert avec le contournement observé.
