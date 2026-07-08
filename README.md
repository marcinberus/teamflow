# TeamFlow

.NET 9 ASP.NET Core Web API for managing projects, teams, users, tasks, progress tracking, and project statistics.

## Tech stack

- **ASP.NET Core 9** — Web API
- **EF Core 9** — write side (repositories, Unit of Work)
- **SQL Server / LocalDB** — database
- **MediatR** — CQRS (commands and queries)
- **FluentValidation** — input validation via pipeline behavior
- **JWT Bearer** — authentication
- **BCrypt** — password hashing
- **Swagger / OpenAPI**
- **xUnit + FluentAssertions + NSubstitute** — unit and integration tests
- **Testcontainers (SQL Server)** — integration tests of repositories with real database

## Architecture


```
TeamFlow.Domain          ← entities, enums, domain exceptions
TeamFlow.Application     ← commands, queries, validators, interfaces
TeamFlow.Infrastructure  ← EF Core, JWT, BCrypt, repositories
TeamFlow.Api             ← controllers, middleware, DI composition root
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server

## Getting started

```bash
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update -p src/TeamFlow/TeamFlow.Infrastructure -s src/TeamFlow/TeamFlow.Api

# Run the API
dotnet run --project src/TeamFlow/TeamFlow.Api
```

The API will be available at:
- `https://localhost:7025`
- `http://localhost:5119`

## Swagger UI

Swagger UI is available in **Development** mode at:

```
https://localhost:7025/swagger
```

### Authenticating in Swagger

1. Call `POST /api/v1/users` to register a new user — the response contains a `token`.
2. Click the **Authorize** button (lock icon) in the top-right of Swagger UI.
3. Enter `Bearer <token>` (including the `Bearer ` prefix) and click **Authorize**.
4. All subsequent requests will include the JWT header automatically.


## API endpoints

All routes are versioned under `/api/v1/`.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/v1/users` | — | Register a new user, returns JWT |

## Running tests

```bash
# Unit tests
dotnet test tests/TeamFlow/TeamFlow.Tests.Unit

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/TeamFlow/TeamFlow.Tests.Integration
```
