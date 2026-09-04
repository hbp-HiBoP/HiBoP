[CmdletBinding()]
param(
    [string]$Output = "",
    [string]$JavaTempPath = "C:\jtmp"
)

$ErrorActionPreference = "Stop"
$spikeRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "Prepare-UnityClient.ps1")
if ($LASTEXITCODE -ne 0) { throw "Unity client preparation failed." }

$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"
$project = Join-Path $spikeRoot "UnityClient"
$artifactRoot = Join-Path $spikeRoot ".artifacts/unity"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
New-Item -ItemType Directory -Force -Path $JavaTempPath | Out-Null
$env:TEMP = $JavaTempPath
$env:TMP = $JavaTempPath
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $artifactRoot "HiBoP-P06-Transport.apk"
}
$Output = [System.IO.Path]::GetFullPath($Output)
$log = Join-Path $artifactRoot "build.log"
$arguments = @(
    "-batchmode",
    "-nographics",
    "-accept-apiupdate",
    "-projectPath", $project,
    "-buildTarget", "Android",
    "-executeMethod", "CRNL.HiBoP.Spikes.P06.UnityClient.Editor.P06UnityBuilder.BuildAndroid",
    "-p06BuildOutput", $Output,
    "-logFile", $log,
    "-forgetProjectPath",
    "-quit"
)
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
    Get-Content $log -Tail 200
    throw "Unity P06 build failed with exit code $($process.ExitCode)."
}
Get-Content $log -Tail 80
