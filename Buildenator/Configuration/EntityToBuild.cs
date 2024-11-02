using Buildenator.CodeAnalysis;
using Buildenator.Configuration.Contract;
using Buildenator.Extensions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Buildenator.Abstraction;
using Buildenator.Diagnostics;
using System.Text;
using System;
using System.Collections.Immutable;

namespace Buildenator.Configuration;

internal sealed class EntityToBuild : IEntityToBuild, IEquatable<EntityToBuild>
{
    public string Name { get; }
    public string FullName { get; }
    public string FullNameWithConstraints { get; }
    public Constructor? ConstructorToBuild { get; }
    public IReadOnlyList<TypedSymbol> AllUniqueSettablePropertiesAndParameters => _uniqueTypedSymbols;
    public IReadOnlyList<TypedSymbol> AllUniqueReadOnlyPropertiesWithoutConstructorsParametersMatch => _uniqueReadOnlyTypedSymbols;
    public ImmutableArray<string> AdditionalNamespaces { get; }
    public IEnumerable<BuildenatorDiagnostic> Diagnostics => _diagnostics;
    public NullableStrategy NullableStrategy { get; }

    public EntityToBuild(
        in EntityDataProxy entityToBuildSymbol,
        in MockingProperties? mockingConfiguration,
        in FixtureProperties? fixtureConfiguration,
        NullableStrategy nullableStrategy,
        string? staticFactoryMethodName)
    {
        AdditionalNamespaces = entityToBuildSymbol.AdditionalNamespaces;

        Name = entityToBuildSymbol.Name;
        FullName = entityToBuildSymbol.FullName;
        FullNameWithConstraints = entityToBuildSymbol.FullNameWithConstraints;

        ConstructorToBuild = Constructor.CreateConstructorOrDefault(entityToBuildSymbol, mockingConfiguration, fixtureConfiguration, nullableStrategy, staticFactoryMethodName);
        (_properties, _uniqueReadOnlyTypedSymbols) = DividePropertiesBySetability(entityToBuildSymbol, mockingConfiguration, fixtureConfiguration, nullableStrategy);
        _uniqueTypedSymbols = _properties;
        if (ConstructorToBuild is not null)
        {
            _properties = _properties.Where(x => !ConstructorToBuild.ContainsParameter(x.SymbolName)).ToImmutableArray();
            _uniqueTypedSymbols = _properties.Concat(ConstructorToBuild.Parameters).ToImmutableArray();
            _uniqueReadOnlyTypedSymbols = _uniqueReadOnlyTypedSymbols.Where(x => !ConstructorToBuild.ContainsParameter(x.SymbolName)).ToImmutableArray();
        }

        NullableStrategy = nullableStrategy;
    }

    public string GenerateBuildsCode(bool shouldGenerateMethodsForUnreachableProperties)
    {
        if (ConstructorToBuild is null)
            return "";

        var disableWarning = NullableStrategy == NullableStrategy.Enabled
            ? "#pragma warning disable CS8604\n"
            : string.Empty;
        var restoreWarning = NullableStrategy == NullableStrategy.Enabled
            ? "#pragma warning restore CS8604\n"
            : string.Empty;

        return $@"{disableWarning}        public {FullName} {DefaultConstants.BuildMethodName}()
        {{
            {GenerateLazyBuildEntityString(shouldGenerateMethodsForUnreachableProperties, ConstructorToBuild.Parameters)}
        }}
{restoreWarning}
";
    }

