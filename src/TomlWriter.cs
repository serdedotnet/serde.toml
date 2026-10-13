using System.Globalization;
using System.Text;

namespace Serde.Toml;

internal static class TomlWriter
{
    public static string Write(TomlValue.Table table)
    {
        var builder = new StringBuilder();
        WriteTable(builder, table, "");
        return builder.ToString();
    }

    private static void WriteTable(StringBuilder builder, TomlValue.Table table, string path)
    {
        foreach (var (key, value) in table)
        {
            if (value is TomlValue.Table || IsTableArray(value))
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
            if (value is TomlValue.Table child)
            {
                WriteHeader(builder, childPath, false);
                WriteTable(builder, child, childPath);
            }
            else if (value is TomlValue.Array array && IsTableArray(array))
            {
                foreach (var element in array)
                {
                    WriteHeader(builder, childPath, true);
                    WriteTable(builder, (TomlValue.Table)element, childPath);
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

    private static bool IsTableArray(TomlValue value) =>
        value is TomlValue.Array { Count: > 0 } array && array.All(item => item is TomlValue.Table);

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

    private static void WriteValue(StringBuilder builder, TomlValue value)
    {
        switch (value)
        {
            case TomlValue.String text:
                WriteString(builder, text.Value);
                break;
            case TomlValue.Boolean boolean:
                builder.Append(boolean.Value ? "true" : "false");
                break;
            case TomlValue.Integer integer:
                builder.Append(integer.Value.ToString(CultureInfo.InvariantCulture));
                break;
            case TomlValue.Float number:
                WriteFloat(builder, number.Value);
                break;
            case TomlValue.DateTime dateTime:
                WriteDateTime(builder, dateTime.Value);
                break;
            case TomlValue.DateTimeOffset dateTimeOffset:
                WriteDateTimeOffset(builder, dateTimeOffset.Value);
                break;
            case TomlValue.Date date:
                builder.Append(date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            case TomlValue.Time time:
                builder.Append(time.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                WriteFraction(builder, time.Value.Ticks);
                break;
            case TomlValue.Array array:
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
            case TomlValue.Table table:
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

    private static void WriteDateTime(StringBuilder builder, DateTime value)
    {
        builder.Append(value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        WriteFraction(builder, value.Ticks);
        switch (value.Kind)
        {
            case DateTimeKind.Utc:
                builder.Append('Z');
                break;
            case DateTimeKind.Local:
                WriteOffset(builder, TimeZoneInfo.Local.GetUtcOffset(value));
                break;
        }
    }

    private static void WriteDateTimeOffset(StringBuilder builder, DateTimeOffset value)
    {
        builder.Append(value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        WriteFraction(builder, value.Ticks);
        if (value.Offset == TimeSpan.Zero)
        {
            builder.Append('Z');
        }
        else
        {
            WriteOffset(builder, value.Offset);
        }
    }

    private static void WriteOffset(StringBuilder builder, TimeSpan offset)
    {
        builder.Append(offset < TimeSpan.Zero ? '-' : '+');
        builder.Append(offset.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture));
    }

    private static void WriteFraction(StringBuilder builder, long ticks)
    {
        var fractionalTicks = ticks % TimeSpan.TicksPerSecond;
        if (fractionalTicks != 0)
        {
            builder.Append('.');
            builder.Append(fractionalTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0'));
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
