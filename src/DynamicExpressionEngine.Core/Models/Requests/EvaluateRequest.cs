namespace DynamicExpressionEngine.Core.Models.Requests;

public sealed class EvaluateRequest
{
    public Dictionary<string, object?> Data { get; set; } = new();
    public string Expression { get; set; } = string.Empty;
}