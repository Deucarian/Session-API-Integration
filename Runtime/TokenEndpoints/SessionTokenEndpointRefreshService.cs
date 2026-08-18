using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Refresh adapter for an explicitly separate refresh endpoint profile.
    /// Do not use a login/reacquisition endpoint as a silent refresh service.
    /// </summary>
    public sealed class SessionTokenEndpointRefreshService : ISessionRefreshService
    {
        private readonly SessionTokenEndpointExecutor executor;
        private readonly SessionTokenEndpointConfig refreshConfig;

        /// <summary>Creates a refresh service from an API client.</summary>
        public SessionTokenEndpointRefreshService(
            IApiClient apiClient,
            SessionTokenEndpointConfig refreshConfig)
            : this(new SessionTokenEndpointExecutor(apiClient), refreshConfig)
        {
        }

        /// <summary>Creates a refresh service from a reusable executor.</summary>
        public SessionTokenEndpointRefreshService(
            SessionTokenEndpointExecutor executor,
            SessionTokenEndpointConfig refreshConfig)
        {
            this.executor = executor ??
                throw new ArgumentNullException(nameof(executor));
            this.refreshConfig = refreshConfig ??
                throw new ArgumentNullException(nameof(refreshConfig));
        }

        /// <inheritdoc />
        public async Task<SessionResult> RefreshAsync(
            SessionData currentSession,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var inputs = new SessionTokenEndpointInputValues())
            {
                return await executor.ExecuteAsync(
                        refreshConfig,
                        inputs,
                        currentSession,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
