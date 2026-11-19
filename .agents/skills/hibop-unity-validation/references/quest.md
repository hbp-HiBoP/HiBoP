# Quest preparation and acceptance

1. Prepare software, fixtures and the shortest useful recipe before requesting
   headset use. Reuse a build only when its relevant inputs remain unchanged.
   Identify the actual Desktop Editor/Player process; a process launch does not
   prove that a usable window is visible.
2. Apply the [build/signing contract](../../../../Docs/agent-knowledge/builds-and-distribution.md). Check the
   existing Unity signing configuration without printing credentials. Missing
   credentials require local Publishing Settings configuration by the user;
   continue independent work. Do not replace the key or re-sign an APK.
3. Use the existing builder, through MCP for an open editor or escalated CLI for
   a closed one. Inspect build completion/logs and `Tools/Test-QuestApk.ps1` output
   when applicable. APK existence alone is not successful validation.
4. For development ADB-over-Wi-Fi use `Tools/Connect-QuestAdbWifi.ps1`. Inspect
   actual device/authorization state outside the Windows sandbox; do not hardcode
   previous addresses or adb paths. If unavailable, check that the headset is
   awake/worn/connected and ask after a few bounded attempts. ADB is development
   tooling; the application uses NCM/mDNS as described in [Synchronization](../../../../Docs/agent-knowledge/synchronization.md).
5. State when the headset is needed and can be removed. Give short actions with
   observable expected outcomes; collect logs yourself. Explicitly request needed
   manual checks before declaring them unavailable. An explicit deferral remains
   deferred. Allow enough observation time; fast automation alone does not prove
   visual acceptance. Preserve initial verdicts and record retests separately.
6. Preserve installed data and pairing identity. A signature mismatch follows
   the signing recovery contract; never automatically uninstall the application.
7. Once the test is validated and no observation is pending, collect evidence,
   then stop only the tested HiBoP package to save battery. For manual tests wait
   for the user's verdict before stopping. Identify the actual device and APK
   package; the current Quest package is `fr.crnl.hibop.quest`:

```text
adb -s <serial-or-IP:port> shell am force-stop <tested-package>
adb -s <serial-or-IP:port> shell pidof <tested-package>
```

No PID confirms the stop (`pidof` exit 1 is normal for an absent process). Record
the result; if disconnected, report the stop as unverified. Do not stop the ADB
server, system services or other applications.

### Development headset power and transport

For the development headset, `Tools/Connect-QuestAdbWifi.ps1 -KeepAwakeWhilePluggedIn`
keeps the proximity override and awake-on-power configuration between tests;
stopping HiBoP does not request headset sleep. Use `-NoProximityOverride` on another
headset or when normal use is requested. To restore normal mode on the configured
headset, explicitly target it with `shell svc power stayon false` and
`shell am broadcast -a com.oculus.vrpowermanager.automation_disable`.
After reboot, rerun the connection script: TCP ADB and overrides may not persist.

Use compatible ADB client versions; avoid automatic server restarts. Unity's
External Tools options `Kill ADB server on exit` and `Kill external ADB instances`
should be disabled for this development session. After Wi-Fi connection use
`adb -s <IP:port>`; `adb -d` selects USB only. These device operations run outside
the Windows sandbox and apply only to the identified development device.

For NCM qualification, scope checks to the requested OS and affected behavior.
Do not repeat an entire Windows campaign just to qualify Mac/Linux, or import old
prototype requirements into a changed scope. Establish present evidence; a dated
report does not prove the current build. Record source/build/fixture, logs/results,
remaining manual checks and failures separately; no implicit remote CI, publishing
or distribution deployment is authorized by a local validation task.
