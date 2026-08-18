using System;
using UnityEngine;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>Where a transient token-endpoint input is written.</summary>
    public enum SessionTokenEndpointInputPlacement
    {
        /// <summary>Replaces a named placeholder in the endpoint template.</summary>
        Endpoint = 0,

        /// <summary>Adds a string property to the JSON request body.</summary>
        JsonBody = 1,

        /// <summary>Adds a query-string parameter.</summary>
        Query = 2,

        /// <summary>Adds a request header.</summary>
        Header = 3
    }

    /// <summary>
    /// Credential-free description of one value required by a token endpoint.
    /// Values are supplied separately through <see cref="SessionTokenEndpointInputValues"/>.
    /// </summary>
    [Serializable]
    public sealed class SessionTokenEndpointInputDefinition
    {
        [SerializeField] private string key = string.Empty;
        [SerializeField] private string requestName = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private SessionTokenEndpointInputPlacement placement =
            SessionTokenEndpointInputPlacement.JsonBody;
        [SerializeField] private bool isSecret;
        [SerializeField] private bool isRequired = true;

        /// <summary>Creates an empty definition for Unity serialization.</summary>
        public SessionTokenEndpointInputDefinition()
        {
        }

        /// <summary>Creates an input definition.</summary>
        public SessionTokenEndpointInputDefinition(
            string key,
            string requestName,
            string displayName = null,
            SessionTokenEndpointInputPlacement placement =
                SessionTokenEndpointInputPlacement.JsonBody,
            bool isSecret = false,
            bool isRequired = true)
        {
            this.key = key?.Trim();
            this.requestName = requestName?.Trim();
            this.displayName = displayName?.Trim();
            this.placement = placement;
            this.isSecret = isSecret;
            this.isRequired = isRequired;
        }

        /// <summary>Stable key used to look up the transient value.</summary>
        public string Key => key;

        /// <summary>
        /// JSON property, query key, header name, or endpoint placeholder name.
        /// </summary>
        public string RequestName => requestName;

        /// <summary>Human-readable label suitable for a consuming UI.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? key
            : displayName;

        /// <summary>Where the value is written in the request.</summary>
        public SessionTokenEndpointInputPlacement Placement => placement;

        /// <summary>Whether a consuming UI must mask this value.</summary>
        public bool IsSecret => isSecret;

        /// <summary>Whether execution must fail when the value is absent.</summary>
        public bool IsRequired => isRequired;

        internal SessionTokenEndpointInputDefinition Clone()
        {
            return new SessionTokenEndpointInputDefinition(
                key?.Trim(),
                requestName?.Trim(),
                displayName?.Trim(),
                placement,
                isSecret,
                isRequired);
        }
    }
}
