# Chubb APAC Policy Management Platform — Low-Level Design (LLD)

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [Domain Layer Design](#domain-layer-design)
2. [Application Layer Design](#application-layer-design)
3. [Infrastructure Layer Design](#infrastructure-layer-design)
4. [API Layer Design](#api-layer-design)
5. [Data Model & Entity Design](#data-model--entity-design)
6. [Algorithm & Business Logic](#algorithm--business-logic)
7. [Error Handling & Resilience](#error-handling--resilience)
8. [Testing Strategy](#testing-strategy)

---

## Domain Layer Design

### Purpose
The Domain layer contains **pure business logic** with zero external dependencies. All entities, enums, and exceptions are defined here. The layer is framework-agnostic and reusable across different presentation layers (API, CLI, background services).

### Entity: Policy

```csharp
public class Policy
{
    public Guid Id { get; set; }
    public required string PolicyNumber { get; set; }  // Unique, indexed
    public required string PolicyholderName { get; set; }
    public required string Underwriter { get; set; }
    
    // Financial
    public decimal PremiumAmount { get; set; }
    public required string Currency { get; set; }
    
    // Classification
    public required PolicyStatus Status { get; set; }  // Active, Expired, Pending, Cancelled
    public required LineOfBusiness LineOfBusiness { get; set; }  // Property, Casualty, A&H, Marine
    public required string Region { get; set; }  // Singapore, Australia, Japan, etc.
    
    // Coverage Dates
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    
    // Operational
    public bool FlaggedForReview { get; set; }
    public string? ReviewReason { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Invariants:**
- `PolicyNumber` must be unique and non-null
- `EffectiveDate` ≤ `ExpiryDate`
- `PremiumAmount` ≥ 0
- Status must be one of: Active, Expired, Pending, Cancelled
- LOB must be one of: Property, Casualty, A&H, Marine

### Enum: PolicyStatus

```csharp
public enum PolicyStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3
}
```

**Business Rules:**
- `Pending` → `Active`: After EffectiveDate reached
- `Active` → `Expired`: After ExpiryDate passed
- Any → `Cancelled`: Admin action, irreversible
- Cannot go backwards (e.g., Expired → Active)

### Enum: LineOfBusiness

```csharp
public enum LineOfBusiness
{
    Property = 0,
    Casualty = 1,
    HealthAndAccident = 2,  // A&H
    Marine = 3
}
```

### Domain Exceptions

```csharp
public class PolicyNotFoundException : DomainException
{
    public Guid PolicyId { get; }
    
    public PolicyNotFoundException(Guid policyId)
        : base($"Policy with ID {policyId} not found.")
    {
        PolicyId = policyId;
    }
}

public class InvalidPolicyFilterException : DomainException
{
    public InvalidPolicyFilterException(string message)
        : base(message) { }
}

public class BulkFlagValidationException : DomainException
{
    public BulkFlagValidationException(string message)
        : base(message) { }
}

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
```

### Repository Interface (Contract)

```csharp
public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<(IList<Policy> Policies, int TotalCount)> GetPoliciesAsync(
        PolicyFilterQuery filter,
        CancellationToken cancellationToken = default);
    
    Task<PolicySummaryData> GetSummaryAsync(CancellationToken cancellationToken = default);
    
    Task UpdateAsync(Policy policy, CancellationToken cancellationToken = default);
    
    Task UpdateManyAsync(IList<Policy> policies, CancellationToken cancellationToken = default);
}

public record PolicyFilterQuery(
    int Page = 1,
    int Size = 20,
    string SortBy = "createdAt",
    string SortDirection = "desc",
    PolicyStatus? Status = null,
    LineOfBusiness? LineOfBusiness = null,
    string? Region = null,
    DateTime? EffectiveDateFrom = null,
    DateTime? EffectiveDateTo = null,
    string? SearchTerm = null);

public record PolicySummaryData(
    int TotalPolicies,
    int ActiveCount,
    int ExpiredCount,
    int PendingCount,
    int CancelledCount,
    Dictionary<LineOfBusiness, decimal> PremiumByLOB,
    int UniqueRegionCount,
    int FlaggedCount);
```

---

## Application Layer Design

### Service: PolicyService

```csharp
public interface IPolicyService
{
    Task<PolicyDto?> GetByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    
    Task<PagedResult<PolicyDto>> GetPoliciesAsync(
        PolicyFilterQuery filter,
        CancellationToken cancellationToken = default);
    
    Task<PolicySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    
    Task FlagPoliciesForReviewAsync(
        BulkFlagRequest request,
        CancellationToken cancellationToken = default);
}

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PolicyService> _logger;
    
    public PolicyService(IPolicyRepository repository, IMapper mapper, ILogger<PolicyService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PolicyDto?> GetByIdAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching policy {PolicyId}", policyId);
        
        var policy = await _repository.GetByIdAsync(policyId, cancellationToken)
            ?? throw new PolicyNotFoundException(policyId);
        
        return _mapper.Map<PolicyDto>(policy);
    }
    
    public async Task<PagedResult<PolicyDto>> GetPoliciesAsync(
        PolicyFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (filter.Page < 1)
            throw new InvalidPolicyFilterException("Page must be >= 1");
        
        if (filter.Size < 1 || filter.Size > 100)
            throw new InvalidPolicyFilterException("Size must be between 1 and 100");
        
        if (filter.EffectiveDateFrom.HasValue && filter.EffectiveDateTo.HasValue
            && filter.EffectiveDateFrom > filter.EffectiveDateTo)
            throw new InvalidPolicyFilterException("EffectiveDateFrom must be <= EffectiveDateTo");
        
        _logger.LogInformation("Fetching policies with filter: {@Filter}", filter);
        
        var (policies, totalCount) = await _repository.GetPoliciesAsync(filter, cancellationToken);
        
        var policyDtos = _mapper.Map<List<PolicyDto>>(policies);
        
        return new PagedResult<PolicyDto>(
            data: policyDtos,
            page: filter.Page,
            size: filter.Size,
            totalCount: totalCount,
            totalPages: (totalCount + filter.Size - 1) / filter.Size);
    }
    
    public async Task<PolicySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching policy summary");
        
        var summary = await _repository.GetSummaryAsync(cancellationToken);
        
        return new PolicySummaryDto(
            TotalPolicies: summary.TotalPolicies,
            ActiveCount: summary.ActiveCount,
            ExpiredCount: summary.ExpiredCount,
            PendingCount: summary.PendingCount,
            CancelledCount: summary.CancelledCount,
            PremiumByLOB: summary.PremiumByLOB,
            UniqueRegionCount: summary.UniqueRegionCount,
            FlaggedCount: summary.FlaggedCount);
    }
    
    public async Task FlagPoliciesForReviewAsync(
        BulkFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PolicyIds == null || request.PolicyIds.Count == 0)
            throw new BulkFlagValidationException("At least one policy ID required");
        
        if (request.PolicyIds.Count > 1000)
            throw new BulkFlagValidationException("Maximum 1000 policies per request");
        
        _logger.LogInformation("Flagging {Count} policies for review", request.PolicyIds.Count);
        
        var policyIds = request.PolicyIds.Distinct().ToList();
        var policies = new List<Policy>();
        
        foreach (var id in policyIds)
        {
            var policy = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new PolicyNotFoundException(id);
            
            policy.FlaggedForReview = true;
            policy.UpdatedAt = DateTime.UtcNow;
            policies.Add(policy);
        }
        
        await _repository.UpdateManyAsync(policies, cancellationToken);
        
        _logger.LogInformation("Successfully flagged {Count} policies for review", policies.Count);
    }
}
```

### DTO Definitions

```csharp
// Request/Response DTOs
public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    string PolicyholderName,
    string Underwriter,
    decimal PremiumAmount,
    string Currency,
    string Status,
    string LineOfBusiness,
    string Region,
    DateTime EffectiveDate,
    DateTime ExpiryDate,
    bool FlaggedForReview,
    string? ReviewReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PolicyFilterQuery(
    int Page = 1,
    int Size = 20,
    string SortBy = "createdAt",
    string SortDirection = "desc",
    string? Status = null,
    string? LineOfBusiness = null,
    string? Region = null,
    DateTime? EffectiveDateFrom = null,
    DateTime? EffectiveDateTo = null,
    string? SearchTerm = null);

public record PolicySummaryDto(
    int TotalPolicies,
    int ActiveCount,
    int ExpiredCount,
    int PendingCount,
    int CancelledCount,
    Dictionary<string, decimal> PremiumByLOB,
    int UniqueRegionCount,
    int FlaggedCount);

public record BulkFlagRequest(
    required List<Guid> PolicyIds,
    string? Reason = null);

public record PagedResult<T>(
    List<T> Data,
    int Page,
    int Size,
    int TotalCount,
    int TotalPages);
```

### Validators

```csharp
public class PolicyFilterQueryValidator : AbstractValidator<PolicyFilterQuery>
{
    private static readonly string[] ValidSortFields = 
        { "policyNumber", "createdAt", "status", "premiumAmount", "region" };
    
    private static readonly string[] ValidSortDirections = { "asc", "desc" };
    
    public PolicyFilterQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be >= 1");
        
        RuleFor(x => x.Size)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100)
            .WithMessage("Size must be between 1 and 100");
        
        RuleFor(x => x.SortBy)
            .Must(sb => ValidSortFields.Contains(sb.ToLower()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}");
        
        RuleFor(x => x.SortDirection)
            .Must(sd => ValidSortDirections.Contains(sd.ToLower()))
            .WithMessage("SortDirection must be 'asc' or 'desc'");
        
        RuleFor(x => x.EffectiveDateFrom)
            .LessThanOrEqualTo(x => x.EffectiveDateTo)
            .When(x => x.EffectiveDateFrom.HasValue && x.EffectiveDateTo.HasValue)
            .WithMessage("EffectiveDateFrom must be <= EffectiveDateTo");
    }
}

public class BulkFlagRequestValidator : AbstractValidator<BulkFlagRequest>
{
    public BulkFlagRequestValidator()
    {
        RuleFor(x => x.PolicyIds)
            .NotEmpty()
            .WithMessage("At least one policy ID required");
        
        RuleFor(x => x.PolicyIds.Count)
            .LessThanOrEqualTo(1000)
            .WithMessage("Maximum 1000 policies per request");
        
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");
    }
}
```

---

## Infrastructure Layer Design

### Entity Configuration (EF Core)

```csharp
public class PolicyEntityTypeConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.PolicyNumber)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(p => p.PolicyholderName)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(p => p.Underwriter)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(p => p.PremiumAmount)
            .HasPrecision(18, 2);
        
        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("SGD");
        
        builder.Property(p => p.Status)
            .HasConversion(new EnumToStringConverter<PolicyStatus>());
        
        builder.Property(p => p.LineOfBusiness)
            .HasConversion(new EnumToStringConverter<LineOfBusiness>());
        
        builder.Property(p => p.Region)
            .HasMaxLength(50);
        
        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        // Indexes
        builder.HasIndex(p => p.PolicyNumber).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Region);
        builder.HasIndex(p => p.LineOfBusiness);
        builder.HasIndex(p => p.FlaggedForReview);
        builder.HasIndex(p => p.EffectiveDate);
        builder.HasIndex(p => p.ExpiryDate);
    }
}
```

### Repository Implementation

```csharp
public class PolicyRepository : IPolicyRepository
{
    private readonly PolicyManagementDbContext _context;
    private readonly ILogger<PolicyRepository> _logger;
    
