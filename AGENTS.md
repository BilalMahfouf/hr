# AGENTS.md — PublicApi

## Project structure

```
AGENTS.md
backend/
├── Veterinary.slnx              # .NET solution file
├── docker-compose.yml           # Postgres + pgAdmin + API
├── src/Api/PublicApi/           # ASP.NET Core 10 minimal API
│   ├── Program.cs               # Entry point, DI composition
│   ├── Domain/                  # Pure domain, no infra deps
│   ├── Features/                # Vertical slices (one folder per feature)
│   ├── Infrastructure/          # EF Core, JWT, SignalR, Quartz (subscription jobs)
│   ├── Common/                  # App-level abstractions + extensions
│   └── Migrations/              # EF Core migrations (ApplicationDbContext)
├── src/Modules/
│   ├── Shared/                  # CQRS interfaces, Result pattern, pagination, domain base types
│   │   ├── Infrastructure/      # Outbox (entity, interceptor, processor job), SharedDbContext,
│   │   │                        # domain event dispatcher/publisher, exception handlers,
│   │   │                        # email services, CurrentUserService, AddSharedInfrastructure DI
│   │   └── Migrations/          # EF Core migrations (SharedDbContext — owns shared schema)
│   ├── Identity/                # User management, auth, JWT abstractions + IdentityDbContext
│   ├── Employees/               # Employee module (multiple class libs)
│   │   ├── Employees/           # Main project (module DI, EmployeeApi)
│   │   └── Employees.Contracts/ # Employee contracts (IEmployeeApi, EmployeeErrors)
│   └── Attendence/              # Attendance records, punches, machines + AttendanceDbContext
├── Tests/
│   ├── Application.Tests/       # Unit tests (xUnit + Moq)
│   ├── Application.IntegrationTests/  # Integration tests (Testcontainers.PostgreSql)
│   └── Domain.Tests/            # Domain unit tests
└── .github/
    └── copilot-instructions.md  # Response DTOs must be flat (no nested records)
```

**Module folder convention:** every module lives in its own folder under `src/Modules/`. A module can contain **multiple class libraries** — one project per subfolder inside the module folder (e.g. `Employees/` holds the `Employees/` and `Employees.Contracts/` class libraries). Single-project modules keep their `.csproj` directly in the module folder (e.g. `Attendence/Attendence.csproj`).

## Key commands (run from `backend`)

| Action | Command |
|---|---|
| Build | `dotnet build src/Api/PublicApi/PublicApi.csproj` |
| Run API | `dotnet run --project src/Api/PublicApi/PublicApi.csproj` |
| Unit tests | `dotnet test Tests/Application.Tests` |
| Integration tests | `dotnet test Tests/Application.IntegrationTests` |
| All tests | `dotnet test Veterinary.slnx` |
| New migration (app) | `dotnet ef migrations add <Name> --project src/Api/PublicApi --startup-project src/Api/PublicApi --context ApplicationDbContext` |
| New migration (shared) | `dotnet ef migrations add <Name> --project src/Modules/Shared --startup-project src/Api/PublicApi --context SharedDbContext` |
| New migration (identity) | `dotnet ef migrations add <Name> --project src/Modules/Identity --startup-project src/Api/PublicApi --context IdentityDbContext` |
| New migration (employees) | `dotnet ef migrations add <Name> --project src/Modules/Employees/Employees --startup-project src/Api/PublicApi --context EmployeeDbContext` |
| Apply migration | `dotnet ef database update --project src/Api/PublicApi --startup-project src/Api/PublicApi --context <DbContext>` (migrations also auto-applied at startup via `app.ApplyMigrations()`) |
| Scalar API UI | `https://localhost:<port>/scalar` (dev only) |
| Docker Compose | `docker compose up --build` (starts API + Postgres 16 + pgAdmin) |

## Architecture essentials

- **.NET 10** with **Carter** modules for routing (`app.MapCarter()` + `IEndpoint`). Routes are grouped under `/api/v1`.
- **CQRS** via `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>` — handlers auto-registered via Scrutor.
- **Result pattern**: handlers return `Result<T>` (never throw for business errors). Endpoints call `.Problem()` on failure for RFC 7807 responses.
- **Vertical slices**: each operation is one file containing Command/Query record + Validator + Handler + Endpoint class, all `public static` in a `Features/{Feature}/{Operation}.cs` file.
- **Outbox pattern** (lives in `Modules.Shared`): `InsertOutboxMessagesInterceptors` serializes domain events via Newtonsoft.Json (TypeNameHandling.All) into `shared.outbox_messages`, committed in the same EF transaction; `ProcessOutboxMessagesJob` (Quartz, every 10s) publishes them via `IDomainEventPublisher`. `SharedDbContext` owns the table's migrations (history table in `shared` schema); every module DbContext maps the table with `ConfigureOutboxMessage(excludeFromMigrations: true)` so the interceptor works in its transaction. Never author outbox schema changes outside `Modules.Shared` migrations.
- **Shared DI**: `AddSharedModule(assemblies)` (validators, CQRS handlers, `IDomainEventHandler<>` scan across ALL module assemblies) + `AddSharedInfrastructure(connectionString)` (interceptors, SharedDbContext + factory, exception handlers, email, current user, outbox Quartz job). Host keeps `AddQuartzHostedService` once.
- **Multi-tenancy**: `TenantId == UserId` (doctor IS the tenant). `TenantInterceptor` stamps it automatically. Queries must filter with `.ForTenant()`.
- **Validation**: FluentValidation validators co-located with commands. Validated via `_validator.ValidateAndThrow()` inside handler. Registered as Singleton.
- **Auth**: JWT (HMAC-SHA256) + refresh tokens stored as `UserSession` entities (7-day expiry, HTTP-only cookie). SignalR uses `?access_token=` query param for WebSocket auth.
- **Payments**: Chargily (Algerian payment gateway) + webhooks in `Features/Subscriptions/Webhooks/`.
- **PDF prescriptions**: Handlebars.Net templates → PuppeteerSharp (requires Chromium). Dockerfile installs Chrome + sets `PUPPETEER_EXECUTABLE_PATH`.
- **Email**: Resend API (primary) + Gmail SMTP via MailKit (fallback/legacy).
- **Config**: `DotNetEnv` loads `.env` at startup. Sensitive values come from env vars, never committed.

## Testing quirks

- Integration tests use **Testcontainers.PostgreSql** (spins a real Postgres 16-alpine container per test session). Requires Docker to be running. Each test class gets its own database and runs migrations fresh.
- `IntegrationTestBase` provides `CurrentTenant`, `HttpContextAccessor`, and stubbed email/payment services. Override `ConfigureServices()` to add mocks.
- Unit tests use **Moq**.

## Response DTO convention (from `.github/copilot-instructions.md`)

All response DTOs must be **flat** — no nested record objects. Use `(AnimalId, AnimalName, ...)` instead of `(AnimalInfo Animal, ...)`. Applies to `GetById` and `GetAll` endpoints.
