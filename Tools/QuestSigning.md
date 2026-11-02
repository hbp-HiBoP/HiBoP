# Quest signing in Unity

Use only the existing keystore configured in Unity's Quest Build Profile / Android
Publishing Settings. `HBPBuilder.BuildQuest` leaves the APK signature produced
by Unity unchanged. The former
post-build `Sign-QuestApk.ps1` script has been removed: replacing Unity's signature
made updates incompatible with the application already installed on the headset.

The current profile references `.android/hibop-quest.keystore` and alias
`hibop-quest`. Keep that existing private key and its passwords outside Git and
logs. When moving to another PC, copy the same key and configure its credentials
in Unity. Do not generate a replacement key for normal builds. The obsolete
signing-folder initializer and its historical identity JSON have been removed.

## APK packaging and size checks

`HBPAndroidPackaging` configures Gradle's APK packaging tasks for a full archive
assembly on every build, in both Debug and Release. This Unity build callback
also runs for Build Profiles, command-line/CI builds, and exported Gradle projects.
Compilation and resource processing remain incremental. Only the final APK is
recreated, so Zipflinger cannot retain large unused regions from earlier builds
or a restored CI Library cache. Gradle still performs alignment and signing.

`Tools/Test-QuestApk.ps1` checks the difference between the APK file size and the
sum of its compressed entries, allowing 1 MiB plus 64 KiB per entry for ZIP
metadata, alignment and signatures. The GitHub Quest workflow runs this check
before handing off the APK; run it explicitly after a local build. The JSON report records
the content size, overhead and allowed overhead; there is no fixed limit on
scientific data size. Run this validator as well after a direct Build Profiles
build, without re-signing its APK.

For a headset containing an APK signed by another key, `adb install -r` is
insufficient. Recover the original signing key, or explicitly approve a one-time
reinstall after backing up the installed APK and accessible app data/evidence.
Never uninstall automatically after `INSTALL_FAILED_UPDATE_INCOMPATIBLE`.

See [Android's update identity rules](https://developer.android.com/google/play/app-updates)
and [application signing](https://developer.android.com/studio/publish/app-signing).
