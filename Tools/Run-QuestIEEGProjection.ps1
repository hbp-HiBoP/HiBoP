[CmdletBinding()]
param([switch]$KeepOpen)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$player = "$repo/.artifacts/quest-021/Windows/HiBoP.6.1.0.win64/HiBoP.exe"
$fixture = "$repo/.artifacts/quest-020/fixture/quest-mni-ieeg.hibop"
$protocol = "$repo/.artifacts/quest-020/fixture/quest-020.prov"
foreach ($path in @($player, $fixture, $protocol)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing $path. Build QUEST-021 Windows and run Tools/Prepare-QuestIEEGFixture.py first." }
}
$run = "$repo/.test-results/quest-021/player-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$arguments = @('-questIEEGProtocol', "`"$protocol`"", '-pf', "`"$fixture`"", '-v', '"MNI iEEG"', '-screen-fullscreen', '0', '-logFile', "`"$run/player.log`"")
if (-not $KeepOpen) { $arguments += '-ieegEvidence', "`"$run`"", '-ieegEvidenceOnce', '-batchmode' }
$arguments | ConvertTo-Json | Set-Content "$run/command.json"
$windowStyle = if ($KeepOpen) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $player -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
if ($KeepOpen) { Write-Output "Player PID $($process.Id); log in $run."; return }
$process.WaitForExit()
$process.ExitCode | Set-Content "$run/exit-code.txt"
if ($process.ExitCode -ne 0) { throw "Player exited $($process.ExitCode); see $run/player.log" }
$result = Get-Content -LiteralPath "$run/result.json" -Raw | ConvertFrom-Json
if (-not $result.success -or $result.rows.Count -ne 18) { throw "Incomplete projection evidence: $run/result.json" }
Write-Output "$run/result.json"
