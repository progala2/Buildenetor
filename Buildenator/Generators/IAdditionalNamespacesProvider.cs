using System.Collections.Immutable;

namespace Buildenator.Generators;

internal interface IAdditionalNamespacesProvider
{
    ImmutableArray<string> AdditionalNamespaces { get; }
}