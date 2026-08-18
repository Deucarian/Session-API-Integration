using System;
using UnityEngine;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>Credential-free JSON-path mapping for a token response.</summary>
    [Serializable]
    public sealed class SessionTokenEndpointResponseMapping
    {
        [SerializeField] private string accessTokenJsonPath = "access_token";
        [SerializeField] private string refreshTokenJsonPath = string.Empty;
        [SerializeField] private string expiresAtUtcJsonPath = string.Empty;
        [SerializeField] private string expiresInSecondsJsonPath = string.Empty;
        [SerializeField] private bool useJwtExpiryFallback = true;

        /// <summary>Creates the default root <c>access_token</c> mapping.</summary>
        public SessionTokenEndpointResponseMapping()
        {
        }

        /// <summary>Creates a response mapping.</summary>
        public SessionTokenEndpointResponseMapping(
            string accessTokenJsonPath = "access_token",
            string refreshTokenJsonPath = null,
            string expiresAtUtcJsonPath = null,
            string expiresInSecondsJsonPath = null,
            bool useJwtExpiryFallback = true)
        {
            this.accessTokenJsonPath = accessTokenJsonPath;
            this.refreshTokenJsonPath = refreshTokenJsonPath;
            this.expiresAtUtcJsonPath = expiresAtUtcJsonPath;
            this.expiresInSecondsJsonPath = expiresInSecondsJsonPath;
            this.useJwtExpiryFallback = useJwtExpiryFallback;
        }

        /// <summary>Required JSON path containing the access token.</summary>
        public string AccessTokenJsonPath => accessTokenJsonPath;

        /// <summary>Optional JSON path containing a refresh token.</summary>
        public string RefreshTokenJsonPath => refreshTokenJsonPath;

        /// <summary>
        /// Optional JSON path containing an ISO-8601 UTC timestamp or Unix seconds.
        /// </summary>
        public string ExpiresAtUtcJsonPath => expiresAtUtcJsonPath;

        /// <summary>Optional JSON path containing seconds until expiry.</summary>
        public string ExpiresInSecondsJsonPath => expiresInSecondsJsonPath;

        /// <summary>
        /// Whether a missing mapped expiry may fall back to the JWT <c>exp</c> claim.
        /// </summary>
        public bool UseJwtExpiryFallback => useJwtExpiryFallback;

        internal SessionTokenEndpointResponseMapping Clone()
        {
            return new SessionTokenEndpointResponseMapping(
                accessTokenJsonPath,
                refreshTokenJsonPath,
                expiresAtUtcJsonPath,
                expiresInSecondsJsonPath,
                useJwtExpiryFallback);
        }
    }
}
