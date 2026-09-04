using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Serde.Toml;

internal sealed class TomlynModelContext : TomlSerializerContext
{
    public static TomlynModelContext Instance { get; } = new();

    public TomlTypeInfo<TomlTable> TableInfo { get; }

    private TomlynModelContext()
    {
        TableInfo = GetBuiltInTypeInfo<TomlTable>(Options);
    }

    public override TomlTypeInfo? GetTypeInfo(Type type, TomlSerializerOptions options) =>
        type == typeof(TomlTable) ? TableInfo : null;
}
