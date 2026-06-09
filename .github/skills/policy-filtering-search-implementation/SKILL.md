---
name: policy-filtering-search-implementation
description: >
  Implement policy filtering, sorting, pagination, and free-text search for the Chubb APAC
  Policy Management Platform. Covers filter model design, FluentValidation constraints,
  efficient EF Core queries, dynamic sorting, and full test coverage.
---

# Policy Filtering & Search Implementation

## When to Use

Invoke this skill when asked to:
- Implement list endpoint filtering (status, region, LOB, date range)
- Add free-text search across policy fields
- Apply sorting and pagination to query results
- Validate query parameter constraints
- Optimize search performance with indexes

**Trigger phrases**: "filter policies", "search policies", "pagination", "sort results", "filter query", "search endpoint", "policy list"

---

## Prerequisites

- EF Core repository pattern in place (see `ef-core-database-operations` skill)
- FluentValidation NuGet package installed
- All mandatory indexes defined on entity configuration

---

## Step 1 - Filter Query Model

**File**: `src/backend/Chubb.PolicyManagement.Application/Models/PolicyFilterQuery.cs`

```csharp
namespace Chubb.PolicyManagement.Application.Models;

public class PolicyFilterQuery
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string Sort { get; set; } = "createdAt,desc";
    public string? Status { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public DateTime? EffectiveDateFrom { get; set; }
    public DateTime? EffectiveDateTo { get; set; }
    public string? Search { get; set; }
}
```

---

## Step 2 - FluentValidation Validator

**File**: `src/backend/Chubb.PolicyManagement.Application/Validators/PolicyFilterQueryValidator.cs`

```csharp
using FluentValidation;

public class PolicyFilterQueryValidator : AbstractValidator<PolicyFilterQuery>
{
    private static readonly string[] ValidStatuses = ["Active", "Expired", "Pending", "Cancelled"];
    private static readonly string[] ValidLobs = ["Property", "Casualty", "A&H", "Marine"];

    public PolicyFilterQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be >= 1");

        RuleFor(q => q.Size)
            .InclusiveBetween(1, 100).WithMessage("Size must be between 1 and 100");

        RuleFor(q => q.Status)
            .Must(s => s == null || ValidStatuses.Contains(s))
            .WithMessage("Status must be one of: Active, Expired, Pending, Cancelled");

        RuleFor(q => q.LineOfBusiness)
            .Must(l => l == null || ValidLobs.Contains(l))
            .WithMessage("LineOfBusiness must be one of: Property, Casualty, A&H, Marine");

        RuleFor(q => q.Region)
            .MaximumLength(100).WithMessage("Region must not exceed 100 characters");

        RuleFor(q => q.Search)
            .MaximumLength(200).WithMessage("Search text must not exceed 200 characters");

        RuleFor(q => q.EffectiveDateFrom)
            .LessThanOrEqualTo(q => q.EffectiveDateTo)
            .When(q => q.EffectiveDateFrom.HasValue && q.EffectiveDateTo.HasValue)
            .WithMessage("EffectiveDateFrom must be before EffectiveDateTo");

        RuleFor(q => q.Sort)
            .Must(IsValidSort)
            .WithMessage("Sort must be 'fieldName,asc' or 'fieldName,desc'");
    }

    private static bool IsValidSort(string sort)
    {
        if (string.IsNullOrEmpty(sort)) return false;
        var parts = sort.Split(',');
        return parts.Length == 2
            && !string.IsNullOrWhiteSpace(parts[0])
            && parts[1].ToLower() is "asc" or "desc";
    }
}
```

---

## Step 3 - Repository Filter & Sort Logic

**File**: `src/backend/Chubb.PolicyManagement.Infrastructure/Persistence/Repositories/PolicyRepository.cs`

```csharp
public async Task<IEnumerable<Policy>> ListAsync(PolicyFilterQuery filter, CancellationToken cancellationToken)
{
    var query = _context.Policies.AsNoTracking();
    query = ApplyFilters(query, filter);
    query = ApplySort(query, filter.Sort);
    return await query
        .Skip((filter.Page - 1) * filter.Size)
        .Take(filter.Size)
        .ToListAsync(cancellationToken);
}

public async Task<int> CountAsync(PolicyFilterQuery filter, CancellationToken cancellationToken)
{
    var query = _context.Policies.AsNoTracking();
    query = ApplyFilters(query, filter);
    return await query.CountAsync(cancellationToken);
}

private static IQueryable<Policy> ApplyFilters(IQueryable<Policy> query, PolicyFilterQuery filter)
{
    if (!string.IsNullOrEmpty(filter.Status))
        query = query.Where(p => p.Status.ToString() == filter.Status);

    if (!string.IsNullOrEmpty(filter.LineOfBusiness))
        query = query.Where(p => p.LineOfBusiness == filter.LineOfBusiness);

    if (!string.IsNullOrEmpty(filter.Region))
        query = query.Where(p => p.Region == filter.Region);

    if (filter.EffectiveDateFrom.HasValue)
        query = query.Where(p => p.EffectiveDate >= filter.EffectiveDateFrom.Value);

    if (filter.EffectiveDateTo.HasValue)
        query = query.Where(p => p.EffectiveDate <= filter.EffectiveDateTo.Value);

    if (!string.IsNullOrEmpty(filter.Search))
    {
        var term = filter.Search.ToLower();
        query = query.Where(p =>
            p.PolicyNumber.ToLower().Contains(term) ||
            p.PolicyholderName.ToLower().Contains(term) ||
            p.Underwriter.ToLower().Contains(term));
    }

    return query;
}

private static IQueryable<Policy> ApplySort(IQueryable<Policy> query, string sort)
{
    var parts = sort.Split(',');
    var desc = parts[1].ToLower() == "desc";

    return parts[0].ToLower() switch
    {
        "policynumber"   => desc ? query.OrderByDescending(p => p.PolicyNumber)   : query.OrderBy(p => p.PolicyNumber),
        "status"         => desc ? query.OrderByDescending(p => p.Status)         : query.OrderBy(p => p.Status),
        "effectivedate"  => desc ? query.OrderByDescending(p => p.EffectiveDate)  : query.OrderBy(p => p.EffectiveDate),
        "premiumamount"  => desc ? query.OrderByDescending(p => p.PremiumAmount)  : query.OrderBy(p => p.PremiumAmount),
        _                => desc ? query.OrderByDescending(p => p.CreatedAt)      : query.OrderBy(p => p.CreatedAt)
    };
}
```

