using Serde;

namespace Benchmarks;

[GenerateSerde]
public partial record Location
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string Region { get; init; }
    public required string PostalCode { get; init; }
    public required string Country { get; init; }
    public int LocationId { get; init; }
    public bool Active { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public static Location Sample { get; } =
        new()
        {
            Name = "Serde Headquarters",
            Address = "123 Serialization Way",
            City = "Seattle",
            Region = "WA",
            PostalCode = "98101",
            Country = "US",
            LocationId = 1234,
            Active = true,
            Latitude = 47.6062,
            Longitude = -122.3321,
        };
}
