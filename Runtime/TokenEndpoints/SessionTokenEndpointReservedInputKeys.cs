namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Input keys resolved from the supplied current session instead of UI values.
    /// They are intended for explicitly configured refresh endpoints.
    /// </summary>
    public static class SessionTokenEndpointReservedInputKeys
    {
        public const string AccessToken = "current_access_token";
        public const string RefreshToken = "current_refresh_token";
    }
}
