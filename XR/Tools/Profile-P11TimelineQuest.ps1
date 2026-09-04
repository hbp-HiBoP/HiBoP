[CmdletBinding()]
param(
    [string]$ApkPath,
    [int]$TimeoutSeconds = 600,
    [switch]$UsbOnly
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\d20-timeline\quest"
if ([string]::IsNullOrWhiteSpace($ApkPath))
{
    $ApkPath = Join-Path $repositoryPath ".artifacts\xr\d20-timeline\HiBoPXR-D20Timeline.apk"
}

$ApkPath = (Resolve-Path $ApkPath).Path
$adb = (Get-Command adb.exe -CommandType Application -ErrorAction Stop).Source
if ($UsbOnly)
{
    $deviceLine = @(& $adb devices -l) | Where-Object { $_ -match '^(?<serial>\S+)\s+device(?:\s|$)' } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($deviceLine)) { throw "No authorized USB Quest is visible." }
    $serial = [regex]::Match($deviceLine, '^(?<serial>\S+)').Groups['serial'].Value
}
else
{
    $connector = Join-Path $repositoryPath "Tools\Connect-QuestAdbWifi.ps1"
    & $connector -AdbPath $adb -UsbWaitSeconds 3 -Quiet
    if ($LASTEXITCODE -ne 0) { throw "Quest ADB Wi-Fi setup failed." }
    $connectionState = Get-Content -Raw (Join-Path $repositoryPath ".codex-temp\quest-adb-wifi.json") | ConvertFrom-Json
    $serial = [string]$connectionState.Endpoint
}
$packageId = "fr.crnl.hibop.xr.d20timeline"
$remoteProfile = "/sdcard/Android/data/$packageId/files/d20-timeline-profile.json"
$localProfile = Join-Path $artifactRoot "d20-timeline-profile.json"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
try
{
    & $adb -s $serial install -r $ApkPath
    if ($LASTEXITCODE -ne 0) { throw "D20 timeline APK installation failed." }
    & $adb -s $serial logcat -c
    & $adb -s $serial shell rm -f $remoteProfile
    $activity = (& $adb -s $serial shell cmd package resolve-activity --brief $packageId | Select-Object -Last 1).Trim()
    if ([string]::IsNullOrWhiteSpace($activity) -or -not $activity.Contains("/")) { throw "Unable to resolve the D20 timeline activity." }

    & $adb -s $serial shell input keyevent KEYCODE_BACK
    Start-Sleep -Seconds 1
    & $adb -s $serial shell am start -W -n $activity
    if ($LASTEXITCODE -ne 0) { throw "D20 timeline application launch failed." }
    & $adb -s $serial shell am broadcast -a com.oculus.vrpowermanager.prox_close | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Unable to enable off-head Quest execution." }

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do
    {
        Start-Sleep -Seconds 2
        & $adb -s $serial shell test -f $remoteProfile
        $profileReady = $LASTEXITCODE -eq 0
    }
    while (-not $profileReady -and [DateTime]::UtcNow -lt $deadline)

    & $adb -s $serial logcat -d | Out-File -Encoding utf8 (Join-Path $artifactRoot "logcat.txt")
    & $adb -s $serial shell dumpsys meminfo $packageId | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo.txt")
    & $adb -s $serial shell dumpsys thermalservice | Out-File -Encoding utf8 (Join-Path $artifactRoot "thermal.txt")
    if (-not $profileReady) { throw "D20 timeline profile did not complete within $TimeoutSeconds seconds." }

    & $adb -s $serial pull $remoteProfile $localProfile
    if ($LASTEXITCODE -ne 0) { throw "Unable to pull the D20 timeline profile." }
    $profile = Get-Content -Raw $localProfile | ConvertFrom-Json
    if ($profile.result -ne "PASS") { throw "D20 timeline device probe failed: $($profile.failure)" }
    if ($profile.schema -ne "d20-timeline-preload-quest-profile-v3") { throw "Unexpected D20 timeline profile schema: $($profile.schema)" }
    if ($profile.indexCount -ne 97) { throw "D20 timeline preload profile must contain 97 indices." }
    if ($profile.minimumRandomSelections -ne 64) { throw "D20 timeline preload profile must contain at least 64 random selections per phase." }
    if ($profile.worstCaseRandomSeconds -ne 60) { throw "D20 timeline worst-case scrub must run for 60 seconds." }
    if ($profile.worstCaseAutoplaySeconds -ne 600) { throw "D20 timeline worst-case autoplay must run for 600 seconds." }
    $expectedColumns = @(1, 3, 8)
    if ($profile.phases.Count -ne 3) { throw "D20 timeline profile must contain exactly three phases." }
    for ($index = 0; $index -lt $expectedColumns.Count; $index++)
    {
        if ($profile.phases[$index].columns -ne $expectedColumns[$index]) { throw "D20 timeline phases are not ordered 1/3/8." }
        if ($profile.phases[$index].indexCount -ne 97) { throw "Every D20 timeline phase must contain 97 indices." }
        if ($profile.phases[$index].selectionSubmitMs.count -lt 64) { throw "Every D20 timeline phase must contain at least 64 selection submissions." }
        if ($profile.phases[$index].selectionToEndOfFrameMs.count -ne $profile.phases[$index].selectionSubmitMs.count) { throw "Every D20 selection submission must have an end-of-frame measurement." }
    }
    $worstCase = $profile.phases[2]
    if ($worstCase.randomSelectionDurationSeconds -lt 60 -or $worstCase.randomSelectionDurationSeconds -gt 61) { throw "Worst-case random scrub duration is outside 60-61 seconds." }
    if ($worstCase.autoplayDurationSeconds -lt 600 -or $worstCase.autoplayDurationSeconds -gt 601) { throw "Worst-case autoplay duration is outside 600-601 seconds." }
    if ($worstCase.autoplaySubmitMs.count -lt 40000) { throw "Worst-case autoplay did not cover enough rendered frames." }
    if ($worstCase.autoplayToEndOfFrameMs.count -ne $worstCase.autoplaySubmitMs.count) { throw "Every autoplay submission must have an end-of-frame measurement." }
    if ($worstCase.randomSelectionMaximumFrameDelta -gt 1 -or $worstCase.autoplayMaximumFrameDelta -gt 1) { throw "A timeline selection was not observed by the next rendered frame." }

    $fatal = Select-String -Path (Join-Path $artifactRoot "logcat.txt") -Pattern "FATAL EXCEPTION|ANR in $packageId|OutOfMemoryError" -CaseSensitive:$false
    if ($fatal) { throw "D20 timeline log contains a crash, ANR or OOM." }
    Write-Host "D20 timeline Quest evidence captured under $artifactRoot"
}
finally
{
    & $adb -s $serial shell am force-stop $packageId 2>$null
}
