using BenchmarkDotNet.Attributes;
using Serde.Toml;

namespace Benchmarks;

[MemoryDiagnoser]
public class DeserializeFromString
{
    private readonly string _toml = TomlSerializer.Serialize(Location.Sample);

    [Benchmark]
    public Location Deserialize() => TomlDeserializer.Deserialize<Location>(_toml);
}
