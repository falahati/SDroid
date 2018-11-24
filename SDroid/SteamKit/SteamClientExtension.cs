using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.SteamKit
{
    public static class SteamClientExtension
    {
        public static async Task<WebSession> AuthenticateWebSession(
            this SteamClient client,
            SteamWebAPI steamWebAPI,
            string userNonce)
        {
            // Generate random SessionId
            var uniqueId = client.SteamID.ToString(); // Guid.NewGuid().ToString("N");
            var sessionId = Convert.ToBase64String(Encoding.UTF8.GetBytes(uniqueId));

            // Generate an AES SessionKey.
            var sessionKey = CryptoHelper.GenerateRandomBlock(32);

            // rsa encrypt it with the public key for the universe we're on
            byte[] cryptedSessionKey;

            using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.Universe)))
            {
                cryptedSessionKey = rsa.Encrypt(sessionKey);
            }

            var loginKey = new byte[20];
            Array.Copy(Encoding.ASCII.GetBytes(userNonce), loginKey, userNonce.Length);

            // AES encrypt the loginkey with our session key.
            var cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);


            try
            {
                var result = await steamWebAPI.RequestDynamic(
                    "ISteamUserAuth",
                    SteamWebAccessRequestMethod.Post,
                    "AuthenticateUser",
                    "v0001",
                    new
                    {
                        steamid = client.SteamID.ConvertToUInt64().ToString(),
                        sessionkey = HttpUtility.UrlEncode(cryptedSessionKey),
                        encrypted_loginkey = HttpUtility.UrlEncode(cryptedLoginKey)
                    }
                ).ConfigureAwait(false);

                return new WebSession(result["token"].ToString(), result["tokensecure"].ToString(), sessionId);

                //using (var steamUserAuth = WebAPI.GetAsyncInterface("ISteamUserAuth", apiKey))
                //{
                //    //using (dynamic userAuth = WebAPI.GetAsyncInterface("", ""))
                //    //{
                //    //    var authResult = await userAuth.AuthenticateUser();
                //    //    authResult = userAuth.AuthenticateUser(
                //    //        steamid: client.SteamID.ConvertToUInt64(),
                //    //        sessionkey: HttpUtility.UrlEncode(cryptedSessionKey),
                //    //        encrypted_loginkey: HttpUtility.UrlEncode(cryptedLoginKey),
                //    //        method: "POST",
                //    //        secure: true
                //    //    );
                //    //}

                //    var result = await steamUserAuth.CallAsync(HttpMethod.Post, "AuthenticateUser", 1,
                //        new Dictionary<string, string>
                //        {
                //            {"steamid", client.SteamID.ConvertToUInt64().ToString()},
                //            {"sessionkey", HttpUtility.UrlEncode(cryptedSessionKey)},
                //            {"encrypted_loginkey", HttpUtility.UrlEncode(cryptedLoginKey)}
                //        }).ConfigureAwait(false);

                //    return new WebSession(result["token"].ToString(), result["tokensecure"].ToString(), sessionId);
                //}
            }
            catch (Exception e)
            {
                return null;
                // TODO: THROW?
            }
        }
    }
}