# Run outside the Windows sandbox. Uses the development diagnostic on an already published scene.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$Evidence,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe',
    [ValidateRange(1, 3)][int]$Passes = 2,
    [switch]$UsbOutage
)
$ErrorActionPreference = 'Stop'
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$Evidence = [IO.Path]::GetFullPath($Evidence)
if (Test-Path -LiteralPath $Evidence) { throw 'Choose a new evidence directory to exclude stale results.' }
New-Item -ItemType Directory -Path $Evidence | Out-Null
$commands = [Collections.Generic.List[object]]::new()
$report = [ordered]@{ success = $false; serial = $Serial; startedUtc = [DateTime]::UtcNow.ToString('o'); usbOutage = [bool]$UsbOutage; passes = @() }
$restoreForward = $false
$outageClock = $null
function Invoke-Adb([string[]]$Arguments) {
    # Windows PowerShell treats ADB's successful stderr progress as an error
    # under Stop. Capture both streams and use the native exit code instead.
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    $commands.Add(@{ arguments = $Arguments; exitCode = $code; utc = [DateTime]::UtcNow.ToString('o') })
    if ($code -ne 0) { throw "ADB failed ($code): $output" }
    return $output
}
function Get-Diagnostics {
    # A fresh install need not contain the output directory yet.
    $exists = @(& $AdbPath -s $Serial shell test -d "$remote/scene-008")
    if ($LASTEXITCODE -eq 1) { return @() }
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Quest diagnostic directory.' }
    return @(Invoke-Adb @('shell', 'ls', "$remote/scene-008") | Where-Object { $_ -match '^\d{8}-\d{6}$' })
}
try {
    $model = (@(Invoke-Adb @('shell', 'getprop', 'ro.product.model')) -join '').Trim()
    if ($model -notmatch 'Quest') { throw "Expected Quest, found $model" }
    $questProcessId = (@(Invoke-Adb @('shell', 'pidof', $package)) -join '').Trim()
    if ($questProcessId -notmatch '^\d+$') { throw 'HiBoP must already be running with a published scene.' }
    $report.processId = $questProcessId
    Invoke-Adb @('shell', 'dumpsys', 'battery') | Set-Content "$Evidence/battery-before.txt"
    Invoke-Adb @('shell', 'dumpsys', 'meminfo', $package) | Set-Content "$Evidence/memory-before.txt"
    if ($UsbOutage) {
        if ($Serial -match ':') { throw 'USB outage requires the USB serial, not an ADB TCP transport.' }
        $forwards = @(Invoke-Adb @('forward', '--list'))
        if ($forwards -notcontains "$Serial tcp:45871 tcp:45871") { throw 'Expected the existing HiBoP USB forward on port 45871.' }
        $restoreForward = $true
        Invoke-Adb @('forward', '--remove', 'tcp:45871') | Out-Null
        $outageClock = [Diagnostics.Stopwatch]::StartNew()
        $report.outageStartedUtc = [DateTime]::UtcNow.ToString('o')
        Invoke-Adb @('forward', '--list') | Set-Content "$Evidence/forwards-during-outage.txt"
    }
    for ($pass = 1; $pass -le $Passes; $pass++) {
        $previous = @(Get-Diagnostics)
        & $AdbPath -s $Serial shell test -f "$remote/scene008-qualify"
        if ($LASTEXITCODE -eq 0) { throw 'Another diagnostic request is pending; do not replace it.' }
        if ($LASTEXITCODE -ne 1) { throw 'Cannot inspect Quest request state.' }
        Invoke-Adb @('shell', 'touch', "$remote/scene008-qualify") | Out-Null
        $deadline = [DateTime]::UtcNow.AddMinutes(4)
        $directory = $null
        do {
            Start-Sleep -Seconds 2
            $candidates = @(Get-Diagnostics | Where-Object { $_ -notin $previous })
            if ($candidates.Count -gt 1) { throw 'Multiple new diagnostics found; evidence ownership is ambiguous.' }
            if ($candidates.Count -eq 1) {
                & $AdbPath -s $Serial shell test -f "$remote/scene-008/$($candidates[0])/result.json"
                if ($LASTEXITCODE -eq 0) { $directory = $candidates[0] }
                elseif ($LASTEXITCODE -ne 1) { throw 'Cannot inspect Quest diagnostic completion.' }
            }
            if ([DateTime]::UtcNow -gt $deadline) { throw 'Quest diagnostic timed out; inspect the scene and log before retrying.' }
        } until ($directory)
        $destination = "$Evidence/pass-$pass"
        Invoke-Adb @('pull', "$remote/scene-008/$directory", $destination) | Out-Null
        $result = Get-Content "$destination/result.json" -Raw | ConvertFrom-Json
        $expected = @('AnatomicColumn', 'CCEPColumn', 'FMRIColumn', 'IEEGColumn', 'MEGColumn', 'StaticColumn')
        if (!$result.success -or $result.states.Count -lt 12) { throw "Incomplete diagnostic: $destination" }
        foreach ($state in $result.states) {
            if (@(Compare-Object $expected @($state.columns.type | Sort-Object)).Count -ne 0 -or $state.columns.Count -ne 6) {
                throw "The six required modalities were not exercised: $destination"
            }
        }
        $observedMaximum = (@($result.peakUnityAllocatedBytesSampledPerFrame) + @($result.states.unityAllocatedBytes) | Measure-Object -Maximum).Maximum
        $report.passes += @{ directory = "pass-$pass"; deviceDirectory = $directory; elapsedMs = $result.elapsedMs; peakUnityAllocatedBytesSampledPerFrame = $result.peakUnityAllocatedBytesSampledPerFrame; maximumUnityAllocatedBytesObserved = $observedMaximum; states = $result.states.Count }
        Invoke-Adb @('shell', 'dumpsys', 'meminfo', $package) | Set-Content "$Evidence/memory-after-$pass.txt"
        Write-Output "Quest pass $pass succeeded: $($result.states.Count) states, $($result.elapsedMs) ms."
    }
    if ($UsbOutage) {
        while ($outageClock.Elapsed.TotalSeconds -lt 60) { Start-Sleep -Seconds 1 }
        $forwards = @(Invoke-Adb @('forward', '--list'))
        if ($forwards | Where-Object { $_ -match ' tcp:45871 ' }) { throw 'HiBoP forwarding was restored during the outage.' }
    }
    $afterProcessId = (@(Invoke-Adb @('shell', 'pidof', $package)) -join '').Trim()
    if ($afterProcessId -ne $questProcessId) { throw 'Quest process changed during qualification.' }
    $report.success = $true
}
catch {
    $report.error = $_.Exception.Message
    throw
}
finally {
    try {
        if ($restoreForward) {
            $report.outageSeconds = $outageClock.Elapsed.TotalSeconds
            Invoke-Adb @('forward', '--no-rebind', 'tcp:45871', 'tcp:45871') | Out-Null
            $report.forwardRestored = $true
        }
        if ($questProcessId) {
            Invoke-Adb @('logcat', '-d', '-v', 'threadtime', "--pid=$questProcessId") | Set-Content "$Evidence/quest.log"
            Invoke-Adb @('shell', 'dumpsys', 'battery') | Set-Content "$Evidence/battery-after.txt"
            Invoke-Adb @('shell', 'dumpsys', 'thermalservice') | Set-Content "$Evidence/thermal.txt"
        }
    }
    catch {
        $report.success = $false
        $report.finalizationError = $_.Exception.Message
        throw
    }
    finally {
        $report.completedUtc = [DateTime]::UtcNow.ToString('o')
        $report | ConvertTo-Json -Depth 6 | Set-Content "$Evidence/run.json"
        $commands | ConvertTo-Json -Depth 6 | Set-Content "$Evidence/commands.json"
    }
}
# Keep the scene available for the owner's visual checks. Stop HiBoP only after that feedback (D23).
