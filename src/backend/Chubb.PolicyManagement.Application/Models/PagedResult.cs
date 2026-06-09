namespace Chubb.PolicyManagement.Application.Models;

public record PagedResult<T>(
    IReadOnlyList<T> Data,
    int Page,
    int Size,
    int TotalCount,
    int TotalPages);
