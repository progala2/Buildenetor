using Buildenator.Abstraction;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Buildenator.Extensions;
using Buildenator.Generators;
using System.Linq;

namespace Buildenator.Configuration;

public readonly record struct FixtureProperties(
    string Name,
    string CreateSingleFormat,
    string? ConstructorParameters,
    string? AdditionalConfiguration,
    FixtureInterfacesStrategy Strategy,
    in ImmutableArray<string> AdditionalNamespaces): IAdditionalNamespacesProvider
{
    private const string FixtureLiteral = "_fixture";
        
    public static FixtureProperties? CreateOrDefault(
        in ImmutableArray<TypedConstant>? localFixtureProperties)
    {
        return localFixtureProperties is { } notNullProperties
            ? new FixtureProperties(notNullProperties)
            : null;
    }
    public static FixtureProperties? CreateOrDefault(
        in FixtureProperties? globalFixtureProperties,
        in FixtureProperties? localFixtureProperties) =>
        localFixtureProperties is { } notNullProperties
            ? notNullProperties
            : globalFixtureProperties;

    private FixtureProperties(in ImmutableArray<TypedConstant> attributeParameters)
        :this(
             attributeParameters.GetOrThrow(0, nameof(Name)),
             attributeParameters.GetOrThrow(1, nameof(CreateSingleFormat)),
             (string?)attributeParameters[2].Value,
             (string?)attributeParameters[3].Value,
             attributeParameters.GetOrThrow<FixtureInterfacesStrategy>(4, nameof(Strategy)),
             (((string?)attributeParameters[5].Value)?.Split(',') ?? []).ToImmutableArray())
    {
    }

    public string GenerateAdditionalConfiguration()
        => AdditionalConfiguration is null ? string.Empty : string.Format(AdditionalConfiguration, FixtureLiteral, Name);

    public bool NeedsAdditionalConfiguration() => AdditionalConfiguration is not null;

    public bool Equals(FixtureProperties other)
    {
        return other.Name == Name && other.CreateSingleFormat == CreateSingleFormat && other.ConstructorParameters == ConstructorParameters
            && other.Strategy == Strategy && other.AdditionalConfiguration == AdditionalConfiguration
            && other.AdditionalNamespaces.SequenceEqual(AdditionalNamespaces);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}