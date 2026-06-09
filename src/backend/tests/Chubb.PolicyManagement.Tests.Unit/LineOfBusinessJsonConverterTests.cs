using System.Text.Encodings.Web;
using System.Text.Json;
using Chubb.PolicyManagement.Api.Converters;
using Chubb.PolicyManagement.Domain.Enums;
using FluentAssertions;

namespace Chubb.PolicyManagement.Tests.Unit;

public class LineOfBusinessJsonConverterTests
{
    // Use UnsafeRelaxedJsonEscaping so that '&' is written as '&' rather than '\u0026',
    // matching the intended API wire format.
    private readonly JsonSerializerOptions _options;

    public LineOfBusinessJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        _options.Converters.Add(new LineOfBusinessJsonConverter());
    }

    // ── Write (serialization) ────────────────────────────────────────────

    [Theory]
    [InlineData(LineOfBusiness.Property, "\"Property\"")]
    [InlineData(LineOfBusiness.Casualty, "\"Casualty\"")]
    [InlineData(LineOfBusiness.Marine, "\"Marine\"")]
    public void Write_NonAAndH_SerializesAsEnumName(LineOfBusiness lob, string expectedJson)
    {
        // Act
        var json = JsonSerializer.Serialize(lob, _options);

        // Assert
        json.Should().Be(expectedJson);
    }

    [Fact]
    public void Write_AAndH_SerializesAsAmpersandH()
    {
        // Act
        var json = JsonSerializer.Serialize(LineOfBusiness.AAndH, _options);

        // Assert — must NOT be "AAndH"; must be "A&H"
        json.Should().Be("\"A&H\"");
        json.Should().NotBe("\"AAndH\"");
    }

    // ── Read (deserialization) ───────────────────────────────────────────

    [Theory]
    [InlineData("\"Property\"", LineOfBusiness.Property)]
    [InlineData("\"Casualty\"", LineOfBusiness.Casualty)]
    [InlineData("\"Marine\"", LineOfBusiness.Marine)]
    public void Read_KnownString_DeserializesCorrectly(string json, LineOfBusiness expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<LineOfBusiness>(json, _options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Read_AmpersandHString_DeserializesAsAAndH()
    {
        // Act
        var result = JsonSerializer.Deserialize<LineOfBusiness>("\"A&H\"", _options);

        // Assert
        result.Should().Be(LineOfBusiness.AAndH);
    }

    [Fact]
    public void Read_AAndHByEnumName_FallsBackToEnumParse()
    {
        // Arrange — "AAndH" is the raw enum name; the switch fallback calls Enum.Parse
        var result = JsonSerializer.Deserialize<LineOfBusiness>("\"AAndH\"", _options);

        // Assert
        result.Should().Be(LineOfBusiness.AAndH);
    }

    [Fact]
    public void Read_UnknownString_ThrowsException()
    {
        // Arrange
        var json = "\"UnknownLob\"";

        // Act — Enum.Parse throws ArgumentException; wrapped by JSON serializer
        var act = () => JsonSerializer.Deserialize<LineOfBusiness>(json, _options);

        // Assert
        act.Should().Throw<Exception>();
    }

    // ── Round-trip ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(LineOfBusiness.Property)]
    [InlineData(LineOfBusiness.Casualty)]
    [InlineData(LineOfBusiness.Marine)]
    [InlineData(LineOfBusiness.AAndH)]
    public void RoundTrip_AllValues_PreservesValue(LineOfBusiness lob)
    {
        // Arrange
        var json = JsonSerializer.Serialize(lob, _options);

        // Act
        var deserialized = JsonSerializer.Deserialize<LineOfBusiness>(json, _options);

        // Assert
        deserialized.Should().Be(lob);
    }

    [Fact]
    public void RoundTrip_AAndH_PreservesAmpersandRepresentation()
    {
        // Arrange
        var json = JsonSerializer.Serialize(LineOfBusiness.AAndH, _options);

        // Assert intermediate — JSON wire format must be "A&H"
        json.Should().Be("\"A&H\"");

        // Act
        var deserialized = JsonSerializer.Deserialize<LineOfBusiness>(json, _options);

        // Assert round-trip
        deserialized.Should().Be(LineOfBusiness.AAndH);
    }

    // ── Object wrapping ──────────────────────────────────────────────────

    [Fact]
    public void Write_InsideObject_SerializesCorrectly()
    {
        // Arrange
        var obj = new { Lob = LineOfBusiness.AAndH };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        json.Should().Contain("A&H");
    }
}
