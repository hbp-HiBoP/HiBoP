[CmdletBinding()]
param(
    [string]$Apk,
    [string]$Directory = $(if ($env:HIBOP_QUEST_SIGNING_DIRECTORY) { $env:HIBOP_QUEST_SIGNING_DIRECTORY } else { Join-Path $env:LOCALAPPDATA 'HiBoP/Signing/QuestDevelopment' }),
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pinPath = Join-Path $PSScriptRoot 'QuestSigning.json'
$configPath = Join-Path $Directory 'signing.json'
if (!(Test-Path -LiteralPath $configPath) -or !(Test-Path -LiteralPath $pinPath)) {
    throw 'Shared Quest signing identity missing. See Tools/QuestSigning.md; copy the existing private signing folder from the other PC. No per-machine debug-key fallback is allowed.'
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$pin = Get-Content -LiteralPath $pinPath -Raw | ConvertFrom-Json
if ($pin.certificateSha256 -notmatch '^[a-fA-F0-9]{64}$') { throw 'Invalid pinned Quest certificate SHA-256.' }
if ([string]::IsNullOrWhiteSpace($config.keystore) -or [string]::IsNullOrWhiteSpace($config.alias) -or [string]::IsNullOrWhiteSpace($config.password)) {
    throw 'Incomplete private signing configuration.'
}
$keystore = [IO.Path]::GetFullPath((Join-Path $Directory $config.keystore))
if (!(Test-Path -LiteralPath $keystore)) { throw 'The configured shared Quest keystore is missing.' }
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$android = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Data/PlaybackEngines/AndroidPlayer"
$keytool = "$android/OpenJDK/bin/keytool.exe"
$java = "$android/OpenJDK/bin/java.exe"
$buildTools = Get-ChildItem -LiteralPath "$android/SDK/build-tools" -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'lib/apksigner.jar') } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$buildTools) { throw 'Android SDK apksigner is missing.' }
$signer = Join-Path $buildTools.FullName 'lib/apksigner.jar'
$previousPassword = $env:HIBOP_QUEST_SIGNING_PASSWORD
try {
    $env:HIBOP_QUEST_SIGNING_PASSWORD = $config.password
    # Public certificate only; the password is passed by environment-variable name.
    $certificateOutput = & $keytool -exportcert -rfc -keystore $keystore -alias $config.alias -storepass:env HIBOP_QUEST_SIGNING_PASSWORD 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Cannot open the shared Quest signing key (check private configuration/password).' }
    $pem = $certificateOutput -join "`n"
    if ($pem -notmatch '(?s)-----BEGIN CERTIFICATE-----\s*(.*?)\s*-----END CERTIFICATE-----') { throw 'No public certificate was exported.' }
    $der = [Convert]::FromBase64String(($Matches[1] -replace '\s', ''))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $fingerprint = [BitConverter]::ToString($sha.ComputeHash($der)).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
    if ($fingerprint -ne $pin.certificateSha256) { throw 'Quest signing certificate differs from the identity pinned in Tools/QuestSigning.json. Copy the correct private signing folder.' }
    if ($ValidateOnly) { Write-Output "Quest signing identity verified: $fingerprint"; return }
    if ([string]::IsNullOrWhiteSpace($Apk) -or !(Test-Path -LiteralPath $Apk)) { throw 'Supply an existing APK to sign.' }
    $Apk = [IO.Path]::GetFullPath($Apk)
    $signed = "$Apk.signed"
    if (Test-Path -LiteralPath $signed) { throw "A previous signed candidate remains at $signed; inspect it before retrying." }
    & $java -jar $signer sign --ks $keystore --ks-key-alias $config.alias --ks-pass env:HIBOP_QUEST_SIGNING_PASSWORD --key-pass env:HIBOP_QUEST_SIGNING_PASSWORD --v4-signing-enabled false --out $signed $Apk
    if ($LASTEXITCODE -ne 0) { throw 'APK signing failed; original APK preserved.' }
    $verification = & $java -jar $signer verify --verbose --print-certs $signed 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Signed APK verification failed; original APK preserved.' }
    $digests = @($verification | Select-String '^Signer #\d+ certificate SHA-256 digest: ([a-fA-F0-9]{64})$' | ForEach-Object { $_.Matches[0].Groups[1].Value.ToLowerInvariant() })
    if ($digests.Count -ne 1 -or $digests[0] -ne $fingerprint) { throw 'The APK signer does not match the pinned identity; original APK preserved.' }
    Move-Item -LiteralPath $signed -Destination $Apk -Force
    [ordered]@{ apk = [IO.Path]::GetFileName($Apk); certificateSha256 = $fingerprint; apkSha256 = (Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant() } |
        ConvertTo-Json | Set-Content -LiteralPath "$Apk.signing.json" -Encoding UTF8
    Write-Output "Signed and verified Quest APK: $Apk"
}
finally { $env:HIBOP_QUEST_SIGNING_PASSWORD = $previousPassword }
