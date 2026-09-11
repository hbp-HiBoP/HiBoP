# Build the existing pinned hbp_math sources; does not change algorithms or install the result.
[CmdletBinding()]
param(
    [string]$SourceRepository = "$PSScriptRoot/../../hbp_math",
    [string]$OutputDirectory = "$PSScriptRoot/../.artifacts/native/hbp_math-android"
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pin = (Get-Content "$PSScriptRoot/NativePlugins.lock.json" -Raw | ConvertFrom-Json).libraries | Where-Object name -eq 'hbp_math'
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$android = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Data/PlaybackEngines/AndroidPlayer"
$ndk = "$android/NDK"
$cmake = "$android/SDK/cmake/3.22.1/bin/cmake.exe"
$ninja = "$android/SDK/cmake/3.22.1/bin/ninja.exe"
$llvm = "$ndk/toolchains/llvm/prebuilt/windows-x86_64/bin"
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path "$output/source", "$output/build" | Out-Null
function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable failed ($LASTEXITCODE)." }
}
Invoke-Checked git @('-C', $SourceRepository, 'archive', '--format=tar', "--output=$output/source.tar", $pin.commit)
Invoke-Checked tar @('-xf', "$output/source.tar", '-C', "$output/source")
$configure = @('-S', "$output/source", '-B', "$output/build", '-G', 'Ninja',
    "-DCMAKE_MAKE_PROGRAM=$ninja", "-DCMAKE_TOOLCHAIN_FILE=$ndk/build/cmake/android.toolchain.cmake",
    '-DCMAKE_BUILD_TYPE=Release', '-DANDROID_ABI=arm64-v8a', '-DANDROID_PLATFORM=android-32',
    '-DANDROID_STL=c++_static', '-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON',
    # CMake may shorten clang++.exe to CLANG_~1.EXE on Windows; retain C++ driver linking.
    '-DCMAKE_CXX_FLAGS=--driver-mode=g++')
Invoke-Checked $cmake $configure
Invoke-Checked $cmake @('--build', "$output/build")
$library = "$output/build/libhbp_math.so"
# Match Android packaging's removal of nonessential symbols before pinning bytes.
Invoke-Checked "$llvm/llvm-strip.exe" @('--strip-unneeded', $library)
$symbols = @(& "$llvm/llvm-nm.exe" -D --defined-only $library)
if ($LASTEXITCODE -ne 0) { throw 'Export inspection failed.' }
$expected = @(Get-Content "$output/source/baseline/hbp_math_abi_exports.txt" | Where-Object { $_.Trim() })
foreach ($symbol in $expected) {
    if (!($symbols -match "\s$([regex]::Escape($symbol))$")) { throw "Missing export: $symbol" }
}
$dynamic = @(& "$llvm/llvm-readelf.exe" -d $library)
if ($LASTEXITCODE -ne 0) { throw 'Dependency inspection failed.' }
$dependencies = @($dynamic | Select-String '\(NEEDED\).*\[(.+)\]' | ForEach-Object { $_.Matches[0].Groups[1].Value })
if (@($dependencies | Where-Object { $_ -notin @('libc.so', 'libm.so', 'libdl.so') }).Count) { throw 'Unexpected Android runtime dependency.' }
$headers = @(& "$llvm/llvm-readelf.exe" -lW $library)
if ($LASTEXITCODE -ne 0) { throw 'ELF inspection failed.' }
$loads = @($headers | Where-Object { $_ -match '^\s*LOAD\s' })
if (!$loads.Count) { throw 'Missing ELF LOAD segments.' }
foreach ($line in $loads) {
    if ([Convert]::ToInt32(($line.Trim() -split '\s+')[-1], 16) -lt 16384) { throw 'ELF segment is not 16 KiB aligned.' }
}
$ndkVersion = ((Get-Content "$ndk/source.properties" | Select-String '^Pkg.Revision') -split '=')[1].Trim()
$manifest = [ordered]@{
    library = 'hbp_math'; sourceCommit = $pin.commit; sourceMode = 'git-archive'
    sourceArchiveSha256 = (Get-FileHash "$output/source.tar").Hash.ToLowerInvariant()
    android = @{ ndkVersion = $ndkVersion; cmakeVersion = '3.22.1'; api = 32; abi = 'arm64-v8a'; stl = 'c++_static'; pageSize = 16384 }
    runtimeDependencies = $dependencies; publicExportCount = $expected.Count; stripUnneeded = $true
    libraryPath = $library; sha256 = (Get-FileHash $library).Hash.ToLowerInvariant(); sizeBytes = (Get-Item $library).Length
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content "$output/manifest.json" -Encoding utf8
$symbols | Set-Content "$output/exports.txt"
$dynamic | Set-Content "$output/dependencies.txt"
$headers | Set-Content "$output/elf-segments.txt"
Write-Output "Built and verified $library ($($expected.Count) ABI exports). Install only after reviewing manifest.json."
