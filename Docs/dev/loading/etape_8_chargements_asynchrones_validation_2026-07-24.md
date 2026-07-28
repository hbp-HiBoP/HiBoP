# Étape 8 — Chargements asynchrones et validations différées

Date : 24 juillet 2026

Statut : conception validée, lots 8.1 à 8.5 implémentés.

Ce document est le point de départ de la dernière étape d'optimisation du
chargement. Il remplace la portée initiale de l'étape 8, qui ne concernait que
le réglage du parallélisme, par un chantier plus complet :

- chargement silencieux de la base locale ;
- validation différée des chemins et des données ;
- promotion d'une opération silencieuse en chargement visible lorsqu'un
  utilisateur doit l'attendre ;
- orchestration sûre des chargements concurrents ;
- réglage adaptatif et borné du parallélisme.

L'étape 9, qui proposait un nouveau format de projet, n'est pas prévue. Les
risques de compatibilité ne sont pas justifiés par les gains attendus après
les étapes 1 à 8.

## Objectifs utilisateur

### Base locale

Au démarrage de HiBoP :

1. les settings, les workspaces et les protocoles restent chargés en premier ;
2. le contenu principal de la base démarre silencieusement ;
3. les patients et `DataInfo` sont lus et liés sans loader visible ;
4. les chemins et l'intégrité des données sont ensuite vérifiés en arrière-plan ;
5. si l'utilisateur demande une fonction qui dépend de la base, un loader
   s'attache à l'opération déjà en cours et affiche sa progression actuelle.

Le chargement silencieux ne signifie pas que les exceptions sont ignorées.
Toute erreur technique doit être conservée par l'opération, journalisée et
présentée lorsque la base est demandée.

### Projet

Lors de l'ouverture d'un projet :

1. le loader actuel reste visible pendant la lecture de l'archive, la
   désérialisation et la liaison des références ;
2. le projet devient disponible dès que son graphe est cohérent ;
3. la validation des chemins et des `DataInfo` démarre immédiatement mais
   silencieusement ;
4. une opération qui dépend des fichiers, notamment une visualisation ou une
   sauvegarde, attend la validation si elle n'est pas terminée ;
5. dans ce cas, le loader s'attache à la validation existante puis enchaîne
   avec l'opération demandée, sans se fermer entre les deux.

## Principe central : deux niveaux de disponibilité

Un simple booléen `IsLoaded` ne suffit plus. Il faut distinguer la disponibilité
du graphe de la validité de ses références externes.

### Ready

L'état `Ready` garantit que :

- les JSON ont été lus ;
- les références sérialisées ont été liées ;
- le graphe a été publié atomiquement ;
- les listes et métadonnées peuvent être consultées et modifiées.

Il ne garantit pas encore que les fichiers référencés existent ni que les
données sont visualisables.

### Validated

L'état `Validated` garantit en plus que les aspects demandés par l'opération
courante sont à jour. Au démarrage et à l'ouverture d'un projet, cela signifie
uniquement :

- les chemins des meshes et IRM ont été contrôlés ;
- la disponibilité et l'empreinte légère des sources ont été contrôlées ;
- les résultats correspondent à la génération actuelle du projet ou du
  workspace.

Une opération dépendante des fichiers fournit sa propre `ValidationRequest` et
attend uniquement les aspects et `DataInfo` qu'elle utilise. Le rapport
d'intégrité explicite est le seul consommateur qui force tous les aspects sur
toute la base.

### États proposés

```text
NotStarted
Loading
Ready
Validating
Validated
ValidatedWithIssues
ValidationFailed
Cancelled
```

`ValidatedWithIssues` représente des erreurs normales dans les données :
fichier absent, extension invalide, header incomplet ou autre problème
d'intégrité. Ces problèmes ne sont pas des pannes du chargement.

`ValidationFailed` représente une exception technique qui a empêché la
validation de se terminer. Le graphe peut rester consultable s'il est déjà
`Ready`, mais les opérations qui nécessitent des données validées doivent être
bloquées avec une erreur explicite et une possibilité de réessayer.

## Opération partagée

Chaque base et chaque projet doivent posséder une opération de chargement
unique et observable. Cette opération contient au minimum :

