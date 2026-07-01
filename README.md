# Deucarian Session API Integration

## What this is

`com.deucarian.session.api-integration` is a Unity UPM package that connects Session to API authentication.

The integration provides `SessionAuthProvider`, an API `IApiAuthProvider` implementation backed by an `ISessionService`.

Current package version: `1.0.4`.

## When to use it

- Your project already uses `com.deucarian.session` and `com.deucarian.api`.
- You want API requests to use the current Session access token.
- You want optional refresh-before-auth behavior through Session rather than duplicating token logic in API callers.

## When not to use it

- Do not use this package without both Session and API installed.
- Do not put login, refresh backend calls, session storage, or API request execution here.
- Do not use this package as a replacement for either target package.

Migration note: replace old manifest entries for `com.deucarian.session.api-bridge` with `com.deucarian.session.api-integration`. Current installs use the `Session-API-Integration.git` repository.

No scripting define symbols are required.

## Install

Stable:

```json
"com.deucarian.session.api-integration": "https://github.com/Deucarian/Session-API-Integration.git#main"
```

Development:

```json
"com.deucarian.session.api-integration": "https://github.com/Deucarian/Session-API-Integration.git#develop"
```

Install Session and API from the same channel unless you intentionally need a mixed-channel test project.

## Dependencies

This package depends on:

- `com.deucarian.session` `1.0.4`
- `com.deucarian.api` `1.1.3`

It does not replace either package. It only adapts Session's current session token to API's authentication contract.

## Unity compatibility

Requires Unity 2021.3 or newer.

## Public API map

- `SessionAuthProvider`: implements API's `IApiAuthProvider` and reads tokens from an `ISessionService`.

## Usage

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

`SessionAuthProvider` returns the access token without the `Bearer` prefix. API owns the authorization header formatting.

## Refresh Behavior

By default, `SessionAuthProvider` attempts `ISessionService.RefreshAsync` when the current access token is expired or expiring soon.

```csharp
var authProvider = new SessionAuthProvider(
    sessionService,
    refreshIfExpiredOrExpiringSoon: true);
```

Disable refresh attempts by passing `false`:

```csharp
var authProvider = new SessionAuthProvider(
    sessionService,
    refreshIfExpiredOrExpiringSoon: false);
```

If refresh fails, the provider does not throw or clear the session itself. It returns based on the `ISessionService` state after the refresh attempt:

- Expired sessions return `null`.
- Still-valid expiring-soon sessions may return the current token when `SessionRefreshFailurePolicy.PreserveSession` keeps the session authenticated.

## Samples

The package contains one sample:

- `Basic API Integration Usage`
- Path: `Samples~/BasicUsage`
- Script: `SessionAuthProviderSample`

The sample uses fake session data and a fake refresh service. It does not make backend calls.

## What This Package Does Not Own

- Session storage.
- Login and refresh backend calls.
- API request execution.
- Authorization header formatting.
- API package behavior.
- Session core runtime APIs.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's EditMode tests in Unity after code or assembly definition changes.

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
