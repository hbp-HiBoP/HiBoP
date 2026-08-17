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

CI explicitly builds all desktop players with IL2CPP. The macOS job also
installs Unity's `mac-il2cpp` module. The local build window keeps its historic
defaults (Windows/Linux IL2CPP, macOS Mono) and provides an IL2CPP toggle for
each selected platform.

## One-time setup

1. In the GitHub repository, open **Settings > Secrets and variables > Actions**.
2. Create the `UNITY_USERNAME` and `UNITY_PASSWORD` repository secrets using a
   Unity ID with a Personal licence.
3. In **Settings > Actions > General**, verify that workflows may create or edit
   release content. The workflow requests `contents: write` only to attach
   archives to a release.
4. Before the first release, verify that the Release native plugins are tracked:
   `hbp_core` must be present for Windows, Linux and macOS ARM64.

Secrets must never be committed or printed in logs. A dedicated Unity ID for CI
is preferable so that licence activation does not affect a local editor.

## Triggering builds

- **Manual test:** open **Actions > Build HiBoP > Run workflow**, select the
  `develop` branch, then run it. The three archives are available as artifacts
  on the workflow run; no release is created or modified.
- **Release:** publish a GitHub Release. The same archives are attached to that
  release after all three builds finish.

Each archive is named after the generated build directory:
`HiBoP.<application-version>.<platform>.zip`. With the current application
version, the expected names are `HiBoP.5.0.9.win64.zip`,
`HiBoP.5.0.9.linux64.zip` and `HiBoP.5.0.9.macos64.zip`.

The first build for each target takes longer because Unity installs the editor
and imports the project. The `Library` cache is reused afterwards, but is
invalidated when `Assets`, `Packages` or `ProjectSettings` change.
