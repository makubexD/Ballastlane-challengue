#Requires -Version 5.1
<#
.SYNOPSIS
    BallastLane — one-command developer setup.
    Validates prerequisites, generates .env if needed, starts Docker or Podman,
    launches backend and frontend in parallel windows, and waits for
    both services to be healthy before showing a summary.
#>

# ─── Prefer PowerShell 7 when available ──────────────────────────────────────
if ($PSVersionTable.PSVersion.Major -lt 7) {
    $pwsh7 = Get-Command 'pwsh' -ErrorAction SilentlyContinue
    if ($pwsh7) {
        & $pwsh7.Path -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Definition @args
        exit $LASTEXITCODE
    }
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Definition

# ═══════════════════════════════════════════════════════════════════════════════
# CONFIGURATION — single source of truth
# Change ports, paths, container name, or timeouts here only.
# ═══════════════════════════════════════════════════════════════════════════════

$Config = [ordered]@{
    # ── Service ports ──────────────────────────────────────────────────────────
    ApiPort          = 5000
    FrontendPort     = 4200
    PgAdminPort      = 5050
    PodmanTunnelPort = 52375

    # ── Backend project layout (relative to backend/) ─────────────────────────
    BackendProject   = 'src/BallastLane.API'
    AppSettingsSrc   = 'src/BallastLane.API/appsettings.json'
    AppSettingsBin   = 'src/BallastLane.API/bin/Debug/net8.0/appsettings.json'

    # ── Infrastructure ────────────────────────────────────────────────────────
    DbContainer      = 'ballastlane_db'
    PgAdminContainer = 'ballastlane_pgadmin'

    # ── Version minimums ──────────────────────────────────────────────────────
    DotnetMin        = 8
    NodeMin          = 20    # hard fail below this
    NodeIdeal        = 22    # warn below this

    # ── Timeouts (seconds) ────────────────────────────────────────────────────
    DbHealthTimeout  = 30
    ApiStartTimeout  = 60
    TunnelTimeout    = 8
}

# ─── Color helpers ────────────────────────────────────────────────────────────

function Write-Ok   { param($msg) Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "  [!!] $msg" -ForegroundColor Yellow }
function Write-Err  { param($msg) Write-Host "  [XX] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "  --> $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n$msg" -ForegroundColor White }

function Write-Banner {
    $line = '=' * 50
    Write-Host "`n$line" -ForegroundColor Blue
    Write-Host '  BallastLane Dev Environment' -ForegroundColor Blue
    Write-Host "$line`n" -ForegroundColor Blue
}

function Write-Summary {
    $line = '=' * 50
    $dbLabel = if ($useSqlite) { 'SQLite (local file)' } else { "PostgreSQL @ $($env:DB_HOST):$hostDbPort" }
    Write-Host "`n$line" -ForegroundColor Green
    Write-Host '  All services are running!' -ForegroundColor Green
    Write-Host ''
    Write-Host "  Frontend  -->  http://localhost:$($Config.FrontendPort)" -ForegroundColor White
    Write-Host "  API       -->  http://localhost:$($Config.ApiPort)" -ForegroundColor White
    Write-Host "  Swagger   -->  http://localhost:$($Config.ApiPort)/swagger" -ForegroundColor White
    if (-not $useSqlite) {
        Write-Host "  pgAdmin   -->  http://localhost:$($Config.PgAdminPort)" -ForegroundColor White
    }
    Write-Host "  DB        -->  $dbLabel" -ForegroundColor White
    Write-Host ''
    Write-Host '  Demo: demo@ballastlane.com  /  Demo@1234' -ForegroundColor Cyan
    Write-Host ''
    Write-Host '  Backend and frontend are running in separate windows.' -ForegroundColor DarkGray
    Write-Host '  Press Ctrl+C here to stop all services and close those windows.' -ForegroundColor DarkGray
    Write-Host '  Closing this terminal window directly may leave service windows open.' -ForegroundColor DarkGray
    Write-Host "$line`n" -ForegroundColor Green
}

# ─── Utilities ────────────────────────────────────────────────────────────────

function Invoke-Tool {
    param([string]$cmd, [string[]]$args_)
    try {
        $output = & $cmd @args_ 2>&1
        return @{ Output = ($output -join ' '); ExitCode = $LASTEXITCODE }
    }
    catch {
        return @{ Output = ''; ExitCode = 1 }
    }
}

function Get-VersionMajor {
    param([string]$versionString)
    if ($versionString -match '(\d+)\.') { return [int]$Matches[1] }
    return 0
}

function Read-DotEnv {
    param([string]$Path)
    $vars = @{}
    if (-not (Test-Path $Path)) { return $vars }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith('#') -and $line -match '^([^=]+)=(.*)$') {
            $vars[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $vars
}

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

# ─── Detection ────────────────────────────────────────────────────────────────

function Get-AvailableEngines {
    $engines = @()
    $d = Invoke-Tool 'docker' @('--version')
    if ($d.ExitCode -eq 0 -and $d.Output -notmatch 'podman') { $engines += 'docker' }
    $p = Invoke-Tool 'podman' @('--version')
    if ($p.ExitCode -eq 0) { $engines += 'podman' }
    return $engines
}

function Get-ComposeForEngine {
    param([string]$engine)
    if ($engine -eq 'docker') {
        $dcp = Invoke-Tool 'docker' @('compose', 'version')
        if ($dcp.ExitCode -eq 0) { return @{ Cmd = 'docker'; Args = @('compose') } }
        $dc = Invoke-Tool 'docker-compose' @('--version')
        if ($dc.ExitCode -eq 0) { return @{ Cmd = 'docker-compose'; Args = @() } }
    }
    if ($engine -eq 'podman') {
        $pci = Invoke-Tool 'podman' @('compose', 'version')
        if ($pci.ExitCode -eq 0) { return @{ Cmd = 'podman'; Args = @('compose') } }
    }
    return $null
}

# ─── Lifecycle helpers ────────────────────────────────────────────────────────

# Patch ConnectionStrings.Database in an appsettings.json file (no-op if absent)
function Set-AppSettings {
    param([string]$Path, [string]$ConnStr)
    if (-not (Test-Path $Path)) { return }
    $j = Get-Content $Path -Raw | ConvertFrom-Json
    $j.ConnectionStrings.Database = $ConnStr
    $j | ConvertTo-Json -Depth 10 | Set-Content $Path -Encoding UTF8
}

# Return the port to use; remaps to the next free port if occupied by a non-container process
function Resolve-Port {
    param([int]$Port)
    $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $listeners) { return $Port }
    $names = @(
        $listeners.OwningProcess | Sort-Object -Unique |
        ForEach-Object { (Get-Process -Id $_ -ErrorAction SilentlyContinue).ProcessName }
    )
    if ($names | Where-Object { $_ -match 'podman|docker|ssh|vpnkit|wslrelay' }) { return $Port }
    $alt = $Port + 2
    while ($alt -lt 5500 -and (Get-NetTCPConnection -LocalPort $alt -State Listen -ErrorAction SilentlyContinue)) { $alt++ }
    Write-Warn "Port $Port in use by '$($names -join ', ')' — remapping to $alt."
    $env:DB_PORT = "$alt"
    return $alt
}

# Poll an HTTP endpoint until ready. Pass $Process to detect early process crash.
function Wait-Service {
    param(
        [string]$Url,
        [string]$Label,
        [int]$TimeoutSecs,
        [System.Diagnostics.Process]$Process = $null
    )
    Write-Host "  Waiting for $Label" -NoNewline -ForegroundColor DarkGray
    $deadline = (Get-Date).AddSeconds($TimeoutSecs)
    while ((Get-Date) -lt $deadline) {
        if ($Process -and $Process.HasExited) {
            Write-Host ' crashed' -ForegroundColor Red
            return $false
        }
        try {
            if ((Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop).StatusCode -lt 400) {
                Write-Host ' ready' -ForegroundColor Green
                return $true
            }
        } catch {}
        Write-Host '.' -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Seconds 1
    }
    Write-Host ' timed out' -ForegroundColor Red
    return $false
}

function Stop-PodmanTunnel {
    if ($script:PodmanTunnel -and -not $script:PodmanTunnel.HasExited) {
        Stop-Process -Id $script:PodmanTunnel.Id -Force -ErrorAction SilentlyContinue
        $script:PodmanTunnel = $null
    }
    Remove-Item Env:DOCKER_HOST -ErrorAction SilentlyContinue
}

# Tear down containers, stop tunnel, restore location, and exit. Requires $ComposeEngine / $prevLocation.
function Stop-Stack {
    param([string]$ErrorMsg)
    Write-Err $ErrorMsg
    if ($ComposeEngine) {
        Invoke-Tool $ComposeEngine.Cmd ($ComposeEngine.Args + @('down', '--remove-orphans'))
    }
    Stop-PodmanTunnel
    Set-Location $prevLocation
    exit 1
}

function Stop-OrphanedService {
    param([int]$Port, [string]$Label)
    $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $listeners) { return }
    $procs = @(
        $listeners.OwningProcess | Sort-Object -Unique |
        ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue } |
        Where-Object { $_ -and $_.ProcessName -notmatch 'podman|docker|ssh|vpnkit|wslrelay' }
    )
    foreach ($proc in $procs) {
        Write-Warn "Stopping orphaned $Label on port $Port (PID $($proc.Id) / $($proc.ProcessName))"
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    if ($procs.Count -gt 0) { Start-Sleep -Milliseconds 500 }
}

# Configure Podman for Docker-compatible API access (Windows only)
function Initialize-Podman {
    # Strip docker-compose.exe Windows Store alias (Docker Desktop stub, fails without DD)
    $env:PATH = ($env:PATH -split ';' | Where-Object {
        $d = $_.Trim()
        -not ($d -match 'WindowsApps' -and (Test-Path (Join-Path $d 'docker-compose.exe')))
    }) -join ';'

    # Restore containers.conf if Podman Desktop compose binary exists but config was lost
    $pdComposeExe = Join-Path $HOME '.local\share\containers\podman-desktop\extensions-storage\podman-desktop.compose\bin\docker-compose.exe'
    $confDir  = Join-Path $env:APPDATA 'containers'
    $confFile = Join-Path $confDir 'containers.conf'
    if ((Test-Path $pdComposeExe) -and -not (Test-Path $confFile)) {
        New-Item -ItemType Directory -Path $confDir -Force | Out-Null
        Set-Content $confFile -Encoding UTF8 -Value @"
[engine]
compose_providers = ['$pdComposeExe']
"@
        Write-Info 'Restored Podman Desktop compose configuration'
    }
    $pdBin = Split-Path -Parent $pdComposeExe
    if ((Test-Path $pdBin) -and ($env:PATH -notmatch [regex]::Escape($pdBin))) { $env:PATH = "$pdBin;$env:PATH" }

    # Kill any stale SSH tunnel left over from a previous run (port still occupied)
    $staleListeners = Get-NetTCPConnection -LocalPort $Config.PodmanTunnelPort -State Listen -ErrorAction SilentlyContinue
    if ($staleListeners) {
        $staleListeners.OwningProcess | Sort-Object -Unique | ForEach-Object {
            $p = Get-Process -Id $_ -ErrorAction SilentlyContinue
            if ($p -and $p.ProcessName -match 'ssh') {
                Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
            }
        }
        Start-Sleep -Milliseconds 400
    }

    # Read machine SSH connection info
    $machineConn = (& podman machine inspect 2>$null | ConvertFrom-Json)[0]
    $sshKey  = $machineConn.SSHConfig.IdentityPath
    $sshPort = [int]$machineConn.SSHConfig.Port

    # Verify SSH port is reachable — WSL2 port forwarding breaks after Windows
    # hibernate/resume or unclean shutdown without stopping the Podman machine.
    $sshReachable = $false
    $sshCheckDeadline = (Get-Date).AddSeconds(2)
    while ((Get-Date) -lt $sshCheckDeadline) {
        try {
            $tc = New-Object System.Net.Sockets.TcpClient
            $tc.Connect('127.0.0.1', $sshPort)
            $tc.Close(); $sshReachable = $true; break
        } catch { Start-Sleep -Milliseconds 300 }
    }
    if (-not $sshReachable) {
        Write-Warn "Podman machine SSH not reachable on port $sshPort — resetting WSL2 network stack..."
        Write-Info 'Running: wsl --shutdown (resets WSL2 port forwarding)...'
        wsl --shutdown 2>&1 | Out-Null
        Start-Sleep -Seconds 3
        Write-Info 'Running: podman machine start (may take up to 60 seconds)...'
        & podman machine start 2>&1 | Out-Null
        # Verify via machine state — not exit code (Docker pipe conflict causes false non-zero)
        $machineConn = (& podman machine inspect 2>$null | ConvertFrom-Json)[0]
        if ($machineConn.State -ne 'running') {
            Write-Err "Podman machine failed to restart (state: $($machineConn.State)). Run: wsl --shutdown && podman machine start"
            exit 1
        }
        $sshKey  = $machineConn.SSHConfig.IdentityPath
        $sshPort = [int]$machineConn.SSHConfig.Port
        # Re-probe SSH with generous timeout — machine cold-started from zero
        $sshReachable = $false
        $sshReprobeDeadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $sshReprobeDeadline) {
            try {
                $tc = New-Object System.Net.Sockets.TcpClient
                $tc.Connect('127.0.0.1', $sshPort)
                $tc.Close(); $sshReachable = $true; break
            } catch { Start-Sleep -Milliseconds 500 }
        }
        if (-not $sshReachable) {
            Write-Err "Podman machine started but SSH still not reachable on port $sshPort after WSL2 reset. Run: podman machine ls"
            exit 1
        }
        Write-Ok 'WSL2 reset complete — Podman machine ready'
    }

    # Find a free local port for the Podman SSH tunnel.
    # The static default (52375) falls in a Windows-excluded range reserved by Hyper-V/WSL2.
    # Scanning 40000-49000 (below the 49152 ephemeral start) and attempting a real TcpListener
    # bind is the only reliable way to skip both the exclusion list and any occupied ports.
    $tunnelPort = $null
    for ($p = 40000; $p -le 49000; $p++) {
        try {
            $l = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $p)
            $l.Start(); $l.Stop()
            $tunnelPort = $p; break
        } catch { }
    }
    if ($null -eq $tunnelPort) {
        Write-Err 'No free port found in 40000-49000 for the Podman socket tunnel.'
        exit 1
    }
    $Config['PodmanTunnelPort'] = $tunnelPort

    # Tunnel Podman socket via SSH to a local TCP port.
    # Always use the Windows System32 OpenSSH client — it handles Windows-style key paths
    # correctly regardless of which terminal launched the script (Git Bash, CMD, PS5, PS7).
    # Using bare 'ssh' would resolve to Git-for-Windows SSH when run from Git Bash, which
    # expects POSIX paths and fails with the Windows-style key path from podman machine inspect.
    $sshExe = Join-Path $env:SystemRoot 'System32\OpenSSH\ssh.exe'
    if (-not (Test-Path $sshExe)) {
        Write-Err 'OpenSSH Client not found. Install: Settings -> Apps -> Optional Features -> OpenSSH Client.'
        exit 1
    }

    $sshErrFile = [System.IO.Path]::GetTempFileName()
    $script:PodmanTunnel = Start-Process $sshExe -ArgumentList @(
        '-N', '-L', "$($Config.PodmanTunnelPort):/run/podman/podman.sock",
        '-i', $sshKey, '-p', $sshPort,
        '-o', 'StrictHostKeyChecking=no', '-o', 'UserKnownHostsFile=/dev/null',
        'root@127.0.0.1'
    ) -PassThru -WindowStyle Hidden -RedirectStandardError $sshErrFile -ErrorAction SilentlyContinue

    if (-not $script:PodmanTunnel) {
        Remove-Item $sshErrFile -ErrorAction SilentlyContinue
        Write-Err 'Could not start SSH tunnel to Podman. Install OpenSSH Client via Settings -> Apps -> Optional Features.'
        exit 1
    }

    $ready = $false
    $deadline = (Get-Date).AddSeconds($Config.TunnelTimeout)
    while ((Get-Date) -lt $deadline) {
        if ($script:PodmanTunnel.HasExited) { break }   # SSH exited immediately — no point waiting
        try {
            $tc = New-Object System.Net.Sockets.TcpClient
            $tc.Connect('127.0.0.1', $Config.PodmanTunnelPort)
            $tc.Close(); $ready = $true; break
        } catch { Start-Sleep -Milliseconds 300 }
    }

    if ($ready) {
        Remove-Item $sshErrFile -ErrorAction SilentlyContinue
        $env:DOCKER_HOST = "tcp://localhost:$($Config.PodmanTunnelPort)"
        Write-Info "Podman socket tunneled → tcp://localhost:$($Config.PodmanTunnelPort)"
    } else {
        $sshErr = if (Test-Path $sshErrFile) {
            (Get-Content $sshErrFile -Raw -ErrorAction SilentlyContinue) -replace '\r?\n', ' '
        } else { '' }
        Remove-Item $sshErrFile -ErrorAction SilentlyContinue
        $sshExited = $script:PodmanTunnel -and $script:PodmanTunnel.HasExited
        if ($script:PodmanTunnel -and -not $sshExited) {
            Stop-Process -Id $script:PodmanTunnel.Id -Force -ErrorAction SilentlyContinue
        }
        $script:PodmanTunnel = $null
        if ($sshErr) { Write-Host "  SSH output: $sshErr" -ForegroundColor DarkRed }
        if ($sshExited) {
            Write-Err "SSH tunnel process exited immediately (check key path: $sshKey)"
        } else {
            Write-Err "SSH tunnel to Podman socket timed out. SSH port: $sshPort. Run: podman machine ls"
        }
        exit 1
    }

    # Suppress docker-credential-desktop errors (Docker Desktop leftover in ~/.docker/config.json)
    $tmp = Join-Path $env:TEMP 'podman-compose-config'
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null
    Set-Content (Join-Path $tmp 'config.json') '{}' -Encoding UTF8
    $env:DOCKER_CONFIG = $tmp
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

