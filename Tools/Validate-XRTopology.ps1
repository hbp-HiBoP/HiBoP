$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$expectedUnityVersion = "6000.5.2f1"
$packages = @(
    "com.crnl.hibop.contracts",
    "com.crnl.hibop.render-model",
    "com.crnl.hibop.protocol"
)

function Assert-Condition
{
    param(
        [Parameter(Mandatory)]
        [bool] $Condition,

        [Parameter(Mandatory)]
        [string] $Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Get-UnityVersion
{
    param([Parameter(Mandatory)][string] $Path)

    $versionLine = Get-Content -LiteralPath $Path |
        Where-Object { $_ -match "^m_EditorVersion: " } |
        Select-Object -First 1

    Assert-Condition ($null -ne $versionLine) "Unity version is missing from $Path."
    return ($versionLine -replace "^m_EditorVersion: ", "").Trim()
}

function Get-Dependency
{
    param(
        [Parameter(Mandatory)] $Manifest,
        [Parameter(Mandatory)][string] $Package
    )

    $property = $Manifest.dependencies.PSObject.Properties[$Package]
    if ($null -eq $property)
    {
        return $null
    }

    return [string] $property.Value
}

Push-Location $repositoryRoot
try
{
    $desktopVersion = Get-UnityVersion "ProjectSettings/ProjectVersion.txt"
    $xrVersion = Get-UnityVersion "XR/ProjectSettings/ProjectVersion.txt"
    Assert-Condition ($desktopVersion -eq $expectedUnityVersion) "Desktop uses $desktopVersion instead of $expectedUnityVersion."
    Assert-Condition ($xrVersion -eq $expectedUnityVersion) "XR uses $xrVersion instead of $expectedUnityVersion."

    $desktopManifest = Get-Content -Raw -LiteralPath "Packages/manifest.json" | ConvertFrom-Json
    $xrManifest = Get-Content -Raw -LiteralPath "XR/Packages/manifest.json" | ConvertFrom-Json

    foreach ($package in $packages)
    {
        $packageRoot = "Shared/Packages/$package"
        Assert-Condition (Test-Path -LiteralPath "$packageRoot/package.json" -PathType Leaf) "$package has no package.json."
        Assert-Condition (@(Get-ChildItem -LiteralPath "$packageRoot/Runtime" -Filter "*.asmdef" -File).Count -eq 1) "$package must expose one Runtime asmdef."
        Assert-Condition (@(Get-ChildItem -LiteralPath "$packageRoot/Tests/Editor" -Filter "*.asmdef" -File).Count -eq 1) "$package must expose one Editor test asmdef."

        Assert-Condition ((Get-Dependency $desktopManifest $package) -eq "file:../Shared/Packages/$package") "Desktop has an invalid local reference for $package."
        Assert-Condition ((Get-Dependency $xrManifest $package) -eq "file:../../Shared/Packages/$package") "XR has an invalid local reference for $package."
        Assert-Condition ($desktopManifest.testables -contains $package) "Desktop does not expose $package tests."
        Assert-Condition ($xrManifest.testables -contains $package) "XR does not expose $package tests."
    }

    $trackedFiles = @(git ls-files)
    Assert-Condition ($LASTEXITCODE -eq 0) "git ls-files failed."

    $generatedPattern = "^XR/(\.utmp|Library|Temp|Obj|Build|Builds|Logs|MemoryCaptures|UserSettings|ProfilerCaptures)/"
    $trackedGenerated = @($trackedFiles | Where-Object { $_ -match $generatedPattern })
    Assert-Condition ($trackedGenerated.Count -eq 0) "Generated XR files are tracked: $($trackedGenerated -join ', ')."

    $ignoredGeneratedPaths = @(
        ".artifacts/xr/P01.log",
        "XR/.utmp/P01.tmp",
        "XR/Library/P01.tmp",
        "XR/Temp/P01.tmp",
        "XR/Obj/P01.tmp",
        "XR/build/P01.tmp",
        "XR/Builds/P01.tmp",
        "XR/Logs/P01.tmp",
        "XR/MemoryCaptures/P01.tmp",
        "XR/UserSettings/P01.tmp",
        "XR/ProfilerCaptures/P01.tmp"
    )
    foreach ($path in $ignoredGeneratedPaths)
    {
        git check-ignore --quiet --no-index -- $path
        Assert-Condition ($LASTEXITCODE -eq 0) "$path is not covered by .gitignore."
    }

    $allowedWorkflowTriggers = @("release", "workflow_dispatch")
    $workflowFiles = @(Get-ChildItem -LiteralPath ".github/workflows" -File |
        Where-Object { $_.Extension -in ".yml", ".yaml" })
    foreach ($workflowFile in $workflowFiles)
    {
        $workflowLines = @(Get-Content -LiteralPath $workflowFile.FullName)
        $onLine = -1
        for ($index = 0; $index -lt $workflowLines.Count; $index++)
        {
            if ($workflowLines[$index] -match "^on:\s*$")
            {
                $onLine = $index
                break
            }
        }

        Assert-Condition ($onLine -ge 0) "$($workflowFile.Name) must declare triggers in an on: block."

        $workflowTriggers = @()
        for ($index = $onLine + 1; $index -lt $workflowLines.Count; $index++)
        {
            if ($workflowLines[$index] -match "^[^\s#]")
            {
                break
            }
            if ($workflowLines[$index] -match "^  ([A-Za-z0-9_-]+):")
            {
                $workflowTriggers += $Matches[1]
            }
        }

        Assert-Condition ($workflowTriggers.Count -gt 0) "$($workflowFile.Name) has no declared trigger."
        $unsupportedTriggers = @($workflowTriggers | Where-Object { $_ -notin $allowedWorkflowTriggers })
        Assert-Condition ($unsupportedTriggers.Count -eq 0) "$($workflowFile.Name) has automatic or unsupported triggers: $($unsupportedTriggers -join ', ')."
    }

    $rawArtifacts = @($trackedFiles | Where-Object {
        $_ -match "^\.artifacts/xr/" -or
        ($_ -match "^(XR|Shared|Docs/dev/xr/evidence)/" -and $_ -match "\.(apk|aab|log|xml)$")
    })
    Assert-Condition ($rawArtifacts.Count -eq 0) "Raw XR artifacts are tracked: $($rawArtifacts -join ', ')."

    $largeFiles = @(
        Get-ChildItem -LiteralPath "XR/Assets", "XR/Packages", "XR/ProjectSettings", "Shared/Packages" -File -Recurse |
            Where-Object { $_.Length -ge 50MB }
    )
    if (Test-Path -LiteralPath "Docs/dev/xr/evidence")
    {
        $largeFiles += Get-ChildItem -LiteralPath "Docs/dev/xr/evidence" -File -Recurse |
            Where-Object { $_.Length -ge 50MB }
    }
    Assert-Condition ($largeFiles.Count -eq 0) "Files of 50 MiB or more require an explicit architecture decision: $($largeFiles.FullName -join ', ')."

    Assert-Condition (-not (Test-Path -LiteralPath "XR/Assets/Scripts/HBP")) "Desktop HBP sources were copied under XR."

    $desktopSourceHashes = @{}
    Get-ChildItem -LiteralPath "Assets/Scripts/HBP" -Filter "*.cs" -File -Recurse | ForEach-Object {
        $desktopSourceHashes[(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash] = $_.FullName
    }
    $copiedSources = @()
    Get-ChildItem -LiteralPath "XR/Assets", "Shared/Packages" -Filter "*.cs" -File -Recurse | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        if ($desktopSourceHashes.ContainsKey($hash))
        {
            $copiedSources += $_.FullName
        }
    }
    Assert-Condition ($copiedSources.Count -eq 0) "Existing HiBoP C# sources were copied: $($copiedSources -join ', ')."

    Write-Output "XR topology validation passed."
}
finally
{
    Pop-Location
}
