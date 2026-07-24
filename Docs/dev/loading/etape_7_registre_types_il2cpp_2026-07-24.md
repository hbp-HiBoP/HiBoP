# Étape 7 — Registre de types explicite et IL2CPP

Date : 24 juillet 2026

## Résultat

Le binder Json.NET ne découvre plus les types au runtime et n'utilise plus
`Type.GetType`. Il résout uniquement les noms présents dans un registre C#
généré et versionné.

L'inventaire actuel contient **154 types concrets** marqués `[JsonObject]` :

- 139 types dans `HBP.Core.Runtime` ;
- 15 types dans `HBP.Data.Runtime`.

Le format JSON ne change pas. L'écriture conserve les noms de types et
d'assemblies actuels, tandis que la lecture accepte également les noms
historiques déclarés dans la configuration d'alias.

## Registre runtime

`SerializationTypeRegistry` maintient :

- une table ordonnée par le nom de type sérialisé ;
- l'ensemble des types autorisés pour l'écriture ;
- une détection des collisions entre noms actuels et alias.

Une résolution inconnue lève une `JsonSerializationException` indiquant le
`$type` concerné et la commande Editor permettant de régénérer le registre.
Cette erreur est remontée au premier niveau même lorsque Json.NET tente de
l'envelopper.

L'assembly contenu dans `$type` reste écrit normalement mais n'est pas utilisé
comme clé de sécurité à la lecture. C'est volontaire : l'ancien binder
acceptait déjà un nom de type connu provenant de `Assembly-CSharp` ou d'un
autre ancien découpage d'assembly. Seul le nom de type explicitement autorisé
peut néanmoins être instancié.

## Deux fragments générés

`HBP.Data.Runtime` dépend de `HBP.Core.Runtime`. Un fichier unique placé dans
Core ne pourrait donc pas contenir des `typeof(...)` vers les types Data sans
créer une dépendance circulaire.

Le générateur produit deux fragments constituant un registre logique unique :

- `GeneratedCoreSerializationTypes.cs` dans `HBP.Core.Runtime` ;
- `GeneratedDataSerializationTypes.cs` dans `HBP.Data.Runtime`.

Le fragment Core est enregistré lors de l'initialisation du registre. Le
fragment Data est enregistré avec
`RuntimeInitializeLoadType.AfterAssembliesLoaded`, avant le chargement des
scènes. Une initialisation Editor supplémentaire garantit sa disponibilité
hors Play Mode.

## Alias historiques

Les règles de rétrocompatibilité sont stockées dans
`Assets/SerializationTypeAliases.json`. Le fichier versionné contient :

- des migrations de préfixes de namespaces ;
- des alias de types individuels lorsque cela sera nécessaire.

Les deux migrations existantes sont conservées :

```text
HBP.Data.Database.*   -> HBP.Core.Database.*
HBP.Data.Preferences.* -> HBP.Core.Preferences.*
```

La réflexion n'est utilisée que par le générateur Editor. Celui-ci développe
les règles en correspondances explicites vers des `typeof(...)`. Le Player ne
lit pas le JSON d'alias et ne recherche aucun type dynamiquement.

Le générateur rejette :

- un schéma de configuration inconnu ;
- une règle incomplète ;
- une cible qui n'existe pas ou n'est pas sérialisable ;
- un préfixe qui ne correspond à aucun type courant ;
- deux alias identiques pointant vers des types différents ;
- un alias en conflit avec un nom actuel.

## Validation Editor

La commande suivante régénère les deux fragments :

```text
Tools/Serialization/Generate Type Registry
```

À chaque tentative de passage en Play Mode, le contenu attendu est comparé aux
fichiers générés. Si le registre est périmé :

- le passage en Play est annulé ;
- une erreur liste les types manquants ;
- la commande de régénération est indiquée.

Un test EditMode vérifie également que les fichiers générés sont identiques à
la sortie attendue et que chaque type concret `[JsonObject]` est enregistré.

## Intégration au build

`HBPBuilder.BuildProjectAndZipIt` lance le générateur avant chaque build
Windows, Linux ou macOS.

Si les sources générées changent, le build s'arrête volontairement : Unity
doit d'abord compiler les nouvelles références `typeof(...)`. Le message
demande de relancer le build après compilation. Lorsque les fichiers sont déjà
à jour, le build continue immédiatement.

Un `IPreprocessBuildWithReport` valide aussi le registre pour les builds
lancés sans `HBPBuilder`. Il refuse un build périmé mais ne modifie pas des
scripts pendant la phase tardive de préprocessing.

## Relation avec `link.xml`

Le fichier `Assets/link.xml` existant est conservé sans modification. Il
préserve intégralement `HBP.Core.Runtime` et `HBP.Data.Runtime`.

Les deux mécanismes sont complémentaires :

- les `typeof(...)` rendent la liste des types autorisés explicite ;
- `link.xml` préserve aussi les constructeurs et membres utilisés par la
  réflexion Json.NET sous IL2CPP.

Réduire la portée de `link.xml` est un chantier distinct qui nécessiterait une
nouvelle matrice de builds et de round-trips IL2CPP.

## Validation automatisée

Les tests ajoutés couvrent :

- l'identité exacte entre inventaire Editor et sources générées ;
- l'enregistrement de tous les types Core et Data ;
- les noms courants et les anciens namespaces ;
- les anciens `$type` utilisant `Assembly-CSharp` ;
- le rejet explicite d'un type inconnu ;
- l'initialisation du fragment Data avant les tests Play Mode ;
- un round-trip fichier dans le Player.

Le test Play Mode est inclus dans `HBP.Workflow.PlayModeTests` afin de pouvoir
être exécuté dans les players Windows et Linux IL2CPP.

Résultats obtenus :

- tests ciblés du binder et du registre : **17 / 17** ;
- `HBP.Serialization.Tests` et `HBP.ProjectWorkflow.Tests` :
  **427 / 427** ;
- `HBP.Workflow.PlayModeTests` dans l'Editor : **19 / 19** ;
- tests ciblés dans un player Windows IL2CPP : **2 / 2**.

Le build Windows contient bien les implémentations IL2CPP générées de
`GeneratedCoreSerializationTypes`, `GeneratedDataSerializationTypes` et
`SerializationTypeRegistry`. L'exécution Linux IL2CPP reste destinée à un
runner Linux ou à la CI ; les mêmes tests Player sont déjà inclus dans
l'assembly portable.

Pendant une exécution de la suite complète depuis l'Editor interactif, Unity
6000.5.2f1 a de nouveau crashé après les 427 tests, dans
`mono_add_process_object` pendant `RestoreSceneSetupTask`. La même suite a
ensuite produit **427 / 427** et un XML complet en batchmode. La pile native
est identique à l'incident antérieur à cette étape et ne contient aucun code
du registre.
