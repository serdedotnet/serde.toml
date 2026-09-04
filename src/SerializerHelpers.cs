using Serde;
using Tomlyn;
using Tomlyn.Model;

namespace Serde.Toml;

internal static class TomlValues
{
    public static long ToInteger(ulong value)
    {
        if (value > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "TOML integers are limited to signed 64-bit values."
            );
        }

        return (long)value;
    }

    public static long ToInteger(UInt128 value)
    {
        if (value > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "TOML integers are limited to signed 64-bit values."
            );
        }

        return (long)value;
    }

    public static long ToInteger(Int128 value)
    {
        if (value < long.MinValue || value > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "TOML integers are limited to signed 64-bit values."
            );
        }

        return (long)value;
    }

    public static double ToFloat(decimal value)
    {
        var result = (double)value;
        try
        {
            if ((decimal)result == value)
            {
                return result;
            }
        }
        catch (OverflowException)
        {
        }

        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            "The decimal value cannot be represented exactly as a TOML float."
        );
    }

    public static string GetEnumName(ISerdeInfo info, int ordinal)
    {
        if ((uint)ordinal >= (uint)info.FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        return info.GetFieldStringName(ordinal);
    }

    public static TomlDateTime ToDateTime(DateTime value)
    {
        var precision = GetSecondPrecision(value.Ticks);
        return value.Kind switch
        {
            DateTimeKind.Utc => new TomlDateTime(
                new DateTimeOffset(value),
                precision,
                TomlDateTimeKind.OffsetDateTimeByZ
            ),
            DateTimeKind.Local => new TomlDateTime(
                new DateTimeOffset(value),
                precision,
                TomlDateTimeKind.OffsetDateTimeByNumber
            ),
            _ => new TomlDateTime(
                new DateTimeOffset(
                    DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
                    TimeSpan.Zero
                ),
                precision,
                TomlDateTimeKind.LocalDateTime
            ),
        };
    }

    public static TomlDateTime ToDateTimeOffset(DateTimeOffset value) =>
        new(
            value,
            GetSecondPrecision(value.Ticks),
            value.Offset == TimeSpan.Zero
                ? TomlDateTimeKind.OffsetDateTimeByZ
                : TomlDateTimeKind.OffsetDateTimeByNumber
        );

    public static TomlDateTime ToDateOnly(DateOnly value) =>
        new(value.Year, value.Month, value.Day);

    public static TomlDateTime ToTimeOnly(TimeOnly value) =>
        new(
            new DateTimeOffset(value.Ticks, TimeSpan.Zero),
            GetSecondPrecision(value.Ticks),
            TomlDateTimeKind.LocalTime
        );

    private static int GetSecondPrecision(long ticks)
    {
        var fractionalTicks = ticks % TimeSpan.TicksPerSecond;
        if (fractionalTicks == 0)
        {
            return 0;
        }

        var precision = 7;
        while (fractionalTicks % 10 == 0)
        {
            fractionalTicks /= 10;
            precision--;
        }

        return precision;
    }
}

