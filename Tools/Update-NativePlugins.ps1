<#
.SYNOPSIS
Builds the native HiBoP plugins on GitHub and installs the validated artifacts.

.DESCRIPTION
The script pins the latest commit from the master branch of EEGFormat, hbp_core
and hbp_math, dispatches their native workflows, waits for all platforms,
validates every artifact manifest, and replaces the Unity plugins as one
rollback-capable transaction.

.EXAMPLE
.\Tools\Update-NativePlugins.ps1

.EXAMPLE
.\Tools\Update-NativePlugins.ps1 -Resume hibop-native-20260828-143015-a83f2c1d

.EXAMPLE
.\Tools\Update-NativePlugins.ps1 -ValidateOnly
#>

[CmdletBinding()]
param(
    [string]$Resume,
    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$configurationPath = Join-Path $PSScriptRoot "NativePlugins.json"
$lockFilePath = Join-Path $PSScriptRoot "NativePlugins.lock.json"
$workingRoot = Join-Path $repositoryRoot ".native-plugin-update"
$runDiscoveryTimeout = [TimeSpan]::FromMinutes(5)
$workflowTimeout = [TimeSpan]::FromHours(3)
$script:installMutexHeld = $false

if ($PSVersionTable.PSVersion.Major -lt 7)
{
    throw "PowerShell 7 or newer is required. Run this script with pwsh."
}

function Assert-PathWithin
{
    param(
        [string]$Path,
        [string]$Root,
        [string]$Description
    )

    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (!$fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "$Description is outside the expected root: $fullPath"
    }
    return $fullPath
}

function Remove-WorkingItem
{
    param([string]$Path)

    $safePath = Assert-PathWithin -Path $Path -Root $workingRoot -Description "Working path"
    if (Test-Path -LiteralPath $safePath)
    {
        Remove-Item -LiteralPath $safePath -Recurse -Force
    }
}

function Get-NativePluginConfiguration
{
    param([hashtable]$ConfigurationOverride)

    if (!$ConfigurationOverride -and !(Test-Path -LiteralPath $configurationPath -PathType Leaf))
    {
        throw "Native plugin configuration is missing: $configurationPath"
    }

    $configuration = if ($ConfigurationOverride) {
        $ConfigurationOverride
    }
    else {
        Get-Content -LiteralPath $configurationPath -Raw | ConvertFrom-Json -AsHashtable
    }
    if ($configuration.schemaVersion -ne 1)
    {
        throw "Unsupported native plugin configuration schema: $($configuration.schemaVersion)"
    }
    if ($configuration.branch -ne "master")
    {
        throw "Native plugins must be built from the GitHub master branch."
    }
    if (!$configuration.workflow)
    {
        throw "The native workflow name is missing."
    }

    $libraries = @($configuration.libraries)
    if ($libraries.Count -ne 3)
    {
        throw "Expected exactly three native libraries, found $($libraries.Count)."
    }

    $expectedPlatforms = @("Windows", "Linux", "MacOS")
    $destinations = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($library in $libraries)
    {
        foreach ($property in @("name", "repository", "manifestRepository"))
        {
            if (!$library[$property])
            {
                throw "Library configuration is missing '$property'."
            }
        }

        $targets = @($library.targets)
        if ($targets.Count -ne 3)
        {
            throw "Expected three platform targets for $($library.name)."
        }
        foreach ($platform in $expectedPlatforms)
        {
            if (@($targets | Where-Object { $_.platform -eq $platform }).Count -ne 1)
            {
                throw "Expected exactly one $platform target for $($library.name)."
            }
        }

        foreach ($target in $targets)
        {
            foreach ($property in @("platform", "architecture", "payload", "destination"))
            {
                if (!$target[$property])
                {
                    throw "Target configuration for $($library.name) is missing '$property'."
                }
            }

            $destination = Assert-PathWithin -Path (Join-Path $repositoryRoot $target.destination) -Root $repositoryRoot -Description "Plugin destination"
            if (!$destinations.Add($destination))
            {
                throw "Duplicate native plugin destination: $destination"
            }
            if (!(Test-Path -LiteralPath $destination))
            {
                throw "Expected native plugin destination is missing: $destination"
            }
        }
    }
    return $configuration
}

function Invoke-GitHub
{
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = @(& gh @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0)
    {
        throw "GitHub CLI failed: gh $($Arguments -join ' ')$([Environment]::NewLine)$($output -join [Environment]::NewLine)"
    }
    return $output -join [Environment]::NewLine
}

function Invoke-GitHubRawDownload
{
    param(
        [Parameter(Mandatory)]
        [string]$Uri,
        [Parameter(Mandatory)]
        [string]$Destination,
        [Parameter(Mandatory)]
        [long]$ExpectedSize,
        [Parameter(Mandatory)]
        [string]$ExpectedSha256
    )

    $destinationPath = Assert-PathWithin -Path $Destination -Root $workingRoot -Description "Artifact download"
    $temporaryPath = "$destinationPath.partial"
    $errorPath = "$destinationPath.stderr"
    New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
    Remove-WorkingItem -Path $temporaryPath
    Remove-WorkingItem -Path $errorPath

    $ghCommand = Get-Command gh -ErrorAction Stop
    try
    {
        $process = Start-Process -FilePath $ghCommand.Source `
            -ArgumentList @("api", $Uri) `
            -RedirectStandardOutput $temporaryPath `
            -RedirectStandardError $errorPath `
            -Wait `
            -PassThru `
            -NoNewWindow
        $errorOutput = if (Test-Path -LiteralPath $errorPath) {
            [System.IO.File]::ReadAllText($errorPath).Trim()
        }
        else {
            ""
        }
        if ($process.ExitCode -ne 0)
        {
            throw "GitHub CLI failed while downloading raw artifact: $errorOutput"
        }

        $downloadedFile = Get-Item -LiteralPath $temporaryPath
        if ($downloadedFile.Length -ne $ExpectedSize)
        {
            throw "Raw artifact size mismatch: expected $ExpectedSize bytes, downloaded $($downloadedFile.Length)."
        }
        $actualSha256 = (Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualSha256 -ne $ExpectedSha256)
        {
            throw "Raw artifact SHA-256 mismatch: expected $ExpectedSha256, downloaded $actualSha256."
        }

        Move-Item -LiteralPath $temporaryPath -Destination $destinationPath -Force
    }
    finally
    {
        Remove-WorkingItem -Path $temporaryPath
        Remove-WorkingItem -Path $errorPath
    }
}

function Select-RunArtifacts
{
    param(
        [string]$LibraryName,
        [long]$RunId,
        [object[]]$Artifacts
    )

    $specifications = @(
        [ordered]@{
            platform = "Windows"
            names = @("$LibraryName-windows-x64-$RunId")
        },
        [ordered]@{
            platform = "Linux"
            names = @("$LibraryName-linux-x64-ubuntu22-$RunId")
        },
        [ordered]@{
            platform = "MacOS"
            names = @(
                "$LibraryName-macos-arm64-$RunId",
                "$LibraryName-macos-arm64-$RunId.tar.gz")
        }
    )

    if ($Artifacts.Count -ne $specifications.Count)
    {
        $names = @($Artifacts | ForEach-Object { $_.name }) -join ", "
        throw "Expected exactly three artifacts for $LibraryName run $RunId, found $($Artifacts.Count): $names"
    }

    return @($specifications | ForEach-Object {
        $specification = $_
        $matches = @($Artifacts | Where-Object {
            $artifactName = [string]$_.name
            @($specification.names | Where-Object {
                [string]::Equals($_, $artifactName, [System.StringComparison]::Ordinal)
            }).Count -eq 1
        })
        if ($matches.Count -ne 1)
        {
            throw "Expected one $($specification.platform) artifact for $LibraryName run $RunId, found $($matches.Count)."
        }
        if ($matches[0].expired)
        {
            throw "Artifact '$($matches[0].name)' has expired."
        }
        [ordered]@{
            platform = $specification.platform
            artifact = $matches[0]
            rawTarGz = $matches[0].name.EndsWith(".tar.gz", [System.StringComparison]::Ordinal)
        }
    })
}

function Save-RunArtifacts
{
    param(
        [hashtable]$Library,
        [hashtable]$RepositoryState,
        [string]$DownloadDirectory
    )

    $runId = [long]$RepositoryState.runId
    $json = Invoke-GitHub -Arguments @(
        "api", "repos/$($RepositoryState.repository)/actions/runs/$runId/artifacts")
    $response = $json | ConvertFrom-Json -AsHashtable
    $selectedArtifacts = @(Select-RunArtifacts `
        -LibraryName $Library.name `
        -RunId $runId `
        -Artifacts @($response.artifacts))

    foreach ($selected in $selectedArtifacts)
    {
        $artifact = $selected.artifact
        $artifactDirectory = Join-Path $DownloadDirectory $selected.platform.ToLowerInvariant()
        New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
        if (!$selected.rawTarGz)
        {
            Invoke-GitHub -Arguments @(
                "run", "download", ([string]$runId),
                "-R", $RepositoryState.repository,
                "-n", $artifact.name,
                "-D", $artifactDirectory) | Out-Null
            continue
        }

        if (!([string]$artifact.digest -match '^sha256:([0-9a-fA-F]{64})$'))
        {
            throw "Raw artifact '$($artifact.name)' is missing a valid GitHub SHA-256 digest."
        }
        $expectedSha256 = $Matches[1].ToLowerInvariant()
        if ([long]$artifact.size_in_bytes -le 0)
        {
            throw "Raw artifact '$($artifact.name)' has an invalid size."
        }
        $expectedUri = "https://api.github.com/repos/$($RepositoryState.repository)/actions/artifacts/$($artifact.id)/zip"
        if (![string]::Equals(
            [string]$artifact.archive_download_url,
            $expectedUri,
            [System.StringComparison]::OrdinalIgnoreCase))
        {
            throw "Unexpected download URL for raw artifact '$($artifact.name)'."
        }
        Invoke-GitHubRawDownload `
            -Uri $artifact.archive_download_url `
            -Destination (Join-Path $artifactDirectory $artifact.name) `
            -ExpectedSize ([long]$artifact.size_in_bytes) `
            -ExpectedSha256 $expectedSha256
    }
}

function Assert-GitHubReady
{
    if (!(Get-Command gh -ErrorAction SilentlyContinue))
    {
        throw "GitHub CLI is required. Install it, run 'gh auth login', then retry."
    }
    Invoke-GitHub -Arguments @("auth", "status") | Out-Null
}

function Assert-WorkflowSupportsOrchestration
{
    param(
        [hashtable]$Configuration,
        [hashtable]$Library
    )

    $yaml = Invoke-GitHub -Arguments @(
        "workflow", "view", $Configuration.workflow,
        "-R", $Library.repository,
        "-r", $Configuration.branch,
        "--yaml")
    foreach ($requiredText in @("request_id:", "source_sha:", "inputs.source_sha"))
    {
        if (!$yaml.Contains($requiredText, [System.StringComparison]::Ordinal))
        {
            throw "$($Library.repository)/$($Configuration.branch) does not yet contain the orchestrated workflow changes ('$requiredText')."
        }
    }
}

function Get-TrackedInstallPaths
{
    param([hashtable]$Configuration)

    $paths = @($Configuration.libraries | ForEach-Object {
        $_.targets | ForEach-Object { $_.destination }
    })
    $paths += [System.IO.Path]::GetRelativePath($repositoryRoot, $lockFilePath).Replace("\", "/")
    return $paths
}

function Assert-InstallTargetsClean
{
    param([hashtable]$Configuration)

    $paths = @(
        "Assets/Plugins/Native",
        [System.IO.Path]::GetRelativePath($repositoryRoot, $lockFilePath).Replace("\", "/")
    )
    Push-Location $repositoryRoot
    try
    {
        $status = @(& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- @paths)
        if ($LASTEXITCODE -ne 0)
        {
            throw "Git failed while checking native plugin destinations."
        }
        if ($status.Count -gt 0)
        {
            throw "Native plugin destinations already contain uncommitted changes:$([Environment]::NewLine)$($status -join [Environment]::NewLine)"
        }
    }
    finally
    {
        Pop-Location
    }
}

function Write-State
{
    param(
        [hashtable]$State,
        [string]$StatePath
    )

    $temporaryPath = "$StatePath.tmp"
    $State.updatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    $State | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $temporaryPath -Encoding utf8
    Move-Item -LiteralPath $temporaryPath -Destination $StatePath -Force
}

function New-OrchestrationState
{
    param(
        [hashtable]$Configuration,
        [string]$RequestId
    )

    return [ordered]@{
        schemaVersion = 1
        requestId = $RequestId
        configurationSha256 = (Get-FileHash -LiteralPath $configurationPath -Algorithm SHA256).Hash.ToLowerInvariant()
        configuration = $Configuration
        createdAt = [DateTimeOffset]::UtcNow.ToString("O")
        updatedAt = [DateTimeOffset]::UtcNow.ToString("O")
        phase = "initialized"
        repositories = @($Configuration.libraries | ForEach-Object {
            [ordered]@{
                name = $_.name
                repository = $_.repository
                sourceSha = $null
                runId = $null
                runUrl = $null
                status = $null
                conclusion = $null
                downloaded = $false
            }
        })
    }
}

function Get-RunForRequest
{
    param(
        [hashtable]$Configuration,
        [hashtable]$RepositoryState,
        [string]$RequestId
    )

    $json = Invoke-GitHub -Arguments @(
        "run", "list",
        "-R", $RepositoryState.repository,
        "-w", $Configuration.workflow,
        "-b", $Configuration.branch,
        "-e", "workflow_dispatch",
        "--limit", "50",
        "--json", "databaseId,displayTitle,headBranch,headSha,status,conclusion,url,createdAt")
    $runs = @($json | ConvertFrom-Json -AsHashtable)
    $matches = @($runs | Where-Object {
        $_.displayTitle -and $_.displayTitle.EndsWith(
            " - $RequestId",
            [System.StringComparison]::Ordinal)
    })
    if ($matches.Count -gt 1)
    {
        throw "Multiple workflow runs match request '$RequestId' in $($RepositoryState.repository)."
    }
    if ($matches.Count -eq 1)
    {
        return $matches[0]
    }
    return $null
}

function Wait-ForRunDiscovery
{
    param(
        [hashtable]$Configuration,
        [hashtable]$RepositoryState,
        [string]$RequestId
    )

    $deadline = [DateTimeOffset]::UtcNow + $runDiscoveryTimeout
    do
    {
        $run = Get-RunForRequest -Configuration $Configuration -RepositoryState $RepositoryState -RequestId $RequestId
        if ($run)
        {
            return $run
        }
        Start-Sleep -Seconds 3
    }
    while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "Timed out while finding the workflow run for $($RepositoryState.repository)."
}

function Wait-ForWorkflowRuns
{
    param(
        [hashtable]$State,
        [string]$StatePath
    )

    $deadline = [DateTimeOffset]::UtcNow + $workflowTimeout
    while ($true)
    {
        $allCompleted = $true
        foreach ($repositoryState in $State.repositories)
        {
            $json = Invoke-GitHub -Arguments @(
                "run", "view", ([string]$repositoryState.runId),
                "-R", $repositoryState.repository,
                "--json", "status,conclusion,url")
            $run = $json | ConvertFrom-Json -AsHashtable
            if ($repositoryState.status -ne $run.status)
            {
                Write-Host "$($repositoryState.name): $($run.status)"
            }
            $repositoryState.status = $run.status
            $repositoryState.conclusion = $run.conclusion
            $repositoryState.runUrl = $run.url
            if ($run.status -ne "completed")
            {
                $allCompleted = $false
            }
        }

        Write-State -State $State -StatePath $StatePath
        if ($allCompleted)
        {
            return
        }
        if ([DateTimeOffset]::UtcNow -ge $deadline)
        {
            throw "Timed out while waiting for native workflows."
        }
        Start-Sleep -Seconds 10
    }
}

function Expand-MacArtifact
{
    param(
        [string]$DownloadDirectory,
        [string]$ExtractionDirectory
    )

    $archives = @(Get-ChildItem -LiteralPath $DownloadDirectory -Recurse -File -Filter "*.tar.gz")
    if ($archives.Count -ne 1)
    {
        throw "Expected exactly one macOS tar.gz under $DownloadDirectory, found $($archives.Count)."
    }
    if (!(Get-Command tar -ErrorAction SilentlyContinue))
    {
        throw "The tar command is required to extract macOS artifacts."
    }

    $entries = @(& tar -tzf $archives[0].FullName)
    if ($LASTEXITCODE -ne 0 -or $entries.Count -eq 0)
    {
        throw "Unable to list macOS artifact: $($archives[0].FullName)"
    }
    foreach ($entry in $entries)
    {
        $normalized = $entry.Replace("\", "/")
        if ([System.IO.Path]::IsPathRooted($normalized) -or
            @($normalized.Split("/") | Where-Object { $_ -eq ".." }).Count -gt 0)
        {
            throw "Unsafe path in macOS artifact: $entry"
        }
    }

    $verboseEntries = @(& tar -tvzf $archives[0].FullName)
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to inspect macOS artifact entry types."
    }
    foreach ($entry in $verboseEntries)
    {
        $type = $entry.TrimStart()[0]
        if ($type -ne "-" -and $type -ne "d")
        {
            throw "Links and special files are not allowed in macOS artifacts: $entry"
        }
    }

    Remove-WorkingItem -Path $ExtractionDirectory
    New-Item -ItemType Directory -Path $ExtractionDirectory -Force | Out-Null
    & tar -xzf $archives[0].FullName -C $ExtractionDirectory
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to extract macOS artifact: $($archives[0].FullName)"
    }

    $reparsePoints = @(Get-ChildItem -LiteralPath $ExtractionDirectory -Recurse -Force |
        Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 })
    if ($reparsePoints.Count -gt 0)
    {
        throw "Reparse points are not allowed in extracted artifacts: $($reparsePoints[0].FullName)"
    }
}

function Assert-SafeManifestPath
{
    param(
        [string]$PackageRoot,
        [string]$RelativePath
    )

    if (!$RelativePath)
    {
        throw "Artifact manifest contains an empty path."
    }
    $normalized = $RelativePath.Replace("\", "/")
    if ([System.IO.Path]::IsPathRooted($normalized) -or
        @($normalized.Split("/") | Where-Object { $_ -eq ".." }).Count -gt 0)
    {
        throw "Unsafe path in artifact manifest: $RelativePath"
    }
    return Assert-PathWithin -Path (Join-Path $PackageRoot $normalized) -Root $PackageRoot -Description "Artifact file"
}

function Test-ArtifactManifest
{
    param(
        [string]$ManifestPath,
        [hashtable]$Library,
        [hashtable]$RepositoryState
    )

    $packageRoot = Split-Path -Parent $ManifestPath
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    if ($manifest.repository -ne $Library.manifestRepository)
    {
        throw "Unexpected repository '$($manifest.repository)' in $ManifestPath."
    }
    if ($manifest.commit -ne $RepositoryState.sourceSha)
    {
        throw "Artifact commit '$($manifest.commit)' does not match requested commit '$($RepositoryState.sourceSha)'."
    }
    if ($manifest.configuration -ne "Release")
    {
        throw "Artifact is not a Release build: $ManifestPath"
    }

    $targets = @($Library.targets | Where-Object { $_.platform -eq $manifest.platform })
    if ($targets.Count -ne 1)
    {
        throw "Unexpected platform '$($manifest.platform)' for $($Library.name)."
    }
    $target = $targets[0]
    if ($manifest.architecture -ne $target.architecture)
    {
        throw "Unexpected architecture '$($manifest.architecture)' for $($Library.name) $($target.platform)."
    }

    $manifestFiles = @($manifest.files)
    if ($manifestFiles.Count -eq 0)
    {
        throw "Artifact manifest contains no files: $ManifestPath"
    }

    $validatedFiles = @()
    $manifestPaths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($file in $manifestFiles)
    {
        $normalizedManifestPath = ([string]$file.path).Replace("\", "/")
        if (!$manifestPaths.Add($normalizedManifestPath))
        {
            throw "Duplicate path in artifact manifest: $normalizedManifestPath"
        }
        $fullPath = Assert-SafeManifestPath -PackageRoot $packageRoot -RelativePath $file.path
        if (!(Test-Path -LiteralPath $fullPath -PathType Leaf))
        {
            throw "Artifact file is missing: $fullPath"
        }
        $item = Get-Item -LiteralPath $fullPath
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)
        {
            throw "Reparse points are not allowed in artifact payloads: $fullPath"
        }
        if ($item.Length -ne [long]$file.sizeBytes)
        {
            throw "Artifact file size mismatch: $fullPath"
        }
        $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne ([string]$file.sha256).ToLowerInvariant())
        {
            throw "Artifact SHA-256 mismatch: $fullPath"
        }
        $validatedFiles += [ordered]@{
            path = $normalizedManifestPath
            sizeBytes = [long]$file.sizeBytes
            sha256 = $hash
        }
    }

    $payloadPath = Join-Path $packageRoot $target.payload
    if (!(Test-Path -LiteralPath $payloadPath))
    {
        throw "Expected payload is missing: $payloadPath"
    }
    $payloadPrefix = $target.payload.Replace("\", "/").TrimEnd("/")
    $payloadFiles = @($validatedFiles | Where-Object {
        $_.path -eq $payloadPrefix -or
        $_.path.StartsWith("$payloadPrefix/", [System.StringComparison]::Ordinal)
    })
    if ($payloadFiles.Count -eq 0)
    {
        throw "Payload '$payloadPrefix' is not covered by the artifact manifest."
    }
    if ((Test-Path -LiteralPath $payloadPath -PathType Leaf) -and
        ($payloadFiles.Count -ne 1 -or $payloadFiles[0].path -ne $payloadPrefix))
    {
        throw "File payload '$payloadPrefix' has an unexpected manifest layout."
    }

    $actualPayloadFiles = if (Test-Path -LiteralPath $payloadPath -PathType Leaf) {
        @($payloadPrefix)
    }
    else {
        $payloadItems = @(Get-ChildItem -LiteralPath $payloadPath -Recurse -Force)
        $reparsePoints = @($payloadItems | Where-Object {
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        })
        if ($reparsePoints.Count -gt 0)
        {
            throw "Reparse points are not allowed in artifact payloads: $($reparsePoints[0].FullName)"
        }
        @($payloadItems | Where-Object { !$_.PSIsContainer } | ForEach-Object {
            [System.IO.Path]::GetRelativePath($packageRoot, $_.FullName).Replace("\", "/")
        })
    }
    $expectedPayloadFiles = @($payloadFiles | ForEach-Object { $_.path })
    $unexpectedFiles = @($actualPayloadFiles | Where-Object { $_ -notin $expectedPayloadFiles })
    $missingFiles = @($expectedPayloadFiles | Where-Object { $_ -notin $actualPayloadFiles })
    if ($unexpectedFiles.Count -gt 0 -or $missingFiles.Count -gt 0)
    {
        throw "Artifact payload files do not exactly match the manifest for '$payloadPrefix'."
    }

    $installFiles = @($payloadFiles | ForEach-Object {
        $relativePath = if ($_.path -eq $payloadPrefix) {
            ""
        }
        else {
            $_.path.Substring($payloadPrefix.Length + 1)
        }
        [ordered]@{
            relativePath = $relativePath
            sizeBytes = $_.sizeBytes
            sha256 = $_.sha256
        }
    })

    return [ordered]@{
        library = $Library.name
        repository = $Library.repository
        platform = $target.platform
        architecture = $target.architecture
        sourcePath = $payloadPath
        destination = $target.destination
        files = $installFiles
    }
}

function Get-ValidatedPackages
{
    param(
        [hashtable]$Configuration,
        [hashtable]$State,
        [string]$RequestDirectory,
        [string]$StatePath
    )

    $packages = @()
    foreach ($library in $Configuration.libraries)
    {
        $repositoryState = @($State.repositories | Where-Object { $_.name -eq $library.name })[0]
        $downloadDirectory = Join-Path $RequestDirectory "downloads\$($library.name)"
        if (!$repositoryState.downloaded -or !(Test-Path -LiteralPath $downloadDirectory -PathType Container))
        {
            Remove-WorkingItem -Path $downloadDirectory
            New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
            Write-Host "Downloading artifacts for $($library.name)..."
            Save-RunArtifacts `
                -Library $library `
                -RepositoryState $repositoryState `
                -DownloadDirectory $downloadDirectory
            $repositoryState.downloaded = $true
            Write-State -State $State -StatePath $StatePath
        }

        $macExtractionDirectory = Join-Path $downloadDirectory "_extracted_macos"
        Expand-MacArtifact -DownloadDirectory $downloadDirectory -ExtractionDirectory $macExtractionDirectory

        $manifestPaths = @(Get-ChildItem -LiteralPath $downloadDirectory -Recurse -File -Filter "artifact-manifest.json" |
            Select-Object -ExpandProperty FullName)
        if ($manifestPaths.Count -ne 3)
        {
            throw "Expected three artifact manifests for $($library.name), found $($manifestPaths.Count)."
        }

        $libraryPackages = @($manifestPaths | ForEach-Object {
            Test-ArtifactManifest -ManifestPath $_ -Library $library -RepositoryState $repositoryState
        })
        foreach ($platform in @("Windows", "Linux", "MacOS"))
        {
            if (@($libraryPackages | Where-Object { $_.platform -eq $platform }).Count -ne 1)
            {
                throw "Expected one validated $platform package for $($library.name)."
            }
        }
        $packages += $libraryPackages
    }

    if ($packages.Count -ne 9)
    {
        throw "Expected nine validated native packages, found $($packages.Count)."
    }
    return $packages
}

