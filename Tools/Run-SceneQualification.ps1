[CmdletBinding()]
param(
    [string]$Player = "$PSScriptRoot/../.artifacts/scene-008/Windows/HiBoP.6.1.0.win64/HiBoP.exe",
    [string]$Evidence = "$PSScriptRoot/../.artifacts/scene-008/windows-six-modalities",
    [ValidateSet('scene-008', 'scene-008-patient')][string]$FixtureName = 'scene-008',
    [switch]$KeepOpen
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$fixture = "$repo/.artifacts/scene-008/fixture"
if (!(Test-Path -LiteralPath "$fixture/scene-008.hibop")) { throw 'Run Tools/Prepare-SceneQualificationFixture.py first.' }
$Evidence = [IO.Path]::GetFullPath($Evidence)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
$arguments = @('-questIEEGProtocol', "`"$fixture/scene-008.prov`"",
    '-pf', "`"$fixture/$FixtureName.hibop`"", '-v', '"SCENE-008 six modalities"', '-screen-fullscreen', '0', '-sceneEvidence', "`"$Evidence`"",
    '-logFile', "`"$Evidence/player.log`"")
if (!$KeepOpen) { $arguments += '-sceneEvidenceOnce', '-batchmode' }
$arguments | ConvertTo-Json | Set-Content "$Evidence/command.json"
# KeepOpen is intended for the visible Desktop pairing/transfer recipe.
if ($KeepOpen) {
    Start-Process -FilePath $Player -ArgumentList $arguments -PassThru
} else {
    $process = Start-Process -FilePath $Player -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Scientific diagnostic failed ($($process.ExitCode)); see $Evidence" }
    if (!(Test-Path "$Evidence/result.json")) { throw 'Player exited without scientific evidence.' }
    $result = Get-Content "$Evidence/result.json" -Raw | ConvertFrom-Json
    if (!$result.success) { throw 'Scientific diagnostic did not succeed.' }
}
