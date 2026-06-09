---
description: >
  Use when Copilot generates, adds, or modifies any backend C# code — controllers, services,
  repositories, middleware, or domain logic. Enforces mandatory exception handling standards:
  RFC 7807 Problem Details, global middleware, domain exceptions, structured logging, and
  no stack trace leakage in production responses.
applyTo: "src/backend/**/*.cs"
---

# Exception Handling — Mandatory Backend Standards

## Rule

**Every backend method that performs I/O, calls external services, or executes business logic MUST have proper exception handling.**

No unhandled exceptions may bubble up to the HTTP response as raw 500 errors without structured error details.

---

## Global Exception Middleware

- A single `GlobalExceptionMiddleware` in the `Api` layer catches **all unhandled exceptions**.
- It must always return **RFC 7807 Problem Details** — never plain text or raw exception messages.
- Register it as the **first middleware** in `Program.cs` so it wraps the entire pipeline.

```csharp
// Program.cs — must be first
app.UseMiddleware<GlobalExceptionMiddleware>();
```

### Required RFC 7807 response shape
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Policy with ID '3fa85f64' was not found.",
  "traceId": "00-abc123-def456-00"
}
```

---

## Domain Exceptions

- Define a specific exception class in `Domain/Exceptions/` for every distinct error case.
- Never throw `Exception` or `ApplicationException` directly — always use domain-specific types.
- Domain exceptions must **not** reference EF Core, ASP.NET, or infrastructure packages.

### Required domain exceptions (minimum)
| Exception Class | HTTP Status | When to Throw |
|---|---|---|
| `PolicyNotFoundException` | `404 Not Found` | Policy ID does not exist |
| `PolicyValidationException` | `400 Bad Request` | Invalid input data |
| `DuplicatePolicyException` | `409 Conflict` | PolicyNumber already exists |

### Template for new domain exceptions
```csharp
namespace Chubb.PolicyManagement.Domain.Exceptions;

public sealed class PolicyNotFoundException : Exception
{
    public PolicyNotFoundException(Guid id)
        : base($"Policy with ID '{id}' was not found.") { }
}
```

---

## Middleware — Exception-to-Status Mapping

Map every domain exception to its HTTP status code inside `GlobalExceptionMiddleware`:

```csharp
private static int GetStatusCode(Exception exception) => exception switch
{
    PolicyNotFoundException      => StatusCodes.Status404NotFound,
    PolicyValidationException    => StatusCodes.Status400BadRequest,
    DuplicatePolicyException     => StatusCodes.Status409Conflict,
    OperationCanceledException   => StatusCodes.Status499ClientClosedRequest,
    _                            => StatusCodes.Status500InternalServerError
};
```

---

## Service Layer

- Wrap all repository calls in `try/catch` only when you need to **translate** exceptions (e.g., EF Core `DbUpdateException` → `DuplicatePolicyException`).
- Do **not** swallow exceptions silently — either re-throw, translate to a domain exception, or let the global middleware handle them.
- Always pass `CancellationToken` and handle `OperationCanceledException`.

```csharp
// Correct — translate infrastructure exception to domain exception
public async Task CreateAsync(CreatePolicyDto dto, CancellationToken ct)
{
    try
    {
        await _repository.AddAsync(policy, ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
    {
        throw new DuplicatePolicyException(dto.PolicyNumber);
    }
}
```

---

## Controller Layer

- Controllers must **not** contain `try/catch` blocks — delegate all exception handling to middleware.
- Use `[ProducesResponseType]` attributes to document all possible error responses for Swagger.

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
{
    // No try/catch — PolicyNotFoundException bubbles to GlobalExceptionMiddleware
    var policy = await _policyService.GetByIdAsync(id, ct);
    return Ok(policy);
}
```

---

## Logging Standards

- Log **every exception** at the appropriate level using `ILogger<T>`:
  - Domain exceptions (4xx) → `LogWarning`
  - Unexpected exceptions (5xx) → `LogError`
- **Never** log sensitive data — no full connection strings, no PII, no request body dumps.
- Always include `traceId` in log entries for correlation.

```csharp
// In GlobalExceptionMiddleware
_logger.LogWarning(exception, "Domain exception occurred. TraceId: {TraceId}", traceId);
_logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
```

---

## Validation Errors

- Use ASP.NET Core's built-in model validation (`[ApiController]` auto-returns `400` with `ValidationProblemDetails`).
- For business-rule validation failures (not model binding), throw `PolicyValidationException` with field-level detail.
- Validation errors must list **which fields** failed, not just a generic message.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "effectiveDate": ["Effective date must be before expiry date."]
  }
}
```

---

## Rules Checklist

1. **No bare `throw new Exception()`** — always use a named domain exception.
2. **No empty catch blocks** — every `catch` must log, translate, or re-throw.
3. **No stack traces in HTTP responses** — only in server-side logs.
4. **No `catch (Exception ex)` in controllers** — that is the middleware's job.
5. **Every new exception type** must be added to the middleware's status-code mapping.
6. **Every async method** must accept and respect `CancellationToken`.
