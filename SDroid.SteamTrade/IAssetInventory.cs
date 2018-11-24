using System.Threading.Tasks;
using SteamKit2;

namespace SDroid.SteamTrade
{
    public interface IAssetInventory
    {
        SteamID SteamId { get; }
        Task<Asset[]> GetAssets();
    }
}