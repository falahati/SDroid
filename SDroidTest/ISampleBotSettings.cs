using SDroid.Interfaces;

namespace SDroidTest
{
    interface ISampleBotSettings: IBotSettings
    {
        /// <inheritdoc cref="IBotSettings" />
        string Username { get; set; }
    }
}
