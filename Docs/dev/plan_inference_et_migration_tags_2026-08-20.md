# Plan d'implémentation de l'inférence et de la migration des tags

Date : 2026-08-20

## 1. Objectif

Uniformiser l'inférence des tags créés par les imports de bases de données et
sécuriser les changements manuels de type sans ajouter de fenêtre de sélection
pendant une mise à jour de base.

Le comportement retenu est le suivant :

- un tag globalement nouveau reçoit automatiquement un type inféré à partir de
  l'état final observable du workspace mis à jour ;
- le type d'un tag existant n'est jamais modifié automatiquement par un import ;
- un changement de type est une action manuelle et explicitement confirmée ;
- les données actuellement chargées sont migrées immédiatement ;
- les projets et workspaces fermés sont migrés lors de leur prochaine ouverture ;
- les tags restent globaux à la machine, comme les alias et les protocoles ;
- les enums stockent désormais leur index et leur valeur textuelle ;
- aucune version de schéma ni historique des anciennes définitions n'est ajouté.

## 2. Décisions produit

### 2.1 Pas de fenêtre de revue pendant l'import

La mise à jour d'une base ne présente pas une liste de types à valider. Elle
effectue un pré-scan invisible, infère les nouveaux tags, puis inclut le résultat
dans le rapport final.

Le rapport doit distinguer :

- les tags créés et leur type inféré ;
- les nouvelles options ajoutées aux `EnumTag` existants ;
- les valeurs incompatibles avec un tag existant ;
- les valeurs ignorées par la politique de parsing.

### 2.2 Inférence uniquement à la création

Le type est inféré uniquement lorsque le tag n'existe pas encore dans la
collection globale. Une mise à jour ultérieure ne peut pas promouvoir ou
dégrader automatiquement ce type.

Pour un tag existant :

- les valeurs compatibles sont importées ;
- les valeurs incompatibles ne sont pas converties vers une valeur par défaut ;
- les cellules incompatibles ne sont pas intégrées dans le cache HiBoP ;
- chaque incompatibilité apparaît dans le rapport avec sa source, son
  propriétaire et sa valeur brute ;
- la source externe reste intacte et pourra être réimportée après une correction
  manuelle du schéma ou des données.

### 2.3 Changement de type explicitement opt-in

L'éditeur manuel de tags est l'unique point d'entrée pour changer le type d'un
tag existant. La validation doit afficher l'impact connu avant tout commit.

### 2.4 Pas de version ni d'historique de schéma

Le chargement différé détecte une migration à partir :

- du type CLR sérialisé du `BaseTagValue` ;
- du type actuel de la définition globale trouvée par `Tag.ID` ;
- de la présence ou de l'absence de la valeur textuelle d'un enum ;
- de la cohérence du couple `(index, value)` d'un enum.

Cette simplification implique une limite volontaire pour les anciens enums,
décrite dans la section 5.5.

## 3. Problèmes actuels à corriger en priorité

La nouvelle fonctionnalité ne doit pas être construite sur le chemin de
migration actuel sans corriger les défauts suivants.

### 3.1 Confusion entre tags valides et tags à migrer

`Patient.CheckTagsAsync` reçoit actuellement les seuls IDs modifiés depuis
`TagCollectionModifier`, puis supprime toute valeur dont l'ID ne figure pas dans
cette sélection.

Il faut séparer deux ensembles :

```text
validTagIds       = tous les tags globaux encore existants
tagIdsToMigrate  = uniquement les tags dont la définition a changé
```

Une migration ciblée ne doit jamais supprimer les valeurs de tags non concernés.

### 3.2 Perte du propriétaire patient/site

Le code actuel fusionne les valeurs de patient et de site dans une même liste,
puis réinsère les valeurs converties dans `Patient.Tags`.

Chaque valeur à migrer doit être représentée avec son propriétaire exact :

```text
(ownerCollection, index, tagValue)
```

Le remplacement se fait dans la collection et à l'emplacement d'origine.

