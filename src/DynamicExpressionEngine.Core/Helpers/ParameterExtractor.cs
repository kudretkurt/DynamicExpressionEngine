using System.Text.RegularExpressions;

namespace DynamicExpressionEngine.Core.Helpers;

public static partial class ParameterExtractor
{
    private static readonly Regex TokenRegex = ParamTokenRegex();

    public static IReadOnlyList<string> ExtractParameters(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Array.Empty<string>();

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TokenRegex.Matches(expression))
        {
            var name = match.Groups["name"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                set.Add(name);
        }

        return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [GeneratedRegex("\\[(?<name>[^\\]]+)\\]", RegexOptions.Compiled)]
    private static partial Regex ParamTokenRegex();
}