function Remove-InstallScratch
{
    param(
        [hashtable]$Configuration,
        [string]$RequestId
    )

    foreach ($library in $Configuration.libraries)
    {
        foreach ($target in $library.targets)
        {
            $destination = Assert-PathWithin -Path (Join-Path $repositoryRoot $target.destination) -Root $repositoryRoot -Description "Plugin destination"
            foreach ($scratchPath in @(
                "$destination.native-update-$RequestId",
                "$destination.native-previous-$RequestId"))
            {
                if (Test-Path -LiteralPath $scratchPath)
                {
                    Remove-Item -LiteralPath $scratchPath -Recurse -Force
                }
            }
        }
    }
}

function Enter-InstallMutex
{
    if ($script:installMutexHeld)
    {
        throw "Another native plugin installation is already active for this checkout."
    }

    $pathBytes = [System.Text.Encoding]::UTF8.GetBytes($repositoryRoot.ToLowerInvariant())
    $pathHash = [System.Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData($pathBytes)).Substring(0, 24)
    $mutex = [System.Threading.Mutex]::new($false, "Local\HiBoP.NativePlugins.$pathHash")
    try
    {
        if (!$mutex.WaitOne(0))
        {
            $mutex.Dispose()
            throw "Another native plugin installation is already active for this checkout."
        }
        $script:installMutexHeld = $true
    }
    catch [System.Threading.AbandonedMutexException]
    {
        # Ownership is granted when the previous process terminated unexpectedly.
        $script:installMutexHeld = $true
    }
    return $mutex
}

