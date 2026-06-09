using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Domain.Enums;
using FluentValidation;

namespace Chubb.PolicyManagement.Application.Validators;

/// <summary>
/// Validator for PolicyFilterQuery to ensure pagination, filtering, and sorting parameters are valid.
/// Enforces constraints per API contract in requirements.md.
/// </summary>
public sealed class PolicyFilterQueryValidator : AbstractValidator<PolicyFilterQuery>
{
    private static readonly string[] AllowedSortFields = 
    {
        "createdAt", "policyNumber", "policyholderName", "status", "lineOfBusiness",
        "premiumAmount", "effectiveDate", "expiryDate", "region", "flaggedForReview"
    };

    private static readonly string[] AllowedSortDirections = { "asc", "desc" };

    public PolicyFilterQueryValidator()
    {
        // Pagination validation
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be >= 1")
            .WithErrorCode("INVALID_PAGE");

        RuleFor(x => x.Size)
            .InclusiveBetween(1, 100)
            .WithMessage("Size must be between 1 and 100")
            .WithErrorCode("INVALID_SIZE");

        // Sort validation
        RuleFor(x => x.Sort)
            .NotEmpty()
            .WithMessage("Sort cannot be empty")
            .WithErrorCode("INVALID_SORT")
            .Custom((sort, context) =>
            {
                var parts = sort.Split(',');
                
                if (parts.Length is < 1 or > 2)
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure()
                    {
                        PropertyName = nameof(PolicyFilterQuery.Sort),
                        ErrorMessage = "Sort must be in format 'field' or 'field,direction'",
                        ErrorCode = "INVALID_SORT_FORMAT"
                    });
                    return;
                }

                var field = parts[0].Trim();
                if (!AllowedSortFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure()
                    {
                        PropertyName = nameof(PolicyFilterQuery.Sort),
                        ErrorMessage = $"Sort field '{field}' is not allowed. Allowed fields: {string.Join(", ", AllowedSortFields)}",
                        ErrorCode = "INVALID_SORT_FIELD"
                    });
                }

                if (parts.Length == 2)
                {
                    var direction = parts[1].Trim();
                    if (!AllowedSortDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
                    {
                        context.AddFailure(new FluentValidation.Results.ValidationFailure()
                        {
                            PropertyName = nameof(PolicyFilterQuery.Sort),
                            ErrorMessage = "Sort direction must be 'asc' or 'desc'",
                            ErrorCode = "INVALID_SORT_DIRECTION"
                        });
                    }
                }
            });

        // Status filter validation
        RuleFor(x => x.Status)
            .Must(status => status == null || IsValidPolicyStatus(status))
            .WithMessage("Status must be one of: Active, Expired, Pending, Cancelled")
            .WithErrorCode("INVALID_STATUS")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        // Line of Business filter validation
        RuleFor(x => x.LineOfBusiness)
            .Must(lob => lob == null || IsValidLineOfBusiness(lob))
            .WithMessage("LineOfBusiness must be one of: Property, Casualty, A&H, Marine")
            .WithErrorCode("INVALID_LINE_OF_BUSINESS")
            .When(x => !string.IsNullOrWhiteSpace(x.LineOfBusiness));

        // Region filter validation
        RuleFor(x => x.Region)
            .MaximumLength(50)
            .WithMessage("Region cannot exceed 50 characters")
            .WithErrorCode("REGION_TOO_LONG")
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        // Date range validation
        RuleFor(x => x.EffectiveDateFrom)
            .LessThanOrEqualTo(x => x.EffectiveDateTo)
            .WithMessage("EffectiveDateFrom must be <= EffectiveDateTo")
            .WithErrorCode("INVALID_DATE_RANGE")
            .When(x => x.EffectiveDateFrom.HasValue && x.EffectiveDateTo.HasValue);

        // Search validation
        RuleFor(x => x.Search)
            .MaximumLength(100)
            .WithMessage("Search cannot exceed 100 characters")
            .WithErrorCode("SEARCH_TOO_LONG")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
    }

    private static bool IsValidPolicyStatus(string status) =>
        Enum.TryParse<PolicyStatus>(status, ignoreCase: true, out _);

    private static bool IsValidLineOfBusiness(string lob)
    {
        var normalized = lob.Replace("&", "And").Replace("A&H", "AAndH");
        return Enum.TryParse<LineOfBusiness>(normalized, ignoreCase: true, out _);
    }
}
