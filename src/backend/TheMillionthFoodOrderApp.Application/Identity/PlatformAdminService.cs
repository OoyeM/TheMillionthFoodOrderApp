using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Application.Identity;

public sealed class PlatformAdminService(IPlatformUserRepository userRepository) : IPlatformAdminService
{
    public async Task<IReadOnlyList<PlatformAdminResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var admins = await userRepository.GetAllPlatformAdminsAsync(cancellationToken);
        return admins.Select(ToResponse).ToList().AsReadOnly();
    }

    public async Task<PlatformAdminResponse> InviteAsync(
        InvitePlatformAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsPlatformAdmin)
                throw new InvalidOperationException(
                    $"User with email '{request.Email}' is already a platform admin.");

            existing.PromoteToPlatformAdmin();
            await userRepository.SaveChangesAsync(cancellationToken);
            return ToResponse(existing);
        }

        // Create a pending user — linked to real identity on first OIDC login
        var newUser = PlatformUser.Create(
            externalIdentityId: $"pending:{request.Email}",
            email: request.Email,
            displayName: request.DisplayName,
            isPlatformAdmin: true);

        await userRepository.AddAsync(newUser, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(newUser);
    }

    public async Task DeactivateAsync(Guid platformUserId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"PlatformUser with id '{platformUserId}' was not found.");

        var adminCount = await userRepository.CountPlatformAdminsAsync(cancellationToken);
        if (adminCount <= 1)
            throw new InvalidOperationException("Cannot deactivate the last platform admin.");

        user.RevokePlatformAdmin();
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PlatformAdminResponse ToResponse(PlatformUser user) =>
        new(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, user.CreatedAt, user.UpdatedAt);
}
