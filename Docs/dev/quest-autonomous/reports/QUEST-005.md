# Rapport QUEST-005 — Contrat de snapshot anatomique

## Résultat

Le checkout ne contenait aucun contrat de transfert anatomique indépendant du
projet Unity. `HBP.Transfer.Anatomy.Contracts` fournit maintenant un snapshot immuable
d'une surface anatomique complète, son codec binaire **HBNA v1**, et sa validation
bornée. La surface conserve positions, normales, triangles et UV optionnels sans
quantification, décimation, conversion de repère ni recalcul des normales.

Le contrat concerne une seule visualisation/colonne existante. Aucun changement
de capture Desktop, rendu Quest, prefab, scène, entrée ou transport n'est inclus.
L'assembly n'est pas auto-référencée : les futurs consommateurs ajouteront une
référence explicite. Aucun code produit existant n'appelle encore le codec.

Après review du propriétaire le 2026-09-08, le contrat est placé dans
`Assets/Scripts/HBP/Transfer/Anatomy`, namespace `HBP.Transfer.Anatomy` et assembly
`HBP.Transfer.Anatomy.Contracts`. `Transfer` regroupe le code commun aux échanges
Desktop ↔ Quest, dans les deux sens ; `Anatomy` spécialise le contenu transporté.
Desktop utilisera ce contrat pour créer/encoder la capture, Quest pour la
décoder avant affichage. Les GUID des fichiers et dossiers déplacés sont conservés.
Ce renommage ne modifie pas le format HBNA v1 ni les octets de l'exemple.

## État et provenance

- Implémentation : IMPLEMENTEE. Technique : REUSSI. Manuel : NON_REQUIS.
- Branche `feature/xr-autonomous`, HEAD initial
  `fd95c507f8ae1ba0f3d2b8939071bb9108f9673e`, checkout propre au départ.
  Ajouts identifiés par SHA-256 dans le manifeste ; aucun commit/push ni changement
  de branche et aucun dépôt voisin modifié.
- Unity fermé au départ ; tests CLI Windows avec Unity `6000.5.2f1`, hors sandbox.
  Les versions des packages et le verrou natif sont référencés dans le manifeste.
  Le contrat et ses tests n'appellent aucune bibliothèque native.
- [QUEST-001](QUEST-001.md) apporte les IDs opaques réels de fixture
  `quest-001-visualization` et `quest-001-column`. `BaseData.ID` est une chaîne,
  pas obligatoirement un GUID : aucune normalisation ni régénération dans le codec.
- [Manifeste de preuves](../evidence/QUEST-005/manifest.json).
  Les logs/XML/DLL et le binaire exemple sous `.test-results/` et `Library/`
  sont locaux et ignorés ; rapport, manifeste et exemple JSON sont versionnables.

Reprise sélective depuis `eb26c323e2bdf6c138f9e9e3c02f3e9796cae249` :
`Shared/Packages/com.crnl.hibop.render-model/Runtime/RenderBuffer.cs` devient
`AnatomyBuffer<T>`, avec contrainte `unmanaged`, transfert de propriété interne
uniquement et copie publique centralisée dans `AnatomySnapshot.Create`.
`CoordinateSpace.cs`, `RenderAssets.cs` et `SurfaceAssetPayloadCodec.cs` ont servi
de référence pour XYZ/unités, buffers, SHA-256, little-endian et limites 2M/12M.
Le nouveau codec sérialise réellement le repère, contrairement au décodeur
historique qui imposait `DesktopUnityMillimetersV1`. Nouveaux types/namespace et
nouveaux GUID `.meta` ; aucun ancien package ni politique d'autorité restauré.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Règle à vérifier |
| --- | --- | --- |
| 1 | [AnatomySnapshot.Create](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomySnapshot.cs) et [AnatomyBuffer](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomyBuffer.cs) | Validation avant copie publique ; propriété exclusive des buffers ; IDs conservés. |
| 2 | [AnatomyCoordinateSpace](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomyCoordinateSpace.cs) | Matrice affine inversible, unités et handedness explicites ; aucun transform de présentation. |
| 3 | [AnatomySnapshotCodec.Encode/Decode](../../../../Assets/Scripts/HBP/Transfer/Anatomy/AnatomySnapshotCodec.cs) | Format ci-dessous ; deux SHA-256 ; tailles exactes et métadonnées vérifiées avant allocation des surfaces. |
| 4 | [AnatomySnapshotTests](../../../../Assets/Tests/EditMode/HBP.Transfer.Anatomy.Tests/AnatomySnapshotTests.cs) | Corruptions avec hashes recalculés, propriété mémoire, round-trip bit-exact et bornes maximales. |
| 5 | [HBP.Transfer.Anatomy.Contracts.asmdef](../../../../Assets/Scripts/HBP/Transfer/Anatomy/HBP.Transfer.Anatomy.Contracts.asmdef) | `noEngineReferences=true`, aucune référence applicative ou DLL externe, `autoReferenced=false`. |