# Clear any stale Podman tunnel env from a previous run in this shell
Remove-Item Env:DOCKER_HOST -ErrorAction SilentlyContinue

Write-Banner

# ─── Step 1: Prerequisites ────────────────────────────────────────────────────

Write-Step '[1/5] Validating prerequisites...'

$availableEngines = @(Get-AvailableEngines)

if ($availableEngines.Count -eq 0) {
    Write-Warn 'No container engine (Docker/Podman) detected.'
    Write-Info 'Falling back to SQLite — no database server required.'
    $useSqlite = $true
    $ContainerEngine = $null
}
elseif ($availableEngines.Count -eq 1) {
    $useSqlite = $false
    $ContainerEngine = $availableEngines[0]
    Write-Info "Using $ContainerEngine (only engine detected)"
}
else {
    $useSqlite = $false
    Write-Host ''
    Write-Host '  Both Docker and Podman are available. Which engine to use?' -ForegroundColor White
    Write-Host '    [1] Docker' -ForegroundColor Cyan
    Write-Host '    [2] Podman' -ForegroundColor Cyan
    $choice = Read-Host '  Enter 1 or 2'
    $ContainerEngine = if ($choice -eq '1') { 'docker' } else { 'podman' }
    Write-Info "Using $ContainerEngine (selected)"
}

