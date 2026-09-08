# Unity invocation must run outside the Codex sandbox (licensing IPC).
[CmdletBinding()]
param([ValidateSet('Windows','Android')][string]$Target = 'Android')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$unity = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
New-Item -ItemType Directory -Force -Path "$repo/.test-results/quest-010" | Out-Null
if ($Target -eq 'Windows') {
    $project = "$repo/.artifacts/quest-010/WindowsProject"
    New-Item -ItemType Directory -Force -Path "$project/Assets/Runtime", "$project/Assets/Editor", "$project/Assets/Plugins", "$project/Packages", "$project/ProjectSettings" | Out-Null
    Copy-Item "$repo/Assets/Scripts/HBP/Transfer/Transport/*.cs" "$project/Assets/Runtime"
    Copy-Item "$repo/Assets/Scripts/HBP/Transfer/Anatomy/*.cs" "$project/Assets/Runtime"
    Copy-Item "$repo/Assets/Scripts/HBP/Transfer/Anatomy/Delivery/AnatomyDelivery.cs" "$project/Assets/Runtime"
    Copy-Item "$repo/Spikes/QUEST-010/Server/DeliveryServer.cs" "$project/Assets/Runtime"
    Copy-Item "$repo/Spikes/QUEST-010/Editor/DeliveryProbeBuilder.cs" "$project/Assets/Editor"
    Copy-Item "$repo/Assets/Plugins/Managed/BouncyCastle/BouncyCastle.Cryptography.dll" "$project/Assets/Plugins"
    Copy-Item "$repo/ProjectSettings/ProjectVersion.txt" "$project/ProjectSettings"
    [IO.File]::WriteAllText("$project/Packages/manifest.json", '{"dependencies":{"com.unity.modules.androidjni":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}', (New-Object Text.UTF8Encoding $false))
    $arguments = @('-batchmode','-nographics','-projectPath',"`"$project`"",'-buildTarget','Win64',
        '-executeMethod','DeliveryProbeBuilder.Build','-probeOutput',"`"$repo/.artifacts/quest-010/Windows/DeliveryServer.exe`"",
        '-logFile',"`"$repo/.test-results/quest-010/windows-build.log`"",'-quit')
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Unity Windows build failed with exit $($process.ExitCode)." }
    Copy-Item "$repo/Assets/Plugins/Managed/BouncyCastle/LICENSE.txt" "$repo/.artifacts/quest-010/Windows/BouncyCastle-LICENSE.txt"
    return
}
$previousJavaOptions = $env:JDK_JAVA_OPTIONS
try {
    $socketDirectory = "$repo/.codex-temp/quest010-java"
    New-Item -ItemType Directory -Force -Path $socketDirectory | Out-Null
    $env:JDK_JAVA_OPTIONS = "$previousJavaOptions -Djdk.net.unixdomain.tmpdir=`"$socketDirectory`""
    $arguments = @('-batchmode','-nographics','-projectPath',"`"$repo`"",'-activeBuildProfile','Assets/Settings/BuildProfiles/Quest.asset',
        '-executeMethod','HBP.Dev.HBPBuilder.BuildFromCommandLine','-buildOutput',"`"$repo/.artifacts/quest-010/Android`"",'-developmentBuild',
        '-logFile',"`"$repo/.test-results/quest-010/android-build.log`"",'-quit','-forgetProjectPath')
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Unity Android build failed with exit $($process.ExitCode)." }
}
finally { $env:JDK_JAVA_OPTIONS = $previousJavaOptions }
Copy-Item -LiteralPath "$repo/Assets/Plugins/Managed/BouncyCastle/LICENSE.txt" -Destination "$repo/.artifacts/quest-010/Android/BouncyCastle-LICENSE.txt"