- son identifiant ;
- sa génération ;
- son état ;
- sa dernière progression connue ;
- son texte de progression ;
- sa tâche `Ready` ;
- sa tâche `Validated` ;
- son résultat ou son exception ;
- son token d'annulation propre.

Plusieurs consommateurs doivent pouvoir attendre la même opération. Un nouvel
appel à `Ensure...Async` ne doit jamais relancer un travail déjà en cours.
Cette propriété est appelée `single flight`.

Les points d'entrée centraux envisagés sont :

```text
EnsureDatabaseReadyAsync()
EnsureDatabaseValidatedAsync()
EnsureProjectValidatedAsync()
```

Les anciens `WaitUntil(() => Database.IsLoaded)` devront être remplacés par
ces attentes explicites. Une attente explicite permet de :

- propager les exceptions ;
- respecter l'annulation ;
- attacher un loader si nécessaire ;
- éviter une attente infinie après un échec ;
- savoir précisément quel niveau de disponibilité est requis.

## Promotion silencieuse vers visible

Une opération de fond mémorise toujours sa progression, même sans interface.

Lorsqu'un utilisateur doit l'attendre :

1. le gestionnaire de chargement s'abonne à l'opération existante ;
2. il affiche immédiatement la dernière progression mémorisée ;
3. les mises à jour suivantes lui sont transmises sur le thread principal ;
4. il se détache lorsque le besoin utilisateur est satisfait ;
5. il ne démarre ni ne duplique l'opération.

Une annulation utilisateur doit être interprétée selon le contexte :

- annuler l'ouverture initiale d'un projet annule le chargement de ce projet ;
- annuler une visualisation qui attend une validation partagée annule
  l'attente et la visualisation, mais pas nécessairement la validation de fond ;
- remplacer le projet ou le workspace annule l'opération devenue obsolète.

## Gestion d'un loader unique

Le `LoadingManager` actuel ouvre et ferme directement un cercle global pour
chaque appel. Deux appels concurrents pourraient donc :

- remplacer leurs callbacks de progression ;
- associer le bouton d'annulation au mauvais travail ;
- fermer le loader alors que l'autre opération continue ;
- afficher un texte ou un pourcentage appartenant à une autre opération.

La cible est une présentation modale unique contrôlée par un coordinateur.
Chaque affichage reçoit une identité ou un bail. Seul le propriétaire courant
peut mettre à jour ou fermer le loader.

Une opération utilisateur composée de plusieurs phases doit conserver le même
affichage :

```text
Validation du projet
        puis
Chargement de la visualisation
```

Le loader ne doit pas se fermer puis se rouvrir entre ces phases.

Les validations silencieuses continuent à travailler sans posséder le loader.
Si plusieurs actions visibles sont demandées, le coordinateur les sérialise ou
leur attribue une priorité explicite.

## Pipeline de la base

Le pipeline cible est :

```text
Settings et workspaces
        ↓
Protocoles
        ↓
Lecture et liaison de la base — silencieuses
        ↓
Publication atomique — Ready
        ↓
Disponibilité des sources et assets — silencieuse
        ↓
Validated ou ValidatedWithIssues
```

Les settings et les workspaces sont déjà chargés séparément par
`GlobalDatabase.InitializeAsync`. Leur interface ne devrait donc pas attendre
inutilement la lecture de tous les patients.

Les fonctions qui affichent ou sélectionnent des patients et des données
doivent attendre au minimum `Ready`. Celles qui présentent un état d'intégrité
comme fiable doivent attendre `Validated` ou afficher explicitement que la
validation est en cours.

Un changement de workspace :

1. crée une nouvelle génération ;
2. annule l'opération de l'ancien workspace ;
3. construit le nouveau graphe sans modifier partiellement le graphe publié ;
4. publie le nouveau graphe en une fois lorsqu'il est `Ready` ;
5. ignore tout résultat tardif provenant de l'ancienne génération.

## Pipeline du projet

Le pipeline cible est :

```text
Lecture du manifeste et de l'archive — loader visible
        ↓
Désérialisation — loader visible
        ↓
Liaison — loader visible
        ↓
Publication atomique — Ready
        ↓
Fermeture du loader initial
        ↓
Disponibilité des sources et assets — silencieuse
        ↓
Validated ou ValidatedWithIssues
```

Le passage à `Ready` ne doit intervenir qu'après la liaison complète. Aucun
objet partiellement désérialisé ne doit devenir visible.

