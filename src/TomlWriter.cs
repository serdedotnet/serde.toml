using System.Globalization;
using System.Text;

namespace Serde.Toml;

internal sealed class TomlTableValue : Dictionary<string, object>;

internal sealed class TomlArrayValue : List<object>;

internal enum TomlValueDateTimeKind
{
    OffsetDateTimeByZ,
    OffsetDateTimeByNumber,
    LocalDateTime,
    LocalDate,
    LocalTime,
}

internal readonly record struct TomlDateTimeValue(object Value, int Precision, TomlValueDateTimeKind Kind);

internal static class TomlWriter
{
    public static string Write(TomlTableValue table)
    {
        var builder = new StringBuilder();
        WriteTable(builder, table, "");
        return builder.ToString();
    }

    private static void WriteTable(StringBuilder builder, TomlTableValue table, string path)
    {
        foreach (var (key, value) in table)
        {
            if (value is TomlTableValue || IsTableArray(value))
            {
                continue;
            }

            WriteKey(builder, key);
            builder.Append(" = ");
            WriteValue(builder, value);
            builder.Append('\n');
        }

        foreach (var (key, value) in table)
        {
            var name = FormatKey(key);
            var childPath = path.Length == 0 ? name : path + "." + name;
            if (value is TomlTableValue child)
            {
                WriteHeader(builder, childPath, false);
                WriteTable(builder, child, childPath);
            }
            else if (value is TomlArrayValue array && IsTableArray(array))
            {
                foreach (var element in array)
                {
                    WriteHeader(builder, childPath, true);
                    WriteTable(builder, (TomlTableValue)element, childPath);
                }
            }
        }
    }

    private static void WriteHeader(StringBuilder builder, string path, bool array)
    {
        if (builder.Length != 0)
        {
            builder.Append('\n');
        }

        builder.Append(array ? "[[" : "[");
        builder.Append(path);
        builder.Append(array ? "]]\n" : "]\n");
    }

    private static bool IsTableArray(object value) =>
        value is TomlArrayValue { Count: > 0 } array && array.All(item => item is TomlTableValue);

    private static string FormatKey(string key)
    {
        if (key.Length > 0 && key.All(c => IsBareKeyChar(c)))
        {
            return key;
        }

        var builder = new StringBuilder();
        WriteString(builder, key);
        return builder.ToString();
    }

    private static void WriteKey(StringBuilder builder, string key) =>
        builder.Append(FormatKey(key));

    private static bool IsBareKeyChar(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-';

    private static void WriteValue(StringBuilder builder, object value)
    {
        switch (value)
        {
            case string text:
                WriteString(builder, text);
                break;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                break;
            case long integer:
                builder.Append(integer.ToString(CultureInfo.InvariantCulture));
                break;
            case double number:
                WriteFloat(builder, number);
                break;
            case TomlDateTimeValue dateTime:
                WriteDateTime(builder, dateTime);
                break;
            case TomlArrayValue array:
                builder.Append('[');
                for (var i = 0; i < array.Count; i++)
                {
                    if (i != 0)
                    {
                        builder.Append(", ");
                    }
                    WriteValue(builder, array[i]);
                }
                builder.Append(']');
                break;
            case TomlTableValue table:
                builder.Append('{');
                var first = true;
                foreach (var (key, item) in table)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }
                    first = false;
                    WriteKey(builder, key);
                    builder.Append(" = ");
                    WriteValue(builder, item);
                }
                builder.Append('}');
                break;
            default:
                throw new NotSupportedException($"Unsupported TOML value: {value.GetType().Name}.");
        }
    }

    private static void WriteFloat(StringBuilder builder, double value)
    {
        if (double.IsNaN(value))
        {
            builder.Append("nan");
        }
        else if (double.IsPositiveInfinity(value))
        {
            builder.Append("inf");
        }
        else if (double.IsNegativeInfinity(value))
        {
            builder.Append("-inf");
        }
        else
        {
            var text = value.ToString("R", CultureInfo.InvariantCulture);
            builder.Append(text);
            if (!text.Contains('.') && !text.Contains('E'))
            {
                builder.Append(".0");
            }
        }
    }

    private static void WriteDateTime(StringBuilder builder, TomlDateTimeValue value)
    {
        switch (value.Kind)
        {
            case TomlValueDateTimeKind.LocalDate:
                builder.Append(((DateOnly)value.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                return;
            case TomlValueDateTimeKind.LocalTime:
                var time = (TimeOnly)value.Value;
                builder.Append(time.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                WriteFraction(builder, time.Ticks, value.Precision);
                return;
        }

        var date = value.Value is DateTimeOffset offset ? offset.DateTime : (DateTime)value.Value;
        builder.Append(date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        WriteFraction(builder, date.Ticks, value.Precision);
        switch (value.Kind)
        {
            case TomlValueDateTimeKind.OffsetDateTimeByZ:
                builder.Append('Z');
                break;
            case TomlValueDateTimeKind.OffsetDateTimeByNumber:
                var zone = value.Value is DateTimeOffset dto ? dto.Offset : TimeZoneInfo.Local.GetUtcOffset(date);
                builder.Append(zone < TimeSpan.Zero ? '-' : '+');
                builder.Append(zone.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                break;
        }
    }

    private static void WriteFraction(StringBuilder builder, long ticks, int precision)
    {
        if (precision > 0)
        {
            builder.Append('.');
            builder.Append((ticks % TimeSpan.TicksPerSecond).ToString("D7", CultureInfo.InvariantCulture).AsSpan(0, precision));
        }
    }

    private static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\t': builder.Append("\\t"); break;
                case '\n': builder.Append("\\n"); break;
                case '\f': builder.Append("\\f"); break;
                case '\r': builder.Append("\\r"); break;
                default:
                    if (c < ' ' || c == '\x7f')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else if (char.IsHighSurrogate(c))
                    {
                        if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                        {
                            throw new ArgumentException("TOML strings cannot contain unpaired surrogates.");
                        }
                        builder.Append(c);
                        builder.Append(text[++i]);
                    }
                    else if (char.IsLowSurrogate(c))
                    {
                        throw new ArgumentException("TOML strings cannot contain unpaired surrogates.");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }
        builder.Append('"');
    }
}
