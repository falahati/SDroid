using System;
using System.Collections.Generic;
using System.Net.Http;
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
            string userNonce)
        {
            // Generate random SessionId
            var sessionId = Guid.NewGuid().ToString("N");

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
                using (var steamUserAuth = WebAPI.GetAsyncInterface("ISteamUserAuth"))
                {
                    var result = await steamUserAuth.CallAsync(
                        HttpMethod.Post,
                        "AuthenticateUser", 1,
                        new Dictionary<string, object>
                        {
                            {"steamid", client.SteamID.ConvertToUInt64().ToString()},
                            {"sessionkey", HttpUtility.UrlEncode(cryptedSessionKey)},
                            {"encrypted_loginkey", HttpUtility.UrlEncode(cryptedLoginKey)}
                        }
                    ).ConfigureAwait(false);

                    return new WebSession(
                        client.SteamID.ConvertToUInt64(),
                        result["token"]?.Value?.ToUpper(),
                        result["tokensecure"]?.Value?.ToUpper(),
                        sessionId,
                        null,
                        null
                    );
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}