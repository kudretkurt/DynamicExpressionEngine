using DynamicExpressionEngine.Core.Models;

namespace DynamicExpressionEngine.Core.Abstractions;

public interface IFunctionCatalog
{
    IReadOnlyList<FunctionInfo> GetAll();
    bool IsAllowed(string name);
    FunctionInfo? Get(string name);
}