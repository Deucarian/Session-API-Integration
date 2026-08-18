# Deucarian Session API Integration

`com.deucarian.session.api-integration` connects Deucarian Session to Deucarian API authentication. Version `1.1.0` also provides backend-neutral, credential-safe token endpoint profiles and adapters.

Use it when an application needs either:

- API requests authenticated by the current `ISessionService` token.
- A reusable token acquisition flow configured by endpoint, transient inputs, and response JSON paths.
- A genuine refresh endpoint integrated with Session without duplicating request or mapping logic.

Do not put product-specific endpoints, credentials, authentication UI, or session storage in this package.

## Install

Stable:

```json
"com.deucarian.session.api-integration": "https://github.com/Deucarian/Session-API-Integration.git#main"
```

Development:

```json
"com.deucarian.session.api-integration": "https://github.com/Deucarian/Session-API-Integration.git#develop"
```

Dependencies:

- `com.deucarian.api` `1.1.5`
- `com.deucarian.session` `1.0.6`
- `com.unity.nuget.newtonsoft-json` `3.2.2`

Unity 2021.3 or newer is required. No scripting define symbols are required.

## Public API

- `SessionAuthProvider`: exposes the current Session token through API's `IApiAuthProvider`.
- `SessionTokenEndpointProfile`: credential-free ScriptableObject profile suitable for a project asset.
- `SessionTokenEndpointConfig`: immutable runtime snapshot of a profile.
- `SessionTokenEndpointInputDefinition`: describes a transient field, placement, label, required state, and masking hint.
- `SessionTokenEndpointInputValues`: disposable in-memory values supplied for one exchange.
- `SessionTokenEndpointResponseMapping`: maps access token, optional refresh token, and optional expiry JSON paths.
- `SessionAccessTokenExpiryResolver`: reads an unverified JWT `exp` NumericDate for local expiry presentation without claiming token validity.
- `SessionTokenEndpointExecutor`: sends a suppressed-log token request and returns sanitized `SessionResult` data.
- `SessionTokenEndpointLoginService`: `ISessionLoginService<SessionTokenEndpointInputValues>` adapter.
- `SessionTokenEndpointRefreshService`: `ISessionRefreshService` adapter for an explicitly separate refresh endpoint.

## Session-backed API authentication

```csharp
using Deucarian.API.Core;
using Deucarian.Session;
using Deucarian.Session.APIIntegration;

ISessionService sessionService = new SessionService(
    new PlayerPrefsSessionStore("my-game.session"),
    new MyRefreshService());

var authProvider = new SessionAuthProvider(sessionService);
IApiClient apiClient = ApiClientFactory.Create(apiClientConfig, authProvider);
```

`SessionAuthProvider` returns the token without a `Bearer` prefix. API owns authorization-header formatting. By default it asks Session to refresh when an access token is expired or expiring soon. Pass `refreshIfExpiredOrExpiringSoon: false` to disable that behavior.

## Configurable token acquisition

Create a credential-free asset from `Assets > Create > Deucarian > Session > Token Endpoint Profile`, or construct the same configuration in code:

```csharp
var config = new SessionTokenEndpointConfig(
    "https://{tenant}.auth.example.invalid/token",
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
            isSecret: true)
    },
    new SessionTokenEndpointResponseMapping(
        accessTokenJsonPath: "access_token",
        refreshTokenJsonPath: "refresh_token",
        expiresInSecondsJsonPath: "expires_in"));

var loginService = new SessionTokenEndpointLoginService(apiClient, config);
using (var inputs = new SessionTokenEndpointInputValues())
{
    inputs.Set("tenant", "example");
    inputs.Set("identity", "synthetic-user");
    inputs.Set("password", "synthetic-password");
    SessionResult result = await sessionService.LoginAsync(inputs, loginService);
}
```

Input definitions support endpoint placeholders, JSON body fields, query parameters, and headers. Definitions contain labels and masking hints only. Actual values are not Unity serializable and are never written into the profile.

Response expiry supports an ISO-8601 timestamp, Unix seconds, seconds-from-now, or an optional JWT `exp` fallback. JWT NumericDate values may be integer or finite fractional JSON numbers or numeric strings. `SessionAccessTokenExpiryResolver.TryResolveJwtExpiry` exposes the same parser for manual and remembered-token flows. Parsed JWT metadata does not validate a token's signature, issuer, audience, revocation state, or server acceptance. When a separately configured refresh endpoint omits a replacement refresh token, the current refresh token is preserved.

HTTP 401 and 403 responses use the sanitized `SessionTokenEndpointErrorCodes.AuthenticationRejected` code. Other transport and server failures continue to use `RequestFailed`; raw response data and backend messages are never copied into Session errors.

## Credential safety

Every exchange sets `ApiRequest.SuppressLogging = true`, including failed exchanges. Returned `SessionError` values use stable package codes and never include request URLs, raw response bodies, backend messages, credentials, or tokens.

Consumers should dispose `SessionTokenEndpointInputValues` immediately after use. They must never place real credentials in profile assets, repository files, logs, diagnostics, or exception messages.

## Honest refresh semantics

`SessionTokenEndpointRefreshService` must only be constructed from a genuine, separately configured refresh endpoint. Use `SessionTokenEndpointReservedInputKeys.RefreshToken` and `AccessToken` in that profile to map current Session values.

Do not present login or reacquisition as silent OAuth refresh when a backend does not document refresh semantics. Use `SessionTokenEndpointLoginService` for interactive acquisition or reauthentication instead.

## Samples

The `Basic API Integration Usage` sample demonstrates `SessionAuthProvider` with synthetic session data. It does not make backend calls.

## Ownership boundary

This package owns generic Session/API adapter behavior. It does not own:

- Session storage or Session core runtime APIs.
- Product-specific endpoint presets or login modes.
- Credential persistence or authentication UI.
- API transport, logging, or authorization-header formatting.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package EditMode tests after code or assembly changes and always run `git diff --check`.

See [AGENTS.md](AGENTS.md), [CONTRIBUTING.md](CONTRIBUTING.md), and the Package Registry architecture documents for contributor rules.

## License

See [LICENSE.md](LICENSE.md).
