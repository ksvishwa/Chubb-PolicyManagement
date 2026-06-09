---
name: openapi-api-contract-design
description: >
  Design and validate OpenAPI 3.x REST API contracts for the Chubb APAC Policy Management Platform.
  Covers endpoint design, request/response schemas, RFC 7807 error responses, and contract-first
  alignment with ASP.NET Core controllers and DTOs.
---

# OpenAPI API Contract Design

## When to Use

Invoke this skill when asked to:
- Design or review REST API endpoints
- Define request/response DTOs and schemas
- Document error responses (400, 404, 422, 500)
- Align controller implementation against the OpenAPI spec
- Add pagination, filtering, or sorting parameters
- Design bulk operation endpoints (e.g., flag policies)

**Trigger phrases**: "define endpoint", "API contract", "request schema", "response schema", "swagger", "openapi", "RFC 7807 error"

---

## Prerequisites

- OpenAPI 3.x specification syntax
- ASP.NET Core controller patterns
- RFC 7807 Problem Details standard
- Read `.github/instructions/exception-handling.instructions.md` for error handling rules

---

## Required Endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/v1/policies` | Paged, filtered, sorted list |
| `GET` | `/api/v1/policies/{id}` | Single policy by UUID |
| `PATCH` | `/api/v1/policies/flag` | Bulk flag for review |
| `GET` | `/api/v1/policies/summary` | Aggregated statistics |

---

## Step 1 - OpenAPI Specification

**File**: `docs/chub-api.yaml`

### Info Block

```yaml
openapi: 3.0.3
info:
  title: Chubb APAC Policy Management API
  version: 1.0.0
  description: REST API for managing APAC insurance policies
servers:
  - url: http://localhost:5000
    description: Local development
```

### GET /api/v1/policies

```yaml
paths:
  /api/v1/policies:
    get:
      summary: List policies with pagination, filtering, and sorting
      operationId: getPolicies
      tags: [Policies]
      parameters:
        - { name: page, in: query, schema: { type: integer, default: 1 } }
        - { name: size, in: query, schema: { type: integer, default: 20, maximum: 100 } }
        - { name: sort, in: query, schema: { type: string, default: "createdAt,desc" } }
        - { name: status, in: query, schema: { type: string, enum: [Active, Expired, Pending, Cancelled] } }
        - { name: lineOfBusiness, in: query, schema: { type: string, enum: [Property, Casualty, "A&H", Marine] } }
        - { name: region, in: query, schema: { type: string } }
        - { name: effectiveDateFrom, in: query, schema: { type: string, format: date } }
        - { name: effectiveDateTo, in: query, schema: { type: string, format: date } }
        - { name: search, in: query, schema: { type: string }, description: "Free-text across policyNumber, policyholderName, underwriter" }
      responses:
        '200':
          description: Paginated list of policies
          content:
            application/json:
              schema: { $ref: '#/components/schemas/PolicyListResponse' }
        '400':
          description: Invalid query parameters
          content:
            application/problem+json:
              schema: { $ref: '#/components/schemas/ProblemDetails' }
        '500':
          description: Internal server error
          content:
            application/problem+json:
              schema: { $ref: '#/components/schemas/ProblemDetails' }
```

### GET /api/v1/policies/{id}

```yaml
  /api/v1/policies/{id}:
    get:
      summary: Get single policy by ID
      operationId: getPolicyById
      tags: [Policies]
      parameters:
        - { name: id, in: path, required: true, schema: { type: string, format: uuid } }
      responses:
        '200':
          content:
            application/json:
              schema: { $ref: '#/components/schemas/PolicyDto' }
        '404':
          content:
            application/problem+json:
              schema: { $ref: '#/components/schemas/ProblemDetails' }
```

### PATCH /api/v1/policies/flag

```yaml
  /api/v1/policies/flag:
    patch:
      summary: Bulk flag policies for review
      operationId: flagPoliciesForReview
      tags: [Policies]
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: '#/components/schemas/BulkFlagRequest' }
      responses:
        '200':
          content:
            application/json:
              schema: { $ref: '#/components/schemas/BulkFlagResponse' }
        '422':
          content:
            application/problem+json:
              schema: { $ref: '#/components/schemas/ProblemDetails' }
```

### GET /api/v1/policies/summary

```yaml
  /api/v1/policies/summary:
    get:
      summary: Get aggregated policy statistics
      operationId: getPolicySummary
      tags: [Policies]
      parameters:
        - { name: region, in: query, schema: { type: string } }
      responses:
        '200':
          content:
            application/json:
              schema: { $ref: '#/components/schemas/PolicySummaryDto' }
```

---

## Step 2 - Component Schemas

All schemas must be `$ref` components — no inline schemas.

