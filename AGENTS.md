# AGENTS.md — HREnap (Full Stack)

## Project Structure

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

frontend/
├── package.json                 # pnpm workspace, React 19 + Vite 7
├── vite.config.ts               # Vite config: React plugin, Tailwind v4 plugin, @ path alias
├── tsconfig.json                # TypeScript project references
├── eslint.config.ts             # Flat ESLint config (TS + React)
├── components.json              # shadcn/ui CLI configuration
├── vercel.json                  # Vercel SPA rewrite rules
├── pnpm-workspace.yaml          # pnpm workspace root
├── .env / .env.example          # Vite env vars (VITE_API_URL)
├── src/
│   ├── main.tsx                 # Entry point — mounts React, boots i18n
│   ├── App.tsx                  # Router definition + QueryClient provider
│   ├── index.css                # Global CSS / Tailwind base imports
│   ├── common/
│   │   ├── layouts/             # Layout, MainLayout, SideBar, TopNavigation
│   │   └── results/
│   ├── components/
│   │   ├── ui/                  # shadcn/ui wrappers (Radix + Tailwind)
│   │   └── tables/              # Generic server-side DataTable system (TanStack Table v8)
│   ├── features/                # Vertical-slice feature modules
│   │   ├── auth/                # Login, Register, Forgot/Reset Password
│   │   ├── dashboard/
│   │   ├── notifications/
│   │   ├── settings/
│   │   ├── subscriptions/
│   │   └── users/
│   ├── hooks/
│   │   └── use-toast.ts         # sonner wrapper
│   └── lib/
│       ├── api/
│       │   ├── api.ts           # Axios instance (baseURL, cookies, timeout)
│       │   ├── tokenManager.ts  # In-memory token store + axios interceptors
│       │   ├── auth.ts          # Auth API calls
│       │   ├── error-types.ts   # RFC 7807 ProblemDetails types + ErrorCodes
│       │   └── axios.d.ts       # AxiosRequestConfig augmentation (skipAuthRefresh)
│       ├── i18n/                # i18next (EN/FR/AR), keyContainer for type-safe keys
│       ├── signalr/             # SignalRProvider, useSignalR hook
│       └── utils.ts             # cn() class merger, getTableRequestParams()
└── public/                      # Static assets
```

**Module folder convention (backend):** Every module lives in its own folder under `src/Modules/`. A module can contain **multiple class libraries** — one project per subfolder (e.g., `Employees/` holds `Employees/` and `Employees.Contracts/`). Single-project modules keep their `.csproj` directly in the module folder (e.g., `Attendence/Attendence.csproj`).

---

## Key Commands

### Backend (run from `backend/`)

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
| Apply migration | `dotnet ef database update --project src/Api/PublicApi --startup-project src/Api/PublicApi --context <DbContext>` (also auto-applied at startup via `app.ApplyMigrations()`) |
| Scalar API UI | `https://localhost:<port>/scalar` (dev only) |
| Docker Compose | `docker compose up --build` (starts API + Postgres 16 + pgAdmin) |

### Frontend (run from `frontend/`)

| Action | Command |
|---|---|
| Install deps | `pnpm install` |
| Dev server | `pnpm dev` (runs on `http://localhost:5173`) |
| Build | `pnpm build` (runs `tsc -b && vite build`) |
| Preview build | `pnpm preview` |
| Lint | `pnpm lint` |

---

## Architecture Essentials (Backend)

