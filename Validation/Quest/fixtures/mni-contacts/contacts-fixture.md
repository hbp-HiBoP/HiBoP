# Synthetic MNI contact fixture

Eight entirely synthetic contacts, two patients and two electrodes per
patient. Eight is fixture data, never a product limit.
MNI surfaces and MRI remain those of the repository, pinned by the
[manifest](manifest.json) hashes. No real patient recording or EEG.

The `.patient` files are product inputs; [contacts.json](contacts.json) is the
expected mapping for review and tests. The
[MNI Contacts](MNI Contacts.visualization) visualization references patients by ID,
**MNI** implantation, **MNI Grey matter** surface and both hemispheres.
The project retains left-then-right patient order. Each has A1, A2,
B1, B2, with stable coordinate and site IDs. A1/A2 names are
deliberately repeated across patients to test associations.

## Coordinates and order

Coordinates in `.patient` files are native MNI mm, right-handed.
`Implantation3D.SiteInfo.UnityPosition` applies `ReferenceSystemConversion.ConvertX`:
`(x,y,z) -> (-x,y,z)`. `Site.Information.DefaultPosition` is the captured position,
in the same `hibop-mni-unity-mm-v1` coordinate system as the prepared surface.
The `AssetToBrain` matrix is identity. No Quest world position is transported.

| Native order | Site ID | Patient index | Patient source index | Name / electrode | Native XYZ (mm) | Transported Unity XYZ (mm) |
| ---: | --- | ---: | ---: | --- | --- | --- |
| 0 | `quest-013-site-00` | 0 | 0 | A1 / A | (-31.25, -18.5, 26.75) | (31.25, -18.5, 26.75) |
| 1 | `quest-013-site-01` | 0 | 1 | A2 / A | (-27.5, -15.25, 29.0) | (27.5, -15.25, 29.0) |
| 2 | `quest-013-site-02` | 0 | 2 | B1 / B | (-12.75, 21.5, 42.25) | (12.75, 21.5, 42.25) |
| 3 | `quest-013-site-03` | 0 | 3 | B2 / B | (-9.0, 24.25, 45.5) | (9.0, 24.25, 45.5) |
| 4 | `quest-013-site-04` | 1 | 0 | A1 / A | (22.5, -34.75, 18.25) | (-22.5, -34.75, 18.25) |
| 5 | `quest-013-site-05` | 1 | 1 | A2 / A | (26.25, -31.0, 21.5) | (-26.25, -31.0, 21.5) |
| 6 | `quest-013-site-06` | 1 | 2 | B1 / B | (38.75, 8.25, -12.5) | (-38.75, 8.25, -12.5) |
| 7 | `quest-013-site-07` | 1 | 3 | B2 / B | (42.0, 11.75, -9.25) | (-42.0, 11.75, -9.25) |

Landmarks are asymmetric between hemispheres and axes. They are inside the
prepared MNI's coordinate envelope; this statement does not imply membership
of an anatomical structure or clinical localization.
Appearance comes from the Desktop pipeline: linear RGBA material color,
anatomical sphere diameter (2 mm before gain), prepared visibility.
Fixture gain is 1 and `ShowAllSites=true` keeps all eight contacts visible without ROI.
ROI/filter/blacklist states do not remove any contact from the transported list;
they are captured separately from visibility.

## Reproduction

The `QuestAnatomyFixtureTests.ContactsArchive_LoadsStableSyntheticPatientsAndMniAssociations`
test rebuilds the archive in a temporary directory from this directory's
files, then checks its patients, coordinates and MNI associations.
The iEEG fixture and multimodal projects reuse these same sources through
`SyntheticSceneProjectFixture`. No pre-existing local artifact is required.

For owner review, inspect the table and source files; no headset required.
The [validation overview](../../../../Docs/dev/quest-autonomous/validation.md) describes the scope
of retained campaigns; the original `QUEST-013` manifest remains
[available in Git](../../retained-results.md#finding-earlier-evidence).
