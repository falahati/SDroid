using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleUtilities;
using SteamKit2;

namespace SDroidTest
{
    internal class Program
    {
        private static void ClearSettings()
        {
            ConsoleWriter.Default.PrintCaption("Clear Settings");
            var sure = ConsoleWriter.Default.PrintQuestion<bool>("Are your sure");

            if (sure)
            {
                var authenticatorSettings = SettingsExtension.Exist<AuthenticatorBotSettings>()
                    ? SettingsExtension.Load<AuthenticatorBotSettings>()
                    : null;
                string authenticatorJson = null;

                if (authenticatorSettings?.Authenticator != null)
                {
                    authenticatorJson = authenticatorSettings.Authenticator.Serialize();
                }

                SettingsExtension.Clear<SteamKitBotSettings>();
                SettingsExtension.Clear<TradeOfferBotSettings>();
                SettingsExtension.Clear<AuthenticatorBotSettings>();

                if (authenticatorSettings != null)
                {
                    ConsoleWriter.Default.PrintWarning(
                        "All settings cleared. Following is the serialized version of Authenticator object created by AuthenticatorBot; " +
                        "keep it safe or you might loss access to that account."
                    );

                    ConsoleWriter.Default.WriteColoredTextLine(authenticatorJson, ConsoleColor.Red);
                }
                else
                {
                    ConsoleWriter.Default.PrintSuccess("All settings cleared.");
                }
            }

            ConsoleWriter.Default.PrintSeparator();
        }

        // ReSharper disable once TooManyDeclarations
        private static void Main()
        {
            ConsoleNavigation.Default.PrintNavigation(new[]
            {
                new ConsoleNavigationItem("TradeOfferBot", (i, item) => StartTradeOfferBot()),
                new ConsoleNavigationItem("SteamKitBot", (i, item) => StartSteamKitBot()),
                new ConsoleNavigationItem("AuthenticatorBot", (i, item) => StartAuthenticatorBot()),
                new ConsoleNavigationItem("Clear saved settings (Careful with authenticator example)",
                    (i, item) => ClearSettings())
            }, "Select a sample bot to run");
        }

