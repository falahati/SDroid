using Newtonsoft.Json;

namespace SDroid.SteamWeb.Models
{
    public class SteamWebAPIResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}