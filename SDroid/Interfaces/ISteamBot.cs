using System;
using Microsoft.Extensions.Logging;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.Interfaces
{
    public interface ISteamBot : IDisposable
    {
        ILogger BotLogger { get; }
        IBotSettings BotSettings { get; }
        SteamBotStatus BotStatus { get; }
        SteamWebAccess WebAccess { get; }
        SteamWebAPI WebAPI { get; }
        SteamID SteamId { get; }
    }
}