Les appels de visualisation devront tous passer par la même barrière de
validation. L'inventaire devra notamment couvrir :

- la fenêtre de visualisations ;
- les actions de toolbar ;
- la ligne de commande ;
- Quick Start ;
- les rechargements après modification des patients ;
- les rechargements après modification des datasets ;
- les rechargements après modification des protocoles ;
- les autres appels directs à `Module3DMain.LoadAsync`.

La sauvegarde du projet devra également attendre la validation. Les erreurs et
warnings des `DataInfo` étant sérialisés, sauvegarder pendant leur recalcul
pourrait sinon écrire un état ancien ou intermédiaire.

## Calcul hors thread principal et publication atomique

Les opérations coûteuses peuvent s'exécuter sur le thread pool :

- résolution et normalisation de chemins ;
- appels `File.Exists` ;
- lecture de headers ;
- calcul des erreurs et warnings ;
- autres traitements ne dépendant pas de l'API Unity.

En revanche, les workers ne doivent pas modifier progressivement le graphe
consulté par l'interface.

La validation doit produire des résultats indépendants, par exemple :

```text
PatientAssetValidationResult
DataInfoValidationResult
ProjectValidationResult
DatabaseValidationResult
```

Leur publication suit les règles suivantes :

1. tous les calculs de la phase sont terminés ;
2. l'annulation est vérifiée ;
3. la génération du résultat est comparée à la génération courante ;
4. l'application se fait en une fois, de préférence sur le thread principal ;
5. un événement unique demande le rafraîchissement de l'interface.

Cette séparation protège notamment :

- `BaseMesh.WasUsable` ;
- `MRI.WasUsable` ;
- les erreurs et warnings de `DataInfo` ;
- les erreurs et warnings des `DataContainer`.

Si l'utilisateur modifie un chemin, un dataset ou un protocole pendant la
validation, la génération concernée est invalidée. Les anciens résultats ne
peuvent jamais écraser la nouvelle valeur.

## Réintroduction de la validation des datasets

`Dataset.CheckDatasetsAsync` mélange actuellement :

- les `DataInfo` de la base ;
- les `DataInfo` du projet globalement chargé.

Ce comportement doit être remplacé par une validation explicite d'une
collection donnée. Chaque opération travaille sur un snapshot clairement
défini :

```text
ValidateDatabaseDataInfosAsync(snapshot, ...)
ValidateProjectDataInfosAsync(snapshot, ...)
```

La validation ne doit jamais consulter `ApplicationState.LoadedProject` pour
décider dynamiquement de son périmètre. Celui-ci doit être fixé à la création
de l'opération.

Les contrôles de `DataInfo` ne sont pas tous équivalents à des `File.Exists`.
Ils peuvent :

- examiner les extensions et tailles ;
- lire des headers ;
- ouvrir des fichiers EEG ;
- interroger les triggers ou les électrodes via `hbp_core`.

Les contrôles natifs doivent donc utiliser un budget de concurrence plus
prudent que les vérifications de chemins. Le crash Unity observé pendant les
étapes précédentes ne prouve pas que cette couche en était responsable, mais
il interdit de supposer qu'une concurrence de 20 est sans risque.

## Validation ciblée par dépendances

La validation est divisée en aspects indépendants :

```text
Structure
SourceAvailability
SourceReadability
StaticContent
Epoching
ChannelMapping
PatientAssets
```

Chaque `DataInfo` sérialise ses `ValidationState`, diagnostics et signatures.
Les tableaux publics `Errors` et `Warnings` restent une vue aplatie pour
préserver les consommateurs existants. Les états historiques sans catégorie
sont migrés vers le bucket correspondant puis marqués périmés lorsque leur
signature ne peut pas être démontrée.

Une `ValidationRequest` associe les aspects à leurs IDs de `DataInfo`, patients,
protocoles et sous-blocs. La fusion conserve les périmètres par aspect : une
requête d'epoching sur A fusionnée avec une requête de channels sur B ne
contrôle ni les channels de A ni l'epoching de B.

Les validations sémantiques EEG partagent une seule ouverture native pendant
une opération. Le lecteur est injectable pour compter les ouvertures dans les
tests. Aucune liste de triggers ou de channels n'est conservée après
l'opération.

Les modifications sont analysées par signatures minimales :

