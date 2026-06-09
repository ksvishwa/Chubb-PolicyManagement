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
}
