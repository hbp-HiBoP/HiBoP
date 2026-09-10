# Run outside the Codex sandbox: Unity licensing uses Windows named-pipe IPC.
[CmdletBinding()]
param(
    [ValidateSet('Windows', 'Android')][string]$Target = 'Windows',
    [ValidatePattern('^(quest|scene)-\d{3}$')][string]$EvidenceId = 'quest-011'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$unity = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
$profile = if ($Target -eq 'Android') { 'Quest' } else { 'DesktopWindows' }
$log = "$repo/.test-results/$EvidenceId/$($Target.ToLower())-build.log"
New-Item -ItemType Directory -Force -Path "$repo/.test-results/$EvidenceId", "$repo/.artifacts/$EvidenceId/java" | Out-Null
$previousJavaOptions = $env:JDK_JAVA_OPTIONS
try {
    if ($Target -eq 'Android') {
        $env:JDK_JAVA_OPTIONS = "$previousJavaOptions -Djdk.net.unixdomain.tmpdir=`"$repo/.artifacts/$EvidenceId/java`""
    }
    $arguments = @('-batchmode', '-nographics', '-projectPath', "`"$repo`"",
        '-activeBuildProfile', "Assets/Settings/BuildProfiles/$profile.asset",
        '-executeMethod', 'HBP.Dev.HBPBuilder.BuildFromCommandLine',
        '-buildOutput', "`"$repo/.artifacts/$EvidenceId/$Target`"", '-developmentBuild',
        '-logFile', "`"$log`"", '-quit', '-forgetProjectPath')
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Unity $Target build failed with exit $($process.ExitCode). See $log" }
}
finally { $env:JDK_JAVA_OPTIONS = $previousJavaOptions }