- chemin ou container : disponibilité, lisibilité et règles dépendantes ;
- protocole : epoching des seuls sous-blocs dont l'événement principal change ;
- code secondaire : aucun accès fichier ;
- patient : assets modifiés et liaisons si les noms de sites changent ;
- canal stimulé CCEP : liaison mémoire uniquement ;
- nom et apparence : structure locale uniquement.

Le changement de workspace et l'ouverture d'un projet ne font jamais de lecture
native ni de parcours CSV complet. Trial Matrix, la visualisation et les exports
demandent ensuite leurs prérequis sur les seules données sélectionnées.

## Parallélisme adaptatif

La valeur `20` en dur doit disparaître. Une politique centrale fournit une
valeur par phase.

Principes :

- si le multithreading utilisateur est désactivé, la concurrence vaut 1 ;
- les valeurs dépendent du nombre de processeurs logiques ;
- chaque catégorie possède un plafond conservateur ;
- les contrôles natifs ont un plafond inférieur aux accès disque simples ;
- la base et le projet partagent un budget global ;
- un chargement visible est prioritaire sur une validation silencieuse ;
- les résultats sont stockés à leur index d'entrée ;
- la progression UI n'est jamais invoquée directement depuis un worker.

Une première politique pourra distinguer :

```text
Parsing JSON et ZIP
Validation de chemins
Validation de métadonnées
Validation utilisant hbp_core
```

La formule exacte sera décidée après mesure. La matrice reste :

```text
1, 2, 4, 8 et 20 workers
```

Ces mesures servent à choisir des plafonds et une formule adaptative, pas une
valeur universelle. Un auto-tuning dynamique permanent n'est pas prévu : il
ajouterait de la variabilité et de la complexité. Une politique basée sur
`Environment.ProcessorCount`, des plafonds et le réglage utilisateur est
préférée.

Le scheduler global doit empêcher la base et le projet d'allouer chacun leur
pool maximal simultanément. Il doit également pouvoir réduire temporairement
le travail silencieux lorsqu'une opération demandée par l'utilisateur commence.

## Risques et protections

| Risque | Protection |
| --- | --- |
| Graphe visible à moitié construit | Publication uniquement après lecture et liaison complètes |
| Résultat d'un ancien projet appliqué au nouveau | Identifiant de génération vérifié avant publication |
| Deux validations identiques | Tâche partagée `single flight` |
| Deux loaders se ferment mutuellement | Coordinateur et bail d'affichage identifié |
| Exception perdue en arrière-plan | Opération possédée, exception conservée et observée |
| Annulation avec résultats partiels | Résultats temporaires et publication atomique |
| UI lisant des tableaux en mutation | Calcul séparé puis remplacement groupé sur le thread principal |
| Sauvegarde d'erreurs périmées | Barrière `Ensure...ValidatedAsync` avant sauvegarde |
| Visualisation avant validation | Barrière commune devant tous les appels 3D |
| Modification pendant la validation | Invalidation de génération et redémarrage |
| Saturation CPU ou disque | Scheduler global, plafonds et priorité au foreground |
| Instabilité de la couche native | Pool dédié conservateur et tests de stress |
| Attente infinie après une panne | États terminaux explicites et propagation de l'exception |
| Comportement différent selon l'OS | Politique portable, concurrence bornée et validation Player |

## Stratégie de migration

L'architecture ne doit pas être activée en une seule modification.

### Lot 8.1 — Tests et opération partagée

- ajouter les états et l'objet d'opération ;
- mémoriser progression, résultat et exception ;
- garantir le `single flight` ;
- conserver le chargement visible et bloquant actuel ;
- ne modifier aucun comportement utilisateur.

Critère : les résultats et la progression restent identiques à l'existant.

### Lot 8.2 — Résultats de validation séparés

- faire produire des résultats indépendants par les validations ;
- appliquer les résultats atomiquement ;
- introduire les générations ;
- conserver encore les validations dans le loader initial.

Critère : erreurs, warnings et `WasUsable` sont identiques avant et après le
refactoring.

### Lot 8.3 — Validation projet silencieuse

- terminer le loader projet à l'état `Ready` ;
- poursuivre la validation en arrière-plan ;
- ajouter les barrières devant visualisation et sauvegarde ;
- couvrir tous les points d'entrée 3D.

