using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Newtonsoft.Json.Linq;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Executes credential-sensitive token exchanges through Deucarian API and
    /// maps successful responses into immutable Session data.
    /// </summary>
    public sealed class SessionTokenEndpointExecutor
    {
        private readonly IApiClient apiClient;
        private readonly Func<DateTimeOffset> utcNowProvider;

        /// <summary>Creates an executor.</summary>
        public SessionTokenEndpointExecutor(IApiClient apiClient)
            : this(apiClient, null)
        {
        }

        internal SessionTokenEndpointExecutor(
            IApiClient apiClient,
            Func<DateTimeOffset> utcNowProvider)
        {
            this.apiClient = apiClient ??
                throw new ArgumentNullException(nameof(apiClient));
            this.utcNowProvider = utcNowProvider ??
                (() => DateTimeOffset.UtcNow);
        }

        /// <summary>Executes one login, acquisition, or refresh exchange.</summary>
        public async Task<SessionResult> ExecuteAsync(
            SessionTokenEndpointConfig config,
            SessionTokenEndpointInputValues inputValues,
            SessionData currentSession = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (config == null)
            {
                return Failure(
                    SessionTokenEndpointErrorCodes.InvalidConfiguration,
                    "Token endpoint configuration is invalid.");
            }

            if (config.UseCurrentAccessTokenAsBearer &&
                string.IsNullOrWhiteSpace(currentSession?.AccessToken))
            {
                return Failure(
                    SessionTokenEndpointErrorCodes.MissingCurrentAccessToken,
                    "A current access token is required for this token endpoint.");
            }

            var jsonBody = new Dictionary<string, string>(StringComparer.Ordinal);
            ApiRequest request = null;
            try
            {
                string endpoint = config.EndpointTemplate;
                request = new ApiRequest(
                    endpoint,
                    config.Method,
                    config.UseCurrentAccessTokenAsBearer
                        ? ApiAuthenticationRequirement.Required
                        : ApiAuthenticationRequirement.Disabled)
                {
                    ResponseFormat = ApiResponseFormat.Json,
                    TimeoutSeconds = config.TimeoutSeconds,
                    SuppressLogging = true,
                    BearerTokenOverride =
                        config.UseCurrentAccessTokenAsBearer
                            ? currentSession.AccessToken
                            : null
                };

                foreach (SessionTokenEndpointInputDefinition definition in
                         config.InputDefinitions)
                {
                    if (!TryResolveInput(
                            definition.Key,
                            inputValues,
                            currentSession,
                            out string value) ||
                        string.IsNullOrWhiteSpace(value))
                    {
                        if (definition.IsRequired)
                        {
                            return Failure(
                                SessionTokenEndpointErrorCodes.MissingInput,
                                "A required token endpoint input is missing.");
                        }

                        continue;
                    }

                    switch (definition.Placement)
                    {
                        case SessionTokenEndpointInputPlacement.Endpoint:
                            endpoint = endpoint.Replace(
                                "{" + definition.RequestName + "}",
                                Uri.EscapeDataString(value));
                            break;
                        case SessionTokenEndpointInputPlacement.JsonBody:
                            jsonBody[definition.RequestName] = value;
                            break;
                        case SessionTokenEndpointInputPlacement.Query:
                            request.QueryParameters[definition.RequestName] = value;
                            break;
                        case SessionTokenEndpointInputPlacement.Header:
                            request.Headers[definition.RequestName] = value;
                            break;
                        default:
                            return Failure(
                                SessionTokenEndpointErrorCodes.InvalidConfiguration,
                                "Token endpoint configuration is invalid.");
                    }
                }

                if (Regex.IsMatch(endpoint, @"\{[^{}]+\}"))
                {
                    return Failure(
                        SessionTokenEndpointErrorCodes.InvalidConfiguration,
                        "Token endpoint configuration is invalid.");
                }

                request = CopyRequestWithEndpoint(request, endpoint, jsonBody);
                ApiResult<JObject> apiResult =
                    await apiClient.SendAsync<JObject>(
                        request,
                        cancellationToken);
                if (apiResult == null || apiResult.IsFailure || apiResult.Data == null)
                {
                    return Failure(
                        SessionTokenEndpointErrorCodes.RequestFailed,
                        "Token endpoint request failed.");
                }

                return MapResponse(
                    config.ResponseMapping,
                    apiResult.Data,
                    currentSession);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Failure(
                    SessionTokenEndpointErrorCodes.RequestFailed,
                    "Token endpoint request failed.");
            }
            finally
            {
                jsonBody.Clear();
                ClearRequest(request);
            }
        }

        private static ApiRequest CopyRequestWithEndpoint(
            ApiRequest source,
            string endpoint,
            Dictionary<string, string> jsonBody)
        {
            var request = new ApiRequest(
                endpoint,
                source.Method,
                source.Authentication)
            {
                Body = jsonBody.Count == 0 ? null : jsonBody,
                BodyFormat = ApiRequestBodyFormat.Json,
                ResponseFormat = source.ResponseFormat,
                TimeoutSeconds = source.TimeoutSeconds,
                SuppressLogging = true,
                BearerTokenOverride = source.BearerTokenOverride
            };
            foreach (KeyValuePair<string, string> header in source.Headers)
            {
                request.Headers[header.Key] = header.Value;
            }

            foreach (KeyValuePair<string, string> query in source.QueryParameters)
            {
                request.QueryParameters[query.Key] = query.Value;
            }

            ClearRequest(source);
            return request;
        }

        private SessionResult MapResponse(
            SessionTokenEndpointResponseMapping mapping,
            JObject response,
            SessionData currentSession)
        {
            if (!TryReadString(
                    response,
                    mapping.AccessTokenJsonPath,
                    out string accessToken) ||
                !SessionData.IsValidAccessToken(accessToken))
            {
                return InvalidResponse();
            }

            string refreshToken = null;
            if (!string.IsNullOrWhiteSpace(mapping.RefreshTokenJsonPath))
            {
                TryReadString(
                    response,
                    mapping.RefreshTokenJsonPath,
                    out refreshToken);
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                refreshToken = currentSession?.RefreshToken;
            }

            if (!TryResolveExpiry(
                    mapping,
                    response,
                    accessToken,
                    out DateTimeOffset? expiresAtUtc))
            {
                return InvalidResponse();
            }

            try
            {
                return SessionResult.Success(
                    new SessionData(
                        accessToken,
                        refreshToken,
                        expiresAtUtc));
            }
            catch (ArgumentException)
            {
                return InvalidResponse();
            }
        }

        private bool TryResolveExpiry(
            SessionTokenEndpointResponseMapping mapping,
            JObject response,
            string accessToken,
            out DateTimeOffset? expiresAtUtc)
        {
            expiresAtUtc = null;
            if (TrySelectToken(
                    response,
                    mapping.ExpiresAtUtcJsonPath,
                    out JToken absoluteExpiry))
            {
                if (!TryParseAbsoluteExpiry(absoluteExpiry, out DateTimeOffset parsed))
                {
                    return false;
                }

                expiresAtUtc = parsed.ToUniversalTime();
                return true;
            }

            if (TrySelectToken(
                    response,
                    mapping.ExpiresInSecondsJsonPath,
                    out JToken relativeExpiry))
            {
                if (!TryReadNonNegativeSeconds(relativeExpiry, out double seconds))
                {
                    return false;
                }

                expiresAtUtc = utcNowProvider().ToUniversalTime().AddSeconds(seconds);
                return true;
            }

            if (mapping.UseJwtExpiryFallback &&
                TryReadJwtExpiry(accessToken, out DateTimeOffset jwtExpiry))
            {
                expiresAtUtc = jwtExpiry.ToUniversalTime();
            }

            return true;
        }

        private static bool TryParseAbsoluteExpiry(
            JToken token,
            out DateTimeOffset value)
        {
            value = default(DateTimeOffset);
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer &&
                long.TryParse(
                    token.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long unixSeconds))
            {
                try
                {
                    value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
            }

            return DateTimeOffset.TryParse(
                token.ToString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value);
        }

        private static bool TryReadNonNegativeSeconds(
            JToken token,
            out double seconds)
        {
            return double.TryParse(
                       token?.ToString(),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out seconds) &&
                   !double.IsNaN(seconds) &&
                   !double.IsInfinity(seconds) &&
                   seconds >= 0d;
        }

        private static bool TryReadJwtExpiry(
            string accessToken,
            out DateTimeOffset expiresAtUtc)
        {
            expiresAtUtc = default(DateTimeOffset);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return false;
            }

            string[] segments = accessToken.Split('.');
            if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[1]))
            {
                return false;
            }

            try
            {
                string payload = segments[1]
                    .Replace('-', '+')
                    .Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;
                    case 3:
                        payload += "=";
                        break;
                    case 1:
                        return false;
                }

                JObject json = JObject.Parse(
                    Encoding.UTF8.GetString(
                        Convert.FromBase64String(payload)));
                JToken expiry = json["exp"];
                if (expiry == null ||
                    !long.TryParse(
                        expiry.ToString(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out long unixSeconds))
                {
                    return false;
                }

                expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryReadString(
            JObject response,
            string jsonPath,
            out string value)
        {
            value = null;
            if (!TrySelectToken(response, jsonPath, out JToken token))
            {
                return false;
            }

            if (token.Type != JTokenType.String)
            {
                return false;
            }

            value = token.Value<string>();
            value = value?.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TrySelectToken(
            JObject response,
            string jsonPath,
            out JToken token)
        {
            token = null;
            if (response == null || string.IsNullOrWhiteSpace(jsonPath))
            {
                return false;
            }

            try
            {
                token = response.SelectToken(jsonPath, false);
                return token != null && token.Type != JTokenType.Null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryResolveInput(
            string key,
            SessionTokenEndpointInputValues inputValues,
            SessionData currentSession,
            out string value)
        {
            if (inputValues != null && inputValues.TryGetValue(key, out value))
            {
                return true;
            }

            if (string.Equals(
                    key,
                    SessionTokenEndpointReservedInputKeys.AccessToken,
                    StringComparison.Ordinal))
            {
                value = currentSession?.AccessToken;
                return !string.IsNullOrWhiteSpace(value);
            }

            if (string.Equals(
                    key,
                    SessionTokenEndpointReservedInputKeys.RefreshToken,
                    StringComparison.Ordinal))
            {
                value = currentSession?.RefreshToken;
                return !string.IsNullOrWhiteSpace(value);
            }

            value = null;
            return false;
        }

        private static SessionResult InvalidResponse()
        {
            return Failure(
                SessionTokenEndpointErrorCodes.InvalidResponse,
                "Token endpoint returned an invalid authentication response.");
        }

        private static SessionResult Failure(string code, string message)
        {
            return SessionResult.Failed(code, message);
        }

        private static void ClearRequest(ApiRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (request.Body is IDictionary<string, string> body)
            {
                body.Clear();
            }

            request.Body = null;
            request.BearerTokenOverride = null;
            request.Headers.Clear();
            request.QueryParameters.Clear();
        }
    }
}
