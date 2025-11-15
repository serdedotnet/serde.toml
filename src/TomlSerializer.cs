using System;
using System.Buffers;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Implements ISerializer for TOML format using Tomlyn library.
/// </summary>
public sealed class TomlSerializer : ISerializer
{
    private readonly TomlTable _rootTable;
    private readonly TomlArray? _currentArray;

    private TomlSerializer(TomlTable rootTable, TomlArray? currentArray = null)
    {
        _rootTable = rootTable;
        _currentArray = currentArray;
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
        AddToCurrentArray(b);
    }

    public void WriteChar(char c)
    {
        AddToCurrentArray(c.ToString());
    }

    public void WriteU8(byte b)
    {
        AddToCurrentArray((long)b);
    }

    public void WriteU16(ushort u16)
    {
        AddToCurrentArray((long)u16);
    }

    public void WriteU32(uint u32)
    {
        AddToCurrentArray((long)u32);
    }

    public void WriteU64(ulong u64)
    {
        AddToCurrentArray((long)u64);
    }

    public void WriteI8(sbyte b)
    {
        AddToCurrentArray((long)b);
    }

    public void WriteI16(short i16)
    {
        AddToCurrentArray((long)i16);
    }

    public void WriteI32(int i32)
    {
        AddToCurrentArray((long)i32);
    }

    public void WriteI64(long i64)
    {
        AddToCurrentArray(i64);
    }

    public void WriteF32(float f)
    {
        AddToCurrentArray((double)f);
    }

    public void WriteF64(double d)
    {
        AddToCurrentArray(d);
    }

    public void WriteDecimal(decimal d)
    {
        AddToCurrentArray((double)d);
    }

    public void WriteString(string s)
    {
        AddToCurrentArray(s);
    }

    public void WriteNull()
    {
        throw new NotSupportedException("TOML does not support null values");
    }

    public void WriteDateTime(DateTime dt)
    {
        // TOML supports both UTC and local datetime formats
        AddToCurrentArray(dt);
    }

    public void WriteDateTimeOffset(DateTimeOffset dt)
    {
        AddToCurrentArray(dt);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        // TOML doesn't have native byte array support, convert to base64 string
        AddToCurrentArray(Convert.ToBase64String(bytes.Span));
    }

    public ITypeSerializer WriteCollection(ISerdeInfo info, int? size)
    {
        switch (info.Kind)
        {
            case InfoKind.Dictionary:
                var table = new TomlTable();
                AddToCurrentArray(table);
                return new DictionarySerializer(table);
            case InfoKind.List:
                var array = new TomlArray();
                AddToCurrentArray(array);
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
                return new EnumSerializer(value => AddToCurrentArray(value));
            case InfoKind.CustomType:
            case InfoKind.Nullable:
                // For root-level types, write to the root table
                // For nested types in arrays, create a new table and add to array
                if (_currentArray == null)
                {
                    // Root level - write directly to _rootTable
                    return new TableSerializer(_rootTable);
                }
                else
                {
                    // Nested in array - create new table
                    var nestedTable = new TomlTable();
                    _currentArray.Add(nestedTable);
                    return new TableSerializer(nestedTable);
                }
            default:
                throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}");
        }
    }

    private void AddToCurrentArray(object value)
    {
        if (_currentArray != null)
        {
            _currentArray.Add(value);
        }
        else
        {
            throw new InvalidOperationException("TomlSerializer primitive write methods can only be called when writing to arrays.");
        }
    }
}