### 3.3 Références runtime non canoniques

Le remplacement d'une définition conserve déjà plus ou moins l'ID grâce aux
méthodes `Copy`, mais les objets chargés peuvent continuer à référencer
l'ancienne instance.

Après une migration, chaque référence doit pointer vers l'unique définition
canonique présente dans `TagCollection`, retrouvée par ID.

### 3.4 Conversions partielles et silencieuses

Les différents `Copy` de `IntTagValue`, `BoolTagValue`, `FloatTagValue` et
`EnumTagValue` ne constituent pas un convertisseur générique fiable. Certaines
conversions produisent actuellement `0`, `false` ou l'option enum d'index `0`
en cas d'échec.

Toute conversion doit désormais retourner explicitement un succès ou un échec.

### 3.5 Filtres non migrés

Les presets de filtres référencent bien les tags par ID, mais leur
`TagFilterValue` peut conserver l'ancien type. La migration doit traiter les
conditions patient, site et multi-site en même temps que les valeurs de données.

## 4. Nouveau modèle des valeurs enum

### 4.1 Représentation

Introduire un type sérialisable réutilisable, par exemple :

```csharp
public readonly struct EnumValueReference
{
    public int Index { get; }
    public string Value { get; }
}
```

`EnumTagValue` et `EnumTagFilterValue` doivent tous les deux utiliser cette
représentation sémantique.

Pour simplifier la rétrocompatibilité JSON, le champ historique `"Value"`
peut rester l'index entier et un nouveau champ textuel être ajouté :

```text
Value       : int     // index historique
StringValue : string  // valeur sémantique ajoutée
```

L'API publique peut exposer ces deux champs comme un `EnumValueReference` sans
imposer immédiatement une rupture du format JSON historique.

### 4.2 Invariant

Après résolution d'une valeur moderne :

```text
0 <= Index < Tag.Values.Length
Tag.Values[Index] == StringValue
```

Règles d'autorité :

1. `StringValue` est la valeur sémantique de référence.
2. `Index` est un cache compatible avec le code et le format existants.
3. Si les deux divergent, rechercher `StringValue` dans `Tag.Values` et réparer
   l'index.
4. Une erreur ne doit jamais être clampée vers l'index `0`.
5. Les labels enum doivent être uniques avec une comparaison ordinale exacte.

### 4.3 Valeur moderne absente de la définition

Si `StringValue` n'existe plus dans un `EnumTag` courant :

- pendant un import, l'ajouter à la fin de `Tag.Values` ;
- pendant une migration de projet/workspace, inclure cet ajout append-only dans
  le plan présenté à l'utilisateur ;
- ne jamais sélectionner automatiquement une autre option.

### 4.4 Enum évolutif

Lorsqu'une nouvelle valeur distincte est rencontrée pour un `EnumTag` existant :

1. appliquer la politique des valeurs ignorées ;
2. comparer exactement avec les options existantes ;
3. ajouter les options manquantes à la fin, dans un ordre déterministe ;
4. créer les couples `(index, value)` correspondants ;
5. mentionner les ajouts dans le rapport d'update.

L'ajout append-only conserve le sens des anciens indices.

### 4.5 Compatibilité des anciens enums index-only

Une valeur legacy ne contient pas de `StringValue`.

Si le tag actuel est encore un `EnumTag` :

- un index valide permet de reconstruire `StringValue` depuis la liste actuelle ;
- un warning groupé indique que cette résolution peut être fausse si les options
  ont été réordonnées ou modifiées par le passé ;
- un index hors limites produit un conflit explicite.

Si le tag actuel n'est plus un `EnumTag`, l'ancien label est irrécupérable sans
l'ancienne définition. Comme aucun historique n'est conservé :

- la conversion doit être déclarée impossible ;
- aucune valeur de remplacement ne doit être inventée ;
- le dialogue peut proposer d'annuler le chargement ou de supprimer explicitement
  les valeurs concernées ;
