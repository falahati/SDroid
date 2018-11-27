## SDroid.SteamTrade
**SDroid.SteamTrade** is a C# library that provides Steam trading and inventory retrieval functionalities.

## Classes
Followings are the classes available in this library along with their primary responsibility.

#### `UserInventory`

`UserInventory` class allows access to a user's inventory sections (aka Apps or Games), contexts and assets along
with asset descriptions. This class uses lazy loading for retrieval of inventory assets and also caches
the results.

#### `Backpack`

`Backpack` class provides access to a user's backpack data for a limited list of compatible games.
The list now only includes "Team Fortress 2" that provides full support. Some other games also allow
user to get a list of user's assets; however, they don't provide a way to access asset descriptions.

Information provided by this class comparing to `UserInventory` is more complete and even
contains some in-game details about some of the items.


#### `Asset`

`Asset` holds the minimum information required to represent an item in a user inventory, user backpack, trade or trade offer. 

It is also the base class of `UserInventoryAsset` and `BackpackAsset` classes.


#### `TradeManager`

`TradeManager` allows the user of the library to initialize new trades as well as getting a list
of all ongoing trades.


#### `Trade`

`Trade` represents an ongoing trade and allows the user to subscribe to the events happening
in the trade including when new items get added to the trade as well as providing the ability
to control a trade.

`Trade` also provides access to the trade partner's inventory. However, the access is limited comparing
to a `UserInventory` instance created directly from a public inventory.


#### `TradeOfferManager`
`TradeOfferManager` allows the user of the library to create a new trade offer as well as managing 
other incoming and outgoing trade offers by subscribing to the events provided for the changes in trade offers' status.

`TradeOfferManager` also provides access to the trade partner's inventory.

#### `TradeOffer`
`TradeOffer` represents an active incoming or outgoing trade offer and should be used alongside
`TradeOfferManager` to accept, decline or cancel a trade offer.

#### `NewTradeOfferItemsList`
`NewTradeOfferItemsList` holds an array of assets for both parties and can be used 
to create a new `TradeOffer` using an instance of `TradeOfferManager`.

## Samples
Followings are some simple samples demonstrating how this library can be used to access user's inventory and manage and/or create trades and trade offers.

#### Getting user's inventory sections (aka Apps or Games) and their contexts

```C#
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
```

#### Getting user's inventory assets and their description

```C#
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
```

#### Getting user's backpack assets

```C#
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
```

#### Managing trade offers

```C#
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
```

#### Creating a new trade offer

```C#
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
```

#### Trading

```C#
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
```