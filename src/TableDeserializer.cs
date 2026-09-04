using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class TableDeserializer : TomlTypeDeserializer
{
    private readonly IReadOnlyList<KeyValuePair<string, object>> _entries;
    private int _nextEntry;
    private object? _currentValue;

    public TableDeserializer(TomlTable table)
    {
        _entries = table.Select(entry => KeyValuePair.Create(entry.Key, entry.Value!)).ToArray();
    }

    public override int? SizeOpt => _entries.Count;

    public override int TryReadIndex(ISerdeInfo info) => TryReadIndexWithName(info).Item1;

    public override (int, string?) TryReadIndexWithName(ISerdeInfo info)
    {
        if (_currentValue is not null)
        {
            throw new InvalidOperationException("The previous TOML field was not consumed.");
        }

        if (_nextEntry >= _entries.Count)
        {
            return (ITypeDeserializer.EndOfType, null);
        }

        var entry = _entries[_nextEntry++];
        _currentValue = entry.Value;
        var index = DeserializerHelpers.FindField(info, entry.Key);
        return (
            index,
            index == ITypeDeserializer.IndexNotFound ? entry.Key : null
        );
    }

    protected override object TakeValue()
    {
        var value = _currentValue
            ?? throw new InvalidOperationException("No TOML field is ready to be read.");
        _currentValue = null;
        return value;
    }
}
