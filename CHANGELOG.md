# Changelog

## 1.0.4 - 2026-06-22

- Updated exact API and Session dependencies for the accepted stable release line.

## 1.0.3 - 2026-06-22

- Updated the exact `com.deucarian.api` dependency to `1.1.1`.

## 1.0.2 - 2026-06-22

- Updated package repository metadata to `Deucarian/Session-API-Integration`.
- Updated active README installation examples to use the current Integration repository URL.

## 1.0.1 - 2026-06-17

- Renamed the package identity from `com.deucarian.session.api-bridge` to `com.deucarian.session.api-integration`.
- Renamed APIBridge assemblies, namespaces, tests, and samples to APIIntegration.
- Migration: remove the old bridge package ID from Unity manifests and add `com.deucarian.session.api-integration`.

## 1.0.0

- Initial integration package.
- Added `SessionAuthProvider` for APIHelper authentication backed by Session Helper.
- Added editor tests and a minimal sample script.
