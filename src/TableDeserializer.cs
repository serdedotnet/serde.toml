using System;
using System.Buffers;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class TableDeserializer : ITypeDeserializer
{
    private readonly TomlTable _table;

    public int? SizeOpt => _table.Count;

    public TableDeserializer(TomlTable table)
    {
        _table = table;
    }

    public int TryReadIndex(ISerdeInfo info, out string? errorName)
    {
        errorName = null;

        // Find the next field that exists in the table
        for (int i = 0; i < info.FieldCount; i++)
        {
            var fieldName = info.GetFieldStringName(i);
            if (_table.ContainsKey(fieldName))
            {
                return i;
            }
        }

        return ITypeDeserializer.EndOfType;
    }

    public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) where T : class?
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            var fieldDeserializer = new TomlDeserializer((TomlTable)value);
            var result = deserialize.Deserialize(fieldDeserializer);
            // Mark as read by removing from table (to avoid re-reading)
            _table.Remove(fieldName);
            return result;
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public void SkipValue(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        _table.Remove(fieldName);
    }

    public bool ReadBool(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value is bool b ? b : throw DeserializerHelpers.TypeMismatchException("bool", value);
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public char ReadChar(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value is string s && s.Length == 1 ? s[0] : throw DeserializerHelpers.TypeMismatchException("single character string", value);
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
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
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value switch
            {
                long l => l,
                int i => i,
                short s => s,
                byte b => b,
                _ => throw DeserializerHelpers.TypeMismatchException("integer", value)
            };
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public float ReadF32(ISerdeInfo info, int index) => Convert.ToSingle(ReadF64(info, index));

    public double ReadF64(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value switch
            {
                double d => d,
                float f => f,
                long l => (double)l,
                int i => (double)i,
                _ => throw DeserializerHelpers.TypeMismatchException("float", value)
            };
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public decimal ReadDecimal(ISerdeInfo info, int index) => Convert.ToDecimal(ReadF64(info, index));

    public string ReadString(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value is string s ? s : throw DeserializerHelpers.TypeMismatchException("string", value);
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public DateTime ReadDateTime(ISerdeInfo info, int index)
    {
        var fieldName = info.GetFieldStringName(index);
        if (_table.TryGetValue(fieldName, out var value))
        {
            _table.Remove(fieldName);
            return value switch
            {
                DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                DateTimeOffset dto => dto.UtcDateTime,
                string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
                _ => throw DeserializerHelpers.TypeMismatchException("DateTime", value)
            };
        }
        throw new InvalidOperationException($"Field not found: {fieldName}");
    }

    public void ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
    {
        var s = ReadString(info, index);
        var bytes = Convert.FromBase64String(s);
        writer.Write(bytes);
    }
}
