## SDroid.SteamWeb
SDroid.SteamWeb is a C# library that provides Steam web login, access and WebAPI functionalities

## Classes
Followings are the classes available on this library along with their primary responsibility.

#### SteamWebAccess

`SteamWebAccess` is the class that can be used to request content from Steam's website. Being this request a JSON request, a binary one or a simple
page content retrieval; this the class you need to use.

#### WebSession

`WebSession` is essentially the cookie holder used by an instance of `SteamWebAccess` to request content from
the Steam's website. A valid `WebSession` can be retrieved with a successful login or by deserializing from
an older login. This class also holds information regarding the logged in session including `SessionId` and
SteamGuard Machine Authentication Token.

#### SteamWebAPI

`SteamWebAPI` is a wrapper around an instance of `SteamWebAccess` tailor-made to access Steam's API endpoints.

#### WebLogin

`WebLogin` allows you to start an authentication process with the Steam's website and acquire a valid instance of `WebSession` that can be used
for creating an instance of `SteamWebAccess` or serialized to disk for later usage.

## Samples
Followings are some simple samples demonstrating how this library can be used to access the Steam's website functionalities.

#### Login

```C#
var username = "USERNAME";
var password = "PASSWORD";

var loginCredentials = new LoginCredentials(username, password);
var webLogin = new WebLogin();

while (true)
{
    try
    {
        var session = await webLogin.DoLogin(loginCredentials);

        if (session?.HasEnoughInfo() == true)
        {
            // Login complete, serialize session or create an instance of SteamWebAccess

            return;
        }
    }
    catch (UserLoginException e)
    {
        switch (e.ErrorCode)
        {
            case UserLoginErrorCode.GeneralFailure:

                throw;
            case UserLoginErrorCode.BadRSAResponse:

                throw;
            case UserLoginErrorCode.BadCredentials:

                throw;
            case UserLoginErrorCode.TooManyFailedLoginAttempts:

                throw;
            case UserLoginErrorCode.NeedsCaptchaCode:
                // Should download the captcha image and fill the CaptchaCode property of LoginCredentials and try again
                var captchaImage = await e.UserLogin.DownloadCaptchaImage();
                loginCredentials.CaptchaCode = "CAPTCHA CODE";

                break;
            case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:
                // This account has a mobile authenticator associated with it
                // Should fill the TwoFactorAuthenticationCode property of LoginCredentials and try again
                loginCredentials.TwoFactorAuthenticationCode = "AUTHENTICATOR 2FA CODE";

                break;
            case UserLoginErrorCode.NeedsEmailVerificationCode:
                // This account uses Steam Guard for added security
                // Should fill the EmailVerificationCode property of LoginCredentials and try again
                loginCredentials.EmailVerificationCode = "EMAIL VERIFICATION CODE";

                break;
        }
    }
}
```

#### Check Session Validity

```C#
var json = "SESSION JSON CONTENT";
var webSession = JsonConvert.DeserializeObject<WebSession>(json);

if (webSession?.HasEnoughInfo() == true)
{
    var webAccess = new SteamWebAccess(webSession);

    if (await webAccess.VerifySession())
    {
        // Session is valid
    }
    else
    {
        // Session expired, please login again
    }
}
```


#### Get Game News using `SteamWebAPI` 

This example doesn't necessarily need an API key. In that case, you can use a constructor of `SteamWebAPI`
that doesn't accept an APIKey or just pass `null` instead.

```C#
var apiKey = "API KEY";
var json = "SESSION JSON CONTENT";
var webSession = JsonConvert.DeserializeObject<WebSession>(json);

if (webSession?.HasEnoughInfo() == true)
{
    var webAccess = new SteamWebAccess(webSession);
    var webAPI = new SteamWebAPI(apiKey, webAccess);

    var tf2News = await webAPI.RequestDynamic(
        "ISteamNews",
        SteamWebAccessRequestMethod.Get,
        "GetNewsForApp",
        "v2",
        new
        {
            appid = 440
        }
    );

    // Show tf2News
}
```

#### Get and/or Register API Key

```C#
var json = "SESSION JSON CONTENT";
var webSession = JsonConvert.DeserializeObject<WebSession>(json);

if (webSession?.HasEnoughInfo() == true)
{
    var webAccess = new SteamWebAccess(webSession);

    var apiKey = await SteamWebAPI.GetApiKey(webAccess);

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        if (await SteamWebAPI.RegisterApiKey(webAccess, "www.example.com"))
        {
            apiKey = await SteamWebAPI.GetApiKey(webAccess);
        }
        else
        {
            // Failed to register a new API key for this account
        }
    }

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        // Retrieved a valid API key associated with the account represented by the passed WebSession
        // Use this API key to create an instance of SteamWebAPI or just save it somewhere for later use

        var webAPI = new SteamWebAPI(apiKey, webAccess);
    }
}
```