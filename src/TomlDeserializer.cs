using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Implements IDeserializer for TOML format using Tomlyn library.
/// </summary>
public sealed class TomlDeserializer : IDeserializer, ITypeDeserializer
{
    private readonly object _currentValue;
    private readonly Stack<object> _valueStack = new();

    private TomlDeserializer(object value)
    {
        _currentValue = value;
    }

    /// <summary>
    /// Deserialize from a TOML string.
    /// </summary>
    public static T Deserialize<T>(string tomlString, IDeserialize<T> deserialize)
    {
        var model = Tomlyn.Toml.ToModel(tomlString);
        var deserializer = new TomlDeserializer(model);
        var result = deserialize.Deserialize(deserializer);
        return result;
    }

    public static T Deserialize<T, TProvider>(string tomlString) where TProvider : IDeserializeProvider<T>
    {
        return Deserialize(tomlString, TProvider.Instance);
    }

    public static T Deserialize<T>(string tomlString) where T : IDeserializeProvider<T>
    {
        return Deserialize(tomlString, T.Instance);
    }

    public T? ReadNullableRef<T>(IDeserialize<T> deserialize) where T : class
    {
        // TOML doesn't have null values, if the value is missing return null
        if (_currentValue == null)
        {
            return null;
        }
        return deserialize.Deserialize(this);
    }

    public bool ReadBool()
    {
        return _currentValue switch
        {
            bool b => b,
            _ => throw new InvalidOperationException($"Expected bool, got {_currentValue?.GetType()}")
        };
    }

    public char ReadChar()
    {
        return _currentValue switch
        {
            string s when s.Length == 1 => s[0],
            _ => throw new InvalidOperationException($"Expected single character string, got {_currentValue?.GetType()}")
        };
    }

    public byte ReadU8()
    {
        return Convert.ToByte(ReadI64());
    }

    public ushort ReadU16()
    {
        return Convert.ToUInt16(ReadI64());
    }

    public uint ReadU32()
    {
        return Convert.ToUInt32(ReadI64());
    }

    public ulong ReadU64()
    {
        return Convert.ToUInt64(ReadI64());
    }

    public sbyte ReadI8()
    {
        return Convert.ToSByte(ReadI64());
    }

    public short ReadI16()
    {
        return Convert.ToInt16(ReadI64());
    }

    public int ReadI32()
    {
        return Convert.ToInt32(ReadI64());
    }

