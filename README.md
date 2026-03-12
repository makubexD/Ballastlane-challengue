# BallastLane Task Manager

A full-stack task management application built with **ASP.NET Core 8** (Clean Architecture, raw ADO.NET, custom JWT) and **Angular 20** (standalone components, Signals, TailwindCSS 4).

---

## One-Command Start

```powershell
# PowerShell
./start-dev.ps1

# Windows CMD or double-click
start-dev.bat

# Git Bash
./start-dev.sh
```

Validates all prerequisites, auto-generates `.env` with a random JWT secret if missing, starts PostgreSQL via Docker, and launches backend + frontend in parallel terminal windows. See [full setup details](#quick-start) below.

---

## Prerequisites

| Tool | Version |
|------|---------|
| Docker Desktop or Podman | Docker 4.x+ / Podman 5.x+ |
| .NET SDK | 8.0+ (9 and 10 also accepted) |
| Node.js | 22 LTS |
| Angular CLI | 20.x (`npm i -g @angular/cli`) |

> **Windows note:** If you have a native PostgreSQL installation, `start-dev.bat`
> automatically detects the port conflict and remaps the container to a free port —
> no manual intervention or administrator rights required.

---

## Quick Start

> The one-command start above handles all of the following automatically.
> The manual steps below are for reference only.

### 1. Start the database

```bash
cp .env.example .env
docker-compose up -d
```

PostgreSQL 16 is available on `localhost:5432`.
pgAdmin is available at `http://localhost:5050` (email: `admin@ballastlane.com` / password: `admin`).

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
│   │   ├── BallastLane.Infrastructure/ # Npgsql repositories, JWT, BCrypt — implements Domain interfaces
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
        ├── core/                       # Auth guard, JWT interceptor, layout
        ├── shared/                     # Reusable UI components, validators
        └── features/
            ├── auth/                   # Login + Register
            └── tasks/                  # Task list, card, form
```

**Key constraints:**
- No Entity Framework, no Dapper, no MediatR — raw Npgsql ADO.NET only
- No ASP.NET Core Identity — custom JWT (`System.IdentityModel.Tokens.Jwt`)
- All code follows TDD (Red-Green-Refactor) with xUnit + Moq (backend) and vitest 4 + TestBed (frontend)
- **Test coverage:** 153 backend unit tests + 10 integration tests + 30 frontend tests — all green

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
