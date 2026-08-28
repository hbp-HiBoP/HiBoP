# GitHub Actions builds

`build.yml` builds HiBoP for Linux x64, Windows x64 and macOS ARM64 when a
GitHub Release is published, and on demand. Each target is built on its native
runner: this guarantees that the macOS player is ARM64 and avoids a dependency
on cross-platform IL2CPP toolchains.

The workflow calls `HBP.Dev.HBPBuilder.BuildFromCommandLine`. That entry point
reuses `HBPBuilder.BuildProjectAndZipIt`, so CI archives contain the same files
as a build run from `Tools/Build HiBoP`: it copies `Assets/Data`, removes `.meta`
and `.obj` files, excludes Localizer atlases, copies the documentation and
processes the macOS/Linux plugins.

## Native plugin update from a Windows workstation

No macOS or Linux workstation is required to update the native plugins. Install
[GitHub CLI](https://cli.github.com/) once, authenticate with `gh auth login`,
then run from the HiBoP checkout:

```powershell
.\Tools\update-native-plugins.cmd
```

The script resolves and pins the latest commit on GitHub `master` for
`EEGFormat`, `hbp_core` and `hbp_math`. It dispatches the three native workflows,
waits for all nine Windows x64, Linux x64 and macOS ARM64 artifacts, validates
their manifests and SHA-256 hashes, and only then replaces the Unity payloads.
The existing Unity `.meta` files are never replaced.

Unity may remain open while GitHub builds and the script downloads artifacts.
It must be closed for the final installation. If Unity is still open, the
validated request is preserved and the script prints its identifier. Close
Unity and resume without rebuilding:

```powershell
.\Tools\update-native-plugins.cmd -Resume <request-id>
```

The operation stops before installation when any workflow or artifact
validation fails. Before replacing files it saves all current payloads under
`.native-plugin-update/<request-id>`, and restores them if installation fails.
On success it writes `Tools/NativePlugins.lock.json` with the source commits,
GitHub run URLs and installed hashes. Commit that lock file together with the
nine native payloads, then run **Build HiBoP** with `platform: all`.

The local validation suite does not use GitHub or modify the real plugins:

```powershell
pwsh .\Tools\Test-NativePluginUpdater.ps1
```

The macOS runner validates the ARM64 binaries and recreates ad-hoc signatures
before Unity imports the bundles. Consequently, copying the bundle through a
Windows checkout does not require `chmod`, `codesign`, a Mac, or any additional
local preparation. Native library CI never modifies the HiBoP repository, and
the HiBoP workflow never rebuilds these libraries.

Before packaging, each job verifies that exactly one platform-specific copy of
`hbp_core`, `hbp_math` and `EEGFormat` is present in the final player, in Unity's
expected location for that platform. The checks also reject the editor-only
`hbp_export`, OpenCV and Boost dependencies. Linux additionally runs `ldd`;
macOS checks the ARM64 slices and native dependencies, then ad-hoc signs and
verifies the completed `.app` after all plugin post-processing. This signature
makes CI artifacts internally consistent; public macOS releases still require
a Developer ID signature and notarization.

CI explicitly builds all desktop players as IL2CPP release builds. HBPBuilder
keeps the Player log enabled, records managed stack traces for errors and
exceptions, removes stack traces from routine release logs, and includes the
GitHub commit in `BuildInfo.json`. The macOS job also installs Unity's
`mac-il2cpp` module. The local build window keeps its historic backend defaults
(Windows/Linux IL2CPP, macOS Mono) and provides an IL2CPP toggle for each
selected platform. Command-line builds are release builds by default; pass
`-developmentBuild` explicitly when a diagnostic development player is needed.

The Linux runner installs Unity Hub from Unity's Debian repository and creates
the headless wrapper expected by `unity-setup`. It resolves the actual package
path dynamically because Unity Hub 3.20+ uses `/usr/lib/unityhub`, while the
action still defaults to the obsolete `/opt/unityhub` location.

## One-time setup

1. In the GitHub repository, open **Settings > Secrets and variables > Actions**.
2. Create the `UNITY_USERNAME` and `UNITY_PASSWORD` repository secrets using a
   Unity ID with a Personal licence.
3. In **Settings > Actions > General**, verify that workflows may create or edit
   release content. The workflow requests `contents: write` only to attach
   archives to a release.
4. Before the first release, verify that the Release native plugins and their
   Unity `PluginImporter` metadata are tracked: `hbp_core` must be present for
   Windows, Linux and macOS ARM64. The workflow fails if the selected plugin is
   missing from the generated player.

Secrets must never be committed or printed in logs. A dedicated Unity ID for CI
is preferable so that licence activation does not affect a local editor.

## Triggering builds

- **Manual test:** open **Actions > Build HiBoP > Run workflow**, select the
  `develop` branch and a platform (`all` by default), then run it. The selected
  archives are available as artifacts on the workflow run; no release is
  created or modified.
- **Release:** publish a GitHub Release. The same archives are attached to that
  release after all three builds finish.

Each archive is named after the generated build directory:
`HiBoP.<application-version>.<platform>.zip`. With the current application
version, the expected names are `HiBoP.5.0.9.win64.zip`,
`HiBoP.5.0.9.linux64.zip` and `HiBoP.5.0.9.macos64.zip`.

The first build for each target takes longer because Unity installs the editor
and imports the project. The `Library` cache is reused afterwards, but is
invalidated when `Assets`, `Packages` or `ProjectSettings` change.
