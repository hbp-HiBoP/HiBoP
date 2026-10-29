[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Serial,
    [string]$AdbPath = 'C:/Android/Sdk/platform-tools/adb.exe',
    [string]$OutputDirectory = "$PSScriptRoot/../Logs/SelectionLab/device-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $AdbPath -s $Serial pull '/sdcard/Android/data/fr.crnl.hibop.quest.selectionlab/files/selection-lab' $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Could not retrieve the selection laboratory results. Verify that the application has been opened on this device.' }
$records = foreach ($file in Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.jsonl' -Recurse) {
    foreach ($line in [IO.File]::ReadLines($file.FullName)) {
        if (-not [string]::IsNullOrWhiteSpace($line)) { $line | ConvertFrom-Json }
    }
}
$records | Where-Object Event -eq 'trial-end' | ForEach-Object {
    [pscustomobject]@{
        Session = $_.Session; Utc = $_.Utc; Build = $_.Build; Profile = $_.Profile; Layout = $_.Layout
        Target = $_.Target; Selected = $_.Selected; Outcome = $_.Outcome; Seconds = $_.Seconds
        FirstTargetSeconds = $_.FirstTargetSeconds; Transitions = $_.Transitions; MeanFrameMs = $_.MeanFrameMs
        Ease = $_.Ease; Confidence = $_.Confidence; Effort = $_.Effort
        StabilizedFrames = $_.StabilizedFrames; TargetCue = $_.TargetCue; RandomSeed = $_.RandomSeed
        CursorStyle = $_.Settings.CursorStyle; CursorColor = $_.Settings.CursorColor
        CursorGuideRadiusMm = if ($null -ne $_.Settings.CursorGuideRadius) { $_.Settings.CursorGuideRadius * 1000 } else { $null }
        CursorOffsetXmm = if ($null -ne $_.Settings.CursorOffset) { $_.Settings.CursorOffset.x * 1000 } else { $null }
        CursorOffsetYmm = if ($null -ne $_.Settings.CursorOffset) { $_.Settings.CursorOffset.y * 1000 } else { $null }
        CursorOffsetZmm = if ($null -ne $_.Settings.CursorOffset) { $_.Settings.CursorOffset.z * 1000 } else { $null }
        Settings = ($_.Settings | ConvertTo-Json -Compress)
    }
} | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'trials.csv') -NoTypeInformation -Encoding UTF8
Write-Output "Results and trials.csv: $([IO.Path]::GetFullPath($OutputDirectory))"
