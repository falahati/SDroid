using SDroid.SteamKit;
using SteamKit2;

namespace SDroid.Interfaces
{
    public interface ISteamKitBot : ISteamBot
    {
        new ISteamKitBotSettings BotSettings { get; }
        CallbackManager CallbackManager { get; }
        SteamClient SteamClient { get; }
        SteamFriends SteamFriends { get; }
        SteamUser SteamUser { get; }
    }
}