```yaml
components:
  schemas:
    PolicyDto:
      type: object
      required: [id, policyNumber, status, lineOfBusiness, region, effectiveDate, policyholderName, underwriter, premiumAmount, createdAt]
      properties:
        id:           { type: string, format: uuid }
        policyNumber: { type: string, maxLength: 50 }
        status:       { type: string, enum: [Active, Expired, Pending, Cancelled] }
        lineOfBusiness: { type: string, enum: [Property, Casualty, "A&H", Marine] }
        region:       { type: string, maxLength: 100 }
        effectiveDate: { type: string, format: date }
        expiryDate:   { type: string, format: date, nullable: true }
        policyholderName: { type: string, maxLength: 200 }
        underwriter:  { type: string, maxLength: 200 }
        premiumAmount: { type: number, format: decimal }
        flaggedForReview: { type: boolean }
        createdAt:    { type: string, format: date-time }
        updatedAt:    { type: string, format: date-time, nullable: true }

    PolicyListResponse:
      type: object
      required: [data, page, size, totalCount, totalPages]
      properties:
        data:        { type: array, items: { $ref: '#/components/schemas/PolicyDto' } }
        page:        { type: integer, minimum: 1 }
        size:        { type: integer, minimum: 1, maximum: 100 }
        totalCount:  { type: integer, minimum: 0 }
        totalPages:  { type: integer, minimum: 0 }

    BulkFlagRequest:
      type: object
      required: [policyIds]
      properties:
        policyIds:
          type: array
          items: { type: string, format: uuid }
          minItems: 1
          maxItems: 1000

    BulkFlagResponse:
      type: object
      required: [flaggedCount, failedCount, message]
      properties:
        flaggedCount: { type: integer }
        failedCount:  { type: integer }
        message:      { type: string }

    PolicySummaryDto:
      type: object
      properties:
        totalPolicies:      { type: integer }
        activePolicies:     { type: integer }
        expiredPolicies:    { type: integer }
        pendingPolicies:    { type: integer }
        cancelledPolicies:  { type: integer }
        totalPremiumAmount: { type: number }
        flaggedForReview:   { type: integer }

    ProblemDetails:
      type: object
      required: [type, title, status, detail]
      properties:
        type:     { type: string, format: uri }
        title:    { type: string }
        status:   { type: integer }
        detail:   { type: string }
        traceId:  { type: string }
        errors:
          type: object
          additionalProperties:
            type: array
            items: { type: string }
```

---

## Step 3 - DTOs in Application Layer

**File**: `src/backend/Chubb.PolicyManagement.Application/DTOs/`

```csharp
// PolicyDto.cs
public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    string Status,
    string LineOfBusiness,
    string Region,
    DateTime EffectiveDate,
    DateTime? ExpiryDate,
    string PolicyholderName,
    string Underwriter,
    decimal PremiumAmount,
    bool FlaggedForReview,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// BulkFlagRequest.cs
public record BulkFlagRequest(IEnumerable<Guid> PolicyIds);

// BulkFlagResponse.cs
public record BulkFlagResponse(int FlaggedCount, int FailedCount, string Message);

// PolicySummaryDto.cs
public record PolicySummaryDto(
    int TotalPolicies,
    int ActivePolicies,
    int ExpiredPolicies,
    int PendingPolicies,
    int CancelledPolicies,
    decimal TotalPremiumAmount,
    int FlaggedForReview);

// PagedResult.cs
public record PagedResult<T>(
    IEnumerable<T> Data,
    int Page,
    int Size,
    int TotalCount,
    int TotalPages);
```

---

## Step 4 - Controller with ProducesResponseType

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PoliciesController : ControllerBase
{
    /// <summary>List policies with pagination, filtering, and sorting.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<PolicyDto>>> GetPolicies([FromQuery] PolicyFilterQuery filter, CancellationToken ct) { ... }

    /// <summary>Get a single policy by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyDto>> GetPolicyById(Guid id, CancellationToken ct) { ... }

    /// <summary>Bulk flag policies for review.</summary>
    [HttpPatch("flag")]
    [ProducesResponseType(typeof(BulkFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BulkFlagResponse>> FlagPoliciesForReview([FromBody] BulkFlagRequest request, CancellationToken ct) { ... }

    /// <summary>Get aggregated policy statistics.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PolicySummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicySummaryDto>> GetPolicySummary([FromQuery] string? region, CancellationToken ct) { ... }
}
```

---

## RFC 7807 Error Response Examples

### 404 Not Found
```json
{
  "type": "https://api.chubb.example.com/errors/not-found",
  "title": "Policy Not Found",
  "status": 404,
  "detail": "Policy with ID '550e8400-e29b-41d4-a716-446655440000' was not found.",
  "traceId": "0HN7K2GKFHB6V:00000001"
}
```

### 422 Validation Error
```json
{
  "type": "https://api.chubb.example.com/errors/validation-failed",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "policyIds": ["PolicyIds must contain at least 1 item."],
    "page": ["Page must be >= 1."]
  },
  "traceId": "0HN7K2GKFHB6V:00000002"
}
```

---

## Checklist

- [ ] All endpoints defined in `docs/chub-api.yaml` with `summary` and `operationId`
- [ ] All schemas referenced via `$ref` components — no inline schemas
- [ ] All error responses use `ProblemDetails` schema
- [ ] Controller methods annotated with `[ProducesResponseType]`
- [ ] DTOs defined as immutable `record` types in `Application/DTOs/`
- [ ] XML doc comments on all controller methods (consumed by Swagger)
- [ ] HTTP status codes 200, 400, 404, 422, 500 all documented
- [ ] Pagination response always includes `page, size, totalCount, totalPages`
