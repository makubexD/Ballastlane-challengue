#Requires -Version 5.1
<#
.SYNOPSIS
    BallastLane — one-command developer setup.
    Validates prerequisites, generates .env if needed, starts Docker or Podman,
    launches backend and frontend in parallel windows, and waits for
    both services to be healthy before showing a summary.
#>

# ─── Prefer PowerShell 7 when available ──────────────────────────────────────
# PS7 offers better ANSI colors, improved error messages, and modern runtime.
# If running in PS5 and pwsh is on PATH, re-launch this script under PS7.
if ($PSVersionTable.PSVersion.Major -lt 7) {
    $pwsh7 = Get-Command 'pwsh' -ErrorAction SilentlyContinue
    if ($pwsh7) {
        & $pwsh7.Path -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Definition @args
        exit $LASTEXITCODE
    }
    # pwsh not found -- continue in PS5
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Definition

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
    Write-Host "`n$line" -ForegroundColor Green
    Write-Host '  All services are running!' -ForegroundColor Green
    Write-Host ''
    Write-Host '  Frontend  -->  http://localhost:4200' -ForegroundColor White
    Write-Host '  API       -->  http://localhost:5000' -ForegroundColor White
    Write-Host '  Swagger   -->  http://localhost:5000/swagger' -ForegroundColor White
    Write-Host '  pgAdmin   -->  http://localhost:5050' -ForegroundColor White
    Write-Host ''
    Write-Host '  Demo: demo@ballastlane.com  /  Demo@1234' -ForegroundColor Cyan
    Write-Host ''
    Write-Host '  Backend and frontend are running in separate windows.' -ForegroundColor DarkGray
    Write-Host '  Press Ctrl+C here to stop all services and close those windows.' -ForegroundColor DarkGray
    Write-Host '  Closing this terminal window directly may leave service windows open.' -ForegroundColor DarkGray
    Write-Host "$line`n" -ForegroundColor Green
}

# ─── Helper: run a command and return (stdout, exit code) ─────────────────────

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

# ─── Helper: parse version major from "X.Y.Z" or "vX.Y.Z" ────────────────────

function Get-VersionMajor {
    param([string]$versionString)
    if ($versionString -match '(\d+)\.') { return [int]$Matches[1] }
    return 0
}

# ─── Helper: wait for HTTP endpoint ──────────────────────────────────────────

function Wait-Http {
    param(
        [string]$Url,
        [string]$Label,
        [int]$TimeoutSecs = 120,
        [int]$IntervalSecs = 3
    )
    $elapsed = 0
    Write-Host "  Waiting for $Label" -NoNewline -ForegroundColor DarkGray
    while ($elapsed -lt $TimeoutSecs) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -lt 400) {
                Write-Host ' ready' -ForegroundColor Green
                return $true
            }
        }
        catch { }
        Write-Host '.' -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Seconds $IntervalSecs
        $elapsed += $IntervalSecs
    }
    Write-Host ' timed out' -ForegroundColor Red
    return $false
}

# ─── Helper: parse .env file → hashtable ─────────────────────────────────────

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

# ─── Helper: generate cryptographically random Base64 secret ─────────────────

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

# ─── Helper: list genuinely available container engines ───────────────────────
# docker.cmd wrapping podman reports "podman" in version output — counts as podman only

function Get-AvailableEngines {
    $engines = @()
    $d = Invoke-Tool 'docker' @('--version')
    if ($d.ExitCode -eq 0 -and $d.Output -notmatch 'podman') { $engines += 'docker' }
    $p = Invoke-Tool 'podman' @('--version')
    if ($p.ExitCode -eq 0) { $engines += 'podman' }
    return $engines
}

# ─── Helper: detect compose tool for a specific engine ────────────────────────
# Docker and Podman paths are fully isolated — no cross-engine tool usage.

