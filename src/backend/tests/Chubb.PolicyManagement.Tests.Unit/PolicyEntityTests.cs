using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;
using Chubb.PolicyManagement.Domain.Exceptions;
using FluentAssertions;

namespace Chubb.PolicyManagement.Tests.Unit;

public class PolicyEntityTests
{
    [Fact]
    public void Policy_DefaultFlaggedForReview_IsFalse()
    {
        var policy = new Policy();
        policy.FlaggedForReview.Should().BeFalse();
    }

    [Fact]
    public void PolicyNotFoundException_ContainsPolicyId()
    {
        var id = Guid.NewGuid();
        var ex = new PolicyNotFoundException(id);

        ex.PolicyId.Should().Be(id);
        ex.Message.Should().Contain(id.ToString());
    }

    [Theory]
    [InlineData(PolicyStatus.Active)]
    [InlineData(PolicyStatus.Expired)]
    [InlineData(PolicyStatus.Pending)]
    [InlineData(PolicyStatus.Cancelled)]
    public void Policy_AllStatuses_AreValid(PolicyStatus status)
    {
        var policy = new Policy { Status = status };
        policy.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(LineOfBusiness.Property)]
    [InlineData(LineOfBusiness.Casualty)]
    [InlineData(LineOfBusiness.AAndH)]
    [InlineData(LineOfBusiness.Marine)]
    public void Policy_AllLinesOfBusiness_AreValid(LineOfBusiness lob)
    {
        var policy = new Policy { LineOfBusiness = lob };
        policy.LineOfBusiness.Should().Be(lob);
    }

    // ── Default string properties ────────────────────────────────────────

    [Fact]
    public void Policy_DefaultPolicyNumber_IsEmptyString()
    {
        var policy = new Policy();
        policy.PolicyNumber.Should().BeEmpty();
    }

    [Fact]
    public void Policy_DefaultPolicyholderName_IsEmptyString()
    {
        var policy = new Policy();
        policy.PolicyholderName.Should().BeEmpty();
    }

    [Fact]
    public void Policy_DefaultCurrency_IsEmptyString()
    {
        var policy = new Policy();
        policy.Currency.Should().BeEmpty();
    }

    [Fact]
    public void Policy_DefaultRegion_IsEmptyString()
    {
        var policy = new Policy();
        policy.Region.Should().BeEmpty();
    }

    [Fact]
    public void Policy_DefaultUnderwriter_IsEmptyString()
    {
        var policy = new Policy();
        policy.Underwriter.Should().BeEmpty();
    }

    [Fact]
    public void Policy_DefaultId_IsEmptyGuid()
    {
        var policy = new Policy();
        policy.Id.Should().Be(Guid.Empty);
    }

    // ── Full property assignment ─────────────────────────────────────────

    [Fact]
    public void Policy_AllPropertiesCanBeAssigned()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var policy = new Policy
        {
            Id = id,
            PolicyNumber = "POL-001",
            PolicyholderName = "Jane Doe",
            LineOfBusiness = LineOfBusiness.Marine,
            Status = PolicyStatus.Active,
            PremiumAmount = 12_345.67m,
            Currency = "SGD",
            EffectiveDate = today,
            ExpiryDate = today.AddYears(1),
            Region = "Singapore",
            Underwriter = "Alice",
            FlaggedForReview = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        policy.Id.Should().Be(id);
        policy.PolicyNumber.Should().Be("POL-001");
        policy.PolicyholderName.Should().Be("Jane Doe");
        policy.LineOfBusiness.Should().Be(LineOfBusiness.Marine);
        policy.Status.Should().Be(PolicyStatus.Active);
        policy.PremiumAmount.Should().Be(12_345.67m);
        policy.Currency.Should().Be("SGD");
        policy.EffectiveDate.Should().Be(today);
        policy.ExpiryDate.Should().Be(today.AddYears(1));
        policy.Region.Should().Be("Singapore");
        policy.Underwriter.Should().Be("Alice");
        policy.FlaggedForReview.Should().BeTrue();
        policy.CreatedAt.Should().Be(now);
        policy.UpdatedAt.Should().Be(now);
    }

    // ── PolicyNotFoundException ──────────────────────────────────────────

    [Fact]
    public void PolicyNotFoundException_MessageContainsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var ex = new PolicyNotFoundException(id);

        // Assert
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void PolicyNotFoundException_PolicyId_MatchesPassedId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var ex = new PolicyNotFoundException(id);

        // Assert
        ex.PolicyId.Should().Be(id);
    }

    [Fact]
    public void PolicyNotFoundException_IsSubclassOfException()
    {
        var ex = new PolicyNotFoundException(Guid.NewGuid());
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── PolicyValidationException ────────────────────────────────────────

    [Fact]
    public void PolicyValidationException_WithMessage_StoresMessageAndEmptyErrors()
    {
        // Arrange / Act
        var ex = new PolicyValidationException("Validation failed");

        // Assert
        ex.Message.Should().Be("Validation failed");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PolicyValidationException_WithMessageAndErrors_StoresErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "PolicyNumber", new[] { "Required", "Too short" } }
        };

        // Act
        var ex = new PolicyValidationException("Validation failed", errors);

        // Assert
        ex.Message.Should().Be("Validation failed");
        ex.Errors.Should().ContainKey("PolicyNumber");
        ex.Errors["PolicyNumber"].Should().Contain("Required");
        ex.Errors["PolicyNumber"].Should().Contain("Too short");
    }

    [Fact]
    public void PolicyValidationException_IsSubclassOfException()
    {
        var ex = new PolicyValidationException("msg");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── Enum value names ─────────────────────────────────────────────────

    [Theory]
    [InlineData(PolicyStatus.Active, "Active")]
    [InlineData(PolicyStatus.Expired, "Expired")]
    [InlineData(PolicyStatus.Pending, "Pending")]
    [InlineData(PolicyStatus.Cancelled, "Cancelled")]
    public void PolicyStatus_AllValuesHaveCorrectName(PolicyStatus status, string expectedName)
    {
        status.ToString().Should().Be(expectedName);
    }

    [Theory]
    [InlineData(LineOfBusiness.Property, "Property")]
    [InlineData(LineOfBusiness.Casualty, "Casualty")]
    [InlineData(LineOfBusiness.AAndH, "AAndH")]
    [InlineData(LineOfBusiness.Marine, "Marine")]
    public void LineOfBusiness_AllValuesHaveCorrectName(LineOfBusiness lob, string expectedName)
    {
        lob.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void PolicyStatus_HasExactlyFourValues()
    {
        Enum.GetValues<PolicyStatus>().Should().HaveCount(4);
    }

    [Fact]
    public void LineOfBusiness_HasExactlyFourValues()
    {
        Enum.GetValues<LineOfBusiness>().Should().HaveCount(4);
    }
}
