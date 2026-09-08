<# Run outside the Codex sandbox. Requires a built Windows probe and authorized Quest over Wi-Fi. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Serial,
    [Parameter(Mandatory=$true)][string]$HostAddress,
    [string]$AdbPath = 'C:/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$run = Join-Path $repo ('.artifacts/quest-009/runs/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$runId = [Guid]::NewGuid().ToString('N')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$package = 'fr.crnl.hibop.transportprobe'
$deviceDirectory = "/sdcard/Android/data/$package/files"
$fixture = Join-Path $repo '.artifacts/quest-008/fixture/quest-anatomy.hbna'
$expectedHash = (Get-FileHash -LiteralPath $fixture).Hash.ToLowerInvariant()
# QUEST-001/006/008 public-reference MNI anatomy: no patient, electrode or EEG data.
# D14 authorizes this fixture; prevent this probe from exporting an arbitrary file.
if ($expectedHash -ne '065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12') { throw 'Only the audited reference MNI fixture is allowed.' }
$secretBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
$random.GetBytes($secretBytes)
$random.Dispose()
$secret = [Convert]::ToBase64String($secretBytes)
[Array]::Clear($secretBytes, 0, $secretBytes.Length)
$encoding = New-Object Text.UTF8Encoding $false
function Write-Config([string]$Path, $Value) { [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Compress), $encoding) }
function Invoke-Adb([string[]]$Arguments) {
    $result = & $AdbPath -s $Serial @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "ADB failed: $($Arguments[0]); $result" }
    return $result
}
$serverConfig = Join-Path $run 'server-config.json'
$clientConfig = Join-Path $run 'client-config.json'
$stopPath = Join-Path $run 'stop'
$readyPath = Join-Path $run 'ready.json'
$player = $null
try {
    Write-Config $serverConfig @{runId=$runId;mode='server';host=$HostAddress;port=5449;secret=$secret;payloadPath=$fixture;readyPath=$readyPath;stopPath=$stopPath}
    $player = Start-Process -FilePath "$repo/.artifacts/quest-009/Windows/TransportProbe.exe" -ArgumentList @('-batchmode','-nographics','-probeConfig',"`"$serverConfig`"",'-logFile',"`"$run/windows.log`"") -PassThru -WindowStyle Hidden
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while (-not (Test-Path -LiteralPath $readyPath)) {
        if ($player.HasExited -or [DateTime]::UtcNow -gt $deadline) { throw 'Windows server did not become ready.' }
        Start-Sleep -Milliseconds 100
    }
    $ready = Get-Content -LiteralPath $readyPath -Raw | ConvertFrom-Json
    Write-Config $clientConfig @{runId=$runId;mode='client';host=$HostAddress;port=5449;pin=$ready.pin;secret=$secret;expectedHash=$expectedHash;outputPath="$deviceDirectory/received.hbna"}
    $secret = $null
    Invoke-Adb @('shell','mkdir','-p',$deviceDirectory) | Out-Null
    Invoke-Adb @('push',$clientConfig,"$deviceDirectory/quest009-config.json") | Out-Null
    Remove-Item -LiteralPath $clientConfig
    Invoke-Adb @('shell','am','force-stop',$package) | Out-Null
    Invoke-Adb @('shell','am','start','-W','-n',"$package/com.unity3d.player.UnityPlayerGameActivity") | Out-Null
    $deadline = [DateTime]::UtcNow.AddSeconds(150)
    $lines = @()
    do {
        Start-Sleep -Milliseconds 500
        $log = @(Invoke-Adb @('logcat','-d','-v','brief','-s','Unity'))
        $startIndex = -1
        for ($i = 0; $i -lt $log.Count; $i++) { if ($log[$i].ToString().Contains("QUEST009_RUN $runId")) { $startIndex = $i } }
        if ($startIndex -ge 0) { $lines = $log[$startIndex..($log.Count - 1)] }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Quest probe timed out.' }
    } until (($lines -match 'QUEST009_PASS|QUEST009_FAIL').Count -gt 0)
    [IO.File]::WriteAllLines("$run/quest.log", [string[]]$lines, $encoding)
    if (($lines -match 'QUEST009_FAIL').Count -gt 0) { throw "Quest probe failed; see $run/quest.log" }
    if (($lines -match 'QUEST009_RESULT').Count -ne 9) { throw 'Quest did not report every required scenario.' }
    Invoke-Adb @('pull',"$deviceDirectory/received.hbna","$run/received.hbna") | Out-Null
    if ((Get-FileHash -LiteralPath "$run/received.hbna").Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Pulled payload hash differs from fixture.' }
    [IO.File]::WriteAllText($stopPath, 'stop', $encoding)
    if (-not $player.WaitForExit(15000)) { throw 'Windows server did not stop within 15 seconds.' }
    if ($player.ExitCode -ne 0) { throw "Windows server exit $($player.ExitCode)" }
    $serverLog = Get-Content -LiteralPath "$run/windows.log"
    if (($serverLog -match 'QUEST009_SERVER delivered').Count -ne 4 -or ($serverLog -match 'QUEST009_SERVER released').Count -ne 9) { throw 'Server did not acknowledge four deliveries and release all nine connections.' }
    Write-Output "PASS: Windows-Quest transfer, identity/pairing rejection, three interruptions/reconnections; $run"
}
finally {
    [IO.File]::WriteAllText($stopPath, 'stop', $encoding)
    if ($null -ne $player -and -not $player.HasExited) {
        if (-not $player.WaitForExit(15000)) { $player.Kill(); Write-Warning 'Probe required forced termination; lifecycle qualification failed.' }
    }
    foreach ($path in @($serverConfig,$clientConfig)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    & $AdbPath -s $Serial shell rm -f "$deviceDirectory/quest009-config.json" | Out-Null
    & $AdbPath -s $Serial shell am force-stop $package | Out-Null
    $remainingPid = & $AdbPath -s $Serial shell pidof $package
    if ($remainingPid) { Write-Warning 'Quest probe process remains.' } else { Write-Output 'Quest probe stopped; ADB and development wake settings preserved.' }
    $secret = $null
}
