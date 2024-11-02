using Buildenator.Abstraction;
using Microsoft.CodeAnalysis;

namespace Buildenator.Configuration;

internal readonly record struct GlobalMakeBuilderDataProxy(
    string? BuildingMethodsPrefix,
    bool? GenerateDefaultBuildMethod,
    NullableStrategy? NullableStrategy,
    bool? GenerateMethodsForUnreachableProperties,
    bool? ImplicitCast,
    bool? GenerateStaticPropertyForBuilderCreation)
{

    internal GlobalMakeBuilderDataProxy(AttributeData attribute)
        : this(
            (string?)attribute.ConstructorArguments[0].Value,
            (bool?)attribute.ConstructorArguments[1].Value,
            attribute.ConstructorArguments[2].Value is null
                ? null
                : (NullableStrategy)attribute.ConstructorArguments[2].Value!,
            (bool?)attribute.ConstructorArguments[3].Value,
            (bool?)attribute.ConstructorArguments[4].Value,
            (bool?)attribute.ConstructorArguments[5].Value)
    {

    }

    internal static GlobalMakeBuilderDataProxy? CreateOrDefault(AttributeData? attributeData) =>
        attributeData is { } notNullAttribute
            ? new GlobalMakeBuilderDataProxy(notNullAttribute)
            : null;
}
