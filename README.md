# JorisHoef SessionHelper APIHelper Bridge

## Overview

JorisHoef SessionHelper APIHelper Bridge is a Unity UPM package that connects Session Helper to APIHelper authentication.

Package ID: `com.jorishoef.session-helper.api-helper-bridge`

Use this package when a Unity project already uses both:

- `com.jorishoef.session-helper`
- `com.jorishoef.api-helper`

The bridge provides `SessionAuthProvider`, an APIHelper `IApiAuthProvider` implementation backed by an `ISessionService`.

No scripting define symbols are required.

## Installation

Install this bridge package alongside Session Helper and APIHelper:

```json
{
  "dependencies": {
    "com.jorishoef.session-helper": "https://github.com/JorisHoef/Session-Helper.git#main",
    "com.jorishoef.api-helper": "https://github.com/JorisHoef/API-Helper.git#main",
    "com.jorishoef.session-helper.api-helper-bridge": "https://github.com/JorisHoef/SessionHelper-APIHelper-Bridge.git#main"
  }
}
```

For development builds, use the `develop` branch refs for each package.

## Dependencies

This package depends on:

- `com.jorishoef.session-helper`
- `com.jorishoef.api-helper`

It does not replace either package. It only adapts Session Helper's current session token to APIHelper's authentication contract.

## Public API

- `SessionAuthProvider`: implements APIHelper's `IApiAuthProvider` and reads tokens from an `ISessionService`.

## Usage

```csharp
using JorisHoef.APIHelper.Core;
using JorisHoef.SessionHelper;
using JorisHoef.SessionHelper.APIHelper;

ISessionService sessionService = new SessionService(
    new PlayerPrefsSessionStore("my-game.session"),
    new MyRefreshService());

var authProvider = new SessionAuthProvider(sessionService);
IApiClient apiClient = ApiClientFactory.Create(apiClientConfig, authProvider);
```

`SessionAuthProvider` returns the access token without the `Bearer` prefix. APIHelper owns the authorization header formatting.

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

- `Basic APIHelper Bridge Usage`
- Path: `Samples~/BasicUsage`
- Script: `SessionAuthProviderSample`

The sample uses fake session data and a fake refresh service. It does not make backend calls.

## What This Package Does Not Own

- Session storage.
- Login and refresh backend calls.
- API request execution.
- Authorization header formatting.
- APIHelper package behavior.
- Session Helper core runtime APIs.