- lors d'un changement manuel `EnumTag -> autre type`, un warning doit annoncer
  que les anciens projets/workspaces contenant des enums index-only pourront ne
  pas être migrables.

Cette limitation est acceptée pour la rétrocompatibilité des enums existants,
actuellement peu utilisés.

### 4.6 Réordonnement, renommage et suppression

Avec le nouveau couple, un réordonnement peut être réparé en recherchant la
valeur textuelle. Les anciennes valeurs index-only restent cependant ambiguës.

Dans cette passe :

- les ajouts automatiques sont exclusivement append-only ;
- un réordonnement manuel affiche le warning de rétrocompatibilité ;
- un renommage ou une suppression affiche un warning plus fort, car les futurs
  chargements peuvent produire un conflit ;
- aucune correction silencieuse n'est autorisée.

## 5. Politique de parsing et inférence

### 5.1 Préférences utilisateur

Ajouter `TagImportPreferences` dans `UserPreferences.Data` avec :

- `TrueValues` ;
- `FalseValues` ;
- `IgnoredValues`.

Valeurs par défaut recommandées :

```text
TrueValues    = true, yes
FalseValues   = false, no
IgnoredValues = n/a, na, nan, null, none, -, not found
```

Les comparaisons sont effectuées après `Trim`, sans tenir compte de la casse.
Les listes doivent être dédupliquées et ne doivent pas se chevaucher.

`0/1` ne font pas partie des valeurs booléennes par défaut, afin de ne pas
transformer automatiquement des colonnes numériques binaires en booléens.

### 5.2 Service pur

Créer une politique immutable `TagParsingPolicy`, construite depuis les
préférences, puis passée explicitement aux services d'inférence et de conversion.

Les importeurs et les classes `Tag` ne doivent pas consulter directement le
singleton des préférences. Cela garantit des imports et des tests déterministes.

### 5.3 Ordre d'inférence

Après retrait des valeurs ignorées :

```text
toutes booléennes  -> BoolTag
toutes entières    -> IntTag
toutes numériques  -> FloatTag
sinon              -> StringTag
```

Une colonne ne contenant que des zéros est un `IntTag`.

`EnumTag` n'est jamais inféré automatiquement. Sa création reste une décision
manuelle ; ses options sont alors préremplies depuis les valeurs distinctes
connues dans la base et le projet actuellement chargés.

### 5.4 Couverture du pré-scan

Avant de créer un tag, agréger les observations de toutes les références de
métadonnées configurées dans le workspace, y compris celles qui ne sont pas
explicitement sélectionnées pour cette update. La clé d'un tag nouveau est au
minimum :

```text
scope (Patient/Site) + nom normalisé
```

Un tag patient et un tag site homonymes ne doivent pas être fusionnés. Un
`GeneralTag` existant reste un cas explicite de résolution.

Le pré-scan doit être terminé avant la création du premier tag afin que l'ordre
des fichiers et des références n'influence pas le type choisi. Pour éviter de
relire systématiquement toutes les sources, une optimisation ultérieure pourra
maintenir un index d'observations par workspace et `DatabaseReference`, mais cet
index ne devra pas réduire la couverture fonctionnelle.

### 5.5 Importeurs concernés

Tous les chemins créant des tags doivent utiliser le même moteur :

- `participants.tsv` BIDS ;
- fichiers d'électrodes/sites BIDS ;
- bases Tags CSV patient et site ;
- bases Tags Excel patient et site ;
- autres imports Intranat/PTS créant actuellement directement des `StringTag`.

Aucun parseur ne doit appeler directement `AddPatientTag`, `AddSiteTag` ou
`Save` pendant son scan.

## 6. Convertisseur central de valeurs

### 6.1 Responsabilité

Créer un service unique, par exemple :

```csharp
TagValueConversionResult TryConvert(
    BaseTagValue source,
    BaseTag target,
    TagParsingPolicy policy);
```

Le service doit :

