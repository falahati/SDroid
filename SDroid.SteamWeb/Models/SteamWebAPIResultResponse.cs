using Newtonsoft.Json;

namespace SDroid.SteamWeb.Models
{
    public class SteamWebAPIResultResponse<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}