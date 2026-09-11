# Quest native math updates

Quest requires both `libhbp_core.so` and `libhbp_math.so`. Android and Desktop
artifacts of each library must use the same scientific source commit.

## Normal GitHub update

Run `Tools/update-native-plugins.cmd` (or `Tools/Update-NativePlugins.ps1`).
`NativePlugins.json` now declares 11 artifacts: three for EEGFormat, four each
for hbp_core and hbp_math, including Android ARM64. The updater requests each
repository's `native.yml` workflow with `platform=all` and an exact source SHA.
It validates the manifests and hashes, then installs all plugins and their lock
as one transaction, preserving Unity `.meta` files and restoring the previous
installation if a step fails. `-Resume` also supports restoring interrupted
updates created before Android math was added.

The Android workflow changes in hbp_math must be committed and pushed to the
configured `master` branch before this remote path can run. The updater checks
for Android workflow support before dispatch. Local edits alone are insufficient.

## Import a locally built Android package

Build through `hbp_math/tools/Invoke-NativeBuild.ps1 -Platform Android` using the
full hbp_math commit from HiBoP's `Tools/NativePlugins.lock.json`. See hbp_math's
README for the pinned NDK/CMake setup and fresh output directories. Then run:

```powershell
./Tools/Update-NativePlugins.ps1 -AndroidPackage C:/HBP/Software/hbp_math/out/package/android-release
```

The same option accepts an hbp_core package. The library is selected from the
package manifest, restricted to hbp_core and hbp_math. The importer checks the
installed library's hashes and source commit, replaces only its Android plugin,
and updates only that Android lock entry. It preserves Desktop artifacts and
other libraries; the lock and previous Android payload are restored on failure.
Unity must be closed during installation. No APK or headset data is modified.

`Build-QuestMathPlugin.ps1` remains the earlier qualification bootstrap. Its
`manifest.json` is not an updater package; prefer the hbp_math repository build,
which produces `artifact-manifest.json`. The existing qualification APK is not
replaced by these tooling changes. Rebuild and qualify Quest after an actual
plugin update. Native ABI/ELF checks do not replace scene runtime validation.
