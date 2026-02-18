using System.Globalization;
using System.Text.Json;
using DynamicExpressionEngine.Core.Helpers;

public static class DataPathResolver
{
    public static bool TryResolve(IReadOnlyDictionary<string, object?> data, string rawPath, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(rawPath)) return false;

        var path = NormalizePath(rawPath);

        // 1) exact match first (flat key like "person.name")
        if (TryGetDictionaryValue(data, path, out var direct))
        {
            value = JsonElementUnwrapper.Unwrap(direct);
            return true;
        }

        // 2) resolve from root using dotted segments
        return TryResolveFromRoot(data, path, out value);
    }

    private static string NormalizePath(string rawPath)
    {
        var path = rawPath.Trim();

        // If NCalc gives "[x]" as name (depending on how you extract), handle it
        if (path.StartsWith("[") && path.EndsWith("]") && path.Length >= 2)
            path = path[1..^1].Trim();

        return path;
    }

    private static bool TryResolveFromRoot(IReadOnlyDictionary<string, object?> data, string path, out object? value)
    {
        value = null;

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) return false;

        if (!TryGetDictionaryValue(data, segments[0], out var current))
            return false;

        return TryResolveFromCurrent(current, segments, startIndex: 1, out value);
    }

    private static bool TryResolveFromCurrent(object? current, IReadOnlyList<string> segments, int startIndex,
        out object? value)
    {
        value = null;

        current = JsonElementUnwrapper.Unwrap(current);

        for (var i = startIndex; i < segments.Count; i++)
        {
            if (current is null) return false;

            var seg = segments[i];

            // numeric segment => array index support
            var isIndex = int.TryParse(seg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx);

            switch (current)
            {
                case JsonElement je:
                    if (isIndex)
                    {
                        if (je.ValueKind != JsonValueKind.Array) return false;
                        if (idx < 0 || idx >= je.GetArrayLength()) return false;
                        current = je[idx];
                    }
                    else
                    {
                        if (je.ValueKind != JsonValueKind.Object) return false;

                        // Case-insensitive property lookup
                        if (!TryGetJsonPropertyIgnoreCase(je, seg, out var prop))
                            return false;

                        current = prop;
                    }

                    break;

                case IReadOnlyDictionary<string, object?> roDict:
                    if (!TryGetDictionaryValue(roDict, seg, out current))
                        return false;
                    break;

                case IDictionary<string, object?> dict:
                    if (!TryGetDictionaryValue(dict, seg, out current))
                        return false;
                    break;

                default:
                    return false;
            }

            current = JsonElementUnwrapper.Unwrap(current);
        }

        value = current;
        return true;
    }

    private static bool TryGetJsonPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        // Fast path: exact case
        if (obj.TryGetProperty(name, out value))
            return true;

        // Slow path: enumerate for case-insensitive match
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetDictionaryValue(IReadOnlyDictionary<string, object?> dict, string key, out object? value)
    {
        // exact
        if (dict.TryGetValue(key, out value))
            return true;

        // ignore-case fallback
        foreach (var kv in dict)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetDictionaryValue(IDictionary<string, object?> dict, string key, out object? value)
    {
        if (dict.TryGetValue(key, out value))
            return true;

        foreach (var kv in dict)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}