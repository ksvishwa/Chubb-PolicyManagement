using Chubb.PolicyManagement.Domain.Enums;

namespace Chubb.PolicyManagement.Application.DTOs;

public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    string PolicyholderName,
    string LineOfBusiness,
    string Status,
    decimal PremiumAmount,
    string Currency,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    string Region,
    string Underwriter,
    bool FlaggedForReview,
    DateTime CreatedAt,
    DateTime UpdatedAt);
