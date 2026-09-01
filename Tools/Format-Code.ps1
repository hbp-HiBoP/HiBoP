<#
.SYNOPSIS
Formats C# files with the repository's pinned ReSharper formatter.

.EXAMPLE
.\Tools\format-code.cmd

.EXAMPLE
.\Tools\format-code.cmd -Base origin/develop

.EXAMPLE
.\Tools\format-code.cmd -All
#>

[CmdletBinding()]
param(
    [string]$Base,
    [switch]$All
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

if ($All -and $Base)
{
    throw "-All and -Base cannot be used together."
}

function Get-GitFiles
{
    param([string[]]$Arguments)

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "Git failed while finding C# files."
    }
    return $output
}

Push-Location $repositoryRoot
try
{
    if ($All)
    {
        $files = Get-GitFiles @("ls-files", "--", "*.cs")
    }
    else
    {
        $files = @(
            Get-GitFiles @("diff", "--name-only", "--diff-filter=ACMR", "--", "*.cs")
            Get-GitFiles @("diff", "--cached", "--name-only", "--diff-filter=ACMR", "--", "*.cs")
            Get-GitFiles @("ls-files", "--others", "--exclude-standard", "--", "*.cs")

            if ($Base)
            {
                Get-GitFiles @("diff", "--name-only", "--diff-filter=ACMR", "$Base...HEAD", "--", "*.cs")
            }
        )
    }

    $files = @($files | Sort-Object -Unique | Where-Object { Test-Path -LiteralPath $_ })
    if ($files.Count -eq 0)
    {
        Write-Host "No C# files to format."
        return
    }

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to restore the repository's formatting tool."
    }

    # The jb dispatcher from this package misparses its own DLL as a solution when it runs on .NET 10.
    # Invoke CleanupCode directly while still resolving the version pinned by the local tool manifest.
    $toolManifest = Get-Content -Raw (Join-Path $repositoryRoot ".config\dotnet-tools.json") | ConvertFrom-Json
    $toolVersion = $toolManifest.tools."jetbrains.resharper.globaltools".version
    $globalPackagesOutput = & dotnet nuget locals global-packages --list
    $globalPackagesLine = $globalPackagesOutput | Select-Object -First 1
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to locate the NuGet global packages folder."
    }

    $globalPackagesRoot = ($globalPackagesLine -replace "^[^:]+:\s*", "").Trim()
    $toolPackageRoot = Join-Path $globalPackagesRoot "jetbrains.resharper.globaltools\$toolVersion"
    $cleanupCode = Get-ChildItem -Path (Join-Path $toolPackageRoot "tools\*\any\cleanupcode.exe") -File |
        Select-Object -ExpandProperty FullName -First 1
    if (!$cleanupCode)
    {
        throw "Unable to find CleanupCode in the restored JetBrains tool package."
    }

    $xrFiles = @($files | Where-Object { $_.StartsWith("XR/", [System.StringComparison]::OrdinalIgnoreCase) })
    $solutionFiles = @($files | Where-Object { !$_.StartsWith("XR/", [System.StringComparison]::OrdinalIgnoreCase) })

    if ($solutionFiles.Count -gt 0)
    {
        $solution = @(
            Join-Path $repositoryRoot "HiBoP.slnx"
            Join-Path $repositoryRoot "HiBoP.sln"
        ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if (!$solution)
        {
            throw "HiBoP.slnx or HiBoP.sln is required. Generate the solution files from Unity first."
        }

        $include = if ($All) { "Assets/**/*.cs" } else { $solutionFiles -join ";" }
        Write-Host "Formatting $($solutionFiles.Count) C# file(s) from the HiBoP solution..."
        & $cleanupCode --profile="Built-in: Reformat Code" --no-updates --no-build --verbosity=ERROR --include=$include $solution
        if ($LASTEXITCODE -ne 0)
        {
            throw "ReSharper CleanupCode failed for the HiBoP solution."
        }
    }

    if ($xrFiles.Count -gt 0)
    {
        $targets = if ($All)
        {
            @(Join-Path $repositoryRoot "XR\Assets\**\*.cs")
        }
        else
        {
            @($xrFiles | ForEach-Object { Join-Path $repositoryRoot $_ })
        }
        Write-Host "Formatting $($xrFiles.Count) standalone XR C# file(s)..."
        # Standalone Unity files trigger noisy, non-fatal package-discovery diagnostics in CleanupCode.
        & $cleanupCode --profile="Built-in: Reformat Code" --no-updates --no-build --verbosity=OFF @targets
        if ($LASTEXITCODE -ne 0)
        {
            throw "ReSharper CleanupCode failed for the XR files."
        }
    }
}
finally
{
    Pop-Location
}
