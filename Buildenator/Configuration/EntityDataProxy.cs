using Buildenator.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Buildenator.Configuration
{
    internal readonly record struct EntityDataProxy
    {
        internal readonly bool IsAbstract;
        internal readonly string Name;
        internal readonly string FullName;
        internal readonly string FullNameWithConstraints;
        internal readonly ImmutableArray<string> AdditionalNamespaces;
        internal readonly ImmutableArray<ConstructorDataProxy> Constructors;
        internal readonly ImmutableArray<ConstructorDataProxy> StaticMethods;
        internal readonly ImmutableArray<TypedSymbolDataProxy> SettableProperties;
        internal readonly ImmutableArray<TypedSymbolDataProxy> UnsettableProperties;

        internal EntityDataProxy(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.IsGenericType)
            {
                namedTypeSymbol = namedTypeSymbol.ConstructedFrom;
                AdditionalNamespaces = namedTypeSymbol.TypeParameters.Where(a => a.ConstraintTypes.Any())
                   .SelectMany(a => a.ConstraintTypes).Select(a => a.ContainingNamespace.ToDisplayString())
                   .ToImmutableArray();
            }
            IsAbstract = namedTypeSymbol.IsAbstract;
            Name = namedTypeSymbol.Name;
            FullName = namedTypeSymbol.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters, typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
            FullNameWithConstraints = namedTypeSymbol.ToDisplayString(new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance));

            var members = namedTypeSymbol.GetMembers();
            var settableProperties = new List<IPropertySymbol>();
            var unsettableProperties = new List<IPropertySymbol>();
            var constructors = new List<ConstructorDataProxy>();
            var staticMethods = new List<ConstructorDataProxy>();

            var setPropertyNames = new HashSet<string>();
            var unsetPropertyNames = new HashSet<string>();
            foreach (var member in members)
            {
                switch (member)
                {
                    case IPropertySymbol a
                    when a.GetMethod is not null && a.GetMethod.DeclaredAccessibility != Accessibility.Private && a.GetMethod.DeclaredAccessibility != Accessibility.Protected:
                        if (a.IsSettableProperty())
                        {
                            settableProperties.Add(a);
                            setPropertyNames.Add(a.Name);
                        }
                        else
                        {
                            unsettableProperties.Add(a);
                            unsetPropertyNames.Add(a.Name);
                        }
                        break;
                    case IMethodSymbol { MethodKind: MethodKind.Constructor, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } m:
                        constructors.Add(new ConstructorDataProxy(m.Name, m.Parameters.Select(p => new TypedSymbolDataProxy(p, p.Type)).ToImmutableArray()));
                        break;
                    case IMethodSymbol { IsStatic: true, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } m:
                        staticMethods.Add(new ConstructorDataProxy(m.Name, m.Parameters.Select(p => new TypedSymbolDataProxy(p, p.Type)).ToImmutableArray()));
                        break;
                }
            }
            var baseType = namedTypeSymbol.BaseType;
            while (baseType != null)
            {
                var newProperties = baseType.GetMembers().OfType<IPropertySymbol>().Split(a => a.IsSettableProperty());
                TakeNotCoveredProperties(ref settableProperties, setPropertyNames, newProperties.Left);
                TakeNotCoveredProperties(ref unsettableProperties, unsetPropertyNames, newProperties.Right);

                baseType = baseType.BaseType;
            }

            (Constructors, StaticMethods) = (constructors.ToImmutableArray(), staticMethods.ToImmutableArray());
            (SettableProperties, UnsettableProperties) =
                (
                settableProperties.Select(p => new TypedSymbolDataProxy(p, p.Type)).ToImmutableArray(),
                unsettableProperties.Select(p => new TypedSymbolDataProxy(p, p.Type)).ToImmutableArray()
                );

        }

        static void TakeNotCoveredProperties(
            ref List<IPropertySymbol> properties, ISet<string> propertyNames, IEnumerable<IPropertySymbol> newProperties)
        {
            var newSetProperties = newProperties.Where(x => !propertyNames.Contains(x.Name)).ToList();

#pragma warning disable RS1024 // Symbols should be compared for equality
            properties = properties.Union(newSetProperties).ToList();
#pragma warning restore RS1024 // Symbols should be compared for equality
            propertyNames.UnionWith(newSetProperties.Select(x => x.Name));
        }

        public bool Equals(EntityDataProxy other)
        {
            return other.FullNameWithConstraints == FullNameWithConstraints && other.IsAbstract == IsAbstract
                && other.Constructors.SequenceEqual(Constructors) && other.StaticMethods.SequenceEqual(StaticMethods)
                && other.SettableProperties.SequenceEqual(SettableProperties) && other.UnsettableProperties.SequenceEqual(UnsettableProperties)
                && other.AdditionalNamespaces.SequenceEqual(AdditionalNamespaces);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}