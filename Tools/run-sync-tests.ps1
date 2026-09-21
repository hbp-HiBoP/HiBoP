[CmdletBinding()]
param(
    [ValidateSet("Fast", "Loopback", "SceneFocused", "Qualification")]
    [string]$Tier = "Fast",
    [string]$ResultRoot = "",
    [switch]$AllInAssemblies
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "check-assembly-dependencies.ps1") -ProjectRoot $projectRoot
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot "ProjectSettings/ProjectVersion.txt") -TotalCount 1
$unityVersion = ($versionLine -split ':', 2)[1].Trim()
$unity = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
if (-not (Test-Path -LiteralPath $unity)) {
    throw "Unity $unityVersion was not found at $unity"
}

if ([string]::IsNullOrWhiteSpace($ResultRoot)) {
    $ResultRoot = Join-Path $projectRoot ".test-results/sync"
}
$ResultRoot = [System.IO.Path]::GetFullPath($ResultRoot)
New-Item -ItemType Directory -Force -Path $ResultRoot | Out-Null

$category = "Sync.$Tier"
$assemblies = switch ($Tier) {
    "Fast" { "HBP.Sync.Tests;HBP.Transfer.Scene.Tests" }
    "Loopback" { "HBP.Transfer.Scene.Tests" }
    "SceneFocused" { "HBP.Transfer.Scene.Tests;HBP.Quest.Anatomy.Tests" }
    "Qualification" { "HBP.Transfer.Scene.Tests;HBP.Quest.Anatomy.Tests;HBP.Serialization.Tests" }
}
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$suffix = if ($AllInAssemblies) { "-assembly-baseline" } else { "" }
$result = Join-Path $ResultRoot "$($Tier.ToLowerInvariant())$suffix-$stamp.xml"
$log = Join-Path $ResultRoot "$($Tier.ToLowerInvariant())$suffix-$stamp.log"
$arguments = @(
    "-batchmode",
    "-nographics",
    "-accept-apiupdate",
    "-projectPath", $projectRoot,
    "-runTests",
    "-testPlatform", "EditMode",
    "-assemblyNames", $assemblies,
    "-testResults", $result,
    "-logFile", $log,
    "-forgetProjectPath"
)
if (-not $AllInAssemblies) {
    $arguments += @("-testCategory", $category)
}

$elapsed = [System.Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
$elapsed.Stop()
Write-Host "Sync.$Tier exit=$($process.ExitCode) wall=$($elapsed.Elapsed.TotalSeconds.ToString('F3'))s results=$result log=$log"
exit $process.ExitCode
