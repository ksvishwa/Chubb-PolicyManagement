using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;
using Chubb.PolicyManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chubb.PolicyManagement.Tests.Integration;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the SQL Server
/// DbContext with SQLite in-memory so integration tests run without a live database.
/// </summary>
public sealed class PolicyWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    // Well-known IDs for use in tests
    public static readonly Guid SeedActiveId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SeedExpiredId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SeedMarineId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContextOptions<PolicyManagementDbContext> registrations
            services.RemoveAll<DbContextOptions<PolicyManagementDbContext>>();

            // Re-register with the persistent SQLite connection
            services.AddDbContext<PolicyManagementDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // Build the host, create schema, then seed
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PolicyManagementDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedDataAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    private static async Task SeedDataAsync(PolicyManagementDbContext db)
    {
        if (await db.Policies.AnyAsync()) return;

        var now = DateTime.UtcNow;
        db.Policies.AddRange(
            new Policy
            {
                Id = SeedActiveId,
                PolicyNumber = "POL-001",
                PolicyholderName = "John Doe",
                LineOfBusiness = LineOfBusiness.Property,
                Status = PolicyStatus.Active,
                PremiumAmount = 10_000m,
                Currency = "USD",
                EffectiveDate = new DateOnly(2024, 1, 1),
                ExpiryDate = new DateOnly(2024, 12, 31),
                Region = "Singapore",
                Underwriter = "Alice Smith",
                FlaggedForReview = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Policy
            {
                Id = SeedExpiredId,
                PolicyNumber = "POL-002",
                PolicyholderName = "Jane Smith",
                LineOfBusiness = LineOfBusiness.Casualty,
                Status = PolicyStatus.Expired,
                PremiumAmount = 5_000m,
                Currency = "SGD",
                EffectiveDate = new DateOnly(2023, 1, 1),
                ExpiryDate = new DateOnly(2023, 12, 31),
                Region = "Hong Kong",
                Underwriter = "Bob Jones",
                FlaggedForReview = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Policy
            {
                Id = SeedMarineId,
                PolicyNumber = "POL-003",
                PolicyholderName = "APAC Corp",
                LineOfBusiness = LineOfBusiness.Marine,
                Status = PolicyStatus.Pending,
                PremiumAmount = 20_000m,
                Currency = "HKD",
                EffectiveDate = new DateOnly(2024, 6, 1),
                ExpiryDate = new DateOnly(2025, 5, 31),
                Region = "Singapore",
                Underwriter = "Carol Brown",
                FlaggedForReview = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        await db.SaveChangesAsync();
    }
}
