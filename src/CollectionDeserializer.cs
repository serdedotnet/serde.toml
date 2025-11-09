using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class CollectionDeserializer : ITypeDeserializer
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
            _array = (TomlArray)value;
        }
        else if (info.Kind == InfoKind.Dictionary)
        {
            _table = (TomlTable)value;
            _tableKeys = _table.Keys.ToList();
            _tableKeyIndex = 0;
        }
    }

    /// <summary>
    /// Helper method to get the next value from the collection (List or Dictionary).
    /// For dictionaries, this handles alternating between keys and values.
    /// </summary>
    private object GetNextCollectionValue()
    {
        if (_info.Kind == InfoKind.List)
        {
            if (_index >= _array!.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }
            return _array[_index++]!;
        }
        else if (_info.Kind == InfoKind.Dictionary)
        {
            if (_tableKeyIndex >= _tableKeys!.Count)
            {
                throw new InvalidOperationException("Index out of range");
            }

            // For dictionaries, we alternate between keys and values
            if (_index % 2 == 0)
            {
                // Reading key
                var key = _tableKeys[_tableKeyIndex];
                _index++;
                return key;
            }
            else
            {
                // Reading value
                var value = _table![_tableKeys[_tableKeyIndex]]!;
                _tableKeyIndex++;
                _index++;
                return value;
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported collection kind: {_info.Kind}");
        }
    }

    /// <summary>
    /// For dictionary deserialization, ensures we're reading a value (not a key) when expecting non-string types.
    /// </summary>
    private void ValidateNotDictionaryKey()
    {
        if (_info.Kind == InfoKind.Dictionary && _index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
    }

    public int TryReadIndex(ISerdeInfo info, out string? errorName)
    {
        errorName = null;

        if (_info.Kind == InfoKind.List)
        {
            if (_index < _array!.Count)
            {
                return _index;
            }
            return ITypeDeserializer.EndOfType;
        }
        else if (_info.Kind == InfoKind.Dictionary)
        {
            if (_tableKeyIndex < _tableKeys!.Count)
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
        ValidateNotDictionaryKey();
        var value = GetNextCollectionValue();
        return value is bool b ? b : throw new InvalidOperationException($"Expected bool, got {value?.GetType()}");
    }

    public char ReadChar(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextCollectionValue();
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
        ValidateNotDictionaryKey();
        var value = GetNextCollectionValue();
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
        ValidateNotDictionaryKey();
        var value = GetNextCollectionValue();
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
        // Strings can be both keys and values in dictionaries, so no validation needed
        var value = GetNextCollectionValue();
        return value is string s ? s : throw new InvalidOperationException($"Expected string, got {value?.GetType()}");
    }

    public DateTime ReadDateTime(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextCollectionValue();
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
