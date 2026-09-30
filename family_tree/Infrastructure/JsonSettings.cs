using System.Text.Json;
using System.Text.Json.Serialization;

namespace family_tree.Infrastructure;

/// <summary>JSON rules shared by the API and the seed loader, so both accept exactly the same input.</summary>
public static class JsonSettings
{
    public static void Configure(JsonSerializerOptions json)
    {
        // Enums travel as names ("Biological"), and numbers like 99 are rejected.
        json.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        // Constructor parameters without a default value must be present in the JSON,
        // and non-nullable properties can't be null. Violations become 400 responses.
        json.RespectRequiredConstructorParameters = true;
        json.RespectNullableAnnotations = true;
    }
}