internal sealed class TomlValueSerializer(Action<object> writeValue, Action? writeNull = null)
    : ISerializer
{
    public void WriteBool(bool value) => writeValue(value);
    public void WriteChar(char value) => writeValue(value.ToString());
    public void WriteU8(byte value) => writeValue((long)value);
    public void WriteU16(ushort value) => writeValue((long)value);
    public void WriteU32(uint value) => writeValue((long)value);
    public void WriteU64(ulong value) => writeValue(TomlValues.ToInteger(value));
    public void WriteU128(UInt128 value) => writeValue(TomlValues.ToInteger(value));
    public void WriteI8(sbyte value) => writeValue((long)value);
    public void WriteI16(short value) => writeValue((long)value);
    public void WriteI32(int value) => writeValue((long)value);
    public void WriteI64(long value) => writeValue(value);
    public void WriteI128(Int128 value) => writeValue(TomlValues.ToInteger(value));
    public void WriteF32(float value) => writeValue((double)value);
    public void WriteF64(double value) => writeValue(value);
    public void WriteDecimal(decimal value) => writeValue(TomlValues.ToFloat(value));
    public void WriteString(string value) => writeValue(value);

    public void WriteNull()
    {
        if (writeNull is null)
        {
            throw new NotSupportedException("TOML cannot represent null in this context.");
        }

        writeNull();
    }

    public void WriteDateTime(DateTime value) => writeValue(TomlValues.ToDateTime(value));
    public void WriteDateTimeOffset(DateTimeOffset value) =>
        writeValue(TomlValues.ToDateTimeOffset(value));
    public void WriteDateOnly(DateOnly value) => writeValue(TomlValues.ToDateOnly(value));
    public void WriteTimeOnly(TimeOnly value) => writeValue(TomlValues.ToTimeOnly(value));
    public void WriteBytes(ReadOnlyMemory<byte> value) =>
        writeValue(Convert.ToBase64String(value.Span));

    public void WriteEnum(ISerdeInfo info, int ordinal) =>
        writeValue(TomlValues.GetEnumName(info, ordinal));

    public ITypeSerializer WriteCollection(ISerdeInfo info, int? size)
    {
        switch (info.Kind)
        {
            case InfoKind.List:
            case InfoKind.Tuple:
                var array = new TomlArray();
                writeValue(array);
                return new ArraySerializer(array);
            case InfoKind.Dictionary:
                var table = new TomlTable();
                writeValue(table);
                return new DictionarySerializer(table);
            default:
                throw new ArgumentException(
                    $"Type kind is {info.Kind}, expected a list, tuple, or dictionary.",
                    nameof(info)
                );
        }
    }

    public ITypeSerializer WriteType(ISerdeInfo info)
    {
        if (info.Kind is not (InfoKind.CustomType or InfoKind.Union))
        {
            throw new ArgumentException(
                $"Type kind is {info.Kind}, expected a custom type or union.",
                nameof(info)
            );
        }

        var table = new TomlTable();
        writeValue(table);
        return new TableSerializer(table);
    }

    public ITypeSerializer WriteType(ISerdeInfo info, int fieldCount) => WriteType(info);
}

internal sealed class ArraySerializer(TomlArray array) : ITypeSerializer
{
    public ISerializer WriteFieldStart(ISerdeInfo typeInfo, int index) =>
        new TomlValueSerializer(array.Add);

    public void WriteFieldEnd(ISerdeInfo typeInfo, int index, ISerializer serializer) { }
    public void End(ISerdeInfo info) { }

    public void SkipValue(ISerdeInfo typeInfo, int index) =>
        throw new NotSupportedException("TOML arrays cannot omit elements.");

    public void WriteBool(ISerdeInfo typeInfo, int index, bool value) => array.Add(value);
    public void WriteChar(ISerdeInfo typeInfo, int index, char value) => array.Add(value.ToString());
    public void WriteU8(ISerdeInfo typeInfo, int index, byte value) => array.Add((long)value);
    public void WriteU16(ISerdeInfo typeInfo, int index, ushort value) => array.Add((long)value);
    public void WriteU32(ISerdeInfo typeInfo, int index, uint value) => array.Add((long)value);
    public void WriteU64(ISerdeInfo typeInfo, int index, ulong value) =>
        array.Add(TomlValues.ToInteger(value));

