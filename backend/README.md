# VeterinaryApi — Backend Documentation

> **Production-grade, enterprise-level documentation for the VeterinaryApi backend.**  
> Target audience: Backend developers onboarding to the project, DevOps engineers, and technical leads.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [High-Level Architecture](#2-high-level-architecture)
3. [Architecture Patterns Used](#3-architecture-patterns-used)
4. [Folder Structure](#4-folder-structure)
5. [Layer-by-Layer Explanation](#5-layer-by-layer-explanation)
   - [Domain Layer](#51-domain-layer)
   - [Features Layer (Application + Presentation)](#52-features-layer-application--presentation)
   - [Infrastructure Layer](#53-infrastructure-layer)
   - [Common Layer](#54-common-layer)
6. [Request / Response Flow](#6-request--response-flow)
7. [Dependency Injection Architecture](#7-dependency-injection-architecture)
8. [Database Configuration & Migrations](#8-database-configuration--migrations)
9. [Authentication & Authorization](#9-authentication--authorization)
10. [Validation Strategy](#10-validation-strategy)
11. [Error Handling Strategy](#11-error-handling-strategy)
12. [Multi-Tenancy Strategy](#12-multi-tenancy-strategy)
13. [Domain Events & Outbox Pattern](#13-domain-events--outbox-pattern)
14. [Background Jobs](#14-background-jobs)
15. [Real-Time Notifications (SignalR)](#15-real-time-notifications-signalr)
16. [Email Service Integration](#16-email-service-integration)
17. [Prescription Generation (PDF)](#17-prescription-generation-pdf)
18. [Pagination](#18-pagination)
19. [Logging Strategy](#19-logging-strategy)
20. [Environment Variables Reference](#20-environment-variables-reference)
21. [Setup & Run Instructions](#21-setup--run-instructions)
22. [Build & Deployment Instructions](#22-build--deployment-instructions)
23. [Running Tests](#23-running-tests)
24. [Code Quality Audit & Improvement Suggestions](#24-code-quality-audit--improvement-suggestions)
25. [Key Architectural Decisions & Trade-offs](#25-key-architectural-decisions--trade-offs)

---

## 1. Project Overview

**VeterinaryApi** is a multi-tenant SaaS backend for a veterinary clinic management system built on **.NET 10** with ASP.NET Core minimal APIs. It allows individual veterinary clinic doctors (tenants) to manage:

- **Clinics** — Clinic registration and profile management.
- **Clients** — Pet owners associated with a clinic.
- **Animals** — Patients (pets) belonging to clients.
- **Appointments** — Scheduled visits with status lifecycle management.
- **Visits** — Clinical visit records with symptoms, diagnosis, treatment, and payment.
- **Vaccinations** — Vaccination records per animal.
- **Prescriptions** — PDF prescription generation from Handlebars templates.
- **Notifications** — In-app real-time notifications via SignalR.
- **Users** — Authentication, registration, profile management, password management.
- **Dashboard** — Aggregated statistics and upcoming appointments.

The system is designed to be **deployed via Docker Compose** with a PostgreSQL database and exposes a versioned REST API (`/api/v1`).

---

## 2. High-Level Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                         Clients (HTTP/WS)                          │
│              (Frontend SPA · Mobile · Postman · Tests)             │
└───────────────────────────────┬────────────────────────────────────┘
                                │ HTTPS / WebSocket
                                ▼
┌────────────────────────────────────────────────────────────────────┐
│                        ASP.NET Core 10                             │
│                    Minimal API + Carter Modules                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                   Middleware Pipeline                       │  │
│  │  HTTPS Redirect → CORS → Exception Handler → Auth → Authz  │  │
│  └─────────────────────────────────────────────────────────────┘  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐  │
│  │  Features /  │   │  Features /  │   │  Features / ...       │  │
│  │  Appointments│   │  Users       │   │  (Vertical Slices)    │  │
│  │  ─────────── │   │  ─────────── │   │                       │  │
│  │  Command     │   │  Command     │   │  Each slice has:      │  │
│  │  Handler     │   │  Handler     │   │  · Command / Query    │  │
│  │  Validator   │   │  Validator   │   │  · Validator          │  │
│  │  Endpoint    │   │  Endpoint    │   │  · Handler            │  │
│  └──────┬───────┘   └──────┬───────┘   │  · Endpoint          │  │
│         │                  │           └──────────────────────┘  │
└─────────┼──────────────────┼────────────────────────────────────────┘
          │                  │
          ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Domain Layer                                  │
│  Entities · Value Objects · Domain Events · Business Rules          │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                             │
│  EF Core (PostgreSQL) · JWT · Outbox · Quartz · SignalR · Email     │
│  EF Interceptors (Audit, Tenant, Outbox) · Background Jobs          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Architecture Patterns Used

| Pattern | Where Applied |
|---|---|
| **Vertical Slice Architecture** | Each feature is fully self-contained in `Features/{Feature}/` |
| **CQRS (Command Query Responsibility Segregation)** | Commands mutate state; Queries read state — separate handler interfaces |
| **Domain-Driven Design (tactical patterns)** | Rich domain entities, domain events, factory methods, domain exceptions |
| **Outbox Pattern** | Domain events are persisted to `OutboxMessages` table before publishing |
| **Result Pattern** | All command/query handlers return `Result<T>` — no exception-based control flow for business errors |
| **Multi-Tenancy (per-user isolation)** | Every entity is stamped with `TenantId` (== `UserId` of the logged-in doctor) |
| **Repository abstraction via `IApplicationDbContext`** | Infrastructure concern hidden behind interface, enabling testability |
| **Scrutor-based auto-registration** | Command/Query handlers and Domain Event handlers are discovered automatically |

---

## 4. Folder Structure

```
src/VeterinaryApi/
├── Program.cs                      # Application entry point, DI composition root
├── appsettings.json                # Non-sensitive configuration
├── appsettings.Development.json    # Development overrides
├── .env                            # Sensitive secrets (NOT committed to VCS)
│
├── Domain/                         # Pure domain model — no framework dependencies
│   ├── Common/                     # Base entities, interfaces, value objects
│   │   ├── Entity.cs               # Base aggregate root (Id, timestamps, soft-delete, domain events, tenancy)
│   │   ├── DomainEvents.cs         # IDomainEvent interface and DomainEvent base record
│   │   ├── DomainException.cs      # Domain exception carrying an Error object
│   │   ├── ISoftDelete.cs          # Soft-delete contract
│   │   ├── ITenantOwned.cs         # Tenant isolation contract
│   │   ├── IWhatshouldINameit.cs   # ICreatedBy audit interface
│   │   ├── Email.cs                # Email value object
│   │   ├── Name.cs                 # Name value object
│   │   └── Address.cs              # Address value object
│   ├── Animals/                    # Animal aggregate
│   ├── Appointments/               # Appointment aggregate + domain events
│   ├── Clients/                    # Client aggregate
│   ├── Clinics/                    # Clinic aggregate
│   ├── Notifications/              # Notification aggregate
│   ├── Users/                      # User aggregate + UserSession + UserRoles
│   ├── Vaccinations/               # Vaccination aggregate
│   ├── Visits/                     # Visit aggregate + VisitType + PaymentStatus
│   └── Gender.cs                   # Shared enum
│
├── Features/                       # Vertical slices — one folder per domain feature
│   ├── Animals/                    # CRUD operations for animals
│   ├── Appointments/               # Appointment lifecycle commands + queries
│   ├── Auth/                       # Auth-specific endpoints (Me)
│   ├── Clients/                    # Client management
│   ├── Clinics/                    # Clinic management
│   ├── Dashboard/                  # Analytics and summaries
│   ├── Notifications/              # Notification queries
│   ├── Prescriptions/              # PDF prescription generation
│   ├── Users/                      # Auth commands (Login, Register, etc.)
│   ├── Vaccinations/               # Vaccination records
│   └── Visits/                     # Visit records
│
├── Infrastructure/                 # External concerns and framework implementations
│   ├── Auth/                       # JWT generation (JwtProvider) + JwtOptions
│   ├── CQRS/                       # DomainEventsDispatcher, DomainEventPublisher
│   ├── Interceptors/               # AuditInterceptor (sets CreatedByUserId)
│   ├── Notifications/              # SignalR NotificationHub + NotificationService
│   ├── OutboxMessages/             # OutboxMessage entity + EF configuration + Quartz job
│   ├── Persistence/                # ApplicationDbContext + EF entity configurations
│   ├── Services/
│   │   ├── Hashers/                # Argon2PasswordHasher
│   │   ├── Notifications/          # (placeholder)
│   │   └── Users/                  # CurrentUserService (reads JWT claims)
│   ├── Tenants/                    # TenantInterceptor (stamps TenantId on save)
│   └── DependencyInjection.cs      # Central infrastructure DI registration
│
├── Common/                         # Cross-cutting concerns shared across all layers
│   ├── Abstracions/                # IApplicationDbContext, ICurrentTenant, IJwtProvider, IPasswordHasher
│   ├── CQRS/                       # ICommand, IQuery, ICommandHandler, IQueryHandler, IDomainEventHandler interfaces
│   ├── Endpoints/                  # IEndpoint (extends ICarterModule)
│   ├── Errors/                     # Error, ErrorType
│   ├── Exceptions/                 # Exception handlers (Validation, Domain, Global)
│   ├── Extensions/                 # MigrationExtensions (ApplyMigrations)
│   ├── Paginations/
│   │   ├── OffSet/                 # OffSetPagedList, TableRequest
│   │   └── Cursor/                 # Cursor-based pagination types
│   ├── Results/                    # Result<T>, ResultExtension (Problem Details mapping)
│   └── Util/                       # Utility helpers
│
├── Migrations/                     # EF Core auto-generated migration files
└── Properties/
    └── launchSettings.json         # Local development launch profiles
```

---

## 5. Layer-by-Layer Explanation

### 5.1 Domain Layer

The Domain layer is the **innermost layer** and has **zero infrastructure dependencies**. It encapsulates all business rules, invariants, and domain knowledge.

#### `Entity` Base Class

Every aggregate root inherits from `Entity`. It provides:

| Property/Method | Purpose |
|---|---|
| `Id` | Auto-generated `Guid` (set in constructor) |
| `CreatedOnUtc` | UTC timestamp set on construction |
| `IsDeleted` / `DeletedOnUtc` | Soft-delete support |
| `TenantId` | Multi-tenant isolation identifier |
| `DomainEvents` | Read-only collection of raised domain events |
| `RaiseDomainEvent()` | Protected method called by entity business methods |
| `ClearDomainEvent()` | Called by the outbox interceptor after collecting events |
| `Delete()` | Marks entity deleted; throws `DomainException` if already deleted |

#### Domain Events

Domain events represent something that **happened** in the domain, expressed as immutable records.

```
IDomainEvent (interface)
    ↳ DomainEvent (abstract record with Id + TenantId)
        ↳ AppointmentCancelledDomainEvent(AppointmentId)
```

Events are raised inside entity methods (e.g., `Appointment.Cancel()`) and collected by the EF Core `InsertOutboxMessagesInterceptor` before being persisted.

#### Factory Methods

All entities use **static factory methods** (`Create(...)`) instead of public constructors. This enforces that an entity can only be created in a valid state, with all business rules applied upfront.

#### Domain Exceptions

`DomainException` wraps an `Error` object and is thrown inside entity methods for business rule violations (e.g., cancelling an already-cancelled appointment). It is caught by `DomainExceptionHandler` at the API layer.

#### Value Objects

`Email`, `Name`, and `Address` are value objects with encapsulated validation logic. (Note: in the current implementation, some are partially integrated.)

---

### 5.2 Features Layer (Application + Presentation)

This layer implements the **Vertical Slice Architecture**. Each feature slice in `Features/{Feature}/{SliceName}.cs` is a self-contained, static class that contains:

```
CreateAppointment (static class)
├── CreateAppointmentCommand    → ICommand<Response>       (input DTO)
├── Response                   → record                   (output DTO)
├── Validator                  → AbstractValidator<...>    (FluentValidation rules)
├── CreateAppointmentsCommandHandler → ICommandHandler<...> (business logic)
└── Endpoint                   → IEndpoint (ICarterModule) (HTTP route mapping)
```

This co-location of all concerns related to one operation makes it trivially easy to find, understand, and modify any feature.

#### CQRS Interfaces

| Interface | Use Case |
|---|---|
| `ICommand<TResponse>` | Mutating operation that returns a typed result |
| `ICommand` | Mutating operation with no response |
| `IQuery<TResponse>` | Read operation |
| `ICommandHandler<TCommand, TResponse>` | Handles a command with response |
| `ICommandHandler<TCommand>` | Handles a command without response |
| `IQueryHandler<TQuery, TResponse>` | Handles a query |
| `IDomainEventHandler<TEvent>` | Handles a domain event (side effects) |

#### Carter Endpoints

All HTTP routes are defined via Carter (`ICarterModule` / `IEndpoint`). Carter is registered once with `app.MapCarter()` and all endpoints are automatically discovered. Routes are grouped under `/api/v1`.

---

### 5.3 Infrastructure Layer

The Infrastructure layer implements all abstractions defined in the Domain and Common layers.

#### `ApplicationDbContext`

- Inherits from `DbContext` and implements `IApplicationDbContext`.
- Contains `DbSet<T>` for all domain entities.
- Applies EF entity configurations from `Persistence/Configurations/` via `ApplyConfigurationsFromAssembly`.
- Receives two EF interceptors via DI: `InsertOutboxMessagesInterceptors` and `TenantInterceptor`.

#### EF Core Interceptors

| Interceptor | Trigger | Responsibility |
|---|---|---|
| `InsertOutboxMessagesInterceptors` | Before `SaveChangesAsync` | Collects domain events from tracked entities, serializes them to `OutboxMessage` rows, clears the entity's event list |
| `TenantInterceptor` | Before `SaveChangesAsync` | Stamps `TenantId` on all newly added `ITenantOwned` entities (except `User`) |
| `AuditInterceptor` | After `SavedChangesAsync` | Sets `CreatedByUserId` on newly added `ICreatedBy` entities |

#### JWT Authentication

- `JwtProvider` generates access tokens using HMAC-SHA256.
- Claims include: `NameIdentifier` (UserId), `Name` (UserName), `sub`, `jti`, `iat`.
- Refresh tokens are random 32-byte base64 strings stored as `UserSession` entities.
- Token configuration is loaded from environment variables at startup.

#### Password Hashing

Passwords are hashed using **Argon2** (via `Isopoh.Cryptography.Argon2`), which is the current gold standard for password hashing. The `IPasswordHasher` abstraction decouples the hashing algorithm from the rest of the system.

#### SignalR Notification Hub

`NotificationHub` is an authorized SignalR hub. It does not define any server-to-client methods directly; the `NotificationService` uses `IHubContext<NotificationHub>` to push messages. Clients receive messages on the `ReceiveNotification` channel, keyed by `UserId`.

For WebSocket connections (SignalR), the JWT token is extracted from the `access_token` query parameter, since browsers cannot send custom headers in WebSocket handshakes.

#### `CurrentUserService`

Reads the `NameIdentifier` claim from the current `HttpContext` user principal. Implements `ICurrentTenant`, providing `UserId` to interceptors and handlers throughout the request lifecycle.

---

### 5.4 Common Layer

The Common layer defines cross-cutting concerns used by all other layers.

#### Result Pattern

Instead of throwing exceptions for business errors, handlers return `Result<T>`:

```
Result (base)           IsSuccess | Error
  └── Result<T>         IsSuccess | Error | Value
```

The `ResultExtension.Problem()` extension method converts a failed `Result` into an RFC 7807 Problem Details HTTP response with the appropriate HTTP status code based on `ErrorType`.

#### Error Types

| ErrorType | HTTP Status | Typical Use |
|---|---|---|
| `Failure` | 500 | Generic infrastructure failures |
| `NotFound` | 404 | Queried entity does not exist |
| `Validation` | 400 | Input validation failed |
| `Conflict` | 409 | Business rule / state conflict |
| `Unauthorized` | 401 | Authentication/authorization failure |

#### Pagination

Two pagination strategies are available:

- **Offset-based** (`OffSet/`): `TableRequest` contains `Page`, `PageSize`, `search`, `SortColumn`, `SortOrder`. Returns `OffSetPagedList<T>` with items and total count.
- **Cursor-based** (`Cursor/`): Suitable for infinite scrolling or high-performance large dataset pagination.

---

## 6. Request / Response Flow

Below is the complete end-to-end flow for a **command** (e.g., `POST /api/v1/appointments`):

```
1. HTTP Request arrives
        │
2. Middleware Pipeline executes
   ├── HTTPS Redirection
   ├── CORS Validation
   ├── Exception Handler (registered — wraps the entire pipeline)
   ├── Authentication (JWT token validated; User principal populated)
   └── Authorization (checks [Authorize] attributes)
        │
3. Carter routes the request to the matching Endpoint.AddRoutes lambda
        │
4. Command record deserialized from request body
        │
5. ICommandHandler<TCommand, TResponse>.Handle() invoked (resolved via DI)
        │
6. FluentValidation Validator.ValidateAndThrow(command)
   ├── If INVALID → FluentValidation.ValidationException thrown
   │       └──→ ValidationExceptionHandler → 400 + Problem Details (field errors)
   └── If VALID → continue
        │
7. Handler queries IApplicationDbContext (EF Core) for necessary data
        │
8. Domain entity created/mutated via factory/business methods
   └── Domain events may be raised (e.g., AppointmentCancelledDomainEvent)
        │
9. _db.SaveChangesAsync() called
   ├── InsertOutboxMessagesInterceptors fires BEFORE save:
   │   ├── Collects domain events from tracked entities
   │   ├── Stamps TenantId on each event
   │   └── Inserts OutboxMessage rows in the same transaction
   ├── TenantInterceptor fires BEFORE save:
   │   └── Stamps TenantId on new ITenantOwned entities
   └── Data + OutboxMessages committed atomically to PostgreSQL
        │
10. Result<Response>.Success(...) returned to Endpoint
        │
11. Endpoint maps Result → IResult (201 Created / 200 OK)
        │
12. HTTP Response sent
        │
── Background: ProcessOutboxMessagesJob (every 10 seconds) ──────────────
13. Quartz job polls OutboxMessages WHERE ProcessedOnUtc IS NULL (batch 20)
14. Deserializes each domain event (Newtonsoft.Json TypeNameHandling.All)
15. DomainEventPublisher.PublishAsync() calls all IDomainEventHandler<T> in parallel
   └── e.g., AppointmentCancelledDomainEventHandler:
       ├── Queries appointment details
       ├── Creates Notification entity
       ├── Persists notification to DB
       └── Pushes real-time notification via SignalR to the correct tenant user
16. OutboxMessage.ProcessedOnUtc stamped; SaveChangesAsync called
```

For a **query** (e.g., `GET /api/v1/appointments`):

Steps 1–5 are the same. Steps 6–9 are replaced by read-only EF Core queries (`.AsNoTracking()`). No domain events are raised. Result is returned directly.

---

## 7. Dependency Injection Architecture

Dependency injection is configured in two places:

### `Program.cs` (Application-level registrations)

| Service | Lifetime | Notes |
|---|---|---|
| `IValidator<T>` (FluentValidation) | Singleton | Registered via `AddValidatorsFromAssemblyContaining<Validator>` |
| `ICommandHandler<,>` | Scoped | Auto-scanned via Scrutor |
| `ICommandHandler<>` | Scoped | Auto-scanned via Scrutor |
| `IQueryHandler<,>` | Scoped | Auto-scanned via Scrutor |
| Exception handlers | Transient | `ValidationExceptionHandler`, `DomainExceptionHandler`, `GlobalExceptionHandler` |
| Carter endpoints | Scoped | Auto-discovered by `AddCarter()` |

### `Infrastructure/DependencyInjection.cs` (Infrastructure-level registrations)

| Service | Lifetime | Notes |
|---|---|---|
| `IPasswordHasher` | Singleton | Argon2 implementation — stateless, thread-safe |
| `IJwtProvider` | Scoped | Token generation with options |
| `JwtOptions` | Options snapshot | Loaded from env vars |
| `IApplicationDbContext` / `ApplicationDbContext` | Scoped | EF Core DbContext |
| EF Interceptors (`AuditInterceptor`, `InsertOutboxMessagesInterceptors`, `TenantInterceptor`) | Scoped | Injected into DbContext options |
| `IEmailService` | Singleton | MailKit SMTP sender; `EmailOptions` from config |
| `ICurrentTenant` → `CurrentUserService` | Scoped | Reads `IHttpContextAccessor` |
| `IHttpContextAccessor` | Singleton | Built-in ASP.NET Core |
| `IDomainEventDispatcher` | Transient | `DomainEventsDispatcher` |
| `IDomainEventPublisher` | Transient | `DomainEventPublisher` |
| `IDomainEventHandler<T>` | Scoped | Auto-scanned via Scrutor for all implementations |
| `INotificatioService` | Scoped | `NotificationService` (SignalR push) |
| Quartz hosted service | Singleton | `ProcessOutboxMessagesJob` scheduled every 10 s |
| SignalR | Singleton | `AddSignalR()` |

---

## 8. Database Configuration & Migrations

### Database

- **Engine**: PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Connection string**: Read from environment variable `ConnectionStrings__Default`
- **Connection pooling**: Configured in the connection string (`Minimum Pool Size=5; Maximum Pool Size=100`)

### EF Core Configuration

Entity configurations live in `Infrastructure/Persistence/Configurations/`. Each aggregate has its own `IEntityTypeConfiguration<T>` class. Configurations are applied via `modelBuilder.ApplyConfigurationsFromAssembly(...)` in `OnModelCreating`.

### Migrations

Migrations are auto-generated using EF Core tooling and are located in `Migrations/`. They are applied automatically at application startup via the `ApplyMigrations()` extension method called in `Program.cs`:

```csharp
app.ApplyMigrations(); // Calls dbContext.Database.Migrate() on startup
```

**Generating a new migration:**

```bash
dotnet ef migrations add <MigrationName> \
  --project src/VeterinaryApi \
  --startup-project src/VeterinaryApi
```

**Applying migrations manually:**

```bash
dotnet ef database update \
  --project src/VeterinaryApi \
  --startup-project src/VeterinaryApi
```

---

## 9. Authentication & Authorization

### Flow

```
1. User sends POST /api/v1/users/login  { email, password }
2. Handler looks up User by email
3. Argon2 password hash verification
4. If valid:
   a. JwtProvider.GenerateToken(user)     → short-lived access token (default 50 min)
   b. JwtProvider.GenerateRefreshToken()  → random 32-byte base64 string
   c. UserSession created (refresh token + expiry) and persisted
   d. Response: { accessToken, refreshToken, expiration }
5. Client stores tokens; sends Authorization: Bearer <accessToken> on subsequent requests
6. Access token expiry → POST /api/v1/users/refresh-token
7. POST /api/v1/users/logout → UserSession invalidated
```

### Token Claims

| Claim | Value |
|---|---|
| `ClaimTypes.NameIdentifier` (`sub`) | User GUID — used as TenantId throughout the system |
| `ClaimTypes.Name` | UserName |
| `jti` | Unique token ID |
| `iat` | Issued-at Unix timestamp |

### JWT Validation Parameters

- **Issuer**: validated against `JWT_ISSUER` env var
- **Audience**: validated against `JWT_AUDIENCE` env var
- **Signing key**: HMAC-SHA256, validated against `JWT_SECRET_KEY` (minimum 32 chars)
- **Lifetime**: validated; `ClockSkew = TimeSpan.Zero` (strict expiry)

### SignalR Authorization

The `NotificationHub` is decorated with `[Authorize]`. For WebSocket upgrades, the JWT is passed via the `access_token` query string parameter (handled by a custom `OnMessageReceived` event in `JwtBearerEvents`).

### Roles

`UserRoles` enum defines roles (`Doctor` is the primary role). Role-based authorization is available (`[Authorize(Roles = "...")]`) but feature-level authorization is primarily enforced by tenant isolation (data scoped to the logged-in user's `TenantId`).

---

## 10. Validation Strategy

**FluentValidation** is used for all input validation. Validators are co-located with their commands inside the feature slice:

```csharp
public sealed class Validator : AbstractValidator<CreateAppointmentCommand>
{
    public Validator()
    {
        RuleFor(e => e.animalId).NotEmpty();
        RuleFor(e => e.AppointmentDate)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow);
        RuleFor(e => e.location).NotEmpty();
    }
}
```

All validators are registered as **singletons** via `AddValidatorsFromAssemblyContaining<Validator>()`.

Validation is invoked inside the handler with `_validator.ValidateAndThrow(command)`. If validation fails, FluentValidation throws a `ValidationException`, which is caught by the `ValidationExceptionHandler` middleware and converted to a structured 400 Problem Details response:

```json
{
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "appointmentDate": ["'Appointment Date' must be greater than '...'."]
  }
}
```

---

## 11. Error Handling Strategy

Three exception handlers are registered in order of priority (most specific → least specific):

| Handler | Catches | HTTP Status | Format |
|---|---|---|---|
| `ValidationExceptionHandler` | `FluentValidation.ValidationException` | 400 | ProblemDetails + field-level errors dictionary |
| `DomainExceptionHandler` | `DomainException` | 409 | ProblemDetails + error code + description |
| `GlobalExceptionHandler` | Any unhandled `Exception` | 500 | Generic ProblemDetails; logs the exception |

For non-exception business failures, the `Result<T>` pattern is used. The `ResultExtension.Problem()` method converts a failed result into an `IResult` with the correct HTTP status code based on `ErrorType`:

| ErrorType | HTTP Status |
|---|---|
| `NotFound` | 404 |
| `Validation` | 400 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |
| `Failure` | 500 |

---

## 12. Multi-Tenancy Strategy

The system implements a **data isolation per doctor (user)** multi-tenancy model. Each registered doctor is their own tenant, and their data is completely isolated from other doctors.

### How It Works

1. When a `User` registers, `user.TenantId = user.Id` — the user IS the tenant.
2. Every entity that implements `ITenantOwned` has a `TenantId` property.
3. The `TenantInterceptor` (EF Core interceptor) automatically stamps `TenantId = currentUser.UserId` on **every new entity** before it is saved, eliminating the possibility of accidentally saving data without a tenant.
4. All queries filter by tenant using `.ForTenant(currentTenant.UserId!.Value)`, an EF Core queryable extension that applies a `WHERE TenantId = @userId` filter.

### Security Consideration

This model ensures a doctor cannot accidentally read or modify another doctor's data, as long as all queries use `.ForTenant(...)`. This should be enforced by code review policy or a global query filter (see Section 24 for improvement suggestions).

---

## 13. Domain Events & Outbox Pattern

### Why the Outbox Pattern?

Without the Outbox pattern, if a domain event is published immediately after `SaveChanges`, a crash between the two calls would result in a lost event (data saved, event never published). The Outbox pattern solves this by:

1. Persisting domain events to an `OutboxMessages` table **in the same database transaction** as the entity change.
2. A background job reads unprocessed messages and publishes them — guaranteed delivery.

### Flow

```
Entity.Cancel() → RaiseDomainEvent(AppointmentCancelledDomainEvent)
       ↓
SaveChangesAsync()
       ↓
InsertOutboxMessagesInterceptors.SavingChangesAsync()
   → Collects events from all tracked entities
   → Serializes each event via Newtonsoft.Json (TypeNameHandling.All preserves polymorphism)
   → Inserts OutboxMessage { Id, Name (AssemblyQualifiedName), Content (JSON), CreatedOnUtc }
       ↓
OutboxMessages + Entity data committed atomically to PostgreSQL
       ↓
ProcessOutboxMessagesJob (Quartz, every 10 seconds)
   → SELECT top 20 WHERE ProcessedOnUtc IS NULL ORDER BY Id
   → Deserializes each message back to the correct IDomainEvent type
   → DomainEventPublisher.PublishAsync(event)
       → Resolves all IDomainEventHandler<TEvent> from DI
       → Executes all handlers in parallel via Task.WhenAll
   → Stamps ProcessedOnUtc = UtcNow
   → SaveChangesAsync
```

### `OutboxMessage` Schema

| Column | Type | Description |
|---|---|---|
| `Id` | `uuid` | Unique message identifier |
| `Name` | `text` | Assembly-qualified type name of the domain event |
| `Content` | `text` | JSON-serialized domain event payload |
| `CreatedOnUtc` | `timestamp` | When the event was created |
| `ProcessedOnUtc` | `timestamp?` | Set when successfully processed (null = pending) |
| `Errors` | `text?` | Error details if processing failed (for future retry logic) |

---

## 14. Background Jobs

The system uses **Quartz.NET** (`Quartz` + `Quartz.Extensions.Hosting`) for scheduling.

### `ProcessOutboxMessagesJob`

| Property | Value |
|---|---|
| Schedule | Every **10 seconds**, repeat forever |
| Concurrency | `[DisallowConcurrentExecution]` — prevents overlapping runs |
| Batch size | 20 messages per run |
| Purpose | Reads unprocessed outbox messages and dispatches domain events |
| Hosted service | `AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true)` — graceful shutdown waits for the current job to complete |

---

## 15. Real-Time Notifications (SignalR)

### Architecture

```
DomainEventHandler (e.g., AppointmentCancelledDomainEventHandler)
    → Creates Notification entity, persists to DB
    → NotificationService.SendNotificationAsync(notification, UserId)
        → IHubContext<NotificationHub>.Clients.User(UserId).SendAsync("ReceiveNotification", payload)
            → Frontend SignalR client receives { notification: { id, title, body, isRead, createdOnUtc } }
```

### Hub URL

`/hubs/notification`

### Message Channel

`"ReceiveNotification"` — clients subscribe to this event name.

### Authentication

WebSocket connections include the JWT via `?access_token=<token>` query parameter.

---

## 16. Email Service Integration

- **Provider**: Gmail SMTP via MailKit (`MailKit` package)
- **Interface**: `IEmailService` (registered as singleton)
- **Configuration**: `EMAIL_CONFIGURATIONS_EMAIL` and `EMAIL_CONFIGURATIONS_PASSWORD` from environment; `HOST` and `PORT` from `appsettings.json`
- **Use cases**: Password reset emails (ForgetPassword / ResetPassword feature)

---

## 17. Prescription Generation (PDF)

- **Template engine**: Handlebars.Net — `PrescriptionTemplate.hbs` embedded resource
- **PDF engine**: PuppeteerSharp (headless Chromium)
- **Flow**: Handlebars renders the template → produces HTML → PuppeteerSharp converts HTML to PDF → PDF bytes returned as file response
- **Note**: On first run, PuppeteerSharp downloads a Chromium binary. Ensure internet access during initialization in Docker.

---

## 18. Pagination

### Offset-Based Pagination (`TableRequest<T>`)

Used by most table endpoints:

| Parameter | Type | Description |
|---|---|---|
| `Page` | `int` | 1-based page number |
| `PageSize` | `int` | Records per page |
| `search` | `string?` | Full-text search filter |
| `SortColumn` | `string?` | Column name to sort by |
| `SortOrder` | `"asc"` / `"desc"` | Sort direction |

Returns `OffSetPagedList<T>` with `Items` and `TotalCount`.

### Cursor-Based Pagination

Available as an alternative for high-volume data endpoints where offset pagination degrades performance at large page numbers.

---

## 19. Logging Strategy

The system uses the **built-in ASP.NET Core `ILogger<T>`** abstraction.

- Log levels configured in `appsettings.json`: `Default = Information`, `Microsoft.AspNetCore = Warning`
- The `GlobalExceptionHandler` logs unhandled exceptions at `LogError` level with full exception details.
- No structured logging provider (e.g., Serilog, OpenTelemetry) is currently configured — see Section 24 for recommendations.

---

## 20. Environment Variables Reference

All sensitive configuration is loaded from the `.env` file (via `DotNetEnv`) and OS environment variables. The `.env` file must **never be committed to version control**.

| Variable | Required | Description | Example |
|---|---|---|---|
| `ConnectionStrings__Default` | ✅ | PostgreSQL connection string | `Host=...;Database=...;Username=...;Password=...` |
| `JWT_SECRET_KEY` | ✅ | HMAC-SHA256 signing key (min 32 chars) | `u8XwL3pQz1VnJ7sBd4FkR9mGh2CyT0aZ` |
| `JWT_ISSUER` | ✅ | JWT issuer claim | `https://api.yourdomain.com` |
| `JWT_AUDIENCE` | ✅ | JWT audience claim | `https://app.yourdomain.com` |
| `JWT_ACCESS_TOKEN_LIFETIME_MINUTES` | ✅ | Access token TTL in minutes | `50` |
| `EMAIL_CONFIGURATIONS_EMAIL` | ✅ | Sender email address | `noreply@yourdomain.com` |
| `EMAIL_CONFIGURATIONS_PASSWORD` | ✅ | SMTP app password (Gmail App Password) | `xxxx xxxx xxxx xxxx` |

**`appsettings.json` (non-sensitive):**

| Key | Description |
|---|---|
| `EMAIL_CONFIGURATIONS:HOST` | SMTP host (`smtp.gmail.com`) |
| `EMAIL_CONFIGURATIONS:PORT` | SMTP port (`587`) |

---

## 21. Setup & Run Instructions

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL or full stack)
- [Node.js / pnpm](https://pnpm.io/) (frontend only)

### Option A: Local Development (with Docker PostgreSQL)

1. **Clone the repository**

```bash
git clone <repo-url>
cd VeterinaryApplication
```

2. **Create `.env` file**

```bash
cp backend/src/VeterinaryApi/.env.example \
   backend/src/VeterinaryApi/.env
# Edit .env and fill in all required values
```

3. **Start PostgreSQL via Docker**

```bash
docker run -d \
  --name veterinary-postgres \
  -e POSTGRES_DB=veterinary_db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=2501 \
  -p 5432:5432 \
  postgres:16
```

4. **Update `ConnectionStrings__Default`** in `.env`:

```
ConnectionStrings__Default="Host=localhost;Port=5432;Database=veterinary_db;Username=postgres;Password=2501;"
```

5. **Run the API**

```bash
cd backend/src/VeterinaryApi
dotnet run
```

Migrations are applied automatically on startup. The API will be available at `https://localhost:7xxx`.

6. **Access Scalar API UI**

Navigate to `https://localhost:7xxx/scalar` for interactive API documentation (development only).

### Option B: Full Stack with Docker Compose

```bash
cd backend
docker-compose up --build
```

This starts:
- `veterinaryapi` — the ASP.NET Core backend
- `veterinary.database` — PostgreSQL 16

---

## 22. Build & Deployment Instructions

### Build for Production

```bash
cd backend/src/VeterinaryApi
dotnet publish -c Release -o ./publish
```

### Build Docker Image

```bash
docker build \
  -f src/VeterinaryApi/Dockerfile \
  -t veterinaryapi:latest \
  .
```

### Docker Compose (Production)

```bash
cd backend
docker-compose -f docker-compose.yml up -d --build
```

### Environment Configuration in Production

Set all environment variables defined in Section 20 via:
- Docker Compose `environment:` block
- Cloud platform secrets (Azure Key Vault, AWS Secrets Manager, etc.)
- Kubernetes `Secret` objects

**Never ship the `.env` file inside the Docker image.**

### Health Check Consideration

Consider adding a `/health` endpoint (via `AddHealthChecks()`) for load balancer and container orchestrator readiness/liveness probes.

---

## 23. Running Tests

Tests are located in `Tests/Application.Tests/`.

```bash
cd backend
dotnet test Tests/Application.Tests/Application.Tests.csproj
```

Test folders observed:
- `Appointments/` — Appointment command/query tests
- `Clinics/` — Clinic tests
- `Users/` — User/auth tests
- `Helpers/` — Shared test helpers

---
