<#
.SYNOPSIS
Selects the global input backend before opening Unity (QUEST-002).
.DESCRIPTION
Desktop keeps legacy input and enables Input System. Quest uses Input System
only because Unity does not support Both on Android. This does not switch the
build target or edit packages. Full Build Profiles belong to QUEST-003.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Desktop', 'Quest')]
    [string]$Platform
)

$ErrorActionPreference = 'Stop'
if (Get-Process Unity -ErrorAction SilentlyContinue)
{
    throw 'Close Unity before selecting the global input backend; it takes effect on editor startup.'
}

$questSettingsPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'ProjectSettings/ProjectSettings.asset'
$questSettings = [IO.File]::ReadAllText($questSettingsPath)
$questPattern = '(?m)^  activeInputHandler: [012](?=\r?$)'
if ([regex]::Matches($questSettings, $questPattern).Count -ne 1)
{
    throw 'Expected exactly one global activeInputHandler setting; no file was changed.'
}
$questMode = if ($Platform -eq 'Desktop') { 2 } else { 1 }
$questUpdated = [regex]::Replace($questSettings, $questPattern, "  activeInputHandler: $questMode")
if ($questUpdated -ne $questSettings)
{
    [IO.File]::WriteAllText($questSettingsPath, $questUpdated, [Text.UTF8Encoding]::new($false))
}
Write-Output "$Platform input selected: activeInputHandler=$questMode. Open Unity with the corresponding build target."