    public void WriteU128(ISerdeInfo typeInfo, int index, UInt128 value) =>
        array.Add(TomlValues.ToInteger(value));

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte value) => array.Add((long)value);
    public void WriteI16(ISerdeInfo typeInfo, int index, short value) => array.Add((long)value);
    public void WriteI32(ISerdeInfo typeInfo, int index, int value) => array.Add((long)value);
    public void WriteI64(ISerdeInfo typeInfo, int index, long value) => array.Add(value);
    public void WriteI128(ISerdeInfo typeInfo, int index, Int128 value) =>
        array.Add(TomlValues.ToInteger(value));

    public void WriteF32(ISerdeInfo typeInfo, int index, float value) => array.Add((double)value);
    public void WriteF64(ISerdeInfo typeInfo, int index, double value) => array.Add(value);
    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal value) =>
        array.Add(TomlValues.ToFloat(value));

    public void WriteString(ISerdeInfo typeInfo, int index, string value) => array.Add(value);

    public void WriteNull(ISerdeInfo typeInfo, int index) =>
        throw new NotSupportedException("TOML arrays cannot contain null values.");

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime value) =>
        array.Add(TomlValues.ToDateTime(value));

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset value) =>
        array.Add(TomlValues.ToDateTimeOffset(value));

    public void WriteDateOnly(ISerdeInfo typeInfo, int index, DateOnly value) =>
        array.Add(TomlValues.ToDateOnly(value));

    public void WriteTimeOnly(ISerdeInfo typeInfo, int index, TimeOnly value) =>
        array.Add(TomlValues.ToTimeOnly(value));

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> value) =>
        array.Add(Convert.ToBase64String(value.Span));

    public void WriteEnum(
        ISerdeInfo typeInfo,
        int index,
        ISerdeInfo fieldInfo,
        int ordinal
    ) => array.Add(TomlValues.GetEnumName(fieldInfo, ordinal));

    public void WriteValue<T>(
        ISerdeInfo typeInfo,
        int index,
        T value,
        ISerialize<T> serialize
    )
        where T : class? => serialize.Serialize(value, new TomlValueSerializer(array.Add));
}

internal sealed class DictionarySerializer(TomlTable table) : ITypeSerializer
{
    private string? _currentKey;

    public ISerializer WriteFieldStart(ISerdeInfo typeInfo, int index) => GetSerializer(index);

    public void WriteFieldEnd(ISerdeInfo typeInfo, int index, ISerializer serializer) { }

    public void End(ISerdeInfo info)
    {
        if (_currentKey is not null)
        {
            throw new InvalidOperationException("Dictionary serialization ended without a value.");
        }
    }

    public void SkipValue(ISerdeInfo typeInfo, int index)
    {
        throw new NotSupportedException("TOML dictionaries cannot omit keys or values.");
    }

    public void WriteBool(ISerdeInfo typeInfo, int index, bool value) =>
        GetSerializer(index).WriteBool(value);

    public void WriteChar(ISerdeInfo typeInfo, int index, char value) =>
        GetSerializer(index).WriteChar(value);

    public void WriteU8(ISerdeInfo typeInfo, int index, byte value) =>
        GetSerializer(index).WriteU8(value);

    public void WriteU16(ISerdeInfo typeInfo, int index, ushort value) =>
        GetSerializer(index).WriteU16(value);

    public void WriteU32(ISerdeInfo typeInfo, int index, uint value) =>
        GetSerializer(index).WriteU32(value);

    public void WriteU64(ISerdeInfo typeInfo, int index, ulong value) =>
        GetSerializer(index).WriteU64(value);

    public void WriteU128(ISerdeInfo typeInfo, int index, UInt128 value) =>
        GetSerializer(index).WriteU128(value);

    public void WriteI8(ISerdeInfo typeInfo, int index, sbyte value) =>
        GetSerializer(index).WriteI8(value);

    public void WriteI16(ISerdeInfo typeInfo, int index, short value) =>
        GetSerializer(index).WriteI16(value);

    public void WriteI32(ISerdeInfo typeInfo, int index, int value) =>
        GetSerializer(index).WriteI32(value);

    public void WriteI64(ISerdeInfo typeInfo, int index, long value) =>
        GetSerializer(index).WriteI64(value);

    public void WriteI128(ISerdeInfo typeInfo, int index, Int128 value) =>
        GetSerializer(index).WriteI128(value);

    public void WriteF32(ISerdeInfo typeInfo, int index, float value) =>
        GetSerializer(index).WriteF32(value);

    public void WriteF64(ISerdeInfo typeInfo, int index, double value) =>
        GetSerializer(index).WriteF64(value);