- extraire une représentation sémantique de la valeur source ;
- construire le bon sous-type de `BaseTagValue` ;
- conserver l'ID de la valeur source ;
- lier la nouvelle valeur à la définition canonique cible ;
- ne pas modifier la source ;
- retourner un échec détaillé au lieu d'une valeur par défaut.

Le changement de définition du tag lui-même doit passer par une factory dédiée
qui conserve explicitement son ID, son nom et sa catégorie. Il ne doit pas
reposer sur une succession générique de `Remove`, `Add` et `Copy`.

### 6.2 Règles minimales de conversion

| Source | Cible | Règle |
|---|---|---|
| String | Bool/Int/Float | Parser avec `TagParsingPolicy` |
| Bool/Int/Float | String | Représentation canonique non ambiguë |
| Int | Float | Conversion exacte |
| Float | Int | Succès seulement si la valeur est finie et entière |
| Bool | Int/Float | `false = 0`, `true = 1` |
| Enum moderne | autre type | Utiliser `StringValue` |
| Enum legacy | Enum courant | Résoudre l'index courant avec warning |
| Enum legacy | type non-enum | Échec explicite sans historique |
| Toute source | Enum | Correspondance exacte ou ajout append-only confirmé |
| Toute source | Empty | Conversion destructive explicitement signalée |

Les contraintes `Min`, `Max` et `Clamped` sont appliquées seulement après que la
conversion sémantique a réussi. Un clamp qui change la valeur doit apparaître
comme conversion avec perte dans le plan de migration.

### 6.3 Filtres

Le même service conceptuel doit convertir les `TagFilterValue` :

- un filtre texte exact peut devenir un filtre booléen, numérique ou enum si sa
  valeur est convertible ;
- un filtre texte `contains` vers un type non textuel est ambigu et bloque le
  commit tant que l'utilisateur n'a pas explicitement décidé de le retirer ;
- `EnumTagFilterValue` utilise le même couple `(index, value)` et les mêmes règles
  de réparation que `EnumTagValue`.

## 7. Changement manuel de type

### 7.1 Construction du plan

Lorsque le type est modifié dans l'éditeur :

1. créer la nouvelle définition avec le même `Tag.ID` ;
2. collecter les valeurs correspondantes dans la base chargée ;
3. collecter les valeurs correspondantes dans le projet ouvert ;
4. collecter les filtres globaux correspondants ;
5. convertir le tout sur des clones ;
6. produire les comptes de succès, conversions avec perte et échecs ;
7. ne rien publier à ce stade.

### 7.2 Confirmation utilisateur

La dialogbox de confirmation affiche :

- le nom, l'ancien type et le nouveau type ;
- le nombre de valeurs patient et site ;
- le nombre de filtres ;
- les conversions avec perte ;
- les échecs ;
- l'impact global sur les projets et workspaces qui seront ouverts plus tard ;
- le warning spécifique aux anciens enums index-only si nécessaire.

Le changement de type reste l'action initiale de l'utilisateur : la dialogbox ne
propose pas un autre type.

### 7.3 Validation et commit

Le commit doit garantir :

- même ID de tag avant et après ;
- même catégorie General/Patient/Site ;
- mêmes IDs de valeurs et de filtres ;
- même propriétaire patient/site ;
- références runtime rebondées vers la définition canonique ;
- aucune mutation partielle si une conversion ou une écriture échoue.

Ordre recommandé :

1. valider entièrement les clones ;
2. préparer les écritures temporaires ;
3. publier la définition globale, les valeurs de la base et les filtres ;
4. sauvegarder la base chargée et les données globales ;
5. appliquer les valeurs migrées au projet en mémoire ;
6. marquer le projet comme modifié sans le sauvegarder automatiquement.

Le projet doit afficher qu'une sauvegarde est nécessaire pour persister sa
migration. En cas d'échec de sauvegarde des données globales ou de la base, le
commit mémoire doit être annulé.

## 8. Migration différée à l'ouverture

### 8.1 Détection

Le chargement d'un projet ou workspace doit désérialiser les `BaseTagValue` sans
les rebinder immédiatement vers la définition globale.

