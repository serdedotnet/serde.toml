using Xunit;
using Serde;
using Serde.Toml;

namespace Serde.Toml.Tests;

public partial class TomlTests
{
    [GenerateSerde]
    private partial record SimpleRecord(string Name, int Age);

    [GenerateSerde]
    private partial record ComplexRecord(
        string Name,
        int Age,
        double Score,
        bool Active);

    [GenerateSerde]
    private partial record RecordWithArray(
        string Name,
        int[] Numbers);

    [GenerateSerde]
    private partial record NestedRecord(
        string Name,
        SimpleRecord Inner);

    [GenerateSerde]
    private partial record DictionaryRecord(
        string Name,
        Dictionary<string, int> Scores);

    [Fact]
    public void TestSerializeSimpleRecord()
    {
        var record = new SimpleRecord("John", 30);
        var toml = TomlSerializer.Serialize(record);
        
        Assert.NotNull(toml);
        Assert.Contains("name = \"John\"", toml);
        Assert.Contains("age = 30", toml);
    }

    [Fact]
    public void TestDeserializeSimpleRecord()
    {
        var toml = """
            name = "John"
            age = 30
            """;
        
        var record = TomlDeserializer.Deserialize<SimpleRecord>(toml);
        
        Assert.Equal("John", record.Name);
        Assert.Equal(30, record.Age);
    }

    [Fact]
    public void TestSerializeComplexRecord()
    {
        var record = new ComplexRecord("Alice", 25, 95.5, true);
        var toml = TomlSerializer.Serialize(record);
        
        Assert.NotNull(toml);
        Assert.Contains("name = \"Alice\"", toml);
        Assert.Contains("age = 25", toml);
        Assert.Contains("score = 95.5", toml);
        Assert.Contains("active = true", toml);
    }

    [Fact]
    public void TestDeserializeComplexRecord()
    {
        var toml = """
            name = "Alice"
            age = 25
            score = 95.5
            active = true
            """;
        
        var record = TomlDeserializer.Deserialize<ComplexRecord>(toml);
        
        Assert.Equal("Alice", record.Name);
        Assert.Equal(25, record.Age);
        Assert.Equal(95.5, record.Score);
        Assert.True(record.Active);
    }

    [Fact]
    public void TestSerializeRecordWithArray()
    {
        var record = new RecordWithArray("Test", new[] { 1, 2, 3, 4, 5 });
        var toml = TomlSerializer.Serialize(record);
        
        Assert.NotNull(toml);
        Assert.Contains("name = \"Test\"", toml);
        Assert.Contains("numbers = [1, 2, 3, 4, 5]", toml);
    }

    [Fact]
    public void TestDeserializeRecordWithArray()
    {
        var toml = """
            name = "Test"
            numbers = [1, 2, 3, 4, 5]
            """;
        
        var record = TomlDeserializer.Deserialize<RecordWithArray>(toml);
        
        Assert.Equal("Test", record.Name);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, record.Numbers);
    }

    [Fact]
    public void TestSerializeNestedRecord()
    {
        var record = new NestedRecord("Outer", new SimpleRecord("Inner", 42));
        var toml = TomlSerializer.Serialize(record);
        
        Assert.NotNull(toml);
        Assert.Contains("name = \"Outer\"", toml);
        // Nested record should be a table
        Assert.Contains("[inner]", toml);
    }

    [Fact]
    public void TestDeserializeNestedRecord()
    {
        var toml = """
            name = "Outer"
            
            [inner]
            name = "Inner"
            age = 42
            """;
        
        var record = TomlDeserializer.Deserialize<NestedRecord>(toml);
        
        Assert.Equal("Outer", record.Name);
        Assert.NotNull(record.Inner);
        Assert.Equal("Inner", record.Inner.Name);
        Assert.Equal(42, record.Inner.Age);
    }

    [Fact]
    public void TestRoundTripSerialization()
    {
        var original = new ComplexRecord("RoundTrip", 99, 88.8, false);
        var toml = TomlSerializer.Serialize(original);
        var deserialized = TomlDeserializer.Deserialize<ComplexRecord>(toml);
        
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Age, deserialized.Age);
        Assert.Equal(original.Score, deserialized.Score);
        Assert.Equal(original.Active, deserialized.Active);
    }

    [Fact]
    public void TestSerializeDictionary()
    {
        var scores = new Dictionary<string, int>
        {
            { "level1", 100 },
            { "level2", 200 },
            { "level3", 300 }
        };
        var record = new DictionaryRecord("Player", scores);
        var toml = TomlSerializer.Serialize(record);
        
        Assert.NotNull(toml);
        Assert.Contains("name = \"Player\"", toml);
        Assert.Contains("[scores]", toml);
        Assert.Contains("level1 = 100", toml);
        Assert.Contains("level2 = 200", toml);
        Assert.Contains("level3 = 300", toml);
    }

    [Fact]
    public void TestDeserializeDictionary()
    {
        var toml = """
            name = "Player"
            
            [scores]
            level1 = 100
            level2 = 200
            level3 = 300
            """;
        
        var record = TomlDeserializer.Deserialize<DictionaryRecord>(toml);
        
        Assert.Equal("Player", record.Name);
        Assert.Equal(3, record.Scores.Count);
        Assert.Equal(100, record.Scores["level1"]);
        Assert.Equal(200, record.Scores["level2"]);
        Assert.Equal(300, record.Scores["level3"]);
    }
}
