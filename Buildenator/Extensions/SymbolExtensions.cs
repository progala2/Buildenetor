using Microsoft.CodeAnalysis;

namespace Buildenator.Extensions;

internal static class SymbolExtensions
{
    public static string PascalCaseName(this ISymbol symbol)
        => $"{symbol.Name.Substring(0, 1).ToUpperInvariant()}{symbol.Name.Substring(1)}";
    public static string CamelCaseName(this ISymbol symbol)
        => $"{symbol.Name.Substring(0, 1).ToLowerInvariant()}{symbol.Name.Substring(1)}";
    public static string UnderScoreName(this ISymbol symbol)
        => $"_{symbol.CamelCaseName()}";
}