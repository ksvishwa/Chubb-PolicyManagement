using Chubb.PolicyManagement.Domain.Enums;

namespace Chubb.PolicyManagement.Domain.Entities;

public class Policy
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyholderName { get; set; } = string.Empty;
    public LineOfBusiness LineOfBusiness { get; set; }
    public PolicyStatus Status { get; set; }
    public decimal PremiumAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Underwriter { get; set; } = string.Empty;
    public bool FlaggedForReview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
