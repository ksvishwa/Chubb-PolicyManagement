# Chubb APAC Policy Management Platform — High-Level Design (HLD)

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Overview](#architecture-overview)
3. [Component Interaction](#component-interaction)
4. [Technology Stack Rationale](#technology-stack-rationale)
5. [Data Flow & Processing](#data-flow--processing)
6. [Security Architecture](#security-architecture)
7. [Scalability & Performance](#scalability--performance)
8. [Deployment Architecture](#deployment-architecture)

---

## System Overview

### Purpose & Mission

The **Chubb APAC Policy Management Platform** is a modern, full-stack insurance policy management system designed to:

- Centralize policy data across Asia-Pacific regions into a single source of truth
- Replace disconnected spreadsheets and legacy systems with a contract-first API and responsive web dashboard
- Provide operational visibility and actionable insights via real-time aggregated statistics
- Enable efficient bulk operations (flagging policies for review, status changes)
- Maintain audit trails and enforce data integrity across policy lifecycle management

### Scope by Tier

| Tier | Component | Purpose | Status |
|---|---|---|---|
| **Tier 1** | ASP.NET Core BFF API, SQL Server DB, EF Core ORM | Backend service layer with contract-first OpenAPI spec | ✅ Required |
| **Tier 2** | Angular 17+ standalone components, responsive UI | Frontend dashboard for policy management | ✅ Required |

### Target Users

- **Policy Administrators** — Manage, filter, and bulk flag policies
- **Risk & Compliance Teams** — Access aggregated statistics, identify patterns
- **Underwriters & Support Staff** — View policy details, update statuses

### Key Capabilities

| Capability | Description |
|---|---|
| **Policy Discovery** | Paginated, filtered, sorted list with free-text search |
| **Policy Details** | Single policy retrieval with full entity attributes |
| **Bulk Flagging** | Flag multiple policies for review in one atomic transaction |
| **Summary Statistics** | Real-time aggregated counts by status, LOB, region |
| **Audit & Compliance** | Immutable CreatedAt/UpdatedAt timestamps, RFC 7807 error responses |
| **Multi-Region** | Support for APAC region filtering (Singapore, Australia, Japan, etc.) |

---

## Architecture Overview

### Clean Architecture Layers

The platform enforces **Clean Architecture** with strict, inward-pointing dependency rules:

```
┌─────────────────────────────────────────────────────┐
│  API Layer (Controllers, Middleware, Swagger UI)    │  External consumers
├─────────────────────────────────────────────────────┤
│  Application Layer (DTOs, Services, Validators)     │  Business logic orchestration
├─────────────────────────────────────────────────────┤
│  Domain Layer (Entities, Enums, Exceptions)         │  Core business rules (no frameworks)
├─────────────────────────────────────────────────────┤
│  Infrastructure Layer (EF Core, DB Context, Repos)  │  Data persistence & external services
└─────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### **Domain Layer** (`Chubb.PolicyManagement.Domain`)
- **Purpose:** Pure C# business model — no infrastructure references
- **Contents:**
  - Entity definitions (`Policy`, `PolicyStatus`, `LineOfBusiness`, `Region`)
  - Domain enums and value objects
  - Domain exceptions (`PolicyNotFoundException`, `ValidationException`)
  - Repository interfaces (contracts only)
- **Dependencies:** None — zero outward references
- **Key Rule:** Must NOT reference EF Core, ASP.NET, or any external packages

#### **Application Layer** (`Chubb.PolicyManagement.Application`)
- **Purpose:** Orchestration, validation, and DTO mapping
- **Contents:**
  - `PolicyService` — business logic for list, detail, flag, summary operations
  - DTOs (`PolicyDto`, `PolicyFilterQuery`, `BulkFlagRequest`, `PolicySummaryDto`)
  - Validators (`PolicyFilterQueryValidator`, `BulkFlagRequestValidator`) — FluentValidation
  - Response models (`PagedResult<T>`)
- **Dependencies:** Domain layer only
- **Key Rule:** Must NOT reference EF Core or Infrastructure — only domain

#### **Infrastructure Layer** (`Chubb.PolicyManagement.Infrastructure`)
- **Purpose:** Data persistence, migrations, repository implementations
- **Contents:**
  - `PolicyManagementDbContext` — EF Core DB context with entity configurations
  - `PolicyRepository` — IRepository implementation
  - EF Core migrations (code-first)
  - Seeding data (200+ realistic APAC policy records)
- **Dependencies:** Domain + Application layers
- **Key Rule:** Contains EF Core but no HTTP/ASP.NET references

#### **API Layer** (`Chubb.PolicyManagement.Api`)
- **Purpose:** HTTP entry point, request/response handling, routing
- **Contents:**
  - `PoliciesController` — endpoints for list, detail, flag, summary
  - `GlobalExceptionMiddleware` — centralized error handling (RFC 7807)
  - Security middleware (JWT, CORS, security headers)
  - Swagger/OpenAPI documentation
  - `Program.cs` — dependency injection wiring
- **Dependencies:** All layers (coordinates all concerns)
- **Key Rule:** No business logic — routes requests and responses only

---

## Component Interaction

### Request Flow: GET /api/v1/policies

```
┌─────────────┐
│   Browser   │
│ (Angular UI)│
└──────┬──────┘
       │ GET /api/v1/policies?status=Active&page=1
       ▼
┌──────────────────────────────┐
│  API: PoliciesController     │  Parse query params, validate
├──────────────────────────────┤
│  .GetPoliciesAsync()         │
└──────┬───────────────────────┘
       │ Create PolicyFilterQuery(status, page, size, sort, search)
       ▼
┌──────────────────────────────┐
│  Application: PolicyService  │  Apply business logic, map to DTOs
├──────────────────────────────┤
│  .GetPoliciesAsync()         │
└──────┬───────────────────────┘
       │ Call IRepository.GetPoliciesAsync(filter)
       ▼
┌──────────────────────────────┐
│  Infrastructure: Repository  │  EF Core query builder
├──────────────────────────────┤
│  .GetPoliciesAsync()         │
│  (IQueryable, .AsNoTracking) │
└──────┬───────────────────────┘
       │ SELECT * FROM Policies WHERE Status = 'Active' ORDER BY CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
       ▼
┌──────────────────────────────┐
│  SQL Server 2022             │  Execute query, return rows
│  Policies Table              │
└──────┬───────────────────────┘
       │ Return 20 Policy rows (plus totalCount for pagination)
       ▼
┌──────────────────────────────┐
│  Repository: Map to DTOs     │  AutoMapper: Entity → PolicyDto
└──────┬───────────────────────┘
       │ Return PagedResult<PolicyDto>
       ▼
┌──────────────────────────────┐
│  Controller: HTTP Response   │  200 OK + JSON body
├──────────────────────────────┤
│  Content-Type: application/json
│  Pagination headers (X-Total-Count, X-Total-Pages)
└──────┬───────────────────────┘
       │ { "data": [...], "page": 1, "size": 20, "totalCount": 250, "totalPages": 13 }
       ▼
┌─────────────┐
│   Browser   │  Render policy table with data binding
│ (Angular UI)│
└─────────────┘
```

### Bulk Flagging: PATCH /api/v1/policies/flag

```
┌─────────────┐
│   Browser   │
│ (Angular UI)│
└──────┬──────┘
       │ PATCH /api/v1/policies/flag { "policyIds": ["id1", "id2", ...] }
       ▼
┌──────────────────────────────┐
│  API: PoliciesController     │  Parse request body, validate via FluentValidation
├──────────────────────────────┤
│  .FlagPoliciesAsync()        │
└──────┬───────────────────────┘
       │ Create BulkFlagRequest, run validator
       ▼
┌──────────────────────────────┐
│  Application: PolicyService  │  Business logic: fetch, update, transaction
├──────────────────────────────┤
│  .FlagPoliciesAsync()        │
└──────┬───────────────────────┘
       │ USING (DbContext.BeginTransaction()) {
       │   - Load policies by ID
       │   - Update FlaggedForReview = true
       │   - Set UpdatedAt = now
       │   - SaveChangesAsync()
       │   - Commit transaction
       │ }
       ▼
┌──────────────────────────────┐
│  Infrastructure: Repository  │  EF Core transaction scope
├──────────────────────────────┤
│  .FlagPoliciesAsync()        │
└──────┬───────────────────────┘
       │ UPDATE Policies SET FlaggedForReview = 1, UpdatedAt = @now WHERE Id IN (...)
       ▼
┌──────────────────────────────┐
│  SQL Server 2022             │  ACID transaction guarantee
└──────┬───────────────────────┘
       │ ROWS AFFECTED = 3
       ▼
┌──────────────────────────────┐
│  Controller: HTTP Response   │  204 No Content (or 200 OK with count)
└──────┬───────────────────────┘
       │
       ▼
┌─────────────┐
│   Browser   │  Success message, refresh list
│ (Angular UI)│
└─────────────┘
```

---

## Technology Stack Rationale

| Layer | Technology | Version | Rationale |
|---|---|---|---|
| **Backend API** | ASP.NET Core | .NET 8 | Modern, performant, enterprise-grade; long-term support (LTS); excellent tooling; C# features (records, nullable, async/await) |
| **ORM** | Entity Framework Core | 8.x | Code-first migrations; LINQ queries; relationship navigation; built-in features (change tracking, concurrency) |
| **Database** | SQL Server | 2022 | Enterprise standard; full-text search; JSON support; indexes on complex queries; Azure SQL compatible |
| **Frontend** | Angular | 17+ | Standalone components; RxJS for reactive state; TypeScript strict mode; built-in testing (Karma/Jasmine or Jest); accessibility (a11y) |
| **API Contract** | OpenAPI 3.x | — | Industry standard; auto-generates Swagger UI; enables contract-first development; client SDK generation |
| **Testing (Backend)** | xUnit + Moq + FluentAssertions | Latest | Modern assertions; fluent API; excellent integration test support via WebApplicationFactory |
| **Testing (Frontend)** | Jasmine/Jest + Karma | Latest | Angular-native testing; easy mocking via HttpClientTestingModule; component testing with async support |
| **Logging** | Serilog | Latest | Structured logging; log levels; sinks (console, file, cloud); integrates with Application Insights |
| **Containerization** | Docker + Docker Compose | Latest | Reproducible environments; local development parity with production; multi-container orchestration |

### Key Design Decisions

1. **Clean Architecture** — Enforces separation of concerns, testability, and independence from frameworks
2. **Contract-First API** — OpenAPI spec drives implementation; no surprises between frontend and backend
3. **Code-First Migrations** — Migrations tracked in version control; rollback/forward strategies; no manual SQL
4. **Async/Await Everywhere** — Non-blocking I/O; better resource utilization; responsive API
5. **Immutable DTOs** — Using C# records prevents accidental mutations; explicit immutability
6. **RFC 7807 Error Responses** — Standardized error format across all APIs; enables consistent frontend error handling
7. **Pagination at DB Level** — `.Skip().Take()` prevents loading entire tables; scales to millions of records

---

## Data Flow & Processing

### Policy Lifecycle States

```
Pending → Active → Expired
         → Cancelled (anytime)
         → (Flagged for Review — orthogonal state)
```

### Key Queries & Filtering

| Query | Purpose | Performance Constraint |
|---|---|---|
| List by Status | Filter active policies only | Indexed on `Status` |
| List by Region | Show policies in specific APAC region | Indexed on `Region` |
| List by LOB | Insurance line of business (Property, Casualty, etc.) | Indexed on `LineOfBusiness` |
| Date Range Filter | Effective date from/to | Indexed on `EffectiveDate`, `ExpiryDate` |
| Free-Text Search | Policy number, holder name, underwriter | Full-text or LIKE query (non-indexed, but acceptable for small dataset) |
| Flagged for Review | Show policies flagged by admins | Indexed on `FlaggedForReview` |

### Aggregation Pipeline

**GET /api/v1/policies/summary** returns aggregated statistics:

```
SELECT 
  COUNT(*) AS TotalPolicies,
  COUNT(CASE WHEN Status = 'Active' THEN 1 END) AS ActivePolicies,
  COUNT(CASE WHEN Status = 'Expired' THEN 1 END) AS ExpiredPolicies,
  COUNT(CASE WHEN Status = 'Pending' THEN 1 END) AS PendingPolicies,
  COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END) AS CancelledPolicies,
  SUM(CASE WHEN LineOfBusiness = 'Property' THEN PremiumAmount ELSE 0 END) AS PropertyPremium,
  COUNT(DISTINCT Region) AS UniqueRegions,
  COUNT(CASE WHEN FlaggedForReview = 1 THEN 1 END) AS FlaggedCount
FROM Policies
```

---

## Security Architecture

### Authentication & Authorization

- **JWT Bearer Tokens** — Stateless authentication; `Authorization: Bearer <token>`
- **Token Claims** — Carry user identity, roles, permissions
- **Refresh Tokens** — Long-lived, rotated; short-lived access tokens (15 min)
- **Token Validation Middleware** — Validates signature, expiration, claims before reaching controllers

### API Security

| Control | Implementation |
|---|---|
| **HTTPS Only** | TLS 1.3; self-signed certs in dev, real certs in prod |
| **CORS Policy** | Whitelist frontend origin; reject cross-origin requests from unknown hosts |
| **Security Headers** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security` |
| **Input Validation** | FluentValidation at API boundary; model state validation; sanitization |
| **SQL Injection Prevention** | Parameterized EF Core queries; never raw string concatenation |
| **Rate Limiting** | Per-IP throttling (optional, via middleware); 100 req/min per client |
| **Audit Logging** | All operations logged (user, timestamp, action, IP) for compliance |

### Data Protection

- **At Rest** — SQL Server Transparent Data Encryption (TDE); encrypted backups
- **In Transit** — TLS 1.3 encryption; HTTPS only
- **PII Handling** — Policyholder names, contact details encrypted; no PII in logs or error messages
- **Secret Management** — Secrets (DB password, JWT key) stored in environment variables or Azure Key Vault; never hardcoded

### OWASP Top 10 Mitigations

| OWASP Risk | Mitigation |
|---|---|
| **A1: Broken Access Control** | JWT validation, role-based authorization, policy-based checks |
| **A2: Cryptographic Failures** | TLS 1.3, AES-256 encryption for sensitive data at rest |
| **A3: Injection** | Parameterized EF Core queries, input validation |
| **A4: SSRF** | API does not make external HTTP calls (self-contained) |
| **A5: Broken Object Level Access Control** | Validates user owns/can access policy before returning data |
| **A6: Sensitive Data Exposure** | No sensitive data in logs, error responses, or URLs |
| **A7: Broken Authentication** | JWT with expiration, refresh token rotation |
| **A8: Broken Authorization** | Claims-based authorization; explicit permission checks |
| **A9: Insecure Deserialization** | JSON deserialization via trusted types only; no dynamic code execution |
| **A10: Insufficient Logging & Monitoring** | Serilog structured logging; correlation IDs for request tracing |

---

## Scalability & Performance

### Database Optimization

| Optimization | Benefit |
|---|---|
| **Indexes on Filter Columns** | `Status`, `Region`, `LineOfBusiness`, `FlaggedForReview`, `PolicyNumber` (unique) |
| **Pagination at DB Level** | `.Skip(offset).Take(pageSize)` limits rows returned |
| **Query Optimization** | Use `.AsNoTracking()` for read-only queries; reduces EF Core overhead |
| **Denormalization (if needed)** | Summary table for aggregations; refreshed periodically |
| **Connection Pooling** | EF Core manages connection pool; tuned for concurrent users |

### API Performance

| Strategy | Implementation |
|---|---|
| **Response Caching** | HTTP caching headers for GET /summary (5-min cache) |
| **Request Compression** | gzip/brotli compression on responses |
| **Async Processing** | All I/O operations async; no thread blocking |
| **Batch Operations** | Bulk flag endpoint processes multiple policies in one transaction |
| **Error Handling Efficiency** | Global exception middleware catches errors once, formats once |

### Horizontal Scaling

- **Stateless API** — No session state; each request independent; can run multiple instances
- **Shared DB** — All instances connect to single SQL Server; no data duplication
- **Load Balancing** — Put behind Azure Load Balancer or Nginx; round-robin traffic
- **Caching Strategy** — Redis (if needed) for distributed cache; JWT tokens cached by client

### Expected Performance Targets

- **P50 Latency:** < 100ms for list queries (20 items)
- **P99 Latency:** < 500ms for complex filters
- **Throughput:** 1000+ concurrent users
- **Database:** 200+ policy records; linear query time O(n log n) with indexes
- **Memory:** ~100MB per API instance (baseline)

---

## Deployment Architecture

### Local Development

```
docker-compose up --build

Services:
  - db: SQL Server 2022 (port 1433, sa/YourPassword123)
  - api: ASP.NET Core (port 5000, http://localhost:5000/swagger)
  - frontend: Angular (port 4200, http://localhost:4200)
```

### Staging & Production

```
┌──────────────────────────────────────────────────┐
│  Azure / Cloud Provider                          │
├──────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────┐  │
│  │  Azure Container Registry (ACR)            │  │
│  │  - api:latest, frontend:latest             │  │
│  └────────────────────────────────────────────┘  │
│         ▲                                         │
│         │ push image                              │
│  ┌──────┴──────────────────────────────────────┐ │
│  │  CI/CD Pipeline (GitHub Actions)           │ │
│  │  - Run tests, build images, scan for CVE   │ │
│  └────────────────────────────────────────────┘ │
│         ▲                                         │
│         │ git push to main                        │
│  ┌──────┴──────────────────────────────────────┐ │
│  │  GitHub Repository (ksvishwa/...)          │ │
│  │  - Dockerfile, docker-compose.yml          │ │
│  └────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
         │
         │ pulls from ACR
         ▼
┌──────────────────────────────────────────────────┐
│  Azure Container Instances / AKS                 │
├──────────────────────────────────────────────────┤
│  api (3x replicas)  ← Load Balancer              │
│  frontend (1x nginx)                             │
│  db: Azure SQL Managed Instance (HA, backups)    │
└──────────────────────────────────────────────────┘
```

### Health Checks & Monitoring

- **API Health Endpoint:** `GET /health` — returns DB connectivity status
- **Liveness Probe:** Container restart if health check fails 3x
- **Readiness Probe:** Remove from load balancer if not ready
- **Application Insights:** Logs, metrics, dependency tracking
- **Alerts:** CPU, memory, DB connection errors; Slack/email notifications

---

## Summary

The **Chubb APAC Policy Management Platform** is a modern, scalable, secure system built on:

1. **Clean Architecture** — Enforced layer separation; testable; framework-independent
2. **Contract-First API** — OpenAPI driving implementation; Swagger UI for exploration
3. **Enterprise-Grade Tech Stack** — .NET 8, Angular 17+, SQL Server 2022, Docker
4. **Security by Default** — JWT, HTTPS, parameterized queries, audit logging
5. **Performance Optimized** — Indexed DB, pagination, async/await, caching
6. **Operationally Ready** — Health checks, structured logging, Docker deployment

This design supports **200+ policy records** in initial release and scales to **millions** with index optimization, caching, and read replicas.
