using System.Collections;

namespace Serde.Toml;

internal abstract record TomlValue
{
    private TomlValue() { }

    public sealed record Boolean(bool Value) : TomlValue;
    public sealed record Integer(long Value) : TomlValue;
    public sealed record Float(double Value) : TomlValue;
    public sealed record String(string Value) : TomlValue;
    public sealed record DateTime(global::System.DateTime Value) : TomlValue;
    public sealed record DateTimeOffset(global::System.DateTimeOffset Value) : TomlValue;
    public sealed record Date(DateOnly Value) : TomlValue;
    public sealed record Time(TimeOnly Value) : TomlValue;

    public sealed record Array : TomlValue, IEnumerable<TomlValue>
    {
        private readonly List<TomlValue> _values = [];

        public int Count => _values.Count;
        public TomlValue this[int index] => _values[index];
        public void Add(TomlValue value) => _values.Add(value);
        public IEnumerator<TomlValue> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed record Table : TomlValue, IEnumerable<KeyValuePair<string, TomlValue>>
    {
        private readonly Dictionary<string, TomlValue> _values = [];

        public TomlValue this[string key]
        {
            get => _values[key];
            set => _values[key] = value;
        }

        public bool Remove(string key) => _values.Remove(key);
        public IEnumerator<KeyValuePair<string, TomlValue>> GetEnumerator() =>
            _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static implicit operator TomlValue(bool value) => new Boolean(value);
    public static implicit operator TomlValue(long value) => new Integer(value);
    public static implicit operator TomlValue(double value) => new Float(value);
    public static implicit operator TomlValue(string value) => new String(value);
}
