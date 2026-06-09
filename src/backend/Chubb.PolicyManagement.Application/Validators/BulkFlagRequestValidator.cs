using Chubb.PolicyManagement.Application.DTOs;
using FluentValidation;

namespace Chubb.PolicyManagement.Application.Validators;

/// <summary>
/// Validator for BulkFlagRequest to ensure policy IDs are valid and within bounds.
/// Enforces constraints per API contract in requirements.md:
/// - Array length: 1–1000 items
/// - No duplicates allowed
/// - All IDs must be valid UUID format (non-empty Guid)
/// </summary>
public sealed class BulkFlagRequestValidator : AbstractValidator<BulkFlagRequest>
{
    public BulkFlagRequestValidator()
    {
        RuleFor(x => x.PolicyIds)
            .NotNull()
            .WithMessage("PolicyIds cannot be null")
            .WithErrorCode("POLICY_IDS_REQUIRED");

        RuleFor(x => x.PolicyIds.ToList())
            .NotEmpty()
            .WithMessage("At least one policy ID is required")
            .WithErrorCode("EMPTY_POLICY_IDS")
            .Must(ids => ids.Count >= 1 && ids.Count <= 1000)
            .WithMessage("Between 1 and 1000 policies can be flagged at once")
            .WithErrorCode("POLICY_IDS_OUT_OF_RANGE")
            .Must(ids => !HasDuplicates(ids))
            .WithMessage("PolicyIds cannot contain duplicate values")
            .WithErrorCode("DUPLICATE_POLICY_IDS")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All PolicyIds must be valid non-empty GUIDs")
            .WithErrorCode("INVALID_POLICY_ID_FORMAT");
    }

    private static bool HasDuplicates(IList<Guid> ids) => 
        ids.Distinct().Count() != ids.Count;
}
