#!/usr/bin/env bash
# BallastLane — cross-platform launcher → start-dev-sqlite.ps1 (no Docker/Podman required)
#
# Windows (Git Bash / MSYS2 / Cygwin / WSL): delegates to PowerShell 5.1+ or 7+
# Linux / macOS with pwsh (PowerShell Core): delegates to PowerShell 7+
# Linux / macOS without pwsh: prints install instructions

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Windows environments ──────────────────────────────────────────────────────
case "$(uname -s)" in
    MINGW*|CYGWIN*|MSYS*)
        if command -v pwsh &>/dev/null; then
            exec pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start-dev-sqlite.ps1" "$@"
        else
            exec powershell.exe -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start-dev-sqlite.ps1" "$@"
        fi
        ;;
esac

# ── Linux / macOS ─────────────────────────────────────────────────────────────
if command -v pwsh &>/dev/null; then
    exec pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start-dev-sqlite.ps1" "$@"
fi

echo ""
echo "  BallastLane Dev Setup — PowerShell Core required"
echo "  ─────────────────────────────────────────────────"
echo "  The setup orchestrator (start-dev-sqlite.ps1) is written in PowerShell."
echo "  Install PowerShell Core and re-run:"
echo ""
echo "    macOS:         brew install --cask powershell"
echo "    Ubuntu/Debian: sudo apt-get install -y powershell"
echo "    Other:         https://learn.microsoft.com/powershell/scripting/install/installing-powershell"
echo ""
exit 1
