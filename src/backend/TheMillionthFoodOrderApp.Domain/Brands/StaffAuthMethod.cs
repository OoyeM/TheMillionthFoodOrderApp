namespace TheMillionthFoodOrderApp.Domain.Brands;

/// <summary>
/// Determines which authentication methods are available for staff on this brand's management portal.
/// </summary>
public enum StaffAuthMethod
{
    /// <summary>Username + password (local Entra account). Default for all brands.</summary>
    EmailPassword = 0,

    /// <summary>Google Workspace SSO via Entra External ID social identity provider.</summary>
    GoogleSso = 1,

    /// <summary>Microsoft Entra ID / Work account SSO via Entra External ID federation.</summary>
    MicrosoftSso = 2
}
