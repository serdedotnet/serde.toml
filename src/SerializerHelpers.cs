using System;
using System.Buffers;
using System.Collections.Generic;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Helper serializer for writing to TOML arrays.
/// </summary>
internal sealed class ArraySerializer : ITypeSerializer
{
    private readonly TomlArray _array;
    private readonly Action? _onEnd;

    internal ArraySerializer(TomlArray array, Action? onEnd = null)
    {
        _array = array;
        _onEnd = onEnd;
    }

    public void End(ISerdeInfo info)
    {
        _onEnd?.Invoke();
    }

    public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        _array.Add(b);
    }

    public void WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        _array.Add(c.ToString());
    }

    public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        _array.Add((long)b);
    }

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        _array.Add((long)u16);
    }

    public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        _array.Add((long)u32);
    }

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        _array.Add((long)u64);
    }

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        _array.Add((long)b);
    }

    public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        _array.Add((long)i16);
    }

    public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        _array.Add((long)i32);
    }

    public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        _array.Add(i64);
    }

    public void WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        _array.Add((double)f);
    }

    public void WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        _array.Add(d);
    }

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        _array.Add((double)d);
    }

    public void WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        _array.Add(s);
    }

    public void WriteNull(ISerdeInfo typeInfo, int index)
    {
        throw new NotSupportedException("TOML does not support null values");
    }

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        _array.Add(dt);
    }

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        _array.Add(dt);
    }

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        _array.Add(Convert.ToBase64String(bytes.Span));
    }

    public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
    {
        var nestedTable = new TomlTable();
        _array.Add(nestedTable);
        var tableSerializer = new TableSerializer(nestedTable);
        serialize.Serialize(value, tableSerializer);
    }
}

/// <summary>
/// Helper serializer for writing to TOML dictionaries.
/// </summary>
internal sealed class DictionarySerializer : ITypeSerializer
{
    private readonly TomlTable _table;
    private readonly Action? _onEnd;
    private string? _currentKey;

    internal DictionarySerializer(TomlTable table, Action? onEnd = null)
    {
        _table = table;
        _onEnd = onEnd;
    }

    public void End(ISerdeInfo info)
    {
        _onEnd?.Invoke();
    }

    public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = b;
        }
    }

    public void WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        WriteString(typeInfo, index, c.ToString());
    }

    public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)b;
        }
    }

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)u16;
        }
    }

    public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)u32;
        }
    }

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)u64;
        }
    }

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)b;
        }
    }

    public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)i16;
        }
    }

    public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (long)i32;
        }
    }

    public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = i64;
        }
    }

    public void WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (double)f;
        }
    }

    public void WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = d;
        }
    }

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = (double)d;
        }
    }

    public void WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        if (index % 2 == 0)
        {
            // This is a key
            _currentKey = s;
        }
        else
        {
            // This is a value
            if (_currentKey != null)
            {
                _table[_currentKey] = s;
            }
        }
    }

    public void WriteNull(ISerdeInfo typeInfo, int index)
    {
        // TOML doesn't support null
    }

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = dt;
        }
    }

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = dt;
        }
    }

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            _table[_currentKey] = Convert.ToBase64String(bytes.Span);
        }
    }

    public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
    {
        if (index % 2 == 0)
        {
            throw new InvalidOperationException("Dictionary keys must be strings");
        }
        if (_currentKey != null)
        {
            var nestedTable = new TomlTable();
            _table[_currentKey] = nestedTable;
            var tableSerializer = new TableSerializer(nestedTable);
            serialize.Serialize(value, tableSerializer);
        }
    }
}

/// <summary>
/// Helper serializer for writing enum values.
/// </summary>
internal sealed class EnumSerializer : ITypeSerializer
{
    private readonly Action<object> _writeValue;

    internal EnumSerializer(Action<object> writeValue)
    {
        _writeValue = writeValue;
    }

    public void End(ISerdeInfo info)
    {
    }

