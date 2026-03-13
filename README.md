# BallastLane Task Manager

A full-stack task management application built with **ASP.NET Core 8** (Clean Architecture, raw ADO.NET, custom JWT) and **Angular 20** (standalone components, Signals, TailwindCSS 4).

---

## One-Command Start

### Option A — Full Docker stack (everything containerized)

```powershell
# Copy .env from template (first time only)
Copy-Item .env.example .env

# Build images and start all services
docker compose up --build
# or with Podman:
podman compose up --build
```

Open `http://localhost:3000` in your browser. No .NET SDK or Node.js required on the host.
See [Docker (Full Stack)](#docker-full-stack) for details and troubleshooting.

### Option B — Local dev (backend + frontend run natively)

```powershell
# PowerShell — PostgreSQL (requires Docker or Podman)
./start-dev.ps1

# PowerShell — SQLite (no container required)
./start-dev-sqlite.ps1

# Windows CMD or double-click
start-dev.bat

# Git Bash
./start-dev.sh
```

`start-dev.ps1` validates prerequisites, auto-generates `.env` with a random JWT secret if missing, and starts the database + backend + frontend in parallel. If Docker/Podman is not detected it automatically falls back to SQLite — no container required. See [full setup details](#quick-start) below.

---

## Prerequisites

| Tool | Version |
|------|---------|
| Docker Desktop or Podman | Docker 4.x+ / Podman 5.x+ *(optional — SQLite fallback if absent)* |
| .NET SDK | 8.0+ (9 and 10 also accepted) |
| Node.js | 22 LTS |
| Angular CLI | 20.x (`npm i -g @angular/cli`) |

> **Windows note:** If you have a native PostgreSQL installation, `start-dev.bat`
> automatically detects the port conflict and remaps the container to a free port —
> no manual intervention or administrator rights required.

---

## Quick Start (Local Dev — backend + frontend run natively)

> **Prefer Docker?** See [Docker (Full Stack)](#docker-full-stack) above for a single `docker compose up --build`.
>
> The `start-dev.ps1` one-command script handles all of the following automatically.
> The manual steps below are for reference only.

### 1. Start the database (local dev only)

**PostgreSQL (requires Docker or Podman):**
```bash
cp .env.example .env
docker compose up -d postgres
```

PostgreSQL 16 is available on `localhost:5432`.
pgAdmin is available at `http://localhost:5050` (email: `admin@ballastlane.com` / password: `admin`).

**SQLite (no container required):**

Skip this step — the API creates `ballastlane.sqlite` automatically on first launch.

### 2. Start the backend API

```bash
cd backend
dotnet restore
dotnet run --project src/BallastLane.API
```

API is available at `http://localhost:5000`.
Swagger UI: `http://localhost:5000/swagger`

The API automatically runs database migrations and seeds demo data on first launch.

### 3. Start the frontend

```bash
cd frontend
npm install
ng serve
```

App is available at `http://localhost:4200`.

---

## Docker (Full Stack)

Use Docker when you want the entire stack containerized — no .NET SDK or Node.js required on the host. All four services (frontend, API, database, pgAdmin) start with a single command.

### How it works

```
Your browser
    │
    │  http://localhost:3000     http://localhost:5000
    ▼                                     ▼
[nginx :80]  ──/api/*──▶  [backend :8080]  ──▶  [postgres :5432]
  host port 3000            ASP.NET Core            PostgreSQL
  serves Angular SPA        (internal port)
```

The frontend nginx serves the Angular app **and** proxies all `/api/*` requests to the backend inside Docker's private network. From the browser's perspective everything is on the same origin — no CORS configuration needed.

### Prerequisites

- **Podman Desktop** (installed) with Podman machine running, **or** Docker Desktop
- A `.env` file at the project root — copy from `.env.example` (see below)

### Windows + Podman Desktop — Step by Step

**Step 1 — Open PowerShell and go to the project root**

```powershell
cd path/to/BallastLaneApp
```

**Step 2 — Create your `.env` file (first time only)**

```powershell
Copy-Item .env.example .env
# Optional: change JWT_SECRET before any production use
notepad .env
```

**Step 3 — Build and start all containers**

```powershell
podman compose up --build
```

The first run downloads base images and compiles the app — allow a few minutes.
Subsequent runs (no code changes): `podman compose up`

**Step 4 — Verify all four containers are running**

```powershell
podman ps
# Expected: ballastlane_frontend, ballastlane_api, ballastlane_db, ballastlane_pgadmin
```

**Step 5 — Open the app in your browser**

```powershell
Start-Process "http://localhost:3000"
# Login: demo@ballastlane.com  /  Demo@1234
```

**Step 6 — Explore the API with Swagger (optional)**

```powershell
Start-Process "http://localhost:5000/swagger"
```

**Step 7 — Inspect the database with pgAdmin (optional)**

```powershell
Start-Process "http://localhost:5050"
# Email: admin@ballastlane.com   Password: admin
# Add server: Host=postgres  Port=5432  DB=ballastlane  User=ballastlane
```

**Step 8 — Stop everything**

```powershell
podman compose down          # stop, keep database data
podman compose down -v       # stop + wipe all volumes (clean slate)
```

### Service URLs

| Service | URL | Notes |
|---------|-----|-------|
| Frontend | `http://localhost:3000` | Angular SPA (nginx) |
| API (via proxy) | `http://localhost:3000/api/...` | Same origin as frontend |
| API (direct) | `http://localhost:5000` | Swagger, health checks |
| Swagger | `http://localhost:5000/swagger` | |
| pgAdmin | `http://localhost:5050` | `admin@ballastlane.com` / `admin` |
| PostgreSQL | `localhost:5432` | For DB clients (DBeaver, etc.) |

### Port conflicts

If any default port is already in use on your machine, edit `.env` before running:

```env
APP_PORT=3001    # if port 3000 is taken
API_PORT=5001    # if port 5000 is taken by another .NET app
PGADMIN_PORT=5051
DB_PORT=5433
```

Then rebuild: `podman compose up --build` and open `http://localhost:3001`.

### `docker compose` vs `podman compose`

The commands are interchangeable — use whichever engine you have installed.
`podman compose` is provided by the **Compose extension** in Podman Desktop.

### Troubleshooting

**`docker-credential-desktop: executable file not found`**

Affects both `podman compose` and `docker compose`. Caused by a leftover `credsStore: "desktop"` entry in `~/.docker/config.json` from a previous Docker Desktop installation.

Fix (PowerShell — one command, permanent):

```powershell
Set-Content "$env:USERPROFILE\.docker\config.json" '{}'
```

Then re-run:

```powershell
podman compose up --build   # Podman Desktop
# or
docker compose up --build   # Docker Desktop
```

---

## Demo Credentials

| Field | Value |
|-------|-------|
| Email | `demo@ballastlane.com` |
| Password | `Demo@1234` |

Three demo tasks (Todo / InProgress / Done) are pre-loaded for the demo user.

---

## Running Tests

### Backend

Unit tests and integration tests are in **separate projects** — unit tests never require a database.

```bash
cd backend

# Unit tests only — no database required
dotnet test tests/BallastLane.Tests/

# Single test class
dotnet test tests/BallastLane.Tests/ --filter "FullyQualifiedName~TaskServiceTests"

# With coverage report
dotnet test tests/BallastLane.Tests/ --collect:"XPlat Code Coverage"
```

Integration tests require a running test database:

```bash
docker-compose -f docker-compose.test.yml up -d
dotnet test tests/BallastLane.IntegrationTests/
```

Connection string is read from the `TEST_DB_CONNECTION` environment variable, or falls back to
`Host=localhost;Port=5433;Database=ballastlane_test;Username=ballastlane_test;Password=ballastlane_test`.

### Frontend

```bash
cd frontend

# Run all tests (vitest)
npx vitest run

# With coverage
npx vitest run --coverage

# Watch mode
npx vitest
```

---

## Architecture

```
BallastLaneApp/
├── backend/
│   ├── src/
│   │   ├── BallastLane.Domain/         # Entities, interfaces, Result<T> — no external deps
│   │   ├── BallastLane.Application/    # Services, DTOs, validators — references Domain only
│   │   ├── BallastLane.Infrastructure/ # ADO.NET repositories (PostgreSQL + SQLite), JWT, BCrypt
│   │   └── BallastLane.API/            # ASP.NET Core controllers, middleware, Program.cs
│   └── tests/
│       ├── BallastLane.Tests/          # Unit tests (xUnit + Moq, no DB)
│       │   ├── Domain/
│       │   ├── Application/
│       │   └── API/
│       └── BallastLane.IntegrationTests/  # Integration tests (real PostgreSQL)
│           └── Infrastructure/
└── frontend/
    └── src/app/
        ├── core/                       # Auth guard, interceptors (auth + CSRF), shell
        ├── shared/                     # Reusable UI components, validators
        └── features/
            ├── auth/                   # Login + Register
            └── tasks/                  # Task list, card, form, pagination
```


---

## API Reference

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Register a new user |
| POST | `/api/auth/login` | No | Login — sets `HttpOnly` cookie |
| POST | `/api/auth/logout` | Cookie | Logout — clears cookie |
| GET | `/api/auth/me` | Cookie | Current user profile |
| GET | `/healthz/live` | No | Liveness probe |
| GET | `/healthz/ready` | No | Readiness probe (DB check) |
| GET | `/api/tasks` | Cookie | List my tasks (paginated) |
| GET | `/api/tasks/{id}` | Cookie | Get task by ID |
| POST | `/api/tasks` | Cookie | Create task |
| PUT | `/api/tasks/{id}` | Cookie | Update task |
| DELETE | `/api/tasks/{id}` | Cookie | Delete task |
