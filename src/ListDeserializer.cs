using System;
using System.Buffers;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class ListDeserializer : ITypeDeserializer
{
    private readonly TomlArray _array;
    private int _index;

    public int? SizeOpt => _array.Count;

    public ListDeserializer(TomlArray array)
    {
        _array = array;
        _index = 0;
    }

    public int TryReadIndex(ISerdeInfo info, out string? errorName)
    {
        errorName = null;
        if (_index < _array.Count)
        {
            return _index;
        }
        return ITypeDeserializer.EndOfType;
    }

    public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) where T : class?
    {
        if (_index >= _array.Count)
        {
            throw new InvalidOperationException("Index out of range");
        }
        var value = _array[_index++]!;
        var deserializer = new TomlDeserializer(value);
        return deserialize.Deserialize(deserializer);
    }

    public void SkipValue(ISerdeInfo info, int index)
    {
        if (_index >= _array.Count)
        {
            throw new InvalidOperationException("Index out of range");
        }
        _index++;
    }

    public bool ReadBool(ISerdeInfo info, int index)
    {
        var value = GetNextValue();
        return value is bool b ? b : throw new InvalidOperationException($"Expected bool, got {value?.GetType()}");
    }

    public char ReadChar(ISerdeInfo info, int index)
    {
        var value = GetNextValue();
        return value is string s && s.Length == 1 ? s[0] : throw new InvalidOperationException($"Expected single character string, got {value?.GetType()}");
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
        var value = GetNextValue();
        return value switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            _ => throw new InvalidOperationException($"Expected integer, got {value?.GetType()}")
        };
    }

    public float ReadF32(ISerdeInfo info, int index) => Convert.ToSingle(ReadF64(info, index));

    public double ReadF64(ISerdeInfo info, int index)
    {
        var value = GetNextValue();
        return value switch
        {
            double d => d,
            float f => f,
            long l => (double)l,
            int i => (double)i,
            _ => throw new InvalidOperationException($"Expected float, got {value?.GetType()}")
        };
    }

    public decimal ReadDecimal(ISerdeInfo info, int index) => Convert.ToDecimal(ReadF64(info, index));

    public string ReadString(ISerdeInfo info, int index)
    {
        var value = GetNextValue();
        return value is string s ? s : throw new InvalidOperationException($"Expected string, got {value?.GetType()}");
    }

    public DateTime ReadDateTime(ISerdeInfo info, int index)
    {
        var value = GetNextValue();
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

    private object GetNextValue()
    {
        if (_index >= _array.Count)
        {
            throw new InvalidOperationException("Index out of range");
        }
        return _array[_index++]!;
    }
}
