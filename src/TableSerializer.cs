using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal sealed class TableSerializer(TomlTable table) : ITypeSerializer
{
    public ISerializer WriteFieldStart(ISerdeInfo typeInfo, int index) =>
        CreateFieldSerializer(typeInfo, index);

    public void WriteFieldEnd(ISerdeInfo typeInfo, int index, ISerializer serializer) { }

    public void End(ISerdeInfo info) { }

    public void SkipValue(ISerdeInfo typeInfo, int index) { }

    public void WriteBool(ISerdeInfo typeInfo, int index, bool value) =>
        Set(typeInfo, index, value);

    public void WriteChar(ISerdeInfo typeInfo, int index, char value) =>
        Set(typeInfo, index, value.ToString());

    public void WriteU8(ISerdeInfo typeInfo, int index, byte value) =>
        Set(typeInfo, index, (long)value);

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort value) =>
        Set(typeInfo, index, (long)value);

    public void WriteU32(ISerdeInfo typeInfo, int index, uint value) =>
        Set(typeInfo, index, (long)value);

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong value) =>
        Set(typeInfo, index, TomlValues.ToInteger(value));

    public void WriteU128(ISerdeInfo typeInfo, int index, UInt128 value) =>
        Set(typeInfo, index, TomlValues.ToInteger(value));

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte value) =>
        Set(typeInfo, index, (long)value);

    public void WriteI16(ISerdeInfo typeInfo, int index, short value) =>
        Set(typeInfo, index, (long)value);

    public void WriteI32(ISerdeInfo typeInfo, int index, int value) =>
        Set(typeInfo, index, (long)value);

    public void WriteI64(ISerdeInfo typeInfo, int index, long value) => Set(typeInfo, index, value);

    public void WriteI128(ISerdeInfo typeInfo, int index, Int128 value) =>
        Set(typeInfo, index, TomlValues.ToInteger(value));

    public void WriteF32(ISerdeInfo typeInfo, int index, float value) =>
        Set(typeInfo, index, (double)value);

    public void WriteF64(ISerdeInfo typeInfo, int index, double value) =>
        Set(typeInfo, index, value);

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal value) =>
        Set(typeInfo, index, TomlValues.ToFloat(value));

    public void WriteString(ISerdeInfo typeInfo, int index, string value) =>
        Set(typeInfo, index, value);

    public void WriteNull(ISerdeInfo typeInfo, int index) { }

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime value) =>
        Set(typeInfo, index, TomlValues.ToDateTime(value));

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset value) =>
        Set(typeInfo, index, TomlValues.ToDateTimeOffset(value));

    public void WriteDateOnly(ISerdeInfo typeInfo, int index, DateOnly value) =>
        Set(typeInfo, index, TomlValues.ToDateOnly(value));

    public void WriteTimeOnly(ISerdeInfo typeInfo, int index, TimeOnly value) =>
        Set(typeInfo, index, TomlValues.ToTimeOnly(value));

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> value) =>
        Set(typeInfo, index, Convert.ToBase64String(value.Span));

    public void WriteEnum(
        ISerdeInfo typeInfo,
        int index,
        ISerdeInfo fieldInfo,
        int ordinal
    ) => Set(typeInfo, index, TomlValues.GetEnumName(fieldInfo, ordinal));

    public void WriteValue<T>(
        ISerdeInfo typeInfo,
        int index,
        T value,
        ISerialize<T> serialize
    )
        where T : class? => serialize.Serialize(value, CreateFieldSerializer(typeInfo, index));

    private TomlValueSerializer CreateFieldSerializer(ISerdeInfo typeInfo, int index)
    {
        var fieldName = typeInfo.GetFieldStringName(index);
        return new TomlValueSerializer(
            value => table[fieldName] = value,
            () => table.Remove(fieldName)
        );
    }

    private void Set(ISerdeInfo typeInfo, int index, object value) =>
        table[typeInfo.GetFieldStringName(index)] = value;
}
