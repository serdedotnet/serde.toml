using System;
using System.Buffers;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Implements ISerializer and ITypeSerializer for TOML tables (custom types).
/// </summary>
internal sealed class TableSerializer : ISerializer, ITypeSerializer
{
    private readonly TomlTable _table;
    private readonly Action? _onEnd;

    internal TableSerializer(TomlTable table, Action? _onEnd = null)
    {
        _table = table;
        this._onEnd = _onEnd;
    }

    public void End(ISerdeInfo info)
    {
        _onEnd?.Invoke();
    }

    // ISerializer implementation - these throw since TableSerializer writes to tables, not arrays
    void ISerializer.WriteBool(bool b)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteChar(char c)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteU8(byte b)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteU16(ushort u16)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteU32(uint u32)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteU64(ulong u64)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteI8(sbyte b)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteI16(short i16)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteI32(int i32)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteI64(long i64)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteF32(float f)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteF64(double d)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteDecimal(decimal d)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteString(string s)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteNull()
    {
        throw new NotSupportedException("TOML does not support null values");
    }

    void ISerializer.WriteDateTime(DateTime dt)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteDateTimeOffset(DateTimeOffset dt)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    void ISerializer.WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    ITypeSerializer ISerializer.WriteCollection(ISerdeInfo info, int? size)
    {
        throw new NotSupportedException("TableSerializer writes to tables. Use ITypeSerializer methods with field info.");
    }

    ITypeSerializer ISerializer.WriteType(ISerdeInfo typeInfo)
    {
        // When called as ISerializer.WriteType, we're already in the right context
        return this;
    }

    // ITypeSerializer implementation
    public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        _table[typeInfo.GetFieldStringName(index)] = b;
    }

    public void WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        _table[typeInfo.GetFieldStringName(index)] = c.ToString();
    }

    public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)b;
    }

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)u16;
    }

    public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)u32;
    }

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)u64;
    }

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)b;
    }

    public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)i16;
    }

    public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        _table[typeInfo.GetFieldStringName(index)] = (long)i32;
    }

    public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        _table[typeInfo.GetFieldStringName(index)] = i64;
    }

    public void WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        _table[typeInfo.GetFieldStringName(index)] = (double)f;
    }

    public void WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        _table[typeInfo.GetFieldStringName(index)] = d;
    }

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        _table[typeInfo.GetFieldStringName(index)] = (double)d;
    }

    public void WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        _table[typeInfo.GetFieldStringName(index)] = s;
    }

    public void WriteNull(ISerdeInfo typeInfo, int index)
    {
        // TOML doesn't have null values, skip
    }

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        // TOML supports both UTC and local datetime formats
        _table[typeInfo.GetFieldStringName(index)] = dt;
    }

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        _table[typeInfo.GetFieldStringName(index)] = dt;
    }

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        _table[typeInfo.GetFieldStringName(index)] = Convert.ToBase64String(bytes.Span);
    }

    public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize) where T : class?
    {
        var fieldName = typeInfo.GetFieldStringName(index);
        
        // Create a field serializer to handle the value
        var fieldSerializer = new FieldSerializer(_table, fieldName);
        serialize.Serialize(value, fieldSerializer);
    }
}
