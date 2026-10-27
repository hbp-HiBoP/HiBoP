[CmdletBinding()]
param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $Path).Path
$forbidden = @('adb', 'adb.exe', 'AdbWinApi.dll', 'AdbWinUsbApi.dll')
if (Test-Path -LiteralPath $resolved -PathType Container) {
    $matches = @(Get-ChildItem -LiteralPath $resolved -Recurse -File | Where-Object { $_.Name -in $forbidden })
} else {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($resolved)
    try { $matches = @($archive.Entries | Where-Object { ($_.FullName -split '[/\\]')[-1] -in $forbidden } | ForEach-Object FullName) }
    finally { $archive.Dispose() }
}
if ($matches.Count) { throw "ADB found in distributable: $($matches -join ', ')" }
Write-Output "No ADB binaries: $resolved"
