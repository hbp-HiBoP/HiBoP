[CmdletBinding()]
param([ValidateSet('UnityProject', 'WindowsProject')][string]$ProjectName = 'UnityProject')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo ".artifacts/quest-009/$ProjectName"
$source = Join-Path $repo 'Spikes/QUEST-009'
New-Item -ItemType Directory -Force -Path "$project/Assets/Runtime", "$project/Assets/Editor", "$project/Packages", "$project/ProjectSettings" | Out-Null
$vendor = Join-Path $repo '.artifacts/quest-009/vendor'
$archive = Join-Path $vendor 'bouncycastle.cryptography.2.7.0.zip'
New-Item -ItemType Directory -Force -Path $vendor, "$project/Assets/Plugins" | Out-Null
if (-not (Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/bouncycastle.cryptography/2.7.0/bouncycastle.cryptography.2.7.0.nupkg' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive).Hash -ne 'F091FFCCAB4D03993E660BACE277659A79DEE0972F54D7F1F4BD46D680966241') { throw 'BouncyCastle package hash mismatch.' }
Expand-Archive -LiteralPath $archive -DestinationPath "$vendor/bouncycastle-2.7.0" -Force
Copy-Item -LiteralPath "$vendor/bouncycastle-2.7.0/lib/netstandard2.0/BouncyCastle.Cryptography.dll" -Destination "$project/Assets/Plugins"
Copy-Item -LiteralPath "$vendor/bouncycastle-2.7.0/LICENSE.md" -Destination "$project/Assets/Plugins/BouncyCastle-LICENSE.txt"
Copy-Item -LiteralPath "$repo/ProjectSettings/ProjectVersion.txt" -Destination "$project/ProjectSettings/ProjectVersion.txt"
Get-ChildItem -LiteralPath "$source/Runtime" -Filter '*.cs' | Copy-Item -Destination "$project/Assets/Runtime"
Get-ChildItem -LiteralPath "$source/Editor" -Filter '*.cs' | Copy-Item -Destination "$project/Assets/Editor"
$manifest = @'
{"dependencies":{"com.unity.modules.androidjni":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}
'@
[IO.File]::WriteAllText("$project/Packages/manifest.json", $manifest, (New-Object Text.UTF8Encoding $false))
Write-Output $project
