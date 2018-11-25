using System;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.Interfaces
{
    public interface ISteamBot : IDisposable
    {
        IBotLogger BotLogger { get; }
        IBotSettings BotSettings { get; }
        SteamBotStatus BotStatus { get; }
        SteamWebAccess WebAccess { get; }
        SteamWebAPI WebAPI { get; }
        SteamID SteamId { get; }
    }
}