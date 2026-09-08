[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$HostAddress,
    [string]$AdbPath = 'C:/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$apk = "$repo/.artifacts/quest-010/Android/HiBoP.Quest.apk"
$fixture = "$repo/.artifacts/quest-008/fixture/quest-anatomy.hbna"
if ((Get-FileHash -LiteralPath $fixture).Hash -ne '065309CA8CC4BABE9F8C2CD9F08BB7F6AE97B1E11374E1299E0F4A416E5ADA12') { throw 'Audited MNI fixture required.' }
$runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$run = "$repo/.test-results/quest-010/runs/$runId"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$encoding = New-Object Text.UTF8Encoding $false
$serverConfig = "$run/server-config.json"
$clientConfig = "$run/client-config.json"
$readyPath = "$run/ready.json"
$deviceDirectory = "/sdcard/Android/data/$package/files"
$server = $null
function Invoke-QuestAdb([string[]]$Arguments) {
    $output = & $AdbPath -s $Serial @Arguments
    if ($LASTEXITCODE -ne 0) { throw "ADB failed: $($Arguments[0])" }
    return $output
}
try {
    Invoke-QuestAdb @('get-state') | Out-Null
    Invoke-QuestAdb @('install','-r',$apk) | Out-Null
    $activity = @(Invoke-QuestAdb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    if (-not $activity) { throw 'No launchable activity found for the installed HiBoP APK.' }
    $secretBytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($secretBytes)
    $rng.Dispose()
    $secret = [Convert]::ToBase64String($secretBytes)
    [Array]::Clear($secretBytes,0,$secretBytes.Length)
    $config = @{ runId=$runId; fixture=$fixture; secret=$secret; ready=$readyPath }
    [IO.File]::WriteAllText($serverConfig, ($config | ConvertTo-Json), $encoding)
    $serverArgs = @('-batchmode','-nographics','-logFile',"`"$run/server.log`"",'-questDeliveryConfig',"`"$serverConfig`"")
    $server = Start-Process -FilePath "$repo/.artifacts/quest-010/Windows/DeliveryServer.exe" -ArgumentList $serverArgs -PassThru -WindowStyle Hidden
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    while (-not (Test-Path -LiteralPath $readyPath)) {
        if ($server.HasExited -or [DateTime]::UtcNow -gt $deadline) { throw 'Delivery server did not become ready.' }
        Start-Sleep -Milliseconds 100
    }
    $ready = Get-Content -Raw -LiteralPath $readyPath | ConvertFrom-Json
    $config = @{runId=$runId; host=$HostAddress; port=$ready.port; pin=$ready.pin; secret=$secret}
    [IO.File]::WriteAllText($clientConfig, ($config | ConvertTo-Json), $encoding)
    $secret = $null
    $config = $null
    Invoke-QuestAdb @('shell','mkdir','-p',$deviceDirectory) | Out-Null
    Invoke-QuestAdb @('push',$clientConfig,"$deviceDirectory/quest010-config.json") | Out-Null
    Remove-Item -LiteralPath $clientConfig
    Invoke-QuestAdb @('shell','am','force-stop',$package) | Out-Null
    $launch = @(Invoke-QuestAdb @('shell','am','start','-W','-n',$activity.Trim()))
    [IO.File]::WriteAllLines("$run/launch.log", [string[]]$launch, $encoding)
    if ($launch -match 'Error|Exception') { throw 'Android refused to launch HiBoP; see launch.log.' }
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    do {
        Start-Sleep -Milliseconds 500
        $lines = @(Invoke-QuestAdb @('logcat','-d','-v','brief','-s','Unity'))
        $finished = $lines | Where-Object { $_ -match "QUEST010_(PASS|FAIL) $runId" }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Quest delivery harness timed out.' }
    } until ($finished)
    $startIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].ToString().Contains("QUEST010_RUN $runId")) { $startIndex = $i } }
    if ($startIndex -ge 0) { $lines = $lines[$startIndex..($lines.Count - 1)] }
    [IO.File]::WriteAllLines("$run/quest.log", [string[]]$lines, $encoding)
    Invoke-QuestAdb @('pull',"$deviceDirectory/quest010-result.json","$run/result.json") | Out-Null
    $result = Get-Content -Raw -LiteralPath "$run/result.json" | ConvertFrom-Json
    if ($result.runId -ne $runId -or -not $result.passed) { throw "Delivery invariants failed; see $run/result.json." }
    if (-not $server.WaitForExit(15000) -or $server.ExitCode -ne 0) { throw 'Delivery server did not finish successfully.' }
    Write-Output "PASS: actual LAN delivery into Quest renderer; $run"
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { $server.Kill(); Write-Warning 'Harness server stopped after an incomplete run.' }
    foreach ($path in @($serverConfig,$clientConfig)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    & $AdbPath -s $Serial shell rm -f "$deviceDirectory/quest010-config.json" | Out-Null
    & $AdbPath -s $Serial shell am force-stop $package | Out-Null
    $remainingPid = & $AdbPath -s $Serial shell pidof $package
    if ($remainingPid) { throw 'HiBoP remains running on Quest.' }
    Write-Output 'HiBoP stopped; no remaining PID. ADB and D26 settings preserved.'
}
# pidof deliberately returns 1 when the stopped application has no PID.
exit 0
