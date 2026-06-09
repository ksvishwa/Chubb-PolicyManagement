using Chubb.PolicyManagement.Api.Controllers;
using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Interfaces;
using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Chubb.PolicyManagement.Tests.Unit;

public class PoliciesControllerTests
{
    private readonly Mock<IPolicyService> _serviceMock = new();
    private readonly PoliciesController _sut;

    public PoliciesControllerTests()
    {
        _sut = new PoliciesController(_serviceMock.Object);
    }

    // ── GetPolicies ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicies_WithDefaultParams_Returns200WithPagedResult()
    {
        // Arrange
        var expected = new PagedResult<PolicyDto>(new List<PolicyDto>(), 1, 20, 0, 0);
        _serviceMock
            .Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetPolicies();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPolicies_WithMatchingFilters_Returns200WithData()
    {
        // Arrange
        var dto = CreatePolicyDto(Guid.NewGuid(), "POL-001", "Active");
        var paged = new PagedResult<PolicyDto>(new List<PolicyDto> { dto }, 1, 20, 1, 1);
        _serviceMock
            .Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _sut.GetPolicies(status: "Active");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = ok.Value.Should().BeOfType<PagedResult<PolicyDto>>().Subject;
        pagedResult.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPolicies_WithEmptyResult_Returns200WithEmptyData()
    {
        // Arrange
        var expected = new PagedResult<PolicyDto>(new List<PolicyDto>(), 1, 20, 0, 0);
        _serviceMock
            .Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetPolicies();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = ok.Value.Should().BeOfType<PagedResult<PolicyDto>>().Subject;
        pagedResult.Data.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPolicies_WithPageLessThan1_Returns400BadRequest(int invalidPage)
    {
        // Act
        var result = await _sut.GetPolicies(page: invalidPage);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(s =>
            s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPolicies_WithSizeLessThan1_Returns400BadRequest(int invalidSize)
    {
        // Act
        var result = await _sut.GetPolicies(size: invalidSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(s =>
            s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPolicies_WithSizeOver100_Returns400BadRequest()
    {
        // Act
        var result = await _sut.GetPolicies(size: 101);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(s =>
            s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPolicies_WithSize100_Returns200()
    {
        // Arrange — boundary: size == 100 is valid
        var expected = new PagedResult<PolicyDto>(new List<PolicyDto>(), 1, 100, 0, 0);
        _serviceMock
            .Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetPolicies(size: 100);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPolicies_PassesAllQueryParamsToService()
    {
        // Arrange
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 12, 31);
        var expected = new PagedResult<PolicyDto>(new List<PolicyDto>(), 2, 10, 0, 0);

        PolicyFilterQuery? capturedQuery = null;
        _serviceMock
            .Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PolicyFilterQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetPolicies(
            page: 2, size: 10, sort: "premiumAmount,asc",
            status: "Active", lineOfBusiness: "Marine",
            region: "Singapore", effectiveDateFrom: from, effectiveDateTo: to, search: "POL");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.Page.Should().Be(2);
        capturedQuery.Size.Should().Be(10);
        capturedQuery.Sort.Should().Be("premiumAmount,asc");
        capturedQuery.Status.Should().Be("Active");
        capturedQuery.LineOfBusiness.Should().Be("Marine");
        capturedQuery.Region.Should().Be("Singapore");
        capturedQuery.EffectiveDateFrom.Should().Be(from);
        capturedQuery.EffectiveDateTo.Should().Be(to);
        capturedQuery.Search.Should().Be("POL");
    }

    // ── GetPolicyById ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicyById_WithValidId_Returns200WithPolicyDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = CreatePolicyDto(id, "POL-001", "Active");
        _serviceMock.Setup(s => s.GetPolicyByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _sut.GetPolicyById(id);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetPolicyById_WhenServiceThrowsPolicyNotFoundException_PropagatesException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.GetPolicyByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PolicyNotFoundException(id));

        // Act
        var act = async () => await _sut.GetPolicyById(id);

        // Assert — middleware handles this; controller propagates
        await act.Should().ThrowAsync<PolicyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    // ── BulkFlagPolicies ─────────────────────────────────────────────────

    [Fact]
    public async Task BulkFlagPolicies_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        var request = new BulkFlagRequest { PolicyIds = new[] { Guid.NewGuid(), Guid.NewGuid() } };
        _serviceMock
            .Setup(s => s.BulkFlagPoliciesAsync(request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.BulkFlagPolicies(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s =>
            s.BulkFlagPoliciesAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkFlagPolicies_WhenServiceThrowsValidationException_PropagatesException()
    {
        // Arrange
        var request = new BulkFlagRequest { PolicyIds = Array.Empty<Guid>() };
        _serviceMock
            .Setup(s => s.BulkFlagPoliciesAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PolicyValidationException("At least one policy ID is required"));

        // Act
        var act = async () => await _sut.BulkFlagPolicies(request);

        // Assert
        await act.Should().ThrowAsync<PolicyValidationException>();
    }

    // ── GetSummary ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_Returns200WithPolicySummaryDto()
    {
        // Arrange
        var dto = new PolicySummaryDto(
            100, 60, 20, 10, 10, 5, 999m,
            new Dictionary<string, int> { ["Property"] = 100 },
            new Dictionary<string, int> { ["Singapore"] = 100 });

        _serviceMock.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _sut.GetSummary();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetSummary_WhenDatabaseEmpty_Returns200WithZeroedSummary()
    {
        // Arrange
        var dto = new PolicySummaryDto(0, 0, 0, 0, 0, 0, 0m,
            new Dictionary<string, int>(),
            new Dictionary<string, int>());

        _serviceMock.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _sut.GetSummary();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = ok.Value.Should().BeOfType<PolicySummaryDto>().Subject;
        summary.TotalPolicies.Should().Be(0);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static PolicyDto CreatePolicyDto(Guid id, string policyNumber, string status) =>
        new(id, policyNumber, "Test Holder", "Property", status,
            10_000m, "USD",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            "Singapore", "John Smith", false,
            DateTime.UtcNow, DateTime.UtcNow);
}
