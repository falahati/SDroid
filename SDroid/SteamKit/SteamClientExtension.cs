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
            string userNonce
            )
        {
            // Generate random SessionId
            var sessionId = Guid.NewGuid().ToString("N");

            // Generate an AES SessionKey.
            var sessionKey = CryptoHelper.GenerateRandomBlock(32);

            // rsa encrypt it with the public key for the universe we're on
            byte[] encryptedSessionKey;

            using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.Universe)))
            {
                encryptedSessionKey = rsa.Encrypt(sessionKey);
            }

            // AES encrypt the loginkey with our session key.
            var encryptedLoginKey = CryptoHelper.SymmetricEncrypt(
                Encoding.ASCII.GetBytes(userNonce),
                sessionKey
            );

            try
            {
                using (var steamUserAuth = WebAPI.GetAsyncInterface("ISteamUserAuth"))
                {
                    var result = await steamUserAuth.CallAsync(
                        HttpMethod.Post,
                        "AuthenticateUser", 
                        1,
                        new Dictionary<string, object>
                        {
                            {"steamid", client.SteamID.ConvertToUInt64().ToString()},
                            {"sessionkey", encryptedSessionKey},
                            {"encrypted_loginkey", encryptedLoginKey}
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
            catch
            {
                return null;
            }
        }
    }
}