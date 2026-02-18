using System.Text.RegularExpressions;
using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Helpers;
using DynamicExpressionEngine.Core.Models.Requests;
using FluentValidation;

namespace DynamicExpressionEngine.Core.Validation;

public sealed class EvaluateRequestValidator : AbstractValidator<EvaluateRequest>
{
    private static readonly Regex ForbiddenChars = new(@"[{};\\]", RegexOptions.Compiled);
    private static readonly Regex FunctionCallRegex = new(@"\b(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Compiled);

    private readonly IFunctionCatalog _functionCatalog;

    // These are “HTTP-layer / cheap” limits
    private const int MaxExpressionLength = 2000;
    private const int MaxParameters = 50;
    private const int MaxFunctions = 50;
    private const int MaxDataRootKeys = 200; // only root keys (cheap); deeper checks in engine
    private const int MaxKeyLength = 120;

    public EvaluateRequestValidator(IFunctionCatalog functionCatalog)
    {
        _functionCatalog = functionCatalog;

        RuleFor(x => x)
            .NotNull()
            .WithMessage("Invalid request");

        RuleFor(x => x.Expression)
            .NotEmpty().WithMessage("Expression is required")
            .MaximumLength(MaxExpressionLength).WithMessage($"Expression is too long (max {MaxExpressionLength})")
            .Must(expr => !ForbiddenChars.IsMatch(expr)).WithMessage("Expression contains forbidden characters");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Data is required");

        // Root key count + key length (cheap)
        // IMPORTANT: Only evaluate these when Data is not null, otherwise we'd risk NullReferenceException.
        When(x => x.Data is not null, () =>
        {
            RuleFor(x => x.Data!)
                .Must(d => d.Count <= MaxDataRootKeys)
                .WithMessage($"Too many data fields (max {MaxDataRootKeys})");

            RuleForEach(x => x.Data!.Keys)
                .MaximumLength(MaxKeyLength)
                .WithMessage($"Data key is too long (max {MaxKeyLength})");
        });

        // Validate referenced params exist (may be a bit more expensive, still OK)
        RuleFor(x => x)
            .Custom((request, ctx) =>
            {
                if (request is null) return;
                if (string.IsNullOrWhiteSpace(request.Expression)) return;
                if (request.Data is null) return;

                var parameters = ParameterExtractor.ExtractParameters(request.Expression);
                if (parameters.Count > MaxParameters)
                {
                    ctx.AddFailure($"Too many parameters referenced (max {MaxParameters}).");
                    return;
                }

                foreach (var p in parameters)
                {
                    // prefer direct
                    if (request.Data.ContainsKey(p))
                        continue;

                    // nested path support
                    if (!DataPathResolver.TryResolve(request.Data, p, out _))
                    {
                        ctx.AddFailure($"Missing parameter '{p}'");
                        // stop early to avoid spamming many errors
                        return;
                    }
                }

                // Optional: referenced functions allowlist pre-check (engine also enforces)
                var functions = ExtractFunctionCalls(request.Expression);
                if (functions.Count > MaxFunctions)
                {
                    ctx.AddFailure($"Too many function calls (max {MaxFunctions}).");
                    return;
                }

                foreach (var fn in functions)
                {
                    if (!_functionCatalog.IsAllowed(fn))
                    {
                        ctx.AddFailure($"Unsupported function '{fn}'");
                        return;
                    }
                }
            });
    }

    private static IReadOnlyList<string> ExtractFunctionCalls(string expression)
    {
        // Note: This is a heuristic; engine still enforces allowlist on actual invocation.
        var matches = FunctionCallRegex.Matches(expression);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in matches)
        {
            var name = m.Groups["name"].Value;
            if (!string.IsNullOrWhiteSpace(name))
                set.Add(name);
        }

        return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}