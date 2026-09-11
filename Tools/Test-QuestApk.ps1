[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Apk,
    [Parameter(Mandatory)][string]$ReportPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Apk))
try {
    $entries = @($archive.Entries | ForEach-Object {
        [ordered]@{ path = $_.FullName; bytes = $_.Length; compressedBytes = $_.CompressedLength }
    })
    $libraries = @($archive.Entries | Where-Object { $_.FullName -match '\.(so|dll|dylib)$' })
    foreach ($entry in $libraries) {
        if ($entry.FullName -notmatch '^lib/arm64-v8a/[^/]+\.so$') {
            throw "Unexpected native binary in Quest APK: $($entry.FullName)"
        }
        if ($entry.Name -notin @('libhbp_core.so', 'libhbp_math.so') -and $entry.Name -match '(?i)hbp_core|hbp_math|EEGFormat|hbp_export|opencv|boost') {
            throw "Desktop scientific runtime is not part of Quest bootstrap: $($entry.FullName)"
        }
        $stream = $entry.Open()
        try {
            $header = New-Object byte[] 20
            $read = 0
            while ($read -lt $header.Length) {
                $count = $stream.Read($header, $read, $header.Length - $read)
                if ($count -eq 0) { break }
                $read += $count
            }
            # Unity stores its line-number symbol table under a .so suffix.
            # This is data consumed by libil2cpp, not an ELF shared library.
            if ($entry.FullName -eq 'lib/arm64-v8a/libil2cpp.usym.so') {
                if ($read -ne 20 -or $header[0] -ne 0x73 -or $header[1] -ne 0x79 -or
                    $header[2] -ne 0x6d -or $header[3] -ne 0x2d -or
                    [BitConverter]::ToUInt32($header, 4) -ne 2) {
                    throw "Invalid Unity symbol table: $($entry.FullName)"
                }
                continue
            }
            if ($read -ne 20 -or $header[0] -ne 0x7f -or $header[1] -ne 0x45 -or
                $header[2] -ne 0x4c -or $header[3] -ne 0x46 -or $header[4] -ne 2 -or
                $header[5] -ne 1 -or $header[18] -ne 183 -or $header[19] -ne 0) {
                throw "Not an ELF64 AArch64 binary: $($entry.FullName)"
            }
        } finally { $stream.Dispose() }
    }
    foreach ($required in @('lib/arm64-v8a/libhbp_core.so', 'lib/arm64-v8a/libhbp_math.so', 'lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libunity.so', 'assets/bin/Data/boot.config')) {
        if ($null -eq $archive.GetEntry($required)) { throw "Missing APK entry: $required" }
    }
    $lockPath = Join-Path $PSScriptRoot 'NativePlugins.lock.json'
    $lock = Get-Content $lockPath -Raw | ConvertFrom-Json
    $nativePins = @{}
    foreach ($name in @('hbp_core', 'hbp_math')) {
        $library = @($lock.libraries | Where-Object name -eq $name)[0]
        $android = @($library.artifacts | Where-Object platform -eq 'Android')[0]
        $stream = $archive.GetEntry("lib/arm64-v8a/lib$name.so").Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $nativeHash = [Convert]::ToHexString($sha.ComputeHash($stream)).ToLowerInvariant() }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($nativeHash -ne $android.files[0].sha256) { throw "APK $name does not match the Android pin." }
        $nativePins[$name] = [ordered]@{ commit = $library.commit; sha256 = $nativeHash; matchesLock = $true; runtimeTested = $false }
    }
    $report = [ordered]@{
        apk = [IO.Path]::GetFullPath($Apk)
        sha256 = (Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant()
        bytes = (Get-Item -LiteralPath $Apk).Length
        nativeLibraries = @($libraries | Where-Object { $_.Name -ne 'libil2cpp.usym.so' } | ForEach-Object FullName)
        symbolTables = @($libraries | Where-Object { $_.Name -eq 'libil2cpp.usym.so' } | ForEach-Object FullName)
        result = 'Passed'
        hbpCore = $nativePins['hbp_core']
        hbpMath = $nativePins['hbp_math']
        entries = $entries
    }
    $parent = Split-Path -Parent ([IO.Path]::GetFullPath($ReportPath))
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8
    Write-Host "APK verified: $($report.nativeLibraries.Count) ARM64 libraries, $($report.bytes) bytes. Report: $ReportPath"
} finally { $archive.Dispose() }
