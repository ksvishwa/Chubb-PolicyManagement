namespace Chubb.PolicyManagement.Domain.Exceptions;

public sealed class PolicyNotFoundException : Exception
{
    public PolicyNotFoundException(Guid id)
        : base($"Policy with id '{id}' was not found.")
    {
        PolicyId = id;
    }

    public Guid PolicyId { get; }
}
