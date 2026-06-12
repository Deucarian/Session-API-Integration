using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using UnityEngine;

namespace Deucarian.Session.APIBridge.Samples
{
    /// <summary>
    /// Minimal sample showing how to pass Session tokens to API.
    /// </summary>
    public sealed class SessionAuthProviderSample : MonoBehaviour
    {
        [SerializeField] private ApiClientConfig apiClientConfig;

        private IApiClient apiClient;
        private ISessionService sessionService;

        private void Awake()
        {
            sessionService = new SessionService(
                new PlayerPrefsSessionStore("session.api.sample"),
                new FakeRefreshService());

            var authProvider = new SessionAuthProvider(sessionService);
            apiClient = ApiClientFactory.Create(apiClientConfig, authProvider);
        }

        private sealed class FakeRefreshService : ISessionRefreshService
        {
            public Task<SessionResult> RefreshAsync(
                SessionData currentSession,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(
                    SessionResult.Success(
                        new SessionData(
                            "sample-refreshed-access-token",
                            currentSession.RefreshToken,
                            DateTimeOffset.UtcNow.AddMinutes(15))));
            }
        }
    }
}
