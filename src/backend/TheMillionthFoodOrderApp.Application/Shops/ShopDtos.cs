namespace TheMillionthFoodOrderApp.Application.Shops;

public sealed record CreateShopRequest(
    string Name,
    string Slug,
    AddressRequest Address,
    string ContactEmail,
    string? ContactPhone);

public sealed record UpdateShopRequest(
    string Name,
    AddressRequest Address,
    string ContactEmail,
    string? ContactPhone);

public sealed record AddressRequest(
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country = "BE");

public sealed record AddressResponse(
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country);

public sealed record ShopResponse(
    Guid Id,
    string Name,
    string Slug,
    AddressResponse Address,
    string ContactEmail,
    string? ContactPhone,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Lightweight shop summary returned by the public storefront endpoint.
/// Only active shops are included; <see cref="IsOpen"/> reflects real-time status.
/// </summary>
public sealed record StorefrontShopResponse(
    Guid Id,
    string Name,
    string Slug,
    AddressResponse Address,
    bool IsOpen);