## Exemple lisible et raison des champs

[example.json](../evidence/QUEST-005/example.json) décrit exactement les champs du
binaire `.test-results/quest-005/example.hbna`, produit par `WriteReviewExample`.
C'est un triangle synthétique de **417 octets**, utilisant les IDs de QUEST-001,
et **pas** une capture du cerveau MNI. La capture réelle relève de QUEST-006.

| Champ | Exemple / sens |
| --- | --- |
| Magic / SchemaVersion | `HBNA` / `1` : version indépendante des anciens codecs, inconnue = rejet. |
| TransferId / SessionId | `example-transfer-005` / `example-session-005` : identité de livraison et de session ; aucune autorité implicite. |
| VisualizationId / ColumnId | IDs de la fixture existante ; leur association est conservée, sans index de colonne ni Unity instanceID. |
| ContentRevision | `1`, entier non signé 64 bits strictement positif, attribué par le producteur pour cette version cohérente. Aucun ordre distribué imposé. |
| FrameId / MappingVersion | `fixture-brain-XYZ` / `1` : nom opaque du repère anatomique cible et version de sa convention de correspondance. |
| Handedness / Unit | `Left` / `Millimeter` : axes source XYZ, un millimètre = `0.001f` m ; metre également accepté. |
| AssetToBrain | Matrice row-major `[-1,0,0,12.25; 0,2,0,-3.5; 0,0,1,7; 0,0,0,1]` : multiplication de vecteurs colonnes. Destination dans les mêmes unités. |
| Winding / Visible | `Clockwise` / `true` : faces avant dans l'espace asset, avant la matrice ; visibilité initiale. |
| Color | `[0.25,0.5,0.75,1]` : RGB linéaire et opacité, quatre float32 dans [0,1], sans ID de matériau/palette Unity. |
| Positions / Normals | Trois XYZ par sommet, 36 octets chacun. `-0` et le subnormal float32 minimal sont intentionnels dans l'exemple. |
| Indices / Uvs | `[0,1,2]` uint32, 12 octets ; trois paires UV float32, 24 octets. Les UV peuvent être absents. |
| SurfaceByteLength / hashes | 108 octets de buffers ; hash surface couvrant ces octets exacts ; hash enveloppe couvrant tout le message hors son propre hash. |

`Handedness` décrit les axes source. Le déterminant négatif de l'exemple inverse
la handedness après `AssetToBrain` ; le consommateur tient compte de cette
inversion pour les faces et utilise l'inverse transposée pour les normales.
Le codec conserve les bits de la matrice et des normales, il n'effectue aucune
transformation. L'origine et le nom du repère réel seront déterminés par la
capture QUEST-006 ; cet exemple ne prétend pas définir une nouvelle convention MNI.

## Format binaire v1

Tous les scalaires numériques sont little-endian. Les flottants sont IEEE-754
binary32 ; NaN/infini sont refusés, les valeurs finies restent bit-exactes.
Ordre exact, sans padding et sans compression :

1. Magic uint32 `0x414E4248` (octets ASCII `HBNA`), version uint16 `1`, longueur
   totale int64 incluant le hash final.
2. Cinq chaînes : TransferId, SessionId, VisualizationId, ColumnId, FrameId.
   Chacune : longueur int32 en octets, puis UTF-8 strict, sans BOM ajouté.
