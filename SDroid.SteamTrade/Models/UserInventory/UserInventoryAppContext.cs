namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAppContext
    {
        internal UserInventoryAppContext(long contextId, string contextName) : this(contextId)
        {
            ContextName = contextName;
        }

        internal UserInventoryAppContext(long contextId)
        {
            ContextId = contextId;
        }

        public long ContextId { get; }

        public string ContextName { get; private set; }


        internal void Update(UserInventoryAppContext sameContext)
        {
            if (ContextId != sameContext.ContextId)
            {
                return;
            }

            ContextName = sameContext.ContextName ?? ContextName;
        }
    }
}