    private string GenerateLazyBuildEntityString(bool shouldGenerateMethodsForUnreachableProperties, IEnumerable<TypedSymbol> parameters)
    {
        var propertiesAssignment = _properties.Select(property => $"{property.SymbolName} = {property.GenerateLazyFieldValueReturn()}").ComaJoin();
        var onlyConstructorString = string.Empty;
        if (ConstructorToBuild is StaticConstructor staticConstructor)
        {
            onlyConstructorString = @$"var result = {FullName}.{staticConstructor.Name}({parameters.Select(symbol => symbol.GenerateLazyFieldValueReturn()).ComaJoin()});
";
        }
        else
        {
            onlyConstructorString = @$"var result = new {FullName}({parameters.Select(symbol => symbol.GenerateLazyFieldValueReturn()).ComaJoin()})
            {{
{(string.IsNullOrEmpty(propertiesAssignment) ? string.Empty : $"                {propertiesAssignment}")}
            }};
";
        }

        return onlyConstructorString
            + $@"{(shouldGenerateMethodsForUnreachableProperties ? GenerateUnreachableProperties() : "")}
            {DefaultConstants.PostBuildMethodName}(result);
            return result;";

        string GenerateUnreachableProperties()
        {
            var output = new StringBuilder();
            output.AppendLine($"var t = typeof({FullName});");
            foreach (var a in AllUniqueReadOnlyPropertiesWithoutConstructorsParametersMatch)
            {
                output.Append($"            t.GetProperty(\"{a.SymbolName}\")")
                    .Append(NullableStrategy == NullableStrategy.Enabled ? "!" : "")
                    .AppendLine($".SetValue(result, {a.GenerateLazyFieldValueReturn()}, System.Reflection.BindingFlags.NonPublic, null, null, null);");
            }
            return output.ToString();
        }
    }

    private static (ImmutableArray<TypedSymbol> Settable, ImmutableArray<TypedSymbol> ReadOnly) DividePropertiesBySetability(
        in EntityDataProxy entityToBuildSymbol, in MockingProperties? mockingConfiguration,
        in FixtureProperties? fixtureConfiguration, NullableStrategy nullableStrategy)
    {
        var settableList = new List<TypedSymbol>();
        foreach(var settableProperty in entityToBuildSymbol.SettableProperties)
        {
            settableList.Add(new TypedSymbol(settableProperty, mockingConfiguration, fixtureConfiguration, nullableStrategy));
        }
        var unsettableList = new List<TypedSymbol>();
        foreach (var unsettableProperty in entityToBuildSymbol.UnsettableProperties)
        {
            unsettableList.Add(new TypedSymbol(unsettableProperty, mockingConfiguration, fixtureConfiguration, nullableStrategy));
        }

        return (settableList.ToImmutableArray(), unsettableList.ToImmutableArray());
    }

    public string GenerateDefaultBuildsCode()
    {
        if (ConstructorToBuild is null)
            return "";

        var moqInit = ConstructorToBuild.Parameters
            .Concat(_properties)
            .Where(symbol => symbol.IsMockable)
            .Select(s => $@"            {s.GenerateFieldInitialization()}")
            .Aggregate(new StringBuilder(), (builder, s) => builder.AppendLine(s))
            .ToString();

        var methodParameters = ConstructorToBuild.Parameters
            .Concat(_properties)
            .Select(s =>
            {
                var fieldType = s.GenerateFieldType();
                return $"{fieldType} {s.UnderScoreName} = default({fieldType})";
            }).ComaJoin();
        var disableWarning = NullableStrategy == NullableStrategy.Enabled
            ? "#pragma warning disable CS8625\n"
            : string.Empty;
        var restoreWarning = NullableStrategy == NullableStrategy.Enabled
            ? "#pragma warning restore CS8625\n"
            : string.Empty;

        return $@"{disableWarning}        public static {FullName} BuildDefault({methodParameters})
        {{
            {moqInit}
            {GenerateDefaultBuildEntityString(ConstructorToBuild.Parameters)}
        }}
{restoreWarning}";
    }

