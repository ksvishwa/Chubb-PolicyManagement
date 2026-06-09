using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Application.Validators;
using FluentAssertions;
using FluentValidation;

namespace Chubb.PolicyManagement.Tests.Unit;

/// <summary>
/// Unit tests for PolicyFilterQueryValidator.
/// Covers all validation rules with 90%+ coverage.
/// </summary>
public class PolicyFilterQueryValidatorTests
{
    private readonly PolicyFilterQueryValidator _validator = new();

    #region Page Validation

    [Fact]
    public async Task Validate_WithValidPage_Succeeds()
    {
        var query = new PolicyFilterQuery { Page = 1 };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Validate_WithInvalidPage_FailsWithPageError(int invalidPage)
    {
        var query = new PolicyFilterQuery { Page = invalidPage };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Page) &&
            e.ErrorCode == "INVALID_PAGE");
    }

    [Fact]
    public async Task Validate_WithLargeValidPage_Succeeds()
    {
        var query = new PolicyFilterQuery { Page = 999999 };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Size Validation

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)]
    public async Task Validate_WithValidSize_Succeeds(int validSize)
    {
        var query = new PolicyFilterQuery { Size = validSize };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public async Task Validate_WithInvalidSize_FailsWithSizeError(int invalidSize)
    {
        var query = new PolicyFilterQuery { Size = invalidSize };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Size) &&
            e.ErrorCode == "INVALID_SIZE");
    }

    #endregion

    #region Sort Validation

    [Theory]
    [InlineData("createdAt")]
    [InlineData("createdAt,asc")]
    [InlineData("createdAt,desc")]
    [InlineData("policyNumber,asc")]
    [InlineData("status,desc")]
    public async Task Validate_WithValidSort_Succeeds(string validSort)
    {
        var query = new PolicyFilterQuery { Sort = validSort };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptySort_FailsWithSortError()
    {
        var query = new PolicyFilterQuery { Sort = string.Empty };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Sort) &&
            e.ErrorCode == "INVALID_SORT");
    }

    [Theory]
    [InlineData("invalidField")]
    [InlineData("invalidField,asc")]
    public async Task Validate_WithInvalidSortField_FailsWithSortFieldError(string invalidSort)
    {
        var query = new PolicyFilterQuery { Sort = invalidSort };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Sort) &&
            e.ErrorCode == "INVALID_SORT_FIELD");
    }

    [Theory]
    [InlineData("createdAt,invalid")]
    [InlineData("createdAt,ascending")]
    public async Task Validate_WithInvalidSortDirection_FailsWithDirectionError(string invalidSort)
    {
        var query = new PolicyFilterQuery { Sort = invalidSort };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Sort) &&
            e.ErrorCode == "INVALID_SORT_DIRECTION");
    }

    [Fact]
    public async Task Validate_WithMalformedSort_FailsWithSortFormatError()
    {
        var query = new PolicyFilterQuery { Sort = "createdAt,asc,extra" };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Sort) &&
            e.ErrorCode == "INVALID_SORT_FORMAT");
    }

    #endregion

    #region Status Filter Validation

    [Theory]
    [InlineData("Active")]
    [InlineData("active")]
    [InlineData("ACTIVE")]
    [InlineData("Expired")]
    [InlineData("Pending")]
    [InlineData("Cancelled")]
    public async Task Validate_WithValidStatus_Succeeds(string validStatus)
    {
        var query = new PolicyFilterQuery { Status = validStatus };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullStatus_Succeeds()
    {
        var query = new PolicyFilterQuery { Status = null };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("Archive")]
    [InlineData("Suspend")]
    public async Task Validate_WithInvalidStatus_FailsWithStatusError(string invalidStatus)
    {
        var query = new PolicyFilterQuery { Status = invalidStatus };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Status) &&
            e.ErrorCode == "INVALID_STATUS");
    }

    #endregion

    #region Line of Business Validation

    [Theory]
    [InlineData("Property")]
    [InlineData("property")]
    [InlineData("Casualty")]
    [InlineData("A&H")]
    [InlineData("Marine")]
    public async Task Validate_WithValidLineOfBusiness_Succeeds(string validLob)
    {
        var query = new PolicyFilterQuery { LineOfBusiness = validLob };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullLineOfBusiness_Succeeds()
    {
        var query = new PolicyFilterQuery { LineOfBusiness = null };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("InvalidLob")]
    [InlineData("Health")]
    [InlineData("Life")]
    public async Task Validate_WithInvalidLineOfBusiness_FailsWithLobError(string invalidLob)
    {
        var query = new PolicyFilterQuery { LineOfBusiness = invalidLob };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.LineOfBusiness) &&
            e.ErrorCode == "INVALID_LINE_OF_BUSINESS");
    }

    #endregion

    #region Region Validation

    [Theory]
    [InlineData("Singapore")]
    [InlineData("Hong Kong")]
    [InlineData("A")]
    public async Task Validate_WithValidRegion_Succeeds(string validRegion)
    {
        var query = new PolicyFilterQuery { Region = validRegion };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullRegion_Succeeds()
    {
        var query = new PolicyFilterQuery { Region = null };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithRegionTooLong_FailsWithRegionError()
    {
        var query = new PolicyFilterQuery { Region = new string('A', 51) };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Region) &&
            e.ErrorCode == "REGION_TOO_LONG");
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public async Task Validate_WithValidDateRange_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var query = new PolicyFilterQuery
        {
            EffectiveDateFrom = today,
            EffectiveDateTo = tomorrow
        };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithSameDateRange_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new PolicyFilterQuery
        {
            EffectiveDateFrom = today,
            EffectiveDateTo = today
        };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFromAfterTo_FailsWithDateRangeError()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var query = new PolicyFilterQuery
        {
            EffectiveDateFrom = today,
            EffectiveDateTo = yesterday
        };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.EffectiveDateFrom) &&
            e.ErrorCode == "INVALID_DATE_RANGE");
    }

    [Fact]
    public async Task Validate_WithOnlyFromDate_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new PolicyFilterQuery { EffectiveDateFrom = today };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithOnlyToDate_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new PolicyFilterQuery { EffectiveDateTo = today };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Search Validation

    [Theory]
    [InlineData("POL-")]
    [InlineData("John")]
    [InlineData("A")]
    public async Task Validate_WithValidSearch_Succeeds(string validSearch)
    {
        var query = new PolicyFilterQuery { Search = validSearch };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullSearch_Succeeds()
    {
        var query = new PolicyFilterQuery { Search = null };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithSearchTooLong_FailsWithSearchError()
    {
        var query = new PolicyFilterQuery { Search = new string('A', 101) };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(PolicyFilterQuery.Search) &&
            e.ErrorCode == "SEARCH_TOO_LONG");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Validate_WithAllFieldsValid_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new PolicyFilterQuery
        {
            Page = 1,
            Size = 50,
            Sort = "premiumAmount,desc",
            Status = "Active",
            LineOfBusiness = "Property",
            Region = "Singapore",
            EffectiveDateFrom = today,
            EffectiveDateTo = today.AddDays(30),
            Search = "POL-"
        };
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var query = new PolicyFilterQuery
        {
            Page = -1,
            Size = 200,
            Status = "InvalidStatus",
            Search = new string('A', 101)
        };
        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion
}
