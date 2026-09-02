[CmdletBinding()]
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe",
    [string]$ApkPath,
    [int]$TimeoutSeconds = 90,
    [ValidateRange(0, 120)]
    [int]$EnduranceMinutes = 30
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\p05\quest"
if ([string]::IsNullOrWhiteSpace($ApkPath))
{
    $ApkPath = Join-Path $repositoryPath ".artifacts\xr\p05\HiBoPXR-P05.apk"
}

$ApkPath = (Resolve-Path $ApkPath).Path
$adb = Join-Path (Split-Path -Parent $UnityPath) "Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$devices = @(& $adb devices | Select-String "\tdevice$")
if ($devices.Count -ne 1)
{
    throw "Exactly one authorized Quest must be connected; found $($devices.Count)."
}

$serial = ($devices[0].Line -split "\s+")[0]
$remoteProfile = "/sdcard/Android/data/fr.crnl.hibop.xr.dev/files/p05-profile.json"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
& $adb -s $serial install -r $ApkPath
if ($LASTEXITCODE -ne 0) { throw "P05 APK installation failed." }
& $adb -s $serial logcat -c
& $adb -s $serial shell rm -f $remoteProfile
& $adb -s $serial shell monkey -p fr.crnl.hibop.xr.dev -c android.intent.category.LAUNCHER 1
if ($LASTEXITCODE -ne 0) { throw "P05 application launch failed." }
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
    throw "P05 profile did not complete within $TimeoutSeconds seconds. Inspect quest/logcat.txt."
}

& $adb -s $serial pull $remoteProfile (Join-Path $artifactRoot "p05-profile.json")
if ($LASTEXITCODE -ne 0) { throw "Unable to pull the P05 profile." }

$elapsedMinutes = 0
$checkpoints = @(5, 15, 30) | Where-Object { $_ -le $EnduranceMinutes }
foreach ($checkpoint in $checkpoints)
{
    $remainingSeconds = ($checkpoint - $elapsedMinutes) * 60
    while ($remainingSeconds -gt 0)
    {
        $waitSeconds = [Math]::Min(30, $remainingSeconds)
        Start-Sleep -Seconds $waitSeconds
        $remainingSeconds -= $waitSeconds
    }

    & $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-${checkpoint}m.txt")
    & $adb -s $serial shell dumpsys thermalservice | Out-File -Encoding utf8 (Join-Path $artifactRoot "thermal-${checkpoint}m.txt")
    $elapsedMinutes = $checkpoint
}

& $adb -s $serial logcat -d | Out-File -Encoding utf8 (Join-Path $artifactRoot "logcat.txt")
& $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-final.txt")
& $adb -s $serial shell am force-stop fr.crnl.hibop.xr.dev
Start-Sleep -Seconds 2
& $adb -s $serial shell dumpsys meminfo fr.crnl.hibop.xr.dev | Out-File -Encoding utf8 (Join-Path $artifactRoot "meminfo-after-stop.txt")
Write-Host "P05 Quest evidence captured under $artifactRoot"