function Exit-InstallMutex
{
    param([System.Threading.Mutex]$Mutex)

    if ($Mutex)
    {
        $Mutex.ReleaseMutex()
        $Mutex.Dispose()
        $script:installMutexHeld = $false
    }
}

function Test-ExclusiveFileAccess
{
    param([string]$Path)

    try
    {
        $stream = [System.IO.File]::Open(
            $Path,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::None)
        $stream.Dispose()
        return $true
    }
    catch
    {
        return $false
    }
}

function Assert-UnityClosed
{
    $unityProcesses = @()
    try
    {
        $projectNeedle = $repositoryRoot.TrimEnd("\").ToLowerInvariant()
        $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction Stop |
            Where-Object {
                $_.CommandLine -and $_.CommandLine.ToLowerInvariant().Contains($projectNeedle)
            })
    }
    catch
    {
        $unityProcesses = @(Get-Process Unity -ErrorAction SilentlyContinue)
    }

    $unityLockFile = Join-Path $repositoryRoot "Temp\UnityLockfile"
    $lockFileIsHeld = (Test-Path -LiteralPath $unityLockFile -PathType Leaf) -and
        !(Test-ExclusiveFileAccess -Path $unityLockFile)
    if ($unityProcesses.Count -gt 0 -or $lockFileIsHeld)
    {
        throw "Close the Unity Editor for this project, then resume this request with -Resume."
    }

    $windowsDlls = @(
        "Assets/Plugins/Native/Windows/x86_64/EEGFormat.dll",
        "Assets/Plugins/Native/Windows/x86_64/hbp_core.dll",
        "Assets/Plugins/Native/Windows/x86_64/hbp_math.dll"
    )
    foreach ($relativePath in $windowsDlls)
    {
        $path = Join-Path $repositoryRoot $relativePath
        if (!(Test-ExclusiveFileAccess -Path $path))
        {
            throw "Native plugin is locked. Close Unity and retry with -Resume: $path"
        }
    }
}

