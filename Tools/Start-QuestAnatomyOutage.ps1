# Deliberate radio test, only after the owner is ready and anatomy has been received.
# The detached on-device script restores Wi-Fi without relying on the ADB connection.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
$run = "$repo/.test-results/quest-012/outage-$runId"
New-Item -ItemType Directory -Path $run | Out-Null
$deviceScript = "/data/local/tmp/quest012-outage-$runId.sh"
$deviceLog = "/data/local/tmp/quest012-outage-$runId.log"
$scriptText = @'
#!/system/bin/sh
set -e
trap 'svc wifi enable' EXIT HUP INT TERM
sleep 2
echo BEFORE
date -u +%Y-%m-%dT%H:%M:%SZ
cat /proc/uptime
pidof fr.crnl.hibop.quest
svc wifi disable
sleep 1
cmd wifi status
cmd wifi status | head -n 1 | grep -q 'Wifi is disabled'
echo DISABLED
cat /proc/uptime
sleep 60
echo RESTORING
cat /proc/uptime
svc wifi enable
sleep 10
cmd wifi status
pidof fr.crnl.hibop.quest
echo COMPLETE
'@
[IO.File]::WriteAllText("$run/outage.sh", $scriptText.Replace("`r`n","`n") + "`n", (New-Object Text.UTF8Encoding $false))
$processId = & $AdbPath -s $Serial shell pidof fr.crnl.hibop.quest
if ($LASTEXITCODE -ne 0 -or -not $processId) { throw 'Launch HiBoP and receive anatomy before the outage.' }
$recent = @(& $AdbPath -s $Serial logcat -d -v brief "--pid=$($processId.Trim())" -s Unity)
if ($LASTEXITCODE -ne 0 -or -not ($recent -match 'QUEST012_SAMPLE .*"ready":true')) { throw 'No instrumented ready anatomy observed in this process.' }
& $AdbPath -s $Serial push "$run/outage.sh" $deviceScript
if ($LASTEXITCODE -ne 0) { throw 'Could not stage the restoration script.' }
# All remote paths above are generated solely from fixed prefixes and a timestamp.
& $AdbPath -s $Serial shell "nohup sh $deviceScript >$deviceLog 2>&1 </dev/null &"
if ($LASTEXITCODE -ne 0) { throw 'Could not dispatch the outage script.' }
@{dispatchedAt=[DateTime]::UtcNow.ToString('O'); serial=$Serial; processId=$processId; deviceScript=$deviceScript; deviceLog=$deviceLog; minimumDisabledSeconds=60; result='PENDING'} |
    ConvertTo-Json | Set-Content -LiteralPath "$run/dispatch.json" -Encoding UTF8
Write-Output "Dispatched; continue the gestures. Wi-Fi restoration is scheduled on the headset. After about 75 seconds, collect $deviceLog via adb pull into $run/outage.log and verify DISABLED/RESTORING uptime delta >= 60, COMPLETE, same PID, and reconnection. Dispatch alone is not a passing result."
