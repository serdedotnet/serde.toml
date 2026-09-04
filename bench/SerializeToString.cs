using BenchmarkDotNet.Attributes;
using Serde.Toml;

namespace Benchmarks;

[MemoryDiagnoser]
public class SerializeToString
{
    [Benchmark]
    public string Serialize() => TomlSerializer.Serialize(Location.Sample);
}
