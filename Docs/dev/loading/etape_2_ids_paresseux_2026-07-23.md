# Étape 2 — Identifiants paresseux

Date : 23 juillet 2026

## Résultat

L'étape 2 du plan d'optimisation est implémentée et validée. La construction
sans paramètre d'un objet dérivé de `BaseData` ne génère plus immédiatement un
GUID qui serait remplacé quelques instants plus tard par Json.NET.

Le champ JSON reste une propriété publique nommée `ID`. Le changement est donc
interne au modèle et ne modifie ni les fichiers actuels, ni les noms de types,
ni le binder de rétrocompatibilité.

Les mesures détaillées sont dans
[`resultats_etape_2_2026-07-23.md`](resultats_etape_2_2026-07-23.md).

## Implémentation

`BaseData` possède maintenant un champ privé `m_ID` :

- le constructeur sans paramètre laisse ce champ à `null` ;
- le getter génère un GUID au premier accès si le champ n'a jamais été
  assigné ;
- le setter affecte directement la valeur lue par Json.NET ;
- la sérialisation d'un objet neuf appelle le getter et garantit donc qu'un ID
  est écrit ;
- `OnDeserialized` génère encore un ID pour un ancien objet dont le champ est
  absent ou vide ;
- `GenerateID()` conserve son rôle historique de remplacement explicite.

Le premier accès est thread-safe. `Volatile.Read` et
`Interlocked.CompareExchange` garantissent que les lecteurs concurrents
observent tous l'unique valeur retenue.

Les constructeurs sans paramètre de `Dataset` et `DataInfo` généraient leur
propre GUID avant de le transmettre à `BaseData`. Ils utilisent maintenant
leurs surcharges sans ID et suivent le même chemin paresseux.

Une recherche sur toute la hiérarchie `HBP.Core.Data` ne laisse plus que deux
appels à `Guid.NewGuid().ToString()` :

- le premier accès paresseux ;
- l'opération explicite `GenerateID()`.

## Contrats préservés

| Cas | Comportement |
| --- | --- |
| JSON avec un ID | le setter stocke directement l'ID, sans GUID préalable |
| JSON historique sans ID | `OnDeserialized` crée un ID de secours |
| JSON avec un ID vide | `OnDeserialized` crée un ID de secours |
| objet créé par le produit | l'ID apparaît au premier accès ou à la sérialisation |
| `Equals` / `GetHashCode` | l'accès à `ID` matérialise une identité stable |
| objet utilisé comme clé | le hash matérialise l'ID avant l'insertion |
| clone | le getter matérialise l'ID source, puis le clone le conserve |
| `GenerateID()` récursif | les implémentations existantes continuent de remplacer tous les IDs concernés |

### Particularité des IDs vides explicites

Le getter distingue `null` de `string.Empty`.

Une affectation métier volontaire `ID = ""` doit rester observable : le
contrôle `Project.CheckProjectIDsAsync` s'en sert pour signaler une donnée
invalide. Générer un ID dans le getter pour cette valeur aurait masqué
l'erreur. En revanche, un ID vide rencontré pendant la désérialisation est
réparé dans `OnDeserialized`, comme auparavant.

Cette distinction a été détectée par la suite de non-régression projet avant
la validation finale.

## JSON, types historiques et IL2CPP

La propriété reste annotée `[JsonProperty]`. Son nom, son type et sa visibilité
ne changent pas ; le manifeste de contrat de sérialisation reste donc
identique.

Les tests couvrent aussi le binder des anciens noms d'assembly et de namespace.
Le changement ne dépend pas du type concret : les types customs et historiques
qui héritent de `BaseData` utilisent automatiquement le même mécanisme.

Le code de production n'ajoute :

- aucune réflexion ;
- aucun `FormatterServices.GetUninitializedObject` ;
- aucun code généré dynamiquement ;
- aucun nouveau type à enregistrer pour le stripping.

`Volatile` et `Interlocked` sont des primitives .NET/AOT prises en charge par
IL2CPP. La validation exécutée ici est une validation Editor Mono ; le test
player IL2CPP global reste prévu à la fin du plan, lorsque les changements des
étapes suivantes seront stabilisés.

## Validation automatisée

`BaseDataLazyIdTests` couvre neuf scénarios :

1. absence de génération d'ID à la construction ;
2. génération et écriture lors de la sérialisation ;
3. conservation d'un ID lu depuis JSON ;
4. réparation d'un ancien JSON sans ID ;
5. conservation d'un ID vide explicitement affecté pour validation ;
6. utilisation comme clé de dictionnaire ;
7. égalité de deux objets neufs ;
8. clone puis régénération explicite ;
9. 64 premiers accès concurrents.

Les suites ciblées de sérialisation, types historiques, archive projet,
contrats et index des tags passent avec les nouveaux tests : **94 / 94**.

## Critère de fin

Le critère de l'étape est rempli : un objet dont le JSON fournit déjà un ID ne
génère plus de GUID temporaire dans son constructeur.

Sur la base réelle, le graphe patient contient au minimum 476 738 objets
`BaseData`. Le coût systématique correspondant a disparu ; seuls les objets
réellement dépourvus d'identité génèrent encore un GUID de secours.
