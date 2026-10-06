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
| Durée de session — 6 octobre | Rétention renouvelable après confirmation applicative, nettoyage des clés de conflit et des rollbacks ; 4096 borne le travail non terminé. Les previews remplacées et les corrections sont clôturées sans rechargement de scène. Les corrections de coupe absente et les messages déjà reçus lors d’une reprise restent résolus. Le défaut T10 de maillage/masque est corrigé avec validation de la topologie exacte, reconstruction ordonnée et préservation des masques plus récents et des propositions optimistes. | 195 tests ciblés validés en plusieurs passes : 100 000 opérations par direction. Sur casque : 8 192 mutations Desktop et 64 cycles de coupes convergents, historiques vides ; trois colonnes, déplacement et timeline confirmés par le propriétaire. Correctif T10 : 7 tests natifs + 99 tests existants passent ; APK vérifié installé, 512 contrôles puis deux diagnostics de huit états, dont un avec masques non vides. Convergence à 524, préfixe 70, historiques vides et aucune erreur T10. Ces essais acceptent le défaut reproduit ; les autres lignes de la matrice gardent leurs limites. [Rapport et limites](tasks/T06.md#t10-correction-and-successful-physical-replay). |

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
| Labels dans le casque | Labels synchronisés mais invisibles sur Quest. | Présentation locale Quest à compléter. | Ajouter leur affichage et qualifier ordre, absence de doublons et lisibilité. | Labels visibles, ordre conforme à l'état canonique et modifications visibles sans renvoi de scène. |
| Contrôles scientifiques Quest | Certaines commandes restent absentes côté casque. | Les entrées locales Quest appartiennent aux jalons d'interaction suivants. | Implémenter ces commandes puis reprendre chaque ligne NT concernée. | Commandes locales disponibles et comportement scientifique validé dans les deux sens. |
| Essais NT/NA | Les lignes non exécutées et modalités exclues de Small restent non qualifiées. | L'absence d'échec observé ne constitue pas un essai. | Reprendre NT avec les entrées nécessaires et NA avec une visualisation de la modalité concernée. | Résultat daté et documenté pour chaque variante réellement exécutée. |
| Reconnexion avec choix Desktop/Quest | Choix de l'état à conserver non qualifié comme flux produit. | Rattaché aux jalons futurs de reconnexion. | Implémenter le choix utilisateur et la reprise depuis l'état retenu. | Choix des deux origines testé, sans mélange d'incarnations ni perte silencieuse. |
| Réapparition de l’activité après suppression | Projection initiale visible, suppression puis reprojection invisible sur Desktop et Quest ; reset des configurations rétablit le fonctionnement. | Occurrence manuelle sans cause ni fréquence déterminées ; ne pas appliquer un correctif spéculatif. | Reproduire le cycle et vérifier calcul, génération publiée, états de rendu et paramètres avant/après reset. | Plusieurs cycles projection/suppression/reprojection affichent l’activité des deux côtés sans reset. |
| Surcoût du transfert single pour l’inflation | Les sources GIFTI natives et leurs transformations sont envoyées dès la publication pour chaque maillage patient concerné, même si inflated n’est jamais utilisé ; risque accru avec de nombreux maillages distincts. | Optimisation demandée après mise en place de l’inflation locale ; coût en octets et en durée non mesuré. | Retirer du transfert initial les sources utiles uniquement à l’inflation ; étudier leur récupération à la première demande pour la ressource concernée, avec cache. Voir le report détaillé ci-dessous. | Envoi initial sans ces sources supplémentaires ; première inflation correcte sans renvoi complet, cache réutilisé et gain mesuré sur un patient comportant plusieurs maillages. |
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


## Inflation locale coordonnée — 5 octobre 2026

Cette extension remplace, pour les nouveaux essais D16, la contrainte historique de préparer inflated avant l'envoi. Les observations et verdicts précédents restent conservés.

Le bouton de représentation passe par un job séquencé Desktop/Quest. Chaque appareil calcule localement ou réutilise son cache avec les mêmes paramètres ; le changement d'affichage est publié après les deux préparations, sans nouvel envoi de scène. Le visuel de chargement existant apparaît après 200 ms si la préparation le nécessite et se ferme au début de l'animation. Annulation/échec avant publication, fermeture et déconnexion libèrent les contrôles après l'arrêt du calcul natif. Une publication canonique déjà acceptée reste acquise.

Les fichiers GIFTI natifs et transformations ne sont ajoutés que pour les maillages courants qui les utilisent déjà sur Desktop. Le MNI préparé conserve son calcul dans les coordonnées des buffers existants. Les ressources individuelles exclues d'un envoi multi restent exclues. Les versions d'algorithme, paramètres, empreintes des entrées et identités du manifeste sont vérifiés. Les masques, UV/couleurs scientifiques et placement local Quest sont conservés.

La dépendance directe de `HBP.Sync.Scene` vers `UniTask` sert à la coordination asynchrone sur le PlayerLoop. `UniTask` ne dépend d'aucune assembly HBP ; aucun lien inverse depuis Core n'est ajouté. Le contrôle statique des dépendances passe.

Validation Unity MCP (`HiBoP@77bfedb67d0f947c`, Unity `6000.5.2f1`) :

- 4/4 tests PlayMode passés, job `857150e3f4f549dd976ea2d6c4770b1e`, 64,56 s d'exécution : entrée du bouton réel, scène sans cache, annulation avant publication, cache Desktop seul, représentation préparée avant capture, sources natives single avec transformation non uniforme, échec sur entrée modifiée puis nouvelle demande réussie, demandes Quest, allers-retours sans recalcul, géométrie affichée identique et préservation des masques/placement.
- 2/2 reprises PlayMode après les derniers ajustements passées, job 4413a65522434f0b87324e51291b9cc4, 28,41 s : calcul à froid/annulation et sources natives single.
- 13/13 tests de snapshots et inflation phase 4 passés, job 53300236de524a5b816972cb9b33b87c, 15,64 s : copier UV/couleurs dans le cache dérivé invalide également sa sérialisation, sans changer sa géométrie.
- 52/52 tests ciblés EditMode passés, job `878d8aa4b6ff4211a072b8c329705043` : inflation native, transformation non uniforme, caches, références scientifiques, rendu et codecs bornés.
- 254/254 tests `Sync.Fast`, `Sync.Loopback`, `Sync.SceneFocused` passés, job `cf5dd6d4982244648aa6471317811dc4`, 115,81 s d'exécution. Le gate des dépendances passe (44 assemblies HBP, 154 liens HBP directs).

Deux montages de test ont été repris avant ces résultats : accès incorrect à des chemins natifs du MNI préparé et chargement du menu 3D. Le chargement du prefab existant déclenche son défaut Editor `InformationsWrapper.OnValidate` : `m_ColorMap` non assignée. Cette exception indépendante est explicitement attendue dans l'essai du bouton ; elle n'est pas corrigée ici. La console de la passe réseau finale ne contient aucune erreur.

Première reprise USB sur fixture MNI à trois colonnes, sans cache inflated dans l'envoi : le résultat XML Unity est **1/1 Passed**, 89,85 s, sauvegardé dans `.test-results/inflated-local/physical-results.xml`. Le suivi MCP du job `b03909e0220d4a398e1d1a8fabf0939e` a perdu l'initialisation et affiché un timeout malgré l'exécution ; le verdict indiqué ici vient du XML inspecté. La réception, les ACK des deux allers-retours et la session toujours live sont attestés par `.test-results/m2-fixes/physical-desktop.json`. Le propriétaire confirme avoir vu le cerveau inflated ; il rapporte l'absence d'animation. Les autres critères visuels ne sont pas encore confirmés.

Le premier APK de cette reprise a été construit par le profil Quest via Unity MCP, signé directement par Unity avec le même certificat que l'application installée, vérifié puis installé avec `adb install -r`, sans désinstallation. Contrôle APK : 335 026 528 octets, 9 bibliothèques ARM64, SHA-256 `4b898a0bd344e88788efeb10dc196404e40813114c7c92301d0219f2b0066cc6`. Deux tentatives préalables ont été interrompues avant les gestes : lancement bloqué par les contrôleurs éteints, puis veille du casque pendant le transfert initial. Le Quest doit rester réveillé pendant les transferts/calculs Unity.

La console Desktop contient uniquement l'exception de prefab attendue décrite plus haut. Les logs Quest inspectés contiennent les erreurs OpenXR Meta de découverte de spaces et de visibilité de boundary (`xrDiscoverSpacesMETA`, `XR_ERROR_RUNTIME_FAILURE`), sans exception d'inflation observée ; ces messages XR restent hors de ce correctif.

Reprise de l'animation : la demande conserve maintenant l'animation existante de 0,6 s sur les deux appareils après préparation, puis attend les deux transitions avant publication canonique. Une première passe animée a détecté la remise à 1 du masque lors du nettoyage ; le nettoyage reconstruit désormais l'affichage accepté en préservant les données scientifiques et les triangles effacés. La revue indépendante a également demandé une stabilisation de la topologie avant animation et une libération des scopes garantie même si la restauration échoue. Les résultats de validation et de build sont consignés ci-dessous.

Validation de la transition animée :

- 4/4 tests PlayMode passés après correction du masque, job `60cb60ee505641bda62ff144d9a8da9f`, 80,90 s : animation des deux appareils, absence de publication intermédiaire et annulation pendant l'animation, en plus des scénarios précédents.
- 2/2 reprises après la revue passées, 47,77 s, XML `.test-results/inflated-local/animated-hemisphere-results.xml` : changement left/right/both immédiatement suivi d'une transition depuis le cache, annulation et masques conservés. L'erreur de compilation initiale du montage (affectation d'une propriété readonly) a été corrigée en passant par l'entrée `SelectMeshPart` de production.
- 256/256 tests `Sync.Fast`, `Sync.Loopback`, `Sync.SceneFocused` passés, 117,11 s, XML `.test-results/inflated-local/sync-final-results.xml`. Console de cette passe : aucune erreur. Gate des dépendances : 44 assemblies, 154 liens HBP directs.

Les suivis MCP des deux dernières passes ont également perdu leurs callbacks d'initialisation ; les résultats XML Unity ont été copiés et inspectés avant de déclarer ces succès. Aucun nouveau test n'a été lancé sur la seule base de ce faux timeout.

Build final avec animation : profil Quest via Unity MCP, job `build-662b94a6c0`, réussi en 158,64 s, 0 erreur et 67 avertissements. APK contrôlé : 335 023 744 octets, 9 bibliothèques ARM64, SHA-256 `ae716d6b1a3fea46175caec42a79cb3e628e7d9f21061c2257ce0bc2764f233b`. Signature Unity identique à l'application installée ; mise à jour USB réussie sans désinstallation. Le profil Desktop a été réactivé et le changement automatique des keywords XR de son asset URP restauré.

Reprise USB avec animation : **1/1 Passed**, 97,13 s, job `1f96aa8a67d14eaf937ea3626fa17ce7`, XML `.test-results/inflated-local/physical-animated-results.xml`. La fixture MNI à trois colonnes a effectué ses deux allers-retours sans renvoi de scène ; les ACK et la session live sont consignés dans `.test-results/inflated-local/physical-animated-desktop.json`. Le propriétaire confirme avoir vu la petite animation. Les critères visuels d'activité, de triangles, de placement et d'annulation physique restent à qualifier séparément.

Une première modification ajoutait une option au LoadingManager pour masquer le cercle au début de la transition, tout en conservant l'animation à l'intérieur de l'opération de chargement. Le propriétaire précise ensuite que cela ne répond pas à sa demande : l'animation doit être exécutée hors LoadingManager. L'animation commune existante est réutilisée ; c'est le chemin d'application distante qui la sautait auparavant. Aucun test automatique supplémentaire n'a été exécuté après ce premier ajustement ; compilation et construction uniquement.

APK du premier ajustement du cercle : build Android du profil Quest réussi en 163,64 s, 0 erreur et 47 avertissements (`build-7d2afd7b17`). Contrôle du contenu : 335 034 292 octets, 9 bibliothèques ARM64, SHA-256 `50a643cecf12fcf5a27bbdd67c513f1d966ef78766ce32e7d51d6136801eaaed`. La signature Unity correspond au certificat de l'application installée ; `adb install -r` a réussi sans désinstallation. Le profil Desktop et ses keywords XR ont été restaurés. Cet APK précède la séparation effective des phases décrite ci-dessous.

Correction de la séparation des phases : le LoadingManager retrouve sa signature précédente, sans option de masquage. Le bouton local prépare le cache dans une opération de chargement, puis lance la transition après son retour. Pour une session v2, le coordinateur utilise un wrapper de préparation installé par la couche UI des deux appareils : calcul, synchronisation de disponibilité et fermeture de l'opération avant transition. L'animation et le commit ne passent plus par LoadingManager. L'annulation du calcul reste reliée au job ; les erreurs de transition sont observées séparément. Aucun test automatique n'est relancé, conformément à la demande du propriétaire ; l'essai manuel reste à faire.

Vérification de cette séparation : compilation Unity sans erreur et revue ciblée de l'ordre Ready/Transition, de l'annulation à la sortie du chargement et des erreurs simultanées. Build Android `build-3460c48fb1` réussi en 166,88 s, 0 erreur et 68 avertissements. APK signé par Unity et contenu contrôlé : 335 041 236 octets, 9 bibliothèques ARM64, SHA-256 `0fe048d912823cb776b8ab4e660940d1b630b7c65ea00f21c0426c99465fcda8`. Le profil Desktop et ses keywords XR ont été restaurés. Aucun casque n'est détecté en USB à la fin de cette passe : l'APK est disponible dans `.artifacts/inflated-local/Android/HiBoP.Quest.apk`, mais cette version n'a pas été installée. Aucun test fonctionnel ni reprise physique supplémentaire exécutés.

### Report — passthrough Quest absent après relance

Symptôme : le propriétaire ne voit plus le passthrough avec l'APK animé, y compris après relance de HiBoP. Le test d'inflation et son animation ont néanmoins pu être observés. Le propriétaire demande de traiter le passthrough ultérieurement.

Raison du report : défaut XR distinct, non corrigé dans cette passe. Les logs signalent « Passthrough camera stopped », l'absence de sous-systèmes XR Session/Camera actifs et des erreurs OpenXR Meta. L'extension `XR_FB_passthrough` est présente, mais `XR_FB_scene_capture` est annoncée non supportée par le runtime observé. Ces constats ne suffisent pas à attribuer une cause certaine. Aucun réglage XR ou permission Android n'a été modifié pour contourner le défaut.

Chantier de reprise : vérifier la création des sous-systèmes AR/Meta et la compatibilité des fonctionnalités demandées avec le runtime du casque, en s'appuyant sur `.test-results/inflated-local/quest-passthrough-restart.log` et en comparant avec une version fonctionnelle. Critère de fermeture : passthrough visible après lancement et relance, puis maintenu pendant préparation et transitions anatomical/inflated.

### Report — sources d’inflation et durée du transfert single patient

Symptôme / risque signalé le 5 octobre 2026 : le transfert initial inclut les fichiers GIFTI natifs non transformés et leurs éventuelles transformations, en plus des surfaces préparées pour l’affichage, pour chaque maillage patient concerné. Ce supplément peut allonger inutilement l’envoi d’une visualisation single, particulièrement pour un patient comportant de nombreux maillages distincts, alors que l’inflation sera relativement peu utilisée selon le propriétaire. Le surcoût réel n’a pas été mesuré ; il ne s’agit pas d’un nouveau blocage reproduit. Le MNI standard n’ajoute pas ces fichiers au transfert.

Décision du 6 octobre 2026 : utiliser les coordonnées anatomiques déjà transformées sur Desktop et Quest. Le changement lié aux transformations anisotropes est accepté. La version de calcul 2 supprime la relecture GIFTI et les sources dédiées à l'inflation dans le transfert, sans conserver de copie permanente des positions natives.

Vérification : le test `M2_TransformedSingleInflation_ConvergesWithoutSourceFilesOrPreparedResult` contrôle l'absence des sources dans le payload, supprime les fichiers après chargement, puis calcule sur les deux sessions sans résultat préparé. Il mesure les octets compressés supprimés pour un mesh patient single transformé (les économies de métadonnées sont exclues). Les contrôles de progression, annulation, déconnexion et publication coordonnée sont conservés.

Critère de fermeture : une visualisation envoyée sans utiliser inflated ne transfère pas de sources supplémentaires dédiées à cette fonction ; la première inflation reste correcte, y compris avec une transformation non uniforme, sans renvoi complet de la scène. Les demandes suivantes réutilisent le cache ; annulation et échec laissent la session utilisable. Le gain sur le transfert initial est mesuré et documenté.

Mesure du 6 octobre 2026 : fixture single patient à trois colonnes, GIFTI MNI gauche chargé comme un mesh patient avec transformation anisotrope `(2, 1, 0,5)` et translation `(5, 7, 11)`. Payload préparé : **5 901 503 octets**. Ressources GIFTI/TRM compressées retirées : **804 940 octets**, soit environ **12,00 %** du payload précédent reconstitué (6 706 443 octets). La reconstitution ajoute uniquement les deux ressources avec la compression `Fastest` du conteneur ; elle exclut l'économie supplémentaire de métadonnées. Il s'agit d'une mesure de fixture, pas d'une estimation générale pour tous les patients. Les identifiants de livraison aléatoires peuvent faire varier légèrement la taille compressée des métadonnées entre les runs. Résultat brut : `.test-results/inflation-v2/payload-measurement.json`.

La comparaison Desktop/Quest ARM au seuil de **0,001 mm** est différée : le propriétaire fera l'essai matériel manuellement plus tard. La réussite sous Windows des sessions Quest simulées ne valide pas cette tolérance sur ARM. La procédure est ajoutée à la recette D16 ; Desktop et Quest doivent être déployés ensemble avec la version de calcul 2.

Validation Unity du 6 octobre 2026 : **83 tests EditMode distincts** passent (52 inflation/transfert, 31 scheduler ; les 11 tests de phase 4 ont aussi été relancés après ajout du fichier de parcellation). Les **6 scénarios PlayMode** sont validés : résultat préparé, calcul à froid et annulation, cache Desktop seul, surface patient transformée sans fichiers, mutation gauche et mutation droite pendant un job. Les deux derniers cas vérifient le motif de refus, le maintien de l'affichage, la notification distante et la reprise de la session ; leur fixture initialise le prefab de dialogues existant. Le défaut `InformationsWrapper.m_ColorMap` du prefab de menu demeure explicitement attendu dans ces essais, comme auparavant. Aucun autre défaut console n'est ignoré dans le dernier run. Revue statique indépendante sans défaut introduit confirmé ; gate des dépendances réussi (44 assemblies, 154 arêtes). Résultats MCP : `.test-results/inflation-v2/`.
