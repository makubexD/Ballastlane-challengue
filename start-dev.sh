#!/usr/bin/env bash
# Git Bash / WSL launcher for start-dev.ps1
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if command -v pwsh &>/dev/null; then
    pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start-dev.ps1" "$@"
else
    powershell.exe -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start-dev.ps1" "$@"
fi
