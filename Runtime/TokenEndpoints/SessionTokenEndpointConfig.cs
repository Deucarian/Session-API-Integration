using System;
using System.Collections.Generic;
using Deucarian.API;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>Immutable runtime snapshot of a token endpoint profile.</summary>
    public sealed class SessionTokenEndpointConfig
    {
        private readonly IReadOnlyList<SessionTokenEndpointInputDefinition>
            inputDefinitions;

        /// <summary>Creates and validates an endpoint configuration.</summary>
        public SessionTokenEndpointConfig(
            string endpointTemplate,
            IEnumerable<SessionTokenEndpointInputDefinition> inputDefinitions,
            SessionTokenEndpointResponseMapping responseMapping,
            HttpMethod method = HttpMethod.POST,
            int timeoutSeconds = 30,
            bool useCurrentAccessTokenAsBearer = false)
        {
            if (string.IsNullOrWhiteSpace(endpointTemplate))
            {
                throw new ArgumentException(
                    "A token endpoint template is required.",
                    nameof(endpointTemplate));
            }

            if (responseMapping == null ||
                string.IsNullOrWhiteSpace(responseMapping.AccessTokenJsonPath))
            {
                throw new ArgumentException(
                    "An access-token response JSON path is required.",
                    nameof(responseMapping));
            }

            if (timeoutSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    "Timeout seconds cannot be negative.");
            }

            EndpointTemplate = endpointTemplate.Trim();
            Method = method;
            TimeoutSeconds = timeoutSeconds;
            UseCurrentAccessTokenAsBearer = useCurrentAccessTokenAsBearer;
            ResponseMapping = responseMapping.Clone();
            this.inputDefinitions = CloneAndValidateInputs(
                EndpointTemplate,
                inputDefinitions);
        }

        /// <summary>Relative endpoint or absolute URL with optional named placeholders.</summary>
        public string EndpointTemplate { get; }

        /// <summary>HTTP method used by the exchange.</summary>
        public HttpMethod Method { get; }

        /// <summary>Per-request timeout in seconds. Zero uses API defaults.</summary>
        public int TimeoutSeconds { get; }

        /// <summary>Whether to send the current access token as bearer authentication.</summary>
        public bool UseCurrentAccessTokenAsBearer { get; }

        /// <summary>Credential-free transient input descriptions.</summary>
        public IReadOnlyList<SessionTokenEndpointInputDefinition> InputDefinitions =>
            inputDefinitions;

        /// <summary>Token response JSON mapping.</summary>
        public SessionTokenEndpointResponseMapping ResponseMapping { get; }

        private static IReadOnlyList<SessionTokenEndpointInputDefinition>
            CloneAndValidateInputs(
            string endpointTemplate,
            IEnumerable<SessionTokenEndpointInputDefinition> definitions)
        {
            var clones = new List<SessionTokenEndpointInputDefinition>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (definitions == null)
            {
                return clones.AsReadOnly();
            }

            foreach (SessionTokenEndpointInputDefinition definition in definitions)
            {
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.Key) ||
                    string.IsNullOrWhiteSpace(definition.RequestName))
                {
                    throw new ArgumentException(
                        "Every token endpoint input requires a key and request name.",
                        nameof(definitions));
                }

                SessionTokenEndpointInputDefinition normalized =
                    definition.Clone();
                if (!keys.Add(normalized.Key))
                {
                    throw new ArgumentException(
                        "Token endpoint input keys must be unique.",
                        nameof(definitions));
                }

                string destination =
                    ((int)normalized.Placement).ToString() + ":" +
                    normalized.RequestName;
                if (!destinations.Add(destination))
                {
                    throw new ArgumentException(
                        "Token endpoint request destinations must be unique.",
                        nameof(definitions));
                }

                if (normalized.Placement ==
                        SessionTokenEndpointInputPlacement.Endpoint &&
                    endpointTemplate.IndexOf(
                        "{" + normalized.RequestName + "}",
                        StringComparison.Ordinal) < 0)
                {
                    throw new ArgumentException(
                        "An endpoint input does not have a matching placeholder.",
                        nameof(definitions));
                }

                clones.Add(normalized);
            }

            return clones.AsReadOnly();
        }
    }
}
