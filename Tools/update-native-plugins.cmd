@echo off
where pwsh.exe >nul 2>&1
if errorlevel 1 (
  echo PowerShell 7 is required. Install it and retry.
  exit /b 1
)
pwsh.exe -NoLogo -NoProfile -File "%~dp0Update-NativePlugins.ps1" %*
exit /b %ERRORLEVEL%
