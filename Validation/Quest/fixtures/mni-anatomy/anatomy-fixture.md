# MNI anatomy fixture

Project `quest-mni-anatomy`, visualization and single column `MNI Anatomy`, without any saved patient, group, dataset, site, cut or view. It selects `MNI Grey matter`, both hemispheres, the anatomical representation and `MNI` MRI. The six fixed IDs and eight source hashes are in [manifest.json](manifest.json).

The `QuestAnatomyFixtureTests` EditMode tests directly read versioned sources
from this directory and load the full anatomy through `MNIObjects`.
They require neither a prepared `.artifacts` archive nor a campaign launcher.
The manifest retains provenance of the earlier deterministic archive;
its creation procedure remains available in Git.

The reference is `Assets/Data/IRM/MNI.nii`, `MNI.trm` and the four gray/white GII files in `Assets/Data/Meshes` at commit `b230166204024b5dd350d8fe897aa854160ff384`. Exact upstream scientific origin is not inferred from the MNI name. Small GII files under `Assets/Tests/Fixtures/Native/Meshes` are synthetic and do not constitute this full brain. This fixture adds no signal or patient directory; that does not classify the reference volume as synthetic.

The shared `MNIObjects.LoadDataAsync` loader loads NIfTI/GII/TRM, adapts winding, assembles hemispheres and computes normals. Distances remain in mm with the existing native/Unity X conversion; this fixture adds neither Quest mm→m conversion nor an XR camera.

These tests do not prove hardware rendering. A deterministic archive does not imply a bit-identical Player, notably because of the build date. A full build on a second PC had not been run during the original campaign; its `QUEST-001` manifest remains [available in Git](../../retained-results.md#finding-earlier-evidence).
