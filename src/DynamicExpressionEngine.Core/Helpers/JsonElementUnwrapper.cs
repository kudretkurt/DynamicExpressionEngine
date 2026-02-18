using System.Text.Json;

namespace DynamicExpressionEngine.Core.Helpers;

public static class JsonElementUnwrapper
{
    public static object? Unwrap(object? value)
    {
        if (value is null)
            return null;

        if (value is JsonElement json)
            return Unwrap(json);

        return value;
    }

    public static object? Unwrap(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number => json.TryGetDecimal(out var dec) ? dec : json.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,

            // IMPORTANT: do not turn objects/arrays into strings
            JsonValueKind.Object => json,
            JsonValueKind.Array => json,

            _ => json.GetRawText()
        };
    }
}