using Buildenator.Configuration;
using Buildenator.Configuration.Contract;
using System.Linq;
using System.Text;

namespace Buildenator.Generators;

internal static class ConstructorsGenerator
{
    internal static string GenerateConstructor(
        string builderName,
        IEntityToBuild entity,
        in FixtureProperties? fixtureConfiguration)
    {
            var hasAnyBody = false;
            var parameters = entity.AllUniqueSettablePropertiesAndParameters;

            var output = new StringBuilder();
        output = output.AppendLine($@"{CommentsGenerator.GenerateSummaryOverrideComment()}
        public {builderName}()
        {{");
            foreach (var typedSymbol in parameters.Where(a => a.NeedsFieldInit()))
            {
                output = output.AppendLine($@"            {typedSymbol.GenerateFieldInitialization()}");
                hasAnyBody = true;
            }

            if (fixtureConfiguration is FixtureProperties fixtureProperties && fixtureProperties.NeedsAdditionalConfiguration())
            {
                output = output.AppendLine($@"            {fixtureProperties.GenerateAdditionalConfiguration()};");
                hasAnyBody = true;
            }

            output = output.AppendLine($@"
        }}");

            return hasAnyBody ? output.ToString() : string.Empty;
        }
}