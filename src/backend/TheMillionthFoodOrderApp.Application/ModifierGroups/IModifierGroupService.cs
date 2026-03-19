namespace TheMillionthFoodOrderApp.Application.ModifierGroups;

public interface IModifierGroupService
{
    Task<ModifierGroupResponse> CreateModifierGroupAsync(CreateModifierGroupRequest request, CancellationToken cancellationToken = default);
    Task<ModifierGroupResponse> UpdateModifierGroupAsync(Guid id, UpdateModifierGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteModifierGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ModifierGroupResponse> GetModifierGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModifierGroupListItemResponse>> GetModifierGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductModifierGroupResponse>> GetProductModifierGroupsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task SetProductModifierGroupsAsync(Guid productId, SetProductModifierGroupsRequest request, CancellationToken cancellationToken = default);
}
