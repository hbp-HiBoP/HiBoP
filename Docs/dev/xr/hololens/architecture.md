# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — architecture observée

**Prototype :** `HiBoP_HoloLens` à `5a119948`, Unity `2021.3.13f1`  
**Nature de l'analyse :** code, assets, settings et historique Git locaux ; aucun appareil HoloLens utilisé

## Conclusion

Le prototype n'est pas une application HoloLens autonome. C'est un fork Unity complet de HiBoP lancé sur un PC Windows, dont Microsoft Mixed Reality OpenXR App Remoting transmet les images stéréo, l'audio éventuel et les entrées au casque.

`ConnectToHoloLens.Connect` et `HBP/HoloLens/ConnectForm` construisent une `RemotingConfiguration` avec IP, port, débit, codec et audio, puis appellent `AppRemoting.Connect`. `HBP/HoloLens/ProjectManager.LoadProject` ouvre le fichier `.hibop` sur le filesystem du PC et charge les scènes via `Module3DMain`.

Conséquences :

- les calculs, données, meshes, sites, coupes et timelines restent dans le même processus desktop ;
- aucun mesh, site ou résultat scientifique n'est sérialisé vers le casque par HiBoP ;
- aucune application UWP/ARM autonome n'est produite par le script de build observé ;
- le « transport » historique est un flux d'affichage/entrée géré par Microsoft, pas un protocole HiBoP ;
- aucune preuve de reconnexion sémantique, versioning, snapshot, révision ou cache de données n'existe.

## Topologie historique

```text
PC Windows — processus Unity HiBoP_HoloLens
  chargement local .hibop
  logique HiBoP complète copiée
  plugins natifs x86_64
  MRTK2 + Microsoft OpenXR
  rendu stéréo
          |
          | Microsoft OpenXR App Remoting
          | vidéo/audio/poses/entrées
          v
HoloLens — runtime de remoting
```

Le script `HBPBuilder.cs` cible Standalone Windows/Linux/macOS et la scène `HoloLensTest.unity`; il ne configure pas un Player UWP/ARM HoloLens.

## Historique et partage

Le dépôt démarre avec MRTK en septembre 2022. Un commit ajoute ensuite les anciens binaires `hbp_export`, `hbp_math` et `EEGFormat`, puis App Remoting et enfin « tout HiBoP ». Des commits ultérieurs appliquent manuellement des modifications de HiBoP classique.

Le prototype ne possède pas d'asmdefs versionnés. La majorité du code HBP a un homologue dans le HiBoP courant, mais aucun fichier comparé n'est encore identique à l'octet. L'architecture est donc une copie devenue divergente, non un partage de composants.

## Autorités et états

| État | Propriétaire historique |
| --- | --- |
| projet, patients, visualisations | processus PC |
| calcul et résultats scientifiques | processus PC |
| timeline, sélection, colonnes, coupes | processus PC |
| objets Unity et transformations | processus PC, pilotés par entrées remoting |
| session App Remoting | runtime Microsoft + formulaire PC |
| cache applicatif casque | inexistant |

Il n'existe pas de séparation nette entre état sémantique, état de rendu et état spatial local. Les transformations MRTK modifient les objets du processus PC.

## Flux observés

1. L'utilisateur saisit l'adresse HoloLens, le port et les paramètres vidéo.
2. Le processus PC appelle `AppRemoting.Connect`.
3. Une fois connecté, le formulaire de projet devient disponible.
4. L'utilisateur choisit un projet `.hibop` local au PC.
5. HiBoP charge les visualisations, construit les scènes et exécute ses plugins natifs desktop.
6. MRTK transforme les gestes remontés en manipulations des GameObjects PC.
7. Le renderer PC produit les images renvoyées au casque.

Une perte de remoting rebascule l'UI de connexion ; il n'y a pas de reprise d'un miroir d'état puisque le casque n'en possède pas.

## Composants fonctionnels

- plusieurs visualisations sont chargées dans une liste de `Base3DScene` ;
- les colonnes peuvent correspondre à plusieurs cerveaux/états ;
- `ObjectManipulator` MRTK est ajouté aux scènes, colonnes et sphères ROI ;
- les sites sont des GameObjects, et le pinch proche scanne linéairement tous les sites ;
- les coupes et projections sont calculées localement dans le processus PC ;
- `MAXIMUM_VIEW_NUMBER = 5` est une limite historique de vues et ne doit pas devenir une limite produit XR.

## Décision pour Quest

À conserver :

- isolation d'un shell XR ;
- concepts de manipulation spatiale, sélection proche, plusieurs cerveaux et ROI ;
- desktop comme autorité scientifique.

À remplacer :

- App Remoting par un client autonome et un protocole de données ;
- copie de code par packages partagés ;
- MRTK2 par OpenXR/XRI et adaptateurs Meta ;
- GameObject/site et scan O(N) par buffers/instancing et index spatial ;
- mesh cloné par colonne par assets immuables et buffers dynamiques ;
- état implicite local par IDs, scopes, révisions, snapshot et resync.

Le prototype est une source de comportement et de dette, pas une base logicielle à porter.
