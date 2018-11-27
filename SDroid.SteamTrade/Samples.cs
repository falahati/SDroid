using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamTrade.EventArguments;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.SteamTrade
{
    internal static class Samples
    {
        public async Task GetUserInventoryApps()
        {
            var webSessionJson = "SESSION JSON CONTENT";
            var steamId = 76561198000000000;

            var webSession = JsonConvert.DeserializeObject<WebSession>(webSessionJson);
            var webAccess = new SteamWebAccess(webSession);
            var userInventory = await UserInventory.GetInventory(webAccess, new SteamID(webSessionJson));

            foreach (var inventoryApp in userInventory.InventoryApps)
            {
                var appId = inventoryApp.AppId;
                var appName = inventoryApp.Name;
                // Do something with appId and appName

                foreach (var appContext in inventoryApp.Contexts)
                {
                    var contextId = appContext.ContextId;
                    var contextName = appContext.ContextName;

                    // Do something with appId, contextId and contextName
                }
            }
        }

        public async Task GetUserInventoryAssets()
        {
            var webSessionJson = "SESSION JSON CONTENT";
            var steamId = 76561198000000000;

            var webSession = JsonConvert.DeserializeObject<WebSession>(webSessionJson);
            var webAccess = new SteamWebAccess(webSession);
            var userInventory = await UserInventory.GetInventory(webAccess, new SteamID(webSessionJson));

            // App Id of the App or Game to retrieve assets
            var appId = AppIds.DotA2;

            // Context Id of the App inventory context to retrieve assets
            var contextId = ContextIds.DotA2.Backpack; 

            var assets = await userInventory.GetAssets(appId, contextId);

            foreach (var asset in assets)
            {
                // Do something with asset

                var assetId = asset.AssetId;
                var amount = asset.Amount;
                
                var assetDescription = await userInventory.GetAssetDescription(asset);

                // Do something with assetDescription

                var assetName = assetDescription.Name;
                var isAssetTradable = assetDescription.IsTradable;
            }
        }
        
        public async Task GetUserBackpackAssets()
        {
            var apiKey = "API KEY";
            var json = "SESSION JSON CONTENT";
            var steamId = 76561198000000000;

            // App Id of the App or Game to retrieve backpack assets
            var appId = AppIds.TeamFortress2;

            var webSession = JsonConvert.DeserializeObject<WebSession>(json);
            var webAccess = new SteamWebAccess(webSession);
            var webApi = new SteamWebAPI(apiKey, webAccess);
            var userBackpack = await Backpack.GetBackpack(webApi, new SteamID(webSessionJson), appId);

            var assets = userBackpack.GetAssets();

            foreach (var asset in assets)
            {
                // Do something with asset

                var assetId = asset.AssetId;
                var amount = asset.Amount;
                var defIndex = asset.DefinitionIndex;
                
                var assetDescription = await userBackpack.GetAssetDescription(asset);

                // Do something with assetDescription

                var assetName = assetDescription.Name;
                var isAssetTradable = assetDescription.IsTradable;
            }
        }
        
        public async Task ManageTradeOffers()
        {
            var apiKey = "API KEY";
            var json = "SESSION JSON CONTENT";
            
            var webSession = JsonConvert.DeserializeObject<WebSession>(json);
            var webAccess = new SteamWebAccess(webSession);
            var webApi = new SteamWebAPI(apiKey, webAccess);
            var tradeOfferManager = new TradeOfferManager(webApi, webAccess, TradeOfferOptions.Default);

            tradeOfferManager.TradeOfferReceived += (sender, args) =>
            {
                // Received new trade offer
                // Accept it right away if it is a gift
                if (!args.TradeOffer.IsOurOffer && args.TradeOffer.OurAssets.Length == 0)
                {
                    await tradeOfferManager.Accept(args.TradeOffer);
                }
            };

            tradeOfferManager.TradeOfferInEscrow += (sender, args) =>
            {
                // A trade offer is in escrow
                // If escrow is longer than 10 days, decline or cancel

                var escrowDuration = await tradeOfferManager.GetEscrowDuration(args.TradeOffer);

                if (escrowDuration.MyEscrowDuration > TimeSpan.FromDays(10))
                {
                    if (args.TradeOffer.IsOurOffer)
                    {
                        await tradeOfferManager.Cancel(args.TradeOffer);
                    }
                    else
                    {
                        await tradeOfferManager.Decline(args.TradeOffer);
                    }
                }
            };

            tradeOfferManager.StartPolling();

            // Wait for the end of program

            tradeOfferManager.Dispose();
        }

        public async Task CreateANewTradeOffer()
        {
            var apiKey = "API KEY";
            var json = "SESSION JSON CONTENT";
            var partnerSteamId = 76561198000000000;

            var webSession = JsonConvert.DeserializeObject<WebSession>(json);
            var webAccess = new SteamWebAccess(webSession);
            var webApi = new SteamWebAPI(apiKey, webAccess);
            var tradeOfferManager = new TradeOfferManager(webApi, webAccess, TradeOfferOptions.Default);

            if (!await tradeOfferManager.ValidateAccess(new SteamID(partnerSteamId)))
            {
                // Can't propose a trade offer to this user
                // Maybe inventory is private
                return;
            }

            // Get partner inventory
            var partnerInventory = await tradeOfferManager.GetPartnerInventory(new SteamID(partnerSteamId));

            // Get all partner dota2 assets
            var partnerDota2Assets = await partnerInventory.GetAssets(AppIds.DotA2);

            // We don't want to give any asset xD
            var ourAssets = new Asset[0];

            var newTradeOfferItemsList = new NewTradeOfferItemsList(ourAssets, partnerDota2Assets.Cast<Asset>().ToArray());
            var tradeOfferMessage = "Give me your Dota2 items";
            var sentTradeOfferId = await tradeOfferManager.Send(new SteamID(partnerSteamId), newTradeOfferItemsList, tradeOfferMessage);

            // We can also get the trade offer
            var sentTradeOffer = await tradeOfferManager.GetTradeOffer(sentTradeOfferId);
        }
        
        public async Task Trade()
        {
            var apiKey = "API KEY";
            var json = "SESSION JSON CONTENT";
            var partnerSteamId = 76561198000000000;

            var webSession = JsonConvert.DeserializeObject<WebSession>(json);
            var webAccess = new SteamWebAccess(webSession);
            var webApi = new SteamWebAPI(apiKey, webAccess);
            var tradeManager = new TradeManager(webApi, webAccess, TradeOptions.Default);

            var trade = await tradeManager.CreateTrade(new SteamID(partnerSteamId));
            var tradePartnerInventory = await trade.GetPartnerInventory();

            trade.PartnerMessaged += (sender, args) =>
            {
                var partnerMessage = args.Message;

                if (partnerMessage == "hello" || args.Message == "hey")
                {
                    trade.SendMessage("Hello to you too.");
                }
            };

            trade.PartnerOfferedItemsChanged += (sender, args) =>
            {
                if (args.Action == PartnerOfferedItemsChangedAction.Added)
                {
                    var userAssets = await tradePartnerInventory.GetAssets(args.Asset.AppId, args.Asset.ContextId);

                    foreach (var userAsset in userAssets)
                    {
                        if (userAsset.Equals(args.Asset))
                        {
                            // This is our asset

                            var assetDescription = await tradePartnerInventory.GetAssetDescription(userAsset);

                            // Do something with assetDescription

                            break;
                        }
                    }
                }
            };

            trade.PartnerReadyStateChanged += (sender, args) =>
            {
                // If this is a gift, set ready and accept
                if (trade.MyOfferedItems.Length == 0)
                {
                    await trade.SetReadyState(true);
                    await trade.AcceptTrade();
                }
            };
            
            // Wait for trade to end

            trade.Dispose();
        }
    }
}
