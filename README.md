# Chubb APAC Policy Management Platform

Full-stack insurance policy management platform for the APAC region, built with ASP.NET Core 8 (Clean Architecture) and Angular 17+.

---

## Prerequisites

| Tool | Version |
|---|---|
| Docker Desktop | 4.x+ |
| .NET SDK | 8.0 |
| Node.js | 20+ |
| Angular CLI | `npm install -g @angular/cli@17` |

---

## Quick Start (Docker)

```bash
# 1. Copy and configure environment variables
cp .env.example .env
# Edit .env — set MSSQL_SA_PASSWORD and ConnectionStrings__DefaultConnection

# 2. Start all services (db → api → frontend)
docker-compose up --build
```

| Service | URL |
|---|---|
| Frontend (Angular) | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Health Check | http://localhost:5000/health |
| SQL Server | localhost:1433 |

---

## Run Backend Only

```bash
cd src/backend/Chubb.PolicyManagement.Api

# Set connection string in appsettings.Development.json or via env var
dotnet run
```

API will be available at http://localhost:5000 with Swagger at /swagger.

---

## Run Frontend Only

```bash
cd src/frontend/policy-dashboard

npm install
ng serve
# Proxies /api/* to http://localhost:5000 via proxy.conf.json
```

Frontend will be available at http://localhost:4200.

---

## Run Tests

### Backend Unit & Integration Tests

```bash
# All tests
dotnet test src/Chubb.PolicyManagement.sln

# Unit tests only
dotnet test src/backend/tests/Chubb.PolicyManagement.Tests.Unit

# Integration tests only (requires running DB)
dotnet test src/backend/tests/Chubb.PolicyManagement.Tests.Integration
```

### Frontend Tests

```bash
cd src/frontend/policy-dashboard
ng test
```

---

## Environment Variables

| Variable | Required | Description |
|---|---|---|
| `MSSQL_SA_PASSWORD` | Yes | SQL Server SA password (min 8 chars, mixed case + special) |
| `ConnectionStrings__DefaultConnection` | Yes | Full SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` / `Production` (default: Production) |

**Example connection string:**
```
Server=db,1433;Database=ChubbPolicyManagement;User Id=sa;Password=<your-password>;TrustServerCertificate=True;
```

---

## Project Structure

```
/
├── README.md
├── docker-compose.yml             # Production orchestration
├── docker-compose.override.yml    # Local dev overrides
├── .env.example                   # Environment variable template
├── docs/
│   └── openapi.yaml               # OpenAPI 3.0 specification
└── src/
    ├── Chubb.PolicyManagement.sln
    ├── backend/
    │   ├── Chubb.PolicyManagement.Domain/          # Entities, enums, interfaces (no dependencies)
    │   │   ├── Entities/Policy.cs
    │   │   ├── Enums/PolicyStatus.cs
    │   │   ├── Enums/LineOfBusiness.cs
    │   │   ├── Exceptions/PolicyNotFoundException.cs
    │   │   └── Interfaces/IPolicyRepository.cs
    │   ├── Chubb.PolicyManagement.Application/     # Use cases, DTOs, services (→ Domain only)
    │   │   ├── DTOs/PolicyDto.cs
    │   │   ├── DTOs/PolicySummaryDto.cs
    │   │   ├── DTOs/BulkFlagRequest.cs
    │   │   ├── Models/PagedResult.cs
    │   │   ├── Models/PolicyFilterQuery.cs
    │   │   ├── Interfaces/IPolicyService.cs
    │   │   └── Services/PolicyService.cs
    │   ├── Chubb.PolicyManagement.Infrastructure/  # EF Core, repositories (→ Domain + Application)
    │   │   ├── Persistence/PolicyManagementDbContext.cs
    │   │   ├── Persistence/Configurations/PolicyEntityConfiguration.cs
    │   │   ├── Persistence/Repositories/PolicyRepository.cs
    │   │   └── InfrastructureServiceExtensions.cs
    │   ├── Chubb.PolicyManagement.Api/             # Controllers, middleware, Program.cs (→ App + Infra)
    │   │   ├── Controllers/PoliciesController.cs
    │   │   ├── Middleware/GlobalExceptionMiddleware.cs
    │   │   ├── Program.cs
    │   │   └── Dockerfile
    │   └── tests/
    │       ├── Chubb.PolicyManagement.Tests.Unit/
    │       │   ├── PolicyServiceTests.cs
    │       │   └── PolicyEntityTests.cs
    │       └── Chubb.PolicyManagement.Tests.Integration/
    │           └── PoliciesControllerIntegrationTests.cs
    └── frontend/
        └── policy-dashboard/                       # Angular 17+ standalone components
            ├── src/
            │   ├── app/
            │   │   ├── core/
            │   │   │   ├── interceptors/error.interceptor.ts
            │   │   │   ├── models/policy.model.ts
            │   │   │   └── services/
            │   │   │       ├── policy-api.service.ts
            │   │   │       ├── storage.service.ts
            │   │   │       └── theme.service.ts
            │   │   ├── features/
            │   │   │   ├── policies/components/policy-list.component.ts
            │   │   │   └── summary/summary.component.ts
            │   │   ├── app.component.ts
            │   │   ├── app.config.ts
            │   │   └── app.routes.ts
            │   ├── environments/
            │   ├── index.html
            │   ├── main.ts
            │   └── styles.scss
            ├── angular.json
            ├── package.json
            ├── proxy.conf.json
            ├── tsconfig.json
            ├── Dockerfile
            └── nginx.conf
```

---

## Architecture

```
Chubb.PolicyManagement.Api
    ↓
Chubb.PolicyManagement.Application  ←→  Chubb.PolicyManagement.Infrastructure
    ↓                                            ↓
Chubb.PolicyManagement.Domain  ←────────────────
```

- **Domain** — pure C#, zero framework dependencies
- **Application** — use cases and orchestration, references Domain only
- **Infrastructure** — EF Core, SQL Server, implements Domain interfaces
- **Api** — HTTP layer, DI wiring, no business logic

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/policies` | Paged, filtered, sorted list |
| `GET` | `/api/v1/policies/{id}` | Single policy by UUID |
| `GET` | `/api/v1/policies/summary` | Aggregated statistics |
| `PATCH` | `/api/v1/policies/flag` | Bulk flag for review |

See [docs/openapi.yaml](docs/openapi.yaml) for the full OpenAPI 3.0 specification.
