using System;
using System.Collections.Generic;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Transient token-endpoint values. This type is deliberately not Unity
    /// serializable and clears its references when disposed.
    /// </summary>
    public sealed class SessionTokenEndpointInputValues : IDisposable
    {
        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Sets or replaces a transient value.</summary>
        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("An input key is required.", nameof(key));
            }

            values[key.Trim()] = value;
        }

        /// <summary>Attempts to retrieve a transient value.</summary>
        public bool TryGetValue(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }

            return values.TryGetValue(key.Trim(), out value);
        }

        /// <summary>Creates an independent transient copy.</summary>
        public SessionTokenEndpointInputValues Clone()
        {
            var copy = new SessionTokenEndpointInputValues();
            foreach (KeyValuePair<string, string> pair in values)
            {
                copy.values[pair.Key] = pair.Value;
            }

            return copy;
        }

        /// <summary>Clears all held value references.</summary>
        public void Clear()
        {
            values.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Clear();
        }
    }
}
