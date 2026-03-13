@echo off
where pwsh >nul 2>&1
if %ERRORLEVEL%==0 (
    pwsh -ExecutionPolicy Bypass -File "%~dp0start-dev-sqlite.ps1" %*
) else (
    powershell -ExecutionPolicy Bypass -File "%~dp0start-dev-sqlite.ps1" %*
)