    public PolicyRepository(PolicyManagementDbContext context, ILogger<PolicyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task<(IList<Policy>, int TotalCount)> GetPoliciesAsync(
        PolicyFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Policies.AsNoTracking();
        
        // Apply filters
        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value);
        
        if (filter.LineOfBusiness.HasValue)
            query = query.Where(p => p.LineOfBusiness == filter.LineOfBusiness.Value);
        
        if (!string.IsNullOrWhiteSpace(filter.Region))
            query = query.Where(p => p.Region == filter.Region);
        
        if (filter.EffectiveDateFrom.HasValue)
            query = query.Where(p => p.EffectiveDate >= filter.EffectiveDateFrom.Value);
        
        if (filter.EffectiveDateTo.HasValue)
            query = query.Where(p => p.ExpiryDate <= filter.EffectiveDateTo.Value);
        
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                p.PolicyNumber.ToLower().Contains(searchTerm) ||
                p.PolicyholderName.ToLower().Contains(searchTerm) ||
                p.Underwriter.ToLower().Contains(searchTerm));
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        // Apply sorting
        query = filter.SortDirection.ToLower() == "desc"
            ? query.OrderByDescending(p => GetSortProperty(p, filter.SortBy))
            : query.OrderBy(p => GetSortProperty(p, filter.SortBy));
        
