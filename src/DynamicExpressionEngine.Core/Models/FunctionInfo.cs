namespace DynamicExpressionEngine.Core.Models;

public sealed class FunctionInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string[] Parameters { get; init; }
    public required string Example { get; init; }
}