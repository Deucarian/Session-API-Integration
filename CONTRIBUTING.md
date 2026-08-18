# Contributing

Thanks for helping improve the Session API integration.

## Development Guidelines

- Keep this package focused on adapting Session to API.
- Do not change API from this package.
- Do not change Session core runtime APIs from this package.
- Keep token endpoint execution declarative and backend-neutral; backend-specific presets belong in consuming packages.
- Never serialize credentials in a `SessionTokenEndpointProfile` or include token exchange values in errors or logs.
- Every token exchange must set `ApiRequest.SuppressLogging`.
- Add XML documentation for public runtime APIs.
- Add editor tests for behavior changes.

## Testing

Run the package editor tests from Unity's Test Runner in a project that has Session, API, and this integration package installed.
