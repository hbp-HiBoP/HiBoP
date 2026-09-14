@echo off
rem Start this manually after closing the existing HiBoP window.
rem Opens the validated Player and runs three diagnostics without closing it.
setlocal
set "HIBOP_EVIDENCE=%~dp0..\.test-results\quest-024\patient-final-%RANDOM%-%RANDOM%"
rem ExecutionPolicy applies only to this PowerShell process; no system setting is changed.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-SceneQualification.ps1" -Player "%~dp0..\.artifacts\quest-024\revision-2\Windows\HiBoP.6.1.0.win64\HiBoP.exe" -FixtureName scene-008-patient-visual -Evidence "%HIBOP_EVIDENCE%" -Passes 3 -KeepOpen
if errorlevel 1 pause
