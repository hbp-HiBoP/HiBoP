[CmdletBinding()]
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe",
    [string]$ApkPath
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path

if ([string]::IsNullOrWhiteSpace($ApkPath))
{
    $ApkPath = Join-Path $repositoryPath ".artifacts\xr\p04\HiBoPXR-P04.apk"
}

$ApkPath = (Resolve-Path $ApkPath).Path
$adb = Join-Path (Split-Path -Parent $UnityPath) "Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$devices = @(& $adb devices | Select-String "\tdevice$")
if ($devices.Count -ne 1)
{
    throw "Exactly one authorized Quest must be connected; found $($devices.Count)."
}

$serial = ($devices[0].Line -split "\s+")[0]
& $adb -s $serial install -r $ApkPath
if ($LASTEXITCODE -ne 0)
{
    throw "APK installation failed."
}

& $adb -s $serial shell monkey -p fr.crnl.hibop.xr.dev -c android.intent.category.LAUNCHER 1
if ($LASTEXITCODE -ne 0)
{
    throw "Application launch failed."
}

Write-Host "P04 launched on the authorized Quest. In-headset diagnostics must show passthrough/VR, head, hands, and both controllers."