    public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal value) =>
        GetSerializer(index).WriteDecimal(value);

    public void WriteString(ISerdeInfo typeInfo, int index, string value) =>
        GetSerializer(index).WriteString(value);

    public void WriteNull(ISerdeInfo typeInfo, int index) => GetSerializer(index).WriteNull();

    public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime value) =>
        GetSerializer(index).WriteDateTime(value);

    public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset value) =>
        GetSerializer(index).WriteDateTimeOffset(value);

    public void WriteDateOnly(ISerdeInfo typeInfo, int index, DateOnly value) =>
        GetSerializer(index).WriteDateOnly(value);

    public void WriteTimeOnly(ISerdeInfo typeInfo, int index, TimeOnly value) =>
        GetSerializer(index).WriteTimeOnly(value);

    public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> value) =>
        GetSerializer(index).WriteBytes(value);

    public void WriteEnum(
        ISerdeInfo typeInfo,
        int index,
        ISerdeInfo fieldInfo,
        int ordinal
    ) => GetSerializer(index).WriteEnum(fieldInfo, ordinal);

    public void WriteValue<T>(
        ISerdeInfo typeInfo,
        int index,
        T value,
        ISerialize<T> serialize
    )
        where T : class? => serialize.Serialize(value, GetSerializer(index));

    private ISerializer GetSerializer(int index) =>
        index % 2 == 0
            ? new DictionaryKeySerializer(SetKey)
            : new TomlValueSerializer(SetValue);

    private void SetKey(string key)
    {
        if (_currentKey is not null)
        {
            throw new InvalidOperationException("Dictionary key was written without a value.");
        }

        _currentKey = key;
    }

    private void SetValue(object value)
    {
        var key = TakeKey();
        table[key] = value;
    }

    private string TakeKey()
    {
        var key = _currentKey
            ?? throw new InvalidOperationException("Dictionary value was written before its key.");
        _currentKey = null;
        return key;
    }
}

internal sealed class DictionaryKeySerializer(Action<string> writeKey) : ISerializer
{
    private sealed class KeyNotStringException()
        : NotSupportedException("TOML dictionary keys must serialize as strings.");

    public void WriteString(string value) => writeKey(value);

    public void WriteBool(bool value) => throw new KeyNotStringException();
    public void WriteChar(char value) => throw new KeyNotStringException();
    public void WriteU8(byte value) => throw new KeyNotStringException();
    public void WriteU16(ushort value) => throw new KeyNotStringException();
    public void WriteU32(uint value) => throw new KeyNotStringException();
    public void WriteU64(ulong value) => throw new KeyNotStringException();
    public void WriteU128(UInt128 value) => throw new KeyNotStringException();
    public void WriteI8(sbyte value) => throw new KeyNotStringException();
    public void WriteI16(short value) => throw new KeyNotStringException();
    public void WriteI32(int value) => throw new KeyNotStringException();
    public void WriteI64(long value) => throw new KeyNotStringException();
    public void WriteI128(Int128 value) => throw new KeyNotStringException();
    public void WriteF32(float value) => throw new KeyNotStringException();
    public void WriteF64(double value) => throw new KeyNotStringException();
    public void WriteDecimal(decimal value) => throw new KeyNotStringException();
    public void WriteNull() => throw new KeyNotStringException();
    public void WriteDateTime(DateTime value) => throw new KeyNotStringException();
    public void WriteDateTimeOffset(DateTimeOffset value) => throw new KeyNotStringException();
    public void WriteDateOnly(DateOnly value) => throw new KeyNotStringException();
    public void WriteTimeOnly(TimeOnly value) => throw new KeyNotStringException();
    public void WriteBytes(ReadOnlyMemory<byte> value) => throw new KeyNotStringException();
    public void WriteEnum(ISerdeInfo info, int ordinal) => throw new KeyNotStringException();

    public ITypeSerializer WriteCollection(ISerdeInfo info, int? size) =>
        throw new KeyNotStringException();

    public ITypeSerializer WriteType(ISerdeInfo info) => throw new KeyNotStringException();
}
