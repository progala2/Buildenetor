using System.Linq;
using System.Text;
using Buildenator.CodeAnalysis;
using Buildenator.Configuration;
using Buildenator.Configuration.Contract;

namespace Buildenator.Generators;

internal sealed class PropertiesStringGenerator(IBuilderProperties builder, IEntityToBuild entity)
{
	private readonly IBuilderProperties _builder = builder;
	private readonly IEntityToBuild _entity = entity;

    public string GeneratePropertiesCode()
	{
		var properties = _entity.AllUniqueSettablePropertiesAndParameters;

		if (_builder.ShouldGenerateMethodsForUnreachableProperties || _entity.ConstructorToBuild is null)
		{
			properties = [.. properties, .. _entity.AllUniqueReadOnlyPropertiesWithoutConstructorsParametersMatch];
		}

		var output = new StringBuilder();

		foreach (var typedSymbol in properties.Where(p => IsNotYetDeclaredField(p)))
		{
            output = output.AppendLine($@"        private {typedSymbol.GenerateLazyFieldType()} {typedSymbol.UnderScoreName};");
		}

		foreach (var typedSymbol in properties.Where(p => IsNotYetDeclaredMethod(p)))
		{
            output = output.AppendLine($@"

        {GenerateMethodDefinition(typedSymbol)}");

		}

		return output.ToString();

		bool IsNotYetDeclaredField(in TypedSymbol x) => !_builder.Fields.TryGetValue(x.UnderScoreName, out _);

		bool IsNotYetDeclaredMethod(in TypedSymbol x) => !_builder.BuildingMethods.TryGetValue(CreateMethodName(x), out var method)
		                                               || !(method.ParametersLength == 1 && method.FirstParameterTypeName == x.TypeName);
	}

	private string GenerateMethodDefinition(in TypedSymbol typedSymbol)
		=> $@"{GenerateMethodDefinitionHeader(typedSymbol)}
        {{
            {GenerateValueAssignment(typedSymbol)};
            return this;
        }}";

	private string GenerateMethodDefinitionHeader(in TypedSymbol typedSymbol)
		=> $"public {_builder.FullName} {CreateMethodName(typedSymbol)}({typedSymbol.GenerateMethodParameterDefinition()})";

	private static string GenerateValueAssignment(in TypedSymbol typedSymbol)
		=> typedSymbol.IsMockable
			? $"{DefaultConstants.SetupActionLiteral}({typedSymbol.UnderScoreName})"
			: $"{typedSymbol.UnderScoreName} = new {DefaultConstants.NullBox}<{typedSymbol.TypeFullName}>({DefaultConstants.ValueLiteral})";

	private string CreateMethodName(in TypedSymbol property) => $"{_builder.BuildingMethodsPrefix}{property.SymbolPascalName}";
}