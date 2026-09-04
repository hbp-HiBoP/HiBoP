# P02 — notes de compatibilité Contracts V1

## Baseline

- package UPM : `com.crnl.hibop.contracts` `0.2.0` ;
- contrat logique : `ContractVersion.V1`, soit `1.0` ;
- assembly Runtime : `CRNL.HiBoP.Contracts`, sans référence d'assembly et avec `noEngineReferences: true` ;
- cible pure vérifiée : `netstandard2.1` / C# 9 ;
- shells Unity vérifiés : Desktop et XR sous Unity `6000.5.2f1`.

## Règles d'évolution

- Le SemVer du package, les versions des applications, le protocole et le schéma restent distincts conformément à D18.
- Un ajout de propriété/commande est optionnel et protégé par capability ; son identifiant numérique n'est jamais réutilisé.
- Modifier le sens, le type, la cardinalité ou le caractère requis d'une valeur impose un nouveau major du contrat et un nouveau hash de schéma.
- Le codec wire, les champs inconnus et la négociation de capability seront définis dans Protocol. P02 ne promet aucune compatibilité binaire avec une représentation de sérialiseur particulière.
- Les IDs sont stables dans un epoch uniquement. Leur forme byte/texte est stable, mais leur valeur est remappée lors d'un nouvel epoch.
- La V1 n'est pas compatible wire avec le prototype HoloLens.

## AOT

Le Runtime n'utilise ni Unity, reflection de sérialisation, `dynamic`, IO, native, unsafe ou type générique ouvert créé à l'exécution. Le smoke IL2CPP/AOT sur Player XR sera ajouté au gate P04 dès que ce paquet fournit la cible correspondante ; il n'est pas simulé par P02.
