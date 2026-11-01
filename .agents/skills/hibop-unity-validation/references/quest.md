# Quest build and device validation

1. Prepare the software, fixture, and shortest useful test sequence before asking
   the user to wear the headset. Reuse an applicable build when its relevant inputs
   are unchanged. State the actual Desktop process (Editor or Player) used for the
   test; do not infer a visible, usable window from a successful process launch.
2. Before building, read `Tools/QuestSigning.md`. Use the existing Unity Quest
   profile's signing identity. Check configuration readiness without logging or
   requesting passwords in chat. If credentials are missing, direct the user to
   Unity's Publishing Settings and continue independent work. Do not generate a
   replacement key, re-sign an APK, or use the historical signing initializer.
3. Use the project's existing builder and packaging validator. For an open editor,
   use MCP. CLI launchers are for a closed editor and follow the escalated-execution
   rule. Inspect the build result, log, and `Tools/Test-QuestApk.ps1` result when
   applicable; do not infer success from an output file alone.
4. For development ADB Wi-Fi, use `Tools/Connect-QuestAdbWifi.ps1`. If unavailable,
   check the headset's wake/connection state and ask promptly after a few bounded
   attempts. Do not hardcode a previous IP or this machine's adb.exe path. ADB is
   development tooling; the current application transport is NCM/mDNS, documented
   in `Docs/dev/quest-autonomous/usb/README.md`.
5. Tell the user when to put the headset on and when it can be removed. Give short
   actions with visible expected outcomes. Collect logs yourself. If manual
   validation is required, prepare and explicitly request it before reporting
   that it remains unavailable; a user-requested deferral stays a deferral.
6. Preserve installed application data and pairing identity. On signature mismatch,
   follow `Tools/QuestSigning.md`; do not automatically uninstall the application.
7. After the test session is validated and no observation is still pending, collect
   evidence and apply the existing battery/stop procedure in
   `Docs/dev/quest-autonomous/TASK-WORKFLOW.md`, section “Batterie et fin des essais
   sur Quest”. Stop only the tested HiBoP package and verify the result. A deferred
   or unavailable stop must be reported accurately.

For NCM qualification, read the current USB qualification/handoff documents only
for the requested platform. Reuse their established results and retest affected
paths; do not repeat the entire Windows campaign for Mac/Linux. Older prototype
requirements do not override the latest explicit scope.
