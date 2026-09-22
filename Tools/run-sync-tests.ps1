#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet("Fast", "Loopback", "SceneFocused", "Qualification")]
    [string]$Tier = "Fast",
    [string]$ResultRoot = "",
    [switch]$AllInAssemblies,
    [string]$UnityPath = "",
    [string]$TestFilter = "",
    [switch]$PlanOnly
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "check-assembly-dependencies.ps1") -ProjectRoot $projectRoot
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot "ProjectSettings/ProjectVersion.txt") -TotalCount 1
$unityVersion = ($versionLine -split ':', 2)[1].Trim()
$category = "Sync.$Tier"
$assemblies = switch ($Tier) {
    "Fast" { "HBP.Sync.Tests;HBP.Transfer.Scene.Tests" }
    "Loopback" { "HBP.Transfer.Scene.Tests" }
    "SceneFocused" { "HBP.Transfer.Scene.Tests;HBP.Quest.Anatomy.Tests" }
    "Qualification" { "HBP.Sync.Tests;HBP.Transfer.Scene.Tests;HBP.Quest.Anatomy.Tests;HBP.Serialization.Tests" }
}
if ($PlanOnly) {
    [ordered]@{ mode = "EditMode"; category = $(if ($AllInAssemblies) { $null } else { $category }); assemblies = $assemblies.Split(';'); unityVersion = $unityVersion; testFilter = $TestFilter } | ConvertTo-Json
    return
}

# Be conservative: a lock file may be stale, but never start another Unity on a possibly open project.
# Use the existing Editor Test Runner/MCP, or close the Editor normally. Do not delete the lock automatically.
if (Test-Path -LiteralPath (Join-Path $projectRoot "Temp/UnityLockfile")) {
    throw "This project has a UnityLockfile. Use the open Editor/MCP (see -PlanOnly), or close Unity normally before a CLI run."
}
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $UnityPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $unityVersion was not found at '$UnityPath'. Supply -UnityPath with the matching Editor executable."
}
if ([string]::IsNullOrWhiteSpace($ResultRoot)) { $ResultRoot = Join-Path $projectRoot ".test-results/sync" }
$ResultRoot = [System.IO.Path]::GetFullPath($ResultRoot)
$runId = "$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')-$([guid]::NewGuid().ToString('N'))"
$directory = Join-Path $ResultRoot "$($Tier.ToLowerInvariant())-$runId"
New-Item -ItemType Directory -Path $directory | Out-Null
$result = Join-Path $directory "results.xml"
$log = Join-Path $directory "unity.log"
$arguments = @(
    "-batchmode", "-nographics", "-accept-apiupdate",
    "-projectPath", $projectRoot, "-runTests", "-testPlatform", "EditMode",
    "-assemblyNames", $assemblies, "-testResults", $result, "-logFile", $log, "-forgetProjectPath"
)
if (-not $AllInAssemblies) { $arguments += @("-testCategory", $category) }
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { $arguments += @("-testFilter", $TestFilter) }

# ArgumentList preserves paths containing spaces; no ad-hoc command-line quoting.
$start = [System.Diagnostics.ProcessStartInfo]::new()
$start.FileName = $UnityPath
$start.WorkingDirectory = $projectRoot
$start.UseShellExecute = $false
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$elapsed = [System.Diagnostics.Stopwatch]::StartNew()
$process = [System.Diagnostics.Process]::Start($start)
$process.WaitForExit()
$elapsed.Stop()
$processExit = $process.ExitCode
$process.Dispose()

$valid = $false
$issue = ""
$total = 0; $passed = 0; $failed = 0; $skipped = 0; $inconclusive = 0
$loadedSeconds = $null
try {
    if (-not (Test-Path -LiteralPath $result -PathType Leaf)) { throw "Unity produced no fresh test-results XML." }
    [xml]$xml = Get-Content -LiteralPath $result -Raw
    $run = $xml.DocumentElement
    if ($run.Name -ne "test-run") { throw "Unexpected XML root '$($run.Name)'." }
    $total = [int]$run.total; $passed = [int]$run.passed; $failed = [int]$run.failed
    $skipped = [int]$run.skipped; $inconclusive = [int]$run.inconclusive
    $loadedSeconds = [double]::Parse([string]$run.duration, [Globalization.CultureInfo]::InvariantCulture)
    if ($total -le 0 -or $passed -ne $total -or $failed -gt 0 -or $skipped -gt 0 -or $inconclusive -gt 0 -or $run.result -ne "Passed") {
        throw "Tier not fully passed: total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive result=$($run.result)."
    }
    if ($processExit -ne 0) { throw "Unity exited with $processExit despite its XML result." }
    $valid = $true
}
catch { $issue = $_.Exception.Message }

$head = "unavailable"
$status = @()
if (Get-Command git -ErrorAction SilentlyContinue) {
    $head = (& git -C $projectRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $status = @(& git -C $projectRoot status --porcelain 2>$null)
}
$summary = [ordered]@{
    runId = $runId; tier = $Tier; testFilter = $TestFilter; allInAssemblies = [bool]$AllInAssemblies; assemblies = $assemblies
    unityVersion = $unityVersion; gitHead = $head; gitStatus = $status
    processExit = $processExit; valid = $valid; issue = $issue
    total = $total; passed = $passed; failed = $failed; skipped = $skipped; inconclusive = $inconclusive
    loadedSeconds = $loadedSeconds; wallSeconds = $elapsed.Elapsed.TotalSeconds
    xml = $result; log = $log
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $directory "summary.json") -Encoding utf8
Write-Host "Sync.$Tier valid=$valid tests=$passed/$total loaded=${loadedSeconds}s wall=$($elapsed.Elapsed.TotalSeconds.ToString('F3'))s evidence=$directory"
if (-not $valid) { Write-Error $issue -ErrorAction Continue; exit 1 }
exit 0
