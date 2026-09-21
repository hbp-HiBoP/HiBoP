[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$PolicyPath = ""
)

$ErrorActionPreference = "Stop"
$started = [System.Diagnostics.Stopwatch]::StartNew()
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $PSScriptRoot "assembly-dependency-policy.json"
}
$PolicyPath = [System.IO.Path]::GetFullPath($PolicyPath)
$assetsRoot = Join-Path $ProjectRoot "Assets"
if (-not (Test-Path -LiteralPath $assetsRoot -PathType Container)) {
    throw "Assembly dependency check failed: Assets directory not found at '$assetsRoot'."
}
if (-not (Test-Path -LiteralPath $PolicyPath -PathType Leaf)) {
    throw "Assembly dependency check failed: policy not found at '$PolicyPath'."
}

$definitions = @()
foreach ($file in Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Filter *.asmdef) {
    try {
        $json = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    }
    catch {
        throw "Assembly dependency check failed: invalid JSON in '$($file.FullName)': $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace([string]$json.name)) {
        throw "Assembly dependency check failed: missing assembly name in '$($file.FullName)'."
    }
    $definitions += [pscustomobject]@{
        Name = [string]$json.name
        Path = $file.FullName
        References = @($json.references)
    }
}

$byName = @{}
foreach ($definition in $definitions) {
    if (-not $byName.ContainsKey($definition.Name)) {
        $byName[$definition.Name] = [System.Collections.Generic.List[object]]::new()
    }
    $byName[$definition.Name].Add($definition)
}
$ambiguous = @($byName.GetEnumerator() | Where-Object { $_.Value.Count -ne 1 })
if ($ambiguous.Count -gt 0) {
    $details = $ambiguous | Sort-Object Key | ForEach-Object {
        "'$($_.Key)' => $($_.Value.Path -join ', ')"
    }
    throw "Assembly dependency check failed: ambiguous assembly names:$([Environment]::NewLine)$($details -join [Environment]::NewLine)"
}

$guidNames = @{}
foreach ($definition in $definitions) {
    $meta = "$($definition.Path).meta"
    if (-not (Test-Path -LiteralPath $meta -PathType Leaf)) {
        continue
    }
    $match = [regex]::Match((Get-Content -LiteralPath $meta -Raw), '(?m)^guid:\s*([0-9a-fA-F]+)\s*$')
    if (-not $match.Success) {
        continue
    }
    $guid = $match.Groups[1].Value.ToLowerInvariant()
    if (-not $guidNames.ContainsKey($guid)) {
        $guidNames[$guid] = [System.Collections.Generic.List[string]]::new()
    }
    $guidNames[$guid].Add($definition.Name)
}

$edges = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($definition in $definitions) {
    if (-not $definition.Name.StartsWith("HBP.", [StringComparison]::Ordinal)) {
        continue
    }
    foreach ($rawReference in $definition.References) {
        $reference = [string]$rawReference
        $target = $reference
        if ($reference.StartsWith("GUID:", [StringComparison]::OrdinalIgnoreCase)) {
            $guid = $reference.Substring(5).ToLowerInvariant()
            if (-not $guidNames.ContainsKey($guid) -or $guidNames[$guid].Count -ne 1) {
                throw "Assembly dependency check failed: missing or ambiguous GUID reference '$reference' from '$($definition.Name)'."
            }
            $target = $guidNames[$guid][0]
        }
        elseif ($reference.StartsWith("HBP.", [StringComparison]::Ordinal)) {
            if (-not $byName.ContainsKey($reference)) {
                throw "Assembly dependency check failed: missing HBP assembly '$reference' referenced by '$($definition.Name)'."
            }
        }
        else {
            continue
        }

        if ($target.StartsWith("HBP.", [StringComparison]::Ordinal)) {
            [void]$edges.Add("$($definition.Name) -> $target")
        }
    }
}

$coreEdges = @($edges | Where-Object { $_.StartsWith("HBP.Core.Runtime -> HBP.", [StringComparison]::Ordinal) })
if ($coreEdges.Count -gt 0) {
    throw "Assembly dependency check failed: HBP.Core.Runtime must not reference another HBP.* assembly:$([Environment]::NewLine)$($coreEdges -join [Environment]::NewLine)"
}

$nodes = @($definitions.Name | Where-Object { $_.StartsWith("HBP.", [StringComparison]::Ordinal) } | Sort-Object -Unique)
$outgoing = @{}
$incoming = @{}
foreach ($node in $nodes) {
    $outgoing[$node] = [System.Collections.Generic.List[string]]::new()
    $incoming[$node] = 0
}
foreach ($edge in $edges) {
    $parts = $edge -split ' -> ', 2
    $outgoing[$parts[0]].Add($parts[1])
    $incoming[$parts[1]]++
}
$ready = [System.Collections.Generic.Queue[string]]::new()
foreach ($node in $nodes) {
    if ($incoming[$node] -eq 0) {
        $ready.Enqueue($node)
    }
}
$visited = 0
while ($ready.Count -gt 0) {
    $node = $ready.Dequeue()
    $visited++
    foreach ($target in $outgoing[$node]) {
        $incoming[$target]--
        if ($incoming[$target] -eq 0) {
            $ready.Enqueue($target)
        }
    }
}
if ($visited -ne $nodes.Count) {
    $cycleNodes = @($nodes | Where-Object { $incoming[$_] -gt 0 })
    throw "Assembly dependency check failed: HBP.* dependency cycle detected among: $($cycleNodes -join ', ')."
}

try {
    $policy = Get-Content -LiteralPath $PolicyPath -Raw | ConvertFrom-Json
}
catch {
    throw "Assembly dependency check failed: invalid policy JSON in '$PolicyPath': $($_.Exception.Message)"
}
$allowed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($edge in @($policy.allowedDirectEdges)) {
    if (-not $allowed.Add([string]$edge)) {
        throw "Assembly dependency check failed: duplicate policy edge '$edge'."
    }
}
$unapproved = @($edges | Where-Object { -not $allowed.Contains($_) } | Sort-Object)
if ($unapproved.Count -gt 0) {
    throw "Assembly dependency check failed: unapproved direct HBP.* edges:$([Environment]::NewLine)$($unapproved -join [Environment]::NewLine)"
}
$stale = @($allowed | Where-Object { -not $edges.Contains($_) } | Sort-Object)
if ($stale.Count -gt 0) {
    throw "Assembly dependency check failed: policy contains missing direct HBP.* edges:$([Environment]::NewLine)$($stale -join [Environment]::NewLine)"
}

$started.Stop()
Write-Host "Assembly dependency check passed: $($nodes.Count) HBP assemblies, $($edges.Count) direct edges, $($started.Elapsed.TotalMilliseconds.ToString('F1')) ms."
