[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$checker = Join-Path $PSScriptRoot "check-assembly-dependencies.ps1"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("hibop-assembly-check-" + [Guid]::NewGuid().ToString("N"))
$temporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
$systemTemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
if (-not $temporaryRoot.StartsWith($systemTemporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to create assembly-check fixtures outside the system temporary directory."
}

function Write-Definition {
    param([string]$Root, [string]$File, [string]$Name, [string[]]$References = @())
    $path = Join-Path $Root "Assets/$File"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    @{ name = $Name; references = $References } | ConvertTo-Json | Set-Content -LiteralPath $path -Encoding UTF8
}

function Invoke-Case {
    param(
        [string]$Name,
        [scriptblock]$Arrange,
        [string[]]$AllowedEdges,
        [bool]$ShouldPass
    )
    $root = Join-Path $temporaryRoot $Name
    New-Item -ItemType Directory -Force -Path (Join-Path $root "Assets") | Out-Null
    & $Arrange $root
    $policy = Join-Path $root "policy.json"
    @{ allowedDirectEdges = $AllowedEdges } | ConvertTo-Json | Set-Content -LiteralPath $policy -Encoding UTF8
    $passed = $true
    try {
        & $checker -ProjectRoot $root -PolicyPath $policy *> $null
    }
    catch {
        $passed = $false
    }
    if ($passed -ne $ShouldPass) {
        throw "Assembly checker self-test '$Name' expected pass=$ShouldPass but observed pass=$passed."
    }
    Write-Host "PASS $Name"
}

try {
    New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null
    Invoke-Case "valid" {
        param($root)
        Write-Definition $root "Core.asmdef" "HBP.Core.Runtime" @("Unity.TextMeshPro")
        Write-Definition $root "Feature.asmdef" "HBP.Feature.Runtime" @("HBP.Core.Runtime")
    } @("HBP.Feature.Runtime -> HBP.Core.Runtime") $true

    Invoke-Case "core-inversion" {
        param($root)
        Write-Definition $root "Core.asmdef" "HBP.Core.Runtime" @("HBP.Feature.Runtime")
        Write-Definition $root "Feature.asmdef" "HBP.Feature.Runtime"
    } @("HBP.Core.Runtime -> HBP.Feature.Runtime") $false

    Invoke-Case "cycle" {
        param($root)
        Write-Definition $root "A.asmdef" "HBP.A" @("HBP.B")
        Write-Definition $root "B.asmdef" "HBP.B" @("HBP.A")
    } @("HBP.A -> HBP.B", "HBP.B -> HBP.A") $false

    Invoke-Case "missing-name" {
        param($root)
        Write-Definition $root "A.asmdef" "HBP.A" @("HBP.Missing")
    } @("HBP.A -> HBP.Missing") $false

    Invoke-Case "ambiguous-name" {
        param($root)
        Write-Definition $root "A1.asmdef" "HBP.A"
        Write-Definition $root "A2.asmdef" "HBP.A"
    } @() $false

    Invoke-Case "unapproved-edge" {
        param($root)
        Write-Definition $root "Core.asmdef" "HBP.Core.Runtime"
        Write-Definition $root "Feature.asmdef" "HBP.Feature.Runtime" @("HBP.Core.Runtime")
    } @() $false
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        $resolved = [System.IO.Path]::GetFullPath($temporaryRoot)
        if (-not $resolved.StartsWith($systemTemporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove assembly-check fixtures outside the system temporary directory."
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

Write-Host "Assembly checker self-tests passed: 6."
