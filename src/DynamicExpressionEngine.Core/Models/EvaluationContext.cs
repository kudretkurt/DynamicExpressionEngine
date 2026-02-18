namespace DynamicExpressionEngine.Core.Models;

public sealed class EvaluationContext
{
    public required string Expression { get; init; }
    public required Dictionary<string, object?> Data { get; init; }
}