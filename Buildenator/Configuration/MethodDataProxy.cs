using Microsoft.CodeAnalysis;

namespace Buildenator.Configuration;

internal readonly record struct MethodDataProxy
{
    internal readonly MethodKind MethodKind;
    internal readonly string Name;
    internal readonly int ParametersLength;
    internal readonly bool IsImplicitlyDeclared;
    internal readonly string FirstParameterTypeName;

    internal MethodDataProxy(IMethodSymbol methodSymbol)
    {
        MethodKind = methodSymbol.MethodKind;
        Name = methodSymbol.Name;
        ParametersLength = methodSymbol.Parameters.Length;
        IsImplicitlyDeclared = methodSymbol.IsImplicitlyDeclared;
        FirstParameterTypeName = methodSymbol.Parameters.Length == 1 ? methodSymbol.Parameters[0].Type.Name : string.Empty;
    }
}