using Buildenator.Abstraction;
using Buildenator.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Buildenator.Configuration.Contract;

internal interface IBuilderProperties
{
    IReadOnlyDictionary<string, MethodDataProxy> BuildingMethods { get; }
    string BuildingMethodsPrefix { get; }
    string ContainingNamespace { get; }
    IImmutableDictionary<string, FieldDataProxy> Fields { get; }
    string FullName { get; }
    string Name { get; }
    NullableStrategy NullableStrategy { get; }
    bool GenerateDefaultBuildMethod { get; }
    bool ImplicitCast { get; }
    bool IsPostBuildMethodOverriden { get; }
    bool IsDefaultConstructorOverriden { get; }
    bool ShouldGenerateMethodsForUnreachableProperties { get; }
    Location OriginalLocation { get; }
    bool IsBuildMethodOverriden { get; }
    IEnumerable<BuildenatorDiagnostic> Diagnostics { get; }
    bool GenerateStaticPropertyForBuilderCreation { get; }
}