Critère : aucun accès dépendant des fichiers ne peut contourner la validation.

### Lot 8.4 — Base silencieuse

- démarrer le contenu principal de la base sans loader ;
- conserver settings, workspaces et protocoles disponibles tôt ;
- promouvoir l'opération lorsque la base est demandée ;
- gérer explicitement les changements de workspace.

Critère : aucun écran de base ne lit un graphe non publié et aucune exception
de fond n'est perdue.

Implémenté le 28 juillet 2026 : le graphe de base est publié atomiquement à
`Ready`, sa validation se poursuit silencieusement, les consommateurs
s'attachent à l'opération partagée avec des barrières explicites et les
changements de workspace remplacent la génération courante. Une exception de
fond reste attachée à l'opération jusqu'à sa présentation par un consommateur ;
la demande suivante peut alors relancer le chargement.

### Lot 8.5 — Validation ciblée par dépendances

- introduire les aspects, requêtes, états et signatures ;
- séparer les diagnostics par règle ;
- comparer les clones avant/après et invalider uniquement les dépendances
  touchées ;
- conserver une vue publique aplatie compatible ;
- demander les prérequis exacts à Trial Matrix, la visualisation et aux exports ;
- réserver la validation complète au rapport d'intégrité.

Critère : le démarrage et l'ouverture d'un projet ne déclenchent aucune lecture
native ou CSV complète, et une modification locale n'ouvre que les sources
réellement dépendantes.

Implémenté le 28 juillet 2026 : requêtes fusionnables à périmètres indépendants,
états sérialisés par aspect et sous-bloc, analyse d'impact protocoles/patients/
`DataInfo`, lecture EEG injectable et partagée par opération, barrières ciblées
pour Trial Matrix, visualisation et exports, rapport d'intégrité complet.

### Lot 8.6 — Scheduler adaptatif

Implémenté le 28 juillet 2026 : politique centrale par catégorie, budget global
partagé entre base et projet, priorité foreground dynamique, mode de repli à un
worker et matrice complète sur `Default` et `visu_full_test`. Les plafonds
retenus sont 8 pour JSON/ZIP et chemins, 4 pour les métadonnées et 2 pour les
appels natifs. Voir
[`resultats_etape_8_6_2026-07-28.md`](resultats_etape_8_6_2026-07-28.md).

- supprimer les valeurs 20 en dur ;
- séparer les catégories de travail ;
- ajouter le budget global et la priorité foreground ;
- exécuter la matrice de benchmark ;
- retenir les plafonds après mesure.

Critère : le parallélisme améliore le temps sans augmenter les erreurs,
l'instabilité native, le pic mémoire ou la latence d'annulation.

## Stratégie de tests

### Tests déterministes d'orchestration

Les tests ne doivent pas dépendre de délais réels. Des implémentations
injectables et des sources de complétion contrôlées doivent permettre de
suspendre précisément une opération.

Cas minimaux :

1. une opération silencieuse se termine sans ouvrir de loader ;
2. un utilisateur s'attache à 30 ou 70 % et reçoit immédiatement la bonne
   progression ;
3. deux consommateurs attendent la même tâche sans dupliquer le travail ;
4. l'annulation d'un consommateur ne publie aucun résultat partiel ;
5. l'annulation d'une visualisation ne tue pas une validation partagée encore
   utile ;
6. un changement de projet annule l'ancienne génération ;
7. une ancienne opération terminant tardivement ne publie rien ;
8. une exception technique produit `ValidationFailed` ;
9. une erreur de données produit `ValidatedWithIssues` ;
10. la progression reste monotone et comprise entre 0 et 1 ;
11. un seul propriétaire peut fermer le loader ;
12. la sauvegarde et la visualisation attendent la validation ;
13. une mutation invalide puis relance la validation ;
14. le mode monothread suit exactement les mêmes transitions.

Les tests async doivent attendre directement les `Task` ou `UniTask`. Aucun
`Wait`, `.Result`, `GetAwaiter().GetResult()`, `Thread.Sleep`, busy-wait ou
assertion NUnit async bloquante ne doit être introduit.

### Tests de compatibilité et de sérialisation

- projets historiques ;
- projets actuels ;
- round-trip sauvegarde puis rechargement ;
- aliases Windows et Linux ;
- chemins absents ou invalides ;
- archives corrompues ;
- ancien dossier `Protocols/` accepté mais ignoré ;
- mêmes erreurs, warnings et états de disponibilité après validation ;
- registre de types et IL2CPP inchangés.

