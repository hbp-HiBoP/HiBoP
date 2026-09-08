<# Run outside the Codex sandbox: Unity licensing requires Windows IPC. #>
[CmdletBinding()]
param([ValidateSet('Windows', 'Android')][string]$Target = 'Windows')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$projectName = if ($Target -eq 'Windows') { 'WindowsProject' } else { 'UnityProject' }
$project = & "$PSScriptRoot/Prepare-QuestTransportProbe.ps1" -ProjectName $projectName
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$unity = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
$logRoot = Join-Path $repo '.test-results/quest-009'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
$buildTarget = if ($Target -eq 'Android') { 'Android' } else { 'Win64' }
$output = if ($Target -eq 'Android') { "$repo/.artifacts/quest-009/Android/TransportProbe.apk" } else { "$repo/.artifacts/quest-009/Windows/TransportProbe.exe" }
# Start-Process needs quoted paths because it joins ArgumentList into one string.
$arguments = @('-batchmode', '-nographics', '-projectPath', "`"$project`"", '-buildTarget', $buildTarget,
    '-executeMethod', 'TransportProbeBuilder.Build', '-probeOutput', "`"$output`"",
    '-logFile', "`"$logRoot/$($Target.ToLowerInvariant())-build.log`"", '-quit')
$previousJavaOptions = $env:JDK_JAVA_OPTIONS
try {
    if ($Target -eq 'Android') {
        # Windows packaged-app filesystem redirection can break Java's AF_UNIX
        # wakeup pipes in the user temp directory. Apply to ALL child JVMs,
        # including Gradle's daemon, without changing global Java/Windows settings.
        $socketDirectory = Join-Path $repo '.codex-temp/quest009-java'
        New-Item -ItemType Directory -Force -Path $socketDirectory | Out-Null
        $env:JDK_JAVA_OPTIONS = "$previousJavaOptions -Djdk.net.unixdomain.tmpdir=`"$socketDirectory`""
    }
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
}
finally { $env:JDK_JAVA_OPTIONS = $previousJavaOptions }
if ($process.ExitCode -ne 0) { throw "Unity $Target build failed (exit $($process.ExitCode)); see $logRoot." }
Copy-Item -LiteralPath "$repo/Spikes/QUEST-009/licenses/BouncyCastle-MIT.md" -Destination ((Split-Path -Parent $output) + '/BouncyCastle-LICENSE.md')
Write-Output $output
