namespace Chubb.PolicyManagement.Application.Models;

public record PolicyFilterQuery
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string Sort { get; init; } = "createdAt,desc";
    public string? Status { get; init; }
    public string? LineOfBusiness { get; init; }
    public string? Region { get; init; }
    public DateOnly? EffectiveDateFrom { get; init; }
    public DateOnly? EffectiveDateTo { get; init; }
    public string? Search { get; init; }
}
