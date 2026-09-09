# Run outside the Windows sandbox. Local reconstruction of a Desktop export, not a TLS proof.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$Fixture,
    [string]$Apk = "$PSScriptRoot/../.artifacts/quest-017/Android/HiBoP.Quest.apk",
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$run = "$repo/.test-results/quest-017/device-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$commands = [Collections.Generic.List[object]]::new()
$verified = $false
function Invoke-QuestAdb([string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    $commands.Add(@{arguments=$Arguments; exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')})
    if ($code -ne 0) { throw "ADB $($Arguments[0]) failed (exit $code): $output" }
    return $output
}
try {
    Invoke-QuestAdb @('get-state') | Out-Null
    $model = (@(Invoke-QuestAdb @('shell','getprop','ro.product.model')) -join '').Trim()
    if ($model -notmatch 'Quest') { throw "Expected Quest, found $model" }
    $verified = $true
    & "$PSScriptRoot/Test-QuestApk.ps1" -Apk $Apk -ReportPath "$run/apk-content.json"
    Invoke-QuestAdb @('install','-r',$Apk) | Set-Content "$run/install.log"
    $installed = @(Invoke-QuestAdb @('shell','pm','path',$package)) | Where-Object { $_ -match '^package:.*/base\.apk$' } | Select-Object -First 1
    if (-not $installed) { throw 'Installed base APK missing.' }
    $apkHash = (Get-FileHash -LiteralPath $Apk).Hash.ToLowerInvariant()
    $installedHash = (@(Invoke-QuestAdb @('shell','sha256sum',$installed.Substring(8).Trim())) -join ' ').Split(' ')[0]
    if ($installedHash -ne $apkHash) { throw 'Installed APK hash mismatch.' }
    $id = [Guid]::NewGuid().ToString('N')
    [IO.File]::WriteAllText("$run/marker.txt", $id)
    Invoke-QuestAdb @('shell','am','force-stop',$package) | Out-Null
    Invoke-QuestAdb @('shell','mkdir','-p',$remote) | Out-Null
    Invoke-QuestAdb @('push',$Fixture,"$remote/quest017-projection.hbna") | Out-Null
    Invoke-QuestAdb @('push',"$run/marker.txt","$remote/quest017-projection-probe") | Out-Null
    $activity = @(Invoke-QuestAdb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    if (-not $activity) { throw 'Quest activity missing.' }
    $launch = @(Invoke-QuestAdb @('shell','am','start','-W','-n',$activity.Trim()))
    $launch | Set-Content "$run/launch.log"
    if ($launch -match 'LaunchCheckControllerRequired') { throw 'Wake Quest controllers and dismiss the system launch prompt.' }
    $deadline = [DateTime]::UtcNow.AddSeconds(100)
    do {
        Start-Sleep -Seconds 2
        $processId = (@(Invoke-QuestAdb @('shell','pidof',$package)) -join '').Trim()
        $lines = @(Invoke-QuestAdb @('logcat','-d','-v','threadtime',"--pid=$processId"))
        $lines | Set-Content "$run/quest.log"
        if ($lines -match 'QUEST017_PROBE_FAIL') { throw "Projection probe failed. See $run/quest.log" }
        if ([DateTime]::UtcNow -gt $deadline) { throw "Projection probe timed out. See $run/quest.log" }
    } until ($lines -match "QUEST017_PROBE_PASS $id")
    Invoke-QuestAdb @('pull',"$remote/quest017-result.json","$run/device-result.json") | Out-Null
    $result = Get-Content "$run/device-result.json" -Raw | ConvertFrom-Json
    $fixtureHash = (Get-FileHash -LiteralPath $Fixture).Hash.ToLowerInvariant()
    if (-not $result.passed -or $result.runId -ne $id -or $result.snapshotSha256 -ne $fixtureHash -or $result.cycles -ne 3) { throw 'Stale or invalid projection result.' }
    Invoke-QuestAdb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/memory.txt"
    @{passed=$true; fixture=$Fixture; fixtureSha256=$fixtureHash; apk=$Apk; apkSha256=$apkHash; serial=$Serial; scope='desktop-export-local-reconstruction'} | ConvertTo-Json | Set-Content "$run/result.json"
    Write-Output "PASS: Quest projection inputs. Evidence: $run"
}
finally {
    $stopped = $false
    if ($verified) {
        & $AdbPath -s $Serial shell rm -f "$remote/quest017-projection-probe" | Out-Null
        Invoke-QuestAdb @('shell','am','force-stop',$package) | Out-Null
        $remaining = & $AdbPath -s $Serial shell pidof $package
        $stopExit = $LASTEXITCODE
        $stopped = !$remaining -and $stopExit -eq 1
    }
    @{commands=$commands; stopped=$stopped} | ConvertTo-Json -Depth 6 | Set-Content "$run/commands.json"
    if ($verified -and -not $stopped) { throw 'Unable to verify HiBoP stopped.' }
}
