namespace DynamicExpressionEngine.Core.Abstractions;

public interface IFunctionRegistry
{
    bool TryInvoke(string name, object?[] args, out object? result, out string? error);
}