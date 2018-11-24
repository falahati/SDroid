using System.Linq;
using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;

namespace SDroidTest
{
    public class TradeOfferLogger : IBotLogger
    {
        /// <inheritdoc />
        public Task Debug(string scope, string message, params object[] formatParams)
        {
            message = formatParams?.Any() == true ? string.Format(message, formatParams) : message;
            ConsoleWriter.Default.PrintMessage($"[{scope}] {message}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Error(string scope, string message, params object[] formatParams)
        {
            message = formatParams?.Any() == true ? string.Format(message, formatParams) : message;
            ConsoleWriter.Default.PrintError($"[{scope}] {message}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Info(string scope, string message, params object[] formatParams)
        {
            message = formatParams?.Any() == true ? string.Format(message, formatParams) : message;
            ConsoleWriter.Default.PrintMessage($"[{scope}] {message}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Success(string scope, string message, params object[] formatParams)
        {
            message = formatParams?.Any() == true ? string.Format(message, formatParams) : message;
            ConsoleWriter.Default.PrintSuccess($"[{scope}] {message}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Warning(string scope, string message, params object[] formatParams)
        {
            message = formatParams?.Any() == true ? string.Format(message, formatParams) : message;
            ConsoleWriter.Default.PrintWarning($"[{scope}] {message}");

            return Task.CompletedTask;
        }
    }
}