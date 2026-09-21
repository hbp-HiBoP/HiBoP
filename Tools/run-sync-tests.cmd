@echo off
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-sync-tests.ps1" %*