    public long ReadI64()
    {
        return _currentValue switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            _ => throw new InvalidOperationException($"Expected integer, got {_currentValue?.GetType()}")
        };
    }

    public float ReadF32()
    {
        return Convert.ToSingle(ReadF64());
    }

    public double ReadF64()
    {
        return _currentValue switch
        {
            double d => d,
            float f => f,
            long l => (double)l,
            int i => (double)i,
            _ => throw new InvalidOperationException($"Expected float, got {_currentValue?.GetType()}")
        };
    }

    public decimal ReadDecimal()
    {
        return Convert.ToDecimal(ReadF64());
    }

    public string ReadString()
    {
        return _currentValue switch
        {
            string s => s,
            _ => throw new InvalidOperationException($"Expected string, got {_currentValue?.GetType()}")
        };
    }

    public DateTime ReadDateTime()
    {
        return _currentValue switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeOffset dto => dto.UtcDateTime,
            string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
            _ => throw new InvalidOperationException($"Expected DateTime, got {_currentValue?.GetType()}")
        };
    }

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        var s = ReadString();
        var bytes = Convert.FromBase64String(s);
        writer.Write(bytes);
    }

    public ITypeDeserializer ReadType(ISerdeInfo typeInfo)
    {
        return typeInfo.Kind switch
        {
            InfoKind.List => new CollectionDeserializer(_currentValue, typeInfo),
            InfoKind.Dictionary => new CollectionDeserializer(_currentValue, typeInfo),
            InfoKind.CustomType => this,
            InfoKind.Nullable => this,
            InfoKind.Enum => this,
            _ => throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}")
        };
    }

    // ITypeDeserializer implementation
    int? ITypeDeserializer.SizeOpt => _currentValue is TomlTable table ? table.Count : _currentValue is TomlArray array ? array.Count : null;

    int ITypeDeserializer.TryReadIndex(ISerdeInfo info, out string? errorName)
    {
        errorName = null;
        if (_currentValue is not TomlTable table)
        {
            return ITypeDeserializer.EndOfType;
        }

        // Find the next field that exists in the table
        for (int i = 0; i < info.FieldCount; i++)
        {
            var fieldName = info.GetFieldStringName(i);
            if (table.ContainsKey(fieldName))
            {
                return i;
            }
        }

        return ITypeDeserializer.EndOfType;
    }

    T ITypeDeserializer.ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                var fieldDeserializer = new TomlDeserializer(value);
                var result = deserialize.Deserialize(fieldDeserializer);
                // Mark as read by removing from table (to avoid re-reading)
                table.Remove(fieldName);
                return result;
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    void ITypeDeserializer.SkipValue(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            table.Remove(fieldName);
        }
    }

    bool ITypeDeserializer.ReadBool(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value is bool b ? b : throw new InvalidOperationException($"Expected bool, got {value?.GetType()}");
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    char ITypeDeserializer.ReadChar(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value is string s && s.Length == 1 ? s[0] : throw new InvalidOperationException($"Expected single character string, got {value?.GetType()}");
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    byte ITypeDeserializer.ReadU8(ISerdeInfo info, int index)
    {
        return Convert.ToByte(((ITypeDeserializer)this).ReadI64(info, index));
    }

    ushort ITypeDeserializer.ReadU16(ISerdeInfo info, int index)
    {
        return Convert.ToUInt16(((ITypeDeserializer)this).ReadI64(info, index));
    }

    uint ITypeDeserializer.ReadU32(ISerdeInfo info, int index)
    {
        return Convert.ToUInt32(((ITypeDeserializer)this).ReadI64(info, index));
    }

    ulong ITypeDeserializer.ReadU64(ISerdeInfo info, int index)
    {
        return Convert.ToUInt64(((ITypeDeserializer)this).ReadI64(info, index));
    }

    sbyte ITypeDeserializer.ReadI8(ISerdeInfo info, int index)
    {
        return Convert.ToSByte(((ITypeDeserializer)this).ReadI64(info, index));
    }

    short ITypeDeserializer.ReadI16(ISerdeInfo info, int index)
    {
        return Convert.ToInt16(((ITypeDeserializer)this).ReadI64(info, index));
    }

    int ITypeDeserializer.ReadI32(ISerdeInfo info, int index)
    {
        return Convert.ToInt32(((ITypeDeserializer)this).ReadI64(info, index));
    }

    long ITypeDeserializer.ReadI64(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value switch
                {
                    long l => l,
                    int i => i,
                    short s => s,
                    byte b => b,
                    _ => throw new InvalidOperationException($"Expected integer, got {value?.GetType()}")
                };
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    float ITypeDeserializer.ReadF32(ISerdeInfo info, int index)
    {
        return Convert.ToSingle(((ITypeDeserializer)this).ReadF64(info, index));
    }

    double ITypeDeserializer.ReadF64(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value switch
                {
                    double d => d,
                    float f => f,
                    long l => (double)l,
                    int i => (double)i,
                    _ => throw new InvalidOperationException($"Expected float, got {value?.GetType()}")
                };
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    decimal ITypeDeserializer.ReadDecimal(ISerdeInfo info, int index)
    {
        return Convert.ToDecimal(((ITypeDeserializer)this).ReadF64(info, index));
    }

    string ITypeDeserializer.ReadString(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value is string s ? s : throw new InvalidOperationException($"Expected string, got {value?.GetType()}");
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    DateTime ITypeDeserializer.ReadDateTime(ISerdeInfo info, int index)
    {
        if (_currentValue is TomlTable table)
        {
            var fieldName = info.GetFieldStringName(index);
            if (table.TryGetValue(fieldName, out var value))
            {
                table.Remove(fieldName);
                return value switch
                {
                    DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                    DateTimeOffset dto => dto.UtcDateTime,
                    string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
                    _ => throw new InvalidOperationException($"Expected DateTime, got {value?.GetType()}")
                };
            }
        }
        throw new InvalidOperationException($"Field not found: {info.GetFieldStringName(index)}");
    }

    void ITypeDeserializer.ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
    {
        var s = ((ITypeDeserializer)this).ReadString(info, index);
        var bytes = Convert.FromBase64String(s);
        writer.Write(bytes);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private sealed class CollectionDeserializer : ITypeDeserializer
    {
        private readonly object _value;
        private readonly ISerdeInfo _info;
        private int _index;
        private readonly TomlArray? _array;
        private readonly TomlTable? _table;
        private readonly List<string>? _tableKeys;
        private int _tableKeyIndex;

        public int? SizeOpt => _array?.Count ?? _tableKeys?.Count;

        public CollectionDeserializer(object value, ISerdeInfo info)
        {
            _value = value;
            _info = info;
            _index = 0;
            
            if (info.Kind == InfoKind.List)
            {
                _array = value as TomlArray ?? throw new InvalidOperationException("Expected TomlArray");
            }
            else if (info.Kind == InfoKind.Dictionary)
            {
                _table = value as TomlTable ?? throw new InvalidOperationException("Expected TomlTable");
                _tableKeys = _table.Keys.ToList();
                _tableKeyIndex = 0;
            }
        }

        public int TryReadIndex(ISerdeInfo info, out string? errorName)
        {
            errorName = null;

            if (_info.Kind == InfoKind.List)
            {
                if (_array != null && _index < _array.Count)
                {
                    return _index;
                }
                return ITypeDeserializer.EndOfType;
            }
            else if (_info.Kind == InfoKind.Dictionary)
            {
                if (_tableKeys != null && _tableKeyIndex < _tableKeys.Count)
                {
                    return _index;
                }
                return ITypeDeserializer.EndOfType;
            }

            return ITypeDeserializer.EndOfType;
        }

        public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) where T : class?
        {
            object value;
            
            if (_info.Kind == InfoKind.List)
            {
                if (_array == null || _index >= _array.Count)
                {
                    throw new InvalidOperationException("Index out of range");
                }
                value = _array[_index]!;
                _index++;
            }
            else if (_info.Kind == InfoKind.Dictionary)
            {
                if (_tableKeys == null || _table == null || _tableKeyIndex >= _tableKeys.Count)
                {
                    throw new InvalidOperationException("Index out of range");
                }

                // For dictionaries, we alternate between keys and values
                if (_index % 2 == 0)
                {
                    // Reading key
                    value = _tableKeys[_tableKeyIndex];
                }
                else
                {
                    // Reading value
                    value = _table[_tableKeys[_tableKeyIndex]]!;
                    _tableKeyIndex++;
                }
                _index++;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported collection kind: {_info.Kind}");
            }

            var deserializer = new TomlDeserializer(value!);
            return deserialize.Deserialize(deserializer);
        }

        public void SkipValue(ISerdeInfo info, int index)
        {
            _index++;
            if (_info.Kind == InfoKind.Dictionary && _index % 2 == 0)
            {
                _tableKeyIndex++;
            }
        }

        public bool ReadBool(ISerdeInfo info, int index)
        {
            if (_array == null || _index >= _array.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            var value = _array[_index++]!;
            return value is bool b ? b : throw new InvalidOperationException($"Expected bool, got {value?.GetType()}");
        }

        public char ReadChar(ISerdeInfo info, int index)
        {
            if (_array == null || _index >= _array.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            var value = _array[_index++]!;
            return value is string s && s.Length == 1 ? s[0] : throw new InvalidOperationException($"Expected single character string, got {value?.GetType()}");
        }

        public byte ReadU8(ISerdeInfo info, int index)
        {
            return Convert.ToByte(ReadI64(info, index));
        }

        public ushort ReadU16(ISerdeInfo info, int index)
        {
            return Convert.ToUInt16(ReadI64(info, index));
        }

        public uint ReadU32(ISerdeInfo info, int index)
        {
            return Convert.ToUInt32(ReadI64(info, index));
        }

        public ulong ReadU64(ISerdeInfo info, int index)
        {
            return Convert.ToUInt64(ReadI64(info, index));
        }

        public sbyte ReadI8(ISerdeInfo info, int index)
        {
            return Convert.ToSByte(ReadI64(info, index));
        }

        public short ReadI16(ISerdeInfo info, int index)
        {
            return Convert.ToInt16(ReadI64(info, index));
        }

        public int ReadI32(ISerdeInfo info, int index)
        {
            return Convert.ToInt32(ReadI64(info, index));
        }

        public long ReadI64(ISerdeInfo info, int index)
        {
            if (_array == null || _index >= _array.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            var value = _array[_index++];
            return value switch
            {
                long l => l,
                int i => i,
                short s => s,
                byte b => b,
                _ => throw new InvalidOperationException($"Expected integer, got {value?.GetType()}")
            };
        }

        public float ReadF32(ISerdeInfo info, int index)
        {
            return Convert.ToSingle(ReadF64(info, index));
        }

        public double ReadF64(ISerdeInfo info, int index)
        {
            if (_array == null || _index >= _array.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            var value = _array[_index++];
            return value switch
            {
                double d => d,
                float f => f,
                long l => (double)l,
                int i => (double)i,
                _ => throw new InvalidOperationException($"Expected float, got {value?.GetType()}")
            };
        }

        public decimal ReadDecimal(ISerdeInfo info, int index)
        {
            return Convert.ToDecimal(ReadF64(info, index));
        }

        public string ReadString(ISerdeInfo info, int index)
        {
            object value;
            
            if (_info.Kind == InfoKind.List)
            {
                if (_array == null || _index >= _array.Count)
                {
                    throw new InvalidOperationException("Index out of range");
                }
                value = _array[_index++]!;
            }
            else if (_info.Kind == InfoKind.Dictionary)
            {
                if (_tableKeys == null || _table == null || _tableKeyIndex >= _tableKeys.Count)
                {
                    throw new InvalidOperationException("Index out of range");
                }

                // For dictionaries, we alternate between keys and values
                if (_index % 2 == 0)
                {
                    // Reading key
                    value = _tableKeys[_tableKeyIndex];
                }
                else
                {
                    // Reading value
                    value = _table[_tableKeys[_tableKeyIndex]]!;
                    _tableKeyIndex++;
                }
                _index++;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported collection kind: {_info.Kind}");
            }

            return value is string s ? s : throw new InvalidOperationException($"Expected string, got {value?.GetType()}");
        }

        public DateTime ReadDateTime(ISerdeInfo info, int index)
        {
            if (_array == null || _index >= _array.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            var value = _array[_index++];
            return value switch
            {
                DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                DateTimeOffset dto => dto.UtcDateTime,
                string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
                _ => throw new InvalidOperationException($"Expected DateTime, got {value?.GetType()}")
            };
        }

        public void ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
        {
            var s = ReadString(info, index);
            var bytes = Convert.FromBase64String(s);
            writer.Write(bytes);
        }
    }
}
