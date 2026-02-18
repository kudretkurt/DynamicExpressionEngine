namespace DynamicExpressionEngine.Core.Models;

public sealed class EvaluationResult
{
    public string? Result { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess { get; init; }

    public static EvaluationResult Success(string? result = null)
    {
        return new EvaluationResult { IsSuccess = true, Result = result };
    }

    public static EvaluationResult Fail(string error)
    {
        return new EvaluationResult { IsSuccess = false, Error = error };
    }
}