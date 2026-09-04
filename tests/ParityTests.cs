using Serde;

namespace Serde.Toml.Tests;

public partial class ParityTests
{
    [GenerateSerde]
    private partial record PrimitiveRecord(
        bool Bool,
        char Char,
        byte U8,
        ushort U16,
        uint U32,
        ulong U64,
        UInt128 U128,
        sbyte I8,
        short I16,
        int I32,
        long I64,
        Int128 I128,
        Half F16,
        float F32,
        double F64,
        decimal Decimal,
        string String
    );

    [GenerateSerde]
    private partial record Child(string Name, int Value);

    [GenerateSerde]
    private partial record CollectionsRecord(
        int[] Numbers,
        List<string> Names,
        Dictionary<string, int[]> Scores,
        Child[] Children,
        (int, string) Pair
    );

    [GenerateSerde]
    private partial record NullableRecord(string Name, string? Note, Child? Child);

    [GenerateSerde]
    private partial record BytesRecord(byte[] Data);

    [GenerateSerde]
    private partial record TemporalRecord(
        DateTime Local,
        DateTime Utc,
        DateTimeOffset Offset,
        DateOnly Date,
        TimeOnly Time
    );

    [GenerateSerde]
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    [GenerateSerde]
    private partial record EnumRecord(Color Color);

    [GenerateSerde]
    private abstract partial record Shape
    {
        private Shape() { }

        public sealed record Circle(double Radius) : Shape;

        public sealed record Rectangle(double Width, double Height) : Shape;

        public sealed record Empty : Shape;
    }

    [GenerateSerde]
    private partial record UnionRecord(string Name, Shape Shape);

    [GenerateSerde]
    private partial record UnsignedRecord(ulong Value);

    [GenerateSerde]
    private partial record ByteRecord(byte Value);

    [GenerateSerde]
    private partial record Int128Record(Int128 Value);

    [GenerateSerde]
    private partial record DecimalRecord(decimal Value);

    [GenerateSerde]
    private partial record NullableArrayRecord(string?[] Values);

    [Fact]
    public void PrimitiveMembersRoundTrip()
    {
        var expected = new PrimitiveRecord(
            true,
            'x',
            byte.MaxValue,
            ushort.MaxValue,
            uint.MaxValue,
            long.MaxValue,
            (UInt128)long.MaxValue,
            sbyte.MinValue,
            short.MinValue,
            int.MinValue,
            long.MinValue,
            long.MinValue,
            (Half)1.5,
            2.5f,
            3.5,
            12.5m,
            "hello"
        );

        Assert.Equal(expected, RoundTrip(expected));
    }

    [Fact]
    public void NestedCollectionsRoundTrip()
    {
        var expected = new CollectionsRecord(
            [1, 2, 3],
            ["one", "two"],
            new Dictionary<string, int[]>
            {
                ["first"] = [1, 2],
                ["second"] = [3, 4],
            },
            [new Child("a", 1), new Child("b", 2)],
            (42, "answer")
        );

        var actual = RoundTrip(expected);

        Assert.Equal(expected.Numbers, actual.Numbers);
        Assert.Equal(expected.Names, actual.Names);
        Assert.Equal(expected.Scores.Keys, actual.Scores.Keys);
        Assert.Equal(expected.Scores["first"], actual.Scores["first"]);
        Assert.Equal(expected.Scores["second"], actual.Scores["second"]);
        Assert.Equal(expected.Children, actual.Children);
        Assert.Equal(expected.Pair, actual.Pair);
    }

    [Fact]
    public void NullableMembersAreOmittedAndRoundTrip()
    {
        var expected = new NullableRecord("sample", null, null);

        var toml = TomlSerializer.Serialize(expected);
        var model =
            global::Tomlyn.TomlSerializer.Deserialize<global::Tomlyn.Model.TomlTable>(toml)!;

        Assert.False(model.ContainsKey("note"));
        Assert.False(model.ContainsKey("child"));
        Assert.Equal(expected, TomlDeserializer.Deserialize<NullableRecord>(toml));
    }

    [Fact]
    public void PresentNullableMembersRoundTrip()
    {
        var expected = new NullableRecord("sample", "note", new Child("nested", 5));

        Assert.Equal(expected, RoundTrip(expected));
    }

    [Fact]
    public void ByteArraysRoundTripAsBase64Strings()
    {
        var expected = new byte[] { 0, 1, 2, 127, 255 };

        var toml = TomlSerializer.Serialize(new BytesRecord(expected));
        var model =
            global::Tomlyn.TomlSerializer.Deserialize<global::Tomlyn.Model.TomlTable>(toml)!;
        var actual = TomlDeserializer.Deserialize<BytesRecord>(toml);

        Assert.Equal(Convert.ToBase64String(expected), model["data"]);
        Assert.Equal(expected, actual.Data);
    }