if (-not $useSqlite) {
    $engineVer = Invoke-Tool $ContainerEngine @('--version')
    Write-Ok "$ContainerEngine $($engineVer.Output -replace '^.*?([\d]+\.[\d]+\.[\d]+).*$','$1')"

    if ($ContainerEngine -eq 'podman') {
        $machineList = Invoke-Tool 'podman' @('machine', 'list', '--noheading')
        if ($machineList.Output -notmatch '\S') {
            Write-Err 'No Podman machine found. Run: podman machine init && podman machine start'
            exit 1
        }
        $machineInfo = (& podman machine inspect 2>$null | ConvertFrom-Json)
        if ($machineInfo -and $machineInfo[0].State -eq 'running') {
            Write-Ok 'Podman machine running'
        } else {
            Write-Warn 'Podman machine exists but is not running. Starting it...'
            Write-Info 'Running: podman machine start (may take up to 60 seconds)...'
            $startOutput = (& podman machine start 2>&1) -join ' '
            if ($LASTEXITCODE -ne 0) {
                Write-Err "Failed to start Podman machine: $startOutput. If WSL2 is missing: wsl --install (then reboot). To reset: podman machine rm && podman machine init && podman machine start"
                exit 1
            }
            Write-Ok 'Podman machine started'
        }
        # Set up SSH tunnel now — required for all subsequent podman/compose calls
        Initialize-Podman
    }

    $enginePs = Invoke-Tool $ContainerEngine @('ps')
    if ($enginePs.ExitCode -ne 0) {
        Write-Err "$ContainerEngine daemon is not running. Start it and try again."
        exit 1
    }
    Write-Ok "$ContainerEngine daemon running"

    $ComposeEngine = Get-ComposeForEngine $ContainerEngine
    if ($null -eq $ComposeEngine) {
        $composeHint = if ($ContainerEngine -eq 'podman') {
            'Install Podman Desktop with the Compose extension.'
        } else {
            'Install Docker Compose: https://docs.docker.com/compose/install/'
        }
        Write-Err "No compose tool found for $ContainerEngine. $composeHint"
        exit 1
    }
    $composeLabel = (@($ComposeEngine.Cmd) + $ComposeEngine.Args) -join ' '
    Write-Ok "Compose: $composeLabel"
} else {
    Write-Ok 'Container engine: SQLite fallback (no container needed)'
    $ComposeEngine = $null
    $composeLabel  = $null
}