function Copy-Payload
{
    param(
        [string]$Source,
        [string]$Destination
    )

    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    if (Test-Path -LiteralPath $Source -PathType Container)
    {
        Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
    }
    else
    {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
    }
}

function Backup-CurrentInstall
{
    param(
        [hashtable]$Configuration,
        [string]$BackupRoot
    )

    Remove-WorkingItem -Path $BackupRoot
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    foreach ($library in $Configuration.libraries)
    {
        foreach ($target in $library.targets)
        {
            $source = Join-Path $repositoryRoot $target.destination
            $destination = Join-Path $BackupRoot $target.destination
            Copy-Payload -Source $source -Destination $destination
        }
    }
    if (Test-Path -LiteralPath $lockFilePath -PathType Leaf)
    {
        $backupLockFile = Join-Path $BackupRoot "Tools\NativePlugins.lock.json"
        Copy-Payload -Source $lockFilePath -Destination $backupLockFile
    }
}

function Restore-InstallBackup
{
    param(
        [hashtable]$Configuration,
        [string]$BackupRoot
    )

    if (!(Test-Path -LiteralPath $BackupRoot -PathType Container))
    {
        throw "Native plugin backup is missing: $BackupRoot"
    }

    foreach ($library in $Configuration.libraries)
    {
        foreach ($target in $library.targets)
        {
            $destination = Join-Path $repositoryRoot $target.destination
            $backup = Join-Path $BackupRoot $target.destination
            if (!(Test-Path -LiteralPath $backup))
            {
                throw "Native plugin backup payload is missing: $backup"
            }
            if (Test-Path -LiteralPath $destination)
            {
                Remove-Item -LiteralPath $destination -Recurse -Force
            }
            Copy-Payload -Source $backup -Destination $destination
        }
    }

    $backupLockFile = Join-Path $BackupRoot "Tools\NativePlugins.lock.json"
    if (Test-Path -LiteralPath $backupLockFile -PathType Leaf)
    {
        Copy-Item -LiteralPath $backupLockFile -Destination $lockFilePath -Force
    }
    elseif (Test-Path -LiteralPath $lockFilePath)
    {
        Remove-Item -LiteralPath $lockFilePath -Force
    }
}

