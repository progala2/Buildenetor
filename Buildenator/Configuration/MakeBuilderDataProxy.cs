using Buildenator.Abstraction;
using Microsoft.CodeAnalysis;

namespace Buildenator.Configuration;

internal readonly record struct MakeBuilderDataProxy(
    in EntityDataProxy TypeForBuilder,
    string? BuildingMethodsPrefix,
    bool? GenerateDefaultBuildMethod,
    NullableStrategy? NullableStrategy,
    bool? GenerateMethodsForUnreachableProperties,
    bool? ImplicitCast,
    string? StaticFactoryMethodName,
    bool? GenerateStaticPropertyForBuilderCreation)
{

    internal MakeBuilderDataProxy(AttributeData attribute)
        : this(
            new EntityDataProxy((INamedTypeSymbol)attribute.ConstructorArguments[0].Value!),
            (string?)attribute.ConstructorArguments[1].Value,
            (bool?)attribute.ConstructorArguments[2].Value,
            attribute.ConstructorArguments[3].Value is null
                ? null
                : (NullableStrategy)attribute.ConstructorArguments[3].Value!,
            (bool?)attribute.ConstructorArguments[4].Value,
            (bool?)attribute.ConstructorArguments[5].Value,
            (string?)attribute.ConstructorArguments[6].Value,
            (bool?)attribute.ConstructorArguments[7].Value)
    {

    }

    internal static MakeBuilderDataProxy? CreateOrDefault(AttributeData? attributeData) =>
        attributeData is { } notNullAttribute
            ? new MakeBuilderDataProxy(notNullAttribute)
            : null;
}