function Get-ComposeForEngine {
    param([string]$engine)

    if ($engine -eq 'docker') {
        # Docker-only path: prefer the compose plugin, fall back to standalone binary
        $dcp = Invoke-Tool 'docker' @('compose', 'version')
        if ($dcp.ExitCode -eq 0) { return @{ Cmd = 'docker'; Args = @('compose') } }
        $dc = Invoke-Tool 'docker-compose' @('--version')
        if ($dc.ExitCode -eq 0) { return @{ Cmd = 'docker-compose'; Args = @() } }
    }

    if ($engine -eq 'podman') {
        # Podman-only path: test that 'podman compose' is available and working
        $pci = Invoke-Tool 'podman' @('compose', 'version')
        if ($pci.ExitCode -eq 0) { return @{ Cmd = 'podman'; Args = @('compose') } }
    }

    return $null
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

Write-Banner

# ─── Step 1: Prerequisites ────────────────────────────────────────────────────

Write-Step '[1/5] Validating prerequisites...'

# Container engine — auto-select or prompt if both are available
$availableEngines = @(Get-AvailableEngines)

if ($availableEngines.Count -eq 0) {
    Write-Err 'No container engine found.'
    Write-Err '  Docker Desktop: https://www.docker.com/products/docker-desktop'
    Write-Err '  Podman:         https://podman.io/getting-started/installation'
    exit 1
}

if ($availableEngines.Count -eq 1) {
    $ContainerEngine = $availableEngines[0]
    Write-Info "Using $ContainerEngine (only engine detected)"
}
else {
    Write-Host ''
    Write-Host '  Both Docker and Podman are available. Which engine to use?' -ForegroundColor White
    Write-Host '    [1] Docker' -ForegroundColor Cyan
    Write-Host '    [2] Podman' -ForegroundColor Cyan
    $choice = Read-Host '  Enter 1 or 2'
    $ContainerEngine = if ($choice -eq '1') { 'docker' } else { 'podman' }
    Write-Info "Using $ContainerEngine (selected)"
}

$engineVer = Invoke-Tool $ContainerEngine @('--version')
Write-Ok "$ContainerEngine $($engineVer.Output -replace '^.*?([\d]+\.[\d]+\.[\d]+).*$','$1')"

# Podman machine lifecycle (Podman only)
if ($ContainerEngine -eq 'podman') {
    # Check if any machine has been initialized
    $machineList = Invoke-Tool 'podman' @('machine', 'list', '--noheading')
    if ($machineList.Output -notmatch '\S') {
        Write-Err 'No Podman machine found. Initialize one first:'
        Write-Err '  podman machine init'
        Write-Err '  podman machine start'
        exit 1
    }

    # Use podman ps as the ground-truth running check (version-agnostic)
    $podmanUp = Invoke-Tool 'podman' @('ps')
    if ($podmanUp.ExitCode -eq 0) {
        Write-Ok 'Podman machine running'
    }
    else {
        Write-Warn 'Podman machine exists but is not running. Starting it...'
        Write-Info 'Running: podman machine start (may take up to 60 seconds)...'
        & podman machine start
        if ($LASTEXITCODE -ne 0) {
            Write-Err 'Failed to start Podman machine.'
            Write-Err '  If WSL2 is not installed: run  wsl --install  then reboot'
            Write-Err '  To reset machine: podman machine rm && podman machine init && podman machine start'
            exit 1
        }
        Write-Ok 'Podman machine started'
    }
}

# Daemon / engine responsive
$enginePs = Invoke-Tool $ContainerEngine @('ps')
if ($enginePs.ExitCode -ne 0) {
    Write-Err "$ContainerEngine daemon is not running. Start it and try again."
    exit 1
}
Write-Ok "$ContainerEngine daemon running"

# ─── Podman: restore compose configuration and clean Windows PATH artifacts ───
if ($ContainerEngine -eq 'podman') {
    # Strip the Windows Store docker-compose.exe app alias from this session's PATH.
    # It is a Docker Desktop stub that fails without Docker Desktop installed.
    $env:PATH = ($env:PATH -split ';' | Where-Object {
        $d = $_.Trim()
        -not ($d -match 'WindowsApps' -and (Test-Path (Join-Path $d 'docker-compose.exe')))
    }) -join ';'

    # Podman Desktop installs its compose binary outside of PATH and registers it via
    # containers.conf. If containers.conf was deleted (by a previous script run that
    # incorrectly set compose_providers=[]), restore it now.
    $pdComposeExe = Join-Path $HOME '.local\share\containers\podman-desktop\extensions-storage\podman-desktop.compose\bin\docker-compose.exe'
    $confDir  = Join-Path $env:APPDATA 'containers'
    $confFile = Join-Path $confDir 'containers.conf'

    if ((Test-Path $pdComposeExe) -and -not (Test-Path $confFile)) {
        if (-not (Test-Path $confDir)) { New-Item -ItemType Directory -Path $confDir -Force | Out-Null }
        # TOML literal strings (single-quoted) allow backslashes without escaping
        $confContent = @"
[engine]
compose_providers = ['$pdComposeExe']
"@
        Set-Content $confFile -Value $confContent -Encoding UTF8
        Write-Info 'Restored Podman Desktop compose configuration in containers.conf'
    }

    # Also add the compose binary directory to this session's PATH as belt-and-suspenders
    $pdComposeBin = Split-Path -Parent $pdComposeExe
    if ((Test-Path $pdComposeBin) -and ($env:PATH -notmatch [regex]::Escape($pdComposeBin))) {
        $env:PATH = "$pdComposeBin;$env:PATH"
    }

    # Tunnel the Podman Docker-compatible socket from WSL to a local TCP port via SSH.
    # The Windows-side named pipe relay is not persistently available without Podman Desktop.
    # The SSH connection (proven by 'podman ps') is always available when the machine runs.
    $machineConn = (& podman machine inspect 2>$null | ConvertFrom-Json)[0]
    $sshKey  = $machineConn.SSHConfig.IdentityPath
    $sshPort = [int]$machineConn.SSHConfig.Port
    $script:PodmanApiPort = 52375
    $script:PodmanTunnel = Start-Process 'ssh' -ArgumentList @(
        '-N', '-L', "$($script:PodmanApiPort):/run/podman/podman.sock",
        '-i', $sshKey, '-p', $sshPort,
        '-o', 'StrictHostKeyChecking=no',
        '-o', 'UserKnownHostsFile=/dev/null',
        'root@127.0.0.1'
    ) -PassThru -WindowStyle Hidden -ErrorAction SilentlyContinue

    if (-not $script:PodmanTunnel) {
        Write-Err 'Could not start SSH tunnel to Podman. Ensure OpenSSH Client is installed:'
        Write-Err '  Settings → Apps → Optional Features → OpenSSH Client'
        exit 1
    }

    $tunnelReady = $false
    $deadline = (Get-Date).AddSeconds(8)
    while ((Get-Date) -lt $deadline) {
        try {
            $tc = New-Object System.Net.Sockets.TcpClient
            $tc.Connect('127.0.0.1', $script:PodmanApiPort)
            $tc.Close(); $tunnelReady = $true; break
        } catch { Start-Sleep -Milliseconds 300 }
    }

    if ($tunnelReady) {
        $env:DOCKER_HOST = "tcp://localhost:$($script:PodmanApiPort)"
        Write-Info "Podman socket tunneled → tcp://localhost:$($script:PodmanApiPort)"
    } else {
        Stop-Process -Id $script:PodmanTunnel.Id -Force -ErrorAction SilentlyContinue
        $script:PodmanTunnel = $null
        Write-Err 'SSH tunnel to Podman socket timed out. Check: podman machine ls'
        exit 1
    }

    # ~/.docker/config.json may contain "credsStore": "desktop" (Docker Desktop leftover).
    # docker-credential-desktop is not present on Podman-only systems → compose fails.
    # Override DOCKER_CONFIG with a minimal config that has no credential store.
    $minDockerConf = Join-Path $env:TEMP 'podman-compose-config'
    New-Item -ItemType Directory -Path $minDockerConf -Force | Out-Null
    Set-Content (Join-Path $minDockerConf 'config.json') '{}' -Encoding UTF8
    $env:DOCKER_CONFIG = $minDockerConf
}

function Stop-PodmanTunnel {
    if ($script:PodmanTunnel -and -not $script:PodmanTunnel.HasExited) {
        Stop-Process -Id $script:PodmanTunnel.Id -Force -ErrorAction SilentlyContinue
        $script:PodmanTunnel = $null
    }
}

# Compose tool
$ComposeEngine = Get-ComposeForEngine $ContainerEngine
if ($null -eq $ComposeEngine) {
    Write-Err "No compose tool found for $ContainerEngine."
    if ($ContainerEngine -eq 'podman') {
        Write-Err '  Podman Desktop with the Compose extension is required.'
        Write-Err '  Run: podman compose version   to verify it works natively.'
    }
    else {
        Write-Err '  Install Docker Compose: https://docs.docker.com/compose/install/'
    }
    exit 1
}
$composeLabel = if ($ComposeEngine.Args.Count -gt 0) {
    "$($ComposeEngine.Cmd) $($ComposeEngine.Args -join ' ')"
} else {
    $ComposeEngine.Cmd
}
Write-Ok "Compose: $composeLabel"

# .NET SDK
$dotnet = Invoke-Tool 'dotnet' @('--version')
if ($dotnet.ExitCode -ne 0) {
    Write-Err '.NET SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/8.0'
    exit 1
}
$dotnetMajor = Get-VersionMajor $dotnet.Output
if ($dotnetMajor -lt 8) {
    Write-Err ".NET SDK $($dotnet.Output) is too old. Version 8+ required."
    exit 1
}
Write-Ok ".NET SDK $($dotnet.Output)"

# Node.js
$node = Invoke-Tool 'node' @('--version')
if ($node.ExitCode -ne 0) {
    Write-Err 'Node.js not found. Install from https://nodejs.org (LTS 22 recommended)'
    exit 1
}
$nodeMajor = Get-VersionMajor ($node.Output -replace 'v', '')
if ($nodeMajor -lt 22) {
    Write-Warn "Node.js $($node.Output) detected. Version 22 LTS is recommended."
} else {
    Write-Ok "Node.js $($node.Output)"
}

# npm
$npm = Invoke-Tool 'npm' @('--version')
if ($npm.ExitCode -ne 0) {
    Write-Err 'npm not found. It should come bundled with Node.js.'
    exit 1
}
Write-Ok "npm $($npm.Output)"

# ─── Step 2: Environment ──────────────────────────────────────────────────────

Write-Step '[2/5] Checking environment...'

$envFile    = Join-Path $Root '.env'
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

# ── Port conflict check ─────────────────────────────────────────────────────────
# docker-compose.yml uses ${DB_PORT:-5432}:5432 — setting $env:DB_PORT before compose
# remaps the host side without touching any system service (no admin rights needed).
# NOTE: $env here = Read-DotEnv hashtable; $env:DB_PORT = PS session env var (separate).
$hostDbPort = [int]$env['DB_PORT']
$listeners = Get-NetTCPConnection -LocalPort $hostDbPort -State Listen -ErrorAction SilentlyContinue
if ($listeners) {
    $ownerPids  = $listeners.OwningProcess | Sort-Object -Unique
    $ownerNames = @($ownerPids | ForEach-Object { (Get-Process -Id $_ -ErrorAction SilentlyContinue).ProcessName })
    $isContainer = $ownerNames | Where-Object { $_ -match 'podman|docker|ssh|vpnkit' }
    if (-not $isContainer) {
        # Port is taken by a non-container process — find the next free port
        $altPort = [int]$env['DB_PORT'] + 2   # +2: skip TEST_DB_PORT which is DB_PORT+1
        while ($altPort -lt 5500) {
            if (-not (Get-NetTCPConnection -LocalPort $altPort -State Listen -ErrorAction SilentlyContinue)) { break }
            $altPort++
        }
        $hostDbPort = $altPort
        $env:DB_PORT = "$hostDbPort"   # compose inherits this and maps $hostDbPort:5432
        Write-Warn "Port $([int]$env['DB_PORT']) in use by '$($ownerNames -join ', ')' — remapping container to host port $hostDbPort."
    }
}

$prevLocation = Get-Location
Set-Location $Root

Write-Info "Running: $composeLabel up -d"
$composeArgs = $ComposeEngine.Args + @('up', '-d')
$dcUp = Invoke-Tool $ComposeEngine.Cmd $composeArgs
if ($dcUp.ExitCode -ne 0) {
    Write-Err "Compose failed. Output: $($dcUp.Output)"
    Write-Info 'Cleaning up containers...'
    Invoke-Tool $ComposeEngine.Cmd ($ComposeEngine.Args + @('down', '--remove-orphans'))
    Stop-PodmanTunnel
    Set-Location $prevLocation
    exit 1
}

# Wait for postgres health
$healthTimeout = 30
$healthElapsed = 0
Write-Host '  Waiting for PostgreSQL' -NoNewline -ForegroundColor DarkGray
$dbHealthy = $false
while ($healthElapsed -lt $healthTimeout) {
    try {
        $health = & $ContainerEngine inspect --format='{{.State.Health.Status}}' ballastlane_db 2>&1
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
    Write-Err "PostgreSQL did not become healthy within 30 seconds."
    Write-Err "  Check: $ContainerEngine logs ballastlane_db"
    Write-Info 'Cleaning up containers...'
    Invoke-Tool $ComposeEngine.Cmd ($ComposeEngine.Args + @('down', '--remove-orphans'))
    Stop-PodmanTunnel
    Set-Location $prevLocation
    exit 1
}

Write-Ok 'PostgreSQL healthy (port 5432)'

# Set password via Unix socket (local trust auth) — forces stored hash to match .env
# Idempotent: works whether the volume is fresh or stale
Write-Info "Setting DB password from .env..."
$setPass = Invoke-Tool $ContainerEngine @(
    'exec', 'ballastlane_db', 'psql',
    '-U', $env['DB_USER'],
    '-c', "ALTER USER $($env['DB_USER']) WITH PASSWORD '$($env['DB_PASSWORD'])'"
)
if ($setPass.ExitCode -ne 0) {
    Write-Err "Failed to set DB password: $($setPass.Output)"
    Write-Err '  Check that the DB container is running: podman logs ballastlane_db'
    Invoke-Tool $ComposeEngine.Cmd ($ComposeEngine.Args + @('down', '--remove-orphans'))
    Stop-PodmanTunnel
    Set-Location $prevLocation
    exit 1
}
Write-Ok "DB password set (user=$($env['DB_USER']))"

Write-Ok 'pgAdmin ready (port 5050)'
Stop-PodmanTunnel

Set-Location $prevLocation

# ─── Step 4: Start services ───────────────────────────────────────────────────

Write-Step '[4/5] Starting services...'

$backendDir   = Join-Path $Root 'backend'
$frontendDir  = Join-Path $Root 'frontend'

    $appSettingsPath = Join-Path $backendDir 'src\BallastLane.API\appsettings.json'
    # Force 127.0.0.1 to avoid IPv6 resolution of 'localhost' hitting a different postgres
    $dbHost = if ($env['DB_HOST'] -eq 'localhost') { '127.0.0.1' } else { $env['DB_HOST'] }
    $dbConnStr = "Host=$dbHost;Port=$hostDbPort;Database=$($env['DB_NAME']);" +
                 "Username=$($env['DB_USER']);Password=$($env['DB_PASSWORD'])"
    $appJson = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    $appJson.ConnectionStrings.Database = $dbConnStr
    $appJson | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8
    Write-Info "appsettings.json updated from .env (DB: $($env['DB_USER'])@$($dbHost):$hostDbPort/$($env['DB_NAME']))"

    # Also patch the bin output copy — dotnet run may use AppContext.BaseDirectory
    $binSettingsPath = Join-Path $backendDir 'src\BallastLane.API\bin\Debug\net8.0\appsettings.json'
    if (Test-Path $binSettingsPath) {
        $binJson = Get-Content $binSettingsPath -Raw | ConvertFrom-Json
        $binJson.ConnectionStrings.Database = $dbConnStr
        $binJson | ConvertTo-Json -Depth 10 | Set-Content $binSettingsPath -Encoding UTF8
        Write-Info 'bin/appsettings.json also updated'
    }

    Write-Info 'Building backend (first run may take a moment)...'
    $buildResult = Invoke-Tool 'dotnet' @(
        'build', (Join-Path $backendDir 'src\BallastLane.API'),
        '--verbosity', 'quiet', '--nologo'
    )
    if ($buildResult.ExitCode -ne 0) {
        Write-Err 'Backend build failed:'
        Write-Err $buildResult.Output
        exit 1
    }
    Write-Ok 'Backend built'

    $connStr = "Host=$dbHost;Port=$hostDbPort;Database=$($env['DB_NAME']);" +
               "Username=$($env['DB_USER']);Password=$($env['DB_PASSWORD'])"
    $backendCmd = "Set-Location '$backendDir'; " +
        "`$env:ASPNETCORE_ENVIRONMENT = 'Development'; " +
        "`$env:ConnectionStrings__Database = '$connStr'; " +
        "Write-Host 'Starting backend...' -ForegroundColor Cyan; " +
        "dotnet run --project src/BallastLane.API --no-build --no-launch-profile --urls http://localhost:5000"
    $backendProc = Start-Process powershell `
        -ArgumentList '-Command', $backendCmd `
        -WindowStyle Normal `
        -PassThru

    Write-Info "Credential check: Host=$($dbHost):$hostDbPort User=$($env['DB_USER']) Password=$('*' * $env['DB_PASSWORD'].Length) chars (IPv4 forced)"
    Write-Ok 'Backend window opened (http://localhost:5000)'

    $frontendCmd = "Set-Location '$frontendDir'; `$env:NG_CLI_ANALYTICS = 'false'; Write-Host 'Starting frontend...' -ForegroundColor Cyan; npm install --silent; ng serve"
    $frontendProc = Start-Process powershell `
        -ArgumentList '-NoExit', '-Command', $frontendCmd `
        -WindowStyle Normal `
        -PassThru

    Write-Ok 'Frontend window opened (http://localhost:4200)'

    # ─── Step 5: Health checks ────────────────────────────────────────────────────

    Write-Step '[5/5] Waiting for services to be ready...'

    # API: live-poll — check HasExited every second so a crash is caught immediately
    Write-Host '  Waiting for API' -NoNewline -ForegroundColor DarkGray
    $apiDeadline = (Get-Date).AddSeconds(60)
    $apiReady = $false
    while ((Get-Date) -lt $apiDeadline) {
        if ($backendProc -and $backendProc.HasExited) {
            Write-Host ' crashed' -ForegroundColor Red
            Write-Err "Backend exited with code $($backendProc.ExitCode)."
            Write-Err '  If this is a PostgreSQL auth error, the DB volume has stale credentials.'
            Write-Err "  Run once to reset: $composeLabel down -v"
            exit 1
        }
        try {
            $r = Invoke-WebRequest -Uri 'http://localhost:5000/api/public/ping' `
                -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
            if ($r.StatusCode -lt 400) { $apiReady = $true; break }
        } catch {}
        Write-Host '.' -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Seconds 1
    }
    if (-not $apiReady) {
        Write-Host ' timed out' -ForegroundColor Red
        Write-Err 'API did not start in 60 seconds.'
        Write-Err "  If auth error: $composeLabel down -v"
        exit 1
    }
    Write-Host ' ready' -ForegroundColor Green

    $frontendReady = Wait-Http 'http://localhost:4200' 'Frontend' 60 3
    if (-not $frontendReady) {
        Write-Err 'Frontend did not start in time. Check the Frontend window for errors.'
        exit 1
    }

    # ─── Summary ──────────────────────────────────────────────────────────────────

    Write-Summary

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
    Write-Host '  Dev environment stopped.' -ForegroundColor DarkGray
    Write-Host '  If any service window remains open, close it manually.' -ForegroundColor DarkGray
}