$dotnet = Invoke-Tool 'dotnet' @('--version')
if ($dotnet.ExitCode -ne 0) {
    Write-Err '.NET SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/8.0'
    exit 1
}
if ((Get-VersionMajor $dotnet.Output) -lt $Config.DotnetMin) {
    Write-Err ".NET SDK $($dotnet.Output) is too old. Version $($Config.DotnetMin)+ required."
    exit 1
}
Write-Ok ".NET SDK $($dotnet.Output)"

$node = Invoke-Tool 'node' @('--version')
if ($node.ExitCode -ne 0) {
    Write-Err 'Node.js not found. Install from https://nodejs.org (LTS 22 recommended)'
    exit 1
}
$nodeMajor = Get-VersionMajor ($node.Output -replace 'v', '')
if ($nodeMajor -lt $Config.NodeMin) {
    Write-Err "Node.js $($node.Output) is too old. Version $($Config.NodeMin).x+ required."
    exit 1
} elseif ($nodeMajor -lt $Config.NodeIdeal) {
    Write-Warn "Node.js $($node.Output) — version $($Config.NodeIdeal) LTS recommended."
} else {
    Write-Ok "Node.js $($node.Output)"
}

# ─── Step 2: Environment ──────────────────────────────────────────────────────

Write-Step '[2/5] Checking environment...'

