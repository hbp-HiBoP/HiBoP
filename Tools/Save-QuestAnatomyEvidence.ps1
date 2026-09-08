# Run ADB outside the Codex sandbox. This command only observes the running demo.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Stage,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$run = "$repo/.test-results/quest-012/$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))-$Stage"
New-Item -ItemType Directory -Path $run | Out-Null
$encoding = New-Object Text.UTF8Encoding $false
$commands = @()
function Save-Adb([string]$Name, [string[]]$Arguments) {
    $output = @(& $AdbPath -s $Serial @Arguments 2>&1)
    $code = $LASTEXITCODE
    [IO.File]::WriteAllLines("$run/$Name.txt", [string[]]$output, $encoding)
    $script:commands += @{ file="$Name.txt"; arguments=$Arguments; exitCode=$code }
    if ($code -ne 0) { throw "ADB $Name failed with exit $code; see $run" }
    return $output
}
try {
    $processId = (@(Save-Adb 'pid' @('shell','pidof','fr.crnl.hibop.quest')) -join '').Trim()
    if ($processId -notmatch '^\d+$') { throw 'Expected one running HiBoP Quest process.' }
    Save-Adb 'logcat' @('logcat','-d','-v','threadtime',"--pid=$processId") | Out-Null
    Save-Adb 'memory' @('shell','dumpsys','meminfo','fr.crnl.hibop.quest') | Out-Null
    Save-Adb 'battery' @('shell','dumpsys','battery') | Out-Null
    Save-Adb 'thermal' @('shell','dumpsys','thermalservice') | Out-Null
    Save-Adb 'wifi' @('shell','cmd','wifi','status') | Out-Null
    Save-Adb 'power-policy' @('shell','settings','get','global','stay_on_while_plugged_in') | Out-Null
    $desktopLog = "$repo/.test-results/quest-012/desktop-player.log"
    if (Test-Path -LiteralPath $desktopLog) { Copy-Item -LiteralPath $desktopLog -Destination "$run/desktop-player.log" }
}
finally {
    $files = @(Get-ChildItem -LiteralPath $run -File | ForEach-Object {
        @{path=$_.Name; bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
    })
    @{stage=$Stage; collectedAt=[DateTime]::UtcNow.ToString('O'); serial=$Serial; commands=$commands; files=$files} |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$run/collection.json" -Encoding UTF8
    Write-Output $run
}