function Install-Payload
{
    param(
        [hashtable]$Package,
        [string]$RequestId
    )

    $destination = Assert-PathWithin -Path (Join-Path $repositoryRoot $Package.destination) -Root $repositoryRoot -Description "Plugin destination"
    $temporary = "$destination.native-update-$RequestId"
    $previous = "$destination.native-previous-$RequestId"
    foreach ($path in @($temporary, $previous))
    {
        if (Test-Path -LiteralPath $path)
        {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }

    Copy-Payload -Source $Package.sourcePath -Destination $temporary
    if (Test-Path -LiteralPath $Package.sourcePath -PathType Leaf)
    {
        if (!(Test-Path -LiteralPath $destination -PathType Leaf))
        {
            Move-Item -LiteralPath $temporary -Destination $destination
            return
        }

        Move-Item -LiteralPath $destination -Destination $previous
        try
        {
            Move-Item -LiteralPath $temporary -Destination $destination
            Remove-Item -LiteralPath $previous -Force
            return
        }
        catch
        {
            if (Test-Path -LiteralPath $destination)
            {
                Remove-Item -LiteralPath $destination -Force
            }
            if (Test-Path -LiteralPath $previous)
            {
                Move-Item -LiteralPath $previous -Destination $destination
            }
            throw
        }
    }

    Move-Item -LiteralPath $destination -Destination $previous
    try
    {
        Move-Item -LiteralPath $temporary -Destination $destination
        Remove-Item -LiteralPath $previous -Recurse -Force
    }
    catch
    {
        if (Test-Path -LiteralPath $destination)
        {
            Remove-Item -LiteralPath $destination -Recurse -Force
        }
        if (Test-Path -LiteralPath $previous)
        {
            Move-Item -LiteralPath $previous -Destination $destination
        }
        throw
    }
}

function Assert-InstalledPackages
{
    param([hashtable[]]$Packages)

    foreach ($package in $Packages)
    {
        $destination = Join-Path $repositoryRoot $package.destination
        foreach ($file in $package.files)
        {
            $path = if ($file.relativePath) {
                Join-Path $destination $file.relativePath
            }
            else {
                $destination
            }
            if (!(Test-Path -LiteralPath $path -PathType Leaf))
            {
                throw "Installed native plugin file is missing: $path"
            }
            $item = Get-Item -LiteralPath $path
            $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($item.Length -ne [long]$file.sizeBytes -or $hash -ne $file.sha256)
            {
                throw "Installed native plugin validation failed: $path"
            }
        }
    }
}

function New-NativePluginLock
{
    param(
        [hashtable]$Configuration,
        [hashtable]$State,
        [hashtable[]]$Packages
    )

    return [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
        requestId = $State.requestId
        branch = $Configuration.branch
        libraries = @($Configuration.libraries | ForEach-Object {
            $library = $_
            $repositoryState = @($State.repositories | Where-Object { $_.name -eq $library.name })[0]
            [ordered]@{
                name = $library.name
                repository = $library.repository
                commit = $repositoryState.sourceSha
                runId = $repositoryState.runId
                runUrl = $repositoryState.runUrl
                artifacts = @($Packages | Where-Object { $_.library -eq $library.name } | ForEach-Object {
                    [ordered]@{
                        platform = $_.platform
                        architecture = $_.architecture
                        destination = $_.destination
                        files = $_.files
                    }
                })
            }
        })
    }
}

function Assert-ExpectedInstallDiff
{
    param([hashtable]$Configuration)

    $allowedRoots = @(Get-TrackedInstallPaths -Configuration $Configuration |
        ForEach-Object { $_.Replace("\", "/").TrimEnd("/") })
    Push-Location $repositoryRoot
    try
    {
        $status = @(& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- Assets/Plugins/Native Tools/NativePlugins.lock.json)
        if ($LASTEXITCODE -ne 0)
        {
            throw "Git failed while verifying the native plugin diff."
        }
    }
    finally
    {
        Pop-Location
    }

    foreach ($line in $status)
    {
        $path = $line.Substring(3).Replace("\", "/")
        if ($path.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase))
        {
            throw "Unity metadata was unexpectedly modified: $path"
        }
        $allowed = @($allowedRoots | Where-Object {
            $path -eq $_ -or $path.StartsWith("$_/", [System.StringComparison]::OrdinalIgnoreCase)
        }).Count -gt 0
        if (!$allowed)
        {
            throw "Unexpected file changed during native plugin installation: $path"
        }
    }
}

function Get-RequestContext
{
    param([hashtable]$Configuration)

    if ($Resume)
    {
        if ($Resume -notmatch "^[A-Za-z0-9._-]+$")
        {
            throw "Invalid resume request identifier: $Resume"
        }
        $requestDirectory = Assert-PathWithin -Path (Join-Path $workingRoot $Resume) -Root $workingRoot -Description "Resume directory"
        $statePath = Join-Path $requestDirectory "state.json"
        if (!(Test-Path -LiteralPath $statePath -PathType Leaf))
        {
            throw "Resume state was not found: $statePath"
        }
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json -AsHashtable
        if ($state.schemaVersion -ne 1 -or $state.requestId -ne $Resume)
        {
            throw "Invalid resume state: $statePath"
        }
        $configurationSha256 = if (Test-Path -LiteralPath $configurationPath -PathType Leaf) {
            (Get-FileHash -LiteralPath $configurationPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        else {
            $null
        }
        if (!$state.configuration)
        {
            throw "Resume state does not contain its native plugin configuration snapshot."
        }
        $configurationChanged = $state.configurationSha256 -ne $configurationSha256
        if ($state.phase -eq "completed")
        {
            Write-Host "Native plugin request '$Resume' is already complete."
            return [ordered]@{
                completed = $true
                requestDirectory = $requestDirectory
                statePath = $statePath
                state = $state
                configuration = $state.configuration
                configurationChanged = $configurationChanged
            }
        }
        return [ordered]@{
            completed = $false
            requestDirectory = $requestDirectory
            statePath = $statePath
            state = $state
            configuration = $state.configuration
            configurationChanged = $configurationChanged
        }
    }

    $requestId = "hibop-native-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
    New-Item -ItemType Directory -Path $workingRoot -Force | Out-Null
    $requestDirectory = Join-Path $workingRoot $requestId
    New-Item -ItemType Directory -Path $requestDirectory | Out-Null
    $statePath = Join-Path $requestDirectory "state.json"
    $state = New-OrchestrationState -Configuration $Configuration -RequestId $requestId
    Write-State -State $state -StatePath $statePath
    return [ordered]@{
        completed = $false
        requestDirectory = $requestDirectory
        statePath = $statePath
        state = $state
        configuration = $Configuration
        configurationChanged = $false
    }
}

$resumeConfiguration = $null
if ($Resume)
{
    if ($Resume -notmatch "^[A-Za-z0-9._-]+$")
    {
        throw "Invalid resume request identifier: $Resume"
    }
    $resumeDirectory = Assert-PathWithin -Path (Join-Path $workingRoot $Resume) -Root $workingRoot -Description "Resume directory"
    $resumeStatePath = Join-Path $resumeDirectory "state.json"
    if (Test-Path -LiteralPath $resumeStatePath -PathType Leaf)
    {
        $resumeStatePreview = Get-Content -LiteralPath $resumeStatePath -Raw | ConvertFrom-Json -AsHashtable
        $resumeConfiguration = $resumeStatePreview.configuration
    }
}
$configuration = Get-NativePluginConfiguration -ConfigurationOverride $resumeConfiguration
if ($ValidateOnly)
{
    Write-Host "Native plugin configuration is valid."
    foreach ($library in $configuration.libraries)
    {
        Write-Host "$($library.repository): $($configuration.branch), $(@($library.targets).Count) targets"
    }
    return
}

Assert-GitHubReady
if (!$Resume)
{
    Assert-InstallTargetsClean -Configuration $configuration
}

$context = Get-RequestContext -Configuration $configuration
if ($context.completed)
{
    return
}
$requestDirectory = $context.requestDirectory
$statePath = $context.statePath
$state = $context.state
$configuration = $context.configuration
$backupRoot = Join-Path $requestDirectory "backup"

if ($Resume)
{
    $recoveryMutex = Enter-InstallMutex
    try
    {
        if ($state.phase -eq "installing")
        {
            Write-Warning "Restoring the previous native plugins after an interrupted installation."
            Restore-InstallBackup -Configuration $configuration -BackupRoot $backupRoot
            $state.phase = "rolled_back"
            Write-State -State $state -StatePath $statePath
        }
        Remove-InstallScratch -Configuration $configuration -RequestId $state.requestId
    }
    finally
    {
        Exit-InstallMutex -Mutex $recoveryMutex
    }
}

if ($context.configurationChanged)
{
    throw "NativePlugins.json changed after this request started. Any interrupted installation was restored; start a new request."
}

Assert-InstallTargetsClean -Configuration $configuration
foreach ($library in $configuration.libraries)
{
    Assert-WorkflowSupportsOrchestration -Configuration $configuration -Library $library
    $repositoryState = @($state.repositories | Where-Object { $_.name -eq $library.name })[0]
    if (!$repositoryState.sourceSha)
    {
        $sourceSha = (Invoke-GitHub -Arguments @(
            "api", "repos/$($library.repository)/commits/$($configuration.branch)",
            "--jq", ".sha")).Trim()
        if ($sourceSha -notmatch "^[0-9a-fA-F]{40}$")
        {
            throw "GitHub returned an invalid source commit for $($library.repository): $sourceSha"
        }
        $repositoryState.sourceSha = $sourceSha.ToLowerInvariant()
        Write-State -State $state -StatePath $statePath
    }
}
$state.phase = "resolved"
Write-State -State $state -StatePath $statePath

foreach ($library in $configuration.libraries)
{
    $repositoryState = @($state.repositories | Where-Object { $_.name -eq $library.name })[0]
    if (!$repositoryState.runId)
    {
        $run = Get-RunForRequest -Configuration $configuration -RepositoryState $repositoryState -RequestId $state.requestId
        if (!$run)
        {
            Write-Host "Dispatching $($library.name) from master at $($repositoryState.sourceSha)..."
            Invoke-GitHub -Arguments @(
                "workflow", "run", $configuration.workflow,
                "-R", $library.repository,
                "--ref", $configuration.branch,
                "-f", "platform=all",
                "-f", "request_id=$($state.requestId)",
                "-f", "source_sha=$($repositoryState.sourceSha)") | Out-Null
            $run = Wait-ForRunDiscovery -Configuration $configuration -RepositoryState $repositoryState -RequestId $state.requestId
        }
        $repositoryState.runId = [long]$run.databaseId
        $repositoryState.runUrl = $run.url
        $repositoryState.status = $run.status
        $repositoryState.conclusion = $run.conclusion
        Write-State -State $state -StatePath $statePath
    }
}
$state.phase = "dispatched"
Write-State -State $state -StatePath $statePath

Wait-ForWorkflowRuns -State $state -StatePath $statePath
$failedRuns = @($state.repositories | Where-Object { $_.conclusion -ne "success" })
if ($failedRuns.Count -gt 0)
{
    $state.phase = "workflow_failed"
    Write-State -State $state -StatePath $statePath
    $details = $failedRuns | ForEach-Object { "$($_.name): $($_.conclusion) - $($_.runUrl)" }
    throw "At least one native workflow failed. No plugin was changed.$([Environment]::NewLine)$($details -join [Environment]::NewLine)"
}

$state.phase = "downloading"
Write-State -State $state -StatePath $statePath
try
{
    $packages = @(Get-ValidatedPackages -Configuration $configuration -State $state -RequestDirectory $requestDirectory -StatePath $statePath)
}
catch
{
    foreach ($repositoryState in $state.repositories)
    {
        $repositoryState.downloaded = $false
    }
    $state.phase = "artifact_validation_failed"
    Write-State -State $state -StatePath $statePath
    throw
}
$state.phase = "validated"
Write-State -State $state -StatePath $statePath

$installMutex = Enter-InstallMutex
$cleanupInstallScratch = $false
try
{
    try
    {
        Assert-UnityClosed
    }
    catch
    {
        $state.phase = "ready_to_install"
        Write-State -State $state -StatePath $statePath
        throw "$($_.Exception.Message) Request: $($state.requestId)"
    }

    Assert-InstallTargetsClean -Configuration $configuration
    Remove-InstallScratch -Configuration $configuration -RequestId $state.requestId
    $state.phase = "backing_up"
    Write-State -State $state -StatePath $statePath
    Backup-CurrentInstall -Configuration $configuration -BackupRoot $backupRoot
    $state.phase = "installing"
    Write-State -State $state -StatePath $statePath

    try
    {
        foreach ($package in $packages)
        {
            Write-Host "Installing $($package.library) $($package.platform)..."
            Install-Payload -Package $package -RequestId $state.requestId
        }

        $lock = New-NativePluginLock -Configuration $configuration -State $state -Packages $packages
        $lock | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $lockFilePath -Encoding utf8
        Assert-InstalledPackages -Packages $packages
        Assert-ExpectedInstallDiff -Configuration $configuration
    }
    catch
    {
        Write-Warning "Native plugin installation failed. Restoring the previous payloads."
        Restore-InstallBackup -Configuration $configuration -BackupRoot $backupRoot
        $cleanupInstallScratch = $true
        $state.phase = "rolled_back"
        Write-State -State $state -StatePath $statePath
        throw
    }

    $state.phase = "completed"
    Write-State -State $state -StatePath $statePath
    $cleanupInstallScratch = $true
    Write-Host ""
    Write-Host "Native plugins installed successfully from GitHub master."
    Write-Host "Request: $($state.requestId)"
    Write-Host "Lock file: $lockFilePath"
    Write-Host "Backup and downloaded artifacts: $requestDirectory"
}
finally
{
    try
    {
        if ($cleanupInstallScratch)
        {
            Remove-InstallScratch -Configuration $configuration -RequestId $state.requestId
        }
    }
    finally
    {
        Exit-InstallMutex -Mutex $installMutex
    }
}
