[CmdletBinding()]
param([int]$Index = 50, [switch]$KeepOpen)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$player = "$repo/.artifacts/quest-020/Windows/HiBoP.6.1.0.win64/HiBoP.exe"
$fixture = "$repo/.artifacts/quest-020/fixture/quest-mni-ieeg.hibop"
if (-not (Test-Path -LiteralPath $player) -or -not (Test-Path -LiteralPath $fixture)) { throw 'Build QUEST-020 Windows and run Tools/Prepare-QuestIEEGFixture.py first.' }
$run = "$repo/.test-results/quest-020/capture-$Index-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$arguments = @('-questIEEGProtocol', "`"$repo/.artifacts/quest-020/fixture/quest-020.prov`"", '-pf', "`"$fixture`"", '-v', '"MNI iEEG"', '-screen-fullscreen', '0', '-captureAnatomy', "`"$run`"", '-captureIEEGIndex', "$Index", '-logFile', "`"$run/player.log`"")
if (-not $KeepOpen) { $arguments += '-captureOnce', '-batchmode' }
$arguments | ConvertTo-Json | Set-Content "$run/command.json"
$windowStyle = if ($KeepOpen) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $player -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
if (-not $KeepOpen) {
    if (-not $process.WaitForExit(180000)) { Stop-Process -Id $process.Id; throw "Capture timed out; see $run/player.log" }
    if ($process.ExitCode -ne 0) { throw "Capture exited $($process.ExitCode); see $run/player.log" }
    $result = Get-ChildItem -LiteralPath $run -Recurse -Filter capture.json | Select-Object -First 1
    if (-not $result) { throw "No capture produced; see $run/player.log" }
    $capture = Get-Content -LiteralPath $result.FullName -Raw | ConvertFrom-Json
    if (-not $capture.RoundTripBitExact -or -not $capture.RepeatedCaptureBitExact -or $capture.IEEG.NavigationIndex -ne $Index -or $capture.IEEG.SpanMin -ne -10 -or $capture.IEEG.SpanMax -ne 10) { throw "Capture verification failed: $($result.FullName)" }
    Write-Output $result.FullName
} else {
    Write-Output "Player PID $($process.Id); captures in $run. F8 captures the current timeline selection."
}
