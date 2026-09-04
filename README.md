# Serde.Toml

A [TOML](https://toml.io/) serializer and deserializer for .NET, built on the
[Serde](https://github.com/serdedotnet/serde) serialization framework and
[Tomlyn](https://github.com/xoofx/Tomlyn).

Serde.Toml is an AOT-compatible serializer/deserializer library for .NET 10+
with a declarative, source-generated model.

## Installation

```sh
dotnet add package Serde.Toml
```

## Usage

Use the `[GenerateSerde]` attribute from the Serde framework to generate
serialization code for your types:

```csharp
using Serde;
using Serde.Toml;

[GenerateSerde]
public partial record ServerOptions
{
    public required string Host { get; init; }
    public int Port { get; init; }
    public bool UseTls { get; init; }
}

var options = new ServerOptions
{
    Host = "localhost",
    Port = 8080,
    UseTls = true,
};

string toml = TomlSerializer.Serialize(options);
ServerOptions deserialized = TomlDeserializer.Deserialize<ServerOptions>(toml);
```

The serialized document is:

```toml
host = "localhost"
port = 8080
useTls = true
```

## Format behavior

- TOML documents must have a table at the root. Records, unions, and
  string-keyed dictionaries can be serialized as complete documents.
- Null record members are omitted. TOML cannot represent null array elements
  or dictionary values.
- TOML dictionary keys must serialize as strings.
- TOML integers are signed 64-bit values. Larger unsigned and 128-bit values
  are rejected rather than truncated.
- Decimal values must be exactly representable by a TOML floating-point value.
- Byte arrays are encoded as Base64 strings.

## Building and testing

```sh
dotnet build
dotnet test
```

## Benchmarking

```sh
dotnet run --project bench -c Release
```

## Related projects

- [Serde](https://github.com/serdedotnet/serde) - the underlying serialization framework
- [Serde.Cbor](https://github.com/serdedotnet/serde.cbor) - the CBOR backend for Serde

## License

BSD-3-Clause. See [LICENSE](LICENSE) for details.
