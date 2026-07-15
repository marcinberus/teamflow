# TeamFlow

.NET 9 ASP.NET Core Web API for managing projects, teams, users, tasks, progress tracking, and project statistics.

## Tech stack

- **ASP.NET Core 9** — Web API
- **EF Core 9** — write side (repositories, Unit of Work)
- **Dapper** — read-side SQL projections
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

**Option 1: Register a new user**
1. Call `POST /api/v1/users` with email, password, firstName, lastName, and role — the response contains a `token`.

**Option 2: Log in with existing credentials**
1. Call `POST /api/v1/login` with email and password — the response contains a `token`.

**Authorize requests**
1. Click the **Authorize** button (lock icon) in the top-right of Swagger UI.
2. Enter `Bearer <token>` (including the `Bearer ` prefix) and click **Authorize**.
3. All subsequent requests will include the JWT Bearer header automatically.


## API endpoints

All routes are versioned under `/api/v1/`.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/v1/users` | — | Register a new user, returns JWT |
| `POST` | `/api/v1/login` | — | Authenticate an existing user, returns JWT |
| `GET` | `/api/v1/users/me` | ✓ | Get current user profile |
| `PUT` | `/api/v1/users/me` | ✓ | Update current user's first and last name |
| `POST` | `/api/v1/projects` | ✓ | Create a project; caller becomes its owner |
| `GET` | `/api/v1/projects?page=1&pageSize=20&status=Active` | ✓ | List projects with pagination and an optional status filter |
| `GET` | `/api/v1/projects/{projectId}` | ✓ | Get full project details |
| `PUT` | `/api/v1/projects/{projectId}` | ✓ | Update a project's name and description (owner or admin) |
| `PATCH` | `/api/v1/projects/{projectId}/status` | ✓ | Change a project's status (owner or admin) |
| `DELETE` | `/api/v1/projects/{projectId}` | ✓ | Permanently delete a project (owner or admin) |
| `POST` | `/api/v1/projects/{projectId}/members` | ✓ | Assign a user to a project (owner, manager, or admin) |
| `GET` | `/api/v1/projects/{projectId}/members` | ✓ | List assigned project members |

## Running tests

```bash
# Unit tests
dotnet test tests/TeamFlow/TeamFlow.Tests.Unit

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/TeamFlow/TeamFlow.Tests.Integration
```
