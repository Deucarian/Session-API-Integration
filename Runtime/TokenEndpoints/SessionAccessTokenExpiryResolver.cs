using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deucarian.Session.APIIntegration
{
    /// <summary>
    /// Reads optional expiry metadata from an access token without validating
    /// the token, its signature, issuer, audience, or current server status.
    /// </summary>
    public static class SessionAccessTokenExpiryResolver
    {
        private static readonly DateTimeOffset UnixEpoch =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Attempts to read the JWT <c>exp</c> NumericDate claim as a UTC time.
        /// Integer and finite fractional JSON numbers or numeric strings are
        /// accepted. This is metadata inspection only and never establishes
        /// that a token is authentic or accepted by its server.
        /// </summary>
        /// <param name="accessToken">Raw compact token without a Bearer prefix.</param>
        /// <param name="expiresAtUtc">Resolved UTC expiry when available.</param>
        /// <returns>True when a valid, in-range NumericDate was resolved.</returns>
        public static bool TryResolveJwtExpiry(
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
                return TryResolveNumericDate(
                    json["exp"],
                    out expiresAtUtc);
            }
            catch (Exception)
            {
                expiresAtUtc = default(DateTimeOffset);
                return false;
            }
        }

        private static bool TryResolveNumericDate(
            JToken token,
            out DateTimeOffset expiresAtUtc)
        {
            expiresAtUtc = default(DateTimeOffset);
            if (token == null ||
                (token.Type != JTokenType.Integer &&
                 token.Type != JTokenType.Float &&
                 token.Type != JTokenType.String))
            {
                return false;
            }

            string text = token.Type == JTokenType.String
                ? token.Value<string>()
                : token.ToString(Formatting.None);
            if (string.IsNullOrWhiteSpace(text) ||
                !decimal.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out decimal unixSeconds))
            {
                return false;
            }

            try
            {
                decimal ticksFromEpoch = decimal.Floor(
                    unixSeconds * TimeSpan.TicksPerSecond);
                decimal minimumTicks =
                    DateTimeOffset.MinValue.UtcDateTime.Ticks -
                    UnixEpoch.UtcDateTime.Ticks;
                decimal maximumTicks =
                    DateTimeOffset.MaxValue.UtcDateTime.Ticks -
                    UnixEpoch.UtcDateTime.Ticks;
                if (ticksFromEpoch < minimumTicks ||
                    ticksFromEpoch > maximumTicks)
                {
                    return false;
                }

                expiresAtUtc = UnixEpoch.AddTicks((long)ticksFromEpoch);
                return true;
            }
            catch (Exception exception)
                when (exception is OverflowException ||
                      exception is ArgumentOutOfRangeException)
            {
                expiresAtUtc = default(DateTimeOffset);
                return false;
            }
        }
    }
}