Pour chaque valeur :

1. conserver le `TagID` sérialisé et la valeur brute ;
2. retrouver la définition canonique par ID ;
3. comparer le type sérialisé au type actuel ;
4. vérifier les couples enum modernes ;
5. identifier les enums legacy sans `StringValue` ;
6. construire un `TagMigrationPlan` sur le graphe non encore publié.

Cette conversion doit intervenir avant `LoadingContext.ResolvePatientTags`, afin
d'éviter les casts génériques invalides après le rebinding.

### 8.2 Information et récupération non bloquante

Le chargement applique automatiquement le type global courant. Si le plan
contient une migration, le workflow UI affiche après publication :

- les tags concernés ;
- les nombres de valeurs patient/site et de filtres ;
- les conversions prévues ;
- les warnings enum legacy ;
- les échecs éventuels ;
- le rappel qu'une sauvegarde sera nécessaire.

Le Core construit et applique le plan ; il ne connaît pas la dialogbox. La
fenêtre est informative et ne propose aucun changement de type.

Si des valeurs sont impossibles à convertir :

- elles sont exclues du graphe actif et conservées dans une quarantaine
  sérialisée avec leur owner, TagID, ValueID, type, valeur brute et raison ;
- l'artefact s'ouvre et la fenêtre détaille la récupération ;
- aucune modification de type n'est proposée.

Un filtre courant global invalide est réinitialisé et archivé. Un preset nommé
invalide est désactivé en bloc afin de ne pas modifier silencieusement sa logique
`All`/`Any`. Les filtres globaux ne bloquent jamais l'ouverture d'un workspace ou
d'un projet.

Les références structurelles ou entrées JSON irrécupérables ouvrent le scope en
mode recovery read-only : les objets concernés sont quarantinés, les objets
valides restent utilisables, et le fichier source demeure inchangé.

### 8.3 Persistance

Après migration :

- conserver tous les IDs ;
- publier le graphe migré ;
- marquer le projet ou workspace comme modifié ;
- persister les changements au prochain `Save` normal ;
- ne plus afficher la migration après sauvegarde et rechargement.

Les mappings booléens utilisés sont ceux des préférences présentes au moment du
chargement. Le dialogue doit afficher le résultat calculé avant confirmation.

### 8.4 Dette produit : comportement post-quarantaine à définir

La quarantaine introduite dans cette passe garantit qu'une incompatibilité ne
bloque plus l'ouverture et qu'aucune valeur de remplacement n'est inventée. Elle
ne constitue cependant pas encore un workflow complet de réparation. Le cycle
de vie post-quarantaine doit faire l'objet d'une décision produit et d'une phase
d'implémentation dédiée.

Trois catégories d'entités sont actuellement concernées.

#### Valeurs de tags patient et site

Une `BaseTagValue` est mise en quarantaine lorsque sa définition globale est
absente, lorsque son type n'est plus compatible ou lorsque sa conversion vers le
type global échoue. Elle est retirée de `Patient.Tags` ou `Site.Tags` et n'est
donc plus visible, filtrable ni utilisée par les traitements. Une
`TagValueRecoveryEntry` conserve sur son propriétaire :

- le `TagID` ;
- le `ValueID` ;
- le type CLR sérialisé ;
- la valeur JSON complète ;
- la raison de la quarantaine.

Si l'utilisateur sauvegarde, ces entrées sont persistées dans l'artefact. Sans
sauvegarde, le fichier original demeure intact et la même récupération sera
reproposée à l'ouverture suivante. La réapparition ultérieure de la définition
du tag ne restaure pas encore automatiquement les valeurs et aucune UI ne permet
actuellement de les remapper ou de les réinjecter.

#### Presets de filtres globaux

Un filtre courant invalide est remplacé par un filtre courant vide et une copie
du preset complet est ajoutée aux presets désactivés. Un preset nommé invalide
est retiré de la liste active et conservé intégralement dans cette même
collection. Le preset entier est archivé afin de ne pas modifier silencieusement
la logique d'une composition `All` ou `Any`.

