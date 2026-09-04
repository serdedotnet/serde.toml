using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NativeAot;
using Benchmarks;
using Serde.Toml;

var toml = TomlSerializer.Serialize(Location.Sample);
var roundTripped = TomlDeserializer.Deserialize<Location>(toml);

Console.WriteLine("Checking correctness of serialization: " + (Location.Sample == roundTripped));
if (Location.Sample != roundTripped)
{
    throw new InvalidOperationException(
        $"""
Round trip is not correct
Original:
{Location.Sample}

Deserialized:
{roundTripped}
"""
    );
}

var config = DefaultConfig
    .Instance.AddJob(Job.Default.WithId("NativeAOT").WithToolchain(NativeAotToolchain.Net10_0))
    .AddDiagnoser(MemoryDiagnoser.Default);

BenchmarkSwitcher.FromAssembly(typeof(SerializeToString).Assembly).Run(args, config);
