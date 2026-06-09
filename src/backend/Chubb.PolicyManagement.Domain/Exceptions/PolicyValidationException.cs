namespace Chubb.PolicyManagement.Domain.Exceptions;

/// <summary>
/// Thrown when input validation fails for a policy-related operation.
/// Maps to HTTP 422 Unprocessable Entity per RFC 7807.
/// </summary>
public sealed class PolicyValidationException : Exception
{
    public PolicyValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public PolicyValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// Field-level validation errors, keyed by property name.
    /// </summary>
    public Dictionary<string, string[]> Errors { get; }
}