Cette récupération globale est sauvegardée avec une copie
`.pre-recovery.bak` du fichier original. Les presets désactivés ne participent
plus au filtrage. Il n'existe pas encore d'écran permettant de les inspecter, de
remapper leurs tags ou valeurs, puis de les réactiver.

#### Objets et fichiers structurels

Peuvent être exclus du graphe actif : protocoles, références de base, patients,
`DataInfo`, datasets, groupes et visualisations. Cela couvre notamment les
fichiers ou entrées non désérialisables, les IDs absents ou dupliqués, les
protocoles ayant des IDs de blocs invalides et les références obligatoires non
résolues.

Un objet correctement désérialisé est conservé en mémoire dans le rapport de
récupération avec son ID et les raisons de son exclusion. Pour une entrée ou un
fichier illisible, le rapport conserve son chemin ou son nom et l'erreur, tandis
que la source reste sur disque. Dès qu'une telle récupération structurelle est
présente, le projet ou workspace passe en lecture seule : sauvegarde, update et
publication des références sont bloquées afin de ne pas remplacer la source par
le seul sous-ensemble actif. Ce rapport est actuellement un état runtime, pas un
sidecar de réparation persistant.

Un `Tags.json` ou fichier de presets global entièrement invalide constitue un
cas voisin mais distinct : le fichier original est préservé, une collection de
secours permet à l'application de rester disponible, et les écritures qui
pourraient écraser le fichier invalide sont interdites. Ce cas ne crée pas une
entrée de quarantaine par objet lorsque la désérialisation globale est
impossible.

Les décisions restantes sont au minimum :

- définir les états explicites `quarantined`, `restored`, `remapped` et
  `discarded`, ainsi que leurs transitions ;
- fournir un écran central listant les éléments concernés, leur provenance et
  leur raison ;
- permettre une restauration automatique lorsque l'identité et le type sont à
  nouveau compatibles ;
- permettre un remapping explicite vers une autre définition, avec prévisualisation
  et conservation des IDs lorsque cela est valide ;
- définir la suppression définitive, sa confirmation et la durée de rétention
  des backups ;
- définir si les rapports structurels doivent être persistés dans un sidecar et
  comment réintégrer un objet réparé ;
- clarifier dans l'UI l'effet exact de `Save` : persister la quarantaine pour les
  valeurs de tags, mais refuser toute sauvegarde en récupération structurelle ;
- ajouter des tests de round-trip quarantaine -> restauration et de non-perte
  après plusieurs ouvertures et sauvegardes.

Le cas réel `ebrains.hibop` illustre l'importance de cette dette : 26 664 valeurs
de tags sont récupérables mais actuellement exclues du graphe actif parce que
leurs définitions globales sont absentes. Tant que la stratégie de restauration
ou de reconstruction de ces définitions n'est pas arrêtée, cet artefact ne doit
pas être sauvegardé après migration.

## 9. Flux transactionnel d'une mise à jour de base

```text
Scan brut de toutes les références de métadonnées du workspace
    -> agrégation des observations
    -> inférence des seuls tags nouveaux
    -> extension append-only des enums existants
    -> conversion des valeurs avec les tags stables
    -> collecte des incompatibilités
    -> validation du draft
    -> commit batch tags + base
    -> rapport d'update
```

Le scan ne modifie jamais `PersistentDataManager.Tags`. Les tags et les valeurs
sont publiés uniquement après validation complète du draft.

## 10. Plan d'implémentation

### Phase 0 - Tests de caractérisation et correctifs destructifs

- ajouter un test démontrant qu'un tag modifié ne supprime pas les autres ;
- ajouter un test garantissant qu'une valeur de site reste dans `Site.Tags` ;
- caractériser le rebinding après remplacement d'une définition ;
- séparer IDs valides et IDs à migrer ;
- remplacer le parcours fusionné patient/site ;
- supprimer tout fallback silencieux vers `0`, `false` ou l'option enum `0`.