    [Fact]
    public void TemporalValuesRoundTrip()
    {
        var expected = new TemporalRecord(
            new DateTime(2026, 9, 3, 8, 15, 30, DateTimeKind.Unspecified).AddTicks(1),
            new DateTime(2026, 9, 3, 15, 15, 30, DateTimeKind.Utc).AddTicks(1),
            new DateTimeOffset(2026, 9, 3, 8, 15, 30, TimeSpan.FromMinutes(345)).AddTicks(1),
            new DateOnly(2026, 9, 3),
            new TimeOnly(8, 15, 30).Add(TimeSpan.FromTicks(1))
        );

        var toml = TomlSerializer.Serialize(expected);
        var actual = TomlDeserializer.Deserialize<TemporalRecord>(toml);

        Assert.Equal(expected, actual);
        Assert.Equal(DateTimeKind.Unspecified, actual.Local.Kind);
        Assert.Equal(DateTimeKind.Utc, actual.Utc.Kind);
        Assert.Equal(expected.Offset.Offset, actual.Offset.Offset);
        Assert.Contains("+05:45", toml);
        Assert.Contains(".0000001", toml);
    }

    [Fact]
    public void EnumsRoundTripByName()
    {
        var expected = new EnumRecord(Color.Green);

        Assert.Equal(expected, RoundTrip(expected));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnionCasesRoundTrip(int caseIndex)
    {
        Shape shape = caseIndex switch
        {
            0 => new Shape.Circle(2.5),
            1 => new Shape.Rectangle(3, 4),
            2 => new Shape.Empty(),
            _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
        };
        var expected = new UnionRecord("shape", shape);

        Assert.Equal(expected, RoundTrip(expected));
    }

    [Fact]
    public void RootDictionaryRoundTripsAsTopLevelTable()
    {
        var expected = new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2,
        };
        var proxy = DictProxy.Ser<string, int, StringProxy, I32Proxy>.Instance;
        var deserialize = DictProxy.De<string, int, StringProxy, I32Proxy>.Instance;

        var toml = TomlSerializer.Serialize(expected, proxy);
        var actual = TomlDeserializer.Deserialize(toml, deserialize);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnknownMembersAreIgnored()
    {
        var toml = """
            unknown = "ignored"
            name = "known"
            value = 42
            """;

        Assert.Equal(new Child("known", 42), TomlDeserializer.Deserialize<Child>(toml));
    }

    [Fact]
    public void RootPrimitiveIsRejected()
    {
        Assert.Throws<NotSupportedException>(() =>
            TomlSerializer.Serialize(42, I32Proxy.Instance)
        );
    }

    [Fact]
    public void RootArrayIsRejected()
    {
        Assert.Throws<NotSupportedException>(() =>
            TomlSerializer.Serialize(
                new[] { 1, 2, 3 },
                ArrayProxy.Ser<int, I32Proxy>.Instance
            )
        );
    }

    [Fact]
    public void UnsignedValuesOutsideTomlRangeAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TomlSerializer.Serialize(new UnsignedRecord(ulong.MaxValue))
        );
    }

    [Fact]
    public void Int128ValuesOutsideTomlRangeAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TomlSerializer.Serialize(new Int128Record((Int128)long.MaxValue + 1))
        );
    }

    [Fact]
    public void InexactDecimalValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TomlSerializer.Serialize(new DecimalRecord(decimal.MaxValue))
        );
    }

    [Fact]
    public void NullableArrayElementsAreRejected()
    {
        Assert.Throws<NotSupportedException>(() =>
            TomlSerializer.Serialize(new NullableArrayRecord(["value", null]))
        );
    }

    [Fact]
    public void DictionaryKeysMustBeStrings()
    {
        var value = new Dictionary<int, string> { [1] = "one" };
        var proxy = DictProxy.Ser<int, string, I32Proxy, StringProxy>.Instance;

        Assert.ThrowsAny<NotSupportedException>(() => TomlSerializer.Serialize(value, proxy));
    }

    [Fact]
    public void NegativeIntegerCannotDeserializeAsUnsigned()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<UnsignedRecord>("value = -1")
        );
    }

    [Fact]
    public void NarrowingIntegerOverflowIsReportedAsDeserializeException()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<ByteRecord>("value = 256")
        );
    }

    [Fact]
    public void DecimalOverflowIsReportedAsDeserializeException()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<DecimalRecord>("value = 1e300")
        );
    }

    [Fact]
    public void MalformedBase64IsReportedAsDeserializeException()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<BytesRecord>("data = \"not base64\"")
        );
    }

    [Fact]
    public void MalformedTomlIsReportedAsDeserializeException()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<Child>("name = [")
        );
    }

    [Fact]
    public void TypeMismatchIsReported()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<Child>(
                """
                name = "wrong"
                value = "not an integer"
                """
            )
        );
    }

    [Fact]
    public void MissingRequiredMemberIsReported()
    {
        Assert.Throws<DeserializeException>(() =>
            TomlDeserializer.Deserialize<Child>("name = \"missing\"")
        );
    }

    private static T RoundTrip<T>(T value)
        where T : ISerializeProvider<T>, IDeserializeProvider<T>
    {
        var toml = TomlSerializer.Serialize(value);
        return TomlDeserializer.Deserialize<T>(toml);
    }
}
