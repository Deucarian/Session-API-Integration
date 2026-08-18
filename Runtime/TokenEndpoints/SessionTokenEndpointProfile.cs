using System.Collections.Generic;
using Deucarian.API;
using UnityEngine;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Credential-free token endpoint asset. It describes where transient
    /// inputs go and how returned session values are mapped.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Deucarian/Session/Token Endpoint Profile",
        fileName = "SessionTokenEndpointProfile")]
    public sealed class SessionTokenEndpointProfile : ScriptableObject
    {
        [SerializeField] private string endpointTemplate = string.Empty;
        [SerializeField] private HttpMethod method = HttpMethod.POST;
        [SerializeField] private int timeoutSeconds = 30;
        [SerializeField] private bool useCurrentAccessTokenAsBearer;
        [SerializeField] private List<SessionTokenEndpointInputDefinition>
            inputDefinitions = new List<SessionTokenEndpointInputDefinition>();
        [SerializeField] private SessionTokenEndpointResponseMapping responseMapping =
            new SessionTokenEndpointResponseMapping();

        /// <summary>Relative endpoint or absolute URL with optional placeholders.</summary>
        public string EndpointTemplate => endpointTemplate;

        /// <summary>HTTP method used by the exchange.</summary>
        public HttpMethod Method => method;

        /// <summary>Per-request timeout in seconds.</summary>
        public int TimeoutSeconds => timeoutSeconds;

        /// <summary>Whether the current access token authenticates the exchange.</summary>
        public bool UseCurrentAccessTokenAsBearer => useCurrentAccessTokenAsBearer;

        /// <summary>Credential-free input descriptions for forms and request mapping.</summary>
        public IReadOnlyList<SessionTokenEndpointInputDefinition> InputDefinitions =>
            inputDefinitions.AsReadOnly();

        /// <summary>Response JSON mapping.</summary>
        public SessionTokenEndpointResponseMapping ResponseMapping => responseMapping;

        /// <summary>Creates an immutable, validated runtime snapshot.</summary>
        public SessionTokenEndpointConfig CreateConfig()
        {
            return new SessionTokenEndpointConfig(
                endpointTemplate,
                inputDefinitions,
                responseMapping,
                method,
                timeoutSeconds,
                useCurrentAccessTokenAsBearer);
        }

        /// <summary>Creates a runtime-only profile from a validated config.</summary>
        public static SessionTokenEndpointProfile CreateRuntime(
            SessionTokenEndpointConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            SessionTokenEndpointProfile profile =
                CreateInstance<SessionTokenEndpointProfile>();
            profile.endpointTemplate = config.EndpointTemplate;
            profile.method = config.Method;
            profile.timeoutSeconds = config.TimeoutSeconds;
            profile.useCurrentAccessTokenAsBearer =
                config.UseCurrentAccessTokenAsBearer;
            profile.inputDefinitions =
                new List<SessionTokenEndpointInputDefinition>();
            foreach (SessionTokenEndpointInputDefinition definition in
                     config.InputDefinitions)
            {
                profile.inputDefinitions.Add(definition.Clone());
            }

            profile.responseMapping = config.ResponseMapping.Clone();
            return profile;
        }
    }
}
