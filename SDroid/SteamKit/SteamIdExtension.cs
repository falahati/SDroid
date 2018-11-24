using System;
using System.Threading.Tasks;
using SDroid.InternalModels.SteamUserAPI;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;
using SteamKit2;

namespace SDroid.SteamKit
{
    public static class SteamIdExtension
    {
        public static async Task<SteamID> GetSteamIdFromUserVanityName(SteamWebAPI steamWebAPI, string vanityName)
        {
            var result = await steamWebAPI.RequestObject<SteamWebAPIResponse<ResolveVanityUrlResponse>>(
                "ISteamUser",
                SteamWebAccessRequestMethod.Get,
                "ResolveVanityURL",
                "v1",
                new
                {
                    vanityurl = vanityName
                }
            ).ConfigureAwait(false);

            if (result?.Response?.Success == ResolveVanityUrlResponseStatus.Success)
            {
                return new SteamID(result.Response.SteamId);
            }

            throw new Exception(result?.Response?.Message);
        }
    }
}