$envFile     = Join-Path $Root '.env'
$placeholder = 'change-me-to-a-256-bit-random-secret-before-production'

if (-not (Test-Path $envFile)) {
    $jwtSecret = New-RandomSecret
    $now = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $content = @"
# Generated by start-dev on $now
DB_HOST=localhost
DB_PORT=5432
DB_NAME=ballastlane
DB_USER=ballastlane
DB_PASSWORD=ballastlane_dev

TEST_DB_HOST=localhost
TEST_DB_PORT=5433
TEST_DB_NAME=ballastlane_test
TEST_DB_USER=ballastlane_test
TEST_DB_PASSWORD=ballastlane_test

JWT_SECRET=$jwtSecret
JWT_EXPIRY_MINUTES=60
"@
    Set-Content -Path $envFile -Value $content -Encoding UTF8
    Write-Ok '.env generated with cryptographically random JWT_SECRET'
}
else {
    Write-Ok '.env file found'
}

$env = Read-DotEnv $envFile

$required = @('DB_HOST','DB_PORT','DB_NAME','DB_USER','DB_PASSWORD','JWT_SECRET','JWT_EXPIRY_MINUTES')
$missing  = @($required | Where-Object { -not $env.ContainsKey($_) })
if ($missing.Count -gt 0) {
    Write-Err "Missing required keys in .env: $($missing -join ', ')"
    exit 1
}
Write-Ok 'All required variables present'