        // Apply pagination
        var offset = (filter.Page - 1) * filter.Size;
        var policies = await query
            .Skip(offset)
            .Take(filter.Size)
            .ToListAsync(cancellationToken);
        
        return (policies, totalCount);
    }
    
    public async Task<PolicySummaryData> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _context.Policies.AsNoTracking().ToListAsync(cancellationToken);
        
        return new PolicySummaryData(
            TotalPolicies: policies.Count,
            ActiveCount: policies.Count(p => p.Status == PolicyStatus.Active),
            ExpiredCount: policies.Count(p => p.Status == PolicyStatus.Expired),
            PendingCount: policies.Count(p => p.Status == PolicyStatus.Pending),
            CancelledCount: policies.Count(p => p.Status == PolicyStatus.Cancelled),
            PremiumByLOB: policies
                .GroupBy(p => p.LineOfBusiness)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.PremiumAmount)),
            UniqueRegionCount: policies.Select(p => p.Region).Distinct().Count(),
            FlaggedCount: policies.Count(p => p.FlaggedForReview));
    }
    
    public async Task UpdateAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _context.Policies.Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UpdateManyAsync(IList<Policy> policies, CancellationToken cancellationToken = default)
    {
        foreach (var policy in policies)
            policy.UpdatedAt = DateTime.UtcNow;
        
        _context.Policies.UpdateRange(policies);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    private static object? GetSortProperty(Policy policy, string sortBy) =>
        sortBy.ToLower() switch
        {
            "policyNumber" => policy.PolicyNumber,
            "createdAt" => policy.CreatedAt,
            "status" => policy.Status,
            "premiumAmount" => policy.PremiumAmount,
            "region" => policy.Region,
            _ => policy.CreatedAt
        };
}
```

### DbContext

```csharp
public class PolicyManagementDbContext : DbContext
{
    public PolicyManagementDbContext(DbContextOptions<PolicyManagementDbContext> options)
        : base(options) { }
    
