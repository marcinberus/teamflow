# TeamFlow

Application for managing projects, teams, users, tasks, progress tracking, and project statistics:
a .NET 9 ASP.NET Core Web API and an Angular 22 single-page frontend.

## Tech stack

- **ASP.NET Core 9** — Web API
- **EF Core 9** — write side (repositories, Unit of Work)
- **Dapper** — read-side SQL projections
- **SQL Server** — database
- **MediatR** — CQRS (commands and queries)
- **FluentValidation** — input validation via pipeline behavior
- **JWT Bearer** — authentication
- **BCrypt** — password hashing
- **Swagger / OpenAPI**
- **xUnit + FluentAssertions + NSubstitute** — unit and integration tests
- **Testcontainers (SQL Server) + Respawn** — integration tests against a real database
- **Serilog + Grafana + Loki** — structured logs
- **Angular 22 + Angular Material** — frontend SPA, tested with Vitest

## Architecture

```
src/TeamFlow/
  TeamFlow.Domain          ← entities, enums, domain exceptions
  TeamFlow.Application     ← commands, queries, validators, interfaces
  TeamFlow.Importing       ← CSV/JSON importers for projects and tasks
  TeamFlow.Infrastructure  ← EF Core, Dapper read services, JWT, BCrypt, repositories
  TeamFlow.Api             ← controllers, middleware, DI composition root
  TeamFlow.Benchmarks      ← BenchmarkDotNet benchmarks for the CSV importer
src/TeamFlow.Frontend      ← Angular SPA, see its README
```

## Local development
### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server

### Getting started

```ps1
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update -p src/TeamFlow/TeamFlow.Infrastructure -s src/TeamFlow/TeamFlow.Api

# Run the API
dotnet watch --project src/TeamFlow/TeamFlow.Api --launch-profile Development
```

## Local development with Docker
### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Docker

### Getting started

```ps1
# Start infrastructure
docker compose up -d --wait

# Select the environment to pick up connection string for the containerized database
$env:ASPNETCORE_ENVIRONMENT = "Docker"

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update -p src/TeamFlow/TeamFlow.Infrastructure -s src/TeamFlow/TeamFlow.Api

# Run the API
dotnet watch --project src/TeamFlow/TeamFlow.Api --launch-profile Docker
```

The API will be available at:
- `https://localhost:7025`
- `http://localhost:5119`

### Viewing logs in Grafana

When the API runs with `ASPNETCORE_ENVIRONMENT=Docker`, Serilog sends structured logs directly to Loki. Grafana is provisioned with Loki as its default data source.

Open Grafana at `http://localhost:3000` (sign in with `admin` / `admin`), then **Explore**, select **Loki** and filter by labels:
- service_name="teamflow-api",
- environment="Docker"

## Frontend

With the API running at `https://localhost:7025`:

```ps1
cd src/TeamFlow.Frontend
npm install
npm start
```

