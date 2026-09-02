[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$spikeRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $spikeRoot "UnityClient"
$pluginsRoot = Join-Path $projectRoot "Assets/ThirdPartyGenerated/Plugins"
$webSocketRoot = Join-Path $projectRoot "Assets/ThirdPartyGenerated/WebSocketSharp"
$vendorRoot = Join-Path $spikeRoot ".artifacts/vendor"
$nugetRoot = Join-Path $env:USERPROFILE ".nuget/packages"

dotnet build (Join-Path $spikeRoot "src/P06.Core/P06.Core.csproj") --configuration Release
if ($LASTEXITCODE -ne 0) { throw "P06.Core netstandard build failed." }

New-Item -ItemType Directory -Force -Path $pluginsRoot | Out-Null
$assemblies = @(
    (Join-Path $spikeRoot "src/P06.Core/bin/Release/netstandard2.1/P06.Core.dll"),
    (Join-Path $nugetRoot "google.protobuf/3.36.1/lib/netstandard2.0/Google.Protobuf.dll"),
    (Join-Path $nugetRoot "memorypack.core/1.21.4/lib/netstandard2.1/MemoryPack.Core.dll"),
    (Join-Path $nugetRoot "messagepack/3.1.8/lib/netstandard2.1/MessagePack.dll"),
    (Join-Path $nugetRoot "messagepack.annotations/3.1.8/lib/netstandard2.0/MessagePack.Annotations.dll"),
    (Join-Path $nugetRoot "microsoft.net.stringtools/17.11.4/lib/netstandard2.0/Microsoft.NET.StringTools.dll"),
    (Join-Path $nugetRoot "system.collections.immutable/8.0.0/lib/netstandard2.0/System.Collections.Immutable.dll"),
    (Join-Path $nugetRoot "system.runtime.compilerservices.unsafe/6.0.0/lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll")
)
foreach ($assembly in $assemblies) {
    Copy-Item -LiteralPath $assembly -Destination $pluginsRoot -Force
}

$webSocketCheckout = Join-Path $vendorRoot "websocket-sharp"
if (-not (Test-Path (Join-Path $webSocketCheckout ".git"))) {
    New-Item -ItemType Directory -Force -Path $vendorRoot | Out-Null
    git clone --filter=blob:none --no-checkout https://github.com/sta/websocket-sharp.git $webSocketCheckout
}
git -C $webSocketCheckout checkout --detach 7aed0002451cf70ed74bc2e1ca6504dab5b50a10
if ($LASTEXITCODE -ne 0) { throw "Unable to checkout the locked websocket-sharp commit." }

New-Item -ItemType Directory -Force -Path $webSocketRoot | Out-Null
Get-ChildItem -LiteralPath (Join-Path $webSocketCheckout "websocket-sharp") | Copy-Item -Destination $webSocketRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $spikeRoot "VendorPatches/websocket-sharp/AssemblyInfo.cs") -Destination (Join-Path $webSocketRoot "AssemblyInfo.cs") -Force

Write-Host "Prepared the isolated P06 Unity client dependencies."
