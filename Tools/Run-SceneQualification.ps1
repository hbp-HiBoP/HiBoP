[CmdletBinding()]
param(
    [string]$Player = "$PSScriptRoot/../.artifacts/scene-008/Windows/HiBoP.6.1.0.win64/HiBoP.exe",
    [string]$Evidence = "$PSScriptRoot/../.artifacts/scene-008/windows-six-modalities",
    [ValidateSet('scene-008', 'scene-008-patient', 'scene-008-patient-visual')][string]$FixtureName = 'scene-008',
    [ValidateRange(1, 3)][int]$Passes = 1,
    [switch]$KeepOpen
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$fixture = "$repo/.artifacts/scene-008/fixture"
if (!(Test-Path -LiteralPath "$fixture/$FixtureName.hibop")) { throw 'Run Tools/Prepare-SceneQualificationFixture.py first.' }
if (!(Test-Path -LiteralPath $Player -PathType Leaf)) { throw "Player not found: $Player" }
$Evidence = [IO.Path]::GetFullPath($Evidence)
if (Test-Path -LiteralPath $Evidence) { throw 'Choose a new evidence directory to exclude stale results.' }
New-Item -ItemType Directory -Path $Evidence | Out-Null
$arguments = @('-questIEEGProtocol', "`"$fixture/scene-008.prov`"",
    '-pf', "`"$fixture/$FixtureName.hibop`"", '-v', '"SCENE-008 six modalities"', '-screen-fullscreen', '0', '-sceneEvidence', "`"$Evidence`"",
    '-logFile', "`"$Evidence/player.log`"")
if (!$KeepOpen) { $arguments += '-sceneEvidenceOnce', '-batchmode' }
if ($Passes -gt 1) { $arguments += '-sceneEvidencePasses', $Passes.ToString() }
$arguments | ConvertTo-Json | Set-Content "$Evidence/command.json"
# KeepOpen is intended for the visible Desktop pairing/transfer recipe.
if ($KeepOpen) {
    Start-Process -FilePath $Player -ArgumentList $arguments -PassThru
} else {
    $process = Start-Process -FilePath $Player -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Scientific diagnostic failed ($($process.ExitCode)); see $Evidence" }
    foreach ($pass in 1..$Passes) {
        $directory = if ($Passes -eq 1) { $Evidence } else { "$Evidence/pass-$pass" }
        if (!(Test-Path "$directory/result.json")) { throw 'Player exited without scientific evidence.' }
        $result = Get-Content "$directory/result.json" -Raw | ConvertFrom-Json
        if (!$result.success -or $result.states.Count -lt 12) { throw 'Scientific diagnostic did not succeed.' }
        $expected = @('AnatomicColumn', 'CCEPColumn', 'FMRIColumn', 'IEEGColumn', 'MEGColumn', 'StaticColumn')
        foreach ($state in $result.states) {
            if ($state.columns.Count -ne 6 -or @(Compare-Object $expected @($state.columns.type | Sort-Object)).Count -ne 0) {
                throw "The six required modalities were not exercised: $directory"
            }
        }
    }
}
