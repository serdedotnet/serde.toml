using System.Buffers;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

internal static class DeserializerHelpers
{
    public static DeserializeException TypeMismatch(string expected, object actual) =>
        new($"Expected {expected}, found {actual.GetType().Name}.");

    public static DeserializeException OutOfRange(object value, string targetType) =>
        new($"Value {value} is outside the range of {targetType}.");

    public static TomlArrayValues ExpectArray(object value) =>
        value switch
        {
            TomlArray array => new TomlArrayValues(array),
            TomlTableArray array => new TomlArrayValues(array),
            _ => throw TypeMismatch("array", value),
        };

    public static TomlTable ExpectTable(object value) =>
        value as TomlTable ?? throw TypeMismatch("table", value);

    public static int FindField(ISerdeInfo info, string name)
    {
        for (var index = 0; index < info.FieldCount; index++)
        {
            if (info.GetFieldStringName(index) == name)
            {
                return index;
            }
        }

        return ITypeDeserializer.IndexNotFound;
    }
}

internal readonly struct TomlArrayValues
{
    private readonly TomlArray? _array;
    private readonly TomlTableArray? _tableArray;

    public TomlArrayValues(TomlArray array)
    {
        _array = array;
    }

    public TomlArrayValues(TomlTableArray tableArray)
    {
        _tableArray = tableArray;
    }

    public int Count => _array?.Count ?? _tableArray!.Count;

    public object this[int index] => _array is not null ? _array[index]! : _tableArray![index];
}

internal abstract class TomlTypeDeserializer : ITypeDeserializer
{
    public abstract int? SizeOpt { get; }
    public abstract int TryReadIndex(ISerdeInfo info);

    public virtual (int, string?) TryReadIndexWithName(ISerdeInfo info) =>
        (TryReadIndex(info), null);

    protected abstract object TakeValue();

    public IDeserializer ReadFieldStart(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue());

    public void ReadFieldEnd(ISerdeInfo info, int index, IDeserializer deserializer) { }

    public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize)
        where T : class? => deserialize.Deserialize(new TomlDeserializer(TakeValue()));

    public void SkipValue(ISerdeInfo info, int index) => _ = TakeValue();

    public bool ReadBool(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadBool();

    public char ReadChar(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadChar();

    public byte ReadU8(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadU8();

    public ushort ReadU16(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadU16();

    public uint ReadU32(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadU32();

    public ulong ReadU64(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadU64();

    public UInt128 ReadU128(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadU128();

    public sbyte ReadI8(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadI8();

    public short ReadI16(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadI16();

    public int ReadI32(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadI32();

    public long ReadI64(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadI64();

    public Int128 ReadI128(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadI128();

    public float ReadF32(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadF32();

    public double ReadF64(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadF64();

    public decimal ReadDecimal(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadDecimal();

    public string ReadString(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadString();

    public DateTime ReadDateTime(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadDateTime();

    public DateTimeOffset ReadDateTimeOffset(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadDateTimeOffset();

    public DateOnly ReadDateOnly(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadDateOnly();

    public TimeOnly ReadTimeOnly(ISerdeInfo info, int index) =>
        new TomlDeserializer(TakeValue()).ReadTimeOnly();

    public void ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer) =>
        new TomlDeserializer(TakeValue()).ReadBytes(writer);

    public int ReadEnum(ISerdeInfo typeInfo, int index, ISerdeInfo fieldInfo) =>
        new TomlDeserializer(TakeValue()).ReadEnum(fieldInfo);

    public void End(ISerdeInfo info) { }
}
