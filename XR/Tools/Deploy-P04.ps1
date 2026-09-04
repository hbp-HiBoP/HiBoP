[CmdletBinding()]
param(
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
$adb = (Get-Command adb.exe -CommandType Application -ErrorAction Stop).Source
$connector = Join-Path $repositoryPath "Tools\Connect-QuestAdbWifi.ps1"
& $connector -AdbPath $adb -UsbWaitSeconds 3 -Quiet
if ($LASTEXITCODE -ne 0)
{
    throw "Quest ADB Wi-Fi setup failed."
}

$connectionState = Get-Content -Raw (Join-Path $repositoryPath ".codex-temp\quest-adb-wifi.json") | ConvertFrom-Json
$serial = [string]$connectionState.Endpoint
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
