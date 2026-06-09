---
name: ef-core-database-operations
description: >
  Master EF Core 8.x database operations for the Chubb APAC Policy Management Platform.
  Covers entity design, IEntityTypeConfiguration, migrations, seeding, repository patterns,
  and performance-safe querying for SQL Server.
---

# EF Core Database Operations

## When to Use

Invoke this skill when asked to:
- Design entity models or configure database schema
- Create or update EF Core migrations
- Implement the repository pattern
- Write database queries (filtering, pagination, sorting)
- Seed initial or test data
- Troubleshoot migration or connection issues

**Trigger phrases**: "create migration", "entity configuration", "seed data", "repository", "add index", "database query"

---

## Prerequisites

- .NET 8 SDK installed
- SQL Server LocalDB or SQL Server 2022 accessible
- Project follows Clean Architecture: Domain -> Application -> Infrastructure -> API
- Read `.github/instructions/ef-core-rules.instructions.md` before writing any EF Core code

---

## Step 1 - Define Entity in Domain Layer

**File**: `src/backend/Chubb.PolicyManagement.Domain/Entities/Policy.cs`

Rules:
- **No EF Core attributes** — all config goes in `IEntityTypeConfiguration<T>`
- Implement `IAuditable` for automatic audit timestamps
- Use C# `required` on non-nullable string properties

```csharp
namespace Chubb.PolicyManagement.Domain.Entities;

public class Policy : IAuditable
{
    public Guid Id { get; set; }
    public required string PolicyNumber { get; set; }
    public PolicyStatus Status { get; set; }
    public required string LineOfBusiness { get; set; }
    public required string Region { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public required string PolicyholderName { get; set; }
    public required string Underwriter { get; set; }
    public decimal PremiumAmount { get; set; }
    public bool FlaggedForReview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum PolicyStatus { Active, Expired, Pending, Cancelled }

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

---

## Step 2 - Entity Configuration (Infrastructure)

**File**: `src/backend/Chubb.PolicyManagement.Infrastructure/Persistence/Configurations/PolicyEntityConfiguration.cs`

Rules:
- Every `string` property **must** have `HasMaxLength()` — no `nvarchar(max)`
- Enums stored as strings via `HasConversion<string>()`
- All required indexes declared here

```csharp
public class PolicyEntityConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("NEWID()");

        builder.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(50);
        builder.Property(p => p.LineOfBusiness).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Region).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PolicyholderName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Underwriter).IsRequired().HasMaxLength(200);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(p => p.ExpiryDate).HasColumnType("date");
        builder.Property(p => p.PremiumAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.FlaggedForReview).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2").ValueGeneratedOnAdd();
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2");

        // Mandatory indexes
        builder.HasIndex(p => p.PolicyNumber).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.LineOfBusiness);
        builder.HasIndex(p => p.Region);
        builder.HasIndex(p => p.EffectiveDate);
        builder.HasIndex(p => p.ExpiryDate);
        builder.HasIndex(p => p.FlaggedForReview);
    }
}
```

---

## Step 3 - DbContext

**File**: `src/backend/Chubb.PolicyManagement.Infrastructure/Persistence/ChubbPolicyManagementContext.cs`

```csharp
public class ChubbPolicyManagementContext : DbContext
{
    public ChubbPolicyManagementContext(DbContextOptions<ChubbPolicyManagementContext> options)
        : base(options) { }

