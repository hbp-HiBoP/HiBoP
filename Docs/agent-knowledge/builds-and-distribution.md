# Builds and distribution

Read for product version changes, Desktop/Quest packaging, release preparation
or native plugin updates. Use the validation skill when executing Unity/builds.

## Product version and player builds

[`codemeta.json`](../../codemeta.json) owns the product version.
`Tools/Change version` edits that source and propagates it to global PlayerSettings
and build profiles; `HBPBuilder` reapplies it before building and producing build
metadata. Do not patch a profile or `BuildInfo` independently to fix a mismatch.
See [HBPBuildProfiles](../../Assets/Scripts/HBP/Dev/Editor/HBPBuildProfiles.cs).

Keep `HBPBuilder` usable locally and in CI. Release builds retain Player logs and
managed stack traces for errors/exceptions, omit routine log stacks and record
source identity in `BuildInfo.json`. Development is an explicit diagnostic choice.
User-facing release text and README content are English;
this does not prescribe the language of design notes or conversation.

Localizer atlases are distributed separately and must stay out of ordinary player
packages on every platform. Do not include them to repair a missing test fixture.
A technical Quest `IncludeLocalizers` opt-in exists; its existence is not a reason
to enable it during a normal build. Installed atlas references and scene-transfer
integrity are governed by [Runtime and state](runtime-and-state.md).

## Native artifacts and signing

Versioned tools must be reproducible from versioned sources/configuration and
declared toolchains or hash-pinned dependencies. Never require an unexplained
pre-existing `.artifacts` build, fixture, capture or workstation-specific path.
Generated outputs/caches are allowed when the tool creates or acquires them;
builds and measurements being inspected must be explicit inputs. Prefer the
owning native repository's build tooling and `Assets/Tests` for product tests.

Use the [native updater](../../Tools/Update-NativePlugins.ps1) and its help, not
manual copies. Quest requires both `libhbp_core.so` and `libhbp_math.so`.
For each library, the Desktop/Android pair being qualified must use the same
scientific source commit; untouched platforms retain their own recorded provenance.
`NativePlugins.json` and its lock own targets, source revisions and hashes; do not
copy historical artifact counts or workstation paths into a new workflow.

Unity may remain open during remote acquisition but must be closed for plugin
replacement. Preserve `.meta` files and the updater's validation/rollback behavior.
The updater pins exact remote source SHAs, requests the declared native workflows,
validates manifests/hashes and installs payloads plus lock transactionally. Failed
acquisition must not change the installation; failed installation restores the
previous payloads and lock. If the editor is open, retain the validated request and
resume with `-Resume <request-id>` after it closes, rather than rebuild artifacts.

`-AndroidPackage <directory>` imports an `artifact-manifest.json` package for
hbp_core or hbp_math only, built from that library's pinned Desktop source commit.
It validates installed Desktop hashes and package metadata, updates only the
Android payload/lock entry, and preserves other platforms/libraries. A bootstrap
`manifest.json` is not an updater package. Remote workflows must actually support
the declared targets in the configured remote revision; local unpushed changes
do not enable that capability. No implicit commit/push is authorized.

The separate `-LocalCorePackages` mode installs exactly Windows and Android
hbp_core packages from one full source SHA, transactionally. It may advance that
pair while retaining macOS/Linux payloads: their per-artifact `sourceCommit`
remains authoritative. Never infer that all platforms were rebuilt from the
library-level lock commit after a partial update. This mode cannot be combined
with another import/resume mode or selection parameters.

Use `Tools/Test-NativePluginUpdater.ps1` and
`Tools/Test-NativePluginAndroidUpdater.ps1` for network-free tooling checks;
these do not prove a remote build or scientific runtime acceptance. Rebuild and
qualify affected Quest behavior after a real plugin update.

## CI and packaging contracts

Preserve manual `workflow_dispatch` and explicitly published release triggers;
do not add push/pull_request triggers without an explicit request. Desktop builds
target Windows x64, Linux x64 and macOS ARM64 on their native runners. CI uses
IL2CPP Release; the local build window defaults Windows/Linux to IL2CPP and macOS
to Mono, with a per-target IL2CPP toggle. `platform: quest` is a separate manual
Android IL2CPP/ARM64 job: neither `all` nor release publishing selects it, and it
does not attach an APK to a release. Manual builds do not publish releases.

`HBP.Dev.HBPBuilder.BuildFromCommandLine` is the shared CLI entry point. Select
the existing build profile, output and log explicitly; use `-developmentBuild`
only for a diagnostic player. Both profiles use Input System only. Desktop
packaging copies data/documentation, removes `.meta`/`.obj` and excludes Localizers;
Quest embeds scientific resources through its standard-data builder. Cache
Library by platform and keep Quest/Gradle caches separate from Desktop.

Final players must contain exactly one appropriate native library per supported
family/platform in Unity's expected location; reject Desktop binaries in Android
and editor-only hbp_export/OpenCV/Boost dependencies. Validate Linux dependencies
with `ldd` and macOS ARM64/dependencies. macOS runner processing re-signs native
bundles before import and the complete app after post-processing; no manual
Windows-side signing is required. Ad-hoc CI signing does not replace Developer ID
signing/notarization for public macOS distribution. HiBoP CI consumes native
artifacts; native CI does not modify HiBoP. Derive target lists from configuration,
not an old artifact count or versioned archive example.

For CI setup/repair, the [build workflow](../../.github/workflows/build.yml)
consumes repository secrets `UNITY_USERNAME` and `UNITY_PASSWORD`; configure
values privately, never in Git or logs. Release archive attachment requires
release-content write permission; manual builds still do not publish releases.
The Linux Unity Hub headless wrapper resolves the installed package path
dynamically; preserve that discovery instead of assuming an old `/opt/unityhub`
layout or pinning a workstation-specific path in instructions.

## Quest signing and archive validation

Use the existing private key configured in Unity's Quest Build Profile / Android
Publishing Settings. Preserve the APK signature Unity produces; do not re-sign,
generate a replacement key or use a historical initializer for normal builds.
When changing computer, transfer the same private key securely and configure
credentials locally. A historical signing JSON does not override Unity's identity.
Never version or log keys/passwords; inspect readiness without exposing values.

On `INSTALL_FAILED_UPDATE_INCOMPATIBLE`, recover the original key or obtain
explicit approval for a one-time reinstall after backing up the installed APK and
accessible app data/evidence. `adb install -r` cannot solve differing keys;
do not uninstall automatically.

`HBPAndroidPackaging` recreates the final APK archive in Debug and Release while
leaving compilation/resource work incremental, preventing unused ZIP regions from
incremental builds or cached Library state. Gradle still aligns/signs the APK.
Run `Tools/Test-QuestApk.ps1` after direct profile builds as well as scripted builds;
inspect its report and ELF/content checks. Its allowed archive overhead is
1 MiB plus 64 KiB per entry, not a cap on scientific data size. Use
the `hibop-unity-validation` skill for execution and headset acceptance.
