<#
.SYNOPSIS
Runs local, network-free tests for Update-NativePlugins.ps1.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Update-NativePlugins.ps1") -ValidateOnly

$testRoot = Join-Path $repositoryRoot ".test-results\native-plugin-updater"
$tarTestRoot = Join-Path $workingRoot "self-test"
if (Test-Path -LiteralPath $testRoot)
{
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}
if (Test-Path -LiteralPath $tarTestRoot)
{
    Remove-WorkingItem -Path $tarTestRoot
}
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

try
{
    $packageRoot = Join-Path $testRoot "package"
    New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
    $payloadPath = Join-Path $packageRoot "Fake.dll"
    [System.IO.File]::WriteAllBytes($payloadPath, [byte[]](0..255))
    $payloadHash = (Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $commit = "0123456789abcdef0123456789abcdef01234567"

    $manifest = [ordered]@{
        repository = "Fake"
        commit = $commit
        platform = "Windows"
        architecture = "x86_64"
        configuration = "Release"
        files = @(
            [ordered]@{
                path = "Fake.dll"
                sizeBytes = 256
                sha256 = $payloadHash
            }
        )
    }
    $manifestPath = Join-Path $packageRoot "artifact-manifest.json"
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8

    $destination = ".test-results/native-plugin-updater/install/Fake.dll"
    $library = [ordered]@{
        name = "Fake"
        repository = "example/Fake"
        manifestRepository = "Fake"
        targets = @(
            [ordered]@{
                platform = "Windows"
                architecture = "x86_64"
                payload = "Fake.dll"
                destination = $destination
            }
        )
    }
    $repositoryState = [ordered]@{
        sourceSha = $commit
    }

    $manifestArguments = @{
        ManifestPath = $manifestPath
        Library = $library
        RepositoryState = $repositoryState
    }
    $package = Test-ArtifactManifest @manifestArguments
    if ($package.files.Count -ne 1 -or $package.files[0].sha256 -ne $payloadHash)
    {
        throw "Validated package did not preserve the expected payload hash."
    }

    $installedPath = Join-Path $repositoryRoot $destination
    New-Item -ItemType Directory -Path (Split-Path -Parent $installedPath) -Force | Out-Null
    [System.IO.File]::WriteAllBytes($installedPath, [byte[]](1, 2, 3))
    Install-Payload -Package $package -RequestId "self-test"
    Assert-InstalledPackages -Packages @($package)

    $scratchConfiguration = [ordered]@{
        libraries = @($library)
    }
    $scratchUpdate = "$installedPath.native-update-self-test"
    $scratchPrevious = "$installedPath.native-previous-self-test"
    [System.IO.File]::WriteAllBytes($scratchUpdate, [byte[]](7))
    [System.IO.File]::WriteAllBytes($scratchPrevious, [byte[]](8))
    Remove-InstallScratch -Configuration $scratchConfiguration -RequestId "self-test"
    if ((Test-Path -LiteralPath $scratchUpdate) -or (Test-Path -LiteralPath $scratchPrevious))
    {
        throw "Installation scratch paths were not removed."
    }

    $mutex = Enter-InstallMutex
    try
    {
        $secondMutexWasRejected = $false
        try
        {
            Enter-InstallMutex | Out-Null
        }
        catch
        {
            $secondMutexWasRejected = $true
        }
        if (!$secondMutexWasRejected)
        {
            throw "Concurrent native plugin installer was not rejected."
        }
    }
    finally
    {
        Exit-InstallMutex -Mutex $mutex
    }

    $bundleRoot = Join-Path $packageRoot "Fake.bundle"
    $bundleExecutable = Join-Path $bundleRoot "Contents\MacOS\libFake"
    New-Item -ItemType Directory -Path (Split-Path -Parent $bundleExecutable) -Force | Out-Null
    [System.IO.File]::WriteAllBytes($bundleExecutable, [byte[]](4, 5, 6))
    $bundleHash = (Get-FileHash -LiteralPath $bundleExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
    $bundleManifest = [ordered]@{
        repository = "Fake"
        commit = $commit
        platform = "MacOS"
        architecture = "arm64"
        configuration = "Release"
        files = @(
            [ordered]@{
                path = "Fake.bundle/Contents/MacOS/libFake"
                sizeBytes = 3
                sha256 = $bundleHash
            }
        )
    }
    $bundleLibrary = [ordered]@{
        name = "Fake"
        repository = "example/Fake"
        manifestRepository = "Fake"
        targets = @(
            [ordered]@{
                platform = "MacOS"
                architecture = "arm64"
                payload = "Fake.bundle"
                destination = ".test-results/native-plugin-updater/install/Fake.bundle"
            }
        )
    }
    $bundleManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $bundleArguments = @{
        ManifestPath = $manifestPath
        Library = $bundleLibrary
        RepositoryState = $repositoryState
    }
    Test-ArtifactManifest @bundleArguments | Out-Null

    $extraPayloadPath = Join-Path $bundleRoot "Contents\MacOS\Extra"
    [System.IO.File]::WriteAllBytes($extraPayloadPath, [byte[]](9, 9, 9))
    $extraFileWasRejected = $false
    try
    {
        Test-ArtifactManifest @bundleArguments | Out-Null
    }
    catch
    {
        $extraFileWasRejected = $true
    }
    Remove-Item -LiteralPath $extraPayloadPath -Force
    if (!$extraFileWasRejected)
    {
        throw "Unmanifested payload file was not rejected."
    }

    $bundleDestination = Join-Path $repositoryRoot $bundleLibrary.targets[0].destination
    if (Test-Path -LiteralPath $bundleDestination)
    {
        Remove-Item -LiteralPath $bundleDestination -Recurse -Force
    }
    Copy-Payload -Source $bundleRoot -Destination $bundleDestination
    $originalFileHash = (Get-FileHash -LiteralPath $installedPath -Algorithm SHA256).Hash
    $originalBundleHash = (Get-FileHash -LiteralPath (Join-Path $bundleDestination "Contents\MacOS\libFake") -Algorithm SHA256).Hash
    $rollbackConfiguration = [ordered]@{
        libraries = @(
            [ordered]@{
                targets = @($library.targets[0], $bundleLibrary.targets[0])
            }
        )
    }
    $rollbackBackup = Join-Path $tarTestRoot "rollback-backup"
    $savedLockFilePath = $lockFilePath
    $lockFilePath = Join-Path $testRoot "NativePlugins.lock.json"
    try
    {
        Set-Content -LiteralPath $lockFilePath -Value "original-lock" -Encoding utf8
        Backup-CurrentInstall -Configuration $rollbackConfiguration -BackupRoot $rollbackBackup
        [System.IO.File]::WriteAllBytes($installedPath, [byte[]](1))
        [System.IO.File]::WriteAllBytes(
            (Join-Path $bundleDestination "Contents\MacOS\libFake"),
            [byte[]](2))
        Set-Content -LiteralPath $lockFilePath -Value "changed-lock" -Encoding utf8
        Restore-InstallBackup -Configuration $rollbackConfiguration -BackupRoot $rollbackBackup

        if ((Get-FileHash -LiteralPath $installedPath -Algorithm SHA256).Hash -ne $originalFileHash -or
            (Get-FileHash -LiteralPath (Join-Path $bundleDestination "Contents\MacOS\libFake") -Algorithm SHA256).Hash -ne $originalBundleHash -or
            (Get-Content -LiteralPath $lockFilePath -Raw).Trim() -ne "original-lock")
        {
            throw "Native plugin backup and rollback did not restore the original files."
        }
    }
    finally
    {
        $lockFilePath = $savedLockFilePath
    }

    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $duplicateManifest = $manifest | ConvertTo-Json -Depth 5 | ConvertFrom-Json -AsHashtable
    $duplicateManifest.files = @($duplicateManifest.files[0], $duplicateManifest.files[0])
    $duplicateManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $duplicatePathWasRejected = $false
    try
    {
        Test-ArtifactManifest @manifestArguments | Out-Null
    }
    catch
    {
        $duplicatePathWasRejected = $true
    }
    if (!$duplicatePathWasRejected)
    {
        throw "Duplicate artifact manifest path was not rejected."
    }

    $unsafeManifest = $manifest | ConvertTo-Json -Depth 5 | ConvertFrom-Json -AsHashtable
    $unsafeManifest.files = @(
        [ordered]@{
            path = "../outside.dll"
            sizeBytes = 1
            sha256 = "00"
        }
    )
    $unsafeManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $unsafePathWasRejected = $false
    try
    {
        Test-ArtifactManifest @manifestArguments | Out-Null
    }
    catch
    {
        $unsafePathWasRejected = $true
    }
    if (!$unsafePathWasRejected)
    {
        throw "Unsafe artifact manifest path was not rejected."
    }

    $tarDownload = Join-Path $tarTestRoot "download"
    $tarExtraction = Join-Path $tarTestRoot "extracted"
    New-Item -ItemType Directory -Path $tarDownload -Force | Out-Null
    $tarPath = Join-Path $tarDownload "Fake-macos-arm64.tar.gz"
    & tar -czf $tarPath -C $packageRoot Fake.dll artifact-manifest.json
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to create the macOS extraction test fixture."
    }
    Expand-MacArtifact -DownloadDirectory $tarDownload -ExtractionDirectory $tarExtraction
    if (!(Test-Path -LiteralPath (Join-Path $tarExtraction "Fake.dll") -PathType Leaf))
    {
        throw "macOS artifact extraction did not produce the expected file."
    }

    $artifactRunId = 123456789
    $selectedArtifacts = @(Select-RunArtifacts -LibraryName "Fake" -RunId $artifactRunId -Artifacts @(
        [ordered]@{
            name = "Fake-windows-x64-$artifactRunId"
            expired = $false
        },
        [ordered]@{
            name = "Fake-linux-x64-ubuntu22-$artifactRunId"
            expired = $false
        },
        [ordered]@{
            name = "Fake-macos-arm64-$artifactRunId.tar.gz"
            expired = $false
        }
    ))
    $rawMacArtifact = @($selectedArtifacts | Where-Object { $_.platform -eq "MacOS" })[0]
    if (!$rawMacArtifact.rawTarGz)
    {
        throw "Raw macOS artifact was not selected for direct download."
    }

    $unexpectedArtifactWasRejected = $false
    try
    {
        Select-RunArtifacts -LibraryName "Fake" -RunId $artifactRunId -Artifacts @(
            [ordered]@{ name = "Fake-windows-x64-$artifactRunId"; expired = $false },
            [ordered]@{ name = "Fake-linux-x64-ubuntu22-$artifactRunId"; expired = $false },
            [ordered]@{ name = "unexpected"; expired = $false }
        ) | Out-Null
    }
    catch
    {
        $unexpectedArtifactWasRejected = $true
    }
    if (!$unexpectedArtifactWasRejected)
    {
        throw "Unexpected GitHub artifact was not rejected."
    }

    $configuration = Get-NativePluginConfiguration
    $state = New-OrchestrationState -Configuration $configuration -RequestId "self-test"
    if (!$state.configuration -or $state.configurationSha256.Length -ne 64)
    {
        throw "Orchestration state did not snapshot its configuration."
    }

    Write-Host "Native plugin updater local tests passed."
}
finally
{
    if (Test-Path -LiteralPath $testRoot)
    {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $tarTestRoot)
    {
        Remove-WorkingItem -Path $tarTestRoot
    }
}
