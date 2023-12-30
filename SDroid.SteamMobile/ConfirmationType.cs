namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Known types of confirmation that a valid instance of Authenticator might receive
    /// </summary>
    public enum ConfirmationType
    {
        /// <summary>
        ///     Invalid confirmation request
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     Test
        /// </summary>
        Test = 1,

        /// <summary>
        ///     A trade confirmation request
        /// </summary>
        Trade = 2,

        /// <summary>
        ///     A market sell transaction request
        /// </summary>
        MarketListing = 3,

        FeatureOptOut = 4,

        PhoneNumberChange = 5,

        AccountRecovery = 6
    }
}