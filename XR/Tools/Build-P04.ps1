[CmdletBinding()]
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe",
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\p04"

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $artifactRoot "HiBoPXR-P04.apk"
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
$arguments = @(
    "-batchmode",
    "-nographics",
    "-accept-apiupdate",
    "-projectPath", $projectPath,
    "-buildTarget", "Android",
    "-executeMethod", "CRNL.HiBoP.XR.Bootstrap.Editor.P04ProjectSetup.BuildAndroid",
    "-p04BuildOutput", $OutputPath,
    "-p04BuildEvidence", (Join-Path $artifactRoot "build-evidence.json"),
    "-logFile", (Join-Path $artifactRoot "build.log"),
    "-forgetProjectPath",
    "-quit"
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
exit $process.ExitCode