Open `http://localhost:4200`. See [src/TeamFlow.Frontend/README.md](src/TeamFlow.Frontend/README.md) for details.

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
| `POST` | `/api/v1/users` | — | Register a new user with a caller-chosen global role, returns JWT |
| `POST` | `/api/v1/login` | — | Authenticate an existing user, returns JWT |
| `GET` | `/api/v1/users?search=alice&page=1&pageSize=20` | ✓ | Search users for member assignment |
| `GET` | `/api/v1/users/me` | ✓ | Get current user profile |
| `PUT` | `/api/v1/users/me` | ✓ | Update current user's first and last name |
| `POST` | `/api/v1/projects` | ✓ | Create a project; caller becomes its owner |
| `POST` | `/api/v1/projects/import` | ✓ | Import projects from a CSV or JSON file sent as `multipart/form-data` |
| `GET` | `/api/v1/projects?page=1&pageSize=20&status=Active&search=portal` | ✓ | List projects with pagination and optional status and text filters |
| `GET` | `/api/v1/projects/{projectId}` | ✓ | Get project details (members, tasks and statistics have their own endpoints) |
| `PUT` | `/api/v1/projects/{projectId}` | ✓ | Update a project's name and description (owner or admin) |
| `PATCH` | `/api/v1/projects/{projectId}/status` | ✓ | Change a project's status (owner or admin) |
| `DELETE` | `/api/v1/projects/{projectId}` | ✓ | Permanently delete a project (owner or admin) |
| `POST` | `/api/v1/projects/{projectId}/members` | ✓ | Assign a user to a project (owner, manager, or admin) |
| `GET` | `/api/v1/projects/{projectId}/members` | ✓ | List assigned project members |
| `DELETE` | `/api/v1/projects/{projectId}/members/{userId}` | ✓ | Remove a project member (owner, manager, or admin); the project owner cannot be removed |
| `POST` | `/api/v1/projects/{projectId}/tasks` | ✓ | Create a task; caller and optional assignee must belong to the project |
| `POST` | `/api/v1/projects/{projectId}/tasks/import` | ✓ | Import tasks from a CSV or JSON file sent as `multipart/form-data` |
| `GET` | `/api/v1/projects/{projectId}/statistics` | ✓ | Get task totals by status, assigned-member count, and a two-decimal completion percentage string |
| `GET` | `/api/v1/projects/{projectId}/tasks?status=InProgress&assignedUserId={userId}&page=1&pageSize=20` | ✓ | List project tasks with pagination and optional status and assignee filters |
| `PUT` | `/api/v1/projects/{projectId}/tasks/{taskId}` | ✓ | Update a task; caller and optional assignee must belong to the project |
| `PATCH` | `/api/v1/projects/{projectId}/tasks/{taskId}/status` | ✓ | Change a task's status; caller must belong to the project |

## Import formats

Both import endpoints accept a `.csv` or `.json` file in the `multipart/form-data` field `file`.

**Projects.** CSV has no header row; each row has exactly two quoted fields, and fields cannot contain quote characters:

```csv
"Website Redesign","Modernize the company website."
```

JSON is a top-level array (property names are case-insensitive):

```json
[
  {
    "name": "Website Redesign",
    "description": "Modernize the company website."
  }
]
```

**Tasks.** CSV has no header row; each row has exactly five quoted fields:

```csv
"Build project dashboard","Create the dashboard.","ee86f9c8-153d-4820-860c-7386a302fa52","2026-12-19T14:30:00+02:00","InProgress"
```

JSON is a top-level array of objects with `title`, `description`, `userId`, `dueDate` and `status`.
Some values are normalized instead of rejected: an unknown status becomes `Todo`, a user who is not
a project member becomes the importing user, and an invalid due date becomes empty.

**Errors.** A malformed CSV row or malformed JSON returns `422` Problem Details with the first
failure in `detail` (e.g. `"Invalid CSV row at line 4."`). There is no per-row error list.

## Running tests

```bash
# Unit tests
dotnet test tests/TeamFlow/TeamFlow.Tests.Unit

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/TeamFlow/TeamFlow.Tests.Integration
```

Integration tests share one Testcontainers SQL Server instance, run sequentially, and reset database data before each test.

Frontend tests (not run in CI, which covers the backend only):

```ps1
cd src/TeamFlow.Frontend
npm test -- --watch=false
```

## Running benchmarks

Run the CSV importer benchmark in Release mode:

```bash
dotnet run -c Release --project src/TeamFlow/TeamFlow.Benchmarks
```

The benchmark compares three CSV importer implementations: `string.Split()`, `ReadOnlySpan<char>`, and `ReadOnlyMemory<char>`, including execution time and memory allocations.

## Known limitations

- Public registration accepts any global role, including `Admin`. This is intentional for demo use only; production needs a server-assigned default role and a separate role-management workflow.
- The project list is global, not scoped to the current user.
