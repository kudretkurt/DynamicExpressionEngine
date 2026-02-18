using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Helpers;
using DynamicExpressionEngine.Core.Models;
using NCalc;

namespace DynamicExpressionEngine.NCalc;

public sealed class NCalcExpressionEngine(IFunctionCatalog functionCatalog, IFunctionRegistry functionRegistry)
    : ExpressionEngineBase
{
    protected override EvaluationResult EvaluateCore(EvaluationContext context)
    {
        try
        {
            var referencedParams = ParameterExtractor.ExtractParameters(context.Expression);
            var expression = new Expression(context.Expression);
            // Bind only referenced parameters; supports nested paths like [person.name].
            foreach (var p in referencedParams)
            {
                if (p.Split('.', StringSplitOptions.RemoveEmptyEntries).Length > 20)
                    return EvaluationResult.Fail($"Invalid expression: Parameter path too deep '{p}'");

                if (!DataPathResolver.TryResolve(context.Data, p, out var v))
                    return EvaluationResult.Fail($"Invalid expression: Missing parameter '{p}'");

                expression.Parameters[p] = v;
            }

            expression.EvaluateFunction += (nameObj, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameObj))
                    throw new InvalidOperationException("Invalid expression: Missing function name");

                if (!functionCatalog.IsAllowed(nameObj))
                    throw new InvalidOperationException($"Invalid expression: Unsupported function {nameObj}");

                var values = new object?[args.Parameters.Length];
                for (var i = 0; i < args.Parameters.Length; i++)
                {
                    var evaluated = args.Parameters[i]?.Evaluate();
                    values[i] = JsonElementUnwrapper.Unwrap(evaluated);
                }

                if (!functionRegistry.TryInvoke(nameObj, values, out var result, out var error))
                    throw new InvalidOperationException(error ?? $"Invalid expression: Unsupported function {nameObj}");

                args.Result = result;
            };

            var eval = expression.Evaluate();
            var unwrapped = JsonElementUnwrapper.Unwrap(eval);

            return EvaluationResult.Success(unwrapped?.ToString());
        }
        catch (InvalidOperationException ex)
        {
            return EvaluationResult.Fail(NormalizeError(ex.Message));
        }
        catch (Exception ex)
        {
            return EvaluationResult.Fail(NormalizeError(ex.Message));
        }
    }

    private static string NormalizeError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Invalid expression";

        return message.StartsWith("Invalid expression:", StringComparison.OrdinalIgnoreCase)
            ? message
            : $"Invalid expression: {message}";
    }
}