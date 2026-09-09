# Run outside the Windows sandbox. Local renderer qualification, not a network transfer proof.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$Fixture,
    [switch]$SkipInstall,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$run = "$repo/.test-results/quest-014/device-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$encoding = New-Object Text.UTF8Encoding $false
$commands = @()
function Invoke-QuestAdb([string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    $script:commands += @{arguments=$Arguments; exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')}
    if ($code -ne 0) { throw "ADB $($Arguments[0]) failed: $output" }
    return $output
}
try {
    Invoke-QuestAdb @('get-state') | Out-Null
    $apk = "$repo/.artifacts/quest-014/Android/HiBoP.Quest.apk"
    $fixtureHash = (Get-FileHash -LiteralPath $Fixture).Hash.ToLowerInvariant()
    if (-not $SkipInstall) { Invoke-QuestAdb @('install','-r',$apk) | Set-Content "$run/install.log" }
    $activity = @(Invoke-QuestAdb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    if (-not $activity) { throw 'No launchable Quest activity.' }
    Invoke-QuestAdb @('shell','mkdir','-p',$remote) | Out-Null
    Invoke-QuestAdb @('push',$Fixture,"$remote/quest014-contacts.hbna") | Out-Null
    Invoke-QuestAdb @('shell','touch',"$remote/quest014-render-probe","$remote/quest012-measure") | Out-Null
    Invoke-QuestAdb @('shell','am','force-stop',$package) | Out-Null
    $launch = @(Invoke-QuestAdb @('shell','am','start','-W','-n',$activity.Trim()))
    $launch | Set-Content "$run/launch.log"
    if ($launch -match 'LaunchCheckControllerRequired') { throw 'Quest requires awake controllers. Wake both controllers and dismiss the headset prompt before retrying with -SkipInstall.' }
    $deadline = [DateTime]::UtcNow.AddSeconds(100)
    $processId = $null
    $stages = @{}
    do {
        Start-Sleep -Seconds 1
        if (-not $processId) { $processId = (@(Invoke-QuestAdb @('shell','pidof',$package)) -join '').Trim() }
        $lines = @(Invoke-QuestAdb @('logcat','-d','-v','threadtime',"--pid=$processId"))
        [IO.File]::WriteAllLines("$run/quest.log", [string[]]$lines, $encoding)
        foreach ($stage in @('surface-visible','surface-hidden','surface-hidden-scale-2')) {
            if (-not $stages.ContainsKey($stage) -and ($lines -match "QUEST014_STAGE $stage`$")) {
                Invoke-QuestAdb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/$stage-memory.txt"
                $stages[$stage] = $true
            }
        }
        if ($lines -match 'QUEST014_PROBE_FAIL') { throw "Quest contact probe failed. See $run/quest.log" }
        if ([DateTime]::UtcNow -gt $deadline) { throw "Quest contact probe timed out. See $run/quest.log" }
    } until ($lines -match 'QUEST014_PROBE_PASS')
    foreach ($stage in @('surface-visible','surface-hidden','surface-hidden-scale-2')) {
        Invoke-QuestAdb @('pull',"$remote/quest014-$stage.png","$run/$stage.png") | Out-Null
    }
    Invoke-QuestAdb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/cleared-memory.txt"
    Invoke-QuestAdb @('shell','dumpsys','battery') | Set-Content "$run/battery.txt"
    Invoke-QuestAdb @('shell','dumpsys','thermalservice') | Set-Content "$run/thermal.txt"
    @{passed=$true; fixture=$Fixture; fixtureSha256=$fixtureHash; apk=$apk; apkSha256=(Get-FileHash -LiteralPath $apk).Hash.ToLowerInvariant(); serial=$Serial; scope='local-render-only'} | ConvertTo-Json | Set-Content "$run/result.json"
    Write-Output "PASS: Quest contact renderer. Evidence: $run"
}
catch {
    @{passed=$false; error=$_.Exception.Message; serial=$Serial; scope='local-render-only'} | ConvertTo-Json | Set-Content "$run/result.json"
    throw
}
finally {
    & $AdbPath -s $Serial shell rm -f "$remote/quest014-render-probe" | Out-Null
    & $AdbPath -s $Serial shell am force-stop $package | Out-Null
    $remaining = & $AdbPath -s $Serial shell pidof $package
    $stopExit = $LASTEXITCODE
    @{commands=$commands; stopped=(!$remaining -and $stopExit -eq 1); stopPidExit=$stopExit} | ConvertTo-Json -Depth 6 | Set-Content "$run/commands.json"
    if ($remaining -or $stopExit -ne 1) { throw 'Unable to verify HiBoP stopped on Quest.' }
    Write-Output 'HiBoP stopped. ADB Wi-Fi and development power settings preserved.'
}
exit 0
