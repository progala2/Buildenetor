using System.Linq;
using System.Text;

namespace Buildenator.Generators;

internal static class NamespacesGenerator
{
    internal static string GenerateNamespaces(params IAdditionalNamespacesProvider?[] additionalNamespacesProviders)
    {
        var enumerable = Enumerable.Empty<string>();
        foreach (var additionalNamespacesProvider in additionalNamespacesProviders)
        {
            if (additionalNamespacesProvider != null && !additionalNamespacesProvider.AdditionalNamespaces.IsDefault)
                enumerable = enumerable.Concat(additionalNamespacesProvider.AdditionalNamespaces);
        }
        enumerable = enumerable.Concat(
        [
            "System",
            "System.Linq",
            "Buildenator.Abstraction.Helpers"
        ]);

        enumerable = enumerable.Distinct();

        var output = new StringBuilder();
        foreach (var @namespace in enumerable)
        {
            output = output.Append("using ").Append(@namespace).AppendLine(";");
        }
        return output.ToString();
    }
}