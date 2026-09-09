# Rapport QUEST-022 — Apparence scientifique commune des sites

## Résultat

La règle de taille, catégorie de couleur et visibilité iEEG est désormais dans
`Core.Object3D.SiteAppearance`. Desktop l'appelle pour son rendu ; la capture
iEEG l'appelle également et fournit ses résultats au backend Quest via les
contacts HBNA existants. Le transfert iEEG ne relit plus le Renderer, le matériau
courant ou le transform du site Desktop. Aucune formule n'est ajoutée au backend Quest.

Entrées : valeur déjà échantillonnée, plage préparée complète `SpanMin/Middle/SpanMax`,
états masked/out-of-ROI/filtered/blacklisted, politiques d'affichage et validité
du générateur. Sorties : échelle scientifique, `SiteType`, visibilité. La valeur
est bornée avant soustraction du milieu ; l'échelle va de **0,5 au milieu à 2,5
aux bornes**. Au milieu, la catégorie reste **Negative**. La blacklist prime sur
la validité du générateur et peut être visible à taille unitaire, tout en restant
exclue de la projection native. Une activité périmée utilise la catégorie Normal.

`SiteMaterials.GetSharedMaterial` reste l'unique correspondance catégorie/palette.
Desktop continue d'y appliquer `IsHighlighted` et son gain de sites. La capture
utilise la variante sans surbrillance, convertit la couleur en linéaire comme
avant et produit `Diameter = 2 × (Scale × SiteGain)`. Le backend existant
`QuestContactRenderer.Prepare` charge les RGBA et un rayon nul pour les sites
invisibles ; l'échelle/pose du groupe Quest restent locales. Aucune sélection XR,
aucune nouvelle palette et aucun changement de prefab ou de protocole.

La capture d'un site invisible masqué/hors ROI fournit maintenant une taille
neutre de 1 avant gain, plutôt qu'un ancien transform mémorisé. Ce champ ne rend
pas le site visible. Les transforms et matériaux des sites invisibles Desktop
conservent leur comportement antérieur, y compris la remise à un du transform
d'un site blacklisté caché. La surbrillance Desktop n'est plus exportée en iEEG.

Le sampling ne change pas : les sites utilisent `TemporalSample.Evaluate`, la
surface utilise `CurrentProjectionSample.Index`. Aucun recalibrage sur l'instant
transféré. CCEP conserve son override de rendu et ses caches, dont le calcul
de taille appelle aussi la règle extraite ; les colonnes statiques restent hors
périmètre. Il n'y a ni nouveau scheduler ni changement de ressources natives.

## État et provenance

- Implémentation : **IMPLEMENTEE** ; technique : **REUSSI** ; manuel : **VALIDE**.
- Branche `feature/xr-autonomous`, HEAD initial `a564f8c9811a01f0d99f3df461a5f1c23439c973`, arbre initial propre.
- Changements non commités ; aucune reprise historique ni modification des dépôts natifs.
- Unity `6000.5.2f1`, profil `Assets/Settings/BuildProfiles/DesktopWindows.asset`, références natives de `Tools/NativePlugins.lock.json`.
- Dépendance [QUEST-021](QUEST-021.md) validée ; fixture synthétique de QUEST-020 réutilisée.
- [Manifeste des preuves](../evidence/QUEST-022/manifest.json). Les binaires et logs complets restent locaux.

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole | Règle à vérifier |
| --- | --- | --- |
| 1 | [SiteAppearance.FromActivity / Resolve](../../../../Assets/Scripts/HBP/Core/Object3D/SiteAppearance.cs) | Ordre des bornages, signe au milieu, visibilité distincte du masque natif. Résultat sans Renderer, UI, matériau ou état local. |
| 2 | [Column3DDynamic.EvaluateSiteAppearance / UpdateSitesRendering](../../../../Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs) | Sampling des sites inchangé, appel commun réel et maintien de la présentation Desktop. Cache conservé pour CCEP. |
| 3 | [DesktopContactsCapture.Capture](../../../../Assets/Scripts/HBP/Transfer/Anatomy/Desktop/DesktopContactsCapture.cs) | Branche iEEG sans lecture des Renderer, palette existante sans surbrillance, gain puis conversion rayon/diamètre et RGB linéaire. Branche anatomique inchangée. |
| 4 | [QuestContactRenderer.Prepare](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestContactRenderer.cs) et [test du buffer](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestContactRenderingTests.cs) | Consommateur existant non modifié : upload des résultats, sans recalcul scientifique et sans influence du placement sur les données. |
| 5 | [SiteAppearanceTests](../../../../Assets/Tests/EditMode/HBP.Transfer.Anatomy.Desktop.Tests/SiteAppearanceTests.cs) et [IEEGProjectionDiagnostic.VerifySites](../../../../Assets/Scripts/HBP/Dev/IEEGProjectionDiagnostic.cs) | Référence indépendante avant extraction ; comparaison au rendu réel du Player et capture lorsque les matériaux des Renderer de sites sont temporairement absents. |

## Vérifications effectuées

