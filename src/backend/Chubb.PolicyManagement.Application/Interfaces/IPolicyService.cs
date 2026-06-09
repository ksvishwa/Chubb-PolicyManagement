using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Models;

namespace Chubb.PolicyManagement.Application.Interfaces;

public interface IPolicyService
{
    Task<PagedResult<PolicyDto>> GetPoliciesAsync(PolicyFilterQuery query, CancellationToken cancellationToken = default);
    Task<PolicyDto> GetPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task BulkFlagPoliciesAsync(BulkFlagRequest request, CancellationToken cancellationToken = default);
    Task<PolicySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
