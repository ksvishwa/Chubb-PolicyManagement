using Chubb.PolicyManagement.Api;
using Chubb.PolicyManagement.Api.Controllers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chubb.PolicyManagement.Tests.Unit;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _sut =
        new(NullLogger<WeatherForecastController>.Instance);

    [Fact]
    public void Get_ReturnsExactlyFiveForecasts()
    {
        // Act
        var result = _sut.Get().ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void Get_AllForecastsHaveFutureDates()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var result = _sut.Get().ToList();

        // Assert
        result.Should().AllSatisfy(f => f.Date.Should().BeOnOrAfter(today.AddDays(1)));
    }

    [Fact]
    public void Get_AllForecastsHaveTemperatureInRange()
    {
        // Act
        var result = _sut.Get().ToList();

        // Assert
        result.Should().AllSatisfy(f =>
        {
            f.TemperatureC.Should().BeInRange(-20, 55);
        });
    }

    [Fact]
    public void Get_AllForecastsHaveNonNullSummary()
    {
        // Act
        var result = _sut.Get().ToList();

        // Assert
        result.Should().AllSatisfy(f => f.Summary.Should().NotBeNullOrEmpty());
    }

    // ── WeatherForecast entity ────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-20)]
    public void WeatherForecast_TemperatureF_DerivedFromTemperatureC(int tempC)
    {
        // Arrange
        var forecast = new WeatherForecast { TemperatureC = tempC };
        var expectedF = 32 + (int)(tempC / 0.5556);

        // Act / Assert — property is a computed expression, not stored
        forecast.TemperatureF.Should().Be(expectedF);
    }

    [Fact]
    public void WeatherForecast_PropertiesCanBeAssigned()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var forecast = new WeatherForecast
        {
            Date = date,
            TemperatureC = 25,
            Summary = "Warm"
        };

        // Assert
        forecast.Date.Should().Be(date);
        forecast.TemperatureC.Should().Be(25);
        forecast.Summary.Should().Be("Warm");
    }
}
