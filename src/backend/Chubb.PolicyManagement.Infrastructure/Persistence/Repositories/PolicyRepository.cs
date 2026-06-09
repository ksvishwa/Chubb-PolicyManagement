using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;
using Chubb.PolicyManagement.Domain.Interfaces;
using Chubb.PolicyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chubb.PolicyManagement.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository(PolicyManagementDbContext dbContext) : IPolicyRepository
{
    public async Task<(IReadOnlyList<Policy> Items, int TotalCount)> GetPagedAsync(
        int page,
        int size,
        string? sortField,
        string? sortDirection,
        PolicyStatus? status,
        LineOfBusiness? lineOfBusiness,
        string? region,
        DateOnly? effectiveDateFrom,
        DateOnly? effectiveDateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Policies.AsNoTracking();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (lineOfBusiness.HasValue)
            query = query.Where(p => p.LineOfBusiness == lineOfBusiness.Value);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(p => p.Region == region);

        if (effectiveDateFrom.HasValue)
            query = query.Where(p => p.EffectiveDate >= effectiveDateFrom.Value);

        if (effectiveDateTo.HasValue)
            query = query.Where(p => p.EffectiveDate <= effectiveDateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.PolicyNumber.Contains(term) ||
                p.PolicyholderName.Contains(term) ||
                p.Underwriter.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortField?.ToLowerInvariant()) switch
        {
            "policynumber" => isDesc ? query.OrderByDescending(p => p.PolicyNumber) : query.OrderBy(p => p.PolicyNumber),
            "policyholdername" => isDesc ? query.OrderByDescending(p => p.PolicyholderName) : query.OrderBy(p => p.PolicyholderName),
            "premiumamount" => isDesc ? query.OrderByDescending(p => p.PremiumAmount) : query.OrderBy(p => p.PremiumAmount),
            "effectivedate" => isDesc ? query.OrderByDescending(p => p.EffectiveDate) : query.OrderBy(p => p.EffectiveDate),
            "expirydate" => isDesc ? query.OrderByDescending(p => p.ExpiryDate) : query.OrderBy(p => p.ExpiryDate),
            "status" => isDesc ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "updatedat" => isDesc ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
            _ => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        };

        var offset = (page - 1) * size;
        var items = await query.Skip(offset).Take(size).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task BulkFlagAsync(IEnumerable<Guid> policyIds, CancellationToken cancellationToken = default)
    {
        await dbContext.Policies
            .Where(p => policyIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FlaggedForReview, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task<PolicySummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var policies = await dbContext.Policies.AsNoTracking().ToListAsync(cancellationToken);

        var byLob = policies
            .GroupBy(p => p.LineOfBusiness.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byRegion = policies
            .GroupBy(p => p.Region)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PolicySummary(
            TotalPolicies: policies.Count,
            ActivePolicies: policies.Count(p => p.Status == PolicyStatus.Active),
            ExpiredPolicies: policies.Count(p => p.Status == PolicyStatus.Expired),
            PendingPolicies: policies.Count(p => p.Status == PolicyStatus.Pending),
            CancelledPolicies: policies.Count(p => p.Status == PolicyStatus.Cancelled),
            FlaggedForReview: policies.Count(p => p.FlaggedForReview),
            TotalPremiumAmount: policies.Sum(p => p.PremiumAmount),
            ByLineOfBusiness: byLob,
            ByRegion: byRegion);
    }
}
