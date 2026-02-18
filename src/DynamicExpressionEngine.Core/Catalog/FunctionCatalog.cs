using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Models;

namespace DynamicExpressionEngine.Core.Catalog;

public sealed class FunctionCatalog : IFunctionCatalog
{
    private static readonly IReadOnlyList<FunctionInfo> Functions =
    [
        new()
        {
            Name = "DateParseExact",
            Description = "Parses a string to DateTime using exact format (InvariantCulture).",
            Parameters = ["value", "format"],
            Example = "DateParseExact('22-02-2026','dd-MM-yyyy')"
        },
        new()
        {
            Name = "FormatDate",
            Description = "Formats a DateTime using InvariantCulture.",
            Parameters = ["date", "format"],
            Example = "FormatDate(DateParseExact([date], 'dd-MM-yyyy'),'yyyy-MM-dd')"
        },
        new()
        {
            Name = "ToString",
            Description = "Converts a value to string (null => empty string).",
            Parameters = ["value"],
            Example = "ToString([value])"
        },
        new()
        {
            Name = "Add",
            Description = "Adds two numbers.",
            Parameters = ["a", "b"],
            Example = "Add(1, 2)"
        },
        new()
        {
            Name = "Subtract",
            Description = "Subtracts two numbers.",
            Parameters = ["a", "b"],
            Example = "Subtract(10, 3)"
        }
    ];

    private static readonly Dictionary<string, FunctionInfo> Index = Functions
        .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FunctionInfo> GetAll()
    {
        return Functions;
    }

    public bool IsAllowed(string name)
    {
        return Index.ContainsKey(name);
    }

    public FunctionInfo? Get(string name)
    {
        return Index.TryGetValue(name, out var info) ? info : null;
    }
}