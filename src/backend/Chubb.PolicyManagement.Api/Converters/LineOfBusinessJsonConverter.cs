using System.Text.Json;
using System.Text.Json.Serialization;
using Chubb.PolicyManagement.Domain.Enums;

namespace Chubb.PolicyManagement.Api.Converters;

/// <summary>
/// Serializes <see cref="LineOfBusiness.AAndH"/> as "A&amp;H" to match the canonical APAC label.
/// </summary>
public sealed class LineOfBusinessJsonConverter : JsonConverter<LineOfBusiness>
{
    public override LineOfBusiness Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "A&H"     => LineOfBusiness.AAndH,
            "Property" => LineOfBusiness.Property,
            "Casualty" => LineOfBusiness.Casualty,
            "Marine"   => LineOfBusiness.Marine,
            _          => Enum.Parse<LineOfBusiness>(value!)
        };
    }

    public override void Write(Utf8JsonWriter writer, LineOfBusiness value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == LineOfBusiness.AAndH ? "A&H" : value.ToString());
    }
}
