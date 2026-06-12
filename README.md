# Deucarian Session API Bridge

## Overview

Deucarian Session API Bridge is a Unity UPM package that connects Session to API authentication.

Package ID: `com.deucarian.session.api-bridge`

Use this package when a Unity project already uses both:

- `com.deucarian.session`
- `com.deucarian.api`

The bridge provides `SessionAuthProvider`, an API `IApiAuthProvider` implementation backed by an `ISessionService`.

No scripting define symbols are required.

## Installation

Install this bridge package alongside Session and API:

```json
{
  "dependencies": {
    "com.deucarian.session": "https://github.com/Deucarian/Session.git#main",
    "com.deucarian.api": "https://github.com/Deucarian/API.git#main",
    "com.deucarian.session.api-bridge": "https://github.com/Deucarian/Session-API-Bridge.git#main"
  }
}
```

For development builds, use the `develop` branch refs for each package.

## Dependencies

This package depends on:

- `com.deucarian.session`
- `com.deucarian.api`

It does not replace either package. It only adapts Session's current session token to API's authentication contract.

## Public API

- `SessionAuthProvider`: implements API's `IApiAuthProvider` and reads tokens from an `ISessionService`.

## Usage

```csharp
using Deucarian.API.Core;
using Deucarian.Session;
using Deucarian.Session.APIBridge;

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

- `Basic API Bridge Usage`
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
