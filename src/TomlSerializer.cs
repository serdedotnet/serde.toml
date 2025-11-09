using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Implements ISerializer for TOML format using Tomlyn library.
/// </summary>
public sealed class TomlSerializer : ISerializer, ITypeSerializer
{
    private readonly TomlTable _rootTable;
    private readonly Stack<object> _tableStack = new();
    private object _currentContainer;

    private TomlSerializer(TomlTable rootTable)
    {
        _rootTable = rootTable;
        _currentContainer = rootTable;
    }

    /// <summary>
    /// Serialize the given type to a TOML string.
    /// </summary>
    public static string Serialize<T>(T value, ISerialize<T> ser)
    {
        var rootTable = new TomlTable();
        var serializer = new TomlSerializer(rootTable);
        ser.Serialize(value, serializer);
        return Tomlyn.Toml.FromModel(rootTable);
    }

    public static string Serialize<T, TProvider>(T value) where TProvider : ISerializeProvider<T>
    {
        return Serialize(value, TProvider.Instance);
    }

    public static string Serialize<T>(T value) where T : ISerializeProvider<T>
    {
        return Serialize(value, T.Instance);
    }

    public void WriteBool(bool b)
    {
        AddToCurrentContainer(b);
    }

    public void WriteChar(char c)
    {
        AddToCurrentContainer(c.ToString());
    }

    public void WriteU8(byte b)
    {
        AddToCurrentContainer((long)b);
    }

    public void WriteU16(ushort u16)
    {
        AddToCurrentContainer((long)u16);
    }

    public void WriteU32(uint u32)
    {
        AddToCurrentContainer((long)u32);
    }

    public void WriteU64(ulong u64)
    {
        AddToCurrentContainer((long)u64);
    }

    public void WriteI8(sbyte b)
    {
        AddToCurrentContainer((long)b);
    }

    public void WriteI16(short i16)
    {
        AddToCurrentContainer((long)i16);
    }

    public void WriteI32(int i32)
    {
        AddToCurrentContainer((long)i32);
    }

    public void WriteI64(long i64)
    {
        AddToCurrentContainer(i64);
    }

    public void WriteF32(float f)
    {
        AddToCurrentContainer((double)f);
    }

    public void WriteF64(double d)
    {
        AddToCurrentContainer(d);
    }

    public void WriteDecimal(decimal d)
    {
        AddToCurrentContainer((double)d);
    }

    public void WriteString(string s)
    {
        AddToCurrentContainer(s);
    }

    public void WriteNull()
    {
        // TOML doesn't have a null value, skip it
    }