3. ContentRevision uint64 ; Handedness uint8 (`Right=1`, `Left=2`) ; Unit uint8
   (`Meter=1`, `Millimeter=2`) ; MappingVersion uint32 ; 16 float32 AssetToBrain.
4. Winding uint8 (`Clockwise=1`, `CounterClockwise=2`) ; Visible uint8 (0 ou 1) ;
   quatre float32 RGBA.
5. VertexCount, IndexCount, UvCount : trois int32, UV comptés en paires.
   SurfaceByteLength int64 puis SHA-256 surface brut (32 octets).
6. Positions XYZ, normales XYZ, indices uint32, UV : respectivement
   `12*V`, `12*V`, `4*I`, `8*U` octets ; toutes les longueurs dérivées sont exactes.
7. SHA-256 enveloppe brut (32 octets), sur tous les octets précédents.

Taille totale = `214 + somme(longueurs UTF-8) + 24*V + 4*I + 8*U`.
SHA-256 est ici une preuve de cohérence des octets, pas une authentification de
l'émetteur. La surface seule peut garder le même hash quand la révision,
l'apparence ou le repère change ; le hash de l'enveloppe change alors.

## Propriété mémoire et limites

`Create` exige que l'appelant stabilise **ensemble** métadonnées et buffers
pendant l'appel. Il valide puis copie chaque buffer ; la cohérence d'une capture
concurrente reste la responsabilité de QUEST-006. Au retour, toutes les valeurs
sont immuables, détenues par le snapshot jusqu'à leur collecte GC, sans pool ni
handle natif. `ToArray` fournit une copie, `AsReadOnlySpan` une lecture seule.
L'encodage d'un snapshot ne nécessite aucun verrou sur la source Desktop.

`Decode` exige des octets stables pendant l'appel, mais ne les conserve pas.
Après contrôle de la longueur/version/hash global, il vérifie les métadonnées,
les dimensions, la longueur exacte et le hash surface avant d'allouer les gros
buffers. Les petits en-têtes alloués avant cela sont bornés. Les valeurs finies
et indices sont ensuite validés, et seuls les tableaux privés valides sont
publiés. Toute corruption est signalée par `InvalidDataException` ; aucun
snapshot partiel retourné. Les arguments publics invalides sont refusés avec
`ArgumentException` ou un de ses sous-types.

Limites v1 : 3 à **2 000 000 sommets**, 3 à **12 000 000 indices**, multiple de
trois ; UV absents ou autant de paires que de sommets ; chaque ID/FrameId non
blanc, UTF-8 strict de **1 à 1 024 octets**. Les données au-delà sont refusées,
jamais tronquées. La longueur globale est bornée à **128 MiB** ; avec ces
dimensions la surface maximale fait **112 000 000 octets** et le message au plus
**112 005 334 octets**. Tous les calculs de longueur utilisent int64 avant allocation.

Ce plafond n'est pas le pic mémoire de l'application : création = sources +
copies ; encodage = snapshot + un tableau message ; décodage = message + buffers
privés, soit environ 224 Mo au maximum, hors métadonnées/GC. Un test gardant à la
fois sources, snapshot, message et résultat peut dépasser 400 Mo. Aucun budget
mémoire Quest ni débit n'est qualifié ici. Un futur récepteur peut appliquer une
limite locale inférieure avant réception ; il devra refuser, sans tronquer.

## Vérifications effectuées