    public DbSet<Policy> Policies { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new PolicyEntityTypeConfiguration());
        
        // Seed data
        modelBuilder.Entity<Policy>().HasData(PolicySeeder.GetSeedPolicies());
    }
}
```

---

## API Layer Design

### PoliciesController

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly ILogger<PoliciesController> _logger;
    
    public PoliciesController(IPolicyService policyService, ILogger<PoliciesController> logger)
    {
        _policyService = policyService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get paginated list of policies with optional filtering and sorting
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string? status = null,
        [FromQuery] string? lineOfBusiness = null,
        [FromQuery] string? region = null,
        [FromQuery] DateTime? effectiveDateFrom = null,
        [FromQuery] DateTime? effectiveDateTo = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/v1/policies - page={Page}, size={Size}", page, size);
        
        var filter = new PolicyFilterQuery(
            Page: page,
            Size: size,
            SortBy: sortBy,
            SortDirection: sortDirection,
            Status: status == null ? null : Enum.Parse<PolicyStatus>(status),
            LineOfBusiness: lineOfBusiness == null ? null : Enum.Parse<LineOfBusiness>(lineOfBusiness),
            Region: region,
            EffectiveDateFrom: effectiveDateFrom,
            EffectiveDateTo: effectiveDateTo,
            SearchTerm: search);
        
        var result = await _policyService.GetPoliciesAsync(filter, cancellationToken);
        
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
        
        return Ok(result);
    }
    
    /// <summary>
    /// Get single policy by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPolicyById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/v1/policies/{Id}", id);
        
        var policy = await _policyService.GetByIdAsync(id, cancellationToken);
        
        return Ok(policy);
    }
    
    /// <summary>
    /// Flag multiple policies for review (bulk operation)
    /// </summary>
    [HttpPatch("flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FlagPolicies(
        [FromBody] BulkFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PATCH /api/v1/policies/flag - count={Count}", request.PolicyIds.Count);
        
        await _policyService.FlagPoliciesForReviewAsync(request, cancellationToken);
        
        return NoContent();
    }
    
    /// <summary>
    /// Get aggregated policy statistics
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PolicySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/v1/policies/summary");
        
        var summary = await _policyService.GetSummaryAsync(cancellationToken);
        
        return Ok(summary);
    }
}
```

