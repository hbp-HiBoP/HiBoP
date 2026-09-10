# Run outside the Windows sandbox. This file-injected benchmark is distinct from the live delivery proof.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][string]$Fixture,
    [switch]$SkipInstall,
    [string]$Apk = "$PSScriptRoot/../.artifacts/quest-023/Android/HiBoP.Quest.apk",
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$package = 'fr.crnl.hibop.quest'
$remote = "/sdcard/Android/data/$package/files"
$run = "$repo/.test-results/quest-023/benchmark-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
New-Item -ItemType Directory -Force -Path $run | Out-Null
$commands = [Collections.Generic.List[object]]::new()
function Invoke-Adb([string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    $commands.Add(@{arguments=$Arguments; exitCode=$code; utc=[DateTime]::UtcNow.ToString('o')})
    if ($code -ne 0) { throw "ADB $($Arguments[0]) failed ($code): $output" }
    return $output
}
$verified = $false
try {
    $model = (@(Invoke-Adb @('shell','getprop','ro.product.model')) -join '').Trim()
    if ($model -notmatch 'Quest') { throw "Expected Quest, found $model" }
    $verified = $true
    $apkHash = (Get-FileHash -LiteralPath $Apk).Hash.ToLowerInvariant()
    if (-not $SkipInstall) { Invoke-Adb @('install','-r',$Apk) | Set-Content "$run/install.log" }
    $installed = @(Invoke-Adb @('shell','pm','path',$package)) | Where-Object { $_ -match '^package:.*/base\.apk$' } | Select-Object -First 1
    if (-not $installed) { throw 'Installed base APK missing.' }
    $installedHash = (@(Invoke-Adb @('shell','sha256sum',$installed.Substring(8).Trim())) -join ' ').Split(' ')[0]
    if ($installedHash -ne $apkHash) { throw 'Installed APK hash differs from the selected build.' }
    & "$PSScriptRoot/Test-QuestApk.ps1" -Apk $Apk -ReportPath "$run/apk-content.json"
    Invoke-Adb @('shell','am','force-stop',$package) | Out-Null
    Invoke-Adb @('shell','mkdir','-p',$remote) | Out-Null
    $fixtures = @(Get-ChildItem -LiteralPath $Fixture -Filter '*.hbna' -File)
    if ($fixtures.Count -ne 18) { throw 'Expected the 18 Desktop comparison fixtures.' }
    Invoke-Adb @('shell','mkdir','-p',"$remote/quest023-fixtures") | Out-Null
    foreach ($file in $fixtures) { Invoke-Adb @('push',$file.FullName,"$remote/quest023-fixtures/$($file.Name)") | Out-Null }
    [IO.File]::WriteAllText("$run/marker", [Guid]::NewGuid().ToString('N'))
    Invoke-Adb @('push',"$run/marker","$remote/quest023-benchmark") | Out-Null
    # Remove only the completion files belonging to this diagnostic; stale completion cannot pass.
    Invoke-Adb @('shell','rm','-f',"$remote/quest023-benchmark-result/complete.json","$remote/quest023-benchmark-result/failure.txt") | Out-Null
    $activity = @(Invoke-Adb @('shell','cmd','package','resolve-activity','--brief',$package)) | Where-Object { $_ -like "$package/*" } | Select-Object -Last 1
    $launch = @(Invoke-Adb @('shell','am','start','-W','-n',$activity.Trim()))
    $launch | Set-Content "$run/launch.log"
    if ($launch -match 'LaunchCheckControllerRequired') { throw 'Wake the Quest controllers and dismiss the system launch prompt, then rerun this command.' }
    $deadline = [DateTime]::UtcNow.AddMinutes(5)
    do {
        Start-Sleep -Seconds 3
        $ieegPid = (@(Invoke-Adb @('shell','pidof',$package)) -join '').Trim()
        $lines = @(Invoke-Adb @('logcat','-d','-v','brief',"--pid=$ieegPid"))
        $lines | Set-Content "$run/quest.log"
        if ($lines -match 'QUEST023_BENCHMARK_FAIL') { throw "Benchmark failed: $run/quest.log" }
        if ([DateTime]::UtcNow -gt $deadline) { throw "Benchmark timed out: $run/quest.log" }
    } until ($lines -match 'QUEST023_BENCHMARK_COMPLETE')
    Invoke-Adb @('pull',"$remote/quest023-benchmark-result","$run/Android") | Out-Null
    Invoke-Adb @('shell','dumpsys','meminfo',$package) | Set-Content "$run/memory.txt"
    Invoke-Adb @('shell','dumpsys','battery') | Set-Content "$run/battery.txt"
    @{fixtures=@($fixtures | Get-FileHash | Select-Object Path,Hash); apk=$Apk; apkSha256=$apkHash; installedSha256=$installedHash; model=$model; completed=$true} | ConvertTo-Json -Depth 4 | Set-Content "$run/result.json"
    Write-Output "Benchmark exported to $run/Android. Scientific acceptance remains separate."
}
finally {
    if ($verified) {
        Invoke-Adb @('shell','am','force-stop',$package) | Out-Null
        $remaining = & $AdbPath -s $Serial shell pidof $package
        $stopped = !$remaining -and $LASTEXITCODE -eq 1
        if (-not $stopped) { throw 'Could not verify HiBoP stopped.' }
    }
    @{commands=$commands; stopped=$stopped} | ConvertTo-Json -Depth 6 | Set-Content "$run/commands.json"
}
