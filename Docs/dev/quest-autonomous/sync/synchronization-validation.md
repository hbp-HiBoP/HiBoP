# Validation, diagnostics et limites

Les contrats actuels sont décrits dans [architecture](synchronization-architecture.md), [opérations](synchronized-operations.md) et [wire](wire-protocol.md). Cette page sépare les points à vérifier aujourd'hui des résultats historiques. **Aucun test Unity, build ni essai physique n'a été exécuté pour la consolidation du 2 novembre 2026.**

## Couverture exécutable

Les tests sont les points d'entrée à sélectionner selon le changement, pas une attestation de réussite courante.

| Domaine | Sources de vérification |
| --- | --- |
| Codecs, bornes, identités et scheduler | [HBP.Sync.Tests](../../../../Assets/Tests/EditMode/HBP.Sync.Tests), notamment `ContractCodecTests`, `SchedulerTests` |
| Application, rebase, publication, merge, horloge | [HBP.Transfer.Scene.Tests](../../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests) : `SceneMutationBoundaryTests`, `OnlineMutationDriverTests`, `SceneCheckpointMergeTests`, `TimelineClockEstimatorTests` |
| Transport réel en loopback, bulk et sessions | `PersistentTransportLoopbackTests`, `MultiSceneTransportTests`, `SessionControlTransportTests`, `SessionPreferencesTests`, `SessionDefinitionsTests` dans le même dossier |
| Ressources et calculs | `PreparedSceneArchiveTests`, `ActivityProjectionTests`, puis [NativePreparationTests](../../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/NativePreparationTests.cs) |
| Fermeture, remplacement, ownership et présentation | [QuestSceneLifecycleTests](../../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/QuestSceneLifecycleTests.cs), [QuestAnatomyLifetimeTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestAnatomyLifetimeTests.cs), [SceneDisplayOwnershipTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/SceneDisplayOwnershipTests.cs) |

Pour exécuter Unity, suivre le workflow du dépôt et le skill `hibop-unity-validation`. Les tests qui ont besoin du PlayerLoop attendent directement leurs opérations asynchrones ; un timeout observé n'autorise pas à laisser le travail actif puis lancer une autre suite. Un handle natif reste vivant jusqu'à la fin effective du travail commencé.

Un essai réseau utile distingue : admission socket, application Unity, génération scientifique terminée et observation visuelle. « NextVisible » désignait une éligibilité CPU au rendu, pas la première image vue dans le casque. Ne pas soustraire des timestamps de deux appareils sans estimation d'horloge. La frontière historique `ScientificStable` n'a pas reçu de qualification complète.

## Mesures de référence

Ces mesures datées expliquent des choix et des limites ; elles ne qualifient pas les performances du checkout courant. Les échantillons et les frontières de mesure diffèrent : ils ne forment pas une série de benchmarks directement comparables.

| Sujet et conditions | Résultat observé | Portée |
| --- | --- | --- |
| Latence de l'ancien échange par snapshot, 22 septembre 2026 ; Windows Editor + Quest Wi-Fi, Unity 6000.5.2f1 | Médiane setter → visible ACK : couleur **1130,9862 ms** (n=8), coupe **1387,7274 ms** (n=9), timeline **1693,3038 ms** (n=25). Un transfert initial : **37,402764 s**. | Architecture remplacée depuis. Échantillons insuffisants pour les qualifications p95 prévues (100/300/100). Aucune latence du runtime actuel n'en est déduite. |
| Frontière inline/bulk du scheduler, 24 septembre ; en processus, 8 échauffements puis 24 mesures | Corps de **4096 octets : 3,98 µs** ; **4097 octets : 53,08 µs**. | Mesure du coût de changement de chemin, sans réseau ni rendu. |
| Checkpoint de 30 000 sites, fin septembre | **3 064 297 octets**, 13 records, 188 chunks ; loopback **1840,4 ms**, autre matrice **1725,7 ms**. | Deux essais distincts ; pas de garantie de performance en casque. |
| Livraison d'inflation, 6 octobre ; trois colonnes, hémisphère MNI gauche utilisé comme patient, échelle 2/1/0,5 et translation 5/7/11 | Payload **5 901 503 octets** ; retrait de **804 940 octets** de GIFTI/TRM compressés, soit environ **12 %** du total précédent reconstitué de 6 706 443 octets. | Compression Fastest des ressources ; gain de métadonnées exclu. La cible de parité matérielle à 0,001 mm n'était pas qualifiée par cet essai Windows. |

L'analyseur [analyze-sync-baseline.py](../../../../Tools/analyze-sync-baseline.py) lit les anciens exports. Il ne réintroduit pas la télémétrie snapshot/visible ACK retirée du runtime. Son ancien collecteur borné à 8192 enregistrements invalidait une mesure en cas de pertes. Les seuils numériques de performance ont été différés le 8 octobre 2026 ; aucune nouvelle garantie n'est définie ici.