Critère de sortie : une migration ciblée ne modifie que le tag demandé et ne
peut pas perdre silencieusement une valeur.

### Phase 1 - Couple enum et rétrocompatibilité simple

- introduire `EnumValueReference` ou l'équivalent sérialisé ;
- ajouter `StringValue` à `EnumTagValue` ;
- appliquer le même modèle à `EnumTagFilterValue` ;
- corriger `EnumTag.Clamp` ;
- implémenter la réparation `StringValue -> Index` ;
- implémenter le chargement legacy index-only avec warning groupé ;
- rendre les ajouts automatiques append-only.

Critère de sortie : réordonner un enum moderne ne change plus le sens de ses
valeurs ou de ses filtres ; un enum legacy ne produit jamais une valeur inventée.

### Phase 2 - Convertisseur central et migration transactionnelle

- créer `TagParsingPolicy` ;
- créer `TagValueConversionService` ;
- créer le convertisseur équivalent pour les filtres ;
- créer `TagSchemaMigrationService.Plan/Validate/Commit` ;
- ajouter une API restrictive de remplacement de définition par ID ;
- préserver les IDs et propriétaires ;
- rebinder toutes les références canoniques ;
- faire passer l'éditeur manuel par ce service.

Critère de sortie : tout changement manuel est prévisualisé, atomique et utilise
la même matrice de conversion.

### Phase 3 - Migration différée des projets et workspaces

- adapter le chargement pour convertir avant le rebinding ;
- produire un plan de migration sans publier le graphe ;
- intégrer une notification non bloquante dans les workflows UI de chargement ;
- mettre en quarantaine les valeurs incompatibles sans inventer ni supprimer leur donnée brute ;
- marquer les artefacts migrés comme devant être sauvegardés ;
- appliquer la même logique aux presets de filtres.

Critère de sortie : ouvrir un ancien artefact ne peut ni modifier silencieusement
ses valeurs, ni publier un graphe incohérent.

### Phase 4 - Préférences et inférence uniforme

- ajouter `TagImportPreferences` et son UI prefab-first ;
- valider et normaliser les tokens ;
- créer un service pur d'inférence ;
- adapter tous les importeurs BIDS, CSV, Excel, Intranat et PTS ;
- pré-scanner toutes les références avant toute création ;
- éliminer les créations directes de `StringTag` dans les parseurs ;
- ne plus autosauvegarder les tags pendant le scan.

Critère de sortie : le même ensemble de valeurs produit le même type quel que
soit l'importeur ou l'ordre des fichiers.

### Phase 5 - Draft, rapport et sauvegarde

- introduire un draft d'update sans effet de bord ;
- créer les nouveaux tags en batch ;
- ajouter les nouvelles options enum en batch ;
- collecter les incompatibilités sans retypage ;
- enrichir le rapport d'update ;
- utiliser des écritures temporaires et un rollback sur échec.

Critère de sortie : un échec ou une annulation avant commit ne modifie ni
`Tags.json`, ni la base chargée.

## 11. Tests indispensables

### 11.1 Enum

- round-trip JSON du couple `(index, value)` ;
- lecture d'un JSON legacy contenant uniquement l'index ;
- warning legacy groupé une seule fois par opération ;
- index legacy valide reconstruisant le label courant ;
- index legacy hors limites produisant un échec ;
- couple divergent où la valeur textuelle gagne et répare l'index ;
- réordonnement conservant le sens d'une valeur et d'un filtre modernes ;
- valeur inconnue ne devenant jamais l'option `0` ;
- ajout append-only conservant les anciens indices ;
- suppression ou renommage produisant un conflit explicite ;
- comportement identique de `EnumTagFilterValue`.

### 11.2 Conversions et IDs

