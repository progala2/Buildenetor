using Buildenator.Abstraction;
using Buildenator.Configuration.Contract;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Buildenator.Diagnostics;

namespace Buildenator.Configuration;

internal readonly record struct BuilderProperties : IBuilderProperties
{
    private readonly Dictionary<string, MethodDataProxy> _buildingMethods;
    private readonly ImmutableDictionary<string, FieldDataProxy> _fields;
    private readonly List<BuildenatorDiagnostic> _diagnostics = [];

    public static BuilderProperties Create(
        in BuilderDataProxy builderSymbol,
        in MakeBuilderDataProxy builderAttribute,
        in GlobalMakeBuilderDataProxy? globalAttributes,
        bool nullableAnnotaionEnabled)
    {
        string? defaultNameWith = null;
        bool? generateDefaultBuildMethod = null;
        NullableStrategy? nullableStrategy = null;
        bool? generateMethodsForUnreachableProperties = null;
        bool? implicitCast = null;
        bool? generateStaticPropertyForBuilderCreation = null;

        if (globalAttributes.HasValue)
        {
            defaultNameWith = globalAttributes.Value.BuildingMethodsPrefix;
            generateDefaultBuildMethod = globalAttributes.Value.GenerateDefaultBuildMethod;
            nullableStrategy = globalAttributes.Value.NullableStrategy;
            generateMethodsForUnreachableProperties = globalAttributes.Value.GenerateMethodsForUnreachableProperties;
            implicitCast = globalAttributes.Value.ImplicitCast;
            generateStaticPropertyForBuilderCreation = globalAttributes.Value.GenerateStaticPropertyForBuilderCreation;
        }

        nullableStrategy = builderAttribute.NullableStrategy is null ? nullableStrategy : builderAttribute.NullableStrategy;

        if ((nullableStrategy is null || nullableStrategy == NullableStrategy.Default) && nullableAnnotaionEnabled)
        {
            nullableStrategy = NullableStrategy.Enabled;
        }


        return new BuilderProperties(builderSymbol,
            new MakeBuilderDataProxy(
                builderAttribute.TypeForBuilder,
                builderAttribute.BuildingMethodsPrefix ?? defaultNameWith,
                builderAttribute.GenerateDefaultBuildMethod ?? generateDefaultBuildMethod,
                nullableStrategy,
                builderAttribute.GenerateMethodsForUnreachableProperties ??
                generateMethodsForUnreachableProperties,
                builderAttribute.ImplicitCast ?? implicitCast,
                builderAttribute.StaticFactoryMethodName,
                builderAttribute.GenerateStaticPropertyForBuilderCreation ?? generateStaticPropertyForBuilderCreation));
    }

    private BuilderProperties(in BuilderDataProxy builderSymbol, in MakeBuilderDataProxy attributeData)
    {
        ContainingNamespace = builderSymbol.ContainingNamespace;
        Name = builderSymbol.Name;
        FullName = builderSymbol.FullName;
        BuildingMethodsPrefix = attributeData.BuildingMethodsPrefix ?? DefaultConstants.BuildingMethodsPrefix;
        NullableStrategy = attributeData.NullableStrategy ?? NullableStrategy.Default;
        GenerateDefaultBuildMethod = attributeData.GenerateDefaultBuildMethod ?? true;
        ImplicitCast = attributeData.ImplicitCast ?? false;
        ShouldGenerateMethodsForUnreachableProperties = attributeData.GenerateMethodsForUnreachableProperties ?? false;
        OriginalLocation = builderSymbol.FirstLocation;
        StaticFactoryMethodName = attributeData.StaticFactoryMethodName;
        GenerateStaticPropertyForBuilderCreation = attributeData.GenerateStaticPropertyForBuilderCreation ?? false;

        if (string.IsNullOrWhiteSpace(BuildingMethodsPrefix))
            throw new ArgumentNullException(nameof(attributeData), "Prefix name shouldn't be empty!");

        _buildingMethods = [];
        foreach (var method in builderSymbol.Methods)
        {
            switch (method)
            {
                case { MethodKind: MethodKind.Ordinary }
                when method.Name.StartsWith(BuildingMethodsPrefix)
                && method.Name != DefaultConstants.BuildMethodName:
                    _buildingMethods.Add(method.Name, method);
                    break;
                case { MethodKind: MethodKind.Ordinary, Name: DefaultConstants.PostBuildMethodName }:
                    IsPostBuildMethodOverriden = true;
                    break;
                case { MethodKind: MethodKind.Ordinary, Name: DefaultConstants.BuildMethodName, ParametersLength: 0 }:
                    IsBuildMethodOverriden = true;
                    _diagnostics.Add(new BuildenatorDiagnostic(
                        BuildenatorDiagnosticDescriptors.BuildMethodOverridenDiagnostic,
                        OriginalLocation));
                    break;
                case { MethodKind: MethodKind.Constructor, ParametersLength: 0, IsImplicitlyDeclared: false }:
                    IsDefaultConstructorOverriden = true;
                    _diagnostics.Add(new BuildenatorDiagnostic(
                        BuildenatorDiagnosticDescriptors.DefaultConstructorOverridenDiagnostic,
                        OriginalLocation));
                    break;
            }
        }
        _fields = builderSymbol.Fields.ToImmutableDictionary(f => f.Name);
    }

    public string ContainingNamespace { get; }
    public string Name { get; }
    public string FullName { get; }
    public string BuildingMethodsPrefix { get; }
    public NullableStrategy NullableStrategy { get; }
    public bool GenerateDefaultBuildMethod { get; }
    public bool ImplicitCast { get; }
    public bool IsPostBuildMethodOverriden { get; }
    public bool IsDefaultConstructorOverriden { get; }
    public bool ShouldGenerateMethodsForUnreachableProperties { get; }
    public bool IsBuildMethodOverriden { get; }
    public Location OriginalLocation { get; }
    public string? StaticFactoryMethodName { get; }
    public bool GenerateStaticPropertyForBuilderCreation { get; }

    public IReadOnlyDictionary<string, MethodDataProxy> BuildingMethods => _buildingMethods;
    public IImmutableDictionary<string, FieldDataProxy> Fields => _fields;

    public IEnumerable<BuildenatorDiagnostic> Diagnostics => _diagnostics;
}
