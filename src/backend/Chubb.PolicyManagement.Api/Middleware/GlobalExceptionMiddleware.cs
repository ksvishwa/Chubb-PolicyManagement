using Chubb.PolicyManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Chubb.PolicyManagement.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PolicyValidationException ex)
        {
            logger.LogWarning("Validation failed: {Message}", ex.Message);
            await WriteValidationProblemDetailsAsync(context, ex);
        }
        catch (PolicyNotFoundException ex)
        {
            logger.LogWarning("Policy not found: {PolicyId}", ex.PolicyId);
            await WriteProblemDetailsAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid argument: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteValidationProblemDetailsAsync(HttpContext context, PolicyValidationException ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var problem = new ProblemDetails
        {
            Type = "https://api.chubb.local/errors/validation-failed",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = ex.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["errors"] = ex.Errors;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
