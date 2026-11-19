# Quest builds and distribution

The reference for build, version, native plugin and signing rules is [Builds and distribution](../../agent-knowledge/builds-and-distribution.md). Unity/ADB execution procedures are in the [hibop-unity-validation](../../../.agents/skills/hibop-unity-validation/SKILL.md) skill, notably its [Quest reference](../../../.agents/skills/hibop-unity-validation/references/quest.md). This page describes product boundaries; it does not replace those procedures.

## Targets and resources

The Quest profile produces an Android **ARM64/IL2CPP/OpenXR** Player from the same project as Desktop. Desktop profiles do not enable the Quest XR runtime. Read the Unity version from `ProjectSettings/ProjectVersion.txt` and product version from `codemeta.json`, without copying those from earlier campaigns. The shared CLI entry point is `HBP.Dev.HBPBuilder.BuildFromCommandLine`; explicitly select profile, output and log.

Quest uses `libhbp_core.so` and `libhbp_math.so`. Prepared-data transfer does not require bundling EEGFormat as a raw-data importer on Android. Qualified Desktop/Android pairs must retain the same scientific provenance per library; other platforms keep their own recorded revisions. [Update-NativePlugins.ps1](../../../Tools/Update-NativePlugins.ps1), its configuration and lock file are the sources of truth, not an earlier library count or workstation path.

The first shared-scene integration revealed that `hbp_math` was missing from the APK: the archive arrived, then resetting a static column failed in `MathDLL.Mean`. The [retained native check](../../../Validation/Quest/retained-results.md#android-native-loading) verifies loading and elementary computations of the corrected binary, without by itself qualifying a Unity scene. Checking network reception therefore does not replace checking native dependencies used after reception.

Standard scientific references are prepared by the Quest builder. Compressed files intended for Unity use, among other extensions, `.gz.bytes`. Localizers atlases are distributed separately and excluded from ordinary packages; the technical option `QuestStandardDataSettings.IncludeLocalizers` is opt-in, not the normal setting.

## APK and identity

Use the private key already configured in the Quest profile/Android Publishing Settings and preserve Unity's generated signature. Earlier signing initializers do not define the current identity. Moving to another machine requires the same key, transferred outside Git without exposing its secrets.

[HBPAndroidPackaging](../../../Assets/Scripts/HBP/Dev/Editor/HBPAndroidPackaging.cs) forces recreation of the final `PackageApplication` archive in Debug and Release while retaining incremental compilation. This avoids unused ZIP regions; Gradle retains alignment and signing. [Test-QuestApk.ps1](../../../Tools/Test-QuestApk.ps1) checks the archive and its ELF files after direct or scripted builds. The accepted envelope margin is **1 MiB + 64 KiB per entry**, not a limit on scientific data.

An incompatible installation signature cannot be fixed with `adb install -r`. Recover the original key; any reinstallation with potential data loss follows the explicit agreement and backup required by the distribution reference.

## Network and platform scope

The product workflow uses LAN/Wi-Fi or USB NCM and in-app pairing. ADB remains a development tool, absent from product packages. [QuestDistributionValidation](../../../Assets/Scripts/HBP/Dev/Editor/QuestDistributionValidation.cs) checks pinned mDNS dependencies, removes the old USB/ADB distribution directory and rejects ADB executables. macOS receives network/Bonjour privacy keys before final signing.

Desktop CI targets Windows x64, Linux x64 and macOS ARM64. Quest is a separate manual job; `all` and release publication do not mean APK publication. A build, ELF inspection or ad-hoc signature does not prove connection qualification, public notarization or scientific parity. The [USB matrix](usb-and-network-connectivity.md#qualification-by-platform) preserves the distinction between historically qualified Windows and macOS/Linux still lacking physical qualification.
