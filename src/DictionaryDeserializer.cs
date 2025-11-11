using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class DictionaryDeserializer : ITypeDeserializer
{
    private readonly TomlTable _table;
    private readonly List<string> _tableKeys;
    private int _index;
    private int _tableKeyIndex;

    public int? SizeOpt => _tableKeys.Count;

    public DictionaryDeserializer(TomlTable table)
    {
        _table = table;
        _tableKeys = _table.Keys.ToList();
        _index = 0;
        _tableKeyIndex = 0;
    }

    public int TryReadIndex(ISerdeInfo info, out string? errorName)
    {
        errorName = null;
        if (_tableKeyIndex < _tableKeys.Count)
        {
            return _index;
        }
        return ITypeDeserializer.EndOfType;
    }

    public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) where T : class?
    {
        object value;
        
        // For dictionaries, we alternate between keys and values
        if (_index % 2 == 0)
        {
            // Reading key
            value = _tableKeys[_tableKeyIndex];
            _index++;
        }
        else
        {
            // Reading value
            value = _table[_tableKeys[_tableKeyIndex]]!;
            _tableKeyIndex++;
            _index++;
        }

        var deserializer = new TomlDeserializer(value);
        return deserialize.Deserialize(deserializer);
    }

    public void SkipValue(ISerdeInfo info, int index)
    {
        _index++;
        if (_index % 2 == 0)
        {
            _tableKeyIndex++;
        }
    }

    public bool ReadBool(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextValue();
        return value is bool b ? b : throw DeserializerHelpers.TypeMismatchException("bool", value);
    }

    public char ReadChar(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextValue();
        return value is string s && s.Length == 1 ? s[0] : throw DeserializerHelpers.TypeMismatchException("single character string", value);
    }

    public byte ReadU8(ISerdeInfo info, int index) => Convert.ToByte(ReadI64(info, index));
    public ushort ReadU16(ISerdeInfo info, int index) => Convert.ToUInt16(ReadI64(info, index));
    public uint ReadU32(ISerdeInfo info, int index) => Convert.ToUInt32(ReadI64(info, index));
    public ulong ReadU64(ISerdeInfo info, int index) => Convert.ToUInt64(ReadI64(info, index));
    public sbyte ReadI8(ISerdeInfo info, int index) => Convert.ToSByte(ReadI64(info, index));
    public short ReadI16(ISerdeInfo info, int index) => Convert.ToInt16(ReadI64(info, index));
    public int ReadI32(ISerdeInfo info, int index) => Convert.ToInt32(ReadI64(info, index));

    public long ReadI64(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextValue();
        return value switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            _ => throw DeserializerHelpers.TypeMismatchException("integer", value)
        };
    }

    public float ReadF32(ISerdeInfo info, int index) => Convert.ToSingle(ReadF64(info, index));

    public double ReadF64(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextValue();
        return value switch
        {
            double d => d,
            float f => f,
            long l => (double)l,
            int i => (double)i,
            _ => throw DeserializerHelpers.TypeMismatchException("float", value)
        };
    }

    public decimal ReadDecimal(ISerdeInfo info, int index) => Convert.ToDecimal(ReadF64(info, index));

    public string ReadString(ISerdeInfo info, int index)
    {
        // Strings can be both keys and values in dictionaries
        var value = GetNextValue();
        return value is string s ? s : throw DeserializerHelpers.TypeMismatchException("string", value);
    }

    public DateTime ReadDateTime(ISerdeInfo info, int index)
    {
        ValidateNotDictionaryKey();
        var value = GetNextValue();
        return value switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeOffset dto => dto.UtcDateTime,
            string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
            _ => throw DeserializerHelpers.TypeMismatchException("DateTime", value)
        };
    }

    public void ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
    {
        var s = ReadString(info, index);
        var bytes = Convert.FromBase64String(s);
        writer.Write(bytes);
    }

    private object GetNextValue()
    {
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
            var value = _table[_tableKeys[_tableKeyIndex]]!;
            _tableKeyIndex++;
            _index++;
            return value;
        }
    }

    private void ValidateNotDictionaryKey()
    {
        if (_index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
    }
}
