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

    $solution = @(
        Join-Path $repositoryRoot "HiBoP.slnx"
        Join-Path $repositoryRoot "HiBoP.sln"
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (!$solution)
    {
        throw "HiBoP.slnx or HiBoP.sln is required. Generate the solution files from Unity first."
    }

    $include = if ($All) { "Assets/**/*.cs" } else { $files -join ";" }
    Write-Host "Formatting $($files.Count) C# file(s)..."
    & dotnet tool run jb -- cleanupcode --profile="Built-in: Reformat Code" --no-updates --no-build --verbosity=ERROR --include=$include $solution
    if ($LASTEXITCODE -ne 0)
    {
        throw "ReSharper CleanupCode failed."
    }
}
finally
{
    Pop-Location
}
