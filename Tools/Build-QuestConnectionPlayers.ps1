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
if ($Target -eq 'Android') {
    & "$PSScriptRoot/Sign-QuestApk.ps1" -ValidateOnly
    $nativeLock = Get-Content "$PSScriptRoot/NativePlugins.lock.json" -Raw | ConvertFrom-Json
    foreach ($name in @('hbp_core', 'hbp_math')) {
        $library = @($nativeLock.libraries | Where-Object name -eq $name)[0]
        $pins = @($library.artifacts | Where-Object platform -eq 'Android')
        if ($pins.Count -ne 1) { throw "Missing Android $name pin; resolve the native artifact before building Quest." }
        $path = Join-Path $repo $pins[0].destination
        if (!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant() -ne $pins[0].files[0].sha256) {
            throw "Android $name is missing or does not match NativePlugins.lock.json."
        }
    }
}
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
    if ($Target -eq 'Android') {
        & "$PSScriptRoot/Sign-QuestApk.ps1" -Apk "$repo/.artifacts/$EvidenceId/Android/HiBoP.Quest.apk"
        & "$PSScriptRoot/Test-QuestApk.ps1" -Apk "$repo/.artifacts/$EvidenceId/Android/HiBoP.Quest.apk" -ReportPath "$repo/.test-results/$EvidenceId/apk-content.json"
    }
}
finally { $env:JDK_JAVA_OPTIONS = $previousJavaOptions }