- String `"42"` vers Int donnant `42` ;
- tokens personnalisés `yes/no` vers Bool ;
- Float non entier vers Int produisant une conversion avec perte ou un échec ;
- enum moderne vers un autre type utilisant `StringValue` ;
- enum legacy vers un type non-enum échouant explicitement ;
- String vers Enum ajoutant une option manquante après confirmation ;
- échec ne modifiant ni la source ni la destination ;
- ID du tag conservé ;
- IDs des valeurs et filtres conservés ;
- valeur de site restant sur son site ;
- tags non concernés jamais supprimés ;
- références runtime pointant vers l'instance canonique.

### 11.3 Migration différée

- détection dans un projet ;
- détection dans un autre workspace ;
- dialogue avant publication ;
- annulation sans mutation ;
- acceptation modifiant seulement la mémoire ;
- artefact marqué comme devant être sauvegardé ;
- sauvegarde puis rechargement sans nouveau dialogue ;
- filtres inclus dans la migration ;
- mappings booléens personnalisés affichés et appliqués ;
- rapport agrégé pour un grand nombre de valeurs.

### 11.4 Imports

- nouveau tag inféré sur l'union de plusieurs fichiers ;
- nouveau tag inféré sur l'union de plusieurs références ;
- ordre des références sans effet ;
- colonne de zéros reconnue comme Int ;
- valeurs ignorées n'affectant pas le type ;
- tag existant incompatible jamais retypé ;
- incompatibilités présentes dans le rapport ;
- nouvelles options enum ajoutées une seule fois dans un ordre déterministe ;
- aucun effet de bord avant le commit.

## 12. Principaux fichiers concernés

Core tags et migration :

- `Assets/Scripts/HBP/Core/Data/Tags/TagCollection.cs`
- `Assets/Scripts/HBP/Core/Data/Tags/BaseTagValue.cs`
- `Assets/Scripts/HBP/Core/Data/Tags/EnumTag.cs`
- `Assets/Scripts/HBP/Core/Data/Tags/EnumTagValue.cs`
- `Assets/Scripts/HBP/Core/Data/FilterConditions/TagFilterValue.cs`
- nouveaux services d'inférence et de migration sous `Core/Data/Tags`

Chargement et données :

- `Assets/Scripts/HBP/Core/Data/LoadingContext.cs`
- `Assets/Scripts/HBP/Core/Data/Patient/Patient.cs`
- `Assets/Scripts/HBP/Core/Data/Patient/Site.cs`
- `Assets/Scripts/HBP/Core/Data/Project/Project.cs`
- `Assets/Scripts/HBP/Core/Database/GlobalDatabase.cs`

Préférences et UI :

- `Assets/Scripts/HBP/Core/Preferences/DataPreferences.cs`
- `Assets/Scripts/HBP/UI/Main/Edit/UserPreferences/UserPreferencesModifier.cs`
- prefab des préférences utilisateur sous `Assets/Resources/Prefabs/UI/Windows`
- `Assets/Scripts/HBP/UI/Main/Tags/TagModifier.cs`
- `Assets/Scripts/HBP/UI/Main/Tags/TagCollectionModifier.cs`
- workflows UI de chargement projet/workspace
- `Assets/Scripts/HBP/UI/Main/Database/DatabaseWorkflow.cs`

Tests :

- `Assets/Tests/EditMode/HBP.Serialization.Tests/TagCollectionIndexTests.cs`
- nouveaux tests unitaires du parsing, des enums et de la matrice de conversion
- tests d'intégration des workflows de chargement et d'update

## 13. Hors périmètre et dette acceptée

Cette passe n'introduit pas :

- de fenêtre de sélection des types pendant l'import ;
- de changement automatique du type d'un tag existant ;
- de `SchemaVersion` sur les tags ou leurs valeurs ;
- d'historique global des anciennes définitions ;
- d'identité stable par option enum distincte du couple `(index, value)`.

La dette explicitement acceptée est la suivante : un ancien enum index-only ne
peut pas être migré de manière fiable vers un type non-enum après disparition de
son ancienne définition. Cette situation produit un warning lors du changement
manuel et un conflit explicite lors du chargement ; elle ne produit jamais une
valeur silencieusement approximée.