### Global Exception Middleware

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        
        ProblemDetails problemDetails = exception switch
        {
            PolicyNotFoundException ex => new ProblemDetails
            {
                Type = "https://api.example.com/errors/policy-not-found",
                Title = "Policy Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
                Instance = context.Request.Path
            },
            
            InvalidPolicyFilterException ex => new ProblemDetails
            {
                Type = "https://api.example.com/errors/invalid-filter",
                Title = "Invalid Policy Filter",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Instance = context.Request.Path
            },
            
            BulkFlagValidationException ex => new ProblemDetails
            {
                Type = "https://api.example.com/errors/bulk-flag-validation",
                Title = "Bulk Flag Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Instance = context.Request.Path
            },
            
            _ => new ProblemDetails
            {
                Type = "https://api.example.com/errors/internal-server-error",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred.",
                Instance = context.Request.Path
            }
        };
        
        response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        
        return response.WriteAsJsonAsync(problemDetails);
    }
}
```

---

## Data Model & Entity Design

### Policy Entity Diagram

```
┌──────────────────────────────────────────┐
│              Policy                      │
├──────────────────────────────────────────┤
│ Id: Guid (PK)                            │
│ PolicyNumber: string (UNIQUE, Indexed)   │
│ PolicyholderName: string (100)           │
│ Underwriter: string (100)                │
│ PremiumAmount: decimal(18,2)             │
│ Currency: string (3, default='SGD')      │
│ Status: PolicyStatus (Indexed)           │
│ LineOfBusiness: LineOfBusiness (Indexed) │
│ Region: string (50, Indexed)             │
│ EffectiveDate: DateTime (Indexed)        │
│ ExpiryDate: DateTime (Indexed)           │
│ FlaggedForReview: bool (Indexed)         │
│ ReviewReason: string (nullable, 500)     │
│ CreatedAt: DateTime (default=GETUTCDATE) │
│ UpdatedAt: DateTime (default=GETUTCDATE) │
└──────────────────────────────────────────┘
```

### Indexes Strategy

| Index | Columns | Purpose | Selectivity |
|---|---|---|---|
| `PK_Policies_Id` | Id | Clustered primary key | Unique |
| `UX_Policies_PolicyNumber` | PolicyNumber | Unique policy identifier | Unique |
| `IX_Policies_Status` | Status | Filter by active/expired/pending | ~4 values |
| `IX_Policies_Region` | Region | Regional filtering | ~10 values |
| `IX_Policies_LOB` | LineOfBusiness | Line of business filtering | ~4 values |
| `IX_Policies_FlaggedForReview` | FlaggedForReview | Show flagged policies | ~2 values |
| `IX_Policies_EffectiveDate` | EffectiveDate | Date range queries | Continuous |
| `IX_Policies_ExpiryDate` | ExpiryDate | Date range queries | Continuous |

---

## Algorithm & Business Logic

### Policy Filtering Algorithm

```
Input: PolicyFilterQuery
Output: IList<Policy>, TotalCount

1. Start with all policies: query = Policies.AsNoTracking()

2. Apply filters (OR combination):
   - IF Status specified: query = query.Where(p => p.Status == Status)
   - IF Region specified: query = query.Where(p => p.Region == Region)
   - IF LOB specified: query = query.Where(p => p.LineOfBusiness == LOB)
   - IF DateFrom specified: query = query.Where(p => p.EffectiveDate >= DateFrom)
   - IF DateTo specified: query = query.Where(p => p.ExpiryDate <= DateTo)
   - IF SearchTerm specified: query = query.Where(p => 
       p.PolicyNumber.Contains(SearchTerm) OR
       p.PolicyholderName.Contains(SearchTerm) OR
       p.Underwriter.Contains(SearchTerm))

3. Calculate TotalCount = query.Count()

4. Apply sorting:
   - IF SortDirection == "desc":
       query = query.OrderByDescending(SortBy)
     ELSE:
       query = query.OrderBy(SortBy)

5. Apply pagination:
   - offset = (Page - 1) * Size
   - query = query.Skip(offset).Take(Size)

6. Materialize: policies = query.ToList()

7. Return (policies, TotalCount)
```

**Complexity Analysis:**
- Time: O(n log n) with indexes on filter columns; O(n) without
- Space: O(pageSize) — only fetches page size rows
- DB Queries: 1 (count) + 1 (paginated data) = 2 round trips

### Bulk Flag Algorithm

```
Input: BulkFlagRequest { PolicyIds[], Reason? }
Output: Success or Exception

1. Validate input:
   - IF PolicyIds.Count == 0: Throw BulkFlagValidationException
   - IF PolicyIds.Count > 1000: Throw BulkFlagValidationException

2. Deduplicate: policyIds = PolicyIds.Distinct()

3. BEGIN TRANSACTION

4. FOR EACH policyId IN policyIds:
   - Load policy = repository.GetByIdAsync(policyId)
   - IF policy == NULL: Throw PolicyNotFoundException(policyId)
   - SET policy.FlaggedForReview = true
   - SET policy.UpdatedAt = DateTime.UtcNow
   - ADD policy to updateList

5. Batch update: repository.UpdateManyAsync(updateList)

