## SDroid.SteamMobile
SDroid.SteamMobile is a C# library that provides Steam Mobile and Mobile Authenticator functionalities

## Classes
Followings are the classes available on this library along with their primary responsibility.

#### SteamMobileWebAccess

`SteamMobileWebAccess` a child type of `SteamWebAccess` which provides you with access to Steam's website functionality
mimicking the behaviour of a mobile phone browser. This behaviour also allows accessing mobile-specific pages. Read more about `SteamWebAccess` click [here](/falahati/SDroid/blob/master/SDroid.SteamWeb/README.md#steamwebaccess).

#### MobileSession

`MobileSession` is a child type of `WebSession` by extending this class to include mobile-specific
information regarding a logged in session. A `MobileSession` can be retrieved with a successful login or by deserializing from
an older logged in session via `MobileLogin` type. An expired `MobileSession` can also be extended using the
OAuth token provided as part of the session and therefore does not requires a reauthentication process unless the OAuth token
itself becomes expired. Read more about `WebSession` click [here](/falahati/SDroid/blob/master/SDroid.SteamWeb/README.md#websession).

#### MobileLogin

`MobileLogin` allows you to start an authentication process with the Steam's mobile endpoint and acquire a valid instance of `MobileSession` that can be used
for creating an instance of `SteamMobileWebAccess` or serialized to disk for later usage. Since `MobileLogin` is a child type of
`WebLogin`, it can be used as an in-place replacement of that type and there the login process
is similar to identical to `WebLogin`. For more information about the `WebLogin` type 
click [here](/falahati/SDroid/blob/master/SDroid.SteamWeb/README.md#weblogin) and for a sample of login process
click [here](/falahati/SDroid/blob/master/SDroid.SteamWeb/README.md#login).

#### Authenticator

`Authenticator` represents a valid authenticator associated with an account allowing the user
to generate Steam Guard codes and manage Steam Mobile confirmations.

#### AuthenticatorLinker

An instance `AuthenticatorLinker` can be used to manage Steam account's phone number and
to create and link a new `Authenticator` instance to an account.

## Samples
Followings are some simple samples demonstrating how this library can be used to access the Steam's website functionalities.

#### Refreshing an expired `MobileSession`

```C#
var json = "SESSION JSON CONTENT";
var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);

if (mobileSession?.HasEnoughInfo() == true)
{
    var webAccess = new SteamMobileWebAccess(mobileSession);

    if (await webAccess.VerifySession())
    {
        // Session is valid
    }
    else
    {
        // Session expired, lets try refreshing it
        if (await mobileSession.RefreshSession(webAccess))
        {
            // Session refreshed and valid again
        }
        else
        {
            // Failed to refresh the session, must log in again
        }
    }
}
```


#### Adding a phone number to account

Before being able to associate an authenticator with an account, you should make sure that the
account has a valid and active phone number.

```C#
var json = "LOGGED IN SESSION JSON CONTENT";
var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);
var webAccess = new SteamMobileWebAccess(mobileSession);
var authenticatorLinker = new AuthenticatorLinker(webAccess);

if (await authenticatorLinker.DoesAccountHasPhoneNumber())
{
    // The account already has a phone number
    return;
}

var phoneNumber = "+98000000000";

if (!await authenticatorLinker.RequestToAddPhoneNumber(phoneNumber))
{
    // Request to add the phone number to account failed
    return;
}
            
// To accept and finalize this authenticator we need to have hold of the
// text message sent to the account's associated phone number
var smsCode = "SMS CODE HERE";

if (!await authenticatorLinker.VerifyPhoneNumberBySMS(smsCode))
{
    // Failed to finalize and verify added phone number
    // Probably bad SMS code; should try again
    return;
}
```


#### Adding an authenticator to account

```C#
var json = "LOGGED IN SESSION JSON CONTENT";
var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);
var webAccess = new SteamMobileWebAccess(mobileSession);
var authenticatorLinker = new AuthenticatorLinker(webAccess);

var authenticator = await authenticatorLinker.RequestToAddAuthenticator();

// We just got a valid authenticator data
// We should serialize and save this instance before doing anything else
authenticator.SerializeToFile("MyAuthenticator.maFile2");

// To accept and finalize this authenticator we need to have hold of the
// text message sent to the account's associated phone number
var smsCode = "SMS CODE HERE";

try
{
    await authenticatorLinker.FinalizeAddAuthenticator(authenticator, smsCode);

    // Authenticator finalized
}
catch (AuthenticatorLinkerException e)
{

    if (e.ErrorCode == AuthenticatorLinkerErrorCode.BadSMSCode)
    {
        // Bad SMS code
        throw;
    }
}
```

#### Fetch, accept and deny confirmations

```C#
var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

var confirmations = await authenticator.FetchConfirmations();

foreach (var confirmation in confirmations)
{
    switch (confirmation.Type)
    {
        case ConfirmationType.Trade:
            var tradeOfferId = confirmation.Creator;
            // Should be cross-matched with a trade offer before deciding to confirm or deny

            if (await authenticator.AcceptConfirmation(confirmation))
            {
                // Trade offer confirmed
            }
            else
            {
                // Failed to confirm the trade offer
            }
            break;
        case ConfirmationType.MarketSellTransaction:
            var marketTransactionId = confirmation.Creator;
            // Should be cross-matched with a market sell transaction before deciding to confirm or deny

            if (await authenticator.DenyConfirmation(confirmation))
            {
                // Market transaction denied
            }
            else
            {
                // Failed to deny the market transaction
            }
            break;
    }
}
```

#### Generate Steam Guard Code

From an instance of `Authenticator`:

```C#
var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

var steamGuardCode = await authenticator.GenerateSteamGuardCode();
// Do something with Steam Guard code
```

From the authenticator shared secret:

```C#
var authenticatorSharedSecret = "AUTHENTICATOR SHARED SECRED";

// Authenticator shared secret is also available as part of a valid Authenticator instance
//authenticatorSharedSecret = authenticator.AuthenticatorData.SharedSecret;

var steamGuardCode = await Authenticator.GenerateSteamGuardCode(authenticatorSharedSecret);
// Do something with Steam Guard code
```

#### Remove and revoke an authenticator

```C#
var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

await authenticator.RevokeAuthenticator();
```