| ID | Vérification | Résultat / preuve |
| --- | --- | --- |
| T1 | Unity EditMode, assembly `HBP.Transfer.Anatomy.Tests` | REUSSI : 36/36, exit 0, dont round-trip aux maxima 2M/12M/112 Mo. XML/log référencés dans le manifeste. |
| T2 | Round-trip avec/sans UV, tous IDs, révision, repère réfléchi/non uniforme et RGBA ; `-0` et subnormal | REUSSI : comparaison des bits float32 et du message réencodé ; IDs UTF-8 non GUID/non normalisés conservés. |
| T3 | Version/magic, longueur totale/surface, dimensions négatives/trop grandes, triangles incomplets, UV incohérents, index invalide, hashes invalides | REUSSI : rejets avec hashes recalculés pour isoler la validation de contenu ; toutes les troncatures de l'exemple et octet final supplémentaire rejetés. |
| T4 | Métadonnées : enums inconnus, révision/mapping nuls, matrice singulière/non affine/NaN, visibilité invalide, RGBA hors plage/NaN, UTF-8 invalide | REUSSI ; positions/normales/UV non finis également rejetés. |
| T5 | Mutation des sources et copies de sortie, effacement du message après Decode, contrôle des références assembly | REUSSI : snapshot inchangé, références limitées à System/mscorlib/netstandard ; asmdef sans moteur/UI/scène/XR/réseau. |
| T6 | `Tools/format-code.cmd`, puis inspection du diff | REUSSI, exit 0, cinq fichiers C# formatés. Les premières tentatives ont échoué sur l'accès NuGet puis l'absence des nouvelles assemblies dans la solution locale ; génération via `UnityEditor.SyncVS.SyncSolution` réussie, exit 0. Tests réexécutés après formatage. |
| T7 | Revue indépendante d'intégrité ; contrôles de chemins/hashes et `git diff --check` | Conventions du repère et validation préallocation précisées après revue ; aucune anomalie résiduelle relevée. Contrôles finaux dans le manifeste. |
| T8 | Déplacement vers `HBP/Transfer/Anatomy` demandé par le propriétaire ; renommage namespace/assemblies/tests | REUSSI : GUID `.meta` conservés, liens mis à jour, formatage et 36/36 tests sur les nouveaux noms ; hash du fichier exemple identique avant/après. |

Les réécritures automatiques de `OpenXR Package Settings.asset` et
`ProjectSettings.asset` ont été inspectées puis rétablies à leur état initial ;
leur diff est conservé localement dans
`.test-results/quest-005/unity-generated-settings.diff`. Le registre ne modifie
que la ligne QUEST-005. Les avertissements Unity relatifs à deux fichiers sans
`.meta` dans les tests du package URP préexistaient aux ajouts du contrat ; aucun
échec de compilation ou de test n'est constaté dans le run final.

Commande des tests depuis la racine du dépôt, hors sandbox Windows :

```powershell
$questUnityArgs = @('-batchmode', '-nographics', '-projectPath', 'C:\HBP\Software\HiBoP', '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'HBP.Transfer.Anatomy.Tests', '-testResults', 'C:\HBP\Software\HiBoP\.test-results\quest-005\editmode-results.xml', '-logFile', 'C:\HBP\Software\HiBoP\.test-results\quest-005\editmode.log', '-forgetProjectPath')
$questUnityProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe' -ArgumentList $questUnityArgs -Wait -PassThru -WindowStyle Hidden
$questUnityProcess.ExitCode
```

Compilation Editor observée ; aucune construction Player Windows/Android, mesure
physique Quest, capture Desktop, intégration réseau ou validation scientifique
nouvelle exécutée. Ces opérations ne sont pas nécessaires au contrat pur de
QUEST-005 et restent dans les tâches suivantes.

## Validation manuelle

**NON_REQUIS** : aucune manipulation nécessaire. L'exemple JSON et le tableau
ci-dessus permettent la review des champs ; les tests automatiques couvrent le
codec. Aucun retour propriétaire n'est présenté comme acquis.

## Décisions, limites et suite

Version initiale explicite v1 ; SHA-256, buffers GC immuables, matrice affine,
apparence uniforme et limites ci-dessus sont des choix techniques locaux.
Aucune décision produit supplémentaire attendue. D03/D04 sont respectées : pas
de caméra, placement/échelle de présentation ou commandes spatiales partagées.
Aucun graphe projet, volume/masque iEEG, ABI scientifique, réseau, cache ou
politique d'autorité Desktop n'est introduit.

Le contrat décrit une surface complète préparée ; il ne peut pas prouver qu'un
producteur a effectivement capturé tout le cerveau. La provenance/complétude
réelle et la stabilité de la capture seront vérifiées dans **QUEST-006**, tâche
suivante proposée, sans l'exécuter ici.
