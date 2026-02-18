using DynamicExpressionEngine.Core.Helpers;
using DynamicExpressionEngine.Core.Models;

namespace DynamicExpressionEngine.Core.Abstractions;

public interface IExpressionEngine
{
    EvaluationResult Evaluate(EvaluationContext context);
}

public abstract class ExpressionEngineBase : IExpressionEngine
{
    public EvaluationResult Evaluate(EvaluationContext context)
    {
        var validation = Validate(context);
        if (!validation.IsSuccess)
            return validation;

        return EvaluateCore(context);
    }
   
    protected virtual EvaluationResult Validate(EvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Expression))
            return EvaluationResult.Fail("Invalid expression: Expression is required");

        if (context.Data is null)
            return EvaluationResult.Fail("Invalid expression: Data is required");

        // Example engine-level limits
        if (context.Expression.Length > 2000)
            return EvaluationResult.Fail("Invalid expression: Expression too long");

        var parameters = ParameterExtractor.ExtractParameters(context.Expression);
        if (parameters.Count > 50)
            return EvaluationResult.Fail("Invalid expression: Too many parameters");

        if (context.Expression.Count(c => c == '(') > 50)
            return EvaluationResult.Fail("Invalid expression: Expression too complex");

        // Optional: deep path guard
        foreach (var p in parameters)
        {
            if (p.Split('.', StringSplitOptions.RemoveEmptyEntries).Length > 20)
                return EvaluationResult.Fail($"Invalid expression: Parameter path too deep '{p}'");
        }

        return EvaluationResult.Success();
    }

    
    protected abstract EvaluationResult EvaluateCore(EvaluationContext context);
}