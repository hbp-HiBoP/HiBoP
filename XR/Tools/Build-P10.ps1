[CmdletBinding()]
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe",
    [string]$OutputPath,
    [string]$JavaTempPath = "C:\jtmp"
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\p10"

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $artifactRoot "HiBoPXR-P10.apk"
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
New-Item -ItemType Directory -Force -Path $JavaTempPath | Out-Null
$env:TEMP = $JavaTempPath
$env:TMP = $JavaTempPath
$arguments = @(
    "-batchmode",
    "-nographics",
    "-accept-apiupdate",
    "-projectPath", $projectPath,
    "-buildTarget", "Android",
    "-executeMethod", "CRNL.HiBoP.XR.Sites.Editor.P10ProjectSetup.BuildAndroid",
    "-p10BuildOutput", $OutputPath,
    "-p10BuildEvidence", (Join-Path $artifactRoot "build-evidence.json"),
    "-logFile", (Join-Path $artifactRoot "build.log"),
    "-forgetProjectPath",
    "-quit"
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
exit $process.ExitCode