    public DbSet<Policy> Policies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChubbPolicyManagementContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Step 4 - Repository Interface (Domain)

**File**: `src/backend/Chubb.PolicyManagement.Domain/Interfaces/IPolicyRepository.cs`

```csharp
public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Policy>> ListAsync(PolicyFilterQuery filter, CancellationToken cancellationToken);
    Task<int> CountAsync(PolicyFilterQuery filter, CancellationToken cancellationToken);
    Task AddAsync(Policy policy, CancellationToken cancellationToken);
    Task UpdateAsync(Policy policy, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

---

## Step 5 - Repository Implementation (Infrastructure)

**File**: `src/backend/Chubb.PolicyManagement.Infrastructure/Persistence/Repositories/PolicyRepository.cs`

Rules:
- All read queries use `.AsNoTracking()`
- Pagination applied at DB level with `.Skip().Take()`
- Never raw SQL string concatenation

```csharp
public class PolicyRepository : IPolicyRepository
{
    private readonly ChubbPolicyManagementContext _context;
    public PolicyRepository(ChubbPolicyManagementContext context) => _context = context;

    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Policies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<Policy>> ListAsync(PolicyFilterQuery filter, CancellationToken cancellationToken)
    {
        var query = BuildFilterQuery(filter);
        return await query
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(PolicyFilterQuery filter, CancellationToken cancellationToken) =>
        await BuildFilterQuery(filter).CountAsync(cancellationToken);

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken)
    {
        await _context.Policies.AddAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Policy policy, CancellationToken cancellationToken)
    {
        _context.Policies.Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await GetByIdAsync(id, cancellationToken);
        if (policy is not null)
        {
            _context.Policies.Remove(policy);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private IQueryable<Policy> BuildFilterQuery(PolicyFilterQuery filter)
    {
        var query = _context.Policies.AsNoTracking();
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(p => p.Status.ToString() == filter.Status);
        if (!string.IsNullOrEmpty(filter.Region))
            query = query.Where(p => p.Region == filter.Region);
        if (!string.IsNullOrEmpty(filter.LineOfBusiness))
            query = query.Where(p => p.LineOfBusiness == filter.LineOfBusiness);
        return query;
    }
}
```

---

## Step 6 - Create Migration

```bash
cd src/backend
dotnet ef migrations add InitialSchema \
  --context ChubbPolicyManagementContext \
  --project Chubb.PolicyManagement.Infrastructure \
  --startup-project Chubb.PolicyManagement.Api

dotnet ef database update --context ChubbPolicyManagementContext
```

Never rename migration files. Never call `EnsureCreated()`.

---

## Step 7 - Idempotent Seeding

**File**: `src/backend/Chubb.PolicyManagement.Infrastructure/Persistence/SeedData.cs`

```csharp
public static class SeedData
{
    public static void Seed(ChubbPolicyManagementContext context)
    {
        if (context.Policies.Any()) return; // Guard - idempotent

        context.Policies.AddRange(new List<Policy>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PolicyNumber = "POL-APAC-2026-001",
                Status = PolicyStatus.Active,
                LineOfBusiness = "Property",
                Region = "APAC",
                EffectiveDate = new DateTime(2026, 1, 1),
                ExpiryDate = new DateTime(2027, 1, 1),
                PolicyholderName = "ABC Corporation",
                Underwriter = "John Smith",
                PremiumAmount = 50000m,
                FlaggedForReview = false
            }
        });
        context.SaveChanges();
    }
}
```

Apply in `Program.cs` (Development only):
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChubbPolicyManagementContext>();
    context.Database.Migrate();
    SeedData.Seed(context);
}
```

---

## Step 8 - Register in DI

```csharp
builder.Services.AddDbContext<ChubbPolicyManagementContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found"),
        sql =>
        {
            sql.MigrationsAssembly("Chubb.PolicyManagement.Infrastructure");
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelaySeconds: 30);
        }));

builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
```

---

## Troubleshooting

| Issue | Fix |
|---|---|
| `nvarchar(max)` column created | Add `HasMaxLength()` to every string property in configuration |
| Enum stored as integer | Add `.HasConversion<string>()` in configuration |
| Migration already applied | Run `dotnet ef migrations list` to check state |
| `EnsureCreated()` in code | Replace with `Database.Migrate()` in Development only |
| N+1 query | Use `.Include()` for related data; never loop and query |

---

## Checklist

- [ ] Entity in `Domain` with zero infrastructure references
- [ ] Configuration in `Infrastructure/Persistence/Configurations/`
- [ ] All string properties have `HasMaxLength()`
- [ ] All mandatory indexes declared
- [ ] Repository interface in `Domain/Interfaces/`
- [ ] Repository implementation in `Infrastructure/Persistence/Repositories/`
- [ ] `SaveChangesAsync` override updates audit fields
- [ ] All read queries use `.AsNoTracking()`
- [ ] Pagination applied at DB level (`.Skip().Take()`)
- [ ] Seeding is idempotent
- [ ] All async methods accept `CancellationToken`
- [ ] 90% test coverage for repository and service layers
