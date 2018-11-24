namespace SDroid.Interfaces
{
    public interface ISteamKitBotSettings : IBotSettings
    {
        int LoginTimeout { get; set; }
        byte[] MachineHash { get; set; }
    }
}