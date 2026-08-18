using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Session login adapter for one explicitly configured token endpoint.
    /// </summary>
    public sealed class SessionTokenEndpointLoginService :
        ISessionLoginService<SessionTokenEndpointInputValues>
    {
        private readonly SessionTokenEndpointExecutor executor;
        private readonly SessionTokenEndpointConfig config;

        /// <summary>Creates a login service from an API client.</summary>
        public SessionTokenEndpointLoginService(
            IApiClient apiClient,
            SessionTokenEndpointConfig config)
            : this(new SessionTokenEndpointExecutor(apiClient), config)
        {
        }

        /// <summary>Creates a login service from a reusable executor.</summary>
        public SessionTokenEndpointLoginService(
            SessionTokenEndpointExecutor executor,
            SessionTokenEndpointConfig config)
        {
            this.executor = executor ??
                throw new ArgumentNullException(nameof(executor));
            this.config = config ??
                throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public Task<SessionResult> LoginAsync(
            SessionTokenEndpointInputValues request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return executor.ExecuteAsync(
                config,
                request,
                null,
                cancellationToken);
        }
    }
}
