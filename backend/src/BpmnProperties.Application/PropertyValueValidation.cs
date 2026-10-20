using System.Text.Json;
using BpmnProperties.Domain;

namespace BpmnProperties.Application;

public static class PropertyValueValidation
{
    public static string? Validate(PropertyDefinition definition, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return definition.IsRequired ? "A required property cannot be null." : null;

        return definition.DataType switch
        {
            PropertyDataType.Text or PropertyDataType.MultilineText or PropertyDataType.SingleSelect or PropertyDataType.FileReference
                when value.ValueKind != JsonValueKind.String => "The property value must be a string.",
            PropertyDataType.Integer when value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _) => "The property value must be an integer.",
            PropertyDataType.Decimal when value.ValueKind != JsonValueKind.Number => "The property value must be a number.",
            PropertyDataType.Boolean when value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False => "The property value must be a boolean.",
            PropertyDataType.Date or PropertyDataType.DateTime when value.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(value.GetString(), out _) => "The property value must be a valid date.",
            PropertyDataType.MultiSelect when value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String) => "The property value must be an array of strings.",
            _ => null
        };
    }
}
