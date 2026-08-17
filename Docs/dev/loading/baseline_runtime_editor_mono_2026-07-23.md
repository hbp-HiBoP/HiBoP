# Baseline runtime du chargement — Editor Mono

Date : 23 juillet 2026  
Runtime : Unity Editor `6000.5.2f1`, Mono, Windows  
Instrumentation : schéma JSON `1`

## 1. Résumé

La capture confirme que la lenteur ne vient pas principalement de la lecture
des fichiers JSON.

Sur le workspace de 240 patients :

- chargement total : **9,992 s** ;
- lecture cumulée des 240 fichiers patients : **2,367 s** ;
- désérialisation cumulée des patients : **156,586 s** ;
- rattachement explicite des tags dans `CheckTagsAsync` : **32,533 s**
  cumulées ;
- recherches logiques de tags : **1 112 754**, soit exactement trois passages
  sur les **370 918** valeurs ;
- appels `File.Exists` : **1 201**, pour **1,407 s** cumulée ;
- CPU processus pendant la session : **118,359 s** ;
- croissance nette de la mémoire managée : **185,66 Mio** ;
- collections GC observées : **96**.

Les durées de phase sont cumulées sur des tâches exécutées avec une concurrence
maximale de 20. Elles décrivent la quantité de travail, mais ne doivent pas être
additionnées ni comparées directement au temps mural.

La priorité suivante reste donc l'étape 1, l'index des tags. Les mesures
renforcent nettement la conclusion de l'audit statique.

## 2. Inventaire de la capture

Quarante rapports valides et réussis ont été produits :

| Nature | Rapports | Contenu |
| --- | ---: | --- |
| Initialisation de base | 6 | settings, protocoles et références |
| Chargement complet de base | 2 | workspace de 2 patients et workspace de 240 patients |
| Lecture de manifeste projet | 30 | deux parcours d'une liste de 15 projets |
| Chargement complet de projet | 2 | deux ouvertures du même projet de 218 patients |

Les petites sessions de settings, protocoles et références durent moins de
20 ms chacune. Elles ne sont pas une priorité d'optimisation.

## 3. Base de données

### 3.1 Vue d'ensemble

| Mesure | Petit workspace | Workspace de charge |
| --- | ---: | ---: |
| Patients | 2 | 240 |
| Valeurs de tags | 26 664 | 370 918 |
| Octets patients | 7 006 740 | 114 573 351 |
| Temps mural total | 442 ms | 9 992 ms |
| CPU processus | 1 500 ms | 118 359 ms |
| CPU / temps mural | 3,39 | 11,85 |
| Mémoire managée nette | +19,61 Mio | +185,66 Mio |
| Collections GC de session | 12 | 96 |
| Recherches de tags | 79 992 | 1 112 754 |

Le nombre de recherches vaut exactement trois fois le nombre de valeurs de
tags dans les deux workspaces. L'instrumentation confirme donc le modèle
calculé dans l'audit :

1. résolution du tag pendant `BaseTagValue.OnDeserialized` ;
2. validation dans `Patient.CheckTagsAsync` ;
3. recherche des valeurs à mettre à jour dans `Patient.CheckTagsAsync`.

### 3.2 Travail cumulé du workspace de charge

| Phase | Durée cumulée | Moyenne par patient | Part du travail patient mesuré |
| --- | ---: | ---: | ---: |
| Lecture patient | 2 367 ms | 9,9 ms | 1,2 % |
| Désérialisation patient | 156 586 ms | 652,4 ms | 81,8 % |
| `CheckTagsAsync` | 32 533 ms | 135,6 ms | 17,0 % |

La désérialisation inclut encore :

- la première résolution linéaire de chaque tag ;
- les GUID temporaires créés par les constructeurs ;
- les callbacks et leurs validations de fichiers.

Le temps explicite de `CheckTagsAsync` ne couvre que deux des trois passages de
tags. Le coût du premier passage reste inclus dans la désérialisation. Le gain
potentiel de l'index des tags est donc supérieur au seul chiffre de 32,5 s
cumulées.

### 3.3 I/O et validation

La lecture cumulée des patients ne représente qu'environ 1,2 % du travail
patient mesuré. Remplacer immédiatement Json.NET ou augmenter le nombre de
workers ne cible donc pas le coût principal.

Les 1 201 appels `File.Exists` prennent 1,407 s cumulée, soit environ 1,17 ms
par appel sur cette machine. Ce coût est réel, mais secondaire dans cette
capture locale. Il reste important de séparer la validation du parsing, car il
peut devenir dominant avec des chemins réseau lents ou indisponibles.

Pour les `DataInfo`, la situation est différente :

- 240 fichiers pour seulement 1 421 421 octets ;
- lecture cumulée : 3 818 ms ;
- désérialisation cumulée : 446 ms.

Ce résultat suggère un coût notable par petit fichier, par tâche ou par
bascule vers le thread pool. Il devra être réévalué lors du réglage du
scheduler, après les corrections d'allocations.

## 4. Projet

Le projet mesuré contient :

- 218 patients ;
- 31 448 sites ;
- 62 896 coordonnées ;
- 340 783 valeurs de tags ;
- 1 dataset ;
- 3 visualisations ;
- une archive de 14 146 849 octets.

### 4.1 Deux ouvertures complètes

| Mesure | Ouverture 1 | Ouverture 2 |
| --- | ---: | ---: |
| Temps mural | 7 415 ms | 8 854 ms |
| CPU processus | 68 391 ms | 108 234 ms |
| CPU / temps mural | 9,22 | 12,22 |
| Lecture/extraction archive | 1 038 ms | 999 ms |
| Travail patient cumulé | 99 866 ms | 150 777 ms |
| Recherches de tags | 1 022 349 | 1 022 349 |
| `File.Exists` | 1 090 | 1 090 |
| Temps cumulé `File.Exists` | 112 ms | 351 ms |
| Mémoire managée nette | +138,07 Mio | +23,69 Mio |
| Collections GC | 54 | 37 |

