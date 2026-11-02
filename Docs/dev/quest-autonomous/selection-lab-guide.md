# Quest selection laboratory

The laboratory is an independent application for comparing selection profiles on a fixed MNI. It does not synchronize with a Desktop. Current code announces **Campaign v5**, field `Build=<product version>/selection-lab-5`; earlier campaign versions remain attached to their measurements.

## Building and using

Follow the [Quest workflow](../../../.agents/skills/hibop-unity-validation/references/quest.md), activate the Quest profile, then `HiBoP > Quest > Build selection laboratory`. The scene is `QuestSelectionLab`, package `fr.crnl.hibop.quest.selectionlab`, output `.artifacts/quest-selection-lab/HiBoP.SelectionLab.apk`. The package is distinct from HiBoP. Do not attribute the laboratory's choice to disable activity projection to the product.

| Control | Use |
| --- | --- |
| Hold right A | Continuous selection with the probe. |
| Method | Contact, depth tolerance or ray; does not move sites. |
| Start / retry | Starts a trial on the current layout. |
| Validate / next target | Finishes/advances according to the layout; does not necessarily mean acquisition failure. |
| Compare selection A/B | Recalls two saved configurations on the same sites. |
| Hold left X for two seconds | Recenters brain and windows; keep comparable scale/position afterwards. |

The R1→R8 row advances automatically on correct acquisition while A is held. Other layouts use validation commands. The target marker can be hidden; record this setting in the comparison.

## Reproducible layouts

[QuestSelectionLayouts](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionLayouts.cs) provides six cases: isolated site, eight-site row, close neighbors, two depths, dense group and a cloud of **2,000 sites**. The cloud uses `System.Random(20261029)` within the **MNI bbox**, not within cortical volume. Guided targets are selected from the half exposed by the cut; all sites remain active in the layout and can be explored. This population does not represent a clinical implantation.

Compare one parameter at a time, with the same layout, pose, scale and target. Alternate A/B order between blocks/users. A suggested short series is 20 guided targets per profile in the cloud, then free exploration from several viewpoints; it does not replace a controlled ergonomic study.

## Settings and interpretation

The [current profile](interactions.md#site-selection) is Depth, contact 3 mm, depth ±10 mm, attraction 3 mm and stabilization 0.15. Radii are **physical radii**, not diameters or anatomical millimeters after scaling. With 3 mm contact and attraction, lateral search can reach 6 mm before adding site radius, while priority contact remains at 3 mm. The Ray cone depends on its length and half-angle; a site exactly behind another may require moving the viewpoint.

Sphere, Halo, Cage and Stylus change the visual indication, not scientific colliders or attraction parameters. The visual preset is a white halo with a 4 mm radius. The stylus adds a 40 mm shaft whose tip corresponds to the selection point. Available colors are white, amber and cyan. The visible ball remains independent of contact radius.

X/Y/Z offsets are physical in the aim coordinate system: X right, Y up and Z forward. They move the cursor and selection origin together; menu rays remain unchanged. The default is Z=+30 mm. Settings, styles and offsets are persisted and recalled by A/B. The yellow diagnostic volume does not include site radius.

Stabilization is **normalized-score** hysteresis, without dwell or retention outside the volume. `StabilizedFrames` counts frames in which it retains the previous candidate; it is neither the number of errors avoided nor a success rate.

## Logs and analysis

[QuestSelectionLab](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionLab.cs) writes to `Application.persistentDataPath/selection-lab`: `settings.json` and JSONL logs. Use [Export-QuestSelectionLab.ps1](../../../Tools/Export-QuestSelectionLab.ps1) to extract data. Retain version, session, layout, full profile, target, selection, result, time, transitions and feedback indicators. A field absent from an older format remains unknown, not zero.

Distinguish **first target acquisition**, duration until trial termination and termination reason. `next-target` can occur after correct acquisition and is not automatically a failure. Transitions between sites do not yield a false-positive rate without a reference trajectory and intent. An average rate near 72 Hz does not measure GPU headroom or worst frames.

The historical campaign of **October 29, 2026, v2**, comprises **173 trials / 1,093 events**, one subject and a non-counterbalanced order. The first version's 107 events affected by target placement are excluded. These observations informed depth/attraction settings and instrumentation; they do not support a general profile ranking or proof of statistical superiority. Stabilization fields added later cannot be reconstructed for v2. Local data is referenced under `Logs/SelectionLab/campaign-20261029`; its presence in another clone is not guaranteed.

Performance of the 2,000 sites during prolonged use and ergonomics across multiple users remain to be measured; neither this guide nor compilation declares them qualified.
