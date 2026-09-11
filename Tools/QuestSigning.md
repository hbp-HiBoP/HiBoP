# Shared Quest development signing

Use `Build-QuestConnectionPlayers.ps1 -Target Android` on both PCs. It validates
the shared signing identity before starting Unity, then signs and verifies the
final APK. Missing/wrong keys fail explicitly. The public certificate SHA-256
is pinned in `QuestSigning.json`; that file contains no private key or password.

The private folder defaults to `%LOCALAPPDATA%\HiBoP\Signing\QuestDevelopment`.
It contains `quest-development.p12`, `signing.json` and `certificate.der`.
The keystore path in `signing.json` is relative to that folder. To use another
location, set `HIBOP_QUEST_SIGNING_DIRECTORY` for the build process.

On the second PC, copy the **entire existing folder** to the same location under
that user's LocalAppData, or use the environment variable. Do not generate a
second key. Run `Tools/Sign-QuestApk.ps1 -ValidateOnly` to confirm the identity.
Keep a protected backup of this folder outside the repository. `signing.json`
contains the development key password in plaintext; protect the copied folder
with permissions restricted to your Windows user, as on the originating PC.
Never put this folder or its password in Git, build reports, or chat messages.

The initial key is created explicitly with `Tools/Initialize-QuestSigning.ps1`.
This command refuses to overwrite a folder or replace a pinned identity. Its
certificate is valid for 36,500 days. Normal builds and switching PCs do not
require renewal or changes to the key. This is a development signing identity,
not a production distribution-key policy.

Unity's intermediate APK uses its normal signing settings; the launcher replaces
that signature and verifies the final APK with Android SDK `apksigner`. It does
not persist signing credentials into Unity settings or build profiles. A direct
Unity Build Profiles build must be passed through `Tools/Sign-QuestApk.ps1 -Apk
<path>` before installation. The adjacent `.apk.signing.json` identifies a final
signed artifact by APK hash and public certificate fingerprint.

For a headset containing an APK signed by another key, `adb install -r` is
insufficient. Recover the original signing key, or explicitly approve a one-time
reinstall after backing up the installed APK and accessible app data/evidence.
Never uninstall automatically after `INSTALL_FAILED_UPDATE_INCOMPATIBLE`.

See [Android's update identity rules](https://developer.android.com/google/play/app-updates)
and [application signing](https://developer.android.com/studio/publish/app-signing).
