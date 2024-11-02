namespace Buildenator.Configuration;

internal readonly record struct FieldDataProxy
{
    internal readonly string Name;

    public FieldDataProxy(string name)
    {
        Name = name;
    }
}
