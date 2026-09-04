[CmdletBinding()]
param(
    [string]$ApkPath,
    [int]$TimeoutSeconds = 180,
    [ValidateRange(30, 120)]
    [int]$EnduranceMinutes = 30,
    [switch]$SkipEndurance
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\p10\quest"

function Get-TotalPssKiB([string]$Path)
{
    $match = Select-String -Path $Path -Pattern '^\s*TOTAL\s+(\d+)\s' | Select-Object -First 1
    if ($null -eq $match) { throw "Unable to read TOTAL PSS from $Path." }
    return [int64]$match.Matches[0].Groups[1].Value
}

function Get-Percentile([double[]]$Values, [double]$Percentile)
{
    if ($Values.Count -eq 0) { throw "A required endurance metric has no samples." }
    [Array]::Sort($Values)
    $index = [Math]::Max(0, [Math]::Min($Values.Count - 1, [Math]::Ceiling($Values.Count * $Percentile) - 1))
    return $Values[$index]
}
if ([string]::IsNullOrWhiteSpace($ApkPath))
{
    $ApkPath = Join-Path $repositoryPath ".artifacts\xr\p10\HiBoPXR-P10.apk"
}

$ApkPath = (Resolve-Path $ApkPath).Path
$adb = (Get-Command adb.exe -CommandType Application -ErrorAction Stop).Source
$connector = Join-Path $repositoryPath "Tools\Connect-QuestAdbWifi.ps1"
& $connector -AdbPath $adb -UsbWaitSeconds 3 -Quiet
if ($LASTEXITCODE -ne 0)
{
    throw "Quest ADB Wi-Fi setup failed."
}

$connectionState = Get-Content -Raw (Join-Path $repositoryPath ".codex-temp\quest-adb-wifi.json") | ConvertFrom-Json
$serial = [string]$connectionState.Endpoint
$remoteProfile = "/sdcard/Android/data/fr.crnl.hibop.xr.dev/files/p10-profile.json"
$localProfile = Join-Path $artifactRoot "p10-profile.json"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
try
{
    & $adb -s $serial install -r $ApkPath
    if ($LASTEXITCODE -ne 0) { throw "P10 APK installation failed." }
    & $adb -s $serial logcat -c
    & $adb -s $serial shell rm -f $remoteProfile
    $activity = (& $adb -s $serial shell cmd package resolve-activity --brief fr.crnl.hibop.xr.dev | Select-Object -Last 1).Trim()
    if ([string]::IsNullOrWhiteSpace($activity) -or -not $activity.Contains("/"))
    {
        throw "Unable to resolve the P10 application activity."
    }

    & $adb -s $serial shell input keyevent KEYCODE_BACK
    Start-Sleep -Seconds 1
    & $adb -s $serial shell am start -W -n $activity
    if ($LASTEXITCODE -ne 0) { throw "P10 application launch failed." }
    # Reassert after launch so a physical prox_far transition during installation
    # cannot pause OpenXR before the off-head profile starts.
    & $adb -s $serial shell am broadcast -a com.oculus.vrpowermanager.prox_close | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Unable to reassert the Quest off-head test state." }
    Start-Sleep -Seconds 2
    $processId = (& $adb -s $serial shell pidof fr.crnl.hibop.xr.dev).Trim()
    if ([string]::IsNullOrWhiteSpace($processId)) { throw "P10 application process did not remain active after launch." }
    & $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-start.txt")

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do
    {
        Start-Sleep -Seconds 2
        & $adb -s $serial shell test -f $remoteProfile
        $profileReady = $LASTEXITCODE -eq 0
    }
    while (-not $profileReady -and [DateTime]::UtcNow -lt $deadline)

    if (-not $profileReady)
    {
        & $adb -s $serial logcat -d | Out-File -Encoding utf8 (Join-Path $artifactRoot "logcat.txt")
        throw "P10 profile did not complete within $TimeoutSeconds seconds."
    }

    & $adb -s $serial pull $remoteProfile $localProfile
    if ($LASTEXITCODE -ne 0) { throw "Unable to pull the P10 profile." }
    $profile = Get-Content -Raw $localProfile | ConvertFrom-Json
    if ($profile.siteCount -ne 37500) { throw "P10 profile contains $($profile.siteCount) sites instead of 37500." }
    if ($profile.individualSiteObjectCount -ne 0) { throw "P10 profile reports per-site objects." }
    $expectedInstances = @(1, 3, 8)
    if ($profile.phases.Count -ne $expectedInstances.Count) { throw "P10 profile must contain exactly the 1/3/8 instance phases." }
    for ($phaseIndex = 0; $phaseIndex -lt $expectedInstances.Count; $phaseIndex++)
    {
        if ($profile.phases[$phaseIndex].instanceCount -ne $expectedInstances[$phaseIndex]) { throw "P10 profile phases are not exactly 1/3/8 instances." }
    }
    foreach ($phase in $profile.phases)
    {
        if ($phase.correctPickCount -ne $phase.expectedPickCount) { throw "P10 picking is not exact for $($phase.instanceCount) instances." }
        if (-not $phase.pickingMs.available -or $phase.pickingMs.p95 -ge 50.0) { throw "P10 picking p95 misses the 50 ms gate for $($phase.instanceCount) instances." }
        if (-not $phase.cpuFrameMs.available -or $phase.cpuFrameMs.p95 -ge 13.89) { throw "P10 CPU frame p95 misses the 13.89 ms gate for $($phase.instanceCount) instances." }
        if (-not $phase.gpuFrameMs.available -or $phase.gpuFrameMs.p95 -ge 13.89) { throw "P10 GPU frame p95 misses the 13.89 ms gate for $($phase.instanceCount) instances." }
    }

    $elapsedMinutes = 0
    $checkpoints = if ($SkipEndurance) { @() } else { @(5, 15, 30, $EnduranceMinutes) | Where-Object { $_ -le $EnduranceMinutes } | Sort-Object -Unique }
    foreach ($checkpoint in $checkpoints)
    {
        $remainingSeconds = ($checkpoint - $elapsedMinutes) * 60
        while ($remainingSeconds -gt 0)
        {
            $waitSeconds = [Math]::Min(30, $remainingSeconds)
            Start-Sleep -Seconds $waitSeconds
            $remainingSeconds -= $waitSeconds
            & $adb -s $serial shell am broadcast -a com.oculus.vrpowermanager.prox_close | Out-Null
            $processId = (& $adb -s $serial shell pidof fr.crnl.hibop.xr.dev).Trim()
            if ([string]::IsNullOrWhiteSpace($processId)) { throw "P10 application process stopped during endurance before ${checkpoint}m." }
        }

        & $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-${checkpoint}m.txt")
        & $adb -s $serial shell dumpsys thermalservice | Out-File -Encoding utf8 (Join-Path $artifactRoot "thermal-${checkpoint}m.txt")
        $elapsedMinutes = $checkpoint
    }

    & $adb -s $serial logcat -d | Out-File -Encoding utf8 (Join-Path $artifactRoot "logcat.txt")
    & $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-final.txt")

    $logPath = Join-Path $artifactRoot "logcat.txt"
    $fatal = Select-String -Path $logPath -Pattern 'FATAL EXCEPTION|ANR in fr\.crnl\.hibop\.xr\.dev|OutOfMemoryError|P10_ENDURANCE_FAILED' -CaseSensitive:$false
    if ($fatal) { throw "P10 log contains a crash, ANR, OOM or endurance correctness failure." }

    if (-not $SkipEndurance)
    {
        foreach ($checkpoint in $checkpoints)
        {
            $thermalPath = Join-Path $artifactRoot "thermal-${checkpoint}m.txt"
            if (-not (Select-String -Path $thermalPath -Pattern '^Thermal Status: 0$' -Quiet)) { throw "P10 thermal status is not nominal at ${checkpoint}m." }
        }

        $fiveMinutePss = Get-TotalPssKiB (Join-Path $artifactRoot "meminfo-5m.txt")
        $finalPss = Get-TotalPssKiB (Join-Path $artifactRoot "meminfo-final.txt")
        if ($finalPss -gt $fiveMinutePss + 32768) { throw "P10 PSS grew by more than 32 MiB after the 5 minute checkpoint." }
        if (-not (Select-String -Path $logPath -Pattern 'P10_ENDURANCE_HEARTBEAT' -Quiet)) { throw "P10 endurance did not report continuous updates and exact picking." }

        $vrLines = Select-String -Path $logPath -Pattern 'VrApi\s+: FPS=' | ForEach-Object { $_.Line }
        if ($vrLines.Count -lt 120) { throw "P10 endurance has fewer than 120 retained VrApi samples." }
        [double[]]$fps = @($vrLines | ForEach-Object { if ($_ -match 'FPS=(\d+)/') { [double]$Matches[1] } })
        [double[]]$appMs = @($vrLines | ForEach-Object { if ($_ -match 'App=([0-9.]+)ms') { [double]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture) } })
        [double[]]$cpuGpuMs = @($vrLines | ForEach-Object { if ($_ -match 'CPU&GPU=([0-9.]+)ms') { [double]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture) } })
        if (($fps | Measure-Object -Minimum).Minimum -lt 72) { throw "P10 endurance dropped below 72 FPS." }
        if ((Get-Percentile $appMs 0.95) -ge 13.89) { throw "P10 endurance App p95 misses the 13.89 ms gate." }
        if ((Get-Percentile $cpuGpuMs 0.95) -ge 13.89) { throw "P10 endurance CPU+GPU p95 misses the 13.89 ms gate." }
    }
    Write-Host "P10 Quest evidence captured under $artifactRoot"
}
finally
{
    & $adb -s $serial shell am force-stop fr.crnl.hibop.xr.dev 2>$null
    if ($LASTEXITCODE -eq 0)
    {
        Start-Sleep -Seconds 2
        & $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-after-stop.txt")
    }
}