La valeur médiane interpolée des deux temps muraux est **8,134 s**, mais deux
échantillons ne suffisent pas pour déclarer une médiane de référence. L'écart
de 19,4 % entre les deux ouvertures confirme qu'il faudra conserver au moins
trois répétitions après chaque optimisation.

Les recherches de tags valent à nouveau exactement trois fois le nombre de
valeurs. Le travail patient domine très largement. L'extraction de l'archive
coûte environ une seconde, soit 11 à 14 % du temps mural actuel : son
optimisation est utile, mais ne doit pas précéder l'index des tags et la
réduction de la pression GC.

Les liaisons explicitement mesurées sont faibles :

- 661 recherches de références ;
- moins de 10 ms cumulées par ouverture.

Le futur `LoadingContext` reste souhaitable pour le déterminisme et la
correction des références, mais les mesures n'en font pas le premier levier de
performance.

### 4.2 Mémoire lors d'une seconde ouverture

Le second chargement commence avec une mémoire managée nettement plus élevée.
Ce constat n'est pas, à lui seul, la preuve d'une fuite.

`ProjectWorkflowService.LoadProjectAsync` conserve volontairement
`previousProject` dans une variable locale pour pouvoir restaurer le projet
précédent en cas d'erreur ou d'annulation, tandis que le nouveau projet est
publié dans `ApplicationState.LoadedProject` avant son chargement. Pendant
`Project.LoadAsync`, les deux graphes peuvent donc être vivants simultanément.

Cette politique de rollback doit être conservée en tête lors de l'analyse des
pics mémoire. Une recherche de rétention éventuelle nécessiterait des snapshots
pris après le retour complet de `LoadProjectAsync` et après une collecte, pas
seulement à la fin de `Project.LoadAsync`.

## 5. Lecture des manifestes

Trente lectures de manifeste ont été observées :

| Mesure | Valeur |
| --- | ---: |
| Minimum | 2,721 ms |
| Médiane | 7,055 ms |
| Maximum | 22,898 ms |
| Total des 30 lectures | 204,624 ms |

Les ouvertures répétées d'archives existent bien, mais leur coût reste faible
devant un chargement complet. Le manifeste unique de l'étape 6 améliorera
surtout la propreté du pipeline et la montée en charge de la liste des projets.

## 6. Limites révélées par l'instrumentation

Les champs suivants ne doivent pas être utilisés pour comparer les phases
parallèles :

- CPU cumulé par phase ;
- deltas mémoire cumulés par phase ;
- collections GC cumulées par phase.

Chaque scope parallèle observe le processus ou le tas global. Leur somme compte
donc plusieurs fois les mêmes événements. Les valeurs de session restent
utiles, ainsi que les durées murales cumulées, maxima, volumes et compteurs
logiques.

Dans cette capture Mono, `allocatedBytes` vaut zéro et n'apporte pas de mesure
fiable. Il faudra utiliser un mécanisme Unity adapté si une attribution exacte
des allocations par phase devient nécessaire.

Dans cette capture initiale, `Loading.Project.Patients` agrégeait lecture,
désérialisation et `CheckTagsAsync`. L'instrumentation a depuis été séparée en
`Patients.Read`, `Patients.Deserialize` et `Patients.BindTags` afin de comparer
finement l'étape 1 sur les prochaines captures.

## 7. Décisions pour les étapes suivantes

### Étape 1 — terminée

La vue stable et l'index par ID ont été implémentés puis mesurés. Les volumes
et les 1 112 754 demandes logiques restent identiques sur le workspace de
charge. La médiane chaude du chargement baisse de 49,0 %, le CPU de 58,1 % et
`Patients.BindTags` de 86,0 %.

Voir
[`resultats_etape_1_2026-07-23.md`](resultats_etape_1_2026-07-23.md)
pour le protocole complet et les résultats projet.

### Étape 2 — immédiatement après

Supprimer les GUID temporaires créés avant que Json.NET affecte les IDs du
fichier. Le bénéfice attendu doit apparaître principalement dans :

- la phase de désérialisation ;
- le CPU total ;
- les collections GC ;
- la mémoire transitoire.

### Étapes 3 à 6 — ordre confirmé

1. séparer la validation des fichiers, surtout pour maîtriser les chemins
   réseau ;
2. introduire le contexte de liaison pour la correction et le déterminisme ;
3. réduire copies et chaînes JSON ;
4. supprimer les lectures et extractions redondantes de l'archive.

La capture ne justifie pas de placer le streaming JSON ou ZIP avant les deux
corrections P0.

### Parallélisme

La concurrence 20 consomme en moyenne environ 9 à 12 équivalents-cœurs pendant
les gros chargements et produit une variabilité importante. Il ne faut pas
encore la modifier : les mesures 1, 2, 4, 8 et 20 workers ne seront pertinentes
qu'après suppression des allocations principales.

### IL2CPP

Cette baseline ne couvre que l'Editor Mono. Une capture Player IL2CPP avec les
mêmes données reste obligatoire avant de finaliser le scheduler, le registre
de types ou une évolution de format.

## 8. Rapports sources

Les trois rapports complets principaux sont :

```text
loading-database-20260723-182916-389-6.json
loading-project-20260723-183132-415-22.json
loading-project-20260723-183152-185-38.json
```

Les rapports ne contiennent ni chemin métier, ni identifiant patient, ni
valeur de tag.
