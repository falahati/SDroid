using SDroid.SteamWeb;

namespace SDroid.Interfaces
{
    public interface IBotSettings : IBotSettingsBase
    {
        string ApiKey { get; set; }
        string DomainName { get; }
        string Proxy { get; }
        string PublicIPAddress { get; set; }
        WebSession Session { get; set; }
        int SessionCheckInterval { get; }
        string Username { get; }
    }
}