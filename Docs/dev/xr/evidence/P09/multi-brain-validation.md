# P09 — validation multi-BrainInstance et partage physique

- **Date :** 3 septembre 2026
- **Résultat :** `PASS — WINDOWS EDITMODE, D1 ANATOMICAL`
- **Baseline :** branche `feature/xr`, commit `83b187757`
- **Unity :** `6000.5.2f1`
- **Surface profilée :** anatomical D1, 69 104 sommets, 138 216 triangles, hash `ab8794d4bd5ecb3daa20d26d74f35b1be533f0a3cf6c02ec5215592bc795d135`

## Gate et modèle livré

L'[ADR P09](../../adr/P09-multi-brain.md) ferme P09-A–E avant le modèle public. Les seuls bindings V1 sont `VisualizationBound`, qui suit la colonne sélectionnée de sa visualisation, et `ColumnBound`, qui reste épinglé à une colonne membre explicite. La création est uniquement demandée par le Quest. Une cible supprimée ferme ses instances concernées et retourne leur ID avec une cause fermée ; une réouverture ne les ressuscite pas.

L'inventaire a révélé que P02 séparait correctement IDs d'entités et IDs de scopes sans encore publier leur mapping. P09 ajoute donc trois clés de contrat : `VisualizationEntity`, `ColumnEntity` et `ColumnVisualization`. Le parser P09 rejette avant mutation tout mapping absent, dupliqué ou contradictoire.

`BrainInstanceRegistry` consomme un snapshot P07 déjà validé, instancie exclusivement le prefab P09, et acquiert les surfaces via `RemoteSurfaceAssetStore` P08 puis `P05StaticSurfaceRenderer`. Un remplacement absent conserve le dernier mesh cohérent. Les poses, rotations, échelles uniformes et visibilités restent locales ; aucun chemin de commande Desktop n'est présent dans l'assembly.

## Fermeture, reprise et indépendance

Les tests prouvent les transitions suivantes :

- changement de sélection : `VisualizationBound` suit la nouvelle colonne, `ColumnBound` ne bouge pas ;
- rebind XR : même instance, même layout, nouvelle liaison validée ;
- fermeture colonne : seules les instances épinglées à cette colonne sont retirées avec `ColumnClosed` ;
- fermeture visualisation : toutes ses instances sont retirées avec `VisualizationClosed` ;
- réouverture dans le même epoch : aucune instance fantôme n'est recréée ;
- snapshot/reprise dans le même epoch : ID, position, rotation et échelle restent identiques ;
- nouvel epoch : toutes les instances ferment avec `NewEpoch`, les leases actifs sont libérés et `ResidentBytes` revient à zéro ;
- deux instances transformées différemment gardent le même objet `SurfaceAsset` et le même objet `Mesh` par identité de référence ; modifier/recentrer/scaler l'une ne modifie pas l'autre ;
- 256 cycles create/close terminent avec zéro instance et zéro mesh référencé, puis la fermeture de session ramène le cache P08 à zéro octet.

## Métriques 1/3/8 archivées

Les valeurs viennent de `BrainInstanceMetrics`, avec `Profiler.GetRuntimeMemorySizeLong` appliqué à l'ensemble des meshes distincts. Le payload D1 est réellement encodé, transféré en chunks, validé et acquis par P08 avant upload P05.

| Instances | Renderers | `SurfaceAsset` distincts | `Mesh` distincts | Payload P08 résident | Mémoire mesh distincte | Draw calls structurels attendus |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 1 | 1 | 3 317 125 octets | 6 635 760 octets | 1 |
| 3 | 3 | 1 | 1 | 3 317 125 octets | 6 635 760 octets | 3 |
| 8 | 8 | 1 | 1 | 3 317 125 octets | 6 635 760 octets | 8 |

La mémoire de topologie reste strictement constante entre 1, 3 et 8 instances : un payload décodé et un mesh. Les draw calls augmentent avec les renderers, comme attendu pour le chemin opaque P05 ; cette valeur est structurelle (un pass opaque par renderer), pas une capture GPU Quest. Le `MaterialPropertyBlock` et le `Transform` restent propres à chaque renderer sans cloner matériau ou topologie.

## Résultats automatisés

| Projet / assemblies | Tests | Échecs | Durée |
| --- | ---: | ---: | ---: |
| XR — `CRNL.HiBoP.XR.BrainInstances.EditModeTests` | 13 | 0 | 0,731525 s |
| XR — P09 + régressions P08/P05 | 24 | 0 | 0,9478179 s |
| Desktop — Contracts + Protocol + RenderModel + Serialization | 670 | 0 | 33,0028003 s |

Commandes exécutées avec Unity fermé, via `Unity.exe -batchmode -nographics -runTests`, sans `-quit`, et résultats XML sous `.test-results/p09/`. La validation `P09ProjectSetup.Validate` passe également avec code de sortie 0 et vérifie le prefab, ses références P05 sérialisées, le prefab de démonstration et la scène multi-instance.

## Limites de la preuve

Il s'agit d'une preuve Windows EditMode et d'une scène de démonstration synthétique. Elle ne mesure pas les draw calls GPU sur Quest et ne raccorde pas encore les gestes mains/contrôleurs, réservés à P13. Elle ne couvre volontairement ni sites, ni timeline, ni coupes, ni persistance durable du layout.
