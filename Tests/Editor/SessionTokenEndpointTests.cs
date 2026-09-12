using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Session.APIIntegration.Tests
{
    public sealed class SessionTokenEndpointTests
    {
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void ProfileCreatesCredentialFreeRuntimeSnapshot()
        {
            SessionTokenEndpointConfig config = CreateLoginConfig();
            SessionTokenEndpointProfile profile =
                SessionTokenEndpointProfile.CreateRuntime(config);
            try
            {
                SessionTokenEndpointConfig snapshot = profile.CreateConfig();

                Assert.That(snapshot.EndpointTemplate, Is.EqualTo(config.EndpointTemplate));
                Assert.That(snapshot.Method, Is.EqualTo(HttpMethod.POST));
                Assert.That(snapshot.InputDefinitions.Count, Is.EqualTo(5));
                Assert.That(snapshot.InputDefinitions[2].DisplayName, Is.EqualTo("Password"));
                Assert.That(snapshot.InputDefinitions[2].IsSecret, Is.True);
                Assert.That(snapshot.ResponseMapping.AccessTokenJsonPath,
                    Is.EqualTo("payload.access"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void LoginMapsAllInputPlacementsAndSuppressesLogging()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient
                {
                    NextResult = SuccessResponse(
                        "{\"payload\":{\"access\":\"synthetic-access\"," +
                        "\"refresh\":\"synthetic-refresh\",\"expires_in\":90}}")
                };
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (SessionTokenEndpointInputValues inputs = CreateLoginInputs())
                {
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.Succeeded, Is.True);
                    Assert.That(result.Session.AccessToken, Is.EqualTo("synthetic-access"));
                    Assert.That(result.Session.RefreshToken, Is.EqualTo("synthetic-refresh"));
                    Assert.That(result.Session.ExpiresAtUtc, Is.EqualTo(Now.AddSeconds(90)));
                }

                Assert.That(client.Snapshot.SuppressLogging, Is.True);
                Assert.That(client.Snapshot.Authentication,
                    Is.EqualTo(ApiAuthenticationRequirement.Disabled));
                Assert.That(client.Snapshot.Endpoint,
                    Is.EqualTo("https://team%20one.example.invalid/api/v2/login"));
                Assert.That(client.Snapshot.Body["identity"], Is.EqualTo("developer"));
                Assert.That(client.Snapshot.Body["password"], Is.EqualTo("synthetic-secret"));
                Assert.That(client.Snapshot.Query["client"], Is.EqualTo("viewer"));
                Assert.That(client.Snapshot.Headers["X-Flow"], Is.EqualTo("local-tool"));
            });
        }

        [Test]
        public void LoginServiceCanPopulateAuthoritativeSession()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient
                {
                    NextResult = SuccessResponse(
                        "{\"payload\":{\"access\":\"session-access\"}}")
                };
                var loginService = new SessionTokenEndpointLoginService(
                    client,
                    CreateLoginConfig());
                var sessionService = new SessionService(new InMemorySessionStore());
                using (SessionTokenEndpointInputValues inputs = CreateLoginInputs())
                {
                    SessionResult result = await sessionService.LoginAsync(
                        inputs,
                        loginService);

                    Assert.That(result.Succeeded, Is.True);
                    Assert.That(sessionService.CurrentSession.AccessToken,
                        Is.EqualTo("session-access"));
                }

                Assert.That(client.Snapshot.SuppressLogging, Is.True);
            });
        }

        [Test]
        public void JwtExpiryIsUsedWhenMappedExpiryIsAbsent()
        {
            RunAsync(async () =>
            {
                long wholeSeconds = Now.AddMinutes(15).ToUnixTimeSeconds();
                string numericDate = wholeSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
                    ".625";
                string jwt = CreateSyntheticJwt(numericDate);
                var client = new RecordingApiClient
                {
                    NextResult = SuccessResponse(
                        new JObject(
                            new JProperty(
                                "payload",
                                new JObject(new JProperty("access", jwt)))))
                };
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (SessionTokenEndpointInputValues inputs = CreateLoginInputs())
                {
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.Succeeded, Is.True);
                    Assert.That(result.Session.ExpiresAtUtc,
                        Is.EqualTo(
                            DateTimeOffset.FromUnixTimeSeconds(wholeSeconds)
                                .AddTicks(6250000)));
                }
            });
        }

        [Test]
        public void PublicJwtExpiryResolverAcceptsIntegerNumericDates()
        {
            long unixSeconds = Now.AddMinutes(10).ToUnixTimeSeconds();
            string jwt = CreateSyntheticJwt(
                unixSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));

            bool resolved =
                SessionAccessTokenExpiryResolver.TryResolveJwtExpiry(
                    jwt,
                    out DateTimeOffset expiresAtUtc);

            Assert.That(resolved, Is.True);
            Assert.That(
                expiresAtUtc,
                Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(unixSeconds)));
        }

        [Test]
        public void PublicJwtExpiryResolverAcceptsNumericStrings()
        {
            long wholeSeconds = Now.AddMinutes(20).ToUnixTimeSeconds();
            string numericDate = wholeSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                ".125";
            string jwt = CreateSyntheticJwt("\"" + numericDate + "\"");

            bool resolved =
                SessionAccessTokenExpiryResolver.TryResolveJwtExpiry(
                    jwt,
                    out DateTimeOffset expiresAtUtc);

            Assert.That(resolved, Is.True);
            Assert.That(
                expiresAtUtc,
                Is.EqualTo(
                    DateTimeOffset.FromUnixTimeSeconds(wholeSeconds)
                        .AddTicks(1250000)));
            Assert.That(expiresAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
        }

        [TestCase("\"NaN\"")]
        [TestCase("\"Infinity\"")]
        [TestCase("true")]
        [TestCase("\"9999999999999999999999999999\"")]
        public void PublicJwtExpiryResolverRejectsInvalidNumericDates(
            string expiryJson)
        {
            bool resolved =
                SessionAccessTokenExpiryResolver.TryResolveJwtExpiry(
                    CreateSyntheticJwt(expiryJson),
                    out DateTimeOffset expiresAtUtc);

            Assert.That(resolved, Is.False);
            Assert.That(expiresAtUtc, Is.EqualTo(default(DateTimeOffset)));
        }

        [Test]
        public void ExplicitRefreshEndpointUsesReservedSessionValues()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient
                {
                    NextResult = SuccessResponse(
                        "{\"access_token\":\"rotated-access\"}")
                };
                var config = new SessionTokenEndpointConfig(
                    "https://auth.example.invalid/api/refresh",
                    new[]
                    {
                        new SessionTokenEndpointInputDefinition(
                            SessionTokenEndpointReservedInputKeys.RefreshToken,
                            "refresh_token",
                            "Refresh token",
                            SessionTokenEndpointInputPlacement.JsonBody,
                            true)
                    },
                    new SessionTokenEndpointResponseMapping(),
                    useCurrentAccessTokenAsBearer: true);
                var refreshService =
                    new SessionTokenEndpointRefreshService(client, config);
                var current = new SessionData(
                    "current-access",
                    "current-refresh",
                    Now.AddMinutes(1));

                SessionResult result = await refreshService.RefreshAsync(current);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Session.AccessToken, Is.EqualTo("rotated-access"));
                Assert.That(result.Session.RefreshToken, Is.EqualTo("current-refresh"));
                Assert.That(client.Snapshot.Body["refresh_token"],
                    Is.EqualTo("current-refresh"));
                Assert.That(client.Snapshot.BearerTokenOverride,
                    Is.EqualTo("current-access"));
                Assert.That(client.Snapshot.SuppressLogging, Is.True);
            });
        }

        [Test]
        public void ApiFailureReturnsOnlySanitizedSessionError()
        {
            RunAsync(async () =>
            {
                const string credential = "credential-sentinel";
                const string rawBody = "raw-body-sentinel";
                const string requestUrl =
                    "https://private-tenant.example.invalid/login";
                var client = new RecordingApiClient
                {
                    NextResult = ApiResult<JObject>.Failure(
                        new ApiError
                        {
                            Message = credential,
                            BackendMessage = rawBody,
                            RawResponseBody = rawBody,
                            RequestUrl = requestUrl,
                            Exception = new InvalidOperationException(credential)
                        },
                        HttpMethod.POST)
                };
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (SessionTokenEndpointInputValues inputs = CreateLoginInputs())
                {
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.IsFailure, Is.True);
                    Assert.That(result.Error.Code,
                        Is.EqualTo(SessionTokenEndpointErrorCodes.RequestFailed));
                    Assert.That(result.Error.Exception, Is.Null);
                    Assert.That(result.Error.Message, Does.Not.Contain(credential));
                    Assert.That(result.Error.Message, Does.Not.Contain(rawBody));
                    Assert.That(result.Error.Message, Does.Not.Contain(requestUrl));
                    Assert.That(result.Error.Message, Does.Not.Contain("synthetic-secret"));
                    Assert.That(result.Error.Message, Does.Not.Contain("synthetic-access"));
                }

                Assert.That(client.Snapshot.SuppressLogging, Is.True);
            });
        }

        [TestCase(
            401L,
            SessionTokenEndpointErrorCodes.AuthenticationRejected)]
        [TestCase(
            403L,
            SessionTokenEndpointErrorCodes.AuthenticationRejected)]
        [TestCase(
            500L,
            SessionTokenEndpointErrorCodes.ServiceUnavailable)]
        [TestCase(404L, SessionTokenEndpointErrorCodes.EndpointNotFound)]
        [TestCase(405L, SessionTokenEndpointErrorCodes.MethodNotAllowed)]
        [TestCase(400L, SessionTokenEndpointErrorCodes.InvalidRequest)]
        [TestCase(422L, SessionTokenEndpointErrorCodes.InvalidRequest)]
        [TestCase(408L, SessionTokenEndpointErrorCodes.RequestTimeout)]
        [TestCase(504L, SessionTokenEndpointErrorCodes.RequestTimeout)]
        [TestCase(429L, SessionTokenEndpointErrorCodes.RateLimited)]
        [TestCase(503L, SessionTokenEndpointErrorCodes.ServiceUnavailable)]
        [TestCase(0L, SessionTokenEndpointErrorCodes.RequestFailed)]
        public void ApiFailuresPreserveSanitizedStatusClassification(
            long statusCode,
            string expectedCode)
        {
            RunAsync(async () =>
            {
                const string sensitiveMessage = "sensitive-response-sentinel";
                var client = new RecordingApiClient
                {
                    NextResult = ApiResult<JObject>.Failure(
                        new ApiError
                        {
                            HttpStatusCode = statusCode,
                            Message = sensitiveMessage,
                            BackendMessage = sensitiveMessage,
                            RawResponseBody = sensitiveMessage
                        },
                        HttpMethod.POST)
                };
                var executor = new SessionTokenEndpointExecutor(
                    client,
                    () => Now);
                using (SessionTokenEndpointInputValues inputs =
                       CreateLoginInputs())
                {
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.IsFailure, Is.True);
                    Assert.That(result.Error.Code, Is.EqualTo(expectedCode));
                    Assert.That(
                        result.Error.Message,
                        Does.Not.Contain(sensitiveMessage));
                    Assert.That(result.Error.Exception, Is.Null);
                }

                Assert.That(client.Snapshot.SuppressLogging, Is.True);
            });
        }

        [Test]
        public void NonStringAccessTokenIsRejected()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient
                {
                    NextResult = SuccessResponse(
                        "{\"payload\":{\"access\":12345}}")
                };
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (SessionTokenEndpointInputValues inputs = CreateLoginInputs())
                {
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.IsFailure, Is.True);
                    Assert.That(result.Error.Code,
                        Is.EqualTo(SessionTokenEndpointErrorCodes.InvalidResponse));
                }
            });
        }

        [Test]
        public void UnresolvedEndpointPlaceholderIsRejectedBeforeTransport()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient();
                var config = new SessionTokenEndpointConfig(
                    "https://{tenant}.{environment}.example.invalid/login",
                    new[]
                    {
                        new SessionTokenEndpointInputDefinition(
                            "tenant",
                            "tenant",
                            placement: SessionTokenEndpointInputPlacement.Endpoint)
                    },
                    new SessionTokenEndpointResponseMapping());
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (var inputs = new SessionTokenEndpointInputValues())
                {
                    inputs.Set("tenant", "team");
                    SessionResult result = await executor.ExecuteAsync(config, inputs);

                    Assert.That(result.IsFailure, Is.True);
                    Assert.That(result.Error.Code,
                        Is.EqualTo(
                            SessionTokenEndpointErrorCodes.InvalidConfiguration));
                    Assert.That(client.CallCount, Is.EqualTo(0));
                }
            });
        }

        [Test]
        public void MissingRequiredInputReturnsSanitizedErrorBeforeTransport()
        {
            RunAsync(async () =>
            {
                var client = new RecordingApiClient();
                var executor = new SessionTokenEndpointExecutor(client, () => Now);
                using (var inputs = new SessionTokenEndpointInputValues())
                {
                    inputs.Set("tenant", "team");
                    SessionResult result = await executor.ExecuteAsync(
                        CreateLoginConfig(),
                        inputs);

                    Assert.That(result.IsFailure, Is.True);
                    Assert.That(result.Error.Code,
                        Is.EqualTo(SessionTokenEndpointErrorCodes.MissingInput));
                    Assert.That(result.Error.Message, Does.Not.Contain("password"));
                    Assert.That(client.CallCount, Is.EqualTo(0));
                }
            });
        }

        private static SessionTokenEndpointConfig CreateLoginConfig()
        {
            return new SessionTokenEndpointConfig(
                "https://{tenant}.example.invalid/api/v2/login",
                new[]
                {
                    new SessionTokenEndpointInputDefinition(
                        "tenant",
                        "tenant",
                        "Tenant",
                        SessionTokenEndpointInputPlacement.Endpoint),
                    new SessionTokenEndpointInputDefinition(
                        "identity",
                        "identity",
                        "Identity"),
                    new SessionTokenEndpointInputDefinition(
                        "password",
                        "password",
                        "Password",
                        isSecret: true),
                    new SessionTokenEndpointInputDefinition(
                        "client",
                        "client",
                        "Client",
                        SessionTokenEndpointInputPlacement.Query),
                    new SessionTokenEndpointInputDefinition(
                        "flow",
                        "X-Flow",
                        "Flow",
                        SessionTokenEndpointInputPlacement.Header)
                },
                new SessionTokenEndpointResponseMapping(
                    "payload.access",
                    "payload.refresh",
                    expiresInSecondsJsonPath: "payload.expires_in"));
        }

        private static SessionTokenEndpointInputValues CreateLoginInputs()
        {
            var inputs = new SessionTokenEndpointInputValues();
            inputs.Set("tenant", "team one");
            inputs.Set("identity", "developer");
            inputs.Set("password", "synthetic-secret");
            inputs.Set("client", "viewer");
            inputs.Set("flow", "local-tool");
            return inputs;
        }

        private static ApiResult<JObject> SuccessResponse(string json)
        {
            return SuccessResponse(JObject.Parse(json));
        }

        private static ApiResult<JObject> SuccessResponse(JObject json)
        {
            return ApiResult<JObject>.Success(
                json,
                HttpMethod.POST,
                200,
                "https://response.example.invalid",
                null);
        }

        private static string CreateSyntheticJwt(string expiryJson)
        {
            return "synthetic." +
                   ToBase64Url(
                       "{\"exp\":" + expiryJson + "}") +
                   ".signature";
        }

        private static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static void RunAsync(Func<Task> action)
        {
            action().GetAwaiter().GetResult();
        }

        private sealed class RequestSnapshot
        {
            public string Endpoint;
            public ApiAuthenticationRequirement Authentication;
            public bool SuppressLogging;
            public string BearerTokenOverride;
            public Dictionary<string, string> Body;
            public Dictionary<string, string> Headers;
            public Dictionary<string, string> Query;
        }

        private sealed class RecordingApiClient : IApiClient
        {
            public ApiResult<JObject> NextResult;
            public RequestSnapshot Snapshot;
            public int CallCount;

            public Task<ApiResult<TResponse>> SendAsync<TResponse>(
                ApiRequest request,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                Snapshot = new RequestSnapshot
                {
                    Endpoint = request.Endpoint,
                    Authentication = request.Authentication,
                    SuppressLogging = request.SuppressLogging,
                    BearerTokenOverride = request.BearerTokenOverride,
                    Body = request.Body is IDictionary<string, string> body
                        ? new Dictionary<string, string>(body)
                        : new Dictionary<string, string>(),
                    Headers = new Dictionary<string, string>(request.Headers),
                    Query = new Dictionary<string, string>(request.QueryParameters)
                };

                object result = NextResult;
                return Task.FromResult((ApiResult<TResponse>)result);
            }

            public Task<ApiResult<TResponse>> SendAsync<TResponse>(
                ApiEndpoint endpoint,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> SendAsync<TResponse>(
                ApiEndpoint endpoint,
                object body,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> GetAsync<TResponse>(
                string endpoint,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> PostAsync<TResponse>(
                string endpoint,
                object body,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> PutAsync<TResponse>(
                string endpoint,
                object body,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> PatchAsync<TResponse>(
                string endpoint,
                object body,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
                string endpoint,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }
        }
    }
}