### Tests Play Mode

Avec des opérations artificiellement ralenties :

- ouvrir le navigateur de base pendant le chargement ;
- ouvrir et fermer des fenêtres pendant la validation projet ;
- demander une visualisation avant la fin de la validation ;
- annuler cette visualisation ;
- changer rapidement de projet ;
- changer de workspace ;
- vérifier l'absence de `NullReferenceException` et de loader bloqué ;
- vérifier le rafraîchissement de l'interface après publication des résultats.

### Tests de stress

- alternances rapides entre plusieurs projets ;
- annulations répétées ;
- base et projet validés simultanément ;
- plusieurs consommateurs de la même opération ;
- fichiers locaux et chemins réseau ;
- contrôles `hbp_core` répétés avec plusieurs niveaux de concurrence ;
- fermeture de l'application ou Domain Reload pendant une opération.

### Validation Player

Le comportement doit être vérifié au minimum :

- dans l'Editor Mono ;
- dans un player Windows IL2CPP ;
- dans un player Linux IL2CPP via un runner Linux ou la CI.

Les tests Player doivent notamment vérifier la propagation des exceptions, les
transitions d'état et l'absence de dépendance à une API indisponible sous
IL2CPP.

## Instrumentation

Les diagnostics de chargement devront enregistrer :

- l'identifiant et la génération de l'opération ;
- les transitions d'état ;
- les durées `Ready` et `Validated` ;
- le moment où un loader s'attache et se détache ;
- le nombre de consommateurs ;
- la concurrence configurée et réellement utilisée ;
- les annulations ;
- les exceptions techniques ;
- le nombre d'erreurs et warnings de données ;
- le rejet d'un résultat provenant d'une ancienne génération.

Le benchmark doit distinguer :

- temps jusqu'à `Ready` ;
- temps jusqu'à `Validated` ;
- temps effectivement bloqué pour l'utilisateur ;
- CPU, GC et pic mémoire ;
- phases de chemins, métadonnées et appels natifs.

La campagne finale utilisera le workspace `Default` et le projet `full_test`,
avec trois exécutions de chaque opération et comparaison des médianes chaudes.

## Mode de repli

Un réglage interne de diagnostic doit permettre :

```text
BackgroundValidation = false
ConcurrencyOverride = 1
```

Il ne doit pas maintenir une deuxième implémentation. La même pipeline est
utilisée, mais le code attend toutes les phases avec le loader visible, comme
dans le comportement actuel.

Ce mode permet :

- de diagnostiquer un problème propre à une machine ou un OS ;
- de retrouver immédiatement un comportement séquentiel ;
- de comparer les résultats synchrones et asynchrones ;
- de sécuriser une version en cas de régression tardive.

## Conditions d'activation

Le comportement silencieux ne sera activé par défaut que lorsque :

1. les tests existants restent tous verts ;
2. les nouveaux tests d'orchestration sont verts ;
3. les tests Play Mode ciblés sont verts ;
4. les scénarios d'annulation ne publient jamais de résultats partiels ;
5. une ancienne génération ne peut jamais modifier le graphe courant ;
6. toutes les entrées de visualisation et de sauvegarde utilisent une barrière ;
7. la console Unity ne contient aucune nouvelle erreur ;
8. un round-trip de `full_test` reste identique ;
9. le mode de repli fonctionne ;
10. les benchmarks confirment un meilleur temps perçu sans instabilité.

## Critère de réussite final

L'étape est réussie si :

- HiBoP démarre sans loader imposé par la base ;
- l'ouverture d'un projet rend son graphe disponible avant les validations
  disque ;
- un utilisateur qui demande une ressource en cours de chargement voit la
  progression réelle de l'unique opération existante ;
- aucune action dépendante des fichiers ne peut utiliser un état non validé ;
- aucune exception de fond n'est perdue ;
- aucune ancienne opération ne peut publier dans un nouveau projet ou
  workspace ;
- les contrôles d'intégrité complets sont réintroduits ;
- le parallélisme s'adapte à la machine tout en restant borné et désactivable ;
- la rétrocompatibilité des projets et la compatibilité IL2CPP sont conservées.
