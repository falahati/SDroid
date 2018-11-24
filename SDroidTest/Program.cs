using System.Threading;
using ConsoleUtilities;

namespace SDroidTest
{
    internal class Program
    {
        private static void Main()
        {
            var bot = new TradeOfferBot(new TradeOfferBotSettings
            {
                Username = ConsoleWriter.Default.PrintQuestion("Username"),
                Password = ConsoleWriter.Default.PrintQuestion("Password")
            }, new TradeOfferLogger());
            bot.StartBot().Wait();

            while (true)
            {
                Thread.Sleep(300);
            }
        }
    }
}