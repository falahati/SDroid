using System.Threading;
using ConsoleUtilities;

namespace SDroidTest
{
    internal class Program
    {
        private static void Main()
        {
            ConsoleNavigation.Default.PrintNavigation(new[]
            {
                new ConsoleNavigationItem("TradeOfferBot", (i, item) => TradeOfferBot()),
                new ConsoleNavigationItem("SteamKitBot", (i, item) => SteamKitBot())
            });
        }

        private static void SteamKitBot()
        {
            var bot = new SteamKitBot(SteamKitBotSettings.LoadSaved(), new Logger());
            bot.StartBot().Wait();

            while (true)
            {
                Thread.Sleep(300);
            }
        }

        private static void TradeOfferBot()
        {
            var bot = new TradeOfferBot(TradeOfferBotSettings.LoadSaved(), new Logger());
            bot.StartBot().Wait();

            while (true)
            {
                Thread.Sleep(300);
            }
        }
    }
}