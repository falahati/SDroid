namespace SDroid.Interfaces
{
    public interface ISteamKitBotSettings : IBotSettings
    {
        string LoginKey { get; set; }
        int LoginTimeout { get; }
        byte[] SentryFile { get; set; }
        byte[] SentryFileHash { get; set; }
        string SentryFileName { get; set; }
    }
}