# Fixture QUEST-013 — MNI Contacts

Huit contacts entièrement synthétiques, deux patients et deux électrodes par
patient. Le nombre huit est une donnée de fixture, jamais une limite du produit.
Les surfaces et l'IRM MNI restent celles du dépôt (D14), épinglées par les hashes
du [manifeste](manifest.json). Aucun enregistrement patient réel ni EEG.

Les `.patient` sont les entrées produit ; [contacts.json](contacts.json) est la
correspondance attendue pour la review et les tests. La visualisation
[MNI Contacts](MNI%20Contacts.visualization) référence les patients par ID,
l'implantation **MNI**, la surface **MNI Grey matter** et les deux hémisphères.
Le projet conserve l'ordre des patients gauche puis droite. Chacun a A1, A2,
B1, B2, avec IDs de coordonnées et sites stables. Les noms A1/A2 sont
volontairement répétés entre patients pour tester les associations.

## Coordonnées et ordre

Les coordonnées des `.patient` sont natives MNI en mm, droitières.
`Implantation3D.SiteInfo.UnityPosition` applique `ReferenceSystemConversion.ConvertX` :
`(x,y,z) -> (-x,y,z)`. `Site.Information.DefaultPosition` est la position capturée,
dans le même repère `hibop-mni-unity-mm-v1` que la surface préparée. La matrice
`AssetToBrain` est l'identité. Aucune position monde Quest n'est transportée.

| Ordre natif | ID site | Index patient | Index source patient | Nom / électrode | Natif XYZ (mm) | Transport Unity XYZ (mm) |
| ---: | --- | ---: | ---: | --- | --- | --- |
| 0 | `quest-013-site-00` | 0 | 0 | A1 / A | (-31.25, -18.5, 26.75) | (31.25, -18.5, 26.75) |
| 1 | `quest-013-site-01` | 0 | 1 | A2 / A | (-27.5, -15.25, 29.0) | (27.5, -15.25, 29.0) |
| 2 | `quest-013-site-02` | 0 | 2 | B1 / B | (-12.75, 21.5, 42.25) | (12.75, 21.5, 42.25) |
| 3 | `quest-013-site-03` | 0 | 3 | B2 / B | (-9.0, 24.25, 45.5) | (9.0, 24.25, 45.5) |
| 4 | `quest-013-site-04` | 1 | 0 | A1 / A | (22.5, -34.75, 18.25) | (-22.5, -34.75, 18.25) |
| 5 | `quest-013-site-05` | 1 | 1 | A2 / A | (26.25, -31.0, 21.5) | (-26.25, -31.0, 21.5) |
| 6 | `quest-013-site-06` | 1 | 2 | B1 / B | (38.75, 8.25, -12.5) | (-38.75, 8.25, -12.5) |
| 7 | `quest-013-site-07` | 1 | 3 | B2 / B | (42.0, 11.75, -9.25) | (-42.0, 11.75, -9.25) |

Les repères sont asymétriques entre hémisphères et entre axes. Ils sont à
l'intérieur de l'enveloppe de coordonnées du MNI préparé ; cette affirmation
ne signifie pas appartenance à une structure anatomique ou localisation clinique.
L'apparence provient du pipeline Desktop : couleur RGBA linéaire du matériau,
diamètre anatomique de la sphère (2 mm avant gain), visibilité préparée.
Le gain de fixture vaut 1 et `ShowAllSites=true` conserve les huit contacts visibles sans ROI. Les états ROI/filtre/blacklist ne retranchent aucun
contact de la liste transportée ; ils sont capturés séparément de la visibilité.

## Reproduire

Depuis `C:\HBP\Software\HiBoP` :

```powershell
.\Tools\Prepare-QuestAnatomyFixture.ps1 -Fixture mni-contacts -OutputDirectory .artifacts/quest-013/fixture
```

Archive déterministe :
`C:\HBP\Software\HiBoP\.artifacts\quest-013\fixture\quest-mni-contacts.hibop`.
Le script vérifie les sources, normalise UTF-8/LF et fixe les dates ZIP.
La fixture anatomique existante reste accessible avec `-Fixture mni-anatomy`.

Pour la review propriétaire, examiner le tableau et les fichiers source ; aucun
casque requis. Le [rapport QUEST-013](../../reports/QUEST-013.md) consigne les
vérifications réellement exécutées et les chemins des binaires.
