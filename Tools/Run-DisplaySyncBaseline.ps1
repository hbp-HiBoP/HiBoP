[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$EvidenceRoot,
    [Parameter(Mandatory)][string]$Serial,
    [int]$Repetitions = 4,
    [int]$FirstIndex = 2,
    [string]$Visualizations = 'Small',
    [string]$Endpoint,
    [int]$HoldSeconds = 0,
    [string]$Condition = 'release-usb',
    [string]$Adb = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
if ($Repetitions -lt 1 -or $Repetitions -gt 10) { throw 'Repetitions must be between 1 and 10.' }
$Executable = (Resolve-Path -LiteralPath $Executable).Path
$Project = (Resolve-Path -LiteralPath $Project).Path
$EvidenceRoot = [IO.Path]::GetFullPath($EvidenceRoot)
New-Item -ItemType Directory -Force -Path $EvidenceRoot | Out-Null
$questProcess = (@(& $Adb -s $Serial shell pidof fr.crnl.hibop.quest) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $questProcess -notmatch '^\d+$') { throw 'Start HiBoP Quest and keep the headset awake.' }
# Only the Desktop process is restarted here. Quest caches intentionally survive.
@{
    executable = $Executable; gameAssemblySha256 = (Get-FileHash (Join-Path (Split-Path $Executable) 'GameAssembly.dll')).Hash
    projectSha256 = (Get-FileHash $Project).Hash; serial = $Serial; questPid = $questProcess
    questRestartedBetweenRepetitions = $false; repetitions = $Repetitions; firstIndex = $FirstIndex
} | ConvertTo-Json | Set-Content (Join-Path $EvidenceRoot 'cohort.json')
for ($index = $FirstIndex; $index -lt $FirstIndex + $Repetitions; $index++) {
    $run = Join-Path $EvidenceRoot "$Condition-cold$index"
    if (Test-Path -LiteralPath $run) { throw "Evidence already exists: $run" }
    New-Item -ItemType Directory -Path $run | Out-Null
    $logProcess = Start-Process -FilePath $Adb -ArgumentList @('-s', $Serial, 'logcat', '-v', 'threadtime', "--pid=$questProcess", '-T', '1') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $run 'quest.log') -RedirectStandardError (Join-Path $run 'quest-log-error.txt') -PassThru
    # Start-Process joins ArgumentList. Quote every path using Windows argument rules.
    function Quote-Argument([string]$value) {
        if ($value.Contains('"')) { throw 'Quotes are unsupported in benchmark paths.' }
        return '"' + $value.TrimEnd('\') + '"'
    }
    $arguments = @('-displaySyncEvidence', (Quote-Argument $run), '-displaySyncProject', (Quote-Argument $Project),
        '-displaySyncEvidenceOnce', '-syncBaseline', '-logFile', (Quote-Argument (Join-Path $run 'player.log')),
        '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
    $arguments += @('-displaySyncVisualizations', (Quote-Argument $Visualizations))
    if ($Endpoint) { $arguments += @('-displaySyncEndpoint', (Quote-Argument $Endpoint)) }
    if ($HoldSeconds -gt 0) { $arguments += @('-displaySyncHoldSeconds', "$HoldSeconds") }
    $displayProcess = $null
    try {
        $displayProcess = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $displayProcess.Id | Set-Content (Join-Path $run 'process-id.txt')
        Write-Output "Display reference $index started (Desktop process $($displayProcess.Id), Quest process $questProcess unchanged)."
        $deadline = [DateTime]::UtcNow.AddMinutes(12)
        while (-not $displayProcess.WaitForExit(1000)) {
            if ([DateTime]::UtcNow -gt $deadline) { throw "Owned Display diagnostic timed out: $run" }
        }
        if ($logProcess.HasExited) { throw "Quest log collector stopped unexpectedly: $run" }
    }
    finally {
        # Only these two processes were created by this diagnostic.
        if ($displayProcess -and -not $displayProcess.HasExited) { Stop-Process -InputObject $displayProcess }
        if (-not $logProcess.HasExited) { Stop-Process -InputObject $logProcess }
    }
    & $Adb -s $Serial shell dumpsys meminfo fr.crnl.hibop.quest | Set-Content (Join-Path $run 'quest-memory-after.txt')
    $displayProcess.ExitCode | Set-Content (Join-Path $run 'exit-code.txt')
    if ($displayProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $run 'display.json'))) { throw "Display diagnostic failed: $run" }
    $result = Get-Content -LiteralPath (Join-Path $run 'display.json') -Raw | ConvertFrom-Json
    if (-not $result.success -or -not $result.desktopIdleThrottlingSuspended) { throw "Uncontrolled or failed measurement: $run" }
    Write-Output "Display reference $index completed: $(@($result.runs | ForEach-Object { [Math]::Round($_.desktopAndPublicationMs) }) -join ', ') ms."
}
