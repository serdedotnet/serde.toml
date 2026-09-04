using System.Buffers;
using System.Globalization;
using Serde;
using Tomlyn;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Deserializes Serde values from TOML documents and values.
/// </summary>
public sealed class TomlDeserializer : IDeserializer
{
    private readonly object _currentValue;

    internal TomlDeserializer(object value)
    {
        _currentValue = value;
    }

    /// <summary>
    /// Deserializes a value from a TOML string.
    /// </summary>
    public static T Deserialize<T>(string toml, IDeserialize<T> deserialize)
    {
        try
        {
            TomlTable model =
                global::Tomlyn.TomlSerializer.Deserialize(
                    toml,
                    TomlynModelContext.Instance.TableInfo
                )
                ?? throw new DeserializeException("TOML document did not produce a root table.");
            return deserialize.Deserialize(new TomlDeserializer(model));
        }
        catch (TomlException exception)
        {
            throw new DeserializeException($"Invalid TOML: {exception.Message}");
        }
    }

    public static T Deserialize<T, TProvider>(string toml)
        where TProvider : IDeserializeProvider<T> => Deserialize(toml, TProvider.Instance);

    public static T Deserialize<T>(string toml)
        where T : IDeserializeProvider<T> => Deserialize(toml, T.Instance);

    public T? ReadNullableRef<T>(IDeserialize<T> deserialize)
        where T : class => deserialize.Deserialize(this);

    public bool TryReadNull() => false;

    public bool ReadBool() =>
        _currentValue is bool value
            ? value
            : throw DeserializerHelpers.TypeMismatch("boolean", _currentValue);

    public char ReadChar() =>
        _currentValue is string { Length: 1 } value
            ? value[0]
            : throw DeserializerHelpers.TypeMismatch("single-character string", _currentValue);

    public byte ReadU8()
    {
        var value = ReadU64();
        return value <= byte.MaxValue
            ? (byte)value
            : throw DeserializerHelpers.OutOfRange(value, "Byte");
    }

    public ushort ReadU16()
    {
        var value = ReadU64();
        return value <= ushort.MaxValue
            ? (ushort)value
            : throw DeserializerHelpers.OutOfRange(value, "UInt16");
    }

    public uint ReadU32()
    {
        var value = ReadU64();
        return value <= uint.MaxValue
            ? (uint)value
            : throw DeserializerHelpers.OutOfRange(value, "UInt32");
    }

    public ulong ReadU64()
    {
        var value = ReadI64();
        if (value < 0)
        {
            throw new DeserializeException($"Expected an unsigned integer, found {value}.");
        }

        return (ulong)value;
    }

    public UInt128 ReadU128() => ReadU64();
    public sbyte ReadI8()
    {
        var value = ReadI64();
        return value is >= sbyte.MinValue and <= sbyte.MaxValue
            ? (sbyte)value
            : throw DeserializerHelpers.OutOfRange(value, "SByte");
    }

    public short ReadI16()
    {
        var value = ReadI64();
        return value is >= short.MinValue and <= short.MaxValue
            ? (short)value
            : throw DeserializerHelpers.OutOfRange(value, "Int16");
    }

    public int ReadI32()
    {
        var value = ReadI64();
        return value is >= int.MinValue and <= int.MaxValue
            ? (int)value
            : throw DeserializerHelpers.OutOfRange(value, "Int32");
    }

    public long ReadI64() =>
        _currentValue is long value
            ? value
            : throw DeserializerHelpers.TypeMismatch("integer", _currentValue);

    public Int128 ReadI128() => ReadI64();
    public float ReadF32()
    {
        var value = ReadF64();
        if (double.IsFinite(value) && (value < -float.MaxValue || value > float.MaxValue))
        {
            throw DeserializerHelpers.OutOfRange(value, "Single");
        }

        return (float)value;
    }

    public double ReadF64() =>
        _currentValue switch
        {
            double value => value,
            long value => value,
            _ => throw DeserializerHelpers.TypeMismatch("float", _currentValue),
        };

    public decimal ReadDecimal()
    {
        try
        {
            return _currentValue switch
            {
                double value => checked((decimal)value),
                long value => value,
                _ => throw DeserializerHelpers.TypeMismatch("float", _currentValue),
            };
        }
        catch (OverflowException)
        {
            throw DeserializerHelpers.OutOfRange(_currentValue, "Decimal");
        }
    }

