using Buildenator.Extensions;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Buildenator.Configuration
{
    internal readonly record struct TypedSymbolDataProxy
    {
        internal readonly string PascalCaseName;
        internal readonly string UnderScoreName;
        internal readonly string TypeFullName;
        internal readonly string TypeName;
        internal readonly string Name;
        internal readonly TypeKind TypeKind;
        internal readonly bool AllInterfacesAreNotEnumerable;

        public TypedSymbolDataProxy(ISymbol symbol, ITypeSymbol type)
        {
            Name = symbol.Name;
            PascalCaseName = symbol.PascalCaseName();
            UnderScoreName = symbol.UnderScoreName();
            TypeFullName = type.ToDisplayString();
            TypeName = type.Name;
            TypeKind = type.TypeKind;
            AllInterfacesAreNotEnumerable = type.AllInterfaces.All(x => x.SpecialType != SpecialType.System_Collections_IEnumerable);
        }
    }
}