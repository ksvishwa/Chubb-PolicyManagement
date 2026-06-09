using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;

namespace Chubb.PolicyManagement.Domain.Interfaces;

public interface IPolicyRepository
{
    Task<(IReadOnlyList<Policy> Items, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default);

    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task BulkFlagAsync(IEnumerable<Guid> policyIds, CancellationToken cancellationToken = default);

    Task<PolicySummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public record PolicySummary(
    int TotalPolicies,
    int ActivePolicies,
    int ExpiredPolicies,
    int PendingPolicies,
    int CancelledPolicies,
    int FlaggedForReview,
    decimal TotalPremiumAmount,
    IDictionary<string, int> ByLineOfBusiness,
    IDictionary<string, int> ByRegion);