    public string ReadString() =>
        _currentValue as string
        ?? throw DeserializerHelpers.TypeMismatch("string", _currentValue);

    public DateTime ReadDateTime() =>
        _currentValue switch
        {
            DateTime value => value,
            DateTimeOffset value => value.UtcDateTime,
            TomlDateTime
            {
                Kind: TomlDateTimeKind.OffsetDateTimeByZ
            } value => value.DateTime.UtcDateTime,
            TomlDateTime
            {
                Kind: TomlDateTimeKind.OffsetDateTimeByNumber
            } value => value.DateTime.LocalDateTime,
            TomlDateTime
            {
                Kind: TomlDateTimeKind.LocalDateTime or TomlDateTimeKind.LocalDate
            } value => DateTime.SpecifyKind(value.DateTime.DateTime, DateTimeKind.Unspecified),
            string value => ParseDateTime(value),
            _ => throw DeserializerHelpers.TypeMismatch("date-time", _currentValue),
        };

    public DateTimeOffset ReadDateTimeOffset() =>
        _currentValue switch
        {
            DateTimeOffset value => value,
            DateTime { Kind: not DateTimeKind.Unspecified } value => new DateTimeOffset(value),
            TomlDateTime
            {
                Kind: TomlDateTimeKind.OffsetDateTimeByZ
                    or TomlDateTimeKind.OffsetDateTimeByNumber
            } value => value.DateTime,
            string value => ParseDateTimeOffset(value),
            _ => throw DeserializerHelpers.TypeMismatch("offset date-time", _currentValue),
        };

    public DateOnly ReadDateOnly() =>
        _currentValue switch
        {
            TomlDateTime { Kind: TomlDateTimeKind.LocalDate } value =>
                DateOnly.FromDateTime(value.DateTime.DateTime),
            string value => ParseDateOnly(value),
            _ => throw DeserializerHelpers.TypeMismatch("local date", _currentValue),
        };

    public TimeOnly ReadTimeOnly() =>
        _currentValue switch
        {
            TomlDateTime { Kind: TomlDateTimeKind.LocalTime } value =>
                TimeOnly.FromDateTime(value.DateTime.DateTime),
            string value => ParseTimeOnly(value),
            _ => throw DeserializerHelpers.TypeMismatch("local time", _currentValue),
        };

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(ReadString());
        }
        catch (FormatException)
        {
            throw new DeserializeException("Expected a Base64-encoded byte array.");
        }
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
    }

    public int ReadEnum(ISerdeInfo info)
    {
        var name = ReadString();
        for (var index = 0; index < info.FieldCount; index++)
        {
            if (info.GetFieldStringName(index) == name)
            {
                return index;
            }
        }

        throw new DeserializeException($"Unknown enum member '{name}' for enum '{info.Name}'.");
    }

    public ITypeDeserializer ReadType(ISerdeInfo info)
    {
        return info.Kind switch
        {
            InfoKind.List or InfoKind.Tuple => new ListDeserializer(
                DeserializerHelpers.ExpectArray(_currentValue)
            ),
            InfoKind.Dictionary => new DictionaryDeserializer(
                DeserializerHelpers.ExpectTable(_currentValue)
            ),
            InfoKind.CustomType or InfoKind.Union => new TableDeserializer(
                DeserializerHelpers.ExpectTable(_currentValue)
            ),
            _ => throw new ArgumentException($"Unsupported type kind: {info.Kind}", nameof(info)),
        };
    }

    public void Dispose() { }

    private static DateTime ParseDateTime(string value)
    {
        try
        {
            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
        }
        catch (FormatException)
        {
            throw new DeserializeException($"Invalid date-time value '{value}'.");
        }
    }

    private static DateTimeOffset ParseDateTimeOffset(string value)
    {
        try
        {
            return DateTimeOffset.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
        }
        catch (FormatException)
        {
            throw new DeserializeException($"Invalid offset date-time value '{value}'.");
        }
    }

    private static DateOnly ParseDateOnly(string value)
    {
        try
        {
            return DateOnly.Parse(value, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new DeserializeException($"Invalid local date value '{value}'.");
        }
    }

    private static TimeOnly ParseTimeOnly(string value)
    {
        try
        {
            return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new DeserializeException($"Invalid local time value '{value}'.");
        }
    }
}
