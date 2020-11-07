using SDroid.SteamMobile;

namespace SDroid.Interfaces
{
    public interface IAuthenticatorSettings : IBotSettingsBase
    {
        Authenticator Authenticator { get; set; }
        int ConfirmationCheckInterval { get; }
    }
}