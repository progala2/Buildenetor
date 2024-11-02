using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Buildenator.Abstraction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Buildenator.Benchmarks;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class GenerationTests
{
    // Create the driver that will control the generation, passing in our generator
    private static readonly GeneratorDriver Driver = CSharpGeneratorDriver.Create(new BuildersGenerator());
    private static readonly GeneratorDriver EmptyDriver = CSharpGeneratorDriver.Create(new EmptySourceGenerator());

    [Benchmark]
    public object DriverAndCompilationOverheadForSimpleCaseTest()
    {
        return EmptyDriver.RunGenerators(CreateCompilation(SimpleSource));
    }

    [Benchmark]
    public object SimpleGenerationTest()
    {
        return Driver.RunGenerators(CreateCompilation(SimpleSource));
    }

    [Benchmark]
    public object ComplicatedGenerationTest()
    {
        return Driver.RunGenerators(CreateCompilation(ComplicatedSource, ComplicatedSource2, ComplicatedSource3));
    }

    private static Compilation CreateCompilation(params SyntaxTree[] syntaxTrees)
    {
        return CSharpCompilation.Create("c" + Guid.NewGuid().ToString("N"),
            syntaxTrees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static readonly SyntaxTree SimpleSource = CSharpSyntaxTree.ParseText(GenerateEntityAndBuilder(2, 5));
    private static readonly SyntaxTree ComplicatedSource = CSharpSyntaxTree.ParseText(GenerateEntityAndBuilder(30));
    private static readonly SyntaxTree ComplicatedSource2 = CSharpSyntaxTree.ParseText(GenerateEntityAndBuilder(30));
    private static readonly SyntaxTree ComplicatedSource3 = CSharpSyntaxTree.ParseText(GenerateEntityAndBuilder(30));

    private static readonly PortableExecutableReference[] References = [
        MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(MakeBuilderAttribute).GetTypeInfo().Assembly.Location)
    ];

    private static string GenerateEntityAndBuilder(int entityCount = 3, int propertiesCount = 10)
    {
        return @"using Buildenator.Abstraction;

namespace Buildenator.IntegrationTests.Source.Builders
{
" + string.Concat(GenerateNameList(entityCount)
            .Select(x => $@"
    [MakeBuilder(typeof({x}))]
    public partial class {x}Builder
    {{
    }}

    public class {x}
    {{
        " + string.Concat(GenerateNameList(propertiesCount).Select(s => $@"
        public string {s} {{ get; set; }}
")) + @"
    }}
}}"));
    }

    private static IEnumerable<string> GenerateNameList(int entityCount)
    {
        return Enumerable.Range(0, entityCount).Select(_ => "A" + Guid.NewGuid().ToString("N"));
    }
}