6. COMMIT TRANSACTION

7. Log: "Successfully flagged {count} policies for review"

8. Return: NoContent (204)
```

**Atomicity Guarantee:** Either all policies are flagged or none are (all-or-nothing semantics)

---

## Error Handling & Resilience

### Exception Mapping to HTTP Status Codes

| Exception | HTTP Status | Response Example |
|---|---|---|
| `PolicyNotFoundException` | 404 Not Found | `{ "type": "...", "title": "Policy Not Found", "status": 404, "detail": "Policy with ID xyz not found." }` |
| `InvalidPolicyFilterException` | 400 Bad Request | `{ "type": "...", "title": "Invalid Policy Filter", "status": 400, "detail": "Page must be >= 1" }` |
| `BulkFlagValidationException` | 400 Bad Request | `{ "type": "...", "title": "Bulk Flag Validation Failed", "status": 400, "detail": "Maximum 1000 policies per request" }` |
| `ArgumentException` | 400 Bad Request | Generic validation error |
| Unhandled Exception | 500 Internal Server Error | `{ "type": "...", "title": "Internal Server Error", "status": 500, "detail": "An unexpected error occurred." }` |

### Resilience Strategies

| Scenario | Strategy |
|---|---|
| **DB Connection Timeout** | Retry with exponential backoff (3 attempts); circuit breaker after 5 consecutive failures |
| **Slow Queries** | Query timeout 30sec; monitor P95 latency; add indexes if needed |
| **Bulk Operation Partial Failure** | Fail entire operation; no partial updates (transactional) |
| **Concurrent Updates** | Last-write-wins (no optimistic concurrency); audit UpdatedAt timestamp |

---

## Testing Strategy

### Unit Test Examples

```csharp
[Fact]
public async Task GetPoliciesAsync_WithStatusFilter_ReturnsOnlyActivePolicies()
{
    // Arrange
    var filter = new PolicyFilterQuery(Status: PolicyStatus.Active);
    var mockRepository = new Mock<IPolicyRepository>();
    var activePolicies = new List<Policy> { /* ... */ };
    mockRepository
        .Setup(r => r.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((activePolicies, 5));
    
    var service = new PolicyService(mockRepository.Object, mapper, logger);
    
    // Act
    var result = await service.GetPoliciesAsync(filter);
    
    // Assert
    result.Data.Should().HaveCount(5);
    result.Data.Should().AllSatisfy(p => p.Status.Should().Be("Active"));
}

[Fact]
public async Task FlagPoliciesForReviewAsync_WithEmptyList_ThrowsBulkFlagValidationException()
{
    // Arrange
    var request = new BulkFlagRequest(new List<Guid>());
    var service = new PolicyService(mockRepository.Object, mapper, logger);
    
    // Act & Assert
    await service.Invoking(s => s.FlagPoliciesForReviewAsync(request, CancellationToken.None))
        .Should()
        .ThrowAsync<BulkFlagValidationException>()
        .WithMessage("At least one policy ID required");
}
```

### Integration Test Examples

```csharp
[Fact]
public async Task GetPolicies_WithValidFilter_Returns200AndPaginatedData()
{
    // Arrange
    using var client = factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/policies?page=1&size=10&status=Active");
    
    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    response.Headers.Should().Contain(h => h.Key == "X-Total-Count");
    var content = await response.Content.ReadAsAsync<PagedResult<PolicyDto>>();
    content.Data.Should().HaveCount(10);
}

[Fact]
public async Task FlagPolicies_WithValidRequest_Returns204NoContent()
{
    // Arrange
    using var client = factory.CreateClient();
    var request = new BulkFlagRequest(new List<Guid> { policyId1, policyId2 });
    
    // Act
    var response = await client.PatchAsync(
        "/api/v1/policies/flag",
        new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
    
    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
}
```

---

## Summary

The LLD provides:

1. **Domain Layer** — Pure business entities, enums, exceptions, and repository contracts
2. **Application Layer** — Services, DTOs, validators, and business logic orchestration
3. **Infrastructure Layer** — EF Core configurations, repository implementations, migrations
4. **API Layer** — Controllers, exception middleware, error responses
5. **Data Model** — Policy entity schema with indexes and constraints
6. **Algorithms** — Filtering, sorting, pagination, bulk flagging logic
7. **Error Handling** — RFC 7807 Problem Details, exception mapping
8. **Testing** — Unit and integration test examples

This design ensures **clean separation of concerns**, **testability**, **scalability**, and **maintainability**.