    private string GenerateDefaultBuildEntityString(IEnumerable<TypedSymbol> parameters)
    {
        if (ConstructorToBuild is StaticConstructor staticConstructor)
        {
            return @$"return {FullName}.{staticConstructor.Name}({parameters.Select(a => a.GenerateFieldValueReturn()).ComaJoin()});";
        }
        else
        {
            var propertiesAssignment = _properties.Select(property => $"{property.SymbolName} = {property.GenerateFieldValueReturn()}").ComaJoin();
            return @$"return new {FullName}({parameters.Select(a => a.GenerateFieldValueReturn()).ComaJoin()})
            {{
{(string.IsNullOrEmpty(propertiesAssignment) ? string.Empty : $"                {propertiesAssignment}")}
            }};";
        }
    }

    public bool Equals(EntityToBuild? other)
    {
        if (other is null) return false;
        return other.FullNameWithConstraints == FullNameWithConstraints
            && (other.ConstructorToBuild is null && ConstructorToBuild is null || other.ConstructorToBuild is not null && other.ConstructorToBuild.Equals(ConstructorToBuild))
            && other.NullableStrategy == NullableStrategy
            && other._properties.SequenceEqual(_properties)
            && other.AdditionalNamespaces.SequenceEqual(AdditionalNamespaces);
    }

    private readonly ImmutableArray<TypedSymbol> _uniqueReadOnlyTypedSymbols;
    private readonly ImmutableArray<TypedSymbol> _uniqueTypedSymbols;
    private readonly ImmutableArray<TypedSymbol> _properties;
    private readonly ImmutableArray<BuildenatorDiagnostic> _diagnostics = ImmutableArray.Create<BuildenatorDiagnostic>();

    internal abstract class Constructor(IReadOnlyDictionary<string, TypedSymbol> constructorParameters) : IEquatable<Constructor>
    {
        public static Constructor? CreateConstructorOrDefault(
            EntityDataProxy entityToBuildSymbol,
            MockingProperties? mockingConfiguration,
            FixtureProperties? fixtureConfiguration,
            NullableStrategy nullableStrategy,
            string? staticFactoryMethodName)
        {
            ImmutableArray<ConstructorDataProxy> constructors;
            if (staticFactoryMethodName is null)
            {
                constructors = entityToBuildSymbol.Constructors;
            }
            else
            {
                constructors = entityToBuildSymbol.StaticMethods;
            }

            if (constructors.Length == 0)
                return default;

            var selectedConstructor = constructors
                            .OrderByDescending(x => x.Parameters.Length)
                            .First();
            var parameters = selectedConstructor
                .Parameters
                .ToDictionary(x => x.PascalCaseName, s => new TypedSymbol(s, mockingConfiguration, fixtureConfiguration, nullableStrategy));

            return staticFactoryMethodName is null
                ? new ObjectConstructor(parameters)
                : new StaticConstructor(parameters, selectedConstructor.Name);
        }

        public IReadOnlyDictionary<string, TypedSymbol> ConstructorParameters { get; } = constructorParameters;

        public bool ContainsParameter(string parameterName) => ConstructorParameters.ContainsKey(parameterName);

        public bool Equals(Constructor? other)
        {
            if (other is null) return false;
            if (other.ConstructorParameters.Count != ConstructorParameters.Count) return false;
            return other.ConstructorParameters.All(p => ConstructorParameters.TryGetValue(p.Key, out var value) && value == p.Value);
        }

        public IEnumerable<TypedSymbol> Parameters => ConstructorParameters.Values;
    }

    internal sealed class ObjectConstructor(IReadOnlyDictionary<string, TypedSymbol> constructorParameters)
        : Constructor(constructorParameters), IEquatable<ObjectConstructor>
    {
        public bool Equals(ObjectConstructor? other) => base.Equals(other);
    }

    internal sealed class StaticConstructor(IReadOnlyDictionary<string, TypedSymbol> constructorParameters, string name)
        : Constructor(constructorParameters), IEquatable<StaticConstructor>
    {
        public string Name { get; } = name;

        public bool Equals(StaticConstructor? other) => other?.Name == Name && base.Equals(other);
    }
}