using Buildenator.CodeAnalysis;
using Buildenator.Diagnostics;
using Buildenator.Generators;
using System.Collections.Generic;

namespace Buildenator.Configuration.Contract;

internal interface IEntityToBuild : IAdditionalNamespacesProvider
{
    string FullName { get; }
    string FullNameWithConstraints { get; }
    string Name { get; }
    EntityToBuild.Constructor? ConstructorToBuild { get; }
    IEnumerable<BuildenatorDiagnostic> Diagnostics { get; }
    IReadOnlyList<TypedSymbol> AllUniqueSettablePropertiesAndParameters { get; }

    string GenerateBuildsCode(bool shouldGenerateMethodsForUnreachableProperties);
    string GenerateDefaultBuildsCode();
    IReadOnlyList<TypedSymbol> AllUniqueReadOnlyPropertiesWithoutConstructorsParametersMatch { get; }
}