namespace Deucarian.Session.APIIntegration
{
    /// <summary>Stable sanitized token-endpoint failure codes.</summary>
    public static class SessionTokenEndpointErrorCodes
    {
        public const string InvalidConfiguration =
            "token_endpoint_invalid_configuration";
        public const string MissingInput =
            "token_endpoint_missing_input";
        public const string MissingCurrentAccessToken =
            "token_endpoint_missing_current_access_token";
        public const string RequestFailed =
            "token_endpoint_request_failed";
        public const string AuthenticationRejected =
            "token_endpoint_authentication_rejected";
        public const string InvalidResponse =
            "token_endpoint_invalid_response";
    }
}