    public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        _writeValue(c.ToString());
    }

    public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        _writeValue((long)b);
    }

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        _writeValue((long)u16);
    }

    public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        _writeValue((long)u32);
    }

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        _writeValue((long)u64);
    }

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        _writeValue((long)b);
    }

    public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        _writeValue((long)i16);
    }

    public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        _writeValue((long)i32);
    }

    public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        _writeValue(i64);
    }

    public void WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        _writeValue(s);
    }

    public void WriteNull(ISerdeInfo typeInfo, int index)
    {
    }

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }

    public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
    {
        throw new NotSupportedException("Enum values must be strings or integers");
    }
}

/// <summary>
/// Helper serializer that knows the field name and parent table when writing a field value.
/// </summary>
internal sealed class FieldSerializer : ISerializer
{
    private readonly TomlTable _parentTable;
    private readonly string _fieldName;

    internal FieldSerializer(TomlTable parentTable, string fieldName)
    {
        _parentTable = parentTable;
        _fieldName = fieldName;
    }

    public void WriteBool(bool b)
    {
        _parentTable[_fieldName] = b;
    }

    public void WriteChar(char c)
    {
        _parentTable[_fieldName] = c.ToString();
    }

    public void WriteU8(byte b)
    {
        _parentTable[_fieldName] = (long)b;
    }

    public void WriteU16(ushort u16)
    {
        _parentTable[_fieldName] = (long)u16;
    }

    public void WriteU32(uint u32)
    {
        _parentTable[_fieldName] = (long)u32;
    }

    public void WriteU64(ulong u64)
    {
        _parentTable[_fieldName] = (long)u64;
    }

    public void WriteI8(sbyte b)
    {
        _parentTable[_fieldName] = (long)b;
    }

    public void WriteI16(short i16)
    {
        _parentTable[_fieldName] = (long)i16;
    }

    public void WriteI32(int i32)
    {
        _parentTable[_fieldName] = (long)i32;
    }

    public void WriteI64(long i64)
    {
        _parentTable[_fieldName] = i64;
    }

    public void WriteF32(float f)
    {
        _parentTable[_fieldName] = (double)f;
    }

    public void WriteF64(double d)
    {
        _parentTable[_fieldName] = d;
    }

    public void WriteDecimal(decimal d)
    {
        _parentTable[_fieldName] = (double)d;
    }

    public void WriteString(string s)
    {
        _parentTable[_fieldName] = s;
    }

    public void WriteNull()
    {
        // TOML doesn't have null, skip
    }

    public void WriteDateTime(DateTime dt)
    {
        _parentTable[_fieldName] = dt;
    }

    public void WriteDateTimeOffset(DateTimeOffset dt)
    {
        _parentTable[_fieldName] = dt;
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        _parentTable[_fieldName] = Convert.ToBase64String(bytes.Span);
    }

    public ITypeSerializer WriteCollection(ISerdeInfo info, int? size)
    {
        switch (info.Kind)
        {
            case InfoKind.Dictionary:
                var table = new TomlTable();
                _parentTable[_fieldName] = table;
                return new DictionarySerializer(table);
            case InfoKind.List:
                var array = new TomlArray();
                _parentTable[_fieldName] = array;
                return new ArraySerializer(array);
            default:
                throw new ArgumentException($"TypeKind is {info.Kind}, expected List or Dictionary");
        }
    }

    public ITypeSerializer WriteType(ISerdeInfo typeInfo)
    {
        switch (typeInfo.Kind)
        {
            case InfoKind.Enum:
                return new EnumSerializer(value => _parentTable[_fieldName] = value);
            case InfoKind.CustomType:
            case InfoKind.Nullable:
                // Create a nested table for custom types
                var nestedTable = new TomlTable();
                _parentTable[_fieldName] = nestedTable;
                return new TableSerializer(nestedTable);
            default:
                throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}");
        }
    }
}
