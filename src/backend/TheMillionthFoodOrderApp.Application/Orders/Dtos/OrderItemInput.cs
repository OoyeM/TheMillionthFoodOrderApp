namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

/// <summary>
/// Represents a single product line in a create-order request.
/// </summary>
public sealed record OrderItemInput(
    Guid ProductId,
    int Quantity,
    IReadOnlyList<Guid> SelectedModifierIds);
