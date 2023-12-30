namespace SDroid.Interfaces
{
    public interface ISteamKitBotSettings : IBotSettings
    {
        int LoginTimeout { get; }
        string GuardData { set; get; }
        int? ConnectionTimeout { get; set; }
    }
}