- **.NET 10** with **Carter** modules for routing (`app.MapCarter()` + `IEndpoint`). Routes grouped under `/api/v1`.
- **CQRS** via `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>` — handlers auto-registered via Scrutor.
- **Result pattern**: handlers return `Result<T>` (never throw for business errors). Endpoints call `.Problem()` on failure for RFC 7807 responses.
- **Vertical slices**: each operation is one file containing Command/Query record + Validator + Handler + Endpoint class, all `public static` in `Features/{Feature}/{Operation}.cs`.
- **Outbox pattern** (lives in `Modules.Shared`): `InsertOutboxMessagesInterceptors` serializes domain events via Newtonsoft.Json (`TypeNameHandling.All`) into `shared.outbox_messages`, committed in the same EF transaction; `ProcessOutboxMessagesJob` (Quartz, every 10s) publishes them via `IDomainEventPublisher`. `SharedDbContext` owns the table's migrations (history table in `shared` schema); every module DbContext maps the table with `ConfigureOutboxMessage(excludeFromMigrations: true)`. Never author outbox schema changes outside `Modules.Shared` migrations.
- **Shared DI**: `AddSharedModule(assemblies)` (validators, CQRS handlers, `IDomainEventHandler<>` scan across ALL module assemblies) + `AddSharedInfrastructure(connectionString)` (interceptors, SharedDbContext + factory, exception handlers, email, current user, outbox Quartz job). Host keeps `AddQuartzHostedService` once.
- **Multi-tenancy**: `TenantId == UserId` (doctor IS the tenant). `TenantInterceptor` stamps it automatically. Queries must filter with `.ForTenant()`.
- **Validation**: FluentValidation validators co-located with commands. Validated via `_validator.ValidateAndThrow()` inside handler. Registered as Singleton.
- **Auth**: JWT (HMAC-SHA256) + refresh tokens stored as `UserSession` entities (7-day expiry, HTTP-only cookie). SignalR uses `?access_token=` query param for WebSocket auth.
- **Payments**: Chargily (Algerian payment gateway) + webhooks in `Features/Subscriptions/Webhooks/`.
- **PDF prescriptions**: Handlebars.Net templates → PuppeteerSharp (requires Chromium). Dockerfile installs Chrome + sets `PUPPETEER_EXECUTABLE_PATH`.
- **Email**: Resend API (primary) + Gmail SMTP via MailKit (fallback/legacy).
- **Config**: `DotNetEnv` loads `.env` at startup. Sensitive values from env vars, never committed.

---

## Architecture Essentials (Frontend)

- **React 19 + TypeScript + Vite 7** with path alias `@` → `src/`.
- **Vertical slice (feature-based) architecture**: Each domain lives under `src/features/` with co-located API, hooks, pages, and types.
- **TanStack Query v5** for all server state (caching, invalidation, mutations). No global client-state store.
- **React Hook Form + Zod** for all forms. Schemas extracted to `schemas.ts` for complex pages.
- **Axios** with interceptors for dual-token auth: access token in memory, refresh token in httpOnly cookie. Automatic token refresh with subscriber queue for concurrent 401s.
- **shadcn/ui** (Radix UI primitives + Tailwind) in `src/components/ui/` — zero business logic.
- **Generic server-side DataTable** (`src/components/tables/`) built on TanStack Table v8. All pagination/sorting/search on server via `TableRequest` / `PagedList<T>`.
- **SignalR** for real-time notifications: `SignalRProvider` in `MainLayout`, connection to `/hubs/notification`, auth via `access_token` query param, event `ReceiveNotification` triggers query invalidation + toast.
- **i18next** with 3 languages (EN/FR/AR). All keys typed via `keyContainer.ts`. RTL support for Arabic via Tailwind logical properties.
- **Tailwind CSS v4** via `@tailwindcss/vite` plugin. No config file needed. `cn()` utility merges classes with `tailwind-merge`.
- **Vercel deployment**: `vercel.json` has SPA rewrite rule. Set `VITE_API_URL` in Vercel env.

---

## Testing Quirks

### Backend
- Integration tests use **Testcontainers.PostgreSql** (spins real Postgres 16-alpine per test session). Requires Docker. Each test class gets its own database and runs migrations fresh.
- `IntegrationTestBase` provides `CurrentTenant`, `HttpContextAccessor`, stubbed email/payment services. Override `ConfigureServices()` to add mocks.
- Unit tests use **Moq**.

### Frontend
- Unit/Component tests: Not yet configured (Playwright available for E2E).
- E2E tests: `@playwright/test` installed. Run with `pnpm playwright test` (if configured).

---

## Response DTO Convention (from `.github/copilot-instructions.md`)

All response DTOs must be **flat** — no nested record objects. Use `(AnimalId, AnimalName, ...)` instead of `(AnimalInfo Animal, ...)`. Applies to `GetById` and `GetAll` endpoints.

---

## Important Constraints

- **Backend**: `.env` in `backend/src/Api/PublicApi/` must exist with all required variables (JWT keys, connection string, email creds). Never commit it.
- **Frontend**: `.env.local` (git-ignored) overrides `.env` for `VITE_API_URL`. Point it at your running backend (`http://localhost:5088/api/v1` by default).
- **Docker**: `docker-compose.yml` in `backend/` runs full stack. Frontend has its own `Dockerfile` for production builds.
- **Migrations**: Outbox table migrations **only** in `src/Modules/Shared/Migrations/`. Other DbContexts map it with `excludeFromMigrations: true`.
- **Multi-tenancy**: Always use `.ForTenant(currentTenant.UserId)` in queries. Tenant is the logged-in user (doctor).