---

## Step 4 - Service Layer Integration

**File**: `src/backend/Chubb.PolicyManagement.Application/Services/PolicyService.cs`

```csharp
public async Task<PagedResult<PolicyDto>> GetPoliciesAsync(
    PolicyFilterQuery filter, CancellationToken cancellationToken)
{
    // Validate before querying
    await _validator.ValidateAndThrowAsync(filter, cancellationToken: cancellationToken);

    var policies = await _repository.ListAsync(filter, cancellationToken);
    var totalCount = await _repository.CountAsync(filter, cancellationToken);
    var totalPages = (int)Math.Ceiling((double)totalCount / filter.Size);

    return new PagedResult<PolicyDto>(
        Data: policies.Select(p => _mapper.Map<PolicyDto>(p)),
        Page: filter.Page,
        Size: filter.Size,
        TotalCount: totalCount,
        TotalPages: totalPages);
}
```

---

## Step 5 - Controller Query Parameter Binding

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<PolicyDto>>> GetPolicies(
    [FromQuery] int page = 1,
    [FromQuery] int size = 20,
    [FromQuery] string sort = "createdAt,desc",
    [FromQuery] string? status = null,
    [FromQuery] string? lineOfBusiness = null,
    [FromQuery] string? region = null,
    [FromQuery] DateTime? effectiveDateFrom = null,
    [FromQuery] DateTime? effectiveDateTo = null,
    [FromQuery] string? search = null,
    CancellationToken cancellationToken = default)
{
    var filter = new PolicyFilterQuery
    {
        Page = page, Size = size, Sort = sort,
        Status = status, LineOfBusiness = lineOfBusiness, Region = region,
        EffectiveDateFrom = effectiveDateFrom, EffectiveDateTo = effectiveDateTo,
        Search = search
    };
    var result = await _policyService.GetPoliciesAsync(filter, cancellationToken);
    return Ok(result);
}
```

---

## Example Queries

```
# Active policies in APAC, page 1
GET /api/v1/policies?status=Active&region=APAC&page=1&size=20

# Free-text search
GET /api/v1/policies?search=ABC%20Corporation

# Date range filter
GET /api/v1/policies?effectiveDateFrom=2026-01-01&effectiveDateTo=2026-12-31

# Combined with sort
GET /api/v1/policies?status=Active&lineOfBusiness=Property&sort=premiumAmount,desc&size=50
```

---

## Testing Filter Logic

```csharp
[Fact]
public async Task ListAsync_WithStatusFilter_ReturnsOnlyMatchingPolicies()
{
    // Arrange
    var filter = new PolicyFilterQuery { Status = "Active", Page = 1, Size = 20 };
    var policies = new List<Policy>
    {
        new() { Id = Guid.NewGuid(), PolicyNumber = "POL-001", Status = PolicyStatus.Active }
    };
    _mockRepo.Setup(r => r.ListAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(policies);
    _mockRepo.Setup(r => r.CountAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(1);

    // Act
    var result = await _sut.GetPoliciesAsync(filter, CancellationToken.None);

    // Assert
    result.Data.Should().HaveCount(1);
    result.TotalCount.Should().Be(1);
}

[Theory]
[InlineData("InvalidStatus")]
[InlineData("active")]
public async Task GetPoliciesAsync_WithInvalidStatus_ThrowsValidationException(string status)
{
    // Arrange
    var filter = new PolicyFilterQuery { Status = status };

    // Act
    var act = () => _sut.GetPoliciesAsync(filter, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<ValidationException>();
}

[Theory]
[InlineData(0)]
[InlineData(101)]
public async Task GetPoliciesAsync_WithInvalidSize_ThrowsValidationException(int size)
{
    // Arrange
    var filter = new PolicyFilterQuery { Size = size };

    // Act
    var act = () => _sut.GetPoliciesAsync(filter, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<ValidationException>();
}
```

---

## Performance Rules

| Rule | Reason |
|---|---|
| `.AsNoTracking()` on all reads | Avoids change tracking overhead |
| `.Skip().Take()` at DB level | Never load all rows into memory |
| Indexes on filtered fields | Required: status, region, LOB, effectiveDate, flaggedForReview |
| Parameterized queries only | Prevents SQL injection (OWASP A03) |
| Filter before sort | Reduces rows to sort |

---

## Checklist

- [ ] `PolicyFilterQuery` model defined with default values
- [ ] FluentValidation validator covers all parameters
- [ ] Repository `ApplyFilters` handles null checks on optional params
- [ ] Repository `ApplySort` has default fallback
- [ ] Pagination uses `.Skip((page-1)*size).Take(size)`
- [ ] Free-text search is parameterized (no string interpolation in raw SQL)
- [ ] Service calls `ValidateAndThrowAsync` before querying
- [ ] Controller maps `[FromQuery]` params to `PolicyFilterQuery`
- [ ] All filter combinations tested with xUnit `[Theory]`
- [ ] 90% test coverage on service and repository filter paths
