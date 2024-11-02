using Buildenator.Abstraction;
using Buildenator.Configuration;
using Buildenator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
#if DEBUG
using System.Diagnostics;
#endif
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Buildenator.Extensions;
using System.Collections.Immutable;
using System.Threading;
using Buildenator.Diagnostics;

[assembly: InternalsVisibleTo("Buildenator.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Buildenator;
/// <inheritdoc />
[Generator]
public class BuildersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // Debugger.Launch();
#endif
        var symbolsAndAttributes = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(MakeBuilderAttribute).FullName,
            predicate: static (sx, _) => sx is ClassDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                var attributes = ctx.TargetSymbol.GetAttributes();
                return (
                BuilderSymbol: new BuilderDataProxy((INamedTypeSymbol)ctx.TargetSymbol),
                BuilderAttribute: new MakeBuilderDataProxy(
                    attributes.Single(
                        attributeData => attributeData.AttributeClass?.Name == nameof(MakeBuilderAttribute))),
                MockingAttribute: MockingProperties.CreateOrDefault(GetMockingConfigurationOrDefault(attributes)),
                FixtureAttribute: FixtureProperties.CreateOrDefault(GetLocalFixturePropertiesOrDefault(attributes))
                        );
            }
        );

        var nullableOptions = context.CompilationProvider
            .Select(static (compilation, _) => compilation.Options.NullableContextOptions);

        var classSymbols = symbolsAndAttributes
            .Where(tuple => !tuple.BuilderAttribute.TypeForBuilder.IsAbstract);

        var configurationBuilders = context.CompilationProvider
            .Select(static (c, _) => c.Assembly)
            .Select(static (assembly, _) => assembly.GetAttributes())
            .Select(static (attributes, _) =>
            (
                Mocking: attributes.Where(x =>
                    x.AttributeClass.HasNameOrBaseClassHas(nameof(MockingConfigurationAttribute)))
                .FirstOrDefault(),
                Builder: attributes.Where(x =>
                    x.AttributeClass.HasNameOrBaseClassHas(nameof(BuildenatorConfigurationAttribute)))
                .FirstOrDefault(),
                Fixture: attributes.Where(x =>
                    x.AttributeClass.HasNameOrBaseClassHas(nameof(FixtureConfigurationAttribute)))
                .FirstOrDefault()
            ))
            .Select(static (assembly, _) =>
            {
                var globalFixtureProperties = assembly.Fixture?.ConstructorArguments;
                var mockingConfigurationBuilder = assembly.Mocking?.ConstructorArguments;
                var globalBuilderProperties = assembly.Builder;

                return (
                FixtureProperties.CreateOrDefault(globalFixtureProperties),
                MockingProperties.CreateOrDefault(mockingConfigurationBuilder),
                GlobalMakeBuilderDataProxy.CreateOrDefault(globalBuilderProperties)
                );
            });

        var generators = classSymbols
            .Combine(configurationBuilders)
            .Combine(nullableOptions)
            .Select(static (leftRightAndNullable, _) =>
            {
                var (builderNamedTypeSymbol, builderAttribute, mockingAttribute, fixtureAttribute) = leftRightAndNullable.Left.Left;
                var (globalFixtureProperties,
                    globalMockingConfiguration,
                    globalBuilderProperties) = leftRightAndNullable.Left.Right;
                var nullableOptions = leftRightAndNullable.Right;

                var mockingProperties = MockingProperties.CreateOrDefault(globalMockingConfiguration, mockingAttribute);

                var fixtureProperties =
                    FixtureProperties.CreateOrDefault(globalFixtureProperties, fixtureAttribute);

                var builderProperties =
                    BuilderProperties.Create(builderNamedTypeSymbol, builderAttribute, globalBuilderProperties, nullableOptions.AnnotationsEnabled());

                return (fixtureProperties, mockingConfiguration: mockingProperties, builderProperties,
                    builderAttribute.TypeForBuilder);
            })
            .Select(static (properties, _) =>
            {
                var (fixtureProperties, mockingProperties, builderProperties, typeForBuilder) = properties;
                return new BuilderSourceStringGenerator(builderProperties,
                    new EntityToBuild(
                        typeForBuilder,
                        mockingProperties,
                        fixtureProperties,
                        builderProperties.NullableStrategy,
                        builderProperties.StaticFactoryMethodName),
                    fixtureProperties,
                    mockingProperties);
            });

        context.RegisterSourceOutput(generators, static (productionContext, generator) =>
        {
            productionContext.AddCsSourceFile(generator.FileName,
                SourceText.From(generator.CreateBuilderCode(), Encoding.UTF8));
            foreach (var diagnostic in generator.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }
        });


        var abstractClassSymbols = symbolsAndAttributes
            .Where(tuple => tuple.BuilderAttribute.TypeForBuilder.IsAbstract)
            .Select((tuple, _) => (tuple.BuilderSymbol, tuple.BuilderAttribute.TypeForBuilder));

        context.RegisterSourceOutput(abstractClassSymbols, (productionContext, tuple)
            =>
        {
            productionContext.ReportDiagnostic(
                new BuildenatorDiagnostic(BuildenatorDiagnosticDescriptors.AbstractDiagnostic,
                    tuple.BuilderSymbol.FirstLocation,
                    tuple.TypeForBuilder.Name)
                );
        });
    }

    private static (SyntaxTree SyntaxTree, SemanticModel SemanticModel) SelectSyntaxTreeAndSemanticModel((SyntaxTree SyntaxTree, Compilation Compilation) tuple, CancellationToken _)
            => (tuple.SyntaxTree, SemanticModel: tuple.Compilation.GetSemanticModel(tuple.SyntaxTree));

    private static ImmutableArray<TypedConstant>? GetLocalFixturePropertiesOrDefault(ImmutableArray<AttributeData> attributeData)
    {
        var attribute = attributeData.SingleOrDefault(x => x.AttributeClass.HasNameOrBaseClassHas(nameof(FixtureConfigurationAttribute)));
        return attribute?.ConstructorArguments;
    }

    private static ImmutableArray<TypedConstant>? GetMockingConfigurationOrDefault(ImmutableArray<AttributeData> attributeData) =>
        attributeData
            .SingleOrDefault(x => x.AttributeClass.HasNameOrBaseClassHas(nameof(MockingConfigurationAttribute)))
            ?.ConstructorArguments;
}