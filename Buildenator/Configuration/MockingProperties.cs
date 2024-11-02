using Buildenator.Abstraction;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Buildenator.Extensions;
using Buildenator.Generators;
using System.Linq;

namespace Buildenator.Configuration;

internal readonly record struct MockingProperties(
        MockingInterfacesStrategy Strategy,
        string TypeDeclarationFormat,
        string FieldDefaultValueAssignmentFormat,
        string ReturnObjectFormat,
        in ImmutableArray<string> AdditionalNamespaces) : IAdditionalNamespacesProvider
{
    public static MockingProperties? CreateOrDefault(
        in ImmutableArray<TypedConstant>? localMockingProperties)
    {
        if (localMockingProperties is not { } attributeParameters)
            return null;

        var strategy = attributeParameters.GetOrThrow<MockingInterfacesStrategy>(0, nameof(Strategy));
        var typeDeclarationFormat = attributeParameters.GetOrThrow(1, nameof(TypeDeclarationFormat));
        var defaultValueAssignmentFormat = attributeParameters.GetOrThrow(2, nameof(FieldDefaultValueAssignmentFormat));
        var returnObjectFormat = attributeParameters.GetOrThrow(3, nameof(ReturnObjectFormat));
        var additionalNamespaces = (string?)attributeParameters[4].Value;

        return new MockingProperties(
            strategy,
            typeDeclarationFormat,
            defaultValueAssignmentFormat,
            returnObjectFormat,
            (additionalNamespaces?.Split(',') ?? []).ToImmutableArray());
    }
    public static MockingProperties? CreateOrDefault(
        in MockingProperties? globalProperties,
        in MockingProperties? localMockingProperties) => 
        localMockingProperties switch
        {
            { } notNullProperties => notNullProperties,
            null => globalProperties
        };

    public bool Equals(MockingProperties other)
    {
        return other.Strategy == Strategy && other.TypeDeclarationFormat == TypeDeclarationFormat
            && other.FieldDefaultValueAssignmentFormat == FieldDefaultValueAssignmentFormat
            && other.ReturnObjectFormat == ReturnObjectFormat
            && other.AdditionalNamespaces.SequenceEqual(AdditionalNamespaces);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}