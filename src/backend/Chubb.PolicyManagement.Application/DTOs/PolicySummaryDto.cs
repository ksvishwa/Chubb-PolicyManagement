namespace Chubb.PolicyManagement.Application.DTOs;

public record PolicySummaryDto(
    int TotalPolicies,
    int ActivePolicies,
    int ExpiredPolicies,
    int PendingPolicies,
    int CancelledPolicies,
    int FlaggedForReview,
    decimal TotalPremiumAmount,
    IDictionary<string, int> ByLineOfBusiness,
    IDictionary<string, int> ByRegion);
