# Rebuild the small project archive; anatomy is resolved by HiBoP's MNIObjects.
[CmdletBinding()]
param(
    [string]$OutputDirectory = '.artifacts/quest-001/fixture',
    [ValidateSet('mni-anatomy', 'mni-contacts')][string]$Fixture = 'mni-anatomy'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path $repositoryRoot "Docs/dev/quest-autonomous/fixtures/$Fixture"
$manifest = Get-Content -LiteralPath (Join-Path $fixtureRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach ($source in $manifest.sources) {
    $sourcePath = Join-Path $repositoryRoot $source.path
    if ($source.hashEncoding -eq 'utf8-lf') {
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes([IO.File]::ReadAllText($sourcePath).Replace("`r`n", "`n"))
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $actual = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose() }
    } else {
        $actual = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    if ($actual -ne $source.sha256) { throw "Fixture source hash mismatch: $($source.path)" }
}

if (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$outputPath = Join-Path $OutputDirectory "quest-$Fixture.hibop"
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::Create)
try {
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($item in $manifest.archiveEntries) {
            $entry = $archive.CreateEntry($item.entry, [IO.Compression.CompressionLevel]::NoCompression)
            $entry.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            if ($item.entry.EndsWith('/')) { continue }
            # Canonical LF/UTF-8 without BOM, independent of Git checkout line endings.
            $content = [IO.File]::ReadAllText((Join-Path $repositoryRoot $item.source)).Replace("`r`n", "`n")
            $bytes = [Text.UTF8Encoding]::new($false).GetBytes($content)
            $entryStream = $entry.Open()
            try { $entryStream.Write($bytes, 0, $bytes.Length) } finally { $entryStream.Dispose() }
        }
    } finally { $archive.Dispose() }
} finally { $stream.Dispose() }

[pscustomobject]@{
    path = $outputPath
    sha256 = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    bytes = (Get-Item -LiteralPath $outputPath).Length
    verifiedSources = @($manifest.sources).Count
} | ConvertTo-Json
