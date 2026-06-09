using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Application.Services;
using Chubb.PolicyManagement.Application.Validators;
using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;
using Chubb.PolicyManagement.Domain.Exceptions;
using Chubb.PolicyManagement.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Chubb.PolicyManagement.Tests.Unit;

public class PolicyServiceTests
{
    private readonly Mock<IPolicyRepository> _repositoryMock = new();
    private readonly Mock<IValidator<PolicyFilterQuery>> _filterValidatorMock = new();
    private readonly Mock<IValidator<BulkFlagRequest>> _bulkFlagValidatorMock = new();
    private readonly PolicyService _sut;

    public PolicyServiceTests()
    {
        // Set up default validator behavior to pass validation
        _filterValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _bulkFlagValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<BulkFlagRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _sut = new PolicyService(
            _repositoryMock.Object,
            NullLogger<PolicyService>.Instance,
            _filterValidatorMock.Object,
            _bulkFlagValidatorMock.Object);
    }

    [Fact]
    public async Task GetPoliciesAsync_WithNoFilters_ReturnsPagedResult()
    {
        var policies = new List<Policy>
        {
            CreatePolicy(Guid.NewGuid(), "POL-001", PolicyStatus.Active),
            CreatePolicy(Guid.NewGuid(), "POL-002", PolicyStatus.Expired)
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, "createdAt", "desc", null, null, null, null, null, null, default))
            .ReturnsAsync((policies.AsReadOnly(), 2));

        var query = new PolicyFilterQuery();
        var result = await _sut.GetPoliciesAsync(query);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPoliciesAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        var activePolicies = new List<Policy>
        {
            CreatePolicy(Guid.NewGuid(), "POL-001", PolicyStatus.Active)
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, "createdAt", "desc", PolicyStatus.Active, null, null, null, null, null, default))
            .ReturnsAsync((activePolicies.AsReadOnly(), 1));

        var query = new PolicyFilterQuery { Status = "Active" };
        var result = await _sut.GetPoliciesAsync(query);

        result.Data.Should().HaveCount(1);
        result.Data[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetPolicyByIdAsync_WithExistingId_ReturnsPolicyDto()
    {
        var id = Guid.NewGuid();
        var policy = CreatePolicy(id, "POL-001", PolicyStatus.Active);
        _repositoryMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(policy);

        var result = await _sut.GetPolicyByIdAsync(id);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.PolicyNumber.Should().Be("POL-001");
    }

    [Fact]
    public async Task GetPolicyByIdAsync_WithNonExistentId_ThrowsPolicyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Policy?)null);

        var act = async () => await _sut.GetPolicyByIdAsync(id);

        await act.Should().ThrowAsync<PolicyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task BulkFlagPoliciesAsync_WithValidIds_CallsRepository()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var request = new BulkFlagRequest { PolicyIds = ids };

        _repositoryMock.Setup(r => r.BulkFlagAsync(ids, default)).Returns(Task.CompletedTask);

        await _sut.BulkFlagPoliciesAsync(request);

        _repositoryMock.Verify(r => r.BulkFlagAsync(It.IsAny<IEnumerable<Guid>>(), default), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAggregatedSummary()
    {
        var summary = new PolicySummary(100, 60, 20, 15, 5, 10, 5_000_000m,
            new Dictionary<string, int> { ["Property"] = 50, ["Marine"] = 50 },
            new Dictionary<string, int> { ["Singapore"] = 100 });

        _repositoryMock.Setup(r => r.GetSummaryAsync(default)).ReturnsAsync(summary);

        var result = await _sut.GetSummaryAsync();

        result.TotalPolicies.Should().Be(100);
        result.ActivePolicies.Should().Be(60);
        result.TotalPremiumAmount.Should().Be(5_000_000m);
    }

    [Fact]
    public async Task GetPoliciesAsync_SizeCappedAt100_WhenSizeExceeds100()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 100, It.IsAny<string>(), It.IsAny<string>(), null, null, null, null, null, null, default))
            .ReturnsAsync((new List<Policy>().AsReadOnly(), 0));

        var query = new PolicyFilterQuery { Size = 500 };
        var result = await _sut.GetPoliciesAsync(query);

        result.Size.Should().Be(100);
    }

    private static Policy CreatePolicy(Guid id, string number, PolicyStatus status) => new()
    {
        Id = id,
        PolicyNumber = number,
        PolicyholderName = "Test Holder",
        LineOfBusiness = LineOfBusiness.Property,
        Status = status,
        PremiumAmount = 10_000m,
        Currency = "USD",
        EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
        ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
        Region = "Singapore",
        Underwriter = "John Smith",
        FlaggedForReview = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    #region Validation Tests

    [Fact]
    public async Task GetPoliciesAsync_WithInvalidFilter_ThrowsValidationException()
    {
        var query = new PolicyFilterQuery { Page = -1 };
        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Page", "Page must be >= 1") { ErrorCode = "INVALID_PAGE" }
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);

        _filterValidatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var act = async () => await _sut.GetPoliciesAsync(query);

        await act.Should().ThrowAsync<PolicyValidationException>()
            .WithMessage("*Policy filter validation failed*");
    }

    [Fact]
    public async Task GetPoliciesAsync_WithValidFilterAfterFix_Succeeds()
    {
        var query = new PolicyFilterQuery { Page = 1, Size = 20 };
        var policies = new List<Policy>
        {
            CreatePolicy(Guid.NewGuid(), "POL-001", PolicyStatus.Active)
        };

        _filterValidatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, "createdAt", "desc", null, null, null, null, null, null, default))
            .ReturnsAsync((policies.AsReadOnly(), 1));

        var result = await _sut.GetPoliciesAsync(query);

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task BulkFlagPoliciesAsync_WithInvalidRequest_ThrowsValidationException()
    {
        var request = new BulkFlagRequest { PolicyIds = Array.Empty<Guid>() };
        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new("PolicyIds", "At least one policy ID is required") { ErrorCode = "EMPTY_POLICY_IDS" }
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);

        _bulkFlagValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var act = async () => await _sut.BulkFlagPoliciesAsync(request);

        await act.Should().ThrowAsync<PolicyValidationException>()
            .WithMessage("*Bulk flag request validation failed*");
    }

    [Fact]
    public async Task BulkFlagPoliciesAsync_WithDuplicateIds_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = new BulkFlagRequest { PolicyIds = new[] { id, id } };
        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new("PolicyIds", "PolicyIds cannot contain duplicate values") { ErrorCode = "DUPLICATE_POLICY_IDS" }
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);

        _bulkFlagValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var act = async () => await _sut.BulkFlagPoliciesAsync(request);

        await act.Should().ThrowAsync<PolicyValidationException>()
            .WithMessage("*Bulk flag request validation failed*");
    }

    #endregion
}
