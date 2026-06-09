# Architecture Decision Records (ADRs)

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [ADR-001: Clean Architecture](#adr-001-clean-architecture)
2. [ADR-002: Contract-First API Design](#adr-002-contract-first-api-design)
3. [ADR-003: EF Core Code-First Migrations](#adr-003-ef-core-code-first-migrations)
4. [ADR-004: Async/Await Throughout](#adr-004-asyncawait-throughout)
5. [ADR-005: RFC 7807 Problem Details Error Handling](#adr-005-rfc-7807-problem-details-error-handling)
6. [ADR-006: Immutable DTOs with C# Records](#adr-006-immutable-dtos-with-c-records)
7. [ADR-007: Repository Pattern with Dependency Injection](#adr-007-repository-pattern-with-dependency-injection)
8. [ADR-008: Pagination at Database Level](#adr-008-pagination-at-database-level)
9. [ADR-009: JWT Authentication](#adr-009-jwt-authentication)
10. [ADR-010: Angular Standalone Components](#adr-010-angular-standalone-components)

---

## ADR-001: Clean Architecture

### Status
**ACCEPTED**

### Context
The platform spans multiple domains (policy management, search, aggregation) with potential for future features (pricing, claims). We need clear separation of concerns to:
- Enable testing of business logic independently from HTTP/DB
- Support multiple presentation layers (web, API, CLI)
- Minimize framework coupling
- Enable easy refactoring

### Decision
Adopt Clean Architecture with strict, inward-pointing dependency rules:

```
Domain (no dependencies) ← Application (depends on Domain only) 
  ← Infrastructure (depends on Domain + Application)
  ← API (depends on all, wires DI)
```

### Rationale
1. **Testability** — Business logic tested without HTTP/DB frameworks
2. **Flexibility** — Replace SQL Server with PostgreSQL, Express with ASP.NET, etc., with minimal impact
3. **Clarity** — Dependency graph is explicit; layering violations caught at compile time
4. **Industry Standard** — Proven pattern used at Microsoft, Amazon, Google

### Consequences
- **Positive:** High testability, clear architecture, framework independence
- **Negative:** More files/layers; requires discipline to enforce; slightly slower initial development
- **Mitigation:** Automated architecture tests verify dependency rules

### Related Decisions
- ADR-007 (Dependency Injection enforcement)

---

## ADR-002: Contract-First API Design

### Status
**ACCEPTED**

### Context
Frontend and backend teams need to work in parallel. Without a contract, they may implement incompatible schemas or endpoints, causing integration delays.

### Decision
Write OpenAPI 3.x specification first (`docs/openapi.yaml`), then implement API to match spec exactly. Frontend generates TypeScript types from spec.

### Rationale
1. **Parallel Development** — Frontend mocks API; backend implements to contract
2. **Documentation** — Swagger UI auto-generated; always in sync with implementation
3. **Governance** — Changes to contract reviewed before implementation
4. **Client SDK Generation** — Tools (NSwag, Swagger Codegen) generate SDKs automatically

### Consequences
- **Positive:** Reduced integration issues, clear API contract, auto-documentation
- **Negative:** Upfront effort to write spec; spec changes require code changes
- **Mitigation:** Validate spec during CI; use OpenAPI linter

### Example Endpoint
```yaml
/api/v1/policies:
  get:
    summary: Get paginated list of policies
    parameters:
      - name: page
        in: query
        schema: { type: integer, minimum: 1, default: 1 }
      - name: status
        in: query
        schema: { type: string, enum: [Active, Expired, Pending, Cancelled] }
    responses:
      '200':
        description: Success
        content:
          application/json:
            schema: { $ref: '#/components/schemas/PagedResult' }
      '400':
        description: Validation Error
        content:
          application/json:
            schema: { $ref: '#/components/schemas/ProblemDetails' }
```

### Related Decisions
- ADR-002 (Contract-First API Design)

---

## ADR-003: EF Core Code-First Migrations

### Status
**ACCEPTED**

### Context
We need to:
- Version database schema alongside code
- Rollback/roll-forward migrations if needed
- Enable multiple environments (dev, staging, prod) to be in sync
- Avoid manual SQL scripts

### Decision
Use EF Core Code-First migrations:
1. Define entities as C# classes
2. Run `dotnet ef migrations add MigrationName`
3. EF Core generates migration files (UP/DOWN)
4. Migrations tracked in git alongside code
5. API applies migrations on startup (dev only); CI/CD pipeline for prod

### Rationale
1. **Version Control** — Migrations are code; full history in git
2. **Automation** — No manual SQL; risk of human error reduced
3. **Rollback** — `dotnet ef database update {PreviousVersion}` reverses migrations
4. **Auditability** — Who changed what and when is clear from commit history

### Consequences
- **Positive:** Reproducible schema changes, version control, rollback capability
- **Negative:** Migrations must be linear; rebasing history tricky; complex migrations need SQL
- **Mitigation:** Use `Sql()` method for complex migrations; review migrations in PRs

### Example Migration
```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Policies",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PolicyNumber = table.Column<string>(maxLength: 50, nullable: false),
                Status = table.Column<string>(nullable: false),
                // ... more columns
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Policies_Id", x => x.Id);
                table.UniqueConstraint("UX_Policies_PolicyNumber", x => x.PolicyNumber);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_Policies_Status",
            table: "Policies",
            column: "Status");
    }
}
```

### Related Decisions
- None

---

## ADR-004: Async/Await Throughout

### Status
**ACCEPTED**

### Context
I/O operations (DB, HTTP) are the bottleneck in web applications. Synchronous I/O blocks threads, reducing throughput.

### Decision
Use async/await for all I/O operations:
- Repository methods return `Task<T>`
- Service methods return `Task<T>`
- Controller actions return `Task<IActionResult>`
- Never use `.Result` or `.Wait()` — guaranteed deadlock in ASP.NET Core

### Rationale
1. **Throughput** — One thread can handle hundreds of concurrent requests
2. **Responsiveness** — No thread starvation; all threads available for processing
3. **Scalability** — Handle more load with fewer resources
4. **Industry Standard** — Modern .NET/async/await expected

### Consequences
- **Positive:** Better scalability, responsiveness, resource utilization
- **Negative:** Learning curve for developers new to async; harder to debug; more context switching
- **Mitigation:** Code review for `.Result`/`.Wait()` usage; educate team on async patterns

### Example
```csharp
// ✅ Correct
public async Task<PolicyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var policy = await _repository.GetByIdAsync(id, cancellationToken);
    return _mapper.Map<PolicyDto>(policy);
}

// ❌ Incorrect (deadlock risk)
public PolicyDto? GetById(Guid id)
{
    var policy = _repository.GetByIdAsync(id).Result;  // ❌ DEADLOCK!
    return _mapper.Map<PolicyDto>(policy);
}
```

### Related Decisions
- ADR-008 (Pagination)

---

## ADR-005: RFC 7807 Problem Details Error Handling

### Status
**ACCEPTED**

### Context
Clients (frontend, mobile, third-party APIs) need consistent, machine-readable error responses. HTML error pages are useless for JSON APIs.

### Decision
All API errors return RFC 7807 Problem Details JSON:
```json
{
  "type": "https://api.example.com/errors/policy-not-found",
  "title": "Policy Not Found",
  "status": 404,
  "detail": "Policy with ID 123e4567-e89b-12d3-a456-426614174000 not found.",
  "instance": "/api/v1/policies/123e4567-e89b-12d3-a456-426614174000"
}
```

### Rationale
1. **Standardization** — RFC 7807 is W3C standard; clients know how to parse it
2. **Machine-Readable** — Type/status/detail enable programmatic error handling
3. **User-Friendly** — Detail explains what went wrong; hint for retry logic
4. **Traceability** — Instance includes path; combined with logs, fully traceable

### Consequences
- **Positive:** Consistent error format, easier client-side error handling, standardized
- **Negative:** Slightly larger error payloads (negligible)
- **Mitigation:** Validate error responses in integration tests

### Implementation
Global exception middleware catches all exceptions, maps to Problem Details, returns RFC 7807.

### Related Decisions
- None

---

## ADR-006: Immutable DTOs with C# Records

### Status
**ACCEPTED**

### Context
DTOs cross layer boundaries (API → Application → Domain). Mutations in one layer affect others, causing bugs. We need immutability guarantees.

### Decision
Use C# records (not classes) for all DTOs:
```csharp
public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    string PolicyholderName,
    // ... more properties
);
```

### Rationale
1. **Immutability** — Records are immutable by default; no accidental mutations
2. **Structural Equality** — Two records with same values are equal; simplifies testing
3. **Conciseness** — Records auto-generate `Equals()`, `GetHashCode()`, `ToString()`
4. **Thread-Safety** — Immutable objects are inherently thread-safe; no race conditions

### Consequences
- **Positive:** Immutability guarantees, concise code, structural equality
- **Negative:** C# 9+; unfamiliar to some teams; records are positional (can be error-prone)
- **Mitigation:** Use named records; educate team on record syntax

### Example
```csharp
// ✅ Correct — immutable
public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    string PolicyholderName);

var dto1 = new PolicyDto(Guid.NewGuid(), "POL-001", "John");
var dto2 = new PolicyDto(Guid.NewGuid(), "POL-001", "John");
Assert.Equal(dto1, dto2);  // ✅ Same values, equal

// ❌ Incorrect — mutable class
public class PolicyDto
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; }
}
var dto = new PolicyDto { Id = Guid.NewGuid() };
dto.PolicyNumber = null;  // ❌ Allows mutation
```

### Related Decisions
- ADR-001 (Clean Architecture — DTOs in Application layer)

---

## ADR-007: Repository Pattern with Dependency Injection

### Status
**ACCEPTED**

### Context
Business logic (services) should not depend on EF Core directly. This couples business logic to database implementation, making testing hard.

### Decision
1. Define `IRepository` interface in Domain layer (contract only)
2. Implement `Repository` in Infrastructure layer (EF Core details)
3. Inject `IRepository` into services via constructor DI
4. Services depend on `IRepository`, not concrete implementation

### Rationale
1. **Testability** — Mock `IRepository` in unit tests; no DB needed
2. **Flexibility** — Replace SQL Server with PostgreSQL by changing implementation only
3. **Separation of Concerns** — Business logic (service) knows nothing about persistence
4. **Dependency Inversion** — High-level modules depend on abstractions, not low-level modules

### Consequences
- **Positive:** Testability, flexibility, clean dependencies
- **Negative:** Extra interface layer; slight indirection
- **Mitigation:** Use IDE refactoring to extract interfaces; document pattern

### Example
```csharp
// Domain/Interfaces/IPolicyRepository.cs
public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IList<Policy>, int)> GetPoliciesAsync(PolicyFilterQuery filter, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/PolicyRepository.cs
public class PolicyRepository : IPolicyRepository
{
    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Policies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}

// Application/Services/PolicyService.cs
public class PolicyService
{
    public PolicyService(IPolicyRepository repository) => _repository = repository;  // Inject interface, not concrete
}

// Program.cs (DI wiring)
services.AddScoped<IPolicyRepository, PolicyRepository>();  // Wire concrete implementation
```

### Related Decisions
- ADR-001 (Clean Architecture)

---

## ADR-008: Pagination at Database Level

### Status
**ACCEPTED**

### Context
The policies table may grow to thousands or millions of rows. Loading all rows into memory is wasteful; pagination limits results.

### Decision
Apply pagination at SQL level using `.Skip(offset).Take(pageSize)`:
```csharp
var offset = (page - 1) * pageSize;
var policies = await query
    .Skip(offset)
    .Take(pageSize)
    .ToListAsync();
```

NOT: Load all rows into memory, then paginate in C#.

### Rationale
1. **Memory Efficiency** — Only page size rows in memory; independent of total table size
2. **Performance** — SQL Server can use indexes; query completes faster
3. **Scalability** — Supports tables with millions of rows without bloat

### Consequences
- **Positive:** Memory-efficient, scalable, fast
- **Negative:** Offset-based pagination has issues (if data changes between pages, can skip/duplicate rows)
- **Mitigation:** For large datasets, use keyset pagination (cursor); for this project, offset is sufficient

### Example
```csharp
// ✅ Correct — pagination at DB level
var offset = (filter.Page - 1) * filter.Size;
var policies = await query
    .Skip(offset)
    .Take(filter.Size)
    .ToListAsync();

// ❌ Incorrect — pagination in memory
var allPolicies = await query.ToListAsync();  // Load ALL rows!
var policies = allPolicies
    .Skip((filter.Page - 1) * filter.Size)
    .Take(filter.Size)
    .ToList();
```

### Related Decisions
- ADR-004 (Async/Await)

---

## ADR-009: JWT Authentication

### Status
**ACCEPTED**

### Context
APIs must authenticate users (verify identity) and authorize them (verify permissions). JWT is stateless, scalable, and industry-standard.

### Decision
Use JWT Bearer tokens:
1. Client authenticates once, receives JWT (access token) + refresh token
2. Client includes `Authorization: Bearer <jwt>` in requests
3. API validates JWT signature and expiration
4. If expired, client uses refresh token to get new JWT
5. Refresh token has longer expiration (e.g., 7 days); rotated on refresh

### Rationale
1. **Stateless** — No session storage needed; scales horizontally
2. **Portable** — JWT is standard; clients understand it
3. **Security** — Cryptographic signature ensures token not tampered
4. **Expiration** — Tokens expire; compromised token has limited window

### Consequences
- **Positive:** Stateless, scalable, standard, secure
- **Negative:** Token revocation requires blacklist/database; no logout on disconnect
- **Mitigation:** Short-lived tokens (15 min); refresh tokens in secure HTTP-only cookies

### Example
```csharp
// Program.cs
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

app.UseAuthentication();
app.UseAuthorization();

// Controller
[Authorize]
[HttpGet]
public async Task<IActionResult> GetPolicies(...)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ... authorize user
}
```

### Related Decisions
- ADR-005 (Error Handling)

---

## ADR-010: Angular Standalone Components

### Status
**ACCEPTED**

### Context
Angular 14+ introduced standalone components (no NgModules). This simplifies component architecture, reduces boilerplate, and makes components more reusable.

### Decision
Use standalone components exclusively:
- No `NgModule` declarations
- Components self-declare dependencies via `imports` array
- Services provided at root via `providedIn: 'root'`
- Routes defined at root level

### Rationale
1. **Simplicity** — Components are self-contained; no ModuleA exports ComponentB
2. **Reusability** — Components can be used in any project; no module coupling
3. **Tree-Shaking** — Unused components are removed from bundle; smaller bundle size
4. **Modern** — Industry moving toward standalone components; better tooling support

### Consequences
- **Positive:** Simpler architecture, better tree-shaking, modern
- **Negative:** Requires Angular 14+; breaking change from old NgModule style
- **Mitigation:** Provide migration guide for teams upgrading; provide component scaffold

### Example
```typescript
// ✅ Correct — standalone component
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-policy-list',
  standalone: true,  // Self-contained
  imports: [CommonModule],  // Declare dependencies
  template: `<div>{{ policies }}</div>`
})
export class PolicyListComponent {}

// ❌ Incorrect — NgModule (old style)
@NgModule({
  declarations: [PolicyListComponent],
  imports: [CommonModule]
})
export class PolicyModule {}
```

### Related Decisions
- None

---

## Summary

These ADRs document key architectural decisions and their trade-offs. Revisit periodically as requirements change or new insights emerge.
