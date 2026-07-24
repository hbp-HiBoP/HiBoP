# Étape 3 — Validation explicite des références de fichiers

Date : 23 juillet 2026

## Résultat

Les vérifications de fichiers des meshes et IRM ne sont plus exécutées par les
callbacks Json.NET.

Le chargement suit maintenant cette séquence :

```text
lecture -> désérialisation -> liaison explicite des références
        -> validation des fichiers -> publication
```

La base et le projet attendent toujours la validation avant de publier la
liste des patients ou de terminer le chargement. La sémantique visible de
`WasUsable` reste donc inchangée.

Les mesures sont détaillées dans
[`resultats_etape_3_2026-07-23.md`](resultats_etape_3_2026-07-23.md).

## Changements

### Callbacks JSON

`BaseMesh.OnDeserialized` et `MRI.OnDeserialized` se limitent maintenant à :

- normaliser les séparateurs du chemin sauvegardé ;
- appeler le callback de base pour les invariants d'identité.

Ils n'appellent plus `RecalculateUsable` ou `RecalculateIsUsable`. Les
callbacks de `SingleMesh` et `LeftRightMesh` normalisent leurs chemins puis
délèguent au callback de `BaseMesh`, sans I/O.

### `AssetReferenceValidator`

Le nouveau validateur :

- reçoit le graphe complet des patients après parsing ;
- collecte les chemins principaux de `SingleMesh`, `LeftRightMesh` et `MRI` ;
- développe chaque chemin sauvegardé une seule fois, y compris les aliases et
  le préfixe projet `.` ;
- déduplique les chemins complets avec une comparaison adaptée à la plateforme ;
- vérifie les chemins sur un nombre borné de workers du thread pool ;
- accepte un `CancellationToken` ;
- n'applique les résultats qu'après la réussite complète de la validation ;
- remplit directement `WasUsable`.

La base utilise `CancellationToken.None`, car son API historique de chargement
n'expose pas de token. Le projet transmet son token existant.

### Publication

Dans `GlobalDatabase`, les patients restent dans une liste locale pendant la
validation. `m_Patients` n'est remplacé qu'une fois celle-ci terminée.

Dans `Project`, le graphe complet reste également local jusqu'à la liaison
explicite et la validation. Il est ensuite publié en une seule fois.

### Progression utilisateur

Depuis l'étape 4, la validation est une sous-phase dédiée après la liaison du
graphe complet. Elle affiche `Validating patient file references` et progresse
selon le nombre de chemins complets uniques validés. Son poids est proportionnel
au nombre de patients, séparément de la lecture et de la désérialisation. Une
validation sans chemin atteint directement la fin de la sous-phase.

## Compatibilité

### Format JSON et types

Le format n'est pas modifié. Aucun champ de cache ou résultat de validation
n'est sérialisé.

Le validateur utilise des tests de types explicites plutôt que de la réflexion.
Les types concrets actuels sont :

- `SingleMesh` ;
- `LeftRightMesh` ;
- `MRI`.

Les autres types customs, rétrocompatibles et les valeurs `$type` continuent
d'être traités par Json.NET et le binder existant.

### Règles d'utilisabilité

Les règles historiques sont conservées :

- nom non vide ;
- présence du ou des fichiers principaux ;
- comparaison d'extension identique à l'ancien getter ;
- `LeftRightMesh` utilisable seulement si les deux hémisphères sont valides.

La validation ne précharge pas `HasMarsAtlas` ou `HasTransformation`, car les
callbacks historiques ne les utilisaient pas pour calculer `WasUsable`.

La comparaison d'extension conserve volontairement son comportement exact,
y compris la sensibilité à la casse et la limitation historique de
`FileInfo.Extension` pour `.nii.gz`. Corriger cette règle fonctionnelle doit
faire l'objet d'un changement séparé.

Les getters interactifs `IsUsable`, `HasMesh`, `HasMRI`,
`HasTransformation` et `HasMarsAtlas` restent dynamiques. Ils peuvent donc
revérifier le système de fichiers lorsqu'une interface les consulte après le
chargement.

## Concurrence et annulation

Le validateur démarre au maximum le nombre de workers demandé par le pipeline :
20 quand le multithreading est actif, 1 sinon. Cette concurrence est séparée
de celle du parsing : les deux phases ne se chevauchent pas.

Chaque worker :

1. vérifie le token ;
2. réserve atomiquement le prochain chemin ;
3. appelle `File.Exists` via l'instrumentation ;
4. recommence jusqu'à épuisement.

Si une annulation survient, l'attente échoue avec
`OperationCanceledException` et aucun `WasUsable` partiel n'est publié.

## IL2CPP

Le code de production n'ajoute ni réflexion, ni génération dynamique, ni
`FormatterServices`.

Il repose sur :

- des collections génériques fermées ;
- `Interlocked` ;
- `CancellationToken` ;
- `UniTask.SwitchToThreadPool` et `UniTask.WhenAll` ;
- des tests de types C# explicites.

Ces mécanismes sont compatibles avec AOT/IL2CPP. Comme pour les étapes
précédentes, la campagne actuelle est exécutée sous Editor Mono ; le player
IL2CPP global reste le verrou final du plan.

## Validation automatisée

`AssetReferenceValidatorTests` couvre :

- chemins locaux existants et absents ;
- extensions valides et invalides ;
- égalité entre le résultat explicite et `IsUsable` ;
- zéro appel fichier pendant la désérialisation ;
- déduplication de deux références identiques ;
- aliases de forme Windows et Linux ;
- chemin relatif d'un projet ;
- partage réseau simulé ;
- concurrence maximale de deux workers ;
- annulation sans publication partielle.

Les trois tests spécifiques passent. Avec les suites projet, fixtures natives,
types historiques, contrats et sérialisation, le total ciblé est de
**104 / 104**.

## Limites observées

Le jeu réel contient 1 201 chemins principaux pour la base et 1 090 pour le
projet. Ces chemins sont déjà uniques après développement des aliases : la
déduplication n'abaisse donc pas le nombre de sondes sur cette donnée précise.
Elle reste utile lorsque plusieurs objets référencent réellement le même
fichier, ce que couvre le test dédié.

Cette étape ne démontre pas de gain mural global sur la machine de référence.
Elle rend en revanche la validation mesurable, annulable et indépendante du
parseur. Le recalibrage de la concurrence des phases devenues essentiellement
CPU devra être étudié séparément.
