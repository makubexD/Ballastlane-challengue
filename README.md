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

```bash
cd backend

# Unit + integration tests
dotnet test

# With coverage report
dotnet test --collect:"XPlat Code Coverage"

# Single test class
dotnet test --filter "FullyQualifiedName~TaskServiceTests"
```

Integration tests require the test database:

```bash
docker-compose -f docker-compose.test.yml up -d
```

Connection string is read from environment (`TEST_DB_HOST`, `TEST_DB_PORT`, etc.) — see `.env.example`.

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
│       └── BallastLane.Tests/
│           ├── Domain/                 # Unit tests (xUnit + Moq, no DB)
│           ├── Infrastructure/         # Integration tests (real PostgreSQL)
│           └── API/                    # Controller tests + WebApplicationFactory
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
- **Test coverage:** 51 backend unit tests + 29 frontend tests — all green

---

## API Reference

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Register a new user |
| POST | `/api/auth/login` | No | Login, returns JWT |
| GET | `/api/auth/me` | JWT | Current user profile |
| GET | `/api/public/ping` | No | Health check |
| GET | `/api/tasks` | JWT | List my tasks |
| GET | `/api/tasks/{id}` | JWT | Get task by ID |
| POST | `/api/tasks` | JWT | Create task |
| PUT | `/api/tasks/{id}` | JWT | Update task |
| DELETE | `/api/tasks/{id}` | JWT | Delete task |
