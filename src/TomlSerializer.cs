using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Serializes Serde values as TOML documents.
/// </summary>
public sealed class TomlSerializer : ISerializer
{
    private readonly TomlTable _rootTable;

    private TomlSerializer(TomlTable rootTable)
    {
        _rootTable = rootTable;
    }

    /// <summary>
    /// Serializes the given value to a TOML string.
    /// </summary>
    public static string Serialize<T>(T value, ISerialize<T> serialize)
    {
        var rootTable = new TomlTable();
        serialize.Serialize(value, new TomlSerializer(rootTable));
        return global::Tomlyn.TomlSerializer.Serialize(
            rootTable,
            TomlynModelContext.Instance.TableInfo
        );
    }

    public static string Serialize<T, TProvider>(T value)
        where TProvider : ISerializeProvider<T> => Serialize(value, TProvider.Instance);

    public static string Serialize<T>(T value)
        where T : ISerializeProvider<T> => Serialize(value, T.Instance);

    public ITypeSerializer WriteCollection(ISerdeInfo info, int? size)
    {
        if (info.Kind != InfoKind.Dictionary)
        {
            throw RootValueNotSupported(info.Kind);
        }

        return new DictionarySerializer(_rootTable);
    }

    public ITypeSerializer WriteType(ISerdeInfo info)
    {
        return info.Kind switch
        {
            InfoKind.CustomType or InfoKind.Union => new TableSerializer(_rootTable),
            _ => throw RootValueNotSupported(info.Kind),
        };
    }

    public ITypeSerializer WriteType(ISerdeInfo info, int fieldCount) => WriteType(info);

    public void WriteBool(bool value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteChar(char value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteU8(byte value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteU16(ushort value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteU32(uint value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteU64(ulong value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteU128(UInt128 value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteI8(sbyte value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteI16(short value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteI32(int value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteI64(long value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteI128(Int128 value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteF32(float value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteF64(double value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteDecimal(decimal value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteString(string value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteNull() => throw RootValueNotSupported(InfoKind.Nullable);
    public void WriteDateTime(DateTime value) => throw RootValueNotSupported(InfoKind.Primitive);

    public void WriteDateTimeOffset(DateTimeOffset value) =>
        throw RootValueNotSupported(InfoKind.Primitive);

    public void WriteDateOnly(DateOnly value) => throw RootValueNotSupported(InfoKind.Primitive);
    public void WriteTimeOnly(TimeOnly value) => throw RootValueNotSupported(InfoKind.Primitive);

    public void WriteBytes(ReadOnlyMemory<byte> value) =>
        throw RootValueNotSupported(InfoKind.Primitive);

    public void WriteEnum(ISerdeInfo info, int ordinal) =>
        throw RootValueNotSupported(InfoKind.Enum);

    private static NotSupportedException RootValueNotSupported(InfoKind kind) =>
        new($"TOML documents must have a table at the root; cannot serialize {kind} as a document.");
}
