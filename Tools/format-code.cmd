@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Format-Code.ps1" %*
exit /b %ERRORLEVEL%
