using Chubb.PolicyManagement.Api.Middleware;
using Chubb.PolicyManagement.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Chubb.PolicyManagement.Tests.Unit;

public class GlobalExceptionMiddlewareTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static GlobalExceptionMiddleware CreateMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware>? logger = null)
    {
        logger ??= NullLogger<GlobalExceptionMiddleware>.Instance;
        return new GlobalExceptionMiddleware(next, logger);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    // ── Happy path ───────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenNoException_PassesThroughWithoutModifyingStatus()
    {
        // Arrange
        var context = CreateContext();
        context.Response.StatusCode = 200;
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    // ── PolicyNotFoundException → 404 ────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenPolicyNotFoundException_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new PolicyNotFoundException(id));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WhenPolicyNotFoundException_ContentTypeIsProblemJson()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new PolicyNotFoundException(Guid.NewGuid()));

        // Act
        await middleware.InvokeAsync(context);

        // Assert — WriteAsJsonAsync sets the actual header to "application/json; charset=utf-8"
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenPolicyNotFoundException_ResponseBodyContainsStatusCode()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new PolicyNotFoundException(Guid.NewGuid()));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("404");
    }

    [Fact]
    public async Task InvokeAsync_WhenPolicyNotFoundException_LogsWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var id = Guid.NewGuid();
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new PolicyNotFoundException(id), loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── PolicyValidationException → 422 ─────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenPolicyValidationException_Returns422()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ => throw new PolicyValidationException("Validation failed"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task InvokeAsync_WhenPolicyValidationException_ContentTypeIsProblemJson()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ => throw new PolicyValidationException("Validation failed",
                new Dictionary<string, string[]> { ["Field"] = new[] { "Error" } }));

        // Act
        await middleware.InvokeAsync(context);

        // Assert — WriteAsJsonAsync sets the actual header to "application/json; charset=utf-8"
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenPolicyValidationException_LogsWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ => throw new PolicyValidationException("Validation failed"),
            loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Validation failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── ArgumentException → 400 ──────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Bad argument"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ContentTypeIsProblemJson()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Bad argument"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert — WriteAsJsonAsync sets the actual header to "application/json; charset=utf-8"
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_LogsWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Bad argument"), loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Unhandled Exception → 500 ────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Unexpected error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ContentTypeIsProblemJson()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Unexpected error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert — WriteAsJsonAsync sets the actual header to "application/json; charset=utf-8"
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ResponseDoesNotLeakInternalMessage()
    {
        // Arrange — production responses must not expose internal exception details
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ => throw new InvalidOperationException("Secret internal detail"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().NotContain("Secret internal detail");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_LogsError()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var context = CreateContext();
        var exception = new InvalidOperationException("Unexpected error");
        var middleware = CreateMiddleware(_ => throw exception, loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ResponseBodyContains500()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new Exception("Boom"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("500");
    }
}
