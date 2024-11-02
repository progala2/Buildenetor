using Buildenator.Abstraction;
using Buildenator.Extensions;
using Microsoft.CodeAnalysis;
using System.Linq;
using Buildenator.Configuration;

namespace Buildenator.CodeAnalysis;

internal readonly record struct TypedSymbol
{
    public TypedSymbol(
        in TypedSymbolDataProxy symbol,
        in MockingProperties? mockingInterfaceStrategy,
        in FixtureProperties? fixtureConfiguration,
        NullableStrategy nullableStrategy)
    {
        UnderScoreName = symbol.UnderScoreName;
        TypeFullName = symbol.TypeFullName;
        TypeName = symbol.TypeName;
        SymbolPascalName = symbol.PascalCaseName;
        SymbolName = symbol.Name;
        _mockingProperties = mockingInterfaceStrategy;
        _fixtureProperties = fixtureConfiguration;
        _nullableStrategy = nullableStrategy;
        IsMockable = _mockingProperties?.Strategy switch
        {
            MockingInterfacesStrategy.All
                when symbol.TypeKind == TypeKind.Interface => true,
            MockingInterfacesStrategy.WithoutGenericCollection
                when symbol.TypeKind == TypeKind.Interface && symbol.AllInterfacesAreNotEnumerable => true,
            _ => false
        };
        IsFakeable = _fixtureProperties?.Strategy switch
        {
            null => false,
            FixtureInterfacesStrategy.None
                when symbol.TypeKind == TypeKind.Interface => false,
            FixtureInterfacesStrategy.OnlyGenericCollections
                when symbol.TypeKind == TypeKind.Interface && symbol.AllInterfacesAreNotEnumerable => false,
            _ => true
        };
    }

    public TypedSymbol(
        IParameterSymbol symbol,
        in MockingProperties? mockingInterfaceStrategy,
        in FixtureProperties? fixtureConfiguration,
        NullableStrategy nullableStrategy)
        : this(symbol, symbol.Type, mockingInterfaceStrategy, fixtureConfiguration, nullableStrategy)
    {
    }

    private TypedSymbol(
        ISymbol symbol,
        ITypeSymbol typeSymbol,
        in MockingProperties? mockingInterfaceStrategy,
        in FixtureProperties? fixtureConfiguration,
        NullableStrategy nullableStrategy)
    {
        UnderScoreName = symbol.UnderScoreName();
        TypeFullName = typeSymbol.ToDisplayString();
        TypeName = typeSymbol.Name;
        SymbolPascalName = symbol.PascalCaseName();
        SymbolName = symbol.Name;
        _mockingProperties = mockingInterfaceStrategy;
        _fixtureProperties = fixtureConfiguration;
        _nullableStrategy = nullableStrategy;
        IsMockable = _mockingProperties?.Strategy switch
        {
            MockingInterfacesStrategy.All
                when typeSymbol.TypeKind == TypeKind.Interface => true,
            MockingInterfacesStrategy.WithoutGenericCollection
                when typeSymbol.TypeKind == TypeKind.Interface && typeSymbol.AllInterfaces.All(x => x.SpecialType != SpecialType.System_Collections_IEnumerable) => true,
            _ => false
        };
        IsFakeable = _fixtureProperties?.Strategy switch
        {
            null => false,
            FixtureInterfacesStrategy.None
                when typeSymbol.TypeKind == TypeKind.Interface => false,
            FixtureInterfacesStrategy.OnlyGenericCollections
                when typeSymbol.TypeKind == TypeKind.Interface && typeSymbol.AllInterfaces.All(x => x.SpecialType != SpecialType.System_Collections_IEnumerable) => false,
            _ => true
        };
    }

    public bool NeedsFieldInit() => IsMockable;

    public string UnderScoreName { get; }
    public string TypeFullName { get; }
    public string TypeName { get; }
    public string SymbolPascalName { get; }
    public string SymbolName {  get; }
    public bool IsFakeable { get; }
    public bool IsMockable { get; }


    private readonly MockingProperties? _mockingProperties;
    private readonly FixtureProperties? _fixtureProperties;
    private readonly NullableStrategy? _nullableStrategy;

    public string GenerateFieldInitialization()
        => _mockingProperties is MockingProperties mockingProperties ? $"{UnderScoreName} = {string.Format(mockingProperties.FieldDefaultValueAssignmentFormat, TypeFullName)};" : string.Empty;

    public string GenerateFieldType()
        => IsMockable ? GenerateMockableFieldType() : TypeFullName;

    public string GenerateLazyFieldType()
        => IsMockable ? GenerateMockableFieldType() : $"{DefaultConstants.NullBox}<{TypeFullName}>?";

    public string GenerateLazyFieldValueReturn()
        => IsMockable
            ? string.Format(_mockingProperties!.Value.ReturnObjectFormat, UnderScoreName)
            : @$"({UnderScoreName}.HasValue ? {UnderScoreName}.Value : new {DefaultConstants.NullBox}<{TypeFullName}>({(IsFakeable
                ? $"{string.Format(_fixtureProperties!.Value.CreateSingleFormat, TypeFullName, SymbolName, DefaultConstants.FixtureLiteral)}"
                  + (_nullableStrategy == NullableStrategy.Enabled ? "!" : "")
                : $"default({TypeFullName})")})).Object";

    public string GenerateFieldValueReturn()
        => IsMockable
            ? string.Format(_mockingProperties!.Value.ReturnObjectFormat, UnderScoreName)
            : UnderScoreName;

    public string GenerateMethodParameterDefinition()
        => IsMockable ? $"Action<{GenerateMockableFieldType()}> {DefaultConstants.SetupActionLiteral}" : $"{TypeFullName} {DefaultConstants.ValueLiteral}";

    private string GenerateMockableFieldType() => string.Format(_mockingProperties!.Value.TypeDeclarationFormat, TypeFullName);

}