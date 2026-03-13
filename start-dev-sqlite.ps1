#Requires -Version 5.1
<#
.SYNOPSIS
    BallastLane — SQLite-only developer setup.
    No Docker or Podman required. Uses a local SQLite file as the database.
    All migrations and seeding are handled automatically on first run.
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
# ═══════════════════════════════════════════════════════════════════════════════

$Config = [ordered]@{
    # ── Service ports ──────────────────────────────────────────────────────────
    ApiPort          = 5000
    FrontendPort     = 4200

    # ── Backend project layout (relative to backend/) ─────────────────────────
    BackendProject   = 'src/BallastLane.API'

    # ── Version minimums ──────────────────────────────────────────────────────
    DotnetMin        = 8
    NodeMin          = 20    # hard fail below this
    NodeIdeal        = 22    # warn below this

    # ── Timeouts (seconds) ────────────────────────────────────────────────────
    ApiStartTimeout  = 60
    FrontendTimeout  = 60
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
    Write-Host '  BallastLane Dev Environment (SQLite)' -ForegroundColor Blue
    Write-Host "$line`n" -ForegroundColor Blue
}

function Write-Summary {
    $line = '=' * 50
    Write-Host "`n$line" -ForegroundColor Green
    Write-Host '  All services are running!' -ForegroundColor Green
    Write-Host ''
    Write-Host "  Frontend  -->  http://localhost:$($Config.FrontendPort)" -ForegroundColor White
    Write-Host "  API       -->  http://localhost:$($Config.ApiPort)" -ForegroundColor White
    Write-Host "  Swagger   -->  http://localhost:$($Config.ApiPort)/swagger" -ForegroundColor White
    Write-Host "  DB        -->  SQLite (local file)" -ForegroundColor White
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

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
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

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

Write-Banner

# ─── Step 1: Prerequisites ────────────────────────────────────────────────────

Write-Step '[1/4] Validating prerequisites...'

Write-Ok 'Database: SQLite (no container engine required)'

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

Write-Step '[2/4] Checking environment...'

$envFile = Join-Path $Root '.env'
$placeholder = 'change-me-to-a-256-bit-random-secret-before-production'

if (-not (Test-Path $envFile)) {
    $jwtSecret = New-RandomSecret
    $now = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $content = @"
# Generated by start-dev-sqlite on $now
JWT_SECRET=$jwtSecret
JWT_EXPIRY_MINUTES=60
"@
    Set-Content -Path $envFile -Value $content -Encoding UTF8
    Write-Ok '.env generated with cryptographically random JWT_SECRET'
}
else {
    Write-Ok '.env file found'
}

# Read .env into hashtable
$envVars = @{}
Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    if ($line -and -not $line.StartsWith('#') -and $line -match '^([^=]+)=(.*)$') {
        $envVars[$Matches[1].Trim()] = $Matches[2].Trim()
    }
}

if (-not $envVars.ContainsKey('JWT_SECRET')) {
    Write-Err "Missing required key 'JWT_SECRET' in .env"
    exit 1
}
Write-Ok 'All required variables present'

if ($envVars['JWT_SECRET'] -eq $placeholder) {
    Write-Warn 'JWT_SECRET is using the example placeholder — safe for local dev only'
}

# ─── Step 3: SQLite setup ─────────────────────────────────────────────────────

Write-Step '[3/4] Setting up SQLite database...'

$dbPath  = Join-Path $Root 'ballastlane.sqlite'
$connStr = "Data Source=$dbPath"
$env:ConnectionStrings__Database = $connStr
$env:ConnectionStrings__Provider  = 'SQLite'
Write-Ok "SQLite database: $dbPath"

# ─── Step 4: Start services ───────────────────────────────────────────────────

Write-Step '[4/4] Starting services...'

$backendDir  = Join-Path $Root 'backend'
$frontendDir = Join-Path $Root 'frontend'

$backendProc  = $null
$frontendProc = $null

try {
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

    $backendCmd = "Set-Location '$backendDir'; " +
        "`$env:ASPNETCORE_ENVIRONMENT = 'Development'; " +
        "`$env:ConnectionStrings__Database = '$connStr'; " +
        "`$env:ConnectionStrings__Provider = 'SQLite'; " +
        "`$env:JWT_SECRET = '$($envVars['JWT_SECRET'])'; " +
        "Write-Host 'Starting backend...' -ForegroundColor Cyan; " +
        "dotnet run --project $($Config.BackendProject) --no-build --no-launch-profile --urls http://localhost:$($Config.ApiPort)"
    $backendProc = Start-Process powershell `
        -ArgumentList '-Command', $backendCmd `
        -WindowStyle Normal `
        -PassThru
    Write-Ok "Backend window opened (http://localhost:$($Config.ApiPort))"

    $frontendCmd = "Set-Location '$frontendDir'; `$env:NG_CLI_ANALYTICS = 'false'; Write-Host 'Starting frontend...' -ForegroundColor Cyan; npm install --silent; ng serve"
    $frontendProc = Start-Process powershell `
        -ArgumentList '-NoExit', '-Command', $frontendCmd `
        -WindowStyle Normal `
        -PassThru
    Write-Ok "Frontend window opened (http://localhost:$($Config.FrontendPort))"

    # ─── Health checks ────────────────────────────────────────────────────────

    Write-Step 'Waiting for services to be ready...'

    $apiUrl = "http://localhost:$($Config.ApiPort)/healthz/live"
    if (-not (Wait-Service $apiUrl 'API' $Config.ApiStartTimeout $backendProc)) {
        Write-Err "API did not start in $($Config.ApiStartTimeout)s. Check the backend window for errors."
        exit 1
    }

    $frontendUrl = "http://localhost:$($Config.FrontendPort)"
    if (-not (Wait-Service $frontendUrl 'Frontend' $Config.FrontendTimeout)) {
        Write-Err 'Frontend did not start. Check the Frontend window for errors.'
        exit 1
    }

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
    Write-Host '  Dev environment stopped.' -ForegroundColor DarkGray
    Write-Host '  If any service window remains open, close it manually.' -ForegroundColor DarkGray
}
