[CmdletBinding()]
param(
    [string]$Directory = (Join-Path $env:LOCALAPPDATA 'HiBoP/Signing/QuestDevelopment')
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pinPath = Join-Path $PSScriptRoot 'QuestSigning.json'
if (Test-Path -LiteralPath $pinPath) {
    throw 'This project already has a signing identity. Copy the existing private signing folder from the other PC; do not generate another key.'
}
$Directory = [IO.Path]::GetFullPath($Directory)
if (Test-Path -LiteralPath $Directory) { throw "Refusing to overwrite an existing signing directory: $Directory" }
$version = ((Get-Content "$repo/ProjectSettings/ProjectVersion.txt" | Select-Object -First 1) -split ': ')[1]
$keytool = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe"
if (!(Test-Path -LiteralPath $keytool)) { throw 'Install the matching Unity Android OpenJDK first.' }
New-Item -ItemType Directory -Path $Directory -Force | Out-Null
# Only this Windows user can read the key and its portable development password.
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User
$acl = Get-Acl -LiteralPath $Directory
$acl.SetAccessRuleProtection($true, $false)
$rule = [Security.AccessControl.FileSystemAccessRule]::new($sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
$acl.SetAccessRule($rule)
Set-Acl -LiteralPath $Directory -AclObject $acl
$bytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($bytes) } finally { $random.Dispose() }
$password = [BitConverter]::ToString($bytes).Replace('-', '')
$previousPassword = $env:HIBOP_QUEST_SIGNING_PASSWORD
try {
    $env:HIBOP_QUEST_SIGNING_PASSWORD = $password
    $keystore = Join-Path $Directory 'quest-development.p12'
    & $keytool -genkeypair -keystore $keystore -storetype PKCS12 -alias hibop-quest-development -keyalg RSA -keysize 3072 -sigalg SHA256withRSA -validity 36500 -dname 'CN=HiBoP Quest Development' -storepass:env HIBOP_QUEST_SIGNING_PASSWORD -keypass:env HIBOP_QUEST_SIGNING_PASSWORD
    if ($LASTEXITCODE -ne 0) { throw 'Key generation failed; no build identity was pinned.' }
    $certificate = Join-Path $Directory 'certificate.der'
    & $keytool -exportcert -keystore $keystore -alias hibop-quest-development -storepass:env HIBOP_QUEST_SIGNING_PASSWORD -file $certificate
    if ($LASTEXITCODE -ne 0) { throw 'Certificate export failed.' }
    $fingerprint = (Get-FileHash -LiteralPath $certificate -Algorithm SHA256).Hash.ToLowerInvariant()
    [ordered]@{ keystore = 'quest-development.p12'; alias = 'hibop-quest-development'; password = $password } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Directory 'signing.json') -Encoding UTF8
    [ordered]@{ purpose = 'Shared HiBoP Quest development signing identity'; certificateSha256 = $fingerprint } |
        ConvertTo-Json | Set-Content -LiteralPath $pinPath -Encoding UTF8
    Write-Output "Created private signing folder: $Directory"
    Write-Output "Public certificate SHA-256: $fingerprint"
    Write-Output 'Copy the entire private folder to the other PC. Never commit signing.json or the keystore.'
}
finally { $env:HIBOP_QUEST_SIGNING_PASSWORD = $previousPassword }
