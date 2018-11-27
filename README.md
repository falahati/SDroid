## SDroid
**SDroid** is a framework for designing and running custom bots for Steam network capable of trading 
and managing the Steam Account they are connected to. 

Based on [geel9's SteamAuth](https://github.com/geel9/SteamAuth) and [Jessecar96's SteamBot](https://github.com/Jessecar96/SteamBot) projects.

## Componenets
### SDroid.SteamWeb
**SDroid.SteamWeb** is a C# library that provides Steam web login, access and WebAPI functionalities.

Click [here](/SDroid.SteamWeb/README.md) to read more about this library.

This library is available as a NuGet package [here](https://www.nuget.org/packages/SDroid.SteamWeb).

### SDroid.SteamMobile
**SDroid.SteamMobile** is a C# library that provides Steam Mobile and Mobile Authenticator functionalities.

Click [here](/SDroid.SteamMobile/README.md) to read more about this library.

This library is available as a NuGet package [here](https://www.nuget.org/packages/SDroid.SteamMobile).

### SDroid.SteamTrade
**SDroid.SteamTrade** is a C# library that provides Steam trading and inventory retrieval functionalities.

Click [here](/SDroid.SteamTrade/README.md) to read more about this library.

This library is available as a NuGet package [here](https://www.nuget.org/packages/SDroid.SteamTrade).

### SDroid
**SDroid** in the highest level library in this project that aims to provides developers with an easier path to
design, develop and deploy a custom bot for the Steam Network.

This readme file contains information about the types available in this library and links to some simple
sample bots developed with it.

This library is available as a NuGet package [here](https://www.nuget.org/packages/SDroid).

## Classes
Followings are the classes available in this library along with their primary responsibility.

### `SteamBot`
`SteamBot` is an abstract class allowing developers to create a simple Steam Bot with as little code as possible.

### `SteamKitBot`
`SteamKitBot` is an abstract class extending the `SteamBot` type which aims to allow developers
to make a Steam Bot with access to the Steam Network through the [SteamKit2](https://github.com/SteamRE/SteamKit) 
library with as little code as possible.


## Interfaces
Followings are the classes available in this library along with their primary responsibility.

### `IAuthenticatorBot`
`IAuthenticatorBot` is an interface that can be added to a `SteamBot` child class that allows the login 
process to use the code generated by the authenticator to login into the Steam Website or Network.

The process of linking an authenticator to the account should be still handled by the developer. However, a 
bot extending the `SteamBot` and implementing the `IAuthenticatorBot` will also be notified if a new 
authenticator confirmation becomes available.

An `IAuthenticatorBot` bot automatically logs in using the mobile endpoints. This also allows for a greater
session validity.

### `ITradeBot`
`ITradeBot` is an interface that can be added to a `SteamBot` child class that automatically initializes an instance of
`TradeManager` after login and notifies the bot extending the `SteamBot` and implementing the `ITradeBot`
of its events.

### `ITradeOfferBot`
`ITradeOfferBot` is an interface that can be added to a `SteamBot` child class that automatically initializes an instance of
`TradeOfferManager` after login and notifies the bot extending the `SteamBot` and implementing the `ITradeOfferBot`
of its events.



## Samples
There are three sample bots available as part of the **SDroidTest** project:

### `AuthenticatorBot`
`AuthenticatorBot` is a bot extending the `SteamBot` and implementing the `IAuthenticatorBot` interface that
asks for a "maFile" or a "maFile2" to deserialize an authenticator as part of the login process. This bot also gets
notifications regarding new authenticator confirmations and can accept or reject these confirmations from the terminal.

Read the code being this bot by clicking [here](/SDroidTest/AuthenticatorBot.cs) or by compiling the `SDroidTest` project.

### `SteamKitBot`
`SteamKitBot` is a bot extending the `SteamKitBot` type and connects to the Steam Network, logs in, changes its online status, gets notifications regarding the account settings and wallet information as well as new chat messages. 

Chat responses can be sent from the terminal to all friends.

Read the code being this bot by clicking [here](/SDroidTest/SteamKitBot.cs) or by compiling the `SDroidTest` project.


### `TradeOfferBot`
`TradeOfferBot` is a bot extending the `SteamBot` and implementing the `ITradeofferBot` interface that login in and
gets notifications regarding changes in trade offers as well as retrieving bot's inventory on login.

Incoming trade offers can be accepted or rejected from the terminal.

Read the code being this bot by clicking [here](/SDroidTest/TradeOfferBot.cs) or by compiling the `SDroidTest` project.



## Disclaimer
Please note that this library created for research and educational propose only and there is no guarantee for it to work or function properly. 

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.



## License
The MIT License (MIT)

Copyright (c) 2018 Soroush Falahati

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

