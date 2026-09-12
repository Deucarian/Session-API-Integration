namespace Deucarian.Session.APIIntegration
{
    /// <summary>Safe token-endpoint failure classification without response bodies, URLs or credentials.</summary>
    public static class SessionTokenEndpointFailures
    {
        public static SessionResult FromHttpStatus(long? status)
        {
            string code;
            switch (status)
            {
                case 400: case 422: code = SessionTokenEndpointErrorCodes.InvalidRequest; break;
                case 401: case 403: code = SessionTokenEndpointErrorCodes.AuthenticationRejected; break;
                case 404: code = SessionTokenEndpointErrorCodes.EndpointNotFound; break;
                case 405: code = SessionTokenEndpointErrorCodes.MethodNotAllowed; break;
                case 408: case 504: code = SessionTokenEndpointErrorCodes.RequestTimeout; break;
                case 429: code = SessionTokenEndpointErrorCodes.RateLimited; break;
                default:
                    code = status >= 500 && status <= 599
                        ? SessionTokenEndpointErrorCodes.ServiceUnavailable
                        : SessionTokenEndpointErrorCodes.RequestFailed;
                    break;
            }
            return SessionResult.Failed(code, Describe(code));
        }

        /// <summary>Describes known codes only. Never display an untrusted error message or exception.</summary>
        public static string Describe(string code)
        {
            switch (code)
            {
                case SessionTokenEndpointErrorCodes.InvalidConfiguration:
                    return "The authentication endpoint is not configured correctly. Check the selected connection and endpoint profile.";
                case SessionTokenEndpointErrorCodes.MissingInput:
                    return "Enter all required sign-in values before trying again.";
                case SessionTokenEndpointErrorCodes.MissingCurrentAccessToken:
                    return "There is no current access token. Sign in to start a new session.";
                case SessionTokenEndpointErrorCodes.AuthenticationRejected:
                    return "Sign-in was rejected (HTTP 401/403). Check your credentials and account permissions.";
                case SessionTokenEndpointErrorCodes.EndpointNotFound:
                    return "No authentication endpoint was found (HTTP 404). Check the endpoint path and environment.";
                case SessionTokenEndpointErrorCodes.MethodNotAllowed:
                    return "The endpoint does not accept this request method (HTTP 405). Check its method in the endpoint profile.";
                case SessionTokenEndpointErrorCodes.InvalidRequest:
                    return "The service rejected the request format or values (HTTP 400/422). Check the required fields and endpoint mapping.";
                case SessionTokenEndpointErrorCodes.RequestTimeout:
                    return "The authentication service timed out. Try again or check the service connection.";
                case SessionTokenEndpointErrorCodes.RateLimited:
                    return "Too many authentication attempts (HTTP 429). Wait before trying again.";
                case SessionTokenEndpointErrorCodes.ServiceUnavailable:
                    return "The authentication service reported a server error (HTTP 5xx). Try again later or contact the service owner.";
                case SessionTokenEndpointErrorCodes.InvalidResponse:
                    return "The service returned an unexpected token response. Check the response mapping in the endpoint profile.";
                case SessionTokenEndpointErrorCodes.RequestFailed:
                    return "The authentication request could not be completed. Check the connection, server address and endpoint configuration.";
                default:
                    return "Authentication failed. Check the configured endpoint and supplied values, then try again.";
            }
        }
    }
}
