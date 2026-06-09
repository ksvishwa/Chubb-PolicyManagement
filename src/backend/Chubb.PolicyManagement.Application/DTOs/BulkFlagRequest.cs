namespace Chubb.PolicyManagement.Application.DTOs;

public record BulkFlagRequest
{
    public required IEnumerable<Guid> PolicyIds { get; init; }
}
