# Correctifs Display : crash Small + Single et attente Quest

14 octobre 2026.

## Crash de l'éditeur

Deux crashes signalés par l'utilisateur pointent vers
`hbp_volume_copy_histogram_bins`, appelé par `ThresholdMRI.UpdateMRIHistogram`
depuis la toolbar. Les dumps sont conservés dans les dossiers Unity
`Crash_2026-10-14_120757125` et `Crash_2026-10-14_120927851` ; une copie du log
initial est dans `.test-results/display-sync-fixes-20261014/Editor-crash.log`.

Une course compatible avec cette pile a été identifiée : `FinalizeInitialization`
publiait la sélection et lançait la préparation anatomique sans l'attendre. Le worker
et les lecteurs paresseux de `MRI3D.Volume` pouvaient charger le même volume.
Le booléen `m_IsLoading` ne rendait pas atomiques le test et le début du chargement.
Le chargement natif remplace les voxels pendant que l'histogramme les parcourt.

La fin d'initialisation attend désormais l'anatomie avant la sélection, la
configuration et la publication aux consommateurs Desktop. Le chargement MRI est
protégé par instance et réutilise le volume déjà chargé. La toolbar ne lit plus
une scène en fermeture ou dont l'anatomie n'est pas prête ; sa demande de mise à
jour est conservée pendant cette attente. Aucun changement de DLL native.

## Admission de Display

Display ouvre sa présentation avant d'attendre les opérations Quest existantes :
inventaire, restauration, rechargement, publications ou autre lot. Cette attente
est annulable et n'annule pas le travail partagé. Le lancement effectif est réservé
sur Unity après disponibilité, sans démarrer une seconde opération concurrente.
Les démarrages automatiques de scènes/inventaires sont suspendus pendant le lot.

Le mode local est décidé immédiatement si aucun Quest n'est connecté à l'entrée.
Pour un lot synchronisé, identité, contexte, génération et préférence sont épinglés
avant l'attente. Un changement de session conserve le chargement local et marque
la synchronisation comme nécessitant une reprise explicite.

Une reprise externe attend aussi l'opération courante. Seule une reprise appartenant
à cette même opération peut éviter de s'attendre elle-même, par vérification de son
token propriétaire. La reprise manuelle reste possible avec l'automatisme désactivé.

## Vérifications réalisées

- 5 tests EditMode MRI/publication réussis, dont douze workers et vingt demandes de
  chargement/lectures d'histogramme par worker, puis réutilisation après retrait du
  fichier source. Job `e984262e4bf1427fb4573d529ae316ca`.
- 15 tests PlayMode réussis : attente, annulation indépendante, propriété de reprise,
  mode local, garde toolbar, progression et restauration scientifique avec calcul
  automatique activé/désactivé. Job `9fa366ff66d441ca83a0641126bc76ad`.
- Le test existant des autres outils de toolbar repasse également : job
  `8e6dc10052cb42eba879a4389fbd023f`. Sa fixture synthétique a été déclarée initialisée
  pour tester les contrôles disponibles, conformément à la nouvelle garde ; la
  première exécution laissait cette scène artificielle non initialisée.
- Trois ouvertures locales Small + Single dans l'éditeur : réussies, histogrammes
  des deux MRI effectivement calculés ; durées indicatives 7 844 / 3 309 / 3 756 ms.
- Trois ouvertures synchronisées Small + Single dans le même éditeur avec le Quest
  USB éveillé : réussies, sans crash ni erreur console ; 19 129 / 16 455 / 15 925 ms.
- Admission déterministe dans le vrai parcours Display : une tâche partagée retenue
  laisse une présentation ouverte et zéro scène ; après libération, les deux scènes
  sont synchronisées. Annulation séparée : une présentation avant, zéro après,
  zéro scène et tâche partagée toujours en attente.
- Contrôle des dépendances réussi : 44 assemblies, 162 arcs directs.

Les durées éditeur ne sont pas une nouvelle baseline Release. Les sept lots exercés
sans crash ne constituent pas une preuve pour toutes les combinaisons de ressources
ou tous les cycles de fermeture. Le verrou MRI ne remplace pas la propriété de durée
de vie des scènes : leur drainage reste nécessaire avant libération native.

Traces et résultats : `.test-results/display-sync-fixes-20261014`.
Le protocole Desktop–Quest ne change pas ; le Quest conserve l'APK compatible déjà
installé pour cette validation. Aucun build Player supplémentaire n'est revendiqué.
