# BallastLane API — Backend

ASP.NET Core 8 Web API built with Clean Architecture, raw Npgsql ADO.NET, and custom JWT authentication. No Entity Framework, no Dapper, no MediatR.

---

## Tech Stack

| Component | Technology |
|---|---|
| Runtime | .NET 8 / C# 12 |
| Architecture | Clean Architecture (4 layers) |
| Data access | Raw ADO.NET — Npgsql 8.x (no ORM) |
| Authentication | Custom JWT — `System.IdentityModel.Tokens.Jwt` (no ASP.NET Core Identity) |
| Password hashing | BCrypt.Net-Next (work factor 12) |
| Testing | xUnit + Moq + Coverlet |

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Docker Desktop | 4.x+ (for PostgreSQL) |

---

## Setup & Run

```bash
# 1. From repo root — start PostgreSQL 16
cp .env.example .env
docker-compose up -d

# 2. From backend/
dotnet restore
dotnet run --project src/BallastLane.API
```

- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

The API automatically runs database migrations and seeds demo data on first launch.

---

## Project Structure

```
backend/
├── BallastLane.slnx
├── src/
│   ├── BallastLane.Domain/
│   │   ├── Entities/           # TaskItem, User (immutable records with factory methods)
│   │   ├── ValueObjects/       # TaskItemStatus enum (Todo, InProgress, Done)
│   │   ├── Interfaces/         # ITaskRepository, IUserRepository, IJwtProvider,
│   │   │                       # IPasswordHasher, IDateTimeProvider
│   │   └── Common/             # Result<T>, Result, DomainException hierarchy
│   ├── BallastLane.Application/
│   │   ├── Services/           # ITaskService + TaskService, IAuthService + AuthService
│   │   ├── DTOs/               # CreateTaskRequest, UpdateTaskRequest, LoginRequest,
│   │   │                       # RegisterRequest, AuthResponse, UserProfileResponse (records)
│   │   └── Validators/         # TaskValidator, AuthValidator (no framework dependency)
│   ├── BallastLane.Infrastructure/
│   │   ├── Auth/               # JwtProvider, BcryptPasswordHasher
│   │   ├── Common/             # NpgsqlConnectionFactory (IDbConnectionFactory),
│   │   │                       # SystemDateTimeProvider
│   │   ├── Persistence/        # SqlTaskRepository, SqlUserRepository, DatabaseMigrator,
│   │   │                       # DatabaseSeeder
│   │   └── DependencyInjection/# AddInfrastructure(IServiceCollection, IConfiguration)
│   └── BallastLane.API/
│       ├── Controllers/        # TasksController, AuthController, PublicController (thin)
│       ├── Extensions/         # ResultExtensions — Result<T> → IActionResult mapping
│       ├── Middleware/         # ExceptionHandlerMiddleware
│       ├── Services/           # ICurrentUserService + CurrentUserService (JWT claim extraction)
│       └── Program.cs          # Composition root — DI, JWT auth, CORS, Swagger, startup
└── tests/
    └── BallastLane.Tests/
        ├── TestData/           # TestConstants.cs, TestDataBuilder.cs
        ├── Domain/             # Unit tests — xUnit + Moq, no database
        ├── Infrastructure/     # Integration tests — real PostgreSQL (port 5433)
        └── API/                # Controller unit tests
```

---

## Key Design Decisions

**`Result<T>` discriminated union** — All expected failures (validation errors, not-found, access-denied) return `Result<T>.Fail(message)`. Exceptions are reserved for infrastructure panics only. Controllers map results to HTTP status codes via `ResultExtensions.ToActionResult()`.

**SQL in `private const` fields** — Every SQL statement in every repository class lives in a `private const string` at the top of the class. No inline SQL literals anywhere.

**`ICurrentUserService`** — Wraps `IHttpContextAccessor` for JWT claim extraction. Fully mockable in controller unit tests without spinning up the entire auth middleware stack.

**Idempotent startup** — `DatabaseMigrator` uses `CREATE TABLE IF NOT EXISTS`. `DatabaseSeeder` checks `COUNT(*) FROM users` before inserting. Safe to restart the API repeatedly.

**Generic auth errors** — Login always returns `"Invalid email or password."` regardless of whether the email doesn't exist or the password is wrong. Prevents user enumeration attacks.

**Dependency flow** — Domain → ← Application → ← Infrastructure → ← API. Domain has zero external NuGet dependencies.

---

## Configuration

All configuration is read from environment variables (loaded from `.env` by Docker Compose, or set directly):

| Variable | Description | Default |
|---|---|---|
| `DB_HOST` | PostgreSQL host | `localhost` |
| `DB_PORT` | PostgreSQL port | `5432` |
| `DB_NAME` | Database name | `ballastlane` |
| `DB_USER` | Database user | `ballastlane` |
| `DB_PASSWORD` | Database password | `ballastlane_dev` |
| `JWT_SECRET` | 256-bit signing key | (required) |
| `JWT_EXPIRY_MINUTES` | Token lifetime | `60` |

See `.env.example` at the repo root.

---

## Running Tests

```bash
# Unit tests only (no database required) — 153 tests
dotnet test tests/BallastLane.Tests/

# Run a single test class
dotnet test --filter "FullyQualifiedName~TaskServiceTests"

# Integration tests (requires test database on port 5433)
docker-compose -f ../docker-compose.test.yml up -d
dotnet test

# With Coverlet coverage report
dotnet test --collect:"XPlat Code Coverage"
```

Integration tests use `[Trait("Category", "Integration")]` and read the connection string from `TEST_DB_CONNECTION` or the individual `TEST_DB_*` environment variables (see `.env.example`).

---

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | No | Register a new user |
| POST | `/api/auth/login` | No | Login — sets `HttpOnly` `access_token` cookie |
| POST | `/api/auth/logout` | Cookie | Logout — clears `access_token` cookie |
| GET | `/api/auth/me` | Cookie | Current user profile |
| GET | `/healthz/live` | No | Liveness probe |
| GET | `/healthz/ready` | No | Readiness probe (DB connectivity check) |
| GET | `/api/tasks` | Cookie | List my tasks — paginated (`?page=1&pageSize=20`) |
| GET | `/api/tasks/{id}` | Cookie | Get task by ID |
| POST | `/api/tasks` | Cookie | Create task |
| PUT | `/api/tasks/{id}` | Cookie | Update task |
| DELETE | `/api/tasks/{id}` | Cookie | Soft-delete task |
