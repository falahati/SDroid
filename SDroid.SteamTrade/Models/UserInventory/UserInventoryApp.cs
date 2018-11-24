using System.Linq;

namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryApp
    {
        internal const long DefaultBackpackContextId = 2;

        internal UserInventoryApp(long appId)
        {
            AppId = appId;
            Contexts = new[] {new UserInventoryAppContext(DefaultBackpackContextId)};
        }

        internal UserInventoryApp(
            long appId,
            string name,
            string icon) : this(appId)
        {
            Name = name;
            Icon = icon;
        }

        internal UserInventoryApp(
            long appId,
            string name,
            string icon,
            UserInventoryAppContext[] contexts,
            string inventoryLogo,
            bool? storeVetted,
            string tradePermissions) : this(appId, name, icon)
        {
            Contexts = contexts;
            StoreVetted = storeVetted;
            InventoryLogo = inventoryLogo;
            TradePermissions = tradePermissions;
        }

        public long AppId { get; }

        public UserInventoryAppContext[] Contexts { get; private set; }

        public string Icon { get; private set; }

        public string InventoryLogo { get; private set; }

        public string Name { get; private set; }

        public bool? StoreVetted { get; private set; }

        public string TradePermissions { get; private set; }

        internal void Update(UserInventoryApp sameApp)
        {
            if (AppId != sameApp.AppId)
            {
                return;
            }

            Name = sameApp.Name ?? Name;
            Icon = sameApp.Icon ?? Icon;
            StoreVetted = sameApp.StoreVetted ?? StoreVetted;
            InventoryLogo = sameApp.InventoryLogo ?? InventoryLogo;
            TradePermissions = sameApp.TradePermissions ?? TradePermissions;

            foreach (var context in Contexts)
            {
                var newContext = sameApp.Contexts.FirstOrDefault(c => c.ContextId == context.ContextId);

                if (newContext != null)
                {
                    context.Update(newContext);
                }
            }

            Contexts = Contexts.Concat(sameApp.Contexts.Where(c1 => Contexts.All(c2 => c1.ContextId != c2.ContextId)))
                .ToArray();
        }
    }
}