## Comportements vérifiés et limites

| Comportement | Observation historique | Ce qui reste distinct |
| --- | --- | --- |
| Rétention et changement de topologie | Le 6 octobre, session matérielle de **8192 couleurs et 64 cycles de coupes** acceptée ; correction de topologie revue sur deux parcours de huit états. | Toutes les opérations, modalités et interruptions ne sont pas couvertes. |
| Contrôles Quest | Acceptation utilisateur du périmètre d'interface le 8 octobre. | Elle ne qualifie pas chaque opération scientifique, ni le routage multiscène ajouté ensuite. |
| Reconnexion et fusion hors ligne | Le 8 octobre, placement initial des sites, reconnexion sans changement, combinaison d'éditions indépendantes, choix Desktop/Quest en cas de conflit et parcours de reprise acceptés sur casque. Les poses locales devaient rester préservées. | Le texte final des dialogues a changé après le build testé ; l'annulation du dialogue avait été testée avant son remplacement par un choix obligatoire. Le statut d'une scène fermée sur Desktop restait à clarifier dans la recette multiscène. |
| Inventaire et rechargement des scènes | Le 12 octobre, envoi manuel et une partie de la recette matérielle acceptés. Le rechargement après changement de normalisation avait exposé un accès Unity depuis un worker, corrigé avec des régressions automatisées. | La recette physique de normalisation devait être répétée après correction ; les derniers parcours de rechargement n'étaient pas tous validés. Aucune acceptation matérielle complète n'est déduite des tests. |
| Vérification automatisée après reprise séquentielle | Le 12 octobre : **1067 réussis, 1 ignoré, 337,25 s** en CLI ; **16 réussis, 92,36 s** en PlayMode. | Résultats datés, pas une exécution actuelle. Le test ignoré dépendait d'une fixture HBNA externe. |

La recette Small ne couvrait pas CCEP, statique, fMRI et MEG ; les corrélations n'y avaient pas été testées et plusieurs opérations sur les ressources n'avaient qu'une couverture bidirectionnelle partielle. Le déplacement des sites avait été accepté visuellement sur Small, sans démontrer la parité numérique Windows/ARM. Des positions brutes ne doivent pas remplacer le contrat commande pour masquer un écart.

## Points à qualifier

- **Frontière d'erreur applicative.** Dans [QuestAnatomySession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs), `IsTransientReplicaInterruption` classe encore `InvalidOperationException` avec les interruptions IO/socket/disposal. Qualifier ce chemin avant de promettre qu'une erreur métier ne déclenche jamais une reconnexion. Ce constat de code ne constitue pas une reproduction d'un défaut utilisateur actuel.
- **Parcours matériels.** Ressources bidirectionnelles, longues coupures/reconnexion, préférences pendant travail natif, inventaire, fermeture et ouverture patient nécessitent leurs propres essais. Les tests de codecs ou loopback ne remplacent ni ces parcours ni les comparaisons de buffers scientifiques.
- **Symptômes à isoler s'ils réapparaissent.** Une scène devenue invisible après retrait puis reprojection d'activité avait été signalée le 5 octobre, sans reproduction isolée ni correction globale démontrée. Le parseur statistique historique désactivé de [SiteConditions](../../../../Assets/Scripts/HBP/Data/Module3D/SiteConditions.cs) conserve aussi un TODO de plage temporelle : ses helpers parcourent `Trial.AllValues`. Ce dernier constat ne concerne pas le job de filtre actif.

Une violation d'accès native Mono du 12 octobre était survenue pendant `RestoreSceneManagerSetup` / `UnloadUnusedAssets`, dans un contexte de suites concurrentes ou annulées. La cause racine n'est pas établie et la reprise séquentielle réussie ne prouve pas une correction native. Pour un diagnostic similaire, les traces locales historiques étaient sous `.test-results/unitask/crash-2026-10-12/`, rapport `Crash_2026-10-12_134308403` : lecture `0x100000000`, code `0xC0000005`, pile `mono_add_process_object`, sans heap managé dans le dump. L'absence de plugin dans cette pile n'exclut pas une corruption antérieure.

## Provenance

Les [résultats de référence Quest](../../../../Validation/Quest/retained-results.md) contiennent les comparaisons scientifiques et les instructions de consultation des anciens rapports dans Git. Les listes de tâches, états de reprise et CSV de recette Small ne font plus partie de la documentation courante. Certains rapports liés et gros artefacts locaux n'étaient jamais disponibles dans le dépôt ; aucun résultat n'a été reconstruit à partir de leur absence.