| ID | Scénario / commande | Résultat / preuve |
| --- | --- | --- |
| T1 | CLI EditMode, [arguments](../evidence/QUEST-022/editmode-command.json) | **77/77 réussis**, aucun ignoré, sortie **0** ; [XML](../evidence/QUEST-022/editmode.xml). |
| T2 | Dans T1 : cinq plages, neuf valeurs chacune, 128 combinaisons d'état et de politique (5 760 cas) | Égalité exacte avec la règle de HEAD initial pour visibilité et taille/catégorie des sites visibles ; valeurs négatives, extrêmes, milieu, plages dégénérées et périmées incluses. |
| T3 | Dans T1 : toutes les politiques temporelles, tous les index du test Desktop 200 Hz / projection 100 Hz | Égalité avec `IEEGInstant.SiteValues`, masques/blacklist et indépendance de sélection, surbrillance, pose et échelle locales. Régressions de projection QUEST-021 conservées. |
| T4 | CLI PlayMode, [arguments](../evidence/QUEST-022/playmode-command.json) | **86 réussis, 0 échec, 6 ignorés**, sortie **0** ; [XML](../evidence/QUEST-022/playmode.xml). Buffer GPU : tailles/couleurs exactes, sept sites visibles sur huit, masqué absent, blacklist visible, placement sans mutation du buffer. |
| T5 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows -EvidenceId quest-022` | **REUSSI**, sortie **0**, Player Windows IL2CPP de développement ; logs et hashes dans le manifeste. |
| T6 | `Tools/Run-QuestIEEGProjection.ps1 -EvidenceId quest-022` | **18/18 comparaisons exactes**, six index × trois paramètres ; **3/3 captures sans matériau dans les Renderer de sites** ; sortie **0**, [résultat](../evidence/QUEST-022/player-result.json) et [arguments](../evidence/QUEST-022/player-command.json). |
| T7 | `Tools/format-code.cmd`, frontière commune et `git diff --check` | **REUSSI** ; [frontière commune](../evidence/QUEST-022/seam-check.json). Restauration NuGet hors sandbox après échec réseau dans la sandbox. |

Les trois captures à 0 ms (paramètres initiaux, modifiés, restaurés) sont **identiques
byte pour byte** aux PNG de QUEST-021 ([hashes](../evidence/QUEST-022/reference-images.json)).
Les vues à [−400 ms](../evidence/QUEST-022/sites-index10.png) et
[800 ms](../evidence/QUEST-022/sites-index130.png) proviennent du nouveau Player.
Ces images montrent surtout la surface opaque : les contacts internes ne sont
pas une preuve visuelle accessible dans cette vue. Le diagnostic contrôle leurs
états, tailles et références de matériaux directement ; la recette ci-dessous
utilise la transparence pour les observer.

Le Player a terminé seul avec le code 0 ; aucun processus utilisateur n'a été
arrêté. Les fichiers de configuration/rendu et métadonnées générés par Unity
ont été sauvegardés sous `.test-results/quest-022/unity-generated.patch`, puis
restaurés à leur état initial. Les artefacts PerformanceTestRun générés ont été
déplacés dans ce même dossier. Aucun changement produit de ces réglages.

Les six tests PlayMode ignorés sont les scénarios optionnels de livraison TLS/
manipulation de captures réelles : leurs arguments de fixture ne sont pas fournis
à cette campagne centrée sur le rendu des sites. Le nouveau test GPU est réussi.
Aucun test physique Quest, build Android ou verdict de parité de pixels Android
n'est annoncé dans cette tâche.

## Validation manuelle du propriétaire

Player livré : `C:\HBP\Software\HiBoP\.artifacts\quest-022\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-020\fixture\quest-mni-ieeg.hibop`.
Le lanceur importe le protocole synthétique en mémoire avant l'archive.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\HBP\Software\HiBoP\Tools\Run-QuestIEEGProjection.ps1" -EvidenceId quest-022 -KeepOpen
```

1. **M1 — VALIDE le 2026-09-09.** Dans **MNI iEEG**, colonne **Synthetic uV**, onglet **Activity**, calculer si nécessaire avec le bouton `Compute IEEG` du prefab `Assets/Prefabs/3D/UI/3D Menu.prefab`. Dans **Timeline**, placer le curseur à **0 ms** (index 50) et comparer avec la [référence Desktop QUEST-021](../evidence/QUEST-021/ieeg-0-index50.png). Pour voir les contacts internes, activer **Transparent Brain** dans les outils de scène et réduire son curseur de transparence (composant `TransparentBrain` du même prefab). Parcourir ensuite **−400 ms** (index 10) et **800 ms** (index 130) : les sites négatifs/positifs gardent leurs couleurs, leur taille suit l'activité et les sites masqués ne réapparaissent pas. Revenir à 0 ms retrouve l'apparence initiale. Retour propriétaire : **« Je valide M1 »**, le 2026-09-09 ([preuve](../evidence/QUEST-022/manual-validation.json)).

## Décisions, limites et suite

Le shader [BufferedContacts](../../../../Assets/Shaders/Quest/BufferedContacts.shader)
existant rend les sites opaques avec éclairage (`site.color.rgb`, alpha final 1),
alors que [SiteShader](../../../../Assets/Resources/Shaders/SiteShader.shader)
Desktop applique la transparence du matériau. Les RGBA sont conservés dans le
buffer mais l'alpha n'est pas représenté par le shader Quest actuel. La couleur
scientifique de base et son sens sont communs ; cela ne prouve pas une identité
visuelle de transparence, luminosité ou occlusion entre plateformes. Aucun
changement de shader n'est inclus pour masquer cette limite. La qualification
visuelle iEEG sur casque appartient à QUEST-023/024.

La règle commune est appelable depuis Core avec des valeurs préparées sans
référence Desktop. Dans le flux livré, Desktop évalue l'apparence et Quest
consomme ses résultats HBNA. Cette tâche n'ajoute pas de commandes scientifiques
ou de recalcul d'apparence interactif au casque.

QUEST-022 est terminée : implémentation et vérifications techniques réussies, M1 validée par le propriétaire le 2026-09-09.

Suite proposée : **QUEST-023**, sans l'engager. Aucune décision scientifique nouvelle.
