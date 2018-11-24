using System.Net;
using SDroid.SteamWeb;

namespace SDroid.Interfaces
{
    public interface IBotSettings : IBotSettingsBase
    {
        string ApiKey { get; set; }
        string DomainName { get; set; }
        string Password { get; set; }
        IWebProxy Proxy { get; set; }
        IPAddress PublicIPAddress { get; set; }
        WebSession Session { get; set; }
        int SessionCheckInterval { get; set; }
        string Username { get; set; }
    }
}