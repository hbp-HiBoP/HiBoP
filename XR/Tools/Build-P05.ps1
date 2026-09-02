[CmdletBinding()]
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe",
    [string]$OutputPath,
    [string]$JavaTempPath = "C:\jtmp"
)

$ErrorActionPreference = "Stop"
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryPath = (Resolve-Path (Join-Path $projectPath "..")).Path
$artifactRoot = Join-Path $repositoryPath ".artifacts\xr\p05"
$d1Output = Join-Path $projectPath "Assets\HiBoPXR\StaticRendering\Data"
$goldenRoot = Join-Path $artifactRoot "d1-golden"

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $artifactRoot "HiBoPXR-P05.apk"
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
New-Item -ItemType Directory -Force -Path $goldenRoot | Out-Null
New-Item -ItemType Directory -Force -Path $JavaTempPath | Out-Null
$env:TEMP = $JavaTempPath
$env:TMP = $JavaTempPath
$exportArguments = @(
    "-batchmode",
    "-accept-apiupdate",
    "-projectPath", $repositoryPath,
    "-executeMethod", "HBP.Dev.XR.Editor.P05D1SurfaceExporter.Export",
    "-p05D1Output", $d1Output,
    "-p05GoldenOutput", $goldenRoot,
    "-logFile", (Join-Path $artifactRoot "d1-export.log"),
    "-forgetProjectPath",
    "-quit"
)
$exportProcess = Start-Process -FilePath $UnityPath -ArgumentList $exportArguments -Wait -PassThru -WindowStyle Hidden
if ($exportProcess.ExitCode -ne 0)
{
    exit $exportProcess.ExitCode
}

$goldenArguments = @(
    "-batchmode",
    "-accept-apiupdate",
    "-projectPath", $projectPath,
    "-executeMethod", "CRNL.HiBoP.XR.StaticRendering.Editor.P05ProjectSetup.CaptureD1Golden",
    "-p05GoldenOutput", $goldenRoot,
    "-logFile", (Join-Path $artifactRoot "d1-golden.log"),
    "-forgetProjectPath",
    "-quit"
)
$goldenProcess = Start-Process -FilePath $UnityPath -ArgumentList $goldenArguments -Wait -PassThru -WindowStyle Hidden
if ($goldenProcess.ExitCode -ne 0)
{
    exit $goldenProcess.ExitCode
}

$arguments = @(
    "-batchmode",
    "-nographics",
    "-accept-apiupdate",
    "-projectPath", $projectPath,
    "-buildTarget", "Android",
    "-executeMethod", "CRNL.HiBoP.XR.StaticRendering.Editor.P05ProjectSetup.BuildAndroid",
    "-p05BuildOutput", $OutputPath,
    "-p05BuildEvidence", (Join-Path $artifactRoot "build-evidence.json"),
    "-logFile", (Join-Path $artifactRoot "build.log"),
    "-forgetProjectPath",
    "-quit"
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
exit $process.ExitCode
