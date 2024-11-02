using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Buildenator.Configuration;

internal readonly record struct BuilderDataProxy : IEquatable<BuilderDataProxy>
{
    internal readonly string ContainingNamespace;
    internal readonly Location FirstLocation;
    internal readonly string Name;
    internal readonly string FullName;
    internal readonly ImmutableArray<MethodDataProxy> Methods;
    internal readonly ImmutableArray<FieldDataProxy> Fields;

    internal BuilderDataProxy(INamedTypeSymbol type)
    {
        ContainingNamespace = type.ContainingNamespace.ToDisplayString();
        FirstLocation = type.Locations.First();
        Name = type.Name;
        FullName = type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        var methods = new List<MethodDataProxy>();
        var fields = new List<FieldDataProxy>();
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                methods.Add(new MethodDataProxy(method));
            }
            else if (member is IFieldSymbol field)
            {
                fields.Add(new FieldDataProxy(field.Name));
            }
        }
        Methods = methods.ToImmutableArray();
        Fields = fields.ToImmutableArray();
    }


    public bool Equals(BuilderDataProxy other)
    {
        return ContainingNamespace == other.ContainingNamespace &&
               FirstLocation.Equals(other.FirstLocation) &&
               FullName == other.FullName &&
               Methods.SequenceEqual(other.Methods) &&
               Fields.SequenceEqual(other.Fields);
    }

    public override int GetHashCode()
    {
        int hash = 17;

        hash = hash * 23 + ContainingNamespace.GetHashCode();
        hash = hash * 23 + FirstLocation.GetHashCode();
        hash = hash * 23 + FullName.GetHashCode();
        hash = hash * 23 + Methods.Aggregate(hash, (h, m) => h * 23 + m.GetHashCode());
        hash = hash * 23 + Fields.Aggregate(hash, (h, f) => h * 23 + f.GetHashCode());

        return hash;
    }
}