    public void WriteDateTime(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime must be in UTC");
        }
        AddToCurrentContainer(dt);
    }

    public void WriteDateTimeOffset(DateTimeOffset dt)
    {
        AddToCurrentContainer(dt);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        // TOML doesn't have native byte array support, convert to base64 string
        AddToCurrentContainer(Convert.ToBase64String(bytes.Span));
    }

    ITypeSerializer ISerializer.WriteCollection(ISerdeInfo info, int? size)
    {
        switch (info.Kind)
        {
            case InfoKind.Dictionary:
                var table = new TomlTable();
                AddToCurrentContainer(table);
                _tableStack.Push(_currentContainer);
                _currentContainer = table;
                return new DictionarySerializer(this);
            case InfoKind.List:
                var array = new TomlArray();
                AddToCurrentContainer(array);
                _tableStack.Push(_currentContainer);
                _currentContainer = array;
                return new ArraySerializer(this);
            default:
                throw new ArgumentException($"TypeKind is {info.Kind}, expected List or Dictionary");
        }
    }

    ITypeSerializer ISerializer.WriteType(ISerdeInfo typeInfo)
    {
        switch (typeInfo.Kind)
        {
            case InfoKind.Enum:
                return new EnumSerializer(this);
            case InfoKind.CustomType:
            case InfoKind.Nullable:
                var table = new TomlTable();
                AddToCurrentContainer(table);
                _tableStack.Push(_currentContainer);
                _currentContainer = table;
                return this;
            default:
                throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}");
        }
    }

    private void AddToCurrentContainer(object value)
    {
        if (_currentContainer is TomlArray array)
        {
            array.Add(value);
        }
        // For tables, values will be added via WriteXxx methods with field names
    }

    // ITypeSerializer implementation
    void ITypeSerializer.WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = b;
        }
    }

    void ITypeSerializer.WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = c.ToString();
        }
    }

    void ITypeSerializer.WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)b;
        }
    }

    void ITypeSerializer.WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)u16;
        }
    }

    void ITypeSerializer.WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)u32;
        }
    }

    void ITypeSerializer.WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)u64;
        }
    }

    void ITypeSerializer.WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)b;
        }
    }

    void ITypeSerializer.WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)i16;
        }
    }

    void ITypeSerializer.WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (long)i32;
        }
    }

    void ITypeSerializer.WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = i64;
        }
    }

    void ITypeSerializer.WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (double)f;
        }
    }

    void ITypeSerializer.WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = d;
        }
    }

    void ITypeSerializer.WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = (double)d;
        }
    }

    void ITypeSerializer.WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = s;
        }
    }

    void ITypeSerializer.WriteNull(ISerdeInfo typeInfo, int index)
    {
        // TOML doesn't have null values, skip
    }

    void ITypeSerializer.WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        if (_currentContainer is TomlTable table)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be in UTC");
            }
            table[typeInfo.GetFieldStringName(index)] = dt;
        }
    }

    void ITypeSerializer.WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = dt;
        }
    }

    void ITypeSerializer.WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        if (_currentContainer is TomlTable table)
        {
            table[typeInfo.GetFieldStringName(index)] = Convert.ToBase64String(bytes.Span);
        }
    }

    void ITypeSerializer.WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize)
    {
        if (_currentContainer is TomlTable table)
        {
            var fieldName = typeInfo.GetFieldStringName(index);
            var nestedTable = new TomlTable();
            table[fieldName] = nestedTable;
            _tableStack.Push(_currentContainer);
            _currentContainer = nestedTable;
            serialize.Serialize(value, this);
            _currentContainer = _tableStack.Pop();
        }
    }

    void ITypeSerializer.End(ISerdeInfo info)
    {
        if (_tableStack.Count > 0)
        {
            _currentContainer = _tableStack.Pop();
        }
    }

    private sealed class ArraySerializer(TomlSerializer serializer) : ITypeSerializer
    {
        public void End(ISerdeInfo info)
        {
            if (serializer._tableStack.Count > 0)
            {
                serializer._currentContainer = serializer._tableStack.Pop();
            }
        }

        public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
        {
            serializer.WriteBool(b);
        }

        public void WriteChar(ISerdeInfo typeInfo, int index, char c)
        {
            serializer.WriteChar(c);
        }

        public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
        {
            serializer.WriteU8(b);
        }

        public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
        {
            serializer.WriteU16(u16);
        }

        public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
        {
            serializer.WriteU32(u32);
        }

        public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
        {
            serializer.WriteU64(u64);
        }

        public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
        {
            serializer.WriteI8(b);
        }

        public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
        {
            serializer.WriteI16(i16);
        }

        public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
        {
            serializer.WriteI32(i32);
        }

        public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
        {
            serializer.WriteI64(i64);
        }

        public void WriteF32(ISerdeInfo typeInfo, int index, float f)
        {
            serializer.WriteF32(f);
        }

        public void WriteF64(ISerdeInfo typeInfo, int index, double d)
        {
            serializer.WriteF64(d);
        }

        public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
        {
            serializer.WriteDecimal(d);
        }

        public void WriteString(ISerdeInfo typeInfo, int index, string s)
        {
            serializer.WriteString(s);
        }

        public void WriteNull(ISerdeInfo typeInfo, int index)
        {
            serializer.WriteNull();
        }

        public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
        {
            serializer.WriteDateTime(dt);
        }

        public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
        {
            serializer.WriteDateTimeOffset(dt);
        }

        public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
        {
            serializer.WriteBytes(bytes);
        }

        public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
        {
            var nestedTable = new TomlTable();
            serializer.AddToCurrentContainer(nestedTable);
            serializer._tableStack.Push(serializer._currentContainer);
            serializer._currentContainer = nestedTable;
            serialize.Serialize(value, serializer);
            serializer._currentContainer = serializer._tableStack.Pop();
        }
    }

    private sealed class DictionarySerializer(TomlSerializer serializer) : ITypeSerializer
    {
        private string? _currentKey;

        public void End(ISerdeInfo info)
        {
            if (serializer._tableStack.Count > 0)
            {
                serializer._currentContainer = serializer._tableStack.Pop();
            }
        }

        public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = b;
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
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)b;
            }
        }

        public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)u16;
            }
        }

        public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)u32;
            }
        }

        public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)u64;
            }
        }

        public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)b;
            }
        }

        public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)i16;
            }
        }

        public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (long)i32;
            }
        }

        public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = i64;
            }
        }

        public void WriteF32(ISerdeInfo typeInfo, int index, float f)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (double)f;
            }
        }

        public void WriteF64(ISerdeInfo typeInfo, int index, double d)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = d;
            }
        }

        public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = (double)d;
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
                if (serializer._currentContainer is TomlTable table && _currentKey != null)
                {
                    table[_currentKey] = s;
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
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                if (dt.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("DateTime must be in UTC");
                }
                table[_currentKey] = dt;
            }
        }

        public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = dt;
            }
        }

        public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                table[_currentKey] = Convert.ToBase64String(bytes.Span);
            }
        }

        public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
        {
            if (index % 2 == 0)
            {
                throw new InvalidOperationException("Dictionary keys must be strings");
            }
            if (serializer._currentContainer is TomlTable table && _currentKey != null)
            {
                var nestedTable = new TomlTable();
                table[_currentKey] = nestedTable;
                serializer._tableStack.Push(serializer._currentContainer);
                serializer._currentContainer = nestedTable;
                serialize.Serialize(value, serializer);
                serializer._currentContainer = serializer._tableStack.Pop();
            }
        }
    }

    private sealed class EnumSerializer(TomlSerializer serializer) : ITypeSerializer
    {
        public void End(ISerdeInfo info)
        {
        }

        public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
        {
            throw new NotSupportedException("Enum values must be strings or integers");
        }

        public void WriteChar(ISerdeInfo typeInfo, int index, char c)
        {
            serializer.WriteChar(c);
        }

        public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
        {
            serializer.WriteU8(b);
        }

        public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
        {
            serializer.WriteU16(u16);
        }

        public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
        {
            serializer.WriteU32(u32);
        }

        public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
        {
            serializer.WriteU64(u64);
        }

        public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
        {
            serializer.WriteI8(b);
        }

        public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
        {
            serializer.WriteI16(i16);
        }

        public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
        {
            serializer.WriteI32(i32);
        }

        public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
        {
            serializer.WriteI64(i64);
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
            serializer.WriteString(s);
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
}
