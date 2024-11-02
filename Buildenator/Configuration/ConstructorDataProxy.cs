using System.Collections.Immutable;
using System.Linq;

namespace Buildenator.Configuration
{
    internal readonly record struct ConstructorDataProxy
    {
        public readonly string Name;
        public readonly ImmutableArray<TypedSymbolDataProxy> Parameters;

        public ConstructorDataProxy(string name, in ImmutableArray<TypedSymbolDataProxy> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public bool Equals(ConstructorDataProxy other)
        {
            return other.Name == Name && other.Parameters.SequenceEqual(Parameters);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}