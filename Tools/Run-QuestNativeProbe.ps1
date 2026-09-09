# Run outside the Windows sandbox. Requires a development IL2CPP APK and an authorized Quest.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [string]$Apk = "$PSScriptRoot/../.artifacts/quest-016/Android/HiBoP.Quest.apk",
    [ValidateRange(1, 5)][int]$Repetitions = 3,
    [switch]$SkipInstall,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$run = "$repo/.test-results/quest-016/device-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$commands = [Collections.Generic.List[object]]::new()
$results = [Collections.Generic.List[object]]::new()
$encoding = [Text.UTF8Encoding]::new($false)
$verifiedDevice = $false
$passed = $false
$failure = $null
$stopped = $false
function Invoke-QuestAdb([string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    $commands.Add(@{arguments=$Arguments; exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')})
    if ($code -ne 0) { throw "ADB $($Arguments[0]) failed (exit $code): $output" }
    return $output
}
function Stop-QuestPlayer {
    Invoke-QuestAdb @('shell','am','force-stop',$package) | Out-Null
    $remaining = & $AdbPath -s $Serial shell pidof $package
    $code = $LASTEXITCODE
    $commands.Add(@{arguments=@('shell','pidof',$package); exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')})
    if ($remaining -or $code -ne 1) { throw 'Unable to verify HiBoP stopped on Quest.' }
}
try {
    Invoke-QuestAdb @('get-state') | Out-Null
    $model = (@(Invoke-QuestAdb @('shell','getprop','ro.product.model')) -join '').Trim()
    if ($model -notmatch 'Quest') { throw "Expected a Quest, found $model" }
    $verifiedDevice = $true
    Invoke-QuestAdb @('shell','getprop','ro.product.cpu.abilist') | Set-Content "$run/device-abi.txt"
    Invoke-QuestAdb @('shell','getprop','ro.build.fingerprint') | Set-Content "$run/device-build.txt"
    & "$PSScriptRoot/Test-QuestApk.ps1" -Apk $Apk -ReportPath "$run/apk-content.json"
    if (-not $SkipInstall) { Invoke-QuestAdb @('install','-r',$Apk) | Set-Content "$run/install.log" }
    # Verify the actual installed APK even when installation is skipped.
    $installed = @(Invoke-QuestAdb @('shell','pm','path',$package)) | Where-Object { $_ -match '^package:.*/base\.apk$' } | Select-Object -First 1
    if (-not $installed) { throw 'Installed base APK not found.' }
    $installedHash = (@(Invoke-QuestAdb @('shell','sha256sum',$installed.Substring(8).Trim())) -join ' ').Split(' ')[0]
    $apkHash = (Get-FileHash -LiteralPath $Apk).Hash.ToLowerInvariant()
    if ($installedHash -ne $apkHash) { throw 'Installed APK differs from the inspected APK.' }
    $activity = @(Invoke-QuestAdb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    if (-not $activity) { throw 'No launchable Quest activity.' }
    Invoke-QuestAdb @('shell','mkdir','-p',"$remote/quest016") | Out-Null
    for ($repetition = 1; $repetition -le $Repetitions; $repetition++) {
        $iteration = "$run/run-$repetition"
        New-Item -ItemType Directory -Force -Path $iteration | Out-Null
        Stop-QuestPlayer
        $id = [Guid]::NewGuid().ToString('N')
        [IO.File]::WriteAllText("$iteration/marker.txt", $id, $encoding)
        Invoke-QuestAdb @('shell','rm','-f',"$remote/quest016/result.json") | Out-Null
        Invoke-QuestAdb @('push',"$iteration/marker.txt","$remote/quest016-native-probe") | Out-Null
        $launch = @(Invoke-QuestAdb @('shell','am','start','-W','-n',$activity.Trim()))
        $launch | Set-Content "$iteration/launch.log"
        if ($launch -match 'LaunchCheckControllerRequired') { throw 'Quest requires awake controllers. Wake them and dismiss the headset prompt before retrying.' }
        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        $processId = $null
        do {
            Start-Sleep -Seconds 1
            if (-not $processId) { $processId = (@(Invoke-QuestAdb @('shell','pidof',$package)) -join '').Trim() }
            if ($processId -notmatch '^\d+$') { throw 'Expected one running Quest Player process.' }
            $lines = @(Invoke-QuestAdb @('logcat','-d','-v','threadtime',"--pid=$processId"))
            # Retain diagnostics and faults, excluding unrelated pairing/session messages.
            $diagnostics = @($lines | Where-Object { $_ -match 'QUEST016_|DllNotFoundException|EntryPointNotFoundException|BadImageFormatException|FATAL EXCEPTION|Fatal signal| E Unity| E AndroidRuntime| F DEBUG' })
            [IO.File]::WriteAllLines("$iteration/logcat.txt", [string[]]$diagnostics, $encoding)
            if ($diagnostics -match 'QUEST016_PROBE_FAIL|DllNotFoundException|EntryPointNotFoundException|BadImageFormatException|FATAL EXCEPTION|Fatal signal') { throw "Native probe failed: $iteration/logcat.txt" }
            if ([DateTime]::UtcNow -gt $deadline) { throw "Native probe timed out: $iteration/logcat.txt" }
        } until ($diagnostics -match "QUEST016_PROBE_PASS runId=$id;")
        Invoke-QuestAdb @('pull',"$remote/quest016/result.json","$iteration/result.json") | Out-Null
        $result = Get-Content "$iteration/result.json" -Raw | ConvertFrom-Json
        if (-not $result.passed -or $result.runId -ne $id -or $result.backend -ne 'IL2CPP' -or
            $result.completedCycles -ne 100 -or $result.releasedHandles -ne 600 -or
            $result.callbackChecks -ne 100 -or $result.nativeErrorChecks -ne 100 -or $result.memory.Count -ne 11) {
            throw 'Incomplete or stale native probe result.'
        }
        Invoke-QuestAdb @('pull',"$remote/quest016/synthetic-32x24x16.nii","$iteration/synthetic-32x24x16.nii") | Out-Null
        if ((Get-FileHash "$iteration/synthetic-32x24x16.nii").Hash.ToLowerInvariant() -ne $result.fixtureSha256) { throw 'Fixture hash mismatch.' }
        Invoke-QuestAdb @('shell','dumpsys','meminfo',$package) | Set-Content "$iteration/memory.txt"
        Invoke-QuestAdb @('shell','dumpsys','battery') | Set-Content "$iteration/battery.txt"
        $results.Add($result)
        Stop-QuestPlayer
        Write-Output "PASS $repetition/$Repetitions`: 100 cycles, 600 released handles. HiBoP stopped."
    }
    $passed = $true
}
catch {
    $failure = $_.Exception.Message
    throw
}
finally {
    try {
        if ($verifiedDevice) {
            Invoke-QuestAdb @('shell','rm','-f',"$remote/quest016-native-probe") | Out-Null
            Stop-QuestPlayer
            $stopped = $true
        }
    }
    finally {
        @{passed=($passed -and $stopped); failure=$failure; stopped=$stopped; serial=$Serial;
          apk=$Apk; apkSha256=$apkHash; installedApkSha256=$installedHash;
          requestedRepetitions=$Repetitions; completedRepetitions=$results.Count; results=$results} |
            ConvertTo-Json -Depth 8 | Set-Content "$run/result.json"
        $commands | ConvertTo-Json -Depth 6 | Set-Content "$run/commands.json"
        Write-Output "Evidence: $run"
    }
}
