using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Interfaces;
using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Application.Validators;
using Chubb.PolicyManagement.Domain.Enums;
using Chubb.PolicyManagement.Domain.Exceptions;
using Chubb.PolicyManagement.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Chubb.PolicyManagement.Application.Services;

public sealed class PolicyService(
    IPolicyRepository repository,
    ILogger<PolicyService> logger,
    IValidator<PolicyFilterQuery> policyFilterValidator,
    IValidator<BulkFlagRequest> bulkFlagValidator) : IPolicyService
{
    public async Task<PagedResult<PolicyDto>> GetPoliciesAsync(PolicyFilterQuery query, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationResult = await policyFilterValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            
            throw new PolicyValidationException("Policy filter validation failed.", errors);
        }

        var size = Math.Min(query.Size, 100);
        var page = Math.Max(query.Page, 1);

        var sortParts = query.Sort.Split(',');
        var sortField = sortParts.Length > 0 ? sortParts[0] : "createdAt";
        var sortDirection = sortParts.Length > 1 ? sortParts[1] : "desc";

        PolicyStatus? status = Enum.TryParse<PolicyStatus>(query.Status, ignoreCase: true, out var parsedStatus)
            ? parsedStatus : null;

        LineOfBusiness? lob = Enum.TryParse<LineOfBusiness>(query.LineOfBusiness?.Replace("&", "And").Replace("A&H", "AAndH"),
            ignoreCase: true, out var parsedLob) ? parsedLob : null;

        var (items, totalCount) = await repository.GetPagedAsync(
            page, size, sortField, sortDirection, status, lob,
            query.Region, query.EffectiveDateFrom, query.EffectiveDateTo,
            query.Search, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)size);

        logger.LogInformation("Retrieved {Count} policies (page {Page}/{TotalPages})", dtos.Count, page, totalPages);

        return new PagedResult<PolicyDto>(dtos, page, size, totalCount, totalPages);
    }

    public async Task<PolicyDto> GetPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await repository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
            throw new PolicyNotFoundException(id);

        return MapToDto(policy);
    }

    public async Task BulkFlagPoliciesAsync(BulkFlagRequest request, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationResult = await bulkFlagValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            
            throw new PolicyValidationException("Bulk flag request validation failed.", errors);
        }

        var ids = request.PolicyIds.ToList();
        logger.LogInformation("Bulk flagging {Count} policies for review", ids.Count);
        await repository.BulkFlagAsync(ids, cancellationToken);
    }

    public async Task<PolicySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await repository.GetSummaryAsync(cancellationToken);
        return new PolicySummaryDto(
            summary.TotalPolicies,
            summary.ActivePolicies,
            summary.ExpiredPolicies,
            summary.PendingPolicies,
            summary.CancelledPolicies,
            summary.FlaggedForReview,
            summary.TotalPremiumAmount,
            summary.ByLineOfBusiness,
            summary.ByRegion);
    }

    private static string FormatLineOfBusiness(LineOfBusiness lob) =>
        lob == LineOfBusiness.AAndH ? "A&H" : lob.ToString();

    private static PolicyDto MapToDto(Domain.Entities.Policy p) => new(
        p.Id,
        p.PolicyNumber,
        p.PolicyholderName,
        FormatLineOfBusiness(p.LineOfBusiness),
        p.Status.ToString(),
        p.PremiumAmount,
        p.Currency,
        p.EffectiveDate,
        p.ExpiryDate,
        p.Region,
        p.Underwriter,
        p.FlaggedForReview,
        p.CreatedAt,
        p.UpdatedAt);
}