        private static void StartAuthenticatorBot()
        {
            ConsoleWriter.Default.PrintCaption("AuthenticatorBot");

            ConsoleWriter.Default.PrintMessage(
                "Initializing terminal, press enter for the list of valid commands.");

            using (var bot = new AuthenticatorBot(SettingsExtension.Load<AuthenticatorBotSettings>(), new Logger()))
            {
                bot.StartBot().Wait();

                new ConsoleTerminal("AuthenticatorBot", new Dictionary<string, Action<string[]>>
                {
                    {
                        "", strings =>
                        {
                            ConsoleWriter.Default.WritePaddedText("Valid commands are as follow:", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("start - Starts the bot if stopped", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("stop - Stops the bot if already started", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "confirm {tradeofferid} [tradeofferid2] [...] - Confirms one or more trade offer", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "deny {tradeofferid} [tradeofferid2] [...] - Denies one or more trade offers", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("exit - quit - Exits terminal and kills the bot", 6,
                                ConsoleColor.Cyan);
                        }
                    },
                    {
                        "start", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StartBot().Wait();
                        }
                    },
                    {
                        "stop", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StopBot().Wait();
                        }
                    },
                    {
                        "confirm", strings =>
                        {
                            if (strings.Length == 0)
                            {
                                ConsoleWriter.Default.PrintWarning("Please specify trade offer id.");

                                return;
                            }

                            foreach (var s in strings)
                            {
                                if (long.TryParse(s, out var tradeOfferId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    bot.ConfirmByTradeOfferId(tradeOfferId).Wait();
                                }
                                else
                                {
                                    ConsoleWriter.Default.PrintWarning($"`{s}` is not a valid trade offer id.");
                                }
                            }
                        }
                    },
                    {
                        "deny", strings =>
                        {
                            if (strings.Length == 0)
                            {
                                ConsoleWriter.Default.PrintWarning("Please specify trade offer id.");

                                return;
                            }

                            foreach (var s in strings)
                            {
                                if (long.TryParse(s, out var tradeOfferId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    bot.DenyByTradeOfferId(tradeOfferId).Wait();
                                }
                                else
                                {
                                    ConsoleWriter.Default.PrintWarning($"`{s}` is not a valid trade offer id.");
                                }
                            }
                        }
                    }
                }).RunTerminal();

                bot.StopBot().Wait();
            }

            ConsoleWriter.Default.PrintSeparator();
        }

        private static void StartSteamKitBot()
        {
            ConsoleWriter.Default.PrintCaption("SteamKitBot");

            using (var bot = new SteamKitBot(SettingsExtension.Load<SteamKitBotSettings>(), new Logger()))
            {
                bot.StartBot().Wait();

                ConsoleWriter.Default.PrintMessage(
                    "Initializing terminal, press enter for the list of valid commands.");
                new ConsoleTerminal("SteamKitBot", new Dictionary<string, Action<string[]>>
                {
                    {
                        "", strings =>
                        {
                            ConsoleWriter.Default.WritePaddedText("Valid commands are as follow:", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("start - Starts the bot if stopped", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("stop - Stops the bot if already started", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("setname {name} - Changes bot's persona name", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "friends - List all friends", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "chat {steamid} {message} - Sends a chat message", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("exit - quit - Exits terminal and kills the bot", 6,
                                ConsoleColor.Cyan);
                        }
                    },
                    {
                        "start", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StartBot().Wait();
                        }
                    },
                    {
                        "stop", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StopBot().Wait();
                        }
                    },
                    {
                        "setname", strings =>
                        {
                            if (strings.Length != 1 || string.IsNullOrWhiteSpace(strings[0]))
                            {
                                ConsoleWriter.Default.PrintWarning("Bad argument.");

                                return;
                            }

                            // ReSharper disable once AccessToDisposedClosure
                            bot.SteamFriends.SetPersonaName(strings[0]);
                        }
                    },
                    {
                        "friends", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            var friendCount = bot.SteamFriends.GetFriendCount();

                            for (var i = 0; i < friendCount; i++)
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                var friendSteamId = bot.SteamFriends.GetFriendByIndex(i);
                                // ReSharper disable once AccessToDisposedClosure
                                var friendName = bot.SteamFriends.GetFriendPersonaName(friendSteamId);

                                ConsoleWriter.Default.PrintMessage(
                                    $"{friendSteamId} - {friendName}");
                            }
                        }
                    },
                    {
                        "chat", strings =>
                        {
                            if (strings.Length >= 2 ||
                                !new SteamID(strings[0]).IsValid ||
                                string.IsNullOrWhiteSpace(string.Join(" ", strings.Skip(1))))
                            {
                                ConsoleWriter.Default.PrintWarning("Bad arguments.");

                                return;
                            }

                            var steamId = new SteamID(strings[0]);
                            var message = string.Join(" ", strings.Skip(1));

                            // ReSharper disable once AccessToDisposedClosure
                            bot.SteamFriends.SendChatMessage(steamId, EChatEntryType.ChatMsg, message);
                        }
                    }
                }).RunTerminal();

                bot.StopBot().Wait();
            }

            ConsoleWriter.Default.PrintSeparator();
        }

        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once MethodTooLong
        private static void StartTradeOfferBot()
        {
            ConsoleWriter.Default.PrintCaption("TradeOfferBot");

            using (var bot = new TradeOfferBot(SettingsExtension.Load<TradeOfferBotSettings>(), new Logger()))
            {
                bot.StartBot().Wait();

                ConsoleWriter.Default.PrintMessage(
                    "Initializing terminal, press enter for the list of valid commands.");
                new ConsoleTerminal("TradeOfferBot", new Dictionary<string, Action<string[]>>
                {
                    {
                        "", strings =>
                        {
                            ConsoleWriter.Default.WritePaddedText("Valid commands are as follow:", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("start - Starts the bot if stopped", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("stop - Stops the bot if already started", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "accept {tradeofferid} [tradeofferid2] [...] - Accepts one or more trade offer", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText(
                                "decline {tradeofferid} [tradeofferid2] [...] - Declines one or more trade offers", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("inventory [appid] [contextid] - Browse inventory", 6,
                                ConsoleColor.Cyan);
                            ConsoleWriter.Default.WritePaddedText("exit - quit - Exits terminal and kills the bot", 6,
                                ConsoleColor.Cyan);
                        }
                    },
                    {
                        "start", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StartBot().Wait();
                        }
                    },
                    {
                        "stop", strings =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            bot.StopBot().Wait();
                        }
                    },
                    {
                        "inventory", strings =>
                        {
                            if (strings.Length == 0)
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                foreach (var inventoryApp in bot.MyInventory.InventoryApps)
                                {
                                    ConsoleWriter.Default.PrintMessage($"{inventoryApp.AppId} - {inventoryApp.Name}");
                                }
                            }
                            else if (strings.Length == 1)
                            {
                                if (long.TryParse(strings[0], out var appId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    var inventoryApp =
                                        bot.MyInventory.InventoryApps.FirstOrDefault(app => app.AppId == appId);

                                    if (inventoryApp != null)
                                    {
                                        foreach (var context in inventoryApp.Contexts)
                                        {
                                            ConsoleWriter.Default.PrintMessage(
                                                $"{context.ContextId} - {context.ContextName}");
                                        }

                                        return;
                                    }
                                }

                                ConsoleWriter.Default.PrintWarning("Bad or invalid app id.");
                            }
                            else if (strings.Length == 2)
                            {
                                if (long.TryParse(strings[0], out var appId) &&
                                    long.TryParse(strings[1], out var contextId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    var inventoryApp =
                                        bot.MyInventory.InventoryApps.FirstOrDefault(app => app.AppId == appId);
                                    var inventoryAppContext =
                                        inventoryApp?.Contexts.FirstOrDefault(context =>
                                            context.ContextId == contextId);

                                    if (inventoryApp != null && inventoryAppContext != null)
                                    {
                                        // ReSharper disable once AccessToDisposedClosure
                                        var assets = bot.MyInventory.GetAssets(inventoryApp.AppId,
                                            inventoryAppContext.ContextId).Result;

                                        foreach (var asset in assets)
                                        {
                                            // ReSharper disable once AccessToDisposedClosure
                                            var assetDescription = bot.MyInventory.GetAssetDescription(asset).Result;
                                            ConsoleWriter.Default.PrintMessage(
                                                $"{asset.AssetId} x {asset.Amount} - {assetDescription.Name}");
                                        }

                                        return;
                                    }
                                }

                                ConsoleWriter.Default.PrintWarning("Bad or invalid app id and/or context id.");
                            }
                        }
                    },
                    {
                        "accept", strings =>
                        {
                            if (strings.Length == 0)
                            {
                                ConsoleWriter.Default.PrintWarning("Please specify trade offer id.");

                                return;
                            }

                            foreach (var s in strings)
                            {
                                if (long.TryParse(s, out var tradeOfferId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    bot.AcceptTradeOfferById(tradeOfferId).Wait();
                                }
                                else
                                {
                                    ConsoleWriter.Default.PrintWarning($"`{s}` is not a valid trade offer id.");
                                }
                            }
                        }
                    },
                    {
                        "decline", strings =>
                        {
                            if (strings.Length == 0)
                            {
                                ConsoleWriter.Default.PrintWarning("Please specify trade offer id.");

                                return;
                            }

                            foreach (var s in strings)
                            {
                                if (long.TryParse(s, out var tradeOfferId))
                                {
                                    // ReSharper disable once AccessToDisposedClosure
                                    bot.DeclineTradeOfferById(tradeOfferId).Wait();
                                }
                                else
                                {
                                    ConsoleWriter.Default.PrintWarning($"`{s}` is not a valid trade offer id.");
                                }
                            }
                        }
                    }
                }).RunTerminal();

                bot.StopBot().Wait();
            }

            ConsoleWriter.Default.PrintSeparator();
        }
    }
}