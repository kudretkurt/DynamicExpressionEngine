using System.Globalization;
using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Helpers;

namespace DynamicExpressionEngine.Core.Registry;

public sealed class FunctionRegistry : IFunctionRegistry
{
    public bool TryInvoke(string name, object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        name ??= string.Empty;

        try
        {
            switch (name)
            {
                case "DateParseExact":
                    return TryDateParseExact(args, out result, out error);

                case "FormatDate":
                    return TryFormatDate(args, out result, out error);

                case "ToString":
                    return TryToString(args, out result, out error);

                case "Add":
                    return TryAdd(args, out result, out error);

                case "Subtract":
                    return TrySubtract(args, out result, out error);

                default:
                    error = $"Invalid expression: Unsupported function {name}";
                    return false;
            }
        }
        catch (Exception ex)
        {
            error = $"Invalid expression: {ex.Message}";
            return false;
        }
    }

    private static bool TryDateParseExact(object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (args.Length != 2)
        {
            error = "Invalid expression: DateParseExact expects (string value, string format)";
            return false;
        }

        var valueObj = JsonElementUnwrapper.Unwrap(args[0]);
        var formatObj = JsonElementUnwrapper.Unwrap(args[1]);

        if (valueObj is not string value || formatObj is not string format)
        {
            error = "Invalid expression: DateParseExact expects (string value, string format)";
            return false;
        }

        if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            error = $"Invalid expression: DateParseExact could not parse '{value}' with format '{format}'";
            return false;
        }

        result = dt;
        return true;
    }

    private static bool TryFormatDate(object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (args.Length != 2)
        {
            error = "Invalid expression: FormatDate expects (DateTime date, string format)";
            return false;
        }

        var dateObj = JsonElementUnwrapper.Unwrap(args[0]);
        var formatObj = JsonElementUnwrapper.Unwrap(args[1]);

        if (dateObj is not DateTime date)
        {
            error = "Invalid expression: FormatDate expects DateTime input";
            return false;
        }

        if (formatObj is not string format)
        {
            error = "Invalid expression: FormatDate expects (DateTime date, string format)";
            return false;
        }

        result = date.ToString(format, CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryToString(object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (args.Length != 1)
        {
            error = "Invalid expression: ToString expects (value)";
            return false;
        }

        var valueObj = JsonElementUnwrapper.Unwrap(args[0]);
        result = valueObj?.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryAdd(object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (args.Length != 2)
        {
            error = "Invalid expression: Add expects (a, b)";
            return false;
        }

        if (!TryGetDecimal(args[0], out var a) || !TryGetDecimal(args[1], out var b))
        {
            error = "Invalid expression: Add expects numeric inputs";
            return false;
        }

        result = a + b;
        return true;
    }

    private static bool TrySubtract(object?[] args, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (args.Length != 2)
        {
            error = "Invalid expression: Subtract expects (a, b)";
            return false;
        }

        if (!TryGetDecimal(args[0], out var a) || !TryGetDecimal(args[1], out var b))
        {
            error = "Invalid expression: Subtract expects numeric inputs";
            return false;
        }

        result = a - b;
        return true;
    }

    private static bool TryGetDecimal(object? value, out decimal dec)
    {
        value = JsonElementUnwrapper.Unwrap(value);

        switch (value)
        {
            case decimal d:
                dec = d;
                return true;
            case double dbl:
                dec = (decimal)dbl;
                return true;
            case float f:
                dec = (decimal)f;
                return true;
            case int i:
                dec = i;
                return true;
            case long l:
                dec = l;
                return true;
            case string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                dec = parsed;
                return true;
            default:
                dec = default;
                return false;
        }
    }
}