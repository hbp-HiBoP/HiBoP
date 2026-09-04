# ADR P02 — contrats purs, identités, scopes et conflits

- **Statut :** ACCEPTED — GATE P02-A–D RESOLVED
- **Date :** 2026-09-01
- **Accepté par :** propriétaire du dépôt HiBoP via l'ordre d'exécution de P02
- **Baseline inspectée :** branche `feature/xr`, commit `7363ee729015590955194e0e545350becad16bd1`
- **Décisions héritées :** D03, D04, D12, D17 et D18
- **Catalogue normatif :** [P02-scope-catalog.md](../contracts/P02-scope-catalog.md)

## Contexte observé

- Le package `com.crnl.hibop.contracts` existe déjà comme squelette UPM consommé par Desktop et XR depuis une source unique. Son asmdef Runtime n'a aucune référence et porte `noEngineReferences: true`.
- Les IDs Desktop ne sont pas utilisables tels quels sur le wire. Certains sont des chaînes persistantes ; `SiteInformation.FullID` concatène un ID patient et un nom de site ; `SelectedSiteID` et plusieurs colonnes utilisent des indices d'ordre.
- `VisualizationConfiguration` persiste les réglages de surface, transparence, sites, coupes et ROI. Les configurations de colonnes persistent alpha, seuils et paramètres par site. Les objets de scène ajoutent sélection, timeline, résultats calculés et états dérivés.
- Un `SiteState` est instancié par colonne. Une identité de site ne suffit donc pas à identifier le scope de ses propriétés ; le scope et l'entité métier doivent rester deux identités distinctes.
- Les poses, échelles et dispositions XR n'existent pas dans les modèles Desktop et restent autoritaires sur Quest, conformément à la spécification produit.

## P02-A — représentation et stabilité des IDs

### Décision

Tous les IDs de contrats sont des valeurs opaques non nulles de **128 bits**, représentées en mémoire par deux entiers non signés de 64 bits et sur le wire par exactement 16 octets en ordre réseau. Leur forme texte canonique de diagnostic est 32 chiffres hexadécimaux minuscules, sans séparateur.

Le package Contracts valide, compare, hashe, parse et écrit ces valeurs. Il ne génère pas d'identités : la génération appartient à l'adaptateur qui possède l'autorité.

- `sessionId` et l'epoch sont générés par l'hôte avec un CSPRNG au démarrage de chaque session Desktop.
- Les IDs d'entités exposées au Quest sont des pseudonymes CSPRNG propres à l'epoch. Ils sont stables pendant cet epoch seulement et ne dérivent ni d'un nom patient, ni d'un chemin, ni d'un index, ni d'un ID Desktop persisté.
- `commandId` et `interactionId` sont générés par l'émetteur avec un CSPRNG et ne sont valides que dans leur epoch.
- Un `scopeId` est distinct de l'ID de l'entité qu'il décrit. Cette séparation est obligatoire pour les états par colonne d'un même site.
- L'identité immuable d'un asset reste son SHA-256 complet et n'est pas remplacée par un ID 128 bits. Les indices de buffers ne sont valides qu'avec ce hash.
- La valeur tout-zéro est invalide pour toute identité publique. Le parse et la construction la rejettent.

Les noms des patients, sites, visualisations et colonnes ne participent jamais à `Equals`, `GetHashCode`, `ToString` ou à un helper de log.

### Conséquences

La reconnexion dans le même epoch réutilise les mêmes pseudonymes. Un nouvel epoch invalide toutes les identités de session et impose un remapping depuis l'autorité Desktop. Aucun index de liste observé dans HiBoP ne devient stable par convention.

## P02-B — scopes et propriété V1

### Décision

Le catalogue [P02-scope-catalog.md](../contracts/P02-scope-catalog.md) est la baseline normative V1. Les scopes initiaux sont `Project`, `Visualization`, `Column`, `BrainInstance`, `Site`, `Cut`, `Roi` et `Timeline`.

Chaque scope possède un `scopeId` opaque, un propriétaire unique et une révision monotone dans l'epoch :

- Desktop possède les scopes Project, Visualization, Column, Site, Cut, Roi et Timeline ;
- Quest possède BrainInstance pour pose, échelle, visibilité locale et disposition ;
- le miroir Quest des scopes Desktop est une copie révisionnée, jamais une seconde autorité ;
- les valeurs calculées (`masked`, `outOfRoi`, buffers, textures, résultats) sont des sorties dérivées citées par leurs révisions d'entrée, pas des propriétés commandables ;
- les informations humaines détaillées sont des réponses transitoires redacted/non persistées, pas des propriétés de snapshot.

Une commande visant une propriété Desktop utilise le `scopeId` de l'état, jamais un ID d'entité supposé équivalent. P02 ne change aucun propriétaire ni comportement Desktop existant.

## P02-C — optionalité et versionnement

### Décision

La V1 applique les règles suivantes :

1. Tout champ est requis par défaut et doit être fourni au constructeur.
2. Une absence s'exprime par `Optional<T>` ; `null`, valeur sentinelle, chaîne vide et ID zéro ne codent jamais l'absence.
3. Une collection requise est non nulle ; l'absence d'élément est une collection vide. Les contrats prennent une copie défensive et n'exposent pas de tableau mutable.
4. Chaque union ou enum réserve `0` à `Unknown`/`None`. Les valeurs numériques publiées ne sont jamais réutilisées.
5. Les changements additifs sont optionnels et nécessitent une capability négociée. Modifier la signification, la cardinalité ou le caractère requis d'un champ exige une nouvelle version majeure de schéma.
6. Le protocole et le hash de schéma restent la source de compatibilité D18. La version SemVer du package ou des applications n'est pas une preuve de compatibilité wire.
7. Un producteur ne doit pas envoyer un champ optionnel sans capability correspondante ; un consommateur ne doit pas inventer une valeur canonique quand le champ est absent.

Les contrats ne portent aucun attribut de sérialiseur. Le codec et les règles de champs inconnus appartiennent à `com.crnl.hibop.protocol` (P06), mais doivent préserver cette sémantique.

## P02-D — conflit `baseScopeRevision`

### Décision

Le host traite une commande dans cet ordre atomique :

1. valider session/epoch et forme de la commande ;
2. rechercher `commandId` dans le journal d'idempotence borné de l'epoch ;
3. si trouvé, retourner le même outcome logique enregistré, même si la révision courante a avancé ;
4. résoudre le scope ;
5. comparer `baseScopeRevision` à la révision courante du scope ;
6. exécuter seulement si elles sont égales.

Pour une nouvelle commande dont la base diffère, le host :

- rejette sans mutation, sans delta et sans incrément de révision ;
- renvoie `STATE_CONFLICT`, `retryable = true`, la révision globale courante et la révision courante du scope ;
- n'applique aucun merge, last-write-wins ou rebase implicite ;
- enregistre ce rejet sous le `commandId` afin qu'un duplicata produise le même outcome.

Le client doit d'abord réconcilier deltas ou snapshot, puis créer un **nouveau** `commandId` avec la nouvelle base s'il souhaite réessayer. Réémettre le même ID ne réévalue jamais la commande.

Les commandes coalescées suivent la même règle. `interactionId/sequence` choisit la dernière intention à calculer, mais ne contourne ni l'idempotence ni le contrôle de révision.

## Gate d'implémentation

P02-A–D étant enregistrées et le catalogue ne contenant aucun propriétaire ambigu, l'implémentation des types publics est autorisée. Elle doit rester dans `com.crnl.hibop.contracts`, sans Unity, IO, UI, native, transport ou sérialiseur concret.

## Réouverture

Réouvrir cet ADR si une identité doit survivre à un epoch sur le wire, si un scope acquiert plusieurs autorités, si une commande doit fusionner automatiquement un conflit ou si le codec retenu ne peut pas représenter l'optionalité sans sentinelle.

### Addendum P09 — mapping entité/scope

P09 a rouvert le catalogue de façon additive le 2026-09-03. Les clés V1 `VisualizationEntity`, `ColumnEntity` et `ColumnVisualization` rendent explicites les mappings déjà exigés conceptuellement par P02-A et nécessaires aux bindings `BrainInstance`. Elles utilisent les `ContractValue` de type `Id`, ne modifient aucun propriétaire et deviennent requises pour négocier la capability multi-instance P09 dans une paire Desktop/XR coordonnée.
