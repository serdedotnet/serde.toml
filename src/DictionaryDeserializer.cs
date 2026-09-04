using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class DictionaryDeserializer : TomlTypeDeserializer
{
    private readonly IReadOnlyList<KeyValuePair<string, object>> _entries;
    private int _index;

    public DictionaryDeserializer(TomlTable table)
    {
        _entries = table.Select(entry => KeyValuePair.Create(entry.Key, entry.Value!)).ToArray();
    }

    public override int? SizeOpt => _entries.Count;

    public override int TryReadIndex(ISerdeInfo info) =>
        _index < _entries.Count * 2 ? _index : ITypeDeserializer.EndOfType;

    protected override object TakeValue()
    {
        if (_index >= _entries.Count * 2)
        {
            throw new DeserializeException("Attempted to read past the end of a TOML table.");
        }

        var entry = _entries[_index / 2];
        return _index++ % 2 == 0 ? entry.Key : entry.Value;
    }
}
