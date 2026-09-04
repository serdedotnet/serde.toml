using Serde;

namespace Serde.Toml;

internal sealed class ListDeserializer(TomlArrayValues array) : TomlTypeDeserializer
{
    private int _index;

    public override int? SizeOpt => array.Count;

    public override int TryReadIndex(ISerdeInfo info) =>
        _index < array.Count ? _index : ITypeDeserializer.EndOfType;

    protected override object TakeValue()
    {
        if (_index >= array.Count)
        {
            throw new DeserializeException("Attempted to read past the end of a TOML array.");
        }

        return array[_index++]!;
    }
}
