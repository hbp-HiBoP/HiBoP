# Run outside the Windows sandbox. Captures the open MNI visualization in the final Windows Player.
# Leaves the final Quest projection available for owner validation; stop HiBoP after that feedback (D23).
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$HostAddress,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$run = "$repo/.test-results/quest-019/delivery-$runId"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$serverConfig = "$run/server-private.json"
$clientConfig = "$run/client-private.json"
$player = "$repo/.artifacts/quest-019/Windows/HiBoP.6.1.0.win64/HiBoP.exe"
$fixture = "$repo/.artifacts/quest-018/fixture/quest-mni-contacts.hibop"
$commands = [Collections.Generic.List[object]]::new()
function Invoke-Adb([string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    $commands.Add(@{arguments=$Arguments; exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')})
    if ($code -ne 0) { throw "ADB $($Arguments[0]) failed ($code): $output" }
    return $output
}
$server = $null
try {
    $model = (@(Invoke-Adb @('shell','getprop','ro.product.model')) -join '').Trim()
    if ($model -notmatch 'Quest') { throw "Expected Quest, found $model" }
    $secretBytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($secretBytes)
    $rng.Dispose()
    $secret = [Convert]::ToBase64String($secretBytes)
    [Array]::Clear($secretBytes,0,$secretBytes.Length)
    [IO.File]::WriteAllText($serverConfig, (@{secret=$secret; output=$run} | ConvertTo-Json))
    $arguments = @('-pf',"`"$fixture`"",'-v','"MNI Contacts"','-screen-fullscreen','0','-densityDelivery',"`"$serverConfig`"",'-logFile',"`"$run/windows.log`"")
    $arguments | ConvertTo-Json | Set-Content "$run/windows-command.json"
    $server = Start-Process -FilePath $player -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $deadline = [DateTime]::UtcNow.AddMinutes(3)
    while (-not (Test-Path "$run/ready.json")) {
        if ($server.HasExited -or (Test-Path "$run/failure.txt") -or [DateTime]::UtcNow -gt $deadline) { throw "Windows capture failed: $run" }
        Start-Sleep -Seconds 1
    }
    $ready = Get-Content "$run/ready.json" -Raw | ConvertFrom-Json
    [IO.File]::WriteAllText($clientConfig, (@{host=$HostAddress; port=$ready.port; pin=$ready.pin; secret=$secret} | ConvertTo-Json))
    $secret = $null
    Invoke-Adb @('shell','am','force-stop',$package) | Out-Null
    Invoke-Adb @('shell','rm','-f',"$remote/quest019-received","$remote/quest019-delivery-result.json") | Out-Null
    Invoke-Adb @('push',$clientConfig,"$remote/quest019-delivery.json") | Out-Null
    Invoke-Adb @('shell','touch',"$remote/quest012-measure") | Out-Null
    Remove-Item -LiteralPath $clientConfig
    $activity = @(Invoke-Adb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    $launch = @(Invoke-Adb @('shell','am','start','-W','-n',$activity.Trim()))
    $launch | Set-Content "$run/launch.log"
    if ($launch -match 'LaunchCheckControllerRequired') { throw 'Wake the Quest controllers and dismiss the system launch prompt, then rerun this command.' }
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    do {
        Start-Sleep -Seconds 2
        & $AdbPath -s $Serial shell test -f "$remote/quest019-received"
        $received = $LASTEXITCODE -eq 0
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Quest did not publish density after reception.' }
    } until ($received)
    $densityPid = (@(Invoke-Adb @('shell','pidof',$package)) -join '').Trim()
    Invoke-Adb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/memory-before.txt"
    $radioScript = "/data/local/tmp/quest019-$runId.sh"
    $radioLog = "/data/local/tmp/quest019-$runId.log"
    $script = @'
#!/system/bin/sh
set -e
trap 'svc wifi enable' EXIT HUP INT TERM
sleep 1
svc wifi disable
sleep 1
cmd wifi status
echo DISABLED
cat /proc/uptime
sleep 60
echo RESTORING
cat /proc/uptime
svc wifi enable
sleep 10
cmd wifi status
echo COMPLETE
'@
    [IO.File]::WriteAllText("$run/radio.sh", $script.Replace("`r`n","`n") + "`n")
    Invoke-Adb @('push',"$run/radio.sh",$radioScript) | Out-Null
    Invoke-Adb @('shell',"nohup sh $radioScript >$radioLog 2>&1 </dev/null &") | Out-Null
    Start-Sleep -Seconds 50
    Start-Sleep -Seconds 25
    & $AdbPath connect $Serial | Out-Null
    Invoke-Adb @('pull',$radioLog,"$run/radio.log") | Out-Null
    Invoke-Adb @('pull',"$remote/quest019-delivery-result.json","$run/device-result.json") | Out-Null
    $result = Get-Content "$run/device-result.json" -Raw | ConvertFrom-Json
    $radio = Get-Content "$run/radio.log" -Raw
    if (-not $result.passed -or $result.contentHash -ne $ready.hash -or $radio -notmatch 'Wifi is disabled' -or $radio -notmatch 'COMPLETE') { throw "Offline qualification failed: $run" }
    if ((@(Invoke-Adb @('shell','pidof',$package)) -join '').Trim() -ne $densityPid) { throw 'Quest process changed during outage.' }
    Invoke-Adb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/memory-after.txt"
    Invoke-Adb @('shell','dumpsys','battery') | Set-Content "$run/battery.txt"
    Invoke-Adb @('shell','dumpsys','thermalservice') | Set-Content "$run/thermal.txt"
    Invoke-Adb @('logcat','-d','-v','threadtime',"--pid=$densityPid") | Set-Content "$run/quest.log"
    @{passed=$true; player=$player; playerSha256=(Get-FileHash $player).Hash; contentHash=$ready.hash; processId=$densityPid; ownerValidation='PENDING'} | ConvertTo-Json | Set-Content "$run/result.json"
    Write-Output "Offline density verified: $run. Quest remains open for manual validation."
}
finally {
    foreach ($path in @($serverConfig,$clientConfig)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id; $server.WaitForExit() }
    @{commands=$commands; windowsStopped=($server -and $server.HasExited); questLeftForOwner=$true} | ConvertTo-Json -Depth 6 | Set-Content "$run/commands.json"
}