if ($env['JWT_SECRET'] -eq $placeholder) {
    Write-Warn 'JWT_SECRET is using the example placeholder — safe for local dev only'
}

# ─── Step 3: Database ─────────────────────────────────────────────────────────

Write-Step '[3/5] Starting database...'

$backendProc  = $null
$frontendProc = $null

try {

$prevLocation = Get-Location
Set-Location $Root

if (-not $useSqlite) {
    $hostDbPort   = Resolve-Port ([int]$env['DB_PORT'])

    # Force-remove stale containers by name before starting fresh.
    # compose down only removes containers it created (matched by project label);
    # containers created under a different project/path are invisible to it.
    # try/catch required: $ErrorActionPreference = 'Stop' turns the NativeCommandError
    # from "No such container" into a terminating error — catch swallows it.
    try {
        $null = & $ContainerEngine 'rm' '-f' $Config.DbContainer $Config.PgAdminContainer 2>&1
    } catch { <# containers may not exist on first run or after compose down — expected #> }

    Write-Info "Running: $composeLabel up -d postgres pgadmin"
    $dcUp = Invoke-Tool $ComposeEngine.Cmd ($ComposeEngine.Args + @('up', '-d', 'postgres', 'pgadmin'))
    if ($dcUp.ExitCode -ne 0) {
        Stop-Stack "Compose failed: $($dcUp.Output)"
    }

    $healthElapsed = 0
    Write-Host '  Waiting for PostgreSQL' -NoNewline -ForegroundColor DarkGray
    $dbHealthy = $false
    while ($healthElapsed -lt $Config.DbHealthTimeout) {
        try {
            $health = & $ContainerEngine inspect --format='{{.State.Health.Status}}' $Config.DbContainer 2>&1
            if ($health -and $health.Trim() -eq 'healthy') {
                Write-Host ' healthy' -ForegroundColor Green
                $dbHealthy = $true
                break
            }
        }
        catch { }
        Write-Host '.' -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Seconds 2
        $healthElapsed += 2
    }
    if (-not $dbHealthy) {
        Stop-Stack "PostgreSQL did not become healthy in $($Config.DbHealthTimeout)s. Check: $ContainerEngine logs $($Config.DbContainer)"
    }
    Write-Ok "PostgreSQL healthy (port $hostDbPort)"

    Write-Info 'Ensuring DB user exists and password matches .env...'
    $user = $env['DB_USER']
    $pass = $env['DB_PASSWORD']
    $db   = $env['DB_NAME']
    # 1 — Ensure role exists + set password (DO block; no CREATE DATABASE here)
    $ensureRoleSql = "DO `$`$ BEGIN " +
        "IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$user') THEN " +
        "CREATE ROLE $user WITH LOGIN; " +
        "END IF; " +
        "ALTER ROLE $user WITH PASSWORD '$pass'; " +
        "END `$`$;"
    $r1 = Invoke-Tool $ContainerEngine @('exec', $Config.DbContainer, 'psql', '-U', $user, '-c', $ensureRoleSql)
    if ($r1.ExitCode -ne 0) {
        Stop-Stack "Failed to configure DB user: $($r1.Output). Check: $ContainerEngine logs $($Config.DbContainer)"
    }

    # 2 — Ensure database exists (CREATE DATABASE cannot run inside a DO block)
    $dbExists = Invoke-Tool $ContainerEngine @('exec', $Config.DbContainer, 'psql', '-U', $user, '-tAc', "SELECT 1 FROM pg_database WHERE datname = '$db'")
    if ($dbExists.Output.Trim() -ne '1') {
        $r2 = Invoke-Tool $ContainerEngine @('exec', $Config.DbContainer, 'psql', '-U', $user, '-c', "CREATE DATABASE $db OWNER $user")
        if ($r2.ExitCode -ne 0) {
            Stop-Stack "Failed to create database: $($r2.Output). Check: $ContainerEngine logs $($Config.DbContainer)"
        }
    }

    # 3 — Always ensure privileges (idempotent)
    $r3 = Invoke-Tool $ContainerEngine @('exec', $Config.DbContainer, 'psql', '-U', $user, '-c', "GRANT ALL PRIVILEGES ON DATABASE $db TO $user")
    if ($r3.ExitCode -ne 0) {
        Stop-Stack "Failed to grant DB privileges: $($r3.Output). Check: $ContainerEngine logs $($Config.DbContainer)"
    }
    Write-Ok "DB ready (user=$user)"

    Write-Ok "pgAdmin ready (port $($Config.PgAdminPort))"
    Stop-PodmanTunnel
} else {
    $hostDbPort = 0
    Write-Ok 'Database: SQLite (file-based, no container needed)'
}

Set-Location $prevLocation

# ─── Step 4: Start services ───────────────────────────────────────────────────

Write-Step '[4/5] Starting services...'

$backendDir  = Join-Path $Root 'backend'
$frontendDir = Join-Path $Root 'frontend'

    if ($useSqlite) {
        $dbPath = Join-Path $Root 'ballastlane.sqlite'
        $env:ConnectionStrings__Database = "Data Source=$dbPath"
        $env:ConnectionStrings__Provider  = 'SQLite'
        $connStr = $env:ConnectionStrings__Database
        Write-Info "SQLite database: $dbPath"
    } else {
        $dbHost  = if ($env['DB_HOST'] -eq 'localhost') { '127.0.0.1' } else { $env['DB_HOST'] }
        $connStr = "Host=$dbHost;Port=$hostDbPort;Database=$($env['DB_NAME']);" +
                   "Username=$($env['DB_USER']);Password=$($env['DB_PASSWORD'])"

        Set-AppSettings (Join-Path $backendDir $Config.AppSettingsSrc) $connStr
        Set-AppSettings (Join-Path $backendDir $Config.AppSettingsBin) $connStr
        Write-Info "appsettings.json patched (DB: $($env['DB_USER'])@${dbHost}:${hostDbPort}/$($env['DB_NAME']))"
    }

    Stop-OrphanedService $Config.ApiPort     'API'
    Stop-OrphanedService $Config.FrontendPort 'Frontend'

    Write-Info 'Building backend (first run may take a moment)...'
    $buildResult = Invoke-Tool 'dotnet' @(
        'build', (Join-Path $backendDir $Config.BackendProject),
        '--verbosity', 'quiet', '--nologo'
    )
    if ($buildResult.ExitCode -ne 0) {
        Write-Err "Backend build failed: $($buildResult.Output)"
        exit 1
    }
    Write-Ok 'Backend built'

    $providerEnv = if ($useSqlite) { "`$env:ConnectionStrings__Provider = 'SQLite'; " } else { "`$env:ConnectionStrings__Provider = 'PostgreSQL'; " }
    $backendCmd = "Set-Location '$backendDir'; " +
        "`$env:ASPNETCORE_ENVIRONMENT = 'Development'; " +
        "`$env:ConnectionStrings__Database = '$connStr'; " +
        $providerEnv +
        "`$env:JWT_SECRET = '$($env['JWT_SECRET'])'; " +
        "Write-Host 'Starting backend...' -ForegroundColor Cyan; " +
        "dotnet run --project $($Config.BackendProject) --no-build --no-launch-profile --urls http://localhost:$($Config.ApiPort)"
    $backendProc = Start-Process powershell `
        -ArgumentList '-Command', $backendCmd `
        -WindowStyle Normal `
        -PassThru
    Write-Ok "Backend window opened (http://localhost:$($Config.ApiPort))"

    $frontendCmd = "Set-Location '$frontendDir'; `$env:NG_CLI_ANALYTICS = 'false'; Write-Host 'Starting frontend...' -ForegroundColor Cyan; npm install; ng serve"
    $frontendProc = Start-Process powershell `
        -ArgumentList '-NoExit', '-Command', $frontendCmd `
        -WindowStyle Normal `
        -PassThru
    Write-Ok "Frontend window opened (http://localhost:$($Config.FrontendPort))"

    # ─── Step 5: Health checks ────────────────────────────────────────────────────

    Write-Step '[5/5] Waiting for services to be ready...'

    $apiUrl = "http://localhost:$($Config.ApiPort)/healthz/live"
    if (-not (Wait-Service $apiUrl 'API' $Config.ApiStartTimeout $backendProc)) {
        Write-Err "API did not start in $($Config.ApiStartTimeout)s. If PostgreSQL auth error: $composeLabel down -v"
        exit 1
    }

    $frontendUrl   = "http://localhost:$($Config.FrontendPort)"
    $feMaxWait     = 300   # hard ceiling (s) — covers catastrophic slow-machine scenarios
    $fePhase       = 60    # poll in 60-second phases
    $feElapsed     = 0
    $frontendReady = $false

    while (-not $frontendReady -and $feElapsed -lt $feMaxWait) {
        $remaining     = [Math]::Min($fePhase, $feMaxWait - $feElapsed)
        $frontendReady = Wait-Service $frontendUrl 'Frontend' $remaining $frontendProc
        $feElapsed    += $remaining

        if (-not $frontendReady) {
            if ($frontendProc.HasExited) { break }   # crashed — no point waiting more
            if ($feElapsed -lt $feMaxWait) {
                Write-Info "Frontend still compiling... ($feElapsed s elapsed, up to $feMaxWait s total)"
            }
        }
    }

    if (-not $frontendReady) {
        if ($frontendProc.HasExited) {
            Write-Err 'Frontend process crashed. Check the Frontend window for errors.'
        } else {
            Write-Err "Frontend did not respond in $feMaxWait s. On a slow machine this can happen — check the Frontend window."
        }
        exit 1
    }

    # ─── Summary ──────────────────────────────────────────────────────────────────

    Write-Summary

    # Keep orchestrator alive so Ctrl+C triggers the finally block
    while ($true) { Start-Sleep -Seconds 60 }
}
finally {
    Write-Host ''
    if ($backendProc -and -not $backendProc.HasExited) {
        Stop-Process -Id $backendProc.Id -Force -ErrorAction SilentlyContinue
        Write-Host '  Backend window closed.' -ForegroundColor DarkGray
    }
    if ($frontendProc -and -not $frontendProc.HasExited) {
        Stop-Process -Id $frontendProc.Id -Force -ErrorAction SilentlyContinue
        Write-Host '  Frontend window closed.' -ForegroundColor DarkGray
    }
    Stop-PodmanTunnel
    Write-Host '  Dev environment stopped.' -ForegroundColor DarkGray
    Write-Host '  If any service window remains open, close it manually.' -ForegroundColor DarkGray
}
