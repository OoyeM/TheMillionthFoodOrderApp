using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// Value object representing a Belgian postal address.
/// </summary>
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string Number { get; }
    public string City { get; }
    public string PostalCode { get; }

    /// <summary>ISO 3166-1 alpha-2 country code. Defaults to "BE" (Belgium).</summary>
    public string Country { get; }

    public Address(string street, string number, string city, string postalCode, string country = "BE")
    {
        Street = street;
        Number = number;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    /// <summary>
    /// Renders the address as a single human-readable line for receipts and tickets,
    /// e.g. "Frietstraat 1, 1000 Brussel". The country is appended only when it is not
    /// the Belgian default ("BE").
    /// </summary>
    public string ToSingleLine()
    {
        var line = $"{Street} {Number}, {PostalCode} {City}";
        return Country == "BE" ? line : $"{line}